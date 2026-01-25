# Change Tracking

[← Business Rules](business-rules.md) | [↑ Guides](index.md) | [Collections →](collections.md)

Neatoo tracks modifications to entities and aggregates through a comprehensive change tracking system. The framework automatically marks entities as modified when properties change and cascades modification state up the parent hierarchy to the aggregate root.

## IsModified and IsSelfModified

EntityBase tracks modification state at two levels: self and children.

`IsSelfModified` indicates whether the entity's own properties have changed:

<!-- snippet: tracking-self-modified -->
```cs
[Fact]
public void IsSelfModified_TracksDirectPropertyChanges()
{
    var employee = new TrackingEmployee(new EntityBaseServices<TrackingEmployee>());

    // Entity starts unmodified
    Assert.False(employee.IsSelfModified);

    // Changing a property marks the entity as self-modified
    employee.Name = "Alice";

    Assert.True(employee.IsSelfModified);
}
```
<!-- endSnippet -->

`IsModified` includes both the entity's properties and any child entities or collections:

<!-- snippet: tracking-is-modified -->
```cs
[Fact]
public void IsModified_IncludesChildCollectionModifications()
{
    var invoice = new TrackingInvoice(new EntityBaseServices<TrackingInvoice>());

    // Start clean by marking unmodified
    invoice.DoMarkUnmodified();
    Assert.False(invoice.IsModified);

    // Add a child item to the collection
    var lineItem = new TrackingLineItem(new EntityBaseServices<TrackingLineItem>());
    invoice.LineItems.Add(lineItem);

    // Parent's IsModified is true because child collection changed
    Assert.True(invoice.IsModified);
}
```
<!-- endSnippet -->

An entity is considered modified if it is new, deleted, has modified properties, or has been explicitly marked modified.

## MarkClean

After a successful save operation, the framework automatically marks the entity as unmodified through `MarkUnmodified`:

<!-- snippet: tracking-mark-clean -->
```cs
[Fact]
public void MarkUnmodified_ClearsModificationState()
{
    var employee = new TrackingEmployee(new EntityBaseServices<TrackingEmployee>());

    // Make changes to the entity
    employee.Name = "Alice";
    employee.Email = "alice@example.com";

    Assert.True(employee.IsModified);
    Assert.Contains("Name", employee.ModifiedProperties);

    // After save, framework calls MarkUnmodified
    employee.DoMarkUnmodified();

    Assert.False(employee.IsModified);
    Assert.False(employee.IsSelfModified);
    Assert.Empty(employee.ModifiedProperties);
}
```
<!-- endSnippet -->

This method clears the modification tracking state on all properties and resets `IsMarkedModified`. The framework calls this automatically after Insert and Update operations complete.

You cannot mark an entity as unmodified while async operations are in progress. Call `await WaitForTasks()` first.

## MarkModified

Explicitly mark an entity as modified to force a save operation:

<!-- snippet: tracking-mark-modified -->
```cs
[Fact]
public void MarkModified_ForcesEntityToBeSaved()
{
    var employee = new TrackingEmployee(new EntityBaseServices<TrackingEmployee>());

    // Entity starts unmodified
    Assert.False(employee.IsModified);

    // Mark as modified without changing properties
    // (e.g., timestamp needs update, version number change)
    employee.DoMarkModified();

    Assert.True(employee.IsModified);
    Assert.True(employee.IsSelfModified);
    Assert.True(employee.IsMarkedModified);
}
```
<!-- endSnippet -->

Use this when external state changes require the entity to be re-saved, even though no Neatoo properties have changed. For example, when a timestamp should be updated or when optimistic concurrency requires a new version number.

## Modified Properties

Track which specific properties have changed since the last save:

<!-- snippet: tracking-modified-properties -->
```cs
[Fact]
public void ModifiedProperties_TracksChangedPropertyNames()
{
    var employee = new TrackingEmployee(new EntityBaseServices<TrackingEmployee>());

    // Change multiple properties
    employee.Name = "Alice";
    employee.Salary = 75000m;

    // ModifiedProperties contains the names of changed properties
    var modified = employee.ModifiedProperties.ToList();

    Assert.Contains("Name", modified);
    Assert.Contains("Salary", modified);
    Assert.DoesNotContain("Email", modified);
}
```
<!-- endSnippet -->

The `ModifiedProperties` collection contains the names of all properties that have been set since the last save or creation. This is useful for optimistic concurrency, audit logging, or partial updates.

## Cascade to Parent

Modification state cascades up the parent hierarchy to the aggregate root.

When a child entity property changes, the parent's `IsModified` becomes true:

<!-- snippet: tracking-cascade-parent -->
```cs
[Fact]
public void ModificationCascadesToParent()
{
    var invoice = new TrackingInvoice(new EntityBaseServices<TrackingInvoice>());

    // Create "existing" item (simulating one loaded from DB)
    var lineItem = new TrackingLineItem(new EntityBaseServices<TrackingLineItem>());
    lineItem.Description = "Original";
    lineItem.DoMarkOld();        // Mark as existing (not new)
    lineItem.DoMarkUnmodified(); // Clear modification tracking
    Assert.False(lineItem.IsModified);

    // Add to collection - simulating fetch
    invoice.LineItems.DoFactoryStart(FactoryOperation.Fetch);
    invoice.LineItems.Add(lineItem);
    invoice.LineItems.DoFactoryComplete(FactoryOperation.Fetch);
    invoice.DoMarkUnmodified();

    Assert.False(invoice.IsModified);
    Assert.False(invoice.LineItems.IsModified);

    // Modify the child entity
    lineItem.Description = "Updated Item";

    // Parent's IsModified becomes true due to child change
    Assert.True(invoice.IsModified);
    // Parent's IsSelfModified remains false (only direct property changes)
    Assert.False(invoice.IsSelfModified);
}
```
<!-- endSnippet -->

This ensures the aggregate root can detect changes anywhere in the object graph. The modification cascade respects aggregate boundaries and does not cross into other aggregates.

## Change Tracking in Collections

EntityListBase tracks modifications across the collection and propagates them to the parent.

When any item in the list is modified, the list's `IsModified` becomes true:

<!-- snippet: tracking-collections-modified -->
```cs
[Fact]
public void CollectionTracksItemModifications()
{
    var invoice = new TrackingInvoice(new EntityBaseServices<TrackingInvoice>());

    // Create "existing" item (simulating one loaded from DB)
    var lineItem = new TrackingLineItem(new EntityBaseServices<TrackingLineItem>());
    lineItem.Amount = 50m;
    lineItem.DoMarkOld();        // Mark as existing (not new)
    lineItem.DoMarkUnmodified(); // Clear modification tracking
    Assert.False(lineItem.IsModified);

    // Add to collection - simulating fetch
    invoice.LineItems.DoFactoryStart(FactoryOperation.Fetch);
    invoice.LineItems.Add(lineItem);
    invoice.LineItems.DoFactoryComplete(FactoryOperation.Fetch);

    // Verify collection is not modified initially
    Assert.False(invoice.LineItems.IsModified);

    // Modifying an item in the collection marks the collection as modified
    lineItem.Amount = 100m;

    Assert.True(invoice.LineItems.IsModified);
    Assert.True(invoice.IsModified);
}
```
<!-- endSnippet -->

The list maintains a separate `DeletedList` for items that have been removed but need to be deleted during persistence:

<!-- snippet: tracking-collections-deleted -->
```cs
[Fact]
public void CollectionTracksDeletedItems()
{
    var invoice = new TrackingInvoice(new EntityBaseServices<TrackingInvoice>());

    // Create "existing" items (simulating loaded from DB)
    var item1 = new TrackingLineItem(new EntityBaseServices<TrackingLineItem>());
    item1.Description = "Item 1";
    item1.Amount = 50m;
    item1.DoMarkOld();
    item1.DoMarkUnmodified();

    var item2 = new TrackingLineItem(new EntityBaseServices<TrackingLineItem>());
    item2.Description = "Item 2";
    item2.Amount = 75m;
    item2.DoMarkOld();
    item2.DoMarkUnmodified();

    // Add to collection - simulating fetch
    invoice.LineItems.DoFactoryStart(FactoryOperation.Fetch);
    invoice.LineItems.Add(item1);
    invoice.LineItems.Add(item2);
    invoice.LineItems.DoFactoryComplete(FactoryOperation.Fetch);

    Assert.Equal(2, invoice.LineItems.Count);
    Assert.False(invoice.LineItems.IsModified);

    // Remove an item - it goes to DeletedList for persistence
    var itemToRemove = invoice.LineItems[0];
    invoice.LineItems.Remove(itemToRemove);

    // Item is removed from active list
    Assert.Single(invoice.LineItems);

    // Collection is modified (has deleted items)
    Assert.True(invoice.LineItems.IsModified);
    Assert.True(itemToRemove.IsDeleted);
    Assert.Equal(1, invoice.LineItems.DeletedCount);
}
```
<!-- endSnippet -->

Items marked as deleted remain in the `DeletedList` until the save operation completes. New items removed from the list are not added to `DeletedList` since they don't exist in persistent storage.

## Self vs Children

Distinguish between modifications to the entity itself versus modifications to child entities or collections.

Check if only the entity's direct properties have changed:

<!-- snippet: tracking-self-vs-children -->
```cs
[Fact]
public void DistinguishSelfFromChildModifications()
{
    var invoice = new TrackingInvoice(new EntityBaseServices<TrackingInvoice>());

    // Create "existing" item
    var lineItem = new TrackingLineItem(new EntityBaseServices<TrackingLineItem>());
    lineItem.Amount = 50m;
    lineItem.DoMarkOld();
    lineItem.DoMarkUnmodified();

    // Add to collection - simulating fetch
    invoice.LineItems.DoFactoryStart(FactoryOperation.Fetch);
    invoice.LineItems.Add(lineItem);
    invoice.LineItems.DoFactoryComplete(FactoryOperation.Fetch);
    invoice.DoMarkUnmodified();

    Assert.False(invoice.IsModified);

    // Modify the child
    lineItem.Amount = 100m;

    // IsModified: true (includes child changes)
    Assert.True(invoice.IsModified);

    // IsSelfModified: false (only direct property changes)
    Assert.False(invoice.IsSelfModified);

    // Now modify the parent directly
    invoice.InvoiceNumber = "INV-001";

    // Both are true
    Assert.True(invoice.IsModified);
    Assert.True(invoice.IsSelfModified);
}
```
<!-- endSnippet -->

This distinction is useful for validation rules that should only trigger on direct property changes, or for save operations that need to know whether the entity's table row requires an update.

For EntityListBase, `IsSelfModified` is always false since lists do not have their own modifiable properties. Lists are only modified through their items or deletion tracking.

## Dirty State and Validation Relationship

Modification state and validation state work together to determine if an entity can be saved.

`IsSavable` combines modification and validation:

<!-- snippet: tracking-is-savable -->
```cs
[Fact]
public void IsSavable_CombinesModificationAndValidation()
{
    var employee = new TrackingEmployee(new EntityBaseServices<TrackingEmployee>());

    // Unmodified entity is not savable
    Assert.False(employee.IsSavable);

    // Modify the entity
    employee.Name = "Alice";

    // Modified, valid, not busy, not child = savable
    Assert.True(employee.IsModified);
    Assert.True(employee.IsValid);
    Assert.False(employee.IsBusy);
    Assert.False(employee.IsChild);
    Assert.True(employee.IsSavable);
}
```
<!-- endSnippet -->

An entity is savable when it is modified, valid, not busy with async operations, and not a child entity. Child entities must be saved through their parent aggregate root.

The save operation checks `IsSavable` and throws `SaveOperationException` with a specific reason if the save cannot proceed:

<!-- snippet: tracking-save-checks -->
```cs
[Fact]
public async Task Save_ThrowsWithSpecificReason()
{
    var employee = new TrackingEmployee(new EntityBaseServices<TrackingEmployee>());

    // Try to save unmodified entity
    var exception = await Assert.ThrowsAsync<SaveOperationException>(
        () => employee.Save());

    Assert.Equal(SaveFailureReason.NotModified, exception.Reason);

    // Modify the entity but no factory configured
    employee.Name = "Alice";

    exception = await Assert.ThrowsAsync<SaveOperationException>(
        () => employee.Save());

    Assert.Equal(SaveFailureReason.NoFactoryMethod, exception.Reason);
}
```
<!-- endSnippet -->

