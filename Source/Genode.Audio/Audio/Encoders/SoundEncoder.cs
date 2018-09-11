using System;
using System.IO;

namespace Genode.Audio
{
    /// <summary>
    /// Provides functionality to encode audio samples to sound file or stream.
    /// </summary>
    /// <inheritdoc />
    public sealed class SoundEncoder : DisposableResource
    {
        private readonly SoundWriter writer;

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
        /// Gets the underlying <see cref="System.IO.Stream"/> of the <see cref="SoundEncoder"/>.
        /// </summary>
        public Stream Stream => writer?.BaseStream;

        /// <summary>
        /// Initializes a new instance of the <see cref="SoundEncoder"/> class.
        /// </summary>
        /// <param name="stream">An audio stream to write.</param>
        /// <param name="sampleRate">The samples rate of the sound, in samples per second.</param>
        /// <param name="channelCount">The number of channels of the sound.</param>
        /// <param name="extension">The extension of target sound format.</param>
        /// <inheritdoc />
        public SoundEncoder(Stream stream, int sampleRate, int channelCount, string extension)
        {
            // Find a suitable writer for the given audio stream
            writer = SoundProcessorFactory.GetWriter(stream, sampleRate, channelCount, extension);
            if (writer == null)
            {
                throw new NotSupportedException("Failed to open audio stream. Invalid audio format or not supported.");
            }

            // Retrieve the attributes of the sound
            ChannelCount = writer.ChannelCount;
            SampleRate   = writer.SampleRate;
        }
   
        /// <summary>
        /// Initializes a new instance of the <see cref="SoundEncoder"/> class.
        /// </summary>
        /// <param name="filename">A relative or absolute path for the file that the current <see cref="SoundEncoder"/> object will encapsulate.</param>
        /// <param name="sampleRate">The samples rate of the sound, in samples per second.</param>
        /// <param name="channelCount">The number of channels of the sound.</param>
        /// <inheritdoc />
        public SoundEncoder(string filename, int sampleRate, int channelCount)
        {
            // Find a suitable writer for the given audio stream
            writer = SoundProcessorFactory.GetWriter(filename, sampleRate, channelCount);
            if (writer == null)
            {
                throw new NotSupportedException("Failed to open audio stream. Invalid audio format or not supported.");
            }

            // Retrieve the attributes of the sound
            ChannelCount = writer.ChannelCount;
            SampleRate   = writer.SampleRate;
        }

        /// <summary>
        /// Write a block of audio samples to the sound.
        /// </summary>
        /// <param name="samples">An array of sample to write to the current stream.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
        /// <param name="count">The number of bytes to be written to the current stream.</param>
        /// <returns>The number of samples actually read.</returns>
        public void Encode(short[] samples, int offset, int count)
        {
            writer?.Write(samples, offset, count);
        }
        
        /// <summary>
        /// Clears all buffers for this stream and causes any buffered data and necessary metadata to be written to the underlying device.
        /// When overriden, the implementation has control whether the buffered data automatically flushed upon writing samples.
        /// </summary>
        public void Flush()
        {
            writer?.Flush();
        }

        /// <summary>
        /// Closes the current writer and releases any resources associated. 
        /// Instead of calling this method, ensure that the stream is properly disposed.
        /// </summary>
        public void Close()
        {
            writer?.Close();
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="SoundEncoder"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    writer?.Dispose();
                }

                base.Dispose(disposing);
            }
        }
    }
}
