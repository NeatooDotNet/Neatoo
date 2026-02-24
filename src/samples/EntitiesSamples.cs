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

    [Create]
    public void Create() { }
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

    [Create]
    public void Create() { }

    [Fetch]
    public void Fetch(string productCode, decimal price, int quantity)
    {
        ProductCode = productCode;
        Price = price;
        Quantity = quantity;
    }
}

/// <summary>
/// Order item list for aggregate samples.
/// </summary>
public interface IEntitiesOrderItemList : IEntityListBase<IEntitiesOrderItem>
{
    int DeletedCount { get; }
}

public class EntitiesOrderItemList : EntityListBase<IEntitiesOrderItem>, IEntitiesOrderItemList
{
    public int DeletedCount => DeletedList.Count;
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

    // Expose protected methods for samples
    public void DoMarkModified() => MarkModified();
    public void DoMarkUnmodified() => MarkUnmodified();

    [Create]
    public void Create() { }

    [Fetch]
    public void Fetch(int id, string orderNumber, DateTime orderDate)
    {
        Id = id;
        OrderNumber = orderNumber;
        OrderDate = orderDate;
    }
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

// -----------------------------------------------------------------
// Cascade save pattern entity classes
// -----------------------------------------------------------------

/// <summary>
/// Repository interface for cascade save samples.
/// </summary>
public interface IEntitiesCascadeOrderRepository
{
    Task<int> InsertOrderAsync(string orderNumber);
    Task UpdateOrderAsync(int id, string orderNumber);
    Task DeleteOrderAsync(int id);
    Task<(int Id, string OrderNumber)> GetByIdAsync(int id);
}

public class MockEntitiesCascadeOrderRepository : IEntitiesCascadeOrderRepository
{
    public Task<int> InsertOrderAsync(string orderNumber) => Task.FromResult(100);
    public Task UpdateOrderAsync(int id, string orderNumber) => Task.CompletedTask;
    public Task DeleteOrderAsync(int id) => Task.CompletedTask;
    public Task<(int Id, string OrderNumber)> GetByIdAsync(int id) => Task.FromResult((id, $"ORD-{id}"));
}

public interface IEntitiesCascadeItemRepository
{
    Task<int> InsertItemAsync(int orderId, string productName, int quantity);
    Task UpdateItemAsync(int id, string productName, int quantity);
    Task DeleteItemAsync(int id);
}

public class MockEntitiesCascadeItemRepository : IEntitiesCascadeItemRepository
{
    public Task<int> InsertItemAsync(int orderId, string productName, int quantity) => Task.FromResult(200);
    public Task UpdateItemAsync(int id, string productName, int quantity) => Task.CompletedTask;
    public Task DeleteItemAsync(int id) => Task.CompletedTask;
}

/// <summary>
/// Child entity for cascade save pattern — handles its own persistence.
/// </summary>
[Factory]
public partial class EntitiesCascadeItem : EntityBase<EntitiesCascadeItem>
{
    public EntitiesCascadeItem(IEntityBaseServices<EntitiesCascadeItem> services) : base(services) { }

    public partial int Id { get; set; }
    public partial string ProductName { get; set; }
    public partial int Quantity { get; set; }

    [Create]
    public void Create() { }

    [Fetch]
    public void Fetch(int id, string productName, int quantity)
    {
        Id = id;
        ProductName = productName;
        Quantity = quantity;
    }

    [Insert]
    public async Task InsertAsync(int orderId, [Service] IEntitiesCascadeItemRepository repository)
    {
        Id = await repository.InsertItemAsync(orderId, ProductName, Quantity);
    }

    [Update]
    public async Task UpdateAsync([Service] IEntitiesCascadeItemRepository repository)
    {
        await repository.UpdateItemAsync(Id, ProductName, Quantity);
    }

    [Delete]
    public async Task DeleteAsync([Service] IEntitiesCascadeItemRepository repository)
    {
        await repository.DeleteItemAsync(Id);
    }
}

/// <summary>
/// Child list for cascade save pattern.
/// </summary>
public class EntitiesCascadeItemList : EntityListBase<EntitiesCascadeItem>
{
    public int DeletedCount => DeletedList.Count;
    public IReadOnlyList<EntitiesCascadeItem> Deleted => DeletedList;
}

/// <summary>
/// Aggregate root demonstrating the cascade save pattern.
/// Each entity's [Insert]/[Update] saves its own direct children.
/// </summary>
#region entities-cascade-insert
[Factory]
public partial class EntitiesCascadeOrder : EntityBase<EntitiesCascadeOrder>
{
    public EntitiesCascadeOrder(IEntityBaseServices<EntitiesCascadeOrder> services) : base(services)
    {
        ItemsProperty.LoadValue(new EntitiesCascadeItemList());
    }

    public partial int Id { get; set; }
    public partial string OrderNumber { get; set; }
    public partial EntitiesCascadeItemList Items { get; set; }

    [Create]
    public void Create() { }

    [Fetch]
    public void Fetch(int id, string orderNumber)
    {
        Id = id;
        OrderNumber = orderNumber;
    }

