# Method Interceptors Reference

Method interceptors track calls, capture arguments, and configure return values for interface methods in your stub. Each method on the stubbed interface gets a corresponding interceptor property that provides verification and configuration capabilities.

---

## Configuring Method Behavior

### Void Methods

Configure void methods using `OnCall` with an `Action`:

```csharp
public interface ILogService
{
    void LogMessage(string message);
}

[KnockOff]
public partial class LogServiceStub : ILogService { }

[Fact]
public void VoidMethod_ConfiguredWithOnCall()
{
    var stub = new LogServiceStub();

    // OnCall for void methods uses Action<...params>
    var logged = new List<string>();
    var tracking = stub.LogMessage.OnCall((message) =>
    {
        logged.Add(message);
    });

    ILogService logger = stub;
    logger.LogMessage("Hello, World!");

    Assert.Single(logged);
    Assert.Equal("Hello, World!", logged[0]);
    tracking.Verify();
}
```

### Methods with Return Values

Configure methods that return values using `OnCall` with a `Func`:

```csharp
public interface IUserService
{
    string GetUserName(int userId);
}

[KnockOff]
public partial class UserServiceStub : IUserService { }

[Fact]
public void MethodWithReturn_ConfiguredWithOnCall()
{
    var stub = new UserServiceStub();

    // OnCall with return value: params, then return type
    var tracking = stub.GetUserName.OnCall((userId) => "TestUser");

    IUserService service = stub;
    var name = service.GetUserName(42);

    Assert.Equal("TestUser", name);
    tracking.Verify();
}
```

### Methods with Multiple Parameters

Methods with multiple parameters include all parameters in the callback:

```csharp
public interface IAuthService
{
    bool ValidateCredentials(string username, string password);
}

[KnockOff]
public partial class AuthServiceStub : IAuthService { }

[Fact]
public void MethodWithMultipleParams_AllAvailableInOnCall()
{
    var stub = new AuthServiceStub();

    // All method parameters are available in the callback
    var tracking = stub.ValidateCredentials.OnCall((username, password) =>
        username == "admin" && password == "secret");

    IAuthService auth = stub;

    Assert.True(auth.ValidateCredentials("admin", "secret"));
    Assert.False(auth.ValidateCredentials("user", "wrong"));

    // Verify exactly 2 calls were made
    tracking.Verify(Times.Exactly(2));
}
```

---

## Verifying Method Calls

### Using Verify()

Call `.Verify()` on the tracking object returned by `OnCall`:

```csharp
public interface IRepository
{
    void Save(object entity);
}

[KnockOff]
public partial class RepositoryStub : IRepository { }

[Fact]
public void Verify_VerifiesMethodInvocation()
{
    var stub = new RepositoryStub();
    var tracking = stub.Save.OnCall((entity) => { });

    IRepository repository = stub;
    repository.Save(new { Id = 1 });

    // Verify the method was called
    tracking.Verify();
}
```

### Verifying Call Frequency with Times

Use `Times` to specify exact call count requirements:

```csharp
public interface INotifier
{
    void Notify(string message);
}

[KnockOff]
public partial class NotifierStub : INotifier { }

[Fact]
public void Verify_ExactCallCount()
{
    var stub = new NotifierStub();
    var tracking = stub.Notify.OnCall((message) => { });

    INotifier notifier = stub;

    // Simulate processing a 2-item collection
    var items = new[] { "item1", "item2" };
    foreach (var item in items)
    {
        notifier.Notify($"Processing {item}");
    }

    // Verify exactly 2 calls (throws if different)
    tracking.Verify(Times.Exactly(2));
}
```

**Available Times constraints:**

| Constraint | Description |
|------------|-------------|
| `Times.Never` | Method must not be called |
| `Times.Once` | Method must be called exactly once |
| `Times.AtLeastOnce` | Method must be called one or more times |
| `Times.Exactly(n)` | Method must be called exactly n times |

### Using Verifiable() for Batch Verification

For batch verification of multiple methods, use `.Verifiable()` then call `stub.Verify()`:

```csharp
public interface IUserRepository
{
    void Save(User entity);
    User GetById(int id);
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
}

[KnockOff]
public partial class UserRepositoryStub : IUserRepository { }

[Fact]
public void Verifiable_BatchVerification()
{
    var stub = new UserRepositoryStub();

    // Mark expected calls with Verifiable()
    stub.Save.OnCall((entity) => { }).Verifiable(Times.Once);
    stub.GetById.OnCall((id) => new User { Id = id }).Verifiable();

    IUserRepository repository = stub;
    repository.Save(new User { Id = 1 });
    repository.GetById(1);

    // Verify all marked methods (throws if any not called correctly)
    stub.Verify();
}
```

**Key difference:**
- `tracking.Verify()` - Verifies a single method
- `stub.Verify()` - Verifies all methods marked with `.Verifiable()`

---

## Capturing Arguments

### Single Parameter Methods

Access the last call's argument using `LastArg`:

```csharp
public interface IUserRepo
{
    User GetUser(int userId);
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
}

[KnockOff]
public partial class UserRepoStub : IUserRepo { }

[Fact]
public void LastArg_CapturesSingleParameter()
{
    var stub = new UserRepoStub();
    var tracking = stub.GetUser.OnCall((userId) => new User { Id = userId });

    IUserRepo repository = stub;
    repository.GetUser(42);

    // LastArg captures the most recent call's argument
    int capturedId = tracking.LastArg;
    Assert.Equal(42, capturedId);
}
```

### Multiple Parameter Methods

Access arguments using the `LastArgs` named tuple:

