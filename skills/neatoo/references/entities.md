# Entities

`EntityBase<T>` provides the foundation for persistent entities with full CRUD lifecycle, change tracking, and save routing. For base class definitions and factory method examples, see [base-classes.md](base-classes.md).

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
<sup><a href='/src/samples/EntitiesSamples.cs#L391-L401' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-is-new' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/samples/EntitiesSamples.cs#L423-L438' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-fetch' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/samples/EntitiesSamples.cs#L440-L455' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-save' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/samples/EntitiesSamples.cs#L457-L477' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-delete' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/samples/EntitiesSamples.cs#L479-L500' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-undelete' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/samples/EntitiesSamples.cs#L532-L557' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-modification-state' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/samples/EntitiesSamples.cs#L559-L578' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-mark-modified' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/samples/EntitiesSamples.cs#L604-L629' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-persistence-state' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/samples/EntitiesSamples.cs#L631-L654' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-savable' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/samples/EntitiesSamples.cs#L656-L684' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-child-state' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/samples/EntitiesSamples.cs#L686-L701' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-factory-services' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/samples/EntitiesSamples.cs#L703-L723' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-save-cancellation' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/samples/EntitiesSamples.cs#L502-L530' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-parent-property' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Aggregate Save Cascading

Understanding how saves propagate through an aggregate hierarchy is critical. Neatoo handles state cascading and save cascading differently.

### State Cascades UP Automatically

The framework automatically propagates `IsModified`, `IsValid`, and `IsBusy` up the object graph. When a child becomes modified, its parent's `IsModified` becomes true. This is framework behavior — you don't write code for it.

```
Grandchild.Name = "new"  →  Grandchild.IsModified = true
                          →  Child.IsModified = true      (automatic)
                          →  Root.IsModified = true        (automatic)
```

### Saves Cascade DOWN Manually

**The framework does NOT auto-save children.** When `factory.SaveAsync(root)` is called, it routes to the root's `[Insert]` or `[Update]` method. That method must explicitly save each child. Those children's `[Insert]`/`[Update]` must save their children, and so on.

```
External code calls:     factory.SaveAsync(order)
                              ↓  (routes to Insert or Update based on IsNew)
Order [Insert]:          saves each OrderItem via itemFactory.SaveAsync(item, orderId)
                              ↓  (routes to Insert or Update based on IsNew)
OrderItem [Insert]:      saves each Detail via detailFactory.SaveAsync(detail, itemId)
```

### The Insert Pattern

Each entity's `[Insert]` saves this entity first (to get the ID), then saves its direct children:

<!-- snippet: entities-cascade-insert -->
<a id='snippet-entities-cascade-insert'></a>
```cs
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
```
<sup><a href='/src/samples/EntitiesSamples.cs#L289-L326' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-cascade-insert' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### The Update Pattern

The `[Update]` has three parts: save this entity, save active children (new or modified), and save deleted children:

<!-- snippet: entities-cascade-update -->
<a id='snippet-entities-cascade-update'></a>
```cs
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
```
<sup><a href='/src/samples/EntitiesSamples.cs#L328-L356' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-cascade-update' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Key:** Each child's own `[Insert]`/`[Update]`/`[Delete]` handles its persistence logic (mapping to EF entities, calling repository methods). The parent only calls `itemFactory.SaveAsync()`.

External code only saves the aggregate root:

<!-- snippet: entities-cascade-correct-external -->
<a id='snippet-entities-cascade-correct-external'></a>
```cs
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
```
<sup><a href='/src/samples/EntitiesSamples.cs#L798-L819' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-cascade-correct-external' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Rules

1. **Only the aggregate root is saved externally** — external code calls `factory.SaveAsync(root)`, never `factory.SaveAsync(child)`
2. **Each entity saves its own direct children** — don't skip levels (root should not save grandchildren)
3. **Save order matters** — parent must be inserted before children (to get the parent ID)
4. **Reassign after save** — `factory.SaveAsync()` returns a new instance, so always reassign: `Items[i] = await itemFactory.SaveAsync(Items[i])`

### Anti-Pattern: Flat Save

Do NOT save entities from a flat coordinator that reaches into the hierarchy:

```csharp
// WRONG — bypasses aggregate cascade
async Task SaveEverything()
{
    await consultationFactory.SaveAsync(consultation);  // saves its visits
    await visitFactory.SaveAsync(visit);                // WRONG: already saved by consultation
    await treatmentFactory.SaveAsync(treatment);        // WRONG: should be saved by visit
}
```

Instead, each level saves its own children:

```csharp
// CORRECT — each entity saves its direct children in [Insert]/[Update]
// Consultation.Insert → saves Visit via visitFactory.SaveAsync
// Visit.Insert → saves Treatment via treatmentFactory.SaveAsync
// External code only calls: await consultationFactory.SaveAsync(consultation)
```

### Anti-Pattern: Parent Does Child's Persistence Inline

Do NOT put child persistence logic (EF entity mapping, repository calls) in the parent's `[Insert]`/`[Update]`. Each child handles its own persistence in its own factory methods.

```csharp
// WRONG — parent manually persists children instead of delegating
[Update]
public async Task UpdateAsync([Service] IAssessmentRepository repository)
{
    var entity = await repository.GetByIdAsync(this.Id);
    MapTo(entity);

    // BAD: parent iterates children and does their persistence work
    foreach (var area in AreasList)
    {
        var areaEntity = entity.Areas.FirstOrDefault(a => a.LocationId == area.LocationId);
        if (areaEntity != null)
        {
            areaEntity.Value = area.Value?.ToString();  // parent maps child properties
        }
        else
        {
            await repository.InsertAreaAsync(new AreaEntity  // parent creates child EF entities
            {
                AssessmentId = this.Id,
                Value = area.Value?.ToString()
            });
        }
    }
}
```

```csharp
// CORRECT — parent delegates to child factory; child handles its own persistence
[Update]
public async Task UpdateAsync([Service] IAssessmentRepository repository,
    [Service] IAreaFactory areaFactory)
{
    var entity = await repository.GetByIdAsync(this.Id);
    MapTo(entity);

    // Each child's [Insert]/[Update]/[Delete] handles its own EF mapping
    for (int i = 0; i < AreasList.Count; i++)
    {
        if (AreasList[i].IsNew)
        {
            AreasList[i] = await areaFactory.SaveAsync(AreasList[i], this.Id);
        }
        else if (AreasList[i].IsModified)
        {
            AreasList[i] = (await areaFactory.SaveAsync(AreasList[i]))!;
        }
    }

    foreach (var deleted in AreasList.DeletedList)
    {
        await areaFactory.SaveAsync(deleted);
    }
}
```

**Why this matters:** When the parent does the child's persistence inline, the child's `[Insert]`/`[Update]`/`[Delete]` methods don't exist or go unused. This means:
- Child persistence logic can't be tested independently
- Conditional persistence decisions (e.g., skip empty children) get tangled into the parent
- The parent grows large and complex as the child's schema evolves

## Related

- [Factory](factory.md) - Factory attributes and patterns
- [Collections](collections.md) - Child entity collections
- [Validation](validation.md) - IsValid and validation rules
- [Authorization](authorization.md) - CanSave, CanDelete
