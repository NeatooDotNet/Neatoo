# Aggregates and Entities

This document covers creating domain model classes using Neatoo's base classes.

## Class Hierarchy Overview

Users typically inherit from `ValidateBase<T>` or `EntityBase<T>`:

```
ValidateBase<T>              - Foundation for validated objects (criteria, filters)
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

EntityBase provides properties for tracking entity lifecycle. All are bindable for UI:

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
| `Parent` | IValidateBase? | Immediate parent in object graph |
| `Root` | IValidateBase? | Aggregate root (null if this IS the root) |

State tracking is automatic for `partial` properties:

<!-- snippet: docs:aggregates-and-entities:state-tracking-properties -->
```csharp
public partial Guid Id { get; set; }
    public partial string? Status { get; set; }     // IsModified tracked automatically
    public partial decimal Total { get; set; }      // IsSavable updated on change
```
<!-- /snippet -->

Inline validation rules can be added in the constructor:

<!-- snippet: docs:aggregates-and-entities:inline-validation-rule -->
```csharp
// Inline validation rule - Total must be positive
        RuleManager.AddValidation(
            t => t.Total <= 0 ? "Total must be greater than zero" : "",
            t => t.Total);
```
<!-- /snippet -->

### Value Objects (POCO + `[Factory]`)

Simple classes without Neatoo base class inheritance. RemoteFactory generates fetch operations via `[Fetch]` methods. No Insert/Update/Delete operations.

**Typical Use:** Lookup data, dropdown options, reference data.

Class declaration - no Neatoo base class:

<!-- snippet: docs:aggregates-and-entities:value-object-declaration -->
```csharp
[Factory]
internal partial class StateProvince : IStateProvince
```
<!-- /snippet -->

Standard properties (no `partial` keyword needed):

<!-- snippet: docs:aggregates-and-entities:value-object-properties -->
```csharp
public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
```
<!-- /snippet -->

Fetch method populates the object:

<!-- snippet: docs:aggregates-and-entities:value-object-fetch -->
```csharp
[Fetch]
    public void Fetch(string code, string name)
    {
        Code = code;
        Name = name;
    }
```
<!-- /snippet -->

### Validated Non-Persisted Objects (ValidateBase)

Use `ValidateBase<T>` for objects that need validation but are NOT persisted. Common use cases include:
- **Criteria objects** for search/filter operations
- **Form input objects** that validate user input before creating entities
- **Configuration objects** that need validation

Class declaration inherits from `ValidateBase<T>`:

<!-- snippet: docs:aggregates-and-entities:validatebase-declaration -->
```csharp
[Factory]
internal partial class PersonSearchCriteria : ValidateBase<PersonSearchCriteria>, IPersonSearchCriteria
```
<!-- /snippet -->

Cross-property validation using inline rules:

<!-- snippet: docs:aggregates-and-entities:criteria-inline-rule -->
```csharp
// Inline date range validation - validates when either date changes
        RuleManager.AddValidation(
            t => t.FromDate.HasValue && t.ToDate.HasValue && t.FromDate > t.ToDate
                ? "From date must be before To date" : "",
            t => t.FromDate);

        RuleManager.AddValidation(
            t => t.FromDate.HasValue && t.ToDate.HasValue && t.FromDate > t.ToDate
                ? "To date must be after From date" : "",
            t => t.ToDate);
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

### Interface Pattern

Interfaces are strongly recommended for Neatoo aggregates. While RemoteFactory supports concrete classes, interfaces provide:

- **Unit testability** - Mock dependencies and test entities in isolation
- **Client-server transfer** - Public interface enables factory generation
- **Minimal overhead** - Interface members are auto-generated from partial properties

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

Class declaration with `[Factory]` attribute:

<!-- snippet: docs:aggregates-and-entities:class-declaration -->
```csharp
[Factory]
internal partial class Customer : EntityBase<Customer>, ICustomer
```
<!-- /snippet -->

Constructor pattern - DI provides entity services:

<!-- snippet: docs:aggregates-and-entities:entity-constructor -->
```csharp
public Customer(IEntityBaseServices<Customer> services) : base(services) { }
```
<!-- /snippet -->

### Key Points

1. **`[Factory]` attribute** - Required for factory code generation
2. **`internal` visibility** - The concrete class is internal; the interface is public
3. **`partial` class** - Neatoo source generators extend the class
4. **`partial` properties** - Required for state tracking and serialization
5. **Constructor with services** - DI provides the entity services

