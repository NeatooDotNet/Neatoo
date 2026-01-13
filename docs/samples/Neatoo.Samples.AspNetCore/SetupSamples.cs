using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Neatoo;
using Neatoo.RemoteFactory;
using Neatoo.Samples.DomainModel.QuickStart;

namespace Neatoo.Samples.AspNetCore;

/// <summary>
/// Server setup examples for documentation.
/// These are static methods showing configuration patterns.
/// </summary>
public static class SetupSamples
{
    #region qs-server-setup
    // Program.cs (ASP.NET Core)
    public static void ConfigureServer(WebApplicationBuilder builder, WebApplication app)
    {
        builder.Services.AddNeatooServices(NeatooFactory.Server, typeof(IOrder).Assembly);

        // Add the RemoteFactory endpoint
        app.MapPost("/api/neatoo", (HttpContext httpContext, RemoteRequestDto request, CancellationToken token) =>
        {
            var handler = httpContext.RequestServices.GetRequiredService<HandleRemoteDelegateRequest>();
            return handler(request, token);
        });
    }
    #endregion

    #region qs-client-setup
    // Program.cs (Blazor WebAssembly)
    public static void ConfigureClient(IServiceCollection services)
    {
        services.AddNeatooServices(NeatooFactory.Remote, typeof(IOrder).Assembly);
        services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
            new HttpClient { BaseAddress = new Uri("https://localhost:5001") });
    }
    #endregion
}
