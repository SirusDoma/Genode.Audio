using System;
using System.Threading;
using System.Threading.Tasks;

using OpenTK;
using OpenTK.Audio.OpenAL;

using Genode;
using Genode.Internal.OpenAL;

namespace Genode.Audio
{
    /// <summary>
    /// Represents a base class streamed audio.
    /// </summary>
    /// <inheritdoc/>
    public abstract class SoundStream : SoundChannel
    {
        private readonly object mutex = new object();
        
        /// <summary>
        /// The number of audio buffers used by the streaming process.
        /// </summary>
        private const int BufferCount   = 3;
        
        /// <summary>
        /// The number of retries (excluding initial attempt) for <see cref="GetStreamData"/>.
        /// </summary>
        private const int BufferRetries = 2;

        private Task task;
        private int[] buffers = new int[BufferCount];
        private int[] bufferLoop = new int[BufferCount];
        private bool streaming;
        private SoundStatus state;
        private int processed;
        private ALFormat format;
        
        /// <summary>
        /// Gets or sets the value indicating whether the <see cref="SoundStream"/> is in loop mode
        /// </summary>
        /// <inheritdoc/>
        public override bool IsLooping { get; set; }

        /// <summary>
        /// Gets the current status of the sound.
        /// </summary>
        /// <inheritdoc/>
        public override SoundStatus Status
        {
            get
            {
                var status = base.Status;
                if (status == SoundStatus.Stopped)
                {
                    lock (mutex)
                    {
                        status = streaming ? state : status;
                    }
                }

                return status;
            }
        }

