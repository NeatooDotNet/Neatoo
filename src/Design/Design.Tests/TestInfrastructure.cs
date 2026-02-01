// -----------------------------------------------------------------------------
// Design.Tests - Test Infrastructure
// -----------------------------------------------------------------------------
// Provides shared test setup including DI container configuration for all
// Design.Tests test classes.
// -----------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Neatoo;
using Neatoo.RemoteFactory;

namespace Design.Tests;

/// <summary>
/// Provides DI container setup for Design.Tests.
/// Uses real Neatoo infrastructure - no mocking of Neatoo classes.
/// </summary>
public static class DesignTestServices
{
    private static IServiceProvider? _serviceProvider;
    private static readonly object _lock = new();

    /// <summary>
    /// Gets a service scope for test execution.
    /// The scope ensures proper service lifetime management.
    /// </summary>
    public static IServiceScope GetScope()
    {
        lock (_lock)
        {
            if (_serviceProvider == null)
            {
                var services = new ServiceCollection();

                // Add Neatoo services with Design.Domain assembly
                services.AddNeatooServices(
                    NeatooFactory.Server,
                    typeof(Design.Domain.BaseClasses.DemoValueObject).Assembly);

                // Register mock repositories for tests
                services.AddTransient<Design.Domain.BaseClasses.IDemoRepository, MockDemoRepository>();
                services.AddTransient<Design.Domain.Aggregates.OrderAggregate.IOrderRepository, MockOrderRepository>();
                services.AddTransient<Design.Domain.FactoryOperations.ICreateDemoRepository, MockCreateDemoRepository>();
                services.AddTransient<Design.Domain.FactoryOperations.ICreateDefaults, MockCreateDefaults>();
                services.AddTransient<Design.Domain.FactoryOperations.IFetchDemoRepository, MockFetchDemoRepository>();
                services.AddTransient<Design.Domain.FactoryOperations.IFetchParentRepository, MockFetchParentRepository>();
                services.AddTransient<Design.Domain.FactoryOperations.IFetchChildRepository, MockFetchChildRepository>();
                services.AddTransient<Design.Domain.FactoryOperations.ISaveDemoRepository, MockSaveDemoRepository>();
                services.AddTransient<Design.Domain.FactoryOperations.ISaveAggregateRepository, MockSaveAggregateRepository>();
                services.AddTransient<Design.Domain.PropertySystem.IPropertyDemoRepository, MockPropertyDemoRepository>();
                services.AddTransient<Design.Domain.Rules.IRulesDemoRepository, MockRulesDemoRepository>();
                services.AddTransient<Design.Domain.Rules.IFluentRulesRepository, MockFluentRulesRepository>();

                _serviceProvider = services.BuildServiceProvider();
            }
            return _serviceProvider.CreateScope();
        }
    }

    /// <summary>
    /// Extension method for convenient service resolution from scope.
    /// </summary>
    public static T GetRequiredService<T>(this IServiceScope scope) where T : notnull
    {
        return scope.ServiceProvider.GetRequiredService<T>();
    }
}

// =============================================================================
// Mock Repository Implementations
// =============================================================================

internal class MockDemoRepository : Design.Domain.BaseClasses.IDemoRepository
{
    public (string Name, int Value) GetById(int id) => ($"Entity-{id}", id * 10);
    public void Insert(string name, int value) { }
    public void Update(string name, int value) { }
    public void Delete(string name) { }
    public IEnumerable<string> GetAllNames() => new[] { "Item1", "Item2", "Item3" };
}

internal class MockOrderRepository : Design.Domain.Aggregates.OrderAggregate.IOrderRepository
{
    private int _nextOrderId = 100;
    private int _nextItemId = 1000;

    public (int Id, string OrderNumber, string CustomerName, DateTime OrderDate, string Status, decimal TotalAmount) GetById(int id)
        => (id, $"ORD-{id}", "Test Customer", DateTime.Today, "Draft", 100.00m);

    public IEnumerable<(int Id, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal)> GetItems(int orderId)
        => new[] { (1, "Widget", 2, 10.00m, 20.00m), (2, "Gadget", 1, 50.00m, 50.00m) };

