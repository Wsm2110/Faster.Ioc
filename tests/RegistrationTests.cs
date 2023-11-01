using Faster.Ioc.Factory;
using Faster.Ioc.Models;
using Faster.Ioc.Tests.Contracts;
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
    public class RegistrationTests
    {
        [TestMethod]
        public void AssertAddingMultipleRegistrationsOfTheSameInterfaceAndType()
        {
            //assign
            var map = new RegistrationFactory(16, 0.5);

            //act
            map.Emplace(typeof(IConcreteInterface), new Registration(typeof(IConcreteInterface), typeof(ConcreteA)));
            map.Emplace(typeof(IConcreteInterface), new Registration(typeof(IConcreteInterface), typeof(ConcreteA)));

            //assert
            Assert.AreEqual(1, map.Count);
        }

        [TestMethod]
        public void AssertAddingMultipleRegistrationsOfTheSameInterface()
        {
            //assign
            var map = new RegistrationFactory(16, 0.5);

            //act
            map.Emplace(typeof(IConcreteInterface), new Registration(typeof(IConcreteInterface), typeof(ConcreteA)));
            map.Emplace(typeof(IConcreteInterface), new Registration(typeof(IConcreteInterface), typeof(ConcreteB)));

            //assert
            Assert.AreEqual(2, map.Count);
        }

        [TestMethod]
        public void AssertRetrievalOfAllInterfaceTypes()
        {
            //assign
            var map = new RegistrationFactory(16, 0.5);

            //act
            map.Emplace(typeof(IConcreteInterface), new Registration(typeof(IConcreteInterface), typeof(ConcreteA)));
            map.Emplace(typeof(IConcreteInterface), new Registration(typeof(IConcreteInterface), typeof(ConcreteB)));

            var result = map.GetAll(typeof(IConcreteInterface));

            //assert
            Assert.AreEqual(2, result.Count());
        }

        [TestMethod]
     
        public void AssertRetrievalOfAllInterfaceTypesWhileDifferentInterfacesArePresent()
        {
            //assign
            var map = new RegistrationFactory(16, 0.5);

            //act
            map.Emplace(typeof(IConcreteInterface), new Registration(typeof(IConcreteInterface), typeof(ConcreteA)));
            map.Emplace(typeof(IConcreteInterface), new Registration(typeof(IConcreteInterface), typeof(ConcreteB)));

            map.Emplace(typeof(ILocatable), new Registration(typeof(ILocatable), typeof(Locatable1)));
            map.Emplace(typeof(ILocatableDummy), new Registration(typeof(ILocatableDummy), typeof(LocatableDummy)));

            var result = map.GetAll(typeof(IConcreteInterface));

            //assert
            Assert.AreEqual(2, result.Count());
        }

    }

}
