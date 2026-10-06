# Business Rules

[← Blazor](blazor.md) | [↑ Guides](index.md) | [Change Tracking →](change-tracking.md)

Neatoo rules exist to support data-binding UIs. In Blazor, WPF, or any data-bound client, when a user commits a field, the binding sets the property — and the rules on that property run. Validation messages appear on the field, computed properties update, and the UI stays in sync without orchestration code. Rules are registered in the entity constructor, either through the `RuleManager` fluent API or by adding rule class instances.

The compiled examples on this page come from the Design.Domain reference set: the `Order`/`OrderItem` and `WorkOrder`/`WorkOrderTask` aggregates, `RuleBasicsDemo`, `AsyncRulesDemo`, and `TriggerPatternsDemo`. The tests are MSTest and resolve factories from a DI scope (`DesignTestServices.GetScope()`); in an application the factory interface is injected.

## Fluent Action Rules

Neatoo separates rules into two types that mirror C#'s `Action` vs `Func` distinction. Action rules perform side effects — computing a derived value, updating a related property — without returning anything. They produce no validation messages and do not affect `IsValid`.

A derived value is a `private set` partial property written by an `AddAction` rule; the rule's trigger list names every input:

<!-- snippet: skill-computed-properties -->
<a id='snippet-skill-computed-properties'></a>
```cs
// A derived value is a private-set partial property written by a rule.
// It notifies, it serializes, and the UI sees it read-only.
RuleManager.AddAction(
    t => t.Cost = t.Hours * t.Rate * (1 - t.Discount),
    t => t.Hours,
    t => t.Rate,
    t => t.Discount);
```
<sup><a href='/src/Design/Design.Domain/Aggregates/WorkOrderAggregate/WorkOrderTask.cs#L37-L45' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-computed-properties' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The action executes whenever `Hours`, `Rate` or `Discount` changes. `AddAction` creates an `ActionFluentRule<T>` that executes the lambda when a trigger property changes; the lambda may write other properties on the entity without creating validation messages.

Async actions use `AddActionAsync`. While the action runs, the trigger property and the entity report `IsBusy`:

<!-- snippet: skill-async-action -->
<a id='snippet-skill-async-action'></a>
```cs
public BusyStateDemo(IValidateBaseServices<BusyStateDemo> services) : base(services)
{
    // Add an async rule - IsBusy becomes true while it runs
    RuleManager.AddActionAsync(
        async t =>
        {
            // Simulate async work
            await Task.Delay(100);
            t.ComputedValue = $"Processed: {t.Name}";
        },
        t => t.Name);
}
```
<sup><a href='/src/Design/Design.Domain/PropertySystem/StateProperties.cs#L356-L369' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-async-action' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

An async action that needs the server does not take a repository or any other server-only service — it calls an injected `[Remote, Execute]` command delegate, exactly as the async validation rule under "Custom Rule Classes" does. Rule code runs on the client too.

## Fluent Validation Rules

Validation rules are the `Func` counterpart — they return a string. An empty or null return means validation passed; anything else becomes a message on the trigger property, which drives `IsValid` and shows in the UI.

`AddValidation` takes exactly one trigger property, because the message attaches to that property. Attribute validation and `AddValidation` rules sit side by side:

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

`AddValidationAsync` is the async form. Its dependency rule is the same as for every rule: a server lookup goes through a `[Remote, Execute]` command delegate injected into the entity's constructor, never through a server-only service.

## Cross-Property Validation

"End date must be after start date" — but which property change should trigger that check? Both. `AddValidation` takes one trigger, so a constraint over several properties takes one of two shapes: validate a computed property that an `AddAction` recomputes from every input, or write a `RuleBase<T>` with several triggers so the message lands where it belongs:

