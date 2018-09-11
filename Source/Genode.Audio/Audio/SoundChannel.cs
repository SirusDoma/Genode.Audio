using System;
using System.Numerics;

using OpenTK.Audio.OpenAL;

using Genode;
using Genode.Internal.OpenAL;

namespace Genode.Audio
{
    /// <summary>
    /// Represents base class for all audio objects that has audio playback and properties such as 
    /// <see cref="Pitch"/>, <see cref="Volume"/>, <see cref="Position"/>, <see cref="Attenuation"/>, etc.
    /// </summary>
    /// <inheritdoc/>
    public abstract class SoundChannel : DisposableResource
    {
        private float   pitch           = 1f;
        private float   gain            = 100f;
        private Vector3 position        = Vector3.Zero;
        private bool    sourceRelative  = false;
        private float   minimumDistance = 1f;
        private float   attenuation     = 1f;
        private bool    isLooping       = false;

        /// <summary>
        /// Gets the <see cref="Sound"/> buffer of the current instance of the <see cref="SoundChannel"/> object.
        /// </summary>
        protected internal virtual Sound Buffer { get; protected set; }

        /// <summary>
        /// Gets the OpenAL source identifier.
        /// </summary>
        public int Handle { get; protected internal set; }

        /// <summary>
        /// Gets a value indicating whether the OpenAL source identifier is invalid
        /// </summary>
        public bool IsInvalid => Handle <= 0 || !ALChecker.Check(() => AL.IsSource(Handle));

        /// <summary>
        /// Gets total number of samples of the sound.
        /// </summary>
        public long SampleCount { get; protected internal set; }

        /// <summary>
        /// Gets the number of channels of the sound.
        /// </summary>
        public int ChannelCount { get; protected internal set; }

        /// <summary>
        /// Gets the samples rate of the sound, in samples per second.
        /// </summary>
        public int SampleRate { get; protected internal set; }

        /// <summary>
        /// Gets the duration of the sound.
        /// </summary>
        public virtual TimeSpan Duration
        {
            get 
            {
                if (SampleRate == 0 || ChannelCount == 0)
                {
                    return TimeSpan.Zero;
                }

                return TimeSpan.FromSeconds((float)SampleCount / SampleRate / ChannelCount);
            }
        }

        /// <summary>
        /// Gets the current status of the sound.
        /// </summary>
        public virtual SoundStatus Status
        {
            get
            {
                var status = ALChecker.Check(() => AL.GetSourceState(Handle));
                switch (status)
                {
                    case ALSourceState.Paused  : return SoundStatus.Paused;
                    case ALSourceState.Playing : return SoundStatus.Playing;
                    case ALSourceState.Initial :
                    case ALSourceState.Stopped :
                    default: return SoundStatus.Stopped;
                }
            }
        }

        /// <summary>
        /// Gets or sets the volume of the <see cref="SoundChannel"/>.
        /// <para>
        /// The pitch represents the perceived fundamental frequency of a sound;
        /// thus you can make a sound more acute or grave by changing its pitch.
        /// A side effect of changing the pitch is to modify the playing speed of the sound as well.
        /// The default value for the pitch is 1.
        /// </para>
        /// </summary>
        public virtual float Pitch
        {
            get => pitch;
            set => ALChecker.Check(() => AL.Source(Handle, ALSourcef.Pitch, pitch = value));
        }

        /// <summary>
        /// Gets or sets the volume of the <see cref="SoundChannel"/>.
        /// <para>
        /// The volume is a value between 0 (mute) and 100 (full volume).
        /// The default value for the volume is 100.
        /// </para>
        /// </summary>
        public virtual float Volume
        {
            get => gain;
            set => ALChecker.Check(() => AL.Source(Handle, ALSourcef.Gain, (gain = value) * 0.01f));
        }

