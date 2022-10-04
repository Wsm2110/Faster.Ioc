using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Faster.Ioc.Tests.Data
{
    public class OverloadDataOne : IOverloadDataOne
    {
        public IConcreteInterface ConcreteInterface { get; }

        public OverloadDataOne(IConcreteInterface concreteInterface)
        {
            ConcreteInterface = concreteInterface;
        }
    }


    public class OverloadDataTwo : IOverloadDataTwo
    {
        public OverloadDataTwo()
        {

        }
    }

    public interface IOverloadDataTwo
    {
    }

    public interface IOverloadDataOne
    {

        public IConcreteInterface ConcreteInterface { get; }
    }
}
