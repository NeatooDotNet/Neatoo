// -----------------------------------------------------------------------------
// Design.Infrastructure - WorkOrder Aggregate Persistence
// -----------------------------------------------------------------------------

using Design.Domain.Aggregates.WorkOrderAggregate;

namespace Design.Infrastructure.InMemory;

/// <summary>
/// In-memory IWorkOrderRepository for Design.Server.
/// </summary>
internal sealed class InMemoryWorkOrderRepository : InMemoryUnitOfWork<WorkOrderRow>, IWorkOrderRepository
{
    protected override Guid KeyOf(WorkOrderRow row) => row.Id;
}
