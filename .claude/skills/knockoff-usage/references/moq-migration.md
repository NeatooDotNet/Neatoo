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
| `mock.Object` | `stub` (direct instance) |
| `.Setup(x => x.Method()).Returns(value)` | `stub.Method.OnCall(() => value)` |
| `.Setup(x => x.Property).Returns(value)` | `stub.Property.Value = value` |
| `.ReturnsAsync(value)` | `stub.Method.OnCall(() => Task.FromResult(value))` |
| `.Callback(x => ...)` | Logic in `OnCall` delegate |
| `.Verify(x => x.Method(), Times.Once)` | `tracking.Verify(Times.Once)` or `stub.Method.Verify(Times.Once)` |
| `.Verifiable()` | `stub.Method.OnCall(...).Verifiable()` |
| `mock.Verify()` | `stub.Verify()` |
| `It.IsAny<T>()` | Callback receives all arguments for inspection |

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

```cs
[Fact]
public void CreateStub_MoqApproach()
{
    var mock = new Mock<IUserRepository>();
    IUserRepository repository = mock.Object;

    Assert.NotNull(repository);
}
```

**KnockOff:**

```cs
// First, declare the stub class
[KnockOff<IUserRepository>]
partial class UserRepositoryStub { }

// Then use it in tests
[Fact]
public void CreateStub_KnockOffApproach()
{
    var stub = new UserRepositoryStub();
    IUserRepository repository = stub;

    Assert.NotNull(repository);
}
```

**Key differences:**
- Moq creates wrapper objects at runtime
- KnockOff requires a partial class declaration—the generator fills in the implementation
- You use the stub instance directly (no `.Object` property)

---

## Step 3: Configure Methods

Replace `.Setup().Returns()` with `OnCall` property assignments.

**Moq:**

```cs
[Fact]
public void SetupMethod_MoqApproach()
{
    var mock = new Mock<IUserRepository>();
    var testUser = new User { Id = 42, Name = "Alice" };

    mock.Setup(x => x.GetUser(It.IsAny<int>())).Returns(testUser);

    IUserRepository repository = mock.Object;
    var user = repository.GetUser(42);

    Assert.NotNull(user);
    Assert.Equal("Alice", user.Name);
}
```

**KnockOff:**

```cs
[Fact]
public void SetupMethod_KnockOffApproach()
{
    var stub = new UserRepositoryStub();
    var testUser = new User { Id = 42, Name = "Alice" };

    stub.GetUser.OnCall((id) => testUser);

    IUserRepository repository = stub;
    var user = repository.GetUser(42);

    Assert.NotNull(user);
    Assert.Equal("Alice", user.Name);
}
```

**Key differences:**
- Moq uses fluent setup with expression trees
- KnockOff uses direct property assignment with typed delegates
- KnockOff's callback receives actual argument values (no `It.IsAny<T>()` needed)

---

## Step 4: Configure Properties

Replace property `.Setup().Returns()` with `.Value` assignments.

**Moq:**

```cs
[Fact]
public void SetupProperty_MoqApproach()
{
    var mock = new Mock<IUserRepository>();

    mock.Setup(x => x.ConnectionString).Returns("server=localhost");

    IUserRepository repository = mock.Object;
    var connStr = repository.ConnectionString;

    Assert.Equal("server=localhost", connStr);
}
```

**KnockOff:**

```cs
[Fact]
public void SetupProperty_KnockOffApproach()
{
    var stub = new UserRepositoryStub();

    stub.ConnectionString.Value = "server=localhost";

    IUserRepository repository = stub;
    var connStr = repository.ConnectionString;

    Assert.Equal("server=localhost", connStr);
}
```

**Key differences:**
- Moq treats properties like methods in setup
- KnockOff provides a `.Value` property on the interceptor
- KnockOff also tracks `GetCount` and `SetCount` for verification

---

## Step 5: Verify Calls

Replace Moq's `.Verify()` calls with KnockOff's `.Verify()` or `.Verifiable()` API.

**Moq:**

```cs
[Fact]
public void VerifyCalls_MoqApproach()
{
    var mock = new Mock<IUserRepository>();

    IUserRepository repository = mock.Object;
    repository.SaveUser(new User { Id = 1, Name = "Bob" });

    mock.Verify(x => x.SaveUser(It.IsAny<User>()), Times.Once());
}
```

**KnockOff:**

```cs
[Fact]
public void VerifyCalls_KnockOffApproach()
{
    var stub = new UserRepositoryStub();

    // Mark method as verifiable during setup
    stub.SaveUser.OnCall((user) => { }).Verifiable();

    IUserRepository repository = stub;
    repository.SaveUser(new User { Id = 1, Name = "Bob" });

    // Verify() checks all members marked with .Verifiable()
    stub.Verify();

    // Or verify with Times constraint directly on interceptor
    // stub.SaveUser.Verify(Times.Once);

    // Or verify via tracking object
    // var tracking = stub.SaveUser.OnCall((user) => { });
    // tracking.Verify(Times.Once);
}
```

