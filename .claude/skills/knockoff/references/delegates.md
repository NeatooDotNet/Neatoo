# Delegate Stub Reference

Delegate stubs are created with `[KnockOff<DelegateType>]`. They generate a stub class with an `Interceptor` property for configuration and verification. The stub implicitly converts to the delegate type.

---

## Named Delegates Only

KnockOff requires **named delegate types**. `Func<>` and `Action<>` are NOT supported.

<!-- snippet: delegate-func-action-not-supported -->
```cs
// Does NOT work:
// [KnockOff<Func<int, int, int>>]  // Not supported

// Define a named delegate instead:
public delegate int NamedCalculation(int a, int b);

[KnockOff<NamedCalculation>]  // Works!
public partial class NamedDelegateExample { }
```
<!-- endSnippet -->

---

## Basic Usage

<!-- snippet: delegate-stub-basic-void -->
```cs
// Create stub, convert to delegate, invoke, and verify
var stub = new BasicVoidDelegateTest.Stubs.OnComplete();
OnComplete callback = stub;
callback();
stub.Interceptor.Verify();
```
<!-- endSnippet -->

**The stub must be converted to the delegate type before invocation.** The Interceptor is for configuration, not direct invocation.

---

## Configuration

### Return(value) -- Constant Value

<!-- snippet: delegate-stub-oncall-value -->
```cs
// Return() - pass the return value directly (simpler syntax)
stub.Interceptor.Return("FORMATTED");
```
<!-- endSnippet -->

### Call(callback) -- Dynamic Behavior

<!-- snippet: delegate-stub-oncall-return -->
```cs
// Return() - compute return value based on input
stub.Interceptor.Call((input) => input.ToUpperInvariant());
```
<!-- endSnippet -->

### Call(callback) -- Void Delegates

<!-- snippet: delegate-stub-oncall-void -->
```cs
// Configure side effects for void delegate
stub.Interceptor.Call(() => notified = true);
```
<!-- endSnippet -->

---

## Sequences

<!-- snippet: delegate-sequences -->
```cs
// Return different values on successive calls
stub.Interceptor.Return(10, 20, 30);
// Call 1: 10, Call 2: 20, Call 3+: 30 (repeats last)
```
<!-- endSnippet -->

Callback sequences:

<!-- snippet: delegate-sequences-callback -->
```cs
// Callback sequences
stub.Interceptor
    .Call((x) => x * 1)
    .ThenReturn((x) => x * 2)
    .ThenReturn((x) => x * 3);
```
<!-- endSnippet -->

---

## When Chains

### Value Matching

<!-- snippet: delegate-when-value-matching -->
```cs
// Match specific argument values
stub.Interceptor.When(1, 2).Return(100)
    .ThenWhen(3, 4).Return(200)
    .ThenCall((a, b) => a + b);  // terminal fallback
```
<!-- endSnippet -->

### Predicate Matching (Void Delegates)

<!-- snippet: delegate-when-void-chains -->
```cs
stub.Interceptor
    .When(1, 2).Call((a, b) => calls.Add("first"))
    .ThenWhen(3, 4).Call((a, b) => calls.Add("second"));
```
<!-- endSnippet -->

---

## Argument Tracking

| Delegate Params | Property | Type |
|----------------|----------|------|
| Single parameter | `LastArg` | `T` |
| Multiple parameters | `LastArgs` | Named tuple `(T1 a, T2 b)` |
| No parameters | -- | No tracking property |

<!-- snippet: delegate-stub-lastcallargs -->
```cs
// LastArgs provides named tuple access
Assert.Equal("Bob", stub.Interceptor.LastArgs!.Value.name);
Assert.Equal(25, stub.Interceptor.LastArgs!.Value.age);
```
<!-- endSnippet -->

---

## Verification

<!-- snippet: delegate-stub-verification-times -->
```cs
// Verify with Times constraints
stub.Interceptor.Verify(Called.Exactly(3));
stub.Interceptor.Verify(Called.AtLeast(2));
stub.Interceptor.Verify(Called.AtMost(5));
```
<!-- endSnippet -->

---

## Generic Delegates

Closed generic delegates use the **simple name** in the Stubs namespace:

<!-- snippet: delegate-stub-closed-generic -->
```cs
// Closed generic: type arguments specified at stub definition
var stub = new DelegateStubTests.Stubs.Factory();
stub.Interceptor.Call(() => "generated value");
Factory<string> factory = stub;
```
<!-- endSnippet -->

---

## Async Delegates

Async delegates (`Task<T>`, `ValueTask<T>`) support auto-wrapping:

<!-- snippet: delegate-async-auto-wrapping -->
```cs
// Tier 1: Returns takes inner type - auto-wraps in Task.FromResult
stub.Interceptor.Return(42);
```
<!-- endSnippet -->

---

## Strict Mode

<!-- snippet: delegate-strict-mode -->
```cs
var stub = new DelegateStubTests.Stubs.Calculate();
stub.Strict = true;

Calculate calc = stub;
Assert.Throws<StubException>(() => calc(1, 2)); // Throws StubException.NotConfigured
```
<!-- endSnippet -->

---

## Reset

`Reset()` clears:
- Call counts
- `LastArg` / `LastArgs`
- Sequence index, When chain position

`Reset()` preserves:
- `Return` / `Call` callbacks
- Sequence structure, When chain structure
- Verifiable marking

<!-- snippet: delegate-stub-reset -->
```cs
// Reset clears tracking state but preserves configuration
stub.Interceptor.Reset();

stub.Interceptor.Verify(Called.Never);
Assert.Null(stub.Interceptor.LastArg);
Assert.Equal("TEST", format("test")); // Return still works
```
<!-- endSnippet -->

---

## Quick Reference

| Task | Code |
|------|------|
| Create stub | `var stub = new Stubs.MyDelegate();` |
| Configure return | `stub.Interceptor.Return(value)` |
| Configure callback | `stub.Interceptor.Call((args) => result)` |
| Configure void | `stub.Interceptor.Call((args) => { })` |
| Value sequence | `stub.Interceptor.Return(1, 2, 3)` |
| When matching | `stub.Interceptor.When(args).Return(value)` |
| Convert to delegate | `MyDelegate del = stub;` |
| Check last args | `stub.Interceptor.LastArgs` |
| Verify calls | `stub.Interceptor.Verify(Called.Once)` |
| Strict mode | `stub.Strict = true` |
| Reset | `stub.Interceptor.Reset()` |
