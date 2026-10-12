// -----------------------------------------------------------------------------
// Design.Server - Server Composition
// -----------------------------------------------------------------------------
// Everything the server registers, behind one method that Program.cs calls
// and the tests call. A test that re-listed the registrations would pass no
// matter what the server did; one that calls this method cannot drift from it.
//
// Neatoo's part is one line: AddNeatooServices in Server mode with the domain
// assembly. It registers Neatoo's services, RemoteFactory's server services
// (including the request handler UseNeatoo() maps) and the generated
// factories, which on this tier run the operations in-process.
//
// DESIGN DECISION: AddNeatooAspNetCore is NOT called here.
//
// RemoteFactory's AddNeatooAspNetCore also reaches AddNeatooRemoteFactory, and
// that method registers with Add, not TryAdd: whichever call runs last owns
// IServiceAssemblies and the JSON converter factory. Called after
// AddNeatooServices, it replaces the assembly list with one that lacks
// Neatoo.dll and puts RemoteFactory's interface converter back beside
// Neatoo's. The server then cannot deserialize any Neatoo entity the client
// sends: every [Remote] Save fails, while Fetch keeps working, so the break is
// easy to miss. Called before, it is harmless but adds nothing this host uses
// (only IAspAuthorize, for [AspAuthorize]). RemoteFactory#117 tracks making the
// double call safe; until then, AddNeatooServices alone is the server's Neatoo
// line, and UseNeatoo() in Program.cs has everything it needs.
// -----------------------------------------------------------------------------

using Design.Domain.Aggregates.OrderAggregate;
using Design.Domain.DI;
using Design.Infrastructure;
using Neatoo;
using Neatoo.RemoteFactory;

namespace Design.Server;

internal static class ServerServices
{
    #region skill-server-composition
    /// <summary>
    /// Registers the server tier: Neatoo in Server mode, the domain's rules,
    /// and the server-only services the domain's operations take.
    /// </summary>
    internal static IServiceCollection AddDesignServer(this IServiceCollection services)
    {
        // Neatoo on the server: Server mode, the domain assembly. The generated
        // factories run the operations here; this also registers what
        // UseNeatoo() resolves. Do not add AddNeatooAspNetCore after this line -
        // see the header (RemoteFactory#117).
        services.AddNeatooServices(NeatooFactory.Server, typeof(IOrder).Assembly);

        // The domain's DI-provided rules, registered on both tiers.
        services.AddDesignDomainRules();

        // The [Service] dependencies the operations resolve: repositories and
        // the other server-only services. Singleton here so the in-memory rows
        // survive across requests.
        services.AddDesignInfrastructure();

        return services;
    }
    #endregion
}
