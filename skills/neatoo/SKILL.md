---
name: Neatoo
description: This skill should be used when working with Neatoo domain models, ValidateBase, EntityBase, ValidateListBase, EntityListBase, partial properties, property change tracking, validation rules, business rules, aggregate roots, entities, value objects, lazy loading, EntityLazyLoad, IEntityLazyLoadFactory, or any .NET DDD domain model framework work. Also triggers for IsValid, IsSelfValid, IsSavable, IsModified, IsNew, IsDeleted, RuleManager, AddActionAsync, AddValidationAsync, AddAction, AddValidation, IsBusy, WaitForTasks, IsLoaded, IsLoading, and base class behavior. This skill also provides guidance on where business logic belongs -- computed properties, conditional visibility, reactive behavior, and validation should live in the domain model (not the UI). Consult this skill when writing .razor files that bind to Neatoo entities to ensure logic stays in the domain layer. Neatoo is the domain model framework -- it does NOT include factory generation. For factory attributes ([Factory], [Create], [Fetch], [Remote], [Service], [AuthorizeFactory]) see the RemoteFactory skill, which is independent and works with any .NET class.
version: 1.0.0
---

# Neatoo Domain Models

Neatoo is a .NET framework for building domain models with automatic change tracking, validation, and rules through Roslyn source generators. It provides base classes that map to DDD concepts.

Neatoo focuses on the domain model: properties, change tracking, validation, rules, and collections. RemoteFactory is a separate, independent tool that generates client-server factories for **any .NET class** — it works with Neatoo entities, plain ViewModels, or POCOs. For factory attributes, authorization, and client-server patterns, see the RemoteFactory skill.

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
| Cross-property validation (single trigger) | `AddValidation` / `AddValidationAsync` | UI event handlers |
| Cross-property validation (multiple triggers) | `RuleBase<T>` / `AsyncRuleBase<T>` | UI event handlers |
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

## The Three-Phase Pattern

Every user interaction in a Neatoo app follows three sequential, non-overlapping phases:

**1. Set state.** Business methods — on the root, on children, called by any consumer — mutate properties. `IsModified` becomes true. `PropertyChanged` fires. Adding items to child collections (e.g., `order.Items.AddItem()`, `plan.PendingAuditRecordsEntity.Add(...)`) is also phase 1; these are state mutations, not persistence.

**2. Validate.** Rules re-run on the affected properties — sync (`AddAction`, `AddValidation`) and async (`AddActionAsync`, `AddValidationAsync`, class-based `AsyncRuleBase<T>`). `IsSelfValid` is the entity alone; `IsValid` aggregates the entity plus every descendant. Consumers call `await entity.WaitForTasks()` if async rules may be in flight before reading `IsValid` / calling `Save()`.

**3. Save.** The caller invokes `Save()` on the root. Factory methods (`[Insert]` / `[Update]` / `[Delete]`, routed by `IsNew` / `IsDeleted`) execute the persistence cascade: `MapTo` the EF entity, call repositories, commit transactions, raise factory events, persist audit records queued in phase 1, invoke `childFactory.Save` (save cascade).

**The phases don't cross.** Business methods never open transactions, call repositories, or raise factory events. Factory methods never call business methods or reach back into phase 1 logic. What factory methods need to read, they read from state that phase 1 set.

### Factory Method vs. Business Method Boundary

| Factory methods own | Business methods own |
|---|---|
| `MapTo` / `MapFrom` the EF entity | Property setters |
| Repository calls | Call other business methods on `this` |
| Transaction begin / commit | Call business methods on children (`this.Child.Method()`) |
| Raise factory events (via `[Service] IFactoryEvents`) | Add items to child collections |
| Call `childFactory.Save` (save cascade) | Queue records onto state-collection properties |
| `[Service]` parameter injection | Read `Parent` reference for ambient root state |
| DB-snapshot-vs-in-memory diffing | (No `[Service]` injection, no repositories, no transactions, no event raises) |

### What the Save Needs Must Be State

Factory methods can only read what's on the entity graph. They cannot read call-context, local variables from the business method that triggered the save, or "intentions" the caller held in their head.

If a save decision depends on something, put it on an entity:

- "Emit the deferred APPROVED audit at archive time" → `[Update]` reads `IsApproved && !PreHasApprovedAudit && ((IVisit)Parent!).Archived`
- "Force a side-effect on this save" → a flag the business method sets and the `[Update]` reads (e.g., `ForceEndReassess`)
- "This save is an extension, not a modification" → loaded state from Fetch (e.g., `PreApprovedTreatments`) compared against current in-memory value
- "Idempotency — don't emit this audit twice" → a `PreHasX` flag loaded during Fetch

Don't:

- Pass flags as parameters to `Save()` — Save's signature is fixed
- Stash post-business state in a service instance
- Reach from `[Update]` back into a business method to "ask" about something
- Infer intent from `IsModified` alone when the save decision depends on a combination of in-memory values and ambient state

## The Aggregate Is a Graph, Not a Façade

Strict DDD treats the aggregate root as the single entry point: a `Visit` class would expose `EndPlanEarly(reason)` that internally calls `Plan.EndEarly(reason)`; consumers only ever touch the root; children are hidden implementation details.

