// -----------------------------------------------------------------------------
// Design.Tests - Client/Server Pair From the Two Compositions
// -----------------------------------------------------------------------------
// Two containers built by the same methods the apps call: Design.Server's
// AddDesignServer and Design.App's AddDesignClient. The client's keyed
// HttpClient is re-registered over a handler that hands each request straight
// to the server container's HandleRemoteDelegateRequest - no Kestrel, but every
// byte is serialized and deserialized exactly as it would be over the wire.
//
// Why the handler and not a stub IMakeRemoteDelegateRequest: the generated
// factories take the remote path whenever IMakeRemoteDelegateRequest is
// registered, in every mode. Stubbing it would pass against a client seam that
// wrongly registered Server mode. Stubbing the HttpClient leaves RemoteFactory's
// real MakeRemoteDelegateRequest in place, which only a Remote-mode seam
// registers; a Server-mode seam runs the operation locally and the handler
// never sees a request.
// -----------------------------------------------------------------------------

using System.Net;
using System.Text.Json;
using Design.App;
using Design.Server;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;

namespace Design.Tests.CompositionTests;

/// <summary>
/// Scoped on the client container: the server scope this client talks to, and
/// how many requests crossed.
/// </summary>
internal sealed class ServerLink
{
    public IServiceProvider Server { get; set; } = null!;
    public int Calls { get; private set; }
    public void Record() => Calls++;
}

/// <summary>
/// Forwards /api/neatoo posts to the server container's request handler.
/// </summary>
internal sealed class ForwardingHandler : HttpMessageHandler
{
    // RemoteFactory's transport context uses JsonSerializerDefaults.Web (camelCase); match it.
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    private readonly ServerLink _link;

    public ForwardingHandler(ServerLink link)
    {
        _link = link;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _link.Record();

        var body = await request.Content!.ReadAsStringAsync(cancellationToken);
        var remoteRequest = JsonSerializer.Deserialize<RemoteRequestDto>(body, WebJson)
            ?? throw new InvalidOperationException("Empty remote request");

        var handle = _link.Server.GetRequiredService<HandleRemoteDelegateRequest>();
        var remoteResponse = await handle(remoteRequest, cancellationToken);

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(remoteResponse, WebJson), System.Text.Encoding.UTF8, "application/json"),
        };
    }
}

#region skill-client-server-pair
internal static class DesignClientServer
{
    private static readonly Lock _lock = new();
    private static IServiceProvider? _server;
    private static IServiceProvider? _client;

    /// <summary>
    /// A fresh server scope and a client scope wired to it.
    /// </summary>
    public static (IServiceScope Server, IServiceScope Client, ServerLink Link) Scopes()
    {
        lock (_lock)
        {
            if (_server is null || _client is null)
            {
                // The server, exactly as Program.cs composes it.
                var serverServices = new ServiceCollection();
                serverServices.AddDesignServer();
                _server = serverServices.BuildServiceProvider();

                // The client, exactly as Program.cs composes it, then the keyed
                // HttpClient re-registered (last wins) to forward into the server.
                var clientServices = new ServiceCollection();
                clientServices.AddDesignClient(new Uri("http://design.test/"));
                clientServices.AddScoped<ServerLink>();
                clientServices.AddKeyedScoped(RemoteFactoryServices.HttpClientKey,
                    (sp, _) => new HttpClient(new ForwardingHandler(sp.GetRequiredService<ServerLink>()))
                    {
                        BaseAddress = new Uri("http://design.test/"),
                    });
                _client = clientServices.BuildServiceProvider();
            }
        }

        var server = _server.CreateScope();
        var client = _client.CreateScope();
        var link = client.ServiceProvider.GetRequiredService<ServerLink>();
        link.Server = server.ServiceProvider;

        return (server, client, link);
    }
}
#endregion
