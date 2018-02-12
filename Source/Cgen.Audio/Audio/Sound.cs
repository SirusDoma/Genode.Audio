using System;
using System.Collections.Generic;
using System.Text;

using OpenTK.Audio;
using OpenTK.Audio.OpenAL;

using Cgen;
using Cgen.Internal.OpenAL;

namespace Cgen.Audio
{
    /// <summary>
    /// Represents a sound that can be played in the audio environment.
    /// </summary>
    public sealed class Sound : SoundSource, IDisposable
    {
        private SoundBuffer _buffer;

        /// <summary>
        /// Gets or sets the <see cref="SoundBuffer"/> containing the audio data to play.
        /// </summary>
        public SoundBuffer Buffer
        {
            get
            {
                return _buffer;
            }
            set
            {
                // First detach from the previous buffer
                if (Status != SoundStatus.Initial && Status != SoundStatus.Stopped)
                    Stop();

                // Detach existing buffer
                _buffer?.DetachSound(this);

                // Assign and use the new buffer
                _buffer = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the current <see cref="Sound"/> object is in loop mode.
        /// </summary>
        public override bool IsLooping
        {
            get
            {
                bool looping = false;
                ALChecker.Check(() => AL.GetSource(Handle, ALSourceb.Looping, out looping));

                return looping;
            }
            set
            {
                ALChecker.Check(() => AL.Source(Handle, ALSourceb.Looping, value));
            }
        }

        /// <summary>
        /// Gets or sets the current playing position of the current <see cref="Sound"/> object.
        /// </summary>
        public override TimeSpan PlayingOffset
        {
            get
            {
                float seconds = 0f;
                ALChecker.Check(() => AL.GetSource(Handle, ALSourcef.SecOffset, out seconds));

                return TimeSpan.FromSeconds(seconds);
            }
            set
            {
                ALChecker.Check(() => AL.Source(Handle, ALSourcef.SecOffset, (float)value.TotalSeconds));
            }
        }

        /// <summary>
        /// Gets total duration of current <see cref="Sound"/> object.
        /// </summary>
        public override TimeSpan Duration
        {
            get
            {
                if (_buffer != null)
                {
                    return _buffer.Duration;
                }

                return TimeSpan.Zero;
            }
        }

        internal Sound(int handle)
        {
            Handle = handle;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Sound"/> class.
        /// </summary>
        public Sound()
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Sound"/> class
        /// with specified <see cref="SoundBuffer"/>.
        /// </summary>
        /// <param name="buffer">The <see cref="SoundBuffer"/> containing souhd sample.</param>
        public Sound(SoundBuffer buffer)
        {
            Buffer = buffer;
        }

        /// <summary>
        /// Start or resume playing the current <see cref="Sound"/> object.
        /// </summary>
        protected internal override void Play()
        {
            Preload();
            ALChecker.Check(() => AL.SourcePlay(Handle));
        }

        /// <summary>
        /// Pause the current <see cref="Sound"/> object.
        /// </summary>
        protected internal override void Pause()
        {
            ALChecker.Check(() => AL.SourcePause(Handle));
        }

        /// <summary>
        /// Stop playing the current <see cref="Sound"/> object.
        /// </summary>
        protected internal override void Stop()
        {
            ALChecker.Check(() => AL.SourceStop(Handle));
        }

        internal void Preload()
        {
            // Just to make sure it is not preloaded and it is first time call
            if (Handle <= 0 || !Validate())
            {
                // Get the source from the pool
                Handle = SoundSystem.Instance.GetSource();
            }

            // Attach the sound if it's not attached yet
            // In case the first time call or the Source handle is replaced due to recycle
            int buffer = 0;
            ALChecker.Check(() => AL.GetSource(Handle, ALGetSourcei.Buffer, out buffer));

            if (Buffer.Handle != buffer || !Buffer.IsAttached(this))
            {
                // Only attach if its not attached
                if (!Buffer.IsAttached(this))
                    Buffer.AttachSound(this);

                ALChecker.Check(() => AL.Source(Handle, ALSourcei.Buffer, Buffer.Handle));
            }

        }

        /// <summary>
        /// Reset the attached <see cref="SoundBuffer"/> from <see cref="Sound"/> instance.
        /// </summary>
        protected internal override void ResetBuffer()
        {
            // First stop the sound in case it is playing
            if (Handle > 0)
            {
                // Detach the buffer
                ALChecker.Check(() => AL.Source(Handle, ALSourcei.Buffer, 0));

                Buffer?.DetachSound(this);
                Buffer = null;
            }
        }

        /// <summary>
        /// Releases all resources used by the <see cref="Sound"/>.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
            
            if (Buffer != null && ALChecker.Check(() => AL.IsBuffer((int)Buffer.Handle)))
            {
                ALChecker.Check(() => AL.DeleteBuffer((int)Buffer.Handle));
            }

            //var sources = SoundSystem.Instance.GetPlayingSources();
            //for (int i = 0; i < sources.Length;)
            //{
            //    var sound = sources[i] as Sound;
            //    if (sound?.Buffer?.Handle == Buffer?.Handle)
            //    {
            //        sound.Stop();
            //        SoundSystem.Instance.Queue(sound);
            //        continue;
            //    }

            //    i++;
            //}

            //ResetBuffer();
        }
    }
}
