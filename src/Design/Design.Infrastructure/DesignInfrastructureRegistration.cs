// -----------------------------------------------------------------------------
// Design.Infrastructure - Service Registration
// -----------------------------------------------------------------------------
// Registers an in-memory implementation of every [Service] interface that
// Design.Domain's factory operations take, plus the server side of the domain
// service interface factory. Singletons: one store per host, so a row written
// by one request is there for the next.
//
// A single-user demo store, not thread-safe: plain collections, no locking.
// Two concurrent requests to a running Design.Server could interleave their
// pending writes. It exists so the server composition can serve every factory,
// not to stand in for persistence.
// -----------------------------------------------------------------------------

using Design.Domain;
using Design.Domain.Aggregates.OrderAggregate;
using Design.Domain.Aggregates.WorkOrderAggregate;
using Design.Domain.BaseClasses;
using Design.Domain.Commands;
using Design.Domain.Entities;
using Design.Domain.ErrorHandling;
using Design.Domain.FactoryOperations;
using Design.Domain.Generators;
using Design.Domain.PropertySystem;
using Design.Domain.ReadModels;
using Design.Domain.Rules;
using Design.Domain.Services;
using Design.Infrastructure.InMemory;
using Microsoft.Extensions.DependencyInjection;

namespace Design.Infrastructure;

/// <summary>
/// Registers the in-memory infrastructure behind Design.Domain for a server host.
/// </summary>
public static class DesignInfrastructureRegistration
{
    public static IServiceCollection AddDesignInfrastructure(this IServiceCollection services)
    {
        // Aggregates and entities
        services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();
        services.AddSingleton<IWorkOrderRepository, InMemoryWorkOrderRepository>();
        services.AddSingleton<IEmployeeRepository, InMemoryEmployeeRepository>();
        services.AddSingleton<IProductRepository, InMemoryProductRepository>();

        // Read models
        services.AddSingleton<IEmployeeDirectoryRepository, InMemoryEmployeeDirectoryRepository>();

        // Commands
        services.AddSingleton<IApproveEmployeeRepository, InMemoryApproveEmployeeRepository>();
        services.AddSingleton<IReportRepository, InMemoryReportRepository>();
        services.AddSingleton<IEmailService, InMemoryEmailService>();
        services.AddSingleton<IEmployeeQueryRepository, InMemoryEmployeeQueryRepository>();

        // Base classes
        services.AddSingleton<IDemoRepository, InMemoryDemoRepository>();

        // Factory operations
        services.AddSingleton<ICreateDefaults, InMemoryCreateDefaults>();
        services.AddSingleton<ICreateDemoRepository, InMemoryCreateDemoRepository>();
        services.AddSingleton<IFetchDemoRepository, InMemoryFetchDemoRepository>();
        services.AddSingleton<IFetchParentRepository, InMemoryFetchParentRepository>();
        services.AddSingleton<IFetchChildRepository, InMemoryFetchChildRepository>();
        services.AddSingleton<ISaveDemoRepository, InMemorySaveDemoRepository>();
        services.AddSingleton<ISaveAggregateRepository, InMemorySaveAggregateRepository>();
        services.AddSingleton<IRemoteDemoRepository, InMemoryRemoteDemoRepository>();
        services.AddSingleton<IDbContext, InMemoryDbContext>();
        services.AddSingleton<IDualUseRepository, InMemoryDualUseRepository>();

        // Property system
        services.AddSingleton<IPropertyDemoRepository, InMemoryPropertyDemoRepository>();
        services.AddSingleton<IFieldLevelAuthRepository, InMemoryFieldLevelAuthRepository>();
        services.AddSingleton<ISalaryPermission, InMemorySalaryPermission>();
        services.AddSingleton<IStatePropertiesRepository, InMemoryStatePropertiesRepository>();

        // Rules
        services.AddSingleton<IRulesDemoRepository, InMemoryRulesDemoRepository>();
        services.AddSingleton<IFluentRulesRepository, InMemoryFluentRulesRepository>();
        services.AddSingleton<IAsyncRulesRepository, InMemoryAsyncRulesRepository>();
        services.AddSingleton<IUsernameRepository, InMemoryUsernameRepository>();
        services.AddSingleton<ICodeRepository, InMemoryCodeRepository>();

        // Error handling and generators
        services.AddSingleton<IErrorDemoRepository, InMemoryErrorDemoRepository>();
        services.AddSingleton<IGeneratorDemoRepository, InMemoryGeneratorDemoRepository>();

        // Common gotchas
        services.AddSingleton<IGotcha2Repository, InMemoryGotcha2Repository>();
        services.AddSingleton<IServerOnlyService, InMemoryServerOnlyService>();
        services.AddSingleton<IGotcha5Repository, InMemoryGotcha5Repository>();

        // Domain service behind an interface factory: the server registers the
        // implementation; the client registers nothing and gets the generated proxy.
        services.AddSingleton<IShippingRateService, ShippingRateService>();

        return services;
    }
}
