using System.Diagnostics.CodeAnalysis;

namespace Faster.Ioc.Tests.Data
{

    [ExcludeFromCodeCoverage]
    public class ConcreteC
    {
        public ITestData Data { get; set; }
        public IConcreteInterface ConcreteType { get; private set; }


        public ConcreteC()
        {
            
        }

        public ConcreteC(IConcreteInterface concrete)
        {
            ConcreteType = concrete;
        }

        public ConcreteC(IConcreteInterface concrete, ITestData data)
        {
            Data = data;
            ConcreteType = concrete;
        }
      
    }
}
