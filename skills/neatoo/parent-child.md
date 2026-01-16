# Parent-Child Relationships

How to work with child collections in Neatoo aggregates.

## Overview

In DDD, an aggregate is a cluster of entities saved together. The aggregate root owns child collections:

```
Order (Aggregate Root)
├── OrderLines (Child Collection)
│   ├── OrderLine
│   ├── OrderLine
│   └── OrderLine
└── Metadata
```

**Key Concepts:**
- Children are created through parent methods, not directly
- Children cannot be saved independently (`IsChild = true`)
- Deleted children go to `DeletedList` for persistence
- Only aggregate root has `[Remote]` factory methods

## Child Entity Pattern

Child entities don't have `[Remote]` - they're managed through the parent:

```csharp
// Child interface
public partial interface IOrderLine : IEntityBase
{
    Guid? Id { get; }
    string? ProductName { get; set; }
    int Quantity { get; set; }
    decimal UnitPrice { get; set; }
    decimal LineTotal { get; }
}

// Child implementation - NO [Remote] attributes
[Factory]
internal partial class OrderLine : EntityBase<OrderLine>, IOrderLine
{
    public OrderLine(IEntityBaseServices<OrderLine> services) : base(services) { }

    public partial Guid? Id { get; set; }
    public partial string? ProductName { get; set; }
    public partial int Quantity { get; set; }
    public partial decimal UnitPrice { get; set; }

    public decimal LineTotal => Quantity * UnitPrice;  // Calculated, not partial

    [Create]  // No [Remote]
    public void Create()
    {
        Id = Guid.NewGuid();
    }

    [Fetch]  // No [Remote] - called by parent
    public void Fetch(OrderLineEntity entity)
    {
        Id = entity.Id;
        ProductName = entity.ProductName;
        Quantity = entity.Quantity;
        UnitPrice = entity.UnitPrice;
    }

    [Insert]  // No [Remote] - called by parent
    public void Insert(OrderLineEntity entity)
    {
        entity.Id = Id ?? Guid.NewGuid();
        entity.ProductName = ProductName;
        entity.Quantity = Quantity;
        entity.UnitPrice = UnitPrice;
    }

    [Update]  // No [Remote] - called by parent
    public void Update(OrderLineEntity entity)
    {
        if (this[nameof(ProductName)].IsModified) entity.ProductName = ProductName;
        if (this[nameof(Quantity)].IsModified) entity.Quantity = Quantity;
        if (this[nameof(UnitPrice)].IsModified) entity.UnitPrice = UnitPrice;
    }
}
```

## Child Collection Pattern

Collections use `EntityListBase<I>` and provide domain methods:

```csharp
// Collection interface with domain methods
public interface IOrderLineList : IEntityListBase<IOrderLine>
{
    IOrderLine AddLine();
    IOrderLine AddLine(string product, int qty, decimal price);
    void RemoveLine(IOrderLine line);
}

// Collection implementation
[Factory]
internal class OrderLineList : EntityListBase<IOrderLine>, IOrderLineList
{
    private readonly IOrderLineFactory _lineFactory;

    // Inject child factory
    public OrderLineList([Service] IOrderLineFactory lineFactory)
    {
        _lineFactory = lineFactory;
    }

    // Domain method: Add a new line
    public IOrderLine AddLine()
    {
        var line = _lineFactory.Create();
        Add(line);  // Sets parent, marks as child
        return line;
    }

    // Domain method: Add with values
    public IOrderLine AddLine(string product, int qty, decimal price)
    {
        var line = _lineFactory.Create();
        line.ProductName = product;
        line.Quantity = qty;
        line.UnitPrice = price;
        Add(line);
        return line;
    }

    // Domain method: Remove a line
    public void RemoveLine(IOrderLine line)
    {
        Remove(line);  // Moves to DeletedList if not new
    }

    // Fetch child items
    [Fetch]
    public void Fetch(IEnumerable<OrderLineEntity> entities)
    {
        foreach (var entity in entities)
        {
            var line = _lineFactory.Fetch(entity);
            Add(line);
        }
    }

    // Save child items (insert/update/delete)
    [Update]
    public void Update(ICollection<OrderLineEntity> entities)
    {
        // CRITICAL: Union with DeletedList to handle deletes
        foreach (var line in this.Union(DeletedList))
        {
            if (line.IsNew && !line.IsDeleted)
            {
                // Insert new item
                var entity = new OrderLineEntity();
                _lineFactory.Insert(line, entity);
                entities.Add(entity);
            }
            else if (line.IsDeleted && !line.IsNew)
            {
                // Delete existing item
                var entity = entities.FirstOrDefault(e => e.Id == line.Id);
                if (entity != null) entities.Remove(entity);
            }
            else if (!line.IsNew && line.IsModified)
            {
                // Update existing item
                var entity = entities.First(e => e.Id == line.Id);
                _lineFactory.Update(line, entity);
            }
        }
    }
}
```

