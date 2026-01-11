# Neatoo Entities Reference

## Class Hierarchy

Neatoo provides a hierarchy of base classes, each adding capabilities:

```
ValidateBase<T>            - Property management, INotifyPropertyChanged, validation, rules engine
    EntityBase<T>          - Persistence awareness, modification tracking, IsNew/IsDeleted

ValidateListBase<I>        - Observable collection, parent-child, validation aggregation
    EntityListBase<I>      - DeletedList, persistence integration
```

## EntityBase<T> - Full Persistence Support

Use `EntityBase<T>` for domain entities that persist to a database.

### Complete Entity Pattern

<!-- snippet: entitybase-basic -->
```csharp
/// <summary>
/// Basic EntityBase example showing automatic state tracking.
/// </summary>
public partial interface IOrder : IEntityBase
{
    Guid Id { get; set; }
    string? Status { get; set; }
    decimal Total { get; set; }
}

[Factory]
internal partial class Order : EntityBase<Order>, IOrder
{
    public Order(IEntityBaseServices<Order> services) : base(services)
    {
        #region docs:aggregates-and-entities:inline-validation-rule
        // Inline validation rule - Total must be positive
        RuleManager.AddValidation(
            t => t.Total <= 0 ? "Total must be greater than zero" : "",
            t => t.Total);
```
<!-- /snippet -->

### Key Requirements

1. **Class is `partial` and `internal`** - Source generator needs partial; internal for encapsulation
2. **Inherits `EntityBase<T>`** - Where T is the class itself
3. **Implements interface** - Public interface for external code
4. **Constructor accepts services** - Pass `IEntityBaseServices<T>` to base
5. **Properties are `partial`** - Source generator provides implementation

### Interface Pattern

<!-- snippet: interface-requirement -->
```csharp
/// <summary>
/// Every aggregate requires a public interface for factory generation.
/// </summary>
public partial interface ICustomer : IEntityBase
{
    // Properties are auto-generated from the partial class
}
```
<!-- /snippet -->

### Class Declaration

<!-- snippet: class-declaration -->
```csharp
[Factory]
internal partial class Customer : EntityBase<Customer>, ICustomer
```
<!-- /snippet -->

### Constructor Pattern

<!-- snippet: entity-constructor -->
```csharp
public Customer(IEntityBaseServices<Customer> services) : base(services) { }
```
<!-- /snippet -->

### Inline Validation Rule

<!-- snippet: inline-validation-rule -->
```csharp
// Inline validation rule - Total must be positive
        RuleManager.AddValidation(
            t => t.Total <= 0 ? "Total must be greater than zero" : "",
            t => t.Total);
```
<!-- /snippet -->

### EntityBase Meta-Properties

| Property | Type | Description |
|----------|------|-------------|
| `IsValid` | bool | Entity AND all children pass validation |
| `IsSelfValid` | bool | This entity's properties pass validation |
| `IsModified` | bool | Entity OR any child has changes |
| `IsSelfModified` | bool | This entity has direct changes |
| `IsBusy` | bool | Async operations running (entity or children) |
| `IsSelfBusy` | bool | Async operations on this entity |
| `IsNew` | bool | Created but not yet persisted |
| `IsDeleted` | bool | Delete() was called |
| `IsSavable` | bool | `IsModified && IsValid && !IsBusy && !IsChild` |
| `IsChild` | bool | Part of parent aggregate |
| `Parent` | IValidateBase? | Immediate parent in object graph |
| `Root` | IValidateBase? | Aggregate root (null if this IS the root) |
| `ModifiedProperties` | IEnumerable<string> | Names of changed properties |
| `PropertyMessages` | IReadOnlyCollection | Validation messages |

### EntityBase Methods

| Method | Return Type | Description |
|--------|-------------|-------------|
| `Save()` | `Task<IEntityBase>` | Persist entity (routes to Insert/Update/Delete) |
| `Save(CancellationToken)` | `Task<IEntityBase>` | Save with cancellation support |
| `Delete()` | void | Mark entity for deletion |
| `UnDelete()` | void | Undo deletion mark |
| `MarkModified()` | void | Force entity to be considered modified |

