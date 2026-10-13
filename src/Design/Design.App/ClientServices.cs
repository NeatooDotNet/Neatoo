// -----------------------------------------------------------------------------
// Design.App - Client Composition
// -----------------------------------------------------------------------------
// Everything the Blazor WebAssembly client registers, behind one method that
// Program.cs calls with the host's base address and the tests call with any
// address. Keeping the composition out of Program.cs is what lets a test prove
// the client is wired for Remote mode: Program.cs itself cannot run in a test.
//
// Neatoo's part is one line: AddNeatooServices in Remote mode with the domain
// assembly. It registers Neatoo's services, RemoteFactory's client services and
// the generated factories, which on this tier are proxies - every [Remote]
// operation becomes an HTTP call to the server. The keyed HttpClient is what
// RemoteFactory sends those calls through; see the RemoteFactory skill for it.
// -----------------------------------------------------------------------------

using Design.Domain.Aggregates.OrderAggregate;
using Design.Domain.DI;
using Design.App.ViewModels;
using MudBlazor.Services;
using Neatoo;
using Neatoo.RemoteFactory;

namespace Design.App;

internal static class ClientServices
{
    #region skill-client-composition
    /// <summary>
    /// Registers the client tier. <paramref name="baseAddress"/> is the server
    /// the generated proxies call.
    /// </summary>
    internal static IServiceCollection AddDesignClient(this IServiceCollection services, Uri baseAddress)
    {
        // Neatoo on the client: Remote mode, the domain assembly. The generated
        // factories are proxies; [Remote] operations cross to the server.
        services.AddNeatooServices(NeatooFactory.Remote, typeof(IOrder).Assembly);

        // The domain's DI-provided rules, registered on both tiers.
        services.AddDesignDomainRules();

        // RemoteFactory's transport: this app's own two-line helper, not a
        // library method. It registers the keyed HttpClient the proxies call
        // through (RemoteFactoryServices.HttpClientKey) - the RemoteFactory
        // skill shows the registration.
        services.AddRemoteFactoryTransport(baseAddress);

        services.AddMudServices();

        // The screens' ViewModels: plain classes over a live aggregate.
        services.AddTransient<OrderEditViewModel>();
        services.AddTransient<WorkOrderViewModel>();
        services.AddTransient<EmployeeEditViewModel>();

        return services;
    }
    #endregion

    /// <summary>
    /// RemoteFactory sends every [Remote] call through this keyed HttpClient.
    /// Kept outside the Neatoo region: the RemoteFactory skill owns it.
    /// </summary>
    private static IServiceCollection AddRemoteFactoryTransport(this IServiceCollection services, Uri baseAddress)
    {
        services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey,
            (_, _) => new HttpClient { BaseAddress = baseAddress });
        return services;
    }
}
