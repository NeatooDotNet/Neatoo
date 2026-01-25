# Property Interceptor Reference

This reference covers all aspects of property interceptors in KnockOff, including static values, dynamic callbacks, verification, and reset behavior.

---

## Overview

Property interceptors are generated for every property in an interface. Each interceptor provides:

- **Value** - A static backing value returned by the getter
- **OnGet** - A dynamic callback for computed values
- **OnSet** - A callback for intercepting setter calls
- **Verification methods** - For asserting on property access patterns
- **LastSetValue** - For capturing the most recent value written

---

## Setting Static Values with Value

The `Value` property is the simplest way to configure a property. Set it before your test runs to return a fixed value.

```csharp
public interface IUserConfig
{
    int UserId { get; }
    string Email { get; set; }
    User CurrentUser { get; }
}

[KnockOff]
public partial class UserConfigStub : IUserConfig { }

[Fact]
public void Value_SetsPropertyReturnValue()
{
    var stub = new UserConfigStub();

    // Set a static value for the property via the interceptor
    stub.CurrentUser.Value = new User { Id = 1, Name = "Alice" };

    IUserConfig config = stub;
    var user = config.CurrentUser;

    Assert.NotNull(user);
    Assert.Equal("Alice", user.Name);
}
```

Configure multiple properties at once for test fixtures:

```csharp
[Fact]
public void Value_ConfigureMultipleProperties()
{
    var stub = new UserConfigStub();

    // Configure several properties before test execution
    stub.UserId.Value = 42;
    stub.Email.Value = "test@example.com";
    stub.CurrentUser.Value = new User { Id = 42, Name = "Test User" };

    IUserConfig config = stub;

    Assert.Equal(42, config.UserId);
    Assert.Equal("test@example.com", config.Email);
    Assert.NotNull(config.CurrentUser);
}
```

**When to use Value:**
- Pre-populating repository stub data
- Configuring service dependencies with fixed values
- Setting up DTOs or configuration objects
- Any scenario where the value does not change during the test

---

## Dynamic Getters with OnGet

Use `OnGet` when a property's value should be computed at access time. The callback is invoked on every property read.

```csharp
public interface ITimeProvider
{
    DateTime Timestamp { get; }
}

[KnockOff]
public partial class TimeProviderStub : ITimeProvider { }

[Fact]
public void OnGet_ReturnsComputedValue()
{
    var stub = new TimeProviderStub();

    // OnGet callback returns dynamic value on each access
    stub.Timestamp.OnGet = () => DateTime.UtcNow;

    ITimeProvider timeProvider = stub;

    var time1 = timeProvider.Timestamp;
    Thread.Sleep(10);
    var time2 = timeProvider.Timestamp;

    // Each access returns current time
    Assert.True(time2 >= time1);
}
```

OnGet callbacks can create state-dependent behavior:

```csharp
public interface IServiceWithInit
{
    bool IsReady { get; }
    void Initialize();
}

[KnockOff]
public partial class ServiceWithInitStub : IServiceWithInit { }

[Fact]
public void OnGet_DependsOnOtherInterceptorState()
{
    var stub = new ServiceWithInitStub();

    // Track initialization state with local variable
    var isInitialized = false;

    // OnGet checks the tracked state
    stub.IsReady.OnGet = () => isInitialized;
    var initTracking = stub.Initialize.OnCall(() => { isInitialized = true; });

    IServiceWithInit service = stub;

    // Initially false (Initialize not called)
    Assert.False(service.IsReady);

    // After Initialize, becomes true
    service.Initialize();
    Assert.True(service.IsReady);
}
```

**When to use OnGet:**
- Values that change over time (timestamps, random values)
- Computed values based on other stub state
- Simulating stateful behavior in dependencies
- Testing race conditions or timing-dependent logic

---

## Setter Interception with OnSet

Use `OnSet` to intercept property writes. This allows tracking values or validating input during tests.

**Important:** OnSet does NOT automatically update `Value`. If you want the property to retain the written value, your callback must explicitly set `Value`.

