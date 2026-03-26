# Shared Rules via Interface-Typed AsyncRuleBase

When the same validation rule applies to multiple entity types, make the rule operate on a shared interface instead of being generic per entity. This eliminates generics, enables direct DI injection, and keeps the rule in one place.

## The Pattern

### 1. Define a shared interface extending IValidateBase

The interface must extend `IValidateBase` to satisfy `AsyncRuleBase<T>`'s constraint (`where T : class, IValidateBase`). Include only the properties the rule needs:

<!-- snippet: shared-rule-interface -->
<a id='snippet-shared-rule-interface'></a>
```cs
public interface IHasUniqueId : IValidateBase
{
    int? ID { get; }
    Guid ExcludeId { get; }
}
```
<sup><a href='/src/samples/SharedRulesSamples.cs#L14-L20' title='Snippet source file'>snippet source</a> | <a href='#snippet-shared-rule-interface' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`IValidateBase` is already implemented by every Neatoo base class (`ValidateBase<T>`, `EntityBase<T>`, etc.), so adding it to the interface introduces no new obligations on implementing entities.

### 2. Write a non-generic rule on the interface

The rule's type parameter is the shared interface, not a specific entity:

<!-- snippet: shared-rule-class -->
<a id='snippet-shared-rule-class'></a>
```cs
public class IdUniquenessRule : AsyncRuleBase<IHasUniqueId>, IIdUniquenessRule
{
    private readonly IIdUniquenessService _service;

    public IdUniquenessRule(IIdUniquenessService service)
        : base(e => e.ID)
    {
        _service = service;
    }

    protected override async Task<IRuleMessages> Execute(
        IHasUniqueId target, CancellationToken? token = null)
    {
        if (target.ID == null) return None;

        var isUnique = await _service.IsUniqueAsync(target.ID.Value, target.ExcludeId);
        return isUnique
            ? None
            : (nameof(target.ID), $"ID {target.ID} is already assigned.").AsRuleMessages();
    }
}
```
<sup><a href='/src/samples/SharedRulesSamples.cs#L23-L45' title='Snippet source file'>snippet source</a> | <a href='#snippet-shared-rule-class' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### 3. Create a DI interface for the rule

The interface extends `IRule<IHasUniqueId>` so it can be passed directly to `RuleManager.AddRule`:

<!-- snippet: shared-rule-di-interface -->
<a id='snippet-shared-rule-di-interface'></a>
```cs
public interface IIdUniquenessRule : IRule<IHasUniqueId> { }
```
<sup><a href='/src/samples/SharedRulesSamples.cs#L48-L50' title='Snippet source file'>snippet source</a> | <a href='#snippet-shared-rule-di-interface' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### 4. Entities implement the shared interface and inject the rule

Each entity maps its own primary key to the interface's properties:

<!-- snippet: shared-rule-entity-usage -->
<a id='snippet-shared-rule-entity-usage'></a>
```cs
[Factory]
internal partial class SharedRuleEmployee : EntityBase<SharedRuleEmployee>, ISharedRuleEmployee
{
    public SharedRuleEmployee(
        IEntityBaseServices<SharedRuleEmployee> services,
        IIdUniquenessRule idUniquenessRule) : base(services)
    {
        RuleManager.AddRule(idUniquenessRule);
    }

    [Create]
    public void Create() { }

    public Guid ExcludeId => EmployeeID;
    public partial Guid EmployeeID { get; set; }
    public partial int? ID { get; set; }
    public partial string Name { get; set; }
}
```
<sup><a href='/src/samples/SharedRulesSamples.cs#L53-L72' title='Snippet source file'>snippet source</a> | <a href='#snippet-shared-rule-entity-usage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`RuleManager.AddRule` is generic at the method level (`AddRule<T>`), not at the class level. When called with an `IRule<IHasUniqueId>`, T is inferred as `IHasUniqueId`. Since `SharedRuleEmployee` implements `IHasUniqueId`, the rule executes against the entity at runtime.

### 5. Register in DI

<!-- snippet: shared-rule-di-registration -->
<a id='snippet-shared-rule-di-registration'></a>
```cs
// Use transient lifetime -- each entity instance gets its own rule instance
// because rules track execution state.
// services.AddTransient<IIdUniquenessRule, IdUniquenessRule>();
```
<sup><a href='/src/samples/SharedRulesSamples.cs#L107-L111' title='Snippet source file'>snippet source</a> | <a href='#snippet-shared-rule-di-registration' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Why This Works

- `AddRule<T>` infers T from the rule argument, not from the entity class. T = `IHasUniqueId`.
- The entity implements `IHasUniqueId`, so the runtime cast succeeds.
- The rule's constructor dependencies (`IIdUniquenessService`) are resolved by DI automatically.
- Entities no longer need to know about the rule's internal dependencies.

## When to Use

- A rule applies to 2+ entity types that share common properties
- The rule needs injected services (repository, external service, etc.)
- Entity constructors are accumulating service parameters only to forward them to rules

## Contrast with Entity-Specific Rules

For rules specific to one entity type, the simpler pattern still applies:

<!-- snippet: shared-rule-entity-specific -->
<a id='snippet-shared-rule-entity-specific'></a>
```cs
// For rules specific to one entity type, inject the service and new the rule:
// RuleManager.AddRule(new HireDateRule()); // no DI needed, entity-specific
```
<sup><a href='/src/samples/SharedRulesSamples.cs#L114-L117' title='Snippet source file'>snippet source</a> | <a href='#snippet-shared-rule-entity-specific' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The shared-rule pattern is specifically for cross-entity rules where DI injection eliminates parameter forwarding and generics.
