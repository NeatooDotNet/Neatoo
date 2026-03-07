// -----------------------------------------------------------------------------
// Design.Domain - Employee Aggregate Interface-First Pattern
// -----------------------------------------------------------------------------
// Every entity and list gets a matched public interface.
// Root interfaces extend IEntityRoot. Child interfaces extend IEntityBase.
// List interfaces extend IEntityListBase<IChild>.
// Concrete classes are internal.
// -----------------------------------------------------------------------------

using Neatoo;

namespace Design.Domain.Entities;

/// <summary>
/// Aggregate root interface for Employee.
/// Extends IEntityRoot — exposes IsSavable and Save().
/// All property types use interfaces, never concretes.
/// </summary>
public interface IEmployee : IEntityRoot
{
    int Id { get; }
    string? FirstName { get; set; }
    string? LastName { get; set; }
    string? Email { get; set; }
    DateTime? HireDate { get; set; }
    decimal Salary { get; set; }
    bool IsActive { get; set; }
    IAddressList? Addresses { get; }
    string FullName { get; }
}

/// <summary>
/// Child entity interface for Address.
/// Extends IEntityBase — no IsSavable, no Save().
/// Addresses are saved through the Employee aggregate root.
/// </summary>
public interface IAddress : IEntityBase
{
    int Id { get; }
    string? Street { get; set; }
    string? City { get; set; }
    string? State { get; set; }
    string? ZipCode { get; set; }
    string? AddressType { get; set; }
}

/// <summary>
/// List interface for AddressList.
/// Extends IEntityListBase parameterized on child INTERFACE.
/// </summary>
public interface IAddressList : IEntityListBase<IAddress> { }
