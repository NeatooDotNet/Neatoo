# Neatoo Framework Documentation

Welcome to the Neatoo documentation. Neatoo is a DDD framework for .NET providing base classes for aggregates, entities, value objects, and collections with validation, change tracking, and client-server state transfer.

## Documentation Structure

This documentation is organized into guides and reference material for expert .NET developers familiar with Domain-Driven Design.

### Folders

- **[guides/](guides/)** - Feature-specific guides covering validation, entities, collections, properties, business rules, change tracking, async patterns, parent-child relationships, Blazor integration, and RemoteFactory
- **[reference/](reference/)** - API reference documentation for core classes and interfaces

### Getting Started

- **[getting-started.md](getting-started.md)** - Installation and first working aggregate with ValidateBase and EntityBase

## Quick Navigation

**New to Neatoo?** Start with [Getting Started](getting-started.md) to install the package and create your first aggregate.

**Looking for specific features?** Browse the [guides/](guides/) folder for in-depth coverage of:
- Validation and business rules
- Entity and aggregate patterns
- Collection management
- Property system and source generators
- Change tracking and async validation
- Blazor UI integration
- Client-server state transfer with RemoteFactory

**Need API details?** See [reference/](reference/) for comprehensive API documentation.

## Framework Overview

Neatoo provides:
- **ValidateBase&lt;T&gt;** - Base class for value objects with validation rules
- **EntityBase&lt;T&gt;** - Base class for entities and aggregate roots
- **EntityListBase&lt;T&gt;** - Collections of entities with parent-child relationships
- **ValidateListBase&lt;T&gt;** - Collections of value objects with validation
- **Source generators** - Automatic property backing fields and factory methods
- **RemoteFactory integration** - Client-server state transfer and dependency injection
- **MudNeatoo** - Blazor components for two-way binding and validation display

---

**UPDATED:** 2026-01-24
