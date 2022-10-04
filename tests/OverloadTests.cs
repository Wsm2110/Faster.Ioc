using System;
using Faster.Ioc.Contracts;
using Faster.Ioc.Tests.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Faster.Ioc.Tests
{
    [TestClass]
    public class OverloadTests
    {
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void AssertRegisteringObjectWithOverloadWithoutOverloadRegistered()
        {
            using (var container = new Container())
            {
                //assign
                container.Register<IConcreteInterface>(() => new ConcreteA(), Lifetime.Singleton);
                //concrete B is not registered, will throw
                container.RegisterOverride<IOverloadDataOne>(() => new OverloadDataOne(new ConcreteB()), Lifetime.Singleton);

                //act
                var a = container.Resolve<IOverloadDataOne>();

                //assert in expectedException
            }
        }

        [TestMethod]
        public void AssertRegisteringObjectWithOverride()
        {
            //assign
            using (var container = new Container())
            {
                container.Register<IConcreteInterface>(() => new ConcreteA(), Lifetime.Singleton);
                container.Register<IConcreteInterface>(() => new ConcreteB(), Lifetime.Singleton);

                //Normally OverrideMock would have a ConcreteA implementation (registered first), but due to the override we can tell the overridemocke which type to implement
                container.RegisterOverride<IMock>(() => new OverrideMock(new ConcreteB()), Lifetime.Singleton, string.Empty);

                //act
                var singleton1 = container.Resolve<IMock>();
                var singleton2 = container.Resolve<IMock>();


                //assert
                Assert.IsTrue(singleton2 == singleton1);
            }
        }

        [TestMethod]
        public void AssertRegisteringObjectWithOverrideAndDispose()
        {
            //assign
            var container = new Container();

            container.Register<IConcreteInterface>(() => new ConcreteA(), Lifetime.Singleton);
            container.Register<IConcreteInterface>(() => new ConcreteB(), Lifetime.Singleton);

            //Normally OverrideMock would have a ConcreteA implementation, but due to the override we can tell the overridemock which type to implement
            container.RegisterOverride<IMock>(() => new OverrideMockDispose(new ConcreteB()), Lifetime.Singleton);

            //act
            var singleton1 = container.Resolve<IMock>();
            var singleton2 = container.Resolve<IMock>();

            Assert.IsTrue(singleton2 == singleton1);

            //assert
            container.Dispose();

            PrivateObject<OverrideMockDispose> po2 = new PrivateObject<OverrideMockDispose>(singleton1);
            var result2 = po2.GetField<bool>("_disposed");
            Assert.IsTrue(result2);
        }

        [TestMethod]

        public void AssertRetrievingOverloadSetsProperParameters()
        {
            using (var container = new Container())
            {
                //assign
                container.Register<IConcreteInterface>(() => new ConcreteA(), Lifetime.Singleton);
                container.Register<IConcreteInterface>(() => new ConcreteB(), Lifetime.Singleton);

                container.RegisterOverride<IOverloadDataOne>(() => new OverloadDataOne(new ConcreteB()), Lifetime.Singleton);

                //act
                var a = container.Resolve<IOverloadDataOne>();

                //assert
                Assert.IsInstanceOfType(a, typeof(IOverloadDataOne));
                Assert.IsInstanceOfType(a.ConcreteInterface, typeof(ConcreteB));
            }
        }

        [TestMethod]
        public void AssertOverrideByKey()
        {
            //assign
            using (var container = new Container())
            {
                container.Register<IConcreteInterface>(() => new ConcreteA(), Lifetime.Singleton);
                container.Register<IConcreteInterface>(() => new ConcreteB(), Lifetime.Singleton);

                //Normally OverrideMock would have a ConcreteA implementation, but due to the override we can tell the overridemock which type to implement
                container.RegisterOverride<IMock>(() => new OverrideMock(new ConcreteB()), Lifetime.Singleton, "abc");

                //act
                var singleton1 = container.Resolve("abc");

                //assert
                Assert.IsTrue(singleton1.GetType() == typeof(OverrideMock));
            }
        }

        [TestMethod]
        public void AssertResolvingTransientOverrideByKeyResultsInTwoTransientObjects()
        {
            //assign
            using (var container = new Container())
            {
                container.Register<IConcreteInterface>(() => new ConcreteA(), Lifetime.Transient);
                container.Register<IConcreteInterface>(() => new ConcreteB(), Lifetime.Transient);

                //Normally OverrideMock would have a ConcreteA implementation, but due to the override we can tell the overridemock which type to implement
                container.RegisterOverride<IMock>(() => new OverrideMock(new ConcreteB()), Lifetime.Transient, "abc");

                //act
                var singleton1 = container.Resolve("abc");

                //assert
                Assert.IsTrue(singleton1.GetType() == typeof(OverrideMock));

                var singleton2 = container.Resolve("abc");

                Assert.AreNotEqual(singleton1, singleton2);
            }
        }

    }
}
