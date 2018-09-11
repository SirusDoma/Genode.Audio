using System;
using System.Numerics;

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class |
        AttributeTargets.Method)]
    public sealed class ExtensionAttribute : Attribute { }
}

namespace Genode
{ 
    public static class VectorExtensions
    {
        /// <summary>
        /// Transforms an array of 2D vectors into a contiguous array of scalars
        /// </summary>
        /// <param name="vectors">The vector to be transformed.</param>
        /// <returns>Array of scalars that transformed from Vector.</returns>
        public static float[] Flatten(this Vector2[] vectors)
        {
            float[] contiguous = new float[vectors.Length * 2];
            for (int i = 0; i < contiguous.Length; ++i)
            {
                contiguous[2 * i] = vectors[i].X;
                contiguous[2 * i + 1] = vectors[i].Y;
            }

            return contiguous;
        }

        /// <summary>
        /// Transforms an array of 3D vectors into a contiguous array of scalars
        /// </summary>
        /// <param name="vectors">The vector to be transformed.</param>
        /// <returns>Array of scalars that transformed from Vector.</returns>
        public static float[] Flatten(this Vector3[] vectors)
        {
            float[] contiguous = new float[vectors.Length * 3];
            for (int i = 0; i < contiguous.Length; ++i)
            {
                contiguous[3 * i]     = vectors[i].X;
                contiguous[3 * i + 1] = vectors[i].Y;
                contiguous[3 * i + 2] = vectors[i].Z;
            }

            return contiguous;
        }

        /// <summary>
        /// Transforms an array of 4D vectors into a contiguous array of scalars
        /// </summary>
        /// <param name="vectors">The vector to be transformed.</param>
        /// <returns>Array of scalars that transformed from Vector.</returns>
        public static float[] Flatten(this Vector4[] vectors)
        {
            float[] contiguous = new float[vectors.Length * 4];
            for (int i = 0; i < contiguous.Length; ++i)
            {
                contiguous[4 * i]     = vectors[i].X;
                contiguous[4 * i + 1] = vectors[i].Y;
                contiguous[4 * i + 2] = vectors[i].Z;
                contiguous[4 * i + 3] = vectors[i].W;
            }

            return contiguous;
        }
    }
}
