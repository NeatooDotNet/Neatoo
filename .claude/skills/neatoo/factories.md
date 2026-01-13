# Neatoo Factory Operations Reference

## Overview

The `[Factory]` attribute marks classes for factory generation. Neatoo's source generator creates factory interfaces and implementations that handle object creation, persistence, and client-server communication.

## Factory Attributes

| Attribute | Purpose | Location | Execution |
|-----------|---------|----------|-----------|
| `[Factory]` | Marks class for factory generation | Class | N/A |
| `[Create]` | Initialize new entity | Method | Client-side |
| `[Fetch]` | Load entity from database | Method | Server-side |
| `[Insert]` | Persist new entity | Method | Server-side |
| `[Update]` | Save changes to existing entity | Method | Server-side |
| `[Delete]` | Remove entity from database | Method | Server-side |
| `[Execute]` | Run command/query operations | Method | Server-side |
| `[Remote]` | Method runs on server | Method | Server-side |
| `[Service]` | Inject service into method | Parameter | Both |

## [Create] - Initialize New Entity

The `[Create]` method runs **client-side** to initialize a new entity.

### Basic Create

<!-- snippet: create-basic -->
```csharp
/// <summary>
/// Basic entity with simple Create operation.
/// </summary>
public partial interface ISimpleProduct : IEntityBase
{
    Guid Id { get; }
    string? Name { get; set; }
    DateTime CreatedDate { get; }
}

[Factory]
internal partial class SimpleProduct : EntityBase<SimpleProduct>, ISimpleProduct
{
    public SimpleProduct(IEntityBaseServices<SimpleProduct> services) : base(services) { }

    public partial Guid Id { get; set; }
    public partial string? Name { get; set; }
    public partial DateTime CreatedDate { get; set; }

    [Create]
    public void Create()
    {
        Id = Guid.NewGuid();
        CreatedDate = DateTime.UtcNow;
    }
}
```
<!-- /snippet -->

### Create with Service Injection

<!-- snippet: create-with-service -->
```csharp
/// <summary>
/// Entity with Create operation using service injection.
/// </summary>
public partial interface IProjectWithTasks : IEntityBase
{
    Guid Id { get; }
    string? Name { get; set; }
    IProjectTaskList Tasks { get; }
}

public partial interface IProjectTask : IEntityBase
{
    Guid Id { get; }
    string? Title { get; set; }
}

public interface IProjectTaskList : IEntityListBase<IProjectTask> { }

[Factory]
internal partial class ProjectTask : EntityBase<ProjectTask>, IProjectTask
{
    public ProjectTask(IEntityBaseServices<ProjectTask> services) : base(services) { }

    public partial Guid Id { get; set; }
    public partial string? Title { get; set; }

    [Create]
    public void Create()
    {
        Id = Guid.NewGuid();
    }
}

[Factory]
internal class ProjectTaskList : EntityListBase<IProjectTask>, IProjectTaskList
{
    [Create]
    public void Create() { }
}

[Factory]
internal partial class ProjectWithTasks : EntityBase<ProjectWithTasks>, IProjectWithTasks
{
    public ProjectWithTasks(IEntityBaseServices<ProjectWithTasks> services) : base(services) { }

    public partial Guid Id { get; set; }
    public partial string? Name { get; set; }
    public partial IProjectTaskList Tasks { get; set; }

    [Create]
    public void Create([Service] IProjectTaskListFactory taskListFactory)
    {
        Id = Guid.NewGuid();
        Tasks = taskListFactory.Create();
    }
}
```
<!-- /snippet -->

## [Fetch] - Load Entity

The `[Fetch]` method runs **server-side** to load an entity from the database.

### Basic Fetch

