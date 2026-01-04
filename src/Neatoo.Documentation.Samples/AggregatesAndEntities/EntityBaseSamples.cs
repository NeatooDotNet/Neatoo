/// <summary>
/// Code samples for docs/aggregates-and-entities.md - EntityBase section
///
/// Full snippets (for complete examples):
/// - docs:aggregates-and-entities:entitybase-basic
/// - docs:aggregates-and-entities:interface-requirement
/// - docs:aggregates-and-entities:aggregate-root-class
/// - docs:aggregates-and-entities:partial-properties
/// - docs:aggregates-and-entities:data-annotations
///
/// Micro-snippets (for focused inline examples):
/// - docs:aggregates-and-entities:state-tracking-properties
/// - docs:aggregates-and-entities:inline-validation-rule
/// - docs:aggregates-and-entities:class-declaration
/// - docs:aggregates-and-entities:entity-constructor
/// - docs:aggregates-and-entities:partial-property-declaration
/// - docs:aggregates-and-entities:non-partial-properties
/// - docs:aggregates-and-entities:displayname-required
/// - docs:aggregates-and-entities:emailaddress-validation
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
    public Order(IEntityBaseServices<Order> services) : base(services)
    {
        #region docs:aggregates-and-entities:inline-validation-rule
        // Inline validation rule - Total must be positive
        RuleManager.AddValidation(
            t => t.Total <= 0 ? "Total must be greater than zero" : "",
            t => t.Total);
        #endregion
    }

    #region docs:aggregates-and-entities:state-tracking-properties
    public partial Guid Id { get; set; }
    public partial string? Status { get; set; }     // IsModified tracked automatically
    public partial decimal Total { get; set; }      // IsSavable updated on change
    #endregion

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
#region docs:aggregates-and-entities:class-declaration
[Factory]
internal partial class Customer : EntityBase<Customer>, ICustomer
#endregion
{
    #region docs:aggregates-and-entities:entity-constructor
    public Customer(IEntityBaseServices<Customer> services) : base(services) { }
    #endregion

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

    #region docs:aggregates-and-entities:partial-property-declaration
    // Correct - generates backing field with change tracking
    public partial string? FirstName { get; set; }
    public partial string? LastName { get; set; }
    #endregion

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

    #region docs:aggregates-and-entities:displayname-required
    [DisplayName("First Name*")]
    [Required(ErrorMessage = "First Name is required")]
    public partial string? FirstName { get; set; }
    #endregion

    #region docs:aggregates-and-entities:emailaddress-validation
    [DisplayName("Email Address")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public partial string? Email { get; set; }
    #endregion

    [Create]
    public void Create() { }
}
#endregion
