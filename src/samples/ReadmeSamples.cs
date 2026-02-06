using Neatoo;
using Neatoo.Internal;
using Neatoo.RemoteFactory;
using Neatoo.Rules;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace Samples;

#region readme-teaser
// Define an Employee aggregate root with validation and business rules
[Factory]
public partial class Employee : EntityBase<Employee>
{
    public Employee(IEntityBaseServices<Employee> services) : base(services)
    {
        // Business rule: Full name is computed from first and last name
        RuleManager.AddAction(
            e => { e.FullName = $"{e.FirstName} {e.LastName}"; },
            e => e.FirstName, e => e.LastName);

        // Validation rule: Salary must be positive
        RuleManager.AddValidation(
            e => e.Salary > 0 ? "" : "Salary must be positive",
            e => e.Salary);
    }

    [Required]
    public partial string FirstName { get; set; }

    [Required]
    public partial string LastName { get; set; }

    public partial string FullName { get; set; }

    public partial decimal Salary { get; set; }

    // Child collection with automatic parent tracking
    public partial IAddressList Addresses { get; set; }

    [Create]
    public void Create() { }
}

public interface IAddress : IEntityBase { }

[Factory]
public partial class Address : EntityBase<Address>, IAddress
{
    public Address(IEntityBaseServices<Address> services) : base(services) { }

    [Required]
    public partial string Street { get; set; }

    public partial string City { get; set; }

    [Create]
    public void Create() { }
}

public interface IAddressList : IEntityListBase<IAddress> { }

public class AddressList : EntityListBase<IAddress>, IAddressList { }
#endregion

#region readme-quick-start
// 1. ValidateBase: For objects that need validation without persistence
[Factory]
public partial class CustomerSearch : ValidateBase<CustomerSearch>
{
    public CustomerSearch(IValidateBaseServices<CustomerSearch> services) : base(services)
    {
        // Inline validation rule
        RuleManager.AddValidation(
            c => !string.IsNullOrEmpty(c.SearchTerm) ? "" : "Search term is required",
            c => c.SearchTerm);
    }

    public partial string SearchTerm { get; set; }

    [Range(1, 100)]
    public partial int MaxResults { get; set; }

    [Create]
    public void Create() { }
}

// 2. EntityBase: For domain entities with full lifecycle support
[Factory]
public partial class Customer : EntityBase<Customer>
{
    public Customer(IEntityBaseServices<Customer> services) : base(services) { }

    [Required(ErrorMessage = "Customer name is required")]
    public partial string Name { get; set; }

    [EmailAddress]
    public partial string Email { get; set; }

    // Child entity with automatic parent cascade
    public partial IOrderList Orders { get; set; }

    // RemoteFactory method: Runs on server, result transferred to client
    [Remote]
    [Insert]
    public Task Insert([Service] ICustomerRepository repo)
    {
        // Persistence logic here
        return Task.CompletedTask;
    }

    [Create]
    public void Create() { }
}

public interface IOrder : IEntityBase { }

[Factory]
public partial class Order : EntityBase<Order>, IOrder
{
    public Order(IEntityBaseServices<Order> services) : base(services) { }

    public partial decimal Amount { get; set; }

    [Create]
    public void Create() { }
}

public interface IOrderList : IEntityListBase<IOrder> { }

public class OrderList : EntityListBase<IOrder>, IOrderList { }

// Mock repository interface for the sample
public interface ICustomerRepository { }
#endregion

/// <summary>
/// Tests for README.md snippets.
/// </summary>
public class ReadmeSamplesTests : SamplesTestBase
{
    [Fact]
    public void TeaserSample_BusinessRulesComputeFullName()
    {
        var factory = GetRequiredService<IEmployeeFactory>();

        // Create employee via factory
        var employee = factory.Create();

        // Set properties - business rule computes FullName
        employee.FirstName = "Alice";
        employee.LastName = "Johnson";

        Assert.Equal("Alice Johnson", employee.FullName);
    }

    [Fact]
    public void TeaserSample_ValidationRulesEnforced()
    {
        var factory = GetRequiredService<IEmployeeFactory>();
        var employee = factory.Create();

        // Invalid salary triggers validation
        employee.Salary = -1000;

        Assert.False(employee.IsValid);
    }

    [Fact]
    public void TeaserSample_ValidEmployeeIsValid()
    {
        var factory = GetRequiredService<IEmployeeFactory>();
        var employee = factory.Create();

        employee.FirstName = "Bob";
        employee.LastName = "Smith";
        employee.Salary = 50000;

        Assert.True(employee.IsValid);
    }

    [Fact]
    public void QuickStartSample_ValidateBaseValidation()
    {
        var factory = GetRequiredService<ICustomerSearchFactory>();
        var search = factory.Create();

        // Empty search term is invalid
        search.SearchTerm = "";
        Assert.False(search.IsValid);

        // Valid search term
        search.SearchTerm = "widgets";
        search.MaxResults = 10;
        Assert.True(search.IsValid);
    }

    [Fact]
    public void QuickStartSample_EntityBaseRequiredValidation()
    {
        var factory = GetRequiredService<ICustomerFactory>();
        var customer = factory.Create();

        // Missing required name
        customer.Name = "";
        Assert.False(customer.IsValid);

        // Valid name
        customer.Name = "Acme Corp";
        Assert.True(customer.IsValid);
    }

    [Fact]
    public void QuickStartSample_ChangeTrackingWorks()
    {
        var factory = GetRequiredService<ICustomerFactory>();
        var customer = factory.Create();

        // New entity starts with properties modified from Create
        // Clear to test subsequent modifications
        customer.FactoryComplete(FactoryOperation.Fetch);

        Assert.False(customer.IsSelfModified);

        customer.Name = "Test Corp";
        Assert.True(customer.IsSelfModified);
        Assert.Contains("Name", customer.ModifiedProperties);
    }
}