## Parent Aggregate Pattern

The aggregate root owns the collection and orchestrates saves:

```csharp
public partial interface IOrder : IEntityBase
{
    Guid? Id { get; }
    string? CustomerName { get; set; }
    IOrderLineList Lines { get; }  // Child collection
}

[Factory]
internal partial class Order : EntityBase<Order>, IOrder
{
    public Order(IEntityBaseServices<Order> services) : base(services) { }

    public partial Guid? Id { get; set; }
    public partial string? CustomerName { get; set; }
    public partial IOrderLineList Lines { get; set; }  // MUST be partial for serialization

    [Create]
    public void Create([Service] IOrderLineListFactory linesFactory)
    {
        Id = Guid.NewGuid();
        Lines = linesFactory.Create();  // Initialize empty collection
    }

    [Remote]
    [Fetch]
    public async Task Fetch(Guid id, [Service] IDbContext db, [Service] IOrderLineListFactory linesFactory)
    {
        var entity = await db.Orders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (entity == null) return;

        Id = entity.Id;
        CustomerName = entity.CustomerName;
        Lines = linesFactory.Fetch(entity.Lines);  // Load children
    }

    [Remote]
    [Insert]
    public async Task Insert([Service] IDbContext db, [Service] IOrderLineListFactory linesFactory)
    {
        var entity = new OrderEntity
        {
            Id = Id ?? Guid.NewGuid(),
            CustomerName = CustomerName,
            Lines = new List<OrderLineEntity>()
        };

        db.Orders.Add(entity);
        linesFactory.Save(Lines, entity.Lines);  // Insert children
        await db.SaveChangesAsync();

        Id = entity.Id;
    }

    [Remote]
    [Update]
    public async Task Update([Service] IDbContext db, [Service] IOrderLineListFactory linesFactory)
    {
        var entity = await db.Orders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == Id);

        if (entity == null) throw new InvalidOperationException("Order not found");

        if (this[nameof(CustomerName)].IsModified)
            entity.CustomerName = CustomerName;

        linesFactory.Save(Lines, entity.Lines);  // Update children
        await db.SaveChangesAsync();
    }
}
```

## Accessing Parent from Child

Children can access their parent via the `Parent` property:

```csharp
// In child entity
public IOrder? ParentOrder => Parent as IOrder;

// In validation rule
public class LineTotalRule : RuleBase<IOrderLine>
{
    public LineTotalRule() : base(l => l.Quantity, l => l.UnitPrice) { }

    protected override IRuleMessages Execute(IOrderLine target)
    {
        var order = target.Parent as IOrder;
        if (order == null) return None;

        if (target.LineTotal > order.MaxLineTotal)
        {
            return (nameof(target.LineTotal),
                $"Line total cannot exceed {order.MaxLineTotal}").AsRuleMessages();
        }
        return None;
    }
}
```

## Accessing Root from Any Depth

For deeply nested aggregates, use `Root`:

```csharp
// Order → Lines → LineDetails
public IOrder? RootOrder => Root as IOrder;
```

## DeletedList - Critical for Persistence

When items are removed from a collection, they move to `DeletedList`:

```csharp
// Remove marks item for deletion
order.Lines.RemoveLine(line);
// If line.IsNew, it's removed completely
// If line was persisted (!IsNew), it goes to Lines.DeletedList

// In Update method, process DeletedList
[Update]
public void Update(ICollection<OrderLineEntity> entities)
{
    // MUST include DeletedList!
    foreach (var line in this.Union(DeletedList))
    {
        if (line.IsDeleted && !line.IsNew)
        {
            // Remove from database
            var entity = entities.FirstOrDefault(e => e.Id == line.Id);
            if (entity != null) entities.Remove(entity);
        }
        // ... handle insert and update
    }
}
```

## Creating Children Through Parent

**Always** create children through the parent's domain methods:

