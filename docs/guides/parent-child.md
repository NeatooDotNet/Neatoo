# Parent-Child

[← Entities](entities.md) | [↑ Guides](index.md) | [Properties →](properties.md)

Neatoo implements parent-child relationships through the Parent property on ValidateBase, enabling aggregate graphs where validation, dirty state, and lifecycle events cascade from children to their owning parents. This creates a tree structure where the aggregate root coordinates state across all child entities and value objects.

## Parent Property Behavior

The Parent property establishes a reference from a child object to its owning parent. This property is set automatically by collections when adding items, or manually when creating standalone child objects.

Set the parent property during child creation:

<!-- snippet: parent-child-setup -->
```cs
[Fact]
public void Parent_SetDuringChildCreation()
{
    // Create aggregate root (order)
    var order = new ParentChildOrder(new EntityBaseServices<ParentChildOrder>());
    order.CustomerName = "Acme Corp";

    // Create child entity (line item)
    var lineItem = new ParentChildLineItem(new EntityBaseServices<ParentChildLineItem>());
    lineItem.ProductName = "Widget Pro";
    lineItem.UnitPrice = 49.99m;
    lineItem.Quantity = 5;

    // Add child to collection - Parent is set automatically
    order.LineItems.Add(lineItem);

    // Parent now references the aggregate root
    Assert.Same(order, lineItem.Parent);
}
```
<!-- endSnippet -->

Parent is of type object to accommodate different parent types:
- Another ValidateBase or EntityBase instance
- A collection (ValidateListBase or EntityListBase)
- An aggregate root
- Null for aggregate roots

The framework uses Parent to navigate the aggregate tree and cascade state changes upward.

## Navigation Properties

Parent enables navigation from child to parent, while the Root property navigates to the aggregate root.

Navigate the aggregate graph:

<!-- snippet: parent-child-navigation -->
```cs
[Fact]
public void Navigation_FromChildToRoot()
{
    var order = new ParentChildOrder(new EntityBaseServices<ParentChildOrder>());
    order.CustomerName = "Beta Inc";

    // Add multiple children
    var item1 = new ParentChildLineItem(new EntityBaseServices<ParentChildLineItem>());
    item1.ProductName = "Gadget A";
    item1.UnitPrice = 25.00m;
    item1.Quantity = 2;

    var item2 = new ParentChildLineItem(new EntityBaseServices<ParentChildLineItem>());
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
<!-- endSnippet -->

Root calculation:
- If Parent is null, Root is null (object is an aggregate root)
- If Parent implements IEntityBase, returns Parent.Root
- Otherwise Parent is the root

This recursive navigation walks the Parent chain to find the top-level aggregate root. Root is cached and recalculated when Parent changes.

## Aggregate Boundaries

The Parent property defines aggregate boundaries. An aggregate root has Parent == null, while all objects within the aggregate have Parent set to the owning entity or collection's parent.

Define an aggregate with children:

<!-- snippet: parent-child-aggregate-boundary -->
```cs
[Fact]
public void AggregateBoundary_EnforcedByParentProperty()
{
    // Order aggregate root
    var order = new ParentChildOrder(new EntityBaseServices<ParentChildOrder>());
    order.CustomerName = "Gamma LLC";

    // Child entity in the aggregate
    var lineItem = new ParentChildLineItem(new EntityBaseServices<ParentChildLineItem>());
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
```cs
[Fact]
public async Task CascadeValidation_ChildInvalidMakesParentInvalid()
{
    var order = new ParentChildOrder(new EntityBaseServices<ParentChildOrder>());
    order.CustomerName = "Delta Corp";
    await order.RunRules();

    // Order starts valid
    Assert.True(order.IsValid);

    // Create child with invalid state (empty ProductName)
    var invalidItem = new ParentChildLineItem(new EntityBaseServices<ParentChildLineItem>());
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
```cs
[Fact]
public void CascadeDirty_ChildModificationCascadesToParent()
{
    var order = new ParentChildOrder(new EntityBaseServices<ParentChildOrder>());

    // Create "existing" child (simulating loaded from database)
    var item = new ParentChildLineItem(new EntityBaseServices<ParentChildLineItem>());
    item.ProductName = "Existing Product";
    item.UnitPrice = 75.00m;
    item.Quantity = 2;
    item.DoMarkOld();        // Mark as existing (not new)
    item.DoMarkUnmodified(); // Clear modification tracking

    // Add during fetch operation
    order.LineItems.DoFactoryStart(FactoryOperation.Fetch);
    order.LineItems.Add(item);
    order.LineItems.DoFactoryComplete(FactoryOperation.Fetch);
    order.DoMarkUnmodified();

    // Order starts unmodified
    Assert.False(order.IsModified);
    Assert.False(order.IsSelfModified);

    // Modify the child's price
    item.UnitPrice = 80.00m;

    // Child is now modified
    Assert.True(item.IsSelfModified);

    // Parent's IsModified reflects child change
    Assert.True(order.IsModified);

    // Parent itself not modified (IsSelfModified is false)
    Assert.False(order.IsSelfModified);
}
```
<!-- endSnippet -->

Cascade rules for IsDirty:
- When a child's IsDirty changes to true, parent's IsDirty becomes true
- When a child's IsDirty changes to false, parent recalculates IsDirty from all children
- Collections aggregate IsDirty from all items
- EntityBase distinguishes IsSelfModified (entity's own properties) from IsModified (includes children)

Calling MarkClean() on the parent cascades down to all children, clearing the entire aggregate's dirty state. This is useful after successful save operations.

## Child Entity Lifecycle

Child entities are marked as children when added to EntityListBase. This affects their lifecycle and persistence behavior.

Child entity lifecycle tracking:

<!-- snippet: parent-child-lifecycle -->
```cs
[Fact]
public async Task ChildLifecycle_MarkedWhenAddedToCollection()
{
    var order = new ParentChildOrder(new EntityBaseServices<ParentChildOrder>());

    // Create child entity
    var item = new ParentChildLineItem(new EntityBaseServices<ParentChildLineItem>());
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
<!-- endSnippet -->

Child entity restrictions:
- Cannot call Save() directly (throws SaveOperationException)
- IsSavable is always false
- Must be saved through the aggregate root
- Marked as IsChild when added to a collection
- ContainingList references the owning collection

When a child is added to a collection:
1. Parent is set to the collection's Parent
2. Root is recalculated from Parent
3. IsChild is set to true
4. ContainingList is set to the collection
5. Validation and dirty state cascade to parent

## ContainingList Property

ContainingList provides a back-reference from a child entity to its owning collection. This enables navigating from entity to collection to sibling entities.

Use ContainingList to access the owning collection:

<!-- snippet: parent-child-containing-list -->
```cs
[Fact]
public void ContainingList_BackReferenceToOwningCollection()
{
    var order = new ParentChildOrder(new EntityBaseServices<ParentChildOrder>());

    // Add items
    var item1 = new ParentChildLineItem(new EntityBaseServices<ParentChildLineItem>());
    item1.ProductName = "Product 1";
    item1.UnitPrice = 10.00m;
    item1.Quantity = 1;

    var item2 = new ParentChildLineItem(new EntityBaseServices<ParentChildLineItem>());
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
<!-- endSnippet -->

ContainingList is set when:
- An item is added to EntityListBase
- An item is removed from EntityListBase (remains set until persistence completes)
- An item is moved to DeletedList

ContainingList is cleared when:
- FactoryComplete fires after successful save (for deleted items)
- Item's parent changes to a different collection

This property enables scenarios like:
- Removing an item from its current collection
- Moving an item to a different collection within the same aggregate
- Accessing sibling entities through the parent collection
- Implementing collection-level validation rules

## Root Access from Children

The Root property provides direct access to the aggregate root from any child entity or value object in the graph.

Access the aggregate root from a child:

<!-- snippet: parent-child-root-access -->
```cs
[Fact]
public void RootAccess_FromChildEntity()
{
    var order = new ParentChildOrder(new EntityBaseServices<ParentChildOrder>());
    order.CustomerName = "Epsilon Ltd";
    order.OrderDate = new DateTime(2024, 6, 15);

    var item = new ParentChildLineItem(new EntityBaseServices<ParentChildLineItem>());
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
<!-- endSnippet -->

Root access patterns:
- Cast Root to the specific aggregate type when needed
- Access aggregate-level properties from child business rules
- Coordinate cross-entity validation at the root
- Implement aggregate-level invariants in child rules

Root is recalculated whenever Parent changes, ensuring it always reflects the current aggregate structure.

## Parent Change Restrictions

Changing a child's Parent is restricted to prevent aggregate corruption. The framework enforces rules to maintain aggregate consistency.

Allowed parent changes:
- Setting Parent from null to an entity (adding to aggregate)
- Setting Parent to null (removing from aggregate)
- Setting Parent to a different entity within the same aggregate (same Root)

Prohibited parent changes:
- Setting Parent to an entity with a different Root (cross-aggregate move)
- Changing Parent while the entity is busy (IsBusy == true)

Attempting prohibited changes throws InvalidOperationException. To move an entity across aggregates:
1. Remove from the source aggregate (set Parent = null)
2. Clear any state that ties it to the source aggregate
3. Add to the destination aggregate (set Parent to new owner)

This two-step process ensures clean aggregate boundaries.

## Parent in Collections

Collections set Parent on items automatically during Add operations. When a collection's own Parent changes, it propagates to all items.

Collections manage parent references:

<!-- snippet: parent-child-collection-parent -->
```cs
[Fact]
public void CollectionParent_AutomaticManagement()
{
    var order = new ParentChildOrder(new EntityBaseServices<ParentChildOrder>());

    // Add items to collection
    var item1 = new ParentChildLineItem(new EntityBaseServices<ParentChildLineItem>());
    item1.ProductName = "Item A";
    item1.UnitPrice = 15.00m;
    item1.Quantity = 3;

    var item2 = new ParentChildLineItem(new EntityBaseServices<ParentChildLineItem>());
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
<!-- endSnippet -->

Collection parent propagation:
- When an item is added, item.Parent is set to collection.Parent
- When collection.Parent changes, all items receive the new parent
- Removed items retain Parent until persistence completes (for deleted entities)
- Collections themselves have a Parent property to participate in the aggregate graph

This automatic management eliminates manual parent tracking in most scenarios.

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
