# Neatoo

A .NET Domain Models framework for Blazor and WPF powered by Roslyn source generators.

[![NuGet](https://img.shields.io/nuget/v/Neatoo.svg)](https://www.nuget.org/packages/Neatoo/)

## Overview

Neatoo provides a DDD-focused framework for building domain models with source-generated properties, validation, business rules, parent-child relationships, and change tracking. Partial property declarations are completed by source generators that wire up backing fields, PropertyChanged events, and validation triggers. RemoteFactory integration enables seamless client-server state transfer for Blazor and distributed applications.

Built for expert .NET developers working with Domain-Driven Design patterns.

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

The example above demonstrates Neatoo's core features: partial property declarations, automatic validation, business rules, parent-child relationships, and change tracking. Source generators produce backing fields, factory methods, and metadata at compile time.

## Key Features

- **Source-generated properties** - Partial properties generate backing fields, PropertyChanged events, and validation triggers
- **Validation system** - Attribute-based validation (Required, Range, etc.), custom validation rules, async validation with external service calls, automatic error aggregation across properties
- **Business rules engine** - Declarative business rules with cross-property dependencies, action rules for computed properties, conditional execution, and rule ordering
- **Parent-child graphs** - Automatic parent tracking, cascade validation, cascade modification state, and aggregate boundary enforcement
- **Change tracking** - IsModified, IsSelfModified, IsNew, IsDeleted with cascade to aggregate root and ModifiedProperties collection
- **Entity lifecycle** - IsNew/IsDeleted state management for persistence coordination, with optional RemoteFactory integration for client-server scenarios
- **Blazor integration** - MudNeatoo package with two-way binding, validation display, and form integration
- **Collection support** - EntityListBase and ValidateListBase with parent cascade and collection validation

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
    public Task Insert([Service] ICustomerRepository repo)
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
