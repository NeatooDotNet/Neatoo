# Neatoo Framework Documentation

Neatoo provides bindable, serializable domain objects for Blazor and WPF applications with shared business logic across client and server. Define validation rules and domain behavior once - they execute identically in the browser and on the server.

> **Primary UI Integration:** Blazor (with optional MudNeatoo component library)
>
> **WPF Support:** The core framework works with WPF through standard `INotifyPropertyChanged` binding. See [Blazor Binding](blazor-binding.md) for patterns that apply to WPF with standard XAML bindings.

---

## Quick Navigation

| Section | Description |
|---------|-------------|
| [Documentation Index](#documentation-index) | All documentation pages organized by topic |
| [Class Hierarchy](#class-hierarchy) | Base classes and when to use each |
| [Key Features](#key-features) | Feature overview table |
| [Quick Example](#quick-example) | Complete aggregate root example |

---

## I Want To...

| Task | Go To |
|------|-------|
| **Get started quickly** | [Quick Start](quick-start.md) |
| **Create a domain entity** | [Aggregates and Entities](aggregates-and-entities.md) |
| **Add validation rules** | [Validation and Rules](validation-and-rules.md) |
| **Load/save data** | [Factory Operations](factory-operations.md) |
| **Bind to Blazor UI** | [Blazor Binding](blazor-binding.md) |
| **Create child collections** | [Entity Collections](collections.md) |
| **Set up client-server** | [Remote Factory](remote-factory.md) |
| **Debug an issue** | [Troubleshooting](troubleshooting.md) |

---

## Documentation Index

### Getting Started

- [Quick Start](quick-start.md) - Get up and running in 10 minutes
- [Installation](installation.md) - NuGet packages and project setup

### Core Concepts

- [Aggregates and Entities](aggregates-and-entities.md) - Creating domain model classes with EntityBase
- [Validation and Rules](validation-and-rules.md) - Business rule implementation with RuleBase
- [Factory Operations](factory-operations.md) - Create, Fetch, Insert, Update, Delete lifecycle
- [Property System](property-system.md) - Getter/Setter, IProperty, meta-properties

### Collections

- [Entity Collections](collections.md) - EntityListBase for child entity collections

### UI Integration

- [Blazor Binding](blazor-binding.md) - Data binding and MudNeatoo components
- [Meta-Properties Reference](meta-properties.md) - IsBusy, IsValid, IsModified, IsSavable

### Advanced Topics

- [Remote Factory Pattern](remote-factory.md) - Client-server state transfer
- [Mapper Methods](mapper-methods.md) - MapFrom, MapTo, MapModifiedTo

### Reference

- [Exceptions](exceptions.md) - Exception handling guide
- [Troubleshooting](troubleshooting.md) - Common issues and solutions
- [Release Notes](release-notes/index.md) - Version history and changelog

### Architecture

- [DDD Analysis](DDD-Analysis.md) - How Neatoo aligns with Domain-Driven Design

---

## Class Hierarchy

Neatoo provides a class hierarchy for building domain models. Users typically inherit from `ValidateBase<T>` or `EntityBase<T>`:

```
Base<T>                      - Internal base class (property management, parent-child relationships)
    |
ValidateBase<T>              - For non-persisted validated objects (criteria, filters, form input)
    |
EntityBase<T>                - For entities with identity, modification tracking, persistence


ListBase<I>                  - Internal base class (observable collection, parent-child relationships)
    |
ValidateListBase<I>          - For lists of validated non-persisted objects
    |
EntityListBase<I>            - For child entity collections with deleted item tracking
```

**Note:** Value Objects in Neatoo are simple POCO classes with `[Factory]` attribute - they do not inherit from any Neatoo base class. See [Aggregates and Entities](aggregates-and-entities.md) for details.

## Key Features

| Feature | Description |
|---------|-------------|
| **Bindable Properties** | INotifyPropertyChanged for UI binding |
| **Meta-Properties** | IsBusy, IsValid, IsModified, IsSavable |
| **Validation Rules** | Sync and async business rules |
| **3-Tier Factory** | Source-generated factories for client-server transfer |
| **Mapper Methods** | Auto-generated MapFrom/MapTo/MapModifiedTo |
| **Authorization** | Declarative authorization via [RemoteFactory](https://github.com/NeatooDotNet/RemoteFactory) |

## Quick Example

```csharp
// Define interface (required for factory generation)
public partial interface ICustomer : IEntityBase { }

// Define aggregate root
[Factory]
internal partial class Customer : EntityBase<Customer>, ICustomer
{
    public Customer(IEntityBaseServices<Customer> services) : base(services) { }

    // Partial properties - Neatoo generates backing code
    public partial string? Name { get; set; }
    public partial string? Email { get; set; }

    // Factory operations
    [Remote]
    [Fetch]
    public async Task Fetch(int id, [Service] IDbContext db)
    {
        var entity = await db.Customers.FindAsync(id);
        if (entity != null)
        {
            MapFrom(entity);
        }
    }

    [Remote]
    [Insert]
    public async Task Insert([Service] IDbContext db)
    {
        await RunRules();
        if (!IsSavable) return;

        var entity = new CustomerEntity();
        MapTo(entity);
        db.Customers.Add(entity);
        await db.SaveChangesAsync();
    }
}
```
