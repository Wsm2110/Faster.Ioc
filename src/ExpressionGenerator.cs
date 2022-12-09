using Faster.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using FastExpressionCompiler.LightExpression;
using Faster.Ioc.Contracts;
using Faster.Ioc.Collections;

namespace Faster.Ioc
{
    /// <summary>
    /// Generate expression based on given paramRegistrations and lifetime settings
    /// </summary>
    internal class ExpressionGenerator : IExpressionGenerator
    {
        #region Fields

        private readonly MultiMap<Type, Registration> _registrations;

        #endregion

        #region Constructor

        /// <summary>
        /// 
        /// </summary>
        /// <param name="registrations"></param>
        public ExpressionGenerator(MultiMap<Type, Registration> registrations)
        {
            _registrations = registrations;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Resolve all param expressions in reverse order and compile into a delegate
        /// </summary>
        /// <param name="registration">The registration.</param>
        /// <returns></returns> 
        public Func<Scoped, object> Create(Type type, HashMap hashmap)
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
                Compile(reg, hashmap);
            }

            // Compile main registation
           return Compile(registration, hashmap);
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Starts a chain of events which leads to an expression and delegate
        /// </summary>
        /// <param name="reg"></param>
        /// <param name="hashmap"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Func<Scoped, object> Compile(Registration reg, HashMap hashmap)
        {
            if (reg.Expression != null)
            {
                //no need to process, already generated an expression
                var d = (Func<Scoped, object>)Expression.Lambda(reg.Expression, Scoped.ScopeParam).CompileFast();

                //store delegate in cache
                hashmap.Emplace(reg.RegisteredType, d);

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
            hashmap.Emplace(reg.RegisteredType, @delegate);

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
                var entries = _registrations.GetAll(parameter.ParameterType);

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
                    var list = _registrations.GetAll(type.GetGenericArguments()[0]).Select(r => r.Expression);

                    yield return Expression.NewArrayInit(type.GetGenericArguments()[0], list);
                    continue;
                }

                if (_registrations.Get(type, out var registration))
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
                    if (_registrations.Get(type.GetGenericTypeDefinition(), out var openGenericRegistration))
                    {
                        var arguments = type.GetGenericArguments();

                        //create new return type
                        var returnType = openGenericRegistration.ReturnType.MakeGenericType(arguments);

                        //create new registration
                        var reg = new Registration(openGenericRegistration.RegisteredType.MakeGenericType(arguments), returnType, openGenericRegistration.Lifetime);

                        //create new closed generic registration - wont harm if there duplicates, wont get saved
                        _registrations.Emplace(type, reg);

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

                foreach (var reg in _registrations.GetAll(type))
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
            if (_registrations.Get(type, out var registration))
            {
                return registration;
            }

            if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
                type.GetGenericTypeDefinition() == typeof(IList<>)))
            {
                //create closed generic reg
                var IenumerableRegistration = new Registration(type, typeof(List<>).MakeGenericType(type.GenericTypeArguments));

                //store closed generic Registration
                _registrations.Emplace(type, IenumerableRegistration);

                //return closed generic
                return IenumerableRegistration;
            }

            // type is closed
            if (type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                //retrieve openGeneric registration
                if (_registrations.Get(type.GetGenericTypeDefinition(), out var openGenericRegistration))
                {
                    //create closed generic reg
                    var closedGenericRegistration = new Registration(type, openGenericRegistration.ReturnType.MakeGenericType(type.GenericTypeArguments), openGenericRegistration.Lifetime);

                    //store closed generic Registration
                    _registrations.Emplace(type, closedGenericRegistration);

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
