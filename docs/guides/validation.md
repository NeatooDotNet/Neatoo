# Validation

[← Remote Factory](remote-factory.md) | [↑ Guides](index.md)

A data-binding UI needs to know *right now* whether the form is valid — which fields have errors, what the messages are, and whether the Save button should be enabled. ValidateBase provides this: every property tracks its own `IsValid` and error messages, and the entity's `IsValid` aggregates the entire object graph. When a user commits a field, validation fires, error messages update, and the UI reflects the new state — all through data-binding, with no manual orchestration. See [Business Rules](business-rules.md) for how to define the rules themselves; this guide covers the validation state and messaging infrastructure.

Validation is a rule. A violation is a message on a property, `IsValid` goes false, and the save is blocked while the user is still editing. Exceptions are for application failures, never for validation.

> The code samples are MSTest tests and domain classes from the Neatoo Design projects. Tests resolve factories from a DI scope (`DesignTestServices.GetScope()`); in an application you inject the factory interface into the component that needs it.

## ValidateBase Inheritance

Inherit from `ValidateBase<T>` to give a domain object rules without a persistence lifecycle — a value object, or form data that is never saved on its own. The type parameter uses the curiously recurring template pattern (CRTP) to provide strongly-typed rule registration and property access. Every Neatoo class gets a matched public interface; the concrete is `internal`, and consumers only ever see the interface:

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

ValidateBase provides:
- **IsValid**: True if all properties and child objects pass validation
- **IsSelfValid**: True if this object's properties pass validation (ignores children)
- **IsBusy**: True while an async rule is running
- **PropertyMessages**: Collection of validation error messages
- **RuleManager**: Add validation and business rules using the fluent API or rule classes
- **WaitForTasks()**: Awaits in-flight async rules
- **PauseAllActions**: Suspends rules, events and modification tracking (see below)

ValidateBase classes must have a constructor accepting `IValidateBaseServices<T>` and pass it to the base constructor. Use `ValidateBase` only when the object needs rules; a read model or DTO is a plain `[Factory]` class with a `[Fetch]` and no Neatoo base class.

## Property Declarations

Properties are declared `partial`. The BaseGenerator completes the implementation: a backing property object, change notification, and the hook that runs rules when the value is set.

<!-- snippet: skill-partial-property-class -->
<a id='snippet-skill-partial-property-class'></a>
```cs
[Factory]
internal partial class ValidationChildDemo : ValidateBase<ValidationChildDemo>, IValidationChildDemo
{
    public partial string? RequiredField { get; set; }

    public ValidationChildDemo(IValidateBaseServices<ValidationChildDemo> services) : base(services)
    {
        RuleManager.AddValidation(
            t => string.IsNullOrWhiteSpace(t.RequiredField) ? "Child field is required" : string.Empty,
            t => t.RequiredField);
    }

    [Create]
    public void Create() { }
}
```
<sup><a href='/src/Design/Design.Domain/PropertySystem/StateProperties.cs#L96-L112' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-partial-property-class' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Property characteristics:
- **Partial keyword**: Required for source generation
- **Instance properties**: The generator implements every `partial` property on a `[Factory]` class and preserves its declared accessibility
- **Getter and setter**: Both, unless the value is computed by a rule — then `private set` (see [Properties](properties.md))
- **Attributes**: DataAnnotations attributes apply validation rules automatically

See [Properties](properties.md) for details on property implementation and source generation.

## Built-In Validation Attributes

Neatoo integrates with `System.ComponentModel.DataAnnotations`. The RuleManager scans properties for validation attributes during construction and converts them to rules using the `IAttributeToRule` service. The attributes become validation rules that execute when the property is set:

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

Supported attributes:
- **[Required]**: Property value must be non-null and non-empty
- **[StringLength(max, MinimumLength = min)]**: String length constraints
- **[MinLength(n)]**: String length must be at least n characters
- **[MaxLength(n)]**: String length cannot exceed n characters
- **[RegularExpression(pattern)]**: String must match regex pattern
- **[Range(min, max)]**: Numeric value must be within range
- **[EmailAddress]**: String must be valid email format