        /// <summary>
        /// Gets or sets the current position of the <see cref="SoundStream"/>.
        /// </summary>
        /// <inheritdoc/>
        public override TimeSpan PlayingOffset
        {
            get
            {
                float seconds = ALChecker.Check(() => { AL.GetSource(Handle, ALSourcef.SecOffset, out float secs); return secs; });
                return TimeSpan.FromSeconds(seconds + (float)processed / SampleRate / ChannelCount);
            }
            set
            {
                // Backup current sound status
                var status = Status;

                // Stop the stream and seek to desired position
                Stop();
                Seek(value);
                
                // Recalculate the number of samples that has been processed to compensate seek point
                processed = (int) (value.TotalSeconds * SampleRate * ChannelCount);

                // The streaming state has been reset due to Stop() call
                // Resume streaming if previous status isn't stopped
                if (status != SoundStatus.Stopped)
                {
                    state = status;
                    streaming = true;

                    task = Task.Run(() => StreamDataAsync());
                }
            }
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="SoundStream"/> class.
        /// </summary>
        /// <param name="handle">A valid OpenAL Source Handle.</param>
        /// <inheritdoc/>
        protected SoundStream(int handle)
            : this(handle, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SoundStream"/> class.
        /// </summary>
        /// <param name="handle">A valid OpenAL Source Handle.</param>
        /// <param name="buffer">A sound that contains valid audio sample buffer.</param>
        /// <inheritdoc/>
        internal SoundStream(int handle, Sound buffer)
            : base(handle, buffer)
        {
            format    = buffer.Format;
            streaming = false;
            processed = 0;
            state     = SoundStatus.Stopped;
        }

        /// <summary>
        /// Change the current playing position in the stream source.
        /// </summary>
        /// <param name="time">New playing position, relative to the beginning of the stream.</param>
        protected abstract void Seek(TimeSpan time);

        /// <summary>
        /// Request a new chunk of audio samples from the stream source.
        /// </summary>
        /// <param name="samples">The audio samples to fill</param>
        /// <returns><c>true</c> if there's more samples to stream; otherwise, <c>false</c>.</returns>
        protected abstract bool GetStreamData(out short[] samples);

        /// <summary>
        /// Initialize the audio stream parameters.
        /// </summary>
        /// <param name="channelCount">The number of channels of the stream.</param>
        /// <param name="sampleRate">The rate of samples, in samples per second.</param>
        protected void Initialize(int channelCount, int sampleRate)
        {
            format    = AudioDevice.Instance.GetFormat(channelCount);
            streaming = false;
            processed = 0;
            
            SampleRate   = sampleRate;
            ChannelCount = channelCount;
        }

        /// <summary>
        /// Change the current playing position in the stream source to the beginning of the loop.
        /// <para>
        /// This function can be overridden by derived classes to allow implementation of custom loop points. Otherwise,
        /// it just calls Seek(TimeSpan.Zero) and returns 0.</para>
        /// </summary>
        /// <returns>The seek position after looping (or -1 if there's no loop).</returns>
        protected virtual int GetLoopPoint()
        {
            Seek(TimeSpan.Zero);
            return 0;
        }
        
        /// <summary>
        /// Start or resume playing of the <see cref="SoundStream"/>.
        /// </summary>
        /// <inheritdoc/>
        protected internal override void Play()
        {
            if (format == 0)
            {
                throw new Exception("Failed to play audio stream:\nSound parameters have not been initialized");    
            }
            
            bool streaming = false;
            var state = SoundStatus.Stopped;

            lock (mutex)
            {
                streaming = this.streaming;
                state = this.state;
            }
            
            // Check whether the stream is active
            if (streaming)
            {
                // If the sound is paused, resume it
                if (state == SoundStatus.Paused)
                {
                    lock(mutex) { this.state = SoundStatus.Playing; }
                    ALChecker.Check(() => AL.SourcePlay(Handle));
                    
                    return;
                }
                else if (state == SoundStatus.Playing)
                {
                    Stop();
                }
            }

            // Set playing status
            this.state = SoundStatus.Playing;
            this.streaming = true;

            // Start streaming the audio sample 
            task = Task.Run(() => StreamDataAsync());
        }

        /// <summary>
        /// Pause the playing <see cref="SoundStream"/>.
        /// </summary>
        /// <inheritdoc/>
        protected internal override void Pause()
        {
            lock (mutex)
            {
                if (!streaming)
                {
                    return;
                }

                state = SoundStatus.Paused;
            }
            
            ALChecker.Check(() => AL.SourcePause(Handle));
        }

        /// <summary>
        /// Stop playing the <see cref="SoundStream"/>.
        /// </summary>
        /// <inheritdoc/>
        protected internal override void Stop()
        {
            // Reset the flag
            lock(mutex) { streaming = false; }
            
            // Wait for the task to terminate
            // Use wait to avoid async keyword and violating naming convention (like StopAsync())
            task?.Wait();
            
            // Reset position
            Seek(TimeSpan.Zero);
        }

        /// <summary>
        /// Start the streaming process.
        /// </summary>
        private async Task StreamDataAsync()
        {
            // Make sure to stream only when the sound isn't stopped!
            lock (mutex)
            {
                if (state == SoundStatus.Stopped)
                {
                    streaming = false;
                    return;
                }
            }

            // Create the buffers and set buffer loop to none
            ALChecker.Check(() => buffers = AL.GenBuffers(BufferCount));
            for (int i = 0; i < BufferCount; ++i)
            {
                bufferLoop[i] = -1;
            }

            // Fill the initial queue
            bool terminating = QueueBuffers();

            // Play or pause the sound depending on current state respectively
            lock (mutex)
            {
                if (state == SoundStatus.Paused)
                {
                    ALChecker.Check(() => AL.SourcePause(Handle));
                }
                else
                {
                    ALChecker.Check(() => AL.SourcePlay(Handle));
                }
            }

            // Begin to stream the data
            while(true)
            {
                // Stop streaming if flag has been turned off
                lock (mutex)
                {
                    if (!streaming)
                    {
                        break;
                    }
                }
                
                // Stream has been interrupted
                if (base.Status == SoundStatus.Stopped)
                {
                    if (!terminating)
                    {
                        // No termination request and probably still more data to stream; continue.
                        ALChecker.Check(() => AL.SourcePlay(Handle));
                    }
                    else
                    {
                        // Termination requested, set the flag off
                        lock(mutex) { streaming = false; }
                    }
                }

                // Get the number of buffer that have been processed.
                int qProcessed = 0;
                ALChecker.Check(() => AL.GetSource(Handle, ALGetSourcei.BuffersProcessed, out qProcessed));
                
                // Loop throughout the processed buffers
                while (qProcessed-- > 0)
                {
                    // Pop the first buffer from the queue
                    int buffer = ALChecker.Check(() => AL.SourceUnqueueBuffer(Handle));

                    // Find the buffer index from buffer block
                    int index = 0;
                    for (int i = 0; i < BufferCount; i++)
                    {
                        if (buffers[i] == buffer)
                        {
                            index = i;
                            break;
                        }
                    }

                    // Retrieve the buffer size in case the loop for the buffer has been specified
                    if (bufferLoop[index] != -1)
                    {
                        // This was the last buffer before EOF or Loop End: reset the sample count
                        processed = bufferLoop[index];
                        bufferLoop[index] = -1;
                        
                        
                    }
                    else
                    {
                        // Get the size and bits format of the buffer
                        int size = 0, bits = 0;
                        ALChecker.Check(() => AL.GetBuffer(buffer, ALGetBufferi.Size, out size));
                        ALChecker.Check(() => AL.GetBuffer(buffer, ALGetBufferi.Bits, out bits));

                        // Bits can be 0 if the format or parameters are corrupt, avoid division by zero
                        if (bits == 0)
                        {
                            Logger.Instance.Error(
                                "Bits in sound stream are 0:\n" +
                                "make sure that the audio format is not corrupt and initialize() has been called correctly"
                            );

                            // Abort streaming
                            lock(mutex) { streaming = false; }
                            terminating = true;

                            break;
                        }
                        else
                        {
                            processed += size / (bits / 8);
                        }
                    }
                    
                    // Queue the selected buffer into playing queue
                    terminating = terminating || ProcessBuffer(index);
                }
                
                // Leave some time for the other threads if the stream is still playing
                if (base.Status != SoundStatus.Stopped)
                {
                    await Task.Delay(10);
                }
            }

            // Stop the source and clear the buffer
            ALChecker.Check(() => AL.SourceStop(Handle));
            
            // No need to wait next update cycle, clear all buffers immediately
            ClearQueue();
        }

        /// <summary>
        /// Process buffer of specified index.
        /// </summary>
        /// <param name="index">Index of buffer to process.</param>
        /// <param name="immediateLoop">Treat empty buffers as spent, and act on loops immediately.</param>
        /// <returns><c>true</c> if the stream source has requested to stop; otherwise, <c>false</c></returns>
        private bool ProcessBuffer(int index, bool immediateLoop = false)
        {
            short[] samples;
            bool terminating = false;

            for (int retry = 0; !GetStreamData(out samples) && retry < BufferRetries; ++retry)
            {
                // Check if the stream must loop or stop
                if (!IsLooping)
                {
                    if (samples != null && samples.Length != 0)
                    {
                        bufferLoop[index] = 0;
                    }

                    terminating = true;
                    break;
                }
                
                // Return to the beginning or loop-start of the stream source using GetLoopPoint(), and store the result in the buffer seek array
                // This marks the buffer as the "last" one (so that we know where to reset the playing position)
                bufferLoop[index] = GetLoopPoint();
                
                // If we got data, break and process it, else try to fill the buffer once again
                if (samples != null && samples.Length != 0)
                {
                    break;
                }
                
                // If immediateLoop is specified, we have to immediately adjust the sample count
                if (immediateLoop && (bufferLoop[index] != -1))
                {
                    // We just tried to begin preloading at EOF or Loop End: reset the sample count
                    processed = bufferLoop[index];
                    bufferLoop[index] = -1;
                }
            }

            // Fill the buffer if some data was returned
            if (samples != null && samples.Length != 0)
            {
                int buffer = buffers[index];

                // Fill the buffer
                int size = samples.Length * sizeof(short);
                ALChecker.Check(() => AL.BufferData(buffer, format, samples, size, SampleRate));

                // Push it into the sound queue
                ALChecker.Check(() => AL.SourceQueueBuffer(Handle, buffer));
            }
            else
            {
                // If we get here, we most likely ran out of retries
                terminating = true;
            }
            
            return terminating;
        }

        /// <summary>
        /// Fill the audio buffers and put them all into the playing queue.
        /// </summary>
        /// <returns><c>true</c> if the stream source has requested to stop; otherwise, <c>false</c></returns>
        private bool QueueBuffers()
        {
            // Fill and enqueue all the available buffers
            bool terminating = false;
            for (int i = 0; (i < BufferCount) && !terminating; ++i)
            {
                // Since no sound has been loaded yet, we can't schedule loop seeks preemptively,
                // So if we start on EOF or Loop End, we let fillAndPushBuffer() adjust the sample count
                terminating = ProcessBuffer(i, (i == 0));
            }

            return terminating;
        }

        /// <summary>
        /// Clear all the audio buffers and empty the playing queue.
        /// </summary>
        private void ClearQueue()
        {
            // Reset the playing position
            processed = 0;

            // Get the number of buffers that still in queue then enqueue all of them
            int queued = 0;
            ALChecker.Check(() => AL.GetSource(Handle, ALGetSourcei.BuffersQueued, out queued));
            ALChecker.Check(() => queued > 0 ? AL.SourceUnqueueBuffers(Handle, queued) : new int[0]);

            // Detach and delete the buffers
            ALChecker.Check(() => AL.Source(Handle, ALSourcei.Buffer, 0));
            ALChecker.Check(() => AL.DeleteBuffers(buffers));
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="SoundStream"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            // Stop the sound
            Stop();
            
            base.Dispose(disposing);
        }
    }
}
