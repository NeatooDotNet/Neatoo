Neatoo is a .NET Domain Model Framework built on the principals in [CSLA](https://cslanet.com/) and [Patterns Of Enterprise Architecture (116 & 165)](https://www.thriftbooks.com/w/patterns-of-enterprise-application-architecture_martin-fowler_david-rice/250298/item/27212332/?utm_source=google&utm_medium=cpc&utm_campaign=high_vol_frontlist_standard_shopping_retention_21262958110&utm_adgroup=&utm_term=&utm_content=698403107257&gad_source=1&gbraid=0AAAAADwY45i8XIkbO1r3yKTmRi6UsVHLW&gclid=CjwKCAjw--K_BhB5EiwAuwYoygC_pDzfyrD4WukBHztysw2QXsKteRV1de7pGyNPlSBZ4RPLz4SltxoCWBcQAvD_BwE#idiq=27212332&edition=3682851). With it you can create streamlined business applications with complex behavior in Blazor and WPF. With Neatoo's business rules, meta properties, parent/child relationships, authentication, physical 2-tier factory including data mapper methods and data binding your Domain Models go well beyond CRUD. Better yet it uses Roslyn source generators instead of reflection. You can see and step thru the [2-tier factory Neatoo generated](https://github.com/NeatooDotNet/Neatoo/blob/main/Examples/Person/Person.DomainModel/Generated/Neatoo.RemoteFactory.FactoryGenerator/Neatoo.RemoteFactory.FactoryGenerator.FactoryGenerator/Person.DomainModel.PersonModelFactory.g.cs) for the Domain Model. With this run time errors are now compile errors and it has amazing performance.

#### Create Rich Domain Models with a single controller and no DTOs*! Go beyond CRUD!!

To get started. Please see [the Person example](https://github.com/NeatooDotNet/Neatoo/tree/main/Examples/Person) Blazor Stand-Alone Web application shown in the animation below. Click to enlarge.

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

## Recommended Reading

Ideas that shaped Neatoo:
- I highly recommend [the CSLA books](https://store.lhotka.net/).
- [Patterns of Enteprise Architecture](https://www.thriftbooks.com/w/patterns-of-enterprise-application-architecture_martin-fowler_david-rice/250298/?resultid=dcd84f2b-51ab-4e22-8e24-3c3a17de30bb#edition=3682851&idiq=4316361) - Domain Model (110) & Data Mapper (165)
- [Object–relational impedance mismatch](https://en.wikipedia.org/wiki/Object%E2%80%93relational_impedance_mismatch)
- [Anemic Domain Model](https://martinfowler.com/bliki/AnemicDomainModel.html)
- [Async Programming : Patterns for Asynchronous MVVM Applications: Data Binding](https://learn.microsoft.com/en-us/archive/msdn-magazine/2014/march/async-programming-patterns-for-asynchronous-mvvm-applications-data-binding)
- [Extending Partial Methods](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/extending-partial-methods) and [Partial Properties](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-13.0/partial-properties)

*No DTOs to get the Domain Model from the server to the client. If you use the repository pattern you may have DTOs for you DAL layer interaction.
