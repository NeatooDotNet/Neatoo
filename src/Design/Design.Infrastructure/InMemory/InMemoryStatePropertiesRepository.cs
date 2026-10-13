// -----------------------------------------------------------------------------
// Design.Infrastructure - StateProperties Persistence
// -----------------------------------------------------------------------------

using Design.Domain.PropertySystem;

namespace Design.Infrastructure.InMemory;

/// <summary>
/// In-memory IStatePropertiesRepository for Design.Server. Synthesizes the
/// row from its id.
/// </summary>
internal sealed class InMemoryStatePropertiesRepository : IStatePropertiesRepository
{
    public (string Name, string ChildValue) GetById(int id) => ($"State-{id}", $"Child-{id}");
}
