using System;
using System.IO;
using System.Text;

namespace Genode.Audio
{
    /// <summary>
    /// Provides function to read wav binary data as audio sample values.
    /// </summary>
    /// <inheritdoc/>
    public sealed class VorbisReader : SoundReader
    {
        private NVorbis.VorbisReader reader;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="VorbisReader"/> class.
        /// </summary>
        public VorbisReader()
            : base()
        {
        }
        
        /// <summary>
        /// Determines whether the <see cref="VorbisReader" /> can handle the given <see cref="Stream" /> of audio data.
        /// </summary>
        /// <param name="stream"><see cref="Stream" /> to check.</param>
        /// <returns><c>true</c> if supported, otherwise <c>false</c>.</returns>
        /// <inheritdoc />
        public override bool Check(Stream stream)
        {
            var encoding  = Encoding.UTF8;
            using (var reader = new BinaryReader(stream, encoding, true))
            {
                stream.Seek(0, SeekOrigin.Begin);
                return encoding.GetString(reader.ReadBytes(4)) == "OggS";
            }
        }

        /// <summary>
        /// Open a <see cref="Stream"/> of sound for reading.
        /// </summary>
        /// <param name="stream"><see cref="Stream"/> to open.</param>
        /// <param name="leaveOpen">Specifies whether the <paramref name="stream"/> should leave open after the current instance of the <see cref="SoundReader"/> disposed.</param>
        /// <returns>A <see cref="SampleInfo"/> containing sample information if decoder is can handle the stream, otherwise <c>null</c>.</returns>
        /// <inheritdoc/>
        protected internal override SampleInfo Initialize(Stream stream, bool leaveOpen = false)
        {
            reader = new NVorbis.VorbisReader(stream, leaveOpen);
            return new SampleInfo(reader.TotalSamples * reader.Channels, reader.Channels, reader.SampleRate);
        }

        /// <summary>
        /// Sets the current read position within current stream.
        /// </summary>
        /// <param name="sampleOffset">The index of the sample to jump to, relative to the beginning.</param>
        /// <inheritdoc/>
        public override void Seek(long sampleOffset)
        {
            reader.DecodedPosition = sampleOffset / SampleInfo.ChannelCount;
        }

        /// <summary>
        /// Reads a block of audio samples from the <see cref="VorbisReader.BaseStream"/> and writes the data to given array of samples.
        /// </summary>
        /// <param name="samples">Sample array to fill.</param>
        /// <param name="count">Maximum number of samples to read.</param>
        /// <returns>The number of samples actually read.</returns>
        /// <inheritdoc/>
        public override long Read(short[] samples, long count)
        {
            float[] buffer = new float[count];
            int read = reader.ReadSamples(buffer, 0, (int)count);
            DecodeSamples(buffer, samples, read);

            return read;
        }
        
        /// <summary>
        /// Decode the float samples into pcm samples.
        /// </summary>
        /// <param name="input">Float samples to decode.</param>
        /// <param name="output">Output of decoded samples.</param>
        /// <param name="length">The number of samples that will be decoded.</param>
        private static void DecodeSamples(float[] input, short[] output, int length)
        {
            for (int i = 0; i < length; i++)
            {
                int temp = (int)(32767f * input[i]);
                if (temp > short.MaxValue)
                    temp = short.MaxValue;
                else if (temp < short.MinValue)
                    temp = short.MinValue;

                output[i] = (short)temp;
            }
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="VorbisReader"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    reader?.Dispose();
                }

                IsDisposed = true;
            }
        }
    }
}