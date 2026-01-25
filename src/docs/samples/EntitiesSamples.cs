using Neatoo;
using Neatoo.Internal;
using Neatoo.RemoteFactory;
using Xunit;

namespace Samples;

// -----------------------------------------------------------------
// Entity classes for entities guide samples
// -----------------------------------------------------------------

/// <summary>
/// Basic entity demonstrating EntityBase inheritance.
/// </summary>
#region entities-base-class
[Factory]
public partial class EntitiesEmployee : EntityBase<EntitiesEmployee>
{
    public EntitiesEmployee(IEntityBaseServices<EntitiesEmployee> services) : base(services) { }

    public partial int Id { get; set; }

    public partial string Name { get; set; }

    public partial string Email { get; set; }

    public partial decimal Salary { get; set; }
}
#endregion

/// <summary>
/// Interface for OrderItem entities.
/// </summary>
public interface IEntitiesOrderItem : IEntityBase
{
    string ProductCode { get; set; }
    decimal Price { get; set; }
    int Quantity { get; set; }

    // Expose protected methods for testing
    void DoMarkOld();
    void DoMarkUnmodified();
}

/// <summary>
/// Child entity for aggregate pattern samples.
/// </summary>
[Factory]
public partial class EntitiesOrderItem : EntityBase<EntitiesOrderItem>, IEntitiesOrderItem
{
    public EntitiesOrderItem(IEntityBaseServices<EntitiesOrderItem> services) : base(services) { }

    public partial string ProductCode { get; set; }

    public partial decimal Price { get; set; }

    public partial int Quantity { get; set; }

    // Expose protected methods for testing
    public void DoMarkOld() => MarkOld();
    public void DoMarkUnmodified() => MarkUnmodified();
}

/// <summary>
/// Order item list for aggregate samples.
/// </summary>
public interface IEntitiesOrderItemList : IEntityListBase<IEntitiesOrderItem>
{
    int DeletedCount { get; }
    void DoFactoryStart(FactoryOperation operation);
    void DoFactoryComplete(FactoryOperation operation);
}

public class EntitiesOrderItemList : EntityListBase<IEntitiesOrderItem>, IEntitiesOrderItemList
{
    public int DeletedCount => DeletedList.Count;
    public void DoFactoryStart(FactoryOperation operation) => FactoryStart(operation);
    public void DoFactoryComplete(FactoryOperation operation) => FactoryComplete(operation);
}

/// <summary>
/// Mock customer repository for factory samples.
/// </summary>
public interface IEntitiesCustomerRepository
{
    Task InsertAsync(EntitiesCustomer customer);
    Task UpdateAsync(EntitiesCustomer customer);
    Task DeleteAsync(EntitiesCustomer customer);
    Task<(int Id, string Name, string Email)> FetchAsync(int id);
}

public class MockEntitiesCustomerRepository : IEntitiesCustomerRepository
{
    public Task InsertAsync(EntitiesCustomer customer) => Task.CompletedTask;
    public Task UpdateAsync(EntitiesCustomer customer) => Task.CompletedTask;
    public Task DeleteAsync(EntitiesCustomer customer) => Task.CompletedTask;
    public Task<(int Id, string Name, string Email)> FetchAsync(int id)
    {
        return Task.FromResult((id, $"Customer {id}", $"customer{id}@example.com"));
    }
}

/// <summary>
/// Order aggregate root for aggregate pattern samples.
/// </summary>
#region entities-aggregate-root
[Factory]
public partial class EntitiesOrder : EntityBase<EntitiesOrder>
{
    public EntitiesOrder(IEntityBaseServices<EntitiesOrder> services) : base(services)
    {
        // Initialize the items collection
        ItemsProperty.LoadValue(new EntitiesOrderItemList());
    }

    public partial int Id { get; set; }

    public partial string OrderNumber { get; set; }

    public partial DateTime OrderDate { get; set; }

    // Child collection establishes aggregate boundary
    public partial IEntitiesOrderItemList Items { get; set; }

    // Expose protected methods for testing
    public void DoMarkNew() => MarkNew();
    public void DoMarkOld() => MarkOld();
    public void DoMarkUnmodified() => MarkUnmodified();
    public void DoMarkModified() => MarkModified();
    public void DoMarkAsChild() => MarkAsChild();
}
#endregion

/// <summary>
/// Customer entity with full factory method implementations.
/// </summary>
#region entities-factory-methods
[Factory]
public partial class EntitiesCustomer : EntityBase<EntitiesCustomer>
{
    public EntitiesCustomer(IEntityBaseServices<EntitiesCustomer> services) : base(services) { }

    public partial int Id { get; set; }

    public partial string Name { get; set; }

    public partial string Email { get; set; }

    // Expose protected methods for testing
    public void DoMarkNew() => MarkNew();
    public void DoMarkOld() => MarkOld();
    public void DoMarkUnmodified() => MarkUnmodified();

    [Create]
    public void Create()
    {
        // Initialize default values for new customer
        Id = 0;
        Name = "";
        Email = "";
    }

    [Fetch]
    public async Task FetchAsync(int id, [Service] IEntitiesCustomerRepository repository)
    {
        var data = await repository.FetchAsync(id);
        Id = data.Id;
        Name = data.Name;
        Email = data.Email;
    }

    [Insert]
    public async Task InsertAsync([Service] IEntitiesCustomerRepository repository)
    {
        await repository.InsertAsync(this);
    }

    [Update]
    public async Task UpdateAsync([Service] IEntitiesCustomerRepository repository)
    {
        await repository.UpdateAsync(this);
    }

    [Delete]
    public async Task DeleteAsync([Service] IEntitiesCustomerRepository repository)
    {
        await repository.DeleteAsync(this);
    }
}
#endregion

/// <summary>
/// Address value object for comparison with EntityBase.
/// </summary>
[Factory]
public partial class EntitiesAddress : ValidateBase<EntitiesAddress>
{
    public EntitiesAddress(IValidateBaseServices<EntitiesAddress> services) : base(services) { }

    public partial string Street { get; set; }

    public partial string City { get; set; }

    public partial string State { get; set; }

    public partial string ZipCode { get; set; }
}

// -----------------------------------------------------------------
// Test classes for entities guide samples
// -----------------------------------------------------------------

public class EntitiesSamplesTests
{
    #region entities-is-new
    [Fact]
    public void IsNew_DistinguishesNewFromExisting()
    {
        var order = new EntitiesOrder(new EntityBaseServices<EntitiesOrder>());

        // Entity is not new by default (factory sets this)
        Assert.False(order.IsNew);

        // Simulate factory Create operation
        order.FactoryComplete(FactoryOperation.Create);

        // Now entity is new - will trigger Insert on save
        Assert.True(order.IsNew);

        // After Insert, entity becomes existing
        order.FactoryComplete(FactoryOperation.Insert);
        Assert.False(order.IsNew);
    }
    #endregion

    #region entities-lifecycle-new
    [Fact]
    public void NewEntity_StartsUnmodifiedAfterCreate()
    {
        var order = new EntitiesOrder(new EntityBaseServices<EntitiesOrder>());

        // Initialize properties using PauseAllActions
        using (order.PauseAllActions())
        {
            order.OrderNumber = "ORD-001";
            order.OrderDate = DateTime.Today;
        }

        // Simulate factory Create operation
        order.FactoryComplete(FactoryOperation.Create);

        // After Create completes:
        Assert.True(order.IsNew);            // New entity
        Assert.False(order.IsSelfModified);  // No direct property modifications
        Assert.True(order.IsValid);          // Passes validation
        Assert.True(order.IsModified);       // IsNew makes entity modified
        Assert.True(order.IsSavable);        // New entity is savable (needs Insert)
    }
    #endregion

    #region entities-fetch
    [Fact]
    public void FetchedEntity_StartsClean()
    {
        var customer = new EntitiesCustomer(new EntityBaseServices<EntitiesCustomer>());

        // Simulate loading from database
        using (customer.PauseAllActions())
        {
            customer.Id = 42;
            customer.Name = "Acme Corp";
            customer.Email = "contact@acme.com";
        }

        // Simulate factory Fetch operation
        customer.FactoryComplete(FactoryOperation.Fetch);

        // After Fetch completes:
        Assert.False(customer.IsNew);         // Existing entity
        Assert.False(customer.IsModified);    // Clean state
        Assert.False(customer.IsSelfModified);// No modifications
        Assert.Equal("Acme Corp", customer.Name);
    }
    #endregion

