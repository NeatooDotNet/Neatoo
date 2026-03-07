// -----------------------------------------------------------------------------
// Design.Domain - Value Object Demo Interfaces
// -----------------------------------------------------------------------------
// Interface-first pattern for value object demonstration entities.
// Every entity/list gets a matched public interface; concretes are internal.
// -----------------------------------------------------------------------------

using Neatoo;

namespace Design.Domain.ValueObjects;

/// <summary>
/// Interface for employee list item (ValidateBase read model).
/// </summary>
public interface IEmployeeListItem : IValidateBase
{
    int Id { get; set; }
    string? FullName { get; set; }
    string? Email { get; set; }
    string? Department { get; set; }
    bool IsActive { get; set; }
}

/// <summary>
/// List interface for employee list (ValidateListBase of read models).
/// Parameterized on child INTERFACE, not concrete.
/// </summary>
public interface IEmployeeList : IValidateListBase<IEmployeeListItem> { }
