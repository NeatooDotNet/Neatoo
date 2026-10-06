# Parent-Child

[← Entities](entities.md) | [↑ Guides](index.md) | [Properties →](properties.md)

Neatoo implements parent-child relationships through the `Parent` property on `ValidateBase`, enabling aggregate graphs where validation and modification state cascade from children to their owning parents. The aggregate root coordinates state across every child entity and value object in the graph.

The examples on this page come from two compiled aggregates: `Order` with its `OrderItem` children, and `WorkOrder` with its `WorkOrderTask` children. The tests resolve factories from a DI scope (`DesignTestServices.GetScope()`); in an application the factory interface is injected.

## Parent Property Behavior

`Parent` is a reference from a child object to its owning entity. The property system sets it at runtime — when a child is added to a collection, or when a child is assigned to a parent's partial property. Application code never sets it. A collection is transparent: an item's `Parent` is the entity that owns the list, not the list.

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

`Parent` is typed `IValidateBase?` and references:
- The owning entity (a `ValidateBase` or `EntityBase` instance)
- The aggregate root, when this is a direct child
- `null` for the aggregate root itself

The framework uses `Parent` to navigate the aggregate tree and cascade state upward.

## Navigation Properties

`Parent` navigates one level up; `Root` navigates to the aggregate root. Both are set the moment a child joins the aggregate:

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

Root calculation: if `Parent` is `null`, `Root` is `null` (this object is the aggregate root); otherwise, if `Parent` has a `Root`, that is returned; otherwise `Parent` itself is the root. `Root` is computed on each access by walking the `Parent` chain, so it always reflects the current aggregate structure, even as entities move between collections within the aggregate.

## Aggregate Boundaries

The `Parent` property defines aggregate boundaries. An aggregate root has `Parent == null`; every object within the aggregate has `Parent` set. A child belongs to exactly one aggregate, and the list enforces that on `Add`: an item whose `Root` is a different aggregate is rejected with `InvalidOperationException`, and the message names the two aggregates so two roots of the same type are distinguishable:

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

