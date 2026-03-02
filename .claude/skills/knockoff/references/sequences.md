# Sequences Reference

Sequences configure different return values or behaviors for successive calls. KnockOff supports sequences for methods, properties, and indexers with NSubstitute-compatible exhaustion behavior.

---

## Core Behavior

- After all values/callbacks are consumed, the **last one repeats** indefinitely (NSubstitute-like)
- Use `ThenDefault()` to return `default(T)` after exhaustion instead
- In strict mode, exhausted sequences throw `StubException.SequenceExhausted`

---

## Method Sequences

### Params Syntax (Preferred)

<!-- snippet: sequences-method-params -->
```cs
// NSubstitute-style concise syntax
stub.Add.Return(1, 2, 3);
```
<!-- endSnippet -->

### Single Value vs Params

C# overload resolution distinguishes them:

<!-- snippet: sequences-single-vs-params -->
```cs
stub.Add.Return(42);       // Single value -- repeats forever (no sequence)
stub.Add.Return(1, 2, 3);  // Params -- sequence, last repeats after exhaustion
```
<!-- endSnippet -->

### Callback Sequences

<!-- snippet: sequences-method-callback -->
```cs
stub.Add
    .Call((int a, int b) => a + b)     // First: computed
    .ThenReturn((int a, int b) => a * b) // Second: computed
    .ThenReturn(999);            // Third+: constant
```
<!-- endSnippet -->

### Callback + Params

<!-- snippet: sequences-callback-then-params -->
```cs
stub.Add.Call((int a, int b) => a + b).ThenReturn(100, 200, 300);

ISeqCalc calc = stub;
calc.Add(1, 2); // 3 (computed)
calc.Add(0, 0); // 100
calc.Add(0, 0); // 200
calc.Add(0, 0); // 300
calc.Add(0, 0); // 300 (repeats)
```
<!-- endSnippet -->

### Value-Based Sequences

<!-- snippet: sequences-value-based -->
```cs
stub.Add.Return(1).ThenReturn(2).ThenReturn(3);
// Equivalent to: stub.Add.Return(1, 2, 3);
```
<!-- endSnippet -->

### ThenDefault()

<!-- snippet: sequences-method-thendefault -->
```cs
stub.Add.Call((int a, int b) => 1).ThenReturn((int a, int b) => 999).ThenDefault();

ISeqCalc calc = stub;
calc.Add(0, 0); // 1
calc.Add(0, 0); // 999
calc.Add(0, 0); // 0 (default)
```
<!-- endSnippet -->

### Async Auto-Wrapping in Sequences

Params values auto-wrap for `Task<T>` and `ValueTask<T>`:

<!-- snippet: sequences-async-autowrap -->
```cs
stub.GetDataAsync.Return("first", "second", "third");

IDataSvc service = stub;
var r1 = await service.GetDataAsync(1); // "first"
var r2 = await service.GetDataAsync(2); // "second"
var r3 = await service.GetDataAsync(3); // "third"
var r4 = await service.GetDataAsync(4); // "third" (repeats)
```
<!-- endSnippet -->

### Void Method Sequences

<!-- snippet: sequences-void-method -->
```cs
stub.Reset
    .Call(() => log.Add("First"))
    .ThenCall(() => log.Add("Second"))
    .ThenCall(() => log.Add("Subsequent"));

ISeqCalc calc = stub;
calc.Reset(); // "First"
calc.Reset(); // "Second"
calc.Reset(); // "Subsequent"
calc.Reset(); // "Subsequent" (repeats last)
```
<!-- endSnippet -->

---

## Property Sequences

### Getter Sequences -- Get().ThenGet()

<!-- snippet: sequences-property-get-value -->
```cs
stub.Name
    .Get("First")
    .ThenGet("Second")
    .ThenGet("Third");
```
<!-- endSnippet -->

With callbacks:

<!-- snippet: sequences-property-get-callback -->
```cs
stub.Name
    .Get(() => "First")
    .ThenGet(() => "Second")
    .ThenGet(() => "Third");
```
<!-- endSnippet -->

### Setter Sequences -- Set().ThenSet()

<!-- snippet: sequences-property-set -->
```cs
stub.Name
    .Set((v) => firstWrite = v)
    .ThenSet((v) => secondWrite = v);
```
<!-- endSnippet -->

### Property ThenDefault()

<!-- snippet: sequences-property-thendefault -->
```cs
stub.Name.Get("first").ThenGet("second").ThenDefault();

ISeqNameSvc service = stub;
_ = service.Name; // "first"
_ = service.Name; // "second"
_ = service.Name; // null (default)
```
<!-- endSnippet -->

---

## Indexer Sequences

Indexer sequences are **global** -- they advance on ANY key access, not per-key.

### All-Keys Getter Sequences

