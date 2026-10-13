// -----------------------------------------------------------------------------
// Design.Infrastructure - Employee Aggregate Persistence
// -----------------------------------------------------------------------------

using Design.Domain.Entities;

namespace Design.Infrastructure.InMemory;

/// <summary>
/// In-memory IEmployeeRepository for Design.Server.
/// </summary>
internal sealed class InMemoryEmployeeRepository : InMemoryUnitOfWork<EmployeeRow>, IEmployeeRepository
{
    protected override Guid KeyOf(EmployeeRow row) => row.Id;
}
