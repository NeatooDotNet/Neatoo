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
<a id='snippet-entities-base-class'></a>
```cs
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
```
<sup><a href='/src/samples/EntitiesSamples.cs#L15-L32' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-base-class' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Use ValidateBase for value objects and DTOs that don't need persistence tracking.

## Aggregate Root Pattern

EntityBase supports Domain-Driven Design aggregate patterns. The aggregate root is the entry point to the aggregate and the only entity directly accessible for persistence operations.

Define an aggregate root:

<!-- snippet: entities-aggregate-root -->
<a id='snippet-entities-aggregate-root'></a>
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
```
<sup><a href='/src/samples/EntitiesSamples.cs#L108-L142' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-aggregate-root' title='Start of snippet'>anchor</a></sup>
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
<a id='snippet-entities-is-new'></a>
```cs
[Fact]
public void IsNew_DistinguishesNewFromExisting()
{
    var factory = GetRequiredService<IEntitiesOrderFactory>();
    var order = factory.Create();

    // After Create: entity is new - will trigger Insert on save
    Assert.True(order.IsNew);
}
```
<sup><a href='/src/samples/EntitiesSamples.cs#L391-L401' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-is-new' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The framework manages `IsNew` automatically through factory operations. FactoryComplete sets `IsNew = true` after Create and `IsNew = false` after Insert or Fetch.

## Entity Lifecycle

Entities progress through a standard lifecycle from creation to deletion.

### New Entity Creation

Create a new entity using the Create factory method:

<!-- snippet: entities-lifecycle-new -->
<a id='snippet-entities-lifecycle-new'></a>
```cs
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
```
<sup><a href='/src/samples/EntitiesSamples.cs#L403-L421' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-lifecycle-new' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

After Create completes:
- `IsNew == true`
- `IsModified == true` (new entities are inherently modified due to IsNew)
- `IsSelfModified == false` (no properties changed yet)
- `IsValid` depends on validation rules
- `IsSavable == false` (new but no property changes)

After setting properties:
- `IsSelfModified == true` (properties were changed)
- `IsModified == true` (remains true - IsNew keeps it modified)
- `IsSavable == true` (new entity with property changes is ready for Insert)

### Fetch Existing Entity

Fetch an existing entity from persistence:

<!-- snippet: entities-fetch -->
<a id='snippet-entities-fetch'></a>
```cs
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
```
<sup><a href='/src/samples/EntitiesSamples.cs#L423-L438' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-fetch' title='Start of snippet'>anchor</a></sup>
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
<a id='snippet-entities-save'></a>
```cs
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
```
<sup><a href='/src/samples/EntitiesSamples.cs#L440-L455' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-save' title='Start of snippet'>anchor</a></sup>
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
<a id='snippet-entities-delete'></a>
```cs
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
```
<sup><a href='/src/samples/EntitiesSamples.cs#L457-L477' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-delete' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

After Delete:
- `IsDeleted == true`
- `IsModified == true` (deletion is a modification)
- Next Save call triggers Delete factory method

Reverse deletion before saving:

<!-- snippet: entities-undelete -->
<a id='snippet-entities-undelete'></a>
```cs
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
```
<sup><a href='/src/samples/EntitiesSamples.cs#L479-L500' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-undelete' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

If the entity is in a collection, `Delete()` delegates to the collection's `Remove()` method for consistency. See [Collections](collections.md) for deleted list management.

## Parent Property

The Parent property establishes the entity's position in the aggregate graph.

Parent navigation:

<!-- snippet: entities-parent-property -->
<a id='snippet-entities-parent-property'></a>
```cs
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
```
<sup><a href='/src/samples/EntitiesSamples.cs#L502-L530' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-parent-property' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Parent cascades:
- Validation state bubbles up to parent
- Modification state bubbles up to parent
- Parent changes propagate to all children in collections

For aggregate roots:
- `Parent == null` (root has no parent)
- `Root == null` (root is the root)

For child entities:
- `Parent` points to owning entity or list's parent
- `Root` navigates to the aggregate root

The Root property walks the Parent chain to find the aggregate root:
- If Parent is null, Root returns null (this entity is an aggregate root or standalone ValidateBase)
- If Parent implements IEntityBase, Root recursively returns Parent.Root (walks up to the aggregate root)
- Otherwise, Root returns Parent (Parent is the aggregate root, not an IEntityBase)

See [Parent-Child](parent-child.md) for detailed parent-child relationship management.

## Entity State Management

EntityBase tracks multiple state dimensions through meta properties.

### Modification State

Modification tracking determines if the entity needs saving:

<!-- snippet: entities-modification-state -->
<a id='snippet-entities-modification-state'></a>
```cs
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
```
<sup><a href='/src/samples/EntitiesSamples.cs#L532-L557' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-modification-state' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

- **IsModified**: True if any property changed, entity is new (`IsNew == true`), entity is deleted (`IsDeleted == true`), or explicitly marked modified. Includes child modifications cascading from collections.
- **IsSelfModified**: True if the entity's own properties changed, entity is new (`IsNew == true`), entity is deleted (`IsDeleted == true`), or explicitly marked modified. Excludes child modifications.
- **ModifiedProperties**: Collection of property names that changed since last save.
- **IsMarkedModified**: Explicitly marked modified via `MarkModified()`.

Mark entity as modified (note: `MarkModified()` is a protected method):

<!-- snippet: entities-mark-modified -->
<a id='snippet-entities-mark-modified'></a>
```cs
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
```
<sup><a href='/src/samples/EntitiesSamples.cs#L559-L578' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-mark-modified' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Modification state is cleared automatically after successful save operations. The framework calls `FactoryComplete`, which internally calls `MarkUnmodified()` to reset tracking:

<!-- snippet: entities-mark-unmodified -->
<a id='snippet-entities-mark-unmodified'></a>
```cs
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
```
<sup><a href='/src/samples/EntitiesSamples.cs#L580-L602' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-mark-unmodified' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`MarkUnmodified()` is protected and called internally by `FactoryComplete` after successful Insert or Update operations. Users do not call it directly.

### Persistence State

Persistence state determines which factory method executes on save:

<!-- snippet: entities-persistence-state -->
<a id='snippet-entities-persistence-state'></a>
```cs
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
```
<sup><a href='/src/samples/EntitiesSamples.cs#L604-L629' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-persistence-state' title='Start of snippet'>anchor</a></sup>
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
<a id='snippet-entities-savable'></a>
```cs
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
```
<sup><a href='/src/samples/EntitiesSamples.cs#L631-L654' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-savable' title='Start of snippet'>anchor</a></sup>
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
<a id='snippet-entities-child-state'></a>
```cs
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
```
<sup><a href='/src/samples/EntitiesSamples.cs#L656-L684' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-child-state' title='Start of snippet'>anchor</a></sup>
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
<a id='snippet-entities-factory-methods'></a>
```cs
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
```
<sup><a href='/src/samples/EntitiesSamples.cs#L147-L195' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-factory-methods' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The Factory property is set through dependency injection:

<!-- snippet: entities-factory-services -->
<a id='snippet-entities-factory-services'></a>
```cs
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
```
<sup><a href='/src/samples/EntitiesSamples.cs#L686-L701' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-factory-services' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

FactoryComplete executes after factory operations to manage entity state:
- After Create: Sets `IsNew = true` (entity needs Insert on save)
- After Fetch: No state changes needed (entity loads with `IsNew = false` from data source)
- After Insert/Update: Sets `IsNew = false` and calls `MarkUnmodified()` to clear modification tracking
- After Delete: No state changes (entity remains deleted)

This automatic state management ensures entity lifecycle matches persistence operations without manual intervention.

## Cancellation Support

Save operations support cancellation tokens:

<!-- snippet: entities-save-cancellation -->
<a id='snippet-entities-save-cancellation'></a>
```cs
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
```
<sup><a href='/src/samples/EntitiesSamples.cs#L703-L723' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-save-cancellation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Cancellation behavior:
- Waits for pending async operations with cancellation support
- Checks cancellation before persistence begins
- Does NOT check cancellation during Insert/Update/Delete execution to avoid data corruption
- Throws OperationCanceledException if canceled before persistence

Use cancellation to coordinate save operations with UI or timeout policies.

---

**UPDATED:** 2026-01-24
