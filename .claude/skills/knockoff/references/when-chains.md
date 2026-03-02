# When Chains Reference

When chains provide parameter-specific matching. When the method is called with matching arguments, the configured return value is used instead of the default Return/Call behavior. When chains are the **highest priority** in the resolution chain.

---

## Value Equality Matching

Match specific argument values using equality. For 2+ parameter methods, pass values as individual arguments:

<!-- snippet: when-chains-ref-value -->
```cs
stub.Add.When(1, 2).Return(100)
    .ThenWhen(5, 5).Return(500);

IWhenCalculator calc = stub;
calc.Add(1, 2);  // 100 (matched)
calc.Add(5, 5);  // 500 (matched)
calc.Add(3, 4);  // falls to Return/default
```
<!-- endSnippet -->

---

## Predicate Matching

Match using a predicate function for complex conditions. For 2+ params, the predicate uses a custom named delegate with typed parameters:

<!-- snippet: when-chains-ref-predicate -->
```cs
// Range check
stub.Add.When((int a, int b) => a > 0 && b > 0).Return(42);
```
<!-- endSnippet -->

---

## ThenWhen() Chaining

Chain multiple matchers as a logical group:

<!-- snippet: when-chains-ref-thenwhen -->
```cs
stub.Add
    .When(1, 1).Return(1)
    .ThenWhen(2, 2).Return(2)
    .ThenWhen(3, 3).Return(3);

IWhenCalculator calc = stub;
calc.Add(1, 1); // 1
calc.Add(2, 2); // 2
calc.Add(3, 3); // 3
calc.Add(4, 4); // falls to Return/default
```
<!-- endSnippet -->

Mix value and predicate matchers in the same chain:

<!-- snippet: when-chains-ref-mixed -->
```cs
stub.Transform
    .When("admin").Return(adminUser)
    .ThenWhen(s => s.Length > 10).Return(premiumUser)
    .ThenWhen(s => s.Length > 0).Return(regularUser);
```
<!-- endSnippet -->

---

## When + Return(value) Only

When chains use `Return(value)` -- there is **no** `Return(callback)` on When chains.

```csharp
// This works:
stub.Add.When(10, 10).Return(100);

// This does NOT exist:
// stub.Add.When(10, 10).Return((int a, int b) => a * b);  // No Return(callback) on When

// For dynamic behavior on all calls, use Call(callback) without When:
stub.Add.Call((int a, int b) => a * b);
```

---

## Void Methods -- Call Instead of Return

Void methods use `Call(callback)` instead of `Return(value)`:

<!-- snippet: when-chains-ref-void-call -->
```cs
stub.Process.When(1, 2).Call((int a, int b) => errors.Add($"{a},{b}"));
```
<!-- endSnippet -->

### ThenCall() Terminal Fallback

Use `.ThenCall()` as a terminal fallback for non-void When chains:

<!-- snippet: when-chains-ref-thencall -->
```cs
stub.Add
    .When(1, 2).Return(100)
    .ThenWhen(3, 4).Return(200)
    .ThenCall((int a, int b) => a + b);  // Fallback for unmatched

IWhenCalculator calc = stub;
calc.Add(1, 2); // 100
calc.Add(3, 4); // 200
calc.Add(5, 6); // 11 (fallback computes)
```
<!-- endSnippet -->

---

## Async Methods

When chains work identically with async methods. `Return(value)` auto-wraps:

<!-- snippet: when-chains-ref-async -->
```cs
stub.GetAsync.When("key1").Return("Item 1");     // Auto-wrapped in Task.FromResult
stub.GetAsync.When("key2").Return("Item 2");

IWhenAsyncDataService service = stub;
var r = await service.GetAsync("key1"); // "Item 1"
```
<!-- endSnippet -->

---

## Verification

### Verifiable() on When Chains

Mark When chains for batch verification:

<!-- snippet: when-chains-ref-verifiable -->
```cs
stub.Add.When(1, 2).Return(100).Verifiable();
stub.Add.When(5, 5).Return(500).Verifiable();
```
<!-- endSnippet -->

### When Chain Verify()

When chains have their own `Verify()` for checking consumption:

<!-- snippet: when-chains-ref-chain-verify -->
```cs
var chain = stub.Add.When(1, 2).Return(10)
    .ThenWhen(3, 4).Return(20)
    .ThenCall((int a, int b) => 999);

IWhenCalculator calc = stub;
calc.Add(1, 2);
calc.Add(3, 4);
calc.Add(0, 0);

chain.Verify(); // Passes -- all matchers consumed
```
<!-- endSnippet -->

---

## Priority in Resolution Chain

When chains are checked **first** -- before all other configuration:

1. **When chains** (highest)
2. Sequences
3. Return / Call
4. Stub overrides
5. Source delegation
6. Default value (lowest)

When a When chain is configured, Return/Call becomes the fallback for unmatched calls:

<!-- snippet: when-chains-ref-priority -->
```cs
stub.Add.Return(0);                     // Default for unmatched
stub.Add.When(1, 2).Return(100);        // Specific match

IWhenCalculator calc = stub;
calc.Add(1, 2); // 100 (When matched)
calc.Add(3, 4); // 0 (fell to Return)
```
<!-- endSnippet -->

---

## Known Bug

`When()` currently **accumulates** like `ThenWhen()` instead of replacing the chain. Calling `When()` again adds to the existing chain rather than starting a new one. See `docs/todos/when-entry-point-should-clear-chain.md`.
