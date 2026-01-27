---
name: Neatoo
description: This skill should be used when working with Neatoo domain models, ValidateBase, EntityBase, ValidateListBase, EntityListBase, [Factory], [Create], [Fetch], [Insert], [Update], [Delete], [Execute], [Remote], [Service], [AuthorizeFactory], partial properties, property change tracking, validation rules, business rules, domain model persistence, aggregate roots, entities, value objects, or any .NET DDD domain model framework work. Also triggers for IsValid, IsSelfValid, IsSavable, IsModified, IsNew, IsDeleted, RuleManager, and source-generated factory methods.
version: 1.0.0
---

# Neatoo Domain Models

Neatoo is a .NET framework for building domain models with automatic change tracking, validation, and persistence through Roslyn source generators. It provides base classes that map to DDD concepts with built-in support for client-server architectures.

## Quick Start

<!-- snippet: skill-quickstart -->
<a id='snippet-skill-quickstart'></a>
```cs
[Factory]
public partial class Product : EntityBase<Product>
{
    public Product(IEntityBaseServices<Product> services) : base(services) { }

    [Required]
    public partial string Name { get; set; }
    public partial decimal Price { get; set; }

    [Create] public void Create() { }
}
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/QuickStartSamples.cs#L11-L23' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-quickstart' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

This generates a factory (`IProductFactory`) with a `Create()` method. Properties auto-track changes, trigger validation, and fire `PropertyChanged`.

## Base Class Quick Reference

| DDD Concept | Neatoo Base Class | Use When |
|-------------|-------------------|----------|
| Aggregate Root | `EntityBase<T>` | Root entity with full CRUD lifecycle |
| Entity | `EntityBase<T>` | Child entity within an aggregate |
| Value Object | `ValidateBase<T>` | Data with validation, no persistence lifecycle |
| Entity Collection | `EntityListBase<I>` | List of child entities (tracks deletions) |
| Validate Collection | `ValidateListBase<I>` | List of value objects (no deletion tracking) |
| Command | Static class with `[Execute]` | Server-side operation returning result |
| Read Model | `ValidateBase<T>` with `[Fetch]` only | Query result (no Insert/Update/Delete) |

## Core Patterns

### Properties with Change Tracking

All Neatoo properties use `partial` properties. The source generator implements backing fields with automatic change tracking and validation triggering:

```csharp
public partial string Name { get; set; }
public partial decimal Price { get; set; }
```

The generator creates property implementations that call `Getter<T>()` and `Setter()` internally.

### Factory Methods

Mark methods with factory attributes to generate client-callable factory methods:

```csharp
[Factory]
public partial class Employee : EntityBase<Employee>
{
    public Employee(IEntityBaseServices<Employee> services) : base(services) { }

    public partial int Id { get; set; }
    public partial string Name { get; set; }

    [Create]
    public void Create() { /* Initialize new */ }

    [Fetch]
    public void Fetch(int id, string name) { Id = id; Name = name; }

    [Insert]
    public async Task InsertAsync([Service] IRepository repo) { /* Save new */ }

    [Update]
    public async Task UpdateAsync([Service] IRepository repo) { /* Save changes */ }

    [Delete]
    public async Task DeleteAsync([Service] IRepository repo) { /* Remove */ }
}
```

### Save Routing

When `Save()` is called, Neatoo automatically routes to the appropriate operation:
- `IsNew == true` → `[Insert]` method
- `IsNew == false && IsDeleted == false` → `[Update]` method
- `IsDeleted == true` → `[Delete]` method

### Validation

Add validation rules in the constructor using RuleManager or validation attributes:

```csharp
public Employee(IEntityBaseServices<Employee> services) : base(services)
{
    // Inline validation with lambda
    RuleManager.AddValidation(
        emp => string.IsNullOrEmpty(emp.Name) ? "Name is required" : "",
        e => e.Name);

    // Or use validation attributes on properties
    // [Required(ErrorMessage = "Name is required")]
    // public partial string Name { get; set; }
}
```

Check validation state with `IsValid`, `IsSelfValid`, and `PropertyMessages`.

### Authorization

Control who can perform factory operations via an authorization interface:

```csharp
public interface IEmployeeAuthorization
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    bool CanCreate();

    [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
    bool CanFetch();

    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    bool CanSave();
}

public class EmployeeAuthorization : IEmployeeAuthorization
{
    private readonly IPrincipal _principal;
    public EmployeeAuthorization(IPrincipal principal) => _principal = principal;

    public bool CanCreate() => _principal.IsInRole("HR");
    public bool CanFetch() => _principal.Identity?.IsAuthenticated ?? false;
    public bool CanSave() => _principal.IsInRole("HR");
}

