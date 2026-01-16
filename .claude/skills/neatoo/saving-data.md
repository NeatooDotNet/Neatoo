# Saving Data

How to persist Neatoo entities with factory operations.

## Factory Methods Overview

| Attribute | Purpose | When Called |
|-----------|---------|-------------|
| `[Create]` | Initialize new entity | `factory.Create()` |
| `[Fetch]` | Load from database | `factory.Fetch(id)` |
| `[Insert]` | Save new entity | `factory.Save(entity)` when `IsNew == true` |
| `[Update]` | Save changes | `factory.Save(entity)` when `IsNew == false` |
| `[Delete]` | Remove entity | `factory.Save(entity)` when `IsDeleted == true` |

## The [Remote] Attribute

`[Remote]` marks methods that execute on the server (required for database access in Blazor):

```csharp
[Create]  // No [Remote] - runs on client
public void Create() { }

[Remote]  // Runs on server
[Fetch]
public async Task Fetch(Guid id, [Service] IDbContext db) { }

[Remote]  // Runs on server
[Insert]
public async Task Insert([Service] IDbContext db) { }

[Remote]  // Runs on server
[Update]
public async Task Update([Service] IDbContext db) { }
```

## Create Operation

`[Create]` initializes a new entity on the client:

```csharp
[Create]
public void Create()
{
    // Set default values
    Id = Guid.NewGuid();
    CreatedDate = DateTime.UtcNow;
    Status = "Draft";
}

// With parameters
[Create]
public void Create(string orderType)
{
    Id = Guid.NewGuid();
    OrderType = orderType;
}
```

**Usage:**
```csharp
var order = orderFactory.Create();
var invoice = invoiceFactory.Create("Invoice");
```

## Fetch Operation

`[Fetch]` loads an entity from the database:

```csharp
[Remote]
[Fetch]
public async Task Fetch(Guid id, [Service] IDbContext db)
{
    var entity = await db.Orders.FindAsync(id);
    if (entity == null) return;  // Entity stays empty/default

    MapFrom(entity);
}

// Mapping method (manually implemented)
public void MapFrom(OrderEntity entity)
{
    Id = entity.Id;
    CustomerName = entity.CustomerName;
    OrderDate = entity.OrderDate;
    Total = entity.Total;
}
```

**Usage:**
```csharp
var order = await orderFactory.Fetch(orderId);
if (order == null || order.Id == null)
{
    // Not found
}
```

## Insert Operation

`[Insert]` persists a new entity:

```csharp
[Remote]
[Insert]
public async Task Insert([Service] IDbContext db)
{
    var entity = new OrderEntity();
    MapTo(entity);

    db.Orders.Add(entity);
    await db.SaveChangesAsync();

    // Capture database-generated ID
    Id = entity.Id;
}

// Mapping method (manually implemented)
public void MapTo(OrderEntity entity)
{
    entity.CustomerName = CustomerName;
    entity.OrderDate = OrderDate;
    entity.Total = Total;
}
```

## Update Operation

`[Update]` persists changes to an existing entity:

```csharp
[Remote]
[Update]
public async Task Update([Service] IDbContext db)
{
    var entity = await db.Orders.FindAsync(Id);
    if (entity == null)
        throw new InvalidOperationException("Order not found");

    MapModifiedTo(entity);  // Only copies changed properties
    await db.SaveChangesAsync();
}

// MapModifiedTo is source-generated
public partial void MapModifiedTo(OrderEntity entity);
```

## Delete Operation

`[Delete]` removes an entity:

```csharp
[Remote]
[Delete]
public async Task Delete([Service] IDbContext db)
{
    var entity = await db.Orders.FindAsync(Id);
    if (entity != null)
    {
        db.Orders.Remove(entity);
        await db.SaveChangesAsync();
    }
}
```

**Usage:**
```csharp
order.Delete();  // Marks for deletion (IsDeleted = true)
await orderFactory.Save(order);  // Executes [Delete] method
```

## The Save Pattern

`Save()` is the entry point that routes to Insert, Update, or Delete:

```csharp
// Factory.Save() examines entity state:
// - IsNew && !IsDeleted → calls [Insert]
// - !IsNew && !IsDeleted → calls [Update]
// - IsDeleted → calls [Delete]

order = await orderFactory.Save(order);  // Always reassign!
```

### CRITICAL: Always Reassign After Save

`Save()` returns a NEW instance. The original is stale.

```csharp
// WRONG - person is now stale!
await personFactory.Save(person);
var id = person.Id;  // Still has old value!

// CORRECT - capture returned instance
person = await personFactory.Save(person);
var id = person.Id;  // Has database-generated ID
```

**Why?** Save() serializes to server, executes, and deserializes back. The original client object is unchanged.

### Save via Entity

You can also call `Save()` on the entity itself:

```csharp
// Via factory
person = await personFactory.Save(person);

// Via entity (returns IEditableBase, cast needed)
person = (IPerson)await person.Save();
```

## Mapping Methods

### MapFrom - Loading Data

