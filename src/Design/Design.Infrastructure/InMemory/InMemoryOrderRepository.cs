// -----------------------------------------------------------------------------
// Design.Infrastructure - Order Aggregate Persistence
// -----------------------------------------------------------------------------

using Design.Domain.Aggregates.OrderAggregate;

namespace Design.Infrastructure.InMemory;

/// <summary>
/// In-memory IOrderRepository for Design.Server.
/// </summary>
internal sealed class InMemoryOrderRepository : InMemoryUnitOfWork<OrderRow>, IOrderRepository
{
    protected override Guid KeyOf(OrderRow row) => row.Id;
}
