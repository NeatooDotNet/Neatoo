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

<!-- snippet: remote-attribute-patterns -->
```cs
/// <summary>
/// Aggregate root entity with [Remote] operations.
/// All CRUD operations are callable from the client.
/// </summary>
public partial interface IRemotePerson : IEntityBase
{
    int Id { get; }
    string? FirstName { get; set; }
    string? LastName { get; set; }
    string? Email { get; set; }
}

[Factory]
internal partial class RemotePerson : EntityBase<RemotePerson>, IRemotePerson
{
    public RemotePerson(IEntityBaseServices<RemotePerson> services) : base(services) { }

    public partial int Id { get; set; }
    public partial string? FirstName { get; set; }
    public partial string? LastName { get; set; }
    public partial string? Email { get; set; }

    [Remote]  // Callable from client
    [Fetch]
    public void Fetch(int id)
    {
        Id = id;
        FirstName = "John";
        LastName = "Doe";
        Email = "john@example.com";
    }

    [Remote]
    [Insert]
    public Task Insert() => Task.CompletedTask;

    [Remote]
    [Update]
    public Task Update() => Task.CompletedTask;

    [Remote]
    [Delete]
    public Task Delete() => Task.CompletedTask;

    [Create]
    public void Create() { }
}
```
<!-- endSnippet -->

### When to Use [Remote]

