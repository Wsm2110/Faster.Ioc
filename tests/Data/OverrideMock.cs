using System;
using System.Collections.Generic;

namespace Faster.Ioc.Tests.Data
{
    public class OverrideMock : IMock
    {
        public readonly IConcreteInterface _concreteB;

        public OverrideMock(IConcreteInterface concreteB)
        {
            _concreteB = concreteB;
        }
    }

    public class OverrideMockDispose : IMock, IDisposable
    {
        public readonly IConcreteInterface ConcreteB;
        private bool _disposed;

        public OverrideMockDispose(IConcreteInterface concreteB)
        {
            ConcreteB = concreteB;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    //
                }

                _disposed = true;
            }
        }


        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }


    public class MockCollection : IMock
    {
        public ICollection<IConcreteInterface> _concretes { get; set; }

        public MockCollection(ICollection<IConcreteInterface> concretes)
        {
            _concretes = concretes;
        }
    }

    public interface IMock
    {
    }
}
