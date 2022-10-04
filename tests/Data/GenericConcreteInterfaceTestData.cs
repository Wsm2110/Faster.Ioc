using System.Diagnostics.CodeAnalysis;

namespace Faster.Ioc.Tests.Data
{
    public interface IGenericConcreteInterfaceDefault<TConcrete> where TConcrete : class
    {

    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TConcrete">The type of the concrete.</typeparam>
    public interface IGenericConcreteInterfaceWithOneParam<TConcrete> where TConcrete : class
    {
        /// <summary>
        /// Gets or sets the concrete interface.
        /// </summary>
        /// <value>
        /// The concrete interface.
        /// </value>
       public IConcreteInterface ConcreteInterface { get; set; }
    }

    public interface IGenericConcreteInterfaceWithGenericParam<TConcrete> where TConcrete : class
    {
        IGenericConcreteInterfaceDefault<ConcreteA> GenericConcreteInterface
        {
            get;
            set;
        }

    }

    public interface IGenericConcreteInterfaceComplex<TConcrete> where TConcrete : class
    {
        public IGenericConcreteInterfaceWithOneParam<ConcreteA> ConcreteInterfaceWithOneParam { get; }
        public IGenericConcreteInterfaceWithGenericParam<ConcreteA> GenericConcreteInterfaceWithGenericParam { get; }
    }


    public class GenericConcreteInterfaceDefault<TConcrete> : IGenericConcreteInterfaceDefault<TConcrete> where TConcrete : class
    {
        public GenericConcreteInterfaceDefault()
        {

        }
    }

    public class GenericConcreteInterfaceWithOneParam<TConcrete> : IGenericConcreteInterfaceWithOneParam<TConcrete> where TConcrete : class
    {
        public IConcreteInterface ConcreteInterface { get; set; }

        public GenericConcreteInterfaceWithOneParam(IConcreteInterface concreteInterface)
        {
            ConcreteInterface = concreteInterface;
        }

    }

    public class GenericConcreteInterfaceWithGenericParam<TConcrete> : IGenericConcreteInterfaceWithGenericParam<TConcrete> where TConcrete : class
    {
        public IGenericConcreteInterfaceDefault<ConcreteA> GenericConcreteInterface { get; set; }

        public GenericConcreteInterfaceWithGenericParam(IGenericConcreteInterfaceDefault<ConcreteA> genericConcreteInterface)
        {
            GenericConcreteInterface = genericConcreteInterface;
        }
    }

    public class GenericConcreteInterfaceComplex<TConcrete> : IGenericConcreteInterfaceComplex<TConcrete> where TConcrete : class
    {
        public IGenericConcreteInterfaceWithOneParam<ConcreteA> ConcreteInterfaceWithOneParam { get; }
        public IGenericConcreteInterfaceWithGenericParam<ConcreteA> GenericConcreteInterfaceWithGenericParam { get; }

        public GenericConcreteInterfaceComplex(IGenericConcreteInterfaceWithOneParam<ConcreteA> concreteInterfaceWithOneParam,
            IGenericConcreteInterfaceWithGenericParam<ConcreteA> genericConcreteInterfaceWithGenericParam)
        {
            ConcreteInterfaceWithOneParam = concreteInterfaceWithOneParam;
            GenericConcreteInterfaceWithGenericParam = genericConcreteInterfaceWithGenericParam;
        }
    }


}
