# Indexer Interceptor Reference

Indexer interceptors are generated for `this[TKey]` members on interfaces and virtual/abstract indexers on classes. They support per-key configuration, all-keys callbacks, sequences, tracking, and verification.

---

## Per-Key Builders

Access per-key builders via the interceptor's C# indexer. Each key gets its own builder with independent configuration.

<!-- snippet: indexers-ref-perkey -->
```cs
// Configure specific keys to return specific values
stub.Indexer["existing"].Returns("100");
stub.Indexer["special"].Returns("999");

IConfigStore collection = stub;
var val = collection["existing"]; // "100"
var val2 = collection["special"]; // "999"
```
<!-- endSnippet -->

### Per-Key Builder API

| Method | Description |
|--------|-------------|
| `stub.Indexer[key].Returns(value)` | Configure return value for this key |
| `stub.Indexer[key].Get(() => value)` | Per-key getter callback (no key param -- already bound) |
| `stub.Indexer[key].Set((value) => {})` | Per-key setter callback (no key param -- already bound) |
| `stub.Indexer[key].Returns(v1).ThenReturns(v2)` | Per-key getter sequence |

**Per-key callbacks do NOT receive the key** -- it's already bound by the indexer accessor.

---

## All-Keys Callbacks (Fallback)

All-keys callbacks handle keys not configured with per-key builders. They receive the key as a parameter.

<!-- snippet: indexers-ref-allkeys-get -->
```cs
// Get callback receives the key
stub.Indexer.Get((key) => key.Length.ToString());

IConfigStore collection = stub;
var len1 = collection["hello"]; // "5"
var len2 = collection["hi"];    // "2"
```
<!-- endSnippet -->

<!-- snippet: indexers-ref-allkeys-set -->
```cs
// Set callback receives key AND value
stub.Indexer.Set((key, value) => storage[key] = value);
```
<!-- endSnippet -->

---

## Per-Key with All-Keys Fallback

Per-key builders take priority over all-keys callbacks. This is the recommended pattern.

<!-- snippet: indexers-ref-perkey-fallback -->
```cs
stub.Indexer["special"].Returns("999");     // Per-key: always "999"
stub.Indexer.Get((key) => key.Length.ToString());     // All-keys: fallback

IConfigStore collection = stub;
var r1 = collection["special"]; // "999" (per-key wins)
var r2 = collection["hello"];   // "5" (callback fallback)
```
<!-- endSnippet -->

---

## Priority Chain

When an indexer getter is invoked, KnockOff resolves the value in this order:

1. **Per-key builder** -- `stub.Indexer[key].Returns(value)` (highest)
2. **When predicate match** -- `When(key => predicate).Returns(value)`
3. **All-keys sequence** -- `Get().ThenGet()` if active
4. **All-keys Get callback** -- `Get((key) => value)`
5. **Source delegation** -- `stub.Source(realImpl)`
6. **Strict mode check** -- throws `StubException` if strict
7. **Default value** -- `default(T)` (lowest)

---

## Multi-Param Indexers

For `this[int row, int col]` indexers, per-key builders use **flattened** syntax while callbacks use **tuple** syntax.

### Per-Key: Flattened Accessors

<!-- snippet: indexers-ref-multi-perkey -->
```cs
// Flattened -- natural C# indexer syntax
stub.Indexer[1, 2].Returns(12.0);
stub.Indexer[3, 4].Returns(34.0);

IMatrix matrix = stub;
var val = matrix[1, 2]; // 12.0
```
<!-- endSnippet -->

### All-Keys: Tuple Callbacks

<!-- snippet: indexers-ref-multi-allkeys -->
```cs
// Get callback receives named tuple
stub.Indexer.Get(key => key.row * 10.0 + key.col);

IMatrix matrix = stub;
var val = matrix[2, 3]; // 23.0
```
<!-- endSnippet -->

**Key insight**: Per-key uses flattened `[row, col]`, callbacks use tuple `(int row, int col)`.

---

## Multi-Indexer Disambiguation

When an interface has multiple indexers distinguished by key type, C# overload resolution handles it automatically:

<!-- snippet: indexers-multiple-overloads -->
```cs
// C# indexer overloads resolve by key type -- no OfXxx needed
stub.Indexer["name"].Returns("Alice");
stub.Indexer[0].Returns(100);
```
<!-- endSnippet -->

No special syntax needed -- the compiler resolves the correct overload.

---

## Init-Only Indexers

Indexers with `{ get; init; }` work identically to `{ get; set; }`. The interceptor API is unchanged.

---

## Sequences (All-Keys)

Indexer getter sequences are **global** -- they advance on ANY key access, not per-key.

### Get().ThenGet()

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

### Set().ThenSet()

<!-- snippet: sequences-indexer-allkeys-set -->
```cs
stub.Indexer.Set((k, v) => log.Add($"First: {k}={v}"))
    .ThenSet((k, v) => log.Add($"Final: {k}={v}"));
```
<!-- endSnippet -->

### ThenDefault()

<!-- snippet: indexers-ref-thendefault -->
```cs
stub.Indexer.Get((k) => k.Length.ToString())
    .ThenGet((k) => "100")
    .ThenDefault();  // null after exhaustion

IConfigStore collection = stub;
var r1 = collection["hello"]; // "5"
var r2 = collection["world"]; // "100"
var r3 = collection["foo"];   // null (default)
```
<!-- endSnippet -->

### Sequences Are Global, Not Per-Key

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

## Tracking

| Property | Type | Description |
|----------|------|-------------|
| `LastGetKey` | `TKey?` | Key from the most recent getter call (any path) |
| `LastSetEntry` | `(TKey, TValue)?` | (Key, Value) from the most recent setter call (any path) |

