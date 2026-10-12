# Collections

[← Change Tracking](change-tracking.md) | [↑ Guides](index.md) | [Entities →](entities.md)

Neatoo provides specialized collection base classes for managing lists of validatable objects and entities within aggregates. These collections automatically propagate parent references to establish aggregate boundaries, aggregate validation state from all items, track modifications through the entity graph, and manage deleted items for persistence.

The examples are the `Order` aggregate (`Order`, `OrderItem`, `OrderItemList`) and the smaller `DemoParent`/`DemoChild`/`DemoEntityList` and `DemoInputModel`/`DemoInputModelList` demos. Tests are MSTest and resolve factories from a DI scope (`DesignTestServices.GetScope()`).

## Interface-First

Every list gets a matched public interface and an `internal` concrete, like every entity. The list interface is parameterized on the child **interface**, never the concrete, so consumers only ever see `IOrderItem`:

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

    /// <summary>
    /// Verb: moves the order to Submitted. Sets state; never persists.
    /// </summary>
    void Submit();
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
<sup><a href='/src/Design/Design.Domain/Aggregates/OrderAggregate/IOrderInterfaces.cs#L68-L114' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-aggregate-interfaces' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## ValidateListBase

ValidateListBase provides observable collection functionality for validatable objects. It aggregates validation state from all items and propagates parent references automatically when items are added. Use it for collections of `ValidateBase` objects — items that need rules but no persistence lifecycle.

A list is a `[Factory]` class like any other Neatoo object: the parent creates it through the list factory inside its `[Create]`, and loads it through the list factory inside its `[Fetch]`. The list's own `[Fetch]` builds each item through the item factory's `[Fetch]`:

<!-- snippet: skill-validate-list-interface -->
<a id='snippet-skill-validate-list-interface'></a>
```cs
/// <summary>
/// List interface for ValidateListBase demo — parameterized on child INTERFACE.
/// </summary>
public interface IDemoInputModelList : IValidateListBase<IDemoInputModel> { }
```
<sup><a href='/src/Design/Design.Domain/BaseClasses/IBaseClassInterfaces.cs#L53-L58' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-validate-list-interface' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

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
internal partial class DemoInputModelList : ValidateListBase<IDemoInputModel>, IDemoInputModelList
{
    // ValidateListBase has no required constructor - uses default.

    [Create]
    public void Create()
    {
        // Start with empty list
    }

    [Remote]
    [Fetch]
    internal void Fetch([Service] IDemoRepository repository, [Service] IDemoInputModelFactory inputModelFactory)
    {
        // The list is paused by its own factory operation (FactoryStart),
        // like any factory target. Each item is loaded by its own [Fetch].
        foreach (var name in repository.GetAllNames())
        {
            Add(inputModelFactory.Fetch(name));
        }
    }
}
```
<sup><a href='/src/Design/Design.Domain/BaseClasses/AllBaseClasses.cs#L333-L366' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-validate-list' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The collection automatically tracks:
- **IsValid** - True if all items in the collection are valid
- **IsSelfValid** - Always true (lists have no self validation)
- **IsBusy** - True if any item is busy executing async operations
- **PropertyMessages** - Aggregated validation messages from all items

## EntityListBase

EntityListBase extends ValidateListBase to add entity-specific persistence tracking. It enforces aggregate boundary rules, manages deleted items through the DeletedList, tracks modification state through the entity graph, and coordinates entity lifecycle events with the factory system.

The child interface extends `IEntityBase` (no `IsSavable`, no `Save()`); the root that owns the list extends `IEntityRoot`; the list interface is parameterized on the child interface:

<!-- snippet: skill-entity-list-interfaces -->
<a id='snippet-skill-entity-list-interfaces'></a>
```cs
/// <summary>
/// Child interface for the EntityListBase demo. Extends IEntityBase: a child
/// has no IsSavable and no Save().
/// </summary>
public interface IDemoChild : IEntityBase
{
    string? Name { get; set; }
}

