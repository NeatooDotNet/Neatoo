# Parent-Child

[← Entities](entities.md) | [↑ Guides](index.md) | [Properties →](properties.md)

Neatoo implements parent-child relationships through the Parent property on ValidateBase, enabling aggregate graphs where validation, dirty state, and lifecycle events cascade from children to their owning parents. This creates a tree structure where the aggregate root coordinates state across all child entities and value objects.

## Parent Property Behavior

The Parent property establishes a reference from a child object to its owning parent. This property is set automatically by collections when adding items, or manually when creating standalone child objects.

Set the parent property during child creation:

<!-- snippet: parent-child-setup -->
<a id='snippet-parent-child-setup'></a>
```cs
[Fact]
public void Parent_SetDuringChildCreation()
{
    var orderFactory = GetRequiredService<IParentChildOrderFactory>();
    var itemFactory = GetRequiredService<IParentChildLineItemFactory>();

    // Create aggregate root (order)
    var order = orderFactory.Create();
    order.CustomerName = "Acme Corp";

    // Create child entity (line item)
    var lineItem = itemFactory.Create();
    lineItem.ProductName = "Widget Pro";
    lineItem.UnitPrice = 49.99m;
    lineItem.Quantity = 5;

    // Add child to collection - Parent is set automatically
    order.LineItems.Add(lineItem);

    // Parent now references the aggregate root
    Assert.Same(order, lineItem.Parent);
}
```
<sup><a href='/src/docs/samples/ParentChildSamples.cs#L130-L153' title='Snippet source file'>snippet source</a> | <a href='#snippet-parent-child-setup' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Parent is of type `IValidateBase?` and can reference:
- Another ValidateBase or EntityBase instance (the owning entity)
- An aggregate root (when this is a direct child)
- Null for aggregate roots themselves

The framework uses Parent to navigate the aggregate tree and cascade state changes upward.

## Navigation Properties

Parent enables navigation from child to parent, while the Root property navigates to the aggregate root.

Navigate the aggregate graph:

<!-- snippet: parent-child-navigation -->
<a id='snippet-parent-child-navigation'></a>
```cs
[Fact]
public void Navigation_FromChildToRoot()
{
    var orderFactory = GetRequiredService<IParentChildOrderFactory>();
    var itemFactory = GetRequiredService<IParentChildLineItemFactory>();

    var order = orderFactory.Create();
    order.CustomerName = "Beta Inc";

    // Add multiple children
    var item1 = itemFactory.Create();
    item1.ProductName = "Gadget A";
    item1.UnitPrice = 25.00m;
    item1.Quantity = 2;

    var item2 = itemFactory.Create();
    item2.ProductName = "Gadget B";
    item2.UnitPrice = 35.00m;
    item2.Quantity = 1;

    order.LineItems.Add(item1);
    order.LineItems.Add(item2);

    // Navigate from child to parent
    Assert.Same(order, item1.Parent);
    Assert.Same(order, item2.Parent);

    // Navigate from child to aggregate root
    Assert.Same(order, item1.Root);
    Assert.Same(order, item2.Root);

    // Aggregate root has null Parent and Root
    Assert.Null(order.Parent);
    Assert.Null(order.Root);
}
```
<sup><a href='/src/docs/samples/ParentChildSamples.cs#L155-L191' title='Snippet source file'>snippet source</a> | <a href='#snippet-parent-child-navigation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Root calculation:
- If Parent is null, Root returns null (this object is the aggregate root)
- If Parent implements IEntityBase, Root recursively calls Parent.Root
- Otherwise, Parent is the root (direct child of aggregate root)

Root is computed on each access by walking the Parent chain until reaching the aggregate root. This recursive traversal ensures Root always reflects the current aggregate structure, even as entities move between collections within the aggregate.

## Aggregate Boundaries

The Parent property defines aggregate boundaries. An aggregate root has Parent == null, while all objects within the aggregate have Parent set to the owning entity or collection's parent.

