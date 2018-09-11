using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace Genode
{
    public sealed class LogWriterListener : TextWriterTraceListener
    {
        public LogWriterListener(string name, string fileName)
            : base(fileName, name)
        {
        }

        public LogWriterListener(string name, Stream stream)
            : base(stream, name)
        {
        }
    }
}
