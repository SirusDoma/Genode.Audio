using System;

using OpenTK;
using OpenTK.Audio.OpenAL;

using Genode;
using Genode.Internal.OpenAL;

namespace Genode.Audio
{
    internal class SoundSample : SoundChannel
    {
        private Sound buffer;

        /// <summary>
        /// Gets or sets the <see cref="Sound" /> of current <see cref="SoundSample" /> object.
        /// </summary>
        /// <inheritdoc />
        protected internal override Sound Buffer
        {
            get => buffer;
            set
            {
                // Detach existing buffer
                if (buffer != null)
                {
                    Stop();
                    ALChecker.Check(() => AL.Source(Handle, ALSourcei.Buffer, 0));
                }

                // Attach given buffer
                buffer = value;
                ALChecker.Check(() => AL.Source(Handle, ALSourcei.Buffer, buffer?.Handle ?? 0));
            }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="SoundSample"/> class.
        /// </summary>
        /// <param name="handle">A valid OpenAL Source Handle.</param>
        /// <param name="buffer">A sound that contains valid audio sample buffer.</param>
        /// <inheritdoc/>
        internal SoundSample(int handle, Sound buffer)
            : base(handle, buffer)
        {
        }

        /// <summary>
        /// Start or resume playing the <see cref="SoundSample"/>.
        /// <para>
        /// This function starts the source if it was stopped, resumes it if it was paused, and restarts it from the beginning if it was already playing.
        /// </para>
        /// </summary>
        /// <inheritdoc/>
        protected internal override void Play()
        {
            ALChecker.Check(() => AL.SourcePlay(Handle));
        }

        /// <summary>
        /// Puase playing the <see cref="SoundSample"/>.
        /// <para>
        /// This function pauses the source if it was playing, otherwise (source already paused or stopped) it has no effect.
        /// </para>
        /// </summary>
        /// <inheritdoc/>
        protected internal override void Pause()
        {
            ALChecker.Check(() => AL.SourcePause(Handle));
        }

        /// <summary>
        /// Stop playing the <see cref="SoundSample"/>.
        /// <para>
        /// This function stops the source if it was playing or paused, and does nothing if it was already stopped.
        /// It also resets the playing position (unlike <see cref="Pause"/>).
        /// </para>
        /// </summary>
        /// <inheritdoc/>
        protected internal override void Stop()
        {
            ALChecker.Check(() => AL.SourceStop(Handle));
        }

        /// <summary>
        /// Reload the audio channel properties.
        /// </summary>
        /// <inheritdoc/>
        protected internal override void Reload()
        {
            base.Reload();
            ALChecker.Check(() => AL.Source(Handle, ALSourcei.Buffer, Buffer?.Handle ?? 0));
        }

        /// <summary>
        /// Reset the audio channel properties.
        /// </summary>
        /// <inheritdoc/>
        protected internal override void Reset()
        {
            base.Reset();
            ALChecker.Check(() => AL.Source(Handle, ALSourcei.Buffer, 0));
        }
    }
}