<!-- snippet: fetch-basic -->
```csharp
/// <summary>
/// Entity with basic Fetch operation.
/// </summary>
public partial interface IFetchableProduct : IEntityBase
{
    int Id { get; }
    string? Name { get; set; }
    decimal Price { get; set; }
}

[Factory]
internal partial class FetchableProduct : EntityBase<FetchableProduct>, IFetchableProduct
{
    public FetchableProduct(IEntityBaseServices<FetchableProduct> services) : base(services) { }

    public partial int Id { get; set; }
    public partial string? Name { get; set; }
    public partial decimal Price { get; set; }

    [Create]
    public void Create() { }

    [Fetch]
    public bool Fetch(int id, [Service] IProductRepository repo)
    {
        var data = repo.FindById(id);
        if (data == null)
            return false;

        Id = data.Id;
        Name = data.Name;
        Price = data.Price;
        return true;
    }
}
```
<!-- /snippet -->

### Multiple Fetch Overloads

<!-- snippet: fetch-multiple-overloads -->
```csharp
/// <summary>
/// Entity with multiple Fetch overloads for different lookup methods.
/// </summary>
public partial interface IProductWithMultipleFetch : IEntityBase
{
    int Id { get; }
    string? Name { get; set; }
    string? Sku { get; set; }
    decimal Price { get; set; }
}

[Factory]
internal partial class ProductWithMultipleFetch : EntityBase<ProductWithMultipleFetch>, IProductWithMultipleFetch
{
    public ProductWithMultipleFetch(IEntityBaseServices<ProductWithMultipleFetch> services) : base(services) { }

    public partial int Id { get; set; }
    public partial string? Name { get; set; }
    public partial string? Sku { get; set; }
    public partial decimal Price { get; set; }

    [Create]
    public void Create() { }

    /// <summary>
    /// Fetch by ID.
    /// </summary>
    [Fetch]
    public bool Fetch(int id, [Service] IProductRepository repo)
    {
        var data = repo.FindById(id);
        if (data == null)
            return false;

        MapFromData(data);
        return true;
    }

    /// <summary>
    /// Fetch by SKU.
    /// </summary>
    [Fetch]
    public bool Fetch(string sku, [Service] IProductRepository repo)
    {
        var data = repo.FindBySku(sku);
        if (data == null)
            return false;

        MapFromData(data);
        return true;
    }

    private void MapFromData(ProductData data)
    {
        Id = data.Id;
        Name = data.Name;
        Sku = data.Sku;
        Price = data.Price;
    }
}
```
<!-- /snippet -->

## [Insert], [Update], [Delete] - Persistence

### Insert Operation

The `[Insert]` method runs **server-side** to persist a new entity.

<!-- snippet: insert-operation -->
```csharp
/// <summary>
/// Entity demonstrating Insert operation pattern.
/// </summary>
public partial interface IInventoryItem : IEntityBase
{
    Guid Id { get; }
    string? Name { get; set; }
    int Quantity { get; set; }
    DateTime LastUpdated { get; }
}

[Factory]
internal partial class InventoryItem : EntityBase<InventoryItem>, IInventoryItem
{
    public InventoryItem(IEntityBaseServices<InventoryItem> services) : base(services) { }

    public partial Guid Id { get; set; }

    [Required(ErrorMessage = "Name is required")]
    public partial string? Name { get; set; }

    public partial int Quantity { get; set; }
    public partial DateTime LastUpdated { get; set; }

    [Create]
    public void Create()
    {
        Id = Guid.NewGuid();
        LastUpdated = DateTime.UtcNow;
    }

    [Fetch]
    public void Fetch(InventoryItemEntity entity)
    {
        Id = entity.Id;
        Name = entity.Name;
        Quantity = entity.Quantity;
        LastUpdated = entity.LastUpdated;
    }

    [Insert]
    public async Task Insert([Service] IInventoryDb db)
    {
        await RunRules();
        if (!IsSavable)
            return;

        var entity = new InventoryItemEntity
        {
            Id = Id,
            Name = Name ?? "",
            Quantity = Quantity,
            LastUpdated = DateTime.UtcNow
        };
        db.Add(entity);
        await db.SaveChangesAsync();

        LastUpdated = entity.LastUpdated;
    }
```
<!-- /snippet -->