**Key differences:**
- Moq uses `mock.Verify(expression, times)` with expression trees
- KnockOff has three verification approaches:
  - `tracking.Verify(times)` on the object returned by `OnCall`
  - `stub.Method.Verify(times)` directly on the interceptor property
  - `.Verifiable()` + `stub.Verify()` for batch verification
- Both support the same `Times` matchers (Once, AtLeastOnce, Exactly, etc.)

---

## Step 6: Async Methods

Replace `.ReturnsAsync()` with `Task.FromResult()` in `OnCall`.

**Moq:**

```cs
[Fact]
public async Task AsyncMethod_MoqApproach()
{
    var mock = new Mock<IUserRepository>();
    var testUser = new User { Id = 42, Name = "Alice" };

    mock.Setup(x => x.GetUserAsync(It.IsAny<int>())).ReturnsAsync(testUser);

    IUserRepository repository = mock.Object;
    var user = await repository.GetUserAsync(42);

    Assert.NotNull(user);
    Assert.Equal("Alice", user.Name);
}
```

**KnockOff:**

```cs
[Fact]
public async Task AsyncMethod_KnockOffApproach()
{
    var stub = new UserRepositoryStub();
    var testUser = new User { Id = 42, Name = "Alice" };

    stub.GetUserAsync.OnCall((id) => Task.FromResult<User?>(testUser));

    IUserRepository repository = stub;
    var user = await repository.GetUserAsync(42);

    Assert.NotNull(user);
    Assert.Equal("Alice", user.Name);
}
```

**Key differences:**
- Moq provides `.ReturnsAsync()` helper
- KnockOff uses standard `Task.FromResult()` or `Task.CompletedTask`
- For exceptions: return `Task.FromException<T>(exception)`

---

## Step 7: Callbacks

Replace `.Callback()` with logic directly in `OnCall` delegates.

**Moq:**

```cs
[Fact]
public void Callback_MoqApproach()
{
    var mock = new Mock<IUserRepository>();
    var savedUsers = new List<User>();

    mock.Setup(x => x.SaveUser(It.IsAny<User>()))
        .Callback<User>(u => savedUsers.Add(u));

    IUserRepository repository = mock.Object;
    repository.SaveUser(new User { Id = 1, Name = "Alice" });
    repository.SaveUser(new User { Id = 2, Name = "Bob" });

    Assert.Equal(2, savedUsers.Count);
    Assert.Equal("Alice", savedUsers[0].Name);
    Assert.Equal("Bob", savedUsers[1].Name);
}
```

**KnockOff:**

```cs
[Fact]
public void Callback_KnockOffApproach()
{
    var stub = new UserRepositoryStub();
    var savedUsers = new List<User>();

    stub.SaveUser.OnCall((user) =>
    {
        savedUsers.Add(user);
    });

    IUserRepository repository = stub;
    repository.SaveUser(new User { Id = 1, Name = "Alice" });
    repository.SaveUser(new User { Id = 2, Name = "Bob" });

    Assert.Equal(2, savedUsers.Count);
    Assert.Equal("Alice", savedUsers[0].Name);
    Assert.Equal("Bob", savedUsers[1].Name);
}
```

**Key differences:**
- Moq separates `.Callback()` and `.Returns()`
- KnockOff combines them in a single delegate—add logic, then return a value if needed
- You can access arguments directly by name

---

## Step 8: Argument Matching

Replace `It.IsAny<T>()` matchers with callback logic.

**Moq:**

```cs
[Fact]
public void ArgumentMatching_MoqApproach()
{
    var mock = new Mock<IUserRepository>();

    mock.Setup(x => x.GetUser(It.Is<int>(id => id > 0)))
        .Returns<int>(id => new User { Id = id, Name = "Valid User" });

    IUserRepository repository = mock.Object;

    var validUser = repository.GetUser(1);
    var invalidUser = repository.GetUser(-1);

    Assert.NotNull(validUser);
    Assert.Null(invalidUser);
}
```

**KnockOff:**

```cs
[Fact]
public void ArgumentMatching_KnockOffApproach()
{
    var stub = new UserRepositoryStub();

    stub.GetUser.OnCall((id) =>
        id > 0 ? new User { Id = id, Name = "Valid User" } : null);

    IUserRepository repository = stub;

    var validUser = repository.GetUser(1);
    var invalidUser = repository.GetUser(-1);

    Assert.NotNull(validUser);
    Assert.Null(invalidUser);
}
```