/// <summary>
/// Root interface that owns the EntityListBase demo list.
/// </summary>
public interface IDemoParent : IEntityRoot
{
    IDemoEntityList? Children { get; }
}
```
<sup><a href='/src/Design/Design.Domain/BaseClasses/IBaseClassInterfaces.cs#L34-L51' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-entity-list-interfaces' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: skill-entity-list-interface -->
<a id='snippet-skill-entity-list-interface'></a>
```cs
/// <summary>
/// List interface for EntityListBase demo — parameterized on child INTERFACE.
/// </summary>
public interface IDemoEntityList : IEntityListBase<IDemoChild>
{
    int DeletedCount { get; }
}
```
<sup><a href='/src/Design/Design.Domain/BaseClasses/IBaseClassInterfaces.cs#L60-L68' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-entity-list-interface' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The list's `[Fetch]` is `internal` and never `[Remote]`: the root's `[Fetch]` calls it on the server. Each child is loaded through the child factory's `[Fetch]`, so every child lands `IsNew=false`, and the list is paused by its own factory operation while items are added, so nothing is marked modified:

<!-- snippet: skill-entity-list -->
<a id='snippet-skill-entity-list'></a>
```cs
/// <summary>
/// Demonstrates: EntityListBase&lt;I&gt; for collections of child entities.
///
/// Key points:
/// - Extends ValidateListBase with persistence tracking
/// - IsModified = any child modified OR DeletedList has items
/// - DeletedList tracks removed non-new items for persistence deletion
/// - Adding items: set ContainingList (routes the child's Delete through the list)
/// - Removing non-new items: MarkDeleted(), add to DeletedList
/// - Root property for aggregate boundary enforcement
/// - Parameterized on a CHILD interface (IDemoChild : IEntityBase), never on
///   a root interface
/// </summary>
[Factory]
internal partial class DemoEntityList : EntityListBase<IDemoChild>, IDemoEntityList
{
    // DESIGN DECISION: EntityListBase doesn't define IsSavable or Save().
    // Lists are ALWAYS saved through their parent aggregate root: the root's
    // [Insert]/[Update] hands its row's child collection to the list
    // factory's Save, and the list's own [Update] brings that collection in
    // line (see Aggregates/OrderAggregate/OrderItemList.cs).

    /// <summary>
    /// Test helper: Exposes the count of items in DeletedList.
    /// The DeletedList is protected, but tests need to verify deletion behavior.
    /// </summary>
    public int DeletedCount => DeletedList.Count;

    [Create]
    public void Create()
    {
        // Empty list
    }

    // Child list operations are internal and never [Remote]: the root's
    // [Fetch] calls this on the server.
    [Fetch]
    internal void Fetch(IEnumerable<string> names, [Service] IDemoChildFactory childFactory)
    {
        foreach (var name in names)
        {
            Add(childFactory.Fetch(name));
        }
    }
}
```
<sup><a href='/src/Design/Design.Domain/BaseClasses/AllBaseClasses.cs#L443-L489' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-entity-list' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The root creates the list through the list factory inside its own `[Create]` and loads it inside its `[Fetch]` — never with `new`, and never in the constructor:

<!-- snippet: skill-parent-creates-list -->
<a id='snippet-skill-parent-creates-list'></a>
```cs
/// <summary>
/// Root that owns DemoEntityList, so the list demo has fetched (not new)
/// children. Persistence is shown in Aggregates/OrderAggregate.
/// </summary>
[Factory]
internal partial class DemoParent : EntityBase<DemoParent>, IDemoParent
{
    public partial IDemoEntityList? Children { get; set; }

    public DemoParent(IEntityBaseServices<DemoParent> services) : base(services) { }

    // The list is created through its own factory inside the parent's [Create]
    [Create]
    public void Create([Service] IDemoEntityListFactory listFactory)
    {
        Children = listFactory.Create();
    }

