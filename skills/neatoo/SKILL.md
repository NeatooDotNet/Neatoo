---
name: Neatoo
description: This skill should be used when working with Neatoo domain models, ValidateBase, EntityBase, ValidateListBase, EntityListBase, partial properties, property change tracking, validation rules, business rules, aggregate roots, entities, value objects, or any .NET DDD domain model framework work. Also triggers for IsValid, IsSelfValid, IsSavable, IsModified, IsNew, IsDeleted, RuleManager, and base class behavior. For factory attributes ([Factory], [Create], [Fetch], [Remote], [Service], [AuthorizeFactory]) see the RemoteFactory skill.
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
<sup><a href='/src/samples/QuickStartSamples.cs#L11-L23' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-quickstart' title='Start of snippet'>anchor</a></sup>
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

## Key Properties

**There is no `IsDirty` in Neatoo.** Use `IsModified` / `IsSelfModified`.

| Property | Type | Meaning |
|----------|------|---------|
| `IsModified` | bool | Has unsaved changes (this or children) |
| `IsSelfModified` | bool | This object (only) has changes |
| `IsValid` | bool | This object and all children pass validation |
| `IsSelfValid` | bool | This object (only) passes validation |
| `IsSavable` | bool | `IsValid && IsModified && !IsBusy && !IsChild` |
| `IsNew` | bool | Not yet persisted |
| `IsDeleted` | bool | Marked for deletion |
| `RuleManager` | IRuleManager | Access to validation rules |

## Core Patterns

### Properties with Change Tracking

All Neatoo properties use `partial` properties. The source generator implements backing fields with automatic change tracking and validation triggering:

<!-- snippet: skill-properties-basic -->
<a id='snippet-skill-properties-basic'></a>
```cs
public partial string Name { get; set; }
public partial decimal Price { get; set; }
```
<sup><a href='/src/samples/QuickStartSamples.cs#L35-L38' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-properties-basic' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The generator creates property implementations that call `Getter<T>()` and `Setter()` internally.

### Factory Methods

Neatoo entities use RemoteFactory for factory generation. See the `/RemoteFactory` skill for factory attributes (`[Factory]`, `[Create]`, `[Fetch]`, `[Insert]`, `[Update]`, `[Delete]`), service injection (`[Service]`), remote execution (`[Remote]`), and authorization (`[AuthorizeFactory]`).

### Save Routing (Neatoo State-Based)

When `Save()` is called, the factory routes based on Neatoo entity state:
- `IsNew == true` → `[Insert]` method
- `IsNew == false && IsDeleted == false` → `[Update]` method
- `IsDeleted == true` → `[Delete]` method

This routing is automatic based on entity state properties.

### Aggregate Save Cascading

State cascades UP automatically; saves cascade DOWN manually — each parent's `[Insert]`/`[Update]` must call `childFactory.SaveAsync()` on its children. See `references/entities.md` → "Aggregate Save Cascading" for the full pattern, rules, and anti-patterns.

### Validation

Add validation rules in the constructor using RuleManager or validation attributes:

<!-- snippet: skill-validation -->
<a id='snippet-skill-validation'></a>
```cs
public SkillValidationExample(IEntityBaseServices<SkillValidationExample> services) : base(services)
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
<sup><a href='/src/samples/SkillValidationSamples.cs#L52-L64' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-validation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Check validation state with `IsValid`, `IsSelfValid`, and `PropertyMessages`.

## Testing

**Critical:** Never mock Neatoo interfaces or classes. Use real factories and mock only external dependencies. Use `[SuppressFactory]` on test-only classes that inherit from Neatoo base classes. See `references/testing.md` for patterns and `references/pitfalls.md` for common mistakes.

## Reference Documentation

Detailed documentation for each topic area:

- **`references/base-classes.md`** - Neatoo-to-DDD mapping, when to use each base
- **`references/properties.md`** - Partial properties, change tracking, calculated properties
- **`references/validation.md`** - RuleManager, attributes, async validation
- **`references/entities.md`** - EntityBase lifecycle, persistence, Save routing
- **`references/collections.md`** - EntityListBase, parent-child relationships, deletion tracking
- **`references/source-generation.md`** - What gets generated, Generated/ folder, [SuppressFactory]
- **`references/blazor.md`** - Blazor-specific binding and component patterns
- **`references/testing.md`** - No mocking Neatoo, integration test patterns
- **`references/pitfalls.md`** - Common mistakes and gotchas

**RemoteFactory topics** (see `/RemoteFactory` skill):
- Factory attributes, service injection, remote execution, authorization

## Troubleshooting

See `references/pitfalls.md` for common issues. Key quick checks: class and properties must be `partial`, class needs `[Factory]` attribute, and `IsSavable` requires both `IsValid` and `IsModified`.
