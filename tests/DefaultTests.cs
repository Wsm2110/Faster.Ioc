using System;
using System.Collections.Generic;
using System.Linq;
using Faster.Ioc.Contracts;
using Faster.Ioc.Tests.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Faster.Ioc.Tests
{
    [TestClass]
    public class UnitTest1
    {


        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void AssertCircularTwo()
        {
            Container container = new Container();
            container.Register(typeof(ISingletonCircularOne), typeof(SingletonCircularOne), Lifetime.Transient);

            container.Register(typeof(ISingletonCircularTwo), typeof(SingletonCircularTwo), Lifetime.Transient);

            container.Register(typeof(ISingletonCircularThree), typeof(SingletonCircularTree), Lifetime.Transient);

            container.Register(typeof(ISingletonCircularFour), typeof(SingletonCircularFour), Lifetime.Transient);

            container.Register(typeof(ISingletonCircularFive), typeof(SingletonCircularFive), Lifetime.Transient);

            var c1 = container.Resolve(typeof(ISingletonCircularOne));

            Assert.IsTrue(c1 != null);
        }

        /// <summary>
        /// Asserts the creation of singleton.
        /// </summary>
        [TestMethod]
        public void AssertCreationOfSingleton()
        {
            using (var container = new Container())
            {
                container.Register<IConcreteInterface, ConcreteA>(Lifetime.Singleton);
            
                var singleton1 = container.Resolve<IConcreteInterface>();
                var singleton2 = container.Resolve<IConcreteInterface>();

                Assert.IsTrue(singleton2 == singleton1);
            }
        }

        [TestMethod]
        public void AssertCreatingMultipleAndReturnList()
        {
            using (var container = new Container())
            {
                container.Register<IConcreteInterface>(() => new ConcreteA(), Lifetime.Singleton);
                container.Register<IConcreteInterface>(() => new ConcreteB(), Lifetime.Singleton);

                var items = container.Resolve<IList<IConcreteInterface>>().ToList();

                Assert.IsTrue(items.Count == 2);
            }
        }

        [TestMethod]
        public void AssertCreatingMultipleAndReturnIEnumerable()
        {
            using (var container = new Container())
            {
                container.Register<IConcreteInterface>(() => new ConcreteA(), Lifetime.Singleton);
                container.Register<IConcreteInterface>(() => new ConcreteB(), Lifetime.Singleton);

                var items = container.Resolve<IEnumerable<IConcreteInterface>>().ToList();

                Assert.IsTrue(items.Count == 2);
            }
        }

        [TestMethod]
        public void AssertCreatingMultipleWithoutDelegateLateBinding()
        {
            using (var container = new Container())
            {
                container.Register<IConcreteInterface, ConcreteE>(Lifetime.Singleton);
                container.Register<IConcreteInterface, ConcreteA>(Lifetime.Singleton);
                container.Register<IConcreteInterface, ConcreteB>(Lifetime.Singleton);

                //param for concreteE
                container.Register<ITestData, ConcreteD>(Lifetime.Singleton);

                var items = container.Resolve<IList<IConcreteInterface>>();

                Assert.IsTrue(items.Count == 3);
            }
        }

        [TestMethod]
        public void AssertCreatingWithParamCollection()
        {
            using (var container = new Container())
            {
                //Register items to be used as a collection
                container.Register<IConcreteInterface, ConcreteB>(Lifetime.Singleton);
                container.Register<IConcreteInterface, ConcreteA>(Lifetime.Singleton);

                //register entry with late binding, meaning one of the params in the constructor hasnt been registered
                container.Register<TestDataTwo, TestDataTwo>(Lifetime.Singleton);

                //register late binding
                container.Register<ITestData, ConcreteD>(Lifetime.Singleton);

                //Resolve
                var items = container.Resolve<TestDataTwo>();

                //Assert
                Assert.IsTrue(items.ConcreteInterfaces.Count() == 2);
                Assert.IsTrue(items.TestData != null);
            }
        }

        /// <summary>
        /// Asserts the creation of singleton.
        /// </summary>
        [TestMethod]
        public void AssertCreationOfSingletonByUsingAnExpression()
        {
            using (var container = new Container())
            {
                container.Register<IConcreteInterface>(() => new ConcreteA(), Lifetime.Singleton);

                var singleton1 = container.Resolve<IConcreteInterface>();
                var singleton2 = container.Resolve<IConcreteInterface>();

                Assert.IsTrue(singleton2 == singleton1);
            }
        }

        [TestMethod]
        public void AssertGenericCreationTransient()
        {
            using (var container = new Container())
            {
                container.Register<IConcreteInterface, ConcreteA>();

                var transient = container.Resolve<IConcreteInterface>();
                var transient2 = container.Resolve<IConcreteInterface>();

                Assert.IsTrue(transient != transient2);
            }
        }

        [TestMethod]
        public void AssertCreationTransient()
        {
            using (var container = new Container())
            {
                container.Register<IConcreteInterface, ConcreteA>();

                var transient = container.Resolve(typeof(IConcreteInterface));
                var transient2 = container.Resolve(typeof(IConcreteInterface));

                Assert.IsTrue(transient != transient2);
            }
        }

        [TestMethod]
        public void AssertResolveConstructorInjectsTypes()
        {
            using (var container = new Container())
            {
                container.Register<IConcreteInterface, ConcreteA>(Lifetime.Singleton);
                container.Register<ITestData, ConcreteD>(Lifetime.Singleton);
                container.Register<ConcreteC, ConcreteC>(Lifetime.Singleton);

                var x = container.Resolve<ConcreteC>();
                Assert.IsTrue(x.ConcreteType != null);
            }
        }

        [TestMethod]
        public void AssertConstructorWithLargestparamCountisBeingTargetted()
        {
            using (var container = new Container())
            {
                container.Register<IConcreteInterface, ConcreteA>(Lifetime.Singleton);
                container.Register<ITestData, ConcreteD>(Lifetime.Singleton);
                container.Register<ConcreteC, ConcreteC>(Lifetime.Singleton);

                var a = container.Resolve<ConcreteC>();

                Assert.IsNotNull(a.Data);
                Assert.IsNotNull(a.ConcreteType);
            }
        }

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
        public void AssertResolvingObjectWithOneOfMoreParameters()
        {
            using (var container = new Container())
            {
                container.Register<ILocatableDummy, LocatableDummy>(Lifetime.Singleton);
                container.Register<IConcreteInterface, ConcreteA>(Lifetime.Singleton);
                var a = container.Resolve<ILocatableDummy>();
                Assert.IsNotNull(a);
            }
        }


        [TestMethod]
        public void AssertIISsue()
        {
            using (var container = new Container())
            {
                container.Register<IConcreteInterface, ConcreteA>(Lifetime.Singleton);
                container.Register<ConcreteC, ConcreteC>(Lifetime.Singleton);
                container.Register<ITestData, ConcreteD>(Lifetime.Singleton);
                var a = container.Resolve<ConcreteC>();
                Assert.IsNotNull(a);

                var b = container.Resolve<ConcreteC>();
                Assert.AreEqual(a, b);

                Assert.AreEqual(a.Data, b.Data);
                Assert.AreEqual(a.ConcreteType, b.ConcreteType);

                var bb = container.Resolve<IConcreteInterface>();
                var x = a.ConcreteType == bb;
            }
        }

    }
}
