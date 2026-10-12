// -----------------------------------------------------------------------------
// Design.Infrastructure - RemoteBoundary Services
// -----------------------------------------------------------------------------

using Design.Domain.FactoryOperations;

namespace Design.Infrastructure.InMemory;

/// <summary>
/// In-memory IRemoteDemoRepository for Design.Server. The store assigns ids;
/// an unknown id fetches a placeholder row named after it.
/// </summary>
internal sealed class InMemoryRemoteDemoRepository : IRemoteDemoRepository
{
    private int _nextId = 1;

    internal Dictionary<int, string> Store { get; } = new();

    public (int Id, string Name) GetById(int id)
        => Store.TryGetValue(id, out var name) ? (id, name) : (id, $"Remote-{id}");

    public int Insert(string name)
    {
        var id = _nextId++;
        Store[id] = name;
        return id;
    }

    public void Update(int id, string name) => Store[id] = name;

    public void Delete(int id) => Store.Remove(id);
}

/// <summary>
/// In-memory IDbContext for Design.Server. Add is pending until SaveChanges;
/// Find matches EntityData rows by Id and returns null for anything else.
/// </summary>
internal sealed class InMemoryDbContext : IDbContext
{
    private readonly List<object> _pendingAdds = new();
    private int _nextId = 1;

    internal List<object> Store { get; } = new();

    public T? Find<T>(object key) where T : class
        => Store.OfType<T>().FirstOrDefault(e => Equals(KeyOf(e), key));

    public void Add<T>(T entity) where T : class => _pendingAdds.Add(entity);

    public void SaveChanges()
    {
        foreach (var entity in _pendingAdds)
        {
            if (entity is EntityData { Id: 0 } data)
            {
                data.Id = _nextId++;
            }
            Store.Add(entity);
        }
        _pendingAdds.Clear();
    }

    private static object? KeyOf(object entity) => entity is EntityData data ? data.Id : null;
}

/// <summary>
/// In-memory IDualUseRepository for Design.Server. The store assigns ids; an
/// unknown id fetches a placeholder address.
/// </summary>
internal sealed class InMemoryDualUseRepository : IDualUseRepository
{
    private int _nextId = 1;

    internal Dictionary<int, (string Street, string City)> Store { get; } = new();

    public (int Id, string Street, string City) GetAddressById(int id)
        => Store.TryGetValue(id, out var a) ? (id, a.Street, a.City) : (id, $"{id} Main St", "Springfield");

    public int InsertAddress(string street, string city)
    {
        var id = _nextId++;
        Store[id] = (street, city);
        return id;
    }

    public void UpdateAddress(int id, string street, string city) => Store[id] = (street, city);

    public void DeleteAddress(int id) => Store.Remove(id);
}
