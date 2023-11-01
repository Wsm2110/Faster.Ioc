using Faster.Ioc.Collections;
using Faster.Ioc.Comparer;
using Faster.Ioc.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Faster.Ioc.Factory
{
    /// <summary>
    /// This hashmap is mirrored from faster.map
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class RegistrationFactory
    {
        #region Properties

        /// <summary>
        /// Gets or sets how many elements are stored in the map
        /// </summary>
        /// <value>
        /// The entry count.
        /// </value>
        public int Count { get; private set; }

        /// <summary>
        /// Gets the size of the map
        /// </summary>
        /// <value>
        /// The size.
        /// </value>
        public uint Size => (uint)_entries.Length;


        public IEnumerable<Registration> Values()
        {          
            for (int i = 0; i < _entries.Length; i++)
            {
                var meta = _metadata[i];
                if (meta >= 0)
                {
                    yield return _entries[i].Value;
                }
            }
        }

        #endregion

        #region Fields
        private const sbyte _emptyBucket = -127;
        private const sbyte _tombstone = -126;

        private static readonly Vector128<sbyte> _emptyBucketVector = Vector128.Create(_emptyBucket);

        private sbyte[] _metadata;
        private Entry<Type, Registration>[] _entries;

        private const uint GoldenRatio = 0x9E3779B9; //2654435769;
        private uint _length;

        private byte _shift = 32;
        private double _maxLookupsBeforeResize;
        private uint _lengthMinusOne;
        private readonly double _loadFactor;
        private readonly IEqualityComparer<Type> _comparer;
        private readonly RegistrationEqualityComparer _regComp;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of class.
        /// </summary>
        /// <param name="length">The length of the hashmap. Will always take the closest power of two</param>
        /// <param name="loadFactor">The loadfactor determines when the hashmap will resize(default is 0.9d)</param>
        /// <param name="keyComparer">Used to compare keys to resolve hashcollisions</param>
        public RegistrationFactory(uint length, double loadFactor)
        {
            if (!Vector128.IsHardwareAccelerated)
            {
                throw new NotSupportedException("Your hardware does not support acceleration for 128 bit vectors");
            }

            //default length is 16
            _length = length;
            _loadFactor = loadFactor;

            if (loadFactor > 0.9)
            {
                _loadFactor = 0.9;
            }

            if (_length < 16)
            {
                _length = 16;
            }
            else if (BitOperations.IsPow2(_length))
            {
                _length = length;
            }
            else
            {
                _length = BitOperations.RoundUpToPowerOf2(_length);
            }

            _maxLookupsBeforeResize = (uint)(_length * _loadFactor);
            _comparer = EqualityComparer<Type>.Default;
            _regComp = new RegistrationEqualityComparer();
            _shift = (byte)(_shift - BitOperations.Log2(_length));

            _entries = new Entry<Type, Registration>[_length + 16];
            _metadata = new sbyte[_length + 16];

            //fill metadata with emptybucket info
            Array.Fill(_metadata, _emptyBucket);

            _lengthMinusOne = _length - 1;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 
        /// Inserts a key and value in the hashmap
        ///
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns>returns false if key already exists</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Emplace(Type key, Registration value)
        {
            //Resize if loadfactor is reached
            if (Count >= _maxLookupsBeforeResize)
            {
                Resize();
            }

            // Get object identity hashcode
            var hashcode = (uint)key.GetHashCode();
            // GEt 7 low bits
            var h2 = H2(hashcode);
            //Create vector of the 7 low bits
            var target = Vector128.Create(Unsafe.As<uint, sbyte>(ref h2));
            // Objectidentity hashcode * golden ratio (fibonnachi hashing) followed by a shift
            uint index = hashcode * GoldenRatio >> _shift;
            //Set initial jumpdistance index
            uint jumpDistance = 0;

            while (true)
            {
                //Load vector @ index
                var source = Vector128.LoadUnsafe(ref Find(_metadata, index));
                //Get a bit sequence for matched hashcodes (h2s)
                var mask = Vector128.Equals(source, target).ExtractMostSignificantBits();
                //Check if key is unique
                while (mask != 0)
                {
                    var bitPos = BitOperations.TrailingZeroCount(mask);
                    var entry = Find(_entries, index + Unsafe.As<int, uint>(ref bitPos));

                    if (_comparer.Equals(entry.Key, key) && _regComp.Equals(entry.Value, value))
                    {
                        //duplicate key found
                        return false;
                    }

                    //clear bit
                    mask = ResetLowestSetBit(mask);
                }

                mask = source.ExtractMostSignificantBits();
                //check for tombstones and empty entries 
                if (mask != 0)
                {
                    var BitPos = BitOperations.TrailingZeroCount(mask);
                    //calculate proper index
                    index += Unsafe.As<int, uint>(ref BitPos);

                    Find(_metadata, index) = Unsafe.As<uint, sbyte>(ref h2);

                    //retrieve entry
                    ref var currentEntry = ref Find(_entries, index);

                    //set key and value
                    currentEntry.Key = key;
                    currentEntry.Value = value;

                    ++Count;
                    return true;
                }

                //Probing is done by incrementing the currentEntry bucket by a triangularly increasing multiple of Groups:jump by 1 more group every time.
                //So first we jump by 1 group (meaning we just continue our linear scan), then 2 groups (skipping over 1 group), then 3 groups (skipping over 2 groups), and so on.
                //Interestingly, this pattern perfectly lines up with our power-of-two size such that we will visit every single bucket exactly once without any repeats(searching is therefore guaranteed to terminate as we always have at least one EMPTY bucket).
                //Also note that our non-linear probing strategy makes us fairly robust against weird degenerate collision chains that can make us accidentally quadratic(Hash DoS).
                //Also note that we expect to almost never actually probe, since that’s WIDTH(16) non-EMPTY buckets we need to fail to find our key in.
                jumpDistance += 16;
                index += jumpDistance;
                index = index & _length - 1;
            }
        }

        /// <summary>
        /// Tries to find the key in the map
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns>Returns false if the key is not found</returns>       
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Registration Get(Type key)
        {
            // Get object identity hashcode
            var hashcode = (uint)key.GetHashCode();
            // Objectidentity hashcode * golden ratio (fibonnachi hashing) followed by a shift
            uint index = hashcode * GoldenRatio >> _shift;
            // GEt 7 low bits
            var h2 = H2(hashcode);
            //Create vector of the 7 low bits
            var target = Vector128.Create(Unsafe.As<uint, sbyte>(ref h2));
            //Set initial jumpdistance index
            uint jumpDistance = 0;

            while (true)
            {
                //load vector @ index
                var source = Vector128.LoadUnsafe(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_metadata), index));
                //get a bit sequence for matched hashcodes (h2s)
                var mask = Vector128.Equals(target, source).ExtractMostSignificantBits();
                //Could be multiple bits which are set
                while (mask > 0)
                {
                    //Retrieve offset 
                    var bitPos = BitOperations.TrailingZeroCount(mask);
                    //Get index and eq
                    var entry = Find(_entries, index + Unsafe.As<int, byte>(ref bitPos));
                    //Use EqualityComparer to find proper entry
                    if (_comparer.Equals(entry.Key, key))
                    {
                        return entry.Value;
                    }

                    //clear bit
                    mask = ResetLowestSetBit(mask);
                }

                //Contains empty buckets    
                if (Vector128.Equals(source, _emptyBucketVector).ExtractMostSignificantBits() > 0)
                {
                    return default;
                }

                //Probing is done by incrementing the currentEntry bucket by a triangularly increasing multiple of Groups:jump by 1 more group every time.
                //So first we jump by 1 group (meaning we just continue our linear scan), then 2 groups (skipping over 1 group), then 3 groups (skipping over 2 groups), and so on.
                //Interestingly, this pattern perfectly lines up with our power-of-two size such that we will visit every single bucket exactly once without any repeats(searching is therefore guaranteed to terminate as we always have at least one EMPTY bucket).
                //Also note that our non-linear probing strategy makes us fairly robust against weird degenerate collision chains that can make us accidentally quadratic(Hash DoS).
                //Also note that we expect to almost never actually probe, since that’s WIDTH(16) non-EMPTY buckets we need to fail to find our key in.
                jumpDistance += 16;
                index += jumpDistance;
                index = index & _lengthMinusOne;
            }
        }

        public IEnumerable<Registration> GetAll(Type key)
        {
            // Get object identity hashcode
            var hashcode = (uint)key.GetHashCode();
            // Objectidentity hashcode * golden ratio (fibonnachi hashing) followed by a shift
            uint index = hashcode * GoldenRatio >> _shift;
            // GEt 7 low bits
            var h2 = H2(hashcode);
            //Create vector of the 7 low bits
            var target = Vector128.Create(Unsafe.As<uint, sbyte>(ref h2));
            //Set initial jumpdistance index
            uint jumpDistance = 0;

            while (true)
            {
                //load vector @ index
                var source = Vector128.LoadUnsafe(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_metadata), index));
                //get a bit sequence for matched hashcodes (h2s)
                var mask = Vector128.Equals(target, source).ExtractMostSignificantBits();
                //Could be multiple bits which are set
                while (mask > 0)
                {
                    //Retrieve offset 
                    var bitPos = BitOperations.TrailingZeroCount(mask);
                    //Get index and eq
                    var entry = Find(_entries, index + Unsafe.As<int, byte>(ref bitPos));
                    //Use EqualityComparer to find proper entry
                    if (_comparer.Equals(entry.Key, key))
                    {
                        yield return entry.Value;
                    }

                    //clear bit
                    mask = ResetLowestSetBit(mask);
                }

                //Contains empty buckets    
                if (Vector128.Equals(source, _emptyBucketVector).ExtractMostSignificantBits() > 0)
                {
                    yield break;
                }

                //Probing is done by incrementing the currentEntry bucket by a triangularly increasing multiple of Groups:jump by 1 more group every time.
                //So first we jump by 1 group (meaning we just continue our linear scan), then 2 groups (skipping over 1 group), then 3 groups (skipping over 2 groups), and so on.
                //Interestingly, this pattern perfectly lines up with our power-of-two size such that we will visit every single bucket exactly once without any repeats(searching is therefore guaranteed to terminate as we always have at least one EMPTY bucket).
                //Also note that our non-linear probing strategy makes us fairly robust against weird degenerate collision chains that can make us accidentally quadratic(Hash DoS).
                //Also note that we expect to almost never actually probe, since that’s WIDTH(16) non-EMPTY buckets we need to fail to find our key in.
                jumpDistance += 16;
                index += jumpDistance;
                index = index & _lengthMinusOne;
            }
        }


        #endregion

        #region Private Methods

        /// <summary>
        /// Resizes this instance.
        /// </summary>     
        private void Resize()
        {
            _shift--;

            //next power of 2
            _length = _length * 2;
            _lengthMinusOne = _length - 1;
            _maxLookupsBeforeResize = _length * _loadFactor;

            var oldEntries = _entries;
            var oldMetadata = _metadata;

            var size = Unsafe.As<uint, int>(ref _length) + 16;

            _metadata = GC.AllocateArray<sbyte>(size);
            _entries = GC.AllocateUninitializedArray<Entry<Type, Registration>>(size);

            _metadata.AsSpan().Fill(_emptyBucket);

            for (uint i = 0; i < oldEntries.Length; ++i)
            {
                var h2 = Find(oldMetadata, i);
                if (h2 < 0)
                {
                    continue;
                }

                var entry = Find(oldEntries, i);

                //expensive if hashcode is slow, or when it`s not cached like strings
                var hashcode = (uint)entry.Key.GetHashCode();
                //calculate index by using object identity * fibonaci followed by a shift
                uint index = hashcode * GoldenRatio >> _shift;
                //Set initial jumpdistance index
                uint jumpDistance = 0;

                while (true)
                {
                    //check for empty entries
                    var mask = Vector128.LoadUnsafe(ref Find(_metadata, index)).ExtractMostSignificantBits();
                    if (mask != 0)
                    {
                        var BitPos = BitOperations.TrailingZeroCount(mask);

                        index += Unsafe.As<int, uint>(ref BitPos);

                        Find(_metadata, index) = h2;
                        Find(_entries, index) = entry;
                        break;
                    }

                    jumpDistance += 16;
                    index += jumpDistance;
                    index = index & _lengthMinusOne;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref T Find<T>(T[] array, uint index)
        {
#if DEBUG
            return ref array[index];
#else
            ref var arr0 = ref MemoryMarshal.GetArrayDataReference(array);
            return ref Unsafe.Add(ref arr0, index);
#endif
        }

        /// <summary>
        /// Reset the lowest significant bit in the given value
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint ResetLowestSetBit(uint value)
        {
            // It's lowered to BLSR on x86
            return value & value - 1;
        }

        /// <summary>
        /// Retrieve 7 low bits from hashcode
        /// </summary>
        /// <param name="hashcode"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint H2(uint hashcode) => hashcode & 0b01111111;

        #endregion
    }
}