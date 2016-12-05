using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

using OpenTK.Audio;
using OpenTK.Audio.OpenAL;

using Cgen;
using Cgen.Internal.OpenAL;
using System.Threading;

namespace Cgen.Audio
{
    /// <summary>
    /// Represents a System that manages sound operation.
    /// </summary>
    public class SoundSystem : IDisposable
    {
        private const int MAX_SOURCE_COUNT = 255;
        private static readonly SoundSystem _instance = new SoundSystem();
        
        /// <summary>
        /// Gets the singleton instance of <see cref="SoundSystem"/>.
        /// </summary>
        public static SoundSystem Instance
        {
            get
            {
                return _instance;
            }
        }

        private List<SoundSource> _sources = new List<SoundSource>();
        private MethodInfo _play, _pause, _stop;
        private Thread _thread;
        private bool _isAuto = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="SoundSystem"/> class
        /// with default OpenAL Context.
        /// </summary>
        protected SoundSystem()
        {
            try
            {
                // Initialize OpenAL Audio
                AudioDevice.Initialize();

                _sources = new List<SoundSource>();
                _play = typeof(SoundSource).GetMethod("Play", BindingFlags.NonPublic | BindingFlags.Instance);
                _pause = typeof(SoundSource).GetMethod("Pause", BindingFlags.NonPublic | BindingFlags.Instance);
                _stop = typeof(SoundSource).GetMethod("Stop", BindingFlags.NonPublic | BindingFlags.Instance);
            }
            catch (Exception ex)
            {
                throw ex.InnerException != null ? ex.InnerException : ex;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SoundSystem"/>
        /// with specified custom OpenAL Context.
        /// </summary>
        /// <param name="context">Custom OpenAL Context to use.</param>
        protected SoundSystem(IntPtr context)
        {
            _sources = new List<SoundSource>();
            _play = typeof(SoundSource).GetMethod("Play", BindingFlags.NonPublic | BindingFlags.Instance);
            _pause = typeof(SoundSource).GetMethod("Pause", BindingFlags.NonPublic | BindingFlags.Instance);
            _stop = typeof(SoundSource).GetMethod("Stop", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        /// <summary>
        /// Initialize the <see cref="SoundSystem"/> using specified device name.
        /// </summary>
        /// <param name="device">The audio device name.</param>
        public void Initialize(string device)
        {
            AudioDevice.Initialize(new AudioContext(device));
        }

        /// <summary>
        /// Initialize the <see cref="SoundSystem"/> explicitly.
        /// </summary>
        public void Initialize()
        {
            // Do nothing, this will trigger the constructor which call AudioDevice.Initialize()
        }

        /// <summary>
        /// Start or resume playing the <see cref="SoundSource"/> object.
        /// </summary>
        /// <param name="source">The <see cref="SoundSource"/> to play or resume.</param>
        public virtual void Play(SoundSource source)
        {
            // Audio Device no longer active?
            if (AudioDevice.IsDisposed)
            {
                throw new ObjectDisposedException("AudioDevice");
            }

            // Nothing to play?
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            // Check whether the number of playing sounds is exceed the limit
            if (_sources.Count >= MAX_SOURCE_COUNT)
            {
                // Force to recycle unused source
                Update();

                // Check again if it exceed the limit
                if (_sources.Count >= MAX_SOURCE_COUNT)
                {
                    // It still exceed and throw the exception
                    throw new InvalidOperationException("Failed to play the source:\n" +
                        "The number of playing sources is exceed the limit.");
                }
            }

            // Create the source handle, in case it is first call
            if (source.Handle <= 0)
            {
                int handle = 0;
                ALChecker.Check(() => handle = AL.GenSource());
                ALChecker.Check(() => AL.Source(handle, ALSourcei.Buffer, 0));

                source.Handle = handle;
            }

            // Play the sound
            try
            {
                _play.Invoke(source, null);

                // Add to the list
                _sources.Add(source);
            }
            catch (Exception ex)
            {
                throw ex.InnerException;
            }
        }

        /// <summary>
        /// Start or resume playing the <see cref="SoundGroup"/> object.
        /// </summary>
        /// <param name="group">The <see cref="SoundGroup"/> to play or resume.</param>
        public virtual void Play(SoundGroup group)
        {
            foreach (var source in group.GetSources())
            {
                Play(source);
            }
        }

        /// <summary>
        /// Pause the <see cref="SoundSource"/> object.
        /// </summary>
        /// <param name="source">The <see cref="SoundSource"/> to pause.</param>
        public virtual void Pause(SoundSource source)
        {
            // Audio Device no longer active?
            if (AudioDevice.IsDisposed)
            {
                throw new ObjectDisposedException("AudioDevice");
            }

            // Nothing to pause
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            // Ignore if sources is not listed or handle is not yet created
            if (!_sources.Contains(source) || source.Handle <= 0)
            {
                return;
            }

            // Pause the sound
            try
            {
                _pause.Invoke(source, null);
            }
            catch (Exception ex)
            {
                throw ex.InnerException;
            }
        }

        /// <summary>
        /// Pause the <see cref="SoundGroup"/> object.
        /// </summary>
        /// <param name="group">The <see cref="SoundGroup"/> to pause.</param>
        public void Pause(SoundGroup group)
        {
            foreach (var source in group.GetSources())
            {
                Pause(source);
            }
        }

        /// <summary>
        /// Stop playing the <see cref="SoundSource"/> object.
        /// </summary>
        /// <param name="source">The <see cref="SoundSource"/> to stop.</param>
        public virtual void Stop(SoundSource source)
        {
            // Nothing to pause
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            // Ignore if sources is not listed or handle is not yet created
            if (!_sources.Contains(source) || source.Handle <= 0)
            {
                return;
            }

            // Stop the sound
            try
            {
                _stop.Invoke(source, null);
            }
            catch (Exception ex)
            {
                throw ex.InnerException;
            }

            // Force to remove the source
            Update();
        }

        /// <summary>
        /// Stop playing the <see cref="SoundGroup"/> object.
        /// </summary>
        /// <param name="group">The <see cref="SoundGroup"/> to pause.</param>
        public void Stop(SoundGroup group)
        {
            foreach (var source in group.GetSources())
            {
                Stop(source);
            }
        }

        /// <summary>
        /// Start Audio Engine with Automatic Update Cycle.
        /// </summary>
        public void AutoUpdate()
        {
            if (_isAuto || _thread != null)
            {
                return;
            }

            _isAuto = true;
            _thread = new Thread(() =>
            {
                while(_isAuto)
                {
                    Update();
                    Thread.Sleep(10);
                }
            });

            _thread.Start();
        }

        /// <summary>
        /// Update the <see cref="SoundSystem"/>.
        /// </summary>
        public void Update()
        {
            // Audio Device no longer active?
            if (AudioDevice.IsDisposed)
            {
                return;
            }

            // Remove and dispose unused sources
            for (int i = _sources.Count -1 ; i >= 0; i--)
            {
                if (_sources[i] == null)
                {
                    _sources.RemoveAt(i);
                    continue;
                }

                if (_sources[i].Status == SoundStatus.Stopped && !_sources[i].IsLooping)
                {
                    _sources[i].Dispose();
                    _sources.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Stop playing all played <see cref="SoundSource"/> object.
        /// </summary>
        public void StopAll()
        {
            // Audio Device no longer active?
            if (AudioDevice.IsDisposed)
            {
                throw new ObjectDisposedException("AudioDevice");
            }

            foreach (var source in _sources)
            {
                // Ignore the sound if null, it will be collected upon next Update() call
                if (source == null)
                {
                    continue;
                }

                // Stop the sound
                try
                {
                    _stop.Invoke(source, null);
                }
                catch (Exception ex)
                {
                    throw ex.InnerException;
                }
            }
        }

        /// <summary>
        /// Gets all playing <see cref="SoundSource"/>.
        /// </summary>
        /// <returns>An array of <see cref="SoundSource"/>.</returns>
        public SoundSource[] GetPlayingSources()
        {
            return _sources.ToArray();
        }

        /// <summary>
        /// Releases all resources used by the <see cref="SoundSystem"/>.
        /// </summary>
        public virtual void Dispose()
        {
            _isAuto = false;
            _thread?.Join();
            AudioDevice.Free();
        }
    }
}
