using System;
using System.Collections.Generic;
using System.Text;

namespace Cgen.Audio
{
    /// <summary>
    /// Represents a group of <see cref="SoundSource"/>.
    /// </summary>
    public class SoundGroup
    {
        private List<SoundSource> _sources;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="SoundGroup"/>.
        /// </summary>
        public SoundGroup()
        {
            _sources = new List<SoundSource>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SoundGroup"/>
        /// with specified <see cref="SoundSource"/>.
        /// </summary>
        public SoundGroup(params SoundSource[] sources)
            : this()
        {
            Add(sources);
        }

        /// <summary>
        /// Sets the pitch of current <see cref="SoundGroup"/> object.
        /// </summary>
        public void SetPitch(float pitch)
        {
            foreach(var source in _sources)
            {
                source.Pitch = pitch;
            }
        }

        /// <summary>
        /// Sets the volume of current <see cref="SoundGroup"/> object.
        /// </summary>
        public void SetVolume(float volume)
        {
            foreach (var source in _sources)
            {
                source.Volume = volume;
            }
        }

        /// <summary>
        /// Sets the 3D position of current <see cref="SoundGroup"/> object in audio scene.
        /// </summary>
        public void SetPosition(Vector3 position)
        {
            foreach (var source in _sources)
            {
                source.Position = position;
            }
        }

        /// <summary>
        /// Sets a value indicating whether the current <see cref="SoundGroup"/> object position should relative to the listener.
        /// </summary>
        public void SetRelativeListener(bool relative)
        {
            foreach (var source in _sources)
            {
                source.IsRelativeListener = relative;
            }
        }

        /// <summary>
        /// Sets the minimum distance of current <see cref="SoundGroup"/> object.
        /// </summary>
        public void SetMinDistance(float distance)
        {
            foreach (var source in _sources)
            {
                source.MinDistance = distance;
            }
        }

        /// <summary>
        /// Sets the attenuation factor of current <see cref="SoundGroup"/> object.
        /// </summary>
        public void SetAttenuation(float attenuation)
        {
            foreach (var source in _sources)
            {
                source.Attenuation = attenuation;
            }
        }

        /// <summary>
        /// Gets the <see cref="SoundSource"/> inside current <see cref="SoundGroup"/> object
        /// </summary>
        /// <returns>An array of <see cref="SoundSource"/>.</returns>
        public SoundSource[] GetSources()
        {
            return _sources.ToArray();
        }

        /// <summary>
        /// Determines whether the specified <see cref="SoundSource"/> is in current <see cref="SoundGroup"/> object.
        /// </summary>
        /// <param name="source">The <see cref="SoundSource"/> to check.</param>
        /// <returns><code>true</code> if the <see cref="SoundSource"/> exist, otherwise false.</returns>
        public bool Contains(SoundSource source)
        {
            return _sources.Contains(source);
        }

        /// <summary>
        /// Add <see cref="SoundSource"/> into current <see cref="SoundGroup"/> object.
        /// </summary>
        /// <param name="sources">The <see cref="SoundSource"/> to add.</param>
        public void Add(params SoundSource[] sources)
        {
            foreach (var source in sources)
            {
                if (!Contains(source) && source != null)
                {
                    if (source.Group != null)
                        source.Group.Remove(source);

                    source.Group  = this;
                    _sources.Add(source);
                }
            }
        }

        /// <summary>
        /// Remove <see cref="SoundSource"/> from current <see cref="SoundGroup"/> object.
        /// </summary>
        /// <param name="sources">The <see cref="SoundSource"/> to remove.</param>
        public void Remove(params SoundSource[] sources)
        {
            foreach (var source in sources)
            {
                if (Contains(source))
                {
                    source.Group  = null;
                    _sources.Add(source);
                }
            }
        }
    }
}
