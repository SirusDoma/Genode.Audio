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

        private bool _isStreaming = false, _stopping = false;
        private int[] _buffers = new int[BUFFER_COUNT];
        private int _channelCount = 0;
        private int _sampleRate = 0;
        private ALFormat _format = 0;
        private bool _loop = false;
        private long _processed = 0;
        private bool[] _endBuffers = new bool[BUFFER_COUNT];

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
                // Get old playing status
                SoundStatus oldStatus = Status;

                // Stop the stream
                if (Status == SoundStatus.Playing)
                    Stop();

                // Let the derived class update the current position
                OnSeek(value);

                // Restart streaming
                _processed = (long)(value.TotalSeconds * _sampleRate * _channelCount);
                _isStreaming = oldStatus != SoundStatus.Stopped;
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
                    int buffer = 0;
                    ALChecker.Check(() => buffer = AL.SourceUnqueueBuffer(Handle));

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
                    if (_endBuffers[bufferNum])
                    {
                        // This was the last buffer: reset the sample count
                        _processed = 0;
                        _endBuffers[bufferNum] = false;
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
            else if (_stopping && _processed != 0)
            {
                // Stop the playback
                ALChecker.Check(() => AL.SourceStop(Handle));

                // Dequeue any buffer left in the queue
                ClearQueue();

                _processed = 0;
                _stopping  = false;
            }
        }

        private void Preload()
        {
            // Reset stop flag
            _stopping = false;

            // Create the buffers
            //ALChecker.Check(() => AL.Source(Handle, ALSourcei.Buffer, 0));
            for (int i = 0; i < BUFFER_COUNT; ++i)
            {
                _buffers[i] = ALChecker.Check(() => AL.GenBuffer());
                _endBuffers[i] = false;
            }

            // Fill the queue
            _stopping = FillQueue();
        }

        private bool FillAndPushBuffer(int bufferNum)
        {
            bool requestStop = false;

            // Acquire audio data
            short[] samples = null;
            if (!OnGetData(out samples))
            {
                // Mark the buffer as the last one (so that we know when to reset the playing position)
                _endBuffers[bufferNum] = true;

                // Check if the stream must loop or stop
                if (_loop)
                {
                    // Return to the beginning of the stream source
                    OnSeek(TimeSpan.Zero);

                    // If we previously had no data, try to fill the buffer once again
                    if (samples == null || (samples.Length == 0))
                    {
                        return FillAndPushBuffer(bufferNum);
                    }
                }
                else
                {
                    // Not looping: request stop
                    requestStop = true;
                }
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

            return requestStop;
        }

        private bool FillQueue()
        {
            // Fill and enqueue all the available buffers
            bool requestStop = false;
            for (int i = 0; (i < BUFFER_COUNT) && !requestStop; ++i)
            {
                if (FillAndPushBuffer(i))
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
            int buffer = 0;
            for (int i = 0; i < nbQueued; ++i)
                ALChecker.Check(() => buffer = AL.SourceUnqueueBuffer(Handle));

            // Delete the buffers
            ALChecker.Check(() => AL.Source(Handle, ALSourcei.Buffer, 0));
            ALChecker.Check(() => AL.DeleteBuffers(_buffers));
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
        /// Performs initialization steps by providing the audio stream parameters.
        /// </summary>
        /// <param name="channelCount">The number of channels of current <see cref="SoundStream"/> object.</param>
        /// <param name="sampleRate">The sample rate, in samples per second.</param>
        protected virtual void Initialize(int channelCount, int sampleRate)
        {
            // Reset the current states
            _channelCount = channelCount;
            _sampleRate = sampleRate;
            _processed = 0;
            _isStreaming = false;

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
            _isStreaming = false;

            // Stop the playback
            ALChecker.Check(() => AL.SourceStop(Handle));

            // Force to update the stream to finalize the buffer and queue
            ClearQueue();

            // Move to the beginning
            OnSeek(TimeSpan.Zero);

            // Reset the playing position
            _processed = 0;
        }

        protected internal override void ResetBuffer()
        {
            _isStreaming = false;
            ALChecker.Check(() => AL.Source(Handle, ALSourcei.Buffer, 0));
            ALChecker.Check(() => AL.DeleteBuffers(_buffers));
        }

        /// <summary>
        /// Releases all resources used by <see cref="SoundStream"/>.
        /// </summary>
        public override void Dispose()
        {
            _isStreaming = false;

            base.Dispose();
            ALChecker.Check(() => AL.DeleteBuffers(_buffers));
        }
    }
}
