# Aggregates and Entities

This document covers creating domain model classes using Neatoo's base classes.

## Class Hierarchy Overview

Users typically inherit from `ValidateBase<T>` or `EntityBase<T>`:

```
Base<T>                      - Internal base class (not for direct use)
    |
ValidateBase<T>              - For non-persisted validated objects (criteria, filters)
    |
EntityBase<T>                - For entities with identity, modification tracking, persistence
```

**Note:** Value Objects are simple POCO classes with `[Factory]` attribute - they do not inherit from any Neatoo base class.

## Base Class Selection

| Use Case | Neatoo Base Class |
|----------|------------------|
| Aggregate root with persistence | `EntityBase<T>` + `[Remote]` operations |
| Child entity within aggregate | `EntityBase<T>` (no `[Remote]`) |
| Value object / lookup data | Simple POCO + `[Factory]` |
| Search criteria / form input | `ValidateBase<T>` |

> **Note:** Value Objects are simple POCO classes with `[Factory]`. [RemoteFactory](https://github.com/NeatooDotNet/RemoteFactory) generates fetch operations. See the [RemoteFactory documentation](https://github.com/NeatooDotNet/RemoteFactory/tree/main/docs) for implementation guidance.

### EntityBase&lt;T&gt;

Provides:
- Modification tracking (`IsModified`, `IsSelfModified`)
- Persistence lifecycle (`IsNew`, `IsDeleted`)
- Savability state (`IsSavable`)
- Child entity support (`IsChild`, automatic parent tracking)

<!-- snippet: docs:aggregates-and-entities:entitybase-basic -->
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
    public Order(IEntityBaseServices<Order> services) : base(services) { }

    public partial Guid Id { get; set; }
    public partial string? Status { get; set; }     // IsModified tracked automatically
    public partial decimal Total { get; set; }      // IsSavable updated on change

    [Create]
    public void Create()
    {
        Id = Guid.NewGuid();
        Status = "New";
    }
}
```
<!-- /snippet -->

### Value Objects (POCO + `[Factory]`)

Simple classes without Neatoo base class inheritance. RemoteFactory generates fetch operations via `[Fetch]` methods. No Insert/Update/Delete operations.

**Typical Use:** Lookup data, dropdown options, reference data.

<!-- snippet: docs:aggregates-and-entities:value-object -->
```csharp
/// <summary>
/// Value Object - simple POCO class with [Factory] attribute.
/// No Neatoo base class inheritance. RemoteFactory generates fetch operations.
/// Typical Use: Lookup data, dropdown options, reference data.
/// </summary>
public interface IStateProvince
{
    string? Code { get; set; }
    string? Name { get; set; }
}

[Factory]
internal partial class StateProvince : IStateProvince
{
    public string? Code { get; set; }
    public string? Name { get; set; }

    [Fetch]
    public void Fetch(StateProvinceDto dto)
    {
        Code = dto.Code;
        Name = dto.Name;
    }
}

// DTO for demonstration
public class StateProvinceDto
{
    public string? Code { get; set; }
    public string? Name { get; set; }
}
```
<!-- /snippet -->

### Validated Non-Persisted Objects (ValidateBase)

Use `ValidateBase<T>` for objects that need validation but are NOT persisted. Common use cases include:
- **Criteria objects** for search/filter operations
- **Form input objects** that validate user input before creating entities
- **Configuration objects** that need validation

<!-- snippet: docs:aggregates-and-entities:validatebase-criteria -->
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

[Factory]
internal partial class PersonSearchCriteria : ValidateBase<PersonSearchCriteria>, IPersonSearchCriteria
{
    public PersonSearchCriteria(IValidateBaseServices<PersonSearchCriteria> services,
                                 IDateRangeSearchRule dateRangeRule) : base(services)
    {
        // Add custom date range validation rule
        RuleManager.AddRule(dateRangeRule);
    }

    [Required(ErrorMessage = "At least one search term required")]
    public partial string? SearchTerm { get; set; }

    public partial DateTime? FromDate { get; set; }
    public partial DateTime? ToDate { get; set; }

    [Create]
    public void Create() { }
}
```
<!-- /snippet -->

