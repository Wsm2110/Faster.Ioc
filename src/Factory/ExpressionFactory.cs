using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using FastExpressionCompiler.LightExpression;
using Faster.Ioc.Contracts;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.InteropServices;
using Faster.Ioc.Collections;
using Faster.Ioc.Models;

namespace Faster.Ioc.Factory
{
    /// <summary>
    /// Generate expression based on given paramRegistrations and lifetime settings
    /// </summary>
    internal class ExpressionFactory : IExpressionFactory
    {
        #region Properties
        public double Count { get; private set; }

        #endregion

        #region Fields

        private RegistrationFactory _regFactory;

        private const sbyte _emptyBucket = -127;
        private const sbyte _tombstone = -126;

        private static readonly Vector128<sbyte> _emptyBucketVector = Vector128.Create(_emptyBucket);

        private sbyte[] _metadata;
        private Entry<Type, Func<Scoped, object>>[] _entries;

        private const uint GoldenRatio = 0x9E3779B9; //2654435769;
        private uint _length;

        private byte _shift = 32;
        private double _maxLookupsBeforeResize;
        private uint _lengthMinusOne;
        private readonly double _loadFactor;
        private readonly IEqualityComparer<Type> _comparer;


        #endregion

        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reg"></param>
        /// <param name="delegates"></param>
        public ExpressionFactory(RegistrationFactory reg)
        {
            _regFactory = reg;

            if (!Vector128.IsHardwareAccelerated)
            {
                throw new NotSupportedException("Your hardware does not support acceleration for 128 bit vectors");
            }

            //default length is 16
            _length = 16;
            _loadFactor = 0.5;
            _maxLookupsBeforeResize = (uint)(_length * _loadFactor);
            _comparer = EqualityComparer<Type>.Default;
            _shift = (byte)(_shift - BitOperations.Log2(_length));

            _entries = new Entry<Type, Func<Scoped, object>>[_length + 16];
            _metadata = new sbyte[_length + 16];

            //fill metadata with emptybucket info
            Array.Fill(_metadata, _emptyBucket);

            _lengthMinusOne = _length - 1;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Tries to find the key in the map
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns>Returns false if the key is not found</returns>       
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Func<Scoped, object> Get(Type key)
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
                    goto generate;
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

            generate:
            return Create(key);
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Resolve all param expressions in reverse order and compile into a delegate
        /// </summary>
        /// <param name="registration">The registration.</param>
        /// <returns></returns> 
        private Func<Scoped, object> Create(Type type)
        {
            //detect circular references
            var crs = new CircularReference();

            //get registration matching type
            var registration = GetRegistration(type);

            //get all param paramRegistrations
            var paramRegistrations = GetParameterRegistrations(registration, crs);

            foreach (var reg in paramRegistrations)
            {
                //Compile param expressions
                Compile(reg);
            }

            // Compile main registation
            return Compile(registration);
        }

        /// <summary>
        /// 
        /// Inserts a key and value in the hashmap
        ///
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns>returns false if key already exists</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Emplace(Type key, Func<Scoped, object> value)
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

                    if (_comparer.Equals(entry.Key, key))
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

        private IEnumerable<Func<Scoped, object>> GetAll(Type key)
        {
            return Enumerable.Empty<Func<Scoped, object>>();
        }

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
            _entries = GC.AllocateUninitializedArray<Entry<Type, Func<Scoped, object>>>(size);

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


        /// <summary>
        /// Starts a chain of events which leads to an expression and delegate
        /// </summary>
        /// <param name="reg"></param>
        /// <param name="hashmap"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Func<Scoped, object> Compile(Registration reg)
        {
            if (reg.Expression != null)
            {
                //no need to process, already generated an expression
                var d = (Func<Scoped, object>)Expression.Lambda(reg.Expression, Scoped.ScopeParam).CompileFast();

                //store delegate in cache
                Emplace(reg.RegisteredType, d);

                return d;
            }

            //Determine if registration has overrides
            var @base = reg.OverrideExpression != null
                ? CreateBaseExpressionWithOverride(reg)
                : CreateBaseExpression(reg);

            //appy lifetime effect
            reg.Expression = CreateLifetimeExpression(reg, @base);

            // Make sure object is disposed
            var dispose = AddCleanup(reg);

            //Generate delegate
            var @delegate = (Func<Scoped, object>)Expression.Lambda(dispose, Scoped.ScopeParam).CompileFast();

            //store delegate in cache
            Emplace(reg.RegisteredType, @delegate);

            //return delegate
            return @delegate;
        }

        /// <summary>
        /// Resolves the object with overrides.
        /// </summary>
        /// <param name="registration">The registration.</param>
        private Expression CreateBaseExpressionWithOverride(Registration registration)
        {
            if (!(registration.OverrideExpression.Body is System.Linq.Expressions.NewExpression body))
            {
                throw new InvalidOperationException("Expression must have a body");
            }

            return registration.Constructor.GetParameters().Length == 0
                ? Expression.New(body.Constructor)
                : Expression.New(body.Constructor, GetParameterExpressionOverride(body));
        }

        /// <summary>
        /// Gets the expression parameters of an override
        /// </summary>
        /// <param name="body">The body.</param>
        /// <returns></returns>
        [MethodImpl(256)]
        private IEnumerable<Expression> GetParameterExpressionOverride(System.Linq.Expressions.NewExpression body)
        {
            var parameters = body.Constructor.GetParameters();
            var args = body.Arguments;
            byte found = 0;

            for (int i = 0; i < args.Count; ++i)
            {
                var parameter = parameters[i];
                var arg = args[i];
                var entries = _regFactory.GetAll(parameter.ParameterType);

                foreach (var e in entries)
                {
                    if (e.ReturnType == arg.Type)
                    {
                        yield return e.Expression;
                        found = 1;
                        break;
                    }
                }

                if (found != 1)
                {
                    throw new InvalidOperationException($"Unable to find type {parameter.ParameterType}");
                }
            }
        }

        /// <summary>
        /// Creates a new exp targetting the largest constructor
        /// </summary>
        /// <param name="reg">The entry.</param>
        [MethodImpl(256)]
        private Expression CreateBaseExpression(Registration reg)
        {
            var parameters = reg.Constructor.GetParameters();
            return parameters.Length == 0
                ? Expression.New(reg.Constructor)
                : Expression.New(reg.Constructor, GetParameterExpressions(parameters));
        }

        /// <summary>
        /// Retrieve all parameters
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        [MethodImpl(256)]
        private IEnumerable<Expression> GetParameterExpressions(ParameterInfo[] parameters)
        {
            for (int i = 0; i < parameters.Length; ++i)
            {
                var type = parameters[i].ParameterType;

                //resolve Ienumerable<> parameters
                if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
                    type.GetGenericTypeDefinition() == typeof(IList<>)))
                {
                    var list = _regFactory.GetAll(type.GetGenericArguments()[0]).Select(r => r.Expression);

                    yield return Expression.NewArrayInit(type.GetGenericArguments()[0], list);
                    continue;
                }

                var registration = _regFactory.Get(type);
                if (registration != null)
                {
                    if (registration.Expression == null)
                    {
                        throw new InvalidOperationException($"Expression of type [{registration.ReturnType.FullName}] not found");
                    }

                    yield return registration.Expression;
                    continue;
                }
            }
        }