<!-- snippet: skill-cross-property-validation-options -->
<a id='snippet-skill-cross-property-validation-options'></a>
```cs
// COMMON MISTAKE: Using the wrong property expression.
//
// WRONG:
//   RuleManager.AddValidation(
//       t => t.A + t.B > 100 ? "Too high" : "",
//       t => t.A);  // Only triggers on A, not B!
//
// AddValidation takes exactly one trigger. For a validation over several
// properties, either:
//
// RIGHT (validate a computed property - the shape used below):
//   RuleManager.AddAction(t => t.Sum = t.A + t.B, t => t.A, t => t.B);
//   RuleManager.AddValidation(t => t.Sum > 100 ? "Too high" : "", t => t.Sum);
//
// RIGHT (a rule class, which takes any number of triggers):
//   internal class SumLimitRule : RuleBase<T>
//   {
//       public SumLimitRule() : base(t => t.A, t => t.B) { }
//       protected override IRuleMessages Execute(T t)
//           => RuleMessages.If(t.A + t.B > 100, nameof(t.A), "Too high");
//   }
```
<sup><a href='/src/Design/Design.Domain/Rules/FluentRules.cs#L192-L214' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-cross-property-validation-options' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: skill-cross-property-validation -->
<a id='snippet-skill-cross-property-validation'></a>
```cs
/// <summary>
/// Demonstrates: Different trigger property patterns.
/// </summary>
[Factory]
internal partial class TriggerPatternsDemo : ValidateBase<TriggerPatternsDemo>, ITriggerPatternsDemo
{
    public partial int A { get; set; }
    public partial int B { get; set; }
    public partial int C { get; set; }
    public partial int Sum { get; set; }
    public partial bool IsOverLimit { get; set; }

    public TriggerPatternsDemo(IValidateBaseServices<TriggerPatternsDemo> services) : base(services)
    {
        // Rule that depends on multiple properties
        RuleManager.AddAction(
            t => t.Sum = t.A + t.B + t.C,
            t => t.A,
            t => t.B,
            t => t.C);

        // Cross-property constraint, validated on the computed Sum: the action
        // above recomputes Sum whenever A, B or C changes, which triggers this.
        RuleManager.AddValidation(
            t => t.Sum > 100 ? "Sum cannot exceed 100" : string.Empty,
            t => t.Sum);

        // Action triggered by computed property
        RuleManager.AddAction(
            t => t.IsOverLimit = t.Sum > 100,
            t => t.Sum);
    }

    [Create]
    public void Create() { }
}
```
<sup><a href='/src/Design/Design.Domain/Rules/FluentRules.cs#L217-L254' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-cross-property-validation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Custom Rule Classes

Fluent lambdas work for one-liners. When the logic grows enough to warrant its own class, when a rule is shared across entity types, or when a rule needs a dependency, inherit from `RuleBase<T>` (synchronous) or `AsyncRuleBase<T>`. Rule classes are `internal`, like the entities they target.

A synchronous rule: trigger properties go to the base constructor, `Execute` returns `IRuleMessages`, `None` means the rule passed:

<!-- snippet: skill-rule-class -->
<a id='snippet-skill-rule-class'></a>
```cs
/// <summary>
/// Demonstrates: Simple validation rule as a class.
/// </summary>
internal class NameRequiredRule : RuleBase<RuleBasicsDemo>
{
    // =========================================================================
    // TriggerProperties - When Does This Rule Run?
    // =========================================================================
    // Rules run when ANY trigger property changes.
    // Specify trigger properties via the base constructor using expressions.
    // =========================================================================
    public NameRequiredRule() : base(t => t.Name) { }

    // =========================================================================
    // Execute - Rule Logic
    // =========================================================================
    // Return IRuleMessages:
    // - (propertyName, message).AsRuleMessages(): Validation failed
    // - None (inherited from AsyncRuleBase): Validation passed (no messages)
    //
    // The messages are associated with the specified property.
    // =========================================================================
    protected override IRuleMessages Execute(RuleBasicsDemo target)
    {
        if (string.IsNullOrWhiteSpace(target.Name))
        {
            // Return error - this makes IsValid=false
            return (nameof(RuleBasicsDemo.Name), "Name is required").AsRuleMessages();
        }

        // Return None - validation passed (None is inherited from AsyncRuleBase)
        return None;
    }
}
```
<sup><a href='/src/Design/Design.Domain/Rules/RuleBasics.cs#L119-L154' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-rule-class' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

A rule with no dependencies is constructed in the entity's constructor:

<!-- snippet: skill-add-rule-inline -->
<a id='snippet-skill-add-rule-inline'></a>
```cs
// Rules with no dependencies are constructed here; a rule that needs
// a command delegate comes from DI instead (see AsyncRules.cs)
RuleManager.AddRule(new NameRequiredRule());
RuleManager.AddRule(new CalculateTotalRule());
```
<sup><a href='/src/Design/Design.Domain/Rules/RuleBasics.cs#L64-L69' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-add-rule-inline' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### A rule that calls the server

A uniqueness check needs the database, and it belongs in a rule so the user sees the answer on the field while editing. Rule code and entity constructors run in the browser, so the rule cannot take a repository. It takes the delegate of a `[Remote, Execute]` command: in the browser the delegate crosses to the server, on the server it calls the method directly, and the repository stays on the server.

