// -----------------------------------------------------------------------------
// Design.Domain - Error Handling Demo Interfaces
// -----------------------------------------------------------------------------
// Interface-first pattern for error handling demonstration entities.
// Every entity gets a matched public interface; concretes are internal.
// -----------------------------------------------------------------------------

using Neatoo;

namespace Design.Domain.ErrorHandling;

/// <summary>
/// Root interface for validation failure demo entity.
/// </summary>
public interface IValidationFailureDemo : IEntityRoot
{
    string? Name { get; set; }
    int Quantity { get; set; }
    string? Email { get; set; }
}
