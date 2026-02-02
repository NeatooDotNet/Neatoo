---
name: KnockOff Usage
description: This skill should be used when the user asks about "KnockOff stubs", "create a stub", "mock with KnockOff", "[KnockOff] attribute", "[KnockOff<T>] attribute", "OnCall", "Returns", "OnGet", "OnSet", "setup stub behavior", "Verify calls", "Verifiable", "VerifyAll", "track method calls", "stub patterns", "Stand-Alone pattern", "Inline Interface", "Inline Class", "Inline Delegate", "stub a delegate", "migrate from Moq", "KnockOff async", "interceptor API", "Strict mode", "Strict()", "assembly-wide strict", "[assembly: KnockOffStrict]", "ThenCall", "ThenGet", "ThenSet", ".Of<T>()", "generic method interceptor", "Source() delegation", "When()", "argument matching", or needs guidance on creating, configuring, or verifying KnockOff test stubs.
version: 2.0.0
---

# KnockOff Usage Guide

KnockOff is a Roslyn Source Generator that creates test stubs at compile time. Stubs are reusable, have zero reflection overhead, and provide compile-time safety.

## CRITICAL BEHAVIORAL GOTCHAS

**Read this section first to avoid common mistakes.**

### 1. Sequences EXHAUST - They Do NOT Repeat

Sequences return `default` after all callbacks are consumed:

```cs
stub.Add.OnCall((a, b) => 1).ThenCall((a, b) => 999);
calc.Add(0, 0); // Returns 1
calc.Add(0, 0); // Returns 999
calc.Add(0, 0); // Returns 0 (default - EXHAUSTED, not 999!)
```

### 2. Events Use Handler Property - No Raise() Method

Events are raised via the `Handler` property with null-conditional:

```cs
// WRONG: stub.StartedInterceptor.Raise(sender, args)
// RIGHT:
stub.StartedInterceptor.Handler?.Invoke(sender, EventArgs.Empty);
```

### 3. Event Interceptors Have "Interceptor" Suffix

```cs
stub.StartedInterceptor  // NOT stub.Started
stub.DataReceivedInterceptor  // NOT stub.DataReceived
```

### 4. Class Stubs Use .Object Property

Inline class stubs don't inherit from the base class:

```cs
// WRONG: ServiceBase service = stub;
// RIGHT:
ServiceBase service = stub.Object;
service.Initialize();
```

### 5. Closed Generic Stubs Use Simple Names

```cs
// For [KnockOff<IRepository<User>>]:
new Stubs.IRepository()  // NOT Stubs.IRepository<User>
```

### 6. Times.Between() Does NOT Exist

```cs
// WRONG: Times.Between(1, 5)
// RIGHT: Use separate constraints
stub.Save.Verify(Times.AtLeast(1));
stub.Save.Verify(Times.AtMost(5));
```

### 7. Returns() vs OnCall() - Mutual Exclusivity

`Returns()` and `OnCall()` are mutually exclusive. Last one wins:

```cs
stub.GetValue.Returns("fixed");           // Sets constant value
stub.GetValue.OnCall((id) => $"val-{id}"); // REPLACES Returns, now dynamic
```

### 8. OnSet Does NOT Auto-Update Getter

```cs
stub.Name.OnSet((v) => { /* tracks value */ });
service.Name = "test";
// Getter still returns default! OnSet doesn't update OnGet
// To link them: stub.Name.OnSet((v) => stub.Name.OnGet(v));
```

### 9. Reset() Clears Tracking BUT Preserves Some State

| Interceptor | Reset Clears | Reset Preserves |
|-------------|--------------|-----------------|
| Method | Tracking, callbacks | Nothing |
| Property | Tracking, LastSetValue, callbacks | Verifiable flag |
| Indexer | Tracking, LastGetKey, LastSetEntry | **Backing dictionary** |
| Event | Tracking counts | **Active subscribers** |

---

## Pattern Selection

| Need | Pattern | Instantiation |
|------|---------|---------------|
| Reusable stub across files | Stand-Alone | `new MyStub()` |
| Custom methods on stub | Stand-Alone | `new MyStub()` |
| Quick test-local stub | Inline Interface | `new Stubs.IService()` |
| Stub a class (virtual/abstract) | Inline Class | `new Stubs.MyClass()` then `.Object` |
| Stub a delegate | Inline Delegate | `new Stubs.MyDelegate()` |

### Stand-Alone Pattern