The command:

<!-- snippet: skill-rule-command -->
<a id='snippet-skill-rule-command'></a>
```cs
/// <summary>
/// Command the rule calls. [Remote]: a client call crosses to the server.
/// </summary>
[Factory]
public static partial class UsernameAvailability
{
    [Remote]
    [Execute]
    private static Task<bool> _IsAvailable(string username, [Service] IUsernameRepository repository)
    {
        return Task.FromResult(!repository.UsernameExists(username));
    }
}
```
<sup><a href='/src/Design/Design.Domain/Rules/AsyncRules.cs#L117-L131' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-rule-command' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The rule. It never sees the repository, and it has a DI interface so the entity can take it from DI and tests can substitute it. The empty-value early return is the usual way to skip an expensive check:

<!-- snippet: skill-rule-with-command -->
<a id='snippet-skill-rule-with-command'></a>
```cs
/// <summary>
/// Rule interface: the entity takes the rule from DI by this interface, and
/// tests can substitute it.
/// </summary>
internal interface ICheckUsernameAvailabilityRule : IRule<AsyncRulesDemo> { }

/// <summary>
/// Demonstrates: async uniqueness validation through a [Remote, Execute] command.
/// </summary>
internal class CheckUsernameAvailabilityRule : AsyncRuleBase<AsyncRulesDemo>, ICheckUsernameAvailabilityRule
{
    private readonly UsernameAvailability.IsAvailable _isAvailable;

    // Trigger properties are passed to the base constructor
    public CheckUsernameAvailabilityRule(UsernameAvailability.IsAvailable isAvailable) : base(t => t.Username)
    {
        _isAvailable = isAvailable;
    }

    protected override async Task<IRuleMessages> Execute(AsyncRulesDemo target, CancellationToken? token = null)
    {
        if (string.IsNullOrWhiteSpace(target.Username))
        {
            target.IsUsernameAvailable = false;
            return None;  // Don't check empty usernames - None is inherited from AsyncRuleBase
        }

        var available = await _isAvailable(target.Username);

        target.IsUsernameAvailable = available;

        if (!available)
        {
            // Create error message: (propertyName, message).AsRuleMessages()
            return (nameof(AsyncRulesDemo.Username), $"Username '{target.Username}' is already taken").AsRuleMessages();
        }

        return None;
    }
}
```
<sup><a href='/src/Design/Design.Domain/Rules/AsyncRules.cs#L133-L174' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-rule-with-command' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The entity receives the rule by constructor injection:

<!-- snippet: skill-rule-injected -->
<a id='snippet-skill-rule-injected'></a>
```cs
// A rule with a dependency comes from DI through its interface. The
// dependency must exist on both tiers - here, a command delegate.
public AsyncRulesDemo(
    IEntityBaseServices<AsyncRulesDemo> services,
    ICheckUsernameAvailabilityRule usernameAvailabilityRule) : base(services)
{
    // Register async rules
    RuleManager.AddRule(new ValidateEmailFormatRule());
    RuleManager.AddRule(usernameAvailabilityRule);
    RuleManager.AddRule(new FetchExternalDataRule());
}
```
<sup><a href='/src/Design/Design.Domain/Rules/AsyncRules.cs#L55-L67' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-rule-injected' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

A rule's dependencies must resolve on both tiers, because the entity is constructed on both. Rule types are `internal`, so the domain assembly registers them; call this after `AddNeatooServices` on client and server:

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

A rule runs every time its trigger property is set, and rules contain no debouncing. Bind the property so it is set on field commit: `MudNeatooTextField` and `MudNeatooNumericField` commit when the field loses focus, so a server-calling rule behind them makes one call per committed value.

## Business Rule Attributes

Neatoo converts DataAnnotations attributes into rules so they go through the same pipeline as hand-written rules. `[Required]` and `[Range]` fire when the property is set, showing errors as the user edits, rather than in a separate validation pass:

<!-- snippet: skill-validation-attributes -->
<a id='snippet-skill-validation-attributes'></a>
```cs
[Required(ErrorMessage = "Product name is required")]
[StringLength(100)]
public partial string? ProductName { get; set; }

[Range(1, 10000, ErrorMessage = "Quantity must be between 1 and 10000")]
public partial int Quantity { get; set; }

[Range(0.01, 1000000, ErrorMessage = "Unit price must be positive")]
public partial decimal UnitPrice { get; set; }
```
<sup><a href='/src/Design/Design.Domain/Aggregates/OrderAggregate/OrderItem.cs#L32-L42' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-validation-attributes' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Supported attributes: `[Required]`, `[StringLength]`, `[MinLength]`, `[MaxLength]`, `[RegularExpression]`, `[Range]`, and `[EmailAddress]`. Other DataAnnotations attributes are ignored.

