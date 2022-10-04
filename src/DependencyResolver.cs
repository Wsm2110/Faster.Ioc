using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Faster.Ioc.Collections;
using Faster.Ioc.Contracts;
using FastExpressionCompiler.LightExpression;
using static FastExpressionCompiler.LightExpression.Expression;

namespace Faster.Ioc
{
#pragma warning disable
    public class DependencyResolver
    {
        #region Fields

        /// <summary>
        /// The registrations
        /// </summary>
        private readonly MultiMap<Type, Registration> _registrations;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the delegate cache.
        /// </summary>
        /// <value>
        /// The delegate cache.
        /// </value>
        public DelegateCache DelegateCache { get; set; }

        /// <summary>
        /// Gets or sets the key cache.
        /// </summary>
        /// <value>
        /// The key cache.
        /// </value>
        public MultiMap<int, Func<Scoped, object>> KeyCache { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyResolver" /> class.
        /// </summary>
        /// <param name="registrations">The registrations.</param>
        /// <param name="delegateCache">The entries.</param>
        public DependencyResolver(MultiMap<Type, Registration> registrations)
        {
            _registrations = registrations;
            DelegateCache = new DelegateCache(256, 0.5, this);
            KeyCache = new MultiMap<int, Func<Scoped, object>>(32, 0.5);
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Creates the delegate.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public Func<Scoped, object> CreateDelegate(Type type)
        {
            if (!type.IsGenericType)
            {
                var registration = _registrations.Get(type);
                if (registration == null)
                {
                    return _ => null;
                }

                return Compile(registration);
            }

            //resolve generic type
            var generic = _registrations.Get(type);
            if (generic != null)
            {
                return Compile(generic);
            }

            var opengeneric = _registrations.Get(type.GetGenericTypeDefinition());
            if (opengeneric == null)
            {
                return null;
            }

            var returnType = opengeneric.ReturnType.MakeGenericType(type.GetGenericArguments());
            var genericRegistration = new Registration(type, returnType, opengeneric.Lifetime);

            //store registration
            _registrations.Emplace(type, genericRegistration);
            return Compile(genericRegistration);
        }

        /// <summary>
        /// Creates one or more delegates
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public IEnumerable<Func<Scoped, object>> CreateDelegates(Type key)
        {
            var registration = _registrations.GetAll(key);
            foreach (var reg in registration)
            {
                yield return CreateDelegate(reg.RegisteredType);
            }
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

            var parameters = registration.Constructor.GetParameters();

            return parameters.Length == 0
                ? New(body.Constructor)
                : New(body.Constructor, GetParameterExpressionOverride(body));
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
                var entries = _registrations.GetAll(parameter.ParameterType);

                foreach (var e in entries)
                {
                    if (e.ReturnType == arg.Type)
                    {
                        yield return e.Expression;
                        found = 1;
                    }
                }

                if (found != 1)
                {
                    throw new InvalidOperationException($"Unable to find type {parameter.ParameterType}");
                }
            }
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Creates a new exp targetting the largest constructor
        /// </summary>
        /// <param name="reg">The entry.</param>
        [MethodImpl(256)]
        private Expression CreateBaseExpression(Registration reg)
        {
            if (reg.Expression != null)
            {
                return reg.Expression;
            }

            var parameters = reg.Constructor.GetParameters();
            return parameters.Length == 0
                ? New(reg.Constructor)
                : New(reg.Constructor, GetParameterExpressions(parameters));
        }

        /// <summary>
        /// Retrieve all parameters
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        [MethodImpl(256)]
        private IEnumerable<Expression> GetParameterExpressions(ParameterInfo[] parameters)
        {
            if (parameters.Length == 0)
            {
                yield break;
            }

            for (int i = 0; i < parameters.Length; ++i)
            {
                ParameterInfo parameter = parameters[i];

                var registration = _registrations.Get(parameter.ParameterType);
                if (!parameter.ParameterType.IsGenericType)
                {
                    //Will always get the first registration available eventhough multiple registrations with the same interface exist
                    if (registration == null)
                    {
                        throw new InvalidOperationException($"{parameter.ParameterType} not registered");
                    }

                    if (registration.Expression == null)
                    {
                        throw new InvalidOperationException($"{registration.RegisteredType} expression not resolved");
                    }

                    yield return registration.Expression;
                    continue;
                }

                if (registration?.Expression != null)
                {
                    yield return registration.Expression;
                    continue;
                }

                //resolve Ienumerable<> parameters
                if (parameter.ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    var list = _registrations.GetAll(parameter.ParameterType.GetGenericArguments()[0]).Select(r => r.Expression);
                    yield return Expression.NewArrayInit(parameter.ParameterType.GetGenericArguments()[0], list);
                    continue;
                }

                //resolve open generics
                var type = parameter.ParameterType.GetGenericTypeDefinition();
                var args = parameter.ParameterType.GetGenericArguments();

                //get generic registration with fixed arguments
                var genericType = _registrations.Get(type.MakeGenericType(args));
                if (genericType != null)
                {
                    yield return New(genericType.Constructor, GetParameterExpressions(genericType.Constructor.GetParameters()));
                    continue;
                }

                //create openGenericType
                var openGenericType = _registrations.Get(type.GetGenericTypeDefinition());
                if (openGenericType == null)
                {
                    yield return null;
                    continue;
                }

                var registeredType = openGenericType.RegisteredType.MakeGenericType(args);
                var returnType = _registrations.Get(type).ReturnType.MakeGenericType(args);

                var genericRegistration = new Registration(registeredType, returnType, openGenericType.Lifetime);
                _registrations.Emplace(genericRegistration.RegisteredType, genericRegistration);

                //recursive call
                yield return New(genericRegistration.Constructor, GetParameterExpressions(genericRegistration.Constructor.GetParameters()));
            }
        }

        [MethodImpl(256)]
#pragma warning disable
        private IEnumerable<Registration> GetParameterRegistrations(Registration registration, CircularReferenceService crs, bool first)
        {
            var parameters = registration.Constructor.GetParameters();
            if (parameters.Length == 0)
            {
                if (first)
                {
                    //return self if no parameters found
                    yield return registration;
                }

                yield break;
            }

            crs.Add(registration);

            for (int i = 0; i < parameters.Length; ++i)
            {
                //Retrieve registrations recursively
                var type = parameters[i].ParameterType;
                if (!type.IsGenericType)
                {
                    goto Resolve;
                }

                //Retrieve Ienumerable
                if (type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    type = type.GetGenericArguments()[0];

                    goto Resolve;
                }

                //Retrieve closed generic type i.e Class<Classx>
                if (!type.IsGenericTypeDefinition)
                {
                    var args = type.GetGenericArguments();
                    var r = _registrations.Get(type.GetGenericTypeDefinition());
                    if (r != null)
                    {
                        //open generic registration
                        var registeredType = r.RegisteredType.MakeGenericType(args);
                        if (_registrations.ContainsKey(registeredType))
                        {
                            type = registeredType;
                        }
                        else
                        {
                            //create new registration with closed generics
                            var returnType = r.ReturnType.MakeGenericType(args);

                            ////Create new registration
                            var genericRegistration = new Registration(registeredType, returnType, r.Lifetime);

                            //Add to registrations
                            _registrations.Emplace(genericRegistration.RegisteredType, genericRegistration);

                            type = genericRegistration.RegisteredType;
                        }
                    }
                }

            Resolve:
                var registrations = _registrations.GetAll(type);
                foreach (var reg in registrations)
                {
                    foreach (var sub in GetParameterRegistrations(reg, crs, false))
                    {
                        yield return sub;
                    }

                    yield return reg;
                }
            }

            if (first)
            {
                yield return registration;
            }

            crs.Exit();
        }

#pragma warning restore

#pragma warning disable
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
#pragma warning restore

        /// <summary>
        /// Resolve all param expressions in reverse order and compile into a delegate
        /// </summary>
        /// <param name="registration">The registration.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Func<Scoped, object> Compile(Registration registration)
        {
            var crs = new CircularReferenceService();
            var registrations = GetParameterRegistrations(registration, crs, true);

            foreach (var reg in registrations)
            {
                if (reg.Expression != null)
                {
                    //already generated an expression
                    continue;
                }

                Expression @base;
                if (reg.OverrideExpression != null)
                {
                    //will create an expression without adding lifetimes
                    @base = CreateBaseExpressionWithOverride(reg);
                }
                else
                {
                    @base = CreateBaseExpression(reg);
                }

                //appy lifetime effect
                reg.Expression = CreateLifetimeExpression(reg, @base);

                //Make sure object is disposed
                var dispose = AddCleanup(reg);

                //Compile delegate
                reg.Value = (Func<Scoped, object>)Lambda(dispose, Scoped.ScopeParam).CompileFast();

                DelegateCache.Emplace(reg.RegisteredType, reg.Value);
            }

            //assigned by ref
            return registration.Value;
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
                //Create Delegate
                var @delegate = Lambda<Func<Scoped, object>>(expression, Scoped.ScopeParam).CompileFast();
                //Create instance of compiled delegate
                var instance = @delegate(null);
                //Create constant expression
                return Constant(instance);
            }

            if (reg.Lifetime == Lifetime.Scoped)
            {
                //Create delegate
                var @delegate = Lambda<Func<object>>(expression).CompileFast();

                //Create method call which will add the constant expression to the scoped instance
                var scopedExpression = Call(Scoped.ScopeParam, Scoped.EmplaceOrGetScopedInstanceMethod, Constant(@delegate), Constant(Scoped.ScopeIndex));

                //increase scoped index
                ++Scoped.ScopeIndex;

                return scopedExpression;
            }

            return expression;
        }

        #endregion
    }
#pragma warning restore
}