## Hierarchy Constraints

**Critical Rule:** Entities must maintain a proper parent-child hierarchy for modification tracking to work correctly.

The hierarchy must maintain persistence tracking from root to leaves:

```
✅ VALID HIERARCHIES:

EntityBase (Aggregate Root)
    └── EntityBase (Child Entity)
            └── EntityBase (Grandchild Entity)

EntityBase (Aggregate Root)
    └── Value Object (simple POCO - leaf only, no modification tracking)


❌ INVALID HIERARCHY:

EntityBase (Aggregate Root)
    └── ValidateBase (non-persisted object)
            └── EntityBase  ← NOT ALLOWED!
```

### Why This Constraint Exists

- `ValidateBase<T>` does not track modifications or manage persistence lifecycle
- If an `EntityBase<T>` is nested under a non-Entity, its changes cannot propagate up
- The aggregate root would not know the grandchild entity was modified
- The `IsSavable` and `IsModified` state would be incorrect

### When to Use Each Pattern

| Scenario | Pattern |
|----------|---------|
| Editable child data that persists | `EntityBase` under `EntityBase` |
| Reference/lookup data | Value Object (simple POCO) |
| Search/filter criteria with validation | `ValidateBase` (no persistence tracking) |
| Form input validation before entity creation | `ValidateBase` |

## Defining an Aggregate Root

### Interface Requirement

Every aggregate requires a public interface. This enables factory generation and client-server transfer:

<!-- snippet: docs:aggregates-and-entities:interface-requirement -->
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

### Aggregate Root Class

<!-- snippet: docs:aggregates-and-entities:aggregate-root-class -->
```csharp
/// <summary>
/// Complete aggregate root class pattern.
/// </summary>
[Factory]
internal partial class Customer : EntityBase<Customer>, ICustomer
{
    public Customer(IEntityBaseServices<Customer> services) : base(services) { }

    // Properties
    public partial Guid? Id { get; set; }
    public partial string? FirstName { get; set; }
    public partial string? LastName { get; set; }
    public partial string? Email { get; set; }

    // Child collection
    public partial ICustomerAddressList? AddressList { get; set; }

    [Create]
    public void Create()
    {
        Id = Guid.NewGuid();
    }
}

// Placeholder interface for child collection
public partial interface ICustomerAddressList : IEntityListBase<ICustomerAddress> { }
public partial interface ICustomerAddress : IEntityBase { }
```
<!-- /snippet -->

### Key Points

1. **`[Factory]` attribute** - Required for factory code generation
2. **`internal` visibility** - The concrete class is internal; the interface is public
3. **`partial` class** - Neatoo source generators extend the class
4. **`partial` properties** - Required for state tracking and serialization
5. **Constructor with services** - DI provides the entity services

## Partial Properties

Properties must be declared as `partial` for Neatoo to generate backing code:

<!-- snippet: docs:aggregates-and-entities:partial-properties -->
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

    // Correct - generates backing field with change tracking
    public partial string? FirstName { get; set; }
    public partial string? LastName { get; set; }

    #region docs:aggregates-and-entities:non-partial-properties
    // Calculated property - not tracked, not serialized
    public string FullName => $"{FirstName} {LastName}";

    // UI-only property - not transferred to server
    public bool IsExpanded { get; set; }
```
<!-- /snippet -->

### What Partial Properties Provide

- **Change tracking**: `IsModified` updates automatically
- **Rule triggering**: Validation rules execute on change
- **State serialization**: Values transfer between client and server
- **UI binding**: `INotifyPropertyChanged` notifications

### Non-Partial Properties

Use regular properties for:
- Calculated/derived values
- UI-only properties
- Server-only properties

<!-- snippet: docs:aggregates-and-entities:non-partial-properties -->
```csharp
// Calculated property - not tracked, not serialized
    public string FullName => $"{FirstName} {LastName}";

    // UI-only property - not transferred to server
    public bool IsExpanded { get; set; }
