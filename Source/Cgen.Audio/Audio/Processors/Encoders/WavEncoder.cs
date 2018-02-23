using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Cgen.Audio
{
    public class WavEncoder : SoundEncoder
    {
        /// <summary>
        /// Check if current <see cref="WavDecoder"/> object can handle a give data from specified extension.
        /// </summary>
        /// <param name="extension">The extension to check.</param>
        /// <returns><code>true</code> if supported, otherwise false.</returns>
        public override bool Check(string extension)
        {
            return extension.ToLower().EndsWith("wav");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WavDecoder"/> class.
        /// </summary>
        public WavEncoder(Stream stream, int sampleRate, int channelCount, bool ownStream = false)
            : base(stream, sampleRate, channelCount, ownStream)
        {
        }

        private bool WriteHeader()
        {
            // Seek to the first offset
            BaseStream.Seek(0, SeekOrigin.Begin);

            // Initialize writer
            var writer = new BinaryWriter(BaseStream);

            // Write the main chunk ID
            writer.Write(Encoding.UTF8.GetBytes("RIFF"));

            // Write the main chunk header
            writer.Write((int)0); // placeholder, will be written later
            writer.Write(Encoding.UTF8.GetBytes("WAVE"));

            // Write the sub-chunk 1 ("format") id and size
            writer.Write(Encoding.UTF8.GetBytes("fmt "));
            writer.Write((int)16);

            // Write the format (PCM)
            writer.Write((short)WavFormat.PCM);

            // Write the sound attributes
            writer.Write((short)SampleInfo.ChannelCount);
            writer.Write((int)SampleInfo.SampleRate);
            writer.Write((int)(SampleInfo.SampleRate * SampleInfo.ChannelCount * 2)); // Bitrate
            writer.Write((short)(SampleInfo.ChannelCount * 2)); // Block Align
            writer.Write((short)16);

            // Write the sub-chunk 2 ("data") id and size
            writer.Write(Encoding.UTF8.GetBytes("data"));
            writer.Write((int)0); // placeholder, will be written later

            return true;
        }

        /// <summary>
        /// Open a <see cref="Stream"/> of sound for reading.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to open.</param>
        /// <param name="ownStream">Specify whether the <see cref="SoundDecoder"/> should close the source <see cref="Stream"/> upon disposing the reader.</param>
        /// <returns>A <see cref="SampleInfo"/> containing sample information.</returns>
        protected override bool Initialize()
        {
            return WriteHeader();
        }

        /// <summary>
        /// Write a block of audio samples to the current <see cref="Stream"/>.
        /// </summary>
        /// <param name="samples">The sample array to fill.</param>
        /// <param name="count">The maximum number of samples to write.</param>
        public override void Write(short[] samples, long count)
        {
            var writer = new BinaryWriter(BaseStream);
            for (int i = 0; i < count; i++)
            {
                if (i >= samples.Length)
                    break;

                writer.Write((short)samples[i]);
            }

            // Update the main chunk size and data sub-chunk size
            int size = (int)BaseStream.Length;
            int mainChunkSize = size - 8;  // 8 bytes RIFF header
            int dataChunkSize = size - 44; // 44 bytes RIFF + WAVE headers

            BaseStream.Seek(4, SeekOrigin.Begin);
            writer.Write(mainChunkSize);

            BaseStream.Seek(40, SeekOrigin.Begin);
            writer.Write(dataChunkSize);
        }
    }
}
