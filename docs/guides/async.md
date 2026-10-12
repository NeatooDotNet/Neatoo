# Asynchronous Operations

[Up](index.md) | [Next: Blazor Integration](blazor.md)

Neatoo provides async support for validation rules, action rules, and task coordination. Every async operation is tracked, so `IsBusy` and `WaitForTasks()` always describe the whole object graph.

The compiled examples on this page come from the Design.Domain reference set (`AsyncRulesDemo`, `BusyStateDemo`, `TriggerPatternsDemo`, `ValidationFailureDemo`, the `Order` aggregate). The tests are MSTest and resolve factories from a DI scope (`DesignTestServices.GetScope()`).

## Async Validation Rules

A validation rule that needs the server — a uniqueness check, an existence check — inherits from `AsyncRuleBase<T>`. It runs on the client, where the user is editing, so it cannot take a repository or any other server-only service. It takes the delegate of a `[Remote, Execute]` command: calling the delegate crosses to the server, where the command resolves the repository.

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
<sup><a href='/src/Design/Design.Domain/Rules/AsyncRules.cs#L126-L140' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-rule-command' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The rule, with a DI interface so the entity can take it from DI and tests can substitute it:

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

The entity receives the rule by constructor injection; the rule is registered in DI on both tiers (see the Business Rules guide):

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

While the rule executes, `IsBusy` is `true` on the trigger property and on the entity. The framework tracks each execution with its own id, so concurrent rules do not clear each other's busy state. A rule runs on every set of its trigger property — bind the property so it is set on field commit (`MudNeatooTextField` and `MudNeatooNumericField` commit on blur), and one committed value means one server call.

## Async Business Rules

`AddActionAsync` creates an inline async action rule (`ActionAsyncFluentRule<T>`) that performs side effects — a computed value, a lookup — without producing validation messages or affecting `IsValid`:

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

An overload takes `(target, token)` for actions that honor a `CancellationToken`. The same dependency rule applies as for validation: server data comes through an injected command delegate.

## WaitForTasks

`WaitForTasks()` returns a `Task` that completes when every tracked async operation on the object and its descendants has finished. It is the step before saving, serializing, or reading validation state: `IsValid` and `IsSavable` reflect only the rules that have completed.

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

Task tracking propagates up the graph through `Parent`. `WaitForTasks()` on an aggregate root waits for every child entity and collection as well.

## IsBusy State

`IsBusy` is `true` while any async rule is executing on the object, and it cascades: a parent is busy while any child is busy, and a list is busy while any item is busy. An aggregate root therefore reflects the busy state of the whole aggregate, which is what lets a UI disable Save (`IsSavable` includes `!IsBusy`) or show a spinner for the graph as a whole. The test above shows the transition: busy right after the set, not busy after `WaitForTasks()`.

## CancellationToken Support

`WaitForTasks(token)`, `RunRules(flag, token)` and `Save(token)` accept a `CancellationToken`. Cancelling a wait throws `OperationCanceledException` and stops only the wait — the running rules complete, and the object's state is left as those rules leave it. Cancelling `RunRules` is different: the object is marked invalid with "Validation cancelled", and `RunRules(RunRulesFlag.All)` re-validates.

A rule receives the token only from an explicit `RunRules(..., token)`; a setter-triggered run passes none. A long-running rule checks it:

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

`Save(token)` waits for in-flight rules before routing to `[Insert]`/`[Update]`, so cancelling it before the rules finish throws without touching persistence:

<!-- snippet: skill-save-cancellation -->
<a id='snippet-skill-save-cancellation'></a>
```cs
[TestMethod]
public async Task Save_WithCancelledToken_ThrowsAndLeavesStateUnchanged()
{
    var entity = _factory.Create();
    entity.Name = "Pending";

    using var cts = new CancellationTokenSource();
    await cts.CancelAsync();

    // Save checks the token before any persistence
    await Assert.ThrowsAsync<OperationCanceledException>(() => entity.Save(cts.Token));

    Assert.AreEqual(0, _repository.InsertedIds.Count, "Nothing was written");
    Assert.IsTrue(entity.IsNew, "State is unchanged");
    Assert.IsTrue(entity.IsModified);
}
```
<sup><a href='/src/Design/Design.Tests/FactoryTests/SaveTests.cs#L187-L204' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-save-cancellation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Task Coordination in Collections