```cs
[KnockOff]
public partial class UserRepoStub : IUserRepo { }

// Usage:
var stub = new UserRepoStub();
stub.GetById.OnCall((id) => new User { Id = id }).Verifiable();
IUserRepo repo = stub;
```

### Inline Interface Pattern

```cs
[KnockOff<IEmailService>]
public partial class EmailTests
{
    [Fact]
    public void Test()
    {
        var stub = new Stubs.IEmailService();
        stub.Send.OnCall((to, subj) => true).Verifiable();
        IEmailService email = stub;
    }
}
```

### Inline Class Pattern

```cs
[KnockOff<DataServiceBase>]
public partial class DataTests
{
    [Fact]
    public void Test()
    {
        var stub = new Stubs.DataServiceBase();
        stub.GetData.OnCall((id) => "test").Verifiable();
        DataServiceBase service = stub.Object;  // Use .Object!
    }
}
```

### Inline Delegate Pattern

```cs
[KnockOff<ValidationRule>]  // delegate bool ValidationRule(string value);
public partial class ValidationTests
{
    [Fact]
    public void Test()
    {
        var stub = new Stubs.ValidationRule();
        stub.Interceptor.OnCall((val) => val != "invalid");
        ValidationRule rule = stub;  // Implicit conversion
    }
}
```

---

## Method Configuration

### Returns() - Fixed Values

```cs
stub.GetUser.Returns(new User { Id = 1, Name = "Alice" });
```

### OnCall() - Dynamic Callbacks

```cs
// With arguments
stub.GetUser.OnCall((id) => new User { Id = id, Name = $"User{id}" });

// Void methods
stub.Save.OnCall((user) => { /* side effects */ });

// Async methods - auto-wrapped, no Task.FromResult needed
stub.GetUserAsync.OnCall((id) => new User { Id = id });  // Returns Task<User>
stub.SaveAsync.OnCall((user) => { });  // Returns Task.CompletedTask
```

### Sequences with ThenCall()

```cs
stub.GetNext
    .OnCall(() => "first")
    .ThenCall(() => "second")
    .ThenCall(() => "third");
// After third call, returns default (exhausts)
```

### When() - Argument Matching

```cs
// Value matching
stub.GetUser.When(42).Returns(adminUser);
stub.GetUser.When(1).Returns(regularUser);

// Predicate matching
stub.GetUser.When(id => id < 0).Returns(null);

// Chaining
stub.GetUser
    .When(42).Returns(admin)
    .ThenWhen(id => id > 100).Returns(premium)
    .ThenWhen(id => id > 0).Returns(regular);

// Void methods use Callback instead of Returns
stub.Log.When("error").Callback((msg) => errorCount++);
```

---

## Property Configuration

```cs
// Static value
stub.Name.OnGet("TestName");

// Dynamic callback
stub.Timestamp.OnGet(() => DateTime.UtcNow);

// Setter interception
stub.Name.OnSet((value) => capturedValues.Add(value));

// Sequences
stub.Counter.OnGet(() => 1).ThenGet(() => 2).ThenGet(() => 3);
```

---

## Indexer Configuration

```cs
// Use Backing dictionary for simple cases
stub.Indexer.Backing["key1"] = value1;
stub.Indexer.Backing["key2"] = value2;

// Or use callbacks
stub.Indexer.OnGet((key) => ComputeValue(key));
stub.Indexer.OnSet((key, value) => { /* handle */ });

// Note: OnGet/OnSet override Backing - they don't work together
```

---

## Event Configuration

```cs
// Events use Handler property (NO Raise method!)
stub.DataReceivedInterceptor.Handler?.Invoke(sender, new DataEventArgs(data));

// Verify subscriptions
stub.DataReceivedInterceptor.VerifyAdd(Times.Once);
stub.DataReceivedInterceptor.VerifyRemove(Times.Never);
```

---

## Generic Methods

```cs
// Use .Of<T>() for type-specific configuration
stub.GetById.Of<User>().OnCall((id) => new User { Id = id });
stub.GetById.Of<Product>().OnCall((id) => new Product { Id = id });

// Verify by type
stub.GetById.Of<User>().Verify(Times.Exactly(2));
stub.GetById.Of<Product>().Verify(Times.Once);
```

---

## Verification

### Individual Verification

```cs
var tracking = stub.Save.OnCall((user) => { });
// ... exercise stub ...
tracking.Verify(Times.Exactly(2));
```

### Batch Verification with Verifiable()

```cs
stub.GetUser.OnCall((id) => user).Verifiable();
stub.Save.OnCall((u) => { }).Verifiable(Times.Once);
// ... exercise stub ...
stub.Verify();  // Checks all Verifiable() members
```

