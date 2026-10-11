// -----------------------------------------------------------------------------
// Design.Domain - A Rule That Takes a Domain Service
// -----------------------------------------------------------------------------
// A rule may call the server (ruling D1). AsyncRules.cs shows the first shape:
// the rule takes the delegate of a [Remote, Execute] command. This file shows
// the second (ruling D21): the rule takes an interface-factory domain service,
// IShippingRateService, in its constructor. On the client that interface
// resolves to RemoteFactory's generated proxy, so each call crosses to the
// server; on the server it resolves to the implementation. Either way the
// rule holds a wrapped server call, never a server-only service.
//
// Both shapes are allowed and neither is preferred. What a rule may NOT take
// is a server-only service (a repository, a DbContext): the rule is built by
// DI with its entity on both tiers, and that dependency cannot be built on
// the client.
//
// The target is an input model (ValidateBase): a shipment request a person
// fills in on a form. It is edited and validated, not persisted as itself.
// -----------------------------------------------------------------------------

using Design.Domain.Services;
using Neatoo;
using Neatoo.RemoteFactory;
using Neatoo.Rules;

namespace Design.Domain.Rules;

/// <summary>
/// Input model: a shipment request being filled in.
/// </summary>
public interface IShipmentRequest : IValidateBase
{
    string? Destination { get; set; }
    decimal WeightKg { get; set; }
    decimal Quote { get; }
}

/// <summary>
/// Rule interface: the input model takes the rule from DI by this interface.
/// </summary>
internal interface IShippingQuoteRule : IRule<ShipmentRequest> { }

#region skill-rule-with-domain-service
/// <summary>
/// Demonstrates: a rule that calls the server through an interface-factory
/// domain service. One rule, both tiers; the service is the proxy on the
/// client and the implementation on the server.
/// </summary>
internal class ShippingQuoteRule : AsyncRuleBase<ShipmentRequest>, IShippingQuoteRule
{
    private readonly IShippingRateService _rates;

    public ShippingQuoteRule(IShippingRateService rates)
        : base(t => t.Destination, t => t.WeightKg)
    {
        _rates = rates;
    }

    protected override async Task<IRuleMessages> Execute(ShipmentRequest target, CancellationToken? token = null)
    {
        if (string.IsNullOrWhiteSpace(target.Destination) || target.WeightKg <= 0)
        {
            target.Quote = 0m;
            return None;
        }

        if (!await _rates.ShipsTo(target.Destination))
        {
            target.Quote = 0m;
            return (nameof(ShipmentRequest.Destination), $"We do not ship to {target.Destination}").AsRuleMessages();
        }

        target.Quote = await _rates.Quote(target.Destination, target.WeightKg);
        return None;
    }
}

/// <summary>
/// Input model whose quote comes from the domain service while the user edits.
/// </summary>
[Factory]
internal partial class ShipmentRequest : ValidateBase<ShipmentRequest>, IShipmentRequest
{
    public partial string? Destination { get; set; }
    public partial decimal WeightKg { get; set; }
    public partial decimal Quote { get; set; }

    public ShipmentRequest(
        IValidateBaseServices<ShipmentRequest> services,
        IShippingQuoteRule quoteRule) : base(services)
    {
        RuleManager.AddRule(quoteRule);
    }

    [Create]
    public void Create() { }
}
#endregion
