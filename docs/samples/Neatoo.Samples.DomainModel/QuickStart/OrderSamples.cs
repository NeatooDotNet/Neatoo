using Neatoo;
using Neatoo.RemoteFactory;

namespace Neatoo.Samples.DomainModel.QuickStart;

// Supporting types for quick-start examples

/// <summary>
/// EF Core entity for persistence.
/// </summary>
public class OrderEntity
{
    public int Id { get; set; }
    public string? CustomerName { get; set; }
    public decimal Total { get; set; }
    public DateTime OrderDate { get; set; }
}

/// <summary>
/// Database context interface for Order persistence.
/// </summary>
public interface IOrderDbContext
{
    IOrderSet Orders { get; }
    Task<int> SaveChangesAsync();
}

/// <summary>
/// Abstraction for Order DbSet operations.
/// </summary>
public interface IOrderSet
{
    Task<OrderEntity?> FindAsync(int? id);
    void Add(OrderEntity entity);
}

#region qs-interface-pattern
public partial interface IOrder : IEntityBase
{
    // Interface members are auto-generated from partial properties
}
#endregion

#region qs-aggregate-root
[Factory]
internal partial class Order : EntityBase<Order>, IOrder
{
    public Order(IEntityBaseServices<Order> services) : base(services) { }

    // Partial properties - Neatoo source-generates the backing implementation
    public partial int? Id { get; set; }
    public partial string? CustomerName { get; set; }
    public partial decimal Total { get; set; }
    public partial DateTime OrderDate { get; set; }

    // Mapper methods - manually implemented
    public void MapFrom(OrderEntity entity)
    {
        Id = entity.Id;
        CustomerName = entity.CustomerName;
        Total = entity.Total;
        OrderDate = entity.OrderDate;
    }

    public void MapTo(OrderEntity entity)
    {
        entity.Id = Id ?? 0;
        entity.CustomerName = CustomerName;
        entity.Total = Total;
        entity.OrderDate = OrderDate;
    }

    // MapModifiedTo - source-generated, only copies modified properties
    public partial void MapModifiedTo(OrderEntity entity);

    // Create operation - called when factory creates a new instance
    [Create]
    public void Create()
    {
        OrderDate = DateTime.UtcNow;
    }

    // Fetch operation - loads from database
    [Remote]
    [Fetch]
    public async Task Fetch(int id, [Service] IOrderDbContext db)
    {
        var entity = await db.Orders.FindAsync(id);
        if (entity != null)
        {
            MapFrom(entity);
        }
    }

    // Insert operation - persists new entity
    [Remote]
    [Insert]
    public async Task Insert([Service] IOrderDbContext db)
    {
        await RunRules();
        if (!IsSavable) return;

        var entity = new OrderEntity();
        MapTo(entity);
        db.Orders.Add(entity);
        await db.SaveChangesAsync();
    }

    // Update operation - persists changes
    [Remote]
    [Update]
    public async Task Update([Service] IOrderDbContext db)
    {
        await RunRules();
        if (!IsSavable) return;

        var entity = await db.Orders.FindAsync(Id);
        if (entity == null)
            throw new KeyNotFoundException("Order not found");

        MapModifiedTo(entity);
        await db.SaveChangesAsync();
    }
}
#endregion
