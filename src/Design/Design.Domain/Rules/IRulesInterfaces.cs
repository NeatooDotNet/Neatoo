// -----------------------------------------------------------------------------
// Design.Domain - Rules Demo Interfaces
// -----------------------------------------------------------------------------
// Interface-first pattern for rule system demonstration entities.
// Every entity gets a matched public interface; concretes are internal.
// -----------------------------------------------------------------------------

using Neatoo;

namespace Design.Domain.Rules;

/// <summary>
/// Root interface for rule basics demo entity.
/// </summary>
public interface IRuleBasicsDemo : IEntityRoot
{
    string? Name { get; set; }
    int Quantity { get; set; }
    decimal Price { get; set; }
    decimal Total { get; set; }
}

/// <summary>
/// Root interface for fluent rules demo entity.
/// </summary>
public interface IFluentRulesDemo : IEntityRoot
{
    string? Name { get; set; }
    string? Email { get; set; }
    int Quantity { get; set; }
    decimal UnitPrice { get; set; }
    decimal Total { get; set; }
    string? Status { get; set; }
}

/// <summary>
/// Interface for trigger patterns demo (ValidateBase, no persistence tracking).
/// </summary>
public interface ITriggerPatternsDemo : IValidateBase
{
    int A { get; set; }
    int B { get; set; }
    int C { get; set; }
    int Sum { get; set; }
    bool IsOverLimit { get; set; }
}

/// <summary>
/// Root interface for async rules demo entity.
/// </summary>
public interface IAsyncRulesDemo : IEntityRoot
{
    string? Email { get; set; }
    string? Username { get; set; }
    bool IsUsernameAvailable { get; set; }
    string? ExternalData { get; set; }
}
