using System;
using System.Linq;

namespace Genode.Audio
{
    internal class Music : SoundStream
    {
        private SoundDecoder decoder;
        private long offset = 0;
        
        internal Music(int handle, Sound buffer) 
            : base(handle, buffer)
        {
            decoder = buffer.Decoder;
            Initialize(buffer.ChannelCount, buffer.SampleRate);
        }

        protected override void Seek(TimeSpan time)
        {
            lock (Buffer)
            {
                // Seek into specified time and track the sample offset
                decoder.Seek(time);
                offset = decoder.SampleOffset;
            }
        }

        protected override bool GetStreamData(out short[] samples)
        {
            // Now, lock with sound object instead since the decoder / underlying stream can be shared among other SoundStream instances
            // This will guarantee that none but this SoundStream that has access to the decoder / underlying stream
            lock (Buffer)
            {
                // Initialize the number of sample should be read into buffer
                int sampleCount = ChannelCount * SampleRate;
    
                // Rebuild the given samples
                samples = new short[sampleCount];
                
                // Now, the decoder and / or the underlying stream instance can be shared among existing instances of SoundStream
                // In this case, decoder offset might have been changed / moved somewhere else by another instance due to decoding process that advance the offset.
                // So we need to track the offset, restore it before decoding the sample and keep tracking it after decoding process finish
                decoder.Seek(offset);
                
                // Decode the audio sample
                long read = decoder.Decode(samples, sampleCount);
                
                // Track the offset in case it modified by another instance of sound stream
                offset = decoder.SampleOffset;
                
                // Trim samples to the number of sample actually read
                // otherwise, the sound may have extended duration which will make an empty gap for looping sound.
                samples = read != samples.Length ? samples.Take((int)read).ToArray() : samples;
    
                // Tell whether any data left to stream
                return read == sampleCount;
            }
        }
    }
}