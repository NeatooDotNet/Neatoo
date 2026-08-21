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
                    typeof(Design.Domain.BaseClasses.IDemoValueObject).Assembly);

                // Register mock repositories for tests
                services.AddTransient<Design.Domain.BaseClasses.IDemoRepository, MockDemoRepository>();
                // Scoped (not transient): aggregate lifecycle tests assert on the
                // repository calls the factories made, so the test scope and the
                // factory operations must observe the same instance.
                services.AddScoped<Design.Domain.Aggregates.OrderAggregate.IOrderRepository, MockOrderRepository>();
                services.AddTransient<Design.Domain.FactoryOperations.ICreateDemoRepository, MockCreateDemoRepository>();
                services.AddTransient<Design.Domain.FactoryOperations.ICreateDefaults, MockCreateDefaults>();
                services.AddTransient<Design.Domain.FactoryOperations.IFetchDemoRepository, MockFetchDemoRepository>();
                services.AddTransient<Design.Domain.FactoryOperations.IFetchParentRepository, MockFetchParentRepository>();
                services.AddTransient<Design.Domain.FactoryOperations.IFetchChildRepository, MockFetchChildRepository>();
                services.AddScoped<Design.Domain.FactoryOperations.ISaveDemoRepository, MockSaveDemoRepository>();
                // Scoped + recording, same rationale as MockOrderRepository above
                services.AddScoped<Design.Domain.FactoryOperations.ISaveAggregateRepository, MockSaveAggregateRepository>();
                services.AddTransient<Design.Domain.PropertySystem.IPropertyDemoRepository, MockPropertyDemoRepository>();
                services.AddTransient<Design.Domain.PropertySystem.IFieldLevelAuthRepository, MockFieldLevelAuthRepository>();
                services.AddTransient<Design.Domain.Rules.IRulesDemoRepository, MockRulesDemoRepository>();
                services.AddTransient<Design.Domain.Rules.IFluentRulesRepository, MockFluentRulesRepository>();

                // Entities demo aggregate (Employee/Address) — scoped + recording
                services.AddScoped<Design.Domain.Entities.IEmployeeRepository, MockEmployeeRepository>();

                // Gotcha demo repositories
                services.AddTransient<Design.Domain.IGotcha2Repository, MockGotcha2Repository>();
                services.AddTransient<Design.Domain.IServerOnlyService, MockServerOnlyService>();
                services.AddTransient<Design.Domain.IGotcha5Repository, MockGotcha5Repository>();

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

    // Recorded interactions — aggregate lifecycle tests assert against these.
    public List<int> InsertedOrderIds { get; } = new();
    public List<int> UpdatedOrderIds { get; } = new();
    public List<int> InsertedItemIds { get; } = new();
    public List<int> UpdatedItemIds { get; } = new();
    public List<int> DeletedItemIds { get; } = new();

    public (int Id, string OrderNumber, string CustomerName, DateTime OrderDate, string Status, decimal TotalAmount) GetById(int id)
        => (id, $"ORD-{id}", "Test Customer", DateTime.Today, "Draft", 100.00m);

    public IEnumerable<(int Id, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal)> GetItems(int orderId)
        => new[] { (1, "Widget", 2, 10.00m, 20.00m), (2, "Gadget", 1, 50.00m, 50.00m) };

    public int InsertOrder(string orderNumber, string customerName, DateTime orderDate, string status, decimal totalAmount)
    {
        var id = _nextOrderId++;
        InsertedOrderIds.Add(id);
        return id;
    }

    public void UpdateOrder(int id, string orderNumber, string customerName, DateTime orderDate, string status, decimal totalAmount)
        => UpdatedOrderIds.Add(id);

    public void DeleteOrder(int id) { }

    public int InsertItem(int orderId, string productName, int quantity, decimal unitPrice, decimal lineTotal)
    {
        var id = _nextItemId++;
        InsertedItemIds.Add(id);
        return id;
    }

    public void UpdateItem(int id, string productName, int quantity, decimal unitPrice, decimal lineTotal)
        => UpdatedItemIds.Add(id);

    public void DeleteItem(int id)
        => DeletedItemIds.Add(id);
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
    // Seeded clear of fetched ids so an inserted id can never collide with one
    private int _nextId = 500;

    // Recorded interactions — SaveTests asserts which persistence path Save() took
    public List<int> InsertedIds { get; } = new();
    public List<int> UpdatedIds { get; } = new();
    public List<int> DeletedIds { get; } = new();

    public (int Id, string Name, decimal Amount) GetById(int id)
        => (id, $"SaveDemo-{id}", id * 100m);

    public int Insert(string name, decimal amount)
    {
        var id = _nextId++;
        InsertedIds.Add(id);
        return id;
    }

    public void Update(int id, string name, decimal amount) => UpdatedIds.Add(id);
    public void Delete(int id) => DeletedIds.Add(id);
}

internal class MockSaveAggregateRepository : Design.Domain.FactoryOperations.ISaveAggregateRepository
{
    private int _nextParentId = 1;
    private int _nextChildId = 200;

