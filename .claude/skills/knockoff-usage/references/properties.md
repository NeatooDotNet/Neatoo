# Property Interceptor Reference

This reference covers all aspects of property interceptors in KnockOff, including static values, dynamic callbacks, sequences, verification, and reset behavior.

---

## Overview

Property interceptors are generated for every property in an interface. Each interceptor provides:

- **OnGet(value)** - Set a static value to return from the getter
- **OnGet(callback)** - Dynamic callback for computed values
- **OnSet(callback)** - Callback for intercepting setter calls
- **OnGet().ThenGet() / OnSet().ThenSet()** - Different behavior for successive accesses (sequences)
- **Verification methods** - For asserting on property access patterns
- **LastSetValue** - For capturing the most recent value written to a setter

---

## Setting Static Values with OnGet

The `OnGet(value)` method is the simplest way to configure a property. Call it before your test runs to return a fixed value.

<!-- snippet: properties-value-basic -->
```cs
[Fact]
public void Value_SetsPropertyReturnValue()
{
    var stub = new UserConfigPropsStub();

    // Set a static value for the property via the interceptor
    stub.CurrentUser.OnGet(new User { Id = 1, Name = "Alice" });

    IUserConfigProps config = stub;
    var user = config.CurrentUser;

    Assert.NotNull(user);
    Assert.Equal("Alice", user.Name);
}
```
<!-- endSnippet -->

Configure multiple properties at once for test fixtures:

<!-- snippet: properties-value-multiple -->
```cs
[Fact]
public void Value_ConfigureMultipleProperties()
{
    var stub = new UserConfigPropsStub();

    // Configure several properties before test execution
    stub.UserId.OnGet(42);
    stub.Email.OnGet("test@example.com");
    stub.CurrentUser.OnGet(new User { Id = 42, Name = "Test User" });

    IUserConfigProps config = stub;

    Assert.Equal(42, config.UserId);
    Assert.Equal("test@example.com", config.Email);
    Assert.NotNull(config.CurrentUser);
}
```
<!-- endSnippet -->

**When to use OnGet(value):**
- Pre-populating repository stub data
- Configuring service dependencies with fixed values
- Setting up DTOs or configuration objects
- Any scenario where the value does not change during the test

---

## Dynamic Getters with OnGet Callbacks

Use `OnGet(() => value)` when a property's value should be computed at access time. The callback is invoked on every property read.

<!-- snippet: properties-onget-dynamic -->
```cs
[Fact]
public void OnGet_ReturnsComputedValue()
{
    var stub = new TimeProviderPropsStub();

    // OnGet callback returns dynamic value on each access
    stub.Timestamp.OnGet(() => DateTime.UtcNow);

    ITimeProviderProps timeProvider = stub;

    var time1 = timeProvider.Timestamp;
    Thread.Sleep(10);
    var time2 = timeProvider.Timestamp;

    // Each access returns current time
    Assert.True(time2 >= time1);
}
```
<!-- endSnippet -->

OnGet callbacks can create state-dependent behavior:

<!-- snippet: properties-onget-stateful -->
```cs
[Fact]
public void OnGet_DependsOnOtherInterceptorState()
{
    var stub = new ServiceWithInitPropsStub();

    // Track initialization state with local variable
    var isInitialized = false;

    // OnGet checks the tracked state
    stub.IsReady.OnGet(() => isInitialized);
    var initTracking = stub.Initialize.OnCall(() => { isInitialized = true; });

    IServiceWithInitProps service = stub;

    // Initially false (Initialize not called)
    Assert.False(service.IsReady);

    // After Initialize, becomes true
    service.Initialize();
    Assert.True(service.IsReady);
}
```
<!-- endSnippet -->

**OnGet supports both value and callback syntax:**

<!-- snippet: properties-onget-value-vs-callback -->
```cs
[Fact]
public void OnGet_ValueVsCallback()
{
    var stub = new ConfigPropsStub();

    // VALUE: Simple syntax for static values
    stub.Name.OnGet("StaticName");

    // CALLBACK: For computed or dynamic values
    stub.Age.OnGet(() => DateTime.Now.Year - 2000);

    IConfigProps config = stub;

    Assert.Equal("StaticName", config.Name);
    Assert.True(config.Age >= 0); // Dynamic value
}
```
<!-- endSnippet -->

**When to use OnGet(callback):**
- Values that change over time (timestamps, random values)
- Computed values based on other stub state
- Simulating stateful behavior in dependencies
- Testing race conditions or timing-dependent logic

---

## Setter Interception with OnSet