```csharp
// CORRECT - Use parent's method
var line = order.Lines.AddLine();
line.ProductName = "Widget";
line.Quantity = 5;

// Or with values
var line = order.Lines.AddLine("Widget", 5, 9.99m);

// WRONG - Don't inject child factory in Blazor components
@inject IOrderLineFactory LineFactory  // BAD!

void AddLine()
{
    var line = LineFactory.Create();  // Bypasses aggregate!
    order.Lines.Add(line);  // Will have issues
}
```

**Why?** Creating through parent:
- Sets up parent-child relationship correctly
- Marks item as child (`IsChild = true`)
- Maintains aggregate consistency
- Keeps factory injection inside aggregate

## Collection Item States

| Item State | On Remove() |
|------------|-------------|
| `IsNew == true` | Removed completely |
| `IsNew == false` | Moved to DeletedList, `IsDeleted = true` |

| Item in DeletedList | Meaning |
|--------------------|---------|
| `IsNew && IsDeleted` | Never persisted, can ignore |
| `!IsNew && IsDeleted` | Was persisted, must delete from DB |

## Usage in Blazor

```razor
@inject IOrderFactory OrderFactory

<MudText>Order: @order?.CustomerName</MudText>

<MudTable Items="@order?.Lines">
    <HeaderContent>
        <MudTh>Product</MudTh>
        <MudTh>Qty</MudTh>
        <MudTh>Price</MudTh>
        <MudTh></MudTh>
    </HeaderContent>
    <RowTemplate Context="line">
        <MudTd>@line.ProductName</MudTd>
        <MudTd>@line.Quantity</MudTd>
        <MudTd>@line.UnitPrice</MudTd>
        <MudTd>
            <MudIconButton Icon="@Icons.Material.Delete"
                          OnClick="@(() => RemoveLine(line))" />
        </MudTd>
    </RowTemplate>
</MudTable>

<MudButton OnClick="@AddLine">Add Line</MudButton>
<MudButton Disabled="@(!order.IsSavable)" OnClick="@Save">Save Order</MudButton>

@code {
    private IOrder order = default!;

    protected override void OnInitialized()
    {
        order = OrderFactory.Create();
    }

    private void AddLine()
    {
        order.Lines.AddLine();  // Use parent's method!
    }

    private void RemoveLine(IOrderLine line)
    {
        order.Lines.RemoveLine(line);  // Moves to DeletedList if persisted
    }

    private async Task Save()
    {
        order = await OrderFactory.Save(order);  // Saves parent + all children
    }
}
```

## Common Mistakes

### Forgetting DeletedList

```csharp
// WRONG - deleted items not processed
[Update]
public void Update(ICollection<LineEntity> entities)
{
    foreach (var line in this)  // Missing DeletedList!
    {
        // ...
    }
}

// CORRECT - include DeletedList
[Update]
public void Update(ICollection<LineEntity> entities)
{
    foreach (var line in this.Union(DeletedList))  // Process all!
    {
        // ...
    }
}
```

### Injecting Child Factory in UI

```csharp
// WRONG - bypasses aggregate
@inject IOrderLineFactory LineFactory

// CORRECT - use parent's domain method
var line = order.Lines.AddLine();
```

### Not Initializing Collection in Create

```csharp
// WRONG - Lines will be null
[Create]
public void Create()
{
    Id = Guid.NewGuid();
    // Oops! Lines not initialized
}

// CORRECT
[Create]
public void Create([Service] IOrderLineListFactory linesFactory)
{
    Id = Guid.NewGuid();
    Lines = linesFactory.Create();  // Initialize collection
}
```

### Missing Partial on Collection Property

```csharp
// WRONG - collection won't serialize properly
public IOrderLineList Lines { get; set; }  // Missing partial!

// CORRECT - must be partial for client-server communication
public partial IOrderLineList Lines { get; set; }
```

## Best Practices

1. **Create children through parent's domain methods** - Not by injecting child factory
2. **Always process DeletedList in Update** - `this.Union(DeletedList)`
3. **Initialize collections in Create** - Factory-created empty collection
4. **Include related entities in Fetch** - `.Include(o => o.Lines)`
5. **Only aggregate root has [Remote]** - Children are managed internally
6. **Save parent to save everything** - Children save through parent

## Next Steps

- [saving-data.md](saving-data.md) - Factory operation details
- [client-server.md](client-server.md) - Setting up Blazor
- [troubleshooting.md](troubleshooting.md) - Debug collection issues