    #region entities-save
    [Fact]
    public async Task Save_DelegatesToAppropriateFactoryMethod()
    {
        var employee = new EntitiesEmployee(new EntityBaseServices<EntitiesEmployee>());

        // New entity - would call Insert
        employee.FactoryComplete(FactoryOperation.Create);
        employee.Name = "Alice";
        Assert.True(employee.IsNew);
        Assert.True(employee.IsModified);

        // Without factory configured, Save throws with NoFactoryMethod reason
        var exception = await Assert.ThrowsAsync<SaveOperationException>(
            () => employee.Save());
        Assert.Equal(SaveFailureReason.NoFactoryMethod, exception.Reason);

        // After Insert, would call Update for subsequent saves
        employee.FactoryComplete(FactoryOperation.Insert);
        Assert.False(employee.IsNew);
        Assert.False(employee.IsModified); // Cleared by FactoryComplete
    }
    #endregion

    #region entities-delete
    [Fact]
    public void Delete_MarksEntityForDeletion()
    {
        var customer = new EntitiesCustomer(new EntityBaseServices<EntitiesCustomer>());

        // Simulate fetched entity
        using (customer.PauseAllActions())
        {
            customer.Id = 42;
            customer.Name = "Acme Corp";
        }
        customer.FactoryComplete(FactoryOperation.Fetch);

        Assert.False(customer.IsDeleted);
        Assert.False(customer.IsModified);

        // Mark for deletion
        customer.Delete();

        // After Delete:
        Assert.True(customer.IsDeleted);  // Marked for deletion
        Assert.True(customer.IsModified); // Deletion is a modification
        Assert.True(customer.IsSavable);  // Ready for delete operation
    }
    #endregion

    #region entities-undelete
    [Fact]
    public void UnDelete_ReversesDeleteBeforeSave()
    {
        var customer = new EntitiesCustomer(new EntityBaseServices<EntitiesCustomer>());

        // Simulate fetched entity
        using (customer.PauseAllActions())
        {
            customer.Id = 42;
            customer.Name = "Acme Corp";
        }
        customer.FactoryComplete(FactoryOperation.Fetch);

        // Mark for deletion
        customer.Delete();
        Assert.True(customer.IsDeleted);
        Assert.True(customer.IsModified);

        // Reverse deletion before save
        customer.UnDelete();

        // After UnDelete:
        Assert.False(customer.IsDeleted);  // No longer marked
        Assert.False(customer.IsModified); // Back to clean state
    }
    #endregion

    #region entities-parent-property
    [Fact]
    public void Parent_EstablishesAggregateGraph()
    {
        var order = new EntitiesOrder(new EntityBaseServices<EntitiesOrder>());

        // Create child item
        var item = new EntitiesOrderItem(new EntityBaseServices<EntitiesOrderItem>());
        item.ProductCode = "WIDGET-001";
        item.Price = 29.99m;
        item.Quantity = 2;

        // Add to collection - establishes parent relationship
        order.Items.Add(item);

        // Item's Parent points to the order
        Assert.Same(order, item.Parent);

        // For child entities, Root returns the aggregate root
        Assert.Same(order, item.Root);

        // For aggregate root, Parent and Root are null
        Assert.Null(order.Parent);
        Assert.Null(order.Root);
    }
    #endregion

    #region entities-modification-state
    [Fact]
    public void ModificationState_TracksChanges()
    {
        var order = new EntitiesOrder(new EntityBaseServices<EntitiesOrder>());
        order.DoMarkUnmodified();

        Assert.False(order.IsModified);
        Assert.False(order.IsSelfModified);
        Assert.Empty(order.ModifiedProperties);

        // Change a property
        order.OrderNumber = "ORD-001";

        // IsSelfModified: direct property change
        Assert.True(order.IsSelfModified);

        // IsModified: includes self and child modifications
        Assert.True(order.IsModified);

        // ModifiedProperties: lists changed properties
        Assert.Contains("OrderNumber", order.ModifiedProperties);
    }
    #endregion

