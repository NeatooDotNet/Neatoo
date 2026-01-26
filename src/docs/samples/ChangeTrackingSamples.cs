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

    [Create]
    public void Create() { }

    [Fetch]
    public void Fetch(string name, string email, decimal salary)
    {
        Name = name;
        Email = email;
        Salary = salary;
    }
}

/// <summary>
/// Entity list for collection tracking samples.
/// </summary>
public interface ITrackingLineItem : IEntityBase
{
    string Description { get; set; }
    decimal Amount { get; set; }
}

[Factory]
public partial class TrackingLineItem : EntityBase<TrackingLineItem>, ITrackingLineItem
{
    public TrackingLineItem(IEntityBaseServices<TrackingLineItem> services) : base(services) { }

    public partial string Description { get; set; }

    public partial decimal Amount { get; set; }

    [Create]
    public void Create() { }

    [Fetch]
    public void Fetch(string description, decimal amount)
    {
        Description = description;
        Amount = amount;
    }
}

public interface ITrackingLineItemList : IEntityListBase<ITrackingLineItem>
{
    int DeletedCount { get; }
}

public class TrackingLineItemList : EntityListBase<ITrackingLineItem>, ITrackingLineItemList
{
    public int DeletedCount => DeletedList.Count;
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

    [Create]
    public void Create() { }

    [Fetch]
    public void Fetch(string invoiceNumber)
    {
        InvoiceNumber = invoiceNumber;
    }
}

// -----------------------------------------------------------------
// Test classes for change tracking samples
// -----------------------------------------------------------------

public class ChangeTrackingSamplesTests : SamplesTestBase
{
    #region tracking-self-modified
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
    #endregion

    #region tracking-is-modified
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
    #endregion

    #region tracking-mark-clean
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
    #endregion

    #region tracking-mark-modified
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
    #endregion

    #region tracking-modified-properties
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
    #endregion

    #region tracking-cascade-parent
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
    #endregion

    #region tracking-collections-modified
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
    #endregion

    #region tracking-collections-deleted
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
    #endregion

    #region tracking-self-vs-children
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
    #endregion

    #region tracking-is-savable
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
    #endregion

    #region tracking-save-checks
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
    #endregion

    #region tracking-pause-actions
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
    #endregion

    #region tracking-is-new
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
    #endregion

    #region tracking-is-deleted
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
    #endregion
}
