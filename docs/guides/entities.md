# Entities

[← Collections](collections.md) | [↑ Guides](index.md) | [Parent-Child →](parent-child.md)

EntityBase extends ValidateBase with entity-specific capabilities for persistence, modification tracking, and aggregate root patterns. Entities track their lifecycle state (new, existing, deleted), support save operations through factory methods, and manage parent-child relationships within aggregates.

## EntityBase vs ValidateBase

EntityBase inherits from ValidateBase and adds entity-specific features for persistence scenarios.

ValidateBase provides:
- Property change tracking
- Validation rules
- Meta properties (IsValid, IsBusy)
- Parent property

EntityBase adds:
- Modification tracking (IsModified, IsSelfModified, ModifiedProperties)
- Persistence state (IsNew, IsDeleted)
- Save operations (Save, Delete)
- Aggregate patterns (Root, IsChild, IsSavable)
- Factory integration for Insert/Update/Delete

Inherit from EntityBase when the object requires persistence:

<!-- snippet: entities-base-class -->
```cs
[Factory]
public partial class EntitiesEmployee : EntityBase<EntitiesEmployee>
{
    public EntitiesEmployee(IEntityBaseServices<EntitiesEmployee> services) : base(services) { }

    public partial int Id { get; set; }

    public partial string Name { get; set; }

    public partial string Email { get; set; }

    public partial decimal Salary { get; set; }
}
```
<!-- endSnippet -->

Use ValidateBase for value objects and DTOs that don't need persistence tracking.

## Aggregate Root Pattern

EntityBase supports Domain-Driven Design aggregate patterns. The aggregate root is the entry point to the aggregate and the only entity directly accessible for persistence operations.

Define an aggregate root:

<!-- snippet: entities-aggregate-root -->
```cs
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
```
<!-- endSnippet -->

Aggregate roots:
- Have `IsChild == false`
- Can call `Save()` directly
- Have `Root == null` (they are the root)
- Coordinate saving child entities

Child entities within the aggregate:
- Have `IsChild == true`
- Cannot call `Save()` directly (throws SaveOperationException)
- Have `Root` pointing to the aggregate root
- Are saved through the parent's save operation

## Identity and IsNew

The `IsNew` property distinguishes new entities (not yet persisted) from existing entities (already in the database).

New entities:
- `IsNew == true`
- Trigger Insert factory method on save
- Created via Create factory method or constructor
- Transition to existing after successful Insert

Existing entities:
- `IsNew == false`
- Trigger Update factory method on save (if modified)
- Fetched via Fetch factory method
- Remain existing after Update

Check entity state:

<!-- snippet: entities-is-new -->
```cs
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
```
<!-- endSnippet -->

The framework manages `IsNew` automatically through factory operations. FactoryComplete sets `IsNew = true` after Create and `IsNew = false` after Insert or Fetch.

## Entity Lifecycle

Entities progress through a standard lifecycle from creation to deletion.

### New Entity Creation

Create a new entity using the Create factory method:

<!-- snippet: entities-lifecycle-new -->
```cs
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
```
<!-- endSnippet -->

After Create completes:
- `IsNew == true`
- `IsModified == false` (new entities start unmodified)
- `IsValid` depends on validation rules
- `IsSavable == false` (not modified yet)

### Fetch Existing Entity

Fetch an existing entity from persistence:

<!-- snippet: entities-fetch -->
```cs
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
```
<!-- endSnippet -->

After Fetch completes:
- `IsNew == false`
- `IsModified == false`
- Properties loaded from database
- Entity is in a clean state

### Save Operations

Save persists the entity through Insert, Update, or Delete factory methods based on entity state.

Save delegates to the appropriate factory method:

<!-- snippet: entities-save -->
```cs
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
```
<!-- endSnippet -->

Save logic:
- If `IsNew == true`, calls Insert factory method
- If `IsDeleted == true`, calls Delete factory method
- Otherwise, calls Update factory method

Save only succeeds when:
- `IsSavable == true`
- `IsValid == true`
- `IsModified == true`
- `IsBusy == false`
- `IsChild == false`

After successful Insert or Update:
- `IsModified == false`
- `IsNew == false`
- `ModifiedProperties` is cleared

### Delete Operations

Mark an entity for deletion:

<!-- snippet: entities-delete -->
```cs
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
```
<!-- endSnippet -->

After Delete:
- `IsDeleted == true`
- `IsModified == true` (deletion is a modification)
- Next Save call triggers Delete factory method

Reverse deletion before saving:

<!-- snippet: entities-undelete -->
```cs
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
```
<!-- endSnippet -->

If the entity is in a collection, `Delete()` delegates to the collection's `Remove()` method for consistency. See [Collections](collections.md) for deleted list management.

## Parent Property

The Parent property establishes the entity's position in the aggregate graph.

Parent navigation:

<!-- snippet: entities-parent-property -->
```cs
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
```
<!-- endSnippet -->

Parent cascades:
- Validation state bubbles up to parent
- IsDirty state bubbles up to parent
- Parent changes propagate to all children in collections

For aggregate roots:
- `Parent == null` (root has no parent)
- `Root == null` (root is the root)

For child entities:
- `Parent` points to owning entity or list's parent
- `Root` navigates to the aggregate root

The Root property automatically walks the Parent chain:
- If Parent is null, Root is null
- If Parent implements IEntityBase, returns Parent.Root
- Otherwise, Parent is the root

See [Parent-Child](parent-child.md) for detailed parent-child relationship management.

## Entity State Management

EntityBase tracks multiple state dimensions through meta properties.

### Modification State

Modification tracking determines if the entity needs saving:

<!-- snippet: entities-modification-state -->
```cs
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
```
<!-- endSnippet -->

- **IsModified**: True if any property changed, entity is new/deleted, or explicitly marked modified. Includes child modifications.
- **IsSelfModified**: True if the entity's own properties changed, entity is new/deleted, or explicitly marked modified. Excludes child modifications.
- **ModifiedProperties**: Collection of property names that changed since last save.
- **IsMarkedModified**: Explicitly marked modified via `MarkModified()`.

Mark entity as modified:

<!-- snippet: entities-mark-modified -->
```cs
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
```
<!-- endSnippet -->

Clear modification state after save:

<!-- snippet: entities-mark-unmodified -->
```cs
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
```
<!-- endSnippet -->

`MarkUnmodified()` is called automatically after successful Insert or Update operations.

### Persistence State

Persistence state determines which factory method executes on save:

<!-- snippet: entities-persistence-state -->
```cs
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
```
<!-- endSnippet -->

State transitions:
- After Create: `IsNew = true, IsDeleted = false`
- After Fetch: `IsNew = false, IsDeleted = false`
- After Delete(): `IsNew = unchanged, IsDeleted = true`
- After Insert: `IsNew = false, IsDeleted = false`
- After Update: `IsNew = false, IsDeleted = false`

### Savability

IsSavable determines if Save() can proceed:

<!-- snippet: entities-savable -->
```cs
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
```
<!-- endSnippet -->

IsSavable is true when:
- `IsModified == true` (something changed)
- `IsValid == true` (passes validation)
- `IsBusy == false` (no async operations running)
- `IsChild == false` (not a child entity)

Child entities must be saved through their parent aggregate root.

### Child Entity State

Child entities are managed by their parent aggregate:

<!-- snippet: entities-child-state -->
```cs
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
```
<!-- endSnippet -->

Child entities:
- Are marked as children when added to EntityListBase
- Cannot call Save() directly
- Have ContainingList set to the owning collection
- Are saved through the aggregate root's save operation

## Factory Integration

EntityBase integrates with RemoteFactory for persistence operations.

Factory methods are defined with attributes:

<!-- snippet: entities-factory-methods -->
```cs
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
```
<!-- endSnippet -->

The Factory property is set through dependency injection:

<!-- snippet: entities-factory-services -->
```cs
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
```
<!-- endSnippet -->

FactoryComplete executes after factory operations:
- After Create: Marks entity as new
- After Fetch: No state changes (already marked old during deserialization)
- After Insert/Update: Marks entity unmodified and old
- After Delete: No state changes

This state management ensures the entity lifecycle matches persistence operations.

## Cancellation Support

Save operations support cancellation tokens:

<!-- snippet: entities-save-cancellation -->
```cs
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
```
<!-- endSnippet -->

Cancellation behavior:
- Waits for pending async operations with cancellation support
- Checks cancellation before persistence begins
- Does NOT check cancellation during Insert/Update/Delete execution to avoid data corruption
- Throws OperationCanceledException if canceled before persistence

Use cancellation to coordinate save operations with UI or timeout policies.

---

**UPDATED:** 2026-01-24
