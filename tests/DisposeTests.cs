using Faster.Ioc.Tests.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Faster.Ioc.Tests
{
    [TestClass]
    public class DisposeTests
    {
        [TestMethod]
        public void AssertClassWithParentImplementingIDisposableResultsIParentAndChildCleanup()
        {
            //assign
            DisposeableChild child = null;
            using (var container = new Container())
            {
                container.Register<DisposeableChild, DisposeableChild>();

                //act 
                child = container.Resolve<DisposeableChild>();
                Assert.IsInstanceOfType(child, typeof(DisposeableChild));
            }

            Assert.AreEqual(true, child.Disposed);
        }

        [TestMethod]
        public void AssertClassWithParentImplementingIdisposableResultsInParentCleanup()
        {
            //assign
            Child child = null;
            using (var container = new Container())
            {
                container.Register<Child, Child>();

                //act 
                child = container.Resolve<Child>();
                Assert.IsInstanceOfType(child, typeof(Child));
            }

            Assert.AreEqual(true, child.Disposed);
        }
    }
}