Neatoo rejects that encapsulation boundary. The aggregate is a graph whose nodes are all directly addressable by any consumer:

```csharp
// ViewModel calls a child business method directly
visit.Plan.EndEarly(reason);

// Razor component binds to a deep property
<MudNeatooTextField EntityProperty="@visit.Plan[nameof(IPlan.EndedEarlyReason)]" />

// Service reads any depth
var sku = order.LineItems[0].Product.Sku;
```

**The root is a coordinator, not a gate.** It owns `Save()`, raises factory events at save time, and its `IsValid` / `IsModified` / `IsSavable` aggregate every descendant. But it does not mediate access to children.

**Encapsulation lives at the property level.** Private setters, business methods, and validation rules guard state wherever it lives. You don't need the root to mediate; each entity's own surface does.

**Consequence:** design every entity as though any consumer can call its methods and bind to its properties. Don't try to rebuild strict DDD gatekeeping by routing every child mutation through root wrappers — you'll fight the framework and end up duplicating logic between the root and child.

## Designing Rules for Open Mutation

Because any consumer can call `visit.Plan.EndEarly(reason)` as a first-class operation, your rule graph must converge correctly after that call:

1. `Plan.EndEarly` sets `Plan.EndedEarly = true` and `Plan.EndedEarlyReason = reason`
2. `Plan`'s own rules run — child-level validation
3. `Plan.IsValid` / `Plan.IsSelfValid` update; `PropertyChanged` / `NeatooPropertyChanged` fires
4. `Visit`'s rules that trigger on `Plan` properties re-run (via `AddAction` with a child-property trigger, or `HandleNeatooPropertyChanged`)
5. `Visit.IsValid` aggregates — root valid only if self plus every descendant is valid
6. `PropertyChanged` fires on the root for `IsValid` / `IsSavable`; bindings re-render

**Design rule:** every mutation a consumer can make must leave the root in a correct state after all rules have run. If external mutation of a child can put the root into an invalid-but-unreported state, the rule graph is incomplete. This is the rule author's responsibility, not a framework guarantee.

### Rule placement by scope

| Invariant scope | Where the rule lives | Trigger |
|---|---|---|
| Child's own state | Child class | Own property |
| Root state that depends on child state | Root class | Child-property trigger on root rule |
| Summary of child state exposed on root | Root class (`AddAction`) | Child-property trigger |
| Sibling consistency in a list | Root class | Override `HandleNeatooPropertyChanged` |

**Convergence check.** After any business method that mutates a descendant, confirm `root.IsValid` reflects the full graph. If not, a rule is missing — typically on the root or an ancestor, triggered by the child property that was mutated. See `references/domain-logic-placement.md` → Pattern 6 (Child Property Triggers) and `references/rules-lifecycle.md` for trigger semantics.

## Parent — The Child's Window to Ambient State

Every child entity has a `Parent` reference populated automatically by the Neatoo source generator when the child is assigned to its parent (partial property set on the parent, or insertion into a child collection). `Parent` is a first-class API, not an implementation detail.

### Inside a child rule

```csharp
// In the child class constructor — read ambient root state
RuleManager.AddAction(
    t => t.IsAvailable = t.InStock && !((IOrder)t.Parent!).IsOnHold,
    t => t.InStock);
```

### Inside a child factory method

```csharp
[Update]
internal async Task Update(
    [Service] ITreatmentPlanChangeFactory auditFactory,
    [Service] ITreatmentPlanChangeListFactory auditListFactory)
{
    // Read parent (root) state to decide what this save should emit
    var visitArchived = ((IVisit)this.Parent!).Archived;
    if (visitArchived && IsApproved && !PreHasApprovedAudit)
    {
        EnsureAuditList(auditListFactory);
        PendingAuditRecordsEntity!.Add(auditFactory.CreateApproved(...));
    }
    // ... normal update path ...
}
```

### Inside a child business method

```csharp
public void RefreshPrice()
{
    var currency = ((IOrder)this.Parent!).Currency;
    this.Price = _priceService.GetPrice(this.Sku, currency);
}
```

### Cast pattern

`Parent` is typed as the framework base (effectively `object?` / `IBase?` at the use site). Cast to the root's interface when accessed: `((IRoot)this.Parent!).X`. If the same child type can be attached to different roots in different contexts, guard with `is IRoot root` pattern matching.

### Rules of use

- **Children read Parent; parents write children.** Don't mutate through `Parent` from child code — it inverts the graph.
- **Parent access is aggregate-scoped.** A child shouldn't use `Parent` to reach an entity belonging to a *different* aggregate. If you need that, the aggregate boundary is drawn wrong.
- **`Parent` is nullable at the type level, non-null at runtime once attached.** Before attachment (just-constructed, not yet assigned to a parent), `Parent` is null. In business methods and `[Update]` paths the entity is always attached.

### Why Parent is under-taught in DDD literature