```csharp
public void MapFrom(PersonEntity entity)
{
    Id = entity.Id;
    FirstName = entity.FirstName;
    LastName = entity.LastName;
    Email = entity.Email;
}
```

### MapTo - Saving New Data

```csharp
public void MapTo(PersonEntity entity)
{
    entity.FirstName = FirstName;
    entity.LastName = LastName;
    entity.Email = Email;
    // Note: Don't map Id for database-generated keys
}
```

### MapModifiedTo - Updating Changed Data

```csharp
// Source-generated - only copies properties where IsModified = true
public partial void MapModifiedTo(PersonEntity entity);

// Generates code like:
// if (this[nameof(FirstName)].IsModified) entity.FirstName = FirstName;
// if (this[nameof(LastName)].IsModified) entity.LastName = LastName;
// etc.
```

## Checking Before Save

Always verify `IsSavable` before calling save:

```csharp
if (person.IsSavable)
{
    person = await personFactory.Save(person);
}
else
{
    // Diagnose why it can't save
    Console.WriteLine($"IsModified: {person.IsModified}");
    Console.WriteLine($"IsValid: {person.IsValid}");
    Console.WriteLine($"IsBusy: {person.IsBusy}");
    Console.WriteLine($"IsChild: {person.IsChild}");
}
```

**IsSavable requires ALL:**
- `IsModified == true` (has changes)
- `IsValid == true` (passes validation)
- `IsBusy == false` (no async operations)
- `IsChild == false` (not part of parent aggregate)

## Waiting for Async Operations

Before saving, wait for async validation to complete:

```csharp
// Wait for async rules
await person.WaitForTasks();

// Re-check after async completes
if (person.IsSavable)
{
    person = await personFactory.Save(person);
}
```

## Saving Aggregates with Children

When an aggregate has child collections, save them in the parent's methods:

```csharp
[Remote]
[Insert]
public async Task Insert([Service] IDbContext db, [Service] IOrderLineListFactory lineFactory)
{
    // Insert parent
    var entity = new OrderEntity();
    MapTo(entity);
    db.Orders.Add(entity);
    await db.SaveChangesAsync();
    Id = entity.Id;

    // Insert children
    lineFactory.Save(Lines, entity.Lines);
    await db.SaveChangesAsync();
}

[Remote]
[Update]
public async Task Update([Service] IDbContext db, [Service] IOrderLineListFactory lineFactory)
{
    var entity = await db.Orders
        .Include(o => o.Lines)
        .FirstOrDefaultAsync(o => o.Id == Id);

    if (entity == null) throw new InvalidOperationException("Not found");

    MapModifiedTo(entity);

    // Update children (handles insert/update/delete)
    lineFactory.Save(Lines, entity.Lines);
    await db.SaveChangesAsync();
}
```

See [parent-child.md](parent-child.md) for child collection details.

## Service Injection in Factory Methods

Use `[Service]` to inject dependencies:

```csharp
[Remote]
[Insert]
public async Task Insert(
    [Service] IDbContext db,
    [Service] IEmailService emailService,
    [Service] IAuditService auditService)
{
    var entity = new OrderEntity();
    MapTo(entity);
    db.Orders.Add(entity);
    await db.SaveChangesAsync();

    await emailService.SendOrderConfirmation(Id);
    await auditService.Log("OrderCreated", Id);
}
```

## Common Mistakes

### Not Calling SaveChangesAsync

```csharp
// WRONG - changes not persisted
[Insert]
public async Task Insert([Service] IDbContext db)
{
    var entity = new OrderEntity();
    MapTo(entity);
    db.Orders.Add(entity);
    // Oops! No SaveChangesAsync!
}

// CORRECT
[Insert]
public async Task Insert([Service] IDbContext db)
{
    var entity = new OrderEntity();
    MapTo(entity);
    db.Orders.Add(entity);
    await db.SaveChangesAsync();  // Persist to database
}
```

### Missing [Remote] Attribute

```csharp
// WRONG - runs on client, can't access database
[Fetch]
public async Task Fetch(Guid id, [Service] IDbContext db) { }  // Will fail!

// CORRECT
[Remote]  // Execute on server
[Fetch]
public async Task Fetch(Guid id, [Service] IDbContext db) { }
```

### Saving Child Directly

```csharp
// WRONG - children should be saved through parent
await orderLineFactory.Save(line);  // Will fail - IsChild is true

// CORRECT - save the parent
await orderFactory.Save(order);  // Saves parent and all children
```

## Best Practices

1. **Always reassign after Save** - `entity = await factory.Save(entity)`
2. **Check IsSavable before Save** - Not just IsValid
3. **Wait for async tasks** - `await entity.WaitForTasks()`
4. **Use MapModifiedTo in Update** - Only send changed properties
5. **Include children in aggregate saves** - Don't save children separately
6. **Add [Remote] for server operations** - Fetch, Insert, Update, Delete

## Next Steps

- [parent-child.md](parent-child.md) - Working with collections
- [client-server.md](client-server.md) - Setting up Blazor
- [troubleshooting.md](troubleshooting.md) - Debug save issues
