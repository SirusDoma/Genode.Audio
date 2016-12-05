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
    /// Represents an object with sound properties.
    /// </summary>
    public abstract class SoundSource : IDisposable
    {
        private int        _source;
        private SoundGroup _group;

        /// <summary>
        /// Gets the OpenAL native handle of current <see cref="SoundSource"/> object.
        /// </summary>
        public int Handle
        {
            get { return _source; }
            internal set { _source = value; }
        }

        /// <summary>
        /// Gets the <see cref="SoundGroup"/> of curret <see cref="SoundSource"/> object.
        /// </summary>
        public SoundGroup Group
        {
            get { return _group; }
            internal set { _group = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the current <see cref="SoundSource"/> object is in loop mode.
        /// </summary>
        public abstract bool IsLooping
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the current playing position of the current <see cref="Sound"/> object.
        /// </summary>
        public abstract TimeSpan PlayingOffset
        {
            get;
            set;
        }

        /// <summary>
        /// Gets total duration of current <see cref="SoundSource"/> object.
        /// </summary>
        public abstract TimeSpan Duration
        {
            get;
        }

        /// <summary>
        /// Gets the current status of current <see cref="SoundSource"/> object.
        /// </summary>
        public virtual SoundStatus Status
        {
            get
            {
                if (Handle <= 0)
                {
                    Logger.Warning("SoundSource Handle is not created.\n" +
                        "Play the sound before modifying source properties.");

                    return SoundStatus.Stopped;
                }

                ALSourceState state = 0;
                ALChecker.Check(() => state = AL.GetSourceState(_source));

                switch (state)
                {
                    case ALSourceState.Initial:
                    case ALSourceState.Stopped: return SoundStatus.Stopped;
                    case ALSourceState.Paused:  return SoundStatus.Paused;
                    case ALSourceState.Playing: return SoundStatus.Playing;
                }

                return SoundStatus.Stopped;
            }
        }

        /// <summary>
        /// Gets or sets the pitch of current <see cref="SoundSource"/> object.
        /// </summary>
        public float Pitch
        {
            get
            {
                if (Handle <= 0)
                {
                    Logger.Warning("SoundSource Handle is not created.\n" +
                        "Play the sound before modifying source properties.");

                    return 0f;
                } 

                float pitch = 0;
                ALChecker.Check(() => AL.GetSource(_source, ALSourcef.Pitch, out pitch));

                return pitch;
            }
            set
            {

                if (Handle <= 0)
                {
                    Logger.Warning("SoundSource Handle is not created.\n" +
                        "Play the sound before modifying source properties.");

                    return;
                }

                ALChecker.Check(() => AL.Source(_source, ALSourcef.Pitch, value));
            }
        }

        /// <summary>
        /// Gets or sets the volume of current <see cref="SoundSource"/> object.
        /// </summary>
        public float Volume
        {
            get
            {
                if (Handle <= 0)
                {
                    Logger.Warning("SoundSource Handle is not created.\n" +
                        "Play the sound before modifying source properties.");

                    return 0f;
                }

                float gain = 0;
                ALChecker.Check(() => AL.GetSource(_source, ALSourcef.Gain, out gain));

                return gain * 100f;
            }
            set
            {
                if (Handle <= 0)
                {
                    Logger.Warning("SoundSource Handle is not created.\n" +
                        "Play the sound before modifying source properties.");

                    return;
                }

                ALChecker.Check(() => AL.Source(_source, ALSourcef.Gain, value * 0.01f));
            }
        }

        /// <summary>
        /// Gets or sets the 3D position of current <see cref="SoundSource"/> object in audio scene.
        /// </summary>
        public Vector3 Position
        {
            get
            {
                if (Handle <= 0)
                {
                    Logger.Warning("SoundSource Handle is not created.\n" +
                        "Play the sound before modifying source properties.");

                    return Vector3.Zero;
                }

                float[] positions = new float[3];
                ALChecker.Check(() => AL.GetSource(_source, ALSource3f.Position, out positions[0], out positions[1], out positions[1]));

                return new Vector3(positions[0], positions[1], positions[2]);
            }
            set
            {
                if (Handle <= 0)
                {
                    Logger.Warning("SoundSource Handle is not created.\n" +
                        "Play the sound before modifying source properties.");

                    return;
                }

                ALChecker.Check(() => AL.Source(_source, ALSource3f.Position, value.X, value.Y, value.Z));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the current <see cref="SoundSource"/> object position should relative to the listener.
        /// </summary>
        public bool IsRelativeListener
        {
            get
            {
                if (Handle <= 0)
                {
                    Logger.Warning("SoundSource Handle is not created.\n" +
                        "Play the sound before modifying source properties.");

                    return false;
                }

                bool relative = false;
                ALChecker.Check(() => AL.GetSource(_source, ALSourceb.SourceRelative, out relative));

                return relative;
            }
            set
            {
                if (Handle <= 0)
                {
                    Logger.Warning("SoundSource Handle is not created.\n" +
                        "Play the sound before modifying source properties.");

                    return;
                }

                ALChecker.Check(() => AL.Source(_source, ALSourceb.SourceRelative, value));
            }
        }

        /// <summary>
        /// Gets or sets the minimum distance of current <see cref="SoundSource"/> object.
        /// </summary>
        public float MinDistance
        {
            get
            {
                if (Handle <= 0)
                {
                    Logger.Warning("SoundSource Handle is not created.\n" +
                        "Play the sound before modifying source properties.");

                    return 0f;
                }

                float distance = 0;
                ALChecker.Check(() => AL.GetSource(_source, ALSourcef.ReferenceDistance, out distance));

                return distance;
            }
            set
            {
                if (Handle <= 0)
                {
                    Logger.Warning("SoundSource Handle is not created.\n" +
                        "Play the sound before modifying source properties.");

                    return;
                }

                ALChecker.Check(() => AL.Source(_source, ALSourcef.ReferenceDistance, value));
            }
        }

        /// <summary>
        /// Gets or sets the attenuation factor of current <see cref="SoundSource"/> object.
        /// </summary>
        public float Attenuation
        {
            get
            {
                if (Handle <= 0)
                {
                    Logger.Warning("SoundSource Handle is not created.\n" +
                        "Play the sound before modifying source properties.");

                    return 0f;
                }

                float attenuation = 0f;
                ALChecker.Check(() => AL.GetSource(_source, ALSourcef.RolloffFactor, out attenuation));

                return attenuation;
            }
            set
            {
                if (Handle <= 0)
                {
                    Logger.Warning("SoundSource Handle is not created.\n" +
                        "Play the sound before modifying source properties.");

                    return;
                }

                ALChecker.Check(() => AL.Source(_source, ALSourcef.RolloffFactor, value));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SoundSource"/> class.
        /// </summary>
        public SoundSource()
        {
        }


        /// <summary>
        /// Start or resume playing the current <see cref="SoundSource"/> object.
        /// </summary>
        protected abstract void Play();

        /// <summary>
        /// Pause the current <see cref="SoundSource"/> object.
        /// </summary>
        protected abstract void Pause();

        /// <summary>
        /// Stop playing the current <see cref="SoundSource"/> object.
        /// </summary>
        protected abstract void Stop();

        /// <summary>
        /// Releases all resources used by the <see cref="SoundSource"/>.
        /// </summary>
        public virtual void Dispose()
        {
            ALChecker.Check(() => AL.Source(_source, ALSourcei.Buffer, 0));
            ALChecker.Check(() => AL.DeleteSource(_source));
        }
    }
}