[Factory]
[AuthorizeFactory<IEmployeeAuthorization>]
public partial class Employee : EntityBase<Employee> { /* ... */ }
```

## Key Properties

| Property | Type | Meaning |
|----------|------|---------|
| `IsValid` | bool | This object and all children pass validation |
| `IsSelfValid` | bool | This object (only) passes validation |
| `IsSavable` | bool | `IsValid && IsModified && !IsBusy && !IsChild` |
| `IsModified` | bool | Has unsaved changes (this or children) |
| `IsSelfModified` | bool | This object (only) has changes |
| `IsNew` | bool | Not yet persisted |
| `IsDeleted` | bool | Marked for deletion |
| `RuleManager` | IRuleManager | Access to validation rules |

## Dependency Injection

Inject dependencies using `[Service]` attribute on factory method parameters:

```csharp
[Fetch]
public async Task Fetch(Guid id, [Service] IEmployeeRepository repo)
{
    var data = await repo.GetByIdAsync(id);
    // Map data to properties
}
```

## Remote Execution

`[Remote]` marks **entry points from the client to the server**. Once execution crosses to the server, it stays there—subsequent method calls don't need `[Remote]`.

| Scenario | Use `[Remote]`? | Reason |
|----------|-----------------|--------|
| Aggregate root Create/Fetch/Save | **Yes** | Entry point from client |
| Top-level UI-initiated operations | **Yes** | Entry point from client |
| Child entity loading within aggregate | No | Called from server after crossing boundary |
| Methods with `[Service]` method injection | No (usually) | Already on server when called |
| Any method called from server-side code | No | Execution already on server |

```csharp
// Aggregate root - needs [Remote] because it's called from client
[Remote]
[Fetch]
public async Task Fetch(Guid id, [Service] IEmployeeRepository repo) { }

// Child entity - no [Remote] needed, called from server-side parent
[Fetch]
public void Fetch(int id, string name, [Service] IChildRepository repo) { }
```

**Constructor vs Method Injection:**
- Constructor injection (`[Service]` on constructor): Services available on both client and server
- Method injection (`[Service]` on method parameters): Server-only services (the common case)

## Testing

**Critical:** Never mock Neatoo interfaces or classes. Use real factories and mock only external dependencies:

```csharp
// Setup DI with Neatoo services and mock external dependencies
services.AddNeatooServices(NeatooFactory.Logical, typeof(Employee).Assembly);
services.AddScoped<IEmployeeRepository, MockEmployeeRepository>();

// DO: Use real Neatoo factories
var factory = serviceProvider.GetRequiredService<IEmployeeFactory>();
var employee = factory.Create();
employee.Name = "Alice";
Assert.IsTrue(employee.IsModified);

// DON'T: Mock Neatoo interfaces
var mock = new Mock<IEntityBase>(); // Never do this
```

**For unit tests without factory generation:** Use `[SuppressFactory]` on test classes that inherit from Neatoo base classes. This prevents the source generator from creating factory methods for test-only classes.

```csharp
[SuppressFactory]
public class TestEmployee : EntityBase<TestEmployee>
{
    public TestEmployee(IEntityBaseServices<TestEmployee> services) : base(services) { }
    public partial string Name { get; set; }
}
```

See `references/testing.md` for integration test patterns and `references/pitfalls.md` for common mistakes.

## Reference Documentation

Detailed documentation for each topic area:

- **`references/base-classes.md`** - Neatoo-to-DDD mapping, when to use each base
- **`references/properties.md`** - Partial properties, change tracking, calculated properties
- **`references/validation.md`** - RuleManager, attributes, async validation
- **`references/entities.md`** - EntityBase lifecycle, persistence, Save routing
- **`references/collections.md`** - EntityListBase, parent-child relationships, deletion tracking
- **`references/factory.md`** - [Factory], [Create], [Fetch], [Insert], [Update], [Delete], [Execute], [Remote], [Service]
- **`references/authorization.md`** - [AuthorizeFactory<T>], authorization interfaces
- **`references/source-generation.md`** - What gets generated, Generated/ folder, [SuppressFactory]
- **`references/blazor.md`** - Blazor-specific binding and component patterns
- **`references/testing.md`** - No mocking Neatoo, integration test patterns
- **`references/pitfalls.md`** - Common mistakes and gotchas

## Troubleshooting

**Factory method not generated:**
- Ensure class has `[Factory]` attribute
- Ensure class is `partial`
- Ensure project references include source generators with `OutputItemType="Analyzer"`
- Rebuild the project
- Check Generated/ folder for output

**Changes not tracked:**
- Use `partial` properties - the generator implements change tracking
- Direct backing field assignment bypasses tracking

**Validation not running:**
- Rules are async - use `await RunRules()` before checking `IsValid`
- Rules must be added in constructor via `RuleManager.AddValidation()`

**Save not working:**
- Check `IsSavable` - must be valid and modified
- Ensure appropriate `[Insert]`/`[Update]`/`[Delete]` methods exist
- For server-side persistence, add `[Remote]` attribute

See `references/pitfalls.md` for more common issues.
