/// <summary>
/// Code samples for docs/best-practices.md
///
/// Snippets:
/// - docs:best-practices:interface-first-pattern
/// - docs:best-practices:interface-usage
/// - docs:best-practices:business-operations-on-interface
/// - docs:best-practices:nullable-id-pattern
/// - docs:best-practices:child-entity-insert
/// - docs:best-practices:parent-saves-children
/// </summary>

using Neatoo.RemoteFactory;

namespace Neatoo.Samples.DomainModel.BestPractices;

// Supporting types for samples (not extracted to docs)
public interface IDbContext
{
    IOrderSet Orders { get; }
    IOrderLineSet OrderLines { get; }
}

public interface IOrderSet
{
    void Add(OrderEntity entity);
    Task SaveChangesAsync();
}

public interface IOrderLineSet
{
    void Add(OrderLineEntity entity);
}

public class OrderEntity
{
    public Guid Id { get; set; }
    public string? CustomerName { get; set; }
}

public class OrderLineEntity
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public string? ProductName { get; set; }
    public int Quantity { get; set; }
}

#region interface-first-pattern
/// <summary>
/// Interface-First Design: Define a public interface for every entity.
/// The interface is your API contract.
/// </summary>
public partial interface IBpCustomer : IEntityBase
{
    Guid? Id { get; }
    string? Name { get; set; }
    string? Email { get; set; }
    IBpPhoneList Phones { get; }

    // Business operations belong on the interface
    Task<IBpCustomer> Archive();
}

/// <summary>
/// Concrete class is internal - consumers use the interface.
/// </summary>
[Factory]
internal partial class BpCustomer : EntityBase<BpCustomer>, IBpCustomer
{
    public BpCustomer(IEntityBaseServices<BpCustomer> services) : base(services) { }

    public partial Guid? Id { get; set; }
    public partial string? Name { get; set; }
    public partial string? Email { get; set; }
    public partial IBpPhoneList Phones { get; set; }

    public async Task<IBpCustomer> Archive()
    {
        // Business logic here
        return (IBpCustomer)await this.Save();
    }

    [Create]
    public void Create([Service] IBpPhoneListFactory phoneListFactory)
    {
        Phones = phoneListFactory.Create();
    }

    [Fetch]
    public void Fetch(Guid id)
    {
        Id = id;
        // In real code: load from database
    }
}
#endregion

// Child list for BpCustomer
public partial interface IBpPhoneList : IValidateListBase<IBpPhone> { }
public partial interface IBpPhone : IValidateBase
{
    string? Number { get; set; }
}

[Factory]
internal partial class BpPhoneList : ValidateListBase<IBpPhone>, IBpPhoneList
{
    [Create]
    public void Create() { }
}

[Factory]
internal partial class BpPhone : ValidateBase<BpPhone>, IBpPhone
{
    public BpPhone(IValidateBaseServices<BpPhone> services) : base(services) { }
    public partial string? Number { get; set; }

    [Create]
    public void Create() { }
}

#region interface-usage
/// <summary>
/// Always use interface types in consuming code.
/// </summary>
public class InterfaceUsageExample
{
    // Fields and properties - use interfaces
    private IBpOrder? _order;
    public IBpCustomer? SelectedCustomer { get; set; }

    // Method parameters and returns - use interfaces
    public void ProcessOrder(IBpOrder order)
    {
        _order = order;
    }

    public IBpCustomer? LoadCustomer(
        Guid id,
        IBpCustomerFactory customerFactory)
    {
        // Factory calls return interfaces
        var customer = customerFactory.Fetch(id);
        return customer;
    }
}
#endregion

// Supporting interface for usage example
public partial interface IBpOrder : IEntityBase
{
    Guid? Id { get; }
}

[Factory]
internal partial class BpOrder : EntityBase<BpOrder>, IBpOrder
{
    public BpOrder(IEntityBaseServices<BpOrder> services) : base(services) { }
    public partial Guid? Id { get; set; }

    [Create]
    public void Create() { }
}

#region business-operations-on-interface
/// <summary>
/// Expose business operations on interfaces.
/// </summary>
public partial interface IBpVisit : IEntityBase
{
    Guid? Id { get; }
    string? Status { get; set; }

    // Business operations belong on the interface
    Task<IBpVisit> Archive();
    void AddNote(string text);
}

[Factory]
internal partial class BpVisit : EntityBase<BpVisit>, IBpVisit
{
    public BpVisit(IEntityBaseServices<BpVisit> services) : base(services) { }

    public partial Guid? Id { get; set; }
    public partial string? Status { get; set; }
    public partial string? Notes { get; set; }

    public async Task<IBpVisit> Archive()
    {
        Status = "Archived";
        return (IBpVisit)await this.Save();
    }

    public void AddNote(string text)
    {
        Notes = string.IsNullOrEmpty(Notes) ? text : $"{Notes}\n{text}";
    }

    [Create]
    public void Create() { }
}
#endregion