Aggregate boundary rules:
- Aggregate roots have `Parent == null` and `Root == null`
- Child entities have `Parent` set and `Root` pointing to the aggregate root
- Child entities cannot be saved independently — their interface extends `IEntityBase`, which exposes no `Save()`
- Crossing an aggregate boundary is a copy into the target and a removal from the source, never a move (see [Aggregate Boundary Enforcement](#aggregate-boundary-enforcement))

## Cascade Validation

Validation state cascades from children to parents through property change events. When a child's validation state changes, the parent recalculates its own: `IsValid` aggregates the object and every descendant, while `IsSelfValid` is the object alone. The child's messages reach the parent's `PropertyMessages`. Where async rules may be in flight, `await WaitForTasks()` before reading validity — rules run on assignment; `RunRules` only forces a re-run.

<!-- snippet: skill-is-valid-vs-self-valid -->
<a id='snippet-skill-is-valid-vs-self-valid'></a>
```cs
[TestMethod]
public async Task InvalidChild_MakesParentInvalid_ButNotSelfInvalid()
{
    var parent = _scope.GetRequiredService<IValidationStateDemoFactory>().Create();
    parent.RequiredField = "set";
    parent.Child!.RequiredField = "set";
    await parent.WaitForTasks();
    Assert.IsTrue(parent.IsValid);

    // Break the child only
    parent.Child.RequiredField = "";
    await parent.WaitForTasks();

    Assert.IsTrue(parent.IsSelfValid, "The parent's own rules pass");
    Assert.IsFalse(parent.IsValid, "IsValid aggregates the child");
    Assert.IsFalse(parent.Child.IsValid);
    Assert.IsTrue(parent.PropertyMessages.Count > 0, "The child's message reaches the parent");
}
```
<sup><a href='/src/Design/Design.Tests/PropertyTests/ValidationStateTests.cs#L31-L50' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-is-valid-vs-self-valid' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Cascade behavior:
- When a child becomes invalid, the parent's `IsValid` becomes false; `IsSelfValid` is unaffected
- When a child becomes valid again, the parent recalculates `IsValid` from all children
- `IsBusy` cascades the same way (any busy child makes the parent busy)
- `PropertyMessages` from children are included in the parent's `PropertyMessages`
- The cascade continues up the `Parent` chain to the aggregate root

The aggregate root's validation state therefore reflects every validation error across the whole graph.

## Cascade Modification State

Modification state cascades from children to parents. When a child becomes modified, the parent's `IsModified` becomes true while its `IsSelfModified` stays false — `IsSelfModified` is the entity's own properties, `IsModified` includes its children:

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

Attaching a child to a live parent is itself a change to the graph. `Add` marks the added item modified — new or existing — because modification state (never `IsNew`) is what aggregates upward, and that is the only channel by which a new child's arrival reaches the parent:

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

Cascade rules for `IsModified`:
- When a child's `IsModified` becomes true, the parent's `IsModified` becomes true
- When a child's `IsModified` becomes false, the parent recalculates from all children
- Collections aggregate `IsModified` from their items and from pending deletions (`DeletedList`)
- `IsSelfModified` is the entity's own properties; `IsModified` includes children
- Adding an item to a live list marks the item modified, so the parent becomes modified
- Removing a fetched item puts it in the list's `DeletedList`, which makes the list and the parent modified

After a save, each saved object's own factory completion clears its own modified state: the root's `FactoryComplete(Update)` clears the root, each child saved through the list's `[Update]` is cleared by its own factory completion, and the list's `FactoryComplete(Update)` clears its `DeletedList`. There is no graph-wide cascade — an object that was not saved through its own factory operation is not cleared.

## Child Entity Lifecycle

Adding a child to an `EntityListBase` sets its `Parent`, its `Root`, and its `ContainingList`. The type system does the rest: a child entity's interface extends `IEntityBase`, so `IsSavable` and `Save()` do not exist on it — the mistake of saving a child on its own is a compile error, not a runtime check:

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

Child entity restrictions:
- `IEntityBase` (the child entity interface) does not expose `IsSavable` or `Save()` — these are on `IEntityRoot` only
- Must be saved through the aggregate root
- Given a `ContainingList` when added to a collection, which routes `Delete()` through the list
- Cannot be added to a different aggregate while already belonging to one

When a child entity is added to a collection:
1. `Parent` is set to the collection's `Parent` (the owning entity)
2. `Root` follows from `Parent` (recursively to the aggregate root)
3. `ContainingList` is set to the collection
4. The item is marked modified, so validation and modification state cascade to the parent
5. The list checks `Root` compatibility and rejects an item from another aggregate

## Collection Navigation

A child reaches its siblings through its parent's collection property, by casting `Parent` to the parent's **interface** — never to the concrete class, which is `internal`. `ContainingList` is protected, framework-only state (delete routing, intra-aggregate moves, `DeletedList` cleanup); it is not the navigation path. The sibling-uniqueness rule on `WorkOrderTask` is the shape:

<!-- snippet: skill-sibling-validation -->
<a id='snippet-skill-sibling-validation'></a>
```cs
// Sibling consistency: the message lands on this task. When a Sequence
// changes, the list re-runs the siblings' rules (see WorkOrderTaskList).
RuleManager.AddValidation(
    t => t.Parent is IWorkOrder root
         && root.Tasks!.Any(other => !ReferenceEquals(other, t) && other.Sequence == t.Sequence)
        ? "Sequence must be unique within the work order"
        : string.Empty,
    t => t.Sequence);
```
<sup><a href='/src/Design/Design.Domain/Aggregates/WorkOrderAggregate/WorkOrderTask.cs#L55-L64' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-sibling-validation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

A rule like this runs when its own property changes. When a sibling's `Sequence` changes, the other tasks' rules must be re-run too, and only the list sees every sibling — so cross-sibling re-evaluation lives on the list:

<!-- snippet: skill-cross-sibling-rules -->
<a id='snippet-skill-cross-sibling-rules'></a>
```cs
// Cross-sibling consistency lives on the LIST: an entity cannot override
// HandleNeatooPropertyChanged, and only the list sees every sibling. When a
// task's Sequence changes, re-run the siblings' rules so their uniqueness
// messages update too.
protected override async Task HandleNeatooPropertyChanged(NeatooPropertyChangedEventArgs eventArgs)
{
    await base.HandleNeatooPropertyChanged(eventArgs);

    if (eventArgs.PropertyName == nameof(IWorkOrderTask.Sequence)
        && eventArgs.Source is IWorkOrderTask changed)
    {
        await Task.WhenAll(this.Except([changed])
            .Select(sibling => sibling.RunRules(nameof(IWorkOrderTask.Sequence))));
    }
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/WorkOrderAggregate/WorkOrderTaskList.cs#L26-L42' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-cross-sibling-rules' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Navigation patterns:
- Access siblings via `((IWorkOrder)task.Parent!).Tasks`
- In a rule, pattern-match instead of casting: `t.Parent is IWorkOrder root && ...` — a rule can run before the child is attached, when `Parent` is still `null`
- Cross-sibling consistency is re-evaluated by the list's `HandleNeatooPropertyChanged` override

## Root Access from Children

`Parent` (and `Root`) give a child ambient access to root state. Cast to the root's interface; the concrete type is `internal`. In a rule, pattern-match, because a rule can run before attachment:

<!-- snippet: skill-parent-in-child-rule -->
<a id='snippet-skill-parent-in-child-rule'></a>
```cs
// A child rule reads ambient root state through Parent. Parent is null
// until the task is attached, so pattern-match instead of casting.
RuleManager.AddAction(
    t => t.IsSchedulable = t.Hours > 0 && t.Parent is IWorkOrder root && !root.IsOnHold,
    t => t.Hours);
```
<sup><a href='/src/Design/Design.Domain/Aggregates/WorkOrderAggregate/WorkOrderTask.cs#L47-L53' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-parent-in-child-rule' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

In a business method the child is always attached, so a cast is safe:

<!-- snippet: skill-parent-in-child-method -->
<a id='snippet-skill-parent-in-child-method'></a>
```cs
/// <summary>
/// Child business method reading root state through Parent. Called by the
/// root's orchestrator rule whenever the discount changes.
/// </summary>
public void ApplyParentDiscount()
{
    Discount = ((IWorkOrder)this.Parent!).Discount;
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/WorkOrderAggregate/WorkOrderTask.cs#L74-L83' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-parent-in-child-method' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

A child rule that reads root state has no trigger for the root's property. The root re-runs the children's rules when that state changes (see the orchestrator rule in the main skill), and the child's derived flag bubbles back into the root:

<!-- snippet: skill-parent-read-test -->
<a id='snippet-skill-parent-read-test'></a>
```cs
[TestMethod]
public async Task ChildRule_ReadsRootStateThroughParent_AndTheRootRerunsItWhenThatStateChanges()
{
    var (order, design, build) = await CreateWithTwoTasks();
    design.Hours = 2m;
    build.Hours = 1m;
    await order.WaitForTasks();

    Assert.IsTrue(design.IsSchedulable, "Hours > 0 and the parent is not on hold");
    Assert.IsFalse(order.HasUnschedulableTasks);
    Assert.IsFalse(order.ShowHoldBanner);

    order.IsOnHold = true;
    await order.WaitForTasks();

    Assert.IsFalse(design.IsSchedulable, "The root re-ran the tasks' rules; the child rule read Parent.IsOnHold");
    Assert.IsTrue(order.HasUnschedulableTasks, "...and the child's change bubbled into the root's flag");
    Assert.IsTrue(order.ShowHoldBanner);
}
```
<sup><a href='/src/Design/Design.Tests/AggregateTests/WorkOrderAggregateTests.cs#L108-L128' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-parent-read-test' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Root access patterns:
- Cast `Root`/`Parent` to the aggregate root's interface for property access
- Access aggregate-level properties from child business rules
- Coordinate cross-entity validation at the root level
- Implement aggregate-level invariants in child validation rules

`Root` is computed on each access by walking the `Parent` chain, so it reflects the current aggregate structure even as entities are reparented within the aggregate.

## Aggregate Boundary Enforcement

The framework enforces aggregate boundaries when adding entities to collections. `Parent` is managed internally and cannot be set by application code.

Allowed operations:
- Adding an entity with `Root == null` (not yet in any aggregate)
- Adding an entity from the same aggregate (same `Root` reference)
- Moving an entity between collections within the same aggregate
- Removing an entity from a collection

Prohibited operations:
- Adding an entity from a different aggregate — throws `InvalidOperationException`; the message names both aggregates and ends "Aggregate boundaries cannot be crossed"
- Adding an entity while it is busy (`IsBusy == true`)
- Setting `Parent` directly

To move data across aggregates, create a new child in the target aggregate and remove the original from the source — the framework's own exception message says exactly this. The original goes to the source list's `DeletedList`; each root is then saved on its own:

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

Aggregate boundaries are consistency boundaries. An entity cannot belong to two aggregates at once; that would create ambiguous ownership and state coordination.

## Parent in Collections

Collections set `Parent` on items during `Add`. When a collection's own `Parent` changes — the parent assigns `Items = itemsFactory.Fetch(row.Items)` inside its `[Fetch]`, after the list has already been populated — the new parent propagates to every item. The snippet under [Parent Property Behavior](#parent-property-behavior) shows the result: the item's `Parent` and `Root` are the owning entity, and the root itself has neither.

Collection parent propagation:
- When an item is added to a collection, `item.Parent` is set to `collection.Parent` (the owning entity)
- When `collection.Parent` changes, all items receive the new parent reference
- Removed items that were persisted keep their `Parent` and `ContainingList` and sit in `DeletedList` until the save completes
- New items (`IsNew == true`) are removed entirely without going to `DeletedList`
- Collections themselves have a `Parent` and a `Root`, so they participate in the aggregate graph

## Paused Parent Cascade

During factory operations and deserialization the object is paused. While paused:
- Property setters do not run rules
- Validation and modification state changes do not propagate
- Property change events are not raised, and are not replayed later
- On an entity, a property set while paused is not marked modified

When the pause ends, cached validity and modification state are recalculated from the current children; no rules run and no catch-up events fire. Inside a factory operation this is exactly what makes plain assignment a clean baseline load. `Parent` and `ContainingList` are still applied to items added while paused — a list's `[Fetch]` adds its children paused, and they still get child identity.

---

**UPDATED:** 2026-10-06
