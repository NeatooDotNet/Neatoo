Neatoo is a .NET Domain Model Framework for C#  that takes full advantage of Blazor and Roslyn. With it you can create streamlined business applications without duplicating logic on the client and server. With Neatoo's business rules, meta properties, parent/child relationships, property data binding, authorization, physical 2-tier data mapper factory and property data binding you will create Domain Models that go well beyond CRUD with less code then ever before. Better yet, instead of reflection Roslyn source generators are utilized so you can see and step thru the code generated for the Domain Model. This provides optimal performance and run time errors are now compile errors. 

#### Create Rich Domain Models and Data Mappers with a single controller and no DTOs*!
#### Go beyond CRUD!!

[Discord](https://discord.gg/M3dVuZkG)
[Nuget](https://www.nuget.org/packages/Neatoo)

## Videos

[![Introduction](https://img.youtube.com/vi/FeS1umd3O1Y/default.jpg)](https://youtu.be/FeS1umd3O1Y)
[![Authorization](https://img.youtube.com/vi/0oepvXmdBVM/default.jpg)](https://youtu.be/nTQUwghvy5Q)

## Example
Please see [the Person example](https://github.com/NeatooDotNet/Neatoo/tree/main/Examples/Person) Blazor Stand-Alone Web application shown in the animation below. Click to enlarge.

<img src="https://raw.githubusercontent.com/NeatooDotNet/Neatoo/main/NeatooPersonRules.gif" width=30% height=30%>

Domain Models: [PersonModel](https://github.com/NeatooDotNet/Neatoo/blob/main/Examples/Person/Person.DomainModel/PersonModel.cs), [PersonPhoneModel](https://github.com/NeatooDotNet/Neatoo/blob/main/Examples/Person/Person.DomainModel/PersonPhoneModel.cs) and [PersonPhoneModelList](https://github.com/NeatooDotNet/Neatoo/blob/main/Examples/Person/Person.DomainModel/PersonPhoneModelList.cs)\
Rules: [UniqueNameRule](https://github.com/NeatooDotNet/Neatoo/blob/main/Examples/Person/Person.DomainModel/UniqueNameRule.cs), [UniquePhoneNumberRule](https://github.com/NeatooDotNet/Neatoo/blob/main/Examples/Person/Person.DomainModel/UniquePhoneNumberRule.cs) and [UniquePhoneTypeRule](https://github.com/NeatooDotNet/Neatoo/blob/main/Examples/Person/Person.DomainModel/UniquePhoneTypeRule.cs)\
Authorization: [PersonModelAuth](https://github.com/NeatooDotNet/Neatoo/blob/main/Examples/Person/Person.DomainModel/PersonModelAuth.cs)\
Roslyn Auto-Generated Factories: [PersonModelFactory](https://github.com/NeatooDotNet/Neatoo/blob/main/Examples/Person/Person.DomainModel/Generated/Neatoo.RemoteFactory.FactoryGenerator/Neatoo.RemoteFactory.FactoryGenerator.FactoryGenerator/Person.DomainModel.PersonModelFactory.g.cs), [PersonModelPhoneFactory](https://github.com/NeatooDotNet/Neatoo/blob/main/Examples/Person/Person.DomainModel/Generated/Neatoo.RemoteFactory.FactoryGenerator/Neatoo.RemoteFactory.FactoryGenerator.FactoryGenerator/Person.DomainModel.PersonPhoneModelFactory.g.cs), [PersonModelPhoneListFactory](https://github.com/NeatooDotNet/Neatoo/blob/main/Examples/Person/Person.DomainModel/Generated/Neatoo.RemoteFactory.FactoryGenerator/Neatoo.RemoteFactory.FactoryGenerator.FactoryGenerator/Person.DomainModel.PersonPhoneModelFactory.g.cs) and [UniqueNameRuleFactory](https://github.com/NeatooDotNet/Neatoo/blob/main/Examples/Person/Person.DomainModel/Generated/Neatoo.RemoteFactory.FactoryGenerator/Neatoo.RemoteFactory.FactoryGenerator.FactoryGenerator/Person.DomainModel.UniqueNameFactory.g.cs) \

##  Please leave feedback!
Thus far, this was a fun winter project. When I incorporated Roslyn source generators is when it seemed it might be something new and useful. My best most enjoyable secular project was done using CSLA. This is an attempt to re-create CSLA with a Dependency Injection and Async/Await centric approach. If there is any interest to use it I will continue to work on it. So, I need some feedback!

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

*No DTOs to get the Domain Model from the server to the client. If you use the repository pattern you may have DTOs for you DAL layer interaction.