Other DataAnnotations attributes (`[Phone]`, `[Url]`, ...) are not mapped and are silently ignored; write a rule instead. Attribute rules execute when the property changes. Messages appear in `PropertyMessages` and `IsValid` reflects the validation state.

## Custom Validation Rules

An inline rule is registered in the constructor with `RuleManager.AddValidation`: the lambda receives the object and returns an error message or an empty string, and the message is attached to the trigger property. The `DemoInputModel` and `ValidationChildDemo` classes above show the shape.

A rule with more than a line or two of logic is a class. A synchronous rule derives from `RuleBase<T>`, takes its trigger properties in the base constructor, and returns `IRuleMessages`:

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

A rule class with no dependencies is constructed in the entity's constructor; a rule that needs a dependency comes from DI (see "Async Validation Rules"):

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

Validation rule patterns:
- **Lambda or class**: `AddValidation` for a one-liner, `RuleBase<T>` when the logic grows or needs a dependency
- **Trigger property**: The rule runs when a trigger property is set; `AddValidation` takes exactly one
- **Error message**: `(propertyName, message).AsRuleMessages()` or a non-empty string indicates failure; `None` or an empty string indicates success
- **Automatic association**: Messages land on the named property

The RuleManager assigns each rule a stable id (from the source text of the `AddValidation`/`AddRule` argument) and runs the rules for a property in `RuleOrder`.

## Cross-Property Validation

`AddValidation` takes exactly one trigger property, because the message attaches to that property. A constraint over several properties — "end date must be after start date" — is either a `RuleBase<T>` with several triggers that attaches the message where it belongs, or a validation on a computed property that an `AddAction` recomputes from every input:

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

Cross-property patterns:
- **Multiple trigger properties**: A rule class takes any number of triggers in its base constructor
- **Dependency tracking**: The rule executes when ANY trigger property changes
- **Chaining**: A rule that sets a property triggers the rules on that property, so the computed-property form needs no extra wiring
- **Execution order**: Rules on one property execute by `RuleOrder` (lower first, default 1)

See [Business Rules](business-rules.md) for rule class implementation details.

## Async Validation Rules

Many validations cannot be checked locally — email uniqueness requires a database query, inventory availability needs a service call. These calls are async to keep the UI responsive, and because Neatoo assumes a data-binding UI the result flows back on its own: when the async rule completes, `IsValid` and `PropertyMessages` update and the UI reflects the new state.

**A rule takes a command, never a server-only service.** Rule code and entity constructors run in the browser too. A rule or an entity constructor that takes a repository, an Entity Framework service, or any other server-only type breaks construction on the client. The rule takes the delegate of a `[Remote, Execute]` command instead: in the browser the delegate crosses to the server; on the server the same delegate calls the method directly.

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

The rule takes the command delegate and never sees the repository. It has a DI interface so the entity can take it from DI and tests can substitute it:

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

Async validation behavior:
- **IsBusy tracking**: RuleManager marks the trigger properties busy until the rule completes; the object's `IsBusy` aggregates them
- **WaitForTasks()**: Await pending rules before reading `IsValid` or saving
- **Parent cascade**: Child tasks propagate up so the aggregate root's `IsBusy` and `WaitForTasks()` cover the whole graph
- **Trigger on commit**: A rule runs every time its trigger property is set and contains no debouncing. MudNeatoo text and numeric fields commit on blur, so a rule behind them makes one server call per committed value

When an async rule executes, RuleManager marks all trigger properties as busy using a unique execution id and clears it with the same id when the rule completes, so concurrent rules do not interfere with each other's tracking.

## Forcing a Re-run: RunRules

Rules run on their own when a property is set outside a factory operation. `RunRules` is for forcing a re-run. The common case: a `[Create]` or `[Fetch]` set properties while the object was paused, so no rule has evaluated them yet — the object reports valid until something asks:

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

