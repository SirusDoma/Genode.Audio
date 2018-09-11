using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Genode
{
    public class ConsoleListener : TraceListener
    {
        public ConsoleListener()
            : base()
        {
        }

        public ConsoleListener(string name)
            : base(name)
        {
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
}
