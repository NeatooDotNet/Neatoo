# Entities

`EntityBase<T>` provides the foundation for persistent entities with full CRUD lifecycle, change tracking, and save routing. For base class definitions and factory method examples, see [base-classes.md](base-classes.md).

## Create Is `new`, Fetch Loads Existing Data

`[Create]` is the equivalent of `new`: it takes what a constructor would take, runs locally, and leaves the object `IsNew=true`. `[Fetch]` loads existing data by key through a server-side repository; it leaves the object `IsNew=false, IsModified=false`, and returns false (the factory returns null) when nothing is found. A loaded child is built with its own `[Fetch]`, never with `[Create]` — a child built with `Create` claims to be new and the next save inserts it again. An operation that may create or load is a `[Remote, Execute]`, not a `[Fetch]`.

## Entity Lifecycle

### Creating New Entities

After Create: `IsNew = true` and `IsModified = **false**` — the object needs persisting, but holds no user work, so unsaved-changes guards stay quiet on it. It is savable anyway: `IsSavable` admits `IsNew`. After Fetch: `IsNew = false` and `IsModified = false` (the object matches its persisted state).

`IsModified` does **not** include `IsNew`. A `[Create]` whose result *is* the user's work declares that with `MarkModified()` in the factory method body — not to make it savable (it already is), but to say a guard should speak.

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

A created-but-untouched entity still inserts; savability and routing both run off `IsNew`:

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
<sup><a href='/src/Design/Design.Tests/FactoryTests/SaveTests.cs#L166-L184' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-new-untouched-still-inserts' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Fetching Existing Entities

A root's `[Fetch]` is `[Remote]` and `internal`, takes the key and a `[Service]` repository, and hands its row's child rows to the list factory's `Fetch`:

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

Every object in the fetched graph lands old and clean:

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

### Saving Entities

`Save()` routes to the appropriate operation based on state. A new entity routes to `[Insert]`, and `FactoryComplete(Insert)` marks it old and unmodified:

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
<sup><a href='/src/Design/Design.Tests/FactoryTests/SaveTests.cs#L104-L127' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-save-routes-to-insert' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Save Routing:**
- `IsDeleted == true` → `[Delete]` (checked first; a created-then-deleted entity is a no-op)
- `IsNew == true` → `[Insert]`
- otherwise → `[Update]`

`IsModified` is not consulted by routing. `entity.Save()` refuses to call the factory unless `IsSavable` is true; a direct `factory.Save(target)` does not check.

### Deleting Entities

Mark for deletion, then save:

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
<sup><a href='/src/Design/Design.Tests/FactoryTests/SaveTests.cs#L145-L164' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-delete-routes-to-delete' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Undeleting Entities

Restore a deleted entity before save:

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
<sup><a href='/src/Design/Design.Tests/BaseClassTests/EntityBaseTests.cs#L141-L157' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-undelete' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## FactoryComplete Lifecycle Hook

`FactoryComplete(FactoryOperation operation)` runs after every factory operation (`Create`, `Fetch`, `Insert`, `Update`, `Delete`; the enum also has `None` and `Execute`). The base implementation resumes the paused object and sets the persistence state: `MarkNew()` after `Create`, `MarkUnmodified()` and `MarkOld()` after `Insert` and `Update`.

An override must call `base.FactoryComplete(operation)` first. Anything it assigns after that call is a live edit: the object is already resumed, so rules run and the entity reports `IsModified=true`. Set defaults and derived values inside the factory method itself, where the object is paused. Reserve an override for work that sets no state.

## Change Tracking

### IsModified and IsSelfModified

A property set after the factory operation marks the property and the entity modified:

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

`IsModified` includes children; `IsSelfModified` is this object alone:

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

### Marking Modified

`MarkModified()` is a **protected** method on `EntityBase`: an entity calls it on itself to say the graph holds work worth keeping without any property having changed. Its one doctrinal use is a `[Create]` whose result *is* the user's work (a "New" button rather than a derived default) — the body calls `MarkModified()` so an unsaved-changes guard bound to `IsModified` speaks. It is not needed to make a new object savable; `IsSavable` already admits `IsNew`.

## IsSavable

`IsSavable` is `(IsModified || IsNew) && IsValid && !IsBusy`: a reason to persist, and nothing blocking it.

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
<sup><a href='/src/Design/Design.Tests/FactoryTests/SaveTests.cs#L68-L96' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-is-savable' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/Design/Design.Tests/FactoryTests/SaveTests.cs#L48-L66' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-invalid-not-savable' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`IsSavable` says nothing about aggregate position -- children are saved through the aggregate root, and that is enforced by the interface split, not by `IsSavable`. Only `IEntityRoot` exposes it.

## Child Entity State

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

## Save Cancellation

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
<sup><a href='/src/Design/Design.Tests/FactoryTests/SaveTests.cs#L186-L203' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-save-cancellation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Parent and Root

A child added to a list gets `Parent` (the owning entity; the list is transparent) and `Root` (the aggregate root). The root has neither:

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

## Aggregate Save Cascading

Neatoo handles state cascading and save cascading differently.

### State Cascades UP Automatically

The framework propagates `IsModified`, `IsValid`, and `IsBusy` up the object graph. When a child becomes modified, its parent's `IsModified` becomes true. This is framework behavior — no code required.

```
Grandchild.Name = "new"  →  Grandchild.IsModified = true
                          →  Child.IsModified = true      (automatic)
                          →  Root.IsModified = true        (automatic)
```

### Saves Cascade DOWN Through the Factories

**The framework does NOT auto-save children.** `Save()` on the root routes to the root's `[Insert]` or `[Update]`. From there the shape is fixed:

1. The root re-runs the rules and refuses an invalid aggregate.
2. The root makes its row (`Insert`) or gets it from the repository (`Update`), maps itself into it, and hands the row's child collection to the **list factory's** `Save`.
3. The list's `[Update]` walks `this.Union(DeletedList)`: a removed persisted child has its row removed from the collection; a new child gets a new row, added to the collection, and is handed it through the **child factory's** `Save`; a modified child is handed its existing row the same way; an unmodified child is skipped.
4. The child maps itself into the row it is handed. In `[Insert]` it sets its own key.
5. The root flushes once.

Every object that changed goes through its own factory operation, which is what marks it clean afterward: `FactoryComplete` fires per factory target, never as a cascade from the parent. A parent that writes child rows itself leaves every child modified and new.

The root's `[Insert]`. The entity sets its own key (`Guid.NewGuid()`); an item row belongs to the order because it sits in the order row's `Items` collection, so no foreign key is passed down:

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

The root's `[Update]` writes its own columns only when its own properties changed, then delegates the same way:

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

The list's `[Update]` brings the row collection in line with the list:

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

The child maps itself into its row. Its persistence operations are `internal`, never `[Remote]`, take no repository, and take a row only the list's `[Update]` can supply:

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

There is no child `[Delete]`: the list removes a removed child's row. The root's `[Delete]` removes its own row, and the child rows go with it as a database cascade would:

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

### Rules

1. **Only the aggregate root is saved externally** — external code calls `root.Save()` (or `factory.Save(root)`), never a child factory.
2. **Each level delegates to the next factory** — root to list factory, list to child factory. The root never inspects or writes child rows.
3. **One flush per root save** — the root calls `SaveChanges()` once, after the list is done.
4. **Keep the returned instance** — on the client, `Save()` returns the saved instance; assign it back: `order = (IOrder)await order.Save();`.

### Anti-Pattern: Parent Writes Child Rows Inline

Do NOT put child persistence (row mapping) in the parent's `[Insert]`/`[Update]`:

```csharp
// WRONG — inside the root's [Update]: the rows get written, but no child factory operation runs
foreach (var item in Items!)
{
    row.Items.First(r => r.Id == item.Id).Quantity = item.Quantity;  // parent maps the child
}
```

The aggregate still reports `IsModified=true` after Save, new children keep `IsNew=true` and get another row on the next Save, and the `DeletedList` never clears. Each child maps itself; the list's `[Update]` coordinates.

