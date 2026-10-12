# Validation

Neatoo's validation system has synchronous and asynchronous rules, standard validation attributes, and cross-property validation. Validation is a rule: a violation is a message on a property, `IsValid` goes false, and the save is blocked while the user is still editing. Exceptions are for application failures, never for validation.

## Basic Validation Rule

Validation attributes on the properties, and `AddValidation` rules in the constructor. The message is attached to the trigger property:

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

## Validation Attributes

Standard .NET validation attributes work on partial properties:

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

## Class-Based Rules

A rule with more than a line or two of logic, or with a dependency, is a class. A synchronous rule derives from `RuleBase<T>` and returns `IRuleMessages` directly; trigger properties go to the base constructor:

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

Several messages from one rule, with the `RuleMessages.If` builder — every failing check is reported at once, not just the first:

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

## Cross-Property Validation

`AddValidation` takes exactly one trigger property, because the message is attached to that property. For a validation over several properties, either validate a computed property (an `AddAction` recomputes it from every input, which triggers the validation), or write a `RuleBase<T>` with several triggers:

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

A rule that sets a property triggers the rules on that property, so chains run on their own:

<!-- snippet: skill-chained-rules -->
<a id='snippet-skill-chained-rules'></a>
```cs
[TestMethod]
public async Task ChainedRules_ActionOnSumFiresWhenSumIsRecomputed()
{
    var entity = _triggerFactory.Create();

    // A rule that sets Sum triggers the rules whose trigger is Sum
    entity.A = 60;
    entity.B = 50;
    await entity.WaitForTasks();

    Assert.AreEqual(110, entity.Sum);
    Assert.IsTrue(entity.IsOverLimit, "The second rule ran because the first set Sum");
    Assert.IsFalse(entity.IsValid, "...and so did the validation on Sum");
}
```
<sup><a href='/src/Design/Design.Tests/RuleTests/FluentRuleTests.cs#L89-L104' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-chained-rules' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Async Rules That Call the Server

A rule may call the server. This is what `AsyncRuleBase<T>`, `AddValidationAsync` and `AddActionAsync` are for: a uniqueness check, an overlap check, a duplicate lookup. Because the rule is on the entity, the user sees the answer on the field while editing.

**Hard rule: a rule takes a command, never a server-only service.** Rule code and entity constructors run in the browser. A rule or an entity constructor that takes a repository, an Entity Framework service, or any other server-only type breaks construction on the client. The rule takes the delegate of a `[Remote, Execute]` command instead. In the browser the delegate crosses to the server; on the server the same delegate calls the method directly.

The command. `[Remote]` makes the client call cross to the server; the repository stays there:

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
<sup><a href='/src/Design/Design.Domain/Rules/AsyncRules.cs#L126-L140' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-rule-command' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The rule takes the command delegate. It never sees the repository. The rule has a DI interface so the entity can take it from DI and tests can substitute it:

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
<sup><a href='/src/Design/Design.Domain/Rules/AsyncRules.cs#L142-L183' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-rule-with-command' title='Start of snippet'>anchor</a></sup>
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

Rule types are `internal`, so the domain assembly registers them. Call this on **both** tiers, after `AddNeatooServices`: an entity that takes a rule in its constructor is built on the client too.

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

**Trigger on field commit, not per keystroke.** A rule runs every time its trigger property is set, and rules contain no debouncing. `MudNeatooTextField` and `MudNeatooNumericField` set the property when the field loses focus, so a rule behind them makes one server call per committed value.

For a rule shared by several entity types, see [shared-rules.md](shared-rules.md).

### Fluent vs Class-Based Rules

| Aspect | Fluent (`AddValidation`/`AddAction`) | Class-Based (`RuleBase<T>`/`AsyncRuleBase<T>`) |
|--------|--------------------------------------|-----------------------------------------------|
| Best for | Simple single-expression rules | Complex rules, rules that take a command delegate |
| Dependencies | Only via closure over constructor parameters | Constructor injection |
| Reusability | Inline, not reusable | Reusable across entities |
| Trigger properties | **Validation**: 1 only. **Action**: 1, 2, 3, or array | Any number via base constructor |
| Multi-property writes | Awkward | Natural — full access to target |