RunRules overloads:
- **RunRules(propertyName)**: Execute the rules triggered by one property
- **RunRules(RunRulesFlag.All)**: Clear all messages and run every rule (this object and children)
- **RunRules(RunRulesFlag.Self)**: Run only this object's rules (not children)
- **RunRules(flag, token)**: Run with cancellation token support

`RunRulesFlag.All` clears existing validation messages before running rules. `RunRules` has no `IsPaused` guard, so a factory method can call it at the end to populate computed values. It is not the step between setting a property and reading `IsValid` — the setter already ran the rules; when async rules may be in flight, `await WaitForTasks()` instead.

## Error Messages and Property-Level State

Each property is backed by its own property object, reached through the indexer. It carries `IsValid`, `PropertyMessages`, `IsBusy` and `IsReadOnly`; the object's `PropertyMessages` aggregates every property's messages. This is what a field-level UI binds:

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

Message metadata:
- **Property**: The `IValidateProperty` that failed validation (`Property.Name` for the name)
- **Message**: The error message text
- **PropertyMessages**: Collection of all messages across the object
- **Property.PropertyMessages**: Messages specific to one property

Property validation metadata:
- **IsValid**: True if the property (and a child object it holds) is valid
- **IsSelfValid**: True if the property itself is valid, ignoring a child object's validation
- **PropertyMessages**: Messages for this property
- **IsBusy**: True while an async rule is running for this property
- **Task**: The pending rule task (`Task.CompletedTask` when not busy)

## Meta-Properties and IsBusy

`IsValid`, `IsSelfValid`, `IsBusy` and `PropertyMessages` aggregate the property objects. While an async rule runs, the property and the object are busy and `IsSavable` is false; `WaitForTasks()` awaits the pending rules:

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

Meta-property definitions:
- **IsValid**: True if this object and ALL child objects pass validation
- **IsSelfValid**: True if this object's properties pass validation (ignores children)
- **IsBusy**: True if any async rule is running
- **PropertyMessages**: All validation messages (this object + children)
- **IsSavable**: (`IEntityRoot` only) `(IsModified || IsNew) && IsValid && !IsBusy` — a created entity is savable without being modified. Not available on `IEntityBase` (child entity interface) or entity lists

Meta-properties raise `PropertyChanged` when their values change; a page that subscribes re-renders its save button and validation indicators from them.

## Object-Level Validation

Application code does not mark a whole object invalid. A failure that spans several properties is a rule triggered on all of them (see Cross-Property Validation above); a failure that comes back from outside the object, such as a payment gateway rejecting a transaction, is an exception, not a validation message.

`ValidateBase` has a protected `MarkInvalid(message)` and an `ObjectInvalid` property, which a built-in rule reports as a property message. The framework uses them itself when a `RunRules` call is cancelled (see Cancellation Token Support below). They are not an application validation channel.

## PauseAllActions for Batching

`PauseAllActions()` returns an `IDisposable`. While paused, property setters run no rules, raise no `PropertyChanged`, and do not mark the object modified:

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

PauseAllActions behavior:
- **IsPaused = true**: Validation rules do NOT execute
- **No events**: `PropertyChanged` and `NeatooPropertyChanged` are not raised; nothing is queued
- **No modification tracking**: on an entity, a property set while paused is not marked modified
- **Automatic resume**: Disposing the returned `IDisposable` calls `ResumeAllActions`
- **Nothing replays on resume**: No catch-up events fire and no rules run for the paused changes; `ResumeAllActions` only recalculates cached validity. Call `RunRules(RunRulesFlag.All)` if computed values are needed

Never use it inside a factory operation: `[Create]`, `[Fetch]`, `[Insert]`, `[Update]` and `[Delete]` already run paused, and disposing the `using` resumes the object early. Deserialization pauses the object on its own. On a fetched entity, edits made under `PauseAllActions()` leave `IsModified` false and are not saved.

