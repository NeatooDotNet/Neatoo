using Neatoo;
using Neatoo.Internal;
using Neatoo.RemoteFactory;
using Xunit;

namespace Samples;

// -----------------------------------------------------------------
// Entity and list classes for collections samples
// -----------------------------------------------------------------

/// <summary>
/// ValidateListBase item for validation samples.
/// </summary>
[Factory]
public partial class CollectionValidateItem : ValidateBase<CollectionValidateItem>
{
    public CollectionValidateItem(IValidateBaseServices<CollectionValidateItem> services) : base(services)
    {
        // Add validation rule for Name
        RuleManager.AddValidation(
            item => !string.IsNullOrEmpty(item.Name) ? "" : "Name is required",
            i => i.Name);
    }

    public partial string Name { get; set; }

    public partial int Value { get; set; }
}

public interface ICollectionValidateItem : IValidateBase
{
    string Name { get; set; }
    int Value { get; set; }
}

/// <summary>
/// ValidateListBase sample for value object collections.
/// </summary>
#region collections-validate-list-definition
public class CollectionValidateItemList : ValidateListBase<CollectionValidateItem>
{
}
#endregion

/// <summary>
/// EntityListBase item for entity collection samples.
/// </summary>
public interface ICollectionOrderItem : IEntityBase
{
    string ProductCode { get; set; }
    decimal Price { get; set; }
    int Quantity { get; set; }

    // Expose protected methods for testing
    void DoMarkOld();
    void DoMarkUnmodified();
}

[Factory]
public partial class CollectionOrderItem : EntityBase<CollectionOrderItem>, ICollectionOrderItem
{
    public CollectionOrderItem(IEntityBaseServices<CollectionOrderItem> services) : base(services)
    {
        // Add validation rule for ProductCode
        RuleManager.AddValidation(
            item => !string.IsNullOrEmpty(item.ProductCode) ? "" : "Product code is required",
            i => i.ProductCode);
    }

    public partial string ProductCode { get; set; }

    public partial decimal Price { get; set; }

    public partial int Quantity { get; set; }

    // Expose protected methods for testing
    public void DoMarkOld() => MarkOld();
    public void DoMarkUnmodified() => MarkUnmodified();
}

/// <summary>
/// EntityListBase sample for entity collections.
/// </summary>
public interface ICollectionOrderItemList : IEntityListBase<ICollectionOrderItem>
{
    int DeletedCount { get; }

    // Expose factory methods for testing
    void DoFactoryStart(FactoryOperation operation);
    void DoFactoryComplete(FactoryOperation operation);
}

#region collections-entity-list-definition
public class CollectionOrderItemList : EntityListBase<ICollectionOrderItem>, ICollectionOrderItemList
{
    public int DeletedCount => DeletedList.Count;

    // Expose factory methods for testing
    public void DoFactoryStart(FactoryOperation operation) => FactoryStart(operation);
    public void DoFactoryComplete(FactoryOperation operation) => FactoryComplete(operation);
}
#endregion

/// <summary>
/// Order aggregate root for parent cascade samples.
/// </summary>
[Factory]
public partial class CollectionOrder : EntityBase<CollectionOrder>
{
    public CollectionOrder(IEntityBaseServices<CollectionOrder> services) : base(services)
    {
        // Initialize the items collection
        ItemsProperty.LoadValue(new CollectionOrderItemList());
    }

    public partial string OrderNumber { get; set; }

    public partial DateTime OrderDate { get; set; }

    // Partial property establishes parent-child tracking relationship
    public partial ICollectionOrderItemList Items { get; set; }

    // Expose protected method for samples
    public void DoMarkUnmodified() => MarkUnmodified();
}

// -----------------------------------------------------------------
// Test classes for collections samples
// -----------------------------------------------------------------

public class CollectionsSamplesTests
{
    #region collections-add-item
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
    #endregion

    #region collections-remove-validate
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
    #endregion

    #region collections-remove-entity
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
    #endregion

    #region collections-parent-cascade
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
    #endregion

    #region collections-validation
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
    #endregion

    #region collections-run-rules
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
    #endregion

    #region collections-iteration
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
    #endregion

    #region collections-deleted-list
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
    #endregion
}
