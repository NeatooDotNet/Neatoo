using Microsoft.Extensions.DependencyInjection;
using Neatoo.Documentation.Samples.AggregatesAndEntities;
using Neatoo.Documentation.Samples.DatabaseValidation;
using Neatoo.Documentation.Samples.FactoryOperations;
using Neatoo.Documentation.Samples.SampleDomain;
using Neatoo.Documentation.Samples.ValidationAndRules;
using Neatoo.Documentation.Samples.ValidationAndRules.RuleUsage;
using Neatoo.RemoteFactory;

namespace Neatoo.Documentation.Samples.TestInfrastructure;

/// <summary>
/// Provides DI configuration for documentation samples.
/// </summary>
public static class SampleServiceProvider
{
    private static IServiceProvider? _container;
    private static readonly object _lock = new();

    /// <summary>
    /// Creates a new service scope for sample testing.
    /// </summary>
    public static IServiceScope CreateScope()
    {
        lock (_lock)
        {
            _container ??= CreateContainer();
            return _container.CreateScope();
        }
    }

    private static ServiceProvider CreateContainer()
    {
        var services = new ServiceCollection();

        // Register Neatoo services with sample domain assembly
        services.AddNeatooServices(NeatooFactory.Server, typeof(SampleDomain.IPerson).Assembly);
        services.RegisterMatchingName(typeof(SampleDomain.IPerson).Assembly);

        // Register sample rules
        services.AddTransient<ValidationAndRules.IAgeValidationRule, AgeValidationRule>();
        services.AddTransient<IUniqueEmailRule, UniqueEmailRule>();
        services.AddTransient<IDateRangeRule, DateRangeRule>();
        services.AddTransient<IDateRangeSearchRule, DateRangeSearchRule>();
        services.AddTransient<IAsyncUniqueEmailRule, AsyncUniqueEmailRule>();

        // Register RuleUsageSamples rules
        services.AddTransient<ValidationAndRules.RuleUsage.IAgeValidationRule, AgeValidationRuleImpl>();
        services.AddTransient<IUniqueNameValidationRule, UniqueNameValidationRuleImpl>();
        services.AddTransient<IUniquePhoneTypeRule, UniquePhoneTypeRule>();
        services.AddTransient<IFullNameRule, FullNameRule>();
        services.AddTransient<IEmailCheckRule, EmailCheckRule>();

        // Register mock services
        services.AddScoped<IEmailService, MockEmailService>();
        services.AddScoped<IProductRepository, MockProductRepository>();
        services.AddScoped<IInventoryDb, MockInventoryDb>();
        services.AddScoped<IUserRepository, MockUserRepository>();

        return services.BuildServiceProvider();
    }
}
