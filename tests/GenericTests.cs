using Faster.Ioc.Contracts;
using Faster.Ioc.Tests.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Faster.Ioc.Tests
{
    [TestClass]
    public class GenericTests
    {

        [TestMethod]
        public void AssertRetrievalOfComplexOpenGenericInterface()
        {
            using (var container = new Container())
            {
                //arrange
                container.Register<IGenericConcreteInterfaceComplex<ConcreteA>, GenericConcreteInterfaceComplex<ConcreteA>>(Lifetime.Singleton);
                container.Register<IConcreteInterface, ConcreteA>(Lifetime.Singleton);
                container.Register(typeof(IGenericConcreteInterfaceWithGenericParam<>), typeof(GenericConcreteInterfaceWithGenericParam<>), Lifetime.Singleton);
                container.Register(typeof(IGenericConcreteInterfaceDefault<>), typeof(GenericConcreteInterfaceDefault<>), Lifetime.Singleton);
                container.Register(typeof(IGenericConcreteInterfaceWithOneParam<>), typeof(GenericConcreteInterfaceWithOneParam<>), Lifetime.Singleton);

                //act
                var a = (IGenericConcreteInterfaceComplex<ConcreteA>)container.Resolve(typeof(IGenericConcreteInterfaceComplex<ConcreteA>));

                //assert
                Assert.IsNotNull(a);
                Assert.IsInstanceOfType(a, typeof(IGenericConcreteInterfaceComplex<ConcreteA>));
                Assert.IsInstanceOfType(a.ConcreteInterfaceWithOneParam, typeof(GenericConcreteInterfaceWithOneParam<ConcreteA>));
                Assert.IsInstanceOfType(a.GenericConcreteInterfaceWithGenericParam, typeof(GenericConcreteInterfaceWithGenericParam<ConcreteA>));
                Assert.IsInstanceOfType(a.ConcreteInterfaceWithOneParam.ConcreteInterface, typeof(ConcreteA));
                Assert.IsInstanceOfType(a.GenericConcreteInterfaceWithGenericParam.GenericConcreteInterface, typeof(GenericConcreteInterfaceDefault<ConcreteA>));
            }
        }

        [TestMethod]
        public void AssertRetrievalOfOpenGenericInterfaceWithOpenGenericParameter()
        {
            using (var container = new Container())
            {
                //arrange
                container.Register(typeof(IGenericConcreteInterfaceWithGenericParam<>), typeof(GenericConcreteInterfaceWithGenericParam<>), Lifetime.Singleton);
                container.Register(typeof(IGenericConcreteInterfaceDefault<>), typeof(GenericConcreteInterfaceDefault<>), Lifetime.Singleton);
                
                //act
                var a = (IGenericConcreteInterfaceWithGenericParam<ConcreteA>)container.Resolve(typeof(IGenericConcreteInterfaceWithGenericParam<ConcreteA>));
                
                //assert
                Assert.IsNotNull(a);
                Assert.IsInstanceOfType(a, typeof(IGenericConcreteInterfaceWithGenericParam<ConcreteA>));
                Assert.IsInstanceOfType(a.GenericConcreteInterface, typeof(GenericConcreteInterfaceDefault<ConcreteA>));
            }
        }

        [TestMethod]
        public void AssertRetrievalOfGenericInterfaceWithOneParameter()
        {
            using (var container = new Container())
            {
                container.Register<IGenericConcreteInterfaceWithOneParam<ConcreteA>, GenericConcreteInterfaceWithOneParam<ConcreteA>>(Lifetime.Singleton);
                container.Register<IConcreteInterface, ConcreteB>(Lifetime.Singleton);

                var a = (IGenericConcreteInterfaceWithOneParam<ConcreteA>)container.Resolve(typeof(IGenericConcreteInterfaceWithOneParam<ConcreteA>));

                Assert.IsNotNull(a);
                Assert.IsInstanceOfType(a, typeof(GenericConcreteInterfaceWithOneParam<ConcreteA>));
                Assert.IsInstanceOfType(a.ConcreteInterface, typeof(ConcreteB));
            }
        }

        [TestMethod]
        public void AssertRetrievalOfGenericInterface()
        {
            using (var container = new Container())
            {
                container.Register<IGenericConcreteInterfaceDefault<ConcreteA>, GenericConcreteInterfaceDefault<ConcreteA>>(Lifetime.Singleton);

                var a = container.Resolve(typeof(IGenericConcreteInterfaceDefault<ConcreteA>));

                Assert.IsNotNull(a);
                Assert.IsInstanceOfType(a, typeof(IGenericConcreteInterfaceDefault<ConcreteA>));
            }
        }

        [TestMethod]
        public void AssertRetrievalOfMultipleGenericInterfaces()
        {
            using (var container = new Container())
            {
                container.Register<IGenericConcreteInterfaceDefault<ConcreteA>, GenericConcreteInterfaceDefault<ConcreteA>>(Lifetime.Singleton);
                container.Register<IGenericConcreteInterfaceDefault<ConcreteB>, GenericConcreteInterfaceDefault<ConcreteB>>(Lifetime.Singleton);

                var a = container.Resolve(typeof(IGenericConcreteInterfaceDefault<ConcreteA>));

                Assert.IsNotNull(a);
                Assert.IsInstanceOfType(a, typeof(GenericConcreteInterfaceDefault<ConcreteA>));

                var b = container.Resolve(typeof(IGenericConcreteInterfaceDefault<ConcreteB>));

                Assert.IsNotNull(b);
                Assert.IsInstanceOfType(b, typeof(GenericConcreteInterfaceDefault<ConcreteB>));
            }
        }


    }
}