    [Remote]
    [Fetch]
    internal void Fetch([Service] IDemoRepository repository, [Service] IDemoEntityListFactory listFactory)
    {
        Children = listFactory.Fetch(repository.GetAllNames());
    }
}
```
<sup><a href='/src/Design/Design.Domain/BaseClasses/AllBaseClasses.cs#L520-L546' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-parent-creates-list' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

In addition to validation state, EntityListBase tracks:
- **IsModified** - True if any item is modified or any items are in the DeletedList
- **IsSelfModified** - Always false (lists have no self state to modify)
- **IsNew** - Always false (collections are not independently persisted)
- **DeletedList** - Protected collection of removed entities pending deletion during save

`IsSavable` is not present on entity lists. Lists are always persisted through the aggregate root, and the property was dead code that invited misuse.

## Adding Items

A child whose `[Create]` takes parameters is created by its factory and added with the standard collection method. Adding sets the item's `Parent` and `Root` and attaches it to the aggregate:

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

A child whose `[Create]` takes no parameters is added by a method on the list that creates and adds it, so the caller never holds a child factory for it.

During insertion, ValidateListBase:
- Sets the item's Parent property to the list's Parent (establishing aggregate boundary)
- Subscribes to property change events for validation state updates
- Updates cached validation state incrementally (O(1) for becoming invalid)

EntityListBase additionally enforces aggregate boundary rules:
- Validates the item isn't already in the collection (no duplicates)
- Prevents adding busy items (with async validation rules running)
- Prevents cross-aggregate moves (item.Root must match list.Root or be null)
- Marks the added item modified, new or existing, so the parent becomes modified
- Sets the item's ContainingList property (tracks which collection owns the entity, and routes the child's `Delete()` through the list)
- Handles intra-aggregate moves (removes from old list's DeletedList, undeletes item)

## Removing Items

Removal behavior differs between ValidateListBase and EntityListBase. ValidateListBase removes items immediately since they have no persistence state. EntityListBase tracks deletions for persistence, distinguishing between new items (remove immediately) and existing items (move to DeletedList for database deletion).

Remove items from ValidateListBase — the item is unsubscribed from events and simply gone:

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

Remove a fetched (persisted) item from EntityListBase — it is marked deleted and moved to the `DeletedList`:

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

For entity lists, removal behavior depends on entity state:
- **New items (IsNew == true)** - Removed immediately since they don't exist in the database
- **Existing items (IsNew == false)** - Marked deleted (IsDeleted = true) and moved to DeletedList
- **ContainingList property** - Remains set to the owning list until the list's save completes
- **During save** - the list's own `[Update]` removes each `DeletedList` item's row (see [Deleted List Management](#deleted-list-management))
- **After the list's save** - its `FactoryComplete(Update)` clears `DeletedList` and nulls `ContainingList` on the removed items

## Parent Property Cascade

Collections automatically cascade parent references to establish aggregate boundaries. The Parent property connects items to their owning aggregate root (or intermediate entity), enabling Root navigation and aggregate consistency enforcement.

Adding an item sets `Parent` (the owning entity — the list is transparent) and `Root` (the aggregate root); the root itself has neither:

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

This establishes the aggregate boundary. All items within the collection belong to the same aggregate root, enabling:
- **Aggregate consistency enforcement** - Cross-aggregate moves are prevented (item.Root must match list.Root)
- **Transactional boundaries** - All entities in the aggregate are persisted together
- **Validation propagation** - Validation state bubbles up through Parent references

The Parent property points to the collection's Parent (typically the aggregate root), not to the collection itself. This enables direct Parent-to-root navigation.

For entity lists, the Root property provides aggregate root access:
- **If Parent is null** - Root is null (entity is standalone, not in an aggregate)
- **If Parent has a Root** - that Root is returned (navigation up the graph)
- **Otherwise** - Parent is the root, and is returned

## Collection Validation

Collections aggregate validation state from all child items. When any child's validation state changes, the collection's state updates automatically. Rules run when a property is set; `await WaitForTasks()` is what waits for async rules before reading `IsValid`:

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

The collection uses cached meta properties with incremental updates:
- When a child becomes invalid, IsValid immediately becomes false (O(1))
- When a child becomes valid and collection is invalid, checks if any other child is still invalid (O(k) where k = first invalid)
- Same algorithm applies to IsBusy state

`RunRules()` on a list runs every item's rules. It is a forced re-run — for example after a `[Create]` that set values while the object was paused, when no rule has evaluated the data yet — not a routine step before reading `IsValid`:

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

## Iteration and Enumeration

`EntityListBase<I>` and `ValidateListBase<I>` extend `ObservableCollection<I>`: `foreach`, LINQ, the indexer and `Count` work as on any collection, and `INotifyCollectionChanged` fires for adds and removes. Iteration sees only active items; removed items live in the protected `DeletedList`.

## Deletion State Behavior

EntityListBase handles removal differently based on entity state. Understanding these transitions is critical for correct persistence behavior.

### New vs Existing Item Removal

| Item State | On Remove | DeletedList | IsDeleted | ContainingList |
|------------|-----------|-------------|-----------|----------------|
| `IsNew == true` | Removed entirely | Unchanged | N/A | Stays set |
| `IsNew == false` | Tracked for deletion | Item added | `true` | Stays set |

New items (never persisted) are removed immediately since there's nothing to delete from the database:

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

### Intra-Aggregate Moves

When you re-add an item that was removed from the same aggregate (or a different collection within the aggregate):

1. Item is removed from the old list's DeletedList
2. `UnDelete()` is called → `IsDeleted = false`
3. Item is marked modified (state transition occurred)
4. `ContainingList` is updated to the new list

This enables moving entities between child collections within the same aggregate without database deletion. The entity remains in the aggregate boundary and is updated (not deleted/re-inserted) during save.

### Cross-Aggregate Transfer

Entities cannot be moved directly between aggregates. Attempting to add an entity with a different `Root` throws `InvalidOperationException` ("Aggregate boundaries cannot be crossed"):

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

To move data between aggregates, create a new child in the target aggregate from the original's values and remove the original from the source:

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

### Adding an Item Marks It Modified

Attaching a child to a live list marks the item and the collection modified, whether the item is new or already persisted. This is intentional — attaching an entity to a parent is a change to that graph that must be persisted, and because `IsNew` never aggregates upward it is the only channel by which a parent learns a new child arrived:

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

Items added while the collection is paused — the list's own `[Fetch]` populating it, or deserialization — are baseline population and are not marked. That is what keeps a fetched graph clean.

---

## Deleted List Management

EntityListBase maintains a protected DeletedList to track removed entities that need deletion during persistence. A pending deletion makes the list, and so the parent, modified:

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

Deleted items remain in DeletedList with their ContainingList property set until the list's own save completes. Nothing in the framework drains the `DeletedList` for you: the root hands its row's child collection to the list factory's `Save`, and the list's `[Update]` walks `this.Union(DeletedList)` — removing a removed persisted child's row, giving a new child a new row and its own `[Insert]`, and giving a modified child its row and its own `[Update]`:

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

**After the list's save (its `FactoryComplete(Update)`):**
- DeletedList is cleared (the rows were removed)
- ContainingList on deleted items is set to null (no longer owned)
- The cached modified state is recalculated

This cleanup happens only because the list is a factory target. A root that writes child rows itself never triggers it, and its `DeletedList` survives the save.

**If an item is re-added before save (intra-aggregate move):**
- Removed from the old list's DeletedList
- Undeleted (IsDeleted = false)
- Marked modified (entity state changed: existed, was deleted, now exists again)
- ContainingList updated to the new list

## Paused Operations

Collections respect the IsPaused flag during deserialization and factory operations. Pausing prevents premature validation and change tracking while the aggregate is being reconstructed.

**While paused:**
- No validation state updates occur (prevents incomplete object validation)
- No property change events fire, and none are replayed afterward
- Items added are not marked modified (baseline population)
- Deleted items can be added to DeletedList during deserialization (restoring persisted state)

**Framework automatically pauses during:**
- JSON deserialization (OnDeserializing attribute hook)
- Factory operations (FactoryStart, before data loading begins)

**Framework automatically resumes after:**
- JSON deserialization complete (OnDeserialized attribute hook)
- Factory operation complete (FactoryComplete, after entity state finalized)

**After resuming:**
- Cached validation state (IsValid, IsBusy) is recalculated from all items
- Cached modification state (IsModified) is recalculated from all items and DeletedList
- No rules run; change tracking resumes for future modifications

---

**UPDATED:** 2026-10-06
