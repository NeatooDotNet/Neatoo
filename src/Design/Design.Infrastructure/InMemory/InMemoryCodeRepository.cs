// -----------------------------------------------------------------------------
// Design.Infrastructure - SharedRules Persistence
// -----------------------------------------------------------------------------

using Design.Domain.Rules;

namespace Design.Infrastructure.InMemory;

/// <summary>
/// In-memory ICodeRepository for Design.Server. One code ("TAKEN") is already
/// in use so the uniqueness rule has something to reject.
/// </summary>
internal sealed class InMemoryCodeRepository : ICodeRepository
{
    /// <summary>Codes in use, with the id of the record that holds each.</summary>
    internal Dictionary<string, Guid> Codes { get; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["TAKEN"] = Guid.NewGuid(),
    };

    public bool CodeExists(Guid excludeId, string code)
        => Codes.TryGetValue(code, out var ownerId) && ownerId != excludeId;
}
