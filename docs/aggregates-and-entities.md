# Aggregates and Entities

This document covers creating domain model classes using Neatoo's base classes.

## Class Hierarchy Overview

```
Base<T>                      - Property management, parent-child relationships
    |
ValidateBase<T>              - Validation rules, property messages, validity tracking
    |
EntityBase<T>                - Identity, modification tracking, persistence lifecycle
```

## Entities vs Value Objects

In DDD terms, Neatoo maps to:

| DDD Concept | Neatoo Base Class | Characteristics |
|-------------|-------------------|-----------------|
| **Entity** | `EntityBase<T>` | Has identity, mutable, tracks modifications, persisted |
| **Value Object** | `Base<T>` | No identity, read-only after creation, immutable |
| **Validated Value Object** | `ValidateBase<T>` | Value object with validation rules |

### Entities (EntityBase)

Use `EntityBase<T>` for domain objects that:
- Have a unique identity (Id)
- Can be modified after creation
- Track their own modification state
- Are persisted to a database

```csharp
// Entity - has identity, editable, tracks changes
[Factory]
internal partial class Order : EntityBase<Order>, IOrder
{
    public partial Guid Id { get; set; }           // Identity
    public partial string Status { get; set; }     // Mutable - IsModified tracked
    public partial decimal Total { get; set; }     // Mutable - IsModified tracked
}
```

### Value Objects (Base)

Use `Base<T>` for domain objects that:
- Are defined by their attributes, not identity
- Are read-only after creation (fetch only)
- Do not track modifications
- Represent lookup data, reference data, or immutable snapshots

```csharp
// Value Object - read-only, no modification tracking
[Factory]
internal partial class StateProvince : Base<StateProvince>, IStateProvince
{
    public partial string Code { get; set; }       // Set during Fetch only
    public partial string Name { get; set; }       // Set during Fetch only

    [Fetch]
    public void Fetch(StateProvinceEntity entity)
    {
        Code = entity.Code;
        Name = entity.Name;
    }

    // No Insert, Update, Delete - read-only
}
```

### Validated Value Objects (ValidateBase)

Use `ValidateBase<T>` when you need validation on a value object:

```csharp
// Validated Value Object - has validation but no persistence tracking
[Factory]
internal partial class Address : ValidateBase<Address>, IAddress
{
    [Required]
    public partial string Street { get; set; }

    [Required]
    public partial string City { get; set; }

    public partial IStateProvince State { get; set; }
}
```

## Hierarchy Constraints

**Critical Rule:** You cannot nest an Entity under a Value Object.

The hierarchy must maintain persistence tracking from root to leaves:

```
✅ VALID HIERARCHIES:

EntityBase (Aggregate Root)
    └── EntityBase (Child Entity)
            └── EntityBase (Grandchild Entity)

EntityBase (Aggregate Root)
    └── Base (Value Object child - leaf only)

Base (Value Object Root)
    └── Base (Value Object child)


❌ INVALID HIERARCHY:

EntityBase (Aggregate Root)
    └── Base (Value Object)
            └── EntityBase  ← NOT ALLOWED!
```

### Why This Constraint Exists

- `Base<T>` does not track modifications or manage persistence lifecycle
- If an `EntityBase<T>` is nested under `Base<T>`, its changes cannot propagate up
- The aggregate root would not know the grandchild entity was modified
- The `IsSavable` and `IsModified` state would be incorrect

### Practical Example

```csharp
// ✅ CORRECT: Entity contains Entity
[Factory]
internal partial class Order : EntityBase<Order>, IOrder
{
    public partial IOrderLineItemList LineItems { get; set; }  // EntityListBase
}

// ✅ CORRECT: Entity contains Value Object (leaf)
[Factory]
internal partial class Order : EntityBase<Order>, IOrder
{
    public partial IShippingInfo ShippingInfo { get; set; }  // Base - read-only
}

// ❌ WRONG: Value Object containing Entity
[Factory]
internal partial class ShippingInfo : Base<ShippingInfo>, IShippingInfo
{
    // DON'T DO THIS - Entity under Value Object breaks modification tracking
    public partial IAddress Address { get; set; }  // If Address is EntityBase, this is wrong!
}
```

