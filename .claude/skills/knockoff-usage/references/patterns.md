# KnockOff Stub Patterns Reference

KnockOff supports three patterns for creating test stubs. Each pattern solves different testing scenarios with varying trade-offs in reusability, ceremony, and capabilities.

---

## Quick Decision Guide

| If you need... | Use this pattern |
|----------------|------------------|
| Reusable stub across multiple test files | Stand-Alone |
| Custom methods on your stub | Stand-Alone |
| Quick, test-local stub | Inline Interface |
| No extra stub files | Inline Interface |
| Stub a class (not interface) | Inline Class |

---

## Stand-Alone Pattern

The Stand-Alone pattern creates a dedicated stub class in its own file. This stub can be reused across test files and supports adding custom methods.

### When to Use

- You need the same stub in multiple test files
- You want to add helper methods or custom behavior to the stub
- You prefer explicit, discoverable stub classes in IntelliSense

### Declaration

Create a partial class with the `[KnockOff]` attribute that implements the interface you want to stub:

```cs
public interface IUserRepository
{
    User? GetById(int id);
    void Save(User user);
}

[KnockOff]
public partial class UserRepositoryStub : IUserRepository
{
    // Optionally add user methods for default behavior
    protected User? GetById(int id) => new User { Id = id, Name = $"User{id}" };
}
```

The source generator produces:
- Explicit interface implementations for all members
- Interceptor properties named after each interface member (e.g., `Save`, `GetById`)
- When you define a user method, a numbered interceptor is created for tracking (e.g., `GetById2`)

### Usage in Tests

```cs
[Fact]
public void StandaloneStub_CanBeConfiguredAndVerified()
{
    // Arrange - instantiate the reusable stub
    var stub = new UserRepositoryStub();

    // Configure void method via OnCall and mark verifiable
    stub.Save.OnCall((user) => { }).Verifiable();

    // Act - cast to interface for use
    IUserRepository repository = stub;
    var user = repository.GetById(42);
    repository.Save(user!);

    // Assert - verify via Verify()
    Assert.NotNull(user);
    stub.Verify();

    // User methods get a numbered interceptor (GetById2) for tracking
    // This allows verification without blocking the user method implementation
    stub.GetById2.Verify(Times.Once);
}
```

### Key Points

- **Instantiation**: `new MyStub()`
- **Interface access**: Cast to interface type or assign to interface variable
- **Verification**: Call `stub.Verify()` or individual interceptor `.Verify()`
- **User methods**: Define protected methods matching interface signatures to provide default behavior
- **Numbered interceptors**: When you define a user method, the generator creates a numbered interceptor (e.g., `GetById2`) to track calls without interfering with your implementation

### Benefits

- **Reusable**: Reference the stub from any test file
- **User methods**: Add custom methods directly on the stub class
- **Discoverable**: Appears in IntelliSense when browsing your test project
- **Explicit**: Clear separation between test code and stub implementation

### Trade-offs

- **Extra file**: Requires a dedicated .cs file for each stub
- **Partial class**: Must remember to mark the class as `partial`
- **Manual interface**: Must manually implement the interface signature

---

## Inline Interface Pattern

The Inline Interface pattern generates a stub class scoped to your test class. The stub is accessed through a nested `Stubs` namespace.

### When to Use

- You need a stub only within one test class
- You don't need custom methods on the stub
- You want minimal ceremony and no extra files

### Declaration

Apply `[KnockOff<IInterface>]` to your test class:

```cs
public interface IUserRepository
{
    User? GetById(int id);
    void Save(User user);
}

[KnockOff<IUserRepository>]
public partial class UserRepositoryTests
{
    // The generator creates Stubs.IUserRepository
}
```

The source generator produces a nested `Stubs` class containing the stub implementation.

### Usage in Tests

```cs
[KnockOff<IUserRepository>]
public partial class UserRepositoryTests
{
    [Fact]
    public void InlineInterfaceStub_GeneratedInStubsNamespace()
    {
        // Arrange - use generated Stubs.InterfaceName class
        var stub = new Stubs.IUserRepository();

        // Configure behavior and mark verifiable
        stub.GetById.OnCall((id) => new User { Id = id, Name = "Test" }).Verifiable();
        stub.Save.OnCall((user) => { }).Verifiable();

        // Act
        IUserRepository repository = stub;
        var user = repository.GetById(1);
        repository.Save(user!);

        // Assert
        Assert.NotNull(user);
        Assert.Equal("Test", user.Name);
        stub.Verify();
    }
}
```

### Key Points

- **Instantiation**: `new Stubs.IInterfaceName()`
- **Interface access**: Cast to interface type or assign to interface variable
- **Verification**: Call `stub.Verify()` or individual interceptor `.Verify()`
- **Scoping**: Stub only exists within the test class where it is declared

### Benefits

- **Scoped**: Stub exists only for this test class, reducing namespace pollution
- **Less ceremony**: No separate file, no manual interface implementation
- **Automatic**: Stub class generated from interface definition

### Trade-offs

- **No user methods**: Cannot add custom methods to the generated stub
- **Stubs namespace**: Must use `Stubs.IFoo` syntax to instantiate
- **Test-local only**: Cannot reuse across multiple test classes

---

## Inline Class Pattern

The Inline Class pattern generates a stub for abstract or virtual class members. This allows stubbing classes without extracting interfaces.

### When to Use

- You need to stub a class (not an interface)
- The class has `virtual` or `abstract` members you want to intercept
- You cannot or don't want to extract an interface

### Declaration

Apply `[KnockOff<ClassName>]` to your test class, targeting a class with virtual or abstract members:

```cs
// Target class with virtual members
public class UserService
{
    public virtual User? GetUser(int id) => null;
    public virtual void SaveUser(User user) { }
    public virtual bool IsConnected { get; set; }
}

[KnockOff<UserService>]
public partial class UserServiceTests
{
    // The generator creates Stubs.UserService
}
```

The source generator produces:
- A wrapper class in the nested `Stubs` namespace
- A derived class that overrides all virtual/abstract members
- An `Object` property to access the actual class instance

### Usage in Tests

```cs
[KnockOff<UserService>]
public partial class UserServiceTests
{
    [Fact]
    public void InlineClassStub_UsesObjectProperty()
    {
        // Arrange - create wrapper stub
        var stub = new Stubs.UserService();

        // Configure virtual member behavior and mark verifiable
        stub.GetUser.OnCall((id) => new User { Id = id, Name = "FromStub" }).Verifiable();

        // Act - use .Object to get the actual class instance
        UserService service = stub.Object;
        var user = service.GetUser(42);

        // Assert
        Assert.NotNull(user);
        Assert.Equal("FromStub", user.Name);
        stub.Verify();
    }
}
```

### Key Points

- **Instantiation**: `new Stubs.ClassName()`
- **Class access**: Use the `.Object` property to get the actual class instance
- **Verification**: Call `stub.Verify()` or individual interceptor `.Verify()`
- **Virtual/abstract only**: Only members marked `virtual` or `abstract` can be intercepted

### Benefits

- **Stub classes**: Works with classes, not just interfaces
- **No interface extraction**: Avoids creating interfaces just for testing
- **Virtual members**: Intercepts any `virtual` or `abstract` members

### Trade-offs

- **Must use .Object**: The stub is a wrapper; use `.Object` property to get the actual instance
- **Virtual/abstract only**: Only overrides members marked `virtual` or `abstract`
- **No user methods**: Cannot add custom methods like Stand-Alone pattern

---

## Pattern Comparison

| Feature | Stand-Alone | Inline Interface | Inline Class |
|---------|-------------|------------------|--------------|
| **Reusable across test files** | Yes | No | No |
| **Custom user methods** | Yes | No | No |
| **Extra file required** | Yes | No | No |
| **Supports interfaces** | Yes | Yes | No |
| **Supports classes** | No | No | Yes |
| **IntelliSense visible** | Yes | Within test class | Within test class |
| **Instantiation syntax** | `new MyStub()` | `new Stubs.IFoo()` | `new Stubs.Foo()` |
| **Access pattern** | Cast to interface | Cast to interface | Use `.Object` property |
| **Best for** | Shared stubs | Local stubs | Class stubs |

---

## Decision Tree

Follow this decision tree to choose the right pattern:

1. **Do you need to stub a class (not an interface)?**
   - Yes: Use **Inline Class** pattern
   - No: Continue to step 2

2. **Do you need the stub in multiple test files?**
   - Yes: Use **Stand-Alone** pattern
   - No: Continue to step 3

3. **Do you need custom methods on the stub?**
   - Yes: Use **Stand-Alone** pattern
   - No: Use **Inline Interface** pattern

### Scenario Recommendations

| Scenario | Recommended Pattern |
|----------|---------------------|
| Repository stub used in 5+ test classes | Stand-Alone |
| Stub with `WithAdminUser()` helper method | Stand-Alone |
| Quick stub for single test class | Inline Interface |
| Stub a `DbContext` with virtual `DbSet` properties | Inline Class |
| Stub an abstract base class | Inline Class |

---

## Complete Example: All Three Patterns Together

This example demonstrates using all three patterns in a realistic test scenario:

```cs
// Stand-alone stub for email service (reusable across test files)
public interface IEmailService
{
    bool Send(string to, string subject, string body);
    bool IsConfigured { get; }
}

[KnockOff]
public partial class EmailServiceStub : IEmailService { }

// Interfaces and classes to stub inline
public interface ILogService
{
    void Log(string message);
}

public abstract class AuditServiceBase
{
    public abstract void Audit(string action);
}

// Test class using all three patterns
[KnockOff<ILogService>]
[KnockOff<AuditServiceBase>]
public partial class IntegrationTests
{
    [Fact]
    public void AllThreePatterns_WorkTogether()
    {
        // Stand-Alone: Reusable email stub
        var emailStub = new EmailServiceStub();
        emailStub.Send.OnCall((to, subject, body) => true).Verifiable();
        emailStub.IsConfigured.Value = true;

        // Inline Interface: Test-local logger stub
        var loggerStub = new Stubs.ILogService();
        var logMessages = new List<string>();
        loggerStub.Log.OnCall((msg) => logMessages.Add(msg)).Verifiable(Times.Exactly(2));

        // Inline Class: Stub for abstract base class
        var auditStub = new Stubs.AuditServiceBase();
        auditStub.Audit.OnCall((action) => { }).Verifiable();

        // Act - simulate integration scenario
        IEmailService email = emailStub;
        ILogService logger = loggerStub;
        AuditServiceBase audit = auditStub.Object;

        logger.Log("Starting operation");
        var sent = email.Send("user@test.com", "Hello", "World");
        audit.Audit("email_sent");
        logger.Log("Operation complete");

        // Assert - each pattern provides Verify()
        Assert.True(sent);
        emailStub.Verify();
        loggerStub.Verify();
        auditStub.Verify();
        Assert.Contains("Starting operation", logMessages);
    }
}
```

### Pattern Usage Summary in the Example

| Stub | Pattern | Reason |
|------|---------|--------|
| `EmailServiceStub` | Stand-Alone | Reusable across test files, could have helper methods |
| `Stubs.ILogService` | Inline Interface | Only needed in this test class |
| `Stubs.AuditServiceBase` | Inline Class | Stubbing an abstract class, not an interface |