The `Save()` method enables the **Business Operations Pattern** - domain methods that modify state and persist atomically. See [Factory Operations - Business Operations](factories.md#business-operations-pattern).

### Save Routing

The framework uses entity state to route Save() calls:

| IsNew | IsDeleted | Operation Called |
|-------|-----------|------------------|
| true | false | `[Insert]` |
| false | false | `[Update]` |
| any | true | `[Delete]` |

## Partial Properties

Properties must be declared as `partial` for the source generator.

### Partial vs Non-Partial

<!-- snippet: partial-properties -->
```csharp
/// <summary>
/// Demonstrates partial vs non-partial properties.
/// </summary>
public partial interface IEmployee : IEntityBase
{
    string? FirstName { get; set; }
    string? LastName { get; set; }
    string FullName { get; }
    bool IsExpanded { get; set; }
}

[Factory]
internal partial class Employee : EntityBase<Employee>, IEmployee
{
    public Employee(IEntityBaseServices<Employee> services) : base(services) { }

    #region docs:aggregates-and-entities:partial-property-declaration
    // Correct - generates backing field with change tracking
    public partial string? FirstName { get; set; }
    public partial string? LastName { get; set; }
```
<!-- /snippet -->

### Partial Property Declaration

<!-- snippet: partial-property-declaration -->
```csharp
// Correct - generates backing field with change tracking
    public partial string? FirstName { get; set; }
    public partial string? LastName { get; set; }
```
<!-- /snippet -->

### Non-Partial Properties (Calculated/UI-Only)

<!-- snippet: non-partial-properties -->
```csharp
// Calculated property - not tracked, not serialized
    public string FullName => $"{FirstName} {LastName}";

    // UI-only property - not transferred to server
    public bool IsExpanded { get; set; }
```
<!-- /snippet -->

### Property Features Provided by Generator

- Value storage in PropertyManager
- Change detection (old vs new value)
- PropertyChanged events
- Modification tracking
- Rule triggering on change
- Busy state management for async operations

## Data Annotations

Use data annotations for display and validation.

### Complete Data Annotations Example

<!-- snippet: data-annotations -->
```csharp
/// <summary>
/// Using data annotations for display and validation.
/// </summary>
public partial interface IContact : IEntityBase
{
    string? FirstName { get; set; }
    string? Email { get; set; }
}

[Factory]
internal partial class Contact : EntityBase<Contact>, IContact
{
    public Contact(IEntityBaseServices<Contact> services) : base(services) { }

    #region docs:aggregates-and-entities:displayname-required
    [DisplayName("First Name*")]
    [Required(ErrorMessage = "First Name is required")]
    public partial string? FirstName { get; set; }
```
<!-- /snippet -->

### DisplayName and Required

<!-- snippet: displayname-required -->
```csharp
[DisplayName("First Name*")]
    [Required(ErrorMessage = "First Name is required")]
    public partial string? FirstName { get; set; }
```
<!-- /snippet -->

### EmailAddress Validation

<!-- snippet: emailaddress-validation -->
```csharp
[DisplayName("Email Address")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public partial string? Email { get; set; }
```
<!-- /snippet -->

## ValidateBase<T> - Validation Without Persistence

Use `ValidateBase<T>` for objects that need validation but are not persisted independently.

### Search Criteria Example

<!-- snippet: validatebase-criteria -->
```csharp
/// <summary>
/// Criteria object - has validation but no persistence.
/// Use ValidateBase for objects that need validation but are NOT persisted.
/// </summary>
public partial interface IPersonSearchCriteria : IValidateBase
{
    string? SearchTerm { get; set; }
    DateTime? FromDate { get; set; }
    DateTime? ToDate { get; set; }
}

#region docs:aggregates-and-entities:validatebase-declaration
[Factory]
internal partial class PersonSearchCriteria : ValidateBase<PersonSearchCriteria>, IPersonSearchCriteria
```
<!-- /snippet -->

### ValidateBase Features

- Validation rules via attributes and RuleManager
- `IsValid`, `IsSelfValid`, `PropertyMessages`
- `IsBusy`, `IsSelfBusy` for async rules
- No persistence tracking (no IsNew, IsModified, IsDeleted)

## Value Objects

Value Objects represent concepts defined by attributes, not identity.

### Value Object Pattern

<!-- snippet: value-object -->
```csharp
/// <summary>
/// Value Object - simple POCO class with [Factory] attribute.
/// No Neatoo base class inheritance. RemoteFactory generates fetch operations.
/// Typical Use: Lookup data, dropdown options, reference data.
/// </summary>
public interface IStateProvince
{
    string Code { get; }
    string Name { get; }
}

#region docs:aggregates-and-entities:value-object-declaration
[Factory]
internal partial class StateProvince : IStateProvince
```
<!-- /snippet -->

### Value Object Characteristics

1. **Identity-less** - Equality based on attributes, not identity
2. **Immutable** - Private setters, operations return new instances
3. **Self-validating** - Throw from Create for invalid inputs
4. **No base class** - Plain class with [Factory] attribute

## Child Entities

Child entities are part of a parent aggregate.

### Child Entity Pattern

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

## EntityListBase<I> - Child Collections

Collections of child entities use `EntityListBase<I>`.

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

### Fetch Operation

<!-- snippet: fetch-operation -->
```csharp
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

### Update Operation

> **Critical**: Always iterate `this.Union(DeletedList)` in Update methods. If you only
> iterate `this`, removed items will silently remain in the database.

<!-- snippet: update-operation -->
```csharp
[Update]
    public void Update(ICollection<PhoneEntity> entities,
                       [Service] IPhoneFactory phoneFactory)
    {
        // Process all items including deleted ones
        foreach (var phone in this.Union(DeletedList))
        {
            PhoneEntity entity;

            if (phone.IsNew)
            {
                // Create new EF entity
                entity = new PhoneEntity();
                entities.Add(entity);
            }
            else
            {
                // Find existing EF entity
                entity = entities.Single(e => e.Id == phone.Id);
            }

            if (phone.IsDeleted)
            {
                // Remove from EF collection
                entities.Remove(entity);
            }
            else
            {
                // Save the item (insert or update)
                phoneFactory.Save(phone, entity);
            }
        }
    }
```
<!-- /snippet -->

### Collection Behavior

| Item State | On Remove() |
|------------|-------------|
| `IsNew == true` | Removed completely |
| `IsNew == false` | Marked deleted, moved to DeletedList |

### Delete/Remove Consistency

Calling `entity.Delete()` on an entity in a list is equivalent to calling `list.Remove(entity)`. Both operations:

1. Remove the entity from the list
2. Mark it as deleted (`IsDeleted = true`)
3. Add it to the `DeletedList` (if not new)

## Aggregate Root Pattern

### Complete Aggregate Root

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

## Common Pitfalls

1. **Non-partial properties** - Source generator requires `partial` keyword
2. **Public class** - Classes should be `internal`, interfaces public
3. **Missing base constructor call** - Must call `base(services)`
4. **Wrong services type** - Use `IEntityBaseServices<T>` for EntityBase, etc.
5. **Direct instantiation** - Always use factories, never `new Entity()`
6. **Not using interface-first design** - See best-practices.md for the recommended pattern
7. **Storing concrete types** - Use interface types for fields and variables