```csharp
public interface IConfig
{
    string Name { get; set; }
    int Age { get; set; }
}

[KnockOff]
public partial class ConfigStub : IConfig { }

[Fact]
public void OnSet_TracksAllWrittenValues()
{
    var stub = new ConfigStub();

    var setValues = new List<string>();
    stub.Name.OnSet = (value) => setValues.Add(value);

    IConfig config = stub;

    config.Name = "First";
    config.Name = "Second";
    config.Name = "Third";

    Assert.Equal(3, setValues.Count);
    Assert.Equal(new[] { "First", "Second", "Third" }, setValues);
}
```

Use `OnSet` to simulate validation logic in dependencies:

```csharp
[Fact]
public void OnSet_SimulatesValidation()
{
    var stub = new ConfigStub();

    // OnSet throws for invalid values
    stub.Age.OnSet = (value) =>
    {
        if (value < 0)
            throw new ArgumentException("Age cannot be negative");
    };

    IConfig config = stub;

    // Valid value works
    config.Age = 25;

    // Invalid value throws
    Assert.Throws<ArgumentException>(() => config.Age = -1);
}
```

**When to use OnSet:**
- Tracking all values written to a property
- Simulating validation failures in dependencies
- Testing how your code handles property setter exceptions
- Verifying the sequence of property writes

---

## Verifying Property Access

Property interceptors support verification similar to method interceptors.

### Using VerifyGet and VerifySet

```csharp
[Fact]
public void VerifyGet_TracksPropertyReads()
{
    var stub = new ConfigStub();
    stub.Age.Value = 42;

    IConfig service = stub;

    _ = service.Age;
    _ = service.Age;

    // VerifyGet checks how many times property was read
    stub.Age.VerifyGet(Times.Exactly(2));
}
```

```csharp
[Fact]
public void VerifySet_TracksPropertyWrites()
{
    var stub = new ConfigStub();

    IConfig service = stub;

    service.Name = "First";
    service.Name = "Second";

    // VerifySet checks how many times property was written
    stub.Name.VerifySet(Times.Exactly(2));
}
```

### Using LastSetValue

`LastSetValue` captures the most recent value written to a property:

```csharp
[Fact]
public void LastSetValue_CapturesLastWrittenValue()
{
    var stub = new ConfigStub();

    IConfig service = stub;

    service.Name = "First";
    service.Name = "Second";
    service.Name = "Expected";

    // LastSetValue contains the most recent value
    Assert.Equal("Expected", stub.Name.LastSetValue);
}
```

### Available Verification Methods

| Method | Description |
|--------|-------------|
| `VerifyGet()` | Verify property getter was called at least once (throws if not) |
| `VerifyGet(Times)` | Verify property getter was called according to Times constraint |
| `VerifySet()` | Verify property setter was called at least once (throws if not) |
| `VerifySet(Times)` | Verify property setter was called according to Times constraint |

### Available Inspection Properties

| Property | Description |
|----------|-------------|
| `LastSetValue` | The most recent value written (null/default if never set) |

---

## Using Verifiable() on Properties

Mark properties for batch verification using `MarkVerifiableGet()` and `MarkVerifiableSet()`:

```csharp
[Fact]
public void Verifiable_MarksPropertyForVerification()
{
    var stub = new ConfigStub();

    // Mark property getter as verifiable
    stub.Name.Value = "test";
    stub.Name.MarkVerifiableGet();

    // Mark property setter as verifiable
    stub.Age.MarkVerifiableSet();

    IConfig service = stub;
    _ = service.Name;
    service.Age = 42;

    // Verify all marked operations
    stub.Name.VerifyGet();
    stub.Age.VerifySet();
}
```

### Available Verifiable Methods

| Method | Description |
|--------|-------------|
| `MarkVerifiableGet()` | Mark getter for batch verification with default constraint (AtLeastOnce) |
| `MarkVerifiableGet(Times)` | Mark getter for batch verification with specific Times constraint |
| `MarkVerifiableSet()` | Mark setter for batch verification with default constraint (AtLeastOnce) |
| `MarkVerifiableSet(Times)` | Mark setter for batch verification with specific Times constraint |

---

## Value vs OnGet Priority

When both `Value` and `OnGet` are configured, `OnGet` takes precedence. Setting `OnGet` replaces any previously set `Value` behavior.

```csharp
[Fact]
public void OnGet_TakesPrecedenceOverValue()
{
    var stub = new ConfigStub();

    // Set a static value
    stub.Name.Value = "initial";

    // Then set OnGet - it takes precedence
    stub.Name.OnGet = () => "dynamic";

    IConfig config = stub;

    // OnGet wins over Value
    Assert.Equal("dynamic", config.Name);
}
```

