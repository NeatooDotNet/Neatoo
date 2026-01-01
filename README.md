[![Logo](https://raw.githubusercontent.com/NeatooDotNet/Neatoo/main/Logo_411.png 'Logo')](http://neatoo.net)

Neatoo is a DDD Aggregate Framework for .NET C# to create bindable and serializable Aggregate Entity Graphs for Blazor and WPF.
With Neatoo you define the business logic once and reuse it in both the UI and the Application Service. 
Allowing you to do more with less code while maintaining consistency. 

Neatoo is for applications with complex business logic. 
A robust user experience is delivered by the bindable properties, property messages and authorization check methods (via [RemoteFactory](https://github.com/NeatooDotNet/RemoteFactory) integration).
The user knows exactly what they can do, what they need to do to save and that the save will succeed!

**Write complex Aggregate Entity Graphs with no DTOs and only one controller!**

### Full documentation at [Neatoo.net](http://neatoo.net)

# Why is Neatoo New?

Neatoo leverages Blazor WebAssembly and Roslyn Source Generators to deliver shared business logic across client and server with no DTO layer.

## Blazor and WebAssembly

With WebAssembly, your .NET domain objects with their validation rules and business logic run *in the browser*.
No need to define individual endpoints for each behavior - the client already has the complete domain model.
RemoteFactory transfers only the entity data; business logic executes locally for immediate validation feedback.

## Roslyn Source Generators

Neatoo uses Roslyn Source Generators to create readable, performant factories specific to each entity.
The 3-Tier Factories handle client-server serialization automatically.
Authorization is integrated within the Factory (via [RemoteFactory](https://github.com/NeatooDotNet/RemoteFactory)).
Source generators mean compile-time errors instead of runtime surprises - debuggable, visible code.

# Features

* Validation
* Authorization (via [RemoteFactory](https://github.com/NeatooDotNet/RemoteFactory))
* Bindable Properties
* Bindable Meta-Properties
* 3-Tier Factory
* Mapper Methods
* Interface Serialization
* Dependency Injection
* Async/Await
* Live Source Generation

## Documentation

Full documentation can be found in the [docs](docs/) folder:

### Getting Started
* [Quick Start](docs/quick-start.md) - Get up and running in 10 minutes
* [Installation](docs/installation.md) - NuGet packages and project setup

### Core Concepts
* [Aggregates and Entities](docs/aggregates-and-entities.md) - Creating domain model classes
* [Validation and Rules](docs/validation-and-rules.md) - Business rule implementation
* [Factory Operations](docs/factory-operations.md) - Create, Fetch, Insert, Update, Delete lifecycle
* [Property System](docs/property-system.md) - Getter/Setter, IProperty, meta-properties
* [Collections](docs/collections.md) - EntityListBase for child entity collections

### UI Integration
* [Blazor Binding](docs/blazor-binding.md) - Data binding and MudNeatoo components
* [Meta-Properties Reference](docs/meta-properties.md) - IsBusy, IsValid, IsModified, IsSavable

### Advanced Topics
* [Remote Factory Pattern](docs/remote-factory.md) - Client-server state transfer
* [Mapper Methods](docs/mapper-methods.md) - MapFrom, MapTo, MapModifiedTo
* [DDD Analysis](docs/DDD-Analysis.md) - How Neatoo aligns with Domain-Driven Design

### Reference
* [Release Notes](docs/release-notes/index.md) - Version history and changelog
* [Troubleshooting](docs/troubleshooting.md) - Common issues and solutions

### Planned Features
* [Lazy Loading Pattern](docs/lazy-loading-pattern.md) - Planned pattern for on-demand child object loading
* [Lazy Loading Analysis](docs/lazy-loading-analysis.md) - Technical analysis of lazy loading infrastructure

[Discord](https://discord.gg/M3dVuZkG)
[Nuget](https://www.nuget.org/packages/Neatoo)

## Neatoo Lifecycle

This is a common lifecycle for a Neatoo Aggregate Entity Graph to and from the database using a 3-Tier infrastructure.

![Lifecycle](https://raw.githubusercontent.com/NeatooDotNet/Neatoo/main/AggregateLifecycle_960.gif)


### Full documentation at [Neatoo.net](http://neatoo.net)