## Aggregate-Level Rules

Some invariants cannot be checked by a single entity. "Total cost must not exceed the budget" sums the children and compares against the root — a rule on the aggregate root that reads its children. The trigger must be the child property path (`t => t.Tasks![0].Cost`): a trigger on the collection reference fires only when the collection is reassigned, never when a child changes. The `[0]` indexer is a syntactic placeholder for "any child".

<!-- snippet: skill-child-aggregation -->
<a id='snippet-skill-child-aggregation'></a>
```cs
// Aggregation over the children, recomputed when any task's Cost changes
RuleManager.AddAction(
    t => t.TotalCost = t.Tasks?.Sum(task => task.Cost) ?? 0,
    t => t.Tasks![0].Cost);
```
<sup><a href='/src/Design/Design.Domain/Aggregates/WorkOrderAggregate/WorkOrder.cs#L70-L75' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-child-aggregation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The aggregation writes `TotalCost`, which triggers the budget rule — a chain, with no handler code:

<!-- snippet: skill-chained-rule -->
<a id='snippet-skill-chained-rule'></a>
```cs
// Chained: the rule above sets TotalCost, which triggers this one.
// Hours -> Cost (on the task) -> TotalCost -> IsOverBudget, no handler code.
RuleManager.AddAction(
    t => t.IsOverBudget = t.TotalCost > t.Budget,
    t => t.TotalCost,
    t => t.Budget);
```
<sup><a href='/src/Design/Design.Domain/Aggregates/WorkOrderAggregate/WorkOrder.cs#L77-L84' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-chained-rule' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: skill-aggregation-test -->
<a id='snippet-skill-aggregation-test'></a>
```cs
[TestMethod]
public async Task TaskCosts_RollUpToTheRoot_AndChainIntoIsOverBudget()
{
    var (order, design, build) = await CreateWithTwoTasks();
    order.Budget = 100m;

    design.Hours = 2m;
    design.Rate = 30m;   // Cost 60
    build.Hours = 1m;
    build.Rate = 50m;    // Cost 50
    await order.WaitForTasks();

    Assert.AreEqual(60m, design.Cost, "The task's own rule computed Cost");
    Assert.AreEqual(110m, order.TotalCost, "The root's child-trigger rule summed the tasks");
    Assert.IsTrue(order.IsOverBudget, "Setting TotalCost triggered the chained rule");
}
```
<sup><a href='/src/Design/Design.Tests/AggregateTests/WorkOrderAggregateTests.cs#L66-L83' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-aggregation-test' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Four or more triggers take an explicit `Expression<Func<T, object?>>[]` (the overloads are not `params`, because the rule's id comes from `CallerArgumentExpression`):

<!-- snippet: skill-four-trigger-array -->
<a id='snippet-skill-four-trigger-array'></a>
```cs
// Four or more triggers take an explicit array: params is incompatible
// with the CallerArgumentExpression that gives the rule its id.
RuleManager.AddAction(
    t => t.Summary = $"{t.Status}; on hold: {t.IsOnHold}; budget {t.Budget}; discount {t.Discount}",
    new Expression<Func<WorkOrder, object?>>[]
    {
        t => t.Status, t => t.IsOnHold, t => t.Budget, t => t.Discount
    });
```
<sup><a href='/src/Design/Design.Domain/Aggregates/WorkOrderAggregate/WorkOrder.cs#L112-L121' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-four-trigger-array' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Rule Execution Order

Rules triggered by one property change execute one after another, each awaited, not in parallel. An action rule that computes a value must finish before a validation rule that reads it, and sequential execution gives a predictable model: you can reason about rule order without reasoning about concurrency. Rules triggered by *different* property changes can overlap.

When a property changes, the framework identifies the rules with that property as a trigger, sorts them by `RuleOrder` (ascending; lower first; default 1), and executes them in that order. Within the same `RuleOrder`, registration order applies. Set `RuleOrder` in a rule class constructor:

