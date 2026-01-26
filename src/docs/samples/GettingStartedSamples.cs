using Microsoft.Extensions.DependencyInjection;
using Neatoo;
using Neatoo.Internal;
using Neatoo.RemoteFactory;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace Samples;

#region getting-started-validate
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
#endregion

#region getting-started-entity
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
#endregion

/// <summary>
/// Tests for getting-started.md snippets demonstrating DI-based factory usage.
/// </summary>
public class GettingStartedSamplesTests : SamplesTestBase
{
    #region getting-started-validate-check
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
    #endregion

    [Fact]
    public void ValidateBase_MaxLengthValidation()
    {
        var factory = GetRequiredService<ICustomerValidatorFactory>();
        var customer = factory.Create();

        // Set required field first
        customer.Name = "Test Customer";

        // MaxLength validation
        customer.Email = new string('a', 101); // Exceeds 100 character limit
        Assert.False(customer.IsValid);

        customer.Email = "test@example.com";
        Assert.True(customer.IsValid);
    }

    [Fact]
    public void EntityBase_MetaProperties()
    {
        var factory = GetRequiredService<IEmployeeEntityFactory>();
        var employee = factory.Create();

        // New entities start as not modified (no property changes yet)
        Assert.False(employee.IsSelfModified);

        // Set properties - triggers modification tracking
        employee.Name = "Alice Johnson";
        Assert.True(employee.IsSelfModified);
        Assert.Contains("Name", employee.ModifiedProperties);
    }

    [Fact]
    public void EntityBase_ValidationWithAttributes()
    {
        var factory = GetRequiredService<IEmployeeEntityFactory>();
        var employee = factory.Create();

        // Set valid name first
        employee.Name = "Bob Smith";
        Assert.True(employee.IsValid);

        // Required validation - clear name to empty
        employee.Name = "";
        Assert.False(employee.IsValid);

        // Restore valid name
        employee.Name = "Bob Smith";
        Assert.True(employee.IsValid);

        // EmailAddress validation
        employee.Email = "invalid-email";
        Assert.False(employee.IsValid);

        employee.Email = "bob@example.com";
        Assert.True(employee.IsValid);
    }

    #region getting-started-entity-use
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
    #endregion

    #region getting-started-factory-usage
    [Fact]
    public async Task Factory_FetchEmployee()
    {
        // Resolve factory from DI
        var factory = GetRequiredService<IEmployeeEntityFactory>();

        // Use factory to fetch existing employee
        var employee = await factory.FetchAsync(1);

        // Employee data loaded from repository
        Assert.Equal(1, employee.Id);
        Assert.Equal("Employee 1", employee.Name);
        Assert.False(employee.IsNew);
    }
    #endregion

    #region getting-started-save-entity
    [Fact]
    public async Task Factory_SaveEmployee()
    {
        var factory = GetRequiredService<IEmployeeEntityFactory>();

        // Create new employee
        var employee = factory.Create();
        employee.Name = "New Employee";
        employee.Email = "new@example.com";
        employee.Salary = 60000m;

        // Verify IsSavable before save
        Assert.True(employee.IsNew);
        Assert.True(employee.IsValid);
        Assert.True(employee.IsSavable);

        // Save the employee (calls InsertAsync for new entities)
        var saved = await factory.SaveAsync(employee);

        // After save, entity is no longer new
        Assert.False(saved.IsNew);
        Assert.False(saved.IsSelfModified);
    }
    #endregion

    #region getting-started-di
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
    #endregion
}

/// <summary>
/// Mock repository for testing purposes.
/// </summary>
public class MockEmployeeRepository : IEmployeeRepository
{
    public Task<EmployeeData> FetchDataAsync(int id)
    {
        var data = new EmployeeData
        {
            Id = id,
            Name = $"Employee {id}",
            Email = $"employee{id}@example.com",
            Salary = 50000m + (id * 1000)
        };
        return Task.FromResult(data);
    }

    public Task InsertAsync(EmployeeEntity employee)
    {
        // Simulate insert - in real implementation would persist to database
        return Task.CompletedTask;
    }

    public Task UpdateAsync(EmployeeEntity employee)
    {
        // Simulate update - in real implementation would persist to database
        return Task.CompletedTask;
    }
}
