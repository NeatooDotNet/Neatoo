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

Register Neatoo services for server mode:

<!-- snippet: server-di-setup -->
```cs
builder.Services.AddNeatooServices(NeatooFactory.Server, typeof(IPerson).Assembly);
```
<!-- endSnippet -->

Map the remote factory endpoint:

<!-- snippet: server-endpoint -->
```cs
app.MapPost("/api/neatoo", (HttpContext httpContext, RemoteRequestDto request, CancellationToken cancellationToken) =>
{
    var handleRemoteDelegateRequest = httpContext.RequestServices.GetRequiredService<HandleRemoteDelegateRequest>();
    return handleRemoteDelegateRequest(request, cancellationToken);
});
```
<!-- endSnippet -->

### Client Setup

Register Neatoo services for remote mode and configure the HTTP client:

<!-- snippet: client-di-setup -->
```cs
builder.Services.AddNeatooServices(NeatooFactory.Remote, typeof(IPerson).Assembly);
builder.Services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
    new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
```
<!-- endSnippet -->

## [Remote] Attribute

Mark factory operations that should be callable from the client:

<!-- pseudo:remote-attribute-patterns -->
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
<!-- /snippet -->

### When to Use [Remote]

| Scenario | Use [Remote]? |
|----------|---------------|
| Aggregate root operations | Yes |
| Child entity operations | No - saved through parent |
| Value Object fetch (simple POCO) | Yes (via [RemoteFactory](https://github.com/NeatooDotNet/RemoteFactory/tree/main/docs)) |
| Methods needing database access | Yes |

### Child Entities

Child entities don't need `[Remote]` since they're managed through the aggregate root:

<!-- pseudo:aggregate-vs-child-patterns -->
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
<!-- /snippet -->

## State Transfer

### What Gets Serialized

The Remote Factory serializes:
- Property values (from partial properties)
- Validation state (rule messages)
- Modification tracking (IsModified, IsNew, etc.)
- Parent-child relationships
- Deleted item lists

### Serialization Behavior

<!-- pseudo:serialization-behavior -->
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
<!-- /snippet -->

### Object Identity After Remote Operations

When using Remote Factory, understand that **remote operations return new object instances**:

<!-- pseudo:object-identity-demo -->
```csharp
var person = await personFactory.Create();
var originalReference = person;

person = await personFactory.Save(person);

// These are DIFFERENT objects
Console.WriteLine(ReferenceEquals(originalReference, person));  // false
```
<!-- /snippet -->

This occurs because:
1. The object is serialized (converted to data)
2. Transmitted to the server
3. A new instance is created on the server
4. Server performs the operation
5. The result is serialized back
6. A **new instance** is deserialized on the client

#### Implications

| Operation | Returns New Instance? | Must Reassign? |
|-----------|----------------------|----------------|
| `Create()` | Yes | Yes (to variable) |
| `Fetch()` | Yes | Yes (to variable) |
| `Save()` | Yes | **Yes - Critical!** |
| `Delete()` | N/A | N/A |

Always treat remote factory operations as returning fresh instances that must be captured.

#### Common Mistake

<!-- invalid:wrong-save-pattern -->
```csharp
// WRONG - discards the new instance
await personFactory.Save(person);
// person is still the OLD pre-save instance!

// CORRECT - captures the new instance
person = await personFactory.Save(person);
// person is now the updated post-save instance
```
<!-- /snippet -->

See [Factory Operations](factory-operations.md#critical-always-reassign-after-save) for detailed guidance.

## Client-Side Business Rules

Rules execute on both client and server:

<!-- pseudo:email-validation-rule -->
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
<!-- /snippet -->

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

<!-- pseudo:unique-email-rule-async -->
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
<!-- /snippet -->

### Client Implementation

<!-- pseudo:client-email-service -->
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
<!-- /snippet -->

### Server Implementation

<!-- pseudo:server-email-service -->
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
<!-- /snippet -->

## Service Injection

Services are resolved from DI on the server:

<!-- pseudo:service-injection -->
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
<!-- /snippet -->

The client doesn't need these services registered since the operation executes on server.

## Return Values

Factory operations can return data to the client:

<!-- pseudo:insert-with-mapto -->
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
<!-- /snippet -->

### Capture Return Values

<!-- pseudo:capture-return-values -->
```csharp
// Important: capture the returned entity
person = await personFactory.Save(person);
Console.WriteLine(person.Id);  // Has the generated ID
```
<!-- /snippet -->

## Error Handling

### Server Exceptions

Exceptions on the server are propagated to the client:

<!-- pseudo:exception-handling -->
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
<!-- /snippet -->

### Validation Failures

When validation fails, the entity is returned with validation messages:

<!-- pseudo:validation-failure-handling -->
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
<!-- /snippet -->

## Authorization

> **Note:** Authorization is provided by RemoteFactory. For comprehensive documentation, see the [RemoteFactory documentation](https://github.com/NeatooDotNet/RemoteFactory/tree/main/docs).

Authorization is checked on the server:

<!-- pseudo:authorization-pattern -->
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
<!-- /snippet -->

### Client Authorization Display

The factory exposes authorization methods:

<!-- pseudo:client-authorization-display -->
```razor
@inject IPersonFactory PersonFactory

@if (PersonFactory.CanCreate())
{
    <MudButton OnClick="CreatePerson">New Person</MudButton>
}
```
<!-- /snippet -->

## Endpoint Configuration

### Custom Endpoint Path

<!-- pseudo:custom-endpoint-path -->
```csharp
app.MapPost("/my-custom-path", async (HttpContext ctx, RemoteRequestDto request) =>
    await NeatooEndpoint.HandleRequest(ctx, request));
```
<!-- /snippet -->

### Client Configuration

<!-- pseudo:client-endpoint-config -->
```csharp
// Configure the endpoint URL
builder.Services.AddNeatooServices(NeatooFactory.Remote, typeof(IMyAggregate).Assembly, options =>
{
    options.Endpoint = "/my-custom-path";
});
```
<!-- /snippet -->

## Single Endpoint Architecture

Neatoo uses a single endpoint for all operations:

```json
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

<!-- pseudo:validate-before-save -->
```csharp
[Update]
public async Task Update([Service] IDbContext db)
{
    await RunRules();        // Run validation
    if (!IsSavable) return;  // Check result
    // ... persist
}
```
<!-- /snippet -->

### 2. Keep Operations Focused

<!-- pseudo:keep-operations-focused -->
```csharp
// Good: Single responsibility
[Fetch]
public async Task Fetch(int id, [Service] IDbContext db) { }

// Avoid: Multiple concerns
[Fetch]
public async Task FetchWithStats(int id, [Service] IDbContext db, [Service] IStatsService stats) { }
```
<!-- /snippet -->

### 3. Handle Concurrency

<!-- pseudo:concurrency-handling -->
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
<!-- /snippet -->

## See Also

- [Factory Operations](factory-operations.md) - Operation attributes
- [Installation](installation.md) - Server and client setup
- [Blazor Binding](blazor-binding.md) - Client UI patterns
