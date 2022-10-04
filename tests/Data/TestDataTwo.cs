using System.Collections.Generic;

namespace Faster.Ioc.Tests.Data
{
    public class TestDataTwo
    {
        public ITestData TestData { get; set; }

        /// <summary>
        /// Gets or sets the concrete interfaces.
        /// </summary>
        /// <value>
        /// The concrete interfaces.
        /// </value>
        public IEnumerable<IConcreteInterface> ConcreteInterfaces { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestDataTwo" /> class.
        /// </summary>
        /// <param name="concreteInterfaces">The concrete interfaces.</param>
        /// <param name="testData">The test data.</param>
        public TestDataTwo(IEnumerable<IConcreteInterface> concreteInterfaces, ITestData testData)
        {
            TestData = testData;
            ConcreteInterfaces = concreteInterfaces;
        }

    }
}