```csharp
public interface IAuthService
{
    bool ValidateCredentials(string username, string password);
}

[KnockOff]
public partial class AuthServiceStub : IAuthService { }

[Fact]
public void LastArgs_CapturesAllParameters()
{
    var stub = new AuthServiceStub();
    var tracking = stub.ValidateCredentials.OnCall((username, password) => true);

    IAuthService auth = stub;
    auth.ValidateCredentials("admin", "secret123");

    // LastArgs is a named tuple with all parameters
    var (username, password) = tracking.LastArgs;
    Assert.Equal("admin", username);
    Assert.Equal("secret123", password);
}
```

---

## Handling Overloaded Methods

When an interface has overloaded methods, KnockOff distinguishes them by the callback signature. The fully-typed lambda tells KnockOff which overload to configure:

```csharp
public interface ISearchRepo
{
    List<User> Find();
    User Find(int id);
    User Find(string name);
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
}

[KnockOff]
public partial class SearchRepoStub : ISearchRepo { }

[Fact]
public void Overloads_DistinguishedByCallbackSignature()
{
    var stub = new SearchRepoStub();

    // Overloads are distinguished by the callback parameter types
    // The fully-typed lambda tells KnockOff which overload to configure
    var findAllTracking = stub.Find.OnCall(() =>
        new List<User>()).Verifiable();
    var findByIdTracking = stub.Find.OnCall((int id) =>
        new User { Id = id, Name = "ById" }).Verifiable();
    var findByNameTracking = stub.Find.OnCall((string name) =>
        new User { Id = 1, Name = name }).Verifiable();

    ISearchRepo repo = stub;

    // Call each overload
    repo.Find();
    repo.Find(42);
    repo.Find("Alice");

    // Verify all overloads were called
    stub.Verify();

    // Access last arguments via tracking objects
    Assert.Equal(42, findByIdTracking.LastArg);
    Assert.Equal("Alice", findByNameTracking.LastArg);
}
```

**Important:** The callback signature determines which overload is configured. Use explicit types in lambdas when parameter types are ambiguous.

---

## Resetting Interceptors

Clear tracking state and remove callbacks using `Reset()`:

```csharp
public interface IProcessor
{
    void ProcessData(string data);
}

[KnockOff]
public partial class ProcessorStub : IProcessor { }

[Fact]
public void Reset_ClearsTrackingState()
{
    var stub = new ProcessorStub();
    var tracking = stub.ProcessData.OnCall((data) => { });

    IProcessor processor = stub;
    processor.ProcessData("initial");

    // Verify one call was made
    tracking.Verify(Times.Once);

    // Reset clears call count on the interceptor
    stub.ProcessData.Reset();

    // After reset, verify never passes
    tracking.Verify(Times.Never);
}
```

**Use cases for Reset():**
- Reusing a stub instance across multiple test phases
- Testing a sequence of interactions where counts should restart
- Isolating assertions between test setup and execution phases

---

## Complete Example

This example demonstrates a realistic test using method configuration, execution, and verification:

```csharp
public interface ICompleteUserRepo
{
    User GetUser(int id);
    void SaveUser(User user);
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}

public class UserService
{
    private readonly ICompleteUserRepo _repo;

    public UserService(ICompleteUserRepo repo)
    {
        _repo = repo;
    }

    public bool UpdateUserEmail(int userId, string newEmail)
    {
        var user = _repo.GetUser(userId);
        if (user == null) return false;

        user.Email = newEmail;
        _repo.SaveUser(user);
        return true;
    }
}

[KnockOff]
public partial class CompleteUserRepoStub : ICompleteUserRepo { }

[Fact]
public void UserService_UpdateUserEmail_CallsRepositoryCorrectly()
{
    // Arrange
    var stub = new CompleteUserRepoStub();

    var testUser = new User { Id = 1, Name = "Alice", Email = "old@test.com" };
    var getTracking = stub.GetUser.OnCall((id) => id == 1 ? testUser : null).Verifiable();
    var saveTracking = stub.SaveUser.OnCall((user) => { }).Verifiable();

    var service = new UserService(stub);

    // Act
    var result = service.UpdateUserEmail(1, "new@test.com");

    // Assert
    Assert.True(result);

    // Verify both methods were called
    stub.Verify();

    // Verify GetUser was called with correct ID
    Assert.Equal(1, getTracking.LastArg);

    // Verify saved user has new email via the tracking args
    var savedUser = saveTracking.LastArg;
    Assert.Equal("new@test.com", savedUser.Email);
}
```

---

## Quick Reference

| Task | Code |
|------|------|
| Configure void method | `stub.Method.OnCall((args) => { })` |
| Configure method with return | `stub.Method.OnCall((args) => returnValue)` |
| Verify method was called | `tracking.Verify()` |
| Verify call count | `tracking.Verify(Times.Exactly(n))` |
| Mark for batch verify | `stub.Method.OnCall(...).Verifiable()` |
| Batch verify all | `stub.Verify()` |
| Get last single arg | `tracking.LastArg` |
| Get last multiple args | `tracking.LastArgs` (named tuple) |
| Reset interceptor | `stub.Method.Reset()` |

---

## Key Takeaways

- **OnCall signature**: Callback receives only the method parameters
- **Verification**: Use `tracking.Verify(Times)` for single methods or `.Verifiable()` + `stub.Verify()` for batch
- **Arguments**: `LastArg` for single parameters, `LastArgs` tuple for multiple
- **Overloads**: Distinguished by callback parameter types - use explicit types in lambdas
- **Reset**: Clears call counts and tracking state
