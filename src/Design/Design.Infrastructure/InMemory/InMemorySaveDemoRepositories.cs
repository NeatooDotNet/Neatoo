// -----------------------------------------------------------------------------
// Design.Infrastructure - SavePatterns Persistence
// -----------------------------------------------------------------------------

using Design.Domain.FactoryOperations;

namespace Design.Infrastructure.InMemory;

/// <summary>
/// In-memory ISaveDemoRepository for Design.Server. The store assigns ids;
/// an unknown id fetches a placeholder row named after it.
/// </summary>
internal sealed class InMemorySaveDemoRepository : ISaveDemoRepository
{
    private int _nextId = 1;

    internal Dictionary<int, (string Name, decimal Amount)> Store { get; } = new();

    public (int Id, string Name, decimal Amount) GetById(int id)
        => Store.TryGetValue(id, out var row)
            ? (id, row.Name, row.Amount)
            : (id, $"SaveDemo-{id}", id * 100m);

    public int Insert(string name, decimal amount)
    {
        var id = _nextId++;
        Store[id] = (name, amount);
        return id;
    }

    public void Update(int id, string name, decimal amount) => Store[id] = (name, amount);

    public void Delete(int id) => Store.Remove(id);
}

/// <summary>
/// In-memory ISaveAggregateRepository for Design.Server.
/// </summary>
internal sealed class InMemorySaveAggregateRepository : InMemoryUnitOfWork<SaveAggregateDemoRow>, ISaveAggregateRepository
{
    protected override Guid KeyOf(SaveAggregateDemoRow row) => row.Id;
}
