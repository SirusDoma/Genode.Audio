using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace Cgen
{
    public static partial class Logger
    {
        public sealed class LogWriterListener : Listener
        {
            public LogWriterListener(string name, string fileName, bool overwrite = true)
                : base()
            {
                if (File.Exists(fileName) && overwrite)
                {
                    File.WriteAllText(fileName, string.Empty);
                }

                _listener = new TextWriterTraceListener(fileName, name);
            }

            public LogWriterListener(string name, Stream stream)
                : base()
            {
                _listener = new TextWriterTraceListener(stream, name);
            }
        }
    }
}