<!-- snippet: docs-rule-order -->
<a id='snippet-docs-rule-order'></a>
```cs
/// <summary>Runs before rules with the default RuleOrder (1).</summary>
internal class EarlyTraceRule : RuleBase<RuleBasicsDemo>
{
    public EarlyTraceRule() : base(t => t.Name)
    {
        RuleOrder = -10;   // lower runs first; the default is 1
    }

    protected override IRuleMessages Execute(RuleBasicsDemo target)
    {
        target.RuleTrace += "early;";
        return None;
    }
}

/// <summary>
/// Default RuleOrder: runs after EarlyTraceRule although it is registered first.
/// </summary>
internal class LateTraceRule : RuleBase<RuleBasicsDemo>
{
    public LateTraceRule() : base(t => t.Name) { }

    protected override IRuleMessages Execute(RuleBasicsDemo target)
    {
        target.RuleTrace += "late;";
        return None;
    }
}
```
<sup><a href='/src/Design/Design.Domain/Rules/RuleBasics.cs#L245-L274' title='Snippet source file'>snippet source</a> | <a href='#snippet-docs-rule-order' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: docs-rule-order-test -->
<a id='snippet-docs-rule-order-test'></a>
```cs
[TestMethod]
public async Task RuleOrder_LowerValuesRunFirst_RegardlessOfRegistrationOrder()
{
    var entity = _factory.Create();

    entity.Name = "Ada";   // both trace rules trigger on Name
    await entity.WaitForTasks();

    Assert.AreEqual("early;late;", entity.RuleTrace,
        "RuleOrder -10 ran before the default (1), although it was registered second");
}
```
<sup><a href='/src/Design/Design.Tests/RuleTests/SyncRuleTests.cs#L104-L116' title='Snippet source file'>snippet source</a> | <a href='#snippet-docs-rule-order-test' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Rules do not run during a factory operation (`[Create]`, `[Fetch]`, ...): the object is paused. A factory method that sets inputs of a computed property ends with `await RunRules(RunRulesFlag.All)`:

<!-- snippet: skill-create-run-rules -->
<a id='snippet-skill-create-run-rules'></a>
```cs
/// <summary>
/// RIGHT WAY: Call RunRules at end of factory method.
/// RunRules works even while paused — no IsPaused guard.
/// </summary>
[Create]
public async Task CreateWithRunRules()
{
    Quantity = 10;
    Price = 5.00m;
    await RunRules(RunRulesFlag.All);  // Forces all rules to execute
    // Total is now 50.00
}
```
<sup><a href='/src/Design/Design.Domain/CommonGotchas.cs#L97-L110' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-create-run-rules' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Conditional Rules

A rule always runs when a trigger property changes; it decides for itself whether there is anything to say. Returning `None` early — for an empty value, or when the entity is in a state where the check does not apply — is also how a rule avoids an expensive call it does not need. `CheckUsernameAvailabilityRule` above returns `None` for an empty username before it calls the command.

A root's state can also be read by a child's rule through `Parent`. `Parent` is null until the child is attached, so pattern-match rather than cast:

<!-- snippet: skill-parent-in-child-rule -->
<a id='snippet-skill-parent-in-child-rule'></a>
```cs
// A child rule reads ambient root state through Parent. Parent is null
// until the task is attached, so pattern-match instead of casting.
RuleManager.AddAction(
    t => t.IsSchedulable = t.Hours > 0 && t.Parent is IWorkOrder root && !root.IsOnHold,
    t => t.Hours);
```
<sup><a href='/src/Design/Design.Domain/Aggregates/WorkOrderAggregate/WorkOrderTask.cs#L47-L53' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-parent-in-child-rule' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Async Business Rules

An async rule receives an optional `CancellationToken`. The token arrives only from an explicit `RunRules(..., token)`; a setter-triggered run passes none. A cancelled `RunRules` marks the object invalid with "Validation cancelled", and `RunRules(RunRulesFlag.All)` re-validates:

<!-- snippet: skill-cancellable-rule -->
<a id='snippet-skill-cancellable-rule'></a>
```cs
/// <summary>
/// Demonstrates: Rule with cancellation support.
/// </summary>
internal class CancellableRule : AsyncRuleBase<AsyncRulesDemo>
{
    public CancellableRule() : base(t => t.Username) { }

    protected override async Task<IRuleMessages> Execute(AsyncRulesDemo target, CancellationToken? token = null)
    {
        // Check cancellation before expensive operation
        token?.ThrowIfCancellationRequested();

        // Simulate expensive async operation
        await Task.Delay(1000);

        // Check cancellation again for very long operations
        token?.ThrowIfCancellationRequested();

        return None;
    }
}
```
<sup><a href='/src/Design/Design.Domain/Rules/AsyncRules.cs#L250-L272' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-cancellable-rule' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Async rules track busy state: the trigger property and the entity report `IsBusy` from the moment the rule starts until it completes, so a bound UI can disable a Save button or show a spinner. Always `await WaitForTasks()` before reading `IsValid` or saving when async rules may be in flight:

<!-- snippet: skill-is-busy -->
<a id='snippet-skill-is-busy'></a>
```cs
[TestMethod]
public async Task AsyncRule_SetsIsBusyUntilItCompletes()
{
    var entity = _scope.GetRequiredService<IBusyStateDemoFactory>().Create();

    entity.Name = "Test";  // triggers the async action rule

    Assert.IsTrue(entity.IsBusy, "The async rule is still running");

    await entity.WaitForTasks();

    Assert.IsFalse(entity.IsBusy);
    Assert.AreEqual("Processed: Test", entity.ComputedValue);
}
```
<sup><a href='/src/Design/Design.Tests/PropertyTests/ValidationStateTests.cs#L52-L67' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-is-busy' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## LoadProperty - Writing a Property Without Triggering Rules

Rules trigger when properties change — but what if a rule *itself* changes a property? If Rule A triggers on `Quantity` and sets `Total`, and Rule B triggers on `Total` and sets `Quantity`, you get an infinite loop. `LoadProperty` breaks the cycle by writing to the property through its wrapper's `LoadValue`, without firing triggers.

<!-- snippet: docs-load-property -->
<a id='snippet-docs-load-property'></a>
```cs
/// <summary>
/// Demonstrates: LoadProperty writes a property through its wrapper's
/// LoadValue - no rules registered on that property run.
/// </summary>
[Factory]
internal partial class LoadPropertyDemo : ValidateBase<LoadPropertyDemo>, ILoadPropertyDemo
{
    public partial int Quantity { get; set; }
    public partial decimal UnitPrice { get; set; }
    public partial decimal Total { get; private set; }
    public partial bool TotalRuleRan { get; private set; }

    public LoadPropertyDemo(IValidateBaseServices<LoadPropertyDemo> services) : base(services)
    {
        RuleManager.AddRule(new LoadPropertyTotalRule());

        // A rule on Total: it does NOT run when Total is written by LoadProperty
        RuleManager.AddAction(t => t.TotalRuleRan = true, t => t.Total);
    }

    [Create]
    public void Create() { }
}

internal class LoadPropertyTotalRule : RuleBase<LoadPropertyDemo>
{
    public LoadPropertyTotalRule() : base(t => t.Quantity, t => t.UnitPrice) { }

    protected override IRuleMessages Execute(LoadPropertyDemo target)
    {
        // Written without triggering the rules registered on Total
        LoadProperty(target, t => t.Total, target.Quantity * target.UnitPrice);
        return None;
    }
}
```
<sup><a href='/src/Design/Design.Domain/Rules/RuleBasics.cs#L446-L482' title='Snippet source file'>snippet source</a> | <a href='#snippet-docs-load-property' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: docs-load-property-test -->
<a id='snippet-docs-load-property-test'></a>
```cs
[TestMethod]
public async Task LoadProperty_WritesTheValue_WithoutRunningTheRulesOnThatProperty()
{
    var entity = _scope.GetRequiredService<ILoadPropertyDemoFactory>().Create();

    entity.Quantity = 3;
    entity.UnitPrice = 2.50m;
    await entity.WaitForTasks();

    Assert.AreEqual(7.50m, entity.Total, "The rule computed Total");
    Assert.IsFalse(entity.TotalRuleRan, "...but the rule registered on Total did not run: LoadProperty fires no triggers");
}
```
<sup><a href='/src/Design/Design.Tests/RuleTests/SyncRuleTests.cs#L118-L131' title='Snippet source file'>snippet source</a> | <a href='#snippet-docs-load-property-test' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`LoadProperty` is a protected method on `RuleBase<T>`/`AsyncRuleBase<T>`. It calls the property wrapper's `LoadValue`, so no rules registered on that property run. On an entity, a value written this way is also not marked modified. Use it when a rule must update a property without triggering the rules registered on that property; the default for a computed property is the setter of a `private set` partial, as in the first section.

## Rule Registration Patterns

Rules are registered in the target class constructor.

Fluent rules for simple, entity-specific logic — the `WorkOrder` root registers an admission rule and two visibility flags this way:

