// -----------------------------------------------------------------------------
// Design.Infrastructure - BaseClasses Demo Persistence
// -----------------------------------------------------------------------------

using Design.Domain.BaseClasses;

namespace Design.Infrastructure.InMemory;

/// <summary>
/// In-memory IDemoRepository for Design.Server. Rows are keyed by name (the
/// interface carries no id on insert), so GetById synthesizes a row named
/// after the id; GetAllNames lists what has been inserted.
/// </summary>
internal sealed class InMemoryDemoRepository : IDemoRepository
{
    internal Dictionary<string, int> Store { get; } = new();

    public (string Name, int Value) GetById(int id) => ($"Entity-{id}", id * 10);

    public void Insert(string name, int value) => Store[name] = value;

    public void Update(string name, int value) => Store[name] = value;

    public void Delete(string name) => Store.Remove(name);

    public IEnumerable<string> GetAllNames() => Store.Keys.ToList();
}