        /// <summary>
        /// Gets or sets the 3D Position of the <see cref="SoundChannel"/> in the audio scene.
        /// <para>
        /// Only sounds with one channel (mono sounds) can be spatialized.
        /// The default position of a sound is (0, 0, 0).
        /// </para>
        /// </summary>
        public virtual Vector3 Position
        {
            get => position;
            set
            {
                position = value;
                ALChecker.Check(() => AL.Source(Handle, ALSource3f.Position, value.X, value.Y, value.Z));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="SoundChannel"/> position is relative to the listener or is absolute.
        /// <para>
        /// Making a sound relative to the listener will ensure that it will always be played the same way regardless of the position of the listener.
        /// This can be useful for non-spatialized sounds, sounds that are produced by the listener, or sounds attached to it.
        /// The default value is false (position is absolute).
        /// </para>
        /// </summary>
        public virtual bool RelativeToListener
        {
            get => sourceRelative;
            set => ALChecker.Check(() => AL.Source(Handle, ALSourceb.SourceRelative, sourceRelative = value));
        }

        /// <summary>
        /// Gets or sets the minimum distance of the <see cref="SoundChannel"/>.
        /// <para>
        /// The "minimum distance" of a sound is the maximum distance at which it is heard at its maximum volume.
        /// Further than the minimum distance, it will start to fade out according to its attenuation factor. 
        /// A value of 0 ("inside the head of the listener") is an invalid value and is forbidden.
        /// The default value of the minimum distance is 1.
        /// </para>
        /// </summary>
        public virtual float MinimumDistance
        {
            get => minimumDistance;
            set => ALChecker.Check(() => AL.Source(Handle, ALSourcef.ReferenceDistance, minimumDistance = value));
        }

        /// <summary>
        /// Gets the attenuation factor of the <see cref="SoundChannel"/>.
        /// <para>
        /// The attenuation is a multiplicative factor which makes the sound more or less loud according to its distance from the listener. 
        /// An attenuation of 0 will produce a non-attenuated sound, i.e. its volume will always be the same whether it is heard from near or from far. 
        /// On the other hand, an attenuation value such as 100 will make the sound fade out very quickly as it gets further from the listener.
        /// The default value of the attenuation is 1.
        /// </para>
        /// </summary>
        public virtual float Attenuation
        {
            get => attenuation;
            set => ALChecker.Check(() => AL.Source(Handle, ALSourcef.RolloffFactor, attenuation = value));
        }

        /// <summary>
        /// Gets or sets a value indicating whether the sound is in loop mode.
        /// </summary>
        public virtual bool IsLooping
        {
            get => isLooping;
            set => ALChecker.Check(() => AL.Source(Handle, ALSourceb.Looping, isLooping = value));
        }

        /// <summary>
        /// Gets or sets the current playing position of the sound.
        /// <para>
        /// The playing position can be changed when the sound is either paused or playing. 
        /// Changing the playing position when the sound is stopped has no effect, since playing the sound will reset its position.
        /// </para>
        /// </summary>
        public virtual TimeSpan PlayingOffset
        {
            get => ALChecker.Check(() => { AL.GetSource(Handle, ALSourcef.SecOffset, out float sec); return TimeSpan.FromSeconds(sec); });
            set => ALChecker.Check(() => AL.Source(Handle, ALSourcef.SecOffset, (float)value.TotalSeconds));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SoundChannel"/> class.
        /// </summary>
        protected SoundChannel(int handle, Sound buffer)
        {
            Handle = handle;
            Buffer = buffer;
        }

        /// <summary>
        /// Start or resume playing the <see cref="SoundChannel"/>.
        /// <para>
        /// This function starts the source if it was stopped, resumes it if it was paused, and restarts it from the beginning if it was already playing.
        /// </para>
        /// </summary>
        protected internal abstract void Play();

        /// <summary>
        /// Puase playing the <see cref="SoundChannel"/>.
        /// <para>
        /// This function pauses the source if it was playing, otherwise (source already paused or stopped) it has no effect.
        /// </para>
        /// </summary>
        protected internal abstract void Pause();

        /// <summary>
        /// Stop playing the <see cref="SoundChannel"/>.
        /// <para>
        /// This function stops the source if it was playing or paused, and does nothing if it was already stopped.
        /// It also resets the playing position (unlike <see cref="Pause"/>).
        /// </para>
        /// </summary>
        protected internal abstract void Stop();

        /// <summary>
        /// Reset the audio channel properties.
        /// </summary>
        protected internal virtual void Reset()
        {
            Stop();

            Pitch              = 1f;
            Volume             = 100f;
            Position           = Vector3.Zero;
            Attenuation        = 1f;
            MinimumDistance    = 1f;
            RelativeToListener = false;
            IsLooping          = false;
        }

        /// <summary>
        /// Reload the audio channel properties.
        /// </summary>
        protected internal virtual void Reload()
        {
            Pitch              = pitch;
            Volume             = gain;
            Position           = position;
            Attenuation        = attenuation;
            MinimumDistance    = minimumDistance;
            RelativeToListener = sourceRelative;
            IsLooping          = isLooping;
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="SoundChannel"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                base.Dispose(disposing);
                if (ALChecker.Check(() => AL.IsSource(Handle)))
                {
                    ALChecker.Check(() => AL.Source(Handle, ALSourcei.Buffer, 0));
                    ALChecker.Check(() => AL.DeleteSource(Handle));
                }
            }
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with current object.</param>
        /// <returns><c>true</c> if the specified object is qual to the current object; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            return obj is SoundChannel channel && channel?.Handle == Handle;
        }

        /// <summary>
        /// Serves as default hash function.
        /// </summary>
        /// <returns>Hash of the current object.</returns>
        public override int GetHashCode()
        {
            return Handle;
        }
    }
}
