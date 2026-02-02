[knockoff-usage](../SKILL.md) > [references](README.md) > moq-migration

# Migrating from Moq to KnockOff

Switching from Moq to KnockOff means moving from per-test mock setup to reusable stub classes. You gain the ability to share stubs across tests while still customizing behavior per-test—while trading Moq's runtime flexibility for source-generated, explicit stub implementations.

This guide walks you through the migration step-by-step, with side-by-side comparisons and a complete before/after example.

---

## What Changes

**Moq's approach:**
- Runtime reflection with fluent `.Setup()` API
- `Mock<T>` wrapper objects
- `.Object` property to access the instance
- `.Verify()` methods for call assertions

**KnockOff's approach:**
- Compile-time source generation with partial classes
- Direct stub classes with `[KnockOff<T>]` attribute
- Interceptor properties for configuration and verification
- Standard assertions on call tracking properties

**What stays the same:**
- You still create test doubles for interfaces and classes
- You still configure behavior and verify calls
- Your test goals and patterns remain unchanged

---

## Quick Reference

| Moq Pattern | KnockOff Equivalent |
|-------------|---------------------|
| `new Mock<IFoo>()` | `new FooStub()` with `[KnockOff<IFoo>] partial class FooStub` |
| `mock.Object` | `stub` (direct) or `stub.Object` (class stubs only) |
| `.Setup(x => x.Method()).Returns(value)` | `stub.Method.Returns(value)` |
| `.Setup(x => x.Method(arg)).Returns(val)` | `stub.Method.When(arg).Returns(val)` |
| `.Setup(x => x.Property).Returns(value)` | `stub.Property.OnGet(value)` |
| `.ReturnsAsync(value)` | `stub.Method.Returns(value)` (auto-wraps) |
| `.Callback(x => ...)` | Logic in `OnCall` delegate |
| `.Verify(x => x.Method(), Times.Once)` | `tracking.Verify(Times.Once)` |
| `.Verifiable()` | `.Verifiable()` then `stub.Verify()` |
| `mock.Verify()` | `stub.Verify()` |
| `It.IsAny<T>()` | Callback receives all args |
| `It.Is<T>(pred)` | `stub.Method.When(pred).Returns(val)` |

---

## Step 1: Install KnockOff

Replace the Moq package with KnockOff.

```bash
# Remove Moq:
dotnet remove package Moq

# Add KnockOff:
dotnet add package KnockOff
```

---

## Step 2: Create Stubs

Replace `Mock<T>` instances with KnockOff stub classes.

**Moq:**

<!-- snippet: moq-migration-create-stub-moq -->
```cs
[Fact]
public void CreateStub_MoqApproach()
{
    var mock = new Mock<IMoqUserRepo>();
    IMoqUserRepo repository = mock.Object;

    Assert.NotNull(repository);
}
```
<!-- endSnippet -->

**KnockOff:**

<!-- snippet: moq-migration-stub-declaration -->
```cs
[KnockOff]
public partial class MoqUserRepoStub : IMoqUserRepo { }
```
<!-- endSnippet -->

<!-- snippet: moq-migration-create-stub-knockoff -->
```cs
[Fact]
public void CreateStub_KnockOffApproach()
{
    var stub = new MoqUserRepoStub();
    IMoqUserRepo repository = stub;

    Assert.NotNull(repository);
}
```
<!-- endSnippet -->

**Key differences:**
- Moq creates wrapper objects at runtime
- KnockOff requires a partial class declaration—the generator fills in the implementation
- You use the stub instance directly (no `.Object` property)

---

## Step 3: Configure Methods

Replace `.Setup().Returns()` with `OnCall` property assignments.

**Moq:**

<!-- snippet: moq-migration-setup-method-moq -->
```cs
[Fact]
public void SetupMethod_MoqApproach()
{
    var mock = new Mock<IMoqUserRepo>();
    var testUser = new User { Id = 42, Name = "Alice" };

    mock.Setup(x => x.GetUser(It.IsAny<int>())).Returns(testUser);

    IMoqUserRepo repository = mock.Object;
    var user = repository.GetUser(42);

    Assert.NotNull(user);
    Assert.Equal("Alice", user.Name);
}
```
<!-- endSnippet -->

**KnockOff:**