Rules operate directly on the target `T`. Return validation messages via `(propertyName, message).AsRuleMessages()`, or `None` when validation passes.

See [domain-logic-placement.md](domain-logic-placement.md) for when to use rules vs verbs vs seams.

## Checking Validation State

`IsValid` aggregates the object and every descendant; `IsSelfValid` is this object alone. A child's messages reach the parent's `PropertyMessages`:

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

## Property-Level State and PropertyMessages

Each property is backed by its own property object, reached through the indexer. It carries `IsValid`, `PropertyMessages`, `IsBusy` and `IsReadOnly`; the object's `PropertyMessages` aggregates every property's messages:

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

## Waiting for Async Rules

While an async rule runs, `IsBusy` is true and `IsSavable` is false. Always `await WaitForTasks()` before reading `IsValid` or saving when async rules may be in flight:

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

## Forcing a Re-run: RunRules

Rules run on their own when a property is set outside a factory operation. `RunRules` is for forcing a re-run — most often at the end of a `[Create]` or `[Fetch]` that set properties while the object was paused, so that computed properties populate:

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

`RunRules(RunRulesFlag.All)` clears existing messages and runs every rule; `RunRules("PropertyName")` runs the rules for one property. `RunRules` has no `IsPaused` guard, so it works inside a factory operation. See [rules-lifecycle.md](rules-lifecycle.md).

## Pausing Rules

`PauseAllActions()` returns an `IDisposable`; while paused, property setters run no rules and raise no `PropertyChanged`, and `ResumeAllActions` does not run the skipped rules:

<!-- snippet: skill-pause-all-actions -->
<a id='snippet-skill-pause-all-actions'></a>
```cs
[TestMethod]
public void Gotcha4_PausedPropertyChanges_DoNotTriggerRules()
{
    // Arrange
    var factory = _scope.GetRequiredService<IGotcha4DemoFactory>();
    var entity = factory.Create();

    // Act - Modify properties while paused
    using (entity.PauseAllActions())
    {
        entity.Quantity = 10;
        entity.Price = 5.00m;
    }
    // ResumeAllActions() is called, but rules don't automatically run

    // Assert - Total is NOT calculated
    Assert.AreEqual(0m, entity.Total, "Total should be 0 - rules did not run while paused");
}
```
<sup><a href='/src/Design/Design.Tests/GotchaTests/CommonGotchaTests.cs#L177-L196' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-pause-all-actions' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Never use it inside a factory operation: the operation is already paused, and disposing the `using` resumes the object early. On an entity, a property set while paused is not marked modified, so edits made under `PauseAllActions()` on a fetched entity are not saved.

## Validation Before Save

`entity.Save()` refuses to run unless `IsSavable` is true, so an invalid entity is never sent. A direct `factory.Save(target)` does not check, which is why the root's `[Insert]` and `[Update]` re-run the rules on the server and throw `SaveOperationException(SaveFailureReason.IsInvalid)`:

<!-- snippet: skill-server-gate-refuses -->
<a id='snippet-skill-server-gate-refuses'></a>
```cs
[TestMethod]
public async Task InvalidOrder_DirectFactorySave_ServerRulesRefuseBeforeWriting()
{
    // Arrange: CustomerName is [Required], but rules do not run during
    // [Create], so the new order still reports valid on this side.
    var order = _orderFactory.Create();
    Assert.IsTrue(order.IsValid, "No rule has run yet, so nothing is broken");

    // Act: a direct factory.Save does not check IsSavable, so only the
    // re-run of the rules inside [Insert] stands between this order and
    // the write.
    var ex = await Assert.ThrowsExactlyAsync<Neatoo.SaveOperationException>(
        () => _orderFactory.Save(order));

    // Assert: refused, and nothing was written
    Assert.AreEqual(Neatoo.SaveFailureReason.IsInvalid, ex.Reason);
    Assert.AreEqual(0, _repository.AddedRows.Count, "No row may be added");
    Assert.AreEqual(0, _repository.SaveChangesCount, "No flush may happen");
}
```
<sup><a href='/src/Design/Design.Tests/AggregateTests/AggregateLifecycleTests.cs#L188-L208' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-server-gate-refuses' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Async Action Rules (AddActionAsync)