        [MethodImpl(256)]
        private IEnumerable<Registration> GetParameterRegistrations(Registration registration, CircularReference circularReferenceService)
        {
            circularReferenceService.Add(registration.RegisteredType);

            var parameters = registration.Constructor.GetParameters();

            for (int i = 0; i < parameters.Length; ++i)
            {
                // Get param type
                var type = parameters[i].ParameterType;

                // Resolve ienumerable
                if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
                    type.GetGenericTypeDefinition() == typeof(IList<>)))
                {
                    type = type.GetGenericArguments()[0];
                }

                // Find closed generic
                if (type.IsGenericType)
                {
                    //if there is an open generic registration, create new closed generic registration
                    var openGenericRegistration = _regFactory.Get(type.GetGenericTypeDefinition());
                    if (openGenericRegistration != null)
                    {
                        var arguments = type.GetGenericArguments();

                        //create new return type
                        var returnType = openGenericRegistration.ReturnType.MakeGenericType(arguments);

                        //create new registration
                        var reg = new Registration(openGenericRegistration.RegisteredType.MakeGenericType(arguments), returnType, openGenericRegistration.Lifetime);

                        //create new closed generic registration - wont harm if there duplicates, wont get saved
                        _regFactory.Emplace(type, reg);

                        //get param registrations
                        foreach (var item in GetParameterRegistrations(reg, circularReferenceService))
                        {
                            yield return item;
                        }

                        //return self
                        yield return reg;
                        continue;
                    }
                }

