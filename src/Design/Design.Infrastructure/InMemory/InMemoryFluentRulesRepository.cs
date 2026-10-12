// -----------------------------------------------------------------------------
// Design.Infrastructure - FluentRules Persistence
// -----------------------------------------------------------------------------

using Design.Domain.Rules;

namespace Design.Infrastructure.InMemory;

/// <summary>
/// In-memory IFluentRulesRepository for Design.Server. Synthesizes the row
/// from its id.
/// </summary>
internal sealed class InMemoryFluentRulesRepository : IFluentRulesRepository
{
    public (string Name, string Email, int Quantity, decimal UnitPrice, decimal Total) GetById(int id)
        => ($"Fluent-{id}", $"user{id}@example.com", 5, 20.00m, 100.00m);
}
