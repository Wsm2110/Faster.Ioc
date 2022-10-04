using System;
using System.Collections.Generic;

namespace Faster.Ioc.Contracts
{
    /// <summary>
    /// 
    /// </summary>
    public interface IScoped
    {
        /// <summary>
        /// Gets or sets the disposeables.
        /// </summary>
        /// <value>
        /// The disposeables.
        /// </value>
        List<IDisposable> Disposeables { get; set; }
        /// <summary>
        /// Gets or sets the service provider.
        /// </summary>
        /// <value>
        /// The service provider.
        /// </value>
        IServiceProvider ServiceProvider { get; set; }

        /// <summary>
        /// Gets the service.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <returns></returns>
        object GetService(Type serviceType);

        /// <summary>
        /// Emplaces the or get scoped instance.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        object EmplaceOrGetScopedInstance(Func<object> obj, int index);

    }
}