<!-- snippet: moq-migration-setup-method-knockoff -->
```cs
[Fact]
public void SetupMethod_KnockOffApproach()
{
    var stub = new MoqUserRepoStub();
    var testUser = new User { Id = 42, Name = "Alice" };

    stub.GetUser.OnCall((id) => testUser);

    IMoqUserRepo repository = stub;
    var user = repository.GetUser(42);

    Assert.NotNull(user);
    Assert.Equal("Alice", user.Name);
}
```
<!-- endSnippet -->

**Key differences:**
- Moq uses fluent setup with expression trees
- KnockOff uses direct property assignment with typed delegates
- KnockOff's callback receives actual argument values (no `It.IsAny<T>()` needed)

---

## Step 4: Configure Properties

Replace property `.Setup().Returns()` with `OnGet()` calls.

**Moq:**

<!-- snippet: moq-migration-setup-property-moq -->
```cs
[Fact]
public void SetupProperty_MoqApproach()
{
    var mock = new Mock<IMoqUserRepo>();

    mock.Setup(x => x.ConnectionString).Returns("server=localhost");

    IMoqUserRepo repository = mock.Object;
    var connStr = repository.ConnectionString;

    Assert.Equal("server=localhost", connStr);
}
```
<!-- endSnippet -->

**KnockOff:**

<!-- snippet: moq-migration-setup-property-knockoff -->
```cs
[Fact]
public void SetupProperty_KnockOffApproach()
{
    var stub = new MoqUserRepoStub();

    stub.ConnectionString.OnGet("server=localhost");

    IMoqUserRepo repository = stub;
    var connStr = repository.ConnectionString;

    Assert.Equal("server=localhost", connStr);
}
```
<!-- endSnippet -->

**Key differences:**
- Moq treats properties like methods in setup
- KnockOff provides `OnGet()` and `OnSet()` methods on the property interceptor
- KnockOff also provides `VerifyGet()` and `VerifySet()` for separate getter/setter verification

---

## Step 5: Verify Calls

Replace Moq's `.Verify()` calls with KnockOff's `.Verify()` or `.Verifiable()` API.

**Moq:**

<!-- snippet: moq-migration-verify-moq -->
```cs
[Fact]
public void VerifyCalls_MoqApproach()
{
    var mock = new Mock<IMoqUserRepo>();

    IMoqUserRepo repository = mock.Object;
    repository.SaveUser(new User { Id = 1, Name = "Bob" });

    mock.Verify(x => x.SaveUser(It.IsAny<User>()), Moq.Times.Once());
}
```
<!-- endSnippet -->

**KnockOff:**

<!-- snippet: moq-migration-verify-knockoff -->
```cs
[Fact]
public void VerifyCalls_KnockOffApproach()
{
    var stub = new MoqUserRepoStub();

    // Mark method as verifiable during setup
    stub.SaveUser.OnCall((user) => { }).Verifiable();

    IMoqUserRepo repository = stub;
    repository.SaveUser(new User { Id = 1, Name = "Bob" });

    // Verify() checks all members marked with .Verifiable()
    stub.Verify();

    // Or verify with Times constraint directly on tracking
    // stub.SaveUser.Verify(Times.Once);
}
```
<!-- endSnippet -->

**Key differences:**
- Moq uses `mock.Verify(expression, times)` with expression trees
- KnockOff has three verification approaches:
  - `tracking.Verify(times)` on the object returned by `OnCall`
  - `stub.Method.Verify(times)` directly on the interceptor property
  - `.Verifiable()` + `stub.Verify()` for batch verification
- Both support the same `Times` matchers (Once, AtLeastOnce, Exactly, etc.)

---

## Step 6: Async Methods

Replace `.ReturnsAsync()` with value overloads (auto-wrapped) or callbacks with `Task.FromResult()`.

**Moq:**

<!-- snippet: moq-migration-async-moq -->
```cs
[Fact]
public async Task AsyncMethod_MoqApproach()
{
    var mock = new Mock<IMoqUserRepo>();
    var testUser = new User { Id = 42, Name = "Alice" };

    mock.Setup(x => x.GetUserAsync(It.IsAny<int>())).ReturnsAsync(testUser);

    IMoqUserRepo repository = mock.Object;
    var user = await repository.GetUserAsync(42);

    Assert.NotNull(user);
    Assert.Equal("Alice", user.Name);
}
```
<!-- endSnippet -->

**KnockOff:**

