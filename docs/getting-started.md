# Getting Started

[Up](index.md)

Get up and running with Neatoo in minutes. This guide covers installation, your first validation object, and your first entity aggregate.

## Prerequisites

Neatoo targets modern .NET:
- .NET 9.0 or 10.0
- C# 13 or later (partial properties)
- Visual Studio 2022, Rider, or VS Code with C# Dev Kit

## Installation

Install the Neatoo NuGet package:

```bash
dotnet add package Neatoo
```

The package includes:
- Core runtime library
- Source generators for property backing fields
- Analyzers and code fixes for best practices
- RemoteFactory integration

## Your First Validation Object

Every Neatoo object gets a public interface, and the concrete class is `internal`: consumers, including the Blazor UI, see only the interface. A value object's interface extends `IValidateBase`:

<!-- snippet: skill-value-object-interface -->
<a id='snippet-skill-value-object-interface'></a>
```cs
/// <summary>
/// Interface for ValidateBase demo — input models and validation-only scenarios.
/// </summary>
public interface IDemoInputModel : IValidateBase
{
    string? Name { get; set; }
    string? Description { get; set; }
}
```
<sup><a href='/src/Design/Design.Domain/BaseClasses/IBaseClassInterfaces.cs#L12-L21' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-value-object-interface' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The class inherits `ValidateBase<T>` and declares `partial` properties — Neatoo's source generator creates the backing fields and wires up property change notifications and validation. Validation attributes on the properties and rules in the constructor are the validation:

<!-- snippet: skill-value-object -->
<a id='snippet-skill-value-object'></a>
```cs
/// <summary>
/// Demonstrates: ValidateBase&lt;T&gt; for input models and validation-only scenarios.
///
/// Key points:
/// - Provides validation infrastructure without persistence tracking
/// - IsValid/IsSelfValid track validation state
/// - IsBusy tracks async operations
/// - PauseAllActions()/ResumeAllActions() control event firing
/// - RuleManager provides fluent API for adding rules
/// </summary>
[Factory]
internal partial class DemoInputModel : ValidateBase<DemoInputModel>, IDemoInputModel
{
    public partial string? Name { get; set; }

    public partial string? Description { get; set; }

    public DemoInputModel(IValidateBaseServices<DemoInputModel> services) : base(services)
    {
        // Rules are added in the constructor; they run when a trigger property changes
        RuleManager.AddValidation(
            t => string.IsNullOrWhiteSpace(t.Name) ? "Name is required" : string.Empty,
            t => t.Name);
    }

    // Local: creating an object needs nothing from the server
    [Create]
    public void Create()
    {
    }

    [Create]
    public void Create(string name)
    {
        Name = name;
    }

    // Loaded by the list's [Fetch]: existing data comes through [Fetch],
    // never [Create]. Internal - only server-side code calls it.
    [Fetch]
    internal void Fetch(string name)
    {
        Name = name;
    }
}
```
<sup><a href='/src/Design/Design.Domain/BaseClasses/AllBaseClasses.cs#L93-L139' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-value-object' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The attributes are standard `System.ComponentModel.DataAnnotations` attributes (`Required`, `StringLength`, `MinLength`, `MaxLength`, `RegularExpression`, `Range`, `EmailAddress`). Neatoo runs them when a property is set.

## Verify Source Generation

After building, check that source generation succeeded:

1. In Visual Studio: Expand the project node → Dependencies → Analyzers → Neatoo.BaseGenerator
2. In Rider: Look for generated files in the project tree under "Generated Code"
3. Or set `<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>` in the project file and look under `obj/.../generated/`

The generated files are named `{Namespace}.{Type}.g.cs` (for example `MyApp.Domain.Address.g.cs`) and contain the property implementations.

## Run Validation

Validation rules execute when a property is set. Each property is backed by its own property object, reached through the indexer, that carries `IsValid`, `PropertyMessages`, `IsBusy` and `IsReadOnly`; the object's own `IsValid` and `PropertyMessages` aggregate every property.

> **Note:** The code samples below are MSTest tests from the Neatoo repository's design tests. They resolve the generated factories from a DI scope (`DesignTestServices.GetScope()`). In your application you inject the factory interface into the Blazor component instead.

