![Lifecycle](https://raw.githubusercontent.com/NeatooDotNet/Neatoo/main/Logo_411.png)

Neatoo is a DDD Aggregate Framework for C# Blazor and WPF applications. It specializes in creating bindable Aggregate Entity Graphs for Blazor and WPF Views that are re-used on the Application Service. Neatoo uniquely uses Roslyn Source Generators to create readable and performant code specific to the Entities.

Neatoo provides:
* Validation
* Bindable Meta-properties
* Authorization
* Factory
* Serialization

Neatoo supports:
* Dependency Injection
* Async/Await 

#### With Neatoo you will create Blazor Business applications with way less code!

[Discord](https://discord.gg/M3dVuZkG)
[Nuget](https://www.nuget.org/packages/Neatoo)

## Neatoo Lifecycle

This is a common lifecycle for a Neatoo Aggregate Entity Graph to and from the database using a 3-Tier infrastructure.

![Lifecycle](https://raw.githubusercontent.com/NeatooDotNet/Neatoo/main/AggregateLifecycle_960.gif)

Neatoo does not limit you to a database. Any Service can be injected to the Factory Methods on the Application Server.

## Video

[![Introduction](https://raw.githubusercontent.com/NeatooDotNet/Neatoo/main/youtubetile.jpg)](https://youtu.be/e9zZ6d8LKkM?si=KX1sNMtkaHF57haB)

## Example

Please explore [the Person example](https://github.com/NeatooDotNet/Neatoo/tree/main/src/Examples/Person) Blazor Stand-Alone Web application. 

* Entities
  - [PersonModel](https://github.com/NeatooDotNet/Neatoo/blob/main/src/Examples/Person/Person.DomainModel/PersonModel.cs) and [PersonPhoneModel](https://github.com/NeatooDotNet/Neatoo/blob/main/src/Examples/Person/Person.DomainModel/PersonPhoneModel.cs)
* Validation
  - [UniqueNameRule](https://github.com/NeatooDotNet/Neatoo/blob/main/src/Examples/Person/Person.DomainModel/UniqueNameRule.cs), [UniquePhoneNumberRule](https://github.com/NeatooDotNet/Neatoo/blob/main/src/Examples/Person/Person.DomainModel/UniquePhoneNumberRule.cs) and [UniquePhoneTypeRule](https://github.com/NeatooDotNet/Neatoo/blob/main/src/Examples/Person/Person.DomainModel/UniquePhoneTypeRule.cs)
* Bindable Meta-properties
  - Provided by [EditBase](https://github.com/NeatooDotNet/Neatoo/blob/main/src/Neatoo/EditBase.cs) also see [IMetaProperties](https://github.com/NeatooDotNet/Neatoo/blob/main/src/Neatoo/IMetaProperties.cs)
* Authorization
  - [PersonModelAuth](https://github.com/NeatooDotNet/Neatoo/blob/main/src/Examples/Person/Person.DomainModel/PersonModelAuth.cs)
* Factory
  - These Roslyn Source Generated Factories to call [Create], [Fetch], [Insert], [Update] and [Delete] methods in [PersonModel](https://github.com/NeatooDotNet/Neatoo/blob/main/src/Examples/Person/Person.DomainModel/PersonModel.cs) based on it's meta-state. 
  - [PersonModelFactory](https://github.com/NeatooDotNet/Neatoo/blob/main/src/Examples/Person/Person.DomainModel/Generated/Neatoo.RemoteFactory.FactoryGenerator/Neatoo.RemoteFactory.FactoryGenerator.FactoryGenerator/Person.DomainModel.PersonModelFactory.g.cs), [PersonModelPhoneFactory](https://github.com/NeatooDotNet/Neatoo/blob/main/src/Examples/Person/Person.DomainModel/Generated/Neatoo.RemoteFactory.FactoryGenerator/Neatoo.RemoteFactory.FactoryGenerator.FactoryGenerator/Person.DomainModel.PersonPhoneModelFactory.g.cs), [PersonModelPhoneListFactory](https://github.com/NeatooDotNet/Neatoo/blob/main/src/Examples/Person/Person.DomainModel/Generated/Neatoo.RemoteFactory.FactoryGenerator/Neatoo.RemoteFactory.FactoryGenerator.FactoryGenerator/Person.DomainModel.PersonPhoneModelFactory.g.cs) and [UniqueNameRuleFactory](https://github.com/NeatooDotNet/Neatoo/blob/main/src/Examples/Person/Person.DomainModel/Generated/Neatoo.RemoteFactory.FactoryGenerator/Neatoo.RemoteFactory.FactoryGenerator.FactoryGenerator/Person.DomainModel.UniqueNameFactory.g.cs)

* Serialization
  - Serialized at the AggregateRoot level of PersonModel as signified by the [Remote] Attribute on the Factory Methods of [PersonModel](https://github.com/NeatooDotNet/Neatoo/blob/main/src/Examples/Person/Person.DomainModel/PersonModel.cs)

This is an animation of the application in action:

<img src="https://raw.githubusercontent.com/NeatooDotNet/Neatoo/main/NeatooPersonRules.gif" width=30% height=30%>

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
- Of course [Implementing Domain-Driven Design by Vaughn Vernon](https://www.amazon.com/Implementing-Domain-Driven-Design-Vaughn-Vernon/dp/0321834577/ref=asc_df_0321834577?mcid=2d57f9b4826b30adbc0f024ba5ffcee1&hvocijid=13025708246257480970-0321834577-&hvexpln=73&tag=hyprod-20&linkCode=df0&hvadid=721245378154&hvpos=&hvnetw=g&hvrand=13025708246257480970&hvpone=&hvptwo=&hvqmt=&hvdev=c&hvdvcmdl=&hvlocint=&hvlocphy=1019976&hvtargid=pla-2281435177658&psc=1)
- The rules engine is a replica of [CSLA by Rockford Lhotka](https://store.lhotka.net/).
- In addition to DDD why to have Domain Model (110) & Data Mapper (165) is in [Patterns of Enteprise Architecture](https://www.thriftbooks.com/w/patterns-of-enterprise-application-architecture_martin-fowler_david-rice/250298/?resultid=dcd84f2b-51ab-4e22-8e24-3c3a17de30bb#edition=3682851&idiq=4316361) - 
- A key idea every developer should understand is [Object–relational impedance mismatch](https://en.wikipedia.org/wiki/Object%E2%80%93relational_impedance_mismatch)
- Everyone warns about [Anemic Domain Models](https://martinfowler.com/bliki/AnemicDomainModel.html)
- [Async Programming : Patterns for Asynchronous MVVM Applications: Data Binding](https://learn.microsoft.com/en-us/archive/msdn-magazine/2014/march/async-programming-patterns-for-asynchronous-mvvm-applications-data-binding) is why each property has bindable meta-state
- [Extending Partial Methods](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/extending-partial-methods) and [Partial Properties](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-13.0/partial-properties) is a key Roslyn Source Generator concept
- The best articles I've found on [writing source generators](https://andrewlock.net/series/creating-a-source-generator/)

