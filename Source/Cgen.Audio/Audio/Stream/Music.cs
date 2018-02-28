using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Cgen.Audio
{
    /// <summary>
    /// Represents a streamed music.
    /// </summary>
    public class Music : SoundStream
    {
        private SoundDecoder _decoder;
        private TimeSpan     _duration;
        private int          _sampleCount;
        private SampleInfo   _info;

        /// <summary>
        /// Gets total duration of current <see cref="Music"/> object.
        /// </summary>
        public override TimeSpan Duration
        {
            get
            {
                return _duration;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Music"/> class.
        /// </summary>
        public Music()
            : base()
        {
            _decoder      = null;
            _sampleCount = 0;
            _duration    = TimeSpan.Zero;

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Music"/> class
        /// from specified file.
        /// </summary>
        /// <param name="filename">The path of the sound file to load.</param>
        public Music(string filename)
            : this(File.Open(filename, FileMode.Open))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Music"/> class
        /// from specified an array of byte containing sound data.
        /// </summary>
        /// <param name="data">An array of byte contains sound data to load.</param>
        public Music(byte[] data)
            : this(new MemoryStream(data), true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Music"/> class
        /// from specified <see cref="Stream"/> containing sound data.
        /// </summary>
        /// <param name="stream">A <see cref="Stream"/> contains sound data to load.</param>
        /// <param name="ownStream">Specifiy a value indicating whether the source stream should be disposed along with instance disposal.</param>
        public Music(Stream stream, bool ownStream = true)
            : this()
        {
            _decoder = SoundProcessorFactory.CreateDecoder(stream, ownStream);
            if (_decoder != null)
            {
                _info = _decoder.SampleInfo; //_decoder.Open(stream);
                Initialize(_info.ChannelCount, _info.SampleRate);
            }
            else
            {
                throw new NotSupportedException("The specified sound is not supported.");
            }
        }

        /// <summary>
        /// Request a new chunk of audio samples from the stream source.
        /// </summary>
        /// <param name="samples">The audio chunk that contains audio samples.</param>
        /// <returns><code>true</code> if reach the end of stream, otherwise false.</returns>
        protected override bool OnGetData(out short[] samples)
        {
            // Fill the chunk parameters
            samples = new short[_sampleCount];
            long count = _decoder.Read(samples, samples.Length);

            // Remove the gap when processing last buffer
            //Array.Resize(ref samples, (int)count);

            // Check if we have reached the end of the audio file
            return count == _sampleCount;
        }

        /// <summary>
        /// Change the current playing position in the stream source.
        /// </summary>
        /// <param name="time">Seek to specified time.</param>
        protected override void OnSeek(TimeSpan time)
        {
            _decoder.Seek((long)time.TotalSeconds * SampleRate * ChannelCount);
        }

        /// <summary>
        /// Performs initialization steps by providing the audio stream parameters.
        /// </summary>
        /// <param name="channelCount">The number of channels of current <see cref="SoundStream"/> object.</param>
        /// <param name="sampleRate">The sample rate, in samples per second.</param>
        protected override void Initialize(int channelCount, int sampleRate)
        {
            // Compute the music duration
            _duration = TimeSpan.FromSeconds(_info.SampleCount / channelCount / (float)sampleRate);

            // Set the internal buffer size so that it can contain 1 second of audio samples
            _sampleCount = sampleRate * channelCount;

            // Initialize the stream
            base.Initialize(channelCount, sampleRate);
        }

        protected internal override void ResetBuffer()
        {
            _decoder?.Dispose();
            base.ResetBuffer();
        }

        /// <summary>
        /// Releases all resources used by the <see cref="Music"/>.
        /// </summary>
        public override void Dispose()
        {
            _decoder?.Dispose();
            base.Dispose();
        }
    }
}
