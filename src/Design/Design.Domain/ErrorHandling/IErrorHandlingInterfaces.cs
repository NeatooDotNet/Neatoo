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

/// <summary>
/// Form object whose validity can be revoked by a result from outside the object.
/// </summary>
public interface IPaymentDemo : IValidateBase
{
    string? Reference { get; set; }
    decimal Amount { get; set; }
    string? ObjectInvalid { get; }
    void RecordGatewayRejection(string reason);
}
