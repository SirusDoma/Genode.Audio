using System;
using System.IO;
using System.Linq;

namespace Genode.Audio
{
    public class VorbisWriter : SoundWriter
    {
        private OggVorbisEncoder.OggStream vorbisStream;
        private OggVorbisEncoder.VorbisInfo sampleInfo;
        private OggVorbisEncoder.ProcessingState state;
        
        public VorbisWriter()
        {
        }
        
        public override bool Check(string extension)
        {
            return string.Equals(extension, ".ogg", StringComparison.InvariantCultureIgnoreCase);
        }

        protected internal override void Initialize(Stream stream, int sampleRate, int channelCount)
        {
            // Reset stream position
            stream.Seek(0, SeekOrigin.Begin);

            // Initialize the ogg/vorbis stream
            vorbisStream = new OggVorbisEncoder.OggStream(new Random().Next());

            // Setup the encoder: VBR, automatic bitrate management
            // Quality is in range [-1 .. 1], 0.4 gives ~128 kbps for a 44 KHz stereo sound
            sampleInfo = OggVorbisEncoder.VorbisInfo.InitVariableBitRate(channelCount, sampleRate, 0.1f);

            // Generate header metadata (leave it empty)
            var headerBuilder = new OggVorbisEncoder.HeaderPacketBuilder();
            var comments = new OggVorbisEncoder.Comments();

            // Generate the header packets
            var infoPacket = headerBuilder.BuildInfoPacket(sampleInfo);
            var commentsPacket = headerBuilder.BuildCommentsPacket(comments);
            var booksPacket = headerBuilder.BuildBooksPacket(sampleInfo);

            // Write the header packets to the ogg stream
            vorbisStream.PacketIn(infoPacket);
            vorbisStream.PacketIn(commentsPacket);
            vorbisStream.PacketIn(booksPacket);

            // Flush to force audio data onto its own page per the spec
            while (vorbisStream.PageOut(out OggVorbisEncoder.OggPage page, true))
            {
                stream.Write(page.Header, 0, page.Header.Length);
                stream.Write(page.Body, 0, page.Body.Length);
            }

            state = OggVorbisEncoder.ProcessingState.Create(sampleInfo);
        }

        public override void Write(short[] samples, int offset, int count)
        {
            // Do not write anymore data if stream has reached it's eof
            if (vorbisStream.Finished)
            {
                return;
            }

            // A frame contains a sample from each channel
            //Array.Resize(ref samples, samples.Length + ((SampleRate * ChannelCount)));
            var data = samples.Skip(offset).Take(count).SelectMany(sample => BitConverter.GetBytes(sample)).ToArray();
            int bufferSize = data.Length / 4;
            
            // Prepare a buffer to hold samples
            var buffer = new float[ChannelCount][];
            for (int channel = 0; channel < ChannelCount; channel++)
            {
                buffer[channel] = new float[bufferSize];
            }

            // Process sample into pcm
            for (var i = 0; i < bufferSize; i++)
            {
                // uninterleave samples
                buffer[0][i] = (short) ((data[i*4 + 1] << 8) | (0x00ff & data[i*4]))/32768f;
                buffer[1][i] = (short) ((data[i*4 + 3] << 8) | (0x00ff & data[i*4 + 2]))/32768f;
            }

            // Tell the library how many samples we've written
            state.WriteData(buffer, bufferSize);
            Flush();
        }

        public override void Flush()
        {
            // Get new packets from the bitrate management engine
            while (!vorbisStream.Finished && state.PacketOut(out OggVorbisEncoder.OggPacket packet))
            {
                // Write the packet to the ogg stream
                vorbisStream.PacketIn(packet);

                // If the stream produced new pages, write them to the output file
                while (vorbisStream.PageOut(out OggVorbisEncoder.OggPage page, false))
                {
                    BaseStream.Write(page.Header, 0, page.Header.Length);
                    BaseStream.Write(page.Body, 0, page.Body.Length);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                Flush();
                state.WriteEndOfStream();
                
                base.Dispose(disposing);
            }
        }
    }
}