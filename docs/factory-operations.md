# Factory Operations

Neatoo uses source-generated factories to manage entity lifecycle operations. This document covers the factory pattern and all operation types.

## Factory Overview

For each class marked with `[Factory]`, Neatoo generates:
- An interface (`IPersonFactory`)
- A factory implementation
- Methods for each decorated operation

```csharp
// Generated interface
public interface IPersonFactory
{
    IPerson Create();
    Task<IPerson> Fetch(int id);
    Task<IPerson> Save(IPerson person);
    bool CanCreate();
    bool CanFetch();
    // ...
}
```

## Operation Attributes

| Attribute | Trigger | Purpose |
|-----------|---------|---------|
| `[Create]` | `Factory.Create()` | Initialize new entity |
| `[Fetch]` | `Factory.Fetch(...)` | Load from data source |
| `[Insert]` | `Factory.Save(entity)` when `IsNew` | Persist new entity |
| `[Update]` | `Factory.Save(entity)` when not `IsNew` | Persist changes |
| `[Delete]` | `Factory.Save(entity)` when `IsDeleted` | Remove from data source |

### Remote Execution

Add `[Remote]` for operations callable from the client:

```csharp
[Remote]  // Callable from Blazor client
[Fetch]
public async Task Fetch(int id, [Service] IDbContext db) { }
```

Aggregate roots typically have `[Remote]` on their operations. Child entities omit `[Remote]` since they're loaded/saved through the parent.

## Create Operation

Called when creating a new entity instance:

```csharp
[Create]
public void Create([Service] IChildListFactory childFactory)
{
    Id = Guid.NewGuid();
    CreatedDate = DateTime.UtcNow;
    ChildItems = childFactory.Create();  // Initialize child collection
}
```

### Usage

```csharp
var person = personFactory.Create();
// person.IsNew = true
// person.IsModified = false (initially)
```

## Fetch Operation

Called to load an entity from a data source:

```csharp
[Remote]
[Fetch]
public async Task<bool> Fetch(int id, [Service] IDbContext db,
                               [Service] IChildListFactory childFactory)
{
    var entity = await db.Persons.Include(p => p.Children).FirstOrDefaultAsync(p => p.Id == id);
    if (entity == null)
        return false;

    MapFrom(entity);
    ChildItems = childFactory.Fetch(entity.Children);
    return true;
}
```

### Usage

```csharp
var person = await personFactory.Fetch(42);
// person.IsNew = false
// person.IsModified = false
```

### Multiple Fetch Overloads

Define multiple fetch methods with different parameters:

```csharp
[Remote]
[Fetch]
public async Task Fetch(int id, [Service] IDbContext db) { }

[Remote]
[Fetch]
public async Task Fetch(string email, [Service] IDbContext db) { }
```

Generated factory:
```csharp
Task<IPerson> Fetch(int id);
Task<IPerson> Fetch(string email);
```

## Insert Operation

Called when saving a new entity (`IsNew = true`):

```csharp
[Remote]
[Insert]
public async Task Insert([Service] IDbContext db, [Service] IChildListFactory childFactory)
{
    await RunRules();
    if (!IsSavable)
        return;

    var entity = new PersonEntity();
    MapTo(entity);
    db.Persons.Add(entity);

    // Save children
    childFactory.Save(ChildItems, entity.Children);

    await db.SaveChangesAsync();
}
```

### Key Points

1. **Run rules first** - Validate before persisting
2. **Check IsSavable** - Don't persist invalid data
3. **MapTo** - Transfer all properties to EF entity
4. **Save children** - Cascade to child collections
5. **SaveChanges** - Commit to database

> **Warning: Do NOT Add Validation Logic Here**
>
> Even though services are available via `[Service]`, do not add business validation in factory methods. Validation here only runs at save time, providing poor user experience.
>
> ```csharp
> // BAD - Don't do this!
> if (await repository.EmailExistsAsync(Email))
>     throw new InvalidOperationException("Email in use");
> ```
>
> Instead, use `AsyncRuleBase<T>` with a Command for database-dependent validation. This provides immediate feedback during editing.
>
> See [Database-Dependent Validation](database-dependent-validation.md) for the correct pattern.

## Update Operation

Called when saving an existing entity (`IsNew = false`, `IsDeleted = false`):

```csharp
[Remote]
[Update]
public async Task Update([Service] IDbContext db, [Service] IChildListFactory childFactory)
{
    await RunRules();
    if (!IsSavable)
        return;

    var entity = await db.Persons.FindAsync(Id);
    if (entity == null)
        throw new KeyNotFoundException("Person not found");

    MapModifiedTo(entity);  // Only modified properties

    childFactory.Save(ChildItems, entity.Children);

    await db.SaveChangesAsync();
}
```

