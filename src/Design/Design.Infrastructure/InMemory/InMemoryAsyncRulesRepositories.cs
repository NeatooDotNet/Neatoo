// -----------------------------------------------------------------------------
// Design.Infrastructure - AsyncRules Services
// -----------------------------------------------------------------------------

using Design.Domain.Rules;

namespace Design.Infrastructure.InMemory;

/// <summary>
/// In-memory IAsyncRulesRepository for Design.Server. Synthesizes the row
/// from its id.
/// </summary>
internal sealed class InMemoryAsyncRulesRepository : IAsyncRulesRepository
{
    public (string Email, string Username) GetById(int id) => ($"user{id}@example.com", $"user{id}");
}

/// <summary>
/// In-memory IUsernameRepository for Design.Server. One username ("taken")
/// is already in use so the uniqueness rule has something to reject.
/// </summary>
internal sealed class InMemoryUsernameRepository : IUsernameRepository
{
    internal HashSet<string> Usernames { get; } = new(StringComparer.OrdinalIgnoreCase) { "taken" };

    public bool UsernameExists(string username) => Usernames.Contains(username);
}
