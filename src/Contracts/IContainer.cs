using System;
using System.Collections.Generic;
using FastExpressionCompiler.LightExpression;
using Microsoft.Extensions.DependencyInjection;


namespace Faster.Ioc.Contracts
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public interface IContainer
    {
        /// <summary>
        /// Registers the specified interface.
        /// </summary>
        /// <param name="interface">The interface.</param>
        /// <param name="implementation">The implementation.</param>
        /// <returns></returns>
        void Register(Type @interface, Type implementation);

        /// <summary>
        /// Registers the specified interface.
        /// </summary>
        /// <param name="interface">The interface.</param>
        /// <param name="implementation">The implementation.</param>
        /// <param name="lifetime">The lifetime.</param>
        void Register(Type @interface, Type implementation, Lifetime lifetime);

        /// <summary>
        /// Registers the specified interface.
        /// </summary>
        /// <param name="interface">The interface.</param>
        /// <param name="implementation">The implementation.</param>
        /// <param name="key">The key.</param>
        void Register(Type @interface, Type implementation, string key);

        /// <summary>
        /// Registers the specified interface.
        /// </summary>
        /// <param name="interface">The interface.</param>
        /// <param name="implementation">The implementation.</param>
        /// <param name="key">The key.</param>
        /// <param name="lifetime">The lifetime.</param>
        void Register(Type @interface, Type implementation, Lifetime lifetime, string key);

        /// <summary>
        /// Registers the specified key.
        /// </summary>
        /// <typeparam name="TRegistrationType">The type of the registration type.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="lifetime">The lifetime.</param>
        void Register<TRegistrationType>(Lifetime lifetime, string key) where TRegistrationType : class;

        /// <summary>
        /// Registers the specified key.
        /// </summary>
        /// <typeparam name="TRegistrationType">The type of the registration type.</typeparam>
        /// <param name="key">The key.</param>
        void Register<TRegistrationType>(string key) where TRegistrationType : class;

        /// <summary>
        /// Registers the specified key.
        /// </summary>
        /// <typeparam name="TRegistrationType">The type of the registration type.</typeparam>
        /// <param name="lifetime">The lifetime.</param>
        void Register<TRegistrationType>(Lifetime lifetime) where TRegistrationType : class;

        /// <summary>
        /// Registers the specified key.
        /// </summary>
        /// <typeparam name="TRegistrationType">The type of the registration type.</typeparam>
        void Register<TRegistrationType>() where TRegistrationType : class;

        /// <summary>
        /// Registers this instance.
        /// </summary>
        /// <typeparam name="TInterface">The type of the interface.</typeparam>
        /// <typeparam name="TImplementation">The type of the implementation.</typeparam>
        /// <returns></returns>
        void Register<TInterface, TImplementation>() where TImplementation : TInterface;

        /// <summary>
        /// Registers this instance.
        /// </summary>
        /// <typeparam name="TInterface">The type of the interface.</typeparam>
        /// <typeparam name="TImplementation">The type of the implementation.</typeparam>
        /// <returns></returns>
        void Register<TInterface, TImplementation>(string key) where TImplementation : TInterface;

        /// <summary>
        /// Registers this instance.
        /// </summary>
        /// <typeparam name="TInterface">The type of the interface.</typeparam>
        /// <typeparam name="TImplementation">The type of the implementation.</typeparam>
        /// <returns></returns>
        void Register<TInterface, TImplementation>(Lifetime lifetime) where TImplementation : TInterface;

        /// <summary>
        /// Registers this instance.
        /// </summary>
        /// <typeparam name="TInterface">The type of the interface.</typeparam>
        /// <typeparam name="TImplementation">The type of the implementation.</typeparam>
        /// <returns></returns>
        void Register<TInterface, TImplementation>(Lifetime lifetime, string key) where TImplementation : TInterface;

        /// <summary>
        /// Registers the specified object which has already been constructed
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="exp">The object.</param>
        /// <returns></returns>
        void Register<T>(System.Linq.Expressions.Expression<Func<object>> exp) where T : class;

        /// <summary>
        /// Registers the specified object which has already been constructed
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="exp">The object.</param>
        /// <param name="lifetime">The lifetime.</param>
        void Register<T>(System.Linq.Expressions.Expression<Func<object>> exp, Lifetime lifetime) where T : class;

        /// <summary>
        /// Registers the specified object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="exp">The object.</param>
        /// <param name="key">The key.</param>
        void Register<T>(System.Linq.Expressions.Expression<Func<object>> exp, string key) where T : class;

        /// <summary>
        /// Registers the specified object which has already been constructed
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="exp">The object.</param>
        /// <param name="key">The key.</param>
        /// <param name="lifetime">The lifetime.</param>
        void Register<T>(System.Linq.Expressions.Expression<Func<object>> exp, Lifetime lifetime, string key) where T : class;

        /// <summary>
        /// Resolves the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        object Resolve(Type type);

        /// <summary>
        /// Resolves this instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T Resolve<T>();

        /// <summary>
        /// Resolves a specific entry by the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        object Resolve(string key);

        /// <summary>
        /// Resolves the specified service type.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="scoped">The scoped.</param>
        /// <returns></returns>
        object Resolve(Type serviceType, IScoped scoped);

        /// <summary>
        /// Resolves all registered concrete classes
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        IEnumerable<object> ResolveAll<T>();

        /// <summary>
        /// Resolves all registered concrete classes
        /// </summary>
        /// <returns></returns>
        IEnumerable<object> ResolveAll(Type type);

        /// <summary>
        /// Registers the specified object by using overrides, overrides should have a default constructor
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="overrideExp">The object.</param>
        /// <param name="lifetime">The lifetime.</param>
        /// <param name="key">The key.</param>
        void RegisterOverride<T>(System.Linq.Expressions.Expression<Func<object>> overrideExp, Lifetime lifetime, string key) where T : class;

        /// <summary>
        /// Registers the specified object by using overrides, overrides should have a default constructor
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="overrideExp">The object.</param>
        /// <param name="lifetime">The lifetime.</param>
        void RegisterOverride<T>(System.Linq.Expressions.Expression<Func<object>> overrideExp, Lifetime lifetime) where T : class;
        
        /// <summary>
        /// Registers the specified object by using overrides, overrides should have a default constructor
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="overrideExp">The object.</param>
        /// <param name="key">The key.</param>
        void RegisterOverride<T>(System.Linq.Expressions.Expression<Func<object>> overrideExp, string key) where T : class;

        /// <summary>
        /// Registers the specified object by using overrides, overrides should have a default constructor
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="overrideExp">The object.</param>
        void RegisterOverride<T>(System.Linq.Expressions.Expression<Func<object>> overrideExp) where T : class;

        /// <summary>
        /// Registers the service collection.
        /// </summary>
        /// <param name="collection">The collection.</param>
        void RegisterServiceCollection(ServiceCollection collection);
        
        /// <summary>
        /// Creates the scope.
        /// </summary>
        /// <returns></returns>
        IServiceScope CreateScope();
    }
}