using Microsoft.Extensions.DependencyInjection.Extensions;
using Neatoo;
using Neatoo.RemoteFactory;
using Person.Ef;
using DomainModel;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors();

// Neatoo
builder.Services.AddNeatooServices(NeatooFactory.Server, typeof(IPerson).Assembly);

// App Specific
builder.Services.AddScoped<IPersonDbContext, PersonDbContext>();
builder.Services.RegisterMatchingName(typeof(IPersonAuth).Assembly);

builder.Services.RemoveAll<IUser>();
builder.Services.AddScoped<IUser, User>();

var app = builder.Build();

// Neatoo
app.MapPost("/api/neatoo", (HttpContext httpContext, RemoteRequestDto request) =>
{
	var handleRemoteDelegateRequest = httpContext.RequestServices.GetRequiredService<HandleRemoteDelegateRequest>();
	return handleRemoteDelegateRequest(request);
});


// App Specific
app.UseCors(builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

app.Use((context, next) =>
{
	var role = context.Request.Headers["UserRoles"];
	var user = context.RequestServices.GetRequiredService<IUser>();
	user.Role = Role.None;
	if (!string.IsNullOrEmpty(role))
	{
		user.Role = Enum.Parse<Role>(role.ToString());
	}
	return next(context);
});

await app.RunAsync();

