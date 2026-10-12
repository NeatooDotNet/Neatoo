// -----------------------------------------------------------------------------
// Design.Infrastructure - RuleBasics Persistence
// -----------------------------------------------------------------------------

using Design.Domain.Rules;

namespace Design.Infrastructure.InMemory;

/// <summary>
/// In-memory IRulesDemoRepository for Design.Server. Synthesizes the row
/// from its id.
/// </summary>
internal sealed class InMemoryRulesDemoRepository : IRulesDemoRepository
{
    public (string Name, int Quantity, decimal Price, decimal Total) GetById(int id)
        => ($"Rule-{id}", 10, 5.00m, 50.00m);
}
