// -----------------------------------------------------------------------------
// Design.Infrastructure - CreatePatterns Services
// -----------------------------------------------------------------------------

using Design.Domain.FactoryOperations;

namespace Design.Infrastructure.InMemory;

/// <summary>
/// In-memory ICreateDefaults for Design.Server.
/// </summary>
internal sealed class InMemoryCreateDefaults : ICreateDefaults
{
    public string DefaultName => "Default Name";
    public int DefaultPriority => 5;
}

/// <summary>
/// In-memory ICreateDemoRepository for Design.Server. Rows are keyed by name
/// (the interface carries no id on insert), so GetById synthesizes a row
/// named after the id.
/// </summary>
internal sealed class InMemoryCreateDemoRepository : ICreateDemoRepository
{
    internal Dictionary<string, int> Store { get; } = new();

    public (string Name, int Priority) GetById(int id) => ($"Demo-{id}", id);

    public void Insert(string name, int priority) => Store[name] = priority;

    public void Update(string name, int priority) => Store[name] = priority;

    public void Delete(string name) => Store.Remove(name);
}