**Key differences:**
- Moq uses `It.IsAny<T>()` and `It.Is<T>()` for argument matching
- KnockOff callbacks receive all arguments—implement your own conditional logic
- For verification, inspect `CallHistory` to check specific argument values

---

## Complete Before/After Example

This example shows a full test class migrated from Moq to KnockOff.

### Before: Moq

```cs
public class UserServiceTests
{
    private readonly Mock<IUserRepository> _mockRepo;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _mockRepo = new Mock<IUserRepository>();
        _service = new UserService(_mockRepo.Object);
    }

    [Fact]
    public async Task GetUser_ReturnsUser()
    {
        var user = new User { Id = 1, Name = "Alice" };
        _mockRepo.Setup(x => x.GetUserAsync(1)).ReturnsAsync(user);

        var result = await _service.GetUserAsync(1);

        Assert.Equal("Alice", result?.Name);
        _mockRepo.Verify(x => x.GetUserAsync(1), Times.Once());
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
        _mockRepo.Verify(x => x.SaveUser(It.IsAny<User>()), Times.Once());
    }
}
```

### After: KnockOff

```cs
// Stub declaration (typically in a shared file or same test file)
[KnockOff<IUserRepository>]
partial class UserRepositoryStub { }

public class UserServiceTests
{
    private readonly UserRepositoryStub _stub;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _stub = new UserRepositoryStub();
        _service = new UserService(_stub);
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
}
```

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

```csharp
// Wrong
[KnockOff<IUserRepository>]
class UserRepositoryStub { }

// Correct
[KnockOff<IUserRepository>]
partial class UserRepositoryStub { }
```

### Wrong `OnCall` Signature

**Problem:** Callback signature doesn't match the method parameters.

```csharp
// Wrong: GetUser(int id) expects (int) callback
stub.GetUser.OnCall(() => user);

// Correct
stub.GetUser.OnCall((id) => user);
```

### Forgetting `.Object` Equivalence

**Problem:** Trying to use `.Object` like in Moq when it doesn't exist.

```csharp
// Moq: needed .Object
var service = new UserService(mock.Object);

// KnockOff: use stub directly
var service = new UserService(stub);
```

### Async Return Type Mismatch

**Problem:** Forgetting to wrap return values in `Task.FromResult()` for async methods.

```csharp
// Wrong: returns User directly for async method
stub.GetUserAsync.OnCall((id) => user);

// Correct: wrap in Task.FromResult
stub.GetUserAsync.OnCall((id) => Task.FromResult(user));
```

### Property Value vs Method OnCall

**Problem:** Using `OnCall` for properties instead of `.Value`.

```csharp
// Wrong: properties use .Value, not OnCall
stub.ConnectionString.OnCall(() => "connection");

// Correct
stub.ConnectionString.Value = "connection";
```

### Void Methods Need Empty Delegate Body

**Problem:** Forgetting that void methods still need a delegate body.

```csharp
// Wrong: no delegate body
stub.DeleteUser.OnCall();

// Correct
stub.DeleteUser.OnCall((id) => { });
```

---

## Times Matcher Reference

KnockOff supports the same `Times` matchers as Moq:

| Matcher | Description |
|---------|-------------|
| `Times.Never` | Method was never called |
| `Times.Once` | Method was called exactly once |
| `Times.AtLeastOnce` | Method was called one or more times |
| `Times.AtLeast(n)` | Method was called at least n times |
| `Times.AtMost(n)` | Method was called at most n times |
| `Times.Exactly(n)` | Method was called exactly n times |
| `Times.Between(min, max)` | Method was called between min and max times |

**Example:**

```csharp
// Moq
mock.Verify(x => x.SaveUser(It.IsAny<User>()), Times.Exactly(3));

// KnockOff
stub.SaveUser.Verify(Times.Exactly(3));
```

---

## Migration Checklist

Use this checklist when migrating a test file from Moq to KnockOff:

- [ ] Add KnockOff NuGet package
- [ ] Remove Moq NuGet package
- [ ] Create stub class declarations with `[KnockOff<T>]` attribute
- [ ] Ensure stub classes are marked `partial`
- [ ] Replace `Mock<T>` field declarations with stub types
- [ ] Remove `.Object` property accesses
- [ ] Convert `.Setup().Returns()` to `.OnCall()`
- [ ] Convert property setups to `.Value = `
- [ ] Convert `.ReturnsAsync()` to `Task.FromResult()`
- [ ] Move `.Callback()` logic into `OnCall` delegates
- [ ] Replace `It.IsAny<T>()` with callback parameter inspection
- [ ] Convert `.Verify()` calls to `tracking.Verify()`, `stub.Method.Verify()`, or batch `stub.Verify()`
- [ ] Update `using` statements (remove Moq, add KnockOff namespace if needed)