<!-- snippet: moq-migration-async-knockoff -->
```cs
[Fact]
public async Task AsyncMethod_KnockOffApproach()
{
    var stub = new MoqUserRepoStub();
    var testUser = new User { Id = 42, Name = "Alice" };

    stub.GetUserAsync.OnCall((id) => Task.FromResult<User?>(testUser));

    IMoqUserRepo repository = stub;
    var user = await repository.GetUserAsync(42);

    Assert.NotNull(user);
    Assert.Equal("Alice", user.Name);
}
```
<!-- endSnippet -->

**Key differences:**
- Moq provides `.ReturnsAsync()` helper
- KnockOff `Returns()` and simplified callbacks auto-wrap in `Task.FromResult()`
- Return the unwrapped type from callbacks - KnockOff handles the Task wrapping
- Only use explicit `Task.FromResult()` when your callback needs actual async operations
- For exceptions: return `Task.FromException<T>(exception)`

---

## Step 7: Callbacks

Replace `.Callback()` with logic directly in `OnCall` delegates.

**Moq:**

<!-- snippet: moq-migration-callback-moq -->
```cs
[Fact]
public void Callback_MoqApproach()
{
    var mock = new Mock<IMoqUserRepo>();
    var savedUsers = new List<User>();

    mock.Setup(x => x.SaveUser(It.IsAny<User>()))
        .Callback<User>(u => savedUsers.Add(u));

    IMoqUserRepo repository = mock.Object;
    repository.SaveUser(new User { Id = 1, Name = "Alice" });
    repository.SaveUser(new User { Id = 2, Name = "Bob" });

    Assert.Equal(2, savedUsers.Count);
    Assert.Equal("Alice", savedUsers[0].Name);
    Assert.Equal("Bob", savedUsers[1].Name);
}
```
<!-- endSnippet -->

**KnockOff:**

<!-- snippet: moq-migration-callback-knockoff -->
```cs
[Fact]
public void Callback_KnockOffApproach()
{
    var stub = new MoqUserRepoStub();
    var savedUsers = new List<User>();

    stub.SaveUser.OnCall((user) =>
    {
        savedUsers.Add(user);
    });

    IMoqUserRepo repository = stub;
    repository.SaveUser(new User { Id = 1, Name = "Alice" });
    repository.SaveUser(new User { Id = 2, Name = "Bob" });

    Assert.Equal(2, savedUsers.Count);
    Assert.Equal("Alice", savedUsers[0].Name);
    Assert.Equal("Bob", savedUsers[1].Name);
}
```
<!-- endSnippet -->

**Key differences:**
- Moq separates `.Callback()` and `.Returns()`
- KnockOff combines them in a single delegate—add logic, then return a value if needed
- You can access arguments directly by name

---

## Step 8: Argument Matching

Replace `It.IsAny<T>()` matchers with callback logic.

**Moq:**

<!-- snippet: moq-migration-arguments-moq -->
```cs
[Fact]
public void ArgumentMatching_MoqApproach()
{
    var mock = new Mock<IMoqUserRepo>();

    mock.Setup(x => x.GetUser(It.Is<int>(id => id > 0)))
        .Returns<int>(id => new User { Id = id, Name = "Valid User" });

    IMoqUserRepo repository = mock.Object;

    var validUser = repository.GetUser(1);
    var invalidUser = repository.GetUser(-1);

    Assert.NotNull(validUser);
    Assert.Null(invalidUser);
}
```
<!-- endSnippet -->

**KnockOff:**

<!-- snippet: moq-migration-arguments-knockoff -->
```cs
[Fact]
public void ArgumentMatching_KnockOffApproach()
{
    var stub = new MoqUserRepoStub();

    stub.GetUser.OnCall((id) =>
        id > 0 ? new User { Id = id, Name = "Valid User" } : null);

    IMoqUserRepo repository = stub;

    var validUser = repository.GetUser(1);
    var invalidUser = repository.GetUser(-1);

    Assert.NotNull(validUser);
    Assert.Null(invalidUser);
}
```
<!-- endSnippet -->

**Key differences:**
- Moq uses `It.IsAny<T>()` and `It.Is<T>()` for argument matching
- KnockOff callbacks receive all arguments—implement your own conditional logic
- For verification, inspect `CallHistory` to check specific argument values

---

## Complete Before/After Example

This example shows a full test class migrated from Moq to KnockOff.

