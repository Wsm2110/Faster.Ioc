using System.Diagnostics.CodeAnalysis;

namespace Faster.Ioc.Tests.Data
{
    public interface ILocatableDummy
    {
        /// <summary>
        /// Gets or sets a.
        /// </summary>
        /// <value>
        /// a.
        /// </value>
        IConcreteInterface _a { get; set; }

    }

    [ExcludeFromCodeCoverage]
    public class LocatableDummy : ILocatableDummy
    {

        /// <summary>
        /// Gets or sets a.
        /// </summary>
        /// <value>
        /// a.
        /// </value>
        public IConcreteInterface _a { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocatableDummy"/> class.
        /// </summary>
        /// <param name="a">a.</param>
        public LocatableDummy(IConcreteInterface a)
        {
            _a = a;
        }

    }
}
