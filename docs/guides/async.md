# Asynchronous Operations

[Up](index.md) | [Next: Blazor Integration](blazor.md)

Neatoo provides comprehensive async support for validation rules, business rules, and task coordination. All async operations are tracked and coordinated to maintain consistent state.

## Async Validation Rules

Validation rules can execute async operations by inheriting from `AsyncRuleBase<T>`. Async rules support cancellation tokens and the framework automatically tracks busy state during execution.

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
            target.Email ?? "",
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

The framework marks trigger properties as busy using unique execution IDs. While an async rule executes, `IsBusy` returns `true` on both the property and the entity. After completion, the same execution ID is used to clear the busy state, ensuring concurrent rules don't interfere with each other's tracking.

## Async Business Rules

Use `AddActionAsync` to create inline async business rules that execute when properties change. These rules perform side effects (computed properties, external service calls) without producing validation messages or affecting the entity's `IsValid` state. The `RuleManager.AddActionAsync` method creates an `AsyncActionFluentRule<T>` internally that executes the lambda when trigger properties change.

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

`WaitForTasks` waits for all currently executing async rules to complete. The method returns a `Task` that completes when all tracked async operations finish. This is essential before saving, serializing, or inspecting validation state to ensure consistency.

Wait for all async operations to complete:

<!-- snippet: async-wait-for-tasks -->
```cs
[Fact]
public async Task WaitForTasks_EnsuresAsyncRulesComplete()
{
    var factory = GetRequiredService<IAsyncActionContactFactory>();
    var contact = factory.Create();

    // Setting ZipCode triggers an async rule
    contact.ZipCode = "90210";

    // WaitForTasks ensures the async rule completes
    await contact.WaitForTasks();

    // Now the TaxRate is guaranteed to be set
    Assert.Equal(0.0825m, contact.TaxRate);
}
```
<!-- endSnippet -->

The framework automatically propagates task tracking up the parent hierarchy through the `Parent` property. When you call `WaitForTasks` on a parent entity, it recursively waits for all child entities and collections to complete their async operations.

## IsBusy State

Neatoo automatically tracks async operation state through the `IsBusy` property. The framework uses execution IDs to track which operations are in progress. An object is busy when:

- Async validation rules are executing (trigger properties marked busy with unique execution ID)
- Async action rules are running (trigger properties marked busy with unique execution ID)
- Any child object is busy (busy state cascades up the parent hierarchy)

Check busy state before performing operations:

<!-- snippet: async-check-busy -->
```cs
[Fact]
public async Task IsBusy_TracksAsyncOperationState()
{
    var factory = GetRequiredService<IAsyncActionContactFactory>();
    var contact = factory.Create();

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

`IsBusy` cascades through the parent hierarchy via the `Parent` property. Aggregate roots reflect the busy state of all children, ensuring the UI can disable save operations or show loading indicators for the entire aggregate.

## CancellationToken Support

All async operations accept an optional `CancellationToken` to enable cancellation. When cancellation is requested during `WaitForTasks`, the wait operation throws `OperationCanceledException`. Running async rules continue executing to completion to avoid inconsistent entity state.

Pass cancellation tokens to async operations:

<!-- snippet: async-cancellation-token -->
```cs
[Fact]
public async Task CancellationToken_CancelsWaitForTasks()
{
    var factory = GetRequiredService<IAsyncCancellableContactFactory>();
    var contact = factory.Create();

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

After catching `OperationCanceledException`, the entity may have partially completed rules. Call `RunRules(RunRulesFlag.All)` to re-execute all rules and establish consistent validation state.

## Task Coordination in Collections

`ValidateListBase` and `EntityListBase` coordinate async operations across all items in the collection. When you call `WaitForTasks` on a list, it iterates through all items and recursively waits for each item's async operations to complete.

Wait for all items in a collection:

<!-- snippet: async-list-wait-tasks -->
```cs
[Fact]
public async Task ListWaitForTasks_WaitsForAllItems()
{
    var list = new AsyncContactItemList();

    var itemFactory = GetRequiredService<IAsyncContactItemFactory>();
    var item1 = itemFactory.Create();
    var item2 = itemFactory.Create();

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

Collections report `IsBusy == true` when any child item is busy. This busy state cascades up through the parent hierarchy, allowing aggregate roots to reflect the busy state of deeply nested collections.

## RunRules with Async

`RunRules` executes validation rules and returns a `Task` that completes when all async rules finish. The method identifies which rules to execute based on the `RunRulesFlag` parameter, executes them sequentially, and waits for async rules to complete. Use this to manually trigger validation after batch property changes.

Manually run all validation rules:

<!-- snippet: async-run-rules -->
```cs
[Fact]
public async Task RunRules_ExecutesAllValidationRules()
{
    var factory = GetRequiredService<IAsyncActionContactFactory>();
    var contact = factory.Create();

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

When a property changes, the framework identifies all rules with that property as a trigger, sorts them by `RuleOrder` (ascending), then executes them sequentially. Async rules do not execute in parallel—each async rule completes before the next rule begins, even if they have the same `RuleOrder`.

Control rule execution order:

<!-- snippet: async-rule-order -->
```cs
[Fact]
public async Task RuleOrder_ControlsExecutionSequence()
{
    var factory = GetRequiredService<IAsyncOrderedRulesContactFactory>();
    var contact = factory.Create();

    contact.Value = "test";
    await contact.WaitForTasks();

    // Rules execute in registration order
    Assert.Equal(1, contact.FirstRuleExecutionOrder);
    Assert.Equal(2, contact.SecondRuleExecutionOrder);
}
```
<!-- endSnippet -->

This sequential execution ensures consistent entity state and prevents race conditions when rules modify the same properties.

## Error Handling

Exceptions thrown in async rules are captured and wrapped in an `AggregateException` when calling `WaitForTasks`. The framework collects exceptions from all rules and surfaces them together, allowing you to handle multiple failures at once.

Handle exceptions from async rules:

<!-- snippet: async-error-handling -->
```cs
[Fact]
public async Task AsyncRule_ExceptionsAreCaptured()
{
    var factory = GetRequiredService<IAsyncErrorContactFactory>();
    var contact = factory.Create();

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

When a rule throws an exception, the framework marks the trigger properties as invalid using `MarkInvalid` and stores the exception message in `PropertyMessages`. The entity's `IsValid` becomes `false` and `IsSavable` becomes `false`.

## Recursive Async Rules

Async rules can modify properties that trigger other async rules, creating a chain of cascading rule executions. The framework tracks all cascading operations and `WaitForTasks` waits for the entire chain to complete, including recursively triggered rules.

Create a rule that triggers another async rule:

<!-- snippet: async-recursive-rules -->
```cs
[Fact]
public async Task RecursiveRules_ChainedRulesExecute()
{
    var factory = GetRequiredService<IAsyncRecursiveContactFactory>();
    var contact = factory.Create();

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

Each rule in the chain executes sequentially. When rule A modifies a property that triggers rule B, rule B starts executing only after rule A completes.

## Async with PauseAllActions

Combine `PauseAllActions` with async operations to batch property changes without triggering rules. After calling `ResumeAllActions` (or disposing the pause scope), rules do not automatically execute. Call `RunRules(RunRulesFlag.All)` manually to execute rules for all properties, then wait for async operations to complete.

Batch async property changes:

<!-- snippet: async-pause-actions -->
```cs
[Fact]
public async Task PauseAllActions_BatchesPropertyChanges()
{
    var factory = GetRequiredService<IAsyncRecursiveContactFactory>();
    var contact = factory.Create();

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

This pattern avoids triggering rules multiple times during batch updates. After resume, `RunRules` executes rules once for all changed properties, and `WaitForTasks` ensures all async operations complete before proceeding.

## Save with Async Validation

When using `RemoteFactory`, the generated `SaveAsync` method automatically calls `WaitForTasks` before executing your `[Insert]` or `[Update]` method. This ensures all async validation rules complete and the entity has consistent validation state before persistence.

Save waits for async validation:

<!-- snippet: async-save-entity -->
```cs
[Fact]
public async Task Save_WaitsForAsyncValidation()
{
    // Factory resolves AsyncContact with UniqueEmailRule injected from DI
    var factory = GetRequiredService<IAsyncContactFactory>();
    var contact = factory.Create();

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

If async rules are still executing (`IsBusy == true`) or the entity is invalid (`IsValid == false`) after `WaitForTasks`, the factory does not call your persistence method. Instead, it relies on `IsSavable` to determine if persistence should proceed. Since `IsSavable == IsModified && IsValid && !IsBusy && !IsChild`, an invalid or busy entity cannot be saved.

## Performance Considerations

Async rules introduce latency. Consider these patterns for optimal performance:

- Use `PauseAllActions` during batch property updates to avoid triggering rules multiple times
- Prefer synchronous rules for simple validation—reserve async for external calls
- Avoid long-running operations in rules; offload to background services
- Use cancellation tokens for operations that may be abandoned

For high-frequency property changes, debounce validation by pausing actions and manually calling `RunRules`.

---

**UPDATED:** 2026-01-24