```
<!-- /snippet -->

## Child Entities

Child entities within an aggregate are marked as children automatically when added to a parent:

<!-- snippet: docs:aggregates-and-entities:child-entity -->
```csharp
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

    // Access parent through the Parent property
    public IContact? ParentContact => Parent as IContact;

    [Create]
    public void Create()
    {
        Id = Guid.NewGuid();
    }
}
```
<!-- /snippet -->

### Child Entity Characteristics

- **`IsChild = true`** - Set automatically when added to a parent
- **Cannot save independently** - `IsSavable` is always false for children
- **Saved through aggregate root** - Parent's Insert/Update handles children

## Entity State Properties

EntityBase provides properties for tracking entity lifecycle:

| Property | Type | Description |
|----------|------|-------------|
| `IsNew` | bool | Entity has not been persisted (triggers Insert) |
| `IsModified` | bool | Entity or any child has changes |
| `IsSelfModified` | bool | This entity's properties changed |
| `IsDeleted` | bool | Entity is marked for deletion |
| `IsChild` | bool | Entity is part of a parent aggregate |
| `IsSavable` | bool | Can be saved (modified, valid, not busy, not child) |
| `IsValid` | bool | All validation rules pass |
| `IsBusy` | bool | Async operations in progress |

## Data Annotations

Use standard data annotations for display and basic validation:

<!-- snippet: docs:aggregates-and-entities:data-annotations -->
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

    [DisplayName("First Name*")]
    [Required(ErrorMessage = "First Name is required")]
    public partial string? FirstName { get; set; }

    [DisplayName("Email Address")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public partial string? Email { get; set; }

    [Create]
    public void Create() { }
}
```
<!-- /snippet -->

Neatoo converts these to validation rules automatically.

## Authorization