#region nullable-id-pattern
/// <summary>
/// Use nullable types for database-generated IDs.
/// null = not yet persisted, Guid/long = persisted.
/// </summary>
public partial interface IBpProduct : IEntityBase
{
    Guid? Id { get; }  // null = not persisted
    string? Name { get; set; }
    decimal Price { get; set; }
}

[Factory]
internal partial class BpProduct : EntityBase<BpProduct>, IBpProduct
{
    public BpProduct(IEntityBaseServices<BpProduct> services) : base(services) { }

    public partial Guid? Id { get; set; }
    public partial string? Name { get; set; }
    public partial decimal Price { get; set; }

    [Create]
    public void Create()
    {
        // Id stays null - will be assigned during Insert
    }

    [Remote]
    [Insert]
    public async Task Insert([Service] IDbContext db)
    {
        var entity = new OrderEntity();
        entity.CustomerName = Name;
        db.Orders.Add(entity);
        await db.Orders.SaveChangesAsync();
        Id = entity.Id;  // Database-generated ID assigned here
    }
}
#endregion

#region child-entity-insert
/// <summary>
/// Child entity Insert receives parent ID as parameter.
/// </summary>
public partial interface IBpOrderLine : IEntityBase
{
    long? Id { get; }
    long? OrderId { get; }  // FK - null until Insert
    string? ProductName { get; set; }
    int Quantity { get; set; }
}

[Factory]
internal partial class BpOrderLine : EntityBase<BpOrderLine>, IBpOrderLine
{
    public BpOrderLine(IEntityBaseServices<BpOrderLine> services) : base(services) { }

    public partial long? Id { get; set; }
    public partial long? OrderId { get; set; }
    public partial string? ProductName { get; set; }
    public partial int Quantity { get; set; }

    [Create]
    public void Create()
    {
        // OrderId stays null - set during Insert
    }

    /// <summary>
    /// Insert receives parent ID as first parameter.
    /// The factory's Save(child, parentId) passes this through.
    /// </summary>
    [Insert]
    public async Task Insert(long orderId, [Service] IDbContext db)
    {
        OrderId = orderId;  // FK set from parameter
        var entity = new OrderLineEntity
        {
            OrderId = orderId,
            ProductName = ProductName,
            Quantity = Quantity
        };
        db.OrderLines.Add(entity);
        await db.Orders.SaveChangesAsync();
        Id = entity.Id;
    }
}
#endregion

#region parent-saves-children
/// <summary>
/// Parent entity Insert: saves itself first, then passes ID to children.
/// </summary>
public partial interface IBpInvoice : IEntityBase
{
    long? Id { get; }
    string? CustomerName { get; set; }
    IBpInvoiceLineList Lines { get; }
}

public partial interface IBpInvoiceLineList : IEntityListBase<IBpInvoiceLine> { }

public partial interface IBpInvoiceLine : IEntityBase
{
    long? Id { get; }
    long? InvoiceId { get; }
    string? Description { get; set; }
}

[Factory]
internal partial class BpInvoice : EntityBase<BpInvoice>, IBpInvoice
{
    public BpInvoice(IEntityBaseServices<BpInvoice> services) : base(services) { }

    public partial long? Id { get; set; }
    public partial string? CustomerName { get; set; }
    public partial IBpInvoiceLineList Lines { get; set; }

    [Create]
    public void Create([Service] IBpInvoiceLineListFactory lineListFactory)
    {
        Lines = lineListFactory.Create();
    }

    [Remote]
    [Insert]
    public async Task Insert([Service] IDbContext db, [Service] IBpInvoiceLineFactory lineFactory)
    {
        // Save parent first
        var entity = new OrderEntity { CustomerName = CustomerName };
        db.Orders.Add(entity);
        await db.Orders.SaveChangesAsync();
        Id = entity.Id.GetHashCode();  // Simulated long ID

        // Save children, passing parent ID to each child's Insert
        foreach (var line in Lines)
        {
            await lineFactory.Save(line, Id.Value);  // Parent ID passed to child
        }
    }
}

[Factory]
internal class BpInvoiceLineList : EntityListBase<IBpInvoiceLine>, IBpInvoiceLineList
{
    [Create]
    public void Create() { }
}

[Factory]
internal partial class BpInvoiceLine : EntityBase<BpInvoiceLine>, IBpInvoiceLine
{
    public BpInvoiceLine(IEntityBaseServices<BpInvoiceLine> services) : base(services) { }

    public partial long? Id { get; set; }
    public partial long? InvoiceId { get; set; }
    public partial string? Description { get; set; }

    [Create]
    public void Create() { }

    /// <summary>
    /// Insert receives parent ID - the factory's Save(child, parentId) passes this through.
    /// </summary>
    [Insert]
    public async Task Insert(long invoiceId, [Service] IDbContext db)
    {
        InvoiceId = invoiceId;  // FK set from parameter
        var entity = new OrderLineEntity
        {
            OrderId = invoiceId,
            ProductName = Description
        };
        db.OrderLines.Add(entity);
        await db.Orders.SaveChangesAsync();
        Id = entity.Id;
    }
}
#endregion
