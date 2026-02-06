# Change Tracking

[← Business Rules](business-rules.md) | [↑ Guides](index.md) | [Collections →](collections.md)

Neatoo tracks modifications to entities and aggregates through a comprehensive change tracking system. The framework automatically marks entities as modified when properties change and cascades modification state up the parent hierarchy to the aggregate root.

## IsModified and IsSelfModified

EntityBase tracks modification state at two levels: self-modifications and child-modifications.

`IsSelfModified` indicates whether the entity's direct properties have changed, or if it has been deleted or explicitly marked modified:

<!-- snippet: tracking-self-modified -->
<a id='snippet-tracking-self-modified'></a>
```cs
[Fact]
public void IsSelfModified_TracksDirectPropertyChanges()
{
    var factory = GetRequiredService<ITrackingEmployeeFactory>();
    var employee = factory.Create();

    // Entity starts unmodified
    Assert.False(employee.IsSelfModified);

    // Changing a property marks the entity as self-modified
    employee.Name = "Alice";

    Assert.True(employee.IsSelfModified);
}
```
<sup><a href='/src/samples/ChangeTrackingSamples.cs#L121-L136' title='Snippet source file'>snippet source</a> | <a href='#snippet-tracking-self-modified' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`IsModified` aggregates modification state from the entity itself, child entities, and child collections. An entity is modified when any of the following are true:

- Child entities or collections are modified (tracked by `PropertyManager.IsModified`)
- The entity is new (`IsNew`)
- The entity is deleted (`IsDeleted`)
- The entity's own properties changed, or it was explicitly marked modified (`IsSelfModified`)

<!-- snippet: tracking-is-modified -->
<a id='snippet-tracking-is-modified'></a>
```cs
[Fact]
public void IsModified_IncludesChildCollectionModifications()
{
    var invoiceFactory = GetRequiredService<ITrackingInvoiceFactory>();
    // Fetch an existing invoice (IsNew = false)
    var invoice = invoiceFactory.Fetch("INV-001");

    // Fetched entity starts unmodified
    Assert.False(invoice.IsModified);

    // Add a child item to the collection
    var lineItemFactory = GetRequiredService<ITrackingLineItemFactory>();
    var lineItem = lineItemFactory.Create();
    invoice.LineItems.Add(lineItem);

    // Parent's IsModified is true because child collection changed
    Assert.True(invoice.IsModified);
}
```
<sup><a href='/src/samples/ChangeTrackingSamples.cs#L138-L157' title='Snippet source file'>snippet source</a> | <a href='#snippet-tracking-is-modified' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## MarkUnmodified

After a successful save operation, the framework automatically marks the entity as unmodified through `MarkUnmodified`:

<!-- snippet: tracking-mark-clean -->
<a id='snippet-tracking-mark-clean'></a>
```cs
[Fact]
public void MarkUnmodified_ClearsModificationState()
{
    var factory = GetRequiredService<ITrackingEmployeeFactory>();
    // Fetch existing employee (IsNew = false)
    var employee = factory.Fetch("Alice", "alice@example.com", 50000m);

    // Make changes to the entity
    employee.Name = "Bob";
    employee.Email = "bob@example.com";

    Assert.True(employee.IsModified);
    Assert.Contains("Name", employee.ModifiedProperties);

    // Framework calls MarkUnmodified after save completes
    // (DoMarkUnmodified exposes the protected method for demonstration)
    employee.DoMarkUnmodified();

    Assert.False(employee.IsModified);
    Assert.False(employee.IsSelfModified);
    Assert.Empty(employee.ModifiedProperties);
}
```
<sup><a href='/src/samples/ChangeTrackingSamples.cs#L159-L182' title='Snippet source file'>snippet source</a> | <a href='#snippet-tracking-mark-clean' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

This method clears the modification tracking state on all properties and resets `IsMarkedModified`. The framework calls this automatically after Insert and Update operations complete.

You cannot mark an entity as unmodified while async operations are in progress. Call `await WaitForTasks()` first.

## MarkModified

Explicitly mark an entity as modified to force a save operation, even when no tracked properties have changed:

