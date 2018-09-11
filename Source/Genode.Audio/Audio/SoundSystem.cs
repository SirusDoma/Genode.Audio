using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using OpenTK;
using OpenTK.Audio.OpenAL;

using Genode;
using Genode.Internal.OpenAL;

namespace Genode.Audio
{
    /// <summary>
    /// Represents an audio system that manages <see cref="SoundChannel"/> playback and lifecycle.
    /// </summary>
    public class SoundSystem : DisposableResource
    {
        /// <summary>
        /// The number of maximum sources that can played at once in the audio environment.
        /// </summary>
        public const int MaxSource = 256;

        /// <summary>
        /// Gets the singleton instance of <see cref="SoundSystem"/>.
        /// </summary>
        public static SoundSystem Instance { get; } = new SoundSystem();

        private readonly AudioDevice device;
        private SoundRecorder recorder;
        private HashSet<SoundChannel> channels;

        /// <summary>
        /// Gets the number of generated sources in the source pool.
        /// </summary>
        internal int PoolCount => channels?.Count ?? 0;

        /// <summary>
        /// Initializes a new instance of <see cref="SoundSystem"/>.
        /// </summary>
        public SoundSystem()
        {
            device   = AudioDevice.Instance;
            channels = new HashSet<SoundChannel>();
        }

        /// <summary>
        /// Ensures that resources are freed and other cleanup operations are performed when the garbage collector reclaims the <see cref="SoundSystem"/>.
        /// </summary>
        ~SoundSystem()
        {
            Dispose(false);
        }

        /// <summary>
        /// Generate or retrieve available OpenAL Source Handle from the source pool.
        /// </summary>
        /// <returns>OpenAL Source Handle.</returns>
        private int GenSource()
        {
            // Reuse stopped channel
            var channel = channels.FirstOrDefault(ch => ch.Status == SoundStatus.Stopped);
            if (channel != null)
            {
                return Enqueue(channel)?.Handle ?? throw new Exception("Failed to retrieve available channel");
            }
            else if (channels.Count < MaxSource)
            {
                // No stopped channel available, generate one as long the pool still below the limit
                return ALChecker.Check(() => AL.GenSource());
            }
            
            // No stopped channel available and the source pool is already at its limit
            throw new OutOfMemoryException("Insufficient audio source handles.");
        }

        /// <summary>
        /// Queue the specified <see cref="SoundChannel"/> into source pool.
        /// </summary>
        /// <param name="channel"><see cref="SoundChannel"/> to queue into source pool.</param>
        /// <returns>Specified <see cref="SoundChannel"/> that desired to queue into source pool.</returns>
        private SoundChannel Queue(SoundChannel channel)
        {
            // Remove current channel that has same source from the pool if exists
            channels.Remove(channel);
            
            // Add the new channel object into the pool
            channels.Add(channel);
            
            // Return the queued channel
            return channel;
        }

        /// <summary>
        /// Enqueue the specified <see cref="SoundChannel"/> from source pool.
        /// </summary>
        /// <param name="channel"><see cref="SoundChannel"/> to enqueue from source pool.</param>
        /// <returns>Specified <see cref="SoundChannel"/> that desired to enqueue from source pool.</returns>
        private SoundChannel Enqueue(SoundChannel channel)
        {
            if (channels.Contains(channel))
            {
                channel.Reset();
                return channel;
            }

            return null;
        }

        /// <summary>
        /// Create a Sound that contains audio samples.
        /// </summary>
        /// <param name="filename">The path of audio file.</param>
        /// <param name="mode">Specifies a value indicating whether the whole buffer should be filled immediately or streamed later.</param>
        /// <returns>A sound buffer of given <see cref="Stream"/>.</returns>
        public Sound LoadSound(string filename, BufferMode mode = BufferMode.Sample)
        {
            return LoadSound(File.Open(filename, FileMode.Open, FileAccess.Read), mode);
        }

        /// <summary>
        /// Create a Sound that contains audio samples.
        /// </summary>
        /// <param name="buffer">An array of bytes that contains audio samples.</param>
        /// <param name="mode">Specifies a value indicating whether the whole buffer should be filled immediately or streamed later.</param>
        /// <returns>A sound buffer of given <see cref="Stream"/>.</returns>
        public Sound LoadSound(byte[] buffer, BufferMode mode = BufferMode.Sample)
        {
            return LoadSound(new MemoryStream(buffer), mode);
        }

        /// <summary>
        /// Create a Sound that contains audio samples.
        /// </summary>
        /// <param name="stream"><see cref="Stream"/> that contains audio samples.</param>
        /// <param name="mode">Specifies a value indicating whether the whole buffer should be filled immediately or streamed later.</param>
        /// <returns>A sound buffer of given <see cref="Stream"/>.</returns>
        public Sound LoadSound(Stream stream, BufferMode mode = BufferMode.Sample)
        {
            return new Sound(stream, mode);
        }