### Update Operation

The `[Update]` method runs **server-side** to persist changes to an existing entity.

<!-- snippet: update-operation -->
```csharp
[Update]
    public async Task Update([Service] IInventoryDb db)
    {
        await RunRules();
        if (!IsSavable)
            return;

        var entity = db.Find(Id);
        if (entity == null)
            throw new KeyNotFoundException("Item not found");

        // Only update modified properties
        if (this[nameof(Name)].IsModified)
            entity.Name = Name ?? "";
        if (this[nameof(Quantity)].IsModified)
            entity.Quantity = Quantity;

        entity.LastUpdated = DateTime.UtcNow;
        await db.SaveChangesAsync();

        LastUpdated = entity.LastUpdated;
    }
```
<!-- /snippet -->

### Delete Operation

The `[Delete]` method runs **server-side** to remove an entity.

<!-- snippet: delete-operation -->
```csharp
[Delete]
    public async Task Delete([Service] IInventoryDb db)
    {
        var entity = db.Find(Id);
        if (entity != null)
        {
            db.Remove(entity);
            await db.SaveChangesAsync();
        }
    }
```
<!-- /snippet -->

## Save() Routing

The `Save()` method routes to the appropriate operation based on entity state:

| IsNew | IsDeleted | Operation |
|-------|-----------|-----------|
| true | false | `[Insert]` |
| false | false | `[Update]` |
| any | true | `[Delete]` |

### Save Usage Examples

<!-- snippet: save-usage-examples -->
```csharp
/// <summary>
/// Examples demonstrating correct Save() usage patterns.
/// </summary>
public static class SaveUsageExamples
{
    /// <summary>
    /// CORRECT: Always reassign after Save().
    /// The Save method returns a new instance after client-server roundtrip.
    /// </summary>
    public static async Task<ISaveableItem> CorrectSavePattern(
        ISaveableItemFactory factory)
    {
        var item = factory.Create();
        item.Name = "New Item";

        // CORRECT - capture the returned instance
        item = await factory.Save(item);

        // Now 'item' has database-generated values
        return item;
    }

    /// <summary>
    /// Delete pattern: mark for deletion, then save.
    /// </summary>
    public static async Task DeletePattern(
        ISaveableItem item,
        ISaveableItemFactory factory)
    {
        // Mark for deletion
        item.Delete();

        // Save triggers the Delete operation
        await factory.Save(item);
    }

    /// <summary>
    /// UnDelete pattern: undo deletion before save.
    /// </summary>
    public static void UnDeletePattern(ISaveableItem item)
    {
        item.Delete();  // Mark for deletion

        // Changed mind - undo the deletion
        item.UnDelete();

        // Now IsDeleted = false, item will not be deleted on save
    }
}
```
<!-- /snippet -->

## Critical: Always Reassign After Save

**IMPORTANT**: `Save()` returns a new object instance. You must reassign:

<!-- pseudo:factories-reassign-save -->
```csharp
// WRONG - stale reference
await personFactory.Save(person);

// CORRECT - capture new instance
person = await personFactory.Save(person);
```
<!-- /snippet -->

### Why Reassignment is Required

The save process:
1. Serializes entity to server
2. Server creates new instance from data
3. Server persists and updates (ID, timestamps)
4. Server serializes updated entity back
5. Client deserializes into NEW object
6. `Save()` returns this new instance

## Entity-Based Save

Entities can save themselves via `entity.Save()` instead of `factory.Save(entity)`:

