using System;
using System.IO;
using System.Text;

namespace Genode.Audio
{
    /// <summary>
    /// Provides function to read wav binary data as audio sample values.
    /// </summary>
    /// <inheritdoc/>
    public sealed class WavReader : SoundReader
    {
        /// <summary>
        /// Represents wav audio format.
        /// </summary>
        public enum WavFormat
        {
            PCM = 1,
            Float = 3
        }

        private int bytesPerSample;
        private long dataStart;
        private long dataEnd;
        private WavFormat format;

        /// <summary>
        /// Initializes a new instance of the <see cref="WavReader"/> class.
        /// </summary>
        public WavReader()
            : base()
        {
        }

        /// <summary>
        /// Determines whether the <see cref="WavReader" /> can handle the given <see cref="Stream" /> of audio data.
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
                
                byte[] header = reader.ReadBytes(12);
                return encoding.GetString(header, 0, 4) == "RIFF" &&
                       encoding.GetString(header, 8, 4) == "WAVE";
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
            // If we are here, it means that the first part of the header
            // (the format) has already been checked
            stream.Seek(12, SeekOrigin.Begin);
            using (var reader = new BinaryReader(stream, Encoding.UTF8, true))
            {
                int sampleCount = 0;
                short channelCount = 0;
                int sampleRate = 0;

                // Parse all the sub-chunks
                bool dataChunkFound = false;
                while (!dataChunkFound)
                {
                    // Parse the sub-chunk id
                    byte[] subChunkId = new byte[4];
                    if (stream.Read(subChunkId, 0, 4) != 4)
                    {
                        throw new FormatException("Failed to open WAV sound file (invalid or unsupported file)");
                    }

                    // Get chunk information
                    int subChunkSize = reader.ReadInt32();
                    long subChunkStart = stream.Position;

                    // Check whether the signature is valid, either fmt or data
                    string signature = Encoding.UTF8.GetString(subChunkId);
                    if (signature == "fmt ")
                    {
                        // Audio format
                        format = (WavFormat) reader.ReadInt16();
                        if (format != WavFormat.PCM && format != WavFormat.Float)
                        {
                            throw new NotSupportedException("Wav format is not supported.");
                        }

                        // Channel count
                        channelCount = reader.ReadInt16();

                        // Sample rate
                        sampleRate = reader.ReadInt32();

                        // Byte rate
                        int byteRate = reader.ReadInt32();

                        // Block align
                        short blockAlign = reader.ReadInt16();

                        // Bits per sample
                        short bitsPerSample = reader.ReadInt16();
                        if (bitsPerSample != 8 && bitsPerSample != 16 && bitsPerSample != 24 && bitsPerSample != 32)
                        {
                            throw new NotSupportedException(
                                $"Unsupported sample size: {bitsPerSample} bit (Supported sample sizes are 8/16/24/32 bit)"
                            );
                        }
                        else
                        {
                            bytesPerSample = bitsPerSample / 8;
                        }

                        // Skip potential extra information (should not exist for PCM)
                        if (subChunkSize > 16)
                        {
                            if (stream.Seek(subChunkSize - 16, SeekOrigin.Current) == -1)
                            {
                                throw new FormatException(
                                    "Failed to open WAV sound file (invalid or unsupported file)");
                            }
                        }
                    }
                    else if (signature == "data")
                    {
                        // Compute the total number of samples
                        sampleCount = subChunkSize / bytesPerSample;

                        // Store the start and end position of samples in the file
                        dataStart = subChunkStart;
                        dataEnd = dataStart + sampleCount * bytesPerSample;

                        dataChunkFound = true;
                    }
                    else
                    {
                        // unknown chunk, skip it
                        if (stream.Seek(subChunkSize, SeekOrigin.Current) == -1)
                        {
                            throw new FormatException(
                                "Failed to open WAV sound file (invalid or unsupported file)");
                        }
                    }
                }

                return new SampleInfo(sampleCount, channelCount, sampleRate);
            }
        }

        /// <summary>
        /// Sets the current read position within current stream.
        /// </summary>
        /// <param name="sampleOffset">The index of the sample to jump to, relative to the beginning.</param>
        /// <inheritdoc/>
        public override void Seek(long sampleOffset)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(WavReader));
            }

            BaseStream.Seek(dataStart + sampleOffset * bytesPerSample, SeekOrigin.Begin);
        }

        /// <summary>
        /// Reads a block of audio samples from the <see cref="BaseStream"/> and writes the data to given array of samples.
        /// </summary>
        /// <param name="samples">Sample array to fill.</param>
        /// <param name="count">Maximum number of samples to read.</param>
        /// <returns>The number of samples actually read.</returns>
        /// <inheritdoc/>
        public override long Read(short[] samples, long count)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(WavReader));
            }
 
            long read = 0, index = 0;
            while ((read < count) && (BaseStream.Position < dataEnd))
            {
                byte[] buffer = new byte[bytesPerSample];
                if (BaseStream.Read(buffer, 0, buffer.Length) == bytesPerSample)
                {
                    switch (bytesPerSample)
                    {
                        case 1:
                        {
                            byte sample = buffer[0];
                            samples[index++] = (short) ((sample - 128) << 8);
                            break;
                        }
                        case 2:
                        {
                            short sample = BitConverter.ToInt16(buffer, 0);
                            samples[index++] = sample;
                            break;
                        }
                        case 3:
                        {
                            int sample = buffer[0] | (buffer[1] << 8) | (buffer[2] << 16);
                            samples[index++] = (short) (sample >> 8);
                            break;
                        }
                        case 4:
                        {
                            if (format == WavFormat.PCM)
                            {
                                int sample = BitConverter.ToInt32(buffer, 0);
                                samples[index++] = (short) (sample >> 16);
                            }
                            else if (format == WavFormat.Float)
                            {
                                float sample = BitConverter.ToSingle(buffer, 0);
                                samples[index++] = (short) (sample * 32767.0f);
                            }

                            break;
                        }
                    }
                }

                read++;
            }


            return read;
        }
    }
}
