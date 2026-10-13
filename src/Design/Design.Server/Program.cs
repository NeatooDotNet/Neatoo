using Design.Server;
using Neatoo.RemoteFactory.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// The whole server composition lives in ServerServices.cs so the tests can
// call the same method. Add registrations THERE, not here.
builder.Services.AddDesignServer();

var app = builder.Build();

// Hosted Blazor WebAssembly: serve the client's framework files and static
// content before the Neatoo endpoint, so static requests never reach it.
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

// RemoteFactory's endpoint (/api/neatoo) as middleware. What it resolves is
// registered by AddNeatooServices(Server) above; see the RemoteFactory skill.
app.UseNeatoo();

// SPA fallback after every other route
app.MapFallbackToFile("index.html");

await app.RunAsync();
