// -----------------------------------------------------------------------------
// Design.Infrastructure - Product Entity Persistence
// -----------------------------------------------------------------------------

using Design.Domain.Entities;

namespace Design.Infrastructure.InMemory;

/// <summary>
/// In-memory IProductRepository for Design.Server.
/// </summary>
internal sealed class InMemoryProductRepository : InMemoryUnitOfWork<ProductRow>, IProductRepository
{
    protected override Guid KeyOf(ProductRow row) => row.Id;
}
