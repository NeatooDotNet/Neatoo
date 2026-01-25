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
}

public interface IAddress : IEntityBase { }

[Factory]
public partial class Address : EntityBase<Address>, IAddress
{
    public Address(IEntityBaseServices<Address> services) : base(services) { }

    [Required]
    public partial string Street { get; set; }

    public partial string City { get; set; }
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
}

public interface IOrder : IEntityBase { }

[Factory]
public partial class Order : EntityBase<Order>, IOrder
{
    public Order(IEntityBaseServices<Order> services) : base(services) { }

    public partial decimal Amount { get; set; }
}

public interface IOrderList : IEntityListBase<IOrder> { }

public class OrderList : EntityListBase<IOrder>, IOrderList { }

// Mock repository interface for the sample
public interface ICustomerRepository { }
#endregion

/// <summary>
/// Tests for README.md snippets.
/// </summary>
public class ReadmeSamplesTests
{
    [Fact]
    public void TeaserSample_BusinessRulesComputeFullName()
    {
        // Create employee with real Neatoo infrastructure
        var employee = new Employee(new EntityBaseServices<Employee>());

        // Set properties - business rule computes FullName
        employee.FirstName = "Alice";
        employee.LastName = "Johnson";

        Assert.Equal("Alice Johnson", employee.FullName);
    }

    [Fact]
    public void TeaserSample_ValidationRulesEnforced()
    {
        var employee = new Employee(new EntityBaseServices<Employee>());

        // Invalid salary triggers validation
        employee.Salary = -1000;

        Assert.False(employee.IsValid);
    }

    [Fact]
    public void TeaserSample_ValidEmployeeIsValid()
    {
        var employee = new Employee(new EntityBaseServices<Employee>());

        employee.FirstName = "Bob";
        employee.LastName = "Smith";
        employee.Salary = 50000;

        Assert.True(employee.IsValid);
    }

    [Fact]
    public void QuickStartSample_ValidateBaseValidation()
    {
        var search = new CustomerSearch(new ValidateBaseServices<CustomerSearch>());

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
        var customer = new Customer(new EntityBaseServices<Customer>());

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
        var customer = new Customer(new EntityBaseServices<Customer>());

        // Entity tracks modifications when properties change
        Assert.False(customer.IsSelfModified);

        customer.Name = "Test Corp";
        Assert.True(customer.IsSelfModified);
        Assert.Contains("Name", customer.ModifiedProperties);
    }
}