### MapModifiedTo

Only transfers properties marked as modified:

```csharp
// Source-generated implementation example
public partial void MapModifiedTo(PersonEntity entity)
{
    if (this[nameof(FirstName)].IsModified)
        entity.FirstName = this.FirstName;
    if (this[nameof(LastName)].IsModified)
        entity.LastName = this.LastName;
    // ...
}
```

> **Warning**: The same anti-pattern warning applies to Update operations. Do not add validation logic after `RunRules()`. See [Database-Dependent Validation](database-dependent-validation.md).

## Delete Operation

Called when saving a deleted entity (`IsDeleted = true`):

```csharp
[Remote]
[Delete]
public async Task Delete([Service] IDbContext db)
{
    var entity = await db.Persons.FindAsync(Id);
    if (entity != null)
    {
        db.Persons.Remove(entity);
        await db.SaveChangesAsync();
    }
}
```

### Marking for Deletion

```csharp
person.Delete();  // Marks IsDeleted = true
await personFactory.Save(person);  // Triggers Delete operation

// Undo deletion before save
person.UnDelete();
```

## Service Injection

Use `[Service]` to inject dependencies into operations:

```csharp
[Insert]
public async Task Insert(
    [Service] IDbContext db,
    [Service] IChildFactory childFactory,
    [Service] IEmailService emailService)
{
    // All services are resolved from DI
}
```

### Available Services

Any service registered in your DI container can be injected.

## Factory.Save() Logic

The `Save` method determines which operation to call:

```csharp
public async Task<IPerson> Save(IPerson person)
{
    if (person.IsDeleted)
        await Delete(person);
    else if (person.IsNew)
        await Insert(person);
    else if (person.IsModified)
        await Update(person);

    return person;
}
```

### Critical: Always Reassign After Save()

When you call `Save()`, the aggregate is **serialized to the server**, persisted, and a **new instance is returned** via deserialization. You MUST capture this return value:

```csharp
// CORRECT - captures the new deserialized instance
person = await personFactory.Save(person);
Console.WriteLine(person.Id);  // Has database-generated ID
```

```csharp
// WRONG - original object is now stale!
await personFactory.Save(person);
Console.WriteLine(person.Id);  // Still empty/0 - you have the PRE-save instance
```

#### Why This Happens

The Remote Factory pattern transfers your object across the client-server boundary:

1. **Client**: Your aggregate is serialized to JSON/binary
2. **Server**: A new instance is created from that data, persistence runs
3. **Server**: The updated aggregate is serialized back
4. **Client**: A **new instance** is deserialized and returned

The object you started with is **not the same object** that comes back. They are two different instances in memory.

#### Consequences of Forgetting

| What You Lose | Example |
|---------------|---------|
| Database-generated IDs | `person.Id` remains `Guid.Empty` or `0` |
| Server-computed values | Timestamps, calculated fields |
| Updated validation state | `IsValid`, `IsSavable` reflect old state |
| Property modification flags | `IsModified` doesn't reflect saved state |
| Concurrency tokens | RowVersion/ETag for optimistic concurrency |

This applies to Blazor components as well - if you don't reassign, the UI will show stale data and subsequent operations may fail. See [Blazor Binding](blazor-binding.md#critical-reassign-after-save-in-blazor-components) for UI-specific guidance.

## Authorization Methods