DDD orthodoxy flags child→parent references as smelly: "children shouldn't know about parents; if they need parent state, the parent should call a child method passing the data." Neatoo's position: **domain aggregates are inherently coupled graphs.** Coupling inside the aggregate boundary is expected and wanted. `Parent` is the natural API for a child to read ambient aggregate context, and building workarounds to avoid it produces duplicate state flow and harder-to-follow code.

The coupling concerns DDD raises are real — they apply to coupling *across* aggregate boundaries, not within. Neatoo's `Parent` stays within the aggregate by design.

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
| `IsModified` | bool | Differs from the baseline the factory op left — would discarding it lose work? `PropertyManager.IsModified \|\| IsDeleted \|\| IsSelfModified`. **Does NOT include `IsNew`**: false after Create, false after Fetch. |
| `IsSelfModified` | bool | This object's own properties changed (excludes children) |
| `IsValid` | bool | This object and all children pass validation |
| `IsSelfValid` | bool | This object (only) passes validation |
| `IsSavable` | bool | `(IsModified \|\| IsNew) && IsValid && !IsBusy && !IsChild` |
| `IsNew` | bool | Not yet persisted. Set true by Create, false by Fetch/Insert. Routing state only — it does **not** imply `IsModified`; a created object is savable but not modified. |
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

RuleManager also provides `AddAction`, `AddActionAsync`, `AddValidationAsync`, and class-based rules. **`AddValidation`/`AddValidationAsync` accept exactly one trigger property** — for multiple triggers, use a class-based rule. See `references/validation.md` for details.

Check validation state with `IsValid`, `IsSelfValid`, and `PropertyMessages`.

### Rules Do NOT Fire During Factory Methods

**Rules (including AddAction computed properties) do NOT fire during `[Create]`, `[Fetch]`, `[Insert]`, `[Update]`, `[Delete]`, or `LoadValue`.** Factory operations are wrapped in `PauseAllActions()`. `ResumeAllActions()` does NOT run rules — it only recalculates cached validity. `PropertyChanged` does NOT fire for changes made while paused.

**`RunRules` works while paused** — it has no `IsPaused` guard. Call `await RunRules(RunRulesFlag.All)` at the end of any factory method that sets properties with dependent AddAction rules:

```csharp
[Create]
public async Task Create()
{
    Quantity = 10;
    UnitPrice = 5.00m;
    await RunRules(RunRulesFlag.All);  // Forces computed properties to populate
    // Total is now 50.00
}
```

Without this call, computed properties remain at their default values when the entity reaches the client. See `references/rules-lifecycle.md` for the complete execution lifecycle, `RunRulesFlag` enum reference, and the factory method timeline.

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
- **`references/rules-lifecycle.md`** - When rules fire and when they don't, RunRulesFlag enum, factory method gap, RunRules works while paused
- **`references/shared-rules.md`** - Shared rules across entities via interface-typed AsyncRuleBase and DI injection
- **`references/entities.md`** - EntityBase lifecycle, persistence, Save routing
- **`references/collections.md`** - EntityListBase, parent-child relationships, deletion tracking
- **`references/lazy-loading.md`** - EntityLazyLoad&lt;T&gt;, IEntityLazyLoadFactory, explicit LoadAsync(), passive Value read, WaitForTasks integration
- **`references/source-generation.md`** - What gets generated, Generated/ folder, [SuppressFactory]
- **`references/trimming.md`** - IL trimming annotations, suppression strategy, consumer project setup
- **`references/blazor.md`** - Blazor-specific binding and component patterns (see also the **MudNeatoo skill** for component binding and anti-patterns)
- **`references/testing.md`** - No mocking Neatoo, integration test patterns
- **`references/pitfalls.md`** - Common mistakes and gotchas

**RemoteFactory topics** (see `/RemoteFactory` skill):
- Factory attributes, service injection, remote execution, authorization

## Troubleshooting

See `references/pitfalls.md` for common issues. Key quick checks: class and properties must be `partial`, class needs `[Factory]` attribute, and `IsSavable` requires `IsValid` plus a reason to persist (`IsModified` **or** `IsNew`).

## IsNew vs IsModified

They answer different questions, and Neatoo keeps them separate:

- **`IsModified`** — "would discarding this lose work?" Drives unsaved-changes guards.
- **`IsNew`** — "does persistence not know this yet?" Drives Insert-vs-Update routing.

A created entity is `IsNew=true, IsModified=false, IsSavable=true`: it needs inserting, but holds no user work, so guards bound to `IsModified` stay quiet on it — including on a freshly re-derived object after a save.

```csharp
// Guards read naturally
if (order.IsModified) { /* warn before navigating away */ }

// A [Create] that IS the user's work says so
[Create]
public void Create() { MarkModified(); }
```

**COMMON MISTAKE:** calling `MarkModified()` in a `[Create]` so the entity can be saved. New entities are already savable — `IsSavable` admits `IsNew`. Using it that way re-welds the two meanings and makes guards cry wolf on every new object.

**`IsNew` never aggregates.** It is per-object routing state; lists report `IsNew => false` and a parent's `IsNew` ignores its children. What flows up a graph is modification state — which is why attaching a child to a live parent (list add, or assigning a new child to a property) marks the child modified, so the parent becomes modified and savable.
