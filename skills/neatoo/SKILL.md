---
name: Neatoo
description: This skill should be used when working with Neatoo domain models, ValidateBase, EntityBase, ValidateListBase, EntityListBase, partial properties, property change tracking, validation rules, business rules, aggregate roots, entities, value objects, lazy loading, LazyLoad, ILazyLoadFactory, or any .NET DDD domain model framework work. Also triggers for IsValid, IsSelfValid, IsSavable, IsModified, IsNew, IsDeleted, RuleManager, AddActionAsync, AddValidationAsync, AddAction, AddValidation, IsBusy, WaitForTasks, IsLoaded, IsLoading, and base class behavior. This skill also provides guidance on where business logic belongs -- computed properties, conditional visibility, reactive behavior, and validation should live in the domain model (not the UI). Consult this skill when writing .razor files that bind to Neatoo entities to ensure logic stays in the domain layer. For factory attributes ([Factory], [Create], [Fetch], [Remote], [Service], [AuthorizeFactory]) see the RemoteFactory skill.
version: 1.0.0
---

# Neatoo Domain Models

Neatoo is a .NET framework for building domain models with automatic change tracking, validation, and persistence through Roslyn source generators. It provides base classes that map to DDD concepts with built-in support for client-server architectures.

Neatoo + RemoteFactory together provide the complete DDD framework. Both skills are assumed loaded together. For factory attributes, authorization, and client-server patterns, see the RemoteFactory skill. This skill covers domain model, validation, properties, and collections.

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

## Domain Logic First -- The Core Principle

**Business logic belongs in the domain model, not the UI.** Neatoo domain models are not DTOs that shuttle data to a smart UI. They are rich domain objects that encapsulate business rules, computed state, validation, and reactive behavior. The UI is a thin binding layer.

**When implementing a feature: design domain properties and rules first. Write the UI as a binding layer over those properties. If you find yourself writing business logic in a `.razor` file, stop and move it to the domain model.**

These patterns use `RuleManager.AddAction` and `AddActionAsync`, covered in Core Patterns below and in `references/validation.md`.

### Where Logic Goes

| Logic Type | Neatoo Mechanism | NOT in |
|-----------|-----------------|--------|
| Computed/derived values | `AddAction` with trigger properties | `.razor` arithmetic/ternary |
| Conditional visibility | Domain `bool` property via `AddAction` | `.razor` `@if` chains |
| Parent reacts to child changes | `AddAction` with child trigger `t => t.Items![0].Prop` | UI event handlers |
| Cross-property validation | `AddValidation` / `AddValidationAsync` | UI event handlers |
| Reactive data fetch | `AddActionAsync` | UI `OnChanged` handlers |
| Cascading state changes | Chained rules (rule sets property -> triggers next rule) | UI code-behind |
| Workflow transitions | Domain methods + `AddAction` for `CanX` properties | UI button click handlers |
| LINQ over children | `AddAction` with child trigger, computed property | `.razor` inline LINQ |
| Parent orchestrates between children | `AddAction` with child trigger, action updates other child | UI bridging code |
| Cross-sibling rules in a list | Override `HandleNeatooPropertyChanged` | UI bridging code |

### The Smell Test

When writing or reviewing `.razor` files: if there are more than 3 conditional/computed expressions, business logic is leaking into the UI. Move it to the domain model as rules or computed properties.

```csharp
// WRONG: UI computes
<MudText>@(order.Quantity * order.UnitPrice)</MudText>
@if (order.Quantity > 0 && order.UnitPrice > 0 && order.Total > 500)
{ <MudAlert>Discount!</MudAlert> }

// RIGHT: Domain computes, UI binds
// In constructor: RuleManager.AddAction(t => t.Total = t.Quantity * t.UnitPrice, t => t.Quantity, t => t.UnitPrice);
// In constructor: RuleManager.AddAction(t => t.QualifiesForDiscount = t.Total > 500, t => t.Total);
<MudText>@order.Total</MudText>
@if (order.QualifiesForDiscount) { <MudAlert>Discount!</MudAlert> }
```