<!-- snippet: entity-save-method -->
```csharp
/// <summary>
/// Examples demonstrating entity.Save() vs factory.Save() patterns.
/// </summary>
public static class EntitySaveExamples
{
    /// <summary>
    /// Factory-based save - the documented pattern.
    /// </summary>
    public static async Task<IVisit> FactorySavePattern(
        IVisit visit,
        IVisitFactory visitFactory)
    {
        visit.PatientName = "Updated Name";

        // Factory-based save
        return await visitFactory.Save(visit);
    }

    /// <summary>
    /// Entity-based save - equivalent, but called on the entity.
    /// The entity internally calls its factory.Save(this).
    /// </summary>
    public static async Task<IVisit> EntitySavePattern(IVisit visit)
    {
        visit.PatientName = "Updated Name";

        // Entity-based save - same result
        return (IVisit)await visit.Save();
    }

    /// <summary>
    /// Business operation pattern - combines state modification with save.
    /// This is the preferred pattern for domain operations.
    /// </summary>
    public static async Task<IVisit> BusinessOperationPattern(IVisit visit)
    {
        // Single call: validates, modifies state, persists
        return await visit.Archive();
    }
}
```
<!-- /snippet -->

### EntityBase.Save() Methods

| Method | Return Type | Description |
|--------|-------------|-------------|
| `Save()` | `Task<IEntityBase>` | Persist entity |
| `Save(CancellationToken)` | `Task<IEntityBase>` | Save with cancellation |

### Save Failure Exceptions

Throws `SaveOperationException` when:

| Reason | Condition |
|--------|-----------|
| `IsChildObject` | `IsChild = true` |
| `IsInvalid` | `IsValid = false` |
| `NotModified` | `IsModified = false` |
| `IsBusy` | `IsBusy = true` |
| `NoFactoryMethod` | No factory reference |

## Business Operations Pattern

