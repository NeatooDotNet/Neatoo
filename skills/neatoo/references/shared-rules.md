# Shared Rules via Interface-Typed AsyncRuleBase

When the same rule applies to more than one entity type, make the rule operate on a shared interface instead of being generic per entity. The rule is written once, registered in DI once, and injected into each entity's constructor. If it needs the server, it calls a `[Remote, Execute]` command delegate — never a repository or any other server-only service, because the rule is constructed on the client too.

## The Pattern

### 1. Define a shared interface extending IValidateBase

The interface must extend `IValidateBase` to satisfy `AsyncRuleBase<T>`'s constraint (`where T : class, IValidateBase`). Include only the properties the rule needs:

<!-- snippet: skill-shared-rule-interface -->
<a id='snippet-skill-shared-rule-interface'></a>
```cs
/// <summary>
/// What the rule needs from any entity it validates. Extends IValidateBase so
/// it satisfies AsyncRuleBase&lt;T&gt;'s constraint.
/// </summary>
public interface IHasUniqueCode : IValidateBase
{
    Guid Id { get; }
    string? Code { get; }
}
```
<sup><a href='/src/Design/Design.Domain/Rules/SharedRules.cs#L29-L39' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-shared-rule-interface' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`IValidateBase` is already implemented by every Neatoo base class, so adding it to the interface introduces no new obligations on implementing entities.

### 2. The command the rule calls

The server call is a `[Remote, Execute]` command. The repository stays on the server:

<!-- snippet: skill-shared-rule-command -->
<a id='snippet-skill-shared-rule-command'></a>
```cs
/// <summary>
/// The server call the rule makes. [Remote]: a client call crosses to the server.
/// </summary>
[Factory]
public static partial class CodeUniqueness
{
    [Remote]
    [Execute]
    private static Task<bool> _IsUnique(Guid id, string code, [Service] ICodeRepository repository)
    {
        return Task.FromResult(!repository.CodeExists(id, code));
    }
}
```
<sup><a href='/src/Design/Design.Domain/Rules/SharedRules.cs#L41-L55' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-shared-rule-command' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### 3. Create a DI interface for the rule

The interface extends `IRule<IHasUniqueCode>` so it can be passed directly to `RuleManager.AddRule`:

<!-- snippet: skill-shared-rule-di-interface -->
<a id='snippet-skill-shared-rule-di-interface'></a>
```cs
/// <summary>
/// DI interface for the rule. Extends IRule&lt;IHasUniqueCode&gt; so an entity
/// can hand it straight to RuleManager.AddRule.
/// </summary>
public interface IUniqueCodeRule : IRule<IHasUniqueCode> { }
```
<sup><a href='/src/Design/Design.Domain/Rules/SharedRules.cs#L57-L63' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-shared-rule-di-interface' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### 4. Write a non-generic rule on the interface

The rule's type parameter is the shared interface, not a specific entity. Its only dependency is the command delegate:

<!-- snippet: skill-shared-rule-class -->
<a id='snippet-skill-shared-rule-class'></a>
```cs
/// <summary>
/// Demonstrates: a rule typed on a shared interface, calling the server
/// through a command delegate.
/// </summary>
internal class UniqueCodeRule : AsyncRuleBase<IHasUniqueCode>, IUniqueCodeRule
{
    private readonly CodeUniqueness.IsUnique _isUnique;

    public UniqueCodeRule(CodeUniqueness.IsUnique isUnique) : base(t => t.Code)
    {
        _isUnique = isUnique;
    }

    protected override async Task<IRuleMessages> Execute(IHasUniqueCode target, CancellationToken? token = null)
    {
        if (string.IsNullOrWhiteSpace(target.Code))
        {
            return None;
        }

        return await _isUnique(target.Id, target.Code)
            ? None
            : (nameof(IHasUniqueCode.Code), $"Code '{target.Code}' is already in use").AsRuleMessages();
    }
}
```
<sup><a href='/src/Design/Design.Domain/Rules/SharedRules.cs#L65-L91' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-shared-rule-class' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### 5. Entities implement the shared interface and inject the rule

