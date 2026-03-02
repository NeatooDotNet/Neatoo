# Async Methods Reference

KnockOff provides three-tier auto-wrapping for async methods returning `Task<T>` or `ValueTask<T>`. You configure with unwrapped values and KnockOff wraps them automatically.

---

## Three-Tier Auto-Wrapping

For an async method like `Task<string> FetchAsync(int id)`:

### Tier 1: Value -- Auto-Wraps

<!-- snippet: async-tier1-value -->
```cs
stub.FetchAsync.Return("value");
// Internally: Task.FromResult("value")

IAsyncFetchSvc service = stub;
var result = await service.FetchAsync(1); // "value"
```
<!-- endSnippet -->

### Tier 2: Simplified Callback -- Auto-Wraps

<!-- snippet: async-tier2-callback -->
```cs
stub.FetchAsync.Call((id) => $"Fetch-{id}");
// Internally: Task.FromResult(callback(id))

IAsyncFetchSvc service = stub;
var result = await service.FetchAsync(42); // "Fetch-42"
```
<!-- endSnippet -->

### Tier 3: Full Callback -- Direct

<!-- snippet: async-tier3-full -->
```cs
stub.FetchAsync.Call((int id) => Task.FromResult($"Full-{id}"));
// Used as-is -- for custom async behavior

IAsyncFetchSvc service = stub;
var result = await service.FetchAsync(99); // "Full-99"
```
<!-- endSnippet -->

**Rule of thumb:** Use Tier 1 or 2 for simple returns. Use Tier 3 when you need actual async behavior (delays, cancellation tokens, etc.).

---

## Void Async Methods (Task, ValueTask)

Methods returning `Task` or `ValueTask` (no result) use `Return()` with an `Action`:

<!-- snippet: async-void-method -->
```cs
stub.ExecuteAsync.Call((command) => { /* side effect */ });

IAsyncFetchSvc service = stub;
await service.ExecuteAsync("test"); // Callback invoked
```
<!-- endSnippet -->

Unconfigured void async methods return `Task.CompletedTask` or `default(ValueTask)`.

---

## Sequences with Auto-Wrapping

Params values auto-wrap for async methods:

<!-- snippet: async-sequences-autowrap -->
```cs
stub.GetDataAsync.Return("first", "second", "third");

IAsyncFetchSvc service = stub;
var r1 = await service.GetDataAsync(1); // "first"
var r2 = await service.GetDataAsync(2); // "second"
var r3 = await service.GetDataAsync(3); // "third"
var r4 = await service.GetDataAsync(4); // "third" (repeats)
```
<!-- endSnippet -->

Callback sequences also work:

<!-- snippet: async-callback-sequences -->
```cs
stub.FetchAsync.Call((id) => $"First-{id}")
    .ThenReturn((id) => Task.FromResult($"Second-{id}"))
    .ThenReturn("constant");
```
<!-- endSnippet -->

---

## When Chains with Auto-Wrapping

When chain `Return(value)` auto-wraps for async methods:

<!-- snippet: async-when-chains -->
```cs
stub.GetDataAsync.When(1).Return("Item 1");
stub.GetDataAsync.When(2).Return("Item 2");
stub.GetDataAsync.When((id) => id > 100).Return("Bulk item");

IAsyncFetchSvc service = stub;
var r = await service.GetDataAsync(1); // "Item 1"
```
<!-- endSnippet -->

---

## Async Delegates

Async delegates (e.g., `delegate Task<int> AsyncOperation(int x)`) support the same three-tier pattern:

<!-- snippet: async-delegate-tiers -->
```cs
var stub = new Stubs.AsyncOp();

// Tier 1: auto-wraps int -> Task<int>
stub.Interceptor.Return(42);

AsyncOp op = stub;
var result = await op(10); // 42
```
<!-- endSnippet -->

Sequences on async delegates also auto-wrap:

<!-- snippet: async-delegate-sequences -->
```cs
var stub = new Stubs.AsyncOp();
stub.Interceptor.Return(10, 20);

AsyncOp op = stub;
var r1 = await op(0); // 10
var r2 = await op(0); // 20
var r3 = await op(0); // 20 (repeats)
```
<!-- endSnippet -->

---

## All 9 Patterns

Async auto-wrapping works identically across all 9 patterns:

| Pattern | Access |
|---------|--------|
| 1. Standalone | `stub.FetchAsync.Return("value")` |
| 2. Generic Standalone | `stub.GetByIdAsync.Return("value")` |
| 3. Standalone Class | `stub.FetchAsync.Return("value")` -> `stub.Object` |
| 4. Generic Standalone Class | `stub.GetByIdAsync.Return("value")` -> `stub.Object` |
| 5. Inline Interface | `stub.FetchAsync.Return("value")` |
| 6. Inline Class | `stub.FetchAsync.Return("value")` -> `stub.Object` |
| 7. Inline Delegate | `stub.Interceptor.Return(42)` |
| 8. Open Generic Interface | `stub.GetByIdAsync.Return("value")` |
| 9. Open Generic Class | `stub.GetByIdAsync.Return("value")` -> `stub.Object` |

---

## Verification

Async methods verify the same way as sync methods:

<!-- snippet: async-verification -->
```cs
await service.FetchAsync(1);
await service.FetchAsync(2);

stub.FetchAsync.Verify(Called.Exactly(2));
Assert.Equal(2, stub.FetchAsync.LastArg); // last argument
```
<!-- endSnippet -->

---

## Async Stub Overrides

Standalone stubs can define async stub overrides:

<!-- snippet: async-stub-overrides-define -->
```cs
public partial class AsyncOverrideDemoStub
{
    protected override async Task<string> ProcessAsync_(string input)
    {
        await Task.Delay(1);
        return $"[Async: {input}]";
    }

    protected override async ValueTask<int> ComputeAsync_(int value)
    {
        await Task.Yield();
        return value * 2;
    }
}
```
<!-- endSnippet -->

`Return()` supersedes async stub overrides per-test, same as sync methods.

---

## Quick Reference

| Task | Code |
|------|------|
| Return value (auto-wrap) | `stub.Method.Return("value")` |
| Simplified callback (auto-wrap) | `stub.Method.Call((args) => result)` |
| Full async callback | `stub.Method.Call((args) => Task.FromResult(result))` |
| Void async callback | `stub.Method.Call((args) => { })` |
| Sequence (auto-wrap) | `stub.Method.Return("a", "b", "c")` |
| When chain (auto-wrap) | `stub.Method.When(arg).Return("value")` |
| Verify | `stub.Method.Verify(Called.Once)` |
