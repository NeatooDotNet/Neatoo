Neatoo is a .NET Domain Model Framework built on the principals in [Patterns Of Enterprise Architecture (116 & 165)](https://www.thriftbooks.com/w/patterns-of-enterprise-application-architecture_martin-fowler_david-rice/250298/item/27212332/?utm_source=google&utm_medium=cpc&utm_campaign=high_vol_frontlist_standard_shopping_retention_21262958110&utm_adgroup=&utm_term=&utm_content=698403107257&gad_source=1&gbraid=0AAAAADwY45i8XIkbO1r3yKTmRi6UsVHLW&gclid=CjwKCAjw--K_BhB5EiwAuwYoygC_pDzfyrD4WukBHztysw2QXsKteRV1de7pGyNPlSBZ4RPLz4SltxoCWBcQAvD_BwE#idiq=27212332&edition=3682851) and [CSLA](https://cslanet.com/). With it you can create streamlined business applications in Blazor and WPF with complex behavior. With Neatoo's inherent meta properties, parent/child relationships, business rule validation, authentication, physical 2-tier factory including data mapper methods and data binding your Domain Models can go well beyond CRUD. Roslyn source generators bring it to a new level of high performance. Create full Domain Models with a single controller and no DTOs*!

### Go beyond CRUD!

To get started. Please see [the Person example app.](https://github.com/NeatooDotNet/Neatoo/tree/main/Examples/Person). This animations below highlights the behaviors.

<img src="https://raw.githubusercontent.com/NeatooDotNet/Neatoo/main/NeatooPersonRules.gif" width=30% height=30%>

Domain Models: [PersonModel](https://github.com/NeatooDotNet/Neatoo/blob/main/Examples/Person/Person.DomainModel/PersonModel.cs) and [PersonPhoneModel](https://github.com/NeatooDotNet/Neatoo/blob/main/Examples/Person/Person.DomainModel/PersonPhoneModel.cs)
Rules: 
Auto-Generated Factories: [PersonModelFactory](https://github.com/NeatooDotNet/Neatoo/blob/main/Examples/Person/Person.DomainModel/Generated/Neatoo.RemoteFactory.FactoryGenerator/Neatoo.RemoteFactory.FactoryGenerator.FactoryGenerator/Person.DomainModel.PersonModelFactory.g.cs) and [PersonModelPhoneFactory](https://github.com/NeatooDotNet/Neatoo/blob/main/Examples/Person/Person.DomainModel/Generated/Neatoo.RemoteFactory.FactoryGenerator/Neatoo.RemoteFactory.FactoryGenerator.FactoryGenerator/Person.DomainModel.PersonPhoneModelFactory.g.cs)

##  Please leave feedback!
Thus far, this was a fun winter project. When I incorporated Roslyn source generators is when it seemed it might be something new and useful. My best most enjoyable secular project was done using CSLA. This is an attempt to re-create CSLA with a Dependency Injection and Async/Await centric approach. I highly recommend [the CSLA books](https://store.lhotka.net/).

[Please let me know](https://github.com/NeatooDotNet/Neatoo/issues):
- Does a framework like this already exists (besides [CSLA](https://cslanet.com/))?
- Would this be useful to you?
- What is useful/good?
- What is missing/bad?

Please be constructive.

## Recommended Reading

Ideas that shaped Neatoo:
- [CSLA](https://cslanet.com/)
- [Patterns of Enteprise Architecture](https://www.thriftbooks.com/w/patterns-of-enterprise-application-architecture_martin-fowler_david-rice/250298/?resultid=dcd84f2b-51ab-4e22-8e24-3c3a17de30bb#edition=3682851&idiq=4316361) - Domain Model (110) & Data Mapper (165)
- [Async Programming : Patterns for Asynchronous MVVM Applications: Data Binding](https://learn.microsoft.com/en-us/archive/msdn-magazine/2014/march/async-programming-patterns-for-asynchronous-mvvm-applications-data-binding)
- [Object–relational impedance mismatch](https://en.wikipedia.org/wiki/Object%E2%80%93relational_impedance_mismatch)
- [Anemic Domain Model](https://martinfowler.com/bliki/AnemicDomainModel.html)

*No DTOs to get the Domain Model from the server to the client. If you use the repository pattern you may have DTOs for you DAL layer interaction.