Use `OnSet(callback)` to intercept property writes. This allows tracking values or validating input during tests.

<!-- snippet: properties-onset-tracking -->
```cs
[Fact]
public void OnSet_TracksAllWrittenValues()
{
    var stub = new ConfigPropsStub();

    var setValues = new List<string>();
    stub.Name.OnSet((value) => setValues.Add(value));

    IConfigProps config = stub;

    config.Name = "First";
    config.Name = "Second";
    config.Name = "Third";

    Assert.Equal(3, setValues.Count);
    Assert.Equal(new[] { "First", "Second", "Third" }, setValues);
}
```
<!-- endSnippet -->

Use `OnSet` to simulate validation logic in dependencies:

<!-- snippet: properties-onset-validation -->
```cs
[Fact]
public void OnSet_SimulatesValidation()
{
    var stub = new ConfigPropsStub();

    // OnSet throws for invalid values
    stub.Age.OnSet((value) =>
    {
        if (value < 0)
            throw new ArgumentException("Age cannot be negative");
    });

    IConfigProps config = stub;

    // Valid value works
    config.Age = 25;

    // Invalid value throws
    Assert.Throws<ArgumentException>(() => config.Age = -1);
}
```
<!-- endSnippet -->

**When to use OnSet:**
- Tracking all values written to a property
- Simulating validation failures in dependencies
- Testing how your code handles property setter exceptions
- Verifying the sequence of property writes

---

## Verifying Property Access

Property interceptors support verification similar to method interceptors.

### Using VerifyGet

<!-- snippet: properties-verify-getcount -->
```cs
[Fact]
public void VerifyGet_TracksPropertyReads()
{
    var stub = new ConfigPropsStub();
    stub.Age.OnGet(42);

    IConfigProps service = stub;

    _ = service.Age;
    _ = service.Age;

    // VerifyGet checks how many times property was read
    stub.Age.VerifyGet(Times.Exactly(2));
}
```
<!-- endSnippet -->

### Using LastSetValue

`LastSetValue` captures the most recent value written to a property:

<!-- snippet: properties-verify-lastsetvalue -->
```cs
[Fact]
public void LastSetValue_CapturesLastWrittenValue()
{
    var stub = new ConfigPropsStub();

    IConfigProps service = stub;

    service.Name = "First";
    service.Name = "Second";
    service.Name = "Expected";

    // LastSetValue contains the most recent value
    Assert.Equal("Expected", stub.Name.LastSetValue);
}
```
<!-- endSnippet -->

### Verification Methods

| Method | Description |
|--------|-------------|
| `VerifyGet()` | Verify property getter was called at least once (throws if not) |
| `VerifyGet(Times)` | Verify property getter was called according to Times constraint |
| `VerifySet()` | Verify property setter was called at least once (throws if not) |
| `VerifySet(Times)` | Verify property setter was called according to Times constraint |
| `Verify()` | Verify property was accessed (get or set) at least once |
| `Verify(Times)` | Verify total access count (get + set) satisfies Times constraint |

### Inspection Properties

| Property | Description |
|----------|-------------|
| `LastSetValue` | The most recent value written (null/default if never set) |

---

## Using Verifiable() on Properties

Mark properties for batch verification using `Verifiable()`:

<!-- snippet: properties-verifiable -->
```cs
[Fact]
public void Verifiable_MarksPropertyForVerification()
{
    var stub = new ConfigPropsStub();

    // Mark property as verifiable
    stub.Name.OnGet("test");
    stub.Name.Verifiable();
    stub.Age.Verifiable();

    IConfigProps service = stub;
    _ = service.Name;
    service.Age = 42;

    // Verify individually (standalone stubs verify at interceptor level)
    stub.Name.Verify();
    stub.Age.Verify();
}
```
<!-- endSnippet -->

### Verifiable Methods

| Method | Description |
|--------|-------------|
| `Verifiable()` | Mark property (get and set) for batch verification with default constraint (AtLeastOnce) |
| `Verifiable(Times)` | Mark property (get and set) for batch verification with specific Times constraint |

---

## Property Sequences

### OnGet().ThenGet() for Successive Reads

Use `OnGet().ThenGet()` when a property should return different values on successive reads.

<!-- snippet: properties-onget-then-sequence -->
```cs
[Fact]
public void OnGet_ThenGet_ReturnsDifferentValuesOnSuccessiveReads()
{
    var stub = new ConfigPropsStub();

    // OnGet().ThenGet() configures different return values for each read
    stub.Name
        .OnGet(() => "First")
        .ThenGet(() => "Second")
        .ThenGet(() => "Third");

    IConfigProps config = stub;

    // Each read returns the next value in the sequence
    Assert.Equal("First", config.Name);
    Assert.Equal("Second", config.Name);
    Assert.Equal("Third", config.Name);
}
```
<!-- endSnippet -->

