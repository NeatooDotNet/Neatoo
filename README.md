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
- **Transparent client-server transfer** — RemoteFactory moves domain object state across the wire through a single controller endpoint. `[Remote]` marks a client entry point: the call crosses to the server, where the repositories live. No controller-per-operation, no routing boilerplate.
- **Source-generated properties** — Partial properties generate backing fields, `PropertyChanged` events, validation triggers, and change tracking at compile time. Zero reflection.
- **Validation and business rules** — Attribute validation (`[Required]`, `[Range]`), inline rules, class-based rules, async rules that reach the server through a command, and error aggregation across the entire object graph.
- **Change tracking** — `IsModified`, `IsSelfModified`, and `IsDeleted` cascade through parent-child graphs to the aggregate root (`IsNew` is per-object routing state and deliberately does not). `ModifiedProperties` tells you exactly what changed. `IsModified` and `IsNew` answer different questions on purpose: a freshly created entity is savable but *not* modified, so unsaved-changes guards stay quiet until the user actually edits something ([why](docs/guides/change-tracking.md#why-isnew-is-not-part-of-ismodified)).
- **DDD aggregate support** — `EntityBase` for persistent entities, `ValidateBase` for value objects, `EntityListBase` for child collections. Interface-first design enforces aggregate boundaries at compile time: roots expose `Save()`, children do not.
- **Blazor integration** — MudNeatoo components bind directly to domain model properties with two-way binding, validation display, and form integration out of the box.

## Example

For a complete working application with domain model, validation rules, authorization, persistence, unit tests, and a Blazor UI, see the [Person Example](src/Examples/Person/).

Declare partial properties. Add validation attributes and business rules in the constructor. Source generators handle the rest — backing fields, `PropertyChanged` events, change tracking, factory methods, and client-server state transfer are all produced at compile time. No reflection, no runtime magic.

<!-- snippet: skill-validation-attributes-and-rules -->
<a id='snippet-skill-validation-attributes-and-rules'></a>
```cs
[Required(ErrorMessage = "Street is required")]
[StringLength(100)]
public partial string? Street { get; set; }

[Required(ErrorMessage = "City is required")]
[StringLength(50)]
public partial string? City { get; set; }

[Required(ErrorMessage = "State is required")]
[StringLength(2, MinimumLength = 2, ErrorMessage = "State must be 2 characters")]
public partial string? State { get; set; }

[Required(ErrorMessage = "Zip code is required")]
[RegularExpression(@"^\d{5}(-\d{4})?$", ErrorMessage = "Invalid zip code format")]
public partial string? ZipCode { get; set; }

[Required(ErrorMessage = "Address type is required")]
public partial string? AddressType { get; set; } // "Home", "Work", "Other"

public Address(IEntityBaseServices<Address> services) : base(services)
{
    // Validation rules
    RuleManager.AddValidation(
        t => !new[] { "Home", "Work", "Other" }.Contains(t.AddressType)
            ? "Address type must be Home, Work, or Other"
            : string.Empty,
        t => t.AddressType);
}
```
<sup><a href='/src/Design/Design.Domain/Entities/Address.cs#L30-L59' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-validation-attributes-and-rules' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## What a DDD Framework Gives You

### Updates Without Guesswork

With transaction scripts, the client sends a partial payload and the server has to merge it with the current database state — figuring out what actually changed, what to update, and what to leave alone. That merge logic is fragile and grows with every new field.

With Neatoo, the full aggregate comes back to the server with its state intact. Every entity tracks `IsNew`, `IsModified`, `IsDeleted`, and `ModifiedProperties` through the entire object graph. When you reach your `[Insert]`, `[Update]`, or `[Delete]` factory methods, there's no guessing — the domain model already knows exactly what changed and what needs to persist.

### Business Logic That Can't Diverge

In most applications, business logic drifts. Critical rules get duplicated between the UI and server, lighter rules live only in the UI, and the two definitions slowly diverge until they contradict each other.

Neatoo puts validation and business rules in the domain model — one definition, compiled into both client and server. Rules are engineered to work with data-binding: when a property changes, dependent validation and action rules fire immediately, updating the UI in real time. The same rules execute again on the server before persistence. They can't diverge because they're the same code.

### Authorization Defined Once, Enforced Everywhere

Authorization follows the same pattern. Client-side checks control what the UI shows — can this user create an order? Edit this field? Delete this record? Server-side checks guard the actual operations. In most applications these are separate implementations that fall out of sync.

RemoteFactory's `[AuthorizeFactory]` attributes define authorization on the factory operation itself. RemoteFactory always enforces these on the server, regardless of what the client sends. The same definitions also power `CanCreate`, `CanFetch`, `CanUpdate`, and `CanDelete` methods that the UI consumes to show or hide actions, disable buttons, and control navigation. One definition drives both enforcement and UI behavior.

### Field-Level Validation Without the Plumbing

Field-level validation — errors displayed next to the input that caused them, updating as the user works — is better UX than a banner at the top of the page. But it fell out of fashion because the plumbing is hard: per-property error tracking, real-time updates on change, cross-field dependencies, and aggregating validity across a parent-child graph.

Neatoo makes it the path of least resistance. Validation rules are declared per-property and fire automatically when bound values change. Errors propagate through the object graph — `IsValid` on the aggregate root reflects every field in every child. MudNeatoo components display errors inline with no extra wiring. You get field-level validation by default, not as an afterthought.

## Installation

Install the Neatoo package via NuGet.

```bash
dotnet add package Neatoo
```

For Blazor support, install the MudNeatoo package:

```bash
dotnet add package Neatoo.Blazor.MudNeatoo
```

Neatoo targets .NET 9.0 and 10.0.

## Quick Start

Every entity gets a public interface; the concrete class is `internal`. A root's interface extends `IEntityRoot`, which exposes `IsSavable` and `Save()`:

<!-- snippet: skill-quick-start-interface -->
<a id='snippet-skill-quick-start-interface'></a>
```cs
/// <summary>
/// Aggregate root interface. Extends IEntityRoot: exposes IsSavable and Save().
/// </summary>
public interface IProduct : IEntityRoot
{
    Guid Id { get; }
    string? Name { get; set; }
    decimal Price { get; set; }
}
```
<sup><a href='/src/Design/Design.Domain/Entities/Product.cs#L16-L26' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-quick-start-interface' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The entity declares partial properties with validation attributes and the factory operations. `[Create]` is local; `[Fetch]`, `[Insert]`, `[Update]` and `[Delete]` are `[Remote]`, so the client's call crosses to the server where the repository is. `[Insert]` and `[Update]` re-run the rules on the server and refuse an invalid entity:

<!-- snippet: skill-quick-start -->
<a id='snippet-skill-quick-start'></a>
```cs
/// <summary>
/// Demonstrates: the minimal aggregate root. Concrete is internal; consumers
/// hold IProduct.
/// </summary>
[Factory]
internal partial class Product : EntityBase<Product>, IProduct
{
    public partial Guid Id { get; set; }

    [Required(ErrorMessage = "Name is required")]
    public partial string? Name { get; set; }

    [Range(0, 1000000, ErrorMessage = "Price cannot be negative")]
    public partial decimal Price { get; set; }

    public Product(IEntityBaseServices<Product> services) : base(services) { }

    // Local: creating a product needs nothing from the server
    [Create]
    public void Create() { }

    // [Remote]: the client fetches this root, so the call crosses to the
    // server, where the repository resolves. Returning false makes the
    // generated factory return null: "no such product" is an answer.
    [Remote]
    [Fetch]
    internal bool Fetch(Guid id, [Service] IProductRepository repository)
    {
        var row = repository.Get(id);
        if (row == null)
        {
            return false;
        }

        // Paused for the length of the body: assignment is a clean baseline load
        Id = row.Id;
        Name = row.Name;
        Price = row.Price;
        return true;
    }

    [Remote]
    [Insert]
    internal async Task Insert([Service] IProductRepository repository)
    {
        // Re-run the rules on the server and refuse an invalid aggregate.
        // Throw, never return: after [Insert] returns, the framework marks
        // the entity saved whether or not anything was written.
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        Id = Guid.NewGuid();  // the entity sets its own key

        var row = new ProductRow();
        MapTo(row);
        repository.Add(row);
        repository.SaveChanges();
    }

    [Remote]
    [Update]
    internal async Task Update([Service] IProductRepository repository)
    {
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        var row = repository.Get(Id)
            ?? throw new KeyNotFoundException($"Product {Id} not found");

        MapTo(row);
        repository.SaveChanges();
    }

    [Remote]
    [Delete]
    internal void Delete([Service] IProductRepository repository)
    {
        var row = repository.Get(Id)
            ?? throw new KeyNotFoundException($"Product {Id} not found");

        repository.Remove(row);
        repository.SaveChanges();
    }

    private void MapTo(ProductRow row)
    {
        row.Id = Id;
        row.Name = Name!;
        row.Price = Price;
    }
}
```
<sup><a href='/src/Design/Design.Domain/Entities/Product.cs#L28-L126' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-quick-start' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Using it: a created object is new and not modified; an edit makes it savable once its rules pass:

<!-- snippet: skill-quick-start-create -->
<a id='snippet-skill-quick-start-create'></a>
```cs
[TestMethod]
public async Task Create_ThenEdit_IsSavable()
{
    var product = _factory.Create();
    Assert.IsTrue(product.IsNew);
    Assert.IsFalse(product.IsModified, "A created object holds no user work");

    product.Name = "Widget";
    product.Price = 9.99m;
    await product.WaitForTasks();

    Assert.IsTrue(product.IsValid);
    Assert.IsTrue(product.IsSavable);
}
```
<sup><a href='/src/Design/Design.Tests/EntityTests/ProductTests.cs#L35-L50' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-quick-start-create' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`Save()` routes to `Insert` or `Update` on `IsNew` and returns the saved instance; a `Fetch` is a clean baseline:

<!-- snippet: skill-quick-start-save -->
<a id='snippet-skill-quick-start-save'></a>
```cs
[TestMethod]
public async Task Save_ThenFetch_RoundTrips()
{
    var product = _factory.Create();
    product.Name = "Widget";
    product.Price = 9.99m;
    await product.WaitForTasks();

    // Save returns the saved instance; keep that one
    product = (IProduct)await product.Save();
    Assert.IsFalse(product.IsNew);
    Assert.IsFalse(product.IsModified);

    var fetched = await _factory.Fetch(product.Id);
    Assert.IsNotNull(fetched);
    Assert.AreEqual("Widget", fetched.Name);
    Assert.IsFalse(fetched.IsModified, "A fetched object is a clean baseline");
}
```
<sup><a href='/src/Design/Design.Tests/EntityTests/ProductTests.cs#L52-L71' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-quick-start-save' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

This example shows:
- Interface-first design: a public `IProduct : IEntityRoot` and an `internal` concrete
- Partial property declarations with source-generated backing fields and change tracking
- Attribute-based validation (`Required`, `Range`)
- Persistence state (`IsNew`, `IsModified`, `IsSavable`) and `Save()` routing
- `[Remote]` factory operations whose `[Service]` repository resolves on the server
- The server gate: rules re-run before the write, and `SaveOperationException` refuses an invalid entity

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

**UPDATED:** 2026-10-06
