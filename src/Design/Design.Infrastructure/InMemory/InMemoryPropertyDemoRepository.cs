// -----------------------------------------------------------------------------
// Design.Infrastructure - PropertyBasics Persistence
// -----------------------------------------------------------------------------

using Design.Domain.PropertySystem;

namespace Design.Infrastructure.InMemory;

/// <summary>
/// In-memory IPropertyDemoRepository for Design.Server. Synthesizes the row
/// from its id.
/// </summary>
internal sealed class InMemoryPropertyDemoRepository : IPropertyDemoRepository
{
    public (string Name, int Value) GetById(int id) => ($"Property-{id}", id * 2);
}
