# Entities

[← Collections](collections.md) | [↑ Guides](index.md) | [Parent-Child →](parent-child.md)

When your domain objects need to be persisted, they need to answer questions that ValidateBase doesn't handle: Has anything changed since the last save? Is this a new object or an existing one? Should the save Insert, Update, or Delete? EntityBase adds this persistence awareness on top of ValidateBase's property tracking and validation, so a single `Save()` call on the aggregate root does the right thing for the whole aggregate — the root routes to its own Insert, Update or Delete, and from there each level of the graph hands its children to the next factory.

The examples are the `Order` aggregate (root, `OrderItem` children, `OrderItemList`), the `Product` root and the `DemoEntity` demo. Tests are MSTest and resolve factories from a DI scope (`DesignTestServices.GetScope()`); in an application the factory interface is injected into the component that uses it.

## EntityBase vs ValidateBase

Not every domain object needs persistence. ValidateBase exists for objects that need rules but have no persistence lifecycle of their own — a value object a person edits, form data validated before submission. It gives you property tracking, validation rules, and meta properties without modification tracking or save operations. A read model or DTO is neither: it needs no rules, so it is a plain `[Factory]` class with `[Fetch]` only and no Neatoo base class.

EntityBase builds on ValidateBase for objects that *do* have identity and a persistence lifecycle. This split mirrors the DDD distinction between entities and value objects.

ValidateBase provides:
- Property change tracking
- Validation rules
- Meta properties (IsValid, IsBusy)
- Parent property

EntityBase adds:
- Modification tracking (IsModified, IsSelfModified, ModifiedProperties)
- Persistence state (IsNew, IsDeleted)
- Save operations (Save, Delete) -- `Save()` and `IsSavable` accessible through `IEntityRoot` (aggregate roots only)
- Aggregate patterns (Root, ContainingList)
- Factory integration for Insert/Update/Delete

Every entity gets a matched public interface and an `internal` concrete; consumers only ever see the interface. A root's interface extends `IEntityRoot`:

<!-- snippet: skill-entity-crud-interface -->
<a id='snippet-skill-entity-crud-interface'></a>
```cs
/// <summary>
/// Root interface for EntityBase demo — persistent domain entities.
/// </summary>
public interface IDemoEntity : IEntityRoot
{
    string? Name { get; set; }
    int Value { get; set; }
}
```
<sup><a href='/src/Design/Design.Domain/BaseClasses/IBaseClassInterfaces.cs#L23-L32' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-entity-crud-interface' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`[Create]` is local. The persistence operations are `[Remote]` (the client calls them, so the call crosses to the server) and `internal`, and take the repository as a method `[Service]` so it resolves on the server. Inside each operation the object is paused: assignment is a clean baseline load.

<!-- snippet: skill-entity-crud -->
<a id='snippet-skill-entity-crud'></a>
```cs
/// <summary>
/// Demonstrates: EntityBase&lt;T&gt; for persistent domain entities.
///
/// Key points:
/// - Inherits all ValidateBase capabilities (validation, rules, busy tracking)
/// - Adds IsNew/IsModified/IsDeleted for persistence state
/// - IsSavable = (IsModified || IsNew) &amp;&amp; IsValid &amp;&amp; !IsBusy
/// - Save() routes to Insert/Update/Delete based on state
/// - Child entities cannot save independently: their interface has no Save()
/// </summary>
[Factory]
internal partial class DemoEntity : EntityBase<DemoEntity>, IDemoEntity
{
    public partial string? Name { get; set; }

    public partial int Value { get; set; }

    public DemoEntity(IEntityBaseServices<DemoEntity> services) : base(services)
    {
        RuleManager.AddValidation(
            t => string.IsNullOrWhiteSpace(t.Name) ? "Name is required" : string.Empty,
            t => t.Name);

        RuleManager.AddValidation(
            t => t.Value < 0 ? "Value must be non-negative" : string.Empty,
            t => t.Value);
    }

    [Create]
    public void Create()
    {
        // FactoryComplete(Create) calls MarkNew(): IsNew=true, IsModified=false.
        // Nothing was set, so there is no user work - still savable, because
        // IsSavable admits IsNew.
    }

    // [Remote]: the client fetches this root, so the call crosses to the
    // server, where the repository resolves. The object is paused for the
    // body, so plain assignment is a clean baseline load.
    [Remote]
    [Fetch]
    internal void Fetch(int id, [Service] IDemoRepository repository)
    {
        var data = repository.GetById(id);
        Name = data.Name;
        Value = data.Value;
        // After Fetch: IsNew=false, IsModified=false
    }

    // Save() routes here when IsNew. FactoryComplete(Insert) then calls
    // MarkUnmodified() and MarkOld().
    [Remote]
    [Insert]
    internal void Insert([Service] IDemoRepository repository)
    {
        repository.Insert(Name!, Value);
    }

    // Save() routes here when !IsDeleted && !IsNew. Routing never consults
    // IsModified; entity.Save()'s IsSavable gate stops unmodified saves.
    [Remote]
    [Update]
    internal void Update([Service] IDemoRepository repository)
    {
        repository.Update(Name!, Value);
    }

    // Save() routes here when IsDeleted (checked first - IsDeleted wins over IsNew)
    [Remote]
    [Delete]
    internal void Delete([Service] IDemoRepository repository)
    {
        repository.Delete(Name!);
    }
}
```
<sup><a href='/src/Design/Design.Domain/BaseClasses/AllBaseClasses.cs#L224-L300' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-entity-crud' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Root vs Child: IEntityRoot and IEntityBase

An Order with its LineItems, or a Customer with its Addresses — these are aggregates. The root (Order, Customer) is the only entry point for persistence because it enforces business rules that span the whole aggregate. If a LineItem could save itself independently, it could violate cross-entity invariants that only the Order knows about (e.g., "order total must not exceed credit limit").

