using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Faster.Ioc.Contracts;
using Faster.Map;
using Faster.Map.Core;
using FastExpressionCompiler.LightExpression;

namespace Faster.Ioc.Collections
{
    /// <summary>
    /// This hashmap uses the following
    /// - Open addressing
    /// - Uses linear probing
    /// - Robinghood hashing
    /// - Upper limit on the probe sequence lenght(psl) which is Log2(size)
    /// </summary>
    public class HashMap
    {
        #region Properties

        /// <summary>
        /// Gets or sets how many elements are stored in the map
        /// </summary>
        /// <type>
        /// The entry count.
        /// </type>
        public int Count { get; private set; }

        /// <summary>
        /// Gets the size of the map
        /// </summary>
        /// <type>
        /// The size.
        /// </type>
        public uint Size => (uint)_entries.Length;

        #endregion

        #region Fields

        private InfoByte[] _info;
        private Entry[] _entries;
        private int _length;
        private int _maxEntriesMinusOne;
        private readonly double _loadFactor;
        private readonly IExpressionGenerator _expressionGenerator;
        private int _shift = 32;
        private int _maxProbeSequenceLength;
        private byte _currentProbeSequenceLength;
        private int _maxlengthBeforeResize;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of class.
        /// </summary>
        /// <param name="length">The length of the hashmap. Will always take the nearest power of two</param>
        /// <param name="loadFactor">The loadfactor determines when the hashmap will resize(default is 0.5d) i.e size 32 loadfactor 0.5 hashmap will resize at 16</param>
        internal HashMap(int length, double loadFactor, IExpressionGenerator expressionGenerator)
        {
            //default length is 8
            _length = length == 0 ? 8 : length;
            _loadFactor = loadFactor;
            _expressionGenerator = expressionGenerator;

            var size = NextPow2(_length);
            _maxProbeSequenceLength = _loadFactor <= 0.5 ? Log2(_length) : PslLimit(_length);
            _maxEntriesMinusOne = size - 1;
            _maxlengthBeforeResize = (int)(size * loadFactor);

            _shift = _shift - Log2(_length) + 1;
            _entries = new Entry[size + _maxProbeSequenceLength + 1];
            _info = new InfoByte[size + _maxProbeSequenceLength + 1];
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Inserts a type using a key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        [MethodImpl(256)]
        public bool Emplace(Type type, Func<Scoped, object> del)
        {
            //Resize if loadfactor is reached
            if (Count >= _maxlengthBeforeResize)
            {
                Resize();
            }

            var hashcode = RuntimeHelpers.GetHashCode(type);
            var index = hashcode & _maxEntriesMinusOne;

            //check if key is unique
            if (Contains(ref hashcode, type))
            {
                return false;
            }

            //Create entry
            Entry fastEntry = default;
            fastEntry.Value = del;
            fastEntry.Type = type;

            //Create default info byte
            InfoByte current = default;

            //Assign 0 to psl so it wont be seen as empty
            current.Psl = 0;

            //retrieve infobyte
            ref var info = ref _info[index];

            do
            {
                //Increase _current probe sequence
                if (_currentProbeSequenceLength < current.Psl)
                {
                    _currentProbeSequenceLength = current.Psl;
                }

                //Empty spot, add entry
                if (info.IsEmpty())
                {
                    _entries[index] = fastEntry;
                    info = current;
                    ++Count;
                    return true;
                }

                //Steal from the rich, give to the poor
                if (current.Psl > info.Psl)
                {
                    Swap(ref fastEntry, ref _entries[index]);
                    Swap(ref current, ref info);
                    continue;
                }

                //max psl is reached, resize
                if (current.Psl == _maxProbeSequenceLength)
                {
                    ++Count;
                    Resize();
                    EmplaceInternal(ref fastEntry, ref current);
                    return true;
                }

                //increase index
                info = ref _info[++index];

                //increase probe sequence length
                ++current.Psl;

            } while (true);
        }

        /// <summary>
        /// Gets the type with the corresponding key
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The type.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Func<Scoped, object> Get(Type type)
        {
            //create index
            int index = RuntimeHelpers.GetHashCode(type) & _maxEntriesMinusOne;

            // resolve entry
            ref var entry = ref _entries[index];

            // reference check
            if (ReferenceEquals(type, entry.Type))
            {
                return entry.Value;
            }   

            ++index;

            while (entry.Type != null)
            {
                entry = ref _entries[index];

                if (ReferenceEquals(type, entry.Type))
                {
                    return entry.Value;
                }

                ++index;
            }
        
            //Not found, try compiling a new expression
            return _expressionGenerator.Create(type, this);
        }

        /// <summary>
        /// Determines whether the specified key exists in the hashmap
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>
        ///   <c>true</c> if the specified key contains key; otherwise, <c>false</c>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(ref int hashcode, Type type)
        {
            int index = hashcode & _maxEntriesMinusOne;
            int maxDistance = index + _currentProbeSequenceLength;

            do
            {
                var entry = _entries[index];

                if (ReferenceEquals(type, entry.Type))
                {
                    return true;
                }

                ++index;
            } while (index < maxDistance);

            return default;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Emplaces a new entry without checking for key existence
        /// </summary>
        /// <param name="entry">The fast entry.</param>
        /// <param name="current">The information byte.</param>
        [MethodImpl(256)]
        private void EmplaceInternal(ref Entry entry, ref InfoByte current)
        {
            var index = entry.Type.GetHashCode() & _maxEntriesMinusOne;

            //reset psl
            current.Psl = 0;

            ref var info = ref _info[index];

            do
            {
                if (info.IsEmpty())
                {
                    _entries[index] = entry;
                    info = current;
                    return;
                }

                if (current.Psl > info.Psl)
                {
                    Swap(ref entry, ref _entries[index]);
                    Swap(ref current, ref _info[index]);
                    continue;
                }

                if (_currentProbeSequenceLength < current.Psl)
                {
                    _currentProbeSequenceLength = current.Psl;
                }

                //increase index
                info = ref _info[++index];

                //increase probe sequence length
                ++current.Psl;

            } while (true);
        }

        /// <summary>
        /// Swaps the content of the specified FastEntry values
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Swap(ref Entry x, ref Entry y)
        {
            var tmp = x;
            x = y;
            y = tmp;
        }

        /// <summary>
        /// Swaps the content of the specified Infobyte values
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Swap(ref InfoByte x, ref InfoByte y)
        {
            var tmp = x;
            x = y;
            y = tmp;
        }

        /// <summary>
        /// Returns a power of two probe sequence lengthzz
        /// </summary>
        /// <param name="size">The size.</param>
        /// <returns></returns>
        [MethodImpl(256)]
        private int PslLimit(int size)
        {
            switch (size)
            {
                case 16: return 4;
                case 32: return 5;
                case 64: return 6;
                case 128: return 7;
                case 256: return 8;
                case 512: return 9;
                case 1024: return 12;
                case 2048: return 15;
                case 4096: return 20;
                case 8192: return 25;
                case 16384: return 30;
                case 32768: return 35;
                case 65536: return 40;
                case 131072: return 45;
                case 262144: return 50;
                case 524288: return 55;
                case 1048576: return 60;
                case 2097152: return 65;
                case 4194304: return 70;
                case 8388608: return 75;
                case 16777216: return 80;
                case 33554432: return 85;
                case 67108864: return 90;
                case 134217728: return 95;
                case 268435456: return 100;
                case 536870912: return 105;
                default: return 10;
            }
        }

        /// <summary>
        /// Resizes this instance.
        /// </summary>
        [MethodImpl(256)]
        private void Resize()
        {
            _shift--;
            _length = NextPow2(_length + 1);
            _maxProbeSequenceLength = _loadFactor <= 0.5 ? Log2(_length) : PslLimit(_length);
            _maxlengthBeforeResize = (int)(_length * _loadFactor);

            _maxEntriesMinusOne = _length - 1;

            var oldEntries = new Entry[_entries.Length];
            Array.Copy(_entries, oldEntries, _entries.Length);

            var oldInfo = new InfoByte[_info.Length];
            Array.Copy(_info, oldInfo, _info.Length);

            _entries = new Entry[_length + _maxProbeSequenceLength + 1];
            _info = new InfoByte[_length + _maxProbeSequenceLength + 1];

            for (var i = 0; i < oldEntries.Length; ++i)
            {
                var info = oldInfo[i];
                if (info.IsEmpty())
                {
                    continue;
                }

                var entry = oldEntries[i];
                EmplaceInternal(ref entry, ref info);
            }
        }

        /// <summary>
        /// Calculates next power of 2
        /// </summary>
        /// <param name="c">The c.</param>
        /// <returns></returns>
        ///
        [MethodImpl(256)]
        private static int NextPow2(int c)
        {
            c--;
            c |= c >> 1;
            c |= c >> 2;
            c |= c >> 4;
            c |= c >> 8;
            c |= c >> 16;
            return ++c;
        }

        // Used for set checking operations (using enumerables) that rely on counting
        private static byte Log2(int value)
        {
            byte c = 0;
            while (value > 0)
            {
                c++;
                value >>= 1;
            }

            return c;
        }

        #endregion
    }
}