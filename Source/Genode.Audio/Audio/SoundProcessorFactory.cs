using System;
using System.Collections.Generic;
using System.IO;

namespace Genode.Audio
{
    /// <summary>
    /// Represents <see cref="SoundReader"/> and <see cref="SoundWriter"/> factory.
    /// </summary>
    public static class SoundProcessorFactory
    {
        private static HashSet<Type> processors;
        private static Dictionary<Type, Type> recorders;

        /// <summary>
        /// Initializes a static instance of the <see cref="SoundProcessorFactory"/> class.
        /// </summary>
        static SoundProcessorFactory()
        {
            // Initialize processors
            processors = new HashSet<Type>();
            recorders  = new Dictionary<Type, Type>();

            // Register built-in reader
            RegisterReader<WavReader>();
            RegisterReader<VorbisReader>();
            
            // Register built-in writer
            RegisterWriter<WavWriter>();
            RegisterWriter<VorbisWriter>();
            
            // Register built-in recorder
            RegisterRecorder<Sound, SoundBufferRecorder>();
        }

        /// <summary>
        /// Register a <see cref="SoundReader"/> into factory.
        /// </summary>
        /// <typeparam name="T">Type of <see cref="SoundReader"/> to register.</typeparam>
        /// <returns><c>true</c> if the <see cref="SoundReader"/> registered successfully; otherwise, false.</returns>
        public static bool RegisterReader<T>()
            where T : SoundReader, new()
        {
            return processors.Add(typeof(T));
        }
        
        /// <summary>
        /// Register a <see cref="SoundWriter"/> into factory.
        /// </summary>
        /// <typeparam name="T">Type of <see cref="SoundWriter"/> to register.</typeparam>
        /// <returns><c>true</c> if the <see cref="SoundWriter"/> registered successfully; otherwise, false.</returns>
        public static bool RegisterWriter<T>()
            where T : SoundWriter, new()
        {
            return processors.Add(typeof(T));
        }

        /// <summary>
        /// Register a <see cref="SoundRecorder{T}"/> into factory.
        /// </summary>
        /// <typeparam name="T">Type of output to register.</typeparam>
        /// <typeparam name="V">Type of <see cref="SoundRecorder{T}"/> to associated to the type of output.</typeparam>
        /// <returns><c>true</c> if the <see cref="SoundWriter"/> registered successfully; otherwise, false.</returns>
        public static bool RegisterRecorder<T, V>()
            where T : class
            where V : SoundRecorder<T>, new()
        {
            if (!recorders.ContainsKey(typeof(T)))
            {
                recorders.Add(typeof(T), typeof(V));
                return true;
            }

            return false;
        }
        
        /// <summary>
        /// Remove a registered <see cref="SoundReader"/> from factory.
        /// </summary>
        /// <typeparam name="T">Type of <see cref="SoundReader"/> to remove.</typeparam>
        /// <returns><c>true</c> if the <see cref="SoundReader"/> removed successfully; otherwise, false.</returns>
        public static bool RemoveReader<T>()
            where T : SoundReader, new()
        {
            return processors.Remove(typeof(T));
        }
        
        /// <summary>
        /// Remove a registered <see cref="SoundWriter"/> from factory.
        /// </summary>
        /// <typeparam name="T">Type of <see cref="SoundWriter"/> to remove.</typeparam>
        /// <returns><c>true</c> if the <see cref="SoundWriter"/> removed successfully; otherwise, false.</returns>
        public static bool RemoveWriter<T>()
            where T : SoundReader, new()
        {
            return processors.Remove(typeof(T));
        }
        
        /// <summary>
        /// Remove a registered <see cref="SoundRecorder{T}"/> from factory.
        /// </summary>
        /// <typeparam name="T">Type of <see cref="SoundRecorder{T}"/> to remove.</typeparam>
        /// <returns><c>true</c> if the <see cref="SoundRecorder{T}"/> removed successfully; otherwise, false.</returns>
        public static bool RemoveRecorder<T>()
            where T : SoundReader, new()
        {
            return recorders.Remove(typeof(T));
        }

        /// <summary>
        /// Get an instance of the <see cref="SoundReader"/> that can handle specified audio file.
        /// </summary>
        /// <param name="filename">A relative or absolute path for the file that the current <see cref="SoundReader"/> object will encapsulate.</param>
        /// <returns>An instance of <see cref="SoundReader"/> that can handle specified audio file.</returns>
        public static SoundReader GetReader(string filename)
        {
            return GetReader(File.OpenRead(filename));
        }

