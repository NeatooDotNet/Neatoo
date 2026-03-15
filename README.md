# Neatoo

A Domain-Driven Design framework for 3-tier .NET Blazor applications, powered by Roslyn source generators.

[![NuGet](https://img.shields.io/nuget/v/Neatoo.svg)](https://www.nuget.org/packages/Neatoo/)

## Why Neatoo?

Blazor compiles the same .NET code to both client and server. The same types, the same definitions, the same runtime — on both sides of the wire. So why are you still writing DTOs, mapping layers, and serialization boilerplate as if they were different?

They aren't. Neatoo takes that insight to its conclusion.

**Your domain model is the contract.** Define your entities, validation rules, and business logic once. Neatoo's source generators wire up property backing fields, change tracking, and validation triggers at compile time. When it's time to save, RemoteFactory transfers domain object state to the server, executes your persistence logic, and returns the result — no DTOs, no mapping, no translation layer. Every operation flows through a single controller endpoint. No more writing a new controller method for every create, fetch, update, and delete.

The domain model you bind to your Blazor form is the same object that validates user input, tracks what changed, and persists to the database. One model, one endpoint, front to back.

## Key Features

- **One model, front to back** — Your domain model binds to the Blazor UI, validates input, tracks changes, and persists to the database. No DTOs, no mapping layers, no translation.
- **Transparent client-server transfer** — RemoteFactory moves domain object state across the wire through a single controller endpoint. Mark a method `[Remote]` and it runs on the server. No controller-per-operation, no routing boilerplate.
- **Source-generated properties** — Partial properties generate backing fields, `PropertyChanged` events, validation triggers, and change tracking at compile time. Zero reflection.
- **Validation and business rules** — Attribute validation (`[Required]`, `[Range]`), inline rules, async rules that call external services, and automatic error aggregation across the entire object graph.
- **Change tracking** — `IsModified`, `IsSelfModified`, `IsNew`, `IsDeleted` cascade through parent-child graphs to the aggregate root. `ModifiedProperties` tells you exactly what changed.
- **DDD aggregate support** — `EntityBase` for persistent entities, `ValidateBase` for value objects, `EntityListBase` for child collections. Interface-first design enforces aggregate boundaries at compile time.
- **Blazor integration** — MudNeatoo components bind directly to domain model properties with two-way binding, validation display, and form integration out of the box.

## Example

Declare partial properties. Add validation attributes and business rules. Source generators handle the rest — backing fields, `PropertyChanged` events, change tracking, factory methods, and client-server state transfer are all produced at compile time. No reflection, no runtime magic.

<!-- snippet: readme-teaser -->
<a id='snippet-readme-teaser'></a>
```cs
// Define an Employee aggregate root with validation and business rules
[Factory]
public partial class Employee : EntityBase<Employee>
{
    public Employee(IEntityBaseServices<Employee> services) : base(services)
    {
        // Business rule: Full name is computed from first and last name
        RuleManager.AddAction(
            e => { e.FullName = $"{e.FirstName} {e.LastName}"; },
            e => e.FirstName, e => e.LastName);

        // Validation rule: Salary must be positive
        RuleManager.AddValidation(
            e => e.Salary > 0 ? "" : "Salary must be positive",
            e => e.Salary);
    }

    [Required]
    public partial string FirstName { get; set; }

    [Required]
    public partial string LastName { get; set; }

    public partial string FullName { get; set; }

    public partial decimal Salary { get; set; }

    // Child collection with automatic parent tracking
    public partial IAddressList Addresses { get; set; }

    [Create]
    public void Create() { }
}

public interface IAddress : IEntityBase { }

[Factory]
public partial class Address : EntityBase<Address>, IAddress
{
    public Address(IEntityBaseServices<Address> services) : base(services) { }

    [Required]
    public partial string Street { get; set; }

    public partial string City { get; set; }

    [Create]
    public void Create() { }
}

public interface IAddressList : IEntityListBase<IAddress> { }

public class AddressList : EntityListBase<IAddress>, IAddressList { }
```
<sup><a href='/src/samples/ReadmeSamples.cs#L10-L64' title='Snippet source file'>snippet source</a> | <a href='#snippet-readme-teaser' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## What a DDD Framework Gives You

### Updates Without Guesswork

With transaction scripts, the client sends a partial payload and the server has to merge it with the current database state — figuring out what actually changed, what to update, and what to leave alone. That merge logic is fragile and grows with every new field.

With Neatoo, the full aggregate comes back to the server with its state intact. Every entity tracks `IsNew`, `IsModified`, `IsDeleted`, and `ModifiedProperties` through the entire object graph. When you reach your `[Insert]`, `[Update]`, or `[Delete]` factory methods, there's no guessing — the domain model already knows exactly what changed and what needs to persist.

### Business Logic That Can't Diverge

In most applications, business logic drifts. Critical rules get duplicated between the UI and server, lighter rules live only in the UI, and the two definitions slowly diverge until they contradict each other.

Neatoo puts validation and business rules in the domain model — one definition, compiled into both client and server. Rules are engineered to work with data-binding: when a property changes, dependent validation and action rules fire immediately, updating the UI in real time. The same rules execute again on the server during persistence. They can't diverge because they're the same code.

### Authorization Defined Once, Enforced Everywhere

Authorization follows the same pattern. Client-side checks control what the UI shows — can this user create an order? Edit this field? Delete this record? Server-side checks guard the actual operations. In most applications these are separate implementations that fall out of sync.

Neatoo's `[AuthorizeFactory]` attributes define authorization on the factory operation itself. RemoteFactory always enforces these on the server, regardless of what the client sends. The same definitions also power `CanCreate`, `CanFetch`, `CanUpdate`, and `CanDelete` methods that the UI consumes to show or hide actions, disable buttons, and control navigation. One definition drives both enforcement and UI behavior.

## Installation

Install the Neatoo package via NuGet.

```bash
dotnet add package Neatoo
```

For Blazor support, install the MudNeatoo package:

```bash
dotnet add package Neatoo.Blazor.MudNeatoo
```

Neatoo targets .NET 8.0, 9.0, and 10.0.

## Quick Start

Create a domain object by inheriting from ValidateBase or EntityBase and declaring partial properties. Source generators handle the rest.

<!-- snippet: readme-quick-start -->
<a id='snippet-readme-quick-start'></a>
```cs
// 1. ValidateBase: For objects that need validation without persistence
[Factory]
public partial class CustomerSearch : ValidateBase<CustomerSearch>
{
    public CustomerSearch(IValidateBaseServices<CustomerSearch> services) : base(services)
    {
        // Inline validation rule
        RuleManager.AddValidation(
            c => !string.IsNullOrEmpty(c.SearchTerm) ? "" : "Search term is required",
            c => c.SearchTerm);
    }

    public partial string SearchTerm { get; set; }

    [Range(1, 100)]
    public partial int MaxResults { get; set; }

    [Create]
    public void Create() { }
}

// 2. EntityBase: For domain entities with full lifecycle support
[Factory]
public partial class Customer : EntityBase<Customer>
{
    public Customer(IEntityBaseServices<Customer> services) : base(services) { }

    [Required(ErrorMessage = "Customer name is required")]
    public partial string Name { get; set; }

    [EmailAddress]
    public partial string Email { get; set; }

    // Child entity with automatic parent cascade
    public partial IOrderList Orders { get; set; }

    // RemoteFactory method: Runs on server, result transferred to client
    [Remote]
    [Insert]
    internal Task Insert([Service] ICustomerRepository repo)
    {
        // Persistence logic here
        return Task.CompletedTask;
    }

    [Create]
    public void Create() { }
}

public interface IOrder : IEntityBase { }

[Factory]
public partial class Order : EntityBase<Order>, IOrder
{
    public Order(IEntityBaseServices<Order> services) : base(services) { }

    public partial decimal Amount { get; set; }

    [Create]
    public void Create() { }
}

public interface IOrderList : IEntityListBase<IOrder> { }

public class OrderList : EntityListBase<IOrder>, IOrderList { }

// Mock repository interface for the sample
public interface ICustomerRepository { }
```
<sup><a href='/src/samples/ReadmeSamples.cs#L66-L135' title='Snippet source file'>snippet source</a> | <a href='#snippet-readme-quick-start' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

This example shows:
- ValidateBase inheritance for validation, business rules, change tracking, and property metadata
- EntityBase inheritance adds persistence lifecycle state (IsNew, IsDeleted, IsModified)
- Partial property declarations with source-generated backing fields and change tracking
- Attribute-based validation (Required, EmailAddress, Range)
- Custom business rules (inline validation rules in constructor)
- RemoteFactory methods for client-server persistence operations
- Parent-child relationships with automatic parent tracking and cascade validation

## Documentation

Comprehensive guides are available in the [docs/](docs/) directory:

- **[Getting Started](docs/getting-started.md)** - Installation through first working aggregate
- **[Validation Guide](docs/guides/validation.md)** - ValidateBase, validation rules, and error handling
- **[Entities Guide](docs/guides/entities.md)** - EntityBase, aggregate roots, and entity lifecycle
- **[Collections Guide](docs/guides/collections.md)** - EntityListBase and ValidateListBase
- **[Properties Guide](docs/guides/properties.md)** - Property system and source generators
- **[Business Rules Guide](docs/guides/business-rules.md)** - Business rules engine and rule execution
- **[Change Tracking Guide](docs/guides/change-tracking.md)** - IsModified, IsSelfModified, state management, and cascade
- **[Async Guide](docs/guides/async.md)** - Async validation and task coordination
- **[Parent-Child Guide](docs/guides/parent-child.md)** - Parent-child graphs and aggregate boundaries
- **[Blazor Guide](docs/guides/blazor.md)** - MudNeatoo Blazor integration
- **[RemoteFactory Guide](docs/guides/remote-factory.md)** - Client-server state transfer
- **[API Reference](docs/reference/api.md)** - Complete API documentation

## Framework Comparison

Neatoo is inspired by CSLA.NET but redesigned around Roslyn source generators for modern .NET development. Where CSLA relies on runtime reflection and manual coding patterns, Neatoo generates boilerplate at compile time while providing stronger type safety and better IDE support.

## License

MIT License - see [LICENSE](LICENSE) for details.

Copyright (c) 2025 NeatooDotNet

---

**UPDATED:** 2026-01-24
