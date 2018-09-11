using System;
using System.IO;

using OpenTK;
using OpenTK.Audio.OpenAL;

using Genode;
using Genode.Internal.OpenAL;

namespace Genode.Audio
{
    /// <summary>
    /// Represents an array of sound samples.
    /// </summary>
    /// <inheritdoc/>
    public class Sound : DisposableResource
    {
        /// <summary>
        /// Gets the audio format of the sound.
        /// </summary>
        internal ALFormat Format => AudioDevice.Instance.GetFormat(ChannelCount);
        
        /// <summary>
        /// Gets the OpenAL buffer identifier.
        /// </summary>
        public int Handle { get; internal set; }

        /// <summary>
        /// Gets the sound duration.
        /// </summary>
        public TimeSpan Duration { get; private set; } = TimeSpan.Zero;

        /// <summary>
        /// Gets or sets the buffer mode of <see cref="Sound"/>.
        /// </summary>
        public BufferMode Mode { get; }

        /// <summary>
        /// Gets the number of sample.
        /// </summary>
        public long SampleCount { get; protected set; }

        /// <summary>
        /// Gets the number of audio channels.
        /// </summary>
        public int ChannelCount { get; protected set; }

        /// <summary>
        /// Gets the number of sample rate.
        /// </summary>
        public int SampleRate { get; protected set; }

        /// <summary>
        /// Gets the underlying stream of <see cref="Sound"/>.
        /// </summary>
        public SoundDecoder Decoder { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Sound"/> class.
        /// </summary>
        /// <inheritdoc/>
        private Sound()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Sound"/> class from a file.
        /// </summary>
        /// <param name="filename">Path of the audio file.</param>
        /// <param name="mode">Specifies a value indicating whether the whole buffer should be filled immediately or streamed later.</param>
        /// <inheritdoc/>
        internal Sound(string filename, BufferMode mode)
            : this(File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read), mode)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Sound"/> class from an array of byte containing audio data.
        /// </summary>
        /// <param name="data">An array of bytes containing audio data.</param>
        /// <param name="mode">Specifies a value indicating whether the whole buffer should be filled immediately or streamed later.</param>
        /// <inheritdoc/>
        internal Sound(byte[] data, BufferMode mode)
            : this(new MemoryStream(data), mode)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Sound"/> class from an audio stream.
        /// </summary>
        /// <param name="stream"><see cref="Stream"/> containing audio data.</param>
        /// <param name="mode">Specifies a value indicating whether the whole buffer should be filled immediately or streamed later.</param>
        /// <inheritdoc/>
        internal Sound(Stream stream, BufferMode mode)
            : this()
        {
            Mode = mode;
            Initialize(new SoundDecoder(stream));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Sound"/> class from an array of audio samples.
        /// This will create sample mode instead of stream mode.
        /// </summary>
        /// <param name="samples">An array of audio samples.</param>
        /// <param name="channelCount">The number of audio channels.</param>
        /// <param name="sampleRate">The number of sample rate.</param>
        /// <inheritdoc/>
        internal Sound(short[] samples, int channelCount, int sampleRate)
            : this()
        {
            if (samples != null && samples.Length > 0)
            {
                LoadBuffer(samples, channelCount, sampleRate);
            }
        }
        
        /// <summary>
        /// Save the audio samples into an array of byte.
        /// </summary>
        /// <typeparam name="T">The desired sound writer to use.</typeparam>
        /// <returns>An array of bytes which an audio samples in binary form.</returns>
        public byte[] Save<T>()
            where T : SoundWriter, new()
        {
            lock (this)
            {
                long offset = Decoder.SampleOffset;
                Decoder.Seek(0);
                
                using (var stream = new MemoryStream())
                using (var writer = SoundProcessorFactory.GetWriter<T>(stream, SampleRate, ChannelCount))
                {
                    int bufferSize = SampleRate * ChannelCount;
                    var samples = new short[bufferSize];
                    while (Decoder.Decode(samples, bufferSize) > 0)
                    {
                        writer.Write(samples, 0, samples.Length);
                    }
                    
                    Decoder.Seek(offset);
                    return stream.ToArray();
                }
            }
        }

        /// <summary>
        /// Save the audio samples into sound file.
        /// </summary>
        /// <param name="filename">The path of sound file to be save.</param>
        public void Save(string filename)
        {
            lock (this)
            {
                long offset = Decoder.SampleOffset;
                Decoder.Seek(0);

                using (var writer = SoundProcessorFactory.GetWriter(filename, SampleRate, ChannelCount))
                {
                    int bufferSize = SampleRate * ChannelCount;
                    var samples = new short[bufferSize];

                    long read = 0;
                    while ((read = Decoder.Decode(samples, bufferSize)) > 0)
                    {
                        writer.Write(samples, 0, (int)read);
                    }
                }

                Decoder.Seek(offset);
            }
        }
        
        /// <summary>
        /// Initialize the sound buffer with a <see cref="SoundDecoder"/>.
        /// </summary>
        /// <param name="decoder"><see cref="SoundDecoder"/> containing audio information and sample to fill the current buffer.</param>
        private void Initialize(SoundDecoder decoder)
        {
            // Retrieve the decoder
            Decoder = decoder;

            // Retrieve sound parameters
            SampleCount  = decoder.SampleCount;
            ChannelCount = decoder.ChannelCount;
            SampleRate   = decoder.SampleRate;

            // Compute duration
            Duration = TimeSpan.FromSeconds((float)SampleCount / SampleRate / ChannelCount);

            // Fill entire buffer immediately if its a sample mode
            if (Mode == BufferMode.Sample)
            {
                // Create the buffer handle
                Handle = ALChecker.Check(() => AL.GenBuffer());

                // Decode the samples
                var samples = new short[SampleCount];
                if (decoder.Decode(samples, SampleCount) == SampleCount)
                {
                    // Update the internal buffer with the new samples
                    LoadBuffer(samples, ChannelCount, SampleRate);
                }
                else
                {
                    throw new Exception("Failed to initialize Sample Buffer");
                }
            }
        }

        /// <summary>
        /// Update <see cref="Sound"/> with filled audio samples and given channel count and sample rate.
        /// </summary>
        /// <param name="samples">The audio sample to fill the buffer.</param>
        /// <param name="channelCount">The number of audio channels.</param>
        /// <param name="sampleRate">The number of sample rate.</param>
        private void LoadBuffer(short[] samples, int channelCount, int sampleRate)
        {
            // Check audio properties
            if (channelCount == 0 || sampleRate == 0 || samples == null || samples.Length == 0)
            {
                throw new ArgumentNullException();
            }

            // Fill the buffer
            int size = samples.Length * sizeof(short);
            ALChecker.Check(() => AL.BufferData(Handle, Format, samples, size, sampleRate));

            Decoder?.Dispose();
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="Sound"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            Decoder?.Dispose();
            ALChecker.Check(() => AL.DeleteBuffer(Handle));
        }
    }
}
