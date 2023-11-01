using System.Runtime.CompilerServices;
using Faster.Ioc.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Faster.Ioc.Factory
{
    public class ScopeFactory : IServiceScopeFactory
    {
        private readonly IContainer _container;

        public ScopeFactory(IContainer container)
        {
            _container = container;
        }

        [MethodImpl(256)]
        public IServiceScope CreateScope()
        {
            return _container.CreateScope();
        }
    }
}
