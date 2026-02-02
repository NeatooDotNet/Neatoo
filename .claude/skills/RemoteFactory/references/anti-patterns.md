# Anti-Patterns to Avoid

## 1. [Remote] on Child Entities

**Problem**: Causes N+1 remote calls - one HTTP request per child entity.

```csharp
// WRONG - 10 assignments = 10 HTTP calls!
[Factory]
public partial class Assignment
{
    [Remote, Create]  // DON'T DO THIS
    public void Create(string name) { }
}

// RIGHT - all children created in parent's single server call
[Factory]
public partial class Assignment
{
    [Create]  // No [Remote] on children
    public void Create(string name) { }
}
```

**Why**: `[Remote]` marks client-to-server entry points. Child entities are created/fetched as part of parent operations that are already on the server.

---

## 2. Attributes on Interface Methods

**Problem**: Causes duplicate generation and registration conflicts.

```csharp
// WRONG - interface methods don't need attributes
[Factory]
public interface IEmployeeRepository
{
    [Fetch]  // DON'T DO THIS
    Task<EmployeeEntity> GetByIdAsync(Guid id);
}

// RIGHT - interface IS the boundary
[Factory]
public interface IEmployeeRepository
{
    Task<EmployeeEntity> GetByIdAsync(Guid id);  // No attribute needed
}
```

**Why**: For interface factories, every method is automatically remote. Adding operation attributes creates duplicate registrations.

---

## 3. Public Static Factory Methods

**Problem**: Conflicts with generated public method.

```csharp
// WRONG - your public method conflicts with generated code
[Factory]
public static partial class EmployeeCommands
{
    [Remote, Execute]
    public static Task<bool> SendNotification(...) { }  // Public!
}

// RIGHT - private implementation, generator creates public API
[Factory]
public static partial class EmployeeCommands
{
    [Remote, Execute]
    private static Task<bool> _SendNotification(...) { }  // Private with underscore
}
```

**Why**: The generator creates the public method that handles serialization. Your code provides the private implementation.

---

## 4. Private Property Setters

**Problem**: Breaks deserialization - properties won't restore after round-trip.

```csharp
// WRONG - won't deserialize
public Guid Id { get; private set; }
public string Name { get; private set; }

// RIGHT - public setters for serialization
public Guid Id { get; set; }
public string Name { get; set; }
```

**Why**: Generated serialization code uses property setters. Private setters aren't accessible from generated partial class.

---

## 5. Storing Method-Injected Services

**Problem**: Field will be null after client/server round-trip.

```csharp
// WRONG - service reference lost after serialization
[Factory]
public partial class Employee
{
    private IEmployeeRepository _repo;  // Will be null on client!

    [Remote, Create]
    public void Create([Service] IEmployeeRepository repo)
    {
        _repo = repo;  // Lost after serialization!
    }

    public void DoSomething()
    {
        _repo.GetAll();  // NullReferenceException on client!
    }
}

// RIGHT - use immediately, don't store
[Factory]
public partial class Employee
{
    [Remote, Create]
    public void Create([Service] IEmployeeRepository repo)
    {
        repo.Initialize(this);  // Use it here, don't store
    }
}
```

**Why**: Method-injected services are server-only. They aren't serialized because the client shouldn't have access to server services.

**Alternative**: Use constructor injection if you need the service on both sides:
```csharp
public Employee([Service] ILogger logger)  // Available on client AND server
{
    _logger = logger;
}
```

---

## 6. Missing `partial` Keyword

**Problem**: Won't compile - CS0260 error.

```csharp
// WRONG - missing partial
[Factory]
public class Employee { }

// RIGHT
[Factory]
public partial class Employee { }
```

**Why**: The generator creates a partial class to add `IOrdinalSerializable` implementation. Without `partial`, C# can't merge the generated code.

---

## 7. [Factory] on Implementation Classes

**Problem**: Duplicate registration when interface already has `[Factory]`.

```csharp
// WRONG - duplicate registration
[Factory]  // Interface has [Factory]
public interface IEmployeeRepository { }

[Factory]  // DON'T add [Factory] to implementation!
public class EmployeeRepository : IEmployeeRepository { }

// RIGHT - only interface has [Factory]
[Factory]
public interface IEmployeeRepository { }

public class EmployeeRepository : IEmployeeRepository { }  // No [Factory]
```

**Why**: The interface defines the factory contract. The implementation is just a service registered in DI.

---

## 8. [Execute] Returning Task (not Task<T>)

**Problem**: Client has nothing to await for confirmation.

```csharp
// WRONG - no return value
[Remote, Execute]
private static Task _ProcessEmployee(Guid id, [Service] IEmployeeService svc)
{
    return svc.ProcessAsync(id);
}

// RIGHT - returns a value
[Remote, Execute]
private static async Task<bool> _ProcessEmployee(Guid id, [Service] IEmployeeService svc)
{
    await svc.ProcessAsync(id);
    return true;
}
```

**Why**: Execute is request-response. The client needs a result to know the operation completed.

---

## 9. [Event] Missing CancellationToken

**Problem**: Events can't be cancelled during shutdown.

```csharp
// WRONG - can't cancel during shutdown
[Remote, Event]
private static Task _OnEmployeeCreated(Guid id, [Service] IEmailService svc)
{
    return svc.SendWelcomeEmail(id);
}

// RIGHT - supports graceful shutdown
[Remote, Event]
private static Task _OnEmployeeCreated(Guid id, [Service] IEmailService svc, CancellationToken ct)
{
    return svc.SendWelcomeEmail(id, ct);
}
```

**Why**: Events run in isolated scopes with fire-and-forget semantics. The CancellationToken comes from `IHostApplicationLifetime.ApplicationStopping` for graceful shutdown.

---

## Summary Table

| Anti-Pattern | Problem | Solution |
|--------------|---------|----------|
| [Remote] on children | N+1 calls | Remove [Remote] from child entities |
| Attributes on interface methods | Duplicate generation | Remove operation attributes |
| Public static methods | Name conflict | Use `private static _MethodName` |
| Private setters | Won't deserialize | Use public setters |
| Storing method services | Null after round-trip | Use immediately or constructor inject |
| Missing partial | Won't compile | Add `partial` keyword |
| [Factory] on implementation | Duplicate registration | Only on interface |
| [Execute] returning Task | No confirmation | Return Task<T> |
| [Event] missing CancellationToken | Can't cancel | Add as final parameter |
