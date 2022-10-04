using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Faster.Ioc.Collections
{

    public class DelegateCache
    {
        #region Fields

        private uint _maxlookups;
        private int _maxlookupsMinusOne;
        private readonly double _loadFactor;
        private readonly DependencyResolver _dependencyResolver;
        private byte _maxProbeSequenceLength;
        private byte _currentProbeSequenceLength;
        private MetaByte[] _info;
        private DelegateCacheEntry[] _entries;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets how many elements are stored in the map
        /// </summary>
        /// <value>
        /// The entry count.
        /// </value>
        public uint Count { get; private set; }

        /// <summary>
        /// Returns all the entries as KeyValuePair objects
        /// </summary>
        /// <value>
        /// The keys.
        /// </value>
        public IEnumerable<KeyValuePair<Type, Func<Scoped, object>>> Entries
        {
            get
            {
                //iterate backwards so we can remove the current item
                for (int i = _info.Length - 1; i >= 0; --i)
                {
                    if (!_info[i].IsEmpty())
                    {
                        var entry = _entries[i];
                        yield return new KeyValuePair<Type, Func<Scoped, object>>(entry.Key, entry.Value);
                    }
                }
            }
        }

        /// <summary>
        /// Returns all available keys
        /// </summary>
        /// <value>
        /// The keys.
        /// </value>
        public IEnumerable<Type> Keys
        {
            get
            {
                //iterate backwards so we can remove the current item
                for (int i = _info.Length - 1; i >= 0; --i)
                {
                    if (!_info[i].IsEmpty())
                    {
                        yield return _entries[i].Key;
                    }
                }
            }
        }

        /// <summary>
        /// Returns all available Values
        /// </summary>
        /// <value>
        /// The keys.
        /// </value>
        public IEnumerable<Func<Scoped, object>> Values
        {
            get
            {
                for (int i = _info.Length - 1; i >= 0; --i)
                {
                    if (!_info[i].IsEmpty())
                    {
                        yield return _entries[i].Value;
                    }
                }
            }
        }


        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DelegateCache" /> class.
        /// </summary>
        /// <param name="length">The length.</param>
        /// <param name="loadFactor">The load factor.</param>
        /// <param name="dependencyResolver">The dependency resolver.</param>
        public DelegateCache(uint length, double loadFactor, DependencyResolver dependencyResolver)
        {
            //default length is 16
            _maxlookups = length;
            _loadFactor = loadFactor;
            _dependencyResolver = dependencyResolver;

            var size = NextPow2(_maxlookups);
            _maxProbeSequenceLength = length < 127
                ? Log2(size)
                : (byte)127;
            _maxlookupsMinusOne = (int)size - 1;
            _entries = new DelegateCacheEntry[_maxlookups + _maxProbeSequenceLength + 1];
            _info = new MetaByte[_maxlookups + _maxProbeSequenceLength + 1];
        }

        #endregion

        #region Methods

        /// <summary>
        /// Inserts the value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        [MethodImpl(256)]
#pragma warning disable S3776 // Cognitive Complexity of methods should not be too high
        public bool Emplace(Type key, Func<Scoped, object> value)
        {
            if ((double)Count / _maxlookups > _loadFactor)
            {
                Resize();
            }

            var hashcode = RuntimeHelpers.GetHashCode(key);
            int index = hashcode & _maxlookupsMinusOne;

            if (ContainsKey(key, value))
            {
                return false;
            }

            DelegateCacheEntry delegateCacheEntry = default;
            delegateCacheEntry.Value = value;
            delegateCacheEntry.Key = key;

            MetaByte metadata = default;
            metadata.Psl = 0;
            metadata.Hashcode = hashcode;

            for (; ; ++metadata.Psl, ++index)
            {
                if (_currentProbeSequenceLength < metadata.Psl)
                {
                    _currentProbeSequenceLength = metadata.Psl;
                }

                var info = _info[index];
                if (info.IsEmpty())
                {
                    _entries[index] = delegateCacheEntry;
                    _info[index] = metadata;
                    ++Count;
                    return true;
                }

                if (info.Hashcode == metadata.Hashcode)
                {
                    ++Count;
                    //make sure same hashcodes are next to eachother
                    StartSwapping(index, ref delegateCacheEntry, ref metadata);
                    return true;
                }

                if (metadata.Psl > info.Psl)
                {
                    //steal from the rich, give to the poor
                    Swap(ref delegateCacheEntry, ref _entries[index]);
                    Swap(ref metadata, ref _info[index]);
                    continue;
                }

                if (metadata.Psl == _maxProbeSequenceLength)
                {
                    if (metadata.Psl == 127)
                    {
                        throw new InvalidOperationException("Only 127 values can be stored with 1 unique key. Since psl is a byte and we use 1 bit the indicate if this struct is empty, it leaves us with 127, hence the max entries stored with the same key is 127");
                    }

                    Resize();
                    //Make sure after a resize to insert the current entry
                    EmplaceInternal(ref delegateCacheEntry, ref metadata);
                    return true;
                }
            }
        }
#pragma warning restore S3776 // Cognitive Complexity of methods should not be too high

        /// <summary>
        /// Swap all entries until there is an empty entry
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="delegateCacheEntry">The entry.</param>
        /// <param name="data">The data.</param>
        private void StartSwapping(int index, ref DelegateCacheEntry delegateCacheEntry, ref MetaByte data)
        {
        Start:

            if (data.IsEmpty())
            {
                return;
            }

            ++data.Psl;

            if (index == _maxlookups)
            {
                Resize();
                EmplaceInternal(ref delegateCacheEntry, ref data);
                return;
            }

            if (_currentProbeSequenceLength < data.Psl)
            {
                _currentProbeSequenceLength = data.Psl;
            }

            //swap lower with upper
            Swap(ref delegateCacheEntry, ref _entries[index + 1]);
            Swap(ref data, ref _info[index + 1]);

            ++index;

#pragma warning disable S907 // "goto" statement should not be used
            goto Start;
#pragma warning restore S907 // "goto" statement should not be used
        }

        /// <summary>
        /// Gets the first entry matching the specified key.
        /// If the same key is used for multiple entries we return the first entry matching the given criteria
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        [MethodImpl(256)]
        public Func<Scoped, object> Get(Type key)
        {
            int hashcode = RuntimeHelpers.GetHashCode(key);
            int index = hashcode & _maxlookupsMinusOne;

            var maxDistance = index + _currentProbeSequenceLength;
            for (; index <= maxDistance; ++index)
            {
                var entry = _entries[index];
                if (ReferenceEquals(entry.Key, key))
                {
                    return entry.Value;
                }
            }

            return _dependencyResolver.CreateDelegate(key);
        }

        /// <summary>
        /// Get all entries matching the key
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public IEnumerable<Func<Scoped, object>> GetAll(Type key)
        {
            int hashcode = key.GetHashCode();
            int index = hashcode & _maxlookupsMinusOne;

            var maxDistance = index + _currentProbeSequenceLength;
            for (; index <= maxDistance; ++index)
            {
                var entry = _entries[index];
                if (ReferenceEquals(entry.Key, key))
                {
                    yield return entry.Value;
                }
            }

            var dels = _dependencyResolver.CreateDelegates(key);
            foreach (var del in dels)
            {
                yield return del;
            }
        }

        /// <summary>
        /// Determines whether the specified key is already inserted in the map
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>
        ///   <c>true</c> if the specified key contains key; otherwise, <c>false</c>.
        /// </returns>
        [MethodImpl(256)]
        public bool ContainsKey(Type key)
        {
            int hashcode = RuntimeHelpers.GetHashCode(key);
            int index = hashcode & _maxlookupsMinusOne;
            var maxDistance = index + _currentProbeSequenceLength;

            for (; index <= maxDistance; ++index)
            {
                if (ReferenceEquals(_entries[index].Key, key))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether the specified key and value exists
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        ///   <c>true</c> if the specified key contains key; otherwise, <c>false</c>.
        /// </returns>
        [MethodImpl(256)]
        public bool ContainsKey(Type key, Func<Scoped, object> value)
        {
            int hashcode = RuntimeHelpers.GetHashCode(key);
            int index = hashcode & _maxlookupsMinusOne;

            var maxDistance = index + _currentProbeSequenceLength;

            for (; index <= maxDistance; ++index)
            {
                var entry = _entries[index];
                if (ReferenceEquals(entry.Key, key) && ReferenceEquals(value, entry.Value))
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// calculates next power of 2
        /// </summary>
        /// <param name="c">The c.</param>
        /// <returns></returns>
        ///
        [MethodImpl(256)]
        private static uint NextPow2(uint c)
        {
            c--;
            c |= c >> 1;
            c |= c >> 2;
            c |= c >> 4;
            c |= c >> 8;
            c |= c >> 16;
            ++c;
            return c;
        }

        /// <summary>
        /// Used for set checking operations (using enumerables) that rely on counting
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        private static byte Log2(uint value)
        {
            byte c = 0;
            while (value > 0)
            {
                c++;
                value >>= 1;
            }

            return c;
        }

        /// <summary>
        /// Resizes this instance.
        /// </summary>
        [MethodImpl(256)]
        private void Resize()
        {
            _maxlookups = NextPow2(_maxlookups + 1);
            _maxProbeSequenceLength = _maxlookups < 127 ? Log2(_maxlookups) : (byte)127;
            _maxlookupsMinusOne = (int)_maxlookups - 1;

            var oldEntries = new DelegateCacheEntry[_entries.Length];
            Array.Copy(_entries, oldEntries, _entries.Length);

            var oldInfo = new MetaByte[_entries.Length];
            Array.Copy(_info, oldInfo, _info.Length);

            _entries = new DelegateCacheEntry[_maxlookups + _maxProbeSequenceLength + 1];
            _info = new MetaByte[_maxlookups + _maxProbeSequenceLength + 1];

            Count = 0;

            for (var i = 0; i < oldEntries.Length; i++)
            {
                var info = oldInfo[i];
                if (info.IsEmpty())
                {
                    continue;
                }

                EmplaceInternal(ref oldEntries[i], ref info);
            }
        }

        /// <summary>
        /// Emplaces a new entry without checking for key existence
        /// </summary>
        /// <param name="delegateCacheEntry">The entry.</param>
        /// <param name="metadata">The metadata.</param>
        /// <returns></returns>
        [MethodImpl(256)]
        private void EmplaceInternal(ref DelegateCacheEntry delegateCacheEntry, ref MetaByte metadata)
        {
            // Calculate index
            int index = metadata.Hashcode & _maxlookupsMinusOne;

            // Reset psl
            metadata.Psl = 0;

            for (; ; ++metadata.Psl, ++index)
            {
                if (_currentProbeSequenceLength < metadata.Psl)
                {
                    _currentProbeSequenceLength = metadata.Psl;
                }

                var info = _info[index];
                if (info.IsEmpty())
                {
                    _entries[index] = delegateCacheEntry;
                    _info[index] = metadata;
                    ++Count;
                    return;
                }

                if (info.Hashcode == metadata.Hashcode)
                {
                    ++Count;
                    //make sure same hashcodes are in line
                    StartSwapping(index, ref delegateCacheEntry, ref metadata);
                    return;
                }

                if (metadata.Psl > info.Psl)
                {
                    Swap(ref delegateCacheEntry, ref _entries[index]);
                    Swap(ref metadata, ref _info[index]);
                    continue;
                }

                if (metadata.Psl == _maxProbeSequenceLength)
                {
                    if (metadata.Psl == 127)
                    {
                        // throw new MultiMapException("Only 127 values can be stored with 1 unique key");
                    }

                    Resize();
                    //Make sure after a resize to insert the current entry
                    EmplaceInternal(ref delegateCacheEntry, ref metadata);
                    return;
                }
            }
        }

        /// <summary>
        /// Swaps the content of the specified values
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        private void Swap(ref DelegateCacheEntry x, ref DelegateCacheEntry y)
        {
            var tmp = x;

            x = y;
            y = tmp;
        }

        /// <summary>
        /// Swaps the content of the specified values
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        private void Swap(ref MetaByte x, ref MetaByte y)
        {
            var tmp = x;

            x = y;
            y = tmp;
        }

        #endregion
    }

    /// <summary>
    /// Storing a key and value(max (16 bytes)) without padding
    /// </summary>
    [DebuggerDisplay("{Key.ToString()} - {Value?.ToString()} ")]
    [StructLayout(LayoutKind.Sequential)]
    struct DelegateCacheEntry
    {
        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        /// <value>
        /// The key.
        /// </value>
        public Type Key;

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public Func<Scoped, object> Value;

    }
}
