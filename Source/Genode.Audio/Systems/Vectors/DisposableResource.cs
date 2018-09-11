using System;
using System.Collections.Generic;
using System.Text;

namespace Genode
{
    /// <summary>
    /// Represents an object that hold managed resource and may contain unmanaged resource as well.
    /// </summary>
    /// <inheritdoc />
    public abstract class DisposableResource : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether the current instance of this object is already disposed.
        /// </summary>
        public bool IsDisposed { get; protected set; }

        /// <summary>
        /// Releases all resources used by the current instance of this object.
        /// </summary>
        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the current instance of this object and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            IsDisposed = true;
        }
    }
}
