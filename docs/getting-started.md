# Getting Started

[Up](index.md)

Get up and running with Neatoo in minutes. This guide covers installation, your first validation object, and your first entity aggregate.

## Prerequisites

Neatoo targets modern .NET:
- .NET 8.0, 9.0, or 10.0
- C# 12 or later (for source generator features)
- Visual Studio 2022, Rider, or VS Code with C# Dev Kit

## Installation

Install the Neatoo NuGet package:

```bash
dotnet add package Neatoo
```

The package includes:
- Core runtime library
- Source generators for property backing fields
- Analyzers and code fixes for best practices
- RemoteFactory integration

## Your First Validation Object

Create a class that inherits from `ValidateBase<T>`. Use `partial` properties - Neatoo's source generator creates backing fields and wires up property change notifications and validation automatically.

<!-- snippet: getting-started-validate -->
<a id='snippet-getting-started-validate'></a>
```cs
[Factory]
public partial class CustomerValidator : ValidateBase<CustomerValidator>
{
    public CustomerValidator(IValidateBaseServices<CustomerValidator> services) : base(services) { }

    [Required]
    public partial string Name { get; set; }

    [MaxLength(100)]
    public partial string Email { get; set; }

    [Create]
    public void Create() { }
}
```
<sup><a href='/src/samples/GettingStartedSamples.cs#L10-L25' title='Snippet source file'>snippet source</a> | <a href='#snippet-getting-started-validate' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

When you build the project, Neatoo's source generator creates:
- Backing fields for each property
- PropertyChanged event wiring
- Validation infrastructure

The `[Required]` and `[MaxLength]` attributes are standard `System.ComponentModel.DataAnnotations` attributes. Neatoo executes these rules automatically when properties change.

## Verify Source Generation

After building, check that source generation succeeded:

1. In Visual Studio: Expand the project node → Dependencies → Analyzers → Neatoo.BaseGenerator
2. In Rider: Look for generated files in the project tree under "Generated Code"
3. Or check `obj/Debug/net8.0/generated/` for the generated files

You should see generated files with names like `CustomerValidator.g.cs` containing the backing field implementations.

## Run Validation

Validation rules execute automatically when properties change. Access validation state through meta-properties.

> **Note:** The code samples below are integration tests that resolve factories from DI. The `GetRequiredService<T>()` helper retrieves services from a configured `IServiceProvider`. In your application, you would inject the factory interface directly into your controllers, services, or Blazor components.

<!-- snippet: getting-started-validate-check -->
<a id='snippet-getting-started-validate-check'></a>
```cs
[Fact]
public void ValidateBase_CheckValidationState()
{
    var factory = GetRequiredService<ICustomerValidatorFactory>();
    var customer = factory.Create();

    // Empty required field is invalid
    customer.Name = "";
    Assert.False(customer.IsValid);

    // Access validation messages for specific property
    var nameProperty = customer["Name"];
    Assert.False(nameProperty.IsValid);

    // Set valid data
    customer.Name = "Acme Corp";
    Assert.True(customer.IsValid);
    Assert.True(customer.IsSelfValid);
}
```
<sup><a href='/src/samples/GettingStartedSamples.cs#L101-L121' title='Snippet source file'>snippet source</a> | <a href='#snippet-getting-started-validate-check' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Key validation meta-properties:
- `IsValid` - True if this object and all children are valid
- `IsSelfValid` - True if this object (excluding children) is valid
- `BrokenRules` - Collection of all validation errors
- `[propertyName]` indexer - Access property-specific validation messages

## Your First Entity

Entities are domain objects with identity and lifecycle management. `EntityBase<T>` extends `ValidateBase<T>` to add persistence state tracking, modification tracking, and save operations:

<!-- snippet: getting-started-entity -->
<a id='snippet-getting-started-entity'></a>
```cs
// Mock repository interface for the sample
public interface IEmployeeRepository
{
    Task<EmployeeData> FetchDataAsync(int id);
    Task InsertAsync(EmployeeEntity employee);
    Task UpdateAsync(EmployeeEntity employee);
}

/// <summary>
/// Data transfer object for employee data.
/// </summary>
public class EmployeeData
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public decimal Salary { get; set; }
}

[Factory]
public partial class EmployeeEntity : EntityBase<EmployeeEntity>
{
    public EmployeeEntity(IEntityBaseServices<EmployeeEntity> services) : base(services) { }

    public partial int Id { get; set; }

    [Required]
    public partial string Name { get; set; }

    [EmailAddress]
    public partial string Email { get; set; }

    public partial decimal Salary { get; set; }

    [Create]
    public void Create()
    {
        // Initialize new employee with defaults
        Id = 0;
        Name = "";
        Email = "";
        Salary = 0;
    }

    [Fetch]
    public async Task FetchAsync(int id, [Service] IEmployeeRepository repository)
    {
        var data = await repository.FetchDataAsync(id);
        Id = data.Id;
        Name = data.Name;
        Email = data.Email;
        Salary = data.Salary;
    }

    [Insert]
    public async Task InsertAsync([Service] IEmployeeRepository repository)
    {
        await repository.InsertAsync(this);
    }

    [Update]
    public async Task UpdateAsync([Service] IEmployeeRepository repository)
    {
        await repository.UpdateAsync(this);
    }
}
```
<sup><a href='/src/samples/GettingStartedSamples.cs#L27-L94' title='Snippet source file'>snippet source</a> | <a href='#snippet-getting-started-entity' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Entity state tracking properties:
- `IsNew` - True for newly created entities that have not been persisted
- `IsModified` - True if any property has changed since load/save (cascades from children)
- `IsDeleted` - True if marked for deletion
- `IsSavable` - Computed property: true if `IsModified && IsValid && !IsBusy && !IsChild`

