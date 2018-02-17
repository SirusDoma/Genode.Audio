using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Cgen.Audio
{
    public static class SoundProcessorFactory
    {
        private static Dictionary<Type, Func<Stream, bool, SoundDecoder>> _decoders;
        private static Dictionary<Type, Func<Stream, int, int, bool, SoundEncoder>> _encoders;

        static SoundProcessorFactory()
        {
            // Register Built-in readers
            _decoders = new Dictionary<Type, Func<Stream, bool, SoundDecoder>>();
            InstallDecoder<WavDecoder>((stream, own) => new WavDecoder(stream, own));
            InstallDecoder<OggDecoder>((stream, own) => new OggDecoder(stream, own));

            // Register Built-in writers
            _encoders = new Dictionary<Type, Func<Stream, int, int, bool, SoundEncoder>>();
            InstallEncoder<WavEncoder>((stream, sampleRate, channelCount, own) => new WavEncoder(stream, sampleRate, channelCount, own));
            InstallEncoder<OggEncoder>((stream, sampleRate, channelCount, own) => new OggEncoder(stream, sampleRate, channelCount, own));
        }

        /// <summary>
        /// Check whether the specified <see cref="SoundDecoder"/> is already registered.
        /// </summary>
        /// <typeparam name="T">Type of <see cref="SoundDecoder"/> to check.</typeparam>
        /// <returns><code>true</code> if registered, otherwise false.</returns>
        public static bool IsDecoderRegistered<T>()
            where T : SoundDecoder
        {
            return _decoders.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Install specified <see cref="SoundDecoder"/> to processor factory.
        /// </summary>
        /// <typeparam name="T">Type of <see cref="SoundDecoder"/> to register.</typeparam>
        public static void InstallDecoder<T>(Func<Stream, bool, SoundDecoder> callback)
            where T : SoundDecoder
        {
            if (IsDecoderRegistered<T>())
            {
                Logger.Warning("{0} is already registered.", typeof(T).Name);
                return;
            }

            _decoders.Add(typeof(T), callback);
        }

        /// <summary>
        /// Uninstall specified <see cref="SoundDecoder"/> to processor factory.
        /// </summary>
        /// <typeparam name="T">Type of <see cref="SoundDecoder"/> to unregister.</typeparam>
        public static void UninstallDecoder<T>()
            where T : SoundDecoder
        {
            if (!IsDecoderRegistered<T>())
            {
                Logger.Warning("{0} is not registered.", typeof(T).Name);
                return;
            }

            _decoders.Remove(typeof(T));
        }

        /// <summary>
        /// Check whether the specified <see cref="SoundEncoder"/> is already registered.
        /// </summary>
        /// <typeparam name="T">Type of <see cref="SoundEncoder"/> to check.</typeparam>
        /// <returns><code>true</code> if registered, otherwise false.</returns>
        public static bool IsEncoderRegistered<T>()
            where T : SoundEncoder
        {
            return _encoders.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Install specified <see cref="SoundEncoder"/> to processor factory.
        /// </summary>
        /// <typeparam name="T">Type of <see cref="SoundEncoder"/> to register.</typeparam>
        public static void InstallEncoder<T>(Func<Stream, int, int, bool, SoundEncoder> callback)
            where T : SoundEncoder
        {
            if (IsEncoderRegistered<T>())
            {
                Logger.Warning("{0} is already registered.", typeof(T).Name);
                return;
            }

            _encoders.Add(typeof(T), callback);
        }

        /// <summary>
        /// Create a registered instance of <see cref="SoundDecoder"/> from specified sound <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream"><see cref="Stream"/> that contains sound.</param>
        /// <returns><see cref="SoundDecoder"/> that can handle the data, otherwise null.</returns>
        public static SoundDecoder CreateDecoder(Stream stream, bool ownStream = false)
        {
            if (!stream.CanRead || !stream.CanSeek)
            {
                throw new ArgumentException("The specified stream must be readable and seekable.");
            }

            foreach (var decoder in _decoders)
            {
                stream.Seek(0, SeekOrigin.Begin);

                var reader = decoder.Value(stream, ownStream);
                if (!reader.Invalid)
                    return reader;
            }

            return null;
        }

        /// <summary>
        /// Create a registered instance of <see cref="SoundEncoder"/> for specified sound <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream"><see cref="Stream"/> that will be written with sample data.</param>
        /// <returns><see cref="SoundEncoder"/> that can handle the data, otherwise null.</returns>
        public static T CreateEncoder<T>(Stream stream, int sampleRate, int channelCount, bool ownStream = false)
            where T : SoundEncoder
        {
            if (!stream.CanRead || !stream.CanSeek || !stream.CanWrite)
            {
                throw new ArgumentException("The specified stream must be readable, writable and seekable.");
            }

            if (_encoders.ContainsKey(typeof(T)))
            {
                var callback = _encoders[typeof(T)];
                stream.Seek(0, SeekOrigin.Begin);

                var encoder = callback(stream, sampleRate, channelCount, ownStream);
                if (!encoder.Invalid)
                    return (T)encoder;
            }

            return null;
        }
    }
}
