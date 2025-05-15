using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Security.Claims;
using System.Security.Principal;
using Microsoft.Extensions.DependencyInjection;
using Neatoo;
using Neatoo.Internal;
using Neatoo.RemoteFactory;
using Neatoo.Console;
using System.Diagnostics;

var stopwatch = new Stopwatch();
var serviceContainer = new ServiceCollection();
serviceContainer.AddNeatooServices(NeatooFactory.StandAlone, Assembly.GetExecutingAssembly());
serviceContainer.AddScoped<NeatooEntityBaseAuth>();
serviceContainer.AddScoped<IPrincipal>(s => CreateDefaultClaimsPrincipal());
var serviceProvider = serviceContainer.BuildServiceProvider();

var neatooFactory = serviceProvider.GetRequiredService<INeatooEntityBaseFactory>();
stopwatch.Reset();
stopwatch.Start();
var neatooEntity = neatooFactory.Create();
stopwatch.Stop();


Console.WriteLine($"Create: {stopwatch.ElapsedMilliseconds} ms");
Console.WriteLine($"{NeatooEntityBase.TotalCount}");

static ClaimsPrincipal CreateDefaultClaimsPrincipal()
{
    var identity = new ClaimsIdentity(new GenericIdentity("Admin"));

    identity.AddClaim(new Claim("Id", Guid.NewGuid().ToString()));
    identity.AddClaim(new Claim(ClaimTypes.Role, "Admin"));

    return new ClaimsPrincipal(identity);
}