## Validation and Change Tracking Integration

Validation integrates with change tracking. Rules run when a property is set as a user edit (`ChangeReason.UserEdit`), not when a value is loaded (`ChangeReason.Load`). Inside a factory operation the object is paused, so plain assignment is the baseline load — no rules, nothing marked modified. `LoadValue()` on the property object is the same kind of load from outside a factory operation; the framework uses it (deserialization, the generated `EntityLazyLoad` setter), and it is not needed in a `[Fetch]`:

<!-- snippet: skill-load-value-outside-operation -->
<a id='snippet-skill-load-value-outside-operation'></a>
```cs
[TestMethod]
public void LoadValue_DoesNotMarkPropertyModified()
{
    // Arrange
    var entity = _factory.Create();

    // Act
    entity["Name"].LoadValue("Loaded");

    // Assert
    Assert.IsFalse(entity["Name"].IsModified, "Property should not be marked modified via LoadValue");
}
```
<sup><a href='/src/Design/Design.Tests/PropertyTests/StatePropertyTests.cs#L46-L59' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-load-value-outside-operation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

ChangeReason integration:
- **ChangeReason.UserEdit**: Normal property assignment, triggers validation
- **ChangeReason.Load**: `LoadValue` assignment, skips validation and modification tracking
- **Factory operations**: The factory pauses the object; assign properties directly
- **Deserialization**: The framework pauses the object; rules do not run

See [Properties](properties.md) for details on ChangeReason and LoadValue.

## Validation Cascade

Validation state cascades from child objects to parents. When a child becomes invalid, the parent's `IsValid` becomes false and the child's message reaches the parent's `PropertyMessages`; the parent's `IsSelfValid` ignores children:

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

Cascade characteristics:
- **Parent IsValid**: False if any child is invalid
- **Parent IsSelfValid**: Only reflects parent's own properties, ignores children
- **Automatic propagation**: Child validation changes update parent immediately
- **Parent-child structure**: Established when a child is assigned to a parent's property or added to a child list (SetParent)
- **NeatooPropertyChanged**: Bubbles up the parent chain with validation state changes

See [Parent-Child](parent-child.md) for details on parent-child relationship establishment and cascade behavior.

## Validation During Save

`IsSavable` (on `IEntityRoot`, the aggregate root interface) is `(IsModified || IsNew) && IsValid && !IsBusy`. An invalid entity is not savable, and `entity.Save()` throws `SaveOperationException` when `IsSavable` is false — reaching that exception is a programming error, because the UI binds the Save button to `IsSavable`:

<!-- snippet: skill-invalid-not-savable -->
<a id='snippet-skill-invalid-not-savable'></a>
```cs
[TestMethod]
public async Task NewEntity_NotSavableWhenInvalid()
{
    // Arrange
    var entity = _factory.Create();
    entity.Name = "Valid First";
    Assert.IsTrue(entity.IsValid);

    // Act - Make it invalid
    entity.Name = null;
    await entity.WaitForTasks();

    // Assert
    Assert.IsTrue(entity.IsNew);
    Assert.IsFalse(entity.IsValid);
    Assert.IsFalse(entity.IsSavable, "Invalid entity should not be savable");
}
```
<sup><a href='/src/Design/Design.Tests/FactoryTests/SaveTests.cs#L49-L67' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-invalid-not-savable' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The client's `IsSavable` is the client's view. The root's `[Insert]` and `[Update]` re-run every rule on the server and refuse an invalid aggregate before writing — recommended, because the framework does not do this for you and a direct `factory.Save(target)` does not check `IsSavable`. Throw, never return: after the method returns, the framework marks the entity saved whether or not anything was written.

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

Save validation patterns:
- **Bind the UI to IsSavable**: The Save button is disabled while the entity is invalid, busy, or has nothing to save
- **Await WaitForTasks()**: Ensure async validation completes before reading `IsValid` or saving; `Save(token)` waits itself
- **Server gate**: `RunRules(RunRulesFlag.All)` then `throw new SaveOperationException(SaveFailureReason.IsInvalid)` in the root's `[Insert]`/`[Update]`
- **Save() throws**: `SaveOperationException` with a `Reason` when the entity is not savable; it never returns null

