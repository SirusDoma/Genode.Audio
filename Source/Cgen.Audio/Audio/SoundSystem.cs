using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Threading;

using OpenTK.Audio;
using OpenTK.Audio.OpenAL;

using Cgen;
using Cgen.Internal.OpenAL;

namespace Cgen.Audio
{
    /// <summary>
    /// Represents a System that manages sound operation.
    /// </summary>
    public class SoundSystem : IDisposable
    {
        private const int MAX_SOURCE_COUNT = 256;
        private static readonly SoundSystem _instance = new SoundSystem();
        private static readonly object mutex = new object();
        
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
        private List<int> _pool = new List<int>();
        private List<int> _allPools = new List<int>();
        private SoundRecorder _recorder;

        /// <summary>
        /// Initializes a new instance of the <see cref="SoundSystem"/> class
        /// with default OpenAL Context.
        /// </summary>
        protected SoundSystem()
            : this(IntPtr.Zero)
        {
            try
            {
                // Initialize OpenAL Audio
                AudioDevice.Initialize();
            }
            catch (Exception ex)
            {
                throw ex.InnerException ?? ex;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SoundSystem"/>
        /// with specified custom OpenAL Context.
        /// </summary>
        /// <param name="context">Custom OpenAL Context to use.</param>
        protected SoundSystem(IntPtr context)
        {
            _sources = new List<SoundSource>(MAX_SOURCE_COUNT);
            _pool    = new List<int>(MAX_SOURCE_COUNT);
        }

        /// <summary>
        /// Gets all available playback device name.
        /// </summary>
        /// <returns>An array of string contains playback device name.</returns>
        public string[] GetAvailableDevices()
        {
            return new List<string>(AudioContext.AvailableDevices).ToArray();
        }

        /// <summary>
        /// Initialize the <see cref="SoundSystem"/> using specified device name.
        /// </summary>
        /// <param name="device">The audio device name.</param>
        public void Initialize(string device)
        {
            if (!AudioContext.AvailableDevices.Contains(device))
            {
                throw new ArgumentException("Invalid device name");
            }

            AudioDevice.Initialize(new AudioContext(device));
        }

        /// <summary>
        /// Initialize the <see cref="SoundSystem"/> explicitly.
        /// </summary>
        public void Initialize()
        {
            // Do nothing, this will trigger the constructor which call AudioDevice.Initialize()
        }

        internal int[] GetPooledSources()
        {
            return _allPools.ToArray();
        }

        internal int GetSource()
        {
            int count = _pool.Count;
            if (count > 0)
            {
                // Get sound from the end of available pools
                int source = _pool[count - 1];
                _pool.RemoveAt(count - 1);

                return source;
            }

            return ALChecker.Check(() => AL.GenSource());
        }

        internal void Queue(SoundSource source)
        {
            // Add to available pool queue and remove it from playing sources
            _pool.Add(source.Handle);
            _sources.Remove(source);

            // In case it is not registered in all pools, register it
            if (!_allPools.Contains(source.Handle))
                _allPools.Add(source.Handle);

            // In case it is still playing, stop it
            Stop(source);
        }

        internal void Enqueue(SoundSource source, bool hardEnqueue = false)
        {
            // Add to playing sources, or if it is a hard enqueue, remove it from everywhere
            if (hardEnqueue)
            {
                _allPools.Remove(source.Handle);
                _pool.Remove(source.Handle);
                _sources.Remove(source);
            }
            else
                _sources.Add(source);
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

            // If the source is not paused, most likely it's stopped and it need to retrieve new handle
            // Playing sources also have currently valid handle in use, don't override them.
            if (source.Status != SoundStatus.Playing && source.Status != SoundStatus.Paused)
            {
                // Always get (or create) handle from the pool each time source need to play.
                // Unless it is paused.
                source.Handle = GetSource();

                // Additionally, we need reset source state in case it is retrieved from pool
                // The handle could be used by different sets of states
                // Thus resetting state should fix overlapping states problem
                source.ResetState();
            }

            try
            {
                // Play the sound
                source.Play();

                // Add to the list
                Enqueue(source);
            }
            catch (Exception ex)
            {
                throw ex.InnerException ?? ex;
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

            // Check whether the specified source has valid handle before pausing
            if (_sources.Contains(source) && source.Validate())
            {
                try
                {
                    // Pause the source
                    source.Pause();
                }
                catch (Exception ex)
                {
                    throw ex.InnerException ?? ex;
                }
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
            // Nothing to stop
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            // Check whether the specified source has valid handle before stopping
            if (_sources.Contains(source) && source.Validate())
            {
                try
                {
                    // Stop the source
                    // Note that we do not enqueue source here
                    // It is Update() responsibility to take source into the pool
                    source.Stop();
                }
                catch (Exception ex)
                {
                    throw ex.InnerException ?? ex;
                }
            }
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
        /// Pause all playing <see cref="SoundSource"/> objects.
        /// </summary>
        public void Pause()
        {
            for (int i = _sources.Count - 1; i >= 0; i--)
            {
                if (_sources[i] != null)
                    Pause(_sources[i]);
            }
        }

        /// <summary>
        /// Resume all paused <see cref="SoundSource"/> objects.
        /// </summary>
        public void Resume()
        {
            for (int i = _sources.Count - 1; i >= 0; i--)
            {
                if (_sources[i] != null && _sources[i].Status == SoundStatus.Paused)
                    Play(_sources[i]);
            }
        }

        /// <summary>
        /// Stop all playing <see cref="SoundSource"/> objects.
        /// </summary>
        public void Stop()
        {
            for (int i = _sources.Count - 1; i >= 0; i--)
            {
                if (_sources[i] != null)
                    Stop(_sources[i]);
            }
        }

        /// <summary>
        /// Stop recording audio.
        /// </summary>
        /// <param name="recorder"><see cref="SoundRecorder"/> to stop recording.</param>
        public void StopCapture()
        {
            _recorder?.Stop();
        }

        /// <summary>
        /// Start recording with given <see cref="SoundRecorder"/> instance.
        /// </summary>
        /// <param name="recorder"><see cref="SoundRecorder"/> instance to record the audio</param>
        public void StartCapture(SoundRecorder recorder)
        {
            if (_recorder == null)
            {
                _recorder = recorder;
                _recorder.Start();
            }
            else
                throw new Exception("Cannot start audio capture while another capture is running.");
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

            // Update Recorder if any
            if (_recorder != null)
            {
                _recorder.Update();
                if (!_recorder.Capturing)
                    _recorder = null;
            }

            // Remove and dispose unused sources
            for (int i = _sources.Count - 1; i >= 0; i--)
            {
                if (_sources[i] == null)
                {
                    _sources.RemoveAt(i);
                    continue;
                }
                else if (_sources[i] is SoundStream)
                {
                    var stream = _sources[i] as SoundStream;
                    stream?.Update();
                }

                // Check whether the specified source can be restored into source pool
                if (_sources[i].Status == SoundStatus.Stopped)
                {
                    // Reset the buffer to freed memory
                    _sources[i].ResetBuffer();

                    // Queue the source from the pool and remove from the playing list
                    Queue(_sources[i]);
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
            _sources.Clear();
            var pool = _allPools.ToArray();
            foreach (int source in pool)
            {
                var sound = new Sound(source);

                Stop(sound);
                sound.Dispose();
            }

            AudioDevice.Dispose();
        }
    }
}
