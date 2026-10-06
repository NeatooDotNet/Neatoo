// -----------------------------------------------------------------------------
// Design.Domain - Base Class Demo Interfaces
// -----------------------------------------------------------------------------
// Interface-first pattern for base class demonstration entities.
// Even simple demos follow the pattern: every entity gets an interface.
// -----------------------------------------------------------------------------

using Neatoo;

namespace Design.Domain.BaseClasses;

/// <summary>
/// Interface for ValidateBase demo — value objects and validation-only scenarios.
/// </summary>
public interface IDemoValueObject : IValidateBase
{
    string? Name { get; set; }
    string? Description { get; set; }
}

/// <summary>
/// Root interface for EntityBase demo — persistent domain entities.
/// </summary>
public interface IDemoEntity : IEntityRoot
{
    string? Name { get; set; }
    int Value { get; set; }
}

/// <summary>
/// Child interface for the EntityListBase demo. Extends IEntityBase: a child
/// has no IsSavable and no Save().
/// </summary>
public interface IDemoChild : IEntityBase
{
    string? Name { get; set; }
}

/// <summary>
/// Root interface that owns the EntityListBase demo list.
/// </summary>
public interface IDemoParent : IEntityRoot
{
    IDemoEntityList? Children { get; }
}

/// <summary>
/// List interface for ValidateListBase demo — parameterized on child INTERFACE.
/// </summary>
public interface IDemoValueObjectList : IValidateListBase<IDemoValueObject> { }

/// <summary>
/// List interface for EntityListBase demo — parameterized on child INTERFACE.
/// </summary>
public interface IDemoEntityList : IEntityListBase<IDemoChild>
{
    int DeletedCount { get; }
}
