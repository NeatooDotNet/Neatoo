// -----------------------------------------------------------------------------
// Design.Domain - Property System Demo Interfaces
// -----------------------------------------------------------------------------
// Interface-first pattern for property system demonstration entities.
// Every entity gets a matched public interface; concretes are internal.
// -----------------------------------------------------------------------------

using Neatoo;

namespace Design.Domain.PropertySystem;

/// <summary>
/// Root interface for property basics demo entity.
/// </summary>
public interface IPropertyBasicsDemo : IEntityRoot
{
    string? Name { get; set; }
    int Count { get; set; }
    decimal Price { get; set; }
    string? Title { get; set; }
    int Quantity { get; set; }
    IPropertyChildDemo? Child { get; set; }
}

/// <summary>
/// Child interface for property child demo (ValidateBase, no persistence tracking).
/// </summary>
public interface IPropertyChildDemo : IValidateBase
{
    string? Value { get; set; }
}

/// <summary>
/// Root interface for SetValue vs LoadValue demo entity.
/// </summary>
public interface ISetValueVsLoadValueDemo : IEntityRoot
{
    string? Name { get; set; }
    int Value { get; set; }
}

/// <summary>
/// Root interface for indexer access demo entity.
/// </summary>
public interface IIndexerAccessDemo : IEntityRoot
{
    string? Name { get; set; }
    int Amount { get; set; }
}

/// <summary>
/// Interface for validation state demo (ValidateBase, no persistence tracking).
/// </summary>
public interface IValidationStateDemo : IValidateBase
{
    string? RequiredField { get; set; }
    IValidationChildDemo? Child { get; set; }
}

/// <summary>
/// Interface for validation child demo (ValidateBase).
/// </summary>
public interface IValidationChildDemo : IValidateBase
{
    string? RequiredField { get; set; }
}

/// <summary>
/// Root interface for modification state demo entity.
/// </summary>
public interface IModificationStateDemo : IEntityRoot
{
    string? Name { get; set; }
    IModificationChildDemo? Child { get; set; }
}

/// <summary>
/// Child interface for modification child demo entity.
/// </summary>
public interface IModificationChildDemo : IEntityBase
{
    string? Value { get; set; }
}

/// <summary>
/// Root interface for save state demo entity.
/// </summary>
public interface ISaveStateDemo : IEntityRoot
{
    string? Name { get; set; }
}

/// <summary>
/// Interface for busy state demo (ValidateBase, no persistence tracking).
/// </summary>
public interface IBusyStateDemo : IValidateBase
{
    string? Name { get; set; }
    string? ComputedValue { get; set; }
}
