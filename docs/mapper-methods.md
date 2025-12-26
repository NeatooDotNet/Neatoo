# Mapper Methods

Neatoo source-generates mapper methods to transfer data between domain models and Entity Framework entities.

## Overview

Declare partial mapper methods in your domain model; Neatoo generates the implementations:

```csharp
[Factory]
internal partial class Person : EntityBase<Person>, IPerson
{
    public partial string? FirstName { get; set; }
    public partial string? LastName { get; set; }
    public partial string? Email { get; set; }

    // Declare mappers - implementations are generated
    public partial void MapFrom(PersonEntity entity);
    public partial void MapTo(PersonEntity entity);
    public partial void MapModifiedTo(PersonEntity entity);
}
```

## MapFrom

Maps from an EF entity to the domain model. Used in Fetch operations.

### Generated Implementation

```csharp
// Source-generated
public partial void MapFrom(PersonEntity entity)
{
    this.FirstName = entity.FirstName;
    this.LastName = entity.LastName;
    this.Email = entity.Email;
}
```

### Usage

```csharp
[Fetch]
public async Task Fetch(int id, [Service] IDbContext db)
{
    var entity = await db.Persons.FindAsync(id);
    if (entity != null)
    {
        MapFrom(entity);  // Copy all properties
    }
}
```

### Behavior

- Copies all matching properties by name
- Does not trigger validation rules
- Does not mark properties as modified
- Uses internal `LoadProperty` mechanism

## MapTo

Maps all properties from domain model to EF entity. Used in Insert operations.

### Generated Implementation

```csharp
// Source-generated
public partial void MapTo(PersonEntity entity)
{
    entity.FirstName = this.FirstName;
    entity.LastName = this.LastName;
    entity.Email = this.Email;
}
```

### Usage

```csharp
[Insert]
public async Task Insert([Service] IDbContext db)
{
    await RunRules();
    if (!IsSavable) return;

    var entity = new PersonEntity();
    MapTo(entity);  // Copy all properties
    db.Persons.Add(entity);
    await db.SaveChangesAsync();
}
```

### Behavior

- Copies all matching properties by name
- Includes all properties, not just modified ones
- Used for new entity creation

## MapModifiedTo

Maps only modified properties. Used in Update operations for efficiency.

### Generated Implementation

```csharp
// Source-generated
public partial void MapModifiedTo(PersonEntity entity)
{
    if (this[nameof(FirstName)].IsModified)
        entity.FirstName = this.FirstName;

    if (this[nameof(LastName)].IsModified)
        entity.LastName = this.LastName;

    if (this[nameof(Email)].IsModified)
        entity.Email = this.Email;
}
```

### Usage

```csharp
[Update]
public async Task Update([Service] IDbContext db)
{
    await RunRules();
    if (!IsSavable) return;

    var entity = await db.Persons.FindAsync(Id);
    MapModifiedTo(entity);  // Only changed properties
    await db.SaveChangesAsync();
}
```

### Behavior

- Only updates properties where `IsModified = true`
- More efficient for partial updates
- Reduces unnecessary database writes
- Works with EF Core change tracking

## Property Matching

Properties are matched by name. The EF entity property name must match the domain model property name.

### Matching Rules

```csharp
// Domain Model
public partial string? FirstName { get; set; }

// EF Entity - MUST match name
public string? FirstName { get; set; }  // OK
public string? First_Name { get; set; } // Won't map
```

### Type Compatibility

Properties must have compatible types:

```csharp
// Domain Model
public partial int Age { get; set; }

// EF Entity
public int Age { get; set; }        // OK - exact match
public int? Age { get; set; }       // OK - nullable compatible
public string Age { get; set; }     // Error - type mismatch
```

## Child Collections

Child collections are NOT handled by mapper methods. Map them explicitly:

```csharp
[Factory]
internal partial class Order : EntityBase<Order>, IOrder
{
    public partial IOrderLineItemList LineItems { get; set; }

    public partial void MapFrom(OrderEntity entity);
    public partial void MapTo(OrderEntity entity);

    [Fetch]
    public async Task Fetch(int id, [Service] IDbContext db,
                            [Service] IOrderLineItemListFactory listFactory)
    {
        var entity = await db.Orders.Include(o => o.LineItems).FirstOrDefaultAsync(o => o.Id == id);
        if (entity != null)
        {
            MapFrom(entity);
            // Explicitly map child collection
            LineItems = listFactory.Fetch(entity.LineItems);
        }
    }

    [Update]
    public async Task Update([Service] IDbContext db,
                             [Service] IOrderLineItemListFactory listFactory)
    {
        var entity = await db.Orders.Include(o => o.LineItems).FirstOrDefaultAsync(o => o.Id == Id);
        MapModifiedTo(entity);
        // Explicitly save child collection
        listFactory.Save(LineItems, entity.LineItems);
        await db.SaveChangesAsync();
    }
}
```

## Custom Mapping Logic

For complex mappings, add custom logic after the generated mapper:

```csharp
[Fetch]
public async Task Fetch(int id, [Service] IDbContext db)
{
    var entity = await db.Persons.FindAsync(id);
    if (entity != null)
    {
        MapFrom(entity);

        // Custom mapping for computed fields
        this.FullName = $"{entity.FirstName} {entity.LastName}";

        // Custom mapping for type conversions
        this.PhoneTypeEnum = Enum.Parse<PhoneType>(entity.PhoneTypeString);
    }
}
```

## Excluding Properties

Properties not declared as `partial` are not included in generated mappers:

```csharp
[Factory]
internal partial class Person : EntityBase<Person>, IPerson
{
    // Included in mappers
    public partial string? FirstName { get; set; }
    public partial string? LastName { get; set; }

    // Excluded from mappers (not partial)
    public string FullName => $"{FirstName} {LastName}";  // Calculated
    public bool IsExpanded { get; set; }                   // UI-only
}
```

## Different Entity Shapes

When domain model and EF entity have different structures:

```csharp
[Factory]
internal partial class Person : EntityBase<Person>, IPerson
{
    public partial string? FirstName { get; set; }
    public partial string? LastName { get; set; }
    public partial string? StreetAddress { get; set; }
    public partial string? City { get; set; }

    // Custom mapping for nested EF structure
    public void MapFromEntity(PersonEntity entity)
    {
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        // EF has nested Address object
        StreetAddress = entity.Address?.Street;
        City = entity.Address?.City;
    }

    public void MapToEntity(PersonEntity entity)
    {
        entity.FirstName = FirstName;
        entity.LastName = LastName;
        entity.Address ??= new AddressEntity();
        entity.Address.Street = StreetAddress;
        entity.Address.City = City;
    }
}
```

## Best Practices

### 1. Keep EF Entity Names Aligned

```csharp
// Domain Model
public partial string? CustomerName { get; set; }

// EF Entity - use same name
public string? CustomerName { get; set; }  // Good
public string? Name { get; set; }          // Won't auto-map
```

### 2. Use MapModifiedTo for Updates

```csharp
// Efficient - only updates changed columns
[Update]
public async Task Update([Service] IDbContext db)
{
    var entity = await db.Persons.FindAsync(Id);
    MapModifiedTo(entity);  // Only modified properties
    await db.SaveChangesAsync();
}
```

### 3. Handle Null Entities

```csharp
[Fetch]
public async Task<bool> Fetch(int id, [Service] IDbContext db)
{
    var entity = await db.Persons.FindAsync(id);
    if (entity == null)
        return false;

    MapFrom(entity);
    return true;
}
```

### 4. Map ID After Insert

```csharp
[Insert]
public async Task Insert([Service] IDbContext db)
{
    var entity = new PersonEntity();
    MapTo(entity);
    db.Persons.Add(entity);
    await db.SaveChangesAsync();

    // Capture database-generated ID
    this.Id = entity.Id;
}
```

## Generated Code Location

View generated mapper implementations in:
```
obj/Debug/net8.0/generated/Neatoo.BaseGenerator/
    Neatoo.BaseGenerator.MapperGenerator/
        DomainModel.PersonMapper.g.cs
```

## See Also

- [Factory Operations](factory-operations.md) - Using mappers in operations
- [Aggregates and Entities](aggregates-and-entities.md) - Domain model design
- [Collections](collections.md) - Child collection mapping
