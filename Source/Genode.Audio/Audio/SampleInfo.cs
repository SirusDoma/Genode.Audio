namespace Genode.Audio
{
    /// <summary>
    /// Represents a sample properties of an audio.
    /// </summary>
    public struct SampleInfo
    {
        /// <summary>
        /// Gets total number of samples of the sound.
        /// </summary>
        public readonly long SampleCount;

        /// <summary>
        /// Gets the number of channels of the sound.
        /// </summary>
        public readonly int ChannelCount;

        /// <summary>
        /// Gets the samples rate of the sound, in samples per second.
        /// </summary>
        public readonly int SampleRate;

        /// <summary>
        /// Initializes a new instance of the <see cref="SampleInfo"/> struct.
        /// </summary>
        /// <param name="sampleCount">The number of samples.</param>
        /// <param name="channelCount">The number of channels.</param>
        /// <param name="sampleRate">The sample rate, in sample per second.</param>
        public SampleInfo(long sampleCount, int channelCount, int sampleRate)
        {
            SampleCount = sampleCount;
            ChannelCount = channelCount;
            SampleRate = sampleRate;
        }
    }
}