### When to Use Each Pattern

| Scenario | Pattern |
|----------|---------|
| Editable child data | `EntityBase` under `EntityBase` |
| Reference/lookup data | `Base` as leaf under `EntityBase` |
| Read-only snapshot | `Base` under `Base` |
| Editable with validation only | `ValidateBase` (no persistence tracking) |

## Defining an Aggregate Root

### Interface Requirement

Every aggregate requires a public interface. This enables factory generation and client-server transfer:

```csharp
public partial interface IPerson : IEntityBase
{
    // Properties are auto-generated from the partial class
}
```

### Aggregate Root Class

```csharp
using Neatoo;
using Neatoo.RemoteFactory;

[Factory]
internal partial class Person : EntityBase<Person>, IPerson
{
    public Person(IEntityBaseServices<Person> services) : base(services) { }

    // Properties
    public partial Guid? Id { get; set; }
    public partial string? FirstName { get; set; }
    public partial string? LastName { get; set; }
    public partial string? Email { get; set; }

    // Child collection
    public partial IPersonPhoneList PersonPhoneList { get; set; }
}
```

### Key Points

1. **`[Factory]` attribute** - Required for factory code generation
2. **`internal` visibility** - The concrete class is internal; the interface is public
3. **`partial` class** - Neatoo source generators extend the class
4. **`partial` properties** - Required for state tracking and serialization
5. **Constructor with services** - DI provides the entity services

## Partial Properties

Properties must be declared as `partial` for Neatoo to generate backing code:

```csharp
// Correct - generates backing field with change tracking
public partial string? Name { get; set; }

// Incorrect - no state tracking, won't serialize properly
public string? Name { get; set; }
```

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

```csharp
// Calculated property - not tracked
public string FullName => $"{FirstName} {LastName}";

// UI-only property - not transferred to server
public bool IsExpanded { get; set; }
```

## Child Entities

Child entities within an aggregate are marked as children automatically when added to a parent:

```csharp
public partial interface IPersonPhone : IEntityBase
{
    internal IPerson? ParentPerson { get; }
}

[Factory]
internal partial class PersonPhone : EntityBase<PersonPhone>, IPersonPhone
{
    public PersonPhone(IEntityBaseServices<PersonPhone> services) : base(services) { }

    public partial Guid? Id { get; set; }
    public partial PhoneType? PhoneType { get; set; }
    public partial string? PhoneNumber { get; set; }

    // Access parent through the Parent property
    public IPerson? ParentPerson => Parent as IPerson;
}
```

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

```csharp
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

[DisplayName("First Name*")]
[Required(ErrorMessage = "First Name is required")]
public partial string? FirstName { get; set; }

[DisplayName("Email Address")]
[EmailAddress(ErrorMessage = "Invalid email format")]
public partial string? Email { get; set; }
```

Neatoo converts these to validation rules automatically.

## Authorization

Use `[AuthorizeFactory<T>]` to add authorization checks:

```csharp
public interface IPersonAuth
{
    bool CanCreate();
    bool CanFetch();
    bool CanInsert();
    bool CanUpdate();
    bool CanDelete();
}

[Factory]
[AuthorizeFactory<IPersonAuth>]
internal partial class Person : EntityBase<Person>, IPerson
{
    // ...
}
```

The generated factory calls these methods before operations. Authorization methods are also available on the factory for UI permission display.

## Aggregate Root vs Child Entity Pattern

### Aggregate Root