The value overload simplifies static sequences:

<!-- snippet: properties-ongetsequence-value -->
```cs
[Fact]
public void OnGet_ValueSyntax_ThenGet()
{
    var stub = new ConfigPropsStub();

    // OnGet with value - simpler syntax for static values
    // ThenGet elevates to sequence mode
    stub.Name.OnGet("First")
        .ThenGet(() => "Second")
        .ThenGet(() => "Third");

    IConfigProps config = stub;

    Assert.Equal("First", config.Name);
    Assert.Equal("Second", config.Name);
    Assert.Equal("Third", config.Name);
}
```
<!-- endSnippet -->

### OnSet().ThenSet() for Successive Writes

Use `OnSet().ThenSet()` when a property should react differently to successive writes.

<!-- snippet: properties-onset-then-sequence -->
```cs
[Fact]
public void OnSet_ThenSet_ReactsDifferentlyToSuccessiveWrites()
{
    var stub = new ConfigPropsStub();

    var firstWriteValue = "";
    var secondWriteValue = "";

    // OnSet().ThenSet() configures different callbacks for each write
    stub.Name
        .OnSet((value) => { firstWriteValue = $"First: {value}"; })
        .ThenSet((value) => { secondWriteValue = $"Second: {value}"; });

    IConfigProps config = stub;

    // First write triggers first callback
    config.Name = "Alpha";
    Assert.Equal("First: Alpha", firstWriteValue);
    Assert.Equal("", secondWriteValue);

    // Second write triggers second callback
    config.Name = "Beta";
    Assert.Equal("Second: Beta", secondWriteValue);
}
```
<!-- endSnippet -->

### Verifying Sequences

Sequences support the same verification as regular callbacks:

<!-- snippet: properties-sequence-verification -->
```cs
[Fact]
public void Sequence_VerifiesLikeRegularCallbacks()
{
    var stub = new ConfigPropsStub();

    // Configure sequences
    var getSequence = stub.Name
        .OnGet(() => "A")
        .ThenGet(() => "B");

    var setSequence = stub.Age
        .OnSet((v) => { })
        .ThenSet((v) => { });

    IConfigProps config = stub;

    // Access properties
    _ = config.Name;
    _ = config.Name;
    config.Age = 1;
    config.Age = 2;

    // Verify sequence was fully consumed
    getSequence.Verify();
    setSequence.Verify();

    // VerifyGet/VerifySet work the same with sequences
    stub.Name.VerifyGet(Times.Exactly(2));
    stub.Age.VerifySet(Times.Exactly(2));
}
```
<!-- endSnippet -->

---

## OnGet Configuration Priority

When you call `OnGet` multiple times, the last call wins. This applies to both value and callback syntax.

<!-- snippet: properties-priority -->
```cs
[Fact]
public void OnGet_TakesPrecedenceOverValue()
{
    var stub = new ConfigPropsStub();

    // Set a static value
    stub.Name.OnGet("initial");

    // Then set OnGet - it takes precedence
    stub.Name.OnGet(() => "dynamic");

    IConfigProps config = stub;

    // Callback syntax takes precedence (last call wins)
    Assert.Equal("dynamic", config.Name);
}
```
<!-- endSnippet -->

**Design principle:** This allows reconfiguring property behavior without explicitly clearing the previous configuration first.

---

## Resetting Property Interceptors

Calling `Reset()` on a property interceptor clears all tracking state and configured callbacks.

<!-- snippet: properties-reset -->
```cs
[Fact]
public void Reset_ClearsCountsButPreservesValue()
{
    var stub = new ConfigPropsStub();

    stub.Name.OnGet("test");

    IConfigProps config = stub;

    // Access property to increment counts
    _ = config.Name;
    config.Name = "updated";

    stub.Name.VerifyGet(Times.AtLeastOnce);
    stub.Name.VerifySet(Times.AtLeastOnce);

    // Reset clears counts and callbacks
    stub.Name.Reset();

    stub.Name.VerifyGet(Times.Never);
    stub.Name.VerifySet(Times.Never);
    // Note: Reset clears tracking counters and all configured callbacks
}
```
<!-- endSnippet -->

### Reset Behavior Summary

Reset() clears:
- Tracking state (get/set counts)
- `LastSetValue`
- All `OnGet` callbacks (including sequences)
- All `OnSet` callbacks (including sequences)
- Sequence index (resets to beginning)
- Source delegation

