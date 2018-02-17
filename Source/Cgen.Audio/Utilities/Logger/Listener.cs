using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;


namespace Cgen
{
    public static partial class Logger
    {
        public abstract class Listener : IDisposable
        {
            protected internal TraceListener _listener;

            public void Dispose()
            {
                _listener?.Dispose();
            }
        }
    }
}
