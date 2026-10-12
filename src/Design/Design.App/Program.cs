using Design.App;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// The whole client composition lives in ClientServices.cs so the tests can
// call the same method. Add registrations THERE, not here.
builder.Services.AddDesignClient(new Uri(builder.HostEnvironment.BaseAddress));

await builder.Build().RunAsync();
