# Collections

[← Change Tracking](change-tracking.md) | [↑ Guides](index.md) | [Entities →](entities.md)

Neatoo provides specialized collection base classes for managing lists of validatable objects and entities within aggregates. These collections automatically propagate parent references, aggregate validation state, track modifications, and manage deleted items for persistence.

## ValidateListBase

ValidateListBase provides observable collection functionality for value objects and validates items. It aggregates validation state from all child items and supports parent-child relationship management.

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

EntityListBase extends ValidateListBase to add entity-specific persistence tracking. It manages deleted items, modification state, and entity lifecycle across the collection.

Inherit from EntityListBase&lt;T&gt; where T implements IEntityBase:

<!-- snippet: collections-entity-list-definition -->
```cs
public class CollectionOrderItemList : EntityListBase<ICollectionOrderItem>, ICollectionOrderItemList
{
    public int DeletedCount => DeletedList.Count;

    // Expose factory methods for testing
    public void DoFactoryStart(FactoryOperation operation) => FactoryStart(operation);
    public void DoFactoryComplete(FactoryOperation operation) => FactoryComplete(operation);
}
```
<!-- endSnippet -->

In addition to validation state, EntityListBase tracks:
- **IsModified** - True if any item is modified or items are in the deleted list
- **IsSelfModified** - Always false (lists have no self state)
- **IsNew** - Always false (collections don't have persistence state)
- **IsSavable** - Always false (saved through parent aggregate)
- **DeletedList** - Internal collection of removed items pending deletion

## Adding Items

Items added to a collection automatically receive parent and root references. For entity lists, additional state management occurs.

Add items using standard collection methods:

<!-- snippet: collections-add-item -->
```cs
[Fact]
public void AddItem_SetsParentAndTracksItem()
{
    var order = new CollectionOrder(new EntityBaseServices<CollectionOrder>());

    // Create an item to add
    var item = new CollectionOrderItem(new EntityBaseServices<CollectionOrderItem>());
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
- Sets the item's Parent property to the list's Parent
- Subscribes to property change events
- Updates cached validation state incrementally

EntityListBase additionally:
- Validates the item isn't already in the collection
- Prevents adding busy items (with async rules running)
- Prevents cross-aggregate moves (item.Root must match list.Root)
- Marks existing items as modified
- Marks items as child entities
- Sets the item's ContainingList property
- Handles re-adding previously deleted items (undeletes them)

## Removing Items

Removal behavior differs between ValidateListBase and EntityListBase. Value object lists simply remove items, while entity lists track deletions for persistence.

Remove items from ValidateListBase:

<!-- snippet: collections-remove-validate -->
```cs
[Fact]
public void RemoveFromValidateList_RemovesImmediately()
{
    var list = new CollectionValidateItemList();
    var item = new CollectionValidateItem(new ValidateBaseServices<CollectionValidateItem>());
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
    var order = new CollectionOrder(new EntityBaseServices<CollectionOrder>());

    // Create an "existing" item (simulating loaded from database)
    var item = new CollectionOrderItem(new EntityBaseServices<CollectionOrderItem>());
    item.ProductCode = "WIDGET-001";
    item.Price = 19.99m;
    item.DoMarkOld();        // Mark as existing (not new)
    item.DoMarkUnmodified(); // Clear modification tracking

    // Add during fetch operation
    order.Items.DoFactoryStart(FactoryOperation.Fetch);
    order.Items.Add(item);
    order.Items.DoFactoryComplete(FactoryOperation.Fetch);

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

For entity lists:
- New items (IsNew == true) are removed immediately
- Existing items are marked deleted and moved to DeletedList
- ContainingList remains set until persistence completes
- Deleted items are persisted during save operations
- DeletedList is cleared after successful save

## Parent Property Cascade

Collections automatically cascade parent references to child items. When a collection's parent changes, all items receive the updated parent.

Parent cascade occurs when:

<!-- snippet: collections-parent-cascade -->
```cs
[Fact]
public void ParentCascade_UpdatesAllItems()
{
    var order = new CollectionOrder(new EntityBaseServices<CollectionOrder>());

    // Add items to the collection
    var item1 = new CollectionOrderItem(new EntityBaseServices<CollectionOrderItem>());
    item1.ProductCode = "ITEM-001";

    var item2 = new CollectionOrderItem(new EntityBaseServices<CollectionOrderItem>());
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

This ensures all items in the aggregate graph maintain correct parent references. The Parent property points to the owning aggregate root (or intermediate entity), not the list itself.

For entity lists, the Root property navigates from Parent:
- If Parent implements IEntityBase, returns Parent.Root
- Otherwise returns Parent (meaning Parent is the aggregate root)

## Collection Validation

Collections aggregate validation state from all child items. When any child's validation state changes, the collection's state updates automatically.

Validation state propagates through property change events:

<!-- snippet: collections-validation -->
```cs
[Fact]
public async Task ValidationState_AggregatesFromChildren()
{
    var list = new CollectionValidateItemList();

    var validItem = new CollectionValidateItem(new ValidateBaseServices<CollectionValidateItem>());
    validItem.Name = "Valid Item";
    await validItem.RunRules();

    var invalidItem = new CollectionValidateItem(new ValidateBaseServices<CollectionValidateItem>());
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

    // Add items without running rules initially
    var item1 = new CollectionValidateItem(new ValidateBaseServices<CollectionValidateItem>());
    item1.Name = ""; // Invalid - empty name

    var item2 = new CollectionValidateItem(new ValidateBaseServices<CollectionValidateItem>());
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
    var order = new CollectionOrder(new EntityBaseServices<CollectionOrder>());

    // Add some items
    for (int i = 1; i <= 3; i++)
    {
        var item = new CollectionOrderItem(new EntityBaseServices<CollectionOrderItem>());
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

EntityListBase maintains an internal DeletedList to track removed items that need deletion during persistence. This list is managed automatically during factory operations.

The deleted list lifecycle:

<!-- snippet: collections-deleted-list -->
```cs
[Fact]
public void DeletedList_TracksRemovedEntitiesUntilSave()
{
    var order = new CollectionOrder(new EntityBaseServices<CollectionOrder>());

    // Create "existing" items (simulating loaded from database)
    var item1 = new CollectionOrderItem(new EntityBaseServices<CollectionOrderItem>());
    item1.ProductCode = "ITEM-001";
    item1.DoMarkOld();
    item1.DoMarkUnmodified();

    var item2 = new CollectionOrderItem(new EntityBaseServices<CollectionOrderItem>());
    item2.ProductCode = "ITEM-002";
    item2.DoMarkOld();
    item2.DoMarkUnmodified();

    // Add during fetch operation
    order.Items.DoFactoryStart(FactoryOperation.Fetch);
    order.Items.Add(item1);
    order.Items.Add(item2);
    order.Items.DoFactoryComplete(FactoryOperation.Fetch);

    // Remove an item - goes to DeletedList
    order.Items.Remove(item1);
    Assert.True(item1.IsDeleted);
    Assert.Equal(1, order.Items.DeletedCount);

    // Collection is modified because of DeletedList
    Assert.True(order.Items.IsModified);

    // After save (FactoryComplete with Update), DeletedList is cleared
    order.Items.DoFactoryStart(FactoryOperation.Update);
    order.Items.DoFactoryComplete(FactoryOperation.Update);

    Assert.Equal(0, order.Items.DeletedCount);
}
```
<!-- endSnippet -->

Deleted items remain in DeletedList with their ContainingList set until FactoryComplete fires after a successful save. At that point:
- DeletedList is cleared
- ContainingList on deleted items is set to null
- Deleted items have been persisted to the database

If an item is re-added before save:
- It's removed from the DeletedList
- It's undeleted (IsDeleted = false)
- It's marked modified (since it existed, was deleted, now exists again)

This enables intra-aggregate moves where an item is removed from one child list and added to another without being deleted from the database.

## Paused Operations

Collections respect the IsPaused flag during deserialization and factory operations. While paused:
- No validation state updates occur
- No property change events fire
- Entity state transitions are deferred
- Items can be added to DeletedList if they're marked deleted during deserialization

The framework automatically pauses during:
- JSON deserialization (OnDeserializing)
- Factory operations (FactoryStart)

And resumes during:
- JSON deserialization complete (OnDeserialized)
- Factory operation complete (FactoryComplete)

After resuming, cached validation and modification state is recalculated from all items.

---

**UPDATED:** 2026-01-24
