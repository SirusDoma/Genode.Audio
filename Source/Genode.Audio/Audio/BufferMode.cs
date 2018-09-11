namespace Genode.Audio
{
    /// <summary>
    /// Represents playback buffer mode.
    /// </summary>
    public enum BufferMode
    {
        /// <summary>
        /// System will load, decompress or decode whole audio data directly into memory at load time.
        /// This will reduce CPU utilization but may cause high memory usage and higher latency compared Stream mode.
        ///
        /// <para>
        /// Use this for small audio sample (e.g: Sound FX).
        /// </para>
        /// </summary>
        Sample,

        /// <summary>
        /// System will load audio data at runtime and stream it from provided source (e.g: disk).
        /// This will reduce memory usage and latency but may cause higher CPU utilization due to streaming process compared to Sample mode.
        ///
        /// <para>
        /// Use this for large audio sample (e.g: Background Music)
        /// </para>
        /// </summary>
        Stream
    }
}