> **Note:** Authorization is provided by [RemoteFactory](https://github.com/NeatooDotNet/RemoteFactory). See the [RemoteFactory authorization documentation](https://github.com/NeatooDotNet/RemoteFactory/tree/main/docs) for `[AuthorizeFactory<T>]` patterns and configuration.

## Aggregate Root vs Child Entity Pattern

### Aggregate Root

<!-- snippet: docs:aggregates-and-entities:aggregate-root-pattern -->
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

    // [Remote] - Called from UI
    [Remote]
    [Fetch]
    public void Fetch(Guid id)
    {
        // In real implementation:
        // var entity = await db.Orders.Include(o => o.LineItems).FirstOrDefaultAsync(o => o.Id == id);
        // MapFrom(entity);
        // LineItems = lineItemListFactory.Fetch(entity.LineItems);
        Id = id;
    }

    [Remote]
    [Insert]
    public async Task Insert()
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
```
<!-- /snippet -->

### Child Entity

<!-- snippet: docs:aggregates-and-entities:child-entity-pattern -->
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

    // No [Remote] - called internally by parent
    [Fetch]
    public void Fetch(OrderLineItemDto dto)
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
```
<!-- /snippet -->

## Complete Example

<!-- snippet: docs:aggregates-and-entities:complete-example -->
```csharp
/// <summary>
/// Complete aggregate root example showing all key patterns.
/// </summary>
public partial interface IPerson : IEntityBase
{
    Guid? Id { get; set; }
    string? FirstName { get; set; }
    string? LastName { get; set; }
    IPersonPhoneList PersonPhoneList { get; }
}

[Factory]
internal partial class Person : EntityBase<Person>, IPerson
{
    public Person(IEntityBaseServices<Person> services) : base(services) { }

    public partial Guid? Id { get; set; }

    [DisplayName("First Name*")]
    [Required(ErrorMessage = "First Name is required")]
    public partial string? FirstName { get; set; }

    [DisplayName("Last Name*")]
    [Required(ErrorMessage = "Last Name is required")]
    public partial string? LastName { get; set; }

    public partial IPersonPhoneList PersonPhoneList { get; set; }

    // Mapper declarations - MapModifiedTo is source-generated
    public void MapFrom(PersonEntity entity)
    {
        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
    }

    public void MapTo(PersonEntity entity)
    {
        entity.Id = Id;
        entity.FirstName = FirstName;
        entity.LastName = LastName;
    }

    public partial void MapModifiedTo(PersonEntity entity);

    [Create]
    public void Create([Service] IPersonPhoneListFactory phoneListFactory)
    {
        PersonPhoneList = phoneListFactory.Create();
    }

    [Fetch]
    public void Fetch(PersonEntity entity, [Service] IPersonPhoneListFactory phoneListFactory)
    {
        MapFrom(entity);
        PersonPhoneList = phoneListFactory.Fetch(entity.Phones);
    }

    [Insert]
    public Task Insert()
    {
        // In real code: create entity, MapTo, save to database
        Id = Guid.NewGuid();
        var entity = new PersonEntity();
        MapTo(entity);
        // db.Persons.Add(entity);
        // phoneListFactory.Save(PersonPhoneList, entity.Phones);
        // await db.SaveChangesAsync();
        return Task.CompletedTask;
    }

    [Update]
    public Task Update()
    {
        // In real code: fetch entity, MapModifiedTo, save changes
        // var entity = await db.Persons.FindAsync(Id);
        // MapModifiedTo(entity);
        // phoneListFactory.Save(PersonPhoneList, entity.Phones);
        // await db.SaveChangesAsync();
        return Task.CompletedTask;
    }

    [Delete]
    public Task Delete()
    {
        // In real code: delete from database
        // await db.Persons.Where(p => p.Id == Id).ExecuteDeleteAsync();
        return Task.CompletedTask;
    }
}

// EF Entity for data access
public class PersonEntity
{
    public Guid? Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public ICollection<PersonPhoneEntity> Phones { get; set; } = new List<PersonPhoneEntity>();
}

public class PersonPhoneEntity
{
    public Guid? Id { get; set; }
    public string? PhoneNumber { get; set; }
}

// Child collection
public partial interface IPersonPhoneList : IEntityListBase<IPersonPhone> { }

public partial interface IPersonPhone : IEntityBase
{
    Guid? Id { get; set; }
    string? PhoneNumber { get; set; }
}

[Factory]
internal partial class PersonPhone : EntityBase<PersonPhone>, IPersonPhone
{
    public PersonPhone(IEntityBaseServices<PersonPhone> services) : base(services) { }

    public partial Guid? Id { get; set; }
    public partial string? PhoneNumber { get; set; }

    [Create]
    public void Create() { }

    [Fetch]
    public void Fetch(PersonPhoneEntity entity)
    {
        Id = entity.Id;
        PhoneNumber = entity.PhoneNumber;
    }
}

[Factory]
internal class PersonPhoneList : EntityListBase<IPersonPhone>, IPersonPhoneList
{
    private readonly IPersonPhoneFactory _phoneFactory;

    public PersonPhoneList([Service] IPersonPhoneFactory phoneFactory)
    {
        _phoneFactory = phoneFactory;
    }

    [Create]
    public void Create() { }

    [Fetch]
    public void Fetch(IEnumerable<PersonPhoneEntity> entities)
    {
        foreach (var entity in entities)
        {
            Add(_phoneFactory.Fetch(entity));
        }
    }
}
```
<!-- /snippet -->

## See Also

- [Validation and Rules](validation-and-rules.md) - Adding business rules
- [Factory Operations](factory-operations.md) - Complete factory lifecycle
- [Collections](collections.md) - Child entity collections