## Cancellation Token Support

If a user navigates away from a form, there is no point finishing a database uniqueness check for an abandoned page. A class-based rule receives an optional `CancellationToken`; a cancelled rule throws `OperationCanceledException`, the object is marked invalid with "Validation cancelled", and `RunRules(RunRulesFlag.All)` re-validates:

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

Cancellation behavior:
- **Token source**: The token reaches a rule only from an explicit `RunRules(flag, token)` or `Save(token)` call; a property setter passes none
- **Field commit, not keystroke**: MudNeatoo text and numeric fields set the property on blur, so a rule runs once per committed value — cancellation is for abandoned pages, not superseded keystrokes
- **OperationCanceledException**: Thrown when cancellation occurs
- **MarkInvalid("Validation cancelled")**: Object marked invalid when a `RunRules` is cancelled; a cancelled `WaitForTasks(token)` only stops the wait
- **Recovery**: `RunRules(RunRulesFlag.All)` re-runs the rules but does not clear `ObjectInvalid` — a known bug ([#96](https://github.com/NeatooDotNet/Neatoo/issues/96)) — so a cancelled `RunRules` leaves the object invalid. Until it is fixed, cancellation is for abandoning the object, not for recovering it

## Validation Rule Execution Order

Rules execute based on their trigger property matching and `RuleOrder` (lower values execute first, default is 1; registration order within the same value). When a property changes, the RuleManager identifies all rules with matching trigger properties and executes them sorted by `RuleOrder`.

Rule execution flow:
1. Property value changes (via setter)
2. `RuleManager.RunRules(propertyName)` is called
3. Rules with trigger properties matching propertyName are selected
4. Selected rules are sorted by `RuleOrder` (ascending)
5. Each rule executes sequentially (even async rules wait for the previous rule to complete)
6. Rule messages are applied to properties via `SetMessagesForRule`
7. `IsValid` and `IsSelfValid` recalculate based on `PropertyMessages`
8. `PropertyChanged` events fire for meta-properties
9. Validation state cascades to parent via `NeatooPropertyChanged`

Synchronous rules (`AddValidation`, `RuleBase`) complete immediately. Async rules (`AddValidationAsync`, `AsyncRuleBase`) mark properties as `IsBusy` during execution and complete when the Task resolves. A rule that throws is an application failure, not a validation result: the exception is rethrown to the caller and the exception message is added to each trigger property.

## Several Messages From One Rule

One rule can report several failures at once with the `RuleMessages.If` builder, so every invalid field shows its error at the same time instead of one at a time:

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

Message collection operations:
- **PropertyMessages**: Read-only collection of all messages
- **Filter by property**: `messages.Where(m => m.Property.Name == "Name")`
- **Clear messages**: `ClearAllMessages()` or `ClearSelfMessages()`; `RunRules(RunRulesFlag.All)` clears and repopulates
- **Object-level messages**: `messages.Where(m => m.Property.Name == "ObjectInvalid")`

`PropertyMessages` updates automatically as validation rules execute and properties change.

## Combining Attributes and Custom Rules

DataAnnotations attributes and custom rules work together. Attributes provide standard validation (required, length, format), while rules implement business logic and cross-property constraints:

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

Combined validation patterns:
- **Attributes for standard constraints**: Required, StringLength, RegularExpression, Range
- **Rules for business logic**: Cross-property validation, business invariants
- **Async rules for server truth**: Uniqueness and lookups, through an injected `[Remote, Execute]` command
- **All rules execute**: Both attribute and custom rules run on property changes
- **Multiple error messages**: One property can have multiple validation failures

Layer validation from simple (attributes) to complex (rules) to build comprehensive validation.

---

**UPDATED:** 2026-10-06