**Design principle:** This allows upgrading from simple Value configuration to dynamic OnGet behavior without removing the Value assignment first.

---

## Resetting Property Interceptors

Calling `Reset()` on a property interceptor clears tracking state and callbacks but **preserves the Value**.

```csharp
[Fact]
public void Reset_ClearsCountsButPreservesValue()
{
    var stub = new ConfigStub();

    stub.Name.Value = "test";

    IConfig config = stub;

    // Access property to increment counts
    _ = config.Name;
    config.Name = "updated";

    stub.Name.VerifyGet(Times.AtLeastOnce);
    stub.Name.VerifySet(Times.AtLeastOnce);

    // Reset clears counts and callbacks
    stub.Name.Reset();

    stub.Name.VerifyGet(Times.Never);
    stub.Name.VerifySet(Times.Never);
    // Note: Reset clears OnGet and OnSet but preserves Value
}
```

### Reset Behavior Summary

| Clears | Preserves |
|--------|-----------|
| Tracking state (get/set counts) | `Value` |
| `LastSetValue` | |
| `OnGet` | |
| `OnSet` | |

**Key Principle:** Reset() clears tracking and callbacks, but preserves state that represents "what the stub currently is" (the Value).

---

## Complete Example

This example demonstrates all property configuration approaches in a realistic test scenario:

```csharp
public interface IUserConfigComplete
{
    User CurrentUser { get; }
    bool IsConnected { get; }
    string ConnectionString { get; set; }
    void Connect();
}

[KnockOff]
public partial class UserConfigCompleteStub : IUserConfigComplete { }

[Fact]
public void CompletePropertyExample_AllConfigurationApproaches()
{
    var stub = new UserConfigCompleteStub();

    // Track connection state with local variable
    var isConnected = false;

    // Value: Static test data
    stub.CurrentUser.Value = new User { Id = 1, Name = "Alice" };

    // OnGet: State-dependent behavior using tracked state
    stub.IsConnected.OnGet = () => isConnected;

    // OnSet: Track all values written
    var connectionStrings = new List<string>();
    stub.ConnectionString.OnSet = (value) => connectionStrings.Add(value);

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

---

## Decision Guide

Choose your configuration approach based on the test scenario:

| Scenario | Use This | Example |
|----------|----------|---------|
| Property should return fixed test data | `Value` | `stub.UserId.Value = 42;` |
| Property should return current time/random value | `OnGet` | `stub.Now.OnGet = () => DateTime.UtcNow;` |
| Property depends on other stub state | `OnGet` | `stub.IsReady.OnGet = () => isInitialized;` |
| Track all values written to property | `OnSet` | `stub.Name.OnSet = (v) => list.Add(v);` |
| Simulate validation in dependency | `OnSet` | `stub.Age.OnSet = (v) => Validate(v);` |
| Verify property was accessed N times | `VerifyGet` | `stub.UserId.VerifyGet(Times.Exactly(2));` |
| Verify last value written | `LastSetValue` | `Assert.Equal("x", stub.Name.LastSetValue);` |

---

## API Summary

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Value` | `T` | Backing value returned by property getter |
| `LastSetValue` | `T` | The value from the most recent setter call |
| `OnGet` | `Func<T>` | Callback invoked when the property is read |
| `OnSet` | `Action<T>` | Callback invoked when the property is written |

### Verification Methods

| Method | Description |
|--------|-------------|
| `VerifyGet()` | Verify property getter was called at least once |
| `VerifyGet(Times)` | Verify property getter was called according to Times constraint |
| `VerifySet()` | Verify property setter was called at least once |
| `VerifySet(Times)` | Verify property setter was called according to Times constraint |
| `MarkVerifiableGet()` | Mark getter for batch verification (AtLeastOnce) |
| `MarkVerifiableGet(Times)` | Mark getter for batch verification with specific constraint |
| `MarkVerifiableSet()` | Mark setter for batch verification (AtLeastOnce) |
| `MarkVerifiableSet(Times)` | Mark setter for batch verification with specific constraint |

### Methods

| Method | Description |
|--------|-------------|
| `Reset()` | Clears tracking, `LastSetValue`, `OnGet`, `OnSet`. Preserves `Value` |