<!-- snippet: indexers-ref-tracking -->
```cs
_ = collection["a"];
_ = collection["b"];
var lastKey = stub.Indexer.LastGetKey; // "b"

collection["x"] = "10";
collection["y"] = "20";
var lastEntry = stub.Indexer.LastSetEntry; // ("y", "20")
```
<!-- endSnippet -->

Tracking counts ALL accesses regardless of whether handled by per-key, callback, or default.

---

## Per-Key Verification

Verify that a specific key was accessed a specific number of times, rather than checking total indexer access counts.

<!-- snippet: indexers-perkey-verify-get -->
```cs
// Verify a specific key was read a specific number of times
stub.Indexer["ApiKey"].VerifyGet(Called.Exactly(2));
stub.Indexer["Timeout"].VerifyGet(Called.Once);
```
<!-- endSnippet -->

<!-- snippet: indexers-perkey-verify-set -->
```cs
// Verify a specific key was written a specific number of times
stub.Indexer["ApiKey"].VerifySet(Called.Once);
stub.Indexer["Timeout"].VerifySet(Called.Exactly(2));
```
<!-- endSnippet -->

**Per-key vs. all-keys verification:**
- `stub.Indexer.VerifyGet(Called.Exactly(3))` -- verifies total get count across all keys
- `stub.Indexer["ApiKey"].VerifyGet(Called.Exactly(2))` -- verifies get count for a specific key only

---

## Predicate-Based Key Matching

Use `When(predicate)` to match keys by condition rather than exact value. Useful for configuring behavior for groups of keys that share a pattern.

<!-- snippet: indexers-when-predicate -->
```cs
// When(predicate) matches keys by condition
stub.Indexer.When(key => key.StartsWith("prefix_", StringComparison.Ordinal)).Returns(99);
```
<!-- endSnippet -->

### Combining Per-Key and When Predicate

Per-key exact match always takes priority over When predicate:

<!-- snippet: indexers-when-with-perkey -->
```cs
// Per-key exact match takes priority over When predicate
stub.Indexer["exact"].Returns(100);
stub.Indexer.When(key => key.Length > 3).Returns(42);
```
<!-- endSnippet -->

In this example, `stub["exact"]` returns 100 (per-key wins), while `stub["hello"]` returns 42 (When predicate matches).

### When with Set Callback

Getter and setter When chains are independent:

<!-- snippet: indexers-when-set-callback -->
```cs
// When(predicate).Set() intercepts writes for matching keys
stub.Indexer.When(key => key.StartsWith("temp_", StringComparison.Ordinal)).Set((key, value) =>
{
    captured.Add((key, value));
});
```
<!-- endSnippet -->

### When Chains with ThenWhen

Chain multiple predicates with `ThenWhen`. Each matcher advances after matching once; the last matcher repeats:

<!-- snippet: indexers-when-chain -->
```cs
// Chain multiple predicates with ThenWhen -- each matcher advances once
stub.Indexer
    .When(key => key.StartsWith("a", StringComparison.Ordinal)).Returns(1)
    .ThenWhen(key => key.StartsWith("b", StringComparison.Ordinal)).Returns(2);
```
<!-- endSnippet -->

---

## Verification (All-Keys)

| Method | Description |
|--------|-------------|
| `VerifyGet()` | Verify getter was called at least once |
| `VerifyGet(Called)` | Verify getter call count |
| `VerifySet()` | Verify setter was called at least once |
| `VerifySet(Called)` | Verify setter call count |
| `Verifiable()` | Mark for batch verification (AtLeastOnce) |
| `Verifiable(Called)` | Mark for batch verification with constraint |

<!-- snippet: indexers-verify-access -->
```cs
// Verify indexer get/set call counts
stub.Indexer.VerifyGet(Called.Exactly(2));
stub.Indexer.VerifySet(Called.Once);
```
<!-- endSnippet -->

Verification counts include ALL access paths (per-key, callback, unconfigured).

---

## Reset

`Reset()` clears:
- Get/set counts
- `LastGetKey`, `LastSetEntry`
- Sequence index

`Reset()` preserves:
- Per-key Returns configuration
- All-keys Get/Set callbacks
- Verifiable marking

---

## API Summary

### Per-Key Builder

| Method | Returns | Description |
|--------|---------|-------------|
| `Returns(TValue)` | `PerKeyBuilder` | Set return value for this key |
| `Get(Func<TValue>)` | `PerKeyBuilder` | Getter callback for this key (no key param) |
| `Set(Action<TValue>)` | `PerKeyBuilder` | Setter callback for this key (no key param) |
| `Returns(v).ThenReturns(v2)` | `PerKeyBuilder` | Per-key sequence |
| `VerifyGet()` | `void` | Verify this key's getter was called at least once |
| `VerifyGet(Called)` | `void` | Verify this key's getter call count |
| `VerifySet()` | `void` | Verify this key's setter was called at least once |
| `VerifySet(Called)` | `void` | Verify this key's setter call count |

### All-Keys Configuration

| Method | Returns | Description |
|--------|---------|-------------|
| `Get(Func<TKey, TValue>)` | `IIndexerGetSequence` | All-keys getter callback |
| `Set(Action<TKey, TValue>)` | `IIndexerSetSequence` | All-keys setter callback |
| `ThenGet(Func<TKey, TValue>)` | `IIndexerGetSequence` | Add to getter sequence |
| `ThenSet(Action<TKey, TValue>)` | `IIndexerSetSequence` | Add to setter sequence |
| `ThenDefault()` | `void` | Return default(T) after exhaustion |
| `When(Func<TKey, bool>)` | `IIndexerWhenBuilder` | Predicate-based key matching |
