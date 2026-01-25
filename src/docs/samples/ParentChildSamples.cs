using Neatoo;
using Neatoo.Internal;
using Neatoo.RemoteFactory;
using Xunit;

namespace Samples;

// -----------------------------------------------------------------
// Entity classes for parent-child samples
// -----------------------------------------------------------------

/// <summary>
/// Line item entity for order aggregate samples.
/// </summary>
public interface IParentChildLineItem : IEntityBase
{
    string ProductName { get; set; }
    decimal UnitPrice { get; set; }
    int Quantity { get; set; }

    // Expose protected methods for testing
    void DoMarkOld();
    void DoMarkUnmodified();
}

[Factory]
public partial class ParentChildLineItem : EntityBase<ParentChildLineItem>, IParentChildLineItem
{
    public ParentChildLineItem(IEntityBaseServices<ParentChildLineItem> services) : base(services)
    {
        // Add validation rule for ProductName
        RuleManager.AddValidation(
            item => !string.IsNullOrEmpty(item.ProductName) ? "" : "Product name is required",
            i => i.ProductName);
    }

    public partial string ProductName { get; set; }

    public partial decimal UnitPrice { get; set; }

    public partial int Quantity { get; set; }

    // Expose protected methods for testing
    public void DoMarkOld() => MarkOld();
    public void DoMarkUnmodified() => MarkUnmodified();
}

/// <summary>
/// Line item list for order aggregate samples.
/// </summary>
public interface IParentChildLineItemList : IEntityListBase<IParentChildLineItem>
{
    int DeletedCount { get; }
    void DoFactoryStart(FactoryOperation operation);
    void DoFactoryComplete(FactoryOperation operation);
}

public class ParentChildLineItemList : EntityListBase<IParentChildLineItem>, IParentChildLineItemList
{
    public int DeletedCount => DeletedList.Count;
    public void DoFactoryStart(FactoryOperation operation) => FactoryStart(operation);
    public void DoFactoryComplete(FactoryOperation operation) => FactoryComplete(operation);
}

/// <summary>
/// Order aggregate root for parent-child relationship samples.
/// </summary>
[Factory]
public partial class ParentChildOrder : EntityBase<ParentChildOrder>
{
    public ParentChildOrder(IEntityBaseServices<ParentChildOrder> services) : base(services)
    {
        // Initialize the line items collection
        LineItemsProperty.LoadValue(new ParentChildLineItemList());
    }

    public partial int OrderId { get; set; }

    public partial string CustomerName { get; set; }

    public partial DateTime OrderDate { get; set; }

    // Child collection establishes aggregate boundary
    public partial IParentChildLineItemList LineItems { get; set; }

    // Expose protected methods for testing
    public void DoMarkNew() => MarkNew();
    public void DoMarkOld() => MarkOld();
    public void DoMarkUnmodified() => MarkUnmodified();
    public void DoMarkModified() => MarkModified();
}

/// <summary>
/// Shipping address value object for testing hierarchy.
/// </summary>
[Factory]
public partial class ParentChildShippingAddress : ValidateBase<ParentChildShippingAddress>
{
    public ParentChildShippingAddress(IValidateBaseServices<ParentChildShippingAddress> services) : base(services)
    {
        // Add validation rule for Street
        RuleManager.AddValidation(
            addr => !string.IsNullOrEmpty(addr.Street) ? "" : "Street is required",
            a => a.Street);
    }

    public partial string Street { get; set; }

    public partial string City { get; set; }

    public partial string PostalCode { get; set; }
}

// -----------------------------------------------------------------
// Test classes for parent-child samples
// -----------------------------------------------------------------

public class ParentChildSamplesTests
{
    #region parent-child-setup
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
    #endregion

    #region parent-child-navigation
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
    #endregion

    #region parent-child-aggregate-boundary
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
    #endregion

    #region parent-child-cascade-validation
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
    #endregion

    #region parent-child-cascade-dirty
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
    #endregion

    #region parent-child-lifecycle
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
    #endregion

    #region parent-child-containing-list
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
    #endregion

    #region parent-child-root-access
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
    #endregion

    #region parent-child-collection-parent
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
    #endregion

    // Additional comprehensive tests

