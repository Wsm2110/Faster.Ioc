# Faster.Ioc - fastest ioc container

Faster.Ioc is an IoC container for Microsoft .NET. It manages dependencies between classes so that applications stay easy to change as they grow in size and complexity.

## About

Faster.Ioc is an incredibly fast Ioc container with endless possibilities. Faster.ioc supports the following:

- Scoped lifetimes (Singleton, Transient, Scoped)
- Multi registrations, no more pesky factories...
- Conditional overrides
- Ienumerable<T>
- Asp.net core features
- Open and closed generics
- Child containers
- Store registrations by key
- Retrieve all registrations of the same interface
- Will always resolve constructor with largest parameters

## How to use

1. Install nuget package Faster.Ioc to your project
```
dotnet add package Faster.Ioc
```

2. Create a Container

``` cs
var container = new Container();

//add registrations with different lifetimes
container.Register<IAnimal, Cow>(Lifetime.Singleton);
container.Register<IAnimal, Horse>(Lifetime.Transient);
container.Register<IAnimal, Bull>(Lifetime.Scoped);

// register by key
container.Register<IAnimal, Goose>(Lifetime.Scoped, "Goose")

// will always resolve the first registration of IAnimal, which in this case is a Cow
var cow = container.Resolve<IAnimal>();

// Resolve by key
var goose = container.Resolve("Goose");

//Cow, Horse and Bull
var animals = container.Resolve<IEnumerable<IAnimal>>()

```

## Advanced

3. Conditional - overriding a parameter with a different implementation

``` cs

var container = new Container();

container.Register<IAnimal, Cow>(Lifetime.Singleton);
container.Register<IAnimal, Horse>(Lifetime.Transient);

//Create override of Farm which be default, since it has an IAnimal parameter would resolve a Cow, but now it will resolve a Horse
//Types used in an override need to be registered!
container.RegisterOverride<IFarm, Farm>(() => new Farm(New Horse()));

// will always resolve the first registration of IAnimal, which in this case is a Cow
var cow = container.Resolve<IFarm>();

public class Farm
{
	public Animal{get; set;}

	public Farm(IAnimal animal)
	{
		Animal = animal;
	}
}

```
4. Ienumerable<T> 

``` cs
var container = new Container();

container.Register<IAnimal, Cow>(Lifetime.Singleton);
container.Register<IAnimal, Horse>(Lifetime.Singleton);

//Create override of Farm which be default, since it has an IAnimal parameter would resolve a Cow, but now it will resolve a Horse
//Types used in an override need to be registered!
container.Register<IFarm, Farm>();

public class Farm
{
	public IEnumerable<IAnimal> {get; private set;}

	public Farm(IEnumerable<IAnimal> animals)
	{
		Animals = animals;
	}
}
```

4. OpenGenerics<T> 

``` cs
var container = new Container();

container.Register<>(typeof(IGeneric<>, Generic<>)Lifetime.Singleton);
container.Register<IFarm, Farm>();

public class Farm
{
	public Generic<IAnimal> { get; private set;}

	public Farm(IGeneric<IAnimal> animals)
	{
		Animals = animals;
	}
}

```
5. Closed Generics<fixed>

``` cs
var container = new Container();

container.Register<>(typeof(IGeneric<int>, Generic<int>)Lifetime.Singleton);
container.Register<IFarm, Farm>();

public class Farm
{
	public Generic<int> { get; private set;}

	public Farm(IGeneric<int> animals)
	{
		Animals = animals;
	}
}

```
6. Scoped Lifetime 

``` cs
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
```
## Disposable

The lifetime of objects implementing IDisposable interface is something to keep in mind. The Container will keep track of all objects implementing IDisposable (Singletons and transient). By default Faster.Map will dispose all objects at shutdown or when .Dispose() is called. This is also when the memory is released by the garbage collector, all references stored in the Container will be gone. If this is troublesome you can either use a Scoped lifetimes or a ChildContainer