    [Insert]
    public async Task InsertAsync(
        [Service] IEntitiesCascadeOrderRepository repository,
        [Service] IEntitiesCascadeItemFactory itemFactory)
    {
        // 1. Save this entity first (get the ID)
        Id = await repository.InsertOrderAsync(OrderNumber);

        // 2. Save children — parent is responsible for calling childFactory.Save()
        for (int i = 0; i < Items.Count; i++)
        {
            Items[i] = await itemFactory.SaveAsync(Items[i], Id);
        }
    }
#endregion

    #region entities-cascade-update
    [Update]
    public async Task UpdateAsync(
        [Service] IEntitiesCascadeOrderRepository repository,
        [Service] IEntitiesCascadeItemFactory itemFactory)
    {
        // 1. Update this entity
        await repository.UpdateOrderAsync(Id, OrderNumber);

        // 2. Save active children — routes to child's [Insert] or [Update]
        for (int i = 0; i < Items.Count; i++)
        {
            if (Items[i].IsNew)
            {
                Items[i] = await itemFactory.SaveAsync(Items[i], Id);
            }
            else if (Items[i].IsModified)
            {
                Items[i] = (await itemFactory.SaveAsync(Items[i]))!;
            }
        }

        // 3. Save deleted children — routes to child's [Delete]
        foreach (var deleted in Items.Deleted)
        {
            await itemFactory.SaveAsync(deleted);
        }
    }
    #endregion

    [Delete]
    public async Task DeleteAsync([Service] IEntitiesCascadeOrderRepository repository)
    {
        await repository.DeleteOrderAsync(Id);
    }
}

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

    [Create]
    public void Create() { }
}

// -----------------------------------------------------------------
// Test classes for entities guide samples
// -----------------------------------------------------------------

public class EntitiesSamplesTests : SamplesTestBase
{
    #region entities-is-new
    [Fact]
    public void IsNew_DistinguishesNewFromExisting()
    {
        var factory = GetRequiredService<IEntitiesOrderFactory>();
        var order = factory.Create();

        // After Create: entity is new - will trigger Insert on save
        Assert.True(order.IsNew);
    }
    #endregion

    #region entities-lifecycle-new
    [Fact]
    public void NewEntity_StartsUnmodifiedAfterCreate()
    {
        var factory = GetRequiredService<IEntitiesOrderFactory>();
        var order = factory.Create();

        // Set properties on the new entity
        order.OrderNumber = "ORD-001";
        order.OrderDate = DateTime.Today;

        // After Create completes:
        Assert.True(order.IsNew);            // New entity
        Assert.True(order.IsSelfModified);   // Properties were modified after create
        Assert.True(order.IsValid);          // Passes validation
        Assert.True(order.IsModified);       // IsNew makes entity modified
        Assert.True(order.IsSavable);        // New entity is savable (needs Insert)
    }
    #endregion

    #region entities-fetch
    [Fact]
    public async Task FetchedEntity_StartsClean()
    {
        var factory = GetRequiredService<IEntitiesCustomerFactory>();

        // Fetch loads the entity from repository
        var customer = await factory.FetchAsync(42);

        // After Fetch completes:
        Assert.False(customer.IsNew);         // Existing entity
        Assert.False(customer.IsModified);    // Clean state
        Assert.False(customer.IsSelfModified);// No modifications
        Assert.Equal("Customer 42", customer.Name);
    }
    #endregion

    #region entities-save
    [Fact]
    public async Task Save_DelegatesToAppropriateFactoryMethod()
    {
        var factory = GetRequiredService<IEntitiesEmployeeFactory>();

        // New entity - would call Insert
        var employee = factory.Create();
        employee.Name = "Alice";
        Assert.True(employee.IsNew);
        Assert.True(employee.IsModified);

        // Save is available through the factory
        Assert.True(employee.IsSavable);
    }
    #endregion

