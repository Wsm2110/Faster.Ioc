using System;

namespace Faster.Ioc.Tests.Data
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public class DisposeableOne : IDisposeableOne, IDisposable
    {
        private bool _disposed;

        private void Dispose(bool disposing)
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
        }
    }

    public class DisposeableTwo : IDisposeableTwo, IDisposable
    {
        private bool _disposed;

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
        }
    }

    public class DisposaebleThree : IDisposeableThree, IDisposable
    {
        private bool _disposed;

        private void Dispose(bool disposing)
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
        }
    }

    public class DisposeableFour : IDisposable
    {
        public bool Disposed { get; set; }

        public void Dispose()
        {
            Dispose(true);
        }

        protected void Dispose(bool disposing)
        {
            Disposed = true;
        }
    }
    
    public sealed class DisposeableChild : DisposeableTwo
    {

        public bool Disposed { get; set; }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }

    public sealed class Child : DisposeableFour
    {
    }



    public interface IDisposeableOne
    {
    }

    public interface IDisposeableTwo
    {
    }

    public interface IDisposeableThree
    {
    }

}
