using System;
using Faster.Ioc.Contracts;
using Faster.Ioc.Models;
using Faster.Ioc.Tests.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Faster.Ioc.Tests
{
    [TestClass]
    public class ScopedLifetimeTests
    {
        [TestMethod]
        public void AssertResolvingScopedLifetimeResultsInTheSameObject()
        {
            //Assign
            Container container = new Container();

            container.Register<IDisposeableOne>(() => new DisposeableOne(), Lifetime.Scoped);

            using (var factory = container.CreateScope())
            {
                //Act
                var instance = factory.ServiceProvider.GetService(typeof(IDisposeableOne));
                var instance2 = factory.ServiceProvider.GetService(typeof(IDisposeableOne));

                //Assert
                Assert.IsTrue(instance == instance2);
            }
        }

        [TestMethod]
        public void AssertResolvingTransientLifetimeResultsInTwoObject()
        {
            //Assign
            Container container = new Container();

            container.Register<IDisposeableOne>(() => new DisposeableOne(), Lifetime.Transient);

            using (var factory = container.CreateScope())
            {
                //Act
                var instance = factory.ServiceProvider.GetService(typeof(IDisposeableOne));
                var instance2 = factory.ServiceProvider.GetService(typeof(IDisposeableOne));

                //Assert
                Assert.IsTrue(instance != instance2);
            }
        }
        
        [TestMethod]
        public void AssertResolvingTransientViaScopeFactoryResultsToIDispoaseableAddedToScopedSession()
        {
            //Assign
            Container container = new Container();

            container.Register<IDisposeableOne>(() => new DisposeableOne(), Lifetime.Transient);

            using (var factory = container.CreateScope())
            {
                //Act
                var instance = factory.ServiceProvider.GetService(typeof(IDisposeableOne));
                var instance2 = factory.ServiceProvider.GetService(typeof(IDisposeableOne));
                
                //Assert
                Assert.AreEqual(0, Container.ContainerScope.Disposeables.Count);
                Assert.AreEqual(2, ((Scoped)factory).Disposeables.Count);

            }
        }
        
        [TestMethod]
        public void AssertResolvingScopeDisposesTransientObjectsImplementingIDisposable()
        {
            var container = new Container();
            container.Register<DisposeableOne, DisposeableOne>(Lifetime.Transient);
            container.Register<DisposeableTwo, DisposeableTwo>(Lifetime.Transient);

            var scope = container.CreateScope();
            var transient1 = scope.ServiceProvider.GetService(typeof(DisposeableOne));
            var transient2 = scope.ServiceProvider.GetService(typeof(DisposeableTwo));

            scope.Dispose();

            //Transient objects are stored in container scoped
            container.Dispose();

            var po = new PrivateObject<Scoped>(scope);
            var result = po.GetField<bool>("_disposed");
            Assert.IsTrue(result);

            var po1 = new PrivateObject<DisposeableOne>(transient1);
            var result1 = po1.GetField<bool>("_disposed");
            Assert.IsTrue(result1);

            var po2 = new PrivateObject<DisposeableTwo>(transient2);
            var result2 = po2.GetField<bool>("_disposed");
            Assert.IsTrue(result2);
        }

        [TestMethod]
        public void AssertDisposingScopedLifetimes()
        {
            using (var container = new Container())
            {
                container.Register<DisposeableOne, DisposeableOne>(Lifetime.Scoped);
                container.Register<DisposeableTwo, DisposeableTwo>(Lifetime.Scoped);

                var scope = container.CreateScope();

                var scope1 = scope.ServiceProvider.GetService(typeof(DisposeableOne));
                var scope2 = scope.ServiceProvider.GetService(typeof(DisposeableTwo));

                scope.Dispose();

                var po = new PrivateObject<Scoped>(scope);
                var result = po.GetField<bool>("_disposed");

                var po1 = new PrivateObject<DisposeableOne>(scope1);
                var result1 = po1.GetField<bool>("_disposed");

                var po2 = new PrivateObject<DisposeableTwo>(scope2);
                var result2 = po2.GetField<bool>("_disposed");

                Assert.IsTrue(result);
                Assert.IsTrue(result1);
                Assert.IsTrue(result2);
            }
        }

        [TestMethod]
        public void AssertDisposingContainerAndScoped()
        {
            var container = new Container();

            container.Register<IDisposeableOne, DisposeableOne>(Lifetime.Scoped);
            container.Register<IDisposeableTwo, DisposeableTwo>(Lifetime.Scoped);
            container.Register<IDisposeableThree, DisposaebleThree>(Lifetime.Singleton);

            var scope = container.CreateScope();

            var scope1 = scope.ServiceProvider.GetService(typeof(IDisposeableOne));
            var scope2 = scope.ServiceProvider.GetService(typeof(IDisposeableTwo));

            var singleton = scope.ServiceProvider.GetService(typeof(IDisposeableThree));

            scope.Dispose();

            var po = new PrivateObject<Scoped>(scope);
            var result = po.GetField<bool>("_disposed");

            var po1 = new PrivateObject<DisposeableOne>(scope1);
            var result1 = po1.GetField<bool>("_disposed");

            var po2 = new PrivateObject<DisposeableTwo>(scope2);
            var result2 = po2.GetField<bool>("_disposed");

            Assert.IsTrue(result);
            Assert.IsTrue(result1);
            Assert.IsTrue(result2);

            container.Dispose();

            var po3 = new PrivateObject<Container>(container);
            var result3 = po3.GetField<bool>("_disposed");

            Assert.IsTrue(result3);

            //singletons are stored in a containerscope even if they are resolved using a scope
            var po4 = new PrivateObject<DisposaebleThree>(singleton);
            var result4 = po4.GetField<bool>("_disposed");

            Assert.IsTrue(result4);
        }

        [TestMethod]
        public void AssertScopedLifetimeCreation()
        {
            Container container = new Container();

            container.Register<ISingletonFour, SingletonFour>(Lifetime.Scoped);
            container.Register<ISingletonOne, SingletonOne>(Lifetime.Scoped);
            
            ISingletonFour instance1 = null;
            ISingletonFour instance2 = null;
            ISingletonFour instance3 = null;

            ISingletonOne instance5 = null;

            using (var instance = container.CreateScope())
            {
                instance1 = (ISingletonFour)instance.ServiceProvider.GetService(typeof(ISingletonFour));
                instance2 = (ISingletonFour)instance.ServiceProvider.GetService(typeof(ISingletonFour));

                instance5 = (ISingletonOne)instance.ServiceProvider.GetService(typeof(ISingletonOne));
            }

            Assert.IsTrue(instance1 == instance2);
            Assert.IsTrue(instance5 != null);


            using (var instance = container.CreateScope())
            {
                instance3 = (ISingletonFour)instance.ServiceProvider.GetService(typeof(ISingletonFour));
            }

            var instance4 = (ISingletonFour)container.Resolve(typeof(ISingletonFour));

            Assert.IsTrue(instance3 != instance1);
            Assert.IsTrue(instance4 != instance3);
        }
        
        [TestMethod]
        public void AssertScopedInAspNetCore()
        {
            Container container = new Container();

            ServiceCollection collection = new ServiceCollection();
            collection.AddScoped(typeof(ISingletonFour), typeof(SingletonFour));
            container.RegisterServiceCollection(collection);

            ISingletonFour instance1 = null;
            ISingletonFour instance2 = null;
            ISingletonFour instance3 = null;

            var factory = container.Resolve<IServiceScopeFactory>();

            using (var scope = factory.CreateScope())
            {
                instance1 = (ISingletonFour)scope.ServiceProvider.GetService(typeof(ISingletonFour));
                instance2 = (ISingletonFour)scope.ServiceProvider.GetService(typeof(ISingletonFour));
            }

            Assert.IsTrue(instance1 == instance2);

            using (var scope = factory.CreateScope())
            {
                instance3 = (ISingletonFour)scope.ServiceProvider.GetService(typeof(ISingletonFour));
            }

            var instance4 = (ISingletonFour)container.Resolve(typeof(ISingletonFour));

            Assert.IsTrue(instance3 != instance1);
            Assert.IsTrue(instance4 != instance3);

        }

    }
}