Neatoo makes this distinction explicit at the type level. Aggregate root interfaces extend `IEntityRoot`, which adds `IsSavable` and `Save()`. Child entity interfaces extend `IEntityBase`, which has neither. List interfaces extend `IEntityListBase<IChild>`, parameterized on the child interface. The developer signals root vs child by choosing the interface -- no attributes, no runtime flags, no magic:

<!-- snippet: skill-aggregate-interfaces -->
<a id='snippet-skill-aggregate-interfaces'></a>
```cs
/// <summary>
/// Aggregate root interface — extends IEntityRoot.
/// Exposes IsSavable and Save() for the root entity.
/// All property types use interfaces, never concretes.
/// </summary>
public interface IOrder : IEntityRoot
{
    Guid Id { get; }
    string? OrderNumber { get; set; }
    string? CustomerName { get; set; }
    DateTime OrderDate { get; set; }
    string? Status { get; set; }
    decimal TotalAmount { get; }
    IOrderItemList? Items { get; }
}

/// <summary>
/// Child entity interface — extends IEntityBase only.
/// No IsSavable, no Save(). Child entities are saved through the aggregate root.
/// </summary>
public interface IOrderItem : IEntityBase
{
    Guid Id { get; }
    string? ProductName { get; set; }
    int Quantity { get; set; }
    decimal UnitPrice { get; set; }
    decimal LineTotal { get; }
}

/// <summary>
/// List interface — extends IEntityListBase parameterized on child INTERFACE.
/// Lists never expose IsSavable — they are always saved through the aggregate root.
/// </summary>
public interface IOrderItemList : IEntityListBase<IOrderItem>
{
    /// <summary>
    /// Test helper: Exposes the count of items in DeletedList.
    /// </summary>
    int DeletedCount { get; }
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/OrderAggregate/IOrderInterfaces.cs#L68-L109' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-aggregate-interfaces' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Why this design exists:** `IsSavable` on `EntityBase` is `(IsModified || IsNew) && IsValid && !IsBusy` — it knows nothing about aggregate position, so a modified child *concrete* reports `true`. Developers used `IsSavable` in save cascade logic to decide whether children needed persisting, and reached for `Save()` on a child, which the framework does not support. This caused a real production bug. The fix is not to teach `IsSavable` about children — it is to remove it from the child interface entirely, so the mistake is a compile error. Child entity factory methods (`[Insert]`/`[Update]`) take the child's own row, which only the list's `[Update]` can supply, and entity classes are `internal`, so external callers cannot save children at all.

The concrete `EntityBase<T>` implements both `IEntityBase` and `IEntityRoot`, so it retains `IsSavable` and `Save()` as concrete members. This does not matter because entity classes are `internal` — consumers interact through the public interface, which is the access control mechanism.

### Aggregate Root

The root owns the child list. It creates the list through the list factory inside its own `[Create]` — never with `new`, never in the constructor — and loads it through the list factory inside its `[Fetch]`:

<!-- snippet: skill-root-create -->
<a id='snippet-skill-root-create'></a>
```cs
[Create]
public void Create([Service] IOrderItemListFactory itemsFactory)
{
    Items = itemsFactory.Create();
    OrderDate = DateTime.Today;
    Status = "Draft";
    OrderNumber = $"ORD-{DateTime.Now:yyyyMMddHHmmss}";
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/OrderAggregate/Order.cs#L105-L114' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-root-create' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Aggregate roots:
- Can call `Save()` directly
- Have `Root == null` (they are the root)
- Coordinate the save: hand their row's child collection to the list factory, flush once

Child entities within the aggregate:
- Cannot call `Save()` — their interface does not expose it
- Have `Root` pointing to the aggregate root
- Are saved through the root's save, each through its own factory operation

## Identity and IsNew

`IsNew` directly drives save routing — it determines whether Save calls Insert or Update. Because the routing depends on it, the framework manages `IsNew` automatically through factory operations rather than leaving it to the developer. After Create, it's true. After Fetch or a successful Insert, it's false. There's no manual tracking to get wrong.

New entities:
- `IsNew == true`
- Trigger Insert factory method on save
- Created via the `[Create]` factory method
- Transition to existing after successful Insert

Existing entities:
- `IsNew == false`
- Trigger Update factory method on save
- Fetched via Fetch factory method
- Remain existing after Update

`IsNew` and `IsModified` answer different questions. A created entity needs inserting but holds no user work, so it is `IsNew` and *not* `IsModified` — and still savable, because `IsSavable` admits `IsNew`:

<!-- snippet: skill-create-is-new-not-modified -->
<a id='snippet-skill-create-is-new-not-modified'></a>
```cs
[TestMethod]
public void Create_SetsIsModifiedFalse_ButStillSavable()
{
    // Arrange & Act
    var entity = _factory.Create();

    // Assert - IsNew and IsModified answer different questions. A created
    // entity needs inserting (IsNew), but holds no user work (not modified),
    // so unsaved-changes guards stay quiet on it. Savability comes from the
    // IsNew term. A [Create] that IS the user's work opts in with
    // MarkModified() in its body.
    Assert.IsTrue(entity.IsNew, "New entity should have IsNew=true");
    Assert.IsFalse(entity.IsModified, "New entity holds no user work");
    Assert.IsTrue(entity.IsSavable, "...but it is savable, so the Insert can happen");
}
```
<sup><a href='/src/Design/Design.Tests/BaseClassTests/EntityBaseTests.cs#L43-L59' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-create-is-new-not-modified' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The framework manages `IsNew` through factory operations. `FactoryComplete` sets `IsNew = true` after Create and `IsNew = false` after Insert or Update. For Fetch, `IsNew` stays at its default `false` because `FactoryComplete(Fetch)` performs no state changes.

## Entity Lifecycle

Entities progress through a standard lifecycle from creation to deletion.

### New Entity Creation

Create a new entity using the Create factory method, then edit it. The edits are ordinary property dirt; `IsNew` was never part of `IsModified`:

<!-- snippet: skill-quick-start-create -->
<a id='snippet-skill-quick-start-create'></a>
```cs
[TestMethod]
public async Task Create_ThenEdit_IsSavable()
{
    var product = _factory.Create();
    Assert.IsTrue(product.IsNew);
    Assert.IsFalse(product.IsModified, "A created object holds no user work");

    product.Name = "Widget";
    product.Price = 9.99m;
    await product.WaitForTasks();

    Assert.IsTrue(product.IsValid);
    Assert.IsTrue(product.IsSavable);
}
```
<sup><a href='/src/Design/Design.Tests/EntityTests/ProductTests.cs#L35-L50' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-quick-start-create' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

After Create completes (before setting properties):
- `IsNew == true`
- `IsModified == false` — a created entity holds no user work, so unsaved-changes guards stay quiet on it ([why](change-tracking.md#why-isnew-is-not-part-of-ismodified))
- `IsSelfModified == false` (no properties changed yet)
- `IsValid` is true until a rule runs: factory operations run paused
- `IsSavable == true` — savability admits `IsNew`, so the Insert can still happen

After setting properties:
- `IsSelfModified == true` (properties were changed)
- `IsModified == true` (now true — property dirt)
- `IsSavable == true` (still savable, now with property changes for Insert)

### Fetch Existing Entity

`[Fetch]` loads existing data by key through a server-side repository; it returns `false` (the factory returns `null`) when nothing is found. The root assigns its own properties from its row and hands the row's child rows to the list factory's `Fetch`, so every child is built with its own `[Fetch]` — never with `[Create]`, which would claim the child is new and insert it again on the next save:

<!-- snippet: skill-root-fetch -->
<a id='snippet-skill-root-fetch'></a>
```cs
[Remote]
[Fetch]
internal bool Fetch(Guid id,
    [Service] IOrderRepository repository,
    [Service] IOrderItemListFactory itemsFactory)
{
    var row = repository.Get(id);
    if (row == null)
    {
        return false;
    }

    Id = row.Id;
    OrderNumber = row.OrderNumber;
    CustomerName = row.CustomerName;
    OrderDate = row.OrderDate;
    Status = row.Status;
    TotalAmount = row.TotalAmount;

    // Items and every item within: IsNew=false, IsModified=false
    Items = itemsFactory.Fetch(row.Items);

    // After Fetch completes: Order.IsNew=false, Order.IsModified=false
    return true;
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/OrderAggregate/Order.cs#L139-L165' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-root-fetch' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

After Fetch completes, every object in the graph is old and clean:

<!-- snippet: skill-fetch-starts-clean -->
<a id='snippet-skill-fetch-starts-clean'></a>
```cs
[TestMethod]
public async Task Fetch_ItemsAreOldAndClean_AggregateNotModified()
{
    // Arrange
    var seeded = _repository.SeedOrder();

    // Act
    var order = await FetchOrder(seeded.Id);

    // Assert - root state
    Assert.IsFalse(order.IsNew, "Fetched order should not be new");
    Assert.IsFalse(order.IsModified, "Fetched order should not be modified");

    // Assert - child state: each item completed its own [Fetch]
    Assert.AreEqual(2, order.Items!.Count);
    foreach (var item in order.Items)
    {
        Assert.IsFalse(item.IsNew, $"Fetched item {item.Id} should not be new");
        Assert.IsFalse(item.IsModified, $"Fetched item {item.Id} should not be modified");
    }

    Assert.AreEqual(0, order.Items.DeletedCount, "No deletions pending after fetch");
}
```
<sup><a href='/src/Design/Design.Tests/AggregateTests/AggregateLifecycleTests.cs#L58-L82' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-fetch-starts-clean' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Save Operations

`Save()` on the root routes to the root's own factory method based on the root's state. A new entity routes to `[Insert]`, and `FactoryComplete(Insert)` marks it old and unmodified:

<!-- snippet: skill-save-routes-to-insert -->
<a id='snippet-skill-save-routes-to-insert'></a>
```cs
[TestMethod]
public async Task Save_WhenNew_RoutesToInsert_AndMarksOld()
{
    // Arrange
    var entity = _factory.Create();
    entity.Name = "Inserted";
    entity.Amount = 42m;

    // Act
    entity = (ISaveDemo)await entity.Save();

    // Assert - routed to Insert, and the generated Id landed on the entity
    Assert.AreEqual(1, _repository.InsertedIds.Count, "Should route to Insert");
    Assert.AreEqual(0, _repository.UpdatedIds.Count);
    Assert.AreEqual(_repository.InsertedIds[0], entity.Id,
        "The generated Id must land on the entity");

    // Assert - FactoryComplete(Insert) marked it unmodified and old
    Assert.IsFalse(entity.IsNew, "Inserted entity is no longer new");
    Assert.IsFalse(entity.IsModified);
    Assert.IsFalse(entity.IsSavable, "Nothing left to save");
}
```
<sup><a href='/src/Design/Design.Tests/FactoryTests/SaveTests.cs#L105-L128' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-save-routes-to-insert' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Save routing, in order:
- `IsDeleted == true` → `[Delete]` (checked first; a created-then-deleted entity is a no-op — nothing was ever written)
- `IsNew == true` → `[Insert]`
- otherwise → `[Update]`

`IsModified` is not consulted by routing. `entity.Save()` refuses to call the factory unless `IsSavable` is true — `(IsModified || IsNew) && IsValid && !IsBusy` — and throws `SaveOperationException`; a direct `factory.Save(target)` does not check.

After successful Insert or Update:
- `IsModified == false`
- `IsNew == false`
- `ModifiedProperties` is cleared

`Save()` returns the saved instance. On the client it is a new object, so keep the returned one: `order = (IOrder)await order.Save();`. A created-but-untouched entity still inserts, because savability and routing both run off `IsNew`:

<!-- snippet: skill-new-untouched-still-inserts -->
<a id='snippet-skill-new-untouched-still-inserts'></a>
```cs
[TestMethod]
public async Task Save_WhenNewAndUntouched_StillInserts()
{
    // The ISNEW behavior at the routing level: a created entity carries no
    // property dirt, so it reports not-modified - and still inserts, because
    // savability and routing both run off IsNew.
    var entity = _factory.Create();
    entity.Name = "Untouched-ish";  // required for validity only

    Assert.IsTrue(entity.IsNew);
    Assert.IsTrue(entity.IsSavable);

    entity = (ISaveDemo)await entity.Save();

    Assert.AreEqual(1, _repository.InsertedIds.Count);
    Assert.IsFalse(entity.IsNew);
}
```
<sup><a href='/src/Design/Design.Tests/FactoryTests/SaveTests.cs#L167-L185' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-new-untouched-still-inserts' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Re-run the Rules on the Server Before the Write

The client's `IsSavable` gate is the client's. The root's `[Insert]` and `[Update]` re-run every rule on the server and refuse an invalid aggregate before writing. This is the recommended shape; the framework does not do it for you. Throw, never return: after `[Insert]`/`[Update]` returns, the framework marks the entity saved whether or not anything was written.

<!-- snippet: skill-root-insert -->
<a id='snippet-skill-root-insert'></a>
```cs
[Remote]
[Insert]
internal async Task Insert([Service] IOrderRepository repository,
    [Service] IOrderItemListFactory itemsFactory)
{
    // Re-run every rule on the server and refuse an invalid aggregate.
    // Recommended - the framework does not do this for you. Throw, never
    // return: after [Insert]/[Update] returns, the framework marks the
    // entity saved whether or not anything was written.
    await RunRules(RunRulesFlag.All);
    if (!IsValid)
    {
        throw new SaveOperationException(SaveFailureReason.IsInvalid);
    }

    // Object is paused — assignment is clean
    Id = Guid.NewGuid();

    var row = new OrderRow();
    MapTo(row);
    repository.Add(row);

    itemsFactory.Save(Items!, row.Items);

    repository.SaveChanges();
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/OrderAggregate/Order.cs#L186-L213' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-root-insert' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: skill-server-gate-refuses -->
<a id='snippet-skill-server-gate-refuses'></a>
```cs
[TestMethod]
public async Task InvalidOrder_DirectFactorySave_ServerRulesRefuseBeforeWriting()
{
    // Arrange: CustomerName is [Required], but rules do not run during
    // [Create], so the new order still reports valid on this side.
    var order = _orderFactory.Create();
    Assert.IsTrue(order.IsValid, "No rule has run yet, so nothing is broken");

    // Act: a direct factory.Save does not check IsSavable, so only the
    // re-run of the rules inside [Insert] stands between this order and
    // the write.
    var ex = await Assert.ThrowsExactlyAsync<Neatoo.SaveOperationException>(
        () => _orderFactory.Save(order));

    // Assert: refused, and nothing was written
    Assert.AreEqual(Neatoo.SaveFailureReason.IsInvalid, ex.Reason);
    Assert.AreEqual(0, _repository.AddedRows.Count, "No row may be added");
    Assert.AreEqual(0, _repository.SaveChangesCount, "No flush may happen");
}
```
<sup><a href='/src/Design/Design.Tests/AggregateTests/AggregateLifecycleTests.cs#L188-L208' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-server-gate-refuses' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Delete Operations

Mark an entity for deletion, then save. `IsDeleted` remains a term in `IsModified`, so the deletion is savable and routes to `[Delete]`:

<!-- snippet: skill-delete-routes-to-delete -->
<a id='snippet-skill-delete-routes-to-delete'></a>
```cs
[TestMethod]
public async Task Save_WhenDeleted_RoutesToDelete()
{
    // Arrange - a persisted entity marked for deletion
    var entity = await _factory.Fetch(9);
    entity.Delete();

    Assert.IsTrue(entity.IsDeleted);
    Assert.IsTrue(entity.IsModified, "IsDeleted remains a term in IsModified");
    Assert.IsTrue(entity.IsSavable);

    // Act
    await entity.Save();

    // Assert - routed to Delete, not Update
    CollectionAssert.AreEqual(new[] { 9 }, _repository.DeletedIds, "Should route to Delete");
    Assert.AreEqual(0, _repository.UpdatedIds.Count);
}
```
<sup><a href='/src/Design/Design.Tests/FactoryTests/SaveTests.cs#L146-L165' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-delete-routes-to-delete' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The root's `[Delete]` removes its own row; the child rows go with it as a database cascade would. There is no child `[Delete]`:

<!-- snippet: skill-root-delete -->
<a id='snippet-skill-root-delete'></a>
```cs
[Remote]
[Delete]
internal void Delete([Service] IOrderRepository repository)
{
    var row = repository.Get(Id)
        ?? throw new KeyNotFoundException($"Order {Id} not found");

    repository.Remove(row);

    repository.SaveChanges();
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/OrderAggregate/Order.cs#L278-L290' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-root-delete' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Reverse deletion before saving:

<!-- snippet: skill-undelete -->
<a id='snippet-skill-undelete'></a>
```cs
[TestMethod]
public async Task UnDelete_ReversesDeleteBeforeSave()
{
    var entity = await _factory.Fetch(1);

    entity.Delete();
    Assert.IsTrue(entity.IsDeleted);
    Assert.IsTrue(entity.IsModified, "Deleting is a modification");

    entity.UnDelete();

    Assert.IsFalse(entity.IsDeleted);
    Assert.IsFalse(entity.IsModified, "Back to the fetched baseline");
    Assert.IsFalse(entity.IsSavable, "Nothing left to save");
}
```
<sup><a href='/src/Design/Design.Tests/BaseClassTests/EntityBaseTests.cs#L143-L159' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-undelete' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

If the entity is in a collection, `Delete()` routes through the collection's `Remove()`. See [Collections](collections.md) for deleted list management.

## Parent Property

Parent navigation serves two purposes. First, it defines the aggregate boundary — the framework knows which entities belong to which aggregate by walking the parent chain. Second, it gives the parent awareness when children change, which is essential for aggregate-level business rules. A rule like "all line item percentages must sum to 100%" on the aggregate root needs to re-evaluate when any child changes. The parent can also push changes down to children.

Adding a child to a list sets `Parent` (the owning entity — the list is transparent) and `Root` (the aggregate root). The root has neither:

<!-- snippet: skill-parent-and-root -->
<a id='snippet-skill-parent-and-root'></a>
```cs
[TestMethod]
public void AddItem_SetsParentAndRoot()
{
    var order = _orderFactory.Create();
    var item = _itemFactory.Create("Widget", 5, 10.00m);
    Assert.IsNull(item.Parent, "Not attached yet");

    order.Items!.Add(item);

    // Parent is the owning entity (the list is transparent); Root is the aggregate root
    Assert.AreSame<object>(order, item.Parent!);
    Assert.AreSame<object>(order, item.Root!);

    // The root itself has neither
    Assert.IsNull(order.Parent);
    Assert.IsNull(order.Root);
}
```
<sup><a href='/src/Design/Design.Tests/AggregateTests/OrderAggregateTests.cs#L75-L93' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-parent-and-root' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Parent cascades:
- Validation state bubbles up to parent
- Modification state bubbles up to parent
- Parent changes propagate to all children in collections

For aggregate roots:
- `Parent == null` (root has no parent)
- `Root == null` (root is the root)

For child entities:
- `Parent` points to the owning entity (the list is transparent)
- `Root` navigates to the aggregate root

The Root property walks the Parent chain: if Parent is null, Root is null (this entity is a root or a standalone object); if Parent has a Root, that Root is returned; otherwise Parent is the root, and is returned. Cast `Parent` and `Root` to the root's *interface* (`(IOrder)item.Parent!`), never to the concrete class.

See [Parent-Child](parent-child.md) for detailed parent-child relationship management.

## Entity State Management

EntityBase tracks multiple state dimensions through meta properties.

### Modification State

When an aggregate saves, not every entity in the graph necessarily changed. `IsModified` tells the aggregate root "something in my subtree needs saving". `IsSelfModified` tells *this specific entity* "I have changes that need an Update call." An Order might have `IsModified == true` because a LineItem changed, but `IsSelfModified == false` — the Order writes no columns of its own, it just hands its children to the list factory.

A fetched entity is a clean baseline; a property set after the fetch marks the property and the entity modified:

<!-- snippet: skill-fetch-then-modify -->
<a id='snippet-skill-fetch-then-modify'></a>
```cs
[TestMethod]
public async Task Fetch_ThenModify_IsModified()
{
    // Arrange
    var entity = await _factory.Fetch(1);

    // Act
    entity.Name = "Changed";

    // Assert
    Assert.IsTrue(entity.IsModified, "Entity should be modified after change");
    Assert.IsTrue(entity["Name"].IsModified, "Name property should be modified");
}
```
<sup><a href='/src/Design/Design.Tests/PropertyTests/StatePropertyTests.cs#L74-L88' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-fetch-then-modify' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`ModifiedProperties` lists which of the entity's own properties changed:

<!-- snippet: docs-modified-properties -->
<a id='snippet-docs-modified-properties'></a>
```cs
[TestMethod]
public void PropertyChange_MarksPropertyModified()
{
    // Arrange
    var entity = _factory.Create();

    // Act
    entity.Name = "Test Name";

    // Assert
    Assert.IsTrue(entity.IsSelfModified);
    Assert.IsTrue(entity.ModifiedProperties.Contains("Name"));
}
```
<sup><a href='/src/Design/Design.Tests/BaseClassTests/EntityBaseTests.cs#L94-L108' title='Snippet source file'>snippet source</a> | <a href='#snippet-docs-modified-properties' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

A child's change makes the parent modified, but not self-modified:

<!-- snippet: skill-child-change-modifies-parent -->
<a id='snippet-skill-child-change-modifies-parent'></a>
```cs
[TestMethod]
public async Task Gotcha5_ChildModification_SetsParentIsModified()
{
    // Arrange
    var factory = _scope.GetRequiredService<IGotcha5ParentFactory>();

    // Fetch parent (starts as unmodified)
    var parent = await factory.Fetch(1);
    Assert.IsFalse(parent.IsModified, "Freshly fetched parent should not be modified");
    Assert.IsFalse(parent.IsSelfModified, "Freshly fetched parent should not be self-modified");

    // Act - Modify only the CHILD
    parent.Child!.Value = "Changed Value";
    await parent.WaitForTasks();

    // Assert
    Assert.IsTrue(parent.Child.IsSelfModified, "Child should be self-modified");
    Assert.IsTrue(parent.Child.IsModified, "Child should be modified");
    Assert.IsFalse(parent.IsSelfModified, "Parent itself is NOT modified - only child changed");
    Assert.IsTrue(parent.IsModified, "Parent.IsModified should be TRUE because child is modified");
}
```
<sup><a href='/src/Design/Design.Tests/GotchaTests/CommonGotchaTests.cs#L258-L280' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-child-change-modifies-parent' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

- **IsModified**: True if any property changed, the entity is deleted (`IsDeleted == true`), or it was explicitly marked modified. Includes child modifications cascading from collections. `IsNew` is not part of it.
- **IsSelfModified**: True if the entity's own properties changed, the entity is deleted, or it was explicitly marked modified. Excludes child modifications.
- **ModifiedProperties**: Collection of property names that changed since last save.
- **IsMarkedModified**: Explicitly marked modified via `MarkModified()`.

`MarkModified()` is a **protected** method on `EntityBase`: an entity calls it on itself to say the graph holds work worth keeping without any property having changed. Its one doctrinal use is a `[Create]` whose result *is* the user's work (a "New" button rather than a derived default) — the body calls `MarkModified()` so an unsaved-changes guard bound to `IsModified` speaks. It is not needed to make a new object savable; `IsSavable` already admits `IsNew`.

Modification state is cleared by the framework after a successful save. `FactoryComplete(Insert)` and `FactoryComplete(Update)` call `MarkUnmodified()` and `MarkOld()` on the object that was the factory target; a child saved through its own factory is cleared by its own completion. Users do not call `MarkUnmodified()` directly. A saved entity is clean, and a fetched one starts clean:

<!-- snippet: skill-quick-start-save -->
<a id='snippet-skill-quick-start-save'></a>
```cs
[TestMethod]
public async Task Save_ThenFetch_RoundTrips()
{
    var product = _factory.Create();
    product.Name = "Widget";
    product.Price = 9.99m;
    await product.WaitForTasks();

    // Save returns the saved instance; keep that one
    product = (IProduct)await product.Save();
    Assert.IsFalse(product.IsNew);
    Assert.IsFalse(product.IsModified);

    var fetched = await _factory.Fetch(product.Id);
    Assert.IsNotNull(fetched);
    Assert.AreEqual("Widget", fetched.Name);
    Assert.IsFalse(fetched.IsModified, "A fetched object is a clean baseline");
}
```
<sup><a href='/src/Design/Design.Tests/EntityTests/ProductTests.cs#L52-L71' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-quick-start-save' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Persistence State

Persistence state determines which factory method executes on save:

| After | `IsNew` | `IsDeleted` |
|-------|---------|-------------|
| Create | `true` | `false` |
| Fetch | `false` | `false` |
| `Delete()` | unchanged | `true` |
| Insert | `false` | `false` |
| Update | `false` | `false` |

The routing tests above (`Save_WhenNew_RoutesToInsert_AndMarksOld`, `Save_WhenDeleted_RoutesToDelete`) and `UnDelete_ReversesDeleteBeforeSave` pin these transitions.

### Savability

Neatoo is a DDD framework designed for data-binding UIs. `IsSavable` is meant to be bound directly to a Save button's `Enabled` state — the framework manages the conditions, and the UI reflects whether saving is possible right now. No conditional logic needed in the view.

`IsSavable` is exposed through `IEntityRoot` (the aggregate root interface), not `IEntityBase` (the child entity interface). This is deliberate — `IsSavable` knows nothing about aggregate position, so a modified child concrete reports `true` even though children are persisted by their root. Removing it from the child interface prevents the trap of using `IsSavable` in save cascade logic at all.

<!-- snippet: skill-is-savable -->
<a id='snippet-skill-is-savable'></a>
```cs
[TestMethod]
public async Task FetchedEntity_NotSavableWhenUnmodified()
{
    // Arrange
    var entity = await _factory.Fetch(1);

    // Assert
    Assert.IsFalse(entity.IsNew);
    Assert.IsFalse(entity.IsModified);
    Assert.IsFalse(entity.IsSavable, "Unmodified fetched entity should not be savable");
}

[TestMethod]
public async Task FetchedEntity_IsSavableWhenModified()
{
    // Arrange
    var entity = await _factory.Fetch(1);

    // Act
    entity.Name = "Updated Name";

    // Assert
    Assert.IsFalse(entity.IsNew);
    Assert.IsTrue(entity.IsModified);
    Assert.IsTrue(entity.IsValid);
    Assert.IsTrue(entity.IsSavable, "Modified valid entity should be savable");
}
```
<sup><a href='/src/Design/Design.Tests/FactoryTests/SaveTests.cs#L69-L97' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-is-savable' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

An invalid entity is not savable:

<!-- snippet: skill-invalid-not-savable -->
<a id='snippet-skill-invalid-not-savable'></a>
```cs
[TestMethod]
public async Task NewEntity_NotSavableWhenInvalid()
{
    // Arrange
    var entity = _factory.Create();
    entity.Name = "Valid First";
    Assert.IsTrue(entity.IsValid);

    // Act - Make it invalid
    entity.Name = null;
    await entity.WaitForTasks();

    // Assert
    Assert.IsTrue(entity.IsNew);
    Assert.IsFalse(entity.IsValid);
    Assert.IsFalse(entity.IsSavable, "Invalid entity should not be savable");
}
```
<sup><a href='/src/Design/Design.Tests/FactoryTests/SaveTests.cs#L49-L67' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-invalid-not-savable' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

IsSavable is true when:
- `IsModified == true` or `IsNew == true` (a reason to persist)
- `IsValid == true` (passes validation)
- `IsBusy == false` (no async operations running)

### Child Entity State

A child's interface extends `IEntityBase`, which has neither `IsSavable` nor `Save()`. The barrier is the type system:

<!-- snippet: skill-child-interface-no-save -->
<a id='snippet-skill-child-interface-no-save'></a>
```cs
[TestMethod]
public void ChildInterface_DoesNotExposeIsSavable()
{
    // Arrange — IOrderItem extends IEntityBase, not IEntityRoot
    var order = _orderFactory.Create();
    var item = _itemFactory.Create("Widget", 5, 10.00m);
    order.Items!.Add(item);

    // Act — Cast to IEntityBase (which IOrderItem extends)
    // Intentionally using interface type to demonstrate the pattern
#pragma warning disable CA1859
    IEntityBase entityBase = item;
#pragma warning restore CA1859

    // Assert — IEntityBase does NOT have IsSavable
    // This is verified by the fact that the following would NOT compile:
    //   entityBase.IsSavable  // CS1061: IEntityBase does not contain IsSavable
    //   entityBase.Save()     // CS1061: IEntityBase does not contain Save
    Assert.AreSame<object>(order, entityBase.Root!, "Child entity belongs to the aggregate");
    Assert.IsTrue(entityBase.IsModified, "Child entity should be modified");
}
```
<sup><a href='/src/Design/Design.Tests/AggregateTests/EntityRootInterfaceTests.cs#L48-L70' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-child-interface-no-save' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Child entities:
- Their interfaces extend `IEntityBase` (not `IEntityRoot`), so `IsSavable` and `Save()` are not accessible to consumers
- Have ContainingList set to the owning collection when added to an EntityListBase, which routes their `Delete()` through the list
- Are saved through the aggregate root's save operation, each through its own factory operation

## Aggregate Save Cascading

Neatoo handles state cascading and save cascading differently.

### State Cascades UP Automatically

The framework propagates `IsModified`, `IsValid`, and `IsBusy` up the object graph. When a child becomes modified, its parent's `IsModified` becomes true. This is framework behavior — no code required.

### Saves Cascade DOWN Through the Factories

**The framework does NOT auto-save children.** `Save()` on the root routes to the root's `[Insert]` or `[Update]`. From there the shape is fixed:

1. The root re-runs the rules and refuses an invalid aggregate.
2. The root makes its row (`Insert`) or gets it from the repository (`Update`), maps itself into it, and hands the row's child collection to the **list factory's** `Save`.
3. The list's `[Update]` walks `this.Union(DeletedList)`: a removed persisted child has its row removed from the collection; a new child gets a new row, added to the collection, and is handed it through the **child factory's** `Save`; a modified child is handed its existing row the same way; an unmodified child is skipped.
4. The child maps itself into the row it is handed. In `[Insert]` it sets its own key.
5. The root flushes once.

Every object that changed goes through its own factory operation, which is what marks it clean afterward: `FactoryComplete` fires per factory target, never as a cascade from the parent.

The root's `[Update]` writes its own columns only when its own properties changed, then delegates:

<!-- snippet: skill-root-update -->
<a id='snippet-skill-root-update'></a>
```cs
[Remote]
[Update]
internal async Task Update([Service] IOrderRepository repository,
    [Service] IOrderItemListFactory itemsFactory)
{
    // Re-run every rule on the server and refuse an invalid aggregate.
    // Recommended - the framework does not do this for you. Throw, never
    // return: after [Insert]/[Update] returns, the framework marks the
    // entity saved whether or not anything was written.
    await RunRules(RunRulesFlag.All);
    if (!IsValid)
    {
        throw new SaveOperationException(SaveFailureReason.IsInvalid);
    }

    var row = repository.Get(Id)
        ?? throw new KeyNotFoundException($"Order {Id} not found");

    // Write the order's own columns only if they changed
    if (IsSelfModified)
    {
        MapTo(row);
    }

    itemsFactory.Save(Items!, row.Items);

    repository.SaveChanges();
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/OrderAggregate/Order.cs#L240-L269' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-root-update' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The list's `[Update]` brings the row collection in line with the list and drains the `DeletedList`:

<!-- snippet: skill-list-update -->
<a id='snippet-skill-list-update'></a>
```cs
[Update]
internal void Update(ICollection<OrderItemRow> rows,
                     [Service] IOrderItemFactory itemFactory)
{
    foreach (var item in this.Union(DeletedList))
    {
        if (item.IsDeleted)
        {
            // The !IsNew check is defensive: a new item removed from the
            // list is discarded (never enters DeletedList), so deleted
            // items reaching here are expected to be persisted ones.
            if (!item.IsNew)
            {
                rows.Remove(rows.Single(r => r.Id == item.Id));
            }
        }
        else if (item.IsNew)
        {
            var row = new OrderItemRow();
            rows.Add(row);
            itemFactory.Save(item, row);
        }
        else if (item.IsModified)
        {
            itemFactory.Save(item, rows.Single(r => r.Id == item.Id));
        }
    }
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/OrderAggregate/OrderItemList.cs#L102-L131' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-list-update' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The child maps itself into its row. Its persistence operations are `internal`, never `[Remote]`, take no repository, and take a row only the list's `[Update]` can supply. There is no child `[Delete]`:

<!-- snippet: skill-child-insert-update -->
<a id='snippet-skill-child-insert-update'></a>
```cs
[Insert]
internal void Insert(OrderItemRow row)
{
    // The entity sets its own key. Paused - plain assignment stays clean.
    Id = Guid.NewGuid();
    MapTo(row);
}

[Update]
internal void Update(OrderItemRow row)
{
    MapTo(row);
}

private void MapTo(OrderItemRow row)
{
    row.Id = Id;
    row.ProductName = ProductName!;
    row.Quantity = Quantity;
    row.UnitPrice = UnitPrice;
    row.LineTotal = LineTotal;
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/OrderAggregate/OrderItem.cs#L141-L164' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-child-insert-update' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

External code only saves the aggregate root. A child added to a fetched aggregate gets exactly one row, and a second save does not insert it again:

<!-- snippet: skill-save-root-only -->
<a id='snippet-skill-save-root-only'></a>
```cs
[TestMethod]
public async Task AddChildToFetchedOrder_SaveTwice_InsertsOnce()
{
    var seeded = _orderRepo.SeedOrder();  // two item rows
    var order = await FetchOrder(seeded.Id);
    var added = _itemFactory.Create("Added", 1, 5.00m);
    order.Items!.Add(added);
    await order.WaitForTasks();

    order = (IOrder)await order.Save();
    var stored = _orderRepo.Store[seeded.Id];
    Assert.AreEqual(3, stored.Items.Count, "The added child gets one new row");

    // A second save must not re-insert - the child was marked old
    order.CustomerName = "Touched again";
    await order.WaitForTasks();
    await order.Save();

    Assert.AreEqual(3, stored.Items.Count,
        "The child must not be inserted a second time");
    Assert.AreEqual(1, stored.Items.Count(r => r.Id == added.Id),
        "The added child has exactly one row");
}
```
<sup><a href='/src/Design/Design.Tests/AggregateTests/AggregateCoverageGapTests.cs#L235-L259' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-save-root-only' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

A root that writes its children's rows inline instead of delegating leaves every child modified and new after the save, inserts new children again on the next save, and never clears the `DeletedList`. Each child maps itself; the list's `[Update]` coordinates.

## Factory Integration

Persistence methods live inside the entity class. The entity's lifecycle state management — marking clean, transitioning from new to existing — is handled by protected methods that only the framework's factory completion calls. If persistence lived in an external class, those state transitions would need to be public API, and they'd inevitably get misused. By keeping factory methods on the entity, the class owns both its data and its lifecycle: the root maps itself into its row and sets its own key in `[Insert]`.

A complete root. `[Create]` is local; `Fetch`/`Insert`/`Update`/`Delete` are `[Remote] internal` client entry points that take the repository as a `[Service]`:

<!-- snippet: skill-quick-start-interface -->
<a id='snippet-skill-quick-start-interface'></a>
```cs
/// <summary>
/// Aggregate root interface. Extends IEntityRoot: exposes IsSavable and Save().
/// </summary>
public interface IProduct : IEntityRoot
{
    Guid Id { get; }
    string? Name { get; set; }
    decimal Price { get; set; }
}
```
<sup><a href='/src/Design/Design.Domain/Entities/Product.cs#L16-L26' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-quick-start-interface' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: skill-quick-start -->
<a id='snippet-skill-quick-start'></a>
```cs
/// <summary>
/// Demonstrates: the minimal aggregate root. Concrete is internal; consumers
/// hold IProduct.
/// </summary>
[Factory]
internal partial class Product : EntityBase<Product>, IProduct
{
    public partial Guid Id { get; set; }

    [Required(ErrorMessage = "Name is required")]
    public partial string? Name { get; set; }

    [Range(0, 1000000, ErrorMessage = "Price cannot be negative")]
    public partial decimal Price { get; set; }

    public Product(IEntityBaseServices<Product> services) : base(services) { }

    // Local: creating a product needs nothing from the server
    [Create]
    public void Create() { }

    // [Remote]: the client fetches this root, so the call crosses to the
    // server, where the repository resolves. Returning false makes the
    // generated factory return null: "no such product" is an answer.
    [Remote]
    [Fetch]
    internal bool Fetch(Guid id, [Service] IProductRepository repository)
    {
        var row = repository.Get(id);
        if (row == null)
        {
            return false;
        }

        // Paused for the length of the body: assignment is a clean baseline load
        Id = row.Id;
        Name = row.Name;
        Price = row.Price;
        return true;
    }

    [Remote]
    [Insert]
    internal async Task Insert([Service] IProductRepository repository)
    {
        // Re-run the rules on the server and refuse an invalid aggregate.
        // Throw, never return: after [Insert] returns, the framework marks
        // the entity saved whether or not anything was written.
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        Id = Guid.NewGuid();  // the entity sets its own key

        var row = new ProductRow();
        MapTo(row);
        repository.Add(row);
        repository.SaveChanges();
    }

    [Remote]
    [Update]
    internal async Task Update([Service] IProductRepository repository)
    {
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        var row = repository.Get(Id)
            ?? throw new KeyNotFoundException($"Product {Id} not found");

        MapTo(row);
        repository.SaveChanges();
    }

    [Remote]
    [Delete]
    internal void Delete([Service] IProductRepository repository)
    {
        var row = repository.Get(Id)
            ?? throw new KeyNotFoundException($"Product {Id} not found");

        repository.Remove(row);
        repository.SaveChanges();
    }

    private void MapTo(ProductRow row)
    {
        row.Id = Id;
        row.Name = Name!;
        row.Price = Price;
    }
}
```
<sup><a href='/src/Design/Design.Domain/Entities/Product.cs#L28-L126' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-quick-start' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The generated `IProductFactory` has `Create()`, `Fetch(Guid)` and `Save(IProduct)`; `[Insert]`, `[Update]` and `[Delete]` are reached through `Save`, which the framework routes on the entity's state. The generated `IFactorySave<T>` is injected into the entity through `IEntityBaseServices<T>`, which is what makes `entity.Save()` work. After each factory operation, the framework updates entity state — setting `IsNew`, clearing modification tracking — so the lifecycle stays in sync with persistence without manual intervention.

## Cancellation Support

`Save(CancellationToken)` checks the token before any persistence:

<!-- snippet: skill-save-cancellation -->
<a id='snippet-skill-save-cancellation'></a>
```cs
[TestMethod]
public async Task Save_WithCancelledToken_ThrowsAndLeavesStateUnchanged()
{
    var entity = _factory.Create();
    entity.Name = "Pending";

    using var cts = new CancellationTokenSource();
    await cts.CancelAsync();

    // Save checks the token before any persistence
    await Assert.ThrowsAsync<OperationCanceledException>(() => entity.Save(cts.Token));

    Assert.AreEqual(0, _repository.InsertedIds.Count, "Nothing was written");
    Assert.IsTrue(entity.IsNew, "State is unchanged");
    Assert.IsTrue(entity.IsModified);
}
```
<sup><a href='/src/Design/Design.Tests/FactoryTests/SaveTests.cs#L187-L204' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-save-cancellation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Cancellation behavior:
- Checks cancellation before persistence begins
- Throws OperationCanceledException if canceled before persistence starts
- Passes the CancellationToken through to your factory methods — it's your choice whether to honor it
- After the factory method returns, Neatoo assumes the operation completed and updates entity state accordingly

Neatoo checks the token before kicking off persistence, but once your Insert/Update/Delete method is running, persistence is your responsibility. The framework doesn't forcibly cancel mid-operation.

---

**UPDATED:** 2026-10-06
