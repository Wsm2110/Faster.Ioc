using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Faster.Ioc.Tests.Data
{
    public class EnumerableTestDataOne : IEnumerableTestData
    {
    }

    public class EnumerableTestDataTwo : IEnumerableTestData
    {
    }

    public class EnumerableTestDataThree : IEnumerableTestData
    {
    }

    public class EnumerableTestData
    {
        public IEnumerable<IEnumerableTestData> Data { get; }

        public EnumerableTestData(IEnumerable<IEnumerableTestData> enumerableTestData)
        {
            Data = enumerableTestData;
        }

    }


    public interface IEnumerableTestData
    {

    }


}