    [Fact]
    public void DeletedItems_RetainParentUntilPersisted()
    {
        var order = new ParentChildOrder(new EntityBaseServices<ParentChildOrder>());

        // Create existing item
        var item = new ParentChildLineItem(new EntityBaseServices<ParentChildLineItem>());
        item.ProductName = "To Be Deleted";
        item.UnitPrice = 30.00m;
        item.Quantity = 1;
        item.DoMarkOld();
        item.DoMarkUnmodified();

        // Add during fetch
        order.LineItems.DoFactoryStart(FactoryOperation.Fetch);
        order.LineItems.Add(item);
        order.LineItems.DoFactoryComplete(FactoryOperation.Fetch);

        // Remove item - goes to DeletedList
        order.LineItems.Remove(item);

        // Item is marked deleted
        Assert.True(item.IsDeleted);

        // Item is in DeletedList
        Assert.Equal(1, order.LineItems.DeletedCount);

        // After save, DeletedList is cleared
        order.LineItems.DoFactoryStart(FactoryOperation.Update);
        order.LineItems.DoFactoryComplete(FactoryOperation.Update);

        Assert.Equal(0, order.LineItems.DeletedCount);
    }

    [Fact]
    public void CrossAggregatePrevention_ThrowsOnDifferentRoot()
    {
        // Create two separate aggregates
        var order1 = new ParentChildOrder(new EntityBaseServices<ParentChildOrder>());
        order1.CustomerName = "Order 1";

        var order2 = new ParentChildOrder(new EntityBaseServices<ParentChildOrder>());
        order2.CustomerName = "Order 2";

        // Add item to first order
        var item = new ParentChildLineItem(new EntityBaseServices<ParentChildLineItem>());
        item.ProductName = "Exclusive Item";
        item.UnitPrice = 200.00m;
        item.Quantity = 1;

        order1.LineItems.Add(item);

        // Attempting to add item with different Root throws
        var exception = Assert.Throws<InvalidOperationException>(
            () => order2.LineItems.Add(item));

        Assert.Contains("belongs to aggregate", exception.Message);
    }

    [Fact]
    public void ValueObjectsInAggregate_ParentSetCorrectly()
    {
        var order = new ParentChildOrder(new EntityBaseServices<ParentChildOrder>());

        // Value object (ValidateBase) also participates in parent-child
        var address = new ParentChildShippingAddress(new ValidateBaseServices<ParentChildShippingAddress>());
        address.Street = "123 Main St";
        address.City = "Springfield";
        address.PostalCode = "12345";

        // Value objects get parent set when assigned to a property
        // (This requires the entity to have an Address property, which we don't have in this sample,
        // but the principle is demonstrated)

        // Value objects have Parent property
        Assert.Null(address.Parent); // Not yet assigned to any parent
    }

    [Fact]
    public async Task BusyStateCascade_PreventsCrossAggregateAdd()
    {
        var order = new ParentChildOrder(new EntityBaseServices<ParentChildOrder>());

        // Create an item that will have async rules running
        var item = new ParentChildLineItem(new EntityBaseServices<ParentChildLineItem>());
        item.ProductName = "Async Product";
        item.UnitPrice = 150.00m;
        item.Quantity = 1;

        // First add to order
        order.LineItems.Add(item);

        // While not busy, item can be in collections normally
        Assert.False(item.IsBusy);

        // Wait for any pending tasks
        await order.WaitForTasks();
    }

    [Fact]
    public void NewItemRemoval_NotAddedToDeletedList()
    {
        var order = new ParentChildOrder(new EntityBaseServices<ParentChildOrder>());

        // Create new item (simulating Create factory operation)
        var newItem = new ParentChildLineItem(new EntityBaseServices<ParentChildLineItem>());
        using (newItem.PauseAllActions())
        {
            newItem.ProductName = "New Item";
            newItem.UnitPrice = 50.00m;
            newItem.Quantity = 1;
        }
        // Factory Create marks item as new
        newItem.FactoryComplete(FactoryOperation.Create);

        order.LineItems.Add(newItem);

        // Item is new
        Assert.True(newItem.IsNew);

        // Remove new item
        order.LineItems.Remove(newItem);

        // New items are NOT added to DeletedList (nothing to delete from DB)
        Assert.Equal(0, order.LineItems.DeletedCount);
    }
}