## Dependency Injection Patterns

Neatoo entities support two DI patterns, and choosing correctly affects memory usage and serialization. The key decision: **will this dependency be used more than once?**

| Pattern | When to Use | Why |
|---------|-------------|-----|
| Constructor injection | Dependency used repeatedly (rules, add methods) | Stored in field for reuse |
| `[Service]` parameter | Dependency used once in factory method | Not stored; cleaner serialization |

### Constructor Injection - For Ongoing Use

Constructor-inject dependencies needed throughout the entity's lifetime:

```csharp
public Person(
    IEntityBaseServices<Person> services,
    IEmailValidator emailValidator) : base(services)
{
    _emailValidator = emailValidator;

    // Validator used in rules - needed throughout lifetime
    RuleManager.AddRule(new EmailValidationRule(_emailValidator));
}
```

```csharp
// List needs factory for repeated AddItem() calls
public PhoneList([Service] IPhoneFactory phoneFactory)
{
    _phoneFactory = phoneFactory;
}

public IPhone AddPhoneNumber()
{
    var phone = _phoneFactory.Create();  // Called multiple times
    Add(phone);
    return phone;
}
```

### [Service] Injection - For Factory Methods

Use `[Service]` for dependencies only needed during `[Create]`, `[Fetch]`, etc. This keeps the entity lighter—dependencies aren't stored as fields and don't need to be serialized during client-server transfer.

```csharp
public Person(IEntityBaseServices<Person> services) : base(services)
{
    // No child list factory stored - only needed once in Create
}

[Create]
public void Create([Service] IPersonPhoneListFactory phoneListFactory)
{
    PersonPhoneList = phoneListFactory.Create();  // One-time initialization
}
```

### Preferred Pattern: Inject into Class, Not Method Parameters

Avoid requiring callers to provide factories as method parameters. DI handles this automatically:

```csharp
// Avoid - forces caller to inject and pass factory
public IOrderLine AddLine(IOrderLineFactory lineFactory)
{
    var line = lineFactory.Create();
    Add(line);
    return line;
}

// Preferred - constructor-injected factory
public IOrderLine AddLine()
{
    var line = _lineFactory.Create();  // _lineFactory from constructor
    Add(line);
    return line;
}
```

## Partial Properties

Properties must be declared as `partial` for Neatoo to generate backing code:

<!-- snippet: docs:aggregates-and-entities:partial-property-declaration -->
```csharp
// Correct - generates backing field with change tracking
    public partial string? FirstName { get; set; }
    public partial string? LastName { get; set; }
```
<!-- /snippet -->

### What Partial Properties Provide

- **Change tracking**: `IsModified` updates automatically
- **Rule triggering**: Validation rules execute on change
- **State serialization**: Values transfer between client and server
- **UI binding**: `INotifyPropertyChanged` notifications

### Non-Partial Properties

Use regular properties for calculated, UI-only, or server-only values:

<!-- snippet: docs:aggregates-and-entities:non-partial-properties -->
```csharp
// Calculated property - not tracked, not serialized
    public string FullName => $"{FirstName} {LastName}";

    // UI-only property - not transferred to server
    public bool IsExpanded { get; set; }
```
<!-- /snippet -->

## Child Entities

Child entities within an aggregate are marked as children automatically when added to a parent.

Access the parent aggregate from a child entity:

<!-- snippet: docs:aggregates-and-entities:parent-access-property -->
```csharp
// Access parent through the Parent property
    public IContact? ParentContact => Parent as IContact;
```
<!-- /snippet -->

### Child Entity Characteristics

- **`IsChild = true`** - Set automatically when added to a parent
- **Cannot save independently** - `IsSavable` is always false for children
- **Saved through aggregate root** - Parent's Insert/Update handles children

### Root Property and Aggregate Boundaries

The `Root` property identifies the aggregate root for any entity in the hierarchy:

```csharp
var order = await orderFactory.Create();
var line = await order.Lines.AddLine();

order.Root    // null (it IS the root)
line.Root     // order
line.Parent   // order (immediate parent)
```

**Cross-Aggregate Enforcement:** Neatoo prevents adding an entity from one aggregate to another:

```csharp
var order1 = await orderFactory.Create();
var order2 = await orderFactory.Create();

var line = await order1.Lines.AddLine();

// Attempt to add to different aggregate
order2.Lines.Add(line);  // THROWS InvalidOperationException
```