After reset, the property returns to unconfigured state.

---

## Complete Example

This example demonstrates all property configuration approaches in a realistic test scenario:

<!-- snippet: properties-complete-example -->
```cs
[Fact]
public void CompletePropertyExample_AllConfigurationApproaches()
{
    var stub = new UserConfigCompleteStub();

    // Track connection state with local variable
    var isConnected = false;

    // OnGet with static value: Fixed test data
    stub.CurrentUser.OnGet(new User { Id = 1, Name = "Alice" });

    // OnGet: State-dependent behavior using tracked state
    stub.IsConnected.OnGet(() => isConnected);

    // OnSet: Track all values written
    var connectionStrings = new List<string>();
    stub.ConnectionString.OnSet((value) => connectionStrings.Add(value));

    // Configure the Connect method to update state
    var connectTracking = stub.Connect.OnCall(() => { isConnected = true; });

    IUserConfigComplete service = stub;

    // Test execution
    var user = service.CurrentUser;            // Read CurrentUser
    Assert.False(service.IsConnected);         // Not connected yet

    service.Connect();                          // Call Connect
    Assert.True(service.IsConnected);          // Now connected

    service.ConnectionString = "Server=test";  // Write ConnectionString

    // Verification
    stub.CurrentUser.VerifyGet(Times.Once);
    Assert.True(service.IsConnected);
    Assert.Single(connectionStrings);
    Assert.Equal("Server=test", stub.ConnectionString.LastSetValue);
}
```
<!-- endSnippet -->

---

## Decision Guide

Choose your configuration approach based on the test scenario:

| Scenario | Use This | Example |
|----------|----------|---------|
| Property should return fixed test data | `OnGet(value)` | `stub.UserId.OnGet(42);` |
| Property should return current time/random value | `OnGet(callback)` | `stub.Now.OnGet(() => DateTime.UtcNow);` |
| Property depends on other stub state | `OnGet(callback)` | `stub.IsReady.OnGet(() => isInitialized);` |
| Property should return different values on successive reads | `OnGet().ThenGet()` | `stub.Name.OnGet("A").ThenGet("B");` |
| Track all values written to property | `OnSet` | `stub.Name.OnSet((v) => list.Add(v));` |
| Simulate validation in dependency | `OnSet` | `stub.Age.OnSet((v) => Validate(v));` |
| Property should react differently to successive writes | `OnSet().ThenSet()` | `stub.Name.OnSet(cb1).ThenSet(cb2);` |
| Verify property was accessed N times | `VerifyGet` | `stub.UserId.VerifyGet(Times.Exactly(2));` |
| Verify last value written | `LastSetValue` | `Assert.Equal("x", stub.Name.LastSetValue);` |

---

## API Summary

### Configuration Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `OnGet(T value)` | `IPropertyGetSequence<T>` | Configure getter to return static value. Chain with `.ThenGet()` for sequences. |
| `OnGet(Func<T> callback)` | `IPropertyGetSequence<T>` | Configure getter with dynamic callback. Chain with `.ThenGet()` for sequences. |
| `OnSet(Action<T> callback)` | `IPropertySetSequence<T>` | Configure setter callback. Chain with `.ThenSet()` for sequences. |

### Verification Methods

| Method | Description |
|--------|-------------|
| `Verify()` | Verify property was accessed (get or set) at least once |
| `Verify(Times)` | Verify total access count satisfies Times constraint |
| `VerifyGet()` | Verify property getter was called at least once |
| `VerifyGet(Times)` | Verify property getter was called according to Times constraint |
| `VerifySet()` | Verify property setter was called at least once |
| `VerifySet(Times)` | Verify property setter was called according to Times constraint |
| `Verifiable()` | Mark property for batch verification (AtLeastOnce) |
| `Verifiable(Times)` | Mark property for batch verification with specific constraint |

### Inspection Properties

| Property | Type | Description |
|----------|------|-------------|
| `LastSetValue` | `T?` | The value from the most recent setter call (null/default if never set) |

### Utility Methods

| Method | Description |
|--------|-------------|
| `Reset()` | Clears all tracking, callbacks, sequences, and source delegation |

### Sequence Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `ThenGet(T value)` | `IPropertyGetSequence<T>` | Add static value to getter sequence |
| `ThenGet(Func<T> callback)` | `IPropertyGetSequence<T>` | Add callback to getter sequence |
| `ThenSet(Action<T> callback)` | `IPropertySetSequence<T>` | Add callback to setter sequence |

---

**UPDATED:** 2026-01-25
