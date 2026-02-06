using Microsoft.Extensions.DependencyInjection;
using Neatoo;
using Neatoo.RemoteFactory;

namespace Samples;

/// <summary>
/// Base class for documentation samples demonstrating integration tests with DI.
/// Provides service provider configuration similar to what production applications use.
/// </summary>
public abstract class SamplesTestBase : IDisposable
{
    private static IServiceProvider? _container;
    private static readonly object _lock = new();
    private IServiceScope? _scope;

    /// <summary>
    /// Gets the current service scope.
    /// </summary>
    protected IServiceScope Scope
    {
        get
        {
            if (_scope is null)
            {
                _scope = CreateScope();
            }
            return _scope;
        }
    }

    /// <summary>
    /// Gets the service provider from the current scope.
    /// </summary>
    protected IServiceProvider ServiceProvider => Scope.ServiceProvider;

    /// <summary>
    /// Creates a new service scope for testing.
    /// </summary>
    private static IServiceScope CreateScope()
    {
        lock (_lock)
        {
            _container ??= CreateContainer();
            return _container.CreateScope();
        }
    }

    private static IServiceProvider CreateContainer()
    {
        var services = new ServiceCollection();

        // Register Neatoo services with NeatooFactory.Logical (all operations run locally)
        services.AddNeatooServices(NeatooFactory.Logical, typeof(SamplesTestBase).Assembly);

        // Register mock services for samples
        RegisterMockServices(services);

        return services.BuildServiceProvider();
    }

    private static void RegisterMockServices(IServiceCollection services)
    {
        // Validation samples
        services.AddScoped<IValidationUniquenessService, MockUniquenessService>();
        services.AddScoped<IInventoryService, MockInventoryService>();

        // Business rules samples
        services.AddScoped<IPricingService, MockPricingService>();

        // Getting started samples
        services.AddScoped<IEmployeeRepository, MockEmployeeRepository>();

        // Remote factory samples
        services.AddScoped<IRfCustomerRepository, MockRfCustomerRepository>();
        services.AddScoped<IRfOrderRepository, MockRfOrderRepository>();

        // API reference samples
        services.AddScoped<IApiCustomerRepository, MockApiCustomerRepository>();

        // Entities samples
        services.AddScoped<IEntitiesCustomerRepository, MockEntitiesCustomerRepository>();

        // Async samples
        services.AddScoped<IEmailValidationService, MockEmailValidationService>();
        services.AddScoped<IContactRepository, MockContactRepository>();

        // Custom rule classes (injected via DI)
        services.AddTransient<SalaryRangeRule>(sp => new SalaryRangeRule(30000m, 200000m));
        services.AddTransient<ProductAvailabilityRule>(sp =>
            new ProductAvailabilityRule(sp.GetRequiredService<IInventoryService>()));
        services.AddTransient<UniqueEmailRule>(sp =>
            new UniqueEmailRule(sp.GetRequiredService<IEmailValidationService>()));
        services.AddTransient<CustomBusinessRule>();

        // Rule instances for ordered rule tests
        services.AddTransient<FirstExecutionRule>();
        services.AddTransient<SecondExecutionRule>();
        services.AddTransient<ThirdExecutionRule>();
        services.AddTransient<ConditionalValidationRule>();
        services.AddTransient<EarlyExitRule>(sp =>
            new EarlyExitRule(sp.GetRequiredService<IInventoryService>()));
        services.AddTransient<CancellableValidationRule>(sp =>
            new CancellableValidationRule(sp.GetRequiredService<IPricingService>()));
        services.AddTransient<ComputedTotalRule>();
        services.AddTransient<SingleTriggerRule>();
        services.AddTransient<MultipleTriggerRule>();
        services.AddTransient<DynamicTriggerRule>();
        services.AddTransient<PassingValidationRule>();
        services.AddTransient<SingleMessageRule>();
        services.AddTransient<MultipleMessagesRule>();
        services.AddTransient<AggregateValidationRule>();
        services.AddTransient<DateRangeValidationRule>();

        // Skill sample repository mocks
        services.AddScoped<ISkillEmployeeRepository, SkillMockEmployeeRepository>();
        services.AddScoped<ISkillCustomerRepository, MockCustomerRepository>();
        services.AddScoped<ISkillProductRepository, MockProductRepository>();
        services.AddScoped<ISkillOrderRepository, MockOrderRepository>();
        services.AddScoped<ISkillAccountRepository, MockAccountRepository>();
        services.AddScoped<ISkillProjectRepository, MockProjectRepository>();
        services.AddScoped<ISkillReportRepository, MockReportRepository>();
        services.AddScoped<ISkillReportGenerator, MockReportGenerator>();
        services.AddScoped<ISkillDataRepository, MockDataRepository>();
        services.AddScoped<ISkillOrderWithItemsRepository, MockOrderWithItemsRepository>();
        services.AddScoped<ISkillEntityRepository, MockEntityRepository>();
        services.AddScoped<ISkillGenRepository, MockGenRepository>();
        services.AddScoped<ISkillRemoteFactoryRepository, MockRemoteFactoryRepository>();

        // Skill sample service mocks
        services.AddScoped<ISkillEmailService, MockEmailService>();
        services.AddScoped<ISkillUserValidationService, MockUserValidationService>();
        services.AddScoped<ISkillAccountValidationService, MockAccountValidationService>();
        services.AddScoped<ISkillEmailValidationService, SkillMockEmailValidationService>();
        services.AddScoped<ISkillOrderAccessService, MockOrderAccessService>();
        services.AddScoped<ISkillProjectMembershipService, MockProjectMembershipService>();
        services.AddScoped<ISkillFeatureFlagService, MockFeatureFlagService>();
    }

    /// <summary>
    /// Gets a required service from the current scope.
    /// </summary>
    protected T GetRequiredService<T>() where T : notnull
    {
        return Scope.ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Gets an optional service from the current scope.
    /// </summary>
    protected T? GetService<T>() where T : class
    {
        return ServiceProvider.GetService<T>();
    }

    public void Dispose()
    {
        _scope?.Dispose();
        _scope = null;
        GC.SuppressFinalize(this);
    }
}
