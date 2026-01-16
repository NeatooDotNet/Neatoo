# Client-Server Setup

How to configure Neatoo with RemoteFactory for Blazor WebAssembly.

## Architecture Overview

```
┌─────────────────────────────────────────────┐
│         Blazor WebAssembly Client           │
│                                             │
│  ┌─────────────┐    ┌─────────────┐        │
│  │ OrderFactory│    │PersonFactory│        │
│  └──────┬──────┘    └──────┬──────┘        │
│         └──────────┬───────┘               │
│                    │                        │
│         ┌──────────▼──────────┐            │
│         │ RemoteNeatoo Portal │            │
│         └──────────┬──────────┘            │
└────────────────────┼────────────────────────┘
                     │ HTTP POST /api/neatoo
┌────────────────────┼────────────────────────┐
│         ASP.NET Core Server                 │
│                    │                        │
│         ┌──────────▼──────────┐            │
│         │ NeatooJsonPortal    │            │
│         └──────────┬──────────┘            │
│                    │                        │
│  ┌─────────────┐   │   ┌─────────────┐     │
│  │ OrderFactory│◄──┼──►│PersonFactory│     │
│  └──────┬──────┘   │   └──────┬──────┘     │
│         │          │          │            │
│  ┌──────▼──────┐   │   ┌──────▼──────┐     │
│  │  DbContext  │   │   │  Services   │     │
│  └─────────────┘   │   └─────────────┘     │
└────────────────────────────────────────────┘
```

## NeatooFactory Modes

| Mode | Use Case | Behavior |
|------|----------|----------|
| `NeatooFactory.Server` | ASP.NET Core server | Executes locally with full service access |
| `NeatooFactory.Remote` | Blazor WebAssembly | Proxies to server via HTTP |
| `NeatooFactory.Logical` | WPF, testing | Executes locally without server infrastructure |

## Project Structure

```
Solution/
├── MyApp.Shared/           # Shared interfaces and DTOs
│   └── Interfaces/
│       ├── IPerson.cs
│       └── IOrder.cs
│
├── MyApp.Domain/           # Domain entities (NOT referenced by client)
│   ├── Person.cs
│   └── Order.cs
│
├── MyApp.Server/           # ASP.NET Core
│   └── Program.cs
│
└── MyApp.Client/           # Blazor WebAssembly
    └── Program.cs
```

**Key Principle:**
- **Client** references **Shared** only (interfaces)
- **Server** references **Domain** and **Shared**
- Domain implementations are never in client bundle

## Server Setup

### 1. Add NuGet Packages

```xml
<PackageReference Include="Neatoo" Version="10.*" />
<PackageReference Include="Neatoo.RemoteFactory.AspNetCore" Version="10.*" />
```

### 2. Register Services (Program.cs)

```csharp
// Register Neatoo services in Server mode
builder.Services.AddNeatooServices(
    NeatooFactory.Server,
    typeof(IPerson).Assembly,    // Shared interfaces
    typeof(Person).Assembly);    // Domain implementations

// Register your own services
builder.Services.AddDbContext<AppDbContext>();
builder.Services.AddScoped<IEmailService, EmailService>();
```

### 3. Map Endpoint (Program.cs)

```csharp
// Single endpoint handles all Neatoo operations
app.MapPost("/api/neatoo", (HttpContext httpContext, RemoteRequestDto request, CancellationToken cancellationToken) =>
{
    var handleRemoteDelegateRequest = httpContext.RequestServices.GetRequiredService<HandleRemoteDelegateRequest>();
    return handleRemoteDelegateRequest(request, cancellationToken);
});
```

### Complete Server Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddNeatooServices(
    NeatooFactory.Server,
    typeof(IPerson).Assembly,
    typeof(Person).Assembly);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// Add CORS for Blazor client
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("https://localhost:5002")
              .AllowAnyMethod()
              .AllowAnyHeader());
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors();

// Neatoo endpoint
app.MapPost("/api/neatoo", (HttpContext ctx, RemoteRequestDto request, CancellationToken token) =>
{
    var handler = ctx.RequestServices.GetRequiredService<HandleRemoteDelegateRequest>();
    return handler(request, token);
});

app.Run();
```

## Client Setup (Blazor WebAssembly)

### 1. Add NuGet Packages

```xml
<PackageReference Include="Neatoo" Version="10.*" />
<PackageReference Include="Neatoo.Blazor.MudNeatoo" Version="10.*" />  <!-- Optional: MudBlazor components -->
```

### 2. Register Services (Program.cs)

```csharp
// Register Neatoo services in Remote mode
builder.Services.AddNeatooServices(
    NeatooFactory.Remote,
    typeof(IPerson).Assembly);  // Shared interfaces only!

// Configure HTTP client for RemoteFactory
builder.Services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
    new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
```

### Complete Client Program.cs

```csharp
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");

// Register Neatoo services in Remote mode
builder.Services.AddNeatooServices(
    NeatooFactory.Remote,
    typeof(IPerson).Assembly);

// HTTP client for RemoteFactory
builder.Services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
    new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// MudBlazor (optional)
builder.Services.AddMudServices();

await builder.Build().RunAsync();
```

## The [Remote] Attribute

`[Remote]` marks methods that execute on the server:

```csharp
[Factory]
internal partial class Person : EntityBase<Person>, IPerson
{
    // Runs on CLIENT - no [Remote]
    [Create]
    public void Create()
    {
        Id = Guid.NewGuid();
    }

