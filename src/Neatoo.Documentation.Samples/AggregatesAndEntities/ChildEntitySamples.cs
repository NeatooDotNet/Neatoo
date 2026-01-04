/// <summary>
/// Code samples for docs/aggregates-and-entities.md - Child Entity section
///
/// Full snippets (for complete examples):
/// - docs:aggregates-and-entities:child-entity
/// - docs:aggregates-and-entities:aggregate-root-pattern
/// - docs:aggregates-and-entities:child-entity-pattern
///
/// Micro-snippets (for focused inline examples):
/// - docs:aggregates-and-entities:parent-access-property
/// - docs:aggregates-and-entities:remote-fetch
/// - docs:aggregates-and-entities:remote-insert
/// - docs:aggregates-and-entities:child-fetch-no-remote
///
/// Corresponding tests: ChildEntitySamplesTests.cs
/// </summary>

using Neatoo.RemoteFactory;

namespace Neatoo.Documentation.Samples.AggregatesAndEntities;

#region docs:aggregates-and-entities:child-entity
/// <summary>
/// Child entity that belongs to a parent aggregate.
/// </summary>
public partial interface IPhoneNumber : IEntityBase
{
    Guid? Id { get; set; }
    PhoneType? PhoneType { get; set; }
    string? Number { get; set; }

    // Access to parent through the Parent property
    internal IContact? ParentContact { get; }
}

public enum PhoneType
{
    Home,
    Work,
    Mobile
}

[Factory]
internal partial class PhoneNumber : EntityBase<PhoneNumber>, IPhoneNumber
{
    public PhoneNumber(IEntityBaseServices<PhoneNumber> services) : base(services) { }

    public partial Guid? Id { get; set; }
    public partial PhoneType? PhoneType { get; set; }
    public partial string? Number { get; set; }

    #region docs:aggregates-and-entities:parent-access-property
    // Access parent through the Parent property
    public IContact? ParentContact => Parent as IContact;
    #endregion

    [Create]
    public void Create()
    {
        Id = Guid.NewGuid();
    }
}
#endregion

#region docs:aggregates-and-entities:aggregate-root-pattern
/// <summary>
/// Aggregate root with [Remote] operations - called from UI.
/// </summary>
public partial interface ISalesOrder : IEntityBase
{
    Guid? Id { get; set; }
    string? CustomerName { get; set; }
    DateTime OrderDate { get; set; }
    IOrderLineItemList? LineItems { get; set; }
}

[Factory]
internal partial class SalesOrder : EntityBase<SalesOrder>, ISalesOrder
{
    public SalesOrder(IEntityBaseServices<SalesOrder> services) : base(services) { }

    public partial Guid? Id { get; set; }
    public partial string? CustomerName { get; set; }
    public partial DateTime OrderDate { get; set; }
    public partial IOrderLineItemList? LineItems { get; set; }

    [Create]
    public void Create([Service] IOrderLineItemList lineItems)
    {
        Id = Guid.NewGuid();
        OrderDate = DateTime.Today;
        LineItems = lineItems;
    }

    #region docs:aggregates-and-entities:remote-fetch
    // [Remote] - Called from UI
    [Remote]
    [Fetch]
    public void Fetch(Guid id)
    #endregion
    {
        // In real implementation:
        // var entity = await db.Orders.Include(o => o.LineItems).FirstOrDefaultAsync(o => o.Id == id);
        // MapFrom(entity);
        // LineItems = lineItemListFactory.Fetch(entity.LineItems);
        Id = id;
    }

    #region docs:aggregates-and-entities:remote-insert
    [Remote]
    [Insert]
    public async Task Insert()
    #endregion
    {
        await RunRules();
        if (!IsSavable) return;

        // In real implementation:
        // var entity = new OrderEntity();
        // MapTo(entity);
        // lineItemListFactory.Save(LineItems, entity.LineItems);
        // db.Orders.Add(entity);
        // await db.SaveChangesAsync();
    }
}
#endregion

#region docs:aggregates-and-entities:child-entity-pattern
/// <summary>
/// Child entity - no [Remote], called internally by parent.
/// </summary>
public partial interface IOrderLineItem : IEntityBase
{
    Guid? Id { get; set; }
    string? ProductName { get; set; }
    int Quantity { get; set; }
    decimal UnitPrice { get; set; }
    decimal LineTotal { get; }
}

[Factory]
internal partial class OrderLineItem : EntityBase<OrderLineItem>, IOrderLineItem
{
    public OrderLineItem(IEntityBaseServices<OrderLineItem> services) : base(services) { }

    public partial Guid? Id { get; set; }
    public partial string? ProductName { get; set; }
    public partial int Quantity { get; set; }
    public partial decimal UnitPrice { get; set; }

    public decimal LineTotal => Quantity * UnitPrice;

    [Create]
    public void Create()
    {
        Id = Guid.NewGuid();
    }

    #region docs:aggregates-and-entities:child-fetch-no-remote
    // No [Remote] - called internally by parent
    [Fetch]
    public void Fetch(OrderLineItemDto dto)
    #endregion
    {
        Id = dto.Id;
        ProductName = dto.ProductName;
        Quantity = dto.Quantity;
        UnitPrice = dto.UnitPrice;
    }

    [Insert]
    public OrderLineItemDto Insert()
    {
        return new OrderLineItemDto
        {
            Id = Id,
            ProductName = ProductName,
            Quantity = Quantity,
            UnitPrice = UnitPrice
        };
    }

    [Update]
    public void Update(OrderLineItemDto dto)
    {
        // MapModifiedTo would be used in real implementation
        dto.ProductName = ProductName;
        dto.Quantity = Quantity;
        dto.UnitPrice = UnitPrice;
    }
}

// DTO for demonstration
public class OrderLineItemDto
{
    public Guid? Id { get; set; }
    public string? ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

// List interface for child collection
public partial interface IOrderLineItemList : IEntityListBase<IOrderLineItem> { }
#endregion