Each entity's interface extends `IEntityRoot` and the shared interface. The entity takes the rule from DI:

<!-- snippet: skill-shared-rule-entity -->
<a id='snippet-skill-shared-rule-entity'></a>
```cs
/// <summary>
/// Demonstrates: an entity implementing the shared interface and taking the
/// rule from DI.
/// </summary>
[Factory]
internal partial class Warehouse : EntityBase<Warehouse>, IWarehouse
{
    public partial Guid Id { get; set; }
    public partial string? Name { get; set; }
    public partial string? Code { get; set; }

    public Warehouse(IEntityBaseServices<Warehouse> services, IUniqueCodeRule uniqueCodeRule) : base(services)
    {
        RuleManager.AddRule(uniqueCodeRule);
    }

    [Create]
    public void Create()
    {
        Id = Guid.NewGuid();
    }
}
```
<sup><a href='/src/Design/Design.Domain/Rules/SharedRules.cs#L109-L132' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-shared-rule-entity' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`RuleManager.AddRule` is generic at the method level (`AddRule<T>`), not at the class level. When called with an `IRule<IHasUniqueCode>`, T is inferred as `IHasUniqueCode`. Since `Warehouse` implements `IHasUniqueCode`, the rule executes against the entity at runtime.

### 6. Register in DI, on both tiers

Rule types are `internal`, so the domain assembly registers them. Transient: each entity instance gets its own rule instance, because a rule tracks execution state.

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
        return services;
    }
}
```
<sup><a href='/src/Design/Design.Domain/DI/DomainRegistration.cs#L14-L31' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-rules-di-registration' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The same rule validates both entity types:

<!-- snippet: skill-shared-rule-test -->
<a id='snippet-skill-shared-rule-test'></a>
```cs
[TestMethod]
public async Task SharedRule_RunsOnBothEntityTypes()
{
    var warehouse = _scope.GetRequiredService<IWarehouseFactory>().Create();
    var supplier = _scope.GetRequiredService<ISupplierFactory>().Create();

    warehouse.Code = "TAKEN";
    supplier.Code = "TAKEN";
    await warehouse.WaitForTasks();
    await supplier.WaitForTasks();

    Assert.IsFalse(warehouse.IsValid, "The command reported the code taken");
    Assert.IsFalse(supplier.IsValid, "Same rule, other entity type");

    warehouse.Code = "WH-1";
    await warehouse.WaitForTasks();
    Assert.IsTrue(warehouse.IsValid);
}
```
<sup><a href='/src/Design/Design.Tests/RuleTests/SharedRuleTests.cs#L31-L50' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-shared-rule-test' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Why This Works

- `AddRule<T>` infers T from the rule argument, not from the entity class. T = `IHasUniqueCode`.
- The entity implements `IHasUniqueCode`, so the runtime cast succeeds.
- The rule's dependency (the command delegate) is resolved by DI on whichever tier builds the entity.
- Entities no longer need to know about the rule's dependencies.

## When to Use

- A rule applies to 2+ entity types that share common properties
- The rule needs a `[Remote, Execute]` command delegate, or a service registered on both tiers (never a repository or other server-only service)
- Entity constructors are accumulating parameters only to forward them to rules

## Contrast with Entity-Specific Rules

A rule specific to one entity type with no dependencies is constructed in the entity's constructor:

<!-- snippet: skill-add-rule-inline -->
<a id='snippet-skill-add-rule-inline'></a>
```cs
public RuleBasicsDemo(IEntityBaseServices<RuleBasicsDemo> services) : base(services)
{
    // Rules with no dependencies are constructed here; a rule that needs
    // a command delegate comes from DI instead (see AsyncRules.cs)
    RuleManager.AddRule(new NameRequiredRule());
    RuleManager.AddRule(new CalculateTotalRule());
}
```
<sup><a href='/src/Design/Design.Domain/Rules/RuleBasics.cs#L59-L67' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-add-rule-inline' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

An entity-specific rule that needs a command delegate still comes from DI through its own interface (see `validation.md` → "Async Rules That Call the Server"). The shared-rule pattern adds the shared interface so one rule and one registration serve several entity types.