### Before: Moq

<!-- snippet: moq-migration-complete-moq -->
```cs
private readonly Mock<IMoqUserRepo> _mockRepo;
private readonly UserServiceMigration _service;

public CompleteMoqTests()
{
    _mockRepo = new Mock<IMoqUserRepo>();
    _service = new UserServiceMigration(_mockRepo.Object);
}

[Fact]
public async Task GetUser_ReturnsUser()
{
    var user = new User { Id = 1, Name = "Alice" };
    _mockRepo.Setup(x => x.GetUserAsync(1)).ReturnsAsync(user);

    var result = await _service.GetUserAsync(1);

    Assert.Equal("Alice", result?.Name);
    _mockRepo.Verify(x => x.GetUserAsync(1), Moq.Times.Once());
}

[Fact]
public void SaveUser_CallsRepository()
{
    User? savedUser = null;
    _mockRepo.Setup(x => x.SaveUser(It.IsAny<User>()))
        .Callback<User>(u => savedUser = u);

    _service.SaveUser(new User { Id = 1, Name = "Bob" });

    Assert.NotNull(savedUser);
    Assert.Equal("Bob", savedUser?.Name);
    _mockRepo.Verify(x => x.SaveUser(It.IsAny<User>()), Moq.Times.Once());
}
```
<!-- endSnippet -->

### After: KnockOff

<!-- snippet: moq-migration-complete-knockoff -->
```cs
private readonly MoqUserRepoStub _stub;
private readonly UserServiceMigration _service;

public CompleteKnockOffTests()
{
    _stub = new MoqUserRepoStub();
    _service = new UserServiceMigration(_stub);
}

[Fact]
public async Task GetUser_ReturnsUser()
{
    var user = new User { Id = 1, Name = "Alice" };
    // Similar to Moq: Setup + Verifiable
    _stub.GetUserAsync.OnCall((id) => Task.FromResult<User?>(user)).Verifiable();

    var result = await _service.GetUserAsync(1);

    Assert.Equal("Alice", result?.Name);
    // Similar to Moq: mock.Verify() -> stub.Verify()
    _stub.Verify();
}

[Fact]
public void SaveUser_CallsRepository()
{
    User? savedUser = null;
    var tracking = _stub.SaveUser.OnCall((user) =>
    {
        savedUser = user;
    }).Verifiable();

    _service.SaveUser(new User { Id = 1, Name = "Bob" });

    Assert.NotNull(savedUser);
    Assert.Equal("Bob", savedUser?.Name);
    // Similar to Moq: mock.Verify(x => x.SaveUser(...), Times.Once())
    tracking.Verify(Times.Once);
}
```
<!-- endSnippet -->

**What changed:**
- Added stub class declaration with `[KnockOff<IUserRepository>]`
- Replaced `Mock<T>` with stub instance
- Replaced `.Setup()` with interceptor property assignments
- Replaced `.Verify()` with direct assertions
- Removed `.Object` property accesses

**What stayed the same:**
- Test logic and assertions
- Test structure and organization
- Coverage and test goals

---

## Common Gotchas

### Forgetting the `partial` Keyword

**Problem:** Stub class isn't marked `partial`, causing duplicate member errors.

<!-- snippet: moq-migration-gotcha-partial-wrong -->
```cs
// Wrong
[KnockOff<IMoqUserRepo>]
class MoqUserRepoStubWrong { }
```
<!-- endSnippet -->

<!-- snippet: moq-migration-gotcha-partial-correct -->
```cs
// Correct
[KnockOff<IMoqUserRepo>]
partial class MoqUserRepoStubCorrect { }
```
<!-- endSnippet -->

### Wrong `OnCall` Signature

**Problem:** Callback signature doesn't match the method parameters.

<!-- snippet: moq-migration-gotcha-signature-wrong -->
```cs
// Wrong: GetUser(int id) expects (int) callback
// stub.GetUser.OnCall(() => user);  // Compile error
```
<!-- endSnippet -->

<!-- snippet: moq-migration-gotcha-signature-correct -->
```cs
// Correct
stub.GetUser.OnCall((id) => user);
```
<!-- endSnippet -->

### Forgetting `.Object` Equivalence

**Problem:** Trying to use `.Object` like in Moq when it doesn't exist.

