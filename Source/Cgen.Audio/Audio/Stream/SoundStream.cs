using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using OpenTK.Audio;
using OpenTK.Audio.OpenAL;

using Cgen;
using Cgen.Internal.OpenAL;

namespace Cgen.Audio
{
    /// <summary>
    /// Represents a streamed audio sources.
    /// </summary>
    public abstract class SoundStream : SoundSource
    {
        private const int BUFFER_COUNT = 3;
        private const int BUFFER_RETRIES = 2;

        private bool   _isStreaming  = false, _stopping = false;
        private int[]  _buffers      = new int[BUFFER_COUNT];
        private int    _channelCount = 0;
        private int    _sampleRate   = 0;
        private bool   _loop         = false;
        private long   _processed    = 0;
        private long[] _bufferSeeks  = new long[BUFFER_COUNT];
        private ALFormat _format     = 0;

        /// <summary>
        /// Gets the current status of current <see cref="SoundStream"/> object.
        /// </summary>
        public override SoundStatus Status
        {
            get
            {
                if (_isStreaming)
                    return SoundStatus.Playing;
                return base.Status;
            }
        }

        /// <summary>
        /// Gets the number of channels used by current <see cref="SoundStream"/> object.
        /// </summary>
        public int ChannelCount
        {
            get { return _channelCount; }
        }

        /// <summary>
        /// Gets the sample rate of current <see cref="SoundStream"/> object.
        /// </summary>
        public int SampleRate
        {
            get { return _sampleRate; }
        }

        /// <summary>
        /// Gets or sets the current playing position of the current <see cref="SoundStream"/> object.
        /// </summary>
        public override TimeSpan PlayingOffset
        {
            get
            {
                if (_sampleRate > 0 && _channelCount > 0)
                {
                    float seconds = 0f;
                    ALChecker.Check(() => AL.GetSource(Handle, ALSourcef.SecOffset, out seconds));

                    return TimeSpan.FromSeconds(seconds + (float)(_processed) / _sampleRate / _channelCount);
                }
                else
                {
                    return TimeSpan.Zero;
                }
            }
            set
            {
                // Let the derived class update the current position
                OnSeek(value);

                // Restart streaming
                _processed = (long)(value.TotalSeconds * _sampleRate * _channelCount);
            }
        }