The `[Fetch]` attribute marks a data access method. RemoteFactory generates a factory interface (`IEmployeeEntityFactory`) with a corresponding `FetchAsync(int id)` method that resolves the repository from DI and calls your method. The `[Service]` attribute indicates the parameter should be injected from the service provider.

## Use the Generated Factory

RemoteFactory generates factory methods based on your `[Create]`, `[Fetch]`, `[Insert]`, `[Update]`, and `[Delete]` attributes. Create and fetch entities through the generated factory interface:

<!-- snippet: getting-started-entity-use -->
<a id='snippet-getting-started-entity-use'></a>
```cs
[Fact]
public void EntityBase_UseEntity()
{
    // Use factory to create new employee
    var factory = GetRequiredService<IEmployeeEntityFactory>();
    var employee = factory.Create();

    // Set properties
    employee.Name = "Alice Johnson";
    employee.Email = "alice@example.com";
    employee.Salary = 75000m;

    // Check entity state
    Assert.True(employee.IsSelfModified); // Properties changed
    Assert.True(employee.IsModified);     // Tracks modification
    Assert.True(employee.IsValid);        // Passes validation

    // Entity tracks which properties were modified
    Assert.Contains("Name", employee.ModifiedProperties);
    Assert.Contains("Email", employee.ModifiedProperties);
    Assert.Contains("Salary", employee.ModifiedProperties);
}
```
<sup><a href='/src/samples/GettingStartedSamples.cs#L181-L204' title='Snippet source file'>snippet source</a> | <a href='#snippet-getting-started-entity-use' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Each method you mark with an attribute becomes a method on the generated factory interface. Your `FetchAsync(int id, ...)` method becomes `IEmployeeEntityFactory.FetchAsync(int id)`.

## Configure Dependency Injection

Register Neatoo services and your repositories with DI:

<!-- snippet: getting-started-di -->
<a id='snippet-getting-started-di'></a>
```cs
[Fact]
public void ConfigureDependencyInjection()
{
    var services = new ServiceCollection();

    // Register Neatoo services
    // NeatooFactory.Logical: All operations run locally (no client-server split)
    services.AddNeatooServices(
        NeatooFactory.Logical,
        typeof(GettingStartedSamplesTests).Assembly);

    // Register your repositories
    services.AddScoped<IEmployeeRepository, MockEmployeeRepository>();

    var provider = services.BuildServiceProvider();

    // Resolve services to verify registration
    var factory = provider.GetRequiredService<IFactory>();
    Assert.NotNull(factory);
}
```
<sup><a href='/src/samples/GettingStartedSamples.cs#L249-L270' title='Snippet source file'>snippet source</a> | <a href='#snippet-getting-started-di' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`AddNeatooServices()` registers all Neatoo services including factories for types in the specified assembly. The `NeatooFactory.Logical` option means all operations run locally (no client-server split).

To use the generated factory in your code, inject the typed factory interface (e.g., `IEmployeeEntityFactory`) into your services, controllers, or Blazor components:

```csharp
public class EmployeeService
{
    private readonly IEmployeeEntityFactory _factory;

    public EmployeeService(IEmployeeEntityFactory factory)
    {
        _factory = factory;
    }

    public async Task<EmployeeEntity> GetEmployeeAsync(int id)
    {
        return await _factory.FetchAsync(id);
    }
}
```

## Next Steps

You now have working validation objects (`ValidateBase<T>`) and entities (`EntityBase<T>`).

**Architectural Concepts:**
- **ValidateBase** provides property management, validation rules, and change notification. Use for validation logic that doesn't need persistence (DTOs, view models, form models).
- **EntityBase** extends ValidateBase with identity, persistence lifecycle, and modification tracking. Use for domain entities that map to database tables.
- **Aggregate roots** are entities that define consistency boundaries. Child entities belong to the aggregate and are persisted with the root.

**Explore deeper:**

- **[Validation](guides/validation.md)** - Custom rules, async validation, rule execution control
- **[Entities](guides/entities.md)** - Entity lifecycle, state management, persistence operations
- **[Collections](guides/collections.md)** - EntityListBase and ValidateListBase for child collections
- **[Properties](guides/properties.md)** - Property system internals, LoadValue, meta-properties
- **[Business Rules](guides/business-rules.md)** - Cross-property validation, aggregate-level rules
- **[Parent-Child Graphs](guides/parent-child.md)** - Aggregate boundaries, cascade behavior
- **[Blazor Integration](guides/blazor.md)** - MudNeatoo components for Blazor forms
- **[RemoteFactory](guides/remote-factory.md)** - Deep dive into factory pattern and client-server transfer

---

**UPDATED:** 2026-01-25