| Scenario | Use [Remote]? |
|----------|---------------|
| Aggregate root operations | Yes |
| Child entity operations | No - saved through parent |
| Value Object fetch (simple POCO) | Yes (via [RemoteFactory](https://github.com/NeatooDotNet/RemoteFactory/tree/main/docs)) |
| Methods needing database access | Yes |

### Child Entities

Child entities don't need `[Remote]` since they're managed through the aggregate root:

<!-- snippet: aggregate-vs-child-patterns -->
```cs
/// <summary>
/// Aggregate Root - needs [Remote] for client-server communication.
/// </summary>
public partial interface IRemoteOrder : IEntityBase
{
    int Id { get; }
    string? CustomerName { get; set; }
}

[Factory]
internal partial class RemoteOrder : EntityBase<RemoteOrder>, IRemoteOrder
{
    public RemoteOrder(IEntityBaseServices<RemoteOrder> services) : base(services) { }

    public partial int Id { get; set; }
    public partial string? CustomerName { get; set; }

    [Remote]
    [Fetch]
    public void Fetch(int id)
    {
        // Load order from database
        Id = id;
        CustomerName = "Acme Corp";
    }

    [Remote]
    [Update]
    public Task Update()
    {
        // Save order and all children
        return Task.CompletedTask;
    }

    [Create]
    public void Create() { }
}

/// <summary>
/// Child Entity - NO [Remote] needed.
/// Saved through parent's operations.
/// </summary>
public partial interface IRemoteOrderLineItem : IEntityBase
{
    int Id { get; }
    string? ProductName { get; set; }
    int Quantity { get; set; }
}

[Factory]
internal partial class RemoteOrderLineItem : EntityBase<RemoteOrderLineItem>, IRemoteOrderLineItem
{
    public RemoteOrderLineItem(IEntityBaseServices<RemoteOrderLineItem> services) : base(services) { }

    public partial int Id { get; set; }
    public partial string? ProductName { get; set; }
    public partial int Quantity { get; set; }

    [Fetch]  // Called by Order.Fetch internally - no [Remote]
    public void Fetch(int id, string productName, int quantity)
    {
        Id = id;
        ProductName = productName;
        Quantity = quantity;
    }

    [Insert]  // Called by Order.Update internally - no [Remote]
    public Task Insert()
    {
        return Task.CompletedTask;
    }

    [Create]
    public void Create() { }
}
```
<!-- endSnippet -->

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

<!-- snippet: email-validation-rule -->
```cs
/// <summary>
/// Interface for entities with email validation.
/// </summary>
public interface IPersonWithEmail : IValidateBase
{
    string? Email { get; set; }
}

/// <summary>
/// Sync validation rule for email format.
/// Executes on both client and server.
/// </summary>
public class EmailFormatValidationRule : RuleBase<IPersonWithEmail>
{
    public EmailFormatValidationRule() : base(p => p.Email) { }

    protected override IRuleMessages Execute(IPersonWithEmail target)
    {
        if (string.IsNullOrEmpty(target.Email))
            return (nameof(target.Email), "Email is required").AsRuleMessages();

        if (!target.Email.Contains('@'))
            return (nameof(target.Email), "Invalid email format").AsRuleMessages();

        return None;
    }
}
```
<!-- endSnippet -->

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

<!-- snippet: unique-email-rule-async -->
```cs
/// <summary>
/// Service interface for email uniqueness checking.
/// Different implementations for client vs server.
/// </summary>
public interface IEmailExistsService
{
    Task<bool> ExistsAsync(string email);
}

/// <summary>
/// Async rule that checks email uniqueness via service.
/// Service implementation differs on client vs server.
/// </summary>
public class UniqueEmailAsyncRule : AsyncRuleBase<IPersonWithEmail>
{
    private readonly IEmailExistsService _emailService;

    public UniqueEmailAsyncRule(IEmailExistsService emailService) : base(p => p.Email)
    {
        _emailService = emailService;
    }

    protected override async Task<IRuleMessages> Execute(IPersonWithEmail target, CancellationToken? token = null)
    {
        if (string.IsNullOrEmpty(target.Email))
            return None;

        // On client: calls server API
        // On server: calls database directly
        if (await _emailService.ExistsAsync(target.Email))
            return (nameof(target.Email), "Email already exists").AsRuleMessages();

        return None;
    }
}
```
<!-- endSnippet -->

### Client Implementation

<!-- snippet: client-email-service -->
```cs
/// <summary>
/// Client-side email service that calls the server API.
/// Registered in Blazor WASM DI container.
/// </summary>
public class ClientEmailExistsService : IEmailExistsService
{
    private readonly HttpClient _http;

    public ClientEmailExistsService(HttpClient http)
    {
        _http = http;
    }

    public async Task<bool> ExistsAsync(string email)
    {
        // Call server API endpoint
        var uri = new Uri($"/api/email/exists?email={Uri.EscapeDataString(email)}", UriKind.Relative);
        var response = await _http.GetAsync(uri);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>();
    }
}
```
<!-- endSnippet -->

### Server Implementation

<!-- snippet: server-email-service -->
```cs
/// <summary>
/// Server-side email service that queries the database.
/// Registered in ASP.NET Core DI container.
/// </summary>
public class ServerEmailExistsService : IEmailExistsService
{
    private readonly IUserRepository _repository;

    public ServerEmailExistsService(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> ExistsAsync(string email)
    {
        return await _repository.EmailExistsAsync(email);
    }
}

/// <summary>
/// Repository interface for user data access.
/// </summary>
public interface IUserRepository
{
    Task<bool> EmailExistsAsync(string email);
}
```
<!-- endSnippet -->

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

<!-- snippet: authorization-pattern -->
```cs
/// <summary>
/// Authorization interface for Person entity operations.
/// </summary>
public interface IPersonAuth
{
    bool CanCreate();
    bool CanFetch();
    bool CanInsert();
    bool CanUpdate();
    bool CanDelete();
}

/// <summary>
/// Authorization implementation that checks user permissions.
/// Injected via DI on the server.
/// </summary>
public class PersonAuth : IPersonAuth
{
    private readonly IUserPermissions _user;

    public PersonAuth(IUserPermissions user) => _user = user;

    public bool CanCreate() => _user.HasPermission("Person.Create");
    public bool CanFetch() => _user.HasPermission("Person.Read");
    public bool CanInsert() => _user.HasPermission("Person.Create");
    public bool CanUpdate() => _user.HasPermission("Person.Update");
    public bool CanDelete() => _user.HasPermission("Person.Delete");
}

/// <summary>
/// Interface for checking user permissions.
/// </summary>
public interface IUserPermissions
{
    bool HasPermission(string permission);
}
```
<!-- endSnippet -->

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
