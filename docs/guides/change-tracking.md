# Change Tracking

[← Business Rules](business-rules.md) | [↑ Guides](index.md) | [Collections →](collections.md)

Change tracking is driven by edits, which normally happen on the client — in Blazor, WPF, or any data-bound UI — and the tracked state travels to the server with the aggregate. The UI binds to `IsModified` and `IsSavable` to show the user what's changed and whether they can save. But change tracking also serves a deeper purpose: when the aggregate reaches the server for persistence, the factory methods need to know *exactly* what changed. Without tracking, you'd have to compare client state against the database to figure out what's different — a convoluted merge operation that quickly becomes unmanageable for complex aggregates. It's far simpler to carry the changes with the data.

The tests on this page resolve factories from a DI scope (`DesignTestServices.GetScope()`); in an application the factory interface is injected.

## IsModified and IsSelfModified

EntityBase tracks modification state at two levels: self-modifications and child-modifications.

`IsSelfModified` indicates whether the entity's direct properties have changed, or whether it has been deleted or explicitly marked modified. A fetched entity is a clean baseline; the first edit marks the property and the entity:

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

`IsModified` aggregates modification state from the entity itself, child entities, and child collections. An entity is modified when any of the following are true:

- Child entities or collections are modified
- The entity is deleted (`IsDeleted`)
- The entity's own properties changed, or it was explicitly marked modified (`IsSelfModified`)

Note what is *not* in that list: `IsNew`. A newly created entity is **not** modified.

### Why IsNew is not part of IsModified

`IsModified` and `IsNew` answer two different questions, and conflating them makes one of the answers wrong:

| Property | Question it answers | What it drives |
|---|---|---|
| `IsModified` | Would discarding this lose work? | Unsaved-changes prompts, navigation guards, "you have unsaved changes" |
| `IsNew` | Does persistence not know this object yet? | Insert-vs-update routing |

