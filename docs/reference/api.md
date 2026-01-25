[↑ Up](index.md)

# API Reference

Complete reference documentation for Neatoo's core classes, interfaces, attributes, and source-generated members. This reference targets expert .NET developers implementing DDD aggregates with validation and persistence.

## Contents

- [ValidateBase\<T\>](#validatebaset)
- [EntityBase\<T\>](#entitybaset)
- [ValidateListBase\<T\>](#validatelistbaset)
- [EntityListBase\<T\>](#entitylistbaset)
- [Key Interfaces](#key-interfaces)
- [Attributes](#attributes)
- [Source Generator Output](#source-generator-output)

---

## ValidateBase\<T\>

Abstract base class providing property management, validation, business rules, and parent-child relationships.

### Constructor

```csharp
protected ValidateBase(IValidateBaseServices<T> services)
```

The constructor accepts dependency-injected services containing property factory, rule manager factory, and property info list. Use the source-generated factory methods instead of direct construction.

### Property System

#### Getter\<P\> / Setter\<P\>

**Deprecated:** These methods exist for backward compatibility with nested test classes. Use partial properties instead.

```csharp
[Obsolete("Use partial properties instead")]
protected virtual P? Getter<P>([CallerMemberName] string propertyName = "")

[Obsolete("Use partial properties instead")]
protected virtual void Setter<P>(P? value, [CallerMemberName] string propertyName = "")
```

Partial properties generate backing fields and property implementation via source generator.

<!-- snippet: api-validatebase-partial-properties -->
```cs
[Factory]
public partial class ApiCustomer : ValidateBase<ApiCustomer>
{
    public ApiCustomer(IValidateBaseServices<ApiCustomer> services) : base(services) { }

    // Partial properties - source generator creates backing fields and implementation
    public partial string Name { get; set; }

    public partial string Email { get; set; }

    public partial DateTime BirthDate { get; set; }
}
```
<!-- endSnippet -->

#### Property Access

```csharp
public IValidateProperty GetProperty(string propertyName)
public IValidateProperty this[string propertyName] { get; }
public bool TryGetProperty(string propertyName, out IValidateProperty validateProperty)
```

Access property metadata and validation state by name.

<!-- snippet: api-validatebase-property-access -->
```cs
[Fact]
public void PropertyAccess_ByNameAndIndexer()
{
    var search = new ApiCustomerSearch(new ValidateBaseServices<ApiCustomerSearch>());
    search.SearchTerm = "Test";
    search.Category = "Products";

    // Access property by name
    IValidateProperty searchProperty = search.GetProperty("SearchTerm");
    Assert.Equal("Test", searchProperty.Value);

    // Access property via indexer
    IValidateProperty categoryProperty = search["Category"];
    Assert.Equal("Products", categoryProperty.Value);

    // TryGetProperty for safe access
    if (search.TryGetProperty("SearchTerm", out var prop))
    {
        Assert.Equal("SearchTerm", prop.Name);
    }
}
```
<!-- endSnippet -->

### Validation and Rules

#### RunRules

```csharp
public virtual Task RunRules(string propertyName, CancellationToken? token = null)
public virtual Task RunRules(RunRulesFlag runRules = RunRulesFlag.All, CancellationToken? token = null)
```

Executes validation rules for specific properties or the entire object graph. `RunRulesFlag.All` clears all messages before running. Supports cancellation, but cancelled validation marks the object invalid until re-validated.

<!-- snippet: api-validatebase-runrules -->
```cs
[Fact]
public async Task RunRules_ExecutesValidation()
{
    var customer = new ApiCustomerValidator(new ValidateBaseServices<ApiCustomerValidator>());

    // Set invalid value
    customer.Name = "";

    // Run rules for specific property
    await customer.RunRules("Name");
    Assert.False(customer["Name"].IsValid);

    // Fix value and run all rules
    customer.Name = "Valid Name";
    await customer.RunRules(RunRulesFlag.All);

    Assert.True(customer.IsValid);
    Assert.Equal("Customer: Valid Name", customer.DisplayName);
}
```
<!-- endSnippet -->

#### RuleManager

```csharp
protected IRuleManager<T> RuleManager { get; }
```

Add validation rules and business rules in the constructor using the RuleManager.

<!-- snippet: api-validatebase-rulemanager -->
```cs
[Factory]
public partial class ApiCustomerValidator : ValidateBase<ApiCustomerValidator>
{
    public ApiCustomerValidator(IValidateBaseServices<ApiCustomerValidator> services) : base(services)
    {
        // Add validation rule via RuleManager
        RuleManager.AddValidation(
            customer => !string.IsNullOrEmpty(customer.Name) ? "" : "Name is required",
            c => c.Name);

        // Add action rule that computes derived value
        RuleManager.AddAction(
            customer => customer.DisplayName = $"Customer: {customer.Name}",
            c => c.Name);
    }

    public partial string Name { get; set; }

    public partial string DisplayName { get; set; }
}
```
<!-- endSnippet -->

#### MarkInvalid

```csharp
protected virtual void MarkInvalid(string message)
```

Permanently marks the object invalid with an object-level error message. The invalid state persists until `RunRules(RunRulesFlag.All)` is called.

<!-- snippet: api-validatebase-markinvalid -->
```cs
[Fact]
public void MarkInvalid_SetsObjectLevelError()
{
    var transaction = new ApiTransaction(new ValidateBaseServices<ApiTransaction>());
    transaction.TransactionId = "TXN-001";
    transaction.Amount = 100;

    Assert.True(transaction.IsValid);

    // Mark invalid due to external validation
    transaction.MarkTransactionInvalid("Payment gateway rejected");

    // Object is now invalid with object-level error
    Assert.False(transaction.IsValid);
    Assert.Equal("Payment gateway rejected", transaction.ObjectInvalid);

    // Error message appears in PropertyMessages
    Assert.Contains(transaction.PropertyMessages,
        m => m.Message.Contains("Payment gateway rejected"));
}
```
<!-- endSnippet -->

#### ObjectInvalid

```csharp
public string? ObjectInvalid { get; protected set; }
```

Object-level validation error message. Automatically checked by validation rules.

#### ClearAllMessages / ClearSelfMessages

```csharp
public virtual void ClearAllMessages()
public virtual void ClearSelfMessages()
```

Clear validation messages from the object graph. `ClearAllMessages` clears recursively; `ClearSelfMessages` clears only direct properties.

### Meta Properties

```csharp
public bool IsValid { get; }          // This object and all children valid
public bool IsSelfValid { get; }      // This object's properties valid (excluding children)
public bool IsBusy { get; }           // Async operations in progress
public IReadOnlyCollection<IPropertyMessage> PropertyMessages { get; }
```

Meta properties raise `PropertyChanged` notifications when values change. These properties reflect aggregated state from the object graph.

<!-- snippet: api-validatebase-metaproperties -->
```cs
[Fact]
public void MetaProperties_ReflectValidationState()
{
    var customer = new ApiCustomerValidator(new ValidateBaseServices<ApiCustomerValidator>());

    // Set invalid value
    customer.Name = "";

    // Check meta-properties
    Assert.False(customer.IsValid);         // Object invalid
    Assert.False(customer.IsSelfValid);     // Own properties invalid
    Assert.False(customer.IsBusy);          // No async operations
    Assert.NotEmpty(customer.PropertyMessages);  // Has error messages
}
```
<!-- endSnippet -->

### Parent-Child Relationships

```csharp
public IValidateBase? Parent { get; protected set; }
protected virtual void SetParent(IValidateBase? parent)
```

Parent is automatically set when an object is assigned to a property. Parent reference enables rule propagation and task coordination up the object graph.

<!-- snippet: api-validatebase-parent -->
```cs
[Fact]
public void Parent_EstablishesHierarchy()
{
    var address = new ApiAddress(new ValidateBaseServices<ApiAddress>());

    // Create child item
    var item = new ApiValidateItem(new ValidateBaseServices<ApiValidateItem>());
    item.Name = "Test Item";

    // Add to collection establishes parent
    address.Items.Add(item);

    // Item's parent is the address
    Assert.Same(address, item.Parent);
}
```
<!-- endSnippet -->

### Async Task Management

```csharp
public virtual Task WaitForTasks()
public virtual Task WaitForTasks(CancellationToken token)
public virtual void AddChildTask(Task task)
```

Wait for all async operations to complete before proceeding. Child tasks propagate up the hierarchy to the root.

<!-- snippet: api-validatebase-tasks -->
```cs
[Fact]
public async Task Tasks_WaitForAsyncOperations()
{
    var pricingService = new MockPricingService();
    var contact = new ApiAsyncContact(
        new ValidateBaseServices<ApiAsyncContact>(),
        pricingService);

    contact.Name = "Test";

    // Setting ZipCode triggers async rule
    contact.ZipCode = "90210";

    // Wait for all async operations
    await contact.WaitForTasks();

    // Async rule completed
    Assert.Equal(0.0825m, contact.TaxRate);
}
```
<!-- endSnippet -->

### Pause/Resume

```csharp
public bool IsPaused { get; }
public virtual IDisposable PauseAllActions()
public virtual void ResumeAllActions()
```

Pause property change events, rule execution, and notifications during batch updates. The returned `IDisposable` automatically resumes when disposed.

<!-- snippet: api-validatebase-pause -->
```cs
[Fact]
public void Pause_SuppressesEventsAndRules()
{
    var customer = new ApiCustomerValidator(new ValidateBaseServices<ApiCustomerValidator>());

    // Pause all actions during batch updates
    using (customer.PauseAllActions())
    {
        Assert.True(customer.IsPaused);

        // Assignments do not trigger rules
        customer.Name = "Batch Update";
    }

    // After resume, IsPaused is false
    Assert.False(customer.IsPaused);
    Assert.Equal("Batch Update", customer.Name);
}
```
<!-- endSnippet -->

### Events

```csharp
public event PropertyChangedEventHandler? PropertyChanged;
public event NeatooPropertyChanged? NeatooPropertyChanged;
```

`PropertyChanged` follows standard `INotifyPropertyChanged` for UI binding. `NeatooPropertyChanged` provides extended information for internal framework operations and supports async handlers.

### Factory Lifecycle Hooks

```csharp
public virtual void FactoryStart(FactoryOperation factoryOperation)
public virtual void FactoryComplete(FactoryOperation factoryOperation)
public virtual Task PostPortalConstruct()
```

Override these methods to add custom logic during factory operations (Create, Fetch, Insert, Update, Delete).

### Services

```csharp
protected IValidateBaseServices<T> Services { get; }
protected IValidatePropertyManager<IValidateProperty> PropertyManager { get; }
```

Access injected services and the property manager for advanced scenarios.

---

## EntityBase\<T\>

Extends `ValidateBase<T>` with entity-specific capabilities for persistence, modification tracking, and aggregate patterns.

### Constructor

```csharp
protected EntityBase(IEntityBaseServices<T> services)
```

Accepts entity services containing property manager, rule manager factory, and save factory.

### Persistence State

```csharp
public virtual bool IsNew { get; protected set; }
public virtual bool IsDeleted { get; protected set; }
public virtual bool IsChild { get; protected set; }
```

- **IsNew**: Entity has not been persisted (Insert operation on save)
- **IsDeleted**: Entity marked for deletion (Delete operation on save)
- **IsChild**: Entity is part of an aggregate and cannot be saved independently

<!-- snippet: api-entitybase-persistence-state -->
```cs
[Fact]
public void PersistenceState_TracksEntityLifecycle()
{
    var employee = new ApiEmployee(new EntityBaseServices<ApiEmployee>());

    // Initial state - not new, not deleted
    Assert.False(employee.IsNew);
    Assert.False(employee.IsDeleted);
    Assert.False(employee.IsChild);

    // After Create operation
    employee.FactoryComplete(FactoryOperation.Create);
    Assert.True(employee.IsNew);   // New entity - will Insert on save

    // After Insert operation
    employee.FactoryComplete(FactoryOperation.Insert);
    Assert.False(employee.IsNew);  // Now existing - will Update on save

    // Mark for deletion
    employee.Delete();
    Assert.True(employee.IsDeleted);  // Will Delete on save
}
```
<!-- endSnippet -->

### Modification Tracking

```csharp
public virtual bool IsModified { get; }
public virtual bool IsSelfModified { get; protected set; }
public virtual bool IsMarkedModified { get; protected set; }
public virtual IEnumerable<string> ModifiedProperties { get; }
```

- **IsModified**: This entity or any child has been modified
- **IsSelfModified**: This entity's direct properties have been modified
- **IsMarkedModified**: Entity explicitly marked modified via `MarkModified()`
- **ModifiedProperties**: Collection of property names that changed

<!-- snippet: api-entitybase-modification -->
```cs
[Fact]
public void ModificationTracking_DetectsChanges()
{
    var employee = new ApiEmployee(new EntityBaseServices<ApiEmployee>());

    // Simulate fetched entity
    using (employee.PauseAllActions())
    {
        employee.Id = 1;
        employee.Name = "Original";
    }
    employee.FactoryComplete(FactoryOperation.Fetch);

    Assert.False(employee.IsModified);
    Assert.False(employee.IsSelfModified);
    Assert.Empty(employee.ModifiedProperties);

    // Change property
    employee.Name = "Modified";

    Assert.True(employee.IsModified);
    Assert.True(employee.IsSelfModified);
    Assert.Contains("Name", employee.ModifiedProperties);
}
```
<!-- endSnippet -->

### Savability

```csharp
public virtual bool IsSavable { get; }
```

Entity can be saved when: `IsModified && IsValid && !IsBusy && !IsChild`

### Aggregate Root

```csharp
public IValidateBase? Root { get; }
```

Walks the Parent chain to find the aggregate root. Returns null if this entity is the root or standalone.

<!-- snippet: api-entitybase-root -->
```cs
[Fact]
public void Root_FindsAggregateRoot()
{
    var order = new ApiOrder(new EntityBaseServices<ApiOrder>());

    // Create child item
    var item = new ApiOrderItem(new EntityBaseServices<ApiOrderItem>());
    item.ProductCode = "WIDGET-001";
    item.Price = 29.99m;

    // Add to collection
    order.Items.Add(item);

    // Root walks Parent chain to find aggregate root
    Assert.Same(order, item.Root);

    // Aggregate root has no root above it
    Assert.Null(order.Root);
}
```
<!-- endSnippet -->

### Save Operations

```csharp
public virtual Task<IEntityBase> Save()
public virtual Task<IEntityBase> Save(CancellationToken token)
```

Persists the entity using the configured factory. Delegates to Insert (if IsNew), Delete (if IsDeleted), or Update based on state. Throws `SaveOperationException` if not savable.

<!-- snippet: api-entitybase-save -->
```cs
[Factory]
public partial class ApiEmployeeEntity : EntityBase<ApiEmployeeEntity>
{
    public ApiEmployeeEntity(IEntityBaseServices<ApiEmployeeEntity> services) : base(services) { }

    public partial int Id { get; set; }

    public partial string Name { get; set; }

    public partial decimal Salary { get; set; }

    // Expose protected methods for testing
    public void DoMarkNew() => MarkNew();
    public void DoMarkOld() => MarkOld();
    public void DoMarkUnmodified() => MarkUnmodified();

    [Create]
    public void Create()
    {
        Id = 0;
        Name = "";
        Salary = 0;
    }

    [Fetch]
    public async Task FetchAsync(int id, [Service] IApiCustomerRepository repository)
    {
        var data = await repository.FetchAsync(id);
        Id = data.Id;
        Name = data.Name;
    }

    [Insert]
    public async Task InsertAsync([Service] IApiCustomerRepository repository)
    {
        await repository.InsertAsync(Id, Name, "");
    }

    [Update]
    public async Task UpdateAsync([Service] IApiCustomerRepository repository)
    {
        await repository.UpdateAsync(Id, Name, "");
    }

    [Delete]
    public async Task DeleteAsync([Service] IApiCustomerRepository repository)
    {
        await repository.DeleteAsync(Id);
    }
}
```
<!-- endSnippet -->

### Delete Operations

```csharp
public void Delete()
public void UnDelete()
```

`Delete()` marks the entity for deletion. If the entity is in a list, delegates to the list's Remove method for consistency. `UnDelete()` reverses the deletion mark.

<!-- snippet: api-entitybase-delete -->
```cs
[Fact]
public void Delete_MarksForDeletion()
{
    var employee = new ApiEmployee(new EntityBaseServices<ApiEmployee>());

    // Simulate fetched entity
    using (employee.PauseAllActions())
    {
        employee.Id = 42;
        employee.Name = "To Delete";
    }
    employee.FactoryComplete(FactoryOperation.Fetch);

    Assert.False(employee.IsDeleted);

    // Mark for deletion
    employee.Delete();
    Assert.True(employee.IsDeleted);
    Assert.True(employee.IsModified);

    // UnDelete reverses the mark
    employee.UnDelete();
    Assert.False(employee.IsDeleted);
}
```
<!-- endSnippet -->

### State Management Methods

```csharp
protected virtual void MarkNew()
protected virtual void MarkOld()
protected virtual void MarkModified()
protected virtual void MarkUnmodified()
protected virtual void MarkDeleted()
protected virtual void MarkAsChild()
```

Control entity state programmatically. `MarkUnmodified()` is called automatically after successful Insert/Update operations.

<!-- snippet: api-entitybase-mark-methods -->
```cs
[Fact]
public void MarkMethods_ControlEntityState()
{
    var employee = new ApiEmployee(new EntityBaseServices<ApiEmployee>());

    // MarkNew - entity needs Insert
    employee.DoMarkNew();
    Assert.True(employee.IsNew);

    // MarkOld - entity exists, needs Update
    employee.DoMarkOld();
    Assert.False(employee.IsNew);

    // MarkModified - force entity to save
    employee.DoMarkModified();
    Assert.True(employee.IsModified);
    Assert.True(employee.IsMarkedModified);

    // MarkUnmodified - clear modification state
    employee.DoMarkUnmodified();
    Assert.False(employee.IsModified);

    // MarkDeleted - entity needs Delete
    employee.DoMarkDeleted();
    Assert.True(employee.IsDeleted);

    // MarkAsChild - entity is part of aggregate
    employee.DoMarkAsChild();
    Assert.True(employee.IsChild);
}
```
<!-- endSnippet -->

### Property Access

```csharp
new protected IEntityProperty GetProperty(string propertyName)
new public IEntityProperty this[string propertyName] { get; }
```

Entity properties extend validation properties with modification tracking.

### Factory

```csharp
public IFactorySave<T>? Factory { get; protected set; }
```

The save factory used to persist this entity, configured via RemoteFactory source generation.

---

## ValidateListBase\<T\>

Base class for collections of validatable objects. Aggregates validation state and coordinates tasks across all items.

### Constructor

```csharp
protected ValidateListBase()
```

Inherits from `ObservableCollection<I>` where `I : IValidateBase`.

### Parent Relationship

```csharp
public IValidateBase? Parent { get; protected set; }
```

The list's parent is set automatically when assigned to a property. Child items have the list's parent as their parent (the list is not the parent).

<!-- snippet: api-validatelistbase-parent -->
```cs
[Fact]
public void ValidateListBase_ParentRelationship()
{
    var address = new ApiAddress(new ValidateBaseServices<ApiAddress>());

    var item = new ApiValidateItem(new ValidateBaseServices<ApiValidateItem>());
    item.Name = "Test";

    // Add item to collection
    address.Items.Add(item);

    // Item's parent is the address (not the list)
    Assert.Same(address, item.Parent);

    // List's parent is also set
    Assert.Same(address, address.Items.Parent);
}
```
<!-- endSnippet -->

### Aggregated Meta Properties

```csharp
public bool IsValid { get; }         // All items valid
public bool IsSelfValid { get; }     // Always true (lists have no own validation)
public bool IsBusy { get; }          // Any item busy
public IReadOnlyCollection<IPropertyMessage> PropertyMessages { get; }
```

Meta properties aggregate state from all items in the collection using incremental caching for performance.

<!-- snippet: api-validatelistbase-metaproperties -->
```cs
[Fact]
public async Task ValidateListBase_AggregatesState()
{
    var list = new ApiValidateItemList();

    var validItem = new ApiValidateItem(new ValidateBaseServices<ApiValidateItem>());
    validItem.Name = "Valid";
    await validItem.RunRules();

    var invalidItem = new ApiValidateItem(new ValidateBaseServices<ApiValidateItem>());
    // Name is empty - invalid
    await invalidItem.RunRules();

    list.Add(validItem);
    Assert.True(list.IsValid);

    list.Add(invalidItem);
    Assert.False(list.IsValid);    // Aggregates child state
    Assert.True(list.IsSelfValid); // Lists have no own validation
}
```
<!-- endSnippet -->

### Validation Operations

```csharp
public async Task RunRules(string propertyName, CancellationToken? token = default)
public async Task RunRules(RunRulesFlag runRules = RunRulesFlag.All, CancellationToken? token = default)
public void ClearAllMessages()
public void ClearSelfMessages()
```

Run validation rules on all items in the collection.

<!-- snippet: api-validatelistbase-validation -->
```cs
[Fact]
public async Task ValidateListBase_RunRulesOnAll()
{
    var list = new ApiValidateItemList();

    var item1 = new ApiValidateItem(new ValidateBaseServices<ApiValidateItem>());
    item1.Name = "";  // Invalid

    var item2 = new ApiValidateItem(new ValidateBaseServices<ApiValidateItem>());
    item2.Name = "Valid";

    list.Add(item1);
    list.Add(item2);

    // Run rules on all items
    await list.RunRules(RunRulesFlag.All);

    Assert.False(item1.IsValid);
    Assert.True(item2.IsValid);
    Assert.False(list.IsValid);

    // Clear messages on all items
    list.ClearAllMessages();
    Assert.Empty(item1.PropertyMessages);
}
```
<!-- endSnippet -->

### Task Management

```csharp
public async Task WaitForTasks()
public async Task WaitForTasks(CancellationToken token)
```

Wait for all items to complete pending async operations.

### Pause/Resume

```csharp
public bool IsPaused { get; protected set; }
public virtual void ResumeAllActions()
```

Control rule execution and event notifications during batch operations.

### Events

```csharp
public event NeatooPropertyChanged? NeatooPropertyChanged;
public event PropertyChangedEventHandler? PropertyChanged;
public event NotifyCollectionChangedEventHandler? CollectionChanged;
```

Standard collection and property change notifications plus Neatoo-specific events.

### Collection Operations

Inherits standard `ObservableCollection<I>` methods: `Add`, `Remove`, `RemoveAt`, `Insert`, `Clear`, `this[int index]`.

When items are added/removed, the list automatically:
- Sets parent references on items
- Subscribes/unsubscribes to property change events
- Updates aggregated meta properties

<!-- snippet: api-validatelistbase-collection-ops -->
```cs
[Fact]
public void ValidateListBase_StandardOperations()
{
    var list = new ApiValidateItemList();

    // Add
    var item = new ApiValidateItem(new ValidateBaseServices<ApiValidateItem>());
    item.Name = "Item 1";
    list.Add(item);

    Assert.Single(list);
    Assert.Contains(item, list);

    // Indexer
    Assert.Same(item, list[0]);

    // Count
    Assert.Equal(1, list.Count);

    // Remove
    list.Remove(item);
    Assert.Empty(list);
}
```
<!-- endSnippet -->

---

## EntityListBase\<T\>

Extends `ValidateListBase<I>` with entity-specific features: deleted item tracking and modification state aggregation. Used for entity collections within aggregates.

### Constructor

```csharp
protected EntityListBase()
```

Where `I : IEntityBase`.

### Entity Meta Properties

```csharp
public bool IsModified { get; }         // Any item modified or deleted items exist
public bool IsSelfModified { get; }     // Always false (lists have no own properties)
public bool IsMarkedModified { get; }   // Always false
public bool IsSavable { get; }          // Always false (saved through parent)
public bool IsNew { get; }              // Always false
public bool IsDeleted { get; }          // Always false
public bool IsChild { get; }            // Always false
```

Lists derive their modification state from their items and deleted list.

<!-- snippet: api-entitylistbase-metaproperties -->
```cs
[Fact]
public void EntityListBase_ModificationFromItems()
{
    var order = new ApiOrder(new EntityBaseServices<ApiOrder>());

    var item = new ApiOrderItem(new EntityBaseServices<ApiOrderItem>());
    item.ProductCode = "TEST";
    item.DoMarkOld();
    item.DoMarkUnmodified();

    // Add during fetch
    order.Items.DoFactoryStart(FactoryOperation.Fetch);
    order.Items.Add(item);
    order.Items.DoFactoryComplete(FactoryOperation.Fetch);
    order.DoMarkUnmodified();

    Assert.False(order.Items.IsModified);

    // Modify item
    item.Price = 99.99m;

    // List reflects item modification
    Assert.True(order.Items.IsModified);
    Assert.False(order.Items.IsSelfModified); // Lists have no own properties
}
```
<!-- endSnippet -->

### Aggregate Root

```csharp
public IValidateBase? Root { get; }
```

Walks the Parent chain to find the aggregate root.

### Deleted Items

```csharp
protected List<I> DeletedList { get; }
```

Tracks removed items that need deletion during persistence. The deleted list is cleared after Update factory operation completes.

<!-- snippet: api-entitylistbase-deletedlist -->
```cs
[Fact]
public void EntityListBase_TracksDeleted()
{
    var order = new ApiOrder(new EntityBaseServices<ApiOrder>());

    var item = new ApiOrderItem(new EntityBaseServices<ApiOrderItem>());
    item.ProductCode = "DELETE-ME";
    item.DoMarkOld();
    item.DoMarkUnmodified();

    // Add during fetch
    order.Items.DoFactoryStart(FactoryOperation.Fetch);
    order.Items.Add(item);
    order.Items.DoFactoryComplete(FactoryOperation.Fetch);

    // Remove existing item
    order.Items.Remove(item);

    // Item is in DeletedList
    Assert.True(item.IsDeleted);
    Assert.Equal(1, order.Items.DeletedCount);

    // After Update, DeletedList is cleared
    order.Items.DoFactoryStart(FactoryOperation.Update);
    order.Items.DoFactoryComplete(FactoryOperation.Update);

    Assert.Equal(0, order.Items.DeletedCount);
}
```
<!-- endSnippet -->

### Collection Operations

When adding items:
- Undeletes previously deleted items if re-added
- Marks existing items as modified
- Marks items as child entities
- Sets ContainingList reference
- Prevents adding items from different aggregates
- Prevents adding busy items

When removing items:
- Marks non-new items as deleted
- Adds deleted items to DeletedList for persistence
- ContainingList stays set until after save completes

<!-- snippet: api-entitylistbase-add-remove -->
```cs
[Fact]
public void EntityListBase_AddRemoveBehavior()
{
    var order = new ApiOrder(new EntityBaseServices<ApiOrder>());

    // Add new item (simulating Create factory operation)
    var newItem = new ApiOrderItem(new EntityBaseServices<ApiOrderItem>());
    using (newItem.PauseAllActions())
    {
        newItem.ProductCode = "NEW-001";
    }
    newItem.FactoryComplete(FactoryOperation.Create);  // Marks as new
    order.Items.Add(newItem);

    // Item is marked as child and is new
    Assert.True(newItem.IsChild);
    Assert.True(newItem.IsNew);
    Assert.Same(order, newItem.Parent);

    // Remove new item - not tracked (was never persisted)
    order.Items.Remove(newItem);
    Assert.Equal(0, order.Items.DeletedCount);

    // Add existing item
    var existingItem = new ApiOrderItem(new EntityBaseServices<ApiOrderItem>());
    existingItem.ProductCode = "EXIST-001";
    existingItem.DoMarkOld();
    existingItem.DoMarkUnmodified();

    order.Items.DoFactoryStart(FactoryOperation.Fetch);
    order.Items.Add(existingItem);
    order.Items.DoFactoryComplete(FactoryOperation.Fetch);

    // Remove existing item - tracked for deletion
    order.Items.Remove(existingItem);
    Assert.Equal(1, order.Items.DeletedCount);
    Assert.True(existingItem.IsDeleted);
}
```
<!-- endSnippet -->

### Factory Lifecycle

```csharp
public override void FactoryComplete(FactoryOperation factoryOperation)
```

After Update operation, clears DeletedList and ContainingList references on deleted items.

---

## Key Interfaces

### IValidateBase

Core interface for all Neatoo objects with validation support.

```csharp
public interface IValidateBase : INeatooObject, INotifyPropertyChanged,
    INotifyNeatooPropertyChanged, IValidateMetaProperties
{
    IValidateBase? Parent { get; }
    bool IsPaused { get; }
    IValidateProperty GetProperty(string propertyName);
    IValidateProperty this[string propertyName] { get; }
    bool TryGetProperty(string propertyName, out IValidateProperty validateProperty);
    void AddChildTask(Task task);
}
```

<!-- snippet: api-interfaces-ivalidatebase -->
```cs
[Fact]
public void IValidateBase_CoreValidationInterface()
{
    IValidateBase customer = new ApiCustomer(new ValidateBaseServices<ApiCustomer>());

    // Core interface members
    Assert.Null(customer.Parent);
    Assert.False(customer.IsPaused);

    // Property access
    IValidateProperty property = customer.GetProperty("Name");
    Assert.NotNull(property);

    IValidateProperty indexedProperty = customer["Email"];
    Assert.NotNull(indexedProperty);

    // TryGetProperty
    Assert.True(customer.TryGetProperty("Name", out var nameProperty));
    Assert.NotNull(nameProperty);
}
```
<!-- endSnippet -->

### IEntityBase

Extends `IValidateBase` with entity persistence and modification tracking.

```csharp
public interface IEntityBase : IValidateBase, IEntityMetaProperties, IFactorySaveMeta
{
    IValidateBase? Root { get; }
    IEnumerable<string> ModifiedProperties { get; }
    void Delete();
    void UnDelete();
    Task<IEntityBase> Save();
    Task<IEntityBase> Save(CancellationToken token);
    new IEntityProperty this[string propertyName] { get; }
}
```

<!-- snippet: api-interfaces-ientitybase -->
```cs
[Fact]
public void IEntityBase_EntityInterface()
{
    IEntityBase employee = new ApiEmployee(new EntityBaseServices<ApiEmployee>());

    // IEntityBase adds persistence properties
    Assert.False(employee.IsNew);
    Assert.False(employee.IsDeleted);
    Assert.False(employee.IsChild);
    Assert.False(employee.IsModified);

    // Delete and UnDelete methods
    employee.Delete();
    Assert.True(employee.IsDeleted);

    employee.UnDelete();
    Assert.False(employee.IsDeleted);

    // ModifiedProperties collection
    Assert.Empty(employee.ModifiedProperties);

    // Root property
    Assert.Null(employee.Root);
}
```
<!-- endSnippet -->

### IValidateProperty

Interface for managed properties supporting validation, change notification, and async operations.

```csharp
public interface IValidateProperty : INotifyPropertyChanged, INotifyNeatooPropertyChanged
{
    string Name { get; }
    object? Value { get; set; }
    Task SetValue(object? newValue);
    void LoadValue(object? value);
    Type Type { get; }
    bool IsBusy { get; }
    bool IsReadOnly { get; }
    bool IsValid { get; }
    bool IsSelfValid { get; }
    IReadOnlyCollection<IPropertyMessage> PropertyMessages { get; }
    Task RunRules(RunRulesFlag runRules = RunRulesFlag.All, CancellationToken? token = null);
    Task WaitForTasks();
}
```

<!-- snippet: api-interfaces-ivalidateproperty -->
```cs
[Fact]
public async Task IValidateProperty_PropertyInterface()
{
    var customer = new ApiCustomer(new ValidateBaseServices<ApiCustomer>());
    customer.Name = "Test";

    IValidateProperty property = customer["Name"];

    // Core property members
    Assert.Equal("Name", property.Name);
    Assert.Equal("Test", property.Value);
    Assert.Equal(typeof(string), property.Type);

    // State properties
    Assert.False(property.IsBusy);
    Assert.False(property.IsReadOnly);
    Assert.True(property.IsValid);
    Assert.True(property.IsSelfValid);
    Assert.Empty(property.PropertyMessages);

    // SetValue for async assignment
    await property.SetValue("Updated");
    Assert.Equal("Updated", property.Value);

    // LoadValue for data loading
    property.LoadValue("Loaded");
    Assert.Equal("Loaded", property.Value);

    // RunRules for property
    await property.RunRules();
    await property.WaitForTasks();
}
```
<!-- endSnippet -->

### IEntityProperty

Extends `IValidateProperty` with modification tracking.

```csharp
public interface IEntityProperty : IValidateProperty, IEntityPropertyModificationTracking
{
    bool IsModified { get; }
}
```

### IPropertyInfo

Metadata about a property on a Neatoo object.

```csharp
public interface IPropertyInfo
{
    PropertyInfo PropertyInfo { get; }
    string Name { get; }
    Type Type { get; }
    string Key { get; }
    bool IsPrivateSetter { get; }
    T? GetCustomAttribute<T>() where T : Attribute;
    IEnumerable<Attribute> GetCustomAttributes();
}
```

<!-- snippet: api-interfaces-ipropertyinfo -->
```cs
[Fact]
public void IPropertyInfo_PropertyMetadata()
{
    var customer = new ApiCustomer(new ValidateBaseServices<ApiCustomer>());

    // Access property metadata through IValidateProperty
    var property = customer["Name"];

    // IPropertyInfo provides metadata about the property
    Assert.Equal("Name", property.Name);
    Assert.Equal(typeof(string), property.Type);
}
```
<!-- endSnippet -->

### IValidateMetaProperties

Meta property interface for validation state.

```csharp
public interface IValidateMetaProperties
{
    bool IsValid { get; }
    bool IsSelfValid { get; }
    bool IsBusy { get; }
    IReadOnlyCollection<IPropertyMessage> PropertyMessages { get; }
}
```

### IEntityMetaProperties

Meta property interface for entity state.

```csharp
public interface IEntityMetaProperties : IValidateMetaProperties
{
    bool IsModified { get; }
    bool IsSelfModified { get; }
    bool IsMarkedModified { get; }
    bool IsSavable { get; }
    bool IsNew { get; }
    bool IsDeleted { get; }
    bool IsChild { get; }
}
```

<!-- snippet: api-interfaces-imetaproperties -->
```cs
[Fact]
public void IMetaProperties_ValidationAndEntityState()
{
    // IValidateMetaProperties - validation state
    IValidateMetaProperties validateMeta = new ApiCustomer(new ValidateBaseServices<ApiCustomer>());
    Assert.True(validateMeta.IsValid);
    Assert.True(validateMeta.IsSelfValid);
    Assert.False(validateMeta.IsBusy);
    Assert.Empty(validateMeta.PropertyMessages);

    // IEntityMetaProperties - adds entity state
    IEntityMetaProperties entityMeta = new ApiEmployee(new EntityBaseServices<ApiEmployee>());
    Assert.False(entityMeta.IsNew);
    Assert.False(entityMeta.IsDeleted);
    Assert.False(entityMeta.IsChild);
    Assert.False(entityMeta.IsModified);
    Assert.False(entityMeta.IsSelfModified);
    Assert.False(entityMeta.IsMarkedModified);
    Assert.False(entityMeta.IsSavable);
}
```
<!-- endSnippet -->

---

## Attributes

### RemoteFactory Attributes

Neatoo uses RemoteFactory attributes for source-generated factory methods.

#### [Factory]

```csharp
[AttributeUsage(AttributeTargets.Class)]
public class FactoryAttribute : Attribute
```

Marks a class for factory method generation. Apply to `ValidateBase<T>`, `EntityBase<T>`, or list classes.

<!-- snippet: api-attributes-factory -->
```cs
[Factory]
public partial class ApiProduct : ValidateBase<ApiProduct>
{
    public ApiProduct(IValidateBaseServices<ApiProduct> services) : base(services) { }

    public partial string Name { get; set; }

    public partial decimal Price { get; set; }
}
```
<!-- endSnippet -->

#### [Create]

```csharp
[AttributeUsage(AttributeTargets.Method)]
public class CreateAttribute : Attribute
```

Marks a method as a Create factory operation. The method becomes a static factory method that returns the object in IsNew state.

<!-- snippet: api-attributes-create -->
```cs
[Factory]
public partial class ApiInvoice : EntityBase<ApiInvoice>
{
    public ApiInvoice(IEntityBaseServices<ApiInvoice> services) : base(services) { }

    public partial int Id { get; set; }

    public partial string InvoiceNumber { get; set; }

    public partial decimal Amount { get; set; }

    [Create]
    public void Create()
    {
        Id = 0;
        InvoiceNumber = $"INV-{DateTime.Now:yyyyMMdd}";
        Amount = 0;
    }
}
```
<!-- endSnippet -->

#### [Fetch]

```csharp
[AttributeUsage(AttributeTargets.Method)]
public class FetchAttribute : Attribute
```

Marks a method as a Fetch factory operation. The method becomes a static factory method that returns an existing object from persistence.

<!-- snippet: api-attributes-fetch -->
```cs
[Factory]
public partial class ApiContact : EntityBase<ApiContact>
{
    public ApiContact(IEntityBaseServices<ApiContact> services) : base(services) { }

    public partial int Id { get; set; }

    public partial string Name { get; set; }

    public partial string Email { get; set; }

    [Fetch]
    public async Task FetchAsync(int id, [Service] IApiCustomerRepository repository)
    {
        var data = await repository.FetchAsync(id);
        Id = data.Id;
        Name = data.Name;
        Email = data.Email;
    }
}
```
<!-- endSnippet -->

#### [Insert]

```csharp
[AttributeUsage(AttributeTargets.Method)]
public class InsertAttribute : Attribute
```

Marks a method as an Insert factory operation. Called by `Save()` when `IsNew` is true.

<!-- snippet: api-attributes-insert -->
```cs
[Factory]
public partial class ApiAccount : EntityBase<ApiAccount>
{
    public ApiAccount(IEntityBaseServices<ApiAccount> services) : base(services) { }

    public partial int Id { get; set; }

    public partial string AccountName { get; set; }

    [Create]
    public void Create()
    {
        Id = 0;
        AccountName = "";
    }

    [Insert]
    public async Task InsertAsync([Service] IApiCustomerRepository repository)
    {
        await repository.InsertAsync(Id, AccountName, "");
    }
}
```
<!-- endSnippet -->

#### [Update]

```csharp
[AttributeUsage(AttributeTargets.Method)]
public class UpdateAttribute : Attribute
```

Marks a method as an Update factory operation. Called by `Save()` when entity is modified but not new or deleted.

<!-- snippet: api-attributes-update -->
```cs
[Factory]
public partial class ApiLead : EntityBase<ApiLead>
{
    public ApiLead(IEntityBaseServices<ApiLead> services) : base(services) { }

    public partial int Id { get; set; }

    public partial string LeadName { get; set; }

    public void DoMarkOld() => MarkOld();

    [Update]
    public async Task UpdateAsync([Service] IApiCustomerRepository repository)
    {
        await repository.UpdateAsync(Id, LeadName, "");
    }
}
```
<!-- endSnippet -->

#### [Delete]

```csharp
[AttributeUsage(AttributeTargets.Method)]
public class DeleteAttribute : Attribute
```

Marks a method as a Delete factory operation. Called by `Save()` when `IsDeleted` is true.

<!-- snippet: api-attributes-delete -->
```cs
[Factory]
public partial class ApiProject : EntityBase<ApiProject>
{
    public ApiProject(IEntityBaseServices<ApiProject> services) : base(services) { }

    public partial int Id { get; set; }

    public partial string ProjectName { get; set; }

    [Delete]
    public async Task DeleteAsync([Service] IApiCustomerRepository repository)
    {
        await repository.DeleteAsync(Id);
    }
}
```
<!-- endSnippet -->

#### [Service]

```csharp
[AttributeUsage(AttributeTargets.Parameter)]
public class ServiceAttribute : Attribute
```

Marks a factory method parameter for dependency injection. Services are resolved from the DI container at factory invocation time.

<!-- snippet: api-attributes-service -->
```cs
[Factory]
public partial class ApiReport : EntityBase<ApiReport>
{
    public ApiReport(IEntityBaseServices<ApiReport> services) : base(services) { }

    public partial int Id { get; set; }

    public partial string ReportName { get; set; }

    // [Service] marks parameters for DI resolution
    [Fetch]
    public async Task FetchAsync(int id, [Service] IApiCustomerRepository repository)
    {
        var data = await repository.FetchAsync(id);
        Id = data.Id;
        ReportName = data.Name;
    }
}
```
<!-- endSnippet -->

#### [SuppressFactory]

```csharp
[AttributeUsage(AttributeTargets.Class)]
public class SuppressFactoryAttribute : Attribute
```

Suppresses factory method generation for a class. Used for test classes that inherit from Neatoo base classes but don't need factory methods.

<!-- snippet: api-attributes-suppressfactory -->
```cs
[SuppressFactory]
public class ApiTestObject : ValidateBase<ApiTestObject>
{
    public ApiTestObject(IValidateBaseServices<ApiTestObject> services) : base(services) { }

    public string Name { get => Getter<string>(); set => Setter(value); }
}
```
<!-- endSnippet -->

### Validation Attributes

Neatoo supports standard `System.ComponentModel.DataAnnotations` attributes for property validation:

- **[Required]**: Property value is required
- **[MaxLength(n)]**: String maximum length
- **[MinLength(n)]**: String minimum length
- **[StringLength(max, MinimumLength = min)]**: String length range
- **[Range(min, max)]**: Numeric value range
- **[EmailAddress]**: Valid email format
- **[RegularExpression(pattern)]**: Matches regex pattern

<!-- snippet: api-attributes-validation -->
```cs
[Factory]
public partial class ApiRegistration : ValidateBase<ApiRegistration>
{
    public ApiRegistration(IValidateBaseServices<ApiRegistration> services) : base(services) { }

    [Required]
    public partial string Username { get; set; }

    [EmailAddress]
    public partial string Email { get; set; }

    [StringLength(100, MinimumLength = 8)]
    public partial string Password { get; set; }

    [Range(18, 120)]
    public partial int Age { get; set; }

    [RegularExpression(@"^\d{5}(-\d{4})?$")]
    public partial string ZipCode { get; set; }
}
```
<!-- endSnippet -->

Validation attributes are automatically converted to validation rules during object construction.

---

## Source Generator Output

Neatoo includes two source generators: BaseGenerator (partial properties) and RemoteFactory Generator (factory methods).

### Partial Property Generation

When you declare a partial property, the BaseGenerator creates:

1. **Backing field** of type `IValidateProperty<T>` or `IEntityProperty<T>`
2. **Property getter** that calls the backing field's Value
3. **Property setter** that calls the backing field's SetValue
4. **InitializePropertyBackingFields override** that creates the property instance

<!-- snippet: api-generator-partial-property -->
```cs
[Factory]
public partial class ApiGeneratedCustomer : ValidateBase<ApiGeneratedCustomer>
{
    public ApiGeneratedCustomer(IValidateBaseServices<ApiGeneratedCustomer> services) : base(services) { }

    // Source generator creates:
    // - private IValidateProperty<string> _NameProperty;
    // - getter: return _NameProperty.Value;
    // - setter: _NameProperty.SetValue(value);
    public partial string Name { get; set; }

    public partial string Email { get; set; }
}
```
<!-- endSnippet -->

The source-generated code for a partial property looks like:

```csharp
private IValidateProperty<string> _NameProperty = null!;

public partial string Name
{
    get => _NameProperty.Value;
    set => _NameProperty.SetValue(value);
}

protected override void InitializePropertyBackingFields(IPropertyFactory<Customer> factory)
{
    base.InitializePropertyBackingFields(factory);
    _NameProperty = factory.CreateValidateProperty<string>(nameof(Name));
}
```

### Factory Method Generation

RemoteFactory generates static factory methods from instance methods marked with factory attributes.

<!-- snippet: api-generator-factory-methods -->
```cs
[Factory]
public partial class ApiGeneratedEntity : EntityBase<ApiGeneratedEntity>
{
    public ApiGeneratedEntity(IEntityBaseServices<ApiGeneratedEntity> services) : base(services) { }

    public partial int Id { get; set; }

    public partial string Name { get; set; }

    // Source generator creates static factory methods from instance methods
    [Create]
    public void Create()
    {
        Id = 0;
        Name = "";
    }

    [Fetch]
    public async Task FetchAsync(int id, [Service] IApiCustomerRepository repository)
    {
        var data = await repository.FetchAsync(id);
        Id = data.Id;
        Name = data.Name;
    }
}
```
<!-- endSnippet -->

For a Fetch method:

```csharp
[Fetch]
public async Task FetchAsync(int id, [Service] IRepository repo)
{
    var data = await repo.GetById(id);
    Name = data.Name;
}
```

The generator creates:

```csharp
public static async Task<Customer> FetchAsync(int id, IServiceProvider services)
{
    var instance = services.GetRequiredService<Customer>();
    instance.FactoryStart(FactoryOperation.Fetch);
    var repo = services.GetRequiredService<IRepository>();
    await instance.FetchAsync(id, repo);
    instance.FactoryComplete(FactoryOperation.Fetch);
    return instance;
}
```

### Save Factory Generation

For Insert/Update/Delete methods with no non-service parameters, RemoteFactory generates a save factory that `Save()` uses:

<!-- snippet: api-generator-save-factory -->
```cs
[Factory]
public partial class ApiGeneratedSaveEntity : EntityBase<ApiGeneratedSaveEntity>
{
    public ApiGeneratedSaveEntity(IEntityBaseServices<ApiGeneratedSaveEntity> services) : base(services) { }

    public partial int Id { get; set; }

    public partial string Name { get; set; }

    public void DoMarkNew() => MarkNew();
    public void DoMarkOld() => MarkOld();

    [Create]
    public void Create()
    {
        Id = 0;
        Name = "";
    }

    // Insert, Update, Delete with no non-service parameters
    // generates IFactorySave<T> implementation
    [Insert]
    public async Task InsertAsync([Service] IApiCustomerRepository repository)
    {
        await repository.InsertAsync(Id, Name, "");
    }

    [Update]
    public async Task UpdateAsync([Service] IApiCustomerRepository repository)
    {
        await repository.UpdateAsync(Id, Name, "");
    }

    [Delete]
    public async Task DeleteAsync([Service] IApiCustomerRepository repository)
    {
        await repository.DeleteAsync(Id);
    }
}
```
<!-- endSnippet -->

```csharp
[Insert]
public async Task InsertAsync([Service] IRepository repo)
{
    await repo.Insert(this);
}

[Update]
public async Task UpdateAsync([Service] IRepository repo)
{
    await repo.Update(this);
}

[Delete]
public async Task DeleteAsync([Service] IRepository repo)
{
    await repo.Delete(this);
}
```

The generator creates an `IFactorySave<Customer>` implementation that delegates to the appropriate method based on entity state.

### RuleIdRegistry Generation

For stable rule IDs across compilations, the BaseGenerator creates a RuleIdRegistry with compile-time constants for each lambda expression used in AddRule calls.

<!-- snippet: api-generator-ruleid -->
```cs
[Factory]
public partial class ApiRuleIdEntity : ValidateBase<ApiRuleIdEntity>
{
    public ApiRuleIdEntity(IValidateBaseServices<ApiRuleIdEntity> services) : base(services)
    {
        // Lambda expressions in AddRule generate stable RuleId entries
        // in RuleIdRegistry for consistent rule identification
        RuleManager.AddValidation(
            entity => entity.Value > 0 ? "" : "Value must be positive",
            e => e.Value);
    }

    public partial int Value { get; set; }
}
```
<!-- endSnippet -->

This registry enables rule suppression and rule-specific behavior without relying on runtime hash codes.

---

**UPDATED:** 2026-01-24