    // Runs on SERVER - has [Remote]
    [Remote]
    [Fetch]
    public async Task Fetch(Guid id, [Service] IDbContext db)
    {
        var entity = await db.Persons.FindAsync(id);
        if (entity != null) MapFrom(entity);
    }

    // Runs on SERVER - has [Remote]
    [Remote]
    [Insert]
    public async Task Insert([Service] IDbContext db)
    {
        var entity = new PersonEntity();
        MapTo(entity);
        db.Persons.Add(entity);
        await db.SaveChangesAsync();
    }

    // Runs on SERVER - has [Remote]
    [Remote]
    [Update]
    public async Task Update([Service] IDbContext db)
    {
        var entity = await db.Persons.FindAsync(Id);
        MapModifiedTo(entity);
        await db.SaveChangesAsync();
    }
}
```

### When [Remote] Is Called

| Method | Client Calls | Server Calls |
|--------|--------------|--------------|
| `[Create]` without `[Remote]` | Client | Server |
| `[Create]` with `[Remote]` | Server via HTTP | Server |
| `[Fetch]` with `[Remote]` | Server via HTTP | Server |
| `[Insert]` with `[Remote]` | Server via HTTP | Server |

## Blazor Component Usage

```razor
@page "/person/{Id:guid?}"
@inject IPersonFactory PersonFactory

<EditForm Model="@person">
    <MudNeatooTextField T="string"
        EntityProperty="@person[nameof(IPerson.FirstName)]" />

    <MudNeatooTextField T="string"
        EntityProperty="@person[nameof(IPerson.LastName)]" />

    <MudNeatooTextField T="string"
        EntityProperty="@person[nameof(IPerson.Email)]" />
</EditForm>

<MudButton Disabled="@(!person.IsSavable)"
           OnClick="@Save">
    Save
</MudButton>

@if (person.IsBusy)
{
    <MudProgressCircular Indeterminate="true" />
}

<NeatooValidationSummary Entity="@person" />

@code {
    [Parameter]
    public Guid? Id { get; set; }

    private IPerson person = default!;

    protected override async Task OnInitializedAsync()
    {
        if (Id.HasValue)
        {
            person = await PersonFactory.Fetch(Id.Value);
        }
        else
        {
            person = PersonFactory.Create();
        }
    }

    private async Task Save()
    {
        // Wait for async validation
        await person.WaitForTasks();

        if (!person.IsSavable) return;

        // CRITICAL: Reassign after save!
        person = await PersonFactory.Save(person);

        // Navigate to edit page with new ID
        if (Id == null && person.Id.HasValue)
        {
            NavigationManager.NavigateTo($"/person/{person.Id}");
        }
    }
}
```

## Authentication/Authorization

### Server-Side Setup

```csharp
// Add authentication
builder.Services.AddAuthentication()
    .AddJwtBearer();

builder.Services.AddAuthorization();

// Protect the Neatoo endpoint
app.MapPost("/api/neatoo", ...)
    .RequireAuthorization();  // Requires authentication
```

### Entity-Level Authorization

```csharp
// Authorization interface
public interface IPersonAuth
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    bool CanCreate();

    [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
    bool CanFetch();

    [AuthorizeFactory(AuthorizeFactoryOperation.Insert | AuthorizeFactoryOperation.Update)]
    bool CanWrite();
}

// Apply to entity
[Factory]
[AuthorizeFactory<IPersonAuth>]
internal partial class Person : EntityBase<Person>, IPerson { }
```

## Error Handling

### Client-Side

```csharp
try
{
    person = await PersonFactory.Save(person);
}
catch (HttpRequestException ex)
{
    // Network or server error
    Snackbar.Add("Unable to save. Please try again.", Severity.Error);
}
catch (Exception ex)
{
    // Business logic error
    Snackbar.Add(ex.Message, Severity.Warning);
}
```

### Server-Side

Exceptions in `[Remote]` methods are serialized back to client:

```csharp
[Remote]
[Insert]
public async Task Insert([Service] IDbContext db)
{
    if (await db.Persons.AnyAsync(p => p.Email == Email))
    {
        throw new InvalidOperationException("Email already exists");
    }
    // ...
}
```

## Common Issues

### CORS Errors

```csharp
// Server Program.cs
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("https://localhost:5002")  // Client URL
              .AllowAnyMethod()
              .AllowAnyHeader());
});

app.UseCors();  // Before MapPost
```

### Missing Assembly Registration

```csharp
// WRONG - missing domain assembly on server
builder.Services.AddNeatooServices(
    NeatooFactory.Server,
    typeof(IPerson).Assembly);  // Only interfaces!

// CORRECT - include domain assembly
builder.Services.AddNeatooServices(
    NeatooFactory.Server,
    typeof(IPerson).Assembly,    // Interfaces
    typeof(Person).Assembly);    // Implementations
```

### Client Referencing Domain

```csharp
// WRONG - client references domain (implementations in client bundle!)
// Client.csproj:
// <ProjectReference Include="..\Domain\Domain.csproj" />

// CORRECT - client only references shared interfaces
// Client.csproj:
// <ProjectReference Include="..\Shared\Shared.csproj" />
```

## Best Practices

1. **Separate interfaces from implementations** - Client references only interfaces
2. **Use [Remote] for database operations** - Fetch, Insert, Update, Delete
3. **Don't use [Remote] on [Create]** - Keep initialization on client
4. **Protect the endpoint** - Use authentication middleware
5. **Handle errors gracefully** - Catch and display user-friendly messages
6. **Configure CORS properly** - Allow your client origin

## Next Steps

- [quick-reference.md](quick-reference.md) - Syntax reference
- [troubleshooting.md](troubleshooting.md) - Debug connection issues