```csharp
[Factory]
internal partial class Order : EntityBase<Order>, IOrder
{
    // [Remote] - Called from UI
    [Remote]
    [Fetch]
    public async Task Fetch(int id, [Service] IOrderDbContext db)
    {
        var entity = await db.Orders.Include(o => o.LineItems).FirstOrDefaultAsync(o => o.Id == id);
        if (entity != null)
        {
            MapFrom(entity);
            LineItems = lineItemListFactory.Fetch(entity.LineItems);
        }
    }

    [Remote]
    [Insert]
    public async Task Insert([Service] IOrderDbContext db)
    {
        await RunRules();
        if (!IsSavable) return;

        var entity = new OrderEntity();
        MapTo(entity);
        lineItemListFactory.Save(LineItems, entity.LineItems);
        db.Orders.Add(entity);
        await db.SaveChangesAsync();
    }
}
```

### Child Entity

```csharp
[Factory]
internal partial class OrderLineItem : EntityBase<OrderLineItem>, IOrderLineItem
{
    // No [Remote] - called internally by parent
    [Fetch]
    public void Fetch(LineItemEntity entity)
    {
        MapFrom(entity);
    }

    [Insert]
    public void Insert(LineItemEntity entity)
    {
        MapTo(entity);
    }

    [Update]
    public void Update(LineItemEntity entity)
    {
        MapModifiedTo(entity);
    }
}
```

## Complete Example

```csharp
// Aggregate Root
public partial interface IPerson : IEntityBase { }

[Factory]
[AuthorizeFactory<IPersonAuth>]
internal partial class Person : EntityBase<Person>, IPerson
{
    public Person(IEntityBaseServices<Person> services,
                  IUniqueNameRule uniqueNameRule) : base(services)
    {
        RuleManager.AddRule(uniqueNameRule);
    }

    public partial Guid? Id { get; set; }

    [DisplayName("First Name*")]
    [Required(ErrorMessage = "First Name is required")]
    public partial string? FirstName { get; set; }

    [DisplayName("Last Name*")]
    [Required(ErrorMessage = "Last Name is required")]
    public partial string? LastName { get; set; }

    public partial IPersonPhoneList PersonPhoneList { get; set; }

    // Mapper declarations - implementations are source-generated
    public partial void MapFrom(PersonEntity personEntity);
    public partial void MapTo(PersonEntity personEntity);
    public partial void MapModifiedTo(PersonEntity personEntity);

    [Create]
    public void Create([Service] IPersonPhoneList personPhoneList)
    {
        PersonPhoneList = personPhoneList;
    }

    [Remote]
    [Fetch]
    public async Task<bool> Fetch([Service] IPersonDbContext db,
                                   [Service] IPersonPhoneListFactory phoneListFactory)
    {
        var entity = await db.FindPerson();
        if (entity == null) return false;

        MapFrom(entity);
        PersonPhoneList = phoneListFactory.Fetch(entity.Phones);
        return true;
    }

    [Remote]
    [Insert]
    public async Task Insert([Service] IPersonDbContext db,
                             [Service] IPersonPhoneListFactory phoneListFactory)
    {
        await RunRules();
        if (!IsSavable) return;

        Id = Guid.NewGuid();
        var entity = new PersonEntity();
        MapTo(entity);
        db.AddPerson(entity);
        phoneListFactory.Save(PersonPhoneList, entity.Phones);
        await db.SaveChangesAsync();
    }

    [Remote]
    [Update]
    public async Task Update([Service] IPersonDbContext db,
                             [Service] IPersonPhoneListFactory phoneListFactory)
    {
        await RunRules();
        if (!IsSavable) return;

        var entity = await db.FindPerson(Id);
        if (entity == null) throw new KeyNotFoundException("Person not found");

        MapModifiedTo(entity);
        phoneListFactory.Save(PersonPhoneList, entity.Phones);
        await db.SaveChangesAsync();
    }

    [Remote]
    [Delete]
    public async Task Delete([Service] IPersonDbContext db)
    {
        await db.DeletePerson(Id);
        await db.SaveChangesAsync();
    }
}
```

## See Also

- [Validation and Rules](validation-and-rules.md) - Adding business rules
- [Factory Operations](factory-operations.md) - Complete factory lifecycle
- [Collections](collections.md) - Child entity collections