                foreach (var reg in _regFactory.GetAll(type))
                {
                    //Get param registrations
                    foreach (var item in GetParameterRegistrations(reg, circularReferenceService))
                    {
                        //return all sub params
                        yield return item;
                    }

                    //return paramreg
                    yield return reg;
                }
            }

            circularReferenceService.Exit();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Expression AddCleanup(Registration reg)
        {
            var isIdisposable = reg.ReturnType.GetInterfaces().FirstOrDefault(i => i == typeof(IDisposable));
            if (isIdisposable == null)
            {
                //Return expression, no Disposable interface found
                return reg.Expression;
            }

            if (reg.Lifetime == Lifetime.Transient)
            {
                //Create variable 'transient'
                var varExp = Expression.Variable(reg.RegisteredType, "transient");

                //Assign variable
                var assignExp = Expression.Assign(varExp, reg.Expression);

                //Create block which adds variable to scope disposeable list
                var block = Expression.Block(new[] { varExp },
                           assignExp,
                           //call disposables property on scoped object
                           Expression.Call(Expression.Property(Scoped.ScopeParam, Scoped.DisposePropertyInfo), Scoped.AddMethodInfo, varExp),
                           varExp);

                return block;
            }

            if (reg.Lifetime == Lifetime.Singleton)
            {
                //Add singleton to container scope
                Container.ContainerScope.Disposeables.Add((IDisposable)((ConstantExpression)reg.Expression).Value);
                return reg.Expression;
            }

            if (reg.Lifetime == Lifetime.Scoped)
            {
                //create variable 
                var variableExpression = Expression.Variable(typeof(object), "scoped");

                //assign variable scoped
                var assignExpression = Expression.Assign(variableExpression, reg.Expression);

                //contains list<>
                var containsExpression = Expression.Call(Expression.Property(Scoped.ScopeParam, Scoped.DisposePropertyInfo), Scoped.ContainsMethodInfo, Expression.Convert(variableExpression, typeof(object)));

                //add list<>
                var addExpression = Expression.Call(Expression.Property(Scoped.ScopeParam, Scoped.DisposePropertyInfo), Scoped.AddMethodInfo, Expression.Convert(variableExpression, typeof(object)));

                //if else statement
                var output = Expression.Condition(containsExpression, Expression.Empty(), addExpression);

                //create block which add constant to Disposable list and returns variable 'scoped'
                return Expression.Block(new[] { variableExpression },
                    assignExpression,
                    output,
                    variableExpression);
            }

            return reg.Expression;
        }

        private Registration GetRegistration(Type type)
        {
            var registration = _regFactory.Get(type);
            if (registration != null)
            {
                return registration;
            }

            if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
                type.GetGenericTypeDefinition() == typeof(IList<>)))
            {
                //create closed generic reg
                var IenumerableRegistration = new Registration(type, typeof(List<>).MakeGenericType(type.GenericTypeArguments));

                //store closed generic Registration
                _regFactory.Emplace(type, IenumerableRegistration);

                //return closed generic
                return IenumerableRegistration;
            }

            // type is closed
            if (type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                //retrieve openGeneric registration
                var openGenericRegistration = _regFactory.Get(type.GetGenericTypeDefinition());
                if (openGenericRegistration != null)
                {
                    //create closed generic reg
                    var closedGenericRegistration = new Registration(type, openGenericRegistration.ReturnType.MakeGenericType(type.GenericTypeArguments), openGenericRegistration.Lifetime);

                    //store closed generic Registration
                    _regFactory.Emplace(type, closedGenericRegistration);

                    //return closed generic
                    return closedGenericRegistration;
                }
            }

            throw new InvalidOperationException($"Please register type:{type.FullName}");
        }

        /// <summary>
        /// Creates an expression based on the current scope
        /// </summary>
        /// <param name="reg">The reg.</param>
        /// <param name="expression">The expression.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Expression CreateLifetimeExpression(Registration reg, Expression expression)
        {
            if (reg.Lifetime == Lifetime.Transient)
            {
                //Dont do anything
                return expression;
            }

            if (reg.Lifetime == Lifetime.Singleton)
            {
                var @delegate = Expression.Lambda(expression).CompileFast<Func<object>>();
                //Create instance of compiled delegate
                var instance = @delegate();
                //Create constant expression
                return Expression.Constant(instance);
            }

            if (reg.Lifetime == Lifetime.Scoped)
            {
                //Create delegate
                var @delegate = Expression.Lambda<Func<object>>(expression).CompileFast();

                //Create method call which will add the constant expression to the scoped instance
                var scopedExpression = Expression.Call(Scoped.ScopeParam, Scoped.EmplaceOrGetScopedInstanceMethod, Expression.Constant(@delegate), Expression.Constant(Scoped.ScopeIndex));

                //increase scoped index
                ++Scoped.ScopeIndex;

                return scopedExpression;
            }

            return expression;
        }


        #endregion

    }
}
