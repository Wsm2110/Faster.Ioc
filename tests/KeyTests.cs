using Faster.Ioc.Contracts;
using Faster.Ioc.Tests.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Faster.Ioc.Tests
{
    [TestClass]
    public class KeyTests
    {
        [TestMethod]
        public void AssertRetrievalByKey()
        {
            using (var container = new Container())
            {
                container.Register<IConcreteInterface, ConcreteA>(Lifetime.Singleton, "ConcreteA");

                var a = container.Resolve(nameof(ConcreteA));

                Assert.IsNotNull(a);
            }
        }

        [TestMethod]
        public void AssertRetrievalOfNotExistingKey()
        {
            using (var container = new Container())
            {
                container.Register<IConcreteInterface, ConcreteA>(Lifetime.Singleton, "Test");

                var a = container.Resolve(nameof(ConcreteA));

                Assert.IsNull(a);
            }
        }

    }
}
