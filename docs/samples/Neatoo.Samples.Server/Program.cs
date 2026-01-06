using Neatoo;
using Neatoo.RemoteFactory;
using Neatoo.Samples.DomainModel.SampleDomain;

var builder = WebApplication.CreateBuilder(args);

#region docs:remote-factory:server-di-setup
builder.Services.AddNeatooServices(NeatooFactory.Server, typeof(IPerson).Assembly);
#endregion

var app = builder.Build();

#region docs:remote-factory:server-endpoint
app.MapPost("/api/neatoo", (HttpContext httpContext, RemoteRequestDto request, CancellationToken cancellationToken) =>
{
    var handleRemoteDelegateRequest = httpContext.RequestServices.GetRequiredService<HandleRemoteDelegateRequest>();
    return handleRemoteDelegateRequest(request, cancellationToken);
});
#endregion

await app.RunAsync();
