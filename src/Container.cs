using System;
using System.Runtime.CompilerServices;
using Faster.Ioc.Collections;
using Faster.Ioc.Contracts;
using Faster.Ioc.Extensions;
using Faster.Map;
using Microsoft.Extensions.DependencyInjection;
using FastExpressionCompiler.LightExpression;
using System.Collections.Generic;
using Faster.Ioc.Comparer;

namespace Faster.Ioc
{
    /// <summary>
    /// Minimalistic ioc container with incredible speed 
    /// </summary>
    /// <seealso cref="IContainer" />
    public sealed class Container : IContainer
    {
        #region Fields

        private MultiMap<Type, Registration> _registrations;
        private readonly FastMap<int, Func<Scoped, object>> _keyCache = new FastMap<int, Func<Scoped, object>>();
        private readonly ExpressionGenerator _generator;
        private readonly HashMap _delegates;
        private bool _disposed;

        #endregion

        #region Properties

        internal static Scoped ContainerScope { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="Container"/> class.
        /// </summary>
        public Container()
        {          
            _registrations = new MultiMap<Type, Registration>(64, 0.5, EqualityComparer<Type>.Default, new RegistrationEqualityComparer());
            _generator = new ExpressionGenerator(_registrations);
            _delegates = new HashMap(64, 0.5, _generator);
            ContainerScope = new Scoped(this);
        }

        /// <summary>
        /// Constructor used to create childcontrainers
        /// </summary>
        /// <param name="registrations"></param>
        public Container(MultiMap<Type, Registration> registrations)
        {
            _registrations = registrations;
            _generator = new ExpressionGenerator(_registrations);
            _delegates = new HashMap(64, 0.6, _generator);
            ContainerScope = new Scoped(this);
        }

        #endregion

        #region Registration Methods

        /// <summary>
        /// Registers the service collection.
        /// </summary>
        /// <param name="collection">The collection.</param>
        public void RegisterServiceCollection(ServiceCollection collection)
        {
            var containerRegistration = new Registration(typeof(IContainer), typeof(Container), Lifetime.Singleton)
            {
                Expression = Expression.Constant(this)
            };

            _registrations.Emplace(typeof(IContainer), containerRegistration);

            var serviceScopeFactory = new Registration(typeof(IServiceScopeFactory), typeof(ScopeFactory), Lifetime.Singleton);

            _registrations.Emplace(typeof(IServiceScopeFactory), serviceScopeFactory);

            foreach (var item in collection)
            {
                var lifetime = item.Lifetime.Convert();
                var registeredType = item.ServiceType;
                var returnType = item.ImplementationType;

                Register(registeredType, returnType, lifetime);
            }
        }

        /// <summary>
        /// Registers the specified interface.
        /// </summary>
        /// <param name="interface">The interface.</param>
        /// <param name="implementation">The implementation.</param>
        public void Register(Type @interface, Type implementation)
        {
            var registration = new Registration(@interface, implementation);
            _registrations.Emplace(registration.RegisteredType, registration);
        }

        /// <summary>
        /// Registers the specified interface with Lifetime scope
        /// </summary>
        /// <param name="interface">The interface.</param>
        /// <param name="implementation">The implementation.</param>
        /// <param name="lifetime">The Lifetime.</param>
        public void Register(Type @interface, Type implementation, Lifetime lifetime)
        {
            var registration = new Registration(@interface, implementation, lifetime);
            _registrations.Emplace(registration.RegisteredType, registration);
        }

        /// <summary>
        /// Registers the specified interface with Lifetime and key
        /// </summary>
        /// <param name="interface">The interface.</param>
        /// <param name="implementation">The implementation.</param>
        /// <param name="key">The key.</param>
        public void Register(Type @interface, Type implementation, string key)
        {
            var registration = new Registration(@interface, implementation, Lifetime.Transient, key.GetHashCode());
            _registrations.Emplace(registration.RegisteredType, registration);
        }

        /// <summary>
        /// Registers the specified interface.
        /// </summary>
        /// <param name="interface">The interface.</param>
        /// <param name="implementation">The implementation.</param>
        /// <param name="lifetime">The Lifetime.</param>
        /// <param name="key">The key.</param>
        public void Register(Type @interface, Type implementation, Lifetime lifetime, string key)
        {
            var registration = new Registration(@interface, implementation, lifetime, key.GetHashCode());
            _registrations.Emplace(registration.RegisteredType, registration);
        }

        /// <summary>
        /// Registers this instance.
        /// </summary>
        /// <typeparam name="TRegistrationType">The type of the registration type.</typeparam>
        public void Register<TRegistrationType>() where TRegistrationType : class
        {
            var registration = new Registration(typeof(TRegistrationType), typeof(TRegistrationType), Lifetime.Transient);
            _registrations.Emplace(registration.RegisteredType, registration);
        }

        /// <summary>
        /// Registers a concrete type with Lifetime scope
        /// </summary>
        /// <typeparam name="TRegistrationType">The type of the registration type.</typeparam>
        /// <param name="lifetime">The Lifetime.</param>
        public void Register<TRegistrationType>(Lifetime lifetime) where TRegistrationType : class
        {
            var registration = new Registration(typeof(TRegistrationType), typeof(TRegistrationType), lifetime);
            _registrations.Emplace(registration.RegisteredType, registration);
        }

        /// <summary>
        /// Registers a concrete type with key
        /// </summary>
        /// <typeparam name="TRegistrationType">The type of the registration type.</typeparam>
        /// <param name="key">The key.</param>
        public void Register<TRegistrationType>(string key) where TRegistrationType : class
        {
            var registration = new Registration(typeof(TRegistrationType), typeof(TRegistrationType), Lifetime.Transient, key.GetHashCode());
            _registrations.Emplace(registration.RegisteredType, registration);
        }

        /// <summary>
        /// Registers a concrete type with Lifetimescope and key
        /// </summary>
        /// <typeparam name="TRegistrationType">The type of the registration type.</typeparam>
        /// <param name="lifetime">The Lifetime.</param>
        /// <param name="key">The key.</param>
        public void Register<TRegistrationType>(Lifetime lifetime, string key) where TRegistrationType : class
        {
            var registration = new Registration(typeof(TRegistrationType), typeof(TRegistrationType), lifetime, key.GetHashCode());
            _registrations.Emplace(registration.RegisteredType, registration);
        }

        /// <summary>
        /// Registers an implementation type for the specified interface
        /// </summary>
        /// <typeparam name="TInterface">RegisteredType to register</typeparam>
        /// <typeparam name="TImplementation">Implementing type</typeparam>
        /// <returns>IRegisteredType object</returns>
        public void Register<TInterface, TImplementation>() where TImplementation : TInterface
        {
            var registration = new Registration(typeof(TInterface), typeof(TImplementation), Lifetime.Transient);
            _registrations.Emplace(registration.RegisteredType, registration);
        }

        /// <summary>
        /// Registers an implementation type for the specified interface
        /// </summary>
        /// <typeparam name="TInterface">RegisteredType to register</typeparam>
        /// <typeparam name="TImplementation">Implementing type</typeparam>
        /// <returns>IRegisteredType object</returns>
        public void Register<TInterface, TImplementation>(string key) where TImplementation : TInterface
        {
            var registration = new Registration(typeof(TInterface), typeof(TImplementation), Lifetime.Transient, key.GetHashCode());
            _registrations.Emplace(registration.RegisteredType, registration);
        }

        /// <summary>
        /// Registers an implementation type for the specified interface
        /// </summary>
        /// <typeparam name="TInterface">RegisteredType to register</typeparam>
        /// <typeparam name="TImplementation">Implementing type</typeparam>
        /// <returns>IRegisteredType object</returns>
        public void Register<TInterface, TImplementation>(Lifetime lifetime) where TImplementation : TInterface
        {
            var registration = new Registration(typeof(TInterface), typeof(TImplementation), lifetime);
            _registrations.Emplace(registration.RegisteredType, registration);
        }

        /// <summary>
        /// Registers an implementation type for the specified interface
        /// </summary>
        /// <typeparam name="TInterface">RegisteredType to register</typeparam>
        /// <typeparam name="TImplementation">Implementing type</typeparam>
        /// <param name="lifetime"></param>
        /// <param name="key"></param>
        public void Register<TInterface, TImplementation>(Lifetime lifetime, string key) where TImplementation : TInterface
        {
            var registration = new Registration(typeof(TInterface), typeof(TImplementation), lifetime, key.GetHashCode());
            _registrations.Emplace(registration.RegisteredType, registration);
        }

        /// <summary>
        /// Registers the specified implementation using an expression.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="exp">The creator.</param>
        /// <param name="lifetime">The lifetime.</param>
        /// <param name="key">The key.</param>
        public void Register<T>(System.Linq.Expressions.Expression<Func<object>> exp, Lifetime lifetime, string key) where T : class
        {
            var registration = new Registration(typeof(T), exp.Body.Type, lifetime, key.GetHashCode())
            {
                OverrideExpression = exp
            };

            _registrations.Emplace(registration.RegisteredType, registration);
        }

        /// <summary>
        /// Registers the specified implementation using an expression.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="exp">The creator.</param>
        /// <param name="key">The key.</param>
        public void Register<T>(System.Linq.Expressions.Expression<Func<object>> exp, string key) where T : class
        {
            var registration = new Registration(typeof(T), exp.Body.Type, Lifetime.Transient, key.GetHashCode())
            {
                OverrideExpression = exp
            };

            _registrations.Emplace(registration.RegisteredType, registration);
        }

        /// <summary>
        /// Registers the specified implementation using an expression.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="exp">The creator.</param>
        /// <param name="lifetime">The lifetime.</param>
        public void Register<T>(System.Linq.Expressions.Expression<Func<object>> exp, Lifetime lifetime) where T : class
        {
            var registration = new Registration(typeof(T), exp.Body.Type, lifetime)
            {
                OverrideExpression = exp
            };

            _registrations.Emplace(registration.RegisteredType, registration);
        }

        /// <summary>
        /// Registers the specified implementation using an expression.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="exp">The creator.</param>
        public void Register<T>(System.Linq.Expressions.Expression<Func<object>> exp) where T : class
        {
            var registration = new Registration(typeof(T), exp.Body.Type, Lifetime.Transient)
            {
                OverrideExpression = exp
            };

            _registrations.Emplace(registration.RegisteredType, registration);
        }

        // <summary>
        // Registers an object which has overrides. Register where T should be an interface and shouldn't be left out eventhough resharper says so....
        // (if there are multiple concrete classes per interface, without overriding it will always resolve the first registered object.
        // By using an override you can manage what type per interface you want to inject) i.e
        //
        // Valid
        // Register<IMock/>(factory => new Mock())
        //
        // Not Valid
        // Register(factory => new Mock())
        // </summary>
        /// <summary>
        /// Registers the specified override.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="overrideExp">The override.</param>
        /// <param name="lifetime">The Lifetime.</param>
        /// <param name="key">The key.</param>
        public void RegisterOverride<T>(System.Linq.Expressions.Expression<Func<object>> overrideExp, Lifetime lifetime, string key) where T : class
        {
            var registration = new Registration(typeof(T), overrideExp.Body.Type, lifetime, key.GetHashCode())
            {
                OverrideExpression = overrideExp
            };

            _registrations.Emplace(registration.RegisteredType, registration);
        }

        /// <summary>
        /// Registers the override.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="overrideExp">The override.</param>
        /// <param name="key">The key.</param>
        public void RegisterOverride<T>(System.Linq.Expressions.Expression<Func<object>> overrideExp, string key) where T : class
        {
            var registration = new Registration(typeof(T), overrideExp.Body.Type, Lifetime.Transient, key.GetHashCode())
            {
                OverrideExpression = overrideExp
            };

            _registrations.Emplace(registration.RegisteredType, registration);
        }

        /// <summary>
        /// Registers the override.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="overrideExp">The override.</param>
        /// <param name="lifetime">The lifetime.</param>
        public void RegisterOverride<T>(System.Linq.Expressions.Expression<Func<object>> overrideExp, Lifetime lifetime) where T : class
        {
            var registration = new Registration(typeof(T), overrideExp.Body.Type, lifetime)
            {
                OverrideExpression = overrideExp
            };

            _registrations.Emplace(registration.RegisteredType, registration);
        }

        /// <summary>
        /// Registers the override.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="overrideExp">The override.</param>
        public void RegisterOverride<T>(System.Linq.Expressions.Expression<Func<object>> overrideExp) where T : class
        {
            var registration = new Registration(typeof(T), overrideExp.Body.Type, Lifetime.Transient)
            {
                OverrideExpression = overrideExp
            };

            _registrations.Emplace(registration.RegisteredType, registration);
        }

        #endregion

        #region Methods

        [MethodImpl(256)]
        public object GetService(Type serviceType)
        {
            return Resolve(serviceType);
        }

        /// <summary>
        /// Resolves the specified type.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="scoped">The scope.</param>
        /// <returns></returns>
        [MethodImpl(256)]
        public object Resolve(Type serviceType, IScoped scoped)
        {
            return _delegates.Get(serviceType)((Scoped)scoped);
        }

        /// <summary>
        /// Returns an implementation of the specified interface
        /// </summary>
        /// <typeparam name="T">RegisteredType type</typeparam>
        /// <returns>Object implementing the interface</returns>
        [MethodImpl(256)]
        public T Resolve<T>() => (T)_delegates.Get(typeof(T))(ContainerScope);

        /// <summary>
        /// Returns an implementation of the specified interface
        /// </summary>
        /// <returns>Object implementing the interface</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object Resolve(Type type) => _delegates.Get(type)(ContainerScope);

        /// <summary>
        /// Resolves a specific entry by the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        [MethodImpl(256)]
        public object Resolve(string key)
        {
            var hashcode = key.GetHashCode();
            if (_keyCache.Get(hashcode, out var result))
            {
                return result(ContainerScope);
            }

            //loop registrations hoping we find a matching hashcode..
            foreach (var value in _registrations.Values)
            {
                if (value.HashCode == hashcode)
                {
                    var @delegate = _generator.Create(value.RegisteredType, _delegates);

                    _keyCache.Emplace(value.HashCode, @delegate);
                    return @delegate(ContainerScope);
                }
            }

            return default;
        }

        /// <summary>
        /// Creates the scope.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(256)]
        public IServiceScope CreateScope() => new Scoped(this);
     
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IContainer CreateChildContainer() =>  new Container(_registrations);

        #endregion

        #region IDisposable
        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {                   
                    ContainerScope.Dispose();
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}