<!-- snippet: skill-property-metadata -->
<a id='snippet-skill-property-metadata'></a>
```cs
[TestMethod]
public void Indexer_ExposesPropertyMetadata()
{
    var entity = _factory.Create();

    // Each partial property is backed by its own property object
    var nameProperty = entity["Name"];

    entity.Name = "";  // Name is required
    Assert.IsFalse(nameProperty.IsValid);
    Assert.IsTrue(nameProperty.PropertyMessages.Count > 0);
    Assert.IsFalse(nameProperty.IsBusy);
    Assert.IsFalse(nameProperty.IsReadOnly);

    // The object aggregates every property's messages
    Assert.IsTrue(entity.PropertyMessages.Any(m => m.Property.Name == "Name"));

    entity.Name = "Set";
    Assert.IsTrue(nameProperty.IsValid);
    Assert.AreEqual(0, nameProperty.PropertyMessages.Count);

    // Strongly typed access by casting
    var typed = (Neatoo.IValidateProperty<string?>)nameProperty;
    Assert.AreEqual("Set", typed.Value);
}
```
<sup><a href='/src/Design/Design.Tests/PropertyTests/PropertyBasicsTests.cs#L83-L109' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-property-metadata' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`IsValid` includes every child object; `IsSelfValid` is this object alone. A child's messages surface on the parent:

<!-- snippet: skill-is-valid-vs-self-valid -->
<a id='snippet-skill-is-valid-vs-self-valid'></a>
```cs
[TestMethod]
public async Task InvalidChild_MakesParentInvalid_ButNotSelfInvalid()
{
    var parent = _scope.GetRequiredService<IValidationStateDemoFactory>().Create();
    parent.RequiredField = "set";
    parent.Child!.RequiredField = "set";
    await parent.WaitForTasks();
    Assert.IsTrue(parent.IsValid);

    // Break the child only
    parent.Child.RequiredField = "";
    await parent.WaitForTasks();

    Assert.IsTrue(parent.IsSelfValid, "The parent's own rules pass");
    Assert.IsFalse(parent.IsValid, "IsValid aggregates the child");
    Assert.IsFalse(parent.Child.IsValid);
    Assert.IsTrue(parent.PropertyMessages.Count > 0, "The child's message reaches the parent");
}
```
<sup><a href='/src/Design/Design.Tests/PropertyTests/ValidationStateTests.cs#L31-L50' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-is-valid-vs-self-valid' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Key validation meta-properties:
- `IsValid` - True if this object and all children are valid
- `IsSelfValid` - True if this object (excluding children) is valid
- `PropertyMessages` - Collection of all validation messages
- `[propertyName]` indexer - Access property-specific validation state and messages

An async rule leaves `IsBusy` true while it runs; `await WaitForTasks()` before reading `IsValid` when async rules may be in flight.

## Your First Entity

Entities are domain objects with identity and lifecycle management. `EntityBase<T>` extends `ValidateBase<T>` with persistence state, modification tracking, and `Save()`. A root entity's interface extends `IEntityRoot`, which is what exposes `IsSavable` and `Save()`; a child entity's interface extends `IEntityBase` and has neither, so a child can only be saved through its root:

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

The entity declares its properties and its factory operations. `[Create]` is `new`: it runs wherever it is called. `[Fetch]` takes a key and loads through a `[Service]` repository; `[Insert]`, `[Update]` and `[Delete]` persist. These four are `[Remote]`: they are the client's entry points, and `[Remote]` makes the client's call cross to the server, where the repository is registered. Inside a factory operation the object is paused, so plain assignment is a clean baseline load. `[Insert]` and `[Update]` re-run the rules on the server and throw `SaveOperationException` rather than write an invalid entity:

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

Entity state tracking properties:
- `IsNew` - True for a created entity that has not been persisted. Routing state only: it does not make the entity modified
- `IsModified` - True if any property has changed since load/save (cascades from children)
- `IsDeleted` - True if marked for deletion
- `IsSavable` - `(IsModified || IsNew) && IsValid && !IsBusy` — a created entity is savable without being modified. Only on `IEntityRoot` (aggregate roots), not on `IEntityBase` (child entities)

## Use the Generated Factory

RemoteFactory generates `IProductFactory` from the attributes: `[Create]` and `[Fetch]` become factory methods (`Create()`, `Fetch(Guid id)`); `[Insert]`, `[Update]` and `[Delete]` are reached through `Save(IProduct)`, which routes on the entity's state — and through the entity's own `Save()`, which the root interface exposes.

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

## Configure Dependency Injection

Register Neatoo services for the domain assembly on both tiers, register the domain's DI-provided rules, and register the repositories the `[Service]` parameters resolve — on the server only. The mode selects where factory operations run: `NeatooFactory.Server` for the server, `NeatooFactory.Remote` for the Blazor client, `NeatooFactory.Logical` when there is no client/server split. The design tests register everything in one container in server mode, with mock repositories:

<!-- snippet: skill-test-services -->
<a id='snippet-skill-test-services'></a>
```cs
public static IServiceScope GetScope()
{
    lock (_lock)
    {
        if (_serviceProvider == null)
        {
            var services = new ServiceCollection();

            // Real Neatoo services and the generated factories for the
            // domain assembly. Server mode: every operation runs in-process.
            services.AddNeatooServices(
                NeatooFactory.Server,
                typeof(Design.Domain.BaseClasses.IDemoInputModel).Assembly);

            // The domain's DI-provided rules, as both tiers would register them
            services.AddDesignDomainRules();

            // Mocks for the external dependencies only - never for Neatoo types
            RegisterMockRepositories(services);

            _serviceProvider = services.BuildServiceProvider();
        }
        return _serviceProvider.CreateScope();
    }
}
```
<sup><a href='/src/Design/Design.Tests/TestInfrastructure.cs#L35-L61' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-test-services' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Rule classes are `internal`, so the domain assembly exposes an extension method that registers them; call it on both tiers, because an entity that takes a rule in its constructor is constructed on the client too:

<!-- snippet: skill-rules-di-registration -->
<a id='snippet-skill-rules-di-registration'></a>
```cs
/// <summary>
/// Registration for the domain's DI-provided rules. Call on BOTH tiers, after
/// AddNeatooServices: an entity that takes a rule in its constructor is built
/// on the client too.
/// </summary>
public static class DomainRegistration
{
    public static IServiceCollection AddDesignDomainRules(this IServiceCollection services)
    {
        // Transient: each entity instance gets its own rule instance, because
        // a rule tracks execution state
        services.AddTransient<ICheckUsernameAvailabilityRule, CheckUsernameAvailabilityRule>();
        services.AddTransient<IUniqueCodeRule, UniqueCodeRule>();
        // A rule that takes an interface-factory domain service registers the
        // same way; the service itself is the proxy on the client (generated)
        // and the implementation on the server (registered there, not here).
        services.AddTransient<IShippingQuoteRule, ShippingQuoteRule>();
        return services;
    }
}
```
<sup><a href='/src/Design/Design.Domain/DI/DomainRegistration.cs#L14-L35' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-rules-di-registration' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

To use a generated factory, inject the factory interface (`IProductFactory`) into the Blazor component: `await ProductFactory.Fetch(id)` returns the entity, or `null` when there is no such product.

## Next Steps

You now have working validation objects (`ValidateBase<T>`) and entities (`EntityBase<T>`).

**Architectural Concepts:**
- **ValidateBase** provides property management, validation rules, and change notification. Use it for an object that needs rules but has no persistence lifecycle of its own — a value object.
- **EntityBase** extends ValidateBase with identity, persistence lifecycle, and modification tracking. Use it for domain entities that map to database rows.
- **Read models** need neither: a plain `[Factory]` class with a `[Fetch]` returns query results without a Neatoo base class.
- **Aggregate roots** are entities that define consistency boundaries. Child entities belong to the aggregate and are persisted with the root.

**Explore deeper:**

- **[Validation](guides/validation.md)** - Custom rules, async validation, rule execution control
- **[Entities](guides/entities.md)** - Entity lifecycle, state management, persistence operations
- **[Collections](guides/collections.md)** - EntityListBase and ValidateListBase for child collections
- **[Properties](guides/properties.md)** - Property system internals, meta-properties
- **[Business Rules](guides/business-rules.md)** - Cross-property validation, aggregate-level rules
- **[Parent-Child Graphs](guides/parent-child.md)** - Aggregate boundaries, cascade behavior
- **[Blazor Integration](guides/blazor.md)** - MudNeatoo components for Blazor forms
- **[RemoteFactory](guides/remote-factory.md)** - Deep dive into factory pattern and client-server transfer

---

**UPDATED:** 2026-10-06
