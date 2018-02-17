using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using OpenTK;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;

namespace Cgen.Internal.OpenAL
{
    /// <summary>
    /// Provides OpenAL (ALC) checking functions.
    /// </summary>
    public static class CaptureChecker
    {
        private static bool _checked = true;

        /// <summary>
        /// Represents Verbose level of <see cref="GLChecker"/>.
        /// </summary>
        public enum VerboseFlags
        {
            /// <summary>
            /// Display all messages.
            /// </summary>
#if !DEBUG
            [Obsolete("Changing VerboseLevel to All will lead performance issue under Release Build Configurations.")]
#endif
            All = 0,

            /// <summary>
            /// Display Warning and Error messages only.
            /// </summary>
            Error = 1
        }

        /// <summary>
        /// Gets the last OpenAL Error that occurred.
        /// Any OpenAL call should be called through <see cref="ALChecker.Check(Action)"/> to make this property working properly.
        /// </summary>
        internal static AlcError LastError
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets whether the Error Checking should be performed and printed under <see cref="Trace"/> / <see cref="Debug"/> Listeners
        /// Regardless to Build Configurations.
        /// </summary>
        public static bool Verbose
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets Verbose Level of if <see cref="Verbose"/> property return true.
        /// </summary>
        public static VerboseFlags VerboseLevel
        {
            get;
            set;
        }

        static CaptureChecker()
        {
            Verbose      = false;
            VerboseLevel = VerboseFlags.Error;
        }

        /// <summary>
        /// Call specific function and check for OpenAL Error.
        /// </summary>
        /// <param name="function">Function to call.</param>
        public static void Check(AudioCapture device, Action function)
        {
            function();
            _checked = false;

            if (Verbose)
            {
                CheckError(device);
                return;
            }
#if DEBUG
            CheckError(device);
#endif
        }

        /// <summary>
        /// Call specific function and check for OpenAL Error.
        /// </summary>
        /// <param name="function">Function to call.</param>
        public static T Check<T>(AudioCapture device, Func<T> function)
        {
            var result = function();
            _checked = false;

            if (Verbose)
            {
                CheckError(device);
                return result;
            }
#if DEBUG
            CheckError(device);
#endif
            return result;
        }

        /// <summary>
        /// Gets for the latest OpenAL Error.
        /// </summary>
        /// <returns>Latest OpenAL Error.</returns>
        internal static AlcError GetError(AudioCapture device)
        {
            AlcError errorCode = device.CurrentError;
            LastError = errorCode;

            return errorCode;
        }

        /// <summary>
        /// Check for the OpenAL Error.
        /// </summary>
        public static void CheckError(AudioCapture device)
        {
            AlcError errorCode = GetError(device);
            if (errorCode == AlcError.NoError)
            {
                if (VerboseLevel == VerboseFlags.All)
                {
                    Logger.StackFrame = _checked ? 2 : 3;
                    Logger.Log("NoError: AL Operation Success", Logger.Level.Information);
                    Logger.StackFrame = 1;
                }

                _checked = true;
                return;
            }

            string error = "Unknown Error.";
            string description = "No Description available.";

            // Decode the error code
            switch (errorCode)
            {
                case AlcError.InvalidDevice:
                    {
                        error = "AL_INVALID_DEVICE";
                        description = "A bad device name has been specified.";
                        break;
                    }

                case AlcError.InvalidEnum:
                    {
                        error = "AL_INVALID_ENUM";
                        description = "An unacceptable value has been specified for an enumerated argument.";
                        break;
                    }

                case AlcError.InvalidValue:
                    {
                        error = "AL_INVALID_VALUE";
                        description = "A numeric argument is out of range.";
                        break;
                    }

                case AlcError.InvalidContext:
                    {
                        error = "AL_INVALID_CONTEXT";
                        description = "The specified operation is not allowed in the current state of audio context of this thread.";
                        break;
                    }

                case AlcError.OutOfMemory:
                    {
                        error = "AL_OUT_OF_MEMORY";
                        description = "There is not enough memory left to execute the command.";
                        break;
                    }
                default:
                    {
                        error = errorCode.ToString();
                        break;
                    }
            }

            Logger.StackFrame = _checked ? 2 : 3;
            Logger.Log(error + ": " + description, Logger.Level.Error);
            Logger.StackFrame = 1;

            _checked = true;
        }
    }
}
