// -----------------------------------------------------------------------------
// Design.Tests - Test Infrastructure
// -----------------------------------------------------------------------------
// Provides shared test setup including DI container configuration for all
// Design.Tests test classes.
// -----------------------------------------------------------------------------

using Design.Domain.DI;
using Microsoft.Extensions.DependencyInjection;
using Neatoo;
using Neatoo.RemoteFactory;
using AddressRow = Design.Domain.Entities.AddressRow;
using EmployeeRow = Design.Domain.Entities.EmployeeRow;
using OrderItemRow = Design.Domain.Aggregates.OrderAggregate.OrderItemRow;
using OrderRow = Design.Domain.Aggregates.OrderAggregate.OrderRow;
using SaveAggregateDemoRow = Design.Domain.FactoryOperations.SaveAggregateDemoRow;
using SaveDemoItemRow = Design.Domain.FactoryOperations.SaveDemoItemRow;

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
                // Scoped (not transient): aggregate lifecycle tests seed the store
                // and assert on the rows the factories wrote, so the test scope and
                // the factory operations must observe the same instance.
                services.AddScoped<Design.Domain.Aggregates.OrderAggregate.IOrderRepository, MockOrderRepository>();
                services.AddTransient<Design.Domain.FactoryOperations.ICreateDemoRepository, MockCreateDemoRepository>();
                services.AddTransient<Design.Domain.FactoryOperations.ICreateDefaults, MockCreateDefaults>();
                services.AddTransient<Design.Domain.FactoryOperations.IFetchDemoRepository, MockFetchDemoRepository>();
                services.AddTransient<Design.Domain.FactoryOperations.IFetchParentRepository, MockFetchParentRepository>();
                services.AddTransient<Design.Domain.FactoryOperations.IFetchChildRepository, MockFetchChildRepository>();
                services.AddScoped<Design.Domain.FactoryOperations.ISaveDemoRepository, MockSaveDemoRepository>();
                // Scoped in-memory store, same rationale as MockOrderRepository above
                services.AddScoped<Design.Domain.FactoryOperations.ISaveAggregateRepository, MockSaveAggregateRepository>();
                services.AddTransient<Design.Domain.PropertySystem.IPropertyDemoRepository, MockPropertyDemoRepository>();
                services.AddTransient<Design.Domain.PropertySystem.IFieldLevelAuthRepository, MockFieldLevelAuthRepository>();
                services.AddScoped<Design.Domain.PropertySystem.ISalaryPermission, MockSalaryPermission>();
                services.AddTransient<Design.Domain.Rules.IRulesDemoRepository, MockRulesDemoRepository>();
                services.AddTransient<Design.Domain.Rules.IFluentRulesRepository, MockFluentRulesRepository>();
                services.AddTransient<Design.Domain.Rules.IAsyncRulesRepository, MockAsyncRulesRepository>();
                services.AddTransient<Design.Domain.Rules.IUsernameRepository, MockUsernameRepository>();
                services.AddDesignDomainRules();

                // Entities demo aggregate (Employee/Address) — scoped in-memory store
                services.AddScoped<Design.Domain.Entities.IEmployeeRepository, MockEmployeeRepository>();
                services.AddTransient<Design.Domain.ReadModels.IEmployeeDirectoryRepository, MockEmployeeDirectoryRepository>();

                // Commands
                services.AddScoped<Design.Domain.Commands.IApproveEmployeeRepository, MockApproveEmployeeRepository>();

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

// =============================================================================
// Aggregate repositories (Order, SaveAggregateDemo, Employee) - in-memory
// units of work
// =============================================================================
// Each mirrors an EF Core unit of work over plain row classes:
// - Store holds what has been flushed, keyed by root id. Get hands back the
//   stored root row itself (child rows attached), as a tracked EF entity
//   would be - changes the aggregate makes to it are what the store holds.
// - Add/Remove are pending until SaveChanges flushes them, so a save that
//   forgets to flush leaves the store without its new row (or still holding
//   its removed one).
// - AddedRows/RemovedRows/SaveChangesCount record the calls, for assertions.
// Registered scoped: the test scope and the factory operations observe the
// same instance. Tests seed the store with Seed...() before fetching.
// =============================================================================

