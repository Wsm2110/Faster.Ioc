using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Faster.Ioc.Contracts;
using FastExpressionCompiler.LightExpression;
using Microsoft.Extensions.DependencyInjection;

namespace Faster.Ioc
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Faster.Ioc.Contracts.IScoped" />
    /// <seealso cref="Microsoft.Extensions.DependencyInjection.IServiceScope" />
    /// <seealso cref="System.IServiceProvider" />
    /// <seealso cref="System.IDisposable" />
    public sealed class Scoped : IScoped, IServiceProvider, IServiceScope
    {
        #region Statics

        /// <summary>
        /// The scope parameter
        /// </summary>
        public static readonly ParameterExpression ScopeParam = Expression.Parameter(typeof(Scoped), "Scope");
        /// <summary>
        /// The emplace or get scoped instance method
        /// </summary>
        public static readonly MethodInfo EmplaceOrGetScopedInstanceMethod = typeof(Scoped).GetMethods().Single(x => x.Name == "EmplaceOrGetScopedInstance");
        /// <summary>
        /// The add method information
        /// </summary>
        public static readonly MethodInfo AddMethodInfo = typeof(List<>).MakeGenericType(typeof(IDisposable)).GetMethod("Add");

        /// <summary>
        /// The contains method information
        /// </summary>
        public static readonly MethodInfo ContainsMethodInfo = typeof(List<>).MakeGenericType(typeof(IDisposable)).GetMethod("Contains");

        /// <summary>
        /// The dispose property information
        /// </summary>
        public static readonly PropertyInfo DisposePropertyInfo = typeof(Scoped).GetProperties().Single(x => x.Name == "Disposeables");


        #endregion
        
        #region Fields

        private object[] _scopedInstances = new object[ScopeCount];
        private bool _disposed;
        private readonly IContainer _container;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the scope counter.
        /// </summary>
        /// <value>
        /// The scope counter.
        /// </value>
        public static int ScopeIndex
        {
            get => _scopeIndex;
            set => _scopeIndex = Interlocked.Add(ref value, 0);
        }

        private static int _scopeIndex;

        /// <summary>
        /// Gets or sets the scope counter.
        /// </summary>
        /// <value>
        /// The scope counter.
        /// </value>
        public static int ScopeCount
        {
            get => _scopeCount;
            set
            {
                _scopeCount = Interlocked.Add(ref value, 0);
                Container.ContainerScope.Resize();
            }
        }

        private static int _scopeCount;
        
        /// <summary>
        /// Gets or sets the disposeables.
        /// </summary>
        /// <value>
        /// The disposeables.
        /// </value>
        public List<IDisposable> Disposeables { get; set; } = new();

        /// <summary>
        /// Gets or sets the service provider.
        /// </summary>
        /// <value>
        /// The service provider.
        /// </value>
        public IServiceProvider ServiceProvider { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="Scoped"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        public Scoped(IContainer container)
        {
            ServiceProvider = this;
            _container = container;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the service object of the specified type.
        /// </summary>
        /// <param name="serviceType">An object that specifies the type of service object to get.</param>
        /// <returns>
        /// A service object of type <paramref name="serviceType" />.-or- null if there is no service object of type <paramref name="serviceType" />.
        /// </returns>

        [MethodImpl(256)]
        public object GetService(Type serviceType)
        {
            return _container.Resolve(serviceType, this);
        }

        /// <summary>
        /// Emplaces the specified delegate.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        [MethodImpl(256)]
        public object EmplaceOrGetScopedInstance(Func<object> obj, int index)
        {
            var instance = _scopedInstances[index];
            if (instance != null)
            {
                return instance;
            }

            var i = obj();
            _scopedInstances[index] = i;

            return i;
        }

        /// <summary>
        /// Resizes this instance.
        /// </summary>
        private void Resize()
        {
            var copy = new object[ScopeCount];

            Array.Copy(_scopedInstances, copy, 0);

            _scopedInstances = copy;
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    for (var index = 0; index < Disposeables.Count; ++index)
                    {
                        var instance = Disposeables[index];
                        instance?.Dispose();
                    }
                }

                _scopedInstances = null;
                Disposeables = null;
            }

            _disposed = true;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(true);
        }

        #endregion
    }
}