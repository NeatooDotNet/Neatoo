# Creating Entities

How to create aggregates, entities, and value objects with Neatoo.

## The Basic Pattern

Every Neatoo entity requires:
1. **Public interface** extending `IEntityBase` or `IValidateBase`
2. **Internal partial class** with `[Factory]` attribute
3. **Constructor** accepting services and calling base
4. **Partial properties** for tracked state
5. **At least one factory method** (typically `[Create]`)

```csharp
// 1. Public interface
public partial interface IProduct : IEntityBase
{
    Guid Id { get; }
    string? Name { get; set; }
    decimal Price { get; set; }
}

// 2. Internal partial class with [Factory]
[Factory]
internal partial class Product : EntityBase<Product>, IProduct
{
    // 3. Constructor with services
    public Product(IEntityBaseServices<Product> services) : base(services) { }

    // 4. Partial properties
    public partial Guid Id { get; set; }
    public partial string? Name { get; set; }
    public partial decimal Price { get; set; }

    // 5. Factory method
    [Create]
    public void Create()
    {
        Id = Guid.NewGuid();
    }
}
```

## Choosing a Base Class

| Base Class | Use When | Key Capabilities |
|------------|----------|------------------|
| `EntityBase<T>` | Entity persists to database | IsNew, IsModified, IsDeleted, Save() |
| `ValidateBase<T>` | Object needs validation but not persistence | IsValid, rules, no persistence tracking |
| `EntityListBase<I>` | Collection of child entities | DeletedList, parent-child relationships |
| `ValidateListBase<I>` | Collection with validation only | Validation aggregation, no persistence |

### EntityBase - For Persisted Entities

Use for domain entities that map to database tables.

```csharp
public partial interface IOrder : IEntityBase
{
    Guid? Id { get; }
    string? CustomerName { get; set; }
    DateTime OrderDate { get; set; }
}

[Factory]
internal partial class Order : EntityBase<Order>, IOrder
{
    public Order(IEntityBaseServices<Order> services) : base(services) { }

    public partial Guid? Id { get; set; }
    public partial string? CustomerName { get; set; }
    public partial DateTime OrderDate { get; set; }

    [Create]
    public void Create()
    {
        OrderDate = DateTime.UtcNow;
    }

    [Remote][Fetch]
    public async Task Fetch(Guid id, [Service] IDbContext db)
    {
        var entity = await db.Orders.FindAsync(id);
        if (entity != null) MapFrom(entity);
    }

    [Remote][Insert]
    public async Task Insert([Service] IDbContext db)
    {
        var entity = new OrderEntity();
        MapTo(entity);
        db.Orders.Add(entity);
        await db.SaveChangesAsync();
        Id = entity.Id;
    }

    [Remote][Update]
    public async Task Update([Service] IDbContext db)
    {
        var entity = await db.Orders.FindAsync(Id);
        MapModifiedTo(entity);
        await db.SaveChangesAsync();
    }

    // Mapping methods
    public void MapFrom(OrderEntity e)
    {
        Id = e.Id;
        CustomerName = e.CustomerName;
        OrderDate = e.OrderDate;
    }

    public void MapTo(OrderEntity e)
    {
        e.CustomerName = CustomerName;
        e.OrderDate = OrderDate;
    }

    public partial void MapModifiedTo(OrderEntity e);  // Source-generated
}
```

### ValidateBase - For Non-Persisted Objects

Use for search criteria, DTOs, or any object that needs validation but not persistence.

```csharp
public partial interface ISearchCriteria : IValidateBase
{
    string? SearchTerm { get; set; }
    DateTime? FromDate { get; set; }
    DateTime? ToDate { get; set; }
}

[Factory]
internal partial class SearchCriteria : ValidateBase<SearchCriteria>, ISearchCriteria
{
    public SearchCriteria(IValidateBaseServices<SearchCriteria> services) : base(services)
    {
        // Add validation rule
        RuleManager.AddValidation(
            t => t.FromDate > t.ToDate ? "From date must be before To date" : "",
            t => t.FromDate, t => t.ToDate);
    }

    public partial string? SearchTerm { get; set; }
    public partial DateTime? FromDate { get; set; }
    public partial DateTime? ToDate { get; set; }

    [Create]
    public void Create() { }
}
```

## Nullable IDs for Database-Generated Keys

Use nullable types (`Guid?`, `int?`) for IDs when the database generates the value.

