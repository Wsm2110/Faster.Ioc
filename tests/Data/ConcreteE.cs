namespace Faster.Ioc.Tests.Data
{
    public class ConcreteE : IConcreteInterface
    {
        public readonly ITestData Testdata;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcreteE"/> class.
        /// </summary>
        /// <param name="testdata">The testdata.</param>
        public ConcreteE(ITestData testdata)
        {
            Testdata = testdata;
        }

    }
}