        /// <summary>
        /// Get an instance of the <see cref="SoundReader"/> that can handle specified audio stream.
        /// </summary>
        /// <param name="stream">An audio stream to read.</param>
        /// <param name="leaveOpen">Specifies whether the <paramref name="stream"/> should leave open after the current instance of the <see cref="SoundReader"/> disposed.</param>
        /// <returns>An instance of <see cref="SoundReader"/> that can handle specified audio stream.</returns>
        public static SoundReader GetReader(Stream stream, bool leaveOpen = false)
        {
            foreach (var type in processors)
            {
                var reader = Activator.CreateInstance(type) as SoundReader;
                if (reader?.Check(stream) ?? false)
                {
                    reader.BaseStream = stream;
                    reader.LeaveOpen  = leaveOpen;
                    reader.SampleInfo = reader.Initialize(stream, leaveOpen);

                    return reader;
                }
            }

            return null;
        }
        
        /// <summary>
        /// Get an instance of the <see cref="SoundWriter"/>.
        /// </summary>
        /// <param name="stream">An audio stream to write.</param>
        /// <param name="sampleRate">The samples rate of the sound, in samples per second.</param>
        /// <param name="channelCount">The number of channels of the sound.</param>
        /// <typeparam name="T">The desired sound writer to use.</typeparam>
        /// <returns>An instance of <see cref="SoundWriter"/> that can handle specified audio file.</returns>
        public static T GetWriter<T>(Stream stream, int sampleRate, int channelCount)
            where T : SoundWriter, new ()
        {
            if (!processors.Contains(typeof(T)))
            {
                throw new ArgumentException("No such encoder exists in factory");
            }

            var writer = new T();
            writer.BaseStream   = stream;
            writer.SampleRate   = sampleRate;
            writer.ChannelCount = channelCount;
            writer.Initialize(stream, sampleRate, channelCount);

            return writer;
        }
        
        /// <summary>
        /// Get an instance of the <see cref="SoundWriter"/> that can handle specified audio file.
        /// </summary>
        /// <param name="stream">An audio stream to write.</param>
        /// <param name="sampleRate">The samples rate of the sound, in samples per second.</param>
        /// <param name="channelCount">The number of channels of the sound.</param>
        /// <param name="extension">The extension of target sound format.</param>
        /// <returns>An instance of <see cref="SoundWriter"/> that can handle specified audio file.</returns>
        public static SoundWriter GetWriter(Stream stream, int sampleRate, int channelCount, string extension)
        {
            foreach (var type in processors)
            {
                var writer = Activator.CreateInstance(type) as SoundWriter;
                if (writer?.Check(extension) ?? false)
                {
                    writer.BaseStream   = stream;
                    writer.SampleRate   = sampleRate;
                    writer.ChannelCount = channelCount;
                    writer.Initialize(stream, sampleRate, channelCount);

                    return writer;
                }
            }

            return null;
        }
        
        /// <summary>
        /// Get an instance of the <see cref="SoundWriter"/> that can handle specified audio file.
        /// </summary>
        /// <param name="filename">A relative or absolute path for the file that the current <see cref="SoundWriter"/> object will encapsulate.</param>
        /// <param name="sampleRate">The samples rate of the sound, in samples per second.</param>
        /// <param name="channelCount">The number of channels of the sound.</param>
        /// <returns>An instance of <see cref="SoundWriter"/> that can handle specified audio file.</returns>
        public static SoundWriter GetWriter(string filename, int sampleRate, int channelCount)
        {
            string extension = Path.GetExtension(filename);
            if (string.IsNullOrEmpty(extension))
            {
                throw new ArgumentException("Specified filename is invalid", "filename");
            }

            var stream = File.Open(filename, FileMode.Create);
            foreach (var type in processors)
            {
                var writer = Activator.CreateInstance(type) as SoundWriter;
                if (writer?.Check(extension) ?? false)
                {
                    writer.BaseStream   = stream;
                    writer.SampleRate   = sampleRate;
                    writer.ChannelCount = channelCount;
                    writer.Initialize(stream, sampleRate, channelCount);

                    return writer;
                }
            }

            return null;
        }
        
        /// <summary>
        /// Get an instance of the <see cref="SoundReader"/> that can handle specified audio stream.
        /// </summary>
        /// <returns>An instance of <see cref="SoundReader"/> that can handle specified audio stream.</returns>
        public static SoundRecorder<T> GetRecorder<T>()
            where T : class
        {
            foreach (var recorder in recorders)
            {
                if (recorder.Key == typeof(T))
                {
                    return Activator.CreateInstance(recorder.Value) as SoundRecorder<T>;
                }
            }

            return null;
        }
    }
}
