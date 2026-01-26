# Collections

[← Change Tracking](change-tracking.md) | [↑ Guides](index.md) | [Entities →](entities.md)

Neatoo provides specialized collection base classes for managing lists of validatable objects and entities within aggregates. These collections automatically propagate parent references to establish aggregate boundaries, aggregate validation state from all items, track modifications through the entity graph, and manage deleted items for persistence.

## ValidateListBase

ValidateListBase provides observable collection functionality for validatable objects. It aggregates validation state from all items and propagates parent references automatically when items are added.

Unlike ValidateBase and EntityBase which require DI services, collection classes can be instantiated directly with `new`. Initialize child collections in entity constructors using `LoadValue()` to establish the parent-child relationship without triggering modification tracking.

Inherit from ValidateListBase&lt;T&gt; where T implements IValidateBase:

<!-- snippet: collections-validate-list-definition -->
```cs
public class CollectionValidateItemList : ValidateListBase<CollectionValidateItem>
{
}
```
<!-- endSnippet -->

The collection automatically tracks:
- **IsValid** - True if all items in the collection are valid
- **IsSelfValid** - Always true (lists have no self validation)
- **IsBusy** - True if any item is busy executing async operations
- **PropertyMessages** - Aggregated validation messages from all items

## EntityListBase

EntityListBase extends ValidateListBase to add entity-specific persistence tracking. It enforces aggregate boundary rules, manages deleted items through the DeletedList, tracks modification state through the entity graph, and coordinates entity lifecycle events with the factory system.

Inherit from EntityListBase&lt;T&gt; where T implements IEntityBase:

<!-- snippet: collections-entity-list-definition -->
```cs
public class CollectionOrderItemList : EntityListBase<ICollectionOrderItem>, ICollectionOrderItemList
{
    public int DeletedCount => DeletedList.Count;
}
```
<!-- endSnippet -->

In addition to validation state, EntityListBase tracks:
- **IsModified** - True if any item is modified or any items are in the DeletedList
- **IsSelfModified** - Always false (lists have no self state to modify)
- **IsNew** - Always false (collections are not independently persisted)
- **IsSavable** - Always false (collections are persisted through the aggregate root)
- **DeletedList** - Protected collection of removed entities pending deletion during save

## Adding Items

Items added to a collection automatically receive parent references, establishing them within the aggregate boundary. For entity lists, the framework enforces aggregate consistency rules and manages entity state transitions.

Add items using standard collection methods:

<!-- snippet: collections-add-item -->
```cs
[Fact]
public void AddItem_SetsParentAndTracksItem()
{
    var orderFactory = GetRequiredService<ICollectionOrderFactory>();
    var order = orderFactory.Create();

    // Create an item to add
    var itemFactory = GetRequiredService<ICollectionOrderItemFactory>();
    var item = itemFactory.Create();
    item.ProductCode = "WIDGET-001";
    item.Price = 19.99m;
    item.Quantity = 2;

    // Add item using standard collection method
    order.Items.Add(item);

    // Item is now in the collection
    Assert.Single(order.Items);
    Assert.Contains(item, order.Items);

    // Item's Parent points to the order (aggregate root)
    Assert.Same(order, item.Parent);
}
```
<!-- endSnippet -->

During insertion, ValidateListBase:
- Sets the item's Parent property to the list's Parent (establishing aggregate boundary)
- Subscribes to property change events for validation state updates
- Updates cached validation state incrementally (O(1) for becoming invalid)

