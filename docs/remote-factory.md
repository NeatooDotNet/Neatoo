# Remote Factory Pattern

Neatoo's Remote Factory enables client-server state transfer for Blazor WebAssembly applications. The domain model code executes on both client and server, with factory operations routing to the server when needed.

> **About RemoteFactory:** Neatoo depends on [RemoteFactory](https://github.com/NeatooDotNet/RemoteFactory), a companion library that provides factory generation, client-server state transfer, authorization, and value object support. Together, Neatoo and RemoteFactory form a complete DDD framework. For detailed RemoteFactory documentation, see the [RemoteFactory GitHub repository](https://github.com/NeatooDotNet/RemoteFactory/tree/main/docs).

## Overview

```
Client (Blazor WASM)                    Server (ASP.NET Core)

IPersonFactory.Fetch(id)  ----------->  Person.Fetch() executes
                          <-----------  Person state serialized
Person with full state

person.Name = "John"
(rules execute locally)

IPersonFactory.Save()     ----------->  Person.Update() executes
                          <-----------  Updated Person state
```

## Configuration

### Server Setup

```csharp
// Program.cs
builder.Services.AddNeatooServices(NeatooFactory.Server, typeof(IMyAggregate).Assembly);

app.MapPost("/api/neatoo", async (HttpContext ctx, RemoteRequestDto request) =>
    await NeatooEndpoint.HandleRequest(ctx, request));
```

### Client Setup

```csharp
// Program.cs
builder.Services.AddNeatooServices(NeatooFactory.Remote, typeof(IMyAggregate).Assembly);

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("https://api.myapp.com")
});
```

## [Remote] Attribute

Mark factory operations that should be callable from the client:

```csharp
[Factory]
internal partial class Person : EntityBase<Person>, IPerson
{
    [Remote]  // Callable from client
    [Fetch]
    public async Task Fetch(int id, [Service] IDbContext db) { }

    [Remote]
    [Insert]
    public async Task Insert([Service] IDbContext db) { }

    [Remote]
    [Update]
    public async Task Update([Service] IDbContext db) { }

    [Remote]
    [Delete]
    public async Task Delete([Service] IDbContext db) { }
}
```

### When to Use [Remote]

| Scenario | Use [Remote]? |
|----------|---------------|
| Aggregate root operations | Yes |
| Child entity operations | No - saved through parent |
| Value object fetch | Yes (via [RemoteFactory](https://github.com/NeatooDotNet/RemoteFactory/tree/main/docs)) |
| Methods needing database access | Yes |

### Child Entities

Child entities don't need `[Remote]` since they're managed through the aggregate root:

```csharp
// Aggregate Root - needs [Remote]
[Factory]
internal partial class Order : EntityBase<Order>, IOrder
{
    [Remote]
    [Fetch]
    public async Task Fetch(int id, [Service] IOrderDbContext db) { }

    [Remote]
    [Update]
    public async Task Update([Service] IOrderDbContext db) { }
}

// Child Entity - NO [Remote]
[Factory]
internal partial class OrderLineItem : EntityBase<OrderLineItem>, IOrderLineItem
{
    [Fetch]  // Called by Order.Fetch internally
    public void Fetch(LineItemEntity entity) { }

    [Insert]  // Called by Order.Update internally
    public void Insert(LineItemEntity entity) { }
}
```

## State Transfer

### What Gets Serialized

The Remote Factory serializes:
- Property values (from partial properties)
- Validation state (rule messages)
- Modification tracking (IsModified, IsNew, etc.)
- Parent-child relationships
- Deleted item lists

### Serialization Behavior

```csharp
// Client creates entity
var person = personFactory.Create();
person.FirstName = "John";
person.LastName = "Doe";

// State is serialized to JSON
{
    "FirstName": "John",
    "LastName": "Doe",
    "IsNew": true,
    "IsModified": true,
    "_propertyState": { ... }
}

// Sent to server, executed, returned
person = await personFactory.Save(person);
// person now has server-generated ID, validation state, etc.
```

## Client-Side Business Rules

Rules execute on both client and server:

```csharp
// Rule defined once
public class EmailValidationRule : RuleBase<IPerson>
{
    public EmailValidationRule() : base(p => p.Email) { }

    protected override IRuleMessages Execute(IPerson target)
    {
        if (string.IsNullOrEmpty(target.Email))
            return (nameof(target.Email), "Email is required").AsRuleMessages();
        return None;
    }
}
```

### Client Behavior
- Rule executes when Email property changes
- Immediate UI feedback
- `IsValid` updates in real-time

### Server Behavior
- Rule executes again in Insert/Update via `RunRules()`
- Server-authoritative validation
- Prevents bypassing client-side rules

## Async Rules on Client

Async rules work on client but may behave differently:

```csharp
public class UniqueEmailRule : AsyncRuleBase<IPerson>
{
    private readonly IEmailService _emailService;

    public UniqueEmailRule(IEmailService emailService) : base(p => p.Email)
    {
        _emailService = emailService;
    }

    protected override async Task<IRuleMessages> Execute(IPerson target, CancellationToken? token)
    {
        // On client: calls server API
        // On server: calls database directly
        if (await _emailService.ExistsAsync(target.Email))
            return (nameof(target.Email), "Email already exists").AsRuleMessages();
        return None;
    }
}
```

### Client Implementation

```csharp
// Client-side service calls the server
public class ClientEmailService : IEmailService
{
    private readonly HttpClient _http;

    public async Task<bool> ExistsAsync(string email)
    {
        // Call server API
        return await _http.GetFromJsonAsync<bool>($"/api/email/exists?email={email}");
    }
}
```

### Server Implementation

```csharp
// Server-side service queries database
public class ServerEmailService : IEmailService
{
    private readonly IDbContext _db;

    public async Task<bool> ExistsAsync(string email)
    {
        return await _db.Users.AnyAsync(u => u.Email == email);
    }
}
```

## Service Injection

Services are resolved from DI on the server:

```csharp
[Insert]
public async Task Insert(
    [Service] IDbContext db,           // Server DI
    [Service] IEmailService email,     // Server DI
    [Service] IAuditService audit)     // Server DI
{
    // All services are server-side implementations
}
```

The client doesn't need these services registered since the operation executes on server.

## Return Values

Factory operations can return data to the client:

```csharp
[Remote]
[Insert]
public async Task<PersonEntity?> Insert([Service] IDbContext db)
{
    await RunRules();
    if (!IsSavable) return null;

    var entity = new PersonEntity();
    MapTo(entity);
    db.Persons.Add(entity);
    await db.SaveChangesAsync();

    // Capture database-generated ID
    this.Id = entity.Id;

    return entity;  // Optionally return for caller
}
```

### Capture Return Values

```csharp
// Important: capture the returned entity
person = await personFactory.Save(person);
Console.WriteLine(person.Id);  // Has the generated ID
```

## Error Handling

### Server Exceptions

Exceptions on the server are propagated to the client:

```csharp
try
{
    person = await personFactory.Save(person);
}
catch (Exception ex)
{
    // Server exception message available
    Console.WriteLine(ex.Message);
}
```

### Validation Failures

When validation fails, the entity is returned with validation messages:

```csharp
person = await personFactory.Save(person);

if (!person.IsValid)
{
    // Validation failed on server
    foreach (var msg in person.PropertyMessages)
    {
        Console.WriteLine($"{msg.Property.Name}: {msg.Message}");
    }
}
```

## Authorization

> **Note:** Authorization is provided by RemoteFactory. For comprehensive documentation, see the [RemoteFactory documentation](https://github.com/NeatooDotNet/RemoteFactory/tree/main/docs).

Authorization is checked on the server:

```csharp
[AuthorizeFactory<IPersonAuth>]
internal partial class Person : EntityBase<Person>, IPerson { }

public class PersonAuth : IPersonAuth
{
    private readonly ICurrentUser _user;

    public PersonAuth(ICurrentUser user) => _user = user;

    public bool CanCreate() => _user.HasPermission("Person.Create");
    public bool CanFetch() => _user.HasPermission("Person.Read");
    public bool CanInsert() => _user.HasPermission("Person.Create");
    public bool CanUpdate() => _user.HasPermission("Person.Update");
    public bool CanDelete() => _user.HasPermission("Person.Delete");
}
```

### Client Authorization Display

The factory exposes authorization methods:

```razor
@inject IPersonFactory PersonFactory

@if (PersonFactory.CanCreate())
{
    <MudButton OnClick="CreatePerson">New Person</MudButton>
}
```

## Endpoint Configuration

### Custom Endpoint Path

```csharp
app.MapPost("/my-custom-path", async (HttpContext ctx, RemoteRequestDto request) =>
    await NeatooEndpoint.HandleRequest(ctx, request));
```

### Client Configuration

```csharp
// Configure the endpoint URL
builder.Services.AddNeatooServices(NeatooFactory.Remote, typeof(IMyAggregate).Assembly, options =>
{
    options.Endpoint = "/my-custom-path";
});
```

## Single Endpoint Architecture

Neatoo uses a single endpoint for all operations:

```
POST /api/neatoo
{
    "TypeName": "DomainModel.Person, DomainModel",
    "OperationType": "Fetch",
    "Parameters": { "id": 42 },
    "EntityState": null
}
```

This eliminates the need for:
- Individual controllers per entity
- DTOs for each operation
- Manual serialization logic

## Best Practices

### 1. Validate Before Save

```csharp
[Update]
public async Task Update([Service] IDbContext db)
{
    await RunRules();        // Run validation
    if (!IsSavable) return;  // Check result
    // ... persist
}
```

### 2. Keep Operations Focused

```csharp
// Good: Single responsibility
[Fetch]
public async Task Fetch(int id, [Service] IDbContext db) { }

// Avoid: Multiple concerns
[Fetch]
public async Task FetchWithStats(int id, [Service] IDbContext db, [Service] IStatsService stats) { }
```

### 3. Handle Concurrency

```csharp
[Update]
public async Task Update([Service] IDbContext db)
{
    var entity = await db.Persons.FindAsync(Id);
    if (entity == null)
        throw new KeyNotFoundException("Person not found");

    if (entity.RowVersion != this.RowVersion)
        throw new ConcurrencyException("Entity was modified");

    MapModifiedTo(entity);
    await db.SaveChangesAsync();
}
```

## See Also

- [Factory Operations](factory-operations.md) - Operation attributes
- [Installation](installation.md) - Server and client setup
- [Blazor Binding](blazor-binding.md) - Client UI patterns
