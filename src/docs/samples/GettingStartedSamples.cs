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
}
#endregion

#region getting-started-entity
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
#endregion

/// <summary>
/// Tests for getting-started.md snippets.
/// </summary>
public class GettingStartedSamplesTests
{
    #region getting-started-validate-check
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
    #endregion

    [Fact]
    public void ValidateBase_MaxLengthValidation()
    {
        var customer = new CustomerValidator(new ValidateBaseServices<CustomerValidator>());

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
        var employee = new EmployeeEntity(new EntityBaseServices<EmployeeEntity>());

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
        var employee = new EmployeeEntity(new EntityBaseServices<EmployeeEntity>());

        // Required validation
        employee.Name = "";
        Assert.False(employee.IsValid);

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
    public Task<EmployeeEntity> FetchAsync(int id)
    {
        var employee = new EmployeeEntity(new EntityBaseServices<EmployeeEntity>())
        {
            Id = id,
            Name = $"Employee {id}",
            Email = $"employee{id}@example.com",
            Salary = 50000m + (id * 1000)
        };
        return Task.FromResult(employee);
    }
}
