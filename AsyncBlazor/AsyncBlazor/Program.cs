using AsyncBlazor.Components;
using HorseBarn.Dal.Ef;
using HorseBarn.lib;
using Neatoo;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddControllers();
builder.Services.AddNeatooServices(Neatoo.RemoteFactory.NeatooFactory.Local, Assembly.GetAssembly(typeof(IHorseBarn)));
builder.Services.AddScoped<IHorseBarnContext, HorseBarnContext>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(AsyncBlazor.Client._Imports).Assembly);


app.MapControllers();

app.Run();