    public int InsertOrder(string orderNumber, string customerName, DateTime orderDate, string status, decimal totalAmount)
        => _nextOrderId++;

    public void UpdateOrder(int id, string orderNumber, string customerName, DateTime orderDate, string status, decimal totalAmount) { }
    public void DeleteOrder(int id) { }

    public int InsertItem(int orderId, string productName, int quantity, decimal unitPrice, decimal lineTotal)
        => _nextItemId++;

    public void UpdateItem(int id, string productName, int quantity, decimal unitPrice, decimal lineTotal) { }
    public void DeleteItem(int id) { }
}

internal class MockCreateDemoRepository : Design.Domain.FactoryOperations.ICreateDemoRepository
{
    public (string Name, int Priority) GetById(int id) => ($"Demo-{id}", id);
    public void Insert(string name, int priority) { }
    public void Update(string name, int priority) { }
    public void Delete(string name) { }
}

internal class MockCreateDefaults : Design.Domain.FactoryOperations.ICreateDefaults
{
    public string DefaultName => "Default Name";
    public int DefaultPriority => 5;
}

internal class MockFetchDemoRepository : Design.Domain.FactoryOperations.IFetchDemoRepository
{
    public (int Id, string Name, string Description) GetById(int id)
        => (id, $"Fetched-{id}", $"Description for {id}");

    public (int Id, string Name, string Description) GetByCriteria(string? name, int minValue)
        => (1, name ?? "Criteria", "Matched by criteria");

    public void Insert(string name, string? description) { }
    public void Update(int id, string name, string? description) { }
    public void Delete(int id) { }
}

internal class MockFetchParentRepository : Design.Domain.FactoryOperations.IFetchParentRepository
{
    public (int Id, string Title) GetById(int id) => (id, $"Parent-{id}");
}

internal class MockFetchChildRepository : Design.Domain.FactoryOperations.IFetchChildRepository
{
    public IEnumerable<(int Id, string Name)> GetByParentId(int parentId)
        => new[] { (1, "Child-1"), (2, "Child-2") };
}

internal class MockSaveDemoRepository : Design.Domain.FactoryOperations.ISaveDemoRepository
{
    private int _nextId = 1;

    public (int Id, string Name, decimal Amount) GetById(int id)
        => (id, $"SaveDemo-{id}", id * 100m);

    public int Insert(string name, decimal amount) => _nextId++;
    public void Update(int id, string name, decimal amount) { }
    public void Delete(int id) { }
}

internal class MockSaveAggregateRepository : Design.Domain.FactoryOperations.ISaveAggregateRepository
{
    private int _nextParentId = 1;
    private int _nextChildId = 100;

    public (int Id, string Title) GetParentById(int id) => (id, $"Aggregate-{id}");

    public IEnumerable<(int Id, string Name, int Quantity)> GetChildrenByParentId(int parentId)
        => new[] { (101, "Item-1", 5), (102, "Item-2", 10) };

    public int InsertParent(string title) => _nextParentId++;
    public void UpdateParent(int id, string title) { }
    public void DeleteParent(int id) { }

    public int InsertChild(int parentId, string name, int quantity) => _nextChildId++;
    public void UpdateChild(int id, string name, int quantity) { }
    public void DeleteChild(int id) { }
}

internal class MockPropertyDemoRepository : Design.Domain.PropertySystem.IPropertyDemoRepository
{
    public (string Name, int Value) GetById(int id) => ($"Property-{id}", id * 2);
}

internal class MockRulesDemoRepository : Design.Domain.Rules.IRulesDemoRepository
{
    public (string Name, int Quantity, decimal Price, decimal Total) GetById(int id)
        => ($"Rule-{id}", 10, 5.00m, 50.00m);
}

internal class MockFluentRulesRepository : Design.Domain.Rules.IFluentRulesRepository
{
    public (string Name, string Email, int Quantity, decimal UnitPrice, decimal Total) GetById(int id)
        => ($"Fluent-{id}", $"test{id}@example.com", 5, 20.00m, 100.00m);
}
