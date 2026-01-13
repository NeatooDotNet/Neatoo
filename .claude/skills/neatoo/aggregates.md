# Neatoo Aggregates Reference

## What is an Aggregate?

In Domain-Driven Design, an **Aggregate** is a cluster of entities treated as a single unit for data changes. The **Aggregate Root** is the entry point that ensures consistency across the entire cluster.

### Aggregate Characteristics

1. **Single Entry Point** - External code accesses only the root
2. **Transactional Boundary** - All changes saved atomically
3. **Consistency Enforcement** - Root enforces invariants across children
4. **Identity through Root** - Children referenced via root's identity

## Aggregate Root Pattern

<!-- snippet: aggregate-root-pattern -->
```csharp
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
```
<!-- /snippet -->

## Child Entity Pattern

### Child Entity with Parent Access

<!-- snippet: child-entity-pattern -->
```csharp
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
```
<!-- /snippet -->

### Parent Access Property

<!-- snippet: parent-access-property -->
```csharp
// Access parent through the Parent property
    public IContact? ParentContact => Parent as IContact;
```
<!-- /snippet -->

## Remote Operations

### Remote Fetch

<!-- snippet: remote-fetch -->
```csharp
// [Remote] - Called from UI
    [Remote]
    [Fetch]
    public void Fetch(Guid id)
```
<!-- /snippet -->

### Remote Insert

<!-- snippet: remote-insert -->
```csharp
[Remote]
    [Insert]
    public async Task Insert()
```
<!-- /snippet -->

## Child Fetch Without Remote

<!-- snippet: child-fetch-no-remote -->
```csharp
// No [Remote] - called internally by parent
    [Fetch]
    public void Fetch(OrderLineItemDto dto)
```
<!-- /snippet -->

## Entity List (Child Collections)

### Child Item

<!-- snippet: child-item -->
```csharp
/// <summary>
/// Child entity for phone numbers.
/// </summary>
public partial interface IPhone : IEntityBase
{
    Guid Id { get; }
    string? PhoneNumber { get; set; }
    string? PhoneType { get; set; }
}

[Factory]
internal partial class Phone : EntityBase<Phone>, IPhone
{
    public Phone(IEntityBaseServices<Phone> services) : base(services) { }

    public partial Guid Id { get; set; }
    public partial string? PhoneNumber { get; set; }
    public partial string? PhoneType { get; set; }

    [Create]
    public void Create()
    {
        Id = Guid.NewGuid();
    }

    [Fetch]
    public void Fetch(PhoneEntity entity)
    {
        Id = entity.Id;
        PhoneNumber = entity.PhoneNumber;
        PhoneType = entity.PhoneType;
    }

    [Insert]
    public void Insert(PhoneEntity entity)
    {
        entity.Id = Id;
        entity.PhoneNumber = PhoneNumber ?? "";
        entity.PhoneType = PhoneType ?? "";
    }

    [Update]
    public void Update(PhoneEntity entity)
    {
        if (this[nameof(PhoneNumber)].IsModified)
            entity.PhoneNumber = PhoneNumber ?? "";
        if (this[nameof(PhoneType)].IsModified)
            entity.PhoneType = PhoneType ?? "";
    }
}
```
<!-- /snippet -->

### Interface Definition

<!-- snippet: interface-definition -->
```csharp
/// <summary>
/// Collection interface with domain-specific methods.
/// </summary>
public interface IPhoneList : IEntityListBase<IPhone>
{
    IPhone AddPhoneNumber();
    void RemovePhoneNumber(IPhone phone);
}
```
<!-- /snippet -->

### List Implementation

<!-- snippet: list-implementation -->
```csharp
/// <summary>
/// EntityListBase implementation with factory injection.
/// </summary>
[Factory]
internal class PhoneList : EntityListBase<IPhone>, IPhoneList
{
    private readonly IPhoneFactory _phoneFactory;

    public PhoneList([Service] IPhoneFactory phoneFactory)
    {
        _phoneFactory = phoneFactory;
    }

    public IPhone AddPhoneNumber()
    {
        var phone = _phoneFactory.Create();
        Add(phone);  // Marks as child, sets parent
        return phone;
    }

    public void RemovePhoneNumber(IPhone phone)
    {
        Remove(phone);  // Marks for deletion if not new
    }

    #region docs:collections:fetch-operation
    [Fetch]
    public void Fetch(IEnumerable<PhoneEntity> entities,
                      [Service] IPhoneFactory phoneFactory)
    {
        foreach (var entity in entities)
        {
            var phone = phoneFactory.Fetch(entity);
            Add(phone);
        }
    }
```
<!-- /snippet -->

## Key Concepts

### Parent-Child Relationship

1. **Parent property** - Access via `Parent` or cast to specific type
2. **Root property** - Access aggregate root from any depth
3. **IsChild flag** - True for entities in a collection

### Collection Behavior

| Item State | On Remove() |
|------------|-------------|
| `IsNew == true` | Removed completely |
| `IsNew == false` | Marked deleted, moved to DeletedList |

### Cross-Aggregate References

Use IDs rather than direct references for cross-aggregate relationships:

<!-- pseudo:cross-aggregate-reference -->
```csharp
// Cross-aggregate reference by ID
public partial Guid? CustomerId { get; set; }

// NOT: public partial ICustomer? Customer { get; set; }
```
<!-- /snippet -->

## Child Creation Pattern

Child entities should be created through the parent collection's add methods, not by injecting child factories into consuming code:

<!-- pseudo:child-creation-pattern -->
```csharp
// CORRECT - Use aggregate's domain method
var phone = contact.PhoneNumbers.AddPhoneNumber();
phone.Number = "555-1234";

// WRONG - Injecting child factory in Blazor component
@inject IPhoneFactory PhoneFactory

void AddPhone()
{
    var phone = PhoneFactory.Create();  // Bypasses aggregate!
    contact.PhoneNumbers.Add(phone);
}
```
<!-- /snippet -->

The collection's `AddPhoneNumber()` method:
- Creates via factory internally
- Sets up parent-child relationship
- Maintains aggregate consistency
- Keeps factory injection inside the aggregate

See pitfalls.md #15 for more details.

## Best Practices

1. **Keep aggregates small** - Smaller transaction boundaries
2. **Reference by ID across aggregates** - Avoid complex loading
3. **Use root for saves** - Only save via aggregate root factory
4. **Validate at aggregate level** - Cross-entity business rules
5. **Create children through parent's add methods** - Don't inject child factories externally
6. **Work with interfaces** - Never cast to concrete types (see pitfalls.md #14)
