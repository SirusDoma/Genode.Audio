using System.IO;

namespace Genode.Audio
{
    /// <summary>
    /// Provide convenient functions to reads audio samples in binary to a stream.
    /// </summary>
    /// <inheritdoc/>
    public abstract class SoundWriter : DisposableResource
    {
        /// <summary>
        /// Gets the underlying <see cref="Stream"/> of the <see cref="SoundWriter"/>.
        /// </summary>
        public Stream BaseStream { get; internal set; }
        
        /// <summary>
        /// Gets the samples rate of the sound, in samples per second.
        /// </summary>
        protected internal int SampleRate { get; internal set; }
        
        /// <summary>
        /// Gets the number of channels of the sound.
        /// </summary>
        protected internal int ChannelCount { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SoundWriter"/> class.
        /// </summary>
        public SoundWriter()
        {
        }
        
        /// <summary>
        /// Determines whether the <see cref="SoundWriter"/> can handle the given file extension.
        /// </summary>
        /// <param name="extension">extension of the sound file to check.</param>
        /// <returns><c>true</c> if supported, otherwise <c>false</c>.</returns>
        public abstract bool Check(string extension);

        /// <summary>
        /// Open a <see cref="Stream"/> of sound for writing.
        /// </summary>
        /// <param name="stream"><see cref="Stream"/> to open.</param>
        /// <param name="sampleRate">The samples rate of the sound, in samples per second.</param>
        /// <param name="channelCount">The number of channels of the sound.</param>
        protected internal abstract void Initialize(Stream stream, int sampleRate, int channelCount);

        /// <summary>
        /// Writes a sequence of audio samples to the current stream
        /// and advances the current position within this stream by the number of bytes written.
        /// </summary>
        /// <param name="samples">An array of sample to write to the current stream.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
        /// <param name="count">The number of bytes to be written to the current stream.</param>
        public abstract void Write(short[] samples, int offset, int count);

        /// <summary>
        /// Closes the current stream and releases any resources associated with the current stream.
        /// </summary>
        public virtual void Close()
        {
            Dispose(true);
        }

        /// <summary>
        /// Clears all buffers for this stream and causes any buffered data and necessary metadata to be written to the underlying device.
        /// When overriden, the implementation has control whether the buffered data automatically flushed upon writing samples.
        /// </summary>
        public virtual void Flush()
        {
            BaseStream.Flush();
        }

        /// <summary>
        /// Flush the underlying stream and releases the unmanaged resources used by the <see cref="SoundWriter"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                Flush();
                if (disposing)
                {
                    BaseStream?.Dispose();
                }

                base.Dispose(disposing);
            }
        }
    }
}