internal class MockOrderRepository : Design.Domain.Aggregates.OrderAggregate.IOrderRepository
{
    private readonly List<OrderRow> _pendingAdds = new();
    private readonly List<OrderRow> _pendingRemoves = new();

    /// <summary>Flushed order rows (with their item rows), keyed by order id.</summary>
    public Dictionary<Guid, OrderRow> Store { get; } = new();

    public List<OrderRow> AddedRows { get; } = new();
    public List<OrderRow> RemovedRows { get; } = new();
    public int SaveChangesCount { get; private set; }

    /// <summary>
    /// Seeds an order with the two default item rows (Widget, Gadget).
    /// </summary>
    public OrderRow SeedOrder()
        => SeedOrder(
            Item("Widget", 2, 10.00m, 20.00m),
            Item("Gadget", 1, 50.00m, 50.00m));

    /// <summary>
    /// Seeds an order with exactly the given item rows. Per-order child rows make
    /// a test's child loading order-specific: a fetch that loaded the wrong
    /// order's items would fail. (LIST-005)
    /// </summary>
    public OrderRow SeedOrder(
        params OrderItemRow[] items)
    {
        var row = new OrderRow
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-SEEDED",
            CustomerName = "Test Customer",
            OrderDate = DateTime.Today,
            Status = "Draft",
            TotalAmount = 100.00m,
        };
        row.Items.AddRange(items);
        Store[row.Id] = row;
        return row;
    }

    public static OrderItemRow Item(
        string productName, int quantity, decimal unitPrice, decimal lineTotal)
        => new()
        {
            Id = Guid.NewGuid(),
            ProductName = productName,
            Quantity = quantity,
            UnitPrice = unitPrice,
            LineTotal = lineTotal,
        };

    public OrderRow? Get(Guid id)
        => Store.GetValueOrDefault(id);

    public void Add(OrderRow row)
    {
        AddedRows.Add(row);
        _pendingAdds.Add(row);
    }

    public void Remove(OrderRow row)
    {
        RemovedRows.Add(row);
        _pendingRemoves.Add(row);
    }

    public void SaveChanges()
    {
        foreach (var row in _pendingAdds) { Store[row.Id] = row; }
        // Removing the root row removes its item rows with it (cascade)
        foreach (var row in _pendingRemoves) { Store.Remove(row.Id); }
        _pendingAdds.Clear();
        _pendingRemoves.Clear();
        SaveChangesCount++;
    }
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
    private readonly List<SaveAggregateDemoRow> _pendingAdds = new();
    private readonly List<SaveAggregateDemoRow> _pendingRemoves = new();

    /// <summary>Flushed root rows (with their child rows), keyed by root id.</summary>
    public Dictionary<Guid, SaveAggregateDemoRow> Store { get; } = new();

    public List<SaveAggregateDemoRow> AddedRows { get; } = new();
    public List<SaveAggregateDemoRow> RemovedRows { get; } = new();
    public int SaveChangesCount { get; private set; }

    /// <summary>Seeds a root with two child rows (Item-1 x5, Item-2 x10).</summary>
    public SaveAggregateDemoRow SeedAggregate()
    {
        var row = new SaveAggregateDemoRow { Id = Guid.NewGuid(), Title = "Aggregate" };
        row.Items.Add(new SaveDemoItemRow { Id = Guid.NewGuid(), Name = "Item-1", Quantity = 5 });
        row.Items.Add(new SaveDemoItemRow { Id = Guid.NewGuid(), Name = "Item-2", Quantity = 10 });
        Store[row.Id] = row;
        return row;
    }

    public SaveAggregateDemoRow? Get(Guid id) => Store.GetValueOrDefault(id);

    public void Add(SaveAggregateDemoRow row)
    {
        AddedRows.Add(row);
        _pendingAdds.Add(row);
    }

    public void Remove(SaveAggregateDemoRow row)
    {
        RemovedRows.Add(row);
        _pendingRemoves.Add(row);
    }

    public void SaveChanges()
    {
        foreach (var row in _pendingAdds) { Store[row.Id] = row; }
        foreach (var row in _pendingRemoves) { Store.Remove(row.Id); }
        _pendingAdds.Clear();
        _pendingRemoves.Clear();
        SaveChangesCount++;
    }
}

internal class MockEmployeeRepository : Design.Domain.Entities.IEmployeeRepository
{
    private readonly List<EmployeeRow> _pendingAdds = new();
    private readonly List<EmployeeRow> _pendingRemoves = new();

    /// <summary>Flushed employee rows (with their address rows), keyed by employee id.</summary>
    public Dictionary<Guid, EmployeeRow> Store { get; } = new();

    public List<EmployeeRow> AddedRows { get; } = new();
    public List<EmployeeRow> RemovedRows { get; } = new();
    public int SaveChangesCount { get; private set; }

    /// <summary>
    /// Seeds an employee with three address rows (Home, Work, Other).
    /// </summary>
    public EmployeeRow SeedEmployee()
    {
        var row = new EmployeeRow
        {
            Id = Guid.NewGuid(),
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@example.com",
            HireDate = new DateTime(2020, 1, 15),
            Salary = 120000m,
            IsActive = true,
        };
        row.Addresses.Add(Address("1 Main St", "62701", "Home"));
        row.Addresses.Add(Address("2 Work Way", "62702", "Work"));
        row.Addresses.Add(Address("3 Quiet Ln", "62703", "Other"));
        Store[row.Id] = row;
        return row;
    }

    private static AddressRow Address(string street, string zipCode, string addressType)
        => new()
        {
            Id = Guid.NewGuid(),
            Street = street,
            City = "Springfield",
            State = "IL",
            ZipCode = zipCode,
            AddressType = addressType,
        };

    public EmployeeRow? Get(Guid id) => Store.GetValueOrDefault(id);

    public void Add(EmployeeRow row)
    {
        AddedRows.Add(row);
        _pendingAdds.Add(row);
    }

    public void Remove(EmployeeRow row)
    {
        RemovedRows.Add(row);
        _pendingRemoves.Add(row);
    }

    public void SaveChanges()
    {
        foreach (var row in _pendingAdds) { Store[row.Id] = row; }
        // Removing the employee row removes its address rows with it (cascade)
        foreach (var row in _pendingRemoves) { Store.Remove(row.Id); }
        _pendingAdds.Clear();
        _pendingRemoves.Clear();
        SaveChangesCount++;
    }
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

internal class MockSalaryPermission : Design.Domain.PropertySystem.ISalaryPermission
{
    public bool CanEditSalary { get; set; }
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

internal class MockAsyncRulesRepository : Design.Domain.Rules.IAsyncRulesRepository
{
    public (string Email, string Username) GetById(int id) => ($"user{id}@example.com", $"user{id}");
}

internal class MockUsernameRepository : Design.Domain.Rules.IUsernameRepository
{
    public bool UsernameExists(string username) => username == "taken";
}

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

internal class MockEmployeeDirectoryRepository : Design.Domain.ReadModels.IEmployeeDirectoryRepository
{
    private static readonly Design.Domain.ReadModels.EmployeeSummary[] Rows =
    [
        new(1, "Ada Lovelace", "ada@example.com", "Engineering", true),
        new(2, "Grace Hopper", "grace@example.com", "Engineering", true),
        new(3, "Alan Turing", "alan@example.com", "Research", false),
    ];

    public IEnumerable<Design.Domain.ReadModels.EmployeeSummary> GetAll() => Rows;

    public IEnumerable<Design.Domain.ReadModels.EmployeeSummary> Search(string? searchTerm, string? department, bool activeOnly)
        => Rows.Where(r => (searchTerm == null || r.FullName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                        && (department == null || r.Department == department)
                        && (!activeOnly || r.IsActive));
}

internal class MockApproveEmployeeRepository : Design.Domain.Commands.IApproveEmployeeRepository
{
    public Dictionary<int, Design.Domain.Commands.ApprovalCandidate> Employees { get; } = new()
    {
        [1] = new(1, "Ada Lovelace", IsActive: true, IsApproved: false),
        [2] = new(2, "Grace Hopper", IsActive: true, IsApproved: true),
        [3] = new(3, "Alan Turing", IsActive: false, IsApproved: false),
    };

    public Design.Domain.Commands.ApprovalCandidate? GetEmployee(int id)
        => Employees.TryGetValue(id, out var e) ? e : null;

    public void ApproveEmployee(int id, string? approverName, DateTime approvedDate)
        => Employees[id] = Employees[id] with { IsApproved = true };
}
