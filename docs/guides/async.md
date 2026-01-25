# Asynchronous Operations

[Up](index.md) | [Next: Blazor Integration](blazor.md)

Neatoo provides comprehensive async support for validation rules, business rules, and task coordination. All async operations are tracked and coordinated to maintain consistent state.

## Async Validation Rules

Business rules can execute async operations by inheriting from `AsyncRuleBase<T>`. Async rules support cancellation tokens and track busy state automatically.

Create an async validation rule by inheriting from `AsyncRuleBase<T>` and implementing the `Execute` method:

<!-- snippet: async-validation-rule -->
```cs
public class UniqueEmailRule : AsyncRuleBase<AsyncContact>
{
    private readonly IEmailValidationService _emailService;

    public UniqueEmailRule(IEmailValidationService emailService)
        : base(c => c.Email) // Trigger when Email changes
    {
        _emailService = emailService;
    }

    protected override async Task<IRuleMessages> Execute(
        AsyncContact target,
        CancellationToken? token = null)
    {
        // Perform async validation
        var isUnique = await _emailService.IsEmailUniqueAsync(
            target.Email,
            token ?? CancellationToken.None);

        if (!isUnique)
        {
            return (nameof(AsyncContact.Email), "Email is already in use").AsRuleMessages();
        }

        return None;
    }
}
```
<!-- endSnippet -->

The framework tracks async rule execution and sets `IsBusy` to `true` while rules are running.

## Async Business Rules

Use `AddActionAsync` to create inline async business rules that execute when properties change. These rules perform side effects without producing validation messages.

Register an async action rule in your constructor:

<!-- snippet: async-action-rule -->
```cs
public AsyncActionContact(IValidateBaseServices<AsyncActionContact> services)
    : base(services)
{
    // Register an async action that fires when ZipCode changes
    RuleManager.AddActionAsync(
        async contact =>
        {
            // Simulate async lookup (e.g., tax service API call)
            await Task.Delay(10);
            contact.TaxRate = contact.ZipCode?.StartsWith("9") == true ? 0.0825m : 0.07m;
        },
        c => c.ZipCode);
}
```
<!-- endSnippet -->

For rules that need cancellation support, use the overload that accepts `CancellationToken`:

<!-- snippet: async-action-rule-with-token -->
```cs
public AsyncCancellableContact(IValidateBaseServices<AsyncCancellableContact> services)
    : base(services)
{
    // Async action with CancellationToken support
    RuleManager.AddActionAsync(
        async (contact, token) =>
        {
            // Check cancellation before expensive operation
            token.ThrowIfCancellationRequested();

            await Task.Delay(50, token); // Honors cancellation
            contact.Status = "Validated";
        },
        c => c.Email);
}
```
<!-- endSnippet -->

## WaitForTasks

`WaitForTasks` ensures all pending async operations complete before proceeding. This is essential before saving, serializing, or inspecting validation state.

Wait for all async operations to complete:

<!-- snippet: async-wait-for-tasks -->
```cs
[Fact]
public async Task WaitForTasks_EnsuresAsyncRulesComplete()
{
    var contact = new AsyncActionContact(new ValidateBaseServices<AsyncActionContact>());

    // Setting ZipCode triggers an async rule
    contact.ZipCode = "90210";

    // WaitForTasks ensures the async rule completes
    await contact.WaitForTasks();

    // Now the TaxRate is guaranteed to be set
    Assert.Equal(0.0825m, contact.TaxRate);
}
```
<!-- endSnippet -->

The framework automatically propagates tasks up the parent hierarchy, so calling `WaitForTasks` on a parent waits for all children.

## IsBusy State

Neatoo automatically tracks async operation state through the `IsBusy` property. An object is busy when:

- Async validation rules are executing
- Async business rules are running
- Property lazy-loading is in progress
- Any child object is busy

Check busy state before performing operations:

<!-- snippet: async-check-busy -->
```cs
[Fact]
public async Task IsBusy_TracksAsyncOperationState()
{
    var contact = new AsyncActionContact(new ValidateBaseServices<AsyncActionContact>());

    // Trigger async rule
    contact.ZipCode = "90210";

    // Object is busy while async rule executes
    Assert.True(contact.IsBusy);

    // Wait for completion
    await contact.WaitForTasks();

    // No longer busy
    Assert.False(contact.IsBusy);
}
```
<!-- endSnippet -->

