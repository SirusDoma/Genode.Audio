using System;
using System.IO;
using System.Text;

namespace Genode.Audio
{
    /// <summary>
    /// Provide convenient functions to writes audio samples in binary to a stream.
    /// </summary>
    /// <inheritdoc/>
    public sealed class WavWriter : SoundWriter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WavWriter"/> class.
        /// </summary>
        /// <inheritdoc/>
        public WavWriter()
        {
        }
        
        /// <summary>
        /// Determines whether the <see cref="WavWriter"/> can handle the given file extension.
        /// </summary>
        /// <param name="extension">extension of the sound file to check.</param>
        /// <returns><c>true</c> if supported, otherwise <c>false</c>.</returns>
        /// <inheritdoc/>
        public override bool Check(string extension)
        {
            return string.Equals(extension, ".wav", StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Open a <see cref="Stream"/> of sound for writing.
        /// </summary>
        /// <param name="stream"><see cref="Stream"/> to open.</param>
        /// <param name="sampleRate">The samples rate of the sound, in samples per second.</param>
        /// <param name="channelCount">The number of channels of the sound.</param>
        /// <inheritdoc/>
        protected internal override void Initialize(Stream stream, int sampleRate, int channelCount)
        {
            var encoding = Encoding.UTF8;
            stream.Seek(0, SeekOrigin.Begin);
            
            using (var writer = new BinaryWriter(stream, encoding, true))
            {
                // Write main chunk ID
                var mainChunkId = encoding.GetBytes("RIFF");
                writer.Write(mainChunkId);

                // Write the main chunk header
                int mainChunkSize = 0;
                writer.Write(mainChunkSize);
                
                // Write the main chunk format
                var mainChunkFormat = encoding.GetBytes("WAVE");
                writer.Write(mainChunkFormat);

                // Write the sub-chunk 1 ("format") id and size
                var fmtChunkId = encoding.GetBytes("fmt ");
                writer.Write(fmtChunkId);
                
                // Write the sub chunk size
                int fmtChunkSize = 16;
                writer.Write(fmtChunkSize);

                // Write the format (PCM)
                short format = 1;
                writer.Write(format);

                // Write the sound attributes
                writer.Write((short)channelCount);
                writer.Write(sampleRate);
                
                int byteRate = sampleRate * channelCount * 2;
                writer.Write(byteRate);
                
                short blockAlign = (short)(channelCount * 2);
                writer.Write(blockAlign);
                
                short bitsPerSample = 16;
                writer.Write(bitsPerSample);

                // Write the sub-chunk 2 ("data") id and size
                var dataChunkId = encoding.GetBytes("data");
                writer.Write(dataChunkId);
                
                int dataChunkSize = 0;
                writer.Write(dataChunkSize);
            }
        }

        /// <summary>
        /// Writes a sequence of audio samples to the current stream
        /// and advances the current position within this stream by the number of bytes written.
        /// </summary>
        /// <param name="samples">An array of sample to write to the current stream.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
        /// <param name="count">The number of bytes to be written to the current stream.</param>
        /// <inheritdoc/>
        public override void Write(short[] samples, int offset, int count)
        {
            using (var writer = new BinaryWriter(BaseStream, Encoding.UTF8, true))
            {
                for (int i = offset; i < count; i++)
                {
                    writer.Write(samples[i]);
                }
                
                Flush();
            }
        }

        /// <summary>
        /// Clears all buffers for this stream and causes any buffered data and necessary metadata to be written to the underlying device.
        /// </summary>
        /// <inheritdoc/>
        public override void Flush()
        {
            using (var writer = new BinaryWriter(BaseStream, Encoding.UTF8, true))
            {
                // Update the main chunk size and data sub-chunk size
                int fileSize = (int)BaseStream.Position;
                int mainChunkSize = fileSize - 8;  // 8 bytes RIFF header
                int dataChunkSize = fileSize - 44; // 44 bytes RIFF + WAVE headers
                
                BaseStream.Seek(4, SeekOrigin.Begin);
                writer.Write(mainChunkSize);
                
                BaseStream.Seek(40, SeekOrigin.Begin);
                writer.Write(dataChunkSize);
                
                // Flush the stream
                base.Flush();
            }
        }
    }
}