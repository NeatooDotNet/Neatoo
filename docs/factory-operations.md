# Factory Operations

Neatoo uses source-generated factories to manage entity lifecycle operations. This document covers the factory pattern and all operation types.

## Factory Overview

For each class marked with `[Factory]`, Neatoo generates:
- An interface (`IArticleFactory`)
- A factory implementation
- Methods for each decorated operation
- Authorization check methods (`CanCreate`, `CanFetch`, etc.) returning `Authorized`

<!-- generated:docs/samples/Neatoo.Samples.DomainModel/Generated/Neatoo.Generator/Neatoo.Factory/Neatoo.Samples.DomainModel.Authorization.ArticleFactory.g.cs#L13-L17 -->
```csharp
public interface IArticleFactory
{
    IArticle? Create();
    Authorized CanCreate();
}
```
<!-- /snippet -->

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

<!-- pseudo:remote-fetch-example -->
```csharp
[Remote]  // Callable from Blazor client
[Fetch]
public async Task Fetch(int id, [Service] IDbContext db) { }
```
<!-- /snippet -->

Aggregate roots typically have `[Remote]` on their operations. Child entities omit `[Remote]` since they're loaded/saved through the parent.

## Create Operation

Called when creating a new entity instance:

<!-- snippet: create-with-service -->
```cs
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
<!-- endSnippet -->

### Usage

<!-- pseudo:create-usage -->
```csharp
var person = personFactory.Create();
// person.IsNew = true
// person.IsModified = false (initially)
```
<!-- /snippet -->

## Fetch Operation

Called to load an entity from a data source:

<!-- snippet: fetch-basic -->
```cs
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
<!-- endSnippet -->

### Usage

<!-- pseudo:fetch-usage -->
```csharp
var person = await personFactory.Fetch(42);
// person.IsNew = false
// person.IsModified = false
```
<!-- /snippet -->

### Multiple Fetch Overloads

Define multiple fetch methods with different parameters:

<!-- snippet: fetch-multiple-overloads -->
```cs
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
<!-- endSnippet -->

Generated factory:
<!-- generated:factory-fetch-overloads -->
```csharp
Task<IPerson> Fetch(int id);
Task<IPerson> Fetch(string email);
```
<!-- /snippet -->

## Insert Operation

Called when saving a new entity (`IsNew = true`):

<!-- snippet: insert-operation -->
```cs
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
<!-- endSnippet -->

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
> <!-- invalid:factory-validation-antipattern -->
> ```csharp
> // BAD - Don't do this!
> if (await repository.EmailExistsAsync(Email))
>     throw new InvalidOperationException("Email in use");
> ```
> <!-- /snippet -->
>
> Instead, use `AsyncRuleBase<T>` with a Command for database-dependent validation. This provides immediate feedback during editing.
>
> See [Database-Dependent Validation](database-dependent-validation.md) for the correct pattern.

## Update Operation

Called when saving an existing entity (`IsNew = false`, `IsDeleted = false`):

<!-- snippet: factory-update-operation -->
```cs
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
<!-- endSnippet -->

### MapModifiedTo

Only transfers properties marked as modified:

<!-- generated:mapmodifiedto-implementation -->
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
<!-- /snippet -->

> **Warning**: The same anti-pattern warning applies to Update operations. Do not add validation logic after `RunRules()`. See [Database-Dependent Validation](database-dependent-validation.md).

## Delete Operation

Called when saving a deleted entity (`IsDeleted = true`):

<!-- snippet: delete-operation -->
```cs
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
<!-- endSnippet -->

### Marking for Deletion

<!-- pseudo:deletion-workflow -->
```csharp
person.Delete();  // Marks IsDeleted = true
await personFactory.Save(person);  // Triggers Delete operation

// Undo deletion before save
person.UnDelete();
```
<!-- /snippet -->

## Service Injection

Use `[Service]` to inject dependencies into operations:

<!-- pseudo:service-injection -->
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
<!-- /snippet -->

### Available Services

Any service registered in your DI container can be injected.

## Factory.Save() Logic

The `Save` method determines which operation to call:

<!-- pseudo:save-routing-logic -->
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
<!-- /snippet -->

### Save with CancellationToken

`EntityBase` supports cancellation via `Save(CancellationToken)`:

<!-- pseudo:save-with-cancellation -->
```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

try
{
    person = await person.Save(cts.Token);
}
catch (OperationCanceledException)
{
    // Save was cancelled before persistence began
}
```
<!-- /snippet -->

**Important:** Cancellation only works **before** persistence. Once `Insert`, `Update`, or `Delete` begins, the operation cannot be cancelled to prevent data corruption. Cancellation applies to:

- `WaitForTasks()` - waiting for async validation to complete
- Pre-persistence checks

### Save Accepts Interface Types

Factory Save methods accept **interface types**, not concrete types. No casting is required:

<!-- pseudo:save-interface-types -->
```csharp
// Factory signature - accepts interface
Task<IOrderLine> Save(IOrderLine target, long orderId);

// Usage - no casting needed
await lineFactory.Save(line, order.Id.Value);
```
<!-- /snippet -->

### Save Overload Generation from Insert

The factory generates Save method signatures based on your Insert method's non-service parameters.

| Insert Signature | Generated Save Signature |
|------------------|--------------------------|
| `Insert([Service] IDb db)` | `Save(IEntity target)` |
| `Insert(long parentId, [Service] IDb db)` | `Save(IEntity target, long parentId)` |
| `Insert(string category, int priority, [Service] IDb db)` | `Save(IEntity target, string category, int priority)` |

Parameters marked with `[Service]` are injected from DI and don't appear in the Save signature. This pattern enables parent entities to pass IDs to children during Insert:

<!-- pseudo:parent-saves-child-generated -->
```csharp
// Child's Insert method
[Insert]
public async Task Insert(long parentId, [Service] IDbContext db)
{
    ParentId = parentId;  // Internal FK
    // ... persist
}

// Generated factory Save (from Insert parameters)
public Task<IChildEntity> Save(IChildEntity target, long parentId);

// Parent uses it
foreach (var child in Children)
{
    await childFactory.Save(child, this.Id.Value);
}
```
<!-- /snippet -->

### Critical: Always Reassign After Save()

When you call `Save()`, the aggregate is **serialized to the server**, persisted, and a **new instance is returned** via deserialization. You MUST capture this return value:

<!-- pseudo:correct-save-pattern -->
```csharp
// CORRECT - captures the new deserialized instance
person = await personFactory.Save(person);
Console.WriteLine(person.Id);  // Has database-generated ID
```
<!-- /snippet -->

<!-- invalid:wrong-save-pattern -->
```csharp
// WRONG - original object is now stale!
await personFactory.Save(person);
Console.WriteLine(person.Id);  // Still empty/0 - you have the PRE-save instance
```
<!-- /snippet -->

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

## Entity-Based Save

In addition to `factory.Save(entity)`, entities can save themselves via `entity.Save()`:

<!-- pseudo:entity-based-save -->
```csharp
// Factory-based save
person = await personFactory.Save(person);

// Entity-based save (equivalent)
person = (IPerson) await person.Save();
```
<!-- /snippet -->

**Both approaches are correct.** Choose based on context:
- Factory-based when you have the factory injected
- Entity-based for business operations on the entity itself

**Anti-pattern: Casting to concrete to access "internal" save methods:**

<!-- invalid:casting-antipattern -->
```csharp
// WRONG - Don't cast to concrete to call internal methods
var concrete = (Person)person;
await concrete.SomeInternalPersistMethod();

// CORRECT - Use the standard patterns
person = await personFactory.Save(person);
// or
person = (IPerson) await person.Save();
```
<!-- /snippet -->

If you feel you need internal save methods, you're likely working around a design issue. The standard `Save()` routes to `[Insert]`, `[Update]`, or `[Delete]` based on entity state.

Both approaches are equivalent—`entity.Save()` internally calls `factory.Save(this)`. The entity-based approach is useful when:

