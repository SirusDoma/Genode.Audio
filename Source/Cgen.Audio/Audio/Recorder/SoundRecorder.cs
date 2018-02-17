using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using OpenTK;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;

using Cgen;
using Cgen.Internal.OpenAL;

namespace Cgen.Audio
{
    /// <summary>
    /// Represents abstract base class for capturing sound data.
    /// </summary>
    public abstract class SoundRecorder : IDisposable
    {
        private static AudioCapture CaptureDevice
        {
            get; set;
        }

        /// <summary>
        /// Gets a value indicating whether the system supports audio capture.
        /// </summary>
        public static bool IsAvailable
        {
            get
            {
                if (AudioDevice.Context == null)
                    return false;
                
                return ALChecker.Check(() => AudioDevice.Context.SupportsExtension("ALC_EXT_CAPTURE")) ||
                       ALChecker.Check(() => AudioDevice.Context.SupportsExtension("ALC_EXT_capture"));
            }
        }

        /// <summary>
        /// Gets an array of the names of all available audio capture devices.
        /// </summary>
        public static string [] AvailableDevices
        {
            get
            {
                var devices = new List<string>();
                foreach (var device in AudioCapture.AvailableDevices)
                    devices.Add(device);

                return devices.ToArray();
            }
        }

        /// <summary>
        /// Gets the name of the default audio capture device.
        /// </summary>
        public static string DefaultDevice
        {
            get
            {
                return AudioCapture.DefaultDevice;
            }
        }

        private Stopwatch _stopwatch;
        private short[]   _samples;
        private string    _deviceName;
        private int       _channelCount;

        /// <summary>
        /// Gets a value indicating whether the <see cref="SoundRecorder"/> is capturing.
        /// </summary>
        public bool Capturing
        {
            get; private set;
        }

        /// <summary>
        /// Gets the number of audio samples captured per seconds.
        /// </summary>
        public int SampleRate
        {
            get; private set;
        }

        /// <summary>
        /// Gets or sets the period interval between calls to the <see cref="OnProcessSample(short[])"/> function in milliseconds.
        /// You may want to use a small interval if you want to process the
        /// recorded data in real time, for example.
        /// </summary>
        /// <remarks>
        /// This is only a hint, the actual period may vary.
        /// So don't rely on this parameter to implement precise timing.
        /// </remarks>
        public long ProcessingInterval
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the number of channels used by this recorder.
        /// The value must be between 1 (mono) or 2 (stereo), otherwise Channel Count may not be modified.
        /// </summary>
        public int ChannelCount
        {
            get { return _channelCount; }
            set
            {
                if (Capturing)
                {
                    Logger.Warning("It's not possible to change the channels while recording");
                    return;
                }

                if (value < 1 || value > 2)
                {
                    Logger.Warning("Unsupported channel count: {0}.\nCurrently only mono (1) and stereo (2) is supported", value);
                    return;
                }

                _channelCount = value;
            }
        }

        /// <summary>
        /// Gets or sets the audio capture device.
        /// </summary>
        public string Device
        {
            get { return _deviceName; }
            set
            {
                // Use default device if device name is not specified
                if (string.IsNullOrEmpty(value))
                    value = DefaultDevice;

                if (Capturing)
                {
                    // Stop Capturing
                    Capturing = false;

                    // Determine the recording format
                    ALFormat format = _channelCount == 1 ? ALFormat.Mono16 : ALFormat.Stereo16;

                    // Open the requested capture device for capturing 16 bits samples
                    CaptureDevice = new AudioCapture(value, SampleRate, format, SampleRate); //Alc.CaptureOpenDevice(value, SampleRate, format, SampleRate);
                    if (CaptureDevice == null)
                    {
                        // Notify derived Class
                        OnStop();

                        Logger.Warning("Failed to open the audio capture device with name: {0}", value);
                        return;
                    }

                    // Start Capture
                    CaptureDevice.Start();

                    // Set the flag and value
                    _deviceName = value;
                    Capturing  = true;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="SoundRecorder"/> class.
        /// </summary>
        public SoundRecorder()
        {
            _deviceName  = DefaultDevice;
            _samples     = new short[44100 * 2];
            _stopwatch   = new Stopwatch();

            SampleRate         = 0;
            ChannelCount       = 1;
            ProcessingInterval = 100;
            Capturing          = false;
        }

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
                    if (sampleCount != _samples.Length)
                        Array.Resize(ref _samples, sampleCount);

                    // Read the sample from the capture stream
                    CaptureChecker.Check(CaptureDevice, () => CaptureDevice.ReadSamples(_samples, bitrate));

                    // Forward captured sample into derived class
                    if (!OnProcessSample(_samples))
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

        public void Update()
        {
            if (ProcessingInterval > 0)
            {
                if (!_stopwatch.IsRunning)
                {
                    _stopwatch.Start();
                }
                else if (_stopwatch.ElapsedMilliseconds > ProcessingInterval)
                {
                    _stopwatch.Reset();
                    _stopwatch.Start();

                    Record();
                }
            }
            else
            {
                Record();
            }
        }

        internal bool Start(int sampleRate = 44100)
        {
            // Check if the device can do audio capture
            if (!IsAvailable)
            {
                Logger.Warning("Failed to start capture: your system cannot capture audio data (call SoundRecorder.Available to check it)");
                return false;
            }

            // Check that another capture is not already running
            if (CaptureDevice != null)
            {
                Logger.Warning("Trying to start audio capture, but another capture is already running");
                return false;
            }

            // Determine the recording format
            ALFormat format = _channelCount == 1 ? ALFormat.Mono16 : ALFormat.Stereo16;

            // Retrieve sample rate
            SampleRate = sampleRate;

            // Open the requested capture device for capturing 16 bits samples
            CaptureDevice = new AudioCapture(Device, SampleRate, format, SampleRate);
            if (CaptureDevice == null)
            {
                Logger.Warning("Failed to open the audio capture device with name: {0}", _deviceName);
                return false;
            }

            // Clear the array of samples
            _samples = new short[SampleRate * ChannelCount];

            // Notify the derived class
            if (OnStart())
            {
                // Start the capture
                CaptureChecker.Check(CaptureDevice, () => CaptureDevice.Start());
                return Capturing = true;
            }

            return false;
        }

        internal void Stop()
        {
            // Stop the capturing by turn off the flag
            if (Capturing)
            {
                // Force to get remaining samples
                Capturing = false;
                Record();

                // Notify derived class
                OnStop();
            }
        }

        protected internal virtual bool OnStart()
        {
            // In case derived does not need to be notified when OnStart 
            return true;
        }

        protected internal abstract void OnStop();

        protected internal abstract bool OnProcessSample(short[] samples);

        public virtual void Dispose()
        {
            if (Capturing)
            {
                // Stop the capture
                CaptureChecker.Check(CaptureDevice, () => CaptureDevice.Stop());
                CaptureDevice = null;
                Capturing = false;

                _samples = null;
            }
        }
    }
}
