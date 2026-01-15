[![Logo](https://raw.githubusercontent.com/NeatooDotNet/Neatoo/main/Logo_411.png 'Logo')](http://neatoo.net)

# Neatoo

A comprehensive DDD framework for Blazor and WPF applications with complex business rules.

Define your validation and business logic once. It runs in the browser for immediate feedback and on the server for security.

**What You Get:**
- **Validation** - Rules run on property change with field-level messages
- **Meta-Properties** - IsValid, IsModified, IsBusy, IsSavable for UI binding
- **Async/Await** - First-class async validation and task coordination
- **Dependency Injection** - Services injected into entities and rules
- **Source Generation** - Factories, serialization, and boilerplate generated at compile-time
- **Client-Server Sync** - Same rules run in browser and on server
- **Authorization** - Integrated authorization checks

[Full documentation at Neatoo.net](http://neatoo.net) · [Discord](https://discord.gg/M3dVuZkG) · [NuGet](https://www.nuget.org/packages/Neatoo)

## Why I Built Neatoo

I'm an enterprise LOB developer focused on business processes and constraints.

My most successful project used CSLA with WPF. We cohesively represented domain modeling, business constraints, and authorization in a layer that applied to both the UI and the server. It just worked.

Then came years of web development - and the inability to share libraries between client and server. None of my projects felt as fluid. Too much business logic duplicated between UI and server APIs. Too many times it wasn't duplicated and lived only in the UI. DDD concepts got lost in translation between layers. I'm also tired of applications that show generic error banners instead of validation on specific form fields - users deserve to know exactly what's wrong and where. Then Blazor arrived, and we can share libraries again.

So I built Neatoo.

It's a modern take on CSLA - built from scratch with async/await and dependency injection as first-class citizens. Roslyn source generators mean compile-time errors instead of runtime surprises, and visible code instead of reflection magic. Now I can build apps the way that works best for me.

## When Neatoo Fits

- Blazor or WPF applications
- Complex business rules and validation
- Rich forms with real-time field-level feedback
- Aggregates with parent-child relationships
- Business logic that must be consistent between UI and server

## When to Look Elsewhere

Neatoo isn't for everything. Consider simpler tools if:

- **Outside Blazor/WPF** - The meta-properties (IsBusy, IsModified, IsSavable) are designed for rich client UI binding
- **High transaction, minimal constraints** - If you're moving data fast without complex validation, the overhead isn't worth it
- **Simple CRUD APIs** - POCOs + FluentValidation is simpler
- **Read-heavy reporting/analytics** - You want projections and DTOs, not tracked entities
- **Stateless microservices** - Infrastructure overkill for thin domain logic
- **Event sourcing** - Neatoo is state-based, not event-based

## What About CQRS and Microservices?

These patterns solve different problems than Neatoo.

| Pattern | What It Solves | What It Doesn't Solve |
|---------|----------------|----------------------|
| CQRS | Read/write scaling, optimized query models | Where business logic lives |
| Microservices | Deployment independence, team autonomy | How to model business rules |
| Event Sourcing | Audit trails, temporal queries | Validation, UI state |

The business logic still has to live *somewhere*. In most architectures it ends up scattered across handlers, services, and controllers - or duplicated between client and server.

Neatoo gives that logic a home.

## Why Not Just Use AI to Generate This?

AI can generate boilerplate. But Neatoo's value isn't in reducing lines of code - source generators already do that.

The value is in **runtime semantics**:
- `IsModified` automatically propagates from children to parent
- `IsBusy` tracks async validation in progress
- `IsSavable` combines modified + valid + not busy in one property
- `WaitForTasks()` coordinates async operations before save

AI would have to generate and maintain this same infrastructure for each project. You lose the ability to communicate a clear architecture. Neatoo gives you a foundation for needs common to LOB applications without reinventing them every time.

## Documentation

Full documentation at [Neatoo.net](http://neatoo.net) or in the [docs](docs/) folder:

### Getting Started
- [Quick Start](docs/quick-start.md) - Get up and running in 10 minutes
- [Installation](docs/installation.md) - NuGet packages and project setup

### Core Concepts
- [Aggregates and Entities](docs/aggregates-and-entities.md) - Creating domain model classes
- [Validation and Rules](docs/validation-and-rules.md) - Business rule implementation
- [Factory Operations](docs/factory-operations.md) - Create, Fetch, Insert, Update, Delete lifecycle
- [Property System](docs/property-system.md) - Getter/Setter, IProperty, meta-properties
- [Collections](docs/collections.md) - EntityListBase for child entity collections

### UI Integration
- [Blazor Binding](docs/blazor-binding.md) - Data binding and MudNeatoo components
- [Meta-Properties Reference](docs/meta-properties.md) - IsBusy, IsValid, IsModified, IsSavable

### Advanced Topics
- [Remote Factory Pattern](docs/remote-factory.md) - Client-server state transfer
- [Mapper Methods](docs/mapper-methods.md) - MapFrom, MapTo, MapModifiedTo
- [DDD Analysis](docs/DDD-Analysis.md) - How Neatoo aligns with Domain-Driven Design

### Reference
- [Release Notes](docs/release-notes/index.md) - Version history and changelog
- [Troubleshooting](docs/troubleshooting.md) - Common issues and solutions