EntityListBase additionally enforces aggregate boundary rules:
- Validates the item isn't already in the collection (no duplicates)
- Prevents adding busy items (with async validation rules running)
- Prevents cross-aggregate moves (item.Root must match list.Root or be null)
- Marks existing entities as modified (they're being re-added to the graph)
- Marks items as child entities (IsChild = true)
- Sets the item's ContainingList property (tracks which collection owns the entity)
- Handles intra-aggregate moves (removes from old list's DeletedList, undeletes item)

## Removing Items

Removal behavior differs between ValidateListBase and EntityListBase. ValidateListBase removes items immediately since they have no persistence state. EntityListBase tracks deletions for persistence, distinguishing between new items (remove immediately) and existing items (move to DeletedList for database deletion).

Remove items from ValidateListBase:

<!-- snippet: collections-remove-validate -->
```cs
[Fact]
public void RemoveFromValidateList_RemovesImmediately()
{
    var list = new CollectionValidateItemList();
    var itemFactory = GetRequiredService<ICollectionValidateItemFactory>();
    var item = itemFactory.Create();
    item.Name = "Test Item";

    list.Add(item);
    Assert.Single(list);

    // Remove from ValidateListBase - item is removed immediately
    list.Remove(item);

    // Item is no longer in the list
    Assert.Empty(list);
}
```
<!-- endSnippet -->

The item is unsubscribed from events and removed immediately.

Remove items from EntityListBase:

<!-- snippet: collections-remove-entity -->
```cs
[Fact]
public void RemoveFromEntityList_TracksForDeletion()
{
    var orderFactory = GetRequiredService<ICollectionOrderFactory>();
    var order = orderFactory.Create();

    // Create an "existing" item (simulating loaded from database)
    var itemFactory = GetRequiredService<ICollectionOrderItemFactory>();
    var item = itemFactory.Fetch("WIDGET-001", 19.99m, 1);

    // Add fetched item to order
    order.Items.Add(item);
    order.DoMarkUnmodified();

    Assert.Single(order.Items);
    Assert.False(item.IsNew);

    // Remove from EntityListBase - existing item goes to DeletedList
    order.Items.Remove(item);

    // Item removed from active list
    Assert.Empty(order.Items);

    // Item is marked deleted and tracked for persistence
    Assert.True(item.IsDeleted);
    Assert.Equal(1, order.Items.DeletedCount);
}
```
<!-- endSnippet -->

The sample uses `DoMarkUnmodified()`, a helper method that exposes the protected `MarkUnmodified()` for demonstration purposes. In production, `MarkUnmodified()` is called automatically by the framework after Fetch or successful save operations.

For entity lists, removal behavior depends on entity state:
- **New items (IsNew == true)** - Removed immediately since they don't exist in the database
- **Existing items (IsNew == false)** - Marked deleted (IsDeleted = true) and moved to DeletedList
- **ContainingList property** - Remains set to the owning list until persistence completes
- **During save** - Repository deletes entities in DeletedList from the database
- **After successful save** - FactoryComplete fires, clearing DeletedList and nulling ContainingList references

## Parent Property Cascade

Collections automatically cascade parent references to establish aggregate boundaries. The Parent property connects items to their owning aggregate root (or intermediate entity), enabling Root navigation and aggregate consistency enforcement.

Parent references propagate when items are added:

<!-- snippet: collections-parent-cascade -->
```cs
[Fact]
public void ParentCascade_UpdatesAllItems()
{
    var orderFactory = GetRequiredService<ICollectionOrderFactory>();
    var order = orderFactory.Create();

    // Add items to the collection
    var itemFactory = GetRequiredService<ICollectionOrderItemFactory>();
    var item1 = itemFactory.Create();
    item1.ProductCode = "ITEM-001";

    var item2 = itemFactory.Create();
    item2.ProductCode = "ITEM-002";

    order.Items.Add(item1);
    order.Items.Add(item2);

    // All items have the same Parent (the aggregate root)
    Assert.Same(order, item1.Parent);
    Assert.Same(order, item2.Parent);

    // For entity lists, Root returns the aggregate root
    Assert.Same(order, item1.Root);
    Assert.Same(order, item2.Root);
}
```
<!-- endSnippet -->

This establishes the aggregate boundary. All items within the collection belong to the same aggregate root, enabling:
- **Aggregate consistency enforcement** - Cross-aggregate moves are prevented (item.Root must match list.Root)
- **Transactional boundaries** - All entities in the aggregate are persisted together
- **Validation propagation** - Validation state bubbles up through Parent references

The Parent property points to the collection's Parent (typically the aggregate root), not to the collection itself. This enables direct Parent-to-root navigation.

For entity lists, the Root property provides aggregate root access:
- **If Parent is null** - Root is null (entity is standalone, not in an aggregate)
- **If Parent implements IEntityBase** - Returns Parent.Root (recursive navigation up the graph)
- **Otherwise** - Returns Parent (Parent is the aggregate root)

## Collection Validation

Collections aggregate validation state from all child items. When any child's validation state changes, the collection's state updates automatically.

Validation state propagates through property change events:

<!-- snippet: collections-validation -->
```cs
[Fact]
public async Task ValidationState_AggregatesFromChildren()
{
    var list = new CollectionValidateItemList();

    var itemFactory = GetRequiredService<ICollectionValidateItemFactory>();

    var validItem = itemFactory.Create();
    validItem.Name = "Valid Item";
    await validItem.RunRules();

    var invalidItem = itemFactory.Create();
    // Name is empty - will be invalid
    await invalidItem.RunRules();

    // Add valid item first - collection is valid
    list.Add(validItem);
    Assert.True(list.IsValid);

    // Add invalid item - collection becomes invalid
    list.Add(invalidItem);
    Assert.False(list.IsValid);

    // IsSelfValid is always true for lists
    Assert.True(list.IsSelfValid);

    // Fix the invalid item
    invalidItem.Name = "Now Valid";
    await invalidItem.RunRules();

    // Collection becomes valid again
    Assert.True(list.IsValid);
}
```
<!-- endSnippet -->

The collection uses cached meta properties with incremental updates:
- When a child becomes invalid, IsValid immediately becomes false (O(1))
- When a child becomes valid and collection is invalid, checks if any other child is still invalid (O(k) where k = first invalid)
- Same algorithm applies to IsBusy state

Run validation rules on all items:

<!-- snippet: collections-run-rules -->
```cs
[Fact]
public async Task RunRules_ExecutesOnAllItems()
{
    var list = new CollectionValidateItemList();

    var itemFactory = GetRequiredService<ICollectionValidateItemFactory>();

    // Add items without running rules initially
    var item1 = itemFactory.Create();
    item1.Name = ""; // Invalid - empty name

    var item2 = itemFactory.Create();
    item2.Name = "Valid";

    list.Add(item1);
    list.Add(item2);

    // Run rules on all items in the collection
    await list.RunRules();

    // First item is invalid
    Assert.False(item1.IsValid);

    // Second item is valid
    Assert.True(item2.IsValid);

    // Collection aggregates the validation state
    Assert.False(list.IsValid);
}
```
<!-- endSnippet -->

This executes validation rules on every item in the collection and updates the aggregated validation state.

## Iteration and Enumeration

Collections implement IEnumerable&lt;T&gt; and support standard iteration patterns. They inherit from ObservableCollection&lt;T&gt;, providing collection change notifications.

Iterate over items:

<!-- snippet: collections-iteration -->
```cs
[Fact]
public void Iteration_SupportsStandardPatterns()
{
    var orderFactory = GetRequiredService<ICollectionOrderFactory>();
    var order = orderFactory.Create();

    var itemFactory = GetRequiredService<ICollectionOrderItemFactory>();

    // Add some items
    for (int i = 1; i <= 3; i++)
    {
        var item = itemFactory.Create();
        item.ProductCode = $"ITEM-{i:000}";
        item.Price = i * 10m;
        item.Quantity = i;
        order.Items.Add(item);
    }

    // Standard foreach iteration
    var codes = new List<string>();
    foreach (var item in order.Items)
    {
        codes.Add(item.ProductCode);
    }
    Assert.Equal(3, codes.Count);

    // LINQ queries work
    var totalValue = order.Items.Sum(i => i.Price * i.Quantity);
    Assert.Equal(140m, totalValue); // (10*1) + (20*2) + (30*3)

    // Index-based access
    var first = order.Items[0];
    Assert.Equal("ITEM-001", first.ProductCode);

    // Count property
    Assert.Equal(3, order.Items.Count);
}
```
<!-- endSnippet -->

Collections support:
- Standard foreach loops
- LINQ queries
- Index-based access with this[index]
- Count property
- Collection changed events (INotifyCollectionChanged)
- Property changed events on collection properties like Count

## Deleted List Management

EntityListBase maintains a protected DeletedList to track removed entities that need deletion during persistence. This enables the repository to delete entities from the database while maintaining aggregate consistency until the save operation completes.

The DeletedList lifecycle:

<!-- snippet: collections-deleted-list -->
```cs
[Fact]
public void DeletedList_TracksRemovedEntitiesUntilSave()
{
    var orderFactory = GetRequiredService<ICollectionOrderFactory>();
    var order = orderFactory.Create();

    var itemFactory = GetRequiredService<ICollectionOrderItemFactory>();

    // Create "existing" items (simulating loaded from database)
    var item1 = itemFactory.Fetch("ITEM-001", 10m, 1);
    var item2 = itemFactory.Fetch("ITEM-002", 20m, 2);

    // Add fetched items
    order.Items.Add(item1);
    order.Items.Add(item2);
    order.DoMarkUnmodified();

    // Remove an item - goes to DeletedList
    order.Items.Remove(item1);
    Assert.True(item1.IsDeleted);
    Assert.Equal(1, order.Items.DeletedCount);

    // Collection is modified because of DeletedList
    Assert.True(order.Items.IsModified);
}
```
<!-- endSnippet -->

Deleted items remain in DeletedList with their ContainingList property set until FactoryComplete fires after a successful save. This preserves aggregate boundaries during the save operation:

**After successful save (FactoryComplete):**
- DeletedList is cleared (entities were deleted from database)
- ContainingList on deleted items is set to null (no longer owned)
- Deleted items are no longer part of the aggregate

**If an item is re-added before save (intra-aggregate move):**
- Removed from the old list's DeletedList
- Undeleted (IsDeleted = false)
- Marked modified (entity state changed: existed, was deleted, now exists again)
- ContainingList updated to the new list

This enables moving entities between child collections within the same aggregate without database deletion. The item remains within the aggregate boundary and is updated, not deleted, during save.

## Paused Operations

Collections respect the IsPaused flag during deserialization and factory operations. Pausing prevents premature validation and change tracking while the aggregate is being reconstructed.

**While paused:**
- No validation state updates occur (prevents incomplete object validation)
- No property change events fire (avoids spurious notifications)
- Entity state transitions are deferred (modification tracking suspended)
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
- Change tracking resumes for future modifications

---

**UPDATED:** 2026-01-25
