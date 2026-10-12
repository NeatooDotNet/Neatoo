// -----------------------------------------------------------------------------
// Design.Infrastructure - CommonGotchas Services
// -----------------------------------------------------------------------------

using Design.Domain;

namespace Design.Infrastructure.InMemory;

/// <summary>
/// In-memory IGotcha2Repository for Design.Server. The interface carries no
/// data, so each operation is a no-op.
/// </summary>
internal sealed class InMemoryGotcha2Repository : IGotcha2Repository
{
    public void Insert() { }
    public void Update() { }
    public void Delete() { }
}

/// <summary>
/// In-memory IServerOnlyService for Design.Server.
/// </summary>
internal sealed class InMemoryServerOnlyService : IServerOnlyService
{
    public string GetServerData() => "Server Data";
    public string GetDataById(int id) => $"Server Data for {id}";
}

/// <summary>
/// In-memory IGotcha5Repository for Design.Server. The interface carries no
/// data, so each operation is a no-op.
/// </summary>
internal sealed class InMemoryGotcha5Repository : IGotcha5Repository
{
    public void Insert() { }
    public void Update() { }
    public void Delete() { }
}