For a fetched-then-edited entity the answers coincide. For a **created** entity they diverge: it needs persisting, but holds no user work — navigating away from an untouched new object loses nothing. Because savability admits either reason (see [IsSavable](#issavable-combining-modification-and-validation)), `IsModified` does not have to claim a fresh create is dirty just to keep a Save button enabled.

The practical payoff: a guard written the obvious way — `if (order.IsModified) { /* warn before navigating away */ }` — stays quiet until the user actually does something.

If a `[Create]` genuinely *is* the user's work — a "New Invoice" button rather than a derived default — the factory method says so by calling `MarkModified()` in its body:

<!-- snippet: docs-create-marks-modified -->
<a id='snippet-docs-create-marks-modified'></a>
```cs
// A [Create] whose result IS the user's work (a "New" button, not a derived
// default) says so: an unsaved-changes guard bound to IsModified should speak.
// Not needed to make the object savable - IsSavable already admits IsNew.
[Create]
public void CreateAsUserWork()
{
    MarkModified();
}
```
<sup><a href='/src/Design/Design.Domain/FactoryOperations/CreatePatterns.cs#L110-L119' title='Snippet source file'>snippet source</a> | <a href='#snippet-docs-create-marks-modified' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

> **Common mistake:** calling `MarkModified()` in a `[Create]` "so the entity can be saved". New entities are already savable — `IsSavable` admits `IsNew` on its own. Using `MarkModified()` for that re-welds the two meanings and makes unsaved-changes guards cry wolf on every new object again.

It is modification state — never `IsNew` — that aggregates up an object graph. That is why attaching a child to a live parent marks the child modified: see [Cascade to Parent](#cascade-to-parent).

## MarkUnmodified

After a successful save the framework marks the entity unmodified: `FactoryComplete(Insert)` and `FactoryComplete(Update)` call `MarkUnmodified()` and `MarkOld()` on the object that was saved. `Save()` returns the saved instance, and that instance is a clean baseline, exactly like a fetched one:

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

`MarkUnmodified()` is protected. It clears the modification tracking state on all of the entity's own properties and resets the marked-modified flag — the entity's *own* state only. A child is cleared by its own factory completion, which is why children are saved through their own factory operations (see [Entities](entities.md)). Nothing is called after a `[Fetch]`: the object is paused during the fetch, so it never became modified. Application code does not call `MarkUnmodified()`.

You cannot mark an entity as unmodified while async operations are in progress; the framework awaits them first.

## MarkModified

`MarkModified()` is protected: an entity calls it on itself to say the graph holds work worth keeping without any property having changed. Its one doctrinal use is the `[Create]` opt-in shown above. After it, `IsSelfModified` and `IsModified` are true, and the entity is savable for that reason as well as for being new. The flag is cleared by `MarkUnmodified()` after a successful save.

## Modified Properties

Knowing *that* something changed isn't always enough — your persistence layer often needs to know *what* changed. Partial database updates only write the columns that were modified, reducing the conflict surface for optimistic concurrency. Audit logging records exactly which fields changed for compliance. `ModifiedProperties` provides this granularity:

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

The `ModifiedProperties` collection contains the names of all properties that have been set since the last save or creation.

## Cascade to Parent

When a child entity changes, the parent needs to know — for business logic as well as the UI. An aggregate root might have rules like "all line item percentages must sum to 100%" that need to re-evaluate when any child changes, and the root's Save button is bound to the same cascade. A child's property change makes the parent modified without making it self-modified:

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

Adding a child is also a change to the graph. A live `Add` marks the added item modified — new or existing — because modification state (not `IsNew`) is what aggregates upward:

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

The aggregate root can therefore detect changes anywhere in the object graph. The modification cascade respects aggregate boundaries and does not cross into other aggregates.

## Change Tracking in Collections

EntityListBase tracks modifications across child items and manages deleted items separately. The list's `IsModified` becomes true when any child item is modified or when items exist in the `DeletedList`. The list keeps an incremental cache, so a child's `IsModified` change updates the list in O(1) rather than scanning every item.

A removed item that was persisted cannot just disappear — its row still has to be removed during the save. The `DeletedList` holds these removed items so the list's `[Update]` sees them even though they are no longer in the active collection:

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

A new item removed from the list is discarded: it never had a row, so there is nothing to delete and it does not enter `DeletedList`:

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

Items in the `DeletedList` stay there until the list's own `[Update]` has removed their rows and the list's `FactoryComplete(Update)` clears the list. That cleanup runs because the root hands the row's child collection to the list factory's `Save` — the list is persisted through its own factory operation (see [Collections](collections.md)).

## Self vs Children

Distinguish between modifications to the entity itself versus modifications to child entities or collections. The cascade snippet above shows both sides at once: after only the child changed, `parent.IsModified` is true and `parent.IsSelfModified` is false.

This distinction is useful when:
- A root's `[Update]` must decide whether its own row needs writing (`IsSelfModified`) while the list's `[Update]` handles the children
- Optimistic concurrency checks need to distinguish entity changes from child changes
- Business rules must differentiate between aggregate root changes and child entity changes

**EntityListBase Architecture:** For collection types, `IsSelfModified` is always false because lists have no modifiable properties of their own. A list's modification state comes entirely from its child items and its `DeletedList`.

## IsSavable: Combining Modification and Validation

`IsSavable` determines whether an entity can be persisted. It combines modification state with validation and busy state. `IsSavable` is only exposed through the `IEntityRoot` interface — child entity interfaces (`IEntityBase`) do not include it, because on a child it is misleading and its presence invited misuse (see below).

An entity is savable when it has a reason to persist **and** nothing blocks persisting it:
- `IsModified || IsNew` — either it differs from its baseline, or persistence doesn't know it yet
- `IsValid` — all validation rules pass
- `!IsBusy` — no async operations are in progress

`IsSavable` says nothing about position in an aggregate: a modified child *concrete* reports `true`. What keeps consumers from saving a child is that the child interface has no `IsSavable` and no `Save()`.

The `|| IsNew` is what lets `IsModified` stay honest. A freshly created entity is savable because inserting it is meaningful — not because it pretends to hold unsaved edits. See [Why IsNew is not part of IsModified](#why-isnew-is-not-part-of-ismodified).

**Why IsSavable is IEntityRoot only:** `EntityBase` defines `IsSavable` and `Save()` as concrete members that know nothing about aggregate position, so a modified child concrete looks savable — yet children are persisted by their aggregate root, never on their own. Developers used `IsSavable` in save cascade logic to decide whether children needed persisting, and reached for `Save()` on a child, which the framework does not support. This caused a real production bug. The fix removes `IsSavable` and `Save()` from the child interface entirely, so the mistake is a compile error. Aggregate root interfaces extend `IEntityRoot`; child entity interfaces extend `IEntityBase`.

A fetched, untouched entity is not savable; the first edit makes it savable:

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

A created entity is savable before any edit, through the `IsNew` term:

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

Validation blocks the save regardless of modification state:

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

`Save()` checks `IsSavable` and throws `SaveOperationException` with a specific reason code if the preconditions are not met:

<!-- snippet: docs-save-not-savable-throws -->
<a id='snippet-docs-save-not-savable-throws'></a>
```cs
[TestMethod]
public async Task Save_WhenNotSavable_ThrowsWithTheReason()
{
    // A fetched, untouched entity: neither modified nor new
    var entity = await _factory.Fetch(1);
    Assert.IsFalse(entity.IsSavable);

    // Reaching this is a programming error: the UI binds Save to IsSavable
    var exception = await Assert.ThrowsExactlyAsync<SaveOperationException>(() => entity.Save());

    Assert.AreEqual(SaveFailureReason.NotModified, exception.Reason);
}
```
<sup><a href='/src/Design/Design.Tests/FactoryTests/SaveTests.cs#L206-L219' title='Snippet source file'>snippet source</a> | <a href='#snippet-docs-save-not-savable-throws' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Common `SaveFailureReason` values:
- `IsInvalid` - Validation rules have failed
- `NotModified` - Neither modified nor new (nothing to persist)
- `IsBusy` - Async operations still in progress
- `NoFactoryMethod` - No Insert/Update/Delete factory method configured

This design provides clear, actionable feedback when save preconditions are not met, rather than silently failing or persisting invalid state.

## Pausing Modification Tracking

Pausing exists primarily because the framework itself needs it. During factory operations (Create, Fetch, Insert, Update, Delete), Neatoo pauses the object so that assigning properties from persistence data doesn't trigger rules, fire events, or mark the entity as modified — inside a factory operation, plain assignment is the baseline load, and no `PauseAllActions()` call is needed there. The same mechanism is available to developers for batch operations — loading from a DTO, deserializing, or bulk updates where you don't want intermediate states.

While paused, property setters still execute but tracking mechanisms are disabled:
- Properties are not marked as modified
- `ModifiedProperties` is not updated
- `IsSelfModified` remains unchanged
- Validation rules do not execute
- `PropertyChanged` events are not raised

Rules do not run while paused, and the pause ending does not run them:

<!-- snippet: skill-pause-all-actions -->
<a id='snippet-skill-pause-all-actions'></a>
```cs
[TestMethod]
public void Gotcha4_PausedPropertyChanges_DoNotTriggerRules()
{
    // Arrange
    var factory = _scope.GetRequiredService<IGotcha4DemoFactory>();
    var entity = factory.Create();

    // Act - Modify properties while paused
    using (entity.PauseAllActions())
    {
        entity.Quantity = 10;
        entity.Price = 5.00m;
    }
    // ResumeAllActions() is called, but rules don't automatically run

    // Assert - Total is NOT calculated
    Assert.AreEqual(0m, entity.Total, "Total should be 0 - rules did not run while paused");
}
```
<sup><a href='/src/Design/Design.Tests/GotchaTests/CommonGotchaTests.cs#L177-L196' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-pause-all-actions' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

On an entity, an edit made while paused is not a modification at all. After the pause ends nothing catches up — no rules, no events, no modified flag — so a paused user edit on a fetched entity leaves it unmodified and unsavable, and the edit is not saved:

<!-- snippet: docs-paused-edit-not-modified -->
<a id='snippet-docs-paused-edit-not-modified'></a>
```cs
[TestMethod]
public async Task PausedEdit_IsNotTrackedAsModified()
{
    var entity = await _factory.Fetch(1);

    using (entity.PauseAllActions())
    {
        entity.Name = "Edited while paused";
    }

    // The value is set, but nothing caught up when the pause ended
    Assert.AreEqual("Edited while paused", entity.Name);
    Assert.IsFalse(entity.IsModified, "A paused assignment is a baseline load, not an edit");
    Assert.IsFalse(entity.IsSavable, "...so there is nothing to save");
}
```
<sup><a href='/src/Design/Design.Tests/FactoryTests/SaveTests.cs#L221-L237' title='Snippet source file'>snippet source</a> | <a href='#snippet-docs-paused-edit-not-modified' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Use pausing when:
- Deserializing from JSON or other formats
- Performing bulk property updates that should not trigger cascading notifications
- Initializing computed or derived properties

**Framework Behavior:** Neatoo automatically pauses during all factory operations (Create, Fetch, Insert, Update, Delete) to ensure data loading does not trigger false modification tracking or validation. After the factory method completes, tracking resumes automatically. Never wrap a factory method body in `PauseAllActions()`: the operation is already paused, and disposing the `using` resumes the object early.

## IsNew and IsDeleted

EntityBase tracks entity lifecycle state to determine which persistence operation to perform.

`IsNew` indicates the entity has not been persisted and requires an Insert operation. A created entity is new, not modified, and savable; a save of an untouched new entity still inserts, because routing and savability both run off `IsNew`:

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

The `IsNew` flag is set by `FactoryComplete(Create)` — only a completed `[Create]` makes an entity new — and cleared after a successful Insert. It is pure routing state: it decides Insert vs Update and contributes to `IsSavable`, but it does **not** make an entity modified. See [Why IsNew is not part of IsModified](#why-isnew-is-not-part-of-ismodified).

`IsDeleted` indicates the entity has been marked for deletion and requires a Delete operation. Deleting is a modification; `UnDelete()` before the save returns the entity to its fetched baseline:

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

A deleted, persisted entity routes to `[Delete]` when saved:

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

Deleted entities contribute to both `IsModified` and `IsSelfModified`, ensuring they are recognized as changed and savable.

**Architecture Note:** `IsNew` and `IsDeleted` are treated differently on purpose, because they say different things about user work.

- `IsDeleted` **does** contribute to `IsModified` and `IsSelfModified`. Deleting is something the user did; discarding it would lose that decision.
- `IsNew` contributes to neither. Creating an object is not, by itself, work the user would mourn — so a new entity is savable (via `IsSavable`) without being modified.

Save routing tests `IsDeleted` first: a created-then-deleted entity is modified (by `IsDeleted`) and savable, and the routing short-circuits it rather than deleting a row that was never written; a deleted persisted entity routes to `[Delete]`; a new entity to `[Insert]`; everything else to `[Update]`.

---

**UPDATED:** 2026-10-06