<!-- snippet: moq-migration-gotcha-object-moq -->
```cs
// Moq: needed .Object
var mock = new Mock<IMoqUserRepo>();
var moqService = new UserServiceMigration(mock.Object);
```
<!-- endSnippet -->

<!-- snippet: moq-migration-gotcha-object-knockoff -->
```cs
// KnockOff: use stub directly
var stub = new MoqUserRepoStub();
var knockoffService = new UserServiceMigration(stub);
```
<!-- endSnippet -->

### Async Methods - Both Overloads Auto-Wrap

**Note:** KnockOff auto-wraps both Returns and simplified callbacks for async methods:

<!-- snippet: moq-migration-gotcha-async-autowrap -->
```cs
// Returns - auto-wraps in Task.FromResult
stub.GetUserAsync.Returns(user);

// Simplified callback - also auto-wraps (return unwrapped type)
stub.GetUserAsync.OnCall((id) => user);

// Only use Task.FromResult when callback needs actual async operations
stub.GetUserAsync.OnCall(async (id) =>
{
    await Task.Delay(1); // Some actual async work
    return user;
});
```
<!-- endSnippet -->

### Property Configuration

**Problem:** Forgetting properties use `OnGet()` and `OnSet()`, not `OnCall()`.

<!-- snippet: moq-migration-gotcha-property-wrong -->
```cs
// Wrong: OnCall is for methods
// stub.ConnectionString.OnCall(() => "connection");  // Compile error
```
<!-- endSnippet -->

<!-- snippet: moq-migration-gotcha-property-correct -->
```cs
// Correct: use OnGet for property getters
stub.ConnectionString.OnGet("connection");

// For setters, use OnSet
stub.ConnectionString.OnSet((value) => { /* handle set */ });
```
<!-- endSnippet -->

### Void Methods Need Empty Delegate Body

**Problem:** Forgetting that void methods still need a delegate body.

<!-- snippet: moq-migration-gotcha-void-wrong -->
```cs
// Wrong: no delegate body
// stub.SaveUser.OnCall();  // Compile error
```
<!-- endSnippet -->

<!-- snippet: moq-migration-gotcha-void-correct -->
```cs
// Correct
stub.SaveUser.OnCall((user) => { });
```
<!-- endSnippet -->

---

## Times Matcher Reference

KnockOff supports these `Times` matchers:

| Matcher | Description |
|---------|-------------|
| `Times.Never` | Method was never called |
| `Times.Once` | Method was called exactly once |
| `Times.AtLeastOnce` | Method was called one or more times |
| `Times.AtLeast(n)` | Method was called at least n times |
| `Times.AtMost(n)` | Method was called at most n times |
| `Times.Exactly(n)` | Method was called exactly n times |

**Note:** Unlike Moq, KnockOff does NOT have `Times.Between()`. Use separate `AtLeast` and `AtMost` checks instead.

**Example:**

<!-- snippet: moq-migration-times-example -->
```cs
// Moq
mock.Verify(x => x.SaveUser(It.IsAny<User>()), Moq.Times.Exactly(3));

// KnockOff
stub.SaveUser.Verify(Times.Exactly(3));

// For range verification (no Times.Between in KnockOff):
stub.SaveUser.Verify(Times.AtLeast(1));
stub.SaveUser.Verify(Times.AtMost(5));
```
<!-- endSnippet -->

---

## Migration Checklist

Use this checklist when migrating a test file from Moq to KnockOff:

- [ ] Add KnockOff NuGet package
- [ ] Remove Moq NuGet package
- [ ] Create stub class declarations with `[KnockOff<T>]` attribute
- [ ] Ensure stub classes are marked `partial`
- [ ] Replace `Mock<T>` field declarations with stub types
- [ ] Remove `.Object` property accesses
- [ ] Convert method `.Setup().Returns()` to `.OnCall()`
- [ ] Convert property setups to `.OnGet()` and `.OnSet()`
- [ ] Convert `.ReturnsAsync()` to `Task.FromResult()`
- [ ] Move `.Callback()` logic into `OnCall` delegates
- [ ] Replace `It.IsAny<T>()` with callback parameter inspection
- [ ] Convert `.Verify()` calls to `tracking.Verify()`, `stub.Method.Verify()`, or batch `stub.Verify()`
- [ ] Update `using` statements (remove Moq, add KnockOff namespace if needed)

---

**UPDATED:** 2026-02-01
