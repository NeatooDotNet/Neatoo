# Installation

This guide covers installing and configuring Neatoo in your .NET project.

## NuGet Packages

### Core Package

```bash
dotnet add package Neatoo
```

Contains:
- Base classes (Base, ValidateBase, EntityBase)
- List classes (ListBase, ValidateListBase, EntityListBase)
- Rules engine (RuleBase, RuleManager)
- Property system

### Remote Factory Package

```bash
dotnet add package Neatoo.RemoteFactory
```

Contains:
- Factory attributes ([Factory], [Remote], [Fetch], [Insert], [Update], [Delete])
- Source generators for factory creation
- JSON serialization for client-server transfer
- RemoteFactory endpoint

### Blazor MudBlazor Components (Optional)

```bash
dotnet add package Neatoo.Blazor.MudNeatoo
```

Contains:
- MudNeatooTextField, MudNeatooNumericField, etc.
- NeatooValidationSummary
- Property binding helpers

## Project Structure

Typical solution structure:

```
MySolution/
    MyApp.DomainModels/         # Domain model classes
        - MyAggregate.cs
        - MyChildEntity.cs
        - MyRules.cs

    MyApp.EntityFramework/       # EF Core entities (optional)
        - MyDbContext.cs
        - Entities/

    MyApp.Server/                # ASP.NET Core backend
        - Program.cs

    MyApp.Client/                # Blazor WebAssembly client
        - Program.cs
```

## Server Configuration

### Program.cs

<!-- snippet: server-program-cs -->
```cs
// Server Program.cs - Complete configuration example
public static void ConfigureServerProgram(WebApplicationBuilder builder, WebApplication app)
{
    // Add Neatoo services for server-side execution
    builder.Services.AddNeatooServices(NeatooFactory.Server, typeof(IMyAggregate).Assembly);

    // Register your DbContext (example - actual implementation depends on your setup)
    // builder.Services.AddDbContext<MyDbContext>(options => options.UseSqlServer(connectionString));

    // Register validation rules
    builder.Services.AddScoped<IMyValidationRule, MyValidationRule>();

    // Map the Neatoo RemoteFactory endpoint
    app.MapPost("/api/neatoo", (HttpContext httpContext, RemoteRequestDto request, CancellationToken token) =>
    {
        var handler = httpContext.RequestServices.GetRequiredService<HandleRemoteDelegateRequest>();
        return handler(request, token);
    });
}
```
<!-- endSnippet -->

### appsettings.json

```json
{
  "ConnectionStrings": {
    "Default": "Server=...;Database=...;..."
  }
}
```

## Client Configuration

### Blazor WebAssembly Program.cs

<!-- snippet: client-program-cs -->
```cs
// Blazor WebAssembly Program.cs - Complete configuration example
public static void ConfigureClientProgram(WebAssemblyHostBuilder builder)
{
    // Add Neatoo services for client-side with remote factory calls
    builder.Services.AddNeatooServices(NeatooFactory.Remote, typeof(IMyAggregate).Assembly);

    // Configure HttpClient for API calls
    builder.Services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
        new HttpClient { BaseAddress = new Uri("https://localhost:5001") });

    // Register validation rules (same rules run on client)
    builder.Services.AddScoped<IMyValidationRule, MyValidationRule>();
}
```
<!-- endSnippet -->

### MudBlazor Configuration

<!-- pseudo:mudblazor-services -->
```csharp
// Program.cs
builder.Services.AddMudServices();
```
<!-- /snippet -->

```razor
<!-- MainLayout.razor or App.razor -->
<MudThemeProvider />
<MudDialogProvider />
<MudSnackbarProvider />
```

## Domain Model Assembly

### Project File

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Neatoo" Version="*" />
    <PackageReference Include="Neatoo.RemoteFactory" Version="*" />
  </ItemGroup>
</Project>
```

### GlobalUsings.cs (Optional)

<!-- pseudo:global-usings -->
```csharp
global using Neatoo;
global using Neatoo.RemoteFactory;
global using Neatoo.Rules;
```
<!-- /snippet -->

## Service Registration

### Domain Model Services

Register factories, rules, and services in both client and server:

<!-- snippet: domain-model-services -->
```cs
// Extension method for shared registration
public static class DomainModelServiceExtensions
{
    public static IServiceCollection AddDomainModelServices(this IServiceCollection services)
    {
        // Validation rules
        services.AddScoped<IUniqueNameRule, UniqueNameRule>();
        services.AddScoped<IEmailValidationRule, EmailValidationRule>();

        // Services used by rules/factories
        services.AddScoped<IEmailValidationService, EmailValidationService>();

        return services;
    }
}
```
<!-- endSnippet -->

### Server-Only Services

<!-- pseudo:server-only-services -->
```csharp
// DbContext and repositories
builder.Services.AddDbContext<MyDbContext>(...);
builder.Services.AddScoped<IMyRepository, MyRepository>();
```
<!-- /snippet -->

## Configuration Modes

### NeatooFactory.Server

- Factory operations execute directly
- Services resolved from local DI
- Used on ASP.NET Core server

### NeatooFactory.Remote

- Factory operations call server via HTTP
- Serializes entity state for transfer
- Used on Blazor WebAssembly client

### Standalone (No RemoteFactory)

For desktop apps or server-only scenarios:

<!-- pseudo:standalone-setup -->
```csharp
builder.Services.AddNeatooServices(NeatooFactory.Server, typeof(IMyAggregate).Assembly);
```
<!-- /snippet -->

## Source Generator Setup

The source generators require:

1. **Partial classes** - Classes must be `partial`
2. **Partial properties** - Properties for tracking must be `partial`
3. **Interface** - Each aggregate needs a public interface
4. **[Factory] attribute** - Marks classes for factory generation

<!-- snippet: source-gen-structure -->
```cs
// Required structure for source generation
public partial interface IMyAggregate : IEntityBase { }

[Factory]
internal partial class MyAggregate : EntityBase<MyAggregate>, IMyAggregate
{
    public MyAggregate(IEntityBaseServices<MyAggregate> services) : base(services) { }

    public partial string? Name { get; set; }  // Partial for state tracking
}
```
<!-- endSnippet -->

## Troubleshooting

### Generated Code Not Appearing

1. Ensure project uses `<TargetFramework>net8.0</TargetFramework>` or later
2. Check that Roslyn analyzers are enabled
3. Clean and rebuild solution
4. Check Output window for generator errors

### Factory Not Found

1. Verify `[Factory]` attribute on class
2. Ensure interface extends `IEntityBase`
3. Check assembly is passed to `AddNeatooServices`

### Rules Not Executing

1. Verify rules are registered in DI
2. Check property is `partial`
3. Ensure rule trigger properties match

### Serialization Errors

1. All transferred properties must be `partial`
2. Complex types need proper JSON serialization
3. Check for circular references in object graph

## See Also

- [Quick Start](quick-start.md) - First aggregate walkthrough
- [Factory Operations](factory-operations.md) - Factory configuration
- [Blazor Binding](blazor-binding.md) - Client setup
