using System;
using System.Numerics;

using OpenTK.Audio;
using OpenTK.Audio.OpenAL;

using Genode;
using Genode.Internal.OpenAL;

namespace Genode.Audio
{
    /// <summary>
    /// Provides High-Level Audio API Device.
    /// </summary>
    internal sealed class AudioDevice : DisposableResource
    {
        /// <summary>
        /// Gets the singleton instance of <see cref="AudioDevice"/>.
        /// </summary>
        public static AudioDevice Instance { get; } = new AudioDevice();

        private AudioContext context;
        private float volume = 100f;
        private Vector3 position = new Vector3(0f, 0f, 0f);
        private Vector3 direction = new Vector3(0f, 0f, -1f);
        private Vector3 upVector = new Vector3(0f, 1f, 0f);

        /// <summary>
        /// Gets or sets the global volume of all the sounds and musics.
        /// <para>
        /// The volume is a number between 0 and 100; it is combined with the individual volume of each sound / music.
        /// The default value for the volume is 100 (maximum).
        /// </para>
        /// </summary>
        public float GlobalVolume
        {
            get => volume;
            set
            {
                volume = value;
                ALChecker.Check(() => AL.Listener(ALListenerf.Gain, volume * 0.01f));
            }
        }

        /// <summary>
        /// Gets or sets the current position of the listener in the scene.
        /// </summary>
        public Vector3 Position
        {
            get => position;
            set
            {
                position = value;
                ALChecker.Check(() => AL.Listener(ALListener3f.Position, position.X, position.Y, position.Z));
            }
        }

        /// <summary>
        /// Gets the current forward vector of the listener in the scene.
        /// <para>
        /// The direction (also called "at vector") is the vector pointing forward from the listener's perspective. 
        /// Together with the up vector, it defines the 3D orientation of the listener in the scene. 
        /// The direction vector doesn't have to be normalized.
        /// The default listener's direction is (0, 0, -1).
        /// </para>
        /// </summary>
        public Vector3 Direction
        {
            get => direction;
            set
            {
                direction = value;
                float[] orientation = {
                    direction.X,
                    direction.Y,
                    direction.Z,
                    upVector.X,
                    upVector.Y,
                    upVector.Z
                };

                ALChecker.Check(() => AL.Listener(ALListenerfv.Orientation, ref orientation));
            }
        }

        /// <summary>
        /// Gets or sets the upward vector of the listener in the scene.
        /// <para>
        /// The up vector is the vector that points upward from the listener's perspective.
        /// Together with the direction, it defines the 3D orientation of the listener in the scene.
        /// The up vector doesn't have to be normalized.
        /// The default listener's up vector is (0, 1, 0). 
        /// It is usually not necessary to change it, especially in 2D scenarios.
        /// </para>
        /// </summary>
        public Vector3 UpVector
        {
            get => upVector;
            set
            {
                upVector = value;
                float[] orientation = {
                    direction.X,
                    direction.Y,
                    direction.Z,
                    upVector.X,
                    upVector.Y,
                    upVector.Z
                };

                ALChecker.Check(() => AL.Listener(ALListenerfv.Orientation, ref orientation));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioDevice"/>.
        /// </summary>
        private AudioDevice()
        {
            try
            {
                context = new AudioContext();
                float[] orientation = {
                    Direction.X,
                    Direction.Y,
                    Direction.Z,
                    UpVector.X,
                    UpVector.Y,
                    UpVector.Z
                };

                ALChecker.Check(() => AL.Listener(ALListenerf.Gain, GlobalVolume * 0.01f));
                ALChecker.Check(() => AL.Listener(ALListener3f.Position, Position.X, Position.Y, Position.Z));
                ALChecker.Check(() => AL.Listener(ALListenerfv.Orientation, ref orientation));
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Failed to open audio device.\n{ex.Message}");
            }
        }

        /// <summary>
        /// Ensures that resources are freed and other cleanup operations are performed when the garbage collector reclaims the <see cref="AudioDevice"/>.
        /// </summary>
        ~AudioDevice()
        {
            Dispose(false);
        }

        /// <summary>
        /// Check if an OpenAL extension is supported.
        /// <para>
        /// This functions automatically finds whether it is an AL or ALC extension, and calls the corresponding function.
        /// </para>
        /// </summary>
        /// <param name="extension">Name of the extension to test.</param>
        /// <returns><c>true</c> if the specified extension is supported; otherwise, <c>false</c></returns>
        public bool IsExtensionSupported(string extension)
        {
            return context.SupportsExtension(extension);
        }

        /// <summary>
        /// Get the OpenAL format that matches the given number of channels.
        /// </summary>
        /// <param name="channelCount">The number of channels.</param>
        /// <returns>Corresponding <see cref="ALFormat"/>.</returns>
        public ALFormat GetFormat(int channelCount)
        {
            switch (channelCount)
            {
                case 1: return ALFormat.Mono16;
                case 2: return ALFormat.Stereo16;
                case 4: return ALFormat.MultiQuad16Ext;
                case 6: return ALFormat.Multi51Chn16Ext;
                case 7: return ALFormat.Multi61Chn16Ext;
                case 8: return ALFormat.Multi71Chn16Ext;
                default: return 0;
            }
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="AudioDevice"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                context.Dispose();
                base.Dispose(disposing);
            }
        }
    }
}
