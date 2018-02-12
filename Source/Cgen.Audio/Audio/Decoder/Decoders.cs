using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Cgen.Audio
{
    public static class Decoders
    {
        private static List<Type> _registered;

        static Decoders()
        {
            _registered = new List<Type>();

            // Register Built-in readers
            Register<WavDecoder>();
            Register<OggDecoder>();
        }

        /// <summary>
        /// Check whether the specified <see cref="SoundDecoder"/> is already registered.
        /// </summary>
        /// <typeparam name="T">Type of <see cref="SoundDecoder"/> to check.</typeparam>
        /// <returns><code>true</code> if registered, otherwise false.</returns>
        public static bool IsRegistered<T>()
            where T : SoundDecoder, new()
        {
            return _registered.Contains(typeof(T));
        }

        /// <summary>
        /// Register specified <see cref="SoundDecoder"/>.
        /// </summary>
        /// <typeparam name="T">Type of <see cref="SoundDecoder"/> to register.</typeparam>
        public static void Register<T>()
            where T : SoundDecoder, new()
        {
            if (IsRegistered<T>())
            {
                Logger.Warning("{0} is already registered.", typeof(T).Name);
                return;
            }

            _registered.Add(typeof(T));
        }

        /// <summary>
        /// Unregister specified <see cref="SoundDecoder"/>.
        /// </summary>
        /// <typeparam name="T">Type of <see cref="SoundDecoder"/> to unregister.</typeparam>
        public static void Unregister<T>()
            where T : SoundDecoder, new()
        {
            if (!IsRegistered<T>())
            {
                Logger.Warning("{0} is not registered.", typeof(T).Name);
                return;
            }

            _registered.Remove(typeof(T));
        }

        /// <summary>
        /// Create a registered instance of <see cref="SoundDecoder"/> from specified sound <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream"><see cref="Stream"/> that contains sound.</param>
        /// <returns><see cref="SoundDecoder"/> that can handle the data, otherwise null.</returns>
        public static SoundDecoder CreateDecoder(Stream stream)
        {
            if (!stream.CanRead || !stream.CanSeek)
            {
                throw new ArgumentException("The specified stream must be readable and seekable.");
            }

            foreach (var type in _registered)
            {
                stream.Position = 0;

                var reader = (SoundDecoder)Activator.CreateInstance(type);
                if (reader.Check(stream))
                    return reader;
            }

            return null;
        }
    }
}