<!-- snippet: sequences-indexer-allkeys-get -->
```cs
stub.Indexer.Get((k) => k.Length.ToString())
    .ThenGet((k) => "100")
    .ThenGet((k) => "999");

IConfigStore collection = stub;
_ = collection["hello"]; // "5" (first callback)
_ = collection["world"]; // "100" (second callback)
_ = collection["foo"];   // "999" (third)
_ = collection["bar"];   // "999" (repeats)
```
<!-- endSnippet -->

### All-Keys Setter Sequences

<!-- snippet: sequences-indexer-allkeys-set -->
```cs
stub.Indexer.Set((k, v) => log.Add($"First: {k}={v}"))
    .ThenSet((k, v) => log.Add($"Final: {k}={v}"));
```
<!-- endSnippet -->

### Per-Key Sequences

<!-- snippet: sequences-indexer-perkey -->
```cs
stub.Indexer["key"].Returns("1").ThenReturns("2").ThenReturns("3");

IConfigStore collection = stub;
_ = collection["key"]; // "1"
_ = collection["key"]; // "2"
_ = collection["key"]; // "3"
_ = collection["key"]; // "3" (repeats)
```
<!-- endSnippet -->

### Global vs Per-Key

All-keys sequences are shared across all keys:

<!-- snippet: sequences-indexer-global -->
```cs
stub.Indexer.Get((k) => "1").ThenGet((k) => "2").ThenGet((k) => "3");

IConfigStore collection = stub;
_ = collection["a"]; // "1"
_ = collection["b"]; // "2" (advanced despite different key!)
_ = collection["c"]; // "3"
```
<!-- endSnippet -->

For per-key behavior, use per-key `Returns` or a Get callback with its own dictionary.

---

## Sequence Exhaustion

| Behavior | How to Configure |
|----------|-----------------|
| Repeat last (default) | `Return(1, 2, 3)` |
| Return default(T) | `Return(1, 2).ThenDefault()` |
| Throw exception | `stub.Strict = true` + sequence |

### Strict Mode Throws on Exhaustion

<!-- snippet: sequences-strict-exhaustion -->
```cs
stub.Strict = true;
stub.Add.Call((int a, int b) => 100).ThenReturn((int a, int b) => 200);

ISeqCalc calc = stub;
calc.Add(0, 0); // 100
calc.Add(0, 0); // 200
Assert.Throws<StubException>(() => calc.Add(0, 0)); // StubException.SequenceExhausted
```
<!-- endSnippet -->

---

## Sequence Verification

Sequences support `Verify()` to check if fully consumed:

<!-- snippet: sequences-verify -->
```cs
var sequence = stub.Add.Return(1, 2, 3);

ISeqCalc calc = stub;
calc.Add(0, 0); // 1
calc.Add(0, 0); // 2
calc.Add(0, 0); // 3

sequence.Verify(); // Passes -- all 3 consumed
```
<!-- endSnippet -->

---

## Interaction with When Chains

When chains have **higher priority** than sequences. When matches don't advance the sequence:

<!-- snippet: sequences-when-interaction -->
```cs
stub.Add.Call((int a, int b) => 1).ThenReturn((int a, int b) => 2);
stub.Add.When(99, 99).Return(9999);

ISeqCalc calc = stub;
calc.Add(0, 0);   // 1 (sequence)
calc.Add(99, 99);  // 9999 (When match -- doesn't advance sequence)
calc.Add(0, 0);   // 2 (sequence advances)
calc.Add(99, 99);  // 9999 (When still matches)
```
<!-- endSnippet -->

---

## Reset and Sequences

`Reset()` clears the sequence index (resets to beginning) but preserves the sequence structure:

<!-- snippet: sequences-reset -->
```cs
stub.Add.Return(1, 2, 3);
ISeqCalc calc = stub;
calc.Add(0, 0); // 1
calc.Add(0, 0); // 2

stub.Add.Reset();

calc.Add(0, 0); // 1 (restarted from beginning)
```
<!-- endSnippet -->

---

## Method Summary

### Method Sequences

| Method | Description |
|--------|-------------|
| `Return(first, params rest)` | Concise value sequence |
| `Call(cb).ThenReturn(cb)` | Callback sequence |
| `Call(cb).ThenReturn(value)` | Mix callbacks and values |
| `Call(cb).ThenReturn(params values)` | Callback then multiple values |
| `Return(v).ThenReturn(v)` | Value-based sequence |
| `ThenDefault()` | Return default(T) after exhaustion |

### Property Sequences

| Method | Description |
|--------|-------------|
| `Get(v).ThenGet(v)` | Getter value sequence |
| `Get(cb).ThenGet(cb)` | Getter callback sequence |
| `Set(cb).ThenSet(cb)` | Setter callback sequence |
| `ThenDefault()` | Return default(T) after exhaustion |

### Indexer Sequences

| Method | Description |
|--------|-------------|
| `Get(cb).ThenGet(cb)` | All-keys getter sequence (global) |
| `Set(cb).ThenSet(cb)` | All-keys setter sequence (global) |
| `[key].Returns(v).ThenReturns(v)` | Per-key sequence |
| `ThenDefault()` | Return default(T) after exhaustion |