        /// <summary>
        /// Gets total duration of current <see cref="SoundStream"/> object.
        /// </summary>
        public override abstract TimeSpan Duration
        {
            get;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the current <see cref="SoundStream"/> object is in loop mode.
        /// </summary>
        public override bool IsLooping
        {
            get { return _loop; }
            set { _loop = value; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SoundStream"/> class.
        /// </summary>
        public SoundStream()
        {
        }

        /// <summary>
        /// Process quequed buffer of <see cref="SoundStream"/>.
        /// In most cases, this handled by <see cref="SoundSystem.Update"/>.
        /// <para>
        /// Do NOT call this manually, unless you're streaming the buffer manually or know what you're doing.
        /// </para> 
        /// </summary>
        public void Update()
        {
            if (_isStreaming)
            {
                // The stream has been interrupted!
                if (base.Status == SoundStatus.Stopped)
                {
                    if (!_stopping)
                        ALChecker.Check(() => AL.SourcePlay(Handle));
                    else
                        _isStreaming = false;
                }

                // Get the number of buffers that have been processed (i.e. ready for reuse)
                int nbProcessed = 0;
                ALChecker.Check(() => AL.GetSource(Handle, ALGetSourcei.BuffersProcessed, out nbProcessed));

                while (nbProcessed-- > 0)
                {
                    // Pop the first unused buffer from the queue
                    int buffer = ALChecker.Check(() => AL.SourceUnqueueBuffer(Handle));

                    // Find its number
                    int bufferNum = 0;
                    for (int i = 0; i < BUFFER_COUNT; ++i)
                    {
                        if (_buffers[i] == buffer)
                        {
                            bufferNum = i;
                            break;
                        }
                    }

                    // Retrieve its size and add it to the samples count
                    if (_bufferSeeks[bufferNum] != -1)
                    {
                        // This was the last buffer before EOF or Loop End: reset the sample count
                        _processed = _bufferSeeks[bufferNum];
                        _bufferSeeks[bufferNum] = -1;
                    }
                    else
                    {
                        int size = 0, bits = 0;
                        ALChecker.Check(() => AL.GetBuffer(buffer, ALGetBufferi.Size, out size));
                        ALChecker.Check(() => AL.GetBuffer(buffer, ALGetBufferi.Bits, out bits));

                        // Bits can be 0 if the format or parameters are corrupt, avoid division by zero
                        if (bits == 0)
                        {
                            Logger.Error("Bits in sound stream are 0: make sure that the audio format is not corrupt " +
                                   "and Initialize() has been called correctly");

                            // Abort streaming (exit main loop)
                            _isStreaming = false;
                            _stopping = true;

                            break;
                        }
                        else
                        {
                            _processed += size / (bits / 8);
                        }
                    }

                    // Fill it and push it back into the playing queue
                    if (!_stopping)
                    {
                        if (FillAndPushBuffer(bufferNum))
                        {
                            _stopping = true;
                            Update();
                        }
                    }
                }
            }
            else if (_stopping)
            {
                // Turn off the flag
                _stopping = false;

                // Stop the playback
                ALChecker.Check(() => AL.SourceStop(Handle));

                // Dequeue any buffer left in the queue
                ClearQueue();
            }
        }

        private void Preload()
        {
            // Reset stop flag
            _stopping = false;

            // Create the buffers
            _buffers = ALChecker.Check(() => AL.GenBuffers(BUFFER_COUNT));
            for (int i = 0; i < BUFFER_COUNT; ++i)
                _bufferSeeks[i] = -1;

            // Fill the queue
            _stopping = FillQueue();
        }

        private bool FillAndPushBuffer(int bufferNum, bool immediateLoop = false)
        {
            bool requestStop = false;

            // Acquire audio data
            short[] samples = null;
            for (int retryCount = 0; !OnGetData(out samples) && (retryCount < BUFFER_RETRIES); ++retryCount)
            {
                // Check if the stream must loop or stop
                if (!_loop)
                {
                    // Not looping: Mark this buffer as ending with 0 and request stop
                    if (samples != null && samples.Length > 0)
                        _bufferSeeks[bufferNum] = 0;

                    _stopping = true;
                    break;
                }

                // Return to the beginning or loop-start of the stream source using onLoop(), and store the result in the buffer seek array
                // This marks the buffer as the "last" one (so that we know where to reset the playing position)
                _bufferSeeks[bufferNum] = OnLoop();

                // If we got data, break and process it, else try to fill the buffer once again
                if (samples != null && samples.Length > 0)
                    break;

                // If immediateLoop is specified, we have to immediately adjust the sample count
                if (immediateLoop && (_bufferSeeks[bufferNum] != -1))
                {
                    // We just tried to begin preloading at EOF or Loop End: reset the sample count
                    _processed = _bufferSeeks[bufferNum];
                    _bufferSeeks[bufferNum] = -1;
                }

                // We're a looping sound that got no data, so we retry onGetData()
            }

            // Fill the buffer if some data was returned
            if (samples != null && samples.Length > 0)
            {
                int buffer = _buffers[bufferNum];

                // Fill the buffer
                int size = samples.Length * sizeof(short);
                ALChecker.Check(() => AL.BufferData(buffer, _format, samples, size, _sampleRate));

                // Push it into the sound queue
                ALChecker.Check(() => AL.SourceQueueBuffer(Handle, buffer));
            }
            else
            {
                requestStop = true;
            }

            return requestStop;
        }

        private bool FillQueue()
        {
            // Fill and enqueue all the available buffers
            bool requestStop = false;
            for (int i = 0; (i < BUFFER_COUNT) && !requestStop; ++i)
            {
                if (FillAndPushBuffer(i, i == 0))
                    requestStop = true;
            }

            return requestStop;
        }

        private void ClearQueue()
        {
            // Get the number of buffers still in the queue
            int nbQueued = 0;
            ALChecker.Check(() => AL.GetSource(Handle, ALGetSourcei.BuffersQueued, out nbQueued));

            // Dequeue them all
            for (int i = 0; i < nbQueued; ++i)
                ALChecker.Check(() => AL.SourceUnqueueBuffer(Handle));

            // Delete the buffers
            ResetBuffer();
        }

        /// <summary>
        /// Request a new chunk of audio samples from the stream source.
        /// </summary>
        /// <param name="samples">The audio chunk that contains audio samples.</param>
        /// <returns><code>true</code> if reach the end of stream, otherwise false.</returns>
        protected abstract bool OnGetData(out short[] samples);

        /// <summary>
        /// Change the current playing position in the stream source.
        /// </summary>
        /// <param name="time">Seek to specified time.</param>
        protected abstract void OnSeek(TimeSpan time);

        /// <summary>
        /// Change the current playing position in the stream source to the beginning of the loop.
        /// <para>
        /// This function can be overridden by derived classes to allow implementation of custom loop points. Otherwise,
        /// it just calls onSeek(Time::Zero) and returns 0.</para>
        /// </summary>
        /// <returns>The seek position after looping (or -1 if there's no loop).</returns>
        protected virtual long OnLoop()
        {
            OnSeek(TimeSpan.Zero);
            return 0;
        }

        /// <summary>
        /// Performs initialization steps by providing the audio stream parameters.
        /// </summary>
        /// <param name="channelCount">The number of channels of current <see cref="SoundStream"/> object.</param>
        /// <param name="sampleRate">The sample rate, in samples per second.</param>
        protected virtual void Initialize(int channelCount, int sampleRate)
        {
            // Reset the current states
            _channelCount = channelCount;
            _sampleRate   = sampleRate;
            _processed    = 0;
            _isStreaming  = false;

            // Deduce the format from the number of channels
            _format = AudioDevice.GetFormat(channelCount);

            // Check if the format is valid
            if (_format == 0)
            {
                throw new NotSupportedException("The specified number of channels (" + _channelCount.ToString() + ") is not supported.");
            }
        }

        /// <summary>
        /// Start or resume playing current <see cref="SoundStream"/> object.
        /// </summary>
        protected internal override void Play()
        {
            // Check if the sound parameters have been set
            if (_format == 0)
            {
                throw new InvalidOperationException(
                    "Audio parameters must be initialized before played.");
            }

            if (_isStreaming && (base.Status == SoundStatus.Paused))
            {
                // If the sound is paused, resume it
                ALChecker.Check(() => AL.SourcePlay(Handle));
                return;
            }
            else if (_isStreaming && (base.Status == SoundStatus.Playing))
            {
                // If the sound is playing, stop it and continue as if it was stopped
                Stop();
            }

            // Move to the beginning
            OnSeek(TimeSpan.Zero);

            // Start updating the stream in a separate thread to avoid blocking the application
            _processed = 0;
            _isStreaming = true;

            // Preload the stream buffer
            Preload();

            // Play the sound
            ALChecker.Check(() => AL.SourcePlay(Handle));
        }

        /// <summary>
        /// Pause the current <see cref="SoundStream"/> object.
        /// </summary>
        protected internal override void Pause()
        {
            if (!_isStreaming)
                return;

            ALChecker.Check(() => AL.SourcePause(Handle));
        }

        /// <summary>
        /// Stop playing the current <see cref="SoundStream"/> object.
        /// </summary>
        protected internal override void Stop()
        {
            // Set the flag to stop
            _isStreaming = false;

            // Stop the playback
            ALChecker.Check(() => AL.SourceStop(Handle));

            // Force to update the stream to finalize the buffer and queue
            ClearQueue();
        }

        protected internal override void ResetBuffer()
        {
            _isStreaming = false;
            ALChecker.Check(() => AL.Source(Handle, ALSourcei.Buffer, 0));
            for (int i = 0; i < BUFFER_COUNT; i++)
            {
                if (_buffers[i] != 0)
                {
                    ALChecker.Check(() => AL.DeleteBuffer(_buffers[i]));
                    _buffers[i] = 0;
                }
            }
        }

        /// <summary>
        /// Releases all resources used by <see cref="SoundStream"/>.
        /// </summary>
        public override void Dispose()
        {
            ResetBuffer();
            base.Dispose();
        }
    }
}
