// -----------------------------------------------------------------------------
// Design.Infrastructure - FieldLevelAuthorization Services
// -----------------------------------------------------------------------------

using Design.Domain.PropertySystem;

namespace Design.Infrastructure.InMemory;

/// <summary>
/// In-memory IFieldLevelAuthRepository for Design.Server. Synthesizes the
/// row from its id.
/// </summary>
internal sealed class InMemoryFieldLevelAuthRepository : IFieldLevelAuthRepository
{
    public (string Name, decimal Salary, string Department) GetById(int id)
        => ($"Employee-{id}", 75000m, "Engineering");
}

/// <summary>
/// In-memory ISalaryPermission for Design.Server. Grants the permission.
/// </summary>
internal sealed class InMemorySalaryPermission : ISalaryPermission
{
    public bool CanEditSalary => true;
}
