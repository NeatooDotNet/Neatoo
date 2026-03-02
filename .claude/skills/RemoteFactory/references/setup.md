# Server and Client Setup

## NuGet Packages

```xml
<!-- Server project -->
<PackageReference Include="Neatoo.RemoteFactory" Version="x.y.z" />
<PackageReference Include="Neatoo.RemoteFactory.AspNetCore" Version="x.y.z" />

<!-- Client project (Blazor WASM) -->
<PackageReference Include="Neatoo.RemoteFactory" Version="x.y.z" />
```

---

## Server Setup (ASP.NET Core)

<!-- snippet: aspnetcore-basic-setup -->
<a id='snippet-aspnetcore-basic-setup'></a>
```cs
public static class BasicSetup
{
    public static void Configure(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Register RemoteFactory services with the domain assembly
        builder.Services.AddNeatooAspNetCore(typeof(Employee).Assembly);

        var app = builder.Build();

        // Map the /api/neatoo endpoint for remote delegate requests
        app.UseNeatoo();

        app.Run();
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCore/BasicSetupSamples.cs#L8-L26' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-basic-setup' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Multiple Assemblies

If `[Factory]` types are in multiple assemblies:

<!-- snippet: aspnetcore-multi-assembly -->
<a id='snippet-aspnetcore-multi-assembly'></a>
```cs
public static class MultiAssemblySample
{
    public static void ConfigureServices(IServiceCollection services)
    {
        // Register factories from multiple domain assemblies
        services.AddNeatooAspNetCore(
            typeof(Employee).Assembly
            // , typeof(OtherDomain.Entity).Assembly
        );
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCore/ServiceRegistrationSamples.cs#L22-L34' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-multi-assembly' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### CORS Configuration (Non-Hosted Deployments Only)

CORS is only needed when the client and server are on different origins (e.g., standalone Blazor WASM hosted separately from the API). In hosted WASM mode, CORS is unnecessary because both share the same origin.

<!-- snippet: aspnetcore-cors -->
<a id='snippet-aspnetcore-cors'></a>
```cs
public static class CorsConfigurationSample
{
    public static void Configure(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Only needed for non-hosted deployments where client and server
        // are on different origins (e.g., separate Blazor WASM standalone app)
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins("https://client.example.com")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        builder.Services.AddNeatooAspNetCore(typeof(Employee).Assembly);

        var app = builder.Build();

        app.UseCors();    // CORS must be before UseNeatoo
        app.UseNeatoo();

        app.Run();
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCore/CorsConfigurationSamples.cs#L9-L38' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-cors' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

---

## Client Setup (Blazor WASM)

<!-- snippet: getting-started-client-program -->
<a id='snippet-getting-started-client-program'></a>
```cs
// Register RemoteFactory for client mode with domain assembly
builder.Services.AddNeatooRemoteFactory(
    NeatooFactory.Remote,
    new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
    typeof(Employee).Assembly);

// Register HttpClient for remote calls to server
// In hosted WASM mode, HostEnvironment.BaseAddress targets the server that hosts the client
builder.Services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Client.Blazor/Program.cs#L11-L22' title='Snippet source file'>snippet source</a> | <a href='#snippet-getting-started-client-program' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### [assembly: FactoryMode] for Client Assemblies

Add to your client project to generate only remote stubs (no local execution code):

<!-- snippet: attributes-factorymode -->
<a id='snippet-attributes-factorymode'></a>
```cs
// Full mode (default): generates local and remote code
// [assembly: FactoryMode(FactoryModeOption.Full)]

// RemoteOnly mode: generates HTTP stubs only (use in Blazor WASM)
// [assembly: FactoryMode(FactoryModeOption.RemoteOnly)]
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/AssemblyAttributeSamples.cs#L5-L11' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-factorymode' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Modes:**
- `FactoryMode.Full` (default) - Generate local and remote code (server assemblies)
- `FactoryMode.RemoteOnly` - Generate HTTP stubs only (client assemblies)

**Why use RemoteOnly?**
- Prevents accidentally calling server-only methods on client
- Reduces generated code size in client bundles
- Enforces the client/server boundary at compile time

```csharp
// Configure HttpClient for server communication
// In hosted Blazor WASM, HostEnvironment.BaseAddress targets the hosting server
builder.Services.AddKeyedScoped(
    RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient
    {
        BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
    });

await builder.Build().RunAsync();
```

### Factory Modes

```csharp
// Remote mode - calls go to server
builder.Services.AddNeatooRemoteFactory(NeatooFactory.Remote, ...);

// Local mode - everything runs locally (for testing, single-tier apps)
builder.Services.AddNeatooRemoteFactory(NeatooFactory.Local, ...);
```

---

## Using Factories

### Inject and Use

<!-- snippet: skill-blazor-usage -->
<a id='snippet-skill-blazor-usage'></a>
```cs
// Blazor component showing factory injection and usage
public partial class EmployeeManagementComponent : ComponentBase
{
    [Inject]
    private IEmployeeFactory EmployeeFactory { get; set; } = null!;

    private IEmployee? _employee;

    protected override async Task OnInitializedAsync()
    {
        // Create a new employee
        _employee = await EmployeeFactory.Create("John", "Doe");
    }

    private async Task LoadEmployee(Guid id)
    {
        // Fetch existing employee
        _employee = await EmployeeFactory.Fetch(id);
    }

    private async Task SaveEmployee()
    {
        if (_employee == null) return;

        // Save routes to Insert/Update/Delete based on IsNew/IsDeleted
        _employee = await EmployeeFactory.Save(_employee);
    }
}

// Placeholder interfaces for demonstration
// (Actual interfaces are generated by RemoteFactory)
public interface IEmployeeFactory
{
    Task<IEmployee> Create(string firstName, string lastName);
    Task<IEmployee?> Fetch(Guid id);
    Task<IEmployee> Save(IEmployee employee);
}

public interface IEmployee
{
    Guid Id { get; }
    string FirstName { get; set; }
    string LastName { get; set; }
    bool IsNew { get; }
    bool IsDeleted { get; set; }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Client.Blazor/Samples/Skill/BlazorUsageSamples.cs#L5-L52' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-blazor-usage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Static Factory Commands

```csharp
// No injection needed - static delegates
await SkillEmployeeCommands.SendNotification("admin@example.com", "Employee updated!");

// Fire-and-forget events
_ = SkillEmployeeEvents.OnEmployeeCreatedEvent(employeeId, "John Doe");
```

---

## Framework Support

RemoteFactory supports:
- **.NET 8.0** (LTS)
- **.NET 9.0** (STS)
- **.NET 10.0** (LTS)

All three frameworks are included in the NuGet packages.

---

## Generated Code Location

Generated code appears in:
```
obj/Debug/{tfm}/generated/Neatoo.Generator/Neatoo.Factory/
```

Files generated:
- `{Namespace}.{TypeName}Factory.g.cs` - Factory interface and implementation
- `{Namespace}.{TypeName}.Ordinal.g.cs` - Serialization implementation
