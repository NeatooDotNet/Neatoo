# Remote Factory Integration

[← Properties](properties.md) | [↑ Guides](index.md) | [Validation →](validation.md)

Neatoo entities integrate with [RemoteFactory](https://github.com/NeatooDotNet/RemoteFactory) for factory generation and client-server execution. This guide covers how Neatoo entity state interacts with factory operations.

For RemoteFactory documentation (factory attributes, service injection, remote execution, authorization, setup), see:
- **GitHub**: [NeatooDotNet/RemoteFactory](https://github.com/NeatooDotNet/RemoteFactory)
- **Claude Code**: `/RemoteFactory` skill

The examples on this page are the `Order` aggregate (root, `OrderItem` children, `OrderItemList`) and the tests that pin its behavior. The tests are MSTest and resolve factories from a DI scope (`DesignTestServices.GetScope()`); in an application the factory interface is injected into the component that uses it.

## Save Routing Based on Entity State

When `Save()` is called on the root, the generated factory routes to one operation based on Neatoo entity state. `IsDeleted` is tested first; `IsModified` is never consulted by routing:

| Entity State | Factory Routes To | After Completion |
|--------------|-------------------|------------------|
| `IsDeleted == true`, `IsNew == false` | `[Delete]` | none — the caller discards the object |
| `IsDeleted == true`, `IsNew == true` | nothing | a created-then-deleted entity was never written |
| `IsNew == true` | `[Insert]` | `IsNew = false`, `IsModified = false` |
| otherwise | `[Update]` | `IsModified = false` |

`entity.Save()` refuses to call the factory unless `IsSavable` is true and throws `SaveOperationException`; reaching that exception is a programming error, because the UI binds its Save control to `IsSavable`. A direct `factory.Save(target)` performs no such check.

## Entity State During Factory Operations

Every factory operation runs with the object paused: assignments inside the body are a clean baseline load (nothing is marked modified, no rules run, no `PropertyChanged`). `FactoryComplete` then resumes the object and sets the persistence state for the operation.

### Create Operations

`[Create]` is the equivalent of `new`. It runs locally — no `[Remote]`, because it needs nothing from the server — and may take `[Service]` parameters that are registered on both tiers, such as the child list factory:

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

After Create completes: `IsNew = true`, `IsModified = false` (a created object needs inserting but holds no user work), `IsSavable = true` because savability admits `IsNew`.

### Fetch Operations

`[Fetch]` loads existing data by key. The client calls it, so it is `[Remote]` — the call crosses to the server, where the `[Service]` repository resolves — and `internal`, which `[Remote]` requires. The root assigns its own properties from its row and hands the row's child rows to the list factory's `Fetch`, so every child goes through its own `[Fetch]`. Returning `false` makes the generated factory return `null`:

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

`IsSavable` is `(IsModified || IsNew) && IsValid && !IsBusy`, available on aggregate roots through `IEntityRoot`. A fetched, untouched entity is not savable; an edit makes it so:

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

The root's `[Insert]` and `[Update]` re-run the rules on the server and refuse an invalid aggregate before writing — the recommended gate, which the framework does not apply for you. The root then makes or gets its row, maps itself into it, hands the row's child collection to the list factory's `Save`, and flushes once:

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

After `[Insert]` or `[Update]` completes, `FactoryComplete` marks the root unmodified and old; each child saved through its own factory is marked the same way by its own completion.

## Child Entity State Cascade

Child entities within an aggregate have their state cascade to the parent:

| Child State | Effect on Parent |
|-------------|------------------|
| `IsModified = true` | Parent `IsModified = true` |
| `IsValid = false` | Parent `IsValid = false` |
| `IsBusy = true` | Parent `IsBusy = true` |

`IsNew` does not cascade: a parent ignores its children's `IsNew`, and lists report `IsNew => false`. Attaching a child to a live parent marks the child modified, which is how the parent learns a new child arrived.

Child entities must save through the aggregate root. Their interfaces extend `IEntityBase` (not `IEntityRoot`), so `IsSavable` and `Save()` are not accessible to consumers, and their persistence operations are `internal`, never `[Remote]`: only server-side code — the list's own `[Fetch]` and `[Update]` — calls them. A child's `[Fetch]` takes the child's row:

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

A child's `[Insert]` and `[Update]` are reached through the child factory's `Save` from the list's `[Update]`, take the child's row, and write only that row. There is no child `[Delete]`: the list removes a removed child's row.

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

## DeletedList Lifecycle

When items are removed from an `EntityListBase`:

1. **New items** (`IsNew = true`) are discarded entirely — they were never persisted:

<!-- snippet: skill-remove-new-item-discarded -->
<a id='snippet-skill-remove-new-item-discarded'></a>
```cs
[TestMethod]
public void RemoveNewItem_NotAddedToDeletedList()
{
    // Arrange
    var order = _orderFactory.Create();
    var item = _itemFactory.Create("Widget", 1, 10.00m);
    order.Items!.Add(item);
    Assert.IsTrue(item.IsNew, "Created item should be new");

    // Act
    order.Items.Remove(item);

    // Assert
    Assert.AreEqual(0, order.Items.Count);
    Assert.AreEqual(0, order.Items.DeletedCount, "New items should not go to DeletedList");
}
```
<sup><a href='/src/Design/Design.Tests/AggregateTests/DeletedListTests.cs#L40-L57' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-remove-new-item-discarded' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

2. **Existing items** (`IsNew = false`) are marked deleted and added to the list's `DeletedList`:

<!-- snippet: skill-remove-fetched-item -->
<a id='snippet-skill-remove-fetched-item'></a>
```cs
[TestMethod]
public async Task Remove_FetchedItem_AddedToDeletedList()
{
    // Arrange - Fetch the root so its list holds existing (non-new) children
    var parent = await _parentFactory.Fetch();
    var list = parent.Children!;
    var item = list[0];
    Assert.IsFalse(item.IsNew, "A fetched child is not new");

    // Act
    list.Remove(item);

    // Assert
    Assert.AreEqual(2, list.Count);
    Assert.AreEqual(1, list.DeletedCount, "Removed fetched item should be in DeletedList");
}
```
<sup><a href='/src/Design/Design.Tests/BaseClassTests/EntityListBaseTests.cs#L108-L125' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-remove-fetched-item' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The `DeletedList` is drained by the list's own `[Update]`, which the root reaches by handing its row's child collection to the list factory's `Save`. The list walks `this.Union(DeletedList)`: a removed persisted child has its row removed from the collection; a new child gets a new row and its own `[Insert]`; a modified child gets its row and its own `[Update]`:

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

`DeletedList` is cleared, and `ContainingList` nulled on the removed items, in the list's `FactoryComplete(Update)`. That runs only because the list is itself a factory target; a root that writes child rows inline never triggers it.

## Serialization State Transfer

When entities cross client-server boundaries:

| State | Serialized? | Notes |
|-------|-------------|-------|
| Property values | Yes | All registered properties |
| `IsNew` | Yes | Preserved across boundary |
| `IsDeleted` | Yes | Preserved across boundary |
| `IsModified` | Yes | Preserved across boundary |
| `DeletedList` items | Yes | For pending deletes |
| Validation messages | Yes | Carried with each property; rules are not re-run on deserialization |
| `IsBusy` | No | Reset on deserialization |

Rules do not run during deserialization: the object is paused, and `OnDeserialized` only resumes it. `IsValid` on the client reflects the messages that travelled with the properties.

---

**See also:**
- [RemoteFactory Documentation](https://github.com/NeatooDotNet/RemoteFactory) - Factory attributes, service injection, remote execution
- [Entities](entities.md) - EntityBase lifecycle, state properties and the save cascade
- [Collections](collections.md) - EntityListBase and DeletedList behavior

---

**UPDATED:** 2026-10-06
