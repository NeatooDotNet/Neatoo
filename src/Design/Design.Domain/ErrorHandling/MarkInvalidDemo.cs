// -----------------------------------------------------------------------------
// Design.Domain - MarkInvalid (object-level invalid state)
// -----------------------------------------------------------------------------
// MarkInvalid records an object-level failure that no property rule expresses:
// a result that arrived from outside the object. Whether this is a legitimate
// validation channel (validation is otherwise always a rule) is not settled;
// this file shows the mechanics only.
// -----------------------------------------------------------------------------

using Neatoo;
using Neatoo.RemoteFactory;

namespace Design.Domain.ErrorHandling;

#region docs-mark-invalid
/// <summary>
/// Demonstrates: MarkInvalid, the object-level invalid flag for a failure that
/// belongs to no single property.
/// </summary>
[Factory]
internal partial class PaymentDemo : ValidateBase<PaymentDemo>, IPaymentDemo
{
    public partial string? Reference { get; set; }
    public partial decimal Amount { get; set; }

    public PaymentDemo(IValidateBaseServices<PaymentDemo> services) : base(services) { }

    [Create]
    public void Create() { }

    // MarkInvalid is protected: only the object itself records an object-level
    // failure. The message appears in PropertyMessages (under ObjectInvalid)
    // and IsValid is false while ObjectInvalid is set.
    public void RecordGatewayRejection(string reason) => MarkInvalid(reason);
}
#endregion
