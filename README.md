For as established and widely accepted the principals of Domain Models are I know of only one complete framework for C#: [CSLA](https://cslanet.com/). In 20 years of enterprise application development my best projects have been with CSLA. Neatoo follows the principals in CSLA. I started by recreating CSLA with DI and Async/Await. This alleviated a lot of concerns. When I switched from reflection to [Roslyn Source Generators](https://github.com/NeatooDotNet/RemoteFactory/blob/main/src/RemoteFactory.FactoryGenerator/FactoryGenerator.cs) is when it became something truly new. [Generated code](https://github.com/NeatooDotNet/Neatoo/blob/main/Examples/Person/Person.DomainModel/Generated/Neatoo.RemoteFactory.FactoryGenerator/Neatoo.RemoteFactory.FactoryGenerator.FactoryGenerator/Person.DomainModel.PersonModelFactory.g.cs) provides **much** better performance and compile errors versus runtime errors.<br><br>
Neatoo is a .NET Domain Model Framework for C# that takes full advantage of Blazor and Roslyn Source Generators. Neatoo provides the mechanics for Domain Model Graphs including business rules, authorization rules, meta properties, physical 3-tier data mapper factory, property data binding and more so you can focus on the business logic.

#### With Neatoo you can create a Blazor application with a rich Domain Model easier than ever before!

[Discord](https://discord.gg/M3dVuZkG)
[Nuget](https://www.nuget.org/packages/Neatoo)

## Video

[![Introduction](https://raw.githubusercontent.com/NeatooDotNet/Neatoo/main/youtubetile.jpg)](https://youtu.be/e9zZ6d8LKkM?si=KX1sNMtkaHF57haB)

## Example

Here is the factory generated for [PersonModel](https://github.com/NeatooDotNet/Neatoo/blob/main/Examples/Person/Person.DomainModel/Generated/Neatoo.RemoteFactory.FactoryGenerator/Neatoo.RemoteFactory.FactoryGenerator.FactoryGenerator/Person.DomainModel.PersonModelFactory.g.cs) by Neatoo using Roslyn Source Generators. It also generates the [implementation](https://github.com/NeatooDotNet/Neatoo/blob/main/Examples/Person/Person.DomainModel/Generated/Neatoo.RemoteFactory.FactoryGenerator/Neatoo.RemoteFactory.FactoryGenerator.FactoryGenerator/Person.DomainModel.PersonModelFactory.g.cs).

```csharp
    public interface IPersonModelFactory
    {
        IPersonModel? Create();
        Task<IPersonModel?> Fetch();
        Task<IPersonModel?> Save(IPersonModel target);
        Task<Authorized<IPersonModel>> TrySave(IPersonModel target);
        Authorized CanCreate();
        Authorized CanFetch();
        Authorized CanUpdate();
        Authorized CanDelete();
        Authorized CanSave();
    }
```

Please see [the Person example](https://github.com/NeatooDotNet/Neatoo/tree/main/Examples/Person) Blazor Stand-Alone Web application shown in the animation below. Click to enlarge.

<img src="https://raw.githubusercontent.com/NeatooDotNet/Neatoo/main/NeatooPersonRules.gif" width=30% height=30%>

Domain Models: [PersonModel](https://github.com/NeatooDotNet/Neatoo/blob/main/Examples/Person/Person.DomainModel/PersonModel.cs), [PersonPhoneModel](https://github.com/NeatooDotNet/Neatoo/blob/main/Examples/Person/Person.DomainModel/PersonPhoneModel.cs) and [PersonPhoneModelList](https://github.com/NeatooDotNet/Neatoo/blob/main/Examples/Person/Person.DomainModel/PersonPhoneModelList.cs)\
Rules: [UniqueNameRule](https://github.com/NeatooDotNet/Neatoo/blob/main/Examples/Person/Person.DomainModel/UniqueNameRule.cs), [UniquePhoneNumberRule](https://github.com/NeatooDotNet/Neatoo/blob/main/Examples/Person/Person.DomainModel/UniquePhoneNumberRule.cs) and [UniquePhoneTypeRule](https://github.com/NeatooDotNet/Neatoo/blob/main/Examples/Person/Person.DomainModel/UniquePhoneTypeRule.cs)\
Authorization: [PersonModelAuth](https://github.com/NeatooDotNet/Neatoo/blob/main/Examples/Person/Person.DomainModel/PersonModelAuth.cs)\
Roslyn Auto-Generated Factories: [PersonModelFactory](https://github.com/NeatooDotNet/Neatoo/blob/main/Examples/Person/Person.DomainModel/Generated/Neatoo.RemoteFactory.FactoryGenerator/Neatoo.RemoteFactory.FactoryGenerator.FactoryGenerator/Person.DomainModel.PersonModelFactory.g.cs), [PersonModelPhoneFactory](https://github.com/NeatooDotNet/Neatoo/blob/main/Examples/Person/Person.DomainModel/Generated/Neatoo.RemoteFactory.FactoryGenerator/Neatoo.RemoteFactory.FactoryGenerator.FactoryGenerator/Person.DomainModel.PersonPhoneModelFactory.g.cs), [PersonModelPhoneListFactory](https://github.com/NeatooDotNet/Neatoo/blob/main/Examples/Person/Person.DomainModel/Generated/Neatoo.RemoteFactory.FactoryGenerator/Neatoo.RemoteFactory.FactoryGenerator.FactoryGenerator/Person.DomainModel.PersonPhoneModelFactory.g.cs) and [UniqueNameRuleFactory](https://github.com/NeatooDotNet/Neatoo/blob/main/Examples/Person/Person.DomainModel/Generated/Neatoo.RemoteFactory.FactoryGenerator/Neatoo.RemoteFactory.FactoryGenerator.FactoryGenerator/Person.DomainModel.UniqueNameFactory.g.cs) \

##  Please leave feedback!
Thus far, this was a fun winter project. If there is any interest to use it I will continue to work on it. So, please provide feedback!

[Please let me know](https://github.com/NeatooDotNet/Neatoo/issues):
- Does a framework like this already exists (besides [CSLA](https://cslanet.com/))?
- Would this be useful to you?
- What is useful/good?
- What is missing/bad?

Please be constructive.

#### [Discord](https://discord.gg/M3dVuZkG)

## Recommended Reading

Ideas that shaped Neatoo:
- I highly recommend [the CSLA books](https://store.lhotka.net/).
- [Patterns of Enteprise Architecture](https://www.thriftbooks.com/w/patterns-of-enterprise-application-architecture_martin-fowler_david-rice/250298/?resultid=dcd84f2b-51ab-4e22-8e24-3c3a17de30bb#edition=3682851&idiq=4316361) - Domain Model (110) & Data Mapper (165)
- [Object–relational impedance mismatch](https://en.wikipedia.org/wiki/Object%E2%80%93relational_impedance_mismatch)
- [Anemic Domain Model](https://martinfowler.com/bliki/AnemicDomainModel.html)
- [Async Programming : Patterns for Asynchronous MVVM Applications: Data Binding](https://learn.microsoft.com/en-us/archive/msdn-magazine/2014/march/async-programming-patterns-for-asynchronous-mvvm-applications-data-binding)
- [Extending Partial Methods](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/extending-partial-methods) and [Partial Properties](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-13.0/partial-properties)

