// -----------------------------------------------------------------------------
// Design.Infrastructure - TwoGeneratorInteraction Persistence
// -----------------------------------------------------------------------------

using Design.Domain.Generators;

namespace Design.Infrastructure.InMemory;

/// <summary>
/// In-memory IGeneratorDemoRepository for Design.Server. Synthesizes the row
/// from its id.
/// </summary>
internal sealed class InMemoryGeneratorDemoRepository : IGeneratorDemoRepository
{
    public (string Name, int Value) GetById(int id) => ($"Generator-{id}", id);
}
