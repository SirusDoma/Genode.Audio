using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace Genode
{
    /// <summary>
    /// Provides logging functions.
    /// </summary>
    public class Logger
    {
        /// <summary>
        /// Gets the singleton instance of <see cref="Logger"/>.
        /// </summary>
        public static Logger Instance { get; } = new Logger();

        /// <summary>
        /// Represents the verbose level of log message.
        /// </summary>
        public enum Level
        {
            None = 0,
            Information = 1,
            Warning = 2,
            Error = 3
        }

        private bool Initialized { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="Logger"/> should include stack trace in log message.
        /// </summary>
        public bool UseStackTrace { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="Logger"/> should include timestamp in log message.
        /// </summary>
        public bool UseTimeStamp { get; set; } = true;

        /// <summary>
        /// Gets or sets a value of which stack should be printed if <see cref="UseStackTrace"/> is activated.
        /// </summary>
        public int StackFrame { get; set; } = 1;

        /// <summary>
        /// Gets or sets the indent level.
        /// </summary>
        public int IndentLevel
        {
            get =>
#if DEBUG
                Debug.IndentLevel;
#else
                Trace.IndentLevel;
#endif
            set =>
#if DEBUG
                Debug.IndentLevel = value;
#else
                Trace.IndentLevel = value;
#endif
        }

        /// <summary>
        /// Gets or sets the number of spaces in an indent.
        /// </summary>
        public int IndentSize
        {
            get =>
#if DEBUG
                Debug.IndentSize;
#else
                Trace.IndentSize;
#endif
            set =>
#if DEBUG
                Debug.IndentSize = value;
#else
                Trace.IndentSize = value;
#endif
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Logger"/>.
        /// </summary>
        public Logger()
        {
            Initialize();
        }

        /// <summary>
        /// Initialize the logger configuration.
        /// </summary>
        private void Initialize()
        {
            if (Initialized)
            {
                return;
            }

            try
            {
#if DEBUG
                Debug.AutoFlush = true;
#else
                Trace.AutoFlush = true;
#endif
            }
            catch (Exception ex)
            {
                Error(ex);
            }
            finally
            {
                Initialized = true;
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            }
        }

        /// <summary>
        /// Log the message with specified verbose level and stack frame.
        /// </summary>
        /// <param name="message">Message to print.</param>
        /// <param name="level">Verbose level to use.</param>
        /// <param name="stackFrameIndex">Stack frame to include.</param>
        private void Log(string message, Level level, int stackFrameIndex)
        {
            if (!Initialized)
            {
                Initialize();
            }

            string header = "";
            if (UseTimeStamp)
            {
                string timestamp = string.Format("[{0}]", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"));
                header = timestamp;
            }
            
            string levelString = string.Empty;
            switch (level)
            {
                case Level.Information: levelString = "Information"; break;
                case Level.Warning:     levelString = "  Warning  "; break;
                case Level.Error:       levelString = "   Error   "; break;
            }

            if (!string.IsNullOrEmpty(levelString))
            {
                header = $"{header} [{levelString}]";
            }

            if (UseStackTrace || level == Level.Warning || level == Level.Error)
            {
                string traceInfo = "";
                var stackTrace = new StackTrace(true);
                var stackFrame = stackTrace.GetFrame(stackFrameIndex);
                var method = stackFrame.GetMethod();

                if (method.Name == ".ctor")
                {
                    traceInfo = $"{method.DeclaringType.Name}()";
                }
                else
                {
                    string name = method.Name;
                    if (name.StartsWith("get_") || name.StartsWith("set_") || name.StartsWith("add_") || name.StartsWith("remove_"))
                    {
                        if (name.StartsWith("remove_"))
                        {
                            name = name.Remove(0, 7);
                        }
                        else
                        {
                            name = name.Remove(0, 4);
                        }
                    }

                    traceInfo = $"{method.DeclaringType.Name}::{name}";
                }

                traceInfo = $"[Ln.{stackFrame.GetFileLineNumber().ToString("000")}] {traceInfo}";
                header = $"{header} {traceInfo} -";
            }

            message = $"{header} {message}";
#if DEBUG
            Debug.WriteLine(message);
            foreach (TraceListener listener in Trace.Listeners)
            {
                listener.WriteLine(message);
            }
#else
            Trace.WriteLine(message);
#endif
        }

        /// <summary>
        /// Occurs when unhandled exception has been thrown.
        /// </summary>
        /// <param name="sender">Sender of the event.</param>
        /// <param name="e">Argument of the event.</param>
        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            Log(ex?.Message ?? "Unknown error", Level.Error, StackFrame + 1);
        }

        /// <summary>
        /// Log the message.
        /// </summary>
        /// <param name="message">Message to print.</param>
        public void Information(string message)
        {
            Log(message, Level.Information, StackFrame + 1);
        }

        /// <summary>
        /// Log the message with specified verbose level.
        /// </summary>
        /// <param name="message">Message to print.</param>
        /// <param name="level">Verbose level to use when printing the message</param>
        public void Log(string message, Level level = Level.None)
        {
            Log(message, level, StackFrame + 1);
        }

        /// <summary>
        /// Log the formatted message with information verbose level.
        /// </summary>
        /// <param name="format">Formatted message to print.</param>
        /// <param name="args">Arguments to fill into formatted message.</param>
        public void Information(string format, params object[] args)
        {
            Log(string.Format(format, args), Level.Information, StackFrame + 1);
        }

        /// <summary>
        /// Log the message with warning verbose level.
        /// </summary>
        /// <param name="message">Message to print.</param>
        public void Warning(string message)
        {
            Log(message, Level.Warning, StackFrame + 1);
        }

        /// <summary>
        /// Log the formatted message with warning verbose level.
        /// </summary>
        /// <param name="format">Formatted message to print.</param>
        /// <param name="args">Arguments to fill into formatted message.</param>
        public void Warning(string format, params object[] args)
        {
            Log(string.Format(format, args), Level.Warning, StackFrame + 1);
        }

        /// <summary>
        /// Log the message with error verbose level.
        /// </summary>
        /// <param name="format">Formatted message to print.</param>
        public void Error(string message)
        {
            Log(message, Level.Error, StackFrame + 1);
        }

        /// <summary>
        /// Log the formatted message with error verbose level.
        /// </summary>
        /// <param name="format">Formatted message to print.</param>
        /// <param name="args">Arguments to fill into formatted message.</param>
        public void Error(string format, params object[] args)
        {
            Log(string.Format(format, args), Level.Error, StackFrame + 1);
        }

        /// <summary>
        /// Log an exception with error verbose level.
        /// </summary>
        /// <param name="ex">Exception to print.</param>
        public void Error(Exception ex)
        {
            Log(ex.Message, Level.Error, StackFrame + 1);
        }

        /// <summary>
        /// Register a listener into current instance of <see cref="Logger"/>.
        /// </summary>
        /// <typeparam name="T">Type of <see cref="TraceListener"/> to add.</typeparam>
        public void AddListener<T>()
            where T : TraceListener, new()
        {
            var listener = new T();
            listener.Name = nameof(T);

            Trace.Listeners.Add(listener);
        }

        /// <summary>
        /// Register a listener into current instance of <see cref="Logger"/>.
        /// </summary>
        /// <typeparam name="T">Type of <see cref="TraceListener"/> to add.</typeparam>
        /// <param name="listener"><see cref="TraceListener"/> instance to add.</param>
        public void AddListener<T>(T listener)
            where T : TraceListener
        {
            Trace.Listeners.Add(listener);
        }

        /// <summary>
        /// Remove registered listener from current instance of <see cref="Logger"/>.
        /// </summary>
        /// <typeparam name="T">Type of <see cref="TraceListener"/> to remove.</typeparam>
        public void RemoveListener<T>()
            where T : TraceListener, new()
        {
            Trace.Listeners.Remove(nameof(T));
        }

        /// <summary>
        /// Remove registered listener from current instance of <see cref="Logger"/>.
        /// </summary>
        /// <typeparam name="T">Type of <see cref="TraceListener"/> to remove.</typeparam>
        /// <param name="listener"><see cref="TraceListener"/> instance to remove.</param>
        public void RemoveListener<T>(T listener)
            where T : TraceListener, new()
        {
            Trace.Listeners.Remove(listener);
        }

        /// <summary>
        /// Register <see cref="AppDomain"/> into current instance of <see cref="Logger"/>.
        /// </summary>
        /// <param name="appDomain"><see cref="AppDomain"/> to handle.</param>
        public void AddDomainHandler(AppDomain appDomain)
        {
            if (appDomain == AppDomain.CurrentDomain)
            {
                Warning("Domain is already registered.");
                return;
            }

            appDomain.UnhandledException += OnUnhandledException;
        }

        /// <summary>
        /// Remove registered <see cref="AppDomain"/> that handled by current instance of <see cref="Logger"/>.
        /// </summary>
        /// <param name="appDomain"><see cref="AppDomain"/> to remove.</param>
        public void RemoveDomainHandler(AppDomain appDomain)
        {
            if (appDomain == AppDomain.CurrentDomain)
            {
                Warning("Engine Application Domain cannot be unregistered.");
                return;
            }

            appDomain.UnhandledException -= OnUnhandledException;
        }

        /// <summary>
        /// Increase the current indent level by one.
        /// </summary>
        public void Indent()
        {
#if DEBUG
            Debug.Indent();
#else
            Trace.Indent();            
#endif
        }

        /// <summary>
        /// Decrease the current indent level by one.
        /// </summary>
        public void Unindent()
        {
#if DEBUG
            Debug.Unindent();
#else
            Trace.Unindent();
#endif
        }
    }
}