<!-- snippet: skill-can-x-rule -->
<a id='snippet-skill-can-x-rule'></a>
```cs
// Admission for the Approve verb. The UI binds the button to it; the
// verb itself does not check and throw.
RuleManager.AddAction(
    t => t.CanApprove = t.Status == "Pending",
    t => t.Status);
```
<sup><a href='/src/Design/Design.Domain/Aggregates/WorkOrderAggregate/WorkOrder.cs#L48-L54' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-can-x-rule' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: skill-visibility-rules -->
<a id='snippet-skill-visibility-rules'></a>
```cs
// A show/hide decision is domain state. The UI binds @if (ShowHoldBanner).
RuleManager.AddAction(
    t => t.ShowHoldBanner = t.IsOnHold && t.Status != "Closed",
    t => t.IsOnHold,
    t => t.Status);

// A flag derived from a child property. The trigger is the child's
// property path, so any task's IsSchedulable change recomputes it.
RuleManager.AddAction(
    t => t.HasUnschedulableTasks = t.Tasks?.Any(task => !task.IsSchedulable) ?? false,
    t => t.Tasks![0].IsSchedulable);
```
<sup><a href='/src/Design/Design.Domain/Aggregates/WorkOrderAggregate/WorkOrder.cs#L56-L68' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-visibility-rules' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Rule classes with no dependencies are constructed inline (`RuleManager.AddRule(new NameRequiredRule())`, shown under "Custom Rule Classes"). A rule class with a dependency comes from DI through its interface and is registered on both tiers (`skill-rule-injected` and the registration above). A rule shared by several entity types operates on a shared interface — see the `IHasUniqueCode` example in the Neatoo skill's shared-rules reference.

## Trigger Properties

Rules declare which properties trigger their execution. When any trigger property changes, the rule executes. Triggers are lambda expressions passed to the rule's base constructor: a single trigger (`: base(t => t.Name)`, as in `NameRequiredRule`) or several (`: base(t => t.Quantity, t => t.Price)`, as in `MultiMessageRule` below). A rule can also add triggers after construction with `AddTriggerProperties(t => t.FirstName)`. A child property path (`t => t.Items![0].LineTotal`) triggers on any child's change:

<!-- snippet: skill-child-property-trigger -->
<a id='snippet-skill-child-property-trigger'></a>
```cs
RuleManager.AddAction(
    t => t.TotalAmount = t.Items?.Sum(i => i.LineTotal) ?? 0,
    t => t.Items![0].LineTotal);
```
<sup><a href='/src/Design/Design.Domain/Aggregates/OrderAggregate/Order.cs#L98-L102' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-child-property-trigger' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Trigger on every input the rule reads. A rule that computes `Total` from `Quantity` and `Price` but triggers only on `Quantity` leaves `Total` stale when `Price` changes.

## Rule Messages

Rules return `IRuleMessages` containing zero or more validation messages. `None` (inherited from `AsyncRuleBase<T>`) means the rule passed; `(propertyName, message).AsRuleMessages()` returns a single message attached to a property — both shown in `NameRequiredRule` above.

Return every failing check at once with the `RuleMessages.If` builder, so all invalid fields show their messages at the same time instead of one at a time:

<!-- snippet: skill-multi-message-rule -->
<a id='snippet-skill-multi-message-rule'></a>
```cs
/// <summary>
/// Demonstrates: Rule returning multiple messages.
/// </summary>
internal class MultiMessageRule : RuleBase<RuleBasicsDemo>
{
    public MultiMessageRule() : base(t => t.Quantity, t => t.Price) { }

    protected override IRuleMessages Execute(RuleBasicsDemo target)
    {
        // Use fluent API to build multiple conditional messages
        return new RuleMessages()
            .If(target.Quantity < 0, nameof(RuleBasicsDemo.Quantity), "Quantity cannot be negative")
            .If(target.Price < 0, nameof(RuleBasicsDemo.Price), "Price cannot be negative")
            .If(target.Quantity > 1000, nameof(RuleBasicsDemo.Quantity), "Quantity exceeds maximum order limit");
    }
}
```
<sup><a href='/src/Design/Design.Domain/Rules/RuleBasics.cs#L201-L218' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-multi-message-rule' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Messages attach to the named property and display through the validation UI; the entity's `PropertyMessages` aggregates them across the graph.

## Manual Rule Execution

Rules fire when a property is set outside a factory operation, so the routine step before reading `IsValid` is `await WaitForTasks()`, not `RunRules`. `RunRules` forces a re-run. It is needed in two places:

