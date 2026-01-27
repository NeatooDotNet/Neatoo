# Collections

`EntityListBase<I>` provides a collection of child entities with automatic parent-child relationship management, validation cascading, and change tracking.

## Basic Collection Definition

Inherit from `EntityListBase<I>`:

<!-- snippet: collections-entity-list-definition -->
<a id='snippet-collections-entity-list-definition'></a>
```cs
/// <summary>
/// EntityListBase for order line items.
/// Tracks deletions for persistence and cascades parent relationship.
/// </summary>
public class SkillCollOrderItemList : EntityListBase<ISkillCollOrderItem>, ISkillCollOrderItemList
{
    // DeletedList tracks removed existing items for DELETE persistence
    public int DeletedCount => DeletedList.Count;
}
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/CollectionSamples.cs#L65-L75' title='Snippet source file'>snippet source</a> | <a href='#snippet-collections-entity-list-definition' title='Start of snippet'>anchor</a></sup>
<a id='snippet-collections-entity-list-definition-1'></a>
```cs
public class CollectionOrderItemList : EntityListBase<ICollectionOrderItem>, ICollectionOrderItemList
{
    public int DeletedCount => DeletedList.Count;
}
```
<sup><a href='/src/docs/samples/CollectionsSamples.cs#L96-L101' title='Snippet source file'>snippet source</a> | <a href='#snippet-collections-entity-list-definition-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Adding Items

Add new items to the collection:

<!-- snippet: collections-add-item -->
<a id='snippet-collections-add-item'></a>
```cs
// Add items to entity collections:
//
// var order = orderFactory.Create();
// var item = itemFactory.Create();
// item.ProductCode = "WIDGET-001";
// item.Price = 19.99m;
// item.Quantity = 2;
//
// order.Items.Add(item);
//
// // Item is now in the collection with parent relationship
// Assert.Single(order.Items);
// Assert.Same(order, item.Parent);
// Assert.True(item.IsChild);
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/CollectionSamples.cs#L155-L170' title='Snippet source file'>snippet source</a> | <a href='#snippet-collections-add-item' title='Start of snippet'>anchor</a></sup>
<a id='snippet-collections-add-item-1'></a>
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
<sup><a href='/src/docs/samples/CollectionsSamples.cs#L135-L159' title='Snippet source file'>snippet source</a> | <a href='#snippet-collections-add-item-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Removing Items

Remove items from entity collections (tracks for deletion):

<!-- snippet: collections-remove-entity -->
<a id='snippet-collections-remove-entity'></a>
```cs
// Remove from entity lists - existing items tracked for deletion:
//
// var order = orderFactory.Create();
// var item = itemFactory.Fetch(1, "WIDGET-001", 19.99m, 1);
// order.Items.Add(item);
// order.DoMarkUnmodified();  // Simulate loaded from DB
//
// order.Items.Remove(item);
//
// // Item is in DeletedList for persistence
// Assert.True(item.IsDeleted);
// Assert.Equal(1, order.Items.DeletedCount);
// Assert.Empty(order.Items);  // Removed from active list
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/CollectionSamples.cs#L172-L186' title='Snippet source file'>snippet source</a> | <a href='#snippet-collections-remove-entity' title='Start of snippet'>anchor</a></sup>
<a id='snippet-collections-remove-entity-1'></a>
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
<sup><a href='/src/docs/samples/CollectionsSamples.cs#L181-L209' title='Snippet source file'>snippet source</a> | <a href='#snippet-collections-remove-entity-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

For validate-only collections (ValidateListBase), items are simply removed:

<!-- snippet: collections-remove-validate -->
<a id='snippet-collections-remove-validate'></a>
```cs
// Remove from validate lists - items removed immediately:
//
// var list = new SkillCollPhoneNumberList();
// var phone = phoneFactory.Create();
// phone.Number = "555-1234";
//
// list.Add(phone);
// Assert.Single(list);
//
// list.Remove(phone);
// Assert.Empty(list);  // No deletion tracking
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/CollectionSamples.cs#L188-L200' title='Snippet source file'>snippet source</a> | <a href='#snippet-collections-remove-validate' title='Start of snippet'>anchor</a></sup>
<a id='snippet-collections-remove-validate-1'></a>
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
<sup><a href='/src/docs/samples/CollectionsSamples.cs#L161-L179' title='Snippet source file'>snippet source</a> | <a href='#snippet-collections-remove-validate-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Parent Cascade

Collections automatically set parent on items:

<!-- snippet: collections-parent-cascade -->
<a id='snippet-collections-parent-cascade'></a>
```cs
// Parent relationship cascades to all items:
//
// var order = orderFactory.Create();
// var item1 = itemFactory.Create();
// var item2 = itemFactory.Create();
//
// order.Items.Add(item1);
// order.Items.Add(item2);
//
// // All items have parent set to aggregate root
// Assert.Same(order, item1.Parent);
// Assert.Same(order, item2.Parent);
//
// // Root walks to aggregate root
// Assert.Same(order, item1.Root);
// Assert.Same(order, item2.Root);
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/CollectionSamples.cs#L202-L219' title='Snippet source file'>snippet source</a> | <a href='#snippet-collections-parent-cascade' title='Start of snippet'>anchor</a></sup>
<a id='snippet-collections-parent-cascade-1'></a>
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
<sup><a href='/src/docs/samples/CollectionsSamples.cs#L211-L237' title='Snippet source file'>snippet source</a> | <a href='#snippet-collections-parent-cascade-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Collection Validation

Collection validates all children:

<!-- snippet: collections-validation -->
<a id='snippet-collections-validation'></a>
```cs
// Validation state aggregates from children:
//
// var list = new SkillCollPhoneNumberList();
// var validPhone = phoneFactory.Create();
// validPhone.Number = "555-1234";
//
// var invalidPhone = phoneFactory.Create();
// // Number is empty - invalid
//
// list.Add(validPhone);
// Assert.True(list.IsValid);
//
// list.Add(invalidPhone);
// Assert.False(list.IsValid);    // Child makes list invalid
// Assert.True(list.IsSelfValid); // List itself has no validation
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/CollectionSamples.cs#L221-L237' title='Snippet source file'>snippet source</a> | <a href='#snippet-collections-validation' title='Start of snippet'>anchor</a></sup>
<a id='snippet-collections-validation-1'></a>
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
<sup><a href='/src/docs/samples/CollectionsSamples.cs#L239-L273' title='Snippet source file'>snippet source</a> | <a href='#snippet-collections-validation-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Running Rules on Collections