        /// <summary>
        /// Start playing the <see cref="SoundChannel"/>.
        /// </summary>
        /// <param name="sound"><see cref="Sound"/> to play.</param>
        /// <returns>A playing <see cref="SoundChannel"/>.</returns>
        public SoundChannel Play(Sound sound)
        {
            // Construct sound channel based on buffer mode
            SoundChannel channel = null;
            switch (sound.Mode)
            {
                case BufferMode.Sample: channel = new SoundSample(GenSource(), sound); break;
                case BufferMode.Stream: channel = new Music(GenSource(), sound);       break;
            }

            // Check whether the Sound Channel eventually instantiated
            if (channel == null)
            {
                throw new NotSupportedException("Specified Sound Buffer Mode is not supported.");
            }

            // Assign sample information
            channel.SampleCount  = sound.SampleCount;
            channel.ChannelCount = sound.ChannelCount;
            channel.SampleRate   = sound.SampleRate;

            // Play the channel
            channel.Play();

            // Queue channel into source pool and return it
            return Queue(channel);
        }

        /// <summary>
        /// Start or resume playing the <see cref="SoundChannel"/>.
        /// </summary>
        /// <param name="channel"><see cref="SoundChannel"/> to play.</param>
        /// <returns>A playing <see cref="SoundChannel"/>.</returns>
        public SoundChannel Play(SoundChannel channel)
        {
            if (channel.Handle <= 0)
            {
                channel.Handle = GenSource();
            }
            else if (channel.Status != SoundStatus.Paused)
            {
                channel.Stop();
                channel.Handle = GenSource();
                channel.Reload();
            }

            channel.Play();
            return Queue(channel);
        }
        
        /// <summary>
        /// Start playing the <see cref="SoundChannel"/>.
        /// </summary>
        /// <param name="sound"><see cref="Sound"/> to play.</param>
        /// <returns>A playing <see cref="SoundChannel"/>.</returns>
        public T Play<T>(Sound sound)
            where T : SoundStream, new()
        {
            // Construct sound channel based on specified implementation
            SoundChannel channel = new T();
            channel.Handle = GenSource();
            channel.Buffer = sound;

            // Assign sample information
            channel.SampleCount  = sound.SampleCount;
            channel.ChannelCount = sound.ChannelCount;
            channel.SampleRate   = sound.SampleRate;

            // Play the channel
            channel.Play();

            // Queue channel into source pool and return it
            return Queue(channel) as T;
        }

        /// <summary>
        /// Pause playing the <see cref="SoundChannel"/>.
        /// </summary>
        /// <param name="channel"><see cref="SoundChannel"/> to pause.</param>
        public void Pause(SoundChannel channel)
        {
            channel.Pause();
        }

        /// <summary>
        /// Stop playing the <see cref="SoundChannel"/>.
        /// </summary>
        /// <param name="channel"><see cref="SoundChannel"/> to stop.</param>
        public void Stop(SoundChannel channel)
        {
            channel.Stop();
        }
        
        /// <summary>
        /// Stop recording audio.
        /// </summary>
        public void Stop(SoundRecorder recorder)
        {
            if (recorder?.Capturing ?? false)
            {
                recorder.Stop();
            }
        }

        /// <summary>
        /// Start capturing audio with default audio input device.
        /// </summary>
        /// <param name="sampleRate">The samples rate of sound to use for recording, in samples per second.</param>
        /// <returns>An instance of <see cref="SoundRecorder"/> to manipulate recording properties and operations.</returns>
        public SoundRecorder<Sound> Capture(int sampleRate = 44100)
        {
            return Capture<Sound>(sampleRate);
        }

        /// <summary>
        /// Start capturing audio with default audio input device.
        /// </summary>
        /// <param name="sampleRate">The samples rate of sound to use for recording, in samples per second.</param>
        /// <typeparam name="T">The type of class that will hold recorded audio samples.</typeparam>
        /// <returns>An instance of <see cref="SoundRecorder{T}"/> to manipulate recording properties and operations.</returns>
        public SoundRecorder<T> Capture<T>(int sampleRate = 44100)
            where T : class
        {
            if (recorder == null)
            {
                recorder = SoundProcessorFactory.GetRecorder<T>();
                recorder.Start(sampleRate);

                return recorder as SoundRecorder<T>;
            }
            
            throw new Exception("Cannot start audio capture while another capture is running.");
        }

        /// <summary>
        /// Update the current instance of <see cref="SoundSystem"/>.
        /// </summary>
        /// <param name="delta">The delta of current game frame.</param>
        public void Update(double delta)
        {
            if (recorder != null)
            {
                recorder?.Update();
                if (!recorder.Capturing)
                {
                    recorder = null;
                }
            }
            
            channels.RemoveWhere(ch => ch.IsInvalid);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="SoundSystem"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                channels?.Apply(source => source?.Dispose());
                device.Dispose();

                base.Dispose(disposing);
            }
        }
    }
}
