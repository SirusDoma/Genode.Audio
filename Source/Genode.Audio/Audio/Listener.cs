using System.Numerics;

namespace Genode.Audio
{
    /// <summary>
    /// Represents audio listener is the point in the scene from where all the sounds are heard.
    /// </summary>
    public static class Listener
    {
        /// <summary>
        /// Gets or sets the global volume of all the sounds and musics.
        /// <para>
        /// The volume is a number between 0 and 100; it is combined with the individual volume of each sound / music.
        /// The default value for the volume is 100 (maximum).
        /// </para>
        /// </summary>
        public static float GlobalVolume
        {
            get => AudioDevice.Instance.GlobalVolume;
            set => AudioDevice.Instance.GlobalVolume = value;
        }

        /// <summary>
        /// Gets or sets the current position of the listener in the scene.
        /// </summary>
        public static Vector3 Position
        {
            get => AudioDevice.Instance.Position;
            set => AudioDevice.Instance.Position = value;
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
        public static Vector3 Direction
        {
            get => AudioDevice.Instance.Direction;
            set => AudioDevice.Instance.Direction = value;
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
        public static Vector3 UpVector
        {
            get => AudioDevice.Instance.UpVector;
            set => AudioDevice.Instance.UpVector = value;
        }
    }
}