`AddActionAsync` performs async work when a property changes — it returns no message. Server data comes through an injected `[Remote, Execute]` command delegate, as for any rule; the demo below simulates the wait:

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

### Multiple Trigger Properties

`AddAction`, `AddActionAsync` and `AddValidation` take 1–3 trigger properties as individual parameters. For 4+ triggers, pass an explicit `Expression<Func<T, object?>>[]` (the overloads cannot be `params` because the rule's id comes from `CallerArgumentExpression`):

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

### Cancellation

A class-based rule receives an optional `CancellationToken`; `AddActionAsync` has an overload whose lambda receives one. The token reaches a rule only from an explicit `RunRules(flag, token)` or `Save(token)`; a property setter passes none. A cancelled `RunRules` throws `OperationCanceledException` and marks the object invalid with "Validation cancelled" through `MarkInvalid`; `RunRules(RunRulesFlag.All)` does not clear that (the built-in rule re-reports `ObjectInvalid`; known bug, NeatooDotNet/Neatoo#96), so cancellation is for abandoning the object, not recovering it:

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
<sup><a href='/src/Design/Design.Domain/Rules/AsyncRules.cs#L259-L281' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-cancellable-rule' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Rules Do Not Run While Paused

Properties set in a factory operation (`[Create]`, `[Fetch]`, ...) or during JSON deserialization do not trigger rules, and `ResumeAllActions()` does not run them afterward. To populate computed values after a load, call `await RunRules(RunRulesFlag.All)` at the end of the factory method. See [rules-lifecycle.md](rules-lifecycle.md).

### Exception Propagation

A rule that throws is a bug, not a validation failure. The exception is re-thrown to the caller — from the property setter when the rule throws before its first `await`, from `WaitForTasks()` otherwise — wrapped in an `AggregateException`, and the trigger property is marked invalid with the exception message:

<!-- snippet: skill-rule-that-throws -->
<a id='snippet-skill-rule-that-throws'></a>
```cs
/// <summary>
/// Demonstrates: What happens when a rule throws an exception.
/// </summary>
internal class ExceptionThrowingRule : AsyncRuleBase<ValidationFailureDemo>
{
    public ExceptionThrowingRule() : base(t => t.Name) { }

    protected override Task<IRuleMessages> Execute(ValidationFailureDemo target, CancellationToken? token = null)
    {
        // =====================================================================
        // DON'T DO THIS IN REAL CODE
        // This simulates a bug in a rule (e.g., null reference, divide by zero)
        // =====================================================================
        if (target.Name == "ThrowException")
        {
            throw new InvalidOperationException("Simulated rule bug");
        }

        return Task.FromResult<IRuleMessages>(None);
    }
}
```
<sup><a href='/src/Design/Design.Domain/ErrorHandling/ErrorPatterns.cs#L208-L230' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-rule-that-throws' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: skill-rule-exception -->
<a id='snippet-skill-rule-exception'></a>
```cs
[TestMethod]
public void RuleThatThrows_SurfacesToTheCaller_AndMarksPropertyInvalid()
{
    var entity = _factory.Create();

    // The rule throws before its first await, so the exception reaches the
    // setter's caller, wrapped in an AggregateException. A rule that throws
    // after an await surfaces the same way from WaitForTasks().
    var exception = Assert.ThrowsExactly<AggregateException>(
        () => entity.Name = "ThrowException");
    Assert.IsInstanceOfType<InvalidOperationException>(exception.InnerException);

    // A rule bug is also visible as a message on the trigger property
    Assert.IsFalse(entity["Name"].IsValid);
    Assert.IsFalse(entity.IsValid);
}
```
<sup><a href='/src/Design/Design.Tests/RuleTests/RuleExceptionTests.cs#L34-L51' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-rule-exception' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Related

- [Properties](properties.md) - How properties trigger validation
- [Entities](entities.md) - IsSavable and validation
- [Shared Rules](shared-rules.md) - One rule across several entity types
