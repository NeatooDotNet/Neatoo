/// <summary>
/// Code samples for docs/aggregates-and-entities.md - EntityBase section
///
/// Snippets in this file:
/// - docs:aggregates-and-entities:entitybase-basic
/// - docs:aggregates-and-entities:interface-requirement
/// - docs:aggregates-and-entities:aggregate-root-class
/// - docs:aggregates-and-entities:partial-properties
/// - docs:aggregates-and-entities:data-annotations
///
/// Corresponding tests: EntityBaseSamplesTests.cs
/// </summary>

using Neatoo.RemoteFactory;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Neatoo.Documentation.Samples.AggregatesAndEntities;

#region docs:aggregates-and-entities:entitybase-basic
/// <summary>
/// Basic EntityBase example showing automatic state tracking.
/// </summary>
public partial interface IOrder : IEntityBase
{
    Guid Id { get; set; }
    string? Status { get; set; }
    decimal Total { get; set; }
}

[Factory]
internal partial class Order : EntityBase<Order>, IOrder
{
    public Order(IEntityBaseServices<Order> services) : base(services) { }

    public partial Guid Id { get; set; }
    public partial string? Status { get; set; }     // IsModified tracked automatically
    public partial decimal Total { get; set; }      // IsSavable updated on change

    [Create]
    public void Create()
    {
        Id = Guid.NewGuid();
        Status = "New";
    }
}
#endregion

#region docs:aggregates-and-entities:interface-requirement
/// <summary>
/// Every aggregate requires a public interface for factory generation.
/// </summary>
public partial interface ICustomer : IEntityBase
{
    // Properties are auto-generated from the partial class
}
#endregion

#region docs:aggregates-and-entities:aggregate-root-class
/// <summary>
/// Complete aggregate root class pattern.
/// </summary>
[Factory]
internal partial class Customer : EntityBase<Customer>, ICustomer
{
    public Customer(IEntityBaseServices<Customer> services) : base(services) { }

    // Properties
    public partial Guid? Id { get; set; }
    public partial string? FirstName { get; set; }
    public partial string? LastName { get; set; }
    public partial string? Email { get; set; }

    // Child collection
    public partial ICustomerAddressList? AddressList { get; set; }

    [Create]
    public void Create()
    {
        Id = Guid.NewGuid();
    }
}

// Placeholder interface for child collection
public partial interface ICustomerAddressList : IEntityListBase<ICustomerAddress> { }
public partial interface ICustomerAddress : IEntityBase { }
#endregion

#region docs:aggregates-and-entities:partial-properties
/// <summary>
/// Demonstrates partial vs non-partial properties.
/// </summary>
public partial interface IEmployee : IEntityBase
{
    string? FirstName { get; set; }
    string? LastName { get; set; }
    string FullName { get; }
    bool IsExpanded { get; set; }
}

[Factory]
internal partial class Employee : EntityBase<Employee>, IEmployee
{
    public Employee(IEntityBaseServices<Employee> services) : base(services) { }

    // Correct - generates backing field with change tracking
    public partial string? FirstName { get; set; }
    public partial string? LastName { get; set; }

    #region docs:aggregates-and-entities:non-partial-properties
    // Calculated property - not tracked, not serialized
    public string FullName => $"{FirstName} {LastName}";

    // UI-only property - not transferred to server
    public bool IsExpanded { get; set; }
    #endregion

    [Create]
    public void Create() { }
}
#endregion

#region docs:aggregates-and-entities:data-annotations
/// <summary>
/// Using data annotations for display and validation.
/// </summary>
public partial interface IContact : IEntityBase
{
    string? FirstName { get; set; }
    string? Email { get; set; }
}

[Factory]
internal partial class Contact : EntityBase<Contact>, IContact
{
    public Contact(IEntityBaseServices<Contact> services) : base(services) { }

    [DisplayName("First Name*")]
    [Required(ErrorMessage = "First Name is required")]
    public partial string? FirstName { get; set; }

    [DisplayName("Email Address")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public partial string? Email { get; set; }

    [Create]
    public void Create() { }
}
#endregion