See `references/domain-logic-placement.md` for detailed patterns: computed properties, conditional visibility, cascading state, async side-effects, workflow state machines, child property triggers for parent-child reactivity, class-based rules with DI, and the refactoring smell test table.

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

RuleManager also provides `AddAction`, `AddActionAsync`, `AddValidationAsync`, and class-based rules. Rules do **not** fire during `[Create]`/`[Fetch]` or `LoadValue` — the framework wraps factory operations in `PauseAllActions()`. See `references/validation.md` for details.

Check validation state with `IsValid`, `IsSelfValid`, and `PropertyMessages`.

### Child Property Triggers — Parent Reacts to Child Changes

To react to child property changes in an aggregate, use a child property trigger expression with `AddAction`. The `[0]` indexer is a syntactic placeholder — any child whose named property changes triggers the rule:

```csharp
// Parent recalculates when any child's LineTotal changes
RuleManager.AddAction(
    t => t.OrderTotal = t.Items?.Sum(i => i.LineTotal) ?? 0,
    t => t.Items![0].LineTotal);

// Multiple child property triggers
RuleManager.AddAction(
    t => t.HasInvalidQuantities = t.Items?.Any(i => i.Quantity <= 0) ?? false,
    t => t.Items![0].Quantity);
```

The action body can also push changes to other children — the parent acts as orchestrator:

```csharp
// When ShippingAddress.State changes, update tax on all items
RuleManager.AddAction(
    t => { foreach (var item in t.Items!) item.TaxRate = TaxRates.Get(t.ShippingAddress!.State); },
    t => t.ShippingAddress!.State);
```

**Do NOT use `t => t.Items` as the trigger** — that only fires when the `Items` property reference itself is reassigned, not when child items change. `TriggerProperty.IsMatch` uses exact string equality: `"Items" != "Items.LineTotal"`.

See `references/domain-logic-placement.md` → "Pattern 6: Child Property Triggers" for child triggers, orchestrator patterns, `NeatooPropertyChanged`, and `HandleNeatooPropertyChanged` overrides.

## Testing

**Critical:** Never mock Neatoo interfaces or classes. Use real factories and mock only external dependencies. Use `[SuppressFactory]` on test-only classes that inherit from Neatoo base classes. See `references/testing.md` for patterns and `references/pitfalls.md` for common mistakes.

## Reference Documentation

Detailed documentation for each topic area:

- **`references/domain-logic-placement.md`** - Where business logic belongs: computed properties, conditional visibility, cascading state, async side-effects, child property triggers, workflow state machines, refactoring smell test
- **`references/base-classes.md`** - Neatoo-to-DDD mapping, when to use each base
- **`references/properties.md`** - Partial properties, change tracking, calculated properties
- **`references/validation.md`** - RuleManager, attributes, async validation
- **`references/entities.md`** - EntityBase lifecycle, persistence, Save routing
- **`references/collections.md`** - EntityListBase, parent-child relationships, deletion tracking
- **`references/lazy-loading.md`** - LazyLoad&lt;T&gt;, ILazyLoadFactory, explicit async loading
- **`references/source-generation.md`** - What gets generated, Generated/ folder, [SuppressFactory]
- **`references/trimming.md`** - IL trimming annotations, suppression strategy, consumer project setup
- **`references/blazor.md`** - Blazor-specific binding and component patterns (see also the **MudNeatoo skill** for component binding and anti-patterns)
- **`references/testing.md`** - No mocking Neatoo, integration test patterns
- **`references/pitfalls.md`** - Common mistakes and gotchas

**RemoteFactory topics** (see `/RemoteFactory` skill):
- Factory attributes, service injection, remote execution, authorization

## Troubleshooting

See `references/pitfalls.md` for common issues. Key quick checks: class and properties must be `partial`, class needs `[Factory]` attribute, and `IsSavable` requires both `IsValid` and `IsModified`.
