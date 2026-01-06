using Microsoft.Extensions.DependencyInjection;
using Neatoo.Samples.DomainModel.AggregatesAndEntities;
using Neatoo.Samples.DomainModel.DatabaseValidation;
using Neatoo.Samples.DomainModel.FactoryOperations;
using Neatoo.Samples.DomainModel.SampleDomain;
using Neatoo.Samples.DomainModel.ValidationAndRules;
using Neatoo.Samples.DomainModel.ValidationAndRules.RuleUsage;
using Neatoo.RemoteFactory;

namespace Neatoo.Samples.DomainModel.TestInfrastructure;

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