Run rules across collection items:

<!-- snippet: collections-run-rules -->
<a id='snippet-collections-run-rules'></a>
```cs
// RunRules executes on all items:
//
// var list = new SkillCollPhoneNumberList();
// list.Add(phone1);
// list.Add(phone2);
//
// await list.RunRules(RunRulesFlag.All);
//
// // Each item's rules have been executed
// Assert.True(phone1.IsValid || !phone1.IsValid);  // State is determined
// Assert.True(phone2.IsValid || !phone2.IsValid);
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/CollectionSamples.cs#L239-L251' title='Snippet source file'>snippet source</a> | <a href='#snippet-collections-run-rules' title='Start of snippet'>anchor</a></sup>
<a id='snippet-collections-run-rules-1'></a>
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
<sup><a href='/src/docs/samples/CollectionsSamples.cs#L275-L305' title='Snippet source file'>snippet source</a> | <a href='#snippet-collections-run-rules-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Iterating Collections

Standard collection operations:

<!-- snippet: collections-iteration -->
<a id='snippet-collections-iteration'></a>
```cs
// Standard collection operations supported:
//
// // Count
// Assert.Equal(3, order.Items.Count);
//
// // Indexer
// var first = order.Items[0];
//
// // foreach
// foreach (var item in order.Items)
// {
//     Console.WriteLine(item.ProductCode);
// }
//
// // LINQ
// var total = order.Items.Sum(i => i.Price * i.Quantity);
// var expensive = order.Items.Where(i => i.Price > 100);
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/CollectionSamples.cs#L253-L271' title='Snippet source file'>snippet source</a> | <a href='#snippet-collections-iteration' title='Start of snippet'>anchor</a></sup>
<a id='snippet-collections-iteration-1'></a>
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
<sup><a href='/src/docs/samples/CollectionsSamples.cs#L307-L345' title='Snippet source file'>snippet source</a> | <a href='#snippet-collections-iteration-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Deleted Items

Entity collections track deleted items:

<!-- snippet: collections-deleted-list -->
<a id='snippet-collections-deleted-list'></a>
```cs
// DeletedList tracks removed existing items:
//
// // Load existing items
// order.Items.Add(itemFactory.Fetch(1, "A", 10m, 1));
// order.Items.Add(itemFactory.Fetch(2, "B", 20m, 1));
// order.DoMarkUnmodified();
//
// // Remove one
// var removed = order.Items[0];
// order.Items.Remove(removed);
//
// // Active list has 1 item
// Assert.Single(order.Items);
//
// // DeletedList has the removed item
// Assert.Equal(1, order.Items.DeletedCount);
// Assert.True(removed.IsDeleted);
//
// // Removed NEW items are not tracked (never persisted)
// var newItem = itemFactory.Create();
// order.Items.Add(newItem);
// order.Items.Remove(newItem);
// Assert.Equal(1, order.Items.DeletedCount);  // Still 1
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/CollectionSamples.cs#L273-L297' title='Snippet source file'>snippet source</a> | <a href='#snippet-collections-deleted-list' title='Start of snippet'>anchor</a></sup>
<a id='snippet-collections-deleted-list-1'></a>
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
<sup><a href='/src/docs/samples/CollectionsSamples.cs#L347-L373' title='Snippet source file'>snippet source</a> | <a href='#snippet-collections-deleted-list-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## ValidateListBase vs EntityListBase

| Feature | ValidateListBase | EntityListBase |
|---------|-----------------|----------------|
| Change Tracking | Yes | Yes |
| Validation | Yes | Yes |
| Parent Reference | Yes | Yes |
| Deleted List | No | Yes |
| Persistence | No | Yes (via parent) |

Use `ValidateListBase<T>` for collections without persistence needs.

<!-- snippet: collections-validate-list-definition -->
<a id='snippet-collections-validate-list-definition'></a>
```cs
/// <summary>
/// ValidateListBase for phone numbers (value object collection).
/// No deletion tracking - items are simply removed.
/// </summary>
public class SkillCollPhoneNumberList : ValidateListBase<ISkillCollPhoneNumber>
{
}
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/CollectionSamples.cs#L141-L149' title='Snippet source file'>snippet source</a> | <a href='#snippet-collections-validate-list-definition' title='Start of snippet'>anchor</a></sup>
<a id='snippet-collections-validate-list-definition-1'></a>
```cs
public class CollectionValidateItemList : ValidateListBase<CollectionValidateItem>
{
}
```
<sup><a href='/src/docs/samples/CollectionsSamples.cs#L43-L47' title='Snippet source file'>snippet source</a> | <a href='#snippet-collections-validate-list-definition-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Related

- [Entities](entities.md) - Parent entity patterns
- [Validation](validation.md) - Collection validation
- [Parent-Child](parent-child.md) - Parent navigation
