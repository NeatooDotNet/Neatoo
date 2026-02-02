# Interface Factory Pattern

Use for remote services where the client calls through a generated proxy. The implementation lives only on the server.

## Complete Example

<!-- snippet: skill-interface-factory-complete -->
<a id='snippet-skill-interface-factory-complete'></a>
```cs
[Factory]
public interface IEmployeeQueryService
{
    Task<IReadOnlyList<EmployeeDto>> GetAllAsync();
    Task<EmployeeDto?> GetByIdAsync(int id);
    Task<int> CountAsync();
}

// Server implementation (no [Factory] attribute)
public class EmployeeQueryService : IEmployeeQueryService
{
    private readonly List<EmployeeDto> _employees = new()
    {
        new EmployeeDto { Id = 1, Name = "John Doe", Department = "Engineering" },
        new EmployeeDto { Id = 2, Name = "Jane Smith", Department = "Marketing" }
    };

    public Task<IReadOnlyList<EmployeeDto>> GetAllAsync()
    {
        return Task.FromResult<IReadOnlyList<EmployeeDto>>(_employees);
    }

    public Task<EmployeeDto?> GetByIdAsync(int id)
    {
        var employee = _employees.FirstOrDefault(e => e.Id == id);
        return Task.FromResult(employee);
    }

    public Task<int> CountAsync()
    {
        return Task.FromResult(_employees.Count);
    }
}

public class EmployeeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Skill/InterfaceFactorySamples.cs#L5-L46' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-interface-factory-complete' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Generates**: Proxy implementation that serializes calls to server.

---

## Critical Rules

### Interface methods do NOT need operation attributes

```csharp
// WRONG - causes duplicate generation
[Factory]
public interface IMyRepository
{
    [Fetch]  // DON'T DO THIS
    Task<Item> GetByIdAsync(int id);
}

// RIGHT - no attributes needed
[Factory]
public interface IMyRepository
{
    Task<Item> GetByIdAsync(int id);
}
```

The interface IS the remote boundary. Every method crosses it automatically.

### Server implementation does NOT have [Factory]

```csharp
// WRONG - causes duplicate registration
[Factory]
public class EmployeeRepository : IEmployeeRepository { }

// RIGHT - no [Factory] on implementation
public class EmployeeRepository : IEmployeeRepository { }
```

The interface already has `[Factory]`; the implementation is just a service.

---

## Server Registration

Register the implementation in DI:

```csharp
// Program.cs
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
```

Or use convention-based registration:
```csharp
builder.Services.RegisterMatchingName<IEmployeeRepository>();  // Auto-finds EmployeeRepository
```

---

## When to Use Interface Factory

- **Remote services without entity identity** - Query services, report generators
- **Clean separation** - Interface defines contract, server provides implementation
- **Multiple implementations** - Can swap implementations via DI
- **Third-party integrations** - Wrap external APIs behind interface