```csharp
public partial interface IOrder : IEntityBase
{
    Guid? Id { get; }  // null = not persisted, value = persisted
}

[Factory]
internal partial class Order : EntityBase<Order>, IOrder
{
    public partial Guid? Id { get; set; }

    [Create]
    public void Create()
    {
        // ID stays null - assigned during Insert
    }

    [Remote][Insert]
    public async Task Insert([Service] IDbContext db)
    {
        var entity = new OrderEntity();
        MapTo(entity);
        db.Orders.Add(entity);
        await db.SaveChangesAsync();
        Id = entity.Id;  // Database-generated ID assigned here
    }
}
```

**Why nullable?**
- `null` clearly means "not yet persisted"
- Avoids ambiguity of `Guid.Empty` or `0`
- After `Save()`, ID has a value

## Data Annotations on Properties

Use standard .NET data annotations for validation and display:

```csharp
[Factory]
internal partial class Contact : EntityBase<Contact>, IContact
{
    public Contact(IEntityBaseServices<Contact> services) : base(services) { }

    [DisplayName("First Name*")]
    [Required(ErrorMessage = "First name is required")]
    [StringLength(100)]
    public partial string? FirstName { get; set; }

    [DisplayName("Email Address")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public partial string? Email { get; set; }

    [Range(0, 150, ErrorMessage = "Age must be between 0 and 150")]
    public partial int? Age { get; set; }

    [Create]
    public void Create() { }
}
```

## Calculated Properties (Non-Partial)

Properties that derive from other values should NOT be partial:

```csharp
[Factory]
internal partial class Employee : EntityBase<Employee>, IEmployee
{
    public partial string? FirstName { get; set; }
    public partial string? LastName { get; set; }

    // NOT partial - calculated from other properties
    public string FullName => $"{FirstName} {LastName}";

    // NOT partial - UI-only state (not serialized)
    public bool IsExpanded { get; set; }
}
```

**Why non-partial?**
- Calculated properties don't need backing fields or change tracking
- UI-only properties shouldn't serialize to server

## Value Objects (Simple POCOs)

For immutable value objects with no Neatoo base class:

```csharp
public interface IStateProvince
{
    string Code { get; }
    string Name { get; }
}

[Factory]
internal partial class StateProvince : IStateProvince
{
    public string Code { get; private set; } = "";
    public string Name { get; private set; } = "";

    [Create]
    public void Create(string code, string name)
    {
        Code = code;
        Name = name;
    }
}
```

## Aggregate Root Pattern

An aggregate root is the entry point for a cluster of entities saved together.

```csharp
// Aggregate root
public partial interface IOrder : IEntityBase
{
    Guid? Id { get; }
    string? CustomerName { get; set; }
    IOrderLineList Lines { get; }  // Child collection
}

// Child entity (no [Remote] - accessed through parent)
public partial interface IOrderLine : IEntityBase
{
    Guid? Id { get; }
    string? ProductName { get; set; }
    int Quantity { get; set; }
}

// Child collection
public interface IOrderLineList : IEntityListBase<IOrderLine>
{
    IOrderLine AddLine();
}
```

**Key points:**
- Only aggregate root has `[Remote]` on factory methods
- Children don't have `[Remote]` - they're managed through parent
- Save the aggregate root - it saves all children

See [parent-child.md](parent-child.md) for collection implementation details.

## Common Mistakes

### Missing `partial` Keyword

```csharp
// WRONG - won't work
[Factory]
internal class Person : EntityBase<Person>, IPerson  // Missing partial!
{
    public string? Name { get; set; }  // Missing partial!
}

// CORRECT
[Factory]
internal partial class Person : EntityBase<Person>, IPerson
{
    public partial string? Name { get; set; }
}
```

### Public Class Instead of Internal

```csharp
// WRONG - should be internal
[Factory]
public partial class Person : EntityBase<Person>, IPerson { }

// CORRECT - interface is public, class is internal
public partial interface IPerson : IEntityBase { }

[Factory]
internal partial class Person : EntityBase<Person>, IPerson { }
```

### Missing Factory Method

```csharp
// WRONG - no factory method
[Factory]
internal partial class Person : EntityBase<Person>, IPerson
{
    public Person(IEntityBaseServices<Person> services) : base(services) { }
}

// CORRECT - at least [Create] is required
[Factory]
internal partial class Person : EntityBase<Person>, IPerson
{
    public Person(IEntityBaseServices<Person> services) : base(services) { }

    [Create]
    public void Create() { }
}
```

## Next Steps

- [adding-validation.md](adding-validation.md) - Add business rules
- [saving-data.md](saving-data.md) - Implement persistence
- [parent-child.md](parent-child.md) - Work with collections
