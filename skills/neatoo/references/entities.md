# Entities

`EntityBase<T>` provides the foundation for persistent entities with full CRUD lifecycle, change tracking, and save routing.

## Basic Entity Definition

Inherit from `EntityBase<T>` and add factory methods:

<!-- snippet: entities-base-class -->
<a id='snippet-entities-base-class'></a>
```cs
/// <summary>
/// Employee entity demonstrating EntityBase lifecycle.
/// </summary>
[Factory]
public partial class SkillEntityEmployee : EntityBase<SkillEntityEmployee>
{
    public SkillEntityEmployee(IEntityBaseServices<SkillEntityEmployee> services) : base(services)
    {
        RuleManager.AddValidation(
            emp => !string.IsNullOrEmpty(emp.Name) ? "" : "Name is required",
            e => e.Name);
    }

    public partial int Id { get; set; }

    [Required]
    public partial string Name { get; set; }

    public partial string Department { get; set; }

    public partial decimal Salary { get; set; }

    // Expose protected methods for demonstration
    public void DoMarkNew() => MarkNew();
    public void DoMarkOld() => MarkOld();
    public void DoMarkUnmodified() => MarkUnmodified();

    [Create]
    public void Create()
    {
        Id = 0;
        Name = "";
        Department = "";
        Salary = 0;
    }

    [Fetch]
    public void Fetch(int id, string name, string department, decimal salary)
    {
        Id = id;
        Name = name;
        Department = department;
        Salary = salary;
    }

    [Insert]
    public Task InsertAsync([Service] ISkillEntityRepository repo) => repo.InsertAsync(this);

    [Update]
    public Task UpdateAsync([Service] ISkillEntityRepository repo) => repo.UpdateAsync(this);

    [Delete]
    public Task DeleteAsync([Service] ISkillEntityRepository repo) => repo.DeleteAsync(this);
}
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/EntitySamples.cs#L15-L70' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-base-class' title='Start of snippet'>anchor</a></sup>
<a id='snippet-entities-base-class-1'></a>
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
<sup><a href='/src/docs/samples/EntitiesSamples.cs#L15-L32' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-base-class-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Aggregate Root Pattern

Mark the root entity of an aggregate:

<!-- snippet: entities-aggregate-root -->
<a id='snippet-entities-aggregate-root'></a>
```cs
/// <summary>
/// Department aggregate root containing member entities.
/// </summary>
[Factory]
public partial class SkillEntityDepartment : EntityBase<SkillEntityDepartment>
{
    public SkillEntityDepartment(IEntityBaseServices<SkillEntityDepartment> services) : base(services)
    {
        MembersProperty.LoadValue(new SkillEntityDepartmentMemberList());
    }

    public partial int Id { get; set; }

    public partial string Name { get; set; }

    public partial string Location { get; set; }

    // Child collection - part of the aggregate
    public partial ISkillEntityDepartmentMemberList Members { get; set; }

    [Create]
    public void Create()
    {
        Id = 0;
        Name = "";
        Location = "";
    }

    [Fetch]
    public void Fetch(
        int id, string name, string location,
        [Service] ISkillEntityDepartmentMemberFactory memberFactory)
    {
        Id = id;
        Name = name;
        Location = location;

        // Load child collection
        // Factory.Fetch creates existing (non-new) items
    }
}
// Aggregate root coordinates persistence of all contained entities.
// Save on the root saves/deletes all children as a unit.
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/EntitySamples.cs#L117-L161' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-aggregate-root' title='Start of snippet'>anchor</a></sup>
<a id='snippet-entities-aggregate-root-1'></a>
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
<sup><a href='/src/docs/samples/EntitiesSamples.cs#L108-L142' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-aggregate-root-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Factory Methods

Define factory methods for Create, Fetch, and persistence:

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
<sup><a href='/src/docs/samples/EntitiesSamples.cs#L147-L195' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-factory-methods' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Entity Lifecycle

### Creating New Entities

New entities have `IsNew = true` until saved:

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
<sup><a href='/src/docs/samples/EntitiesSamples.cs#L223-L233' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-is-new' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->
<!-- snippet: entities-lifecycle-new -->
<!-- endSnippet -->

### Fetching Existing Entities

Load entities from persistence:

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
<sup><a href='/src/docs/samples/EntitiesSamples.cs#L255-L270' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-fetch' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Saving Entities

`Save()` routes to the appropriate operation based on state:

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
<sup><a href='/src/docs/samples/EntitiesSamples.cs#L272-L287' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-save' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Save Routing:**
- `IsNew == true` → `[Insert]` method
- `IsNew == false && IsDeleted == false` → `[Update]` method
- `IsDeleted == true` → `[Delete]` method

### Deleting Entities

Mark for deletion, then save:

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
<sup><a href='/src/docs/samples/EntitiesSamples.cs#L289-L309' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-delete' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Undeleting Entities

Restore a deleted entity before save:

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
<sup><a href='/src/docs/samples/EntitiesSamples.cs#L311-L332' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-undelete' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Change Tracking

### IsModified and IsSelfModified

Track modification state:

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
<sup><a href='/src/docs/samples/EntitiesSamples.cs#L364-L389' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-modification-state' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Marking Clean/Dirty

Manually control dirty state:

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
<sup><a href='/src/docs/samples/EntitiesSamples.cs#L391-L410' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-mark-modified' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->
<!-- snippet: entities-mark-unmodified -->
<!-- endSnippet -->

## Persistence State

Entities track both modification and persistence state:

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
<sup><a href='/src/docs/samples/EntitiesSamples.cs#L436-L461' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-persistence-state' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## IsSavable

Check if an entity can be saved:

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
<sup><a href='/src/docs/samples/EntitiesSamples.cs#L463-L486' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-savable' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`IsSavable` returns `true` when:
- `IsValid == true` (passes validation)
- `IsModified == true` (has changes)
- `IsBusy == false` (no async operations pending)

## Child Entity State

Parent entities reflect child state:

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
<sup><a href='/src/docs/samples/EntitiesSamples.cs#L488-L516' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-child-state' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Factory Services

Inject services into factory methods:

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
<sup><a href='/src/docs/samples/EntitiesSamples.cs#L518-L533' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-factory-services' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Save Cancellation

Handle cancellation during save operations:

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
<sup><a href='/src/docs/samples/EntitiesSamples.cs#L535-L555' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-save-cancellation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Parent Property

Access the parent in child entities:

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
<sup><a href='/src/docs/samples/EntitiesSamples.cs#L334-L362' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-parent-property' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Related

- [Factory](factory.md) - Factory attributes and patterns
- [Collections](collections.md) - Child entity collections
- [Validation](validation.md) - IsValid and validation rules
- [Authorization](authorization.md) - CanSave, CanDelete
