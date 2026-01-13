using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Neatoo;
using Neatoo.RemoteFactory;

namespace Neatoo.Samples.BlazorClient;

// Supporting types for installation samples
public interface IMyAggregate : IEntityBase { }
public interface IMyValidationRule { }
public class MyValidationRule : IMyValidationRule { }

/// <summary>
/// Client configuration samples for installation guide.
/// </summary>
public static class InstallationSamples
{
    #region client-program-cs
    // Blazor WebAssembly Program.cs - Complete configuration example
    public static void ConfigureClientProgram(WebAssemblyHostBuilder builder)
    {
        // Add Neatoo services for client-side with remote factory calls
        builder.Services.AddNeatooServices(NeatooFactory.Remote, typeof(IMyAggregate).Assembly);

        // Configure HttpClient for API calls
        builder.Services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
            new HttpClient { BaseAddress = new Uri("https://localhost:5001") });

        // Register validation rules (same rules run on client)
        builder.Services.AddScoped<IMyValidationRule, MyValidationRule>();
    }
    #endregion
}