    #region entities-mark-modified
    [Fact]
    public void MarkModified_ForcesEntityToBeSaved()
    {
        var order = new EntitiesOrder(new EntityBaseServices<EntitiesOrder>());

        // Simulate fetched entity
        using (order.PauseAllActions())
        {
            order.OrderNumber = "ORD-001";
        }
        order.FactoryComplete(FactoryOperation.Fetch);

        Assert.False(order.IsModified);
        Assert.False(order.IsMarkedModified);

        // Force entity to be saved (e.g., timestamp update)
        order.DoMarkModified();

        Assert.True(order.IsModified);
        Assert.True(order.IsSelfModified);
        Assert.True(order.IsMarkedModified);
    }
    #endregion

    #region entities-mark-unmodified
    [Fact]
    public void MarkUnmodified_ClearsAfterSave()
    {
        var order = new EntitiesOrder(new EntityBaseServices<EntitiesOrder>());

        // Make changes
        order.OrderNumber = "ORD-001";
        order.OrderDate = DateTime.Today;

        Assert.True(order.IsModified);
        Assert.Contains("OrderNumber", order.ModifiedProperties);

        // Simulate successful save via FactoryComplete
        order.FactoryComplete(FactoryOperation.Update);

        // After save, modification state is cleared
        Assert.False(order.IsModified);
        Assert.False(order.IsSelfModified);
        Assert.Empty(order.ModifiedProperties);
    }
    #endregion

    #region entities-persistence-state
    [Fact]
    public void PersistenceState_DeterminesFactoryMethod()
    {
        // New entity - starts without state
        var newOrder = new EntitiesOrder(new EntityBaseServices<EntitiesOrder>());
        Assert.False(newOrder.IsNew);
        Assert.False(newOrder.IsDeleted);

        // After Create: IsNew = true, IsDeleted = false -> Insert
        newOrder.FactoryComplete(FactoryOperation.Create);
        Assert.True(newOrder.IsNew);
        Assert.False(newOrder.IsDeleted);

        // After Insert: IsNew = false, IsDeleted = false
        newOrder.FactoryComplete(FactoryOperation.Insert);
        Assert.False(newOrder.IsNew);
        Assert.False(newOrder.IsDeleted);

        // Fetched entity scenario
        var fetchedOrder = new EntitiesOrder(new EntityBaseServices<EntitiesOrder>());
        using (fetchedOrder.PauseAllActions())
        {
            fetchedOrder.OrderNumber = "ORD-001";
        }
        // Fetch operation doesn't change IsNew (handled by deserialization)
        // but the entity should be marked old via MarkOld during fetch
        fetchedOrder.DoMarkOld();
        fetchedOrder.FactoryComplete(FactoryOperation.Fetch);
        Assert.False(fetchedOrder.IsNew);
        Assert.False(fetchedOrder.IsDeleted);

        // After Delete(): IsNew = unchanged, IsDeleted = true -> Delete
        fetchedOrder.Delete();
        Assert.False(fetchedOrder.IsNew);
        Assert.True(fetchedOrder.IsDeleted);

        // UnDelete reverses deletion
        fetchedOrder.UnDelete();
        Assert.False(fetchedOrder.IsDeleted);
    }
    #endregion

    #region entities-savable
    [Fact]
    public void IsSavable_CombinesStateChecks()
    {
        var order = new EntitiesOrder(new EntityBaseServices<EntitiesOrder>());

        // Simulate fetched entity
        using (order.PauseAllActions())
        {
            order.OrderNumber = "ORD-001";
        }
        order.FactoryComplete(FactoryOperation.Fetch);

        // Unmodified entity is not savable
        Assert.False(order.IsModified);
        Assert.False(order.IsSavable);

        // Make a change
        order.OrderNumber = "ORD-002";

        // Now check savability conditions
        Assert.True(order.IsModified);    // Something changed
        Assert.True(order.IsValid);       // Passes validation
        Assert.False(order.IsBusy);       // No async operations
        Assert.False(order.IsChild);      // Not a child entity
        Assert.True(order.IsSavable);     // Can save!
    }
    #endregion