| Scenario | item.Root | list.Root | Result |
|----------|-----------|-----------|--------|
| Add brand new item | null | Order | Allowed |
| Add from same aggregate | Order | Order | Allowed |
| Add from different aggregate | Order1 | Order2 | **Throws** |

### Adding Items to Entity Lists

When adding items to an `EntityListBase<T>`, Neatoo enforces constraints and manages state automatically.

**Constraints (throws `InvalidOperationException`):**

| Constraint | Reason |
|------------|--------|
| Null item | `ArgumentNullException` - null items not allowed |
| Duplicate item | Item already in this list |
| Busy item | Item has async rules running (`IsBusy = true`) |
| Cross-aggregate | Item belongs to a different aggregate root |

**State Changes on Add:**

| State | Behavior |
|-------|----------|
| `Parent` | Set to list's parent (aggregate root) |
| `IsChild` | Set to `true` |
| `IsDeleted` | If `true`, `UnDelete()` called automatically |
| `IsModified` | Existing items (`IsNew = false`) marked modified |
| `ContainingList` | Set to this list (internal tracking) |

**Intra-Aggregate Moves:**

Items can be moved between lists within the same aggregate:

```csharp
var order = await orderFactory.Create();
var line = await order.ActiveLines.AddLine();

// Move to a different list in the same aggregate
order.ActiveLines.Remove(line);
order.ArchivedLines.Add(line);  // Allowed - same Root
```

When an existing item is re-added (after removal), Neatoo automatically:
1. Removes it from the previous list's `DeletedList`
2. Clears the `IsDeleted` flag via `UnDelete()`
3. Updates `ContainingList` to the new list

**Paused Mode (Deserialization):**

During factory operations (`FactoryStart`/`FactoryComplete`), constraints are relaxed:
- Duplicate check skipped (trusted source)
- Busy check skipped
- Cross-aggregate check skipped
- `IsChild` not set (factory handles this)

## Data Annotations

Data annotations provide display metadata and basic validation. For comprehensive coverage, see [Validation and Rules](validation-and-rules.md).

Combine `[DisplayName]` and `[Required]` for labeled required fields:

<!-- snippet: docs:aggregates-and-entities:displayname-required -->
```csharp
[DisplayName("First Name*")]
    [Required(ErrorMessage = "First Name is required")]
    public partial string? FirstName { get; set; }
```
<!-- /snippet -->

Format validation with `[EmailAddress]`:

<!-- snippet: docs:aggregates-and-entities:emailaddress-validation -->
```csharp
[DisplayName("Email Address")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public partial string? Email { get; set; }
```
<!-- /snippet -->

Neatoo converts these to validation rules automatically.

## Authorization

> **Note:** Authorization is provided by [RemoteFactory](https://github.com/NeatooDotNet/RemoteFactory). See the [RemoteFactory authorization documentation](https://github.com/NeatooDotNet/RemoteFactory/tree/main/docs) for `[AuthorizeFactory<T>]` patterns and configuration.

## The [Remote] Attribute

The `[Remote]` attribute determines whether a factory operation executes on the server or locally:

| Entity Type | Factory Methods | Why |
|-------------|-----------------|-----|
| **Aggregate Root** | `[Remote] [Fetch]`, `[Remote] [Insert]`, etc. | Called from UI, executes on server |
| **Child Entity** | `[Fetch]`, `[Insert]`, etc. (no `[Remote]`) | Called by parent, parent handles server communication |

Aggregate root - `[Remote]` operations called from UI:

<!-- snippet: docs:aggregates-and-entities:remote-fetch -->
```csharp
// [Remote] - Called from UI
    [Remote]
    [Fetch]
    public void Fetch(Guid id)
```
<!-- /snippet -->

<!-- snippet: docs:aggregates-and-entities:remote-insert -->
```csharp
[Remote]
    [Insert]
    public async Task Insert()
```
<!-- /snippet -->

Child entity - no `[Remote]`, parent calls internally:

<!-- snippet: docs:aggregates-and-entities:child-fetch-no-remote -->
```csharp
// No [Remote] - called internally by parent
    [Fetch]
    public void Fetch(OrderLineItemDto dto)
```
<!-- /snippet -->

The aggregate root's `[Remote]` methods handle all database operations, including saving child entities.

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
