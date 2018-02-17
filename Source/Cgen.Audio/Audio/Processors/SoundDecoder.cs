using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Cgen.Audio
{
    /// <summary>
    /// Represents a decoder to decode specific audio format.
    /// </summary>
    public abstract class SoundDecoder : IDisposable
    {
        /// <summary>
        /// Gets the sample information of current <see cref="SoundDecoder"/> buffer.
        /// </summary>
        public SampleInfo SampleInfo
        {
            get; protected internal set;
        }

        /// <summary>
        /// Gets the underlying <see cref="Stream"/> of the <see cref="SoundDecoder"/> instance.
        /// </summary>
        public Stream BaseStream
        {
            get; private set;
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="SoundDecoder"/> should close the source <see cref="Stream"/> upon disposing the decoder.
        /// </summary>
        public bool OwnStream
        {
            get; private set;
        }

        /// <summary>
        /// Gets a value indicating whether the decoder has been provided with invalid <see cref="Stream"/>.
        /// </summary>
        protected internal bool Invalid
        {
            get; private set;
        }

        /// <summary>
        /// Check if current <see cref="SoundDecoder"/> object can handle a give data from specified <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to check.</param>
        /// <returns><code>true</code> if supported, otherwise false.</returns>
        public abstract bool Check(Stream stream);

        /// <summary>
        /// Initializes a new instance of the <see cref="SoundDecoder"/> class.
        /// </summary>
        /// <param name="stream"> The < see cref="Stream"/> to open.</param>
        /// <param name="ownStream">Specify whether the <see cref="SoundDecoder"/> should close the source <see cref="Stream"/> upon disposing the decoder.</param>
        public SoundDecoder(Stream stream, bool ownStream = false)
        {
            BaseStream = stream;
            OwnStream  = ownStream;

            if (Check(stream))
                SampleInfo = Initialize();
            else
                Invalid = true;
        }

        /// <summary>
        /// Open a <see cref="Stream"/> of sound for reading.
        /// </summary>
        /// <returns>A <see cref="SampleInfo"/> containing sample information.</returns>
        protected abstract SampleInfo Initialize();

        /// <summary>
        /// Sets the current read position of the sample offset.
        /// </summary>
        /// <param name="sampleOffset">The index of the sample to jump to, relative to the beginning.</param>
        public abstract void Seek(long sampleOffset);

        /// <summary>
        /// Reads a block of audio samples from the current <see cref="Stream"/> and writes the data to given sample.
        /// </summary>
        /// <param name="samples">The sample array to fill.</param>
        /// <param name="count">The maximum number of samples to read.</param>
        /// <returns>The number of samples actually read.</returns>
        public abstract long Read(short[] samples, long count);

        /// <summary>
        /// Release all resources used by the <see cref="SoundDecoder"/>.
        /// </summary>
        public virtual void Dispose()
        {
            if (OwnStream && !Invalid)
                BaseStream?.Dispose();
        }
    }
}