Define an aggregate with children:

<!-- snippet: parent-child-aggregate-boundary -->
<a id='snippet-parent-child-aggregate-boundary'></a>
```cs
[Fact]
public void AggregateBoundary_EnforcedByParentProperty()
{
    var orderFactory = GetRequiredService<IParentChildOrderFactory>();
    var itemFactory = GetRequiredService<IParentChildLineItemFactory>();

    // Order aggregate root
    var order = orderFactory.Create();
    order.CustomerName = "Gamma LLC";

    // Child entity in the aggregate
    var lineItem = itemFactory.Create();
    lineItem.ProductName = "Component X";
    lineItem.UnitPrice = 100.00m;
    lineItem.Quantity = 3;

    // Add to aggregate
    order.LineItems.Add(lineItem);

    // Aggregate root: Parent == null, Root == null
    Assert.Null(order.Parent);
    Assert.Null(order.Root);

    // Child entity: Parent set, Root points to aggregate root
    Assert.Same(order, lineItem.Parent);
    Assert.Same(order, lineItem.Root);

    // Child is marked as child entity
    Assert.True(lineItem.IsChild);

    // Aggregate root is not a child
    Assert.False(order.IsChild);
}
```
<sup><a href='/src/docs/samples/ParentChildSamples.cs#L193-L227' title='Snippet source file'>snippet source</a> | <a href='#snippet-parent-child-aggregate-boundary' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Aggregate boundary rules:
- Aggregate roots have Parent == null and Root == null
- Child entities have Parent set and Root pointing to the aggregate root
- Child entities cannot be saved independently (IsChild == true)
- Crossing aggregate boundaries requires explicit relationship management
- Parent changes are restricted to prevent cross-aggregate contamination

Attempting to add a child with a different Root to a collection throws InvalidOperationException. This enforces aggregate boundaries at runtime.

## Cascade Validation

Validation state cascades from children to parents through property change events. When a child's validation state changes, the parent is notified and updates its own validation state.

Child validation cascades to parent:

<!-- snippet: parent-child-cascade-validation -->
<a id='snippet-parent-child-cascade-validation'></a>
```cs
[Fact]
public async Task CascadeValidation_ChildInvalidMakesParentInvalid()
{
    var orderFactory = GetRequiredService<IParentChildOrderFactory>();
    var itemFactory = GetRequiredService<IParentChildLineItemFactory>();

    var order = orderFactory.Create();
    order.CustomerName = "Delta Corp";
    await order.RunRules();

    // Order starts valid
    Assert.True(order.IsValid);

    // Create child with invalid state (empty ProductName)
    var invalidItem = itemFactory.Create();
    invalidItem.ProductName = ""; // Invalid - empty
    invalidItem.UnitPrice = 50.00m;
    invalidItem.Quantity = 1;
    await invalidItem.RunRules();

    // Child is invalid
    Assert.False(invalidItem.IsValid);

    // Add invalid child to order
    order.LineItems.Add(invalidItem);

    // Parent's IsValid reflects child's invalid state
    Assert.False(order.IsValid);

    // Fix the child
    invalidItem.ProductName = "Valid Product";
    await invalidItem.RunRules();

    // Parent becomes valid again
    Assert.True(order.IsValid);
}
```
<sup><a href='/src/docs/samples/ParentChildSamples.cs#L229-L266' title='Snippet source file'>snippet source</a> | <a href='#snippet-parent-child-cascade-validation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Cascade behavior:
- When a child becomes invalid, parent's IsValid becomes false immediately
- When a child becomes valid, parent recalculates IsValid from all children
- IsBusy cascades the same way (any busy child makes parent busy)
- PropertyMessages from children are included in parent's PropertyMessages
- Cascade continues up the Parent chain to the aggregate root

This ensures the aggregate root's validation state reflects all validation errors across the entire aggregate graph.

## Cascade Dirty State