Common save failure reasons include `IsChildObject`, `IsInvalid`, `NotModified`, `IsBusy`, and `NoFactoryMethod`.

## Pausing Modification Tracking

Use `PauseAllActions` to prevent modification tracking during batch operations:

<!-- snippet: tracking-pause-actions -->
```cs
[Fact]
public void PauseAllActions_PreventsModificationTracking()
{
    var employee = new TrackingEmployee(new EntityBaseServices<TrackingEmployee>());

    // Pause modification tracking during batch operations
    using (employee.PauseAllActions())
    {
        employee.Name = "Alice";
        employee.Email = "alice@example.com";
        employee.Salary = 75000m;
    }

    // Properties were set but not tracked as modifications
    Assert.Equal("Alice", employee.Name);
    Assert.False(employee.IsSelfModified);
    Assert.Empty(employee.ModifiedProperties);
}
```
<!-- endSnippet -->

While paused, property changes do not mark the entity as modified. This is useful when loading data from persistence or deserializing from JSON.

The framework automatically pauses during factory operations (Create, Fetch, Insert, Update, Delete) to prevent spurious modification tracking.

## IsNew and IsDeleted

EntityBase tracks entity lifecycle state beyond simple modification.

`IsNew` indicates the entity has not been persisted:

<!-- snippet: tracking-is-new -->
```cs
[Fact]
public void IsNew_IndicatesUnpersistedEntity()
{
    var employee = new TrackingEmployee(new EntityBaseServices<TrackingEmployee>());

    // Entity created directly is not new by default
    // (Factory.Create sets IsNew automatically)
    Assert.False(employee.IsNew);

    // Simulate factory create operation
    using (employee.PauseAllActions())
    {
        // Factory sets properties during create
    }
    employee.FactoryComplete(FactoryOperation.Create);

    // Now entity is marked as new
    Assert.True(employee.IsNew);

    // New entities are considered modified (need Insert)
    Assert.True(employee.IsModified);
}
```
<!-- endSnippet -->

New entities trigger an Insert operation when saved. After Insert completes, the framework automatically marks the entity as old (existing).

`IsDeleted` indicates the entity has been marked for deletion:

<!-- snippet: tracking-is-deleted -->
```cs
[Fact]
public void IsDeleted_MarksEntityForDeletion()
{
    var employee = new TrackingEmployee(new EntityBaseServices<TrackingEmployee>());

    // Simulate a fetched entity
    using (employee.PauseAllActions())
    {
        employee.Name = "Alice";
    }
    employee.FactoryComplete(FactoryOperation.Fetch);

    Assert.False(employee.IsDeleted);
    Assert.False(employee.IsModified);

    // Mark for deletion
    employee.Delete();

    Assert.True(employee.IsDeleted);
    Assert.True(employee.IsModified);
    Assert.True(employee.IsSavable);

    // Reverse deletion before save
    employee.UnDelete();

    Assert.False(employee.IsDeleted);
    Assert.False(employee.IsModified);
}
```
<!-- endSnippet -->

Deleted entities trigger a Delete operation when saved. Use `UnDelete()` to reverse the deletion before saving.

## Modification Tracking Implementation

Neatoo tracks modifications through the PropertyManager and Entity/ValidatePropertyManager hierarchy.

Each property tracks whether it has been set since the entity was created or last saved. The PropertyManager aggregates this state across all properties and raises PropertyChanged events when modification state changes.

For entities, the modification state includes:
- Any property value changes
- New entity status (`IsNew`)
- Deleted entity status (`IsDeleted`)
- Explicit modification marking (`IsMarkedModified`)

For lists, the modification state includes:
- Any child entity modifications
- Items in the `DeletedList`

The framework uses incremental cache updates to avoid O(n) scans on every property change. When a child entity's `IsModified` changes, the parent updates its cached state in O(1) time.

---

**UPDATED:** 2026-01-24