Domain operations like `Archive()`, `Complete()`, `Approve()` can use `entity.Save()` to modify state and persist atomically. See the full example in [Factory Operations - Business Operations](../../docs/factory-operations.md#business-operations).

**Benefits:**
- Clean API on the entity interface
- No `[Service]` parameters needed
- Atomic operation—can't forget to save
- Discoverable—operation is where expected

## Child Entity Factory

Child entities with parent references use a specific factory pattern.

### Child Entity Pattern

<!-- snippet: child-entity -->
```csharp
/// <summary>
/// Child entity - no [Remote] since managed through parent.
/// </summary>
public partial interface IInvoiceLine : IEntityBase
{
    Guid Id { get; }
    string? Description { get; set; }
    decimal Amount { get; set; }
}

[Factory]
internal partial class InvoiceLine : EntityBase<InvoiceLine>, IInvoiceLine
{
    public InvoiceLine(IEntityBaseServices<InvoiceLine> services) : base(services) { }

    public partial Guid Id { get; set; }
    public partial string? Description { get; set; }
    public partial decimal Amount { get; set; }

    [Create]
    public void Create()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// No [Remote] - called by parent factory.
    /// </summary>
    [Fetch]
    public void Fetch(InvoiceLineEntity entity)
    {
        Id = entity.Id;
        Description = entity.Description;
        Amount = entity.Amount;
    }

    /// <summary>
    /// Insert populates the EF entity for parent to save.
    /// </summary>
    [Insert]
    public void Insert(InvoiceLineEntity entity)
    {
        entity.Id = Id;
        entity.Description = Description ?? "";
        entity.Amount = Amount;
    }

    /// <summary>
    /// Update only transfers modified properties.
    /// </summary>
    [Update]
    public void Update(InvoiceLineEntity entity)
    {
        if (this[nameof(Description)].IsModified)
            entity.Description = Description ?? "";
        if (this[nameof(Amount)].IsModified)
            entity.Amount = Amount;
    }
}
```
<!-- /snippet -->

### List Factory Pattern

> **Critical: Always include DeletedList in Update methods**
>
> When iterating items in a list's `[Update]` method, you must use `this.Union(DeletedList)`
> to include items that were removed from the list. Removed items are moved to `DeletedList`
> and marked `IsDeleted = true`. If you only iterate `this`, removed items will silently
> remain in the database.

<!-- snippet: list-factory -->
```csharp
/// <summary>
/// List factory handles collection of child entities.
/// </summary>
public interface IInvoiceLineList : IEntityListBase<IInvoiceLine> { }

[Factory]
internal class InvoiceLineList : EntityListBase<IInvoiceLine>, IInvoiceLineList
{
    [Create]
    public void Create() { }

    /// <summary>
    /// Fetch populates list from EF entities.
    /// </summary>
    [Fetch]
    public void Fetch(IEnumerable<InvoiceLineEntity> entities,
                      [Service] IInvoiceLineFactory lineFactory)
    {
        foreach (var entity in entities)
        {
            var line = lineFactory.Fetch(entity);
            Add(line);
        }
    }

    /// <summary>
    /// Save handles insert/update/delete for all items.
    /// </summary>
    [Update]
    public void Update(ICollection<InvoiceLineEntity> entities,
                       [Service] IInvoiceLineFactory lineFactory)
    {
        foreach (var line in this.Union(DeletedList))
        {
            InvoiceLineEntity entity;

            if (line.IsNew)
            {
                entity = new InvoiceLineEntity();
                entities.Add(entity);
            }
            else
            {
                entity = entities.Single(e => e.Id == line.Id);
            }

            if (line.IsDeleted)
            {
                entities.Remove(entity);
            }
            else
            {
                lineFactory.Save(line, entity);
            }
        }
    }
}
```
<!-- /snippet -->

## [Execute] - Commands and Queries

Use `[Execute]` for operations that don't follow the standard CRUD lifecycle.

### Command Pattern

<!-- snippet: command-pattern -->
```csharp
/// <summary>
/// Command for checking email uniqueness.
/// The source generator creates a delegate that can be injected and executed remotely.
/// </summary>
[Factory]
public static partial class CheckEmailUnique
{
    [Execute]
    internal static async Task<bool> _IsUnique(
        string email,
        Guid? excludeId,
        [Service] IUserRepository repo)
    {
        return !await repo.EmailExistsAsync(email, excludeId);
    }
}
```
<!-- /snippet -->

## [Service] - Dependency Injection

The `[Service]` attribute injects dependencies into factory methods.

### Service Resolution

- Services resolve from the DI container
- On server: Full service resolution
- On client: Only client-registered services available
- Method parameters without `[Service]` come from factory call

## [Remote] Attribute

The `[Remote]` attribute indicates a method runs on the server.

### When to Use [Remote]

| Method | Needs [Remote]? | Reason |
|--------|-----------------|--------|
| `[Create]` | No | Runs client-side for initialization |
| `[Fetch]` | Yes | Database access on server |
| `[Insert]` | Yes | Database write on server |
| `[Update]` | Yes | Database write on server |
| `[Delete]` | Yes | Database write on server |
| `[Execute]` | Yes | Server-side operations |

## Error Handling

### Factory Return Values

| Method | Success | Failure |
|--------|---------|---------|
| `Create()` | Entity instance | null (if unauthorized) |
| `Fetch()` | Entity instance | null (not found or unauthorized) |
| `Save()` | Updated entity | null (if unauthorized) or throws |
| `TrySave()` | `Authorized<T>` with value | `Authorized<T>` with message |
| `Execute()` | Updated object | Throws on error |

## Best Practices

1. **Use LoadProperty in Fetch** - Avoid triggering rules when loading
2. **Check IsModified Before Update** - Only update changed properties
3. **Generate IDs on Insert** - Not in Create, to avoid server round-trip issues
4. **Always reassign after Save** - `person = await factory.Save(person)`

## Common Pitfalls

1. **Missing [Remote]** - Server methods need [Remote] attribute
2. **Not reassigning after Save** - `person = await factory.Save(person)`
3. **Using property setters in Fetch** - Use `LoadProperty()` instead
4. **Not processing DeletedList** - Collections need to handle deletions
5. **Missing await** - All factory methods are async
6. **Injecting child factories into consuming code** - Use parent's add methods instead (see pitfalls.md #15)
7. **Casting to concrete to call internal methods** - Add methods to interface instead (see pitfalls.md #14)
