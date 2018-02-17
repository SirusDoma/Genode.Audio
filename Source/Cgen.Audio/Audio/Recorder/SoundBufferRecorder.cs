using System;
using System.Collections.Generic;
using System.Text;

namespace Cgen.Audio
{
    /// <summary>
    /// Represents a <see cref="SoundBuffer"/> from recorded audio samples via <see cref="SoundRecorder"/>.
    /// </summary>
    public class SoundBufferRecorder : SoundRecorder
    {
        private List<short> _samples;
        
        /// <summary>
        /// Get recorded <see cref="SoundBuffer"/>.
        /// </summary>
        public SoundBuffer Buffer
        {
            get; private set;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="SoundBufferRecorder"/> class.
        /// </summary>
        public SoundBufferRecorder()
            : base()
        {
            _samples = new List<short>();
        }

        protected internal override bool OnStart()
        {
            _samples.Clear();
            Buffer = new SoundBuffer();

            return true;
        }

        protected internal override bool OnProcessSample(short[] samples)
        {
            _samples.AddRange(samples);
            return true;
        }

        protected internal override void OnStop()
        {
            if (Buffer == null)
                throw new InvalidOperationException("You should start the recording with Start() before retrieving recorded sound data.");
            else if (_samples.Count > 0)
                Buffer = new SoundBuffer(_samples.ToArray(), ChannelCount, SampleRate);
        }
    }
}
