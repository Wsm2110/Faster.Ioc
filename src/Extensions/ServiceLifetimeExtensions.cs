using System;
using Faster.Ioc.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Faster.Ioc.Extensions
{
    /// <summary>
    /// 
    /// </summary>
    public static class ServiceLifetimeExtensions
    {
        /// <summary>
        /// Converts the specified lifetime.
        /// </summary>
        /// <param name="lifetime">The lifetime.</param>
        /// <returns></returns>
        public static Lifetime Convert(this ServiceLifetime lifetime)
        {
            if (lifetime == ServiceLifetime.Scoped)
            {
                return Lifetime.Scoped;
            }

            if (lifetime == ServiceLifetime.Singleton)
            {
                return Lifetime.Singleton;
            }

            return Lifetime.Transient;

        }

    }
}