`ValidateListBase<I>` and `EntityListBase<I>` coordinate async operations across their items. `WaitForTasks()` on a list waits for every item; a list's `IsBusy` is `true` while any item is busy, and that state cascades to the list's parent. Nothing is needed on the list itself: because `IsBusy` and `WaitForTasks()` aggregate through `Parent`, calling them on the aggregate root covers every nested collection.

## RunRules with Async

`RunRules` forces a re-run of rules and returns a `Task` that completes when the async ones finish. It is not the routine step between setting a property and reading `IsValid` — the setter already ran the rules; `WaitForTasks()` is what you await. `RunRules` is for the cases where nothing ran: after a factory operation (the object was paused), or to re-validate after a cancelled `RunRules`.

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

`RunRules(RunRulesFlag.All)` clears all validation messages before running, giving a clean validation state.

## Async Rule Execution Order

When a property changes, the framework identifies the rules with that property as a trigger, sorts them by `RuleOrder` (ascending; default 1), then executes them one after another — each async rule completes before the next begins, even with the same `RuleOrder`. Within the same `RuleOrder`, registration order applies. Rules triggered by *different* property changes can overlap.

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

Sequential execution per trigger keeps entity state consistent when several rules write the same properties.

## Error Handling

A rule that throws is a bug, not a validation result. The framework adds the exception message to each trigger property's messages (so the property and the entity read `IsValid == false` and `IsSavable == false`) and re-throws: from the property setter, wrapped in an `AggregateException`, when the rule throws before its first `await`; from `WaitForTasks()` otherwise.

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

Catch it where application failures are handled — an error boundary or a logger — not as a validation channel. Validation is a rule returning messages.

## Recursive Async Rules

A rule can set a property that triggers other rules, forming a chain. The framework tracks every cascading execution, and `WaitForTasks()` waits for the whole chain:

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

Each rule in the chain executes after the one that triggered it completes.

## Async with PauseAllActions

`PauseAllActions()` pauses an object outside a factory operation. While paused, setters run no rules and raise no `PropertyChanged`; disposing the scope (`ResumeAllActions`) recalculates cached validity but does not run the skipped rules and replays no events. Call `RunRules(RunRulesFlag.All)` afterward if the computed values are needed, then `WaitForTasks()`:

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

On an entity, a property set while paused is not marked modified, so edits made under `PauseAllActions()` on a fetched entity are not saved. Never use it inside a factory operation: the operation is already paused, and disposing the `using` resumes the object early.

## Save with Async Validation

`entity.Save()` on an aggregate root checks `IsSavable` — `(IsModified || IsNew) && IsValid && !IsBusy` — and throws `SaveOperationException` when it is false; the generated factory `Save` does not wait for rules or check savability itself. `Save(token)` waits for in-flight rules first. The caller's routine is therefore `await WaitForTasks()`, then save; a UI binds the Save button to `IsSavable` so the exception is never reached:

<!-- snippet: skill-is-savable -->
<a id='snippet-skill-is-savable'></a>
```cs
[TestMethod]
public async Task FetchedEntity_NotSavableWhenUnmodified()
{
    // Arrange
    var entity = await _factory.Fetch(1);

    // Assert
    Assert.IsFalse(entity.IsNew);
    Assert.IsFalse(entity.IsModified);
    Assert.IsFalse(entity.IsSavable, "Unmodified fetched entity should not be savable");
}

[TestMethod]
public async Task FetchedEntity_IsSavableWhenModified()
{
    // Arrange
    var entity = await _factory.Fetch(1);

    // Act
    entity.Name = "Updated Name";

    // Assert
    Assert.IsFalse(entity.IsNew);
    Assert.IsTrue(entity.IsModified);
    Assert.IsTrue(entity.IsValid);
    Assert.IsTrue(entity.IsSavable, "Modified valid entity should be savable");
}
```
<sup><a href='/src/Design/Design.Tests/FactoryTests/SaveTests.cs#L69-L97' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-is-savable' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

On the server, the root's `[Insert]`/`[Update]` re-run every rule and refuse an invalid aggregate (`RunRules(RunRulesFlag.All)` then `throw new SaveOperationException(SaveFailureReason.IsInvalid)`) — see the Business Rules guide, "Manual Rule Execution".

## Performance Considerations

Async rules introduce latency:

- Prefer synchronous rules for simple validation; reserve async for server calls through a command delegate.
- Bind server-calling rules to fields that commit on blur rather than per keystroke; rules contain no debouncing, and a rapid sequence of sets starts a rule per set (the last one to complete sets the final state).
- Use `WaitForTasks()` before reading state or saving, and `Save(token)` where a wait may be abandoned.

---

**UPDATED:** 2026-10-06
