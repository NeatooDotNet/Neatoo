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

```csharp
[Factory]
internal partial class Order : EntityBase<Order>, IOrder
{
    public partial Guid Id { get; set; }
    public partial string Status { get; set; }     // IsModified tracked automatically
    public partial decimal Total { get; set; }     // IsSavable updated on change
}
```

### Value Objects (POCO + `[Factory]`)

Simple classes without Neatoo base class inheritance. RemoteFactory generates fetch operations via `[Fetch]` methods. No Insert/Update/Delete operations.

**Typical Use:** Lookup data, dropdown options, reference data.

```csharp
[Factory]
internal partial class StateProvince : IStateProvince
{
    public string Code { get; set; }
    public string Name { get; set; }

    [Fetch]
    public void Fetch(StateProvinceEntity entity)
    {
        Code = entity.Code;
        Name = entity.Name;
    }
}
```

### Validated Non-Persisted Objects (ValidateBase)

Use `ValidateBase<T>` for objects that need validation but are NOT persisted. Common use cases include:
- **Criteria objects** for search/filter operations
- **Form input objects** that validate user input before creating entities
- **Configuration objects** that need validation

```csharp
// Criteria object - has validation but no persistence
[Factory]
internal partial class PersonSearchCriteria : ValidateBase<PersonSearchCriteria>, IPersonSearchCriteria
{
    [Required(ErrorMessage = "At least one search term required")]
    public partial string? SearchTerm { get; set; }

    public partial DateTime? FromDate { get; set; }
    public partial DateTime? ToDate { get; set; }

    // Custom validation rule
    public PersonSearchCriteria(IValidateBaseServices<PersonSearchCriteria> services) : base(services)
    {
        RuleManager.AddValidation(
            t => t.FromDate > t.ToDate ? "From date must be before To date" : "",
            t => t.FromDate, t => t.ToDate);
    }
}
```

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

### Practical Example

```csharp
// ✅ CORRECT: Entity contains Entity
[Factory]
internal partial class Order : EntityBase<Order>, IOrder
{
    public partial IOrderLineItemList LineItems { get; set; }  // EntityListBase
}

// ✅ CORRECT: Entity references Value Object (simple POCO)
[Factory]
internal partial class Order : EntityBase<Order>, IOrder
{
    public partial IShippingInfo ShippingInfo { get; set; }  // Simple POCO - read-only
}

// ✅ CORRECT: ValidateBase for search criteria (not persisted)
[Factory]
internal partial class OrderSearchCriteria : ValidateBase<OrderSearchCriteria>, IOrderSearchCriteria
{
    public partial string? CustomerName { get; set; }
    public partial DateTime? OrderDate { get; set; }
}
```

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

> **Note:** Authorization is provided by [RemoteFactory](https://github.com/NeatooDotNet/RemoteFactory). For comprehensive authorization patterns and configuration, see the [RemoteFactory documentation](https://github.com/NeatooDotNet/RemoteFactory/tree/main/docs).

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
