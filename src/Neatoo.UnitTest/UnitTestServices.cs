using Microsoft.Extensions.DependencyInjection;
using Neatoo.UnitTest.TestInfrastructure.ServiceProviders;

namespace Neatoo.UnitTest;

/// <summary>
/// Legacy service provider for backwards compatibility.
/// New tests should use TestServiceProvider from TestInfrastructure.ServiceProviders.
/// </summary>
public static class UnitTestServices
{
    /// <summary>
    /// Gets a service scope for testing. Delegates to TestServiceProvider.
    /// </summary>
    public static IServiceScope GetLifetimeScope(bool localPortal = false)
    {
        return TestServiceProvider.CreateScope(localPortal);
    }
}

/// <summary>
/// Extension methods for IServiceScope to simplify service resolution in tests.
/// </summary>
public static class ServiceScopeProviderExtension
{
    /// <summary>
    /// Gets a required service from the service scope.
    /// </summary>
    public static T GetRequiredService<T>(this IServiceScope scope) where T : notnull
    {
        return scope.ServiceProvider.GetRequiredService<T>();
    }
}
