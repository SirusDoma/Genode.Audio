using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Linq;
using OpenTK;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;

using Genode;
using Genode.Internal.OpenAL;

namespace Genode.Audio
{
    /// <summary>
    /// Represents abstract base class for capturing sound data.
    /// </summary>
    /// <inheritdoc/>
    public abstract class SoundRecorder<T> : SoundRecorder
        where T : class
    {
        /// <summary>
        /// Get recorded <see cref="Output"/>.
        /// </summary>
        public T Output { get; protected set; }
    }

    /// <summary>
    /// Represents abstract base class for capturing sound data.
    /// </summary>
    /// <inheritdoc/>
    public abstract class SoundRecorder : DisposableResource
    {
        /// <summary>
        /// Gets or sets the capturing device.
        /// </summary>
        private static AudioCapture CaptureDevice { get; set; }
        
        /// <summary>
        /// Gets the name of the default audio capture device.
        /// </summary>
        public static string DefaultDevice => AudioCapture.DefaultDevice;
        
        /// <summary>
        /// Gets an array of the names of all available audio capture devices.
        /// </summary>
        public static string[] AvailableDevices => AudioCapture.AvailableDevices.ToArray();

        /// <summary>
        /// Gets a value indicating whether the system supports audio capture.
        /// </summary>
        public static bool IsAvailable
        {
            get
            {
                return ALChecker.Check(() => AudioDevice.Instance.IsExtensionSupported("ALC_EXT_CAPTURE")) ||
                       ALChecker.Check(() => AudioDevice.Instance.IsExtensionSupported("ALC_EXT_capture"));
            }
        }

        private Stopwatch stopwatch;
        private short[]   samples;
        private string    device;
        private int       channelCount;

        /// <summary>
        /// Gets a value indicating whether the <see cref="SoundRecorder{T}"/> is capturing.
        /// </summary>
        public bool Capturing { get; private set; }

        /// <summary>
        /// Gets the number of audio samples captured per seconds.
        /// </summary>
        public int SampleRate { get; private set; }

        /// <summary>
        /// Gets or sets the period interval between calls to the <see cref="ProcessSamples"/> function in milliseconds.
        /// You may want to use a small interval if you want to process the
        /// recorded data in real time, for example.
        /// </summary>
        /// <remarks>
        /// This is only a hint, the actual period may vary.
        /// So don't rely on this parameter to implement precise timing.
        /// </remarks>
        public long ProcessingInterval { get; set; }

        /// <summary>
        /// Gets or sets the number of channels used by this recorder.
        /// The value must be between 1 (mono) or 2 (stereo), otherwise Channel Count may not be modified.
        /// </summary>
        public int ChannelCount
        {
            get => channelCount;
            set
            {
                if (Capturing)
                {
                    Logger.Instance.Warning("It's not possible to change the channels while recording");
                    return;
                }

                if (value < 1 || value > 2)
                {
                    Logger.Instance.Warning("Unsupported channel count: {0}.\nCurrently only mono (1) and stereo (2) is supported", value);
                    return;
                }

                channelCount = value;
            }
        }

