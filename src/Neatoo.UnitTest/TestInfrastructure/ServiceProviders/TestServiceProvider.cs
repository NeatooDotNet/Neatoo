using Microsoft.Extensions.DependencyInjection;
using Neatoo.UnitTest.Objects;
using Neatoo.RemoteFactory;
using Neatoo.UnitTest.Integration.Concepts.ValidateBase;
using Neatoo.UnitTest.Integration.Concepts.EntityBase;
using Neatoo.UnitTest.Integration.Aggregates.Person;

namespace Neatoo.UnitTest.TestInfrastructure.ServiceProviders;

/// <summary>
/// Provides a centralized service provider for integration tests.
/// Use this class to obtain properly configured DI scopes for testing.
/// </summary>
public static class TestServiceProvider
{
    private static IServiceProvider? _container;
    private static IServiceProvider? _localPortalContainer;
    private static readonly object _lock = new();

    /// <summary>
    /// Gets a new service scope for testing.
    /// </summary>
    /// <param name="useLocalPortal">If true, uses a local portal configuration; otherwise uses server configuration.</param>
    /// <returns>A new IServiceScope that should be disposed after use.</returns>
    public static IServiceScope CreateScope(bool useLocalPortal = false)
    {
        lock (_lock)
        {
            if (_container == null)
            {
                _container = CreateContainer(NeatooFactory.Server);
                _localPortalContainer = CreateContainer(NeatooFactory.Logical);
            }

            return useLocalPortal
                ? _localPortalContainer!.CreateScope()
                : _container.CreateScope();
        }
    }

    private static IServiceProvider CreateContainer(NeatooFactory factoryType)
    {
        var services = new ServiceCollection();

        services.AddNeatooServices(factoryType, typeof(IEntityPerson).Assembly);
        services.RegisterMatchingName(typeof(IEntityPerson).Assembly);

        // Register shared rules
        services.AddTransient(typeof(ISharedShortNameRule<>), typeof(SharedShortNameRule<>));
        services.AddTransient<Func<IDisposableDependency>>(cc => () => cc.GetRequiredService<IDisposableDependency>());

        // Register test dependencies
        services.AddTransient<IDisposableDependency, DisposableDependency>();
        services.AddScoped<DisposableDependencyList>();

        // Register test data
        services.AddSingleton<IReadOnlyList<PersonDto>>(cc => PersonDto.Data());

        return services.BuildServiceProvider();
    }
}

