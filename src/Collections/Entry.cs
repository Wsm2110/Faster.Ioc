using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Faster.Ioc.Collections
{

    /// <summary>
    /// Storing a key and delegate(max (16 bytes)) without padding
    /// </summary>
    [DebuggerDisplay("{Type?.Name}")]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct Entry
    {
        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        /// <type>
        /// The key.
        /// </type>
        public Type Type { get; set; }

        /// <summary>
        /// Gets or sets the delegate.
        /// </summary>
        /// <type>
        /// The type.
        /// </type>
        public Func<Scoped, object> Value;

    }
}
