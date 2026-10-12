// -----------------------------------------------------------------------------
// Design.Infrastructure - ErrorPatterns Persistence
// -----------------------------------------------------------------------------

using Design.Domain.ErrorHandling;

namespace Design.Infrastructure.InMemory;

/// <summary>
/// In-memory IErrorDemoRepository for Design.Server. Synthesizes the row
/// from its id.
/// </summary>
internal sealed class InMemoryErrorDemoRepository : IErrorDemoRepository
{
    public (string Name, int Quantity, string Email) GetById(int id)
        => ($"Error-{id}", 1, $"user{id}@example.com");
}