- Implementing business operations on the entity (see [Business Operations](#business-operations))
- The entity has access to its factory reference
- You prefer the fluent style

### EntityBase.Save() Methods

| Method | Return Type | Description |
|--------|-------------|-------------|
| `Save()` | `Task<IEntityBase>` | Persist entity, routes to Insert/Update/Delete |
| `Save(CancellationToken)` | `Task<IEntityBase>` | Save with cancellation support |

### Save Failure Exceptions

`Save()` throws `SaveOperationException` when the entity cannot be saved:

| Reason | Condition |
|--------|-----------|
| `IsChildObject` | `IsChild = true` - child entities must be saved through parent |
| `IsInvalid` | `IsValid = false` - fix validation errors first |
| `NotModified` | `IsModified = false` - no changes to save |
| `IsBusy` | `IsBusy = true` - wait for async operations |
| `NoFactoryMethod` | Entity has no factory reference |

## Business Operations

Domain operations like `Archive()`, `Complete()`, or `Approve()` often need to:

1. Validate preconditions
2. Modify entity state
3. Persist changes
4. Return the updated entity

The **Business Operation pattern** combines these steps into a single interface method using `entity.Save()`:

<!-- snippet: business-operation-pattern -->
```cs
/// <summary>
/// Entity with business operations that modify state and persist.
/// The Archive() method demonstrates the pattern: validate, modify, persist via Save().
/// </summary>
public partial interface IVisit : IEntityBase
{
    Guid Id { get; }
    string? PatientName { get; set; }
    VisitStatus Status { get; set; }
    bool Archived { get; }
    DateTime LastUpdated { get; }

    /// <summary>
    /// Archives the visit. No [Service] parameters - callable from interface.
    /// </summary>
    Task<IVisit> Archive();
}

[Factory]
internal partial class Visit : EntityBase<Visit>, IVisit
{
    public Visit(IEntityBaseServices<Visit> services) : base(services) { }

    public partial Guid Id { get; set; }

    [Required(ErrorMessage = "Patient name is required")]
    public partial string? PatientName { get; set; }

    public partial VisitStatus Status { get; set; }
    public partial bool Archived { get; set; }
    public partial DateTime LastUpdated { get; set; }

    /// <summary>
    /// Business operation: Archives the visit.
    /// </summary>
    /// <returns>The updated entity after persistence.</returns>
    public async Task<IVisit> Archive()
    {
        // Validate preconditions
        if (Archived)
            throw new InvalidOperationException("Visit is already archived");

        // Modify properties (client-side)
        Status = VisitStatus.Archived;
        Archived = true;
        LastUpdated = DateTime.UtcNow;

        // Persist via existing Save() - triggers [Update]
        return (IVisit)await this.Save();
    }

    [Create]
    public void Create()
    {
        Id = Guid.NewGuid();
        Status = VisitStatus.Active;
        Archived = false;
        LastUpdated = DateTime.UtcNow;
    }

    [Remote]
    [Fetch]
    public void Fetch(VisitEntity entity)
    {
        Id = entity.Id;
        PatientName = entity.PatientName;
        Status = entity.Status;
        Archived = entity.Archived;
        LastUpdated = entity.LastUpdated;
    }

    [Remote]
    [Insert]
    public async Task Insert([Service] IVisitDb db)
    {
        await RunRules();
        if (!IsSavable)
            return;

        var entity = new VisitEntity
        {
            Id = Id,
            PatientName = PatientName,
            Status = Status,
            Archived = Archived,
            LastUpdated = DateTime.UtcNow
        };
        db.Visits.Add(entity);
        await db.SaveChangesAsync();
        LastUpdated = entity.LastUpdated;
    }

    [Remote]
    [Update]
    public async Task Update([Service] IVisitDb db)
    {
        await RunRules();
        if (!IsSavable)
            return;

        var entity = db.Find(Id);
        if (entity == null)
            throw new KeyNotFoundException("Visit not found");

        // Only update modified properties
        if (this[nameof(PatientName)].IsModified)
            entity.PatientName = PatientName;
        if (this[nameof(Status)].IsModified)
            entity.Status = Status;
        if (this[nameof(Archived)].IsModified)
            entity.Archived = Archived;

        entity.LastUpdated = DateTime.UtcNow;
        await db.SaveChangesAsync();
        LastUpdated = entity.LastUpdated;
    }
}
```
<!-- endSnippet -->

### Pattern Benefits

| Benefit | Description |
|---------|-------------|
| Clean API | `visit = await visit.Archive()` |
| Interface-compatible | No `[Service]` params in interface method |
| No extra infrastructure | Uses existing `EntityBase.Save()` |
| Atomic | Can't forget to save after operation |
| Discoverable | Operation is on the entity where expected |
| Testable | Can mock at interface level |

### How It Works

1. **Client:** Business method validates preconditions and sets properties
2. **Client:** `this.Save()` serializes entity and sends to server
3. **Server:** `[Update]` receives entity, persists modified properties
4. **Server:** Returns updated entity
5. **Client:** Receives new instance

### When to Use This Pattern

Use business operations for domain actions that:

- Modify multiple related properties atomically
- Have validation preconditions
- Should appear on the public interface
- Need to persist immediately

Examples: `Archive()`, `Complete()`, `Approve()`, `Cancel()`, `Submit()`

### Alternative: [Execute] Command Pattern

For operations that don't belong on the entity interface, use the `[Execute]` command pattern:

<!-- pseudo:execute-command-pattern -->
```csharp
[Factory]
public static partial class ArchiveVisitCommand
{
    [Execute]
    internal static async Task<IVisit?> _Archive(
        Guid visitId,
        [Service] IVisitFactory visitFactory,
        [Service] IVisitDb db)
    {
        var visit = await visitFactory.Fetch(visitId);
        if (visit == null) return null;

        // Business logic here
        return await visit.Archive();
    }
}

// Usage - via injected delegate
var archivedVisit = await _archiveVisit(visit.Id);
```
<!-- /snippet -->

Choose the command pattern when:
- Operation needs parameters not available on the entity
- Operation spans multiple aggregates
- You want to keep the entity interface minimal

## Authorization Methods

> **Note:** Authorization is provided by [RemoteFactory](https://github.com/NeatooDotNet/RemoteFactory). For comprehensive documentation, see the [RemoteFactory documentation](https://github.com/NeatooDotNet/RemoteFactory/tree/main/docs).

Factories include authorization check methods:

<!-- pseudo:authorize-factory -->
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
<!-- /snippet -->

### UI Permission Display

<!-- pseudo:ui-permission-display -->
```razor
<MudButton Disabled="@(!personFactory.CanCreate())" OnClick="CreatePerson">
    New Person
</MudButton>
```
<!-- /snippet -->

## Child Entity Operations

Child entities don't have `[Remote]` since they're managed through the parent:

<!-- snippet: factory-child-entity -->
```cs
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
<!-- endSnippet -->

## List Factory Operations

List factories provide Save method that handles the collection.

> **Critical: Always include DeletedList in Update methods**
>
> When iterating items in a list's `[Update]` method, you must use `this.Union(DeletedList)`
> to include items that were removed from the list. Removed items are moved to `DeletedList`
> and marked `IsDeleted = true`. If you only iterate `this`, removed items will silently
> remain in the database.

<!-- snippet: list-factory -->
```cs
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
<!-- endSnippet -->

## Mapper Methods

Declare partial mapper methods; Neatoo source-generates implementations:

<!-- pseudo:mapper-declarations -->
```csharp
public partial void MapFrom(PersonEntity entity);
public partial void MapTo(PersonEntity entity);
public partial void MapModifiedTo(PersonEntity entity);
```
<!-- /snippet -->

Properties are mapped by name. The EF entity property names must match the domain model property names.

## Factory Lifecycle Callbacks

Entities receive callbacks during factory operations:

<!-- pseudo:factory-callbacks -->
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
<!-- /snippet -->

## Complete Example

<!-- pseudo:complete-order-example -->
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
<!-- /snippet -->

## See Also

- [Database-Dependent Validation](database-dependent-validation.md) - Async validation pattern
- [Remote Factory Pattern](remote-factory.md) - Client-server state transfer
- [Mapper Methods](mapper-methods.md) - MapFrom, MapTo details
- [Collections](collections.md) - Child list factories