    #region entities-child-state
    [Fact]
    public async Task ChildEntity_CannotSaveDirectly()
    {
        var order = new EntitiesOrder(new EntityBaseServices<EntitiesOrder>());

        // Create child item
        var item = new EntitiesOrderItem(new EntityBaseServices<EntitiesOrderItem>());
        item.ProductCode = "WIDGET-001";
        item.Price = 29.99m;

        // Add to collection marks entity as child
        order.Items.Add(item);

        // Child entity state
        Assert.True(item.IsChild);
        Assert.Same(order, item.Root);
        Assert.False(item.IsSavable); // Children can't save independently

        // Attempting to save throws
        var exception = await Assert.ThrowsAsync<SaveOperationException>(
            () => item.Save());
        Assert.Equal(SaveFailureReason.IsChildObject, exception.Reason);
    }
    #endregion

    #region entities-factory-services
    [Fact]
    public void Factory_SetThroughDependencyInjection()
    {
        var services = new EntityBaseServices<EntitiesEmployee>();

        // Create entity with services (normally done by DI)
        var employee = new EntitiesEmployee(services);

        // Factory property is set through services
        // (will be null here since no DI container)
        Assert.Null(employee.Factory);

        // When Factory is configured via DI, Save() delegates to it
        // The factory calls Insert, Update, or Delete based on entity state
    }
    #endregion

    #region entities-save-cancellation
    [Fact]
    public async Task Save_SupportsCancellation()
    {
        var order = new EntitiesOrder(new EntityBaseServices<EntitiesOrder>());

        // Simulate new entity
        order.FactoryComplete(FactoryOperation.Create);
        order.OrderNumber = "ORD-001";

        // Create a cancelled token
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Save checks cancellation before persistence
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => order.Save(cts.Token));

        // Entity state is unchanged when cancelled
        Assert.True(order.IsNew);
        Assert.True(order.IsModified);
    }
    #endregion

    [Fact]
    public void ValidateBase_ForValueObjects()
    {
        // Use ValidateBase for value objects and DTOs
        var address = new EntitiesAddress(new ValidateBaseServices<EntitiesAddress>());
        address.Street = "123 Main St";
        address.City = "Springfield";
        address.State = "IL";
        address.ZipCode = "62701";

        // ValidateBase provides validation without persistence tracking
        Assert.True(address.IsValid);

        // No modification tracking for value objects
        // (ValidateBase doesn't have IsModified, IsSelfModified, etc.)
    }

    [Fact]
    public void AggregateRoot_HasIsChildFalse()
    {
        var order = new EntitiesOrder(new EntityBaseServices<EntitiesOrder>());

        // Aggregate roots are not children
        Assert.False(order.IsChild);

        // Aggregate roots can call Save() directly
        Assert.Null(order.Root); // Root has no root above it
    }

    [Fact]
    public void ChildEntity_MarkedWhenAddedToList()
    {
        var order = new EntitiesOrder(new EntityBaseServices<EntitiesOrder>());
        var item = new EntitiesOrderItem(new EntityBaseServices<EntitiesOrderItem>());

        // Before adding to collection
        Assert.False(item.IsChild);

        // Add to collection
        order.Items.Add(item);

        // After adding - marked as child
        Assert.True(item.IsChild);
    }

    [Fact]
    public void ModificationCascadesToParent()
    {
        var order = new EntitiesOrder(new EntityBaseServices<EntitiesOrder>());

        // Create existing item
        var item = new EntitiesOrderItem(new EntityBaseServices<EntitiesOrderItem>());
        item.ProductCode = "ITEM-001";
        item.DoMarkOld();
        item.DoMarkUnmodified();

        // Add during fetch
        order.Items.DoFactoryStart(FactoryOperation.Fetch);
        order.Items.Add(item);
        order.Items.DoFactoryComplete(FactoryOperation.Fetch);
        order.DoMarkUnmodified();

        Assert.False(order.IsModified);

        // Modify child
        item.Price = 49.99m;

        // Parent's IsModified reflects child change
        Assert.True(order.IsModified);
        Assert.False(order.IsSelfModified); // Parent itself not modified
    }
}