`IsBusy` cascades to parent objects, ensuring aggregate roots reflect the busy state of all children.

## CancellationToken Support

All async operations accept an optional `CancellationToken` to enable cancellation. When cancellation is requested, validation is marked invalid and must be re-validated.

Pass cancellation tokens to async operations:

<!-- snippet: async-cancellation-token -->
```cs
[Fact]
public async Task CancellationToken_CancelsWaitForTasks()
{
    var contact = new AsyncCancellableContact(new ValidateBaseServices<AsyncCancellableContact>());

    contact.Email = "test@example.com";

    // Create a token that cancels quickly
    using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(10));

    // WaitForTasks with cancellation
    await Assert.ThrowsAsync<OperationCanceledException>(
        async () => await contact.WaitForTasks(cts.Token));

    // After cancellation, must re-run rules to clear invalid state
    await contact.RunRules(RunRulesFlag.All);
}
```
<!-- endSnippet -->

Cancellation only affects waiting—running tasks complete to avoid inconsistent state. After cancellation, call `RunRules(RunRulesFlag.All)` to clear the invalid state.

## Task Coordination in Collections

`ValidateListBase` and `EntityListBase` coordinate tasks across all items in the collection. `WaitForTasks` on a list waits for all child items to complete.

Wait for all items in a collection:

<!-- snippet: async-list-wait-tasks -->
```cs
[Fact]
public async Task ListWaitForTasks_WaitsForAllItems()
{
    var list = new AsyncContactItemList();

    var item1 = new AsyncContactItem(new EntityBaseServices<AsyncContactItem>());
    var item2 = new AsyncContactItem(new EntityBaseServices<AsyncContactItem>());

    list.Add(item1);
    list.Add(item2);

    // Trigger async operations on items
    item1.Description = "First";
    item2.Description = "Second";

    // WaitForTasks on list waits for all child items
    await list.WaitForTasks();

    // All items are no longer busy
    Assert.False(list.IsBusy);
    Assert.False(item1.IsBusy);
    Assert.False(item2.IsBusy);
}
```
<!-- endSnippet -->

Collections report `IsBusy` when any child item is busy, providing aggregate busy state.

## RunRules with Async

`RunRules` executes all validation rules, including async rules, and waits for completion. Use this to manually trigger validation after batch property changes.

Manually run all validation rules:

<!-- snippet: async-run-rules -->
```cs
[Fact]
public async Task RunRules_ExecutesAllValidationRules()
{
    var contact = new AsyncActionContact(new ValidateBaseServices<AsyncActionContact>());

    // Set properties without waiting
    contact.ZipCode = "90210";

    // RunRules executes all rules and waits for completion
    await contact.RunRules(RunRulesFlag.All);

    // Validation state is now current
    Assert.True(contact.IsValid);
    Assert.Equal(0.0825m, contact.TaxRate);
}
```
<!-- endSnippet -->

`RunRules(RunRulesFlag.All)` clears all validation messages before running rules, providing a clean validation state.

## Async Rule Execution Order

Async rules execute in the order they were registered via `RuleManager.AddRule`. Rules with lower `RuleOrder` values execute first.

Control rule execution order:

<!-- snippet: async-rule-order -->
```cs
[Fact]
public async Task RuleOrder_ControlsExecutionSequence()
{
    var contact = new AsyncOrderedRulesContact(new ValidateBaseServices<AsyncOrderedRulesContact>());

    contact.Value = "test";
    await contact.WaitForTasks();

    // Rules execute in registration order
    Assert.Equal(1, contact.FirstRuleExecutionOrder);
    Assert.Equal(2, contact.SecondRuleExecutionOrder);
}
```
<!-- endSnippet -->

Multiple async rules triggered by the same property change execute sequentially, not in parallel.

## Error Handling

Exceptions thrown in async rules are captured and surfaced when calling `WaitForTasks`. The framework aggregates exceptions from all rules.

Handle exceptions from async rules:

