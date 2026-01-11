using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Neatoo;
using Neatoo.RemoteFactory;
using Neatoo.Samples.BlazorClient;
using Neatoo.Samples.DomainModel.SampleDomain;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

#region client-di-setup
builder.Services.AddNeatooServices(NeatooFactory.Remote, typeof(IPerson).Assembly);
builder.Services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
    new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
#endregion

await builder.Build().RunAsync();
