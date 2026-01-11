# Best Practices

This document outlines recommended patterns for building applications with Neatoo.

## 1. Interface-First Design

**Define a public interface for every Neatoo entity.** The interface is your API contract; all code outside the entity class works with interfaces.

### The Pattern

<!-- snippet: interface-first-pattern -->
```cs
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
```
<!-- endSnippet -->

### Why This Matters

| Benefit | Description |
|---------|-------------|
| **Testability** | Mock dependencies and stub entities in unit tests |
| **Clean API** | Interface defines what consumers can do; implementation details stay hidden |
| **Client-server** | RemoteFactory generates transfer code from interfaces |
| **Encapsulation** | `internal` class prevents direct instantiation; factory pattern enforced |

### Applying the Pattern

**Always use interface types** in consuming code:

<!-- snippet: interface-usage -->
```cs
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
```
<!-- endSnippet -->

**Expose business operations on interfaces:**

<!-- snippet: business-operations-on-interface -->
```cs
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
```
<!-- endSnippet -->

### Anti-Patterns to Avoid

Casting to concrete types defeats the interface-first pattern:

```csharp
// WRONG - Casting to concrete
var person = personFactory.Create();
var concrete = (Person)person;  // Don't do this

// WRONG - Storing concrete types
private Person _person;  // Should be IPerson

// WRONG - Calling child factory directly
var phone = phoneFactory.Create();  // Bypass aggregate's add method
```

**If you find yourself needing to cast, the interface is incomplete.** Add the needed method or property to the interface.

| Symptom | Solution |
|---------|----------|
| Need a method not on interface | Add the method to the interface |
| Need to access internal state | Expose via interface property |
| Child factory called directly | Use `parent.Children.AddItem()` pattern |

For a complete list of interface-related mistakes, see [Common Pitfalls](troubleshooting.md) Section 14.

## 2. Factory Pattern for Entity Creation

**Never instantiate entities directly.** Always use the generated factory.

```csharp
// WRONG - Direct instantiation
var order = new Order();  // Won't compile (internal class)

// CORRECT - Factory creation
var order = await orderFactory.Create();
```

The factory:
- Sets up parent-child relationships
- Initializes state tracking
- Provides dependency injection
- Enables client-server transfer

## 3. Aggregate Root Saves Children

**Save through the aggregate root, not individual children.**

```csharp
// WRONG - Saving child directly
await orderLineFactory.Save(lineItem);  // Child factories don't have Save

// CORRECT - Save through aggregate root
order = await orderFactory.Save(order);  // Saves order AND all line items
```

Child entities:
- Set `IsChild = true` automatically when added to parent
- Have `IsSavable = false` (cannot save independently)
- Persist through the aggregate root's Insert/Update methods

## 4. Reassign After Save

**Always capture the return value from Save().** The server deserializes a new instance.

```csharp
// WRONG - Changes lost
await personFactory.Save(person);
person.Name = "Updated";  // person still references the old instance

// CORRECT - Reassign
person = await personFactory.Save(person);
person.Name = "Updated";  // Works with the server-returned instance
```

## 5. Check IsSavable, Not Just IsValid

**Use `IsSavable` for the complete save-readiness check.**

```csharp
// WRONG - Incomplete check
if (person.IsValid) { await Save(); }  // Ignores IsBusy, IsModified, IsChild

// CORRECT - Complete check
if (person.IsSavable) { await Save(); }
```

`IsSavable` is `true` when:
- `IsModified == true` (has changes)
- `IsValid == true` (passes validation)
- `IsBusy == false` (no async operations running)
- `IsChild == false` (is an aggregate root)

## 6. Wait for Async Rules

**Await `WaitForTasks()` before checking validity** when using async validation rules.

```csharp
// WRONG - May check before async rules complete
if (person.IsValid) { ... }

// CORRECT - Wait for async rules
await person.WaitForTasks();
if (person.IsValid) { ... }
```

## 7. Nullable IDs for Database-Generated Keys

**Use nullable types for ID properties when the database generates the value.** A `null` ID means "not yet persisted."

### The Pattern

<!-- snippet: nullable-id-pattern -->
```cs
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
```
<!-- endSnippet -->

### Why This Matters

| Approach | Problem |
|----------|---------|
| `Guid Id` with `Guid.Empty` | Ambiguous - is it "not set" or an actual empty GUID? |
| `long Id` with `0` | Ambiguous - is it "not set" or ID zero? |
| `Guid? Id` with `null` | Clear - `null` means "doesn't exist in database yet" |

### Key Points

1. **ID is null until Insert** - Don't assign IDs in Create methods
2. **Database generates the ID** - The ID is assigned during Insert after `SaveChangesAsync()`
3. **After Save(), capture the return** - The returned instance has the database-assigned ID
4. **Use nullable for FKs too** - Child entity foreign keys can be `null` until parent is persisted

### Parent-Child Relationships

When both parent and child are new, the child's foreign key is `null` until the parent is persisted. The parent passes its ID to the child's Insert via the factory Save method.

**Child entity - Insert receives parent ID as parameter:**

<!-- snippet: child-entity-insert -->
```cs
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
```
<!-- endSnippet -->

**Parent entity - saves itself first, then passes ID to children:**

<!-- snippet: parent-saves-children -->
```cs
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
```
<!-- endSnippet -->

**Key insight:** The child's `[Insert]` method accepts the parent ID as a parameter. The factory's `Save()` method passes this through to Insert. This keeps Create clean (no FK needed) while ensuring Insert has the parent ID.

## See Also

- [Aggregates and Entities](aggregates-and-entities.md) - Entity patterns and class hierarchy
- [Factory Operations](factory-operations.md) - Complete factory lifecycle
- [Troubleshooting](troubleshooting.md) - Common pitfalls and solutions