<!-- snippet: async-error-handling -->
```cs
[Fact]
public async Task AsyncRule_ExceptionsAreCaptured()
{
    var contact = new AsyncErrorContact(new ValidateBaseServices<AsyncErrorContact>());

    // This value causes the rule to throw
    contact.Value = "error";

    // Exception is surfaced when waiting (wrapped in AggregateException)
    var exception = await Assert.ThrowsAsync<AggregateException>(
        async () => await contact.WaitForTasks());

    // The original exception is available via InnerException
    Assert.IsType<InvalidOperationException>(exception.InnerException);

    // Property is marked invalid with exception message
    Assert.False(contact["Value"].IsValid);
}
```
<!-- endSnippet -->

When a rule throws an exception, the property is marked invalid with the exception message.

## Recursive Async Rules

Async rules can modify properties that trigger other async rules. The framework tracks all cascading rule executions.

Create a rule that triggers another async rule:

<!-- snippet: async-recursive-rules -->
```cs
[Fact]
public async Task RecursiveRules_ChainedRulesExecute()
{
    var contact = new AsyncRecursiveContact(new ValidateBaseServices<AsyncRecursiveContact>());

    // Setting FirstName triggers FullName rule
    contact.FirstName = "John";
    contact.LastName = "Doe";

    // WaitForTasks waits for the entire chain
    await contact.WaitForTasks();

    // First rule set FullName
    Assert.Equal("John Doe", contact.FullName);

    // Second rule (triggered by FullName) set Initials
    Assert.Equal("JD", contact.Initials);
}
```
<!-- endSnippet -->

`WaitForTasks` waits for the entire rule chain to complete, including recursively triggered rules.

## Async with PauseAllActions

Combine `PauseAllActions` with async operations to batch property changes and execute rules once after resuming.

Batch async property changes:

<!-- snippet: async-pause-actions -->
```cs
[Fact]
public async Task PauseAllActions_BatchesPropertyChanges()
{
    var contact = new AsyncRecursiveContact(new ValidateBaseServices<AsyncRecursiveContact>());

    // Pause to batch changes
    using (contact.PauseAllActions())
    {
        // These changes don't trigger rules yet
        contact.FirstName = "Jane";
        contact.LastName = "Smith";
    }
    // After resume, manually run rules for all properties
    await contact.RunRules(RunRulesFlag.All);

    // Wait for async rules to complete
    await contact.WaitForTasks();

    // Rules executed once for all changes
    Assert.Equal("Jane Smith", contact.FullName);
    Assert.Equal("JS", contact.Initials);
}
```
<!-- endSnippet -->

Async rules triggered during pause execute when `ResumeAllActions` is called, then you can wait for completion.

## Save with Async Validation

`EntityBase.Save` automatically calls `WaitForTasks` before persistence, ensuring all validation completes.

Save waits for async validation:

<!-- snippet: async-save-entity -->
```cs
[Fact]
public async Task Save_WaitsForAsyncValidation()
{
    var emailService = new MockEmailValidationService();
    var rule = new UniqueEmailRule(emailService);

    // Inject rule via constructor
    var contact = new AsyncContact(new EntityBaseServices<AsyncContact>(), rule);

    // Simulate Create factory operation to mark as new
    ((IFactoryOnComplete)contact).FactoryComplete(FactoryOperation.Create);

    contact.Name = "Test Contact";
    contact.Email = "valid@example.com";

    // Save automatically calls WaitForTasks before persistence
    // Here we verify the pattern by waiting first
    await contact.WaitForTasks();

    Assert.True(contact.IsValid);
    Assert.True(contact.IsSavable);
    Assert.False(contact.IsBusy);
}
```
<!-- endSnippet -->

If validation fails or the object is invalid after waiting, save throws `SaveException`.

## Performance Considerations

Async rules introduce latency. Consider these patterns for optimal performance:

- Use `PauseAllActions` during batch property updates to avoid triggering rules multiple times
- Prefer synchronous rules for simple validation—reserve async for external calls
- Avoid long-running operations in rules; offload to background services
- Use cancellation tokens for operations that may be abandoned

For high-frequency property changes, debounce validation by pausing actions and manually calling `RunRules`.

---

**UPDATED:** 2026-01-24
