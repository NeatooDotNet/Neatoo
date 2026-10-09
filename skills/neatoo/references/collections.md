# Collections

`EntityListBase<I>` provides a collection of child entities with automatic parent-child relationship management, validation cascading, and change tracking.

## Basic Collection Definition

The list is parameterized on the child **interface**, implements its own public list interface, and is `internal`. Its `[Fetch]` and `[Update]` are `internal`, never `[Remote]`:

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

The list loads its children through the child factory's `[Fetch]`, so each lands `IsNew=false`. The list is paused by its own factory operation while items are added, so nothing is marked modified:

<!-- snippet: skill-list-fetch -->
<a id='snippet-skill-list-fetch'></a>
```cs
[Fetch]
internal void Fetch(IEnumerable<OrderItemRow> rows,
                    [Service] IOrderItemFactory itemFactory)
{
    foreach (var row in rows)
    {
        Add(itemFactory.Fetch(row));
    }
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/OrderAggregate/OrderItemList.cs#L61-L71' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-list-fetch' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The parent creates the list through the list factory inside its `[Create]`, never with `new`:

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

## Adding Items

A child whose `[Create]` takes parameters is created by its factory and added:

<!-- snippet: skill-add-item -->
<a id='snippet-skill-add-item'></a>
```cs
[TestMethod]
public void AddItem_ItemJoinsAggregate()
{
    // Arrange
    var order = _orderFactory.Create();
    var item = _itemFactory.Create("Widget", 5, 10.00m);

    // Act
    order.Items!.Add(item);

    // Assert
    Assert.AreSame<object>(order, item.Root!, "Added item belongs to the aggregate");
    Assert.AreEqual(1, order.Items.Count);
}
```
<sup><a href='/src/Design/Design.Tests/AggregateTests/OrderAggregateTests.cs#L58-L73' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-add-item' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

A child whose `[Create]` takes no parameters is added by a method on the list that creates and adds it (`AddPhoneNumber()`), so the caller never holds a child factory for it.

## Removing Items

Removing a fetched (persisted) item moves it to the `DeletedList` and marks it deleted:

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

For `ValidateListBase`, there is no persistence and no `DeletedList`; a removed item is simply gone:

<!-- snippet: skill-validate-list-remove -->
<a id='snippet-skill-validate-list-remove'></a>
```cs
[TestMethod]
public void Remove_ItemLeavesImmediately()
{
    var list = _listFactory.Create();
    var item = _itemFactory.Create("Test Item");
    list.Add(item);

    // No persistence, so no DeletedList: the item is simply gone
    list.Remove(item);

    Assert.AreEqual(0, list.Count);
}
```
<sup><a href='/src/Design/Design.Tests/BaseClassTests/ValidateListBaseTests.cs#L81-L94' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-validate-list-remove' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Parent and Root

Adding sets `Parent` (the owning entity) and `Root` (the aggregate root) on the item:

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

## Collection Validation

A list is valid when every item is valid; `IsSelfValid` is always true for a list, which has no rules of its own:

<!-- snippet: skill-list-validity-aggregates -->
<a id='snippet-skill-list-validity-aggregates'></a>
```cs
[TestMethod]
public async Task Add_InvalidItem_ListBecomesInvalid()
{
    // Arrange
    var list = _listFactory.Create();
    var validItem = _itemFactory.Create("Valid");
    list.Add(validItem);
    Assert.IsTrue(list.IsValid);

    // Act - Add item then make it invalid
    var itemToInvalidate = _itemFactory.Create("Initially Valid");
    list.Add(itemToInvalidate);
    itemToInvalidate.Name = ""; // Make invalid
    await itemToInvalidate.WaitForTasks();

    // Assert
    Assert.IsFalse(list.IsValid, "List should be invalid if any child is invalid");
}
```
<sup><a href='/src/Design/Design.Tests/BaseClassTests/ValidateListBaseTests.cs#L60-L79' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-list-validity-aggregates' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`RunRules()` on a list runs every item's rules. Use it to force a re-run — for example after a `[Create]` that set values while the object was paused — not as a routine step before reading `IsValid`. After a live edit, `await WaitForTasks()` is what waits for async rules:

<!-- snippet: skill-run-rules-after-create -->
<a id='snippet-skill-run-rules-after-create'></a>
```cs
[TestMethod]
public async Task Address_InvalidAddressType_IsInvalid()
{
    // Address.Create(street, city, state, zip, type) - the overload
    // AddressList documents as the RIGHT way to copy across aggregates,
    // which nothing called.
    var address = _addressFactory.Create("1 Main St", "Springfield", "IL", "62701", "Vacation");
    await address.WaitForTasks();

    // Factory operations run paused, so no rule has evaluated this data yet -
    // the object reports valid until something asks. This is why a factory
    // method that must not produce invalid objects calls RunRules() itself.
    Assert.IsTrue(address.IsValid, "Rules have not run yet - the factory op was paused");

    await address.RunRules();
    Assert.IsFalse(address.IsValid, "Address type must be Home, Work, or Other");

    // A live edit runs rules automatically
    address.AddressType = "Work";
    await address.WaitForTasks();
    Assert.IsTrue(address.IsValid);
}
```
<sup><a href='/src/Design/Design.Tests/AggregateTests/AggregateCoverageGapTests.cs#L206-L229' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-run-rules-after-create' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Iterating Collections

`EntityListBase<I>` and `ValidateListBase<I>` extend `ObservableCollection<I>`: `foreach`, LINQ, indexer and `Count` work as on any collection, and iteration sees only active items — removed items live in the protected `DeletedList`.

## Deleted Items

A pending deletion makes the list, and so the parent, modified:

<!-- snippet: skill-deleted-list-marks-modified -->
<a id='snippet-skill-deleted-list-marks-modified'></a>
```cs
[TestMethod]
public async Task Remove_FetchedItem_MarksItemDeletedAndListModified()
{
    var parent = await _parentFactory.Fetch();
    var list = parent.Children!;
    var item = list[0];
    Assert.IsFalse(list.IsModified, "A fetched list is clean");

    list.Remove(item);

    Assert.IsTrue(item.IsDeleted, "The removed item is marked for deletion");
    Assert.IsTrue(list.IsModified, "A pending deletion makes the list modified");
    Assert.IsTrue(parent.IsModified, "...and the parent with it");
}
```
<sup><a href='/src/Design/Design.Tests/BaseClassTests/EntityListBaseTests.cs#L127-L142' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-deleted-list-marks-modified' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Deletion State Behavior

Understanding how EntityListBase handles removal is critical for correct persistence:

### New vs Existing Item Removal

| Item State | On Remove | Result |
|------------|-----------|--------|
| `IsNew == true` | Removed entirely | Item is gone—nothing to delete from database |
| `IsNew == false` | Moved to DeletedList | Item marked `IsDeleted`, tracked for DELETE |

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

### Intra-Aggregate Moves (Re-adding Removed Items)

Re-adding an item that was removed from another list of the same aggregate (an order with `PendingItems` and `CompletedItems`):

1. Item is removed from the old list's DeletedList
2. `UnDelete()` is called → `IsDeleted = false`
3. Item is marked modified (state changed)
4. `ContainingList` updated to new list

This moves items between collections within the same aggregate without deleting a row.

### After Save Completes

The list is saved through its own factory operation (the root hands the row's child collection to the list factory's `Save`). When the list's `FactoryComplete(FactoryOperation.Update)` fires:

| Action | Purpose |
|--------|---------|
| `DeletedList.Clear()` | The rows were removed |
| `ContainingList = null` on deleted items | No longer owned by any collection |
| Recalculate the cached modified state | Items were marked unmodified by their own saves |

This cleanup happens only because the list is a factory target. A root that writes child rows itself never triggers it.

### Adding an Item Marks It Modified

Attaching a child to a live list marks the child and the list modified, whether the child is new or already persisted:

<!-- snippet: skill-add-marks-modified -->
<a id='snippet-skill-add-marks-modified'></a>
```cs
[TestMethod]
public void IsModified_TrueWhenNewItemAdded()
{
    // Arrange
    var order = _orderFactory.Create();

    // Act
    var item = _itemFactory.Create("Widget", 1, 10.00m);
    order.Items!.Add(item);

    // Assert - New order with new items is modified
    Assert.IsTrue(order.Items.IsModified);
    Assert.IsTrue(order.IsModified);
}
```
<sup><a href='/src/Design/Design.Tests/AggregateTests/DeletedListTests.cs#L111-L126' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-add-marks-modified' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

This is intentional—attaching an entity to a live parent is a change to that graph, and must be persisted.

It is also load-bearing for **new** items. `IsNew` never aggregates upward (a parent ignores its children's `IsNew`; lists report `IsNew => false`), so modification state is the only channel by which a parent learns a child was attached. Without the mark, adding a newly created child to a fetched aggregate would leave the parent clean and unsavable.

Items added while the collection is **paused** — the list's own `[Fetch]` populating it, or deserialization — are baseline population and are not marked. That is what keeps a fetched graph clean.

### Cross-Aggregate Transfer

Entities cannot be moved directly between aggregates. The list throws:

<!-- snippet: skill-cross-aggregate-add-throws -->
<a id='snippet-skill-cross-aggregate-add-throws'></a>
```cs
[TestMethod]
public async Task AddItemFromAnotherAggregate_Throws_WithDistinguishingMessage()
{
    // Arrange - two separate Order aggregates, each with fetched children
    var (order1, order2) = await FetchTwoOrders();
    var itemFromOrder1 = order1.Items![0];

    Assert.AreNotSame(order1, order2);
    Assert.AreSame(order1, itemFromOrder1.Root, "Item's Root is its own aggregate");

    // Act & Assert - the boundary is enforced
    var ex = Assert.ThrowsExactly<InvalidOperationException>(
        () => order2.Items!.Add(itemFromOrder1));

    // The message must distinguish the two aggregates. Both are Orders, so
    // naming types alone would say "'Order' ... 'Order'" and read as a bug.
    // "Order" as a literal: the concrete type is internal (interface-first),
    // so the test cannot reference it - which is itself the pattern working
    StringAssert.Contains(ex.Message, "different");
    StringAssert.Contains(ex.Message, "Order");
    Assert.IsFalse(
        ex.Message.Contains("belongs to aggregate 'Order', but this list belongs to aggregate 'Order'"),
        "The message must not render both aggregates identically");
}
```
<sup><a href='/src/Design/Design.Tests/AggregateTests/AggregateBoundaryTests.cs#L45-L70' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-cross-aggregate-add-throws' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

To move data between aggregates, create a new child in the target aggregate from the original's values and remove the original from the source. A client cannot re-fetch a child on its own (child `[Fetch]` is `internal`):

<!-- snippet: skill-cross-aggregate-copy -->
<a id='snippet-skill-cross-aggregate-copy'></a>
```cs
[TestMethod]
public async Task CopyAndRemove_IsTheSupportedWayToMoveBetweenAggregates()
{
    // The pattern OrderItemList.cs documents as RIGHT: copy the data into a
    // new child of the target aggregate, remove the original from the source
    var (order1, order2) = await FetchTwoOrders();
    var original = order1.Items![0];

    var copy = _itemFactory.Create(original.ProductName!, original.Quantity, original.UnitPrice);
    order2.Items!.Add(copy);
    order1.Items.Remove(original);

    Assert.AreSame(order2, copy.Root);
    Assert.AreEqual(1, order1.Items.DeletedCount, "The original is queued for deletion");
    Assert.IsTrue(order1.IsModified);
    Assert.IsTrue(order2.IsModified);
}
```
<sup><a href='/src/Design/Design.Tests/AggregateTests/AggregateBoundaryTests.cs#L86-L104' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-cross-aggregate-copy' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

---

## ValidateListBase vs EntityListBase

| Feature | ValidateListBase | EntityListBase |
|---------|-----------------|----------------|
| Change Tracking | Yes | Yes |
| Validation | Yes | Yes |
| Parent Reference | Yes | Yes |
| Deleted List | No | Yes |
| Persistence | No | Yes (via root) |
| Cross-Aggregate Check | No | Yes |
| INotifyCollectionChanged | Yes | Yes |

Both list base classes implement `INotifyCollectionChanged`. Blazor does not subscribe to it on its own; the page re-renders when a component that observes the list calls `StateHasChanged`.

Use `ValidateListBase<I>` for collections of `ValidateBase` objects:

<!-- snippet: skill-validate-list -->
<a id='snippet-skill-validate-list'></a>
```cs
/// <summary>
/// Demonstrates: ValidateListBase&lt;I&gt; for collections of ValidateBase items.
///
/// Key points:
/// - Extends ObservableCollection&lt;I&gt; with validation aggregation
/// - IsValid = all children are valid
/// - IsBusy = any child is busy
/// - Parent-child relationships managed automatically
/// </summary>
[Factory]
internal partial class DemoValueObjectList : ValidateListBase<IDemoValueObject>, IDemoValueObjectList
{
    // ValidateListBase has no required constructor - uses default.

    [Create]
    public void Create()
    {
        // Start with empty list
    }

    [Remote]
    [Fetch]
    internal void Fetch([Service] IDemoRepository repository, [Service] IDemoValueObjectFactory valueObjectFactory)
    {
        // The list is paused by its own factory operation (FactoryStart),
        // like any factory target. Each item is loaded by its own [Fetch].
        foreach (var name in repository.GetAllNames())
        {
            Add(valueObjectFactory.Fetch(name));
        }
    }
}
```
<sup><a href='/src/Design/Design.Domain/BaseClasses/AllBaseClasses.cs#L331-L364' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-validate-list' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Related

- [Entities](entities.md) - The save cascade: the list's `[Update]`
- [Validation](validation.md) - Collection validation
