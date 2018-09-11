using System;
using System.Collections.Generic;
using System.Text;

namespace Genode.Audio
{
    /// <summary>
    /// Represents a <see cref="Audio.Sound"/> from recorded audio samples via <see cref="SoundRecorder{T}"/>.
    /// </summary>
    /// <inheritdoc/>
    internal sealed class SoundBufferRecorder : SoundRecorder<Sound>
    {
        private List<short> samples;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="SoundBufferRecorder"/> class.
        /// </summary>
        /// <inheritdoc/>
        public SoundBufferRecorder()
            : base()
        {
            samples = new List<short>();
        }

        /// <summary>
        /// Initialize recording initiation of <see cref="SoundBufferRecorder"/>.
        /// </summary>
        /// <inheritdoc/>
        protected internal override void Initialize()
        {
            Output = new Sound(new short[0], ChannelCount, SampleRate);
            samples.Clear();
        }

        /// <summary>
        /// Process the given samples as recoded data.
        /// </summary>
        /// <param name="samples">The audio samples to process.</param>
        /// <returns><c>true</c> if there's more data to process; otherwise, <c>false</c>.</returns>
        /// <inheritdoc/>
        protected internal override bool ProcessSamples(short[] samples)
        {
            this.samples.AddRange(samples);
            return true;
        }

        /// <summary>
        /// Flush the recorded samples from the buffer to <see cref="Sound"/>.
        /// This will finalize the recording session.
        /// </summary>
        /// <exception cref="InvalidOperationException">Exception may occur in case Start() is not yet called.</exception>
        /// <inheritdoc/>
        protected internal override void Flush()
        {
            if (Output == null)
            {
                throw new InvalidOperationException(
                    "You should start the recording with Start() before retrieving recorded sound data.");
            }

            if (samples.Count > 0)
            {
                Output = new Sound(samples.ToArray(), ChannelCount, SampleRate);
            }
        }
    }
}