- **At the end of a factory method** that set inputs of computed properties — the object was paused, so nothing ran (`skill-create-run-rules` above).
- **On the server, before a write.** The root's `[Insert]`/`[Update]` re-run every rule and refuse an invalid aggregate; this is the one validation gate in a factory method:

<!-- snippet: skill-root-insert -->
<a id='snippet-skill-root-insert'></a>
```cs
[Remote]
[Insert]
internal async Task Insert([Service] IOrderRepository repository,
    [Service] IOrderItemListFactory itemsFactory)
{
    // Re-run every rule on the server and refuse an invalid aggregate.
    // Recommended - the framework does not do this for you. Throw, never
    // return: after [Insert]/[Update] returns, the framework marks the
    // entity saved whether or not anything was written.
    await RunRules(RunRulesFlag.All);
    if (!IsValid)
    {
        throw new SaveOperationException(SaveFailureReason.IsInvalid);
    }

    // Object is paused — assignment is clean
    Id = Guid.NewGuid();

    var row = new OrderRow();
    MapTo(row);
    repository.Add(row);

    itemsFactory.Save(Items!, row.Items);

    repository.SaveChanges();
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/OrderAggregate/Order.cs#L186-L213' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-root-insert' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Forcing a run after `Create`:

<!-- snippet: skill-run-rules-forces -->
<a id='snippet-skill-run-rules-forces'></a>
```cs
[TestMethod]
public async Task Gotcha1_RulesFireAfterCreate_WithExplicitRunRules()
{
    // Arrange
    var factory = _scope.GetRequiredService<IGotcha1DemoFactory>();

    // Act
    var entity = factory.Create();

    // RunRules works even after factory (IsPaused is now false)
    await entity.RunRules(RunRulesFlag.All);

    // Assert - Now the rule has run
    Assert.AreEqual(50.00m, entity.Total, "Total should be calculated after RunRules");
    Assert.IsTrue(entity.RuleHasRun, "Rule should have run after explicit RunRules call");
}
```
<sup><a href='/src/Design/Design.Tests/GotchaTests/CommonGotchaTests.cs#L52-L69' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-run-rules-forces' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The `RunRulesFlag` enum selects which rules run:
- `All`: Clear all messages and run all rules
- `Self`: Run this object's rules only (skip child property rules)
- `NotExecuted`: Run only rules that have not executed yet (does not clear messages)
- `Executed`: Run only rules that have already executed
- `None`: No-op
- `NoMessages` / `Messages`: **Known bug** -- `NoMessages` always matches and `Messages` never matches because the per-rule `Messages` collection is never populated. Avoid these flags until fixed.

Flags combine (`NotExecuted | Executed` runs all rules). `RunRules("PropertyName")` runs only the rules that have that property as a trigger — the task list in the WorkOrder aggregate uses it to re-run a sibling's `Sequence` rule when another task's `Sequence` changes:

<!-- snippet: skill-cross-sibling-rules -->
<a id='snippet-skill-cross-sibling-rules'></a>
```cs
// Cross-sibling consistency lives on the LIST: an entity cannot override
// HandleNeatooPropertyChanged, and only the list sees every sibling. When a
// task's Sequence changes, re-run the siblings' rules so their uniqueness
// messages update too.
protected override async Task HandleNeatooPropertyChanged(NeatooPropertyChangedEventArgs eventArgs)
{
    await base.HandleNeatooPropertyChanged(eventArgs);

    if (eventArgs.PropertyName == nameof(IWorkOrderTask.Sequence)
        && eventArgs.Source is IWorkOrderTask changed)
    {
        await Task.WhenAll(this.Except([changed])
            .Select(sibling => sibling.RunRules(nameof(IWorkOrderTask.Sequence))));
    }
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/WorkOrderAggregate/WorkOrderTaskList.cs#L26-L42' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-cross-sibling-rules' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Advanced: Stable Rule IDs

In RemoteFactory scenarios, the domain model graph crosses the client-server boundary. If a rule fails server-side, that broken state travels back to the client with the graph. When the user fixes the data, the client needs to know exactly which rule to re-run and clear. This only works if the same rule has the same ID on both sides.

Neatoo assigns deterministic rule IDs from the source text of the argument passed to `AddAction`, `AddValidation`, or `AddRule`, captured with `CallerArgumentExpression` and hashed (FNV-1a) into a generated `GetRuleId` override on the entity. Both tiers compile the same source, so they compute the same ids.

---

**UPDATED:** 2026-10-06
