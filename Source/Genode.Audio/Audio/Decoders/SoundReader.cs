using System.IO;


namespace Genode.Audio
{
    /// <summary>
    /// Provides function to read binary data as audio sample values.
    /// </summary>
    /// <inheritdoc />
    public abstract class SoundReader : DisposableResource
    {
        /// <summary>
        /// Gets the sample information.
        /// </summary>
        public SampleInfo SampleInfo { get; internal set; }

        /// <summary>
        /// Gets the underlying <see cref="Stream"/> of the <see cref="SoundReader"/>.
        /// </summary>
        public Stream BaseStream { get; internal set; }

        /// <summary>
        /// Gets or sets a value indicating whether <see cref="BaseStream"/> should leave open after the current instance of the <see cref="SoundReader"/> disposed.
        /// </summary>
        protected internal bool LeaveOpen { get; internal set; }

        /// <summary>
        /// Determines whether the <see cref="SoundReader"/> can handle the given <see cref="Stream"/> of audio data.
        /// </summary>
        /// <param name="stream"><see cref="Stream"/> to check.</param>
        /// <returns><c>true</c> if supported, otherwise <c>false</c>.</returns>
        public abstract bool Check(Stream stream);

        /// <summary>
        /// Open a <see cref="Stream"/> of sound for reading.
        /// </summary>
        /// <param name="stream"><see cref="Stream"/> to open.</param>
        /// <param name="leaveOpen">Specifies whether the <paramref name="stream"/> should leave open after the current instance of the <see cref="SoundReader"/> disposed.</param>
        /// <returns>A <see cref="Audio.SampleInfo"/> containing sample information if decoder is can handle the stream, otherwise <c>null</c>.</returns>
        protected internal abstract SampleInfo Initialize(Stream stream, bool leaveOpen = false);

        /// <summary>
        /// Sets the current read position within current stream.
        /// </summary>
        /// <param name="sampleOffset">The index of the sample to jump to, relative to the beginning.</param>
        public abstract void Seek(long sampleOffset);

        /// <summary>
        /// Reads a block of audio samples from the <see cref="BaseStream"/> and writes the data to given array of samples.
        /// </summary>
        /// <param name="samples">Sample array to fill.</param>
        /// <param name="count">Maximum number of samples to read.</param>
        /// <returns>The number of samples actually read.</returns>
        public abstract long Read(short[] samples, long count);

        /// <summary>
        /// Closes the current reader and releases any resources associated. 
        /// Instead of calling this method, ensure that the stream is properly disposed.
        /// </summary>
        public void Close()
        {
            BaseStream?.Close();
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="SoundReader"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    if (!LeaveOpen)
                    {
                        BaseStream?.Dispose();
                    }
                }

                base.Dispose(disposing);
            }
        }
    }
}