### Anti-Pattern: Flat Save

Do NOT save entities from a flat coordinator that reaches into the hierarchy:

```csharp
// WRONG — bypasses the cascade
await consultationFactory.Save(consultation);  // saves its visits
await visitFactory.Save(visit);                // already saved by consultation
```

External code calls `Save()` on the root only.

## Child Entity Factory Method Visibility

| Method | Visibility | Rationale |
|--------|-----------|-----------|
| `[Create]` | `public`, local | Client code calls `itemFactory.Create(...)` then `order.Items.Add(item)`. A child whose `[Create]` takes no parameters is added by a method on the list that creates and adds. |
| `[Fetch]` | `internal` | Called only by the list's `[Fetch]` on the server, with the child's row |
| `[Insert]` | `internal` | Reached only through the child factory's `Save` from the list's `[Update]`, with the child's row |
| `[Update]` | `internal` | Same |
| `[Delete]` | none | The list's `[Update]` removes a removed child's row |

Child persistence operations are never `[Remote]`: they run on the server because only server code calls them. `[Remote]` requires `internal` (NF0105: `[Remote]` cannot be used with a public method); an `internal` method without `[Remote]` is a server-only method the client cannot reach.

`[Remote]` means "client entry point", not "runs on the server". It marks the operations the client calls — the root's `Fetch`, `Insert`, `Update`, `Delete` — and makes those calls cross to the server, where the `[Service]` repository is. A `[Create]` with no server dependency needs no `[Remote]`; it runs wherever it is called:

<!-- snippet: skill-remote-entry-point -->
<a id='snippet-skill-remote-entry-point'></a>
```cs
[Create]
public void Create()
{
    // No persistence, no server-only services needed
    // Can run on client or server
}

// The client fetches this root, so it is a client entry point: [Remote]
// makes the client call cross to the server, where the repository lives.
[Remote]
[Fetch]
internal void Fetch(int id, [Service] IRemoteDemoRepository repository)
{
    // This method body runs on SERVER only.
    // repository is resolved from server's DI container.
    var data = repository.GetById(id);
    Id = data.Id;
    Name = data.Name;
}
```
<sup><a href='/src/Design/Design.Domain/FactoryOperations/RemoteBoundary.cs#L82-L102' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-remote-entry-point' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

A child's `[Create]`:

<!-- snippet: skill-child-create -->
<a id='snippet-skill-child-create'></a>
```cs
[Create]
public void Create()
{
    Quantity = 1;
}

[Create]
public void Create(string productName, int quantity, decimal unitPrice)
{
    ProductName = productName;
    Quantity = quantity;
    UnitPrice = unitPrice;
    // LineTotal calculated by rule
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/OrderAggregate/OrderItem.cs#L57-L72' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-child-create' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

A child's `[Fetch]`, called from the list's `[Fetch]` with the child's row:

<!-- snippet: skill-child-fetch -->
<a id='snippet-skill-child-fetch'></a>
```cs
[Fetch]
internal void Fetch(OrderItemRow row)
{
    Id = row.Id;
    ProductName = row.ProductName;
    Quantity = row.Quantity;
    UnitPrice = row.UnitPrice;
    LineTotal = row.LineTotal;
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/OrderAggregate/OrderItem.cs#L88-L98' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-child-fetch' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Internal methods get `IsServerRuntime` guards in the generated factory implementation, making them trimmable on the client. See [Trimming](trimming.md).

A type that must be both a root in one graph and a child in another needs two interfaces (`IEntityRoot` and `IEntityBase`) and two sets of operations. Do not bolt root-shaped `[Remote]` operations onto a child type: any parent-less persistence operation makes the generator emit a public `Save(target)` that lets a consumer persist the child outside its aggregate.

## Related

- [Collections](collections.md) - Child entity collections
- [Validation](validation.md) - IsValid and validation rules
- [Domain Logic Placement](domain-logic-placement.md) - Where business logic belongs
- [Trimming](trimming.md) - IL trimming annotations and consumer project setup
