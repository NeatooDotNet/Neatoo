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

Create a class that inherits from `ValidateBase<T>`. Properties use the `Getter<T>()` and `Setter(value)` pattern, which triggers source generation for backing fields and property change notifications.

<!-- snippet: getting-started-validate -->
```cs
[Factory]
public partial class CustomerValidator : ValidateBase<CustomerValidator>
{
    public CustomerValidator(IValidateBaseServices<CustomerValidator> services) : base(services) { }

    [Required]
    public partial string Name { get; set; }

    [MaxLength(100)]
    public partial string Email { get; set; }
}
```
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

Validation rules execute automatically when properties change. Access validation state through meta-properties:

<!-- snippet: getting-started-validate-check -->
```cs
[Fact]
public void ValidateBase_CheckValidationState()
{
    var customer = new CustomerValidator(new ValidateBaseServices<CustomerValidator>());

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
<!-- endSnippet -->

Key validation meta-properties:
- `IsValid` - True if this object and all children are valid
- `IsSelfValid` - True if this object (excluding children) is valid
- `BrokenRules` - Collection of all validation errors
- `[propertyName]` indexer - Access property-specific validation messages

## Your First Entity Aggregate

Entities extend validation objects with persistence, identity, and modification tracking. Create an entity by inheriting from `EntityBase<T>`:

<!-- snippet: getting-started-entity -->
```cs
// Mock repository interface for the sample
public interface IEmployeeRepository
{
    Task<EmployeeEntity> FetchAsync(int id);
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

    [Fetch]
    public async Task FetchAsync(int id, [Service] IEmployeeRepository repository)
    {
        var employee = await repository.FetchAsync(id);
        Id = employee.Id;
        Name = employee.Name;
        Email = employee.Email;
        Salary = employee.Salary;
    }
}
```
<!-- endSnippet -->

Entity objects provide:
- `IsNew` - True for newly created entities (no identity)
- `IsModified` - True if any property has changed since load/save
- `IsDirty` - True if modified OR any child is dirty
- `IsDeleted` - True if marked for deletion
- `IsSavable` - True if valid and dirty

The `[Fetch]` attribute marks a factory method that loads the entity from persistence. RemoteFactory generates the factory infrastructure and wiring. The `[Service]` parameter instructs dependency injection to provide the repository.

## Use the Generated Factory

RemoteFactory generates factory methods based on your attributes. Create and fetch entities through the factory:

<!-- snippet: getting-started-entity-use -->
```cs
[Fact]
public void EntityBase_UseEntity()
{
    // Create entity for testing
    // (In production, the factory's Create method sets IsNew automatically)
    var employee = new EmployeeEntity(new EntityBaseServices<EmployeeEntity>());

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
<!-- endSnippet -->

The factory method name comes from the method decorated with `[Fetch]`. In this example, `FetchAsync` becomes `Employee.FetchAsync()` on the generated factory.

## Configure Dependency Injection

Register Neatoo services and your repositories with DI:

<!-- snippet: getting-started-di -->
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
<!-- endSnippet -->

`AddNeatoo()` registers core Neatoo services. `AddRemoteFactory<T>()` registers the factory for the specified type.

## Next Steps

You now have a working Neatoo domain object with validation and persistence. Explore deeper:

- **[Validation](guides/validation.md)** - Custom rules, async validation, rule execution control
- **[Entities](guides/entities.md)** - Entity lifecycle, state management, aggregate patterns
- **[Collections](guides/collections.md)** - EntityListBase and ValidateListBase for child collections
- **[Properties](guides/properties.md)** - Property system internals, LoadValue, meta-properties
- **[Business Rules](guides/business-rules.md)** - Cross-property validation, aggregate-level rules
- **[Parent-Child Graphs](guides/parent-child.md)** - Aggregate boundaries, cascade behavior
- **[Blazor Integration](guides/blazor.md)** - MudNeatoo components for Blazor forms
- **[RemoteFactory](guides/remote-factory.md)** - Deep dive into factory pattern and client-server transfer

---

**UPDATED:** 2026-01-24
