# Property Interceptor Reference

Property interceptors are generated for every property in an interface or for every virtual/abstract property in a stubbed class. Each interceptor provides getter/setter configuration, tracking, and verification.

---

## Class Stub Property Behavior

For class stubs (Patterns 3, 4, 6, 9), unconfigured virtual properties call the base class getter/setter by default. This is the same base fallback behavior as class stub methods -- equivalent to Moq's `CallBase = true`, but on by default.

- **Virtual property, unconfigured**: calls base getter/setter
- **Virtual property, configured** (Get/Set): interceptor handles it, base is NOT called
- **Abstract property, unconfigured**: returns `default(T)` for getter (or throws in strict mode)
- **Abstract property, configured**: interceptor handles it

---

## Setting Static Values with Get

The `Get(value)` method is the simplest way to configure a property. Call it before your test runs to return a fixed value.

<!-- snippet: properties-value-basic -->
```cs
// Set a static value for the property via the interceptor
stub.CurrentUser.Get(new User { Id = 1, Name = "Alice" });
```
<!-- endSnippet -->

Configure multiple properties at once for test fixtures:

<!-- snippet: properties-value-multiple -->
```cs
// Configure several properties before test execution
stub.UserId.Get(42);
stub.Email.Get("test@example.com");
stub.CurrentUser.Get(new User { Id = 42, Name = "Test User" });
```
<!-- endSnippet -->

---

## Dynamic Getters with Get Callbacks

Use `Get(() => value)` when a property's value should be computed at access time. The callback is invoked on every property read.

<!-- snippet: properties-onget-dynamic -->
```cs
// Get callback returns dynamic value on each access
stub.Timestamp.Get(() => DateTime.UtcNow);
```
<!-- endSnippet -->

Get callbacks can create state-dependent behavior:

<!-- snippet: properties-onget-stateful -->
```cs
// Get checks the tracked state
stub.IsReady.Get(() => isInitialized);
// Initialize method updates the tracked state
stub.Initialize.Call(() => { isInitialized = true; });
```
<!-- endSnippet -->

**Get supports both value and callback syntax:**

<!-- snippet: properties-onget-value-vs-callback -->
```cs
// VALUE: Simple syntax for static values
stub.Name.Get("StaticName");

// CALLBACK: For computed or dynamic values
stub.Age.Get(() => DateTime.Now.Year - 2000);
```
<!-- endSnippet -->

---

## Setter Interception with Set

Use `Set(callback)` to intercept property writes. This allows tracking values or validating input during tests.

<!-- snippet: properties-onset-tracking -->
```cs
// Set captures every value written to the property
var setValues = new List<string>();
stub.Name.Set((value) => setValues.Add(value));
```
<!-- endSnippet -->

Use `Set` to simulate validation logic in dependencies:

<!-- snippet: properties-onset-validation -->
```cs
// Set throws for invalid values
stub.Age.Set((value) =>
{
    if (value < 0)
        throw new ArgumentException("Age cannot be negative");
});
```
<!-- endSnippet -->

---

## LastSetValue

`LastSetValue` captures the most recent value written to a property:

<!-- snippet: properties-verify-lastsetvalue -->
```cs
// LastSetValue contains the most recent value
Assert.Equal("Expected", stub.Name.LastSetValue);
```
<!-- endSnippet -->

---

## Get Configuration Priority

When you call `Get` multiple times, the last call wins:

<!-- snippet: properties-priority -->
```cs
// Last Get call wins - can upgrade from value to callback
stub.Name.Get("initial");
stub.Name.Get(() => "dynamic");
```
<!-- endSnippet -->

---

## Resetting Property Interceptors

Calling `Reset()` on a property interceptor clears tracking state but preserves configured callbacks.

<!-- snippet: properties-reset -->
```cs
// Reset clears counts but preserves callbacks
stub.Name.Reset();

stub.Name.VerifyGet(Called.Never);
stub.Name.VerifySet(Called.Never);
```
<!-- endSnippet -->

**Reset() clears:** Get/set counts, LastSetValue, sequence index, source delegation.

**Reset() preserves:** Get/Set callbacks (including sequence structure), Verifiable marking.

---

## Decision Guide

| Scenario | Use This | Example |
|----------|----------|---------|
| Fixed test data | `Get(value)` | `stub.UserId.Get(42)` |
| Dynamic/computed value | `Get(callback)` | `stub.Now.Get(() => DateTime.UtcNow)` |
| State-dependent | `Get(callback)` | `stub.IsReady.Get(() => isInitialized)` |
| Track values written | `Set(callback)` | `stub.Name.Set(v => list.Add(v))` |
| Simulate validation | `Set(callback)` | `stub.Age.Set(v => Validate(v))` |
| Verify reads | `VerifyGet` | `stub.UserId.VerifyGet(Called.Exactly(2))` |
| Verify last write | `LastSetValue` | `Assert.Equal("x", stub.Name.LastSetValue)` |

---

## API Summary

### Configuration Methods

| Method | Description |
|--------|-------------|
| `Get(T value)` | Configure getter to return static value |
| `Get(Func<T> callback)` | Configure getter with dynamic callback |
| `Set(Action<T> callback)` | Configure setter callback |

### Inspection Properties

| Property | Description |
|----------|-------------|
| `LastSetValue` | Most recent value written (null/default if never set) |

### Utility Methods

| Method | Description |
|--------|-------------|
| `Reset()` | Clears tracking state; preserves Get/Set callbacks and Verifiable marking |

**Cross-cutting features** covered in dedicated reference files:
- Sequences: `Get().ThenGet()`, `Set().ThenSet()`, `ThenDefault()` → see **sequences.md**
- Verification: `VerifyGet()`, `VerifySet()`, `Verify()`, `Verifiable()` → see **verification.md**
- Stub override properties: underscore-suffix pattern → see **stub-overrides.md**