Dirty state cascades from children to parents. When a child becomes dirty, the parent's IsDirty becomes true. This enables tracking modifications anywhere in the aggregate.

Child modifications cascade to parent:

<!-- snippet: parent-child-cascade-dirty -->
<a id='snippet-parent-child-cascade-dirty'></a>
```cs
[Fact]
public void CascadeDirty_ChildModificationCascadesToParent()
{
    var orderFactory = GetRequiredService<IParentChildOrderFactory>();
    var itemFactory = GetRequiredService<IParentChildLineItemFactory>();

    // Fetch existing order (starts clean)
    var order = orderFactory.Fetch(1, "Order 1", DateTime.Today);
    Assert.False(order.IsModified);

    // Add a new child item
    var item = itemFactory.Create();
    item.ProductName = "New Product";
    item.UnitPrice = 75.00m;
    item.Quantity = 2;
    order.LineItems.Add(item);

    // Order is modified because child was added
    Assert.True(order.IsModified);

    // Parent itself not modified (IsSelfModified is false)
    Assert.False(order.IsSelfModified);

    // The item's modification also contributes
    Assert.True(item.IsModified);
}
```
<sup><a href='/src/docs/samples/ParentChildSamples.cs#L268-L295' title='Snippet source file'>snippet source</a> | <a href='#snippet-parent-child-cascade-dirty' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Cascade rules for IsDirty:
- When a child's IsDirty changes to true, parent's IsDirty becomes true
- When a child's IsDirty changes to false, parent recalculates IsDirty from all children
- Collections aggregate IsDirty from all items
- EntityBase distinguishes IsSelfModified (entity's own properties) from IsModified (includes children)
- Adding a new child to a collection marks the parent as modified (IsModified becomes true)
- Removing an existing child marks the parent as modified

After a successful save operation, the factory completion flow clears the modified state for the entity and all children in the aggregate.

## Child Entity Lifecycle

Child entities are marked as children when added to EntityListBase. This affects their lifecycle and persistence behavior.

Child entity lifecycle tracking:

<!-- snippet: parent-child-lifecycle -->
<a id='snippet-parent-child-lifecycle'></a>
```cs
[Fact]
public async Task ChildLifecycle_MarkedWhenAddedToCollection()
{
    var orderFactory = GetRequiredService<IParentChildOrderFactory>();
    var itemFactory = GetRequiredService<IParentChildLineItemFactory>();

    var order = orderFactory.Create();

    // Create child entity
    var item = itemFactory.Create();
    item.ProductName = "New Product";
    item.UnitPrice = 99.99m;
    item.Quantity = 1;

    // Before adding: not a child
    Assert.False(item.IsChild);
    Assert.Null(item.Root);

    // Add to collection
    order.LineItems.Add(item);

    // After adding:
    // 1. IsChild is set to true
    Assert.True(item.IsChild);

    // 2. Root points to aggregate root
    Assert.Same(order, item.Root);

    // 3. Parent is set
    Assert.Same(order, item.Parent);

    // 4. IsSavable is false (children can't save independently)
    Assert.False(item.IsSavable);

    // 5. Attempting to save throws
    var exception = await Assert.ThrowsAsync<SaveOperationException>(
        () => item.Save());
    Assert.Equal(SaveFailureReason.IsChildObject, exception.Reason);
}
```
<sup><a href='/src/docs/samples/ParentChildSamples.cs#L297-L337' title='Snippet source file'>snippet source</a> | <a href='#snippet-parent-child-lifecycle' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Child entity restrictions:
- Cannot call Save() directly (throws SaveOperationException with SaveFailureReason.IsChildObject)
- IsSavable is always false
- Must be saved through the aggregate root
- Marked as IsChild when added to a collection
- Cannot be added to a different aggregate while already belonging to one

When a child entity is added to a collection:
1. Parent is set to the collection's Parent (the owning entity)
2. Root is recalculated from Parent (recursively to aggregate root)
3. IsChild is set to true
4. ContainingList is set to the collection
5. Validation and dirty state cascade to parent
6. Cross-aggregate validation ensures Root compatibility

## Collection Navigation

Child entities navigate to sibling entities through their parent's collection property. The internal ContainingList property (protected, framework use only) tracks the owning collection for delete consistency and intra-aggregate moves, but application code accesses siblings by casting Parent to the entity type and accessing its collection property.

Navigate to sibling entities through the parent collection:

<!-- snippet: parent-child-containing-list -->
<a id='snippet-parent-child-containing-list'></a>
```cs
[Fact]
public void CollectionNavigation_AccessSiblingsThroughParent()
{
    var orderFactory = GetRequiredService<IParentChildOrderFactory>();
    var itemFactory = GetRequiredService<IParentChildLineItemFactory>();

    var order = orderFactory.Create();

    // Add items
    var item1 = itemFactory.Create();
    item1.ProductName = "Product 1";
    item1.UnitPrice = 10.00m;
    item1.Quantity = 1;

    var item2 = itemFactory.Create();
    item2.ProductName = "Product 2";
    item2.UnitPrice = 20.00m;
    item2.Quantity = 2;

    order.LineItems.Add(item1);
    order.LineItems.Add(item2);

    // Access sibling through parent
    var sibling = order.LineItems[1];
    Assert.Same(item2, sibling);

    // Navigate from entity to collection to count siblings
    var siblingCount = order.LineItems.Count;
    Assert.Equal(2, siblingCount);

    // Calculate total through collection
    decimal total = 0;
    foreach (var item in order.LineItems)
    {
        total += item.UnitPrice * item.Quantity;
    }
    Assert.Equal(50.00m, total); // (10*1) + (20*2)
}
```
<sup><a href='/src/docs/samples/ParentChildSamples.cs#L339-L378' title='Snippet source file'>snippet source</a> | <a href='#snippet-parent-child-containing-list' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Navigation patterns:
- Access siblings via `((ParentChildOrder)item.Parent).LineItems[index]`
- Count siblings via `((ParentChildOrder)item.Parent).LineItems.Count`
- Iterate siblings via `foreach (var sibling in ((ParentChildOrder)item.Parent).LineItems)`
- Cast Parent to the specific parent entity type to access collection properties

The internal ContainingList property (protected, not directly accessible in application code) tracks ownership for:
- Delete consistency (calling Delete() on a child delegates to the owning collection's Remove())
- Intra-aggregate moves between collections (framework validates Root compatibility)
- Cleanup after save operations (clearing deleted items from DeletedList)

## Root Access from Children

The Root property provides direct access to the aggregate root from any child entity or value object in the graph.

Access the aggregate root from a child:

<!-- snippet: parent-child-root-access -->
<a id='snippet-parent-child-root-access'></a>
```cs
[Fact]
public void RootAccess_FromChildEntity()
{
    var orderFactory = GetRequiredService<IParentChildOrderFactory>();
    var itemFactory = GetRequiredService<IParentChildLineItemFactory>();

    var order = orderFactory.Create();
    order.CustomerName = "Epsilon Ltd";
    order.OrderDate = new DateTime(2024, 6, 15);

    var item = itemFactory.Create();
    item.ProductName = "Enterprise Widget";
    item.UnitPrice = 500.00m;
    item.Quantity = 10;

    order.LineItems.Add(item);

    // Access aggregate root from child
    var root = item.Root;
    Assert.NotNull(root);

    // Cast to specific aggregate type when needed
    var orderRoot = root as ParentChildOrder;
    Assert.NotNull(orderRoot);

    // Access aggregate-level properties from child context
    Assert.Equal("Epsilon Ltd", orderRoot!.CustomerName);
    Assert.Equal(new DateTime(2024, 6, 15), orderRoot.OrderDate);
}
```
<sup><a href='/src/docs/samples/ParentChildSamples.cs#L380-L410' title='Snippet source file'>snippet source</a> | <a href='#snippet-parent-child-root-access' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Root access patterns:
- Cast Root to the specific aggregate root type for property access
- Access aggregate-level properties from child business rules
- Coordinate cross-entity validation at the root level
- Implement aggregate-level invariants in child validation rules

Root is computed on each access by recursively walking the Parent chain to the aggregate root, so it always reflects the current aggregate structure even as entities are reparented within the aggregate.

## Aggregate Boundary Enforcement

The framework enforces aggregate boundaries when adding entities to collections. Parent is managed internally and cannot be set directly by application code.

Allowed operations:
- Adding an entity with Root == null (not yet in any aggregate)
- Adding an entity from the same aggregate (same Root reference)
- Moving an entity between collections within the same aggregate
- Removing an entity from a collection

Prohibited operations:
- Adding an entity from a different aggregate (throws InvalidOperationException with message "belongs to aggregate")
- Adding an entity while it is busy (IsBusy == true)
- Setting Parent directly (Parent is managed internally by the framework)

To move an entity across aggregates:
1. Remove from the source collection (entity goes to DeletedList if persisted)
2. After save completes, the entity is no longer in any aggregate
3. Create a new entity instance or re-fetch from persistence
4. Add to the destination aggregate

The cross-aggregate restriction enforces the DDD principle that aggregate boundaries are consistency boundaries. An entity cannot belong to multiple aggregates simultaneously, as this would create ambiguous ownership and state coordination.

## Parent in Collections

Collections set Parent on items automatically during Add operations. When a collection's own Parent changes, it propagates to all items.

Collections manage parent references:

<!-- snippet: parent-child-collection-parent -->
<a id='snippet-parent-child-collection-parent'></a>
```cs
[Fact]
public void CollectionParent_AutomaticManagement()
{
    var orderFactory = GetRequiredService<IParentChildOrderFactory>();
    var itemFactory = GetRequiredService<IParentChildLineItemFactory>();

    var order = orderFactory.Create();

    // Add items to collection
    var item1 = itemFactory.Create();
    item1.ProductName = "Item A";
    item1.UnitPrice = 15.00m;
    item1.Quantity = 3;

    var item2 = itemFactory.Create();
    item2.ProductName = "Item B";
    item2.UnitPrice = 25.00m;
    item2.Quantity = 2;

    // When items are added, Parent is set automatically
    order.LineItems.Add(item1);
    order.LineItems.Add(item2);

    Assert.Same(order, item1.Parent);
    Assert.Same(order, item2.Parent);

    // Collection's Root returns the aggregate root (cast to IEntityListBase for Root access)
    Assert.Same(order, ((IEntityListBase)order.LineItems).Root);

    // All items share the same Root
    Assert.Same(order, item1.Root);
    Assert.Same(order, item2.Root);
}
```
<sup><a href='/src/docs/samples/ParentChildSamples.cs#L412-L446' title='Snippet source file'>snippet source</a> | <a href='#snippet-parent-child-collection-parent' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Collection parent propagation:
- When an item is added to a collection, item.Parent is set to collection.Parent (the owning entity)
- When collection.Parent changes, all items in the collection receive the new parent reference
- Removed items that were persisted retain Parent and move to DeletedList until save completes
- New items (IsNew == true) are removed entirely without going to DeletedList
- Collections themselves have a Parent property to participate in the aggregate graph hierarchy

This automatic parent management eliminates the need for manual parent tracking while maintaining aggregate consistency.

## Paused Parent Cascade

During deserialization and factory operations, the framework pauses parent cascade to avoid performance issues and state corruption during batch operations.

While paused:
- Parent changes don't trigger cascade
- Validation state changes don't propagate
- Dirty state changes don't propagate
- Property change events are deferred

After resuming:
- Cached validation state is recalculated from all children
- Cached dirty state is recalculated from all children
- Property change events fire for any accumulated changes

This ensures efficient bulk operations while maintaining eventual consistency of aggregate state.

---

**UPDATED:** 2026-01-24
