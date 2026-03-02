---
name: knockoff
description: >-
  This skill should be used when the user asks about KnockOff stubs, creating stubs, mocking with KnockOff, KnockOff attributes, Return, Call, Get, Set, setup stub behavior, Verify calls, Verifiable, VerifyAll, track method calls, stub patterns, Stand-Alone pattern, Inline Interface, Inline Class, Inline Delegate, stub a delegate, migrate from Moq, KnockOff async, interceptor API, Strict mode, ThenReturn, ThenCall, ThenGet, ThenSet, generic method interceptor, Source() delegation, When(), argument matching, or needs guidance on creating, configuring, or verifying KnockOff test stubs. When writing tests that need stubs, this skill MUST be consulted to check for existing stubs before creating new inline stubs.
---

# KnockOff Usage Guide

KnockOff is a Roslyn Source Generator that creates test stubs at compile time. Stubs are reusable, have zero reflection overhead, and provide compile-time safety.

## Architecture

Each method on a stubbed interface/class gets a **fully generated interceptor class** (e.g., `AddInterceptor`) that inherits from `MethodInterceptorRuntime`. These interceptors provide:
- Clean IntelliSense with XML documentation showing original method signatures and parameter names
- Typed `Call`/`Return`/`When` methods with custom named delegates for parameter IntelliSense
- `Verify()`, `LastArg`/`LastArgs`, `Reset()`, `Verifiable()` directly on the interceptor

**Callback parameter conventions:**
- **0 params:** `() => ...`
- **1 param:** Raw type, you name the lambda parameter: `(int id) => ...`
- **2+ params:** Custom named delegate with typed params: `(int a, int b) => a + b`
- **ref/out:** Custom delegate with ref/out modifiers: `(ref int a) => { a = a + 1; }`

**Overloaded methods** use a single interceptor property with overloaded `Call`/`When` methods. The lambda signature disambiguates which overload is configured. `Call()` returns a tracking handle for per-overload `Verify()`/`LastArg`/`LastArgs`.

## CRITICAL GOTCHAS

### 1. Sequences REPEAT Last Value After Exhaustion

<!-- snippet: skill-gotcha-sequence-exhaustion -->
```cs
stub.Add.Return(1, 999);
calc.Add(0, 0); // Returns 1
calc.Add(0, 0); // Returns 999
calc.Add(0, 0); // Returns 999 (repeats last value!)

// Use ThenDefault() to return default(T) instead of repeating
stub.Add.Return(1, 999).ThenDefault();
calc.Add(0, 0); // Returns 1
calc.Add(0, 0); // Returns 999
calc.Add(0, 0); // Returns 0 (default - ThenDefault() terminates with default)
```
<!-- endSnippet -->

### 2. Events Use Raise() and Bare Names

<!-- snippet: skill-gotcha-event-raise -->
```cs
// Events use .Raise() method:
stub.Started.Raise(stub, EventArgs.Empty);
```
<!-- endSnippet -->

<!-- snippet: skill-gotcha-event-naming -->
```cs
// Event interceptors use the event name directly:
stub.Started.VerifyAdd(Called.Never);
stub.DataReceived.VerifyAdd(Called.Never);
```
<!-- endSnippet -->

### 3. Class Stubs Call Base by Default + Use .Object