        /// <summary>
        /// Gets or sets the audio capture device.
        /// </summary>
        public string Device
        {
            get => device;
            set
            {
                // Use default device if device name is not specified
                if (string.IsNullOrEmpty(value))
                {
                    value = DefaultDevice;
                }

                if (Capturing)
                {
                    // Stop Capturing
                    Capturing = false;

                    // Determine the recording format
                    ALFormat format = channelCount == 1 ? ALFormat.Mono16 : ALFormat.Stereo16;

                    // Open the requested capture device for capturing 16 bits samples
                    CaptureDevice = new AudioCapture(value, SampleRate, format, SampleRate); //Alc.CaptureOpenDevice(value, SampleRate, format, SampleRate);
                    if (CaptureDevice == null)
                    {
                        // Notify derived Class
                        Flush();

                        Logger.Instance.Warning("Failed to open the audio capture device with name: {0}", value);
                        return;
                    }

                    // Start Capture
                    CaptureDevice.Start();

                    // Set the flag and value
                    device = value;
                    Capturing  = true;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SoundRecorder{T}"/> class.
        /// </summary>
        public SoundRecorder()
        {
            device  = DefaultDevice;
            samples     = new short[44100 * 2];
            stopwatch   = new Stopwatch();

            SampleRate         = 0;
            ChannelCount       = 1;
            ProcessingInterval = 100;
            Capturing          = false;
        }

        /// <summary>
        /// Process recording request.
        /// </summary>
        private void Record()
        {
            // Process available samples
            if (Capturing)
            {
                // Get the number of sample available
                int bitrate = CaptureDevice.AvailableSamples;

                // Get the sample if there is any sample to read
                if (bitrate > 0)
                {
                    // Resize sample with the sample count if it's not match
                    // This is important otherwise OpenAL may crash
                    int sampleCount = bitrate * ChannelCount;
                    if (sampleCount != samples.Length)
                    {
                        Array.Resize(ref samples, sampleCount);
                    }

                    // Read the sample from the capture stream
                    CaptureChecker.Check(CaptureDevice, () => CaptureDevice.ReadSamples(samples, bitrate));

                    // Forward captured sample into derived class
                    if (!ProcessSamples(samples))
                    {
                        // The derived class wants to stop the capture
                        Capturing = false;
                    }
                }
            }
            else if (CaptureDevice != null)
            {
                // Stop the capture
                CaptureChecker.Check(CaptureDevice, () => CaptureDevice.Stop());

                // Get the samples left in the buffer
                Capturing = true;
                Record();

                // Close the device
                CaptureDevice = null;

                // Switch off the flag
                Capturing = false;
            }
        }

        /// <summary>
        /// Request <see cref="SoundReader"/> to record audio.
        /// </summary>
        /// <param name="sampleRate">The samples rate of sound to use for recording, in samples per second.</param>
        /// <returns></returns>
        internal void Start(int sampleRate = 44100)
        {
            // Check if the device can do audio capture
            if (!IsAvailable)
            {
                throw new Exception("Failed to start capture: your system cannot capture audio data (call SoundRecorder.Available to check it)");
            }

            // Check that another capture is not already running
            if (CaptureDevice != null)
            {
                throw new Exception("Trying to start audio capture, but another capture is already running");
            }

            // Determine the recording format and retrieve sample rate
            ALFormat format = channelCount == 1 ? ALFormat.Mono16 : ALFormat.Stereo16;
            SampleRate = sampleRate;

            // Open the requested capture device for capturing 16 bits samples
            CaptureDevice = new AudioCapture(Device, SampleRate, format, SampleRate);
            if (CaptureDevice == null)
            {
                throw new Exception($"Failed to open the audio capture device with name: {device}");
            }

            // Clear the array of samples
            samples = new short[SampleRate * ChannelCount];

            // Notify the derived class
            Initialize();
            
            // Start the capture
            CaptureChecker.Check(CaptureDevice, () => CaptureDevice.Start());
            Capturing = true;
        }

        /// <summary>
        /// Request <see cref="SoundRecorder{T}"/> to stop recording process.
        /// </summary>
        internal void Stop()
        {
            // Stop the capturing by turn off the flag
            if (Capturing)
            {
                // Force to get remaining samples
                Capturing = false;
                Record();

                // Notify derived class
                Flush();
            }
        }
        
        /// <summary>
        /// Update the recorded data in the buffer.
        /// </summary>
        internal void Update()
        {
            if (ProcessingInterval > 0)
            {
                if (!stopwatch.IsRunning)
                {
                    stopwatch.Start();
                }
                else if (stopwatch.ElapsedMilliseconds > ProcessingInterval)
                {
                    stopwatch.Reset();
                    stopwatch.Start();

                    Record();
                }
            }
            else
            {
                Record();
            }
        }

        /// <summary>
        /// When overriden, initialize recording initiation of <see cref="SoundRecorder{T}"/>.
        /// </summary>
        protected internal abstract void Initialize();
        
        /// <summary>
        /// When overriden, process the given samples as recoded data.
        /// </summary>
        /// <param name="samples">The audio samples to process.</param>
        /// <returns><c>true</c> if there's more data to process; otherwise, <c>false</c>.</returns>
        protected internal abstract bool ProcessSamples(short[] samples);

        /// <summary>
        /// When overriden, flush the recorded samples from the buffer.
        /// This will finalize the recording session.
        /// </summary>
        protected internal abstract void Flush();

        /// <summary>
        /// Releases the unmanaged resources used by the current instance of the <see cref="SoundRecorder{T}"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed && Capturing)
            {
                // Stop the capture
                CaptureChecker.Check(CaptureDevice, () => CaptureDevice.Stop());
                CaptureDevice = null;
                Capturing = false;

                samples = null;
            }
            
            base.Dispose(disposing);
        }
    }
}
