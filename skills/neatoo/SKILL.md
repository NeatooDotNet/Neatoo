---
name: neatoo
description: Neatoo DDD framework for .NET with Blazor WebAssembly. Use when building domain entities with EntityBase, implementing business rules and validation, creating factories with [Factory] attribute, setting up client-server communication with RemoteFactory, working with aggregates and parent-child relationships, or troubleshooting source generator issues.
allowed-tools: Read, Write, Edit, Glob, Grep, Bash(dotnet:*), WebFetch, WebSearch
---

# Neatoo Framework Skill

Neatoo is a Domain-Driven Design framework for .NET that provides:
- **Entities with automatic state tracking** (IsModified, IsValid, IsBusy)
- **Validation rules** via attributes and custom RuleBase classes
- **Source-generated factories** for Create/Fetch/Save operations
- **Client-server communication** via RemoteFactory for Blazor WebAssembly

## Quick Decision Tree

| User Intent | Go To |
|-------------|-------|
| Create an entity or aggregate | [creating-entities.md](creating-entities.md) |
| Add validation or business rules | [adding-validation.md](adding-validation.md) |
| Save data (Insert/Update/Delete) | [saving-data.md](saving-data.md) |
| Work with child collections | [parent-child.md](parent-child.md) |
| Set up Blazor client-server | [client-server.md](client-server.md) |
| Something isn't working | [troubleshooting.md](troubleshooting.md) |
| Quick syntax lookup | [quick-reference.md](quick-reference.md) |

## Critical Patterns - Always Follow

### 1. Interface-First Design

Every entity needs a public interface. The concrete class is `internal`.

```csharp
// Public interface - this is your API
public partial interface IPerson : IEntityBase
{
    string? Name { get; set; }
}

// Internal implementation
[Factory]
internal partial class Person : EntityBase<Person>, IPerson
{
    public Person(IEntityBaseServices<Person> services) : base(services) { }
    public partial string? Name { get; set; }

    [Create]
    public void Create() { }
}
```

### 2. Always Reassign After Save

`Save()` returns a NEW instance. Always capture the return value.

```csharp
// WRONG - person is now stale
await personFactory.Save(person);

// CORRECT - person has updated state
person = await personFactory.Save(person);
```

### 3. Partial Keyword Required

Both class AND properties must be `partial` for source generation.

```csharp
[Factory]
internal partial class Person : EntityBase<Person>, IPerson  // partial class
{
    public partial string? Name { get; set; }  // partial property
}
```

### 4. Check IsSavable, Not Just IsValid

`IsSavable` is the complete save-readiness check.

```csharp
// IsSavable = IsModified && IsValid && !IsBusy && !IsChild
if (person.IsSavable)
{
    person = await personFactory.Save(person);
}
```

### 5. Use [Remote] for Server Operations

Methods that access the database need `[Remote]` to execute on the server.

```csharp
[Remote]  // Executes on server
[Fetch]
public async Task Fetch(Guid id, [Service] IDbContext db) { }

[Create]  // No [Remote] - runs on client
public void Create() { }
```

## Anti-Patterns - Never Do

| Anti-Pattern | Why It's Wrong | Correct Approach |
|--------------|----------------|------------------|
| Casting to concrete type | Breaks serialization, bypasses interface contract | Add needed method to interface |
| Injecting child factories in Blazor components | Bypasses aggregate consistency | Use parent's domain methods (e.g., `order.Lines.AddLine()`) |
| Validation in `[Insert]`/`[Update]` methods | Users only see errors after clicking Save | Use rules that run immediately |
| Non-partial class or properties | Source generator can't add implementations | Add `partial` keyword |
| Ignoring Save() return value | Original object is stale after save | Always reassign: `x = await Save(x)` |

## Minimum Viable Entity

The smallest working Neatoo entity:

```csharp
public partial interface IProduct : IEntityBase { }

[Factory]
internal partial class Product : EntityBase<Product>, IProduct
{
    public Product(IEntityBaseServices<Product> services) : base(services) { }

    public partial string? Name { get; set; }

    [Create]
    public void Create() { }
}
```

This generates:
- `IProductFactory` interface
- `ProductFactory` implementation
- Property backing fields with change tracking

## File Index

| File | Purpose |
|------|---------|
| [creating-entities.md](creating-entities.md) | Creating aggregates, entities, value objects |
| [adding-validation.md](adding-validation.md) | Data annotations, custom rules, async validation |
| [saving-data.md](saving-data.md) | Factory operations, MapFrom/MapTo patterns |
| [parent-child.md](parent-child.md) | Collections, DeletedList, parent access |
| [client-server.md](client-server.md) | RemoteFactory setup for Blazor WebAssembly |
| [troubleshooting.md](troubleshooting.md) | Common errors and diagnostic steps |
| [quick-reference.md](quick-reference.md) | One-page cheatsheet |