Class stubs (Patterns 3, 4, 6, 9) call base for unconfigured virtual methods (like Moq's `CallBase = true`, but default). Abstract methods return `default(T)`.

<!-- snippet: skill-gotcha-class-object -->
```cs
// WRONG: ServiceBase service = stub;
// RIGHT:
var stub = new Stubs.ServiceBase();
ServiceBase service = stub.Object;
service.Initialize();
```
<!-- endSnippet -->

### 4. Closed Generic Stubs Use Simple Names

<!-- snippet: skill-gotcha-closed-generic -->
```cs
// For [KnockOff<IRepository<User>>]:
var stub = new Stubs.IRepository();  // NOT Stubs.IRepository<User>
```
<!-- endSnippet -->

### 5. Called.Between() Does NOT Exist

<!-- snippet: skill-gotcha-times-between -->
```cs
// WRONG: Called.Between(1, 5)
// RIGHT: Use separate constraints
stub.Save.Verify(Called.AtLeast(1));
stub.Save.Verify(Called.AtMost(5));
```
<!-- endSnippet -->

### 6. Configuration — Last One Wins

All configuration methods use direct replacement. Calling any config method replaces the previous one of the same kind. **Known bug:** `.When()` currently accumulates like `.ThenWhen()` instead of replacing.

### 7. Set Does NOT Auto-Update Getter

<!-- snippet: skill-gotcha-onset-no-auto-update -->
```cs
stub.Name.Set((v) => { /* tracks value */ });
service.Name = "test";
// Getter still returns default! Set doesn't update Get
// To link them: stub.Name.Set((v) => stub.Name.Get(v));
```
<!-- endSnippet -->

### 8. Reset() Clears Tracking BUT Preserves Config

Reset clears counts, captured args, sequence position, source. Reset preserves Return/Call/Get/Set callbacks, sequence structure, Verifiable marking.

---

## PROACTIVE: Detect Duplicate Inline Stubs

Before creating an inline stub with `[KnockOff<T>]`, **always search for existing stubs** of that type. If the same type is already stubbed inline elsewhere, recommend creating a standalone stub.

---

## Pattern Selection

| Need | Pattern | Instantiation |
|------|---------|---------------|
| Reusable stub across files | Standalone | `new MyStub()` |
| Custom methods on stub | Standalone | `new MyStub()` |
| Generic stub with type params | Generic Standalone | `new MyStub<T>()` |
| Quick test-local stub | Inline Interface | `new Stubs.IService()` |
| Stub a class (virtual/abstract) | Inline Class | `new Stubs.MyClass()` then `.Object` |
| Stub a delegate | Inline Delegate | `new Stubs.MyDelegate()` |
| Test-local generic interface | Open Generic | `new Stubs.IFoo<T>()` |

### Standalone Pattern

<!-- snippet: skill-standalone-pattern -->
```cs
[KnockOff]
public partial class SkillUserRepoStub : ISkillUserRepo { }
```
<!-- endSnippet -->

<!-- snippet: skill-standalone-usage -->
```cs
[Fact]
public void StandaloneStub_ConfigureAndVerify()
{
    var stub = new SkillUserRepoStub();
    stub.GetById.Call((id) => new User { Id = id }).Verifiable();
    stub.Save.Call((user) => { }).Verifiable();
    ISkillUserRepo repo = stub;

    var user = repo.GetById(42);
    repo.Save(user!);

    stub.Verify();
}
```
<!-- endSnippet -->

### Inline Interface / Class / Delegate

<!-- snippet: skill-inline-interface-pattern -->
```cs
[KnockOff<ISkillEmailService>]
public partial class SkillEmailTests
{
    [Fact]
    public void Test()
    {
        var stub = new Stubs.ISkillEmailService();
        stub.Send.Call((string to, string subject) => true).Verifiable();
        ISkillEmailService email = stub;
    }
}
```
<!-- endSnippet -->

**Class stubs** use `.Object`: `ServiceBase service = stub.Object;`

**Delegate stubs** use `stub.Interceptor` for config: `stub.Interceptor.Return(42);`

---

## Method Configuration

<!-- snippet: skill-method-returns -->
```cs
stub.GetUser.Return(new User { Id = 1, Name = "Alice" });
```
<!-- endSnippet -->

<!-- snippet: skill-method-oncall -->
```cs
// With arguments
stub.GetUser.Call((id) => new User { Id = id, Name = $"User{id}" });

// Void methods
stub.Save.Call((user) => { /* side effects */ });

// Async methods - auto-wrapped, no Task.FromResult needed
stub.GetUserAsync.Call((id) => new User { Id = id });  // Returns Task<User>
stub.SaveAsync.Call((user) => { });  // Returns Task.CompletedTask
```
<!-- endSnippet -->

<!-- snippet: skill-method-sequences -->
```cs
// Concise value sequences (preferred)
stub.GetNext.Return(1, 2, 3);
// After third call, repeats 3 (NSubstitute-like behavior)

// Mix callbacks with value sequences
stub.Add.Call((int a, int b) => a + b).ThenReturn(100, 200);
// First: computed, then 100, 200, 200...

// Use ThenDefault() to return default(T) instead of repeating:
stub.GetNext.Return(1, 2).ThenDefault();
```
<!-- endSnippet -->

<!-- snippet: skill-method-when -->
```cs
// Value matching
stub.GetUser.When(42).Return(adminUser);
stub.GetUser.When(1).Return(regularUser);

// Predicate matching
stub.GetUser.When(id => id < 0).Return(null);

// Chaining
stub.GetUser
    .When(42).Return(adminUser)
    .ThenWhen(id => id > 100).Return(premiumUser)
    .ThenWhen(id => id > 0).Return(regularUser);

// Void methods use Call instead of Return
stub.Log.When("error").Call((msg) => { /* handle */ });
```
<!-- endSnippet -->

---

## Property Configuration

<!-- snippet: skill-property-config -->
```cs
// Static value
stub.Name.Get("TestName");

// Dynamic callback
stub.Timestamp.Get(() => DateTime.UtcNow);

// Setter interception
stub.Name.Set((value) => capturedValues.Add(value));

// Sequences
stub.Counter.Get(() => 1).ThenGet(() => 2).ThenGet(() => 3);
```
<!-- endSnippet -->

---

## Indexer Configuration

<!-- snippet: skill-indexer-config -->
```cs
// Use per-key Returns for specific keys
stub.Indexer["key1"].Returns("value1");
stub.Indexer["key2"].Returns("value2");

// Or use callbacks as fallback for unconfigured keys
stub.Indexer.Get((key) => $"computed-{key}");
stub.Indexer.Set((key, value) => { /* handle */ });

// When(predicate) matches keys by condition
stub.Indexer.When(key => key.StartsWith("prefix_", StringComparison.Ordinal)).Returns("matched");

// Per-key > When > Get callback (priority order)
```
<!-- endSnippet -->

---

## Event Configuration

<!-- snippet: skill-event-config -->
```cs
// Events use Raise() method
stub.DataReceived.Raise(stub, new DataEventArgs("test-data"));

// Verify subscriptions
stub.DataReceived.VerifyAdd(Called.Once);
stub.DataReceived.VerifyRemove(Called.Never);
```
<!-- endSnippet -->

---

## Generic Methods

<!-- snippet: skill-generic-methods -->
```cs
// Use .Of<T>() for type-specific configuration
stub.GetById.Of<User>().Call((id) => new User { Id = id });
stub.GetById.Of<Product>().Call((id) => new Product { Id = id });

// Verify by type
stub.GetById.Of<User>().Verify(Called.Never);
stub.GetById.Of<Product>().Verify(Called.Never);
```
<!-- endSnippet -->

---

## Delegate Configuration

Delegates use `stub.Interceptor`. Named delegates only (no `Func<>`/`Action<>`). See **delegates.md** for full reference.

<!-- snippet: skill-delegate-config -->
```cs
var stub = new Stubs.SkillArithmeticOp();

// Returns (value or callback)
stub.Interceptor.Return(42);
stub.Interceptor.Call((a, b) => a + b);

// Sequences
stub.Interceptor.Return(10, 20, 30);

// When chains
stub.Interceptor.When(1, 2).Return(100)
    .ThenWhen(3, 4).Return(200);

// Async auto-wrapping (for delegates returning Task<T>)
// stub.Interceptor.Return(42);              // auto-wraps in Task.FromResult
// stub.Interceptor.Call((int x) => x * 2); // simplified, auto-wrapped

// Verification (fresh stub for clean tracking)
var verifyStub = new Stubs.SkillArithmeticOp();
verifyStub.Interceptor.Call((a, b) => a + b);
SkillArithmeticOp op = verifyStub;
op(1, 2);
verifyStub.Interceptor.Verify(Called.Once);
Assert.Equal((1, 2), verifyStub.Interceptor.LastArgs);

// Strict mode
stub.Strict = true;

// Implicit conversion to delegate type
SkillArithmeticOp opRef = stub;
```
<!-- endSnippet -->

---

## Verification

`stub.Verify()` checks `.Verifiable()` members. `stub.VerifyAll()` checks ALL configured members. See **verification.md** for full reference.

<!-- snippet: skill-verify-batch -->
```cs
stub.GetUser.Call((id) => new User { Id = id }).Verifiable();
stub.Save.Call((u) => { }).Verifiable(Called.Once);
// ... exercise stub ...
```
<!-- endSnippet -->

Called constraints: `Called.Never`, `Called.Once`, `Called.AtLeastOnce`, `Called.Exactly(n)`, `Called.AtLeast(n)`, `Called.AtMost(n)`

---

## Argument Capture

<!-- snippet: skill-arg-capture -->
```cs
// Single parameter - LastArg
var getTracking = stub.GetUser.Call((id) => new User { Id = id });
service.GetUser(42);
Assert.Equal(42, getTracking.LastArg);

// Multiple parameters - LastArgs tuple
var updateTracking = stub.Update.Call((int id, string name) => { });
service.Update(1, "Alice");
var (id, name) = updateTracking.LastArgs;
```
<!-- endSnippet -->

---

## Strict Mode

Throws `StubException` for unconfigured member access:

<!-- snippet: skill-strict-mode -->
```cs
// Per-stub
// [KnockOff(Strict = true)]
// public partial class StrictStub : IService { }

// Or at runtime
var stub = new SvcStub();
stub.Strict();

// Assembly-wide default
// [assembly: KnockOffStrict]
```
<!-- endSnippet -->

---

## Stub Overrides (Standalone Only)

Override `protected virtual` methods/properties with underscore suffix for reusable defaults. `Return()`/`Call()`/`Get()`/`Set()` supersede overrides per-test. See **stub-overrides.md** for full reference.

<!-- snippet: skill-stub-override-define -->
```cs
[KnockOff]
public partial class SkStubOverrideRepoStub : IUserRepo { }

public partial class SkStubOverrideRepoStub
{
    // Override virtual method with underscore suffix - compiler enforces signature!
    protected override User? GetById_(int id) => new User { Id = id, Name = "Default" };
}
```
<!-- endSnippet -->

`Call()` supersedes the override: `stub.GetById.Call(id => new User { Id = id, Name = "Override" });`

---

## Source Delegation (Interface Stubs Only)

`stub.Source(realImpl)` delegates unconfigured calls to a real implementation. See **source-delegation.md** for hierarchy support and details.

<!-- snippet: skill-source-delegation -->
```cs
var stub = new SkSourceDelegationStub();
stub.Source(realImplementation);

// Configured members override source
stub.GetById.Call((id) => testUser);  // This wins over source

// Reset clears tracking (counts, args, sequence position) and source delegation
// but preserves callbacks (Return, Returns, Get, Set)
// stub.GetById.Reset();
```
<!-- endSnippet -->

---

## Moq Migration Quick Reference

| Moq | KnockOff |
|-----|----------|
| `new Mock<IFoo>()` | `new FooStub()` or `new Stubs.IFoo()` |
| `mock.Object` | `stub` (interface) or `stub.Object` (class) |
| `.Setup(x => x.Method()).Returns(val)` | `stub.Method.Return(val)` |
| `.Setup(x => x.Method(arg)).Returns(val)` | `stub.Method.When(arg).Return(val)` |
| `.Setup(x => x.Prop).Returns(val)` | `stub.Prop.Get(val)` |
| `.ReturnsAsync(val)` | `stub.Method.Return(val)` (auto-wraps) |
| `.Callback(action)` | Logic inside `Call` callback |
| `mock.CallBase = true` | Default for class stubs |
| `.Verify(x => x.Method(), Times.Once)` | `tracking.Verify(Called.Once)` |
| `.Verifiable()` + `mock.Verify()` | `.Verifiable()` + `stub.Verify()` |
| `It.IsAny<T>()` | Callback always receives all args |
| `It.Is<T>(pred)` | `stub.Method.When(pred).Return(val)` |

---

## Common Mistakes

### Missing `partial` Keyword

<!-- snippet: skill-mistake-partial -->
```cs
// WRONG: Compilation errors
// [KnockOff]
// public class FooStub : IFoo { }

// RIGHT:
[KnockOff]
public partial class SkillPartialDemoStub : ISvc { }
```
<!-- endSnippet -->

### Wrong Callback Signature

<!-- snippet: skill-mistake-wrong-signature -->
```cs
// WRONG: Type mismatch
// stub.Process.Call((string id) => { });  // Method takes int

// RIGHT: Match signature exactly
stub.Process.Call((int id) => { });
```
<!-- endSnippet -->

### Forgetting .Object for Class Stubs

<!-- snippet: skill-mistake-forgetting-object -->
```cs
// WRONG:
// MyClass service = stub;  // Won't compile

// RIGHT:
var stub = new Stubs.ServiceBase();
ServiceBase service = stub.Object;
```
<!-- endSnippet -->

### Using Func<>/Action<> Instead of Named Delegates

<!-- snippet: skill-mistake-func-action -->
```cs
// WRONG: KnockOff doesn't support generic delegates
// [KnockOff<Func<int, string>>]  // Won't work

// RIGHT: Define a named delegate
public delegate string SkillNamedOperation(int value);
[KnockOff<SkillNamedOperation>]
public partial class SkillNamedDelegateHost { }
```
<!-- endSnippet -->

---

## Reference Documentation

Load these on demand for detailed coverage of specific topics:

### Member Types
- **`references/methods.md`** — Method configuration, ref/out params, overloads, argument capture
- **`references/properties.md`** — Property Get/Set, LastSetValue, decision guide
- **`references/indexers.md`** — Per-key builders, all-keys callbacks, multi-param indexers, priority chain
- **`references/events.md`** — Raise() signatures, HasSubscribers, VerifyAdd/VerifyRemove
- **`references/delegates.md`** — Named delegate stubs, Interceptor access, implicit conversion

### Cross-Cutting Features
- **`references/sequences.md`** — Method/property/indexer sequences, ThenReturn, ThenDefault, exhaustion
- **`references/when-chains.md`** — Value matching, predicate matching, ThenWhen, first-match-wins
- **`references/verification.md`** — Verify/Verifiable/VerifyAll, Called constraints, VerificationException
- **`references/async-methods.md`** — Three-tier auto-wrapping for Task<T>/ValueTask<T>
- **`references/generic-methods.md`** — Of<T>() pattern, CalledTypeArguments, multi-type params

### Advanced Features
- **`references/stub-overrides.md`** — Protected override methods/properties, underscore convention
- **`references/source-delegation.md`** — Interface hierarchy, partial stubbing, priority chain
- **`references/strict-mode.md`** — Per-stub, runtime, assembly-wide strict configuration

### Guides
- **`references/patterns.md`** — Complete guide to all 9 stub patterns
- **`references/moq-migration.md`** — Comprehensive Moq-to-KnockOff migration guide
