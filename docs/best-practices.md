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

### Interfaces Are Persistence-Agnostic

**Interfaces represent the domain model, not the database schema.** Foreign keys are persistence implementation details that should not appear on interfaces.

| Domain Concept | Interface Exposes | Interface Hides |
|----------------|-------------------|-----------------|
| Parent-child relationship | `IOrder.Lines` (collection) | `IOrderLine.OrderId` (FK) |
| Entity identity | `IOrderLine.Id` (primary key) | Database-specific details |
| Parent navigation | `IOrderLine.ParentOrder` (object reference) | Foreign key value |

#### Why Hide Foreign Keys?

Foreign keys are database schema details. Exposing them on interfaces:

- **Couples domain to persistence** - What if you switch from SQL to NoSQL?
- **Tempts improper mutation** - Consumers might try to reassign `OrderId` to "move" a child
- **Leaks abstraction** - Domain consumers shouldn't need to know about database keys

#### Parent Navigation Property Pattern

If consumers need to know which parent a child belongs to, expose a **parent navigation property** instead of an FK:

<!-- invalid:fk-on-interface -->
```csharp
// WRONG - FK on interface leaks persistence detail
public interface IOrderLine : IEntityBase
{
    long? OrderId { get; }  // Exposes database key
}
```
<!-- /snippet -->

<!-- pseudo:parent-navigation-correct -->
```csharp
// CORRECT - Object reference via Neatoo's Parent property
public interface IOrderLine : IEntityBase
{
    IOrder? ParentOrder { get; }  // Domain-oriented navigation
}

// Implementation uses built-in Parent property
// NO FK property on domain object - pass through Insert parameter instead
internal partial class OrderLine : EntityBase<OrderLine>, IOrderLine
{
    public IOrder? ParentOrder => this.Parent as IOrder;
}
```
<!-- /snippet -->

The navigation property uses Neatoo's built-in `Parent` property, which is automatically set when a child is added to a parent's collection.

#### Best Practice: Don't Store FKs on Domain Objects

**Pass parent IDs through factory method parameters, not as stored properties.**

The child's `[Insert]` method receives the parent ID as a parameter and passes it directly to the EF entity during persistence. The domain object never stores the FK:

<!-- pseudo:fk-through-insert -->
```csharp
// Insert receives parentId, passes directly to EF - no FK property needed
[Insert]
public async Task Insert(long orderId, [Service] IDbContext db)
{
    var entity = new OrderLineEntity
    {
        OrderId = orderId,  // FK goes to EF entity only
        ProductName = ProductName
    };
    db.OrderLines.Add(entity);
    await db.SaveChangesAsync();
    Id = entity.Id;
}
```
<!-- /snippet -->

This pattern:
- Keeps domain objects persistence-agnostic
- Avoids `partial` FK properties appearing on generated interfaces
- Uses factory's `Save(child, parentId)` overload to pass IDs through

#### Anti-Pattern: FK with Setter on Interface

<!-- invalid:fk-setter-antipattern -->
```csharp
// WRONG - FK with setter allows mutation
public interface IOrderLine : IEntityBase
{
    long? OrderId { get; set; }  // Setter is dangerous
}

// This forces awkward patterns:
var line = lineFactory.Create();
line.OrderId = order.Id;  // Manual FK assignment - bad!

// Or worse - consumers think they can "move" a line:
line.OrderId = differentOrder.Id;  // Doesn't actually work as expected
```
<!-- /snippet -->

#### Anti-Pattern: Read-Only FK on Interface

Even read-only FKs violate the persistence-agnostic principle:

<!-- invalid:fk-readonly-antipattern -->
```csharp
// STILL WRONG - Read-only FK still leaks persistence
public interface IOrderLine : IEntityBase
{
    long? OrderId { get; }  // No setter, but still exposes database key
}

// Problems:
// 1. Interface now depends on database schema (long vs GUID)
// 2. Consumers may cache/compare FKs instead of using object references
// 3. Violates DDD - domain model shouldn't expose storage keys
```
<!-- /snippet -->

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

### Parent-Child Relationships

When saving parent-child aggregates, the parent passes its ID to the child's Insert method. The child's FK is an internal implementation detail—never expose it on the interface (see [Interfaces Are Persistence-Agnostic](#interfaces-are-persistence-agnostic)).

**Child entity - Insert receives parent ID as parameter:**

<!-- snippet: child-entity-insert -->
```cs
/// <summary>
/// Child entity Insert receives parent ID as parameter.
/// Foreign key is an internal implementation detail - not exposed on interface.
/// </summary>
public partial interface IBpOrderLine : IEntityBase
{
    long? Id { get; }
    string? ProductName { get; set; }
    int Quantity { get; set; }
    // Note: No OrderId on interface - FK is a persistence implementation detail
}

[Factory]
internal partial class BpOrderLine : EntityBase<BpOrderLine>, IBpOrderLine
{
    public BpOrderLine(IEntityBaseServices<BpOrderLine> services) : base(services) { }

    public partial long? Id { get; set; }
    public partial string? ProductName { get; set; }
    public partial int Quantity { get; set; }

    [Create]
    public void Create() { }

    /// <summary>
    /// Insert receives parent ID as first parameter.
    /// The factory's Save(child, parentId) passes this through.
    /// FK is passed directly to persistence - not stored on domain object.
    /// </summary>
    [Insert]
    public async Task Insert(long orderId, [Service] IDbContext db)
    {
        var entity = new OrderLineEntity
        {
            OrderId = orderId,  // FK goes directly to EF entity
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
    string? Description { get; set; }

    /// <summary>
    /// Parent navigation via object reference - use instead of FK.
    /// </summary>
    IBpInvoice? ParentInvoice { get; }
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
    public partial string? Description { get; set; }

    // Parent navigation - uses Neatoo's built-in Parent property
    public IBpInvoice? ParentInvoice => this.Parent as IBpInvoice;

    [Create]
    public void Create() { }

    /// <summary>
    /// Insert receives parent ID - the factory's Save(child, parentId) passes this through.
    /// FK is passed directly to persistence - not stored on domain object.
    /// </summary>
    [Insert]
    public async Task Insert(long invoiceId, [Service] IDbContext db)
    {
        var entity = new OrderLineEntity
        {
            OrderId = invoiceId,  // FK goes directly to EF entity
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
