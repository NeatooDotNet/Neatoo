using HorseBarn.lib;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Neatoo;
using System.Reflection;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddNeatooServices( Neatoo.RemoteFactory.NeatooFactory.Remote, Assembly.GetAssembly(typeof(IHorseBarn)));

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
