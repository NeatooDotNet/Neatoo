// -----------------------------------------------------------------------------
// Design.Infrastructure - FetchPatterns Persistence
// -----------------------------------------------------------------------------

using Design.Domain.FactoryOperations;

namespace Design.Infrastructure.InMemory;

/// <summary>
/// In-memory IFetchDemoRepository for Design.Server. The store assigns ids;
/// an unknown id (or unmatched criteria) fetches a placeholder row.
/// </summary>
internal sealed class InMemoryFetchDemoRepository : IFetchDemoRepository
{
    private int _nextId = 1;

    internal Dictionary<int, (string Name, string? Description)> Store { get; } = new();

    public (int Id, string Name, string Description) GetById(int id)
        => Store.TryGetValue(id, out var row)
            ? (id, row.Name, row.Description ?? "")
            : (id, $"Fetched-{id}", $"Description for {id}");

    public (int Id, string Name, string Description) GetByCriteria(string? name, int minValue)
    {
        foreach (var (id, row) in Store)
        {
            if (id >= minValue
                && (name == null || row.Name.Contains(name, StringComparison.OrdinalIgnoreCase)))
            {
                return (id, row.Name, row.Description ?? "");
            }
        }
        return (1, name ?? "Criteria", "Matched by criteria");
    }

    public void Insert(string name, string? description) => Store[_nextId++] = (name, description);

    public void Update(int id, string name, string? description) => Store[id] = (name, description);

    public void Delete(int id) => Store.Remove(id);
}

/// <summary>
/// In-memory IFetchParentRepository for Design.Server. Synthesizes the parent
/// row from its id.
/// </summary>
internal sealed class InMemoryFetchParentRepository : IFetchParentRepository
{
    public (int Id, string Title) GetById(int id) => (id, $"Parent-{id}");
}

/// <summary>
/// In-memory IFetchChildRepository for Design.Server. Every parent has the
/// same two child rows.
/// </summary>
internal sealed class InMemoryFetchChildRepository : IFetchChildRepository
{
    public IEnumerable<(int Id, string Name)> GetByParentId(int parentId)
        => [(1, "Child-1"), (2, "Child-2")];
}
