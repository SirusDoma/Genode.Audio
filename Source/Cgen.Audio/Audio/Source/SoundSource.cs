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
    /// This class should be used for internal only and / or low-level cases that require OpenAL context.
    /// </summary>
    public abstract class SoundSource : IDisposable
    {
        private readonly object _mutex = new object();
        private int _source;
        private SoundGroup _group;

        private bool _resetting       = false;
        private float _volume         = 100f;
        private Vector3 _position     = new Vector3(0, 0, 0);
        private float _pitch          = 1f;
        private float _attenuation    = 1f;
        private float _minDistance    = 1f;
        private bool _relativeListner = false;

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
                if (Handle <= 0 || !Validate())
                {
                    return SoundStatus.Initial;
                }

                ALSourceState state = ALChecker.Check(() => AL.GetSourceState(Handle));
                switch (state)
                {
                    case ALSourceState.Initial: return SoundStatus.Initial;
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
                return _pitch;
            }
            set
            {

                if (_pitch != value || _resetting)
                {
                    _pitch = value;
                    if (Handle <= 0)
                        return;

                    ALChecker.Check(() => AL.Source(Handle, ALSourcef.Pitch, value));
                }
            }
        }

        /// <summary>
        /// Gets or sets the volume of current <see cref="SoundSource"/> object.
        /// </summary>
        public float Volume
        {
            get
            {
                return _volume;
            }
            set
            {
                if (_volume != value || _resetting)
                {
                    _volume = value;
                    if (Handle <= 0)
                        return;

                    ALChecker.Check(() => AL.Source(Handle, ALSourcef.Gain, value * 0.01f));
                }
            }
        }

        /// <summary>
        /// Gets or sets the 3D position of current <see cref="SoundSource"/> object in audio scene.
        /// </summary>
        public Vector3 Position
        {
            get
            {
                return _position;
            }
            set
            {
                if (_position != value || _resetting)
                {
                    _position = value;
                    if (Handle <= 0)
                        return;

                    ALChecker.Check(() => AL.Source(Handle, ALSource3f.Position, value.X, value.Y, value.Z));
                }
            }
        }

        /// <summary>
        /// Gets or sets the X-coordinate position of current <see cref="SoundSource"/> object in audio scene.
        /// </summary>
        public float Pan
        {
            get
            {
                return Position.X;
            }
            set
            {
                var position = Position;
                Position = new Vector3(value, position.Y, position.Z);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the current <see cref="SoundSource"/> object position should relative to the listener.
        /// </summary>
        public bool IsRelativeListener
        {
            get
            {
                return _relativeListner;
            }
            set
            {
                if (_relativeListner != value || _resetting)
                {
                    _relativeListner = value;
                    if (Handle <= 0)
                        return;

                    ALChecker.Check(() => AL.Source(Handle, ALSourceb.SourceRelative, value));
                }
            }
        }

        /// <summary>
        /// Gets or sets the minimum distance of current <see cref="SoundSource"/> object.
        /// </summary>
        public float MinDistance
        {
            get
            {
                return _minDistance;
            }
            set
            {
                if (_minDistance != value || _resetting)
                {
                    _minDistance = value;
                    if (Handle <= 0)
                        return;

                    ALChecker.Check(() => AL.Source(Handle, ALSourcef.ReferenceDistance, value));
                }
            }
        }

        /// <summary>
        /// Gets or sets the attenuation factor of current <see cref="SoundSource"/> object.
        /// </summary>
        public float Attenuation
        {
            get
            {
                return _attenuation;
            }
            set
            {
                if (_attenuation != value || _resetting)
                {
                    _attenuation = value;
                    if (Handle <= 0)
                        return;


                    ALChecker.Check(() => AL.Source(Handle, ALSourcef.RolloffFactor, value));
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SoundSource"/> class.
        /// </summary>
        public SoundSource()
        {
        }

        /// <summary>
        /// Return a value indicating whether the native <see cref="SoundSource.Handle"/> is in valid state.
        /// </summary>
        public bool Validate()
        {
            return ALChecker.Check(() => AL.IsSource(Handle));
        }

        /// <summary>
        /// Start or resume playing the current <see cref="SoundSource"/> object.
        /// </summary>
        protected internal abstract void Play();

        /// <summary>
        /// Pause the current <see cref="SoundSource"/> object.
        /// </summary>
        protected internal abstract void Pause();

        /// <summary>
        /// Stop playing the current <see cref="SoundSource"/> object.
        /// </summary>
        protected internal abstract void Stop();

        /// <summary>
        /// When inherited, reset the attached sound buffer(s) from <see cref="SoundSource"/> instance.
        /// </summary>
        protected internal abstract void ResetBuffer();

        internal void ResetState()
        {
            _resetting = true;
            { 
                Volume             = Volume;
                Position           = Position;
                Pitch              = Pitch;
                IsLooping          = IsLooping;
                Attenuation        = Attenuation;
                MinDistance        = MinDistance;
                IsRelativeListener = IsRelativeListener;
                PlayingOffset      = TimeSpan.Zero;
            }
            _resetting = false;
        }

        /// <summary>
        /// Releases all resources used by the <see cref="SoundSource"/>.
        /// </summary>
        public virtual void Dispose()
        {
            if (Validate())
            {
                ALChecker.Check(() => AL.Source(Handle, ALSourcei.Buffer, 0));
                ALChecker.Check(() => AL.DeleteSource(Handle));

                SoundSystem.Instance.Enqueue(this, true);
            }
        }
    }
}
