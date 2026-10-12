// -----------------------------------------------------------------------------
// Design.Infrastructure - In-Memory Unit of Work
// -----------------------------------------------------------------------------
// Shared shape for the aggregate repositories: Get hands back the stored root
// row itself (child rows attached), Add/Remove are pending until SaveChanges
// flushes them. Removing a root row removes its child rows with it.
// -----------------------------------------------------------------------------

namespace Design.Infrastructure.InMemory;

/// <summary>
/// In-memory unit of work over root rows keyed by id, for Design.Server.
/// </summary>
internal abstract class InMemoryUnitOfWork<TRow> where TRow : class
{
    private readonly List<TRow> _pendingAdds = new();
    private readonly List<TRow> _pendingRemoves = new();

    /// <summary>Flushed root rows, keyed by id.</summary>
    internal Dictionary<Guid, TRow> Store { get; } = new();

    protected abstract Guid KeyOf(TRow row);

    public TRow? Get(Guid id) => Store.GetValueOrDefault(id);

    public void Add(TRow row) => _pendingAdds.Add(row);

    public void Remove(TRow row) => _pendingRemoves.Add(row);

    public void SaveChanges()
    {
        foreach (var row in _pendingAdds) { Store[KeyOf(row)] = row; }
        foreach (var row in _pendingRemoves) { Store.Remove(KeyOf(row)); }
        _pendingAdds.Clear();
        _pendingRemoves.Clear();
    }
}
