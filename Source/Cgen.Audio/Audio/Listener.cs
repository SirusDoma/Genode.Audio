using System;
using System.Collections.Generic;
using System.Text;

namespace Cgen.Audio
{
    /// <summary>
    /// Represents an audio listener is the point in the scene from where all the sounds are heard.
    /// </summary>
    public static class Listener
    {
        /// <summary>
        /// Gets or setst the global volume of all the <see cref="SoundSource"/> objects.
        /// </summary>
        public static float Volume
        {
            get
            {
                return AudioDevice.Volume;
            }
            set
            {
                AudioDevice.Volume = value;
            }
        }

        /// <summary>
        /// Gets or sets the position of the listener in the scene.
        /// </summary>
        public static Vector3 Position
        {
            get
            {
                return AudioDevice.Position;
            }
            set
            {
                AudioDevice.Position = value;
            }
        }

        /// <summary>
        /// Gets or sets the forward vector of the listener in the scene.
        /// </summary>
        public static Vector3 Direction
        {
            get
            {
                return AudioDevice.Direction;
            }
            set
            {
                AudioDevice.Direction = value;
            }
        }

        /// <summary>
        /// Gets or sets the upward vector of the listener in the scene.
        /// </summary>
        public static Vector3 UpVector
        {
            get
            {
                return AudioDevice.UpVector;
            }
            set
            {
                AudioDevice.UpVector = value;
            }
        }
    }
}
