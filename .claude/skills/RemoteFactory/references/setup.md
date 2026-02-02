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
<a id='snippet-aspnetcore-basic-setup-1'></a>
```cs
/// <summary>
/// Complete server configuration in a single Program.cs pattern.
/// </summary>
public static class BasicSetupSample
{
    public static void ConfigureServices(WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var domainAssembly = typeof(Employee).Assembly;

        // Register RemoteFactory with ASP.NET Core integration
        builder.Services.AddNeatooAspNetCore(
            new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
            domainAssembly);

        // Register factory interface -> implementation mappings
        builder.Services.RegisterMatchingName(domainAssembly);

        // Register infrastructure services
        builder.Services.AddInfrastructureServices();
    }

    public static void ConfigureApp(WebApplication app)
    {
        // Configure the /api/neatoo endpoint
        app.UseNeatoo();
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCoreSamples.cs#L18-L47' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-basic-setup-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Multiple Assemblies

If `[Factory]` types are in multiple assemblies:

<!-- snippet: aspnetcore-multi-assembly -->
<a id='snippet-aspnetcore-multi-assembly'></a>
```cs
/// <summary>
/// Registering multiple domain assemblies.
/// </summary>
public static class MultiAssemblySample
{
    public static void ConfigureServices(IServiceCollection services)
    {
        // Register factories from multiple assemblies
        // Each assembly can contain domain models with [Factory] attributes
        services.AddNeatooAspNetCore(
            new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
            typeof(Employee).Assembly           // EmployeeManagement.Domain
            // , typeof(OtherModel).Assembly    // Other domain assemblies
        );
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCoreSamples.cs#L279-L296' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-multi-assembly' title='Start of snippet'>anchor</a></sup>
<a id='snippet-aspnetcore-multi-assembly-1'></a>
```cs
public static class MultiAssemblySample
{
    public static void ConfigureServices(IServiceCollection services)
    {
        // Primary domain assembly
        var employeeDomainAssembly = typeof(Employee).Assembly;

        // Additional assemblies containing [Factory] types:
        // var hrDomainAssembly = typeof(HR.Domain.HrEntity).Assembly;
        // var payrollDomainAssembly = typeof(Payroll.Domain.PayrollEntity).Assembly;

        // Register all assemblies with RemoteFactory
        services.AddNeatooAspNetCore(
            employeeDomainAssembly
            // hrDomainAssembly,
            // payrollDomainAssembly
        );

        // Auto-register services from all assemblies
        services.RegisterMatchingName(
            employeeDomainAssembly
            // hrDomainAssembly,
            // payrollDomainAssembly
        );
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCore/ServiceRegistrationSamples.cs#L37-L64' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-multi-assembly-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### CORS Configuration (for Blazor WASM)

<!-- snippet: aspnetcore-cors -->
<a id='snippet-aspnetcore-cors'></a>
```cs
public static class CorsConfigurationSample
{
    public static void Configure(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure default CORS policy
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins(
                        "http://localhost:5001",  // Development
                        "https://myapp.example.com" // Production
                    )
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials(); // Required for auth cookies
            });

            // Named policy with specific headers for Neatoo API
            options.AddPolicy("NeatooApi", policy =>
            {
                policy.WithOrigins("http://localhost:5001")
                    .WithHeaders("Content-Type", "X-Correlation-Id")
                    .WithMethods("POST");
            });
        });

        builder.Services.AddNeatooAspNetCore(typeof(Employee).Assembly);

        var app = builder.Build();

        // CORS must come before UseNeatoo
        app.UseCors();
        app.UseNeatoo();

        app.Run();
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCore/CorsConfigurationSamples.cs#L7-L48' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-cors' title='Start of snippet'>anchor</a></sup>
<a id='snippet-aspnetcore-cors-1'></a>
```cs
/// <summary>
/// CORS configuration for Blazor WASM clients.
/// </summary>
public static class CorsSample
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()    // Or WithOrigins("https://client.example.com")
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });
    }

    public static void ConfigureApp(WebApplication app)
    {
        // CORS must be before UseNeatoo for cross-origin requests
        app.UseCors();
        app.UseNeatoo();
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCoreSamples.cs#L354-L380' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-cors-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

---

## Client Setup (Blazor WASM)

<!-- snippet: getting-started-client-program -->
<a id='snippet-getting-started-client-program'></a>
```cs
// Configure RemoteFactory for Remote (client) mode
var domainAssembly = typeof(Employee).Assembly;

builder.Services.AddNeatooRemoteFactory(
    NeatooFactory.Remote,
    new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
    domainAssembly);

// Register the keyed HttpClient for RemoteFactory remote calls
builder.Services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
{
    return new HttpClient { BaseAddress = new Uri(serverBaseAddress) };
});
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Client.Blazor/Program.cs#L14-L28' title='Snippet source file'>snippet source</a> | <a href='#snippet-getting-started-client-program' title='Start of snippet'>anchor</a></sup>
<a id='snippet-getting-started-client-program-1'></a>
```cs
public static class ClientProgram
{
    public static async Task ConfigureClient(string[] args, string serverBaseAddress)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);

        // Configure RemoteFactory for Remote (client) mode with domain assembly
        builder.Services.AddNeatooRemoteFactory(
            NeatooFactory.Remote,
            new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
            typeof(EmployeeModel).Assembly);

        // Register keyed HttpClient for RemoteFactory remote calls
        builder.Services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
        {
            return new HttpClient { BaseAddress = new Uri(serverBaseAddress) };
        });

        await builder.Build().RunAsync();
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Client.Blazor/Samples/ClientProgramSample.cs#L7-L29' title='Snippet source file'>snippet source</a> | <a href='#snippet-getting-started-client-program-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### [assembly: FactoryMode] for Client Assemblies

Add to your client project to generate only remote stubs (no local execution code):

<!-- snippet: attributes-factorymode -->
<a id='snippet-attributes-factorymode'></a>
```cs
// Assembly-level factory mode configuration examples:
//
// Server assembly (default mode):
// [assembly: FactoryMode(FactoryMode.Full)]
// - Generates local and remote execution paths
// - Use in server/API projects
//
// Client assembly (remote-only mode):
// [assembly: FactoryMode(FactoryMode.RemoteOnly)]
// - Generates HTTP stubs only, no local execution
// - Use in Blazor WebAssembly and other client projects
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/AssemblyAttributeSamples.cs#L6-L18' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-factorymode' title='Start of snippet'>anchor</a></sup>
<a id='snippet-attributes-factorymode-1'></a>
```cs
// Full mode (default): Generate both local methods and remote stubs
// Use in shared domain assemblies that can run on both client and server
[assembly: FactoryMode(FactoryModeOption.Full)]

// RemoteOnly mode: Generate HTTP stubs only
// Use in client-only assemblies (e.g., Blazor WASM)
// [assembly: FactoryMode(FactoryModeOption.RemoteOnly)]
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Modes/FactoryModeAttributes.cs#L5-L15' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-factorymode-1' title='Start of snippet'>anchor</a></sup>
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
builder.Services.AddKeyedScoped(
    RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient
    {
        BaseAddress = new Uri("https://localhost:5000/")  // Server URL
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
