// -----------------------------------------------------------------------------
// Design.Domain - A Domain Service the Client Calls (Interface Factory)
// -----------------------------------------------------------------------------
// Shipping rates come from a carrier integration that exists only on the
// server. The client still needs the answers while a person edits, so the
// domain service is declared as a RemoteFactory interface factory: [Factory]
// on the interface, a plain implementation with no attribute. On the server
// the interface resolves to the implementation; on the client RemoteFactory
// generates a proxy that carries each call to the server.
//
// This is a DOMAIN SERVICE, not a repository. A repository is persistence and
// is reached only by [Service] on a server-side factory operation; the client
// never calls one (see RemoteFactory#112 and the RemoteFactory skill for the
// interface-factory pattern itself). What belongs here is a question the
// domain asks of the outside world: can we ship there, and what does it cost.
//
// The implementation is in this assembly only so Design.Tests can run it.
// In an application it lives in the server project and is registered there;
// the client registers nothing for this interface.
// -----------------------------------------------------------------------------

using Neatoo.RemoteFactory;

namespace Design.Domain.Services;

#region skill-domain-service-interface-factory
/// <summary>
/// Domain service the client calls. [Factory] on the interface; RemoteFactory
/// generates the client proxy. Two related calls on one service.
/// </summary>
[Factory]
public interface IShippingRateService
{
    Task<bool> ShipsTo(string destination);
    Task<decimal> Quote(string destination, decimal weightKg);
}

/// <summary>
/// Server implementation. No [Factory]: the interface already has it.
/// Registered on the server only.
/// </summary>
public class ShippingRateService : IShippingRateService
{
    private static readonly Dictionary<string, decimal> RatePerKg = new(StringComparer.OrdinalIgnoreCase)
    {
        ["US"] = 4.00m,
        ["CA"] = 6.50m,
        ["UK"] = 9.00m,
    };

    public Task<bool> ShipsTo(string destination)
        => Task.FromResult(RatePerKg.ContainsKey(destination));

    public Task<decimal> Quote(string destination, decimal weightKg)
        => Task.FromResult(RatePerKg.TryGetValue(destination, out var rate) ? rate * weightKg : 0m);
}
#endregion
