using System;
using System.IO;

namespace Genode.Audio
{
    /// <summary>
    /// Provides functionality to decode audio samples from sound file or data.
    /// </summary>
    /// <inheritdoc />
    public sealed class SoundDecoder : DisposableResource
    {
        private readonly SoundReader reader;

        /// <summary>
        /// Gets the total number of audio samples of the sound.
        /// </summary>
        public long SampleCount { get; }

        /// <summary>
        /// Gets the number of channels used by the sound.
        /// 1 = mono, 2 = stereo.
        /// </summary>
        public int ChannelCount { get; }

        /// <summary>
        /// Gets Sample Rate per second of the sound.
        /// </summary>
        public int SampleRate { get; }

        /// <summary>
        /// Gets the total duration of the sound.
        /// <para>
        /// This function is provided for convenience, the duration is deduced from the other sound file attributes.
        /// </para>
        /// </summary>
        public TimeSpan Duration => TimeSpan.FromSeconds(ChannelCount == 0 || SampleRate == 0 ? 0f : (float)SampleCount / ChannelCount / SampleRate);

        /// <summary>
        /// Get the read offset of the file in samples
        /// </summary>
        public TimeSpan TimeOffset => TimeSpan.FromSeconds(ChannelCount == 0 || SampleRate == 0 ? 0f : (float)SampleOffset / ChannelCount / SampleRate);

        /// <summary>
        /// Get the read offset of the file in time
        /// </summary>
        public long SampleOffset { get; private set; }

        /// <summary>
        /// Gets the underlying <see cref="System.IO.Stream"/> of the <see cref="SoundDecoder"/>.
        /// </summary>
        public Stream Stream => reader?.BaseStream;

        /// <summary>
        /// Gets or sets a value indicating whether <see cref="Stream"/> should leave open after the current instance of the <see cref="SoundReader"/> disposed.
        /// </summary>
        public bool LeaveOpen => reader?.LeaveOpen ?? false;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Genode.Audio.SoundDecoder" /> class.
        /// </summary>
        /// <param name="filename">Path of audio file.</param>
        /// <inheritdoc />
        public SoundDecoder(string filename)
            : this(File.Open(filename, FileMode.Open, FileAccess.Read), false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SoundDecoder"/> class.
        /// </summary>
        /// <param name="data">An array of byte containing audio data.</param>
        /// <inheritdoc />
        public SoundDecoder(byte[] data)
            : this(new MemoryStream(data), false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SoundDecoder"/> class.
        /// </summary>
        /// <param name="stream"><see cref="Stream"/> containing audio data.</param>
        /// <param name="leaveOpen">Specifies whether the given <see cref="Stream"/> should left open after <see cref="SoundDecoder"/> disposed.</param>
        /// <inheritdoc />
        public SoundDecoder(Stream stream, bool leaveOpen = false)
        {
            // Find a suitable reader for the given audio stream
            reader = SoundProcessorFactory.GetReader(stream, leaveOpen);
            if (reader == null)
            {
                throw new NotSupportedException("Failed to open audio stream. Invalid audio format or not supported.");
            }

            // Retrieve the attributes of the sound
            SampleCount  = reader.SampleInfo.SampleCount;
            ChannelCount = reader.SampleInfo.ChannelCount;
            SampleRate   = reader.SampleInfo.SampleRate;
        }

        /// <summary>
        /// Sets the current read position within current decoder.
        /// </summary>
        /// <param name="sampleOffset">The index of the sample to jump to, relative to the beginning.</param>
        public void Seek(long sampleOffset)
        {
            reader?.Seek(SampleOffset = Math.Min(sampleOffset, SampleCount));
        }

        /// <summary>
        /// Sets the current read position within current decoder.
        /// </summary>
        /// <param name="timeOffset">The time of the sample to jump to, relative to the beginning.</param>
        public void Seek(TimeSpan timeOffset)
        {
            Seek((long)timeOffset.TotalSeconds * SampleRate * ChannelCount);
        }

        /// <summary>
        /// Reads a block of audio samples from the sound and writes the data to given array of samples.
        /// </summary>
        /// <param name="samples">Sample array to fill.</param>
        /// <param name="count">Maximum number of samples to read.</param>
        /// <returns>The number of samples actually read.</returns>
        public long Decode(short[] samples, long count)
        {
            long read = reader?.Read(samples, count) ?? 0L;
            SampleOffset += read;

            return read;
        }

        /// <summary>
        /// Closes the current reader and releases any resources associated. 
        /// Instead of calling this method, ensure that the stream is properly disposed.
        /// </summary>
        public void Close()
        {
            reader?.Close();
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="SoundDecoder"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    reader?.Dispose();
                }

                base.Dispose(disposing);
            }
        }
    }
}
