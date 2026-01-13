using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Neatoo;
using Neatoo.RemoteFactory;

namespace Neatoo.Samples.AspNetCore;

// Supporting types for installation samples
public interface IMyAggregate : IEntityBase { }
public interface IMyValidationRule { }
public class MyValidationRule : IMyValidationRule { }

/// <summary>
/// Server configuration samples for installation guide.
/// </summary>
public static class InstallationSamples
{
    #region server-program-cs
    // Server Program.cs - Complete configuration example
    public static void ConfigureServerProgram(WebApplicationBuilder builder, WebApplication app)
    {
        // Add Neatoo services for server-side execution
        builder.Services.AddNeatooServices(NeatooFactory.Server, typeof(IMyAggregate).Assembly);

        // Register your DbContext (example - actual implementation depends on your setup)
        // builder.Services.AddDbContext<MyDbContext>(options => options.UseSqlServer(connectionString));

        // Register validation rules
        builder.Services.AddScoped<IMyValidationRule, MyValidationRule>();

        // Map the Neatoo RemoteFactory endpoint
        app.MapPost("/api/neatoo", (HttpContext httpContext, RemoteRequestDto request, CancellationToken token) =>
        {
            var handler = httpContext.RequestServices.GetRequiredService<HandleRemoteDelegateRequest>();
            return handler(request, token);
        });
    }
    #endregion
}

// Supporting types for domain model services sample
public interface IUniqueNameRule { }
public class UniqueNameRule : IUniqueNameRule { }
public interface IEmailValidationRule { }
public class EmailValidationRule : IEmailValidationRule { }
public interface IEmailValidationService { }
public class EmailValidationService : IEmailValidationService { }

#region domain-model-services
// Extension method for shared registration
public static class DomainModelServiceExtensions
{
    public static IServiceCollection AddDomainModelServices(this IServiceCollection services)
    {
        // Validation rules
        services.AddScoped<IUniqueNameRule, UniqueNameRule>();
        services.AddScoped<IEmailValidationRule, EmailValidationRule>();

        // Services used by rules/factories
        services.AddScoped<IEmailValidationService, EmailValidationService>();

        return services;
    }
}
#endregion