> **Note:** Authorization is provided by [RemoteFactory](https://github.com/NeatooDotNet/RemoteFactory). For comprehensive documentation, see the [RemoteFactory documentation](https://github.com/NeatooDotNet/RemoteFactory/tree/main/docs).

Factories include authorization check methods:

```csharp
[AuthorizeFactory<IPersonAuth>]
internal partial class Person : EntityBase<Person>, IPerson { }

// Generated factory includes:
public bool CanCreate();
public bool CanFetch();
public bool CanInsert();
public bool CanUpdate();
public bool CanDelete();
```

### UI Permission Display

```razor
<MudButton Disabled="@(!personFactory.CanCreate())" OnClick="CreatePerson">
    New Person
</MudButton>
```

## Child Entity Operations

Child entities don't have `[Remote]` since they're managed through the parent:

```csharp
[Factory]
internal partial class PersonPhone : EntityBase<PersonPhone>, IPersonPhone
{
    // No [Remote] - called by parent factory
    [Fetch]
    public void Fetch(PersonPhoneEntity entity)
    {
        MapFrom(entity);
    }

    [Insert]
    public void Insert(PersonPhoneEntity entity)
    {
        Id = Guid.NewGuid();
        MapTo(entity);
    }

    [Update]
    public void Update(PersonPhoneEntity entity)
    {
        MapModifiedTo(entity);
    }
}
```

## List Factory Operations

List factories provide Save method that handles the collection:

```csharp
[Factory]
internal class PersonPhoneList : EntityListBase<IPersonPhone>, IPersonPhoneList
{
    [Fetch]
    public void Fetch(IEnumerable<PersonPhoneEntity> entities,
                      [Service] IPersonPhoneFactory phoneFactory)
    {
        foreach (var entity in entities)
        {
            var phone = phoneFactory.Fetch(entity);
            Add(phone);
        }
    }

    [Update]
    public void Update(ICollection<PersonPhoneEntity> entities,
                       [Service] IPersonPhoneFactory phoneFactory)
    {
        foreach (var phone in this.Union(DeletedList))
        {
            PersonPhoneEntity entity;

            if (phone.IsNew)
            {
                entity = new PersonPhoneEntity();
                entities.Add(entity);
            }
            else
            {
                entity = entities.Single(e => e.Id == phone.Id);
            }

            if (phone.IsDeleted)
            {
                entities.Remove(entity);
            }
            else
            {
                phoneFactory.Save(phone, entity);
            }
        }
    }
}
```

## Mapper Methods

Declare partial mapper methods; Neatoo source-generates implementations:

```csharp
public partial void MapFrom(PersonEntity entity);
public partial void MapTo(PersonEntity entity);
public partial void MapModifiedTo(PersonEntity entity);
```

Properties are mapped by name. The EF entity property names must match the domain model property names.

## Factory Lifecycle Callbacks

Entities receive callbacks during factory operations:

```csharp
public virtual void FactoryStart(FactoryOperation operation)
{
    // Called before operation - pauses actions
}

public virtual void FactoryComplete(FactoryOperation operation)
{
    // Called after operation
    // Create: marks IsNew = true
    // Insert/Update: marks unmodified, IsNew = false
}
```

## Complete Example

```csharp
[Factory]
internal partial class Order : EntityBase<Order>, IOrder
{
    public Order(IEntityBaseServices<Order> services) : base(services) { }

    public partial int Id { get; set; }
    public partial string CustomerName { get; set; }
    public partial IOrderLineItemList LineItems { get; set; }

    public partial void MapFrom(OrderEntity entity);
    public partial void MapTo(OrderEntity entity);
    public partial void MapModifiedTo(OrderEntity entity);

    [Create]
    public void Create([Service] IOrderLineItemList lineItems)
    {
        LineItems = lineItems;
        OrderDate = DateTime.UtcNow;
    }

    [Remote]
    [Fetch]
    public async Task Fetch(int id, [Service] IOrderDbContext db,
                            [Service] IOrderLineItemListFactory lineItemFactory)
    {
        var entity = await db.Orders
            .Include(o => o.LineItems)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (entity != null)
        {
            MapFrom(entity);
            LineItems = lineItemFactory.Fetch(entity.LineItems);
        }
    }

    [Remote]
    [Insert]
    public async Task Insert([Service] IOrderDbContext db,
                             [Service] IOrderLineItemListFactory lineItemFactory)
    {
        await RunRules();
        if (!IsSavable) return;

        var entity = new OrderEntity();
        MapTo(entity);
        db.Orders.Add(entity);
        lineItemFactory.Save(LineItems, entity.LineItems);
        await db.SaveChangesAsync();

        Id = entity.Id;  // Capture generated ID
    }

    [Remote]
    [Update]
    public async Task Update([Service] IOrderDbContext db,
                             [Service] IOrderLineItemListFactory lineItemFactory)
    {
        await RunRules();
        if (!IsSavable) return;

        var entity = await db.Orders.FindAsync(Id);
        if (entity == null)
            throw new KeyNotFoundException("Order not found");

        MapModifiedTo(entity);
        lineItemFactory.Save(LineItems, entity.LineItems);
        await db.SaveChangesAsync();
    }

    [Remote]
    [Delete]
    public async Task Delete([Service] IOrderDbContext db)
    {
        await db.Orders.Where(o => o.Id == Id).ExecuteDeleteAsync();
    }
}
```

## See Also

- [Database-Dependent Validation](database-dependent-validation.md) - Async validation pattern
- [Remote Factory Pattern](remote-factory.md) - Client-server state transfer
- [Mapper Methods](mapper-methods.md) - MapFrom, MapTo details
- [Collections](collections.md) - Child list factories