### Verify() vs VerifyAll()

- `stub.Verify()` - Only members marked with `.Verifiable()`
- `stub.VerifyAll()` - ALL configured members (OnCall, OnGet, etc.)

### Times Constraints

| Constraint | Description |
|------------|-------------|
| `Times.Never` | Must not be called |
| `Times.Once` | Exactly 1 call |
| `Times.AtLeastOnce` | 1 or more calls |
| `Times.Exactly(n)` | Exactly n calls |
| `Times.AtLeast(n)` | n or more calls |
| `Times.AtMost(n)` | n or fewer calls |

---

## Argument Capture

```cs
// Single parameter - LastArg
var tracking = stub.GetUser.OnCall((id) => user);
service.GetUser(42);
Assert.Equal(42, tracking.LastArg);

// Multiple parameters - LastArgs tuple
var tracking = stub.Update.OnCall((id, name) => { });
service.Update(1, "Alice");
var (id, name) = tracking.LastArgs;
```

---

## Strict Mode

Throws `StubException` for unconfigured member access:

```cs
// Per-stub
[KnockOff(Strict = true)]
public partial class StrictStub : IService { }

// Or at runtime
var stub = new ServiceStub().Strict();

// Assembly-wide default
[assembly: KnockOffStrict]
```

---

## Source Delegation

Delegate unconfigured calls to a real implementation:

```cs
var stub = new RepoStub();
stub.Source(realImplementation);

// Configured members override source
stub.GetById.OnCall((id) => testUser);  // This wins over source

// Reset clears source
stub.GetById.Reset();  // Clears source AND configuration
```

**Note:** Source() only works with interface stubs, not class stubs.

---

## Moq Migration Quick Reference

| Moq | KnockOff |
|-----|----------|
| `new Mock<IFoo>()` | `new FooStub()` or `new Stubs.IFoo()` |
| `mock.Object` | `stub` (interface) or `stub.Object` (class) |
| `.Setup(x => x.Method()).Returns(val)` | `stub.Method.Returns(val)` |
| `.Setup(x => x.Method(arg)).Returns(val)` | `stub.Method.When(arg).Returns(val)` |
| `.Setup(x => x.Prop).Returns(val)` | `stub.Prop.OnGet(val)` |
| `.ReturnsAsync(val)` | `stub.Method.Returns(val)` (auto-wraps) |
| `.Callback(action)` | Logic inside `OnCall` callback |
| `.Verify(x => x.Method(), Times.Once)` | `tracking.Verify(Times.Once)` |
| `.Verifiable()` + `mock.Verify()` | `.Verifiable()` + `stub.Verify()` |
| `It.IsAny<T>()` | Callback always receives all args |
| `It.Is<T>(pred)` | `stub.Method.When(pred).Returns(val)` |

---

## Common Mistakes

### Missing `partial` Keyword

```cs
// WRONG: Compilation errors
[KnockOff]
public class FooStub : IFoo { }

// RIGHT:
[KnockOff]
public partial class FooStub : IFoo { }
```

### Wrong Callback Signature

```cs
// WRONG: Type mismatch
stub.Process.OnCall((string id) => { });  // Method takes int

// RIGHT: Match signature exactly
stub.Process.OnCall((int id) => { });
```

### Forgetting .Object for Class Stubs

```cs
// WRONG:
MyClass service = stub;  // Won't compile

// RIGHT:
MyClass service = stub.Object;
```

### Using Func<>/Action<> Instead of Named Delegates

```cs
// WRONG: KnockOff doesn't support generic delegates
[KnockOff<Func<int, string>>]  // Won't work

// RIGHT: Define a named delegate
public delegate string MyOperation(int value);
[KnockOff<MyOperation>]
```

### Expecting Sequences to Repeat

```cs
// WRONG assumption: Last value repeats
stub.GetNext.OnCall(() => 1).ThenCall(() => 2);
// After 2 calls, returns 0 (default), NOT 2

// RIGHT: If you need infinite, use OnCall with state
var counter = 0;
stub.GetNext.OnCall(() => ++counter);
```

---

## Reference Documentation

For detailed documentation, see the reference files in `references/`:

- **`references/patterns.md`** - Complete pattern guide with examples
- **`references/methods.md`** - Method configuration and verification
- **`references/properties.md`** - Property interceptors
- **`references/api-reference.md`** - Complete API reference
- **`references/strict-mode.md`** - Strict mode configuration
- **`references/moq-migration.md`** - Migration guide

---

**UPDATED:** 2026-02-01
