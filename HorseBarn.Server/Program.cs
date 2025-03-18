using HorseBarn.lib;
using System.Reflection;
using HorseBarn.Dal.Ef;
using Neatoo;
using HorseBarn.lib.Horse;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddNeatooServices( Neatoo.RemoteFactory.NeatooFactory.Local, Assembly.GetExecutingAssembly(), Assembly.GetAssembly(typeof(IHorseBarn)));


builder.Services.AddScoped<IHorseBarnContext, HorseBarnContext>();

builder.Services.AddTransient<IsHorseNameUniqueServer>();

builder.Services.AddTransient<IsHorseNameUnique>(cc =>
{
    return (name) => cc.GetRequiredService<IsHorseNameUniqueServer>().IsHorseNameUnique(name);
});

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseAuthorization();

app.MapControllers();

app.Run();
