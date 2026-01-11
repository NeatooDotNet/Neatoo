# Neatoo Client-Server Architecture Reference

## Overview

Neatoo.RemoteFactory enables sharing domain models between Blazor WebAssembly clients and ASP.NET Core servers. A single HTTP endpoint handles all factory operations with automatic serialization.

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    Blazor WebAssembly Client                     │
│                                                                  │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐       │
│  │ PersonFactory │    │ OrderFactory │    │ Other Factory│       │
│  └──────┬───────┘    └──────┬───────┘    └──────┬───────┘       │
│         │                   │                   │                │
│         └───────────────────┼───────────────────┘                │
│                             │                                    │
│                    ┌────────▼────────┐                           │
│                    │ RemoteNeatoo    │                           │
│                    │ Portal (Client) │                           │
│                    └────────┬────────┘                           │
└─────────────────────────────┼────────────────────────────────────┘
                              │
                        HTTP POST
                      /api/neatoo
                              │
┌─────────────────────────────┼────────────────────────────────────┐
│                    ASP.NET Core Server                           │
│                             │                                    │
│                    ┌────────▼────────┐                           │
│                    │ NeatooJsonPortal│                           │
│                    │    (Server)     │                           │
│                    └────────┬────────┘                           │
│                             │                                    │
│         ┌───────────────────┼───────────────────┐                │
│         │                   │                   │                │
│  ┌──────▼───────┐    ┌──────▼───────┐    ┌──────▼───────┐       │
│  │ PersonFactory │    │ OrderFactory │    │ Other Factory│       │
│  └──────┬───────┘    └──────┬───────┘    └──────┬───────┘       │
│         │                   │                   │                │
│  ┌──────▼───────┐    ┌──────▼───────┐    ┌──────▼───────┐       │
│  │  DbContext   │    │  DbContext   │    │   Services   │       │
│  └──────────────┘    └──────────────┘    └──────────────┘       │
└──────────────────────────────────────────────────────────────────┘
```

## Server Setup

### Server DI Configuration

<!-- snippet: server-di-setup -->
```csharp
builder.Services.AddNeatooServices(NeatooFactory.Server, typeof(IPerson).Assembly);
```
<!-- /snippet -->

### Server Endpoint Mapping

<!-- snippet: server-endpoint -->
```csharp
app.MapPost("/api/neatoo", (HttpContext httpContext, RemoteRequestDto request, CancellationToken cancellationToken) =>
{
    var handleRemoteDelegateRequest = httpContext.RequestServices.GetRequiredService<HandleRemoteDelegateRequest>();
    return handleRemoteDelegateRequest(request, cancellationToken);
});
```
<!-- /snippet -->

### NeatooFactory Modes

| Mode | Description | Use Case |
|------|-------------|----------|
| `NeatooFactory.Server` | Executes operations locally with ASP.NET Core | Production server |
| `NeatooFactory.Remote` | Proxies operations to remote server | Blazor WebAssembly client |
| `NeatooFactory.Logical` | Executes operations locally without server infrastructure | Testing, WPF |

## Client Setup (Blazor WebAssembly)

### Client DI Configuration

<!-- snippet: client-di-setup -->
```csharp
builder.Services.AddNeatooServices(NeatooFactory.Remote, typeof(IPerson).Assembly);
builder.Services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
    new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
```
<!-- /snippet -->

## Project Structure

### Recommended Solution Layout

```
Solution/
├── MyApp.Shared/           # Shared interfaces and contracts
│   ├── Interfaces/
│   │   ├── IPerson.cs
│   │   └── IOrder.cs
│   └── DTOs/
│       └── CustomerResult.cs
│
├── MyApp.Domain/           # Domain entities and business logic
│   ├── Entities/
│   │   ├── Person.cs
│   │   └── Order.cs
│   ├── Rules/
│   │   └── EmailValidationRule.cs
│   └── Services/
│       └── IEmailService.cs
│
├── MyApp.Server/           # ASP.NET Core server
│   ├── Program.cs
│   ├── Data/
│   │   └── AppDbContext.cs
│   └── Services/
│       └── EmailService.cs
│
└── MyApp.Client/           # Blazor WebAssembly client
    └── Program.cs
```

### Assembly References

| Project | References |
|---------|------------|
| Shared | Neatoo (interfaces only) |
| Domain | Shared, Neatoo |
| Server | Domain, Shared, Neatoo.RemoteFactory |
| Client | Shared, Neatoo.RemoteFactory |

### Key Principle

- **Client** references interfaces (IPerson) from Shared
- **Server** references implementations (Person) from Domain
- **Both** can create/manipulate entities through factories

## [Remote] Attribute

The `[Remote]` attribute marks methods that execute on the server.

### When Methods Run

| Scenario | [Create] | [Fetch]/[Insert]/[Update]/[Delete] |
|----------|----------|-------------------------------------|
| Client calls factory | Client | Server (via HTTP) |
| Server calls factory | Server | Server (local) |

## Serialization

Neatoo uses System.Text.Json with custom converters for:

- Entity graphs with circular references
- Interface-based deserialization
- Meta-property preservation

### Ordinal Serialization (10.2.0+)

Ordinal serialization reduces payload sizes by 40-50% by using numeric indices instead of property names.

## Error Handling

### Factory Return Values

| Method | Success | Failure |
|--------|---------|---------|
| `Create()` | Entity instance | null (if unauthorized) |
| `Fetch()` | Entity instance | null (not found or unauthorized) |
| `Save()` | Updated entity | null (if unauthorized) or throws |
| `TrySave()` | `Authorized<T>` with value | `Authorized<T>` with message |
| `Execute()` | Updated object | Throws on error |

## Performance Considerations

### Minimize Round Trips

Fetch aggregates with children in one call instead of multiple separate fetches.

### Payload Size

Only modified properties serialize for updates.

## WPF Applications

For WPF apps, use `NeatooFactory.Logical` mode since there's no client-server split.

## Cancellation Support

Cancellation flows from client to server via HTTP connection state. Both `HttpContext.RequestAborted` and `IHostApplicationLifetime.ApplicationStopping` trigger cancellation.

### Best Practices

1. **Pass CancellationToken to all async calls** - EF Core, HttpClient, file I/O
2. **Check cancellation at safe points** - Before expensive operations
3. **Use transactions for atomicity** - Cancellation between DB calls leaves partial state
4. **Handle OperationCanceledException** - Don't let it crash your UI

## Common Pitfalls

1. **Wrong NeatooFactory mode** - Server needs `Server`, Client needs `Remote`, Standalone (WPF/tests) needs `Logical`
2. **Missing [Remote] attribute** - Server methods won't execute on server
3. **Referencing Domain from Client** - Client should only reference Shared
4. **Not configuring authentication** - Endpoint unprotected by default
5. **Large payloads** - Include only needed data in responses
6. **Ignoring CancellationToken** - Operations continue after client disconnects
