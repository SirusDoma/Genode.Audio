using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace Cgen
{
    public static partial class Logger
    {
        private class ConsoleTraceListener : TraceListener
        {
            public ConsoleTraceListener(string name)
            {
                Name = name;
            }

            public override void Write(string message)
            {
                Console.Write(message);
            }

            public override void WriteLine(string message)
            {
                Console.WriteLine(message);
            }
        }

        public sealed class ConsoleListener : Listener
        {
            public ConsoleListener(string name)
                : base()
            {
                _listener = new ConsoleTraceListener(name);
            }
        }
    }
}
