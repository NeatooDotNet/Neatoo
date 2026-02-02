using Microsoft.Extensions.DependencyInjection;
using Neatoo;
using Neatoo.RemoteFactory;
using Neatoo.Skills.Domain;

namespace Neatoo.Skills.Tests;

// =============================================================================
// TEST BASE CLASS - Sets up DI container for integration tests
// =============================================================================

#region test-base-class
/// <summary>
/// Base class for Neatoo skill tests.
/// Configures DI container with real Neatoo services and mock external dependencies.
/// </summary>
public abstract class SkillTestBase : IDisposable
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
            _scope ??= CreateScope();
            return _scope;
        }
    }

    /// <summary>
    /// Gets the service provider from the current scope.
    /// </summary>
    protected IServiceProvider ServiceProvider => Scope.ServiceProvider;

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

        // Register Neatoo services with NeatooFactory.Logical
        // (all operations run locally, no remote calls)
        services.AddNeatooServices(NeatooFactory.Logical, typeof(SkillTestBase).Assembly);
        services.AddNeatooServices(NeatooFactory.Logical, typeof(SkillEmployee).Assembly);

        // Register mock services for external dependencies
        RegisterMockServices(services);

        return services.BuildServiceProvider();
    }

    private static void RegisterMockServices(IServiceCollection services)
    {
        // Repository mocks
        services.AddScoped<ISkillEmployeeRepository, MockEmployeeRepository>();
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

        // Service mocks
        services.AddScoped<ISkillEmailService, MockEmailService>();
        services.AddScoped<ISkillUserValidationService, MockUserValidationService>();
        services.AddScoped<ISkillAccountValidationService, MockAccountValidationService>();
        services.AddScoped<ISkillEmailValidationService, MockEmailValidationService>();
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
#endregion

#region test-project-setup
// Test project setup requirements:
//
// 1. Add project references:
//    - Reference to domain project (where Neatoo entities are)
//    - Reference to Neatoo NuGet package
//
// 2. Register services:
//    services.AddNeatooServices(NeatooFactory.Logical, typeof(MyEntity).Assembly);
//
// 3. Mock EXTERNAL dependencies, not Neatoo classes:
//    services.AddScoped<IMyRepository, MockMyRepository>();
//
// 4. Use real Neatoo factories:
//    var factory = GetRequiredService<IEmployeeFactory>();
//    var employee = factory.Create();
//
// NeatooFactory.Logical means all factory operations run locally
// (no HTTP calls to remote server)
#endregion

// =============================================================================
// MOCK IMPLEMENTATIONS - Mock external dependencies only
// =============================================================================

// Repository mocks
public class MockEmployeeRepository : ISkillEmployeeRepository
{
    public Task<(int Id, string Name, string Email, decimal Salary)> FetchByIdAsync(int id)
        => Task.FromResult((id, $"Employee {id}", $"emp{id}@company.com", 50000m));

    public Task InsertAsync(SkillEmployee employee) => Task.CompletedTask;
    public Task UpdateAsync(SkillEmployee employee) => Task.CompletedTask;
    public Task DeleteAsync(SkillEmployee employee) => Task.CompletedTask;
}

public class MockCustomerRepository : ISkillCustomerRepository
{
    public Task<CustomerData?> FetchByIdAsync(int id)
        => Task.FromResult<CustomerData?>(new CustomerData(id, $"Customer {id}", $"cust{id}@example.com"));

    public Task InsertAsync(int id, string name, string email) => Task.CompletedTask;
    public Task UpdateAsync(int id, string name, string email) => Task.CompletedTask;
    public Task DeleteAsync(int id) => Task.CompletedTask;
}

public class MockProductRepository : ISkillProductRepository
{
    public Task<ProductData?> FetchByIdAsync(int id)
        => Task.FromResult<ProductData?>(new ProductData(id, $"Product {id}", 99.99m));
}

public class MockOrderRepository : ISkillOrderRepository
{
    public Task<OrderData?> FetchByCustomerEmailAsync(string email)
        => Task.FromResult<OrderData?>(new OrderData(1, "ORD-00001", DateTime.Today));

    public Task InsertOrderAsync(int id, int quantity, decimal unitPrice) => Task.CompletedTask;
}

public class MockAccountRepository : ISkillAccountRepository
{
    public Task InsertAsync(int id, string name, decimal balance) => Task.CompletedTask;
    public Task UpdateAsync(int id, string name, decimal balance) => Task.CompletedTask;
    public Task DeleteAsync(int id) => Task.CompletedTask;
}

public class MockProjectRepository : ISkillProjectRepository
{
    public Task DeleteAsync(int id) => Task.CompletedTask;
}

public class MockReportRepository : ISkillReportRepository
{
    public Task<ReportMetadata> FetchMetadataAsync(int id)
        => Task.FromResult(new ReportMetadata(id, $"Report {id}"));
}

public class MockReportGenerator : ISkillReportGenerator
{
    public Task<byte[]> GenerateAsync(int id)
        => Task.FromResult(new byte[] { 0x00, 0x01, 0x02 });
}

public class MockDataRepository : ISkillDataRepository
{
    public Task<DataRecord> FetchAsync(int id)
        => Task.FromResult(new DataRecord(id, $"Data {id}"));

    public Task InsertAsync(int id, string data) => Task.CompletedTask;
}

public class MockOrderWithItemsRepository : ISkillOrderWithItemsRepository
{
    public Task<OrderData> FetchAsync(int id)
        => Task.FromResult(new OrderData(id, $"ORD-{id:D5}", DateTime.Today));

    public Task<List<OrderItemData>> FetchItemsAsync(int orderId)
        => Task.FromResult(new List<OrderItemData>
        {
            new(1, "WIDGET-001", 29.99m, 2),
            new(2, "GADGET-002", 49.99m, 1)
        });
}

public class MockEntityRepository : ISkillEntityRepository
{
    public Task InsertAsync(SkillEntityEmployee employee) => Task.CompletedTask;
    public Task UpdateAsync(SkillEntityEmployee employee) => Task.CompletedTask;
    public Task DeleteAsync(SkillEntityEmployee employee) => Task.CompletedTask;
}

public class MockGenRepository : ISkillGenRepository
{
    public Task<EntityData> FetchAsync(int id)
        => Task.FromResult(new EntityData(id, $"Entity {id}"));

    public Task InsertAsync(int id, string data) => Task.CompletedTask;
    public Task UpdateAsync(int id, string data) => Task.CompletedTask;
    public Task DeleteAsync(int id) => Task.CompletedTask;
}

// Service mocks
public class MockEmailService : ISkillEmailService
{
    public Task SendAsync(string to, string subject, string body) => Task.CompletedTask;
}

public class MockUserValidationService : ISkillUserValidationService
{
    public Task<bool> IsEmailUniqueAsync(string email)
        => Task.FromResult(!email.Contains("taken"));
}

public class MockAccountValidationService : ISkillAccountValidationService
{
    public Task<bool> IsEmailValidAsync(string email)
        => Task.FromResult(email.Contains("@"));
}

public class MockEmailValidationService : ISkillEmailValidationService
{
    public Task<bool> IsCompanyEmailAsync(string email)
        => Task.FromResult(email.EndsWith("@company.com", StringComparison.OrdinalIgnoreCase));
}

public class MockOrderAccessService : ISkillOrderAccessService
{
    public bool IsOrderOwner(int orderId, string userId) => true;
}

public class MockProjectMembershipService : ISkillProjectMembershipService
{
    public Task<bool> IsMemberAsync(int projectId, string userId)
        => Task.FromResult(true);

    public Task<bool> HasWriteAccessAsync(string userId)
        => Task.FromResult(true);
}

public class MockFeatureFlagService : ISkillFeatureFlagService
{
    public bool IsEnabled(string featureName) => true;
}

public class MockRemoteFactoryRepository : ISkillRemoteFactoryRepository
{
    private int _nextId = 100;

    public Task<(int Id, string Name, string Department)> FetchAsync(int id)
        => Task.FromResult((id, $"Entity {id}", $"Dept {id}"));

    public Task<int> InsertAsync(string name, string department)
        => Task.FromResult(_nextId++);

    public Task UpdateAsync(int id, string name, string department)
        => Task.CompletedTask;

    public Task DeleteAsync(int id)
        => Task.CompletedTask;
}
