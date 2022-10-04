using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Faster.Ioc.Contracts;
using Faster.Ioc.Tests.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Faster.Ioc.Tests
{

    [TestClass]
    public class EnumerableTests
    {
        [TestMethod]
        public void AssertRetrievalOfDataWithIEnumerable()
        {
            using (var container = new Container())
            {
                //arrange
                container.Register<EnumerableTestData, EnumerableTestData>(Lifetime.Singleton);
                container.Register<IEnumerableTestData, EnumerableTestDataOne>(Lifetime.Singleton);
                container.Register<IEnumerableTestData, EnumerableTestDataTwo>(Lifetime.Singleton);
                container.Register<IEnumerableTestData, EnumerableTestDataThree>(Lifetime.Singleton);
                
                //act
                var a = (EnumerableTestData)container.Resolve(typeof(EnumerableTestData));

                //assert
                Assert.IsNotNull(a);
                Assert.IsInstanceOfType(a.Data, typeof(IEnumerable<IEnumerableTestData>));
                Assert.AreEqual(3, a.Data.Count());
            }
        }


    }
}