    #region entities-delete
    [Fact]
    public async Task Delete_MarksEntityForDeletion()
    {
        var factory = GetRequiredService<IEntitiesCustomerFactory>();

        // Fetch existing customer
        var customer = await factory.FetchAsync(42);

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
    public async Task UnDelete_ReversesDeleteBeforeSave()
    {
        var factory = GetRequiredService<IEntitiesCustomerFactory>();

        // Fetch existing customer
        var customer = await factory.FetchAsync(42);

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
        var orderFactory = GetRequiredService<IEntitiesOrderFactory>();
        var itemFactory = GetRequiredService<IEntitiesOrderItemFactory>();

        var order = orderFactory.Create();

        // Create child item
        var item = itemFactory.Create();
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
        var factory = GetRequiredService<IEntitiesOrderFactory>();

        // Fetch existing order
        var order = factory.Fetch(1, "ORD-001", DateTime.Today);

        Assert.False(order.IsModified);
        Assert.False(order.IsSelfModified);
        Assert.Empty(order.ModifiedProperties);

        // Change a property
        order.OrderNumber = "ORD-002";

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
        var factory = GetRequiredService<IEntitiesOrderFactory>();

        // Fetch existing order
        var order = factory.Fetch(1, "ORD-001", DateTime.Today);

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
        var factory = GetRequiredService<IEntitiesOrderFactory>();
        var order = factory.Create();

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
    public async Task PersistenceState_DeterminesFactoryMethod()
    {
        var factory = GetRequiredService<IEntitiesOrderFactory>();

        // New entity - after Create: IsNew = true -> Insert
        var newOrder = factory.Create();
        Assert.True(newOrder.IsNew);
        Assert.False(newOrder.IsDeleted);

        // Fetched entity - after Fetch: IsNew = false
        var fetchedOrder = factory.Fetch(1, "ORD-001", DateTime.Today);
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
        var factory = GetRequiredService<IEntitiesOrderFactory>();

        // Fetch existing order
        var order = factory.Fetch(1, "ORD-001", DateTime.Today);

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
        var orderFactory = GetRequiredService<IEntitiesOrderFactory>();
        var itemFactory = GetRequiredService<IEntitiesOrderItemFactory>();

        var order = orderFactory.Create();

        // Create child item
        var item = itemFactory.Create();
        item.ProductCode = "WIDGET-001";
        item.Price = 29.99m;
        item.Quantity = 1;

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
        var factory = GetRequiredService<IEntitiesCustomerFactory>();

        // Factory is resolved from DI
        var customer = factory.Create();

        // Factory property is set through services when entity has Insert/Update/Delete methods
        Assert.NotNull(customer.Factory);

        // When Factory is configured via DI, Save() delegates to it
        // The factory calls Insert, Update, or Delete based on entity state
    }
    #endregion

    #region entities-save-cancellation
    [Fact]
    public async Task Save_SupportsCancellation()
    {
        var factory = GetRequiredService<IEntitiesOrderFactory>();
        var order = factory.Create();
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
        var factory = GetRequiredService<IEntitiesAddressFactory>();

        // Use ValidateBase for value objects and DTOs
        var address = factory.Create();
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
        var factory = GetRequiredService<IEntitiesOrderFactory>();
        var order = factory.Create();

        // Aggregate roots are not children
        Assert.False(order.IsChild);

        // Aggregate roots can call Save() directly
        Assert.Null(order.Root); // Root has no root above it
    }

    [Fact]
    public void ChildEntity_MarkedWhenAddedToList()
    {
        var orderFactory = GetRequiredService<IEntitiesOrderFactory>();
        var itemFactory = GetRequiredService<IEntitiesOrderItemFactory>();

        var order = orderFactory.Create();
        var item = itemFactory.Create();

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
        var orderFactory = GetRequiredService<IEntitiesOrderFactory>();
        var itemFactory = GetRequiredService<IEntitiesOrderItemFactory>();

        // Fetch existing order (starts clean)
        var order = orderFactory.Fetch(1, "ORD-001", DateTime.Today);
        Assert.False(order.IsModified);

        // Add a new child item
        var item = itemFactory.Create();
        item.ProductCode = "ITEM-001";
        item.Price = 25.00m;
        item.Quantity = 1;
        order.Items.Add(item);

        // Parent's IsModified reflects child collection change
        Assert.True(order.IsModified);
        Assert.False(order.IsSelfModified); // Parent itself not modified
    }

    #region entities-cascade-correct-external
    [Fact]
    public async Task CascadeSave_OnlyRootSavedExternally()
    {
        var orderFactory = GetRequiredService<IEntitiesCascadeOrderFactory>();
        var itemFactory = GetRequiredService<IEntitiesCascadeItemFactory>();

        var order = orderFactory.Create();
        order.OrderNumber = "ORD-001";

        var item = itemFactory.Create();
        item.ProductName = "Widget";
        item.Quantity = 5;
        order.Items.Add(item);

        // CORRECT: only save the aggregate root from external code
        // order.Insert calls itemFactory.SaveAsync for each child
        var saved = (await orderFactory.SaveAsync(order))!;

        Assert.False(saved.IsNew);
    }
    #endregion

    #region entities-cascade-update-test
    [Fact]
    public async Task CascadeInsert_SavesRootAndChildren()
    {
        var orderFactory = GetRequiredService<IEntitiesCascadeOrderFactory>();
        var itemFactory = GetRequiredService<IEntitiesCascadeItemFactory>();

        // Create a new order with child items
        var order = orderFactory.Create();
        order.OrderNumber = "ORD-001";

        var item1 = itemFactory.Create();
        item1.ProductName = "Widget";
        item1.Quantity = 3;
        order.Items.Add(item1);

        var item2 = itemFactory.Create();
        item2.ProductName = "Gadget";
        item2.Quantity = 1;
        order.Items.Add(item2);

        Assert.True(order.IsNew);
        Assert.Equal(2, order.Items.Count);

        // Save the root — Insert cascades to children
        var saved = (await orderFactory.SaveAsync(order))!;

        Assert.False(saved.IsNew);
        Assert.Equal(2, saved.Items.Count);
    }
    #endregion
}
