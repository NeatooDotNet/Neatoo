# Event Interceptor Reference

Event interceptors are generated for every event on a stubbed interface or virtual/abstract event on a stubbed class. They track subscriptions, provide `Raise()` to fire events, and support verification.

---

## Event Interceptor Naming

Event interceptors use **bare names** matching the event -- no "Interceptor" suffix.

```csharp
stub.Started         // NOT stub.StartedInterceptor
stub.DataReceived    // NOT stub.DataReceivedInterceptor
stub.Completed       // NOT stub.CompletedInterceptor
```

The event is accessed through the interface cast, while the interceptor is accessed on the stub. They live in separate scopes.

---

## Raise() Method

The `Raise()` signature matches the event's delegate type. Calling `Raise()` invokes all subscribed handlers.

### EventHandler Events

<!-- snippet: events-ref-raise-eventhandler -->
```cs
// event EventHandler? Started;
stub.Started.Raise(source, EventArgs.Empty);
```
<!-- endSnippet -->

Signature: `void Raise(object? sender, EventArgs e)`

### EventHandler\<T\> Events

<!-- snippet: events-ref-raise-eventhandler-t -->
```cs
// event EventHandler<DataEventArgs>? DataReceived;
stub.DataReceived.Raise(source, new DataEventArgs { Data = "test data" });
```
<!-- endSnippet -->

Signature: `void Raise(object? sender, T e)` where T : EventArgs

### Action Events (No Parameters)

<!-- snippet: events-ref-raise-action -->
```cs
// event Action? Completed;
stub.Completed.Raise();
```
<!-- endSnippet -->

Signature: `void Raise()`

### Action\<T\> Events (Typed Parameters)

<!-- snippet: events-ref-raise-action-typed -->
```cs
// event Action<string, int>? Progress;
stub.Progress.Raise("Loading", 50);
```
<!-- endSnippet -->

Signature: `void Raise(T1 arg1, T2 arg2)`

### Action\<T1, T2, ...\> Events

Same pattern extends to any number of type parameters:

| Delegate Type | Raise Signature |
|---------------|-----------------|
| `EventHandler` | `Raise(object? sender, EventArgs e)` |
| `EventHandler<T>` | `Raise(object? sender, T e)` |
| `Action` | `Raise()` |
| `Action<T1>` | `Raise(T1 arg1)` |
| `Action<T1, T2>` | `Raise(T1 arg1, T2 arg2)` |
| `Action<T1, T2, T3>` | `Raise(T1 arg1, T2 arg2, T3 arg3)` |
| Custom delegates | `Raise()` uses `DynamicInvoke` internally |

**Prefer EventHandler\<T\> or Action\<T\>** over custom delegates for best type safety.

### Raise() Safety

`Raise()` is safe to call with no subscribers -- it's a no-op (uses null-conditional invocation internally).

<!-- snippet: events-ref-raise-safety -->
```cs
var stub = new EventSourceStub();
// No subscribers -- no exception
stub.Started.Raise(null, EventArgs.Empty);
```
<!-- endSnippet -->

---

## HasSubscribers Property

Check if any handlers are currently subscribed:

<!-- snippet: events-ref-has-subscribers -->
```cs
var stub = new EventSourceStub();
IEventSource source = stub;

Assert.False(stub.Started.HasSubscribers);

source.Started += (s, e) => { };
Assert.True(stub.Started.HasSubscribers);
```
<!-- endSnippet -->

---

## Subscription Verification

### VerifyAdd / VerifyRemove

Track and verify subscription (+= ) and unsubscription (-=) counts separately.

<!-- snippet: events-ref-verify-add-remove -->
```cs
stub.Started.VerifyAdd(Called.Exactly(2));  // 2 subscriptions
stub.Started.VerifyRemove(Called.Once);     // 1 unsubscription
```
<!-- endSnippet -->

### Verification Methods

| Method | Description |
|--------|-------------|
| `VerifyAdd()` | Verify event was subscribed at least once |
| `VerifyAdd(Called)` | Verify subscription count with constraint |
| `VerifyRemove()` | Verify event was unsubscribed at least once |
| `VerifyRemove(Called)` | Verify unsubscription count with constraint |
| `Verify()` | Alias for `VerifyAdd()` -- verify at least one subscription |
| `Verifiable()` | Mark for batch verification (AtLeastOnce) |
| `Verifiable(Called)` | Mark for batch verification with constraint |

---

## Batch Verification

Events participate in `stub.Verify()` and `stub.VerifyAll()`:

<!-- snippet: events-ref-batch-verifiable -->
```cs
stub.Started.Verifiable();           // Requires at least one subscription
stub.DataReceived.Verifiable(Called.Never); // Must NOT be subscribed
```
<!-- endSnippet -->

---

## Reset

`Reset()` clears:
- All subscribed handlers (`HasSubscribers` becomes `false`)
- Add/remove counts (both reset to 0)

<!-- snippet: events-ref-reset -->
```cs
source.Started += (s, e) => { };
source.Started += (s, e) => { };

stub.Started.Reset();

Assert.False(stub.Started.HasSubscribers);
// VerifyAdd(Called.AtLeastOnce) would FAIL now
```
<!-- endSnippet -->

**Note:** Event Reset clears subscribers (unlike method/property Reset which preserves callbacks). This is because event handlers are tracking state, not configuration.

---

## Supported Across All Patterns

Events work identically across all applicable patterns:

| Pattern | Event Access | Subscribe Via |
|---------|-------------|---------------|
| Standalone | `stub.EventName` | `IInterface iface = stub; iface.Event += ...` |
| Standalone Class | `stub.EventName` | `stub.Object.Event += ...` |
| Inline Interface | `stub.EventName` | `IInterface iface = stub; iface.Event += ...` |
| Inline Class | `stub.EventName` | `stub.Object.Event += ...` |
| Open Generic Interface | `stub.EventName` | `IInterface<T> iface = stub; iface.Event += ...` |
| Open Generic Class | `stub.EventName` | `stub.Object.Event += ...` |

---

## Complete Example

<!-- snippet: events-ref-complete -->
```cs
var stub = new EventSourceStub();
IEventSource source = stub;

// Track events
var events = new List<string>();

source.Started += (s, e) => events.Add("started");
source.DataReceived += (s, e) => events.Add($"data: {e.Data}");
source.Completed += () => events.Add("completed");
source.Progress += (msg, pct) => events.Add($"{msg}: {pct}%");

// Fire events
stub.Started.Raise(source, EventArgs.Empty);
stub.DataReceived.Raise(source, new DataEventArgs { Data = "test" });
stub.Progress.Raise("Loading", 75);
stub.Completed.Raise();

// Verify subscriptions
stub.Started.VerifyAdd(Called.Once);
stub.DataReceived.VerifyAdd(Called.Once);
stub.Completed.VerifyAdd(Called.Once);
stub.Progress.VerifyAdd(Called.Once);
```
<!-- endSnippet -->