<!-- snippet: tracking-mark-modified -->
<a id='snippet-tracking-mark-modified'></a>
```cs
[Fact]
public void MarkModified_ForcesEntityToBeSaved()
{
    var factory = GetRequiredService<ITrackingEmployeeFactory>();
    // Fetch existing employee (IsNew = false)
    var employee = factory.Fetch("Alice", "alice@example.com", 50000m);

    // Fetched entity starts unmodified
    Assert.False(employee.IsModified);

    // Mark as modified without changing properties
    // (e.g., timestamp needs update, version number change)
    // (DoMarkModified exposes the protected method for demonstration)
    employee.DoMarkModified();

    Assert.True(employee.IsModified);
    Assert.True(employee.IsSelfModified);
    Assert.True(employee.IsMarkedModified);
}
```
<sup><a href='/src/samples/ChangeTrackingSamples.cs#L184-L204' title='Snippet source file'>snippet source</a> | <a href='#snippet-tracking-mark-modified' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Use `MarkModified` when:
- A timestamp property should be updated on every save
- Optimistic concurrency requires incrementing a version number
- Database-side computed columns need recalculation
- External state changes require persistence without property changes

Setting `IsMarkedModified` affects both `IsSelfModified` and `IsModified`, ensuring the entity is recognized as savable. The flag is cleared automatically by `MarkUnmodified` after successful save operations.

## Modified Properties

Track which specific properties have changed since the last save:

<!-- snippet: tracking-modified-properties -->
<a id='snippet-tracking-modified-properties'></a>
```cs
[Fact]
public void ModifiedProperties_TracksChangedPropertyNames()
{
    var factory = GetRequiredService<ITrackingEmployeeFactory>();
    var employee = factory.Create();

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
<sup><a href='/src/samples/ChangeTrackingSamples.cs#L206-L224' title='Snippet source file'>snippet source</a> | <a href='#snippet-tracking-modified-properties' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The `ModifiedProperties` collection contains the names of all properties that have been set since the last save or creation. This is useful for optimistic concurrency, audit logging, or partial updates.

## Cascade to Parent

Modification state automatically cascades up the parent hierarchy to the aggregate root. This ensures the aggregate root always knows when any part of the aggregate graph has changed.

When a child entity or collection becomes modified, the parent's `IsModified` becomes true through the PropertyManager's incremental cache updates:

<!-- snippet: tracking-cascade-parent -->
<a id='snippet-tracking-cascade-parent'></a>
```cs
[Fact]
public void ModificationCascadesToParent()
{
    var invoiceFactory = GetRequiredService<ITrackingInvoiceFactory>();
    var lineItemFactory = GetRequiredService<ITrackingLineItemFactory>();

    // Fetch existing invoice (IsNew = false, IsModified = false)
    var invoice = invoiceFactory.Fetch("INV-001");

    // Fetched entity starts unmodified
    Assert.False(invoice.IsModified);
    Assert.False(invoice.IsSelfModified);

    // Add a new child item (simulating user adding an item to an order)
    var lineItem = lineItemFactory.Create();
    lineItem.Description = "New Item";
    lineItem.Amount = 50m;
    invoice.LineItems.Add(lineItem);

    // Parent's IsModified becomes true because child collection changed
    Assert.True(invoice.IsModified);

    // Parent's IsSelfModified remains false (no direct property changes)
    Assert.False(invoice.IsSelfModified);
}
```
<sup><a href='/src/samples/ChangeTrackingSamples.cs#L226-L252' title='Snippet source file'>snippet source</a> | <a href='#snippet-tracking-cascade-parent' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

This ensures the aggregate root can detect changes anywhere in the object graph. The modification cascade respects aggregate boundaries and does not cross into other aggregates.

## Change Tracking in Collections

EntityListBase tracks modifications across child items and manages deleted items separately. The list's `IsModified` becomes true when any child item is modified or when items exist in the `DeletedList`.

Collection modification tracking uses an incremental cache to avoid O(n) scans on every property change. When a child's `IsModified` changes, the list updates its cached state in O(1) time:

<!-- snippet: tracking-collections-modified -->
<a id='snippet-tracking-collections-modified'></a>
```cs
[Fact]
public void CollectionTracksItemModifications()
{
    var invoiceFactory = GetRequiredService<ITrackingInvoiceFactory>();
    var lineItemFactory = GetRequiredService<ITrackingLineItemFactory>();

    // Fetch existing invoice
    var invoice = invoiceFactory.Fetch("INV-001");
    Assert.False(invoice.IsModified);

    // Add a new item to the collection
    var lineItem = lineItemFactory.Create();
    lineItem.Description = "New Item";
    lineItem.Amount = 50m;
    invoice.LineItems.Add(lineItem);

    // Invoice is modified because collection changed
    Assert.True(invoice.IsModified);
    Assert.True(invoice.LineItems.IsModified);
}
```
<sup><a href='/src/samples/ChangeTrackingSamples.cs#L254-L275' title='Snippet source file'>snippet source</a> | <a href='#snippet-tracking-collections-modified' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The list maintains a separate `DeletedList` for items that have been removed but need to be deleted during persistence:

<!-- snippet: tracking-collections-deleted -->
<a id='snippet-tracking-collections-deleted'></a>
```cs
[Fact]
public void CollectionTracksDeletedItems()
{
    var invoiceFactory = GetRequiredService<ITrackingInvoiceFactory>();
    var lineItemFactory = GetRequiredService<ITrackingLineItemFactory>();

    // Create invoice and add items (simulating a new aggregate)
    var invoice = invoiceFactory.Create();
    var item1 = lineItemFactory.Create();
    item1.Description = "Item 1";
    item1.Amount = 50m;
    var item2 = lineItemFactory.Create();
    item2.Description = "Item 2";
    item2.Amount = 75m;

    invoice.LineItems.Add(item1);
    invoice.LineItems.Add(item2);

    Assert.Equal(2, invoice.LineItems.Count);

    // Remove a new item (never persisted) - not tracked for deletion
    invoice.LineItems.Remove(item1);

    // Item is removed but not marked deleted (was never saved)
    Assert.Single(invoice.LineItems);
    Assert.Equal(0, invoice.LineItems.DeletedCount);

    // Add an item that was fetched (represents existing persisted data)
    var existingItem = lineItemFactory.Fetch("Existing Item", 100m);
    invoice.LineItems.Add(existingItem);

    // Simulate a completed save operation to establish "persisted" state
    // (FactoryComplete is called by the framework after Insert/Update)
    invoice.FactoryComplete(FactoryOperation.Insert);

    // Now remove the "existing" item
    invoice.LineItems.Remove(existingItem);

    // Existing items go to DeletedList
    Assert.True(existingItem.IsDeleted);
    Assert.Equal(1, invoice.LineItems.DeletedCount);
}
```
<sup><a href='/src/samples/ChangeTrackingSamples.cs#L277-L320' title='Snippet source file'>snippet source</a> | <a href='#snippet-tracking-collections-deleted' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Items marked as deleted remain in the `DeletedList` until the save operation completes. New items removed from the list are not added to `DeletedList` since they don't exist in persistent storage.

## Self vs Children

Distinguish between modifications to the entity itself versus modifications to child entities or collections.

Check if only the entity's direct properties have changed:

<!-- snippet: tracking-self-vs-children -->
<a id='snippet-tracking-self-vs-children'></a>
```cs
[Fact]
public void DistinguishSelfFromChildModifications()
{
    var invoiceFactory = GetRequiredService<ITrackingInvoiceFactory>();
    var lineItemFactory = GetRequiredService<ITrackingLineItemFactory>();

    // Fetch existing invoice (starts clean)
    var invoice = invoiceFactory.Fetch("INV-001");
    Assert.False(invoice.IsModified);

    // Add a new child item
    var lineItem = lineItemFactory.Create();
    lineItem.Description = "New Item";
    lineItem.Amount = 50m;
    invoice.LineItems.Add(lineItem);

    // IsModified: true (includes child changes)
    Assert.True(invoice.IsModified);

    // IsSelfModified: false (only direct property changes)
    Assert.False(invoice.IsSelfModified);

    // Now modify the parent directly
    invoice.InvoiceNumber = "INV-002";

    // Both are true
    Assert.True(invoice.IsModified);
    Assert.True(invoice.IsSelfModified);
}
```
<sup><a href='/src/samples/ChangeTrackingSamples.cs#L322-L352' title='Snippet source file'>snippet source</a> | <a href='#snippet-tracking-self-vs-children' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

This distinction is useful when:
- Validation rules should only trigger on direct property changes
- Save operations need to determine if the entity's table row requires an UPDATE
- Optimistic concurrency checks need to distinguish entity changes from child changes
- Business rules must differentiate between aggregate root changes and child entity changes

**EntityListBase Architecture:** For collection types, `IsSelfModified` is always false because lists do not have their own modifiable properties. A list's modification state comes entirely from its child items (`_cachedChildrenModified`) and deleted items (`DeletedList`).

## IsSavable: Combining Modification and Validation

The `IsSavable` property determines whether an entity can be persisted. It combines modification state, validation state, and aggregate boundary rules to enforce correct save semantics.

An entity is savable when all of the following are true:
- `IsModified` - The entity has changes requiring persistence
- `IsValid` - All validation rules pass
- `!IsBusy` - No async operations are in progress
- `!IsChild` - The entity is not a child (child entities save through their parent)

This architecture ensures child entities within an aggregate cannot be saved independently, maintaining aggregate consistency:

<!-- snippet: tracking-is-savable -->
<a id='snippet-tracking-is-savable'></a>
```cs
[Fact]
public void IsSavable_CombinesModificationAndValidation()
{
    var factory = GetRequiredService<ITrackingEmployeeFactory>();
    // Fetch existing employee (IsNew = false)
    var employee = factory.Fetch("Alice", "alice@example.com", 50000m);

    // Fetched entity starts unmodified
    Assert.False(employee.IsSavable);

    // Modify the entity
    employee.Name = "Bob";

    // Modified, valid, not busy, not child = savable
    Assert.True(employee.IsModified);
    Assert.True(employee.IsValid);
    Assert.False(employee.IsBusy);
    Assert.False(employee.IsChild);
    Assert.True(employee.IsSavable);
}
```
<sup><a href='/src/samples/ChangeTrackingSamples.cs#L354-L375' title='Snippet source file'>snippet source</a> | <a href='#snippet-tracking-is-savable' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The `Save()` method checks `IsSavable` and throws `SaveOperationException` with a specific reason code if the preconditions are not met:

<!-- snippet: tracking-save-checks -->
<a id='snippet-tracking-save-checks'></a>
```cs
[Fact]
public async Task Save_ThrowsWithSpecificReason()
{
    var factory = GetRequiredService<ITrackingEmployeeFactory>();
    // Fetch existing employee (IsNew = false)
    var employee = factory.Fetch("Alice", "alice@example.com", 50000m);

    // Fetched entity is unmodified
    Assert.False(employee.IsModified);

    // Try to save unmodified entity
    var exception = await Assert.ThrowsAsync<SaveOperationException>(
        () => employee.Save());

    Assert.Equal(SaveFailureReason.NotModified, exception.Reason);
}
```
<sup><a href='/src/samples/ChangeTrackingSamples.cs#L377-L394' title='Snippet source file'>snippet source</a> | <a href='#snippet-tracking-save-checks' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Common `SaveFailureReason` values:
- `IsChildObject` - Entity is a child and must be saved through its parent
- `IsInvalid` - Validation rules have failed
- `NotModified` - No changes detected (nothing to persist)
- `IsBusy` - Async operations still in progress
- `NoFactoryMethod` - No Insert/Update/Delete factory method configured

This design provides clear, actionable feedback when save preconditions are not met, rather than silently failing or persisting invalid state.

## Pausing Modification Tracking

Use `PauseAllActions` to temporarily disable modification tracking, validation, and property change notifications during batch operations:

<!-- snippet: tracking-pause-actions -->
<a id='snippet-tracking-pause-actions'></a>
```cs
[Fact]
public void PauseAllActions_PreventsModificationTracking()
{
    var factory = GetRequiredService<ITrackingEmployeeFactory>();
    var employee = factory.Create();

    // Clear initial state
    employee.DoMarkUnmodified();

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
<sup><a href='/src/samples/ChangeTrackingSamples.cs#L396-L419' title='Snippet source file'>snippet source</a> | <a href='#snippet-tracking-pause-actions' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

While paused, property setters still execute but tracking mechanisms are disabled:
- Properties are not marked as modified
- `ModifiedProperties` is not updated
- `IsSelfModified` remains unchanged
- Validation rules do not execute
- `PropertyChanged` events are not raised

Use pausing when:
- Loading data from persistence (property setters populate entity state)
- Deserializing from JSON or other formats
- Performing bulk property updates that should not trigger cascading notifications
- Initializing computed or derived properties

**Framework Behavior:** Neatoo automatically pauses during all factory operations (Create, Fetch, Insert, Update, Delete) to ensure data loading does not trigger false modification tracking or validation. After the factory method completes, tracking resumes automatically.

## IsNew and IsDeleted

EntityBase tracks entity lifecycle state to determine which persistence operation to perform.

`IsNew` indicates the entity has not been persisted and requires an Insert operation:

<!-- snippet: tracking-is-new -->
<a id='snippet-tracking-is-new'></a>
```cs
[Fact]
public void IsNew_IndicatesUnpersistedEntity()
{
    var factory = GetRequiredService<ITrackingEmployeeFactory>();
    var employee = factory.Create();

    // Factory.Create sets IsNew automatically
    Assert.True(employee.IsNew);

    // New entities are considered modified (need Insert)
    Assert.True(employee.IsModified);
}
```
<sup><a href='/src/samples/ChangeTrackingSamples.cs#L421-L434' title='Snippet source file'>snippet source</a> | <a href='#snippet-tracking-is-new' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The `IsNew` flag is automatically set by factory Create methods and cleared after successful Insert. New entities are always considered modified (`IsNew` contributes to `IsModified`).

`IsDeleted` indicates the entity has been marked for deletion and requires a Delete operation:

<!-- snippet: tracking-is-deleted -->
<a id='snippet-tracking-is-deleted'></a>
```cs
[Fact]
public void IsDeleted_MarksEntityForDeletion()
{
    var factory = GetRequiredService<ITrackingEmployeeFactory>();
    var employee = factory.Fetch("Alice", "alice@example.com", 50000m);

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
<sup><a href='/src/samples/ChangeTrackingSamples.cs#L436-L459' title='Snippet source file'>snippet source</a> | <a href='#snippet-tracking-is-deleted' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The `IsDeleted` flag is set by calling `Delete()` and can be reversed with `UnDelete()` before saving. Deleted entities contribute to both `IsModified` and `IsSelfModified`, ensuring they are recognized as changed and savable.

**Architecture Note:** Both `IsNew` and `IsDeleted` affect multiple modification properties to ensure proper save behavior. A new entity is always modified (needs Insert), and a deleted entity is always self-modified (needs Delete), regardless of property changes.

## Modification Tracking Architecture

Neatoo implements a layered change tracking architecture through the PropertyManager hierarchy.

### Property-Level Tracking

Each property tracks whether it has been set since entity creation or the last save operation. The PropertyManager aggregates this state across all properties and raises `PropertyChanged` events when modification state transitions occur.

### Entity-Level Tracking (EntityBase)

Entity modification state is computed from multiple sources:

```
IsModified = PropertyManager.IsModified    // Child entities/collections
          || IsNew                         // New entity (needs Insert)
          || IsDeleted                     // Deleted (needs Delete)
          || IsSelfModified               // Self or explicitly marked

IsSelfModified = PropertyManager.IsSelfModified  // Direct property changes
              || IsDeleted                      // Deleted entities
              || IsMarkedModified              // Explicitly marked
```

### Collection-Level Tracking (EntityListBase)

Collection modification state uses incremental caching:

```
IsModified = _cachedChildrenModified    // Any child is modified
          || DeletedList.Any()          // Items pending deletion

IsSelfModified = false                   // Always false (no own properties)
```

The `_cachedChildrenModified` flag updates incrementally when child `IsModified` changes:
- Child becomes modified → Set cache to true (O(1))
- Child becomes unmodified → Scan remaining children if cache was true (O(k) where k = first modified child)

This approach avoids O(n) scans on every property change while maintaining accurate aggregate state.

---

**UPDATED:** 2026-01-25
