using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MudBlazor.Services;
using Neatoo;
using Neatoo.RemoteFactory;
using DomainModel;
using Person.App;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMudServices();

// Incorporate Neatoo (which includes RemoteFactory)
builder.Services.AddNeatooServices(NeatooFactory.Remote, typeof(IPerson).Assembly);
builder.Services.AddKeyedScoped(Neatoo.RemoteFactory.RemoteFactoryServices.HttpClientKey, (sp, key) => {
		return new HttpClient { BaseAddress = new Uri("http://localhost:5183/") };
});


// App Specific
builder.Services.RegisterMatchingName(typeof(IPersonAuth).Assembly);

builder.Services.RemoveAll<IUser>();
builder.Services.AddSingleton<IUser, User>();

await builder.Build().RunAsync();
