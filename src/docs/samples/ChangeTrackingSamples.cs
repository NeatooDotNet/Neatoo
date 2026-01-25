using Neatoo;
using Neatoo.Internal;
using Neatoo.RemoteFactory;
using Xunit;

namespace Samples;

// -----------------------------------------------------------------
// Entity classes for change tracking samples
// -----------------------------------------------------------------

/// <summary>
/// Simple entity demonstrating basic change tracking.
/// </summary>
[Factory]
public partial class TrackingEmployee : EntityBase<TrackingEmployee>
{
    public TrackingEmployee(IEntityBaseServices<TrackingEmployee> services) : base(services) { }

    public partial string Name { get; set; }

    public partial string Email { get; set; }

    public partial decimal Salary { get; set; }

    // Expose protected method for samples
    public void DoMarkModified() => MarkModified();

    // Expose protected method for samples
    public void DoMarkUnmodified() => MarkUnmodified();
}

/// <summary>
/// Entity list for collection tracking samples.
/// </summary>
public interface ITrackingLineItem : IEntityBase
{
    string Description { get; set; }
    decimal Amount { get; set; }

    // Expose protected methods for testing
    void DoMarkOld();
    void DoMarkUnmodified();
}

[Factory]
public partial class TrackingLineItem : EntityBase<TrackingLineItem>, ITrackingLineItem
{
    public TrackingLineItem(IEntityBaseServices<TrackingLineItem> services) : base(services) { }

    public partial string Description { get; set; }

    public partial decimal Amount { get; set; }

    // Expose protected methods for testing
    public void DoMarkOld() => MarkOld();
    public void DoMarkUnmodified() => MarkUnmodified();
}

public interface ITrackingLineItemList : IEntityListBase<ITrackingLineItem>
{
    int DeletedCount { get; }

    // Expose factory methods for testing
    void DoFactoryStart(FactoryOperation operation);
    void DoFactoryComplete(FactoryOperation operation);
}

public class TrackingLineItemList : EntityListBase<ITrackingLineItem>, ITrackingLineItemList
{
    public int DeletedCount => DeletedList.Count;

    // Expose factory methods for testing
    public void DoFactoryStart(FactoryOperation operation) => FactoryStart(operation);
    public void DoFactoryComplete(FactoryOperation operation) => FactoryComplete(operation);
}

/// <summary>
/// Invoice aggregate root with line items for collection tracking.
/// </summary>
[Factory]
public partial class TrackingInvoice : EntityBase<TrackingInvoice>
{
    public TrackingInvoice(IEntityBaseServices<TrackingInvoice> services) : base(services)
    {
        // Use LoadValue to initialize without triggering modification tracking
        LineItemsProperty.LoadValue(new TrackingLineItemList());
    }

    public partial string InvoiceNumber { get; set; }

    public partial decimal Total { get; set; }

    // Partial property establishes parent-child tracking relationship
    public partial ITrackingLineItemList LineItems { get; set; }

    // Expose protected method for samples
    public void DoMarkUnmodified() => MarkUnmodified();
}

// -----------------------------------------------------------------
// Test classes for change tracking samples
// -----------------------------------------------------------------

public class ChangeTrackingSamplesTests
{
    #region tracking-self-modified
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
    #endregion

    #region tracking-is-modified
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
    #endregion

    #region tracking-mark-clean
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
    #endregion

    #region tracking-mark-modified
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
    #endregion

    #region tracking-modified-properties
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
    #endregion

    #region tracking-cascade-parent
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
    #endregion

    #region tracking-collections-modified
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
    #endregion

    #region tracking-collections-deleted
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
    #endregion

    #region tracking-self-vs-children
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
    #endregion

    #region tracking-is-savable
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
    #endregion

    #region tracking-save-checks
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
    #endregion

    #region tracking-pause-actions
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
    #endregion

    #region tracking-is-new
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
    #endregion

    #region tracking-is-deleted
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
    #endregion
}