    // Recorded interactions — aggregate lifecycle tests assert against these.
    public List<int> InsertedParentIds { get; } = new();
    public List<int> UpdatedParentIds { get; } = new();
    public List<int> InsertedChildIds { get; } = new();
    public List<int> UpdatedChildIds { get; } = new();
    public List<int> DeletedChildIds { get; } = new();

    public (int Id, string Title) GetParentById(int id) => (id, $"Aggregate-{id}");

    public IEnumerable<(int Id, string Name, int Quantity)> GetChildrenByParentId(int parentId)
        => new[] { (101, "Item-1", 5), (102, "Item-2", 10) };

    public int InsertParent(string title)
    {
        var id = _nextParentId++;
        InsertedParentIds.Add(id);
        return id;
    }

    public void UpdateParent(int id, string title) => UpdatedParentIds.Add(id);
    public void DeleteParent(int id) { }

    public int InsertChild(int parentId, string name, int quantity)
    {
        var id = _nextChildId++;
        InsertedChildIds.Add(id);
        return id;
    }

    public void UpdateChild(int id, string name, int quantity) => UpdatedChildIds.Add(id);
    public void DeleteChild(int id) => DeletedChildIds.Add(id);
}

internal class MockEmployeeRepository : Design.Domain.Entities.IEmployeeRepository
{
    // Seeded well clear of the fetched ids (201/202/203) so an inserted id can
    // never collide with a fetched one — exact-match routing assertions in the
    // lifecycle tests would otherwise be able to pass by coincidence.
    private int _nextEmployeeId = 100;
    private int _nextAddressId = 300;

    // Recorded interactions — aggregate lifecycle tests assert against these.
    public List<int> InsertedEmployeeIds { get; } = new();
    public List<int> UpdatedEmployeeIds { get; } = new();
    public List<int> InsertedAddressIds { get; } = new();
    public List<int> UpdatedAddressIds { get; } = new();
    public List<int> DeletedAddressIds { get; } = new();

    /// <summary>
    /// Parent id each InsertAddress call received, in call order. Pins FK
    /// propagation: the root must write its own generated Id before delegating
    /// child persistence, or children are written with employeeId = 0.
    /// </summary>
    public List<int> InsertAddressParentIds { get; } = new();

    public (int Id, string FirstName, string LastName, string Email, DateTime? HireDate, decimal Salary, bool IsActive) GetById(int id)
        => (id, "Ada", "Lovelace", "ada@example.com", new DateTime(2020, 1, 15), 120000m, true);

    public IEnumerable<(int Id, string Street, string City, string State, string ZipCode, string AddressType)> GetAddresses(int employeeId)
        => new[]
        {
            (201, "1 Main St", "Springfield", "IL", "62701", "Home"),
            (202, "2 Work Way", "Springfield", "IL", "62702", "Work"),
            (203, "3 Quiet Ln", "Springfield", "IL", "62703", "Other"),
        };

    public int InsertEmployee(string firstName, string lastName, string email, DateTime? hireDate, decimal salary, bool isActive)
    {
        var id = _nextEmployeeId++;
        InsertedEmployeeIds.Add(id);
        return id;
    }

    public void UpdateEmployee(int id, string firstName, string lastName, string email, DateTime? hireDate, decimal salary, bool isActive)
        => UpdatedEmployeeIds.Add(id);

    public void DeleteEmployee(int id) { }

    public int InsertAddress(int employeeId, string street, string city, string state, string zipCode, string addressType)
    {
        var id = _nextAddressId++;
        InsertedAddressIds.Add(id);
        InsertAddressParentIds.Add(employeeId);
        return id;
    }

    public void UpdateAddress(int id, string street, string city, string state, string zipCode, string addressType)
        => UpdatedAddressIds.Add(id);

    public void DeleteAddress(int id) => DeletedAddressIds.Add(id);
}

internal class MockPropertyDemoRepository : Design.Domain.PropertySystem.IPropertyDemoRepository
{
    public (string Name, int Value) GetById(int id) => ($"Property-{id}", id * 2);
}

internal class MockFieldLevelAuthRepository : Design.Domain.PropertySystem.IFieldLevelAuthRepository
{
    public (string Name, decimal Salary, string Department) GetById(int id)
        => ($"Employee-{id}", 75000m, "Engineering");
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

// =============================================================================
// Mock Repositories for Gotcha Tests
// =============================================================================

internal class MockGotcha2Repository : Design.Domain.IGotcha2Repository
{
    public void Insert() { }
    public void Update() { }
    public void Delete() { }
}

internal class MockServerOnlyService : Design.Domain.IServerOnlyService
{
    public string GetServerData() => "Server Data";
    public string GetDataById(int id) => $"Server Data for {id}";
}

internal class MockGotcha5Repository : Design.Domain.IGotcha5Repository
{
    public void Insert() { }
    public void Update() { }
    public void Delete() { }
}
