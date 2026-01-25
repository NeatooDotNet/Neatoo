# Remote Factory

[← Properties](properties.md) | [↑ Guides](index.md) | [Validation →](validation.md)

RemoteFactory is Neatoo's client-server state transfer system that generates factory methods for entity lifecycle operations (Create, Fetch, Save, Delete). The source generator analyzes factory method attributes and creates strongly-typed factory interfaces with automatic dependency injection, local/remote routing, and JSON serialization for transferring domain model state between client and server.

## Factory Method Attributes

Factory methods define entity lifecycle operations. The RemoteFactory source generator discovers methods marked with [Create], [Fetch], [Insert], [Update], and [Delete] attributes and generates factory classes with dependency injection support.

Declare factory methods on an EntityBase class:

<!-- snippet: remotefactory-factory-methods -->
```cs
[Factory]
public partial class RfCustomer : EntityBase<RfCustomer>
{
    public RfCustomer(IEntityBaseServices<RfCustomer> services) : base(services) { }

    public partial int Id { get; set; }
    public partial string Name { get; set; }
    public partial string Email { get; set; }

    [Create]
    public void Create()
    {
        Id = 0;
        Name = "";
        Email = "";
    }

    [Fetch]
    public async Task FetchById(int id, [Service] IRfCustomerRepository repository)
    {
        var data = await repository.FetchByIdAsync(id);
        if (data != null)
        {
            Id = data.Id;
            Name = data.Name;
            Email = data.Email;
        }
    }

    [Insert]
    public async Task InsertAsync([Service] IRfCustomerRepository repository)
    {
        await repository.InsertAsync(Id, Name, Email);
    }

    [Update]
    public async Task UpdateAsync([Service] IRfCustomerRepository repository)
    {
        await repository.UpdateAsync(Id, Name, Email);
    }

    [Delete]
    public async Task DeleteAsync([Service] IRfCustomerRepository repository)
    {
        await repository.DeleteAsync(Id);
    }
}
```
<!-- endSnippet -->

Factory method patterns:
- **[Factory]** attribute on the class enables source generation
- **[Create]** initializes a new entity instance
- **[Fetch]** loads entity state from persistence
- **[Insert]** persists a new entity
- **[Update]** persists changes to an existing entity
- **[Delete]** removes an entity from persistence
- **[Service]** parameter attribute injects dependencies from IServiceProvider

The source generator creates a factory interface (ICustomerFactory) and implementation (CustomerFactory) with methods matching the declared factory methods.

## Generated Factory Interface

RemoteFactory generates a public interface exposing the factory methods declared on the entity class. This interface becomes the primary API for creating, fetching, and saving entities.

Generated factory interface:

<!-- snippet: remotefactory-generated-interface -->
```cs
[Fact]
public void GeneratedInterface_ExposesFactoryMethods()
{
    // The source generator creates a public interface:
    // public interface IRfCustomerFactory
    // {
    //     RfCustomer Create(CancellationToken cancellationToken = default);
    //     Task<RfCustomer> FetchById(int id, CancellationToken cancellationToken = default);
    //     Task<RfCustomer?> SaveAsync(RfCustomer target, CancellationToken cancellationToken = default);
    // }

    // Verify the interface type exists
    var interfaceType = typeof(IRfCustomerFactory);
    Assert.NotNull(interfaceType);
    Assert.True(interfaceType.IsInterface);

    // Verify expected methods exist
    var createMethod = interfaceType.GetMethod("Create");
    Assert.NotNull(createMethod);

    var fetchMethod = interfaceType.GetMethod("FetchById");
    Assert.NotNull(fetchMethod);

    var saveMethod = interfaceType.GetMethod("SaveAsync");
    Assert.NotNull(saveMethod);
}
```
<!-- endSnippet -->

Interface characteristics:
- Public interface named I{ClassName}Factory
- Contains methods for Create, Fetch, Save, and Delete operations
- Save method unifies Insert/Update logic based on IsNew
- Factory methods preserve parameter names and types
- CancellationToken support for async operations
- Registered in DI container for injection into application code

Consumers inject ICustomerFactory to work with Customer entities without directly calling lifecycle methods.

## Factory Method Generation

The source generator creates both the factory interface and a concrete implementation class. The implementation handles local vs remote execution, dependency injection, state management, and task coordination.

Generated factory implementation:

<!-- snippet: remotefactory-generated-implementation -->
```cs
[Fact]
public void GeneratedImplementation_HandlesLifecycle()
{
    // The source generator creates an internal implementation:
    // internal class RfCustomerFactory : FactorySaveBase<RfCustomer>,
    //     IFactorySave<RfCustomer>, IRfCustomerFactory
    // {
    //     public virtual RfCustomer Create(CancellationToken cancellationToken = default)
    //     {
    //         return LocalCreate(cancellationToken);
    //     }
    //
    //     public RfCustomer LocalCreate(CancellationToken cancellationToken = default)
    //     {
    //         var target = ServiceProvider.GetRequiredService<RfCustomer>();
    //         return DoFactoryMethodCall(target, FactoryOperation.Create,
    //             () => target.Create());
    //     }
    // }

    // Create entity directly for demonstration
    var customer = new RfCustomer(new EntityBaseServices<RfCustomer>());

    // Factory coordinates lifecycle - FactoryComplete sets state
    customer.FactoryComplete(FactoryOperation.Create);

    Assert.True(customer.IsNew);
}
```
<!-- endSnippet -->

Implementation details:
- Inherits from FactorySaveBase<T> for save coordination
- Constructor receives IServiceProvider for service resolution
- Delegate properties (FetchProperty, SaveProperty) enable local/remote routing
- DoFactoryMethodCallAsync wraps method execution with state management
- FactoryStart/FactoryComplete manage entity state transitions (IsNew, IsModified)
- PauseAllActions suspends validation during data loading

The factory implementation is an internal class registered in the DI container and provided through the public interface.

## Service Parameter Injection

Factory methods can declare dependencies using the [Service] attribute. The factory implementation resolves these services from IServiceProvider at runtime.

Inject repository dependencies:

<!-- snippet: remotefactory-service-injection -->
```cs
[Factory]
public partial class RfCustomerWithServices : EntityBase<RfCustomerWithServices>
{
    public RfCustomerWithServices(IEntityBaseServices<RfCustomerWithServices> services) : base(services) { }

    public partial int Id { get; set; }
    public partial string Name { get; set; }
    public partial string Email { get; set; }

    // [Service] parameters are resolved from DI container at runtime
    [Fetch]
    public async Task FetchAsync(
        int id,
        [Service] IRfCustomerRepository repository)
    {
        var data = await repository.FetchByIdAsync(id);
        if (data != null)
        {
            Id = data.Id;
            Name = data.Name;
            Email = data.Email;
        }
    }
}
```
<!-- endSnippet -->

Service injection patterns:
- **[Service]** parameters are resolved from DI container
- Services can be interfaces or concrete types
- Multiple services can be injected into a single method
- Service resolution happens during factory method execution
- Scoped services follow the factory's lifetime (typically scoped per request)
- Missing services throw InvalidOperationException

Service injection enables separation between domain logic (entity methods) and infrastructure (repositories, DbContext).

## Fetch: Loading State from Persistence

Fetch methods load entity state from persistence. The factory wraps the Fetch call with PauseAllActions to prevent validation rule execution during data loading.

Implement a Fetch method:

<!-- snippet: remotefactory-fetch -->
```cs
[Factory]
public partial class RfCustomerFetch : EntityBase<RfCustomerFetch>
{
    public RfCustomerFetch(IEntityBaseServices<RfCustomerFetch> services) : base(services) { }

    public partial int Id { get; set; }
    public partial string Name { get; set; }
    public partial string Email { get; set; }

    // Fetch loads entity state from persistence
    // Factory wraps this with PauseAllActions to prevent validation during load
    [Fetch]
    public async Task FetchAsync(int id, [Service] IRfCustomerRepository repository)
    {
        var data = await repository.FetchByIdAsync(id);
        if (data != null)
        {
            // Property assignments during Fetch use LoadValue internally
            Id = data.Id;
            Name = data.Name;
            Email = data.Email;
        }
        // After Fetch: IsNew = false, IsModified = false
    }
}
```
<!-- endSnippet -->

Fetch behavior:
- FactoryStart called before Fetch method executes
- PauseAllActions suspends validation, property change events, and dirty tracking
- Property assignments use LoadValue internally (ChangeReason.Load)
- Child entities can be fetched recursively through child factories
- Parent-child relationships are established during loading
- FactoryComplete called after Fetch completes
- IsNew = false, IsModified = false after Fetch completes

LoadValue vs direct assignment during Fetch:
- Direct assignment within PauseAllActions defers all event handling
- After Resume, PropertyChanged fires but validation does not execute
- Parent-child structure is established correctly
- Entity state reflects loaded data without modification tracking

## Save: Persisting Entity Changes

Save methods unify Insert/Update logic. The generated factory determines whether to call Insert or Update based on IsNew, then persists changes through the entity's factory method.

Implement Insert and Update methods:

<!-- snippet: remotefactory-save -->
```cs
[Factory]
public partial class RfCustomerSave : EntityBase<RfCustomerSave>
{
    public RfCustomerSave(IEntityBaseServices<RfCustomerSave> services) : base(services) { }

    public partial int Id { get; set; }
    public partial string Name { get; set; }
    public partial string Email { get; set; }

    [Create]
    public void Create()
    {
        Id = 0;
        Name = "";
        Email = "";
    }

    // Insert: Called when IsNew == true
    [Insert]
    public async Task InsertAsync([Service] IRfCustomerRepository repository)
    {
        // Persist new entity
        await repository.InsertAsync(Id, Name, Email);
        // After Insert: IsNew = false, IsModified = false
    }

    // Update: Called when IsNew == false and IsModified == true
    [Update]
    public async Task UpdateAsync([Service] IRfCustomerRepository repository)
    {
        // Persist changes to existing entity
        await repository.UpdateAsync(Id, Name, Email);
        // After Update: IsModified = false
    }
}
```
<!-- endSnippet -->

Save coordination:
1. Application calls ICustomerFactory.Save(customer)
2. Factory checks customer.IsNew
3. If IsNew == true, factory calls Insert method
4. If IsNew == false and IsModified == true, factory calls Update method
5. FactoryStart called before Insert/Update
6. Entity method executes (map to EF entity, call DbContext.SaveChanges)
7. FactoryComplete called after Insert/Update
8. IsModified = false, IsNew = false (after Insert)

Save methods should validate before persisting:

<!-- snippet: remotefactory-save-validation -->
```cs
[Factory]
public partial class RfCustomerValidated : EntityBase<RfCustomerValidated>
{
    public RfCustomerValidated(IEntityBaseServices<RfCustomerValidated> services) : base(services) { }

    public partial int Id { get; set; }
    public partial string Name { get; set; }
    public partial string Email { get; set; }

    [Create]
    public void Create()
    {
        Id = 0;
        Name = "";
        Email = "";
    }

    [Insert]
    public async Task InsertAsync([Service] IRfCustomerRepository repository)
    {
        // Check IsSavable before persisting
        // IsSavable verifies: IsValid && !IsBusy && IsModified && !IsChild
        if (!IsSavable)
        {
            // Validation failed - do not persist
            // Factory will still complete lifecycle, but no data persisted
            return;
        }

        await repository.InsertAsync(Id, Name, Email);
    }

    [Update]
    public async Task UpdateAsync([Service] IRfCustomerRepository repository)
    {
        if (!IsSavable)
        {
            return;
        }

        await repository.UpdateAsync(Id, Name, Email);
    }
}
```
<!-- endSnippet -->

IsSavable checks IsValid and !IsBusy. Returning null from Insert/Update signals validation failure to the caller.

## Delete: Removing Entities

Delete methods remove entities from persistence. The factory marks the entity as deleted before calling the Delete method.

Implement a Delete method:

<!-- snippet: remotefactory-delete -->
```cs
[Factory]
public partial class RfCustomerDelete : EntityBase<RfCustomerDelete>
{
    public RfCustomerDelete(IEntityBaseServices<RfCustomerDelete> services) : base(services) { }

    public partial int Id { get; set; }
    public partial string Name { get; set; }

    [Fetch]
    public async Task FetchAsync(int id, [Service] IRfCustomerRepository repository)
    {
        var data = await repository.FetchByIdAsync(id);
        if (data != null)
        {
            Id = data.Id;
            Name = data.Name;
        }
    }

    // Delete: Called when IsDeleted == true during Save
    [Delete]
    public async Task DeleteAsync([Service] IRfCustomerRepository repository)
    {
        // Remove entity from persistence
        await repository.DeleteAsync(Id);
        // Entity cannot be modified or saved after delete completes
    }
}
```
<!-- endSnippet -->

Delete behavior:
- Application calls entity.MarkForDeletion() before Save
- Factory detects IsDeleted == true during Save
- Factory calls Delete method instead of Insert/Update
- Delete method removes the entity from persistence
- FactoryComplete marks entity as deleted permanently
- Deleted entities cannot be modified or saved again

Delete pattern for aggregate roots:
- Call MarkForDeletion() on the root
- Call Save on the root
- Factory routes to Delete method
- Cascade delete to child entities if needed

## Remote vs Local Execution

RemoteFactory supports both local (in-process) and remote (client-server) execution. The [Remote] attribute marks methods that should execute on the server when running in a distributed architecture.

Mark factory methods for remote execution:

<!-- snippet: remotefactory-remote-attribute -->
```cs
[Factory]
public partial class RfCustomerRemote : EntityBase<RfCustomerRemote>
{
    public RfCustomerRemote(IEntityBaseServices<RfCustomerRemote> services) : base(services) { }

    public partial int Id { get; set; }
    public partial string Name { get; set; }
    public partial string Email { get; set; }

    // [Create] without [Remote] - executes locally on client
    [Create]
    public void Create()
    {
        Id = 0;
        Name = "";
        Email = "";
    }

    // [Remote] marks methods for server execution in distributed architecture
    // In NeatooFactory.Remote mode, this executes on server via HTTP
    [Remote]
    [Fetch]
    public async Task FetchAsync(int id, [Service] IRfCustomerRepository repository)
    {
        var data = await repository.FetchByIdAsync(id);
        if (data != null)
        {
            Id = data.Id;
            Name = data.Name;
            Email = data.Email;
        }
    }

    [Remote]
    [Insert]
    public async Task InsertAsync([Service] IRfCustomerRepository repository)
    {
        await repository.InsertAsync(Id, Name, Email);
    }

    [Remote]
    [Update]
    public async Task UpdateAsync([Service] IRfCustomerRepository repository)
    {
        await repository.UpdateAsync(Id, Name, Email);
    }

    [Remote]
    [Delete]
    public async Task DeleteAsync([Service] IRfCustomerRepository repository)
    {
        await repository.DeleteAsync(Id);
    }
}
```
<!-- endSnippet -->

Local vs remote routing:
- **NeatooFactory.Logical**: All factory methods execute locally (same process)
- **NeatooFactory.Remote**: Methods marked [Remote] execute on server via HTTP
- **NeatooFactory.Server**: Server-side configuration, all methods execute locally
- Factory delegate properties (FetchProperty, SaveProperty) point to local or remote implementation
- Remote execution serializes entity state as JSON, sends to server, deserializes response

Remote execution flow:
1. Client calls ICustomerFactory.Fetch(id)
2. FetchProperty points to RemoteFetch delegate
3. RemoteFetch serializes parameters and entity state
4. HTTP request sent to server endpoint
5. Server deserializes request, resolves factory, calls LocalFetch
6. Server serializes result entity
7. Client deserializes response and returns entity

The remote pattern enables Blazor WebAssembly clients to execute server-side persistence logic.

## Client-Server Serialization

Entities transfer between client and server as JSON. RemoteFactory includes custom JSON converters for ValidateBase and EntityBase types that preserve entity state (IsDirty, IsValid, validation messages) across serialization.

JSON serialization preserves entity state:

<!-- snippet: remotefactory-serialization -->
```cs
[Fact]
public void Serialization_PreservesPropertyValues()
{
    // Create and populate entity
    var customer = new RfCustomer(new EntityBaseServices<RfCustomer>());
    using (customer.PauseAllActions())
    {
        customer.Id = 42;
        customer.Name = "Acme Corp";
        customer.Email = "contact@acme.com";
    }
    customer.FactoryComplete(FactoryOperation.Fetch);

    // Serialize to JSON
    var json = JsonSerializer.Serialize(customer);

    // Property values are preserved in JSON
    Assert.Contains("\"Id\":42", json);
    Assert.Contains("\"Name\":\"Acme Corp\"", json);
    Assert.Contains("\"Email\":\"contact@acme.com\"", json);

    // Note: Meta-properties (IsNew, IsDirty, IsValid) are NOT serialized
    // They are recalculated after deserialization
}
```
<!-- endSnippet -->

Serialization behavior:
- Property values serialize normally
- Meta-properties (IsDirty, IsValid, IsNew) are NOT serialized (client state is transient)
- Validation messages are NOT serialized
- Parent-child relationships are preserved through object graph structure
- Child collections serialize as arrays
- Custom JsonConverter for IValidateBase and IEntityBase types

After deserialization:
- Property values are restored
- IsValid recalculates based on validation rules
- IsDirty starts as false (loaded state is clean)
- Parent-child relationships reconnect during deserialization

The serialization model focuses on transferring data values, not transient state.

## DTOs vs Domain Models

RemoteFactory serializes domain models directly without intermediate DTOs. This reduces mapping overhead but couples client and server to the same domain model contract.

Direct domain model serialization:

<!-- snippet: remotefactory-dto-pattern -->
```cs
[Fact]
public void DirectSerialization_NoIntermediateDtos()
{
    // Neatoo serializes domain models directly without DTOs
    var customer = new RfCustomer(new EntityBaseServices<RfCustomer>());
    using (customer.PauseAllActions())
    {
        customer.Id = 1;
        customer.Name = "Direct Corp";
        customer.Email = "direct@example.com";
    }
    customer.FactoryComplete(FactoryOperation.Fetch);

    // Serialize entity directly - no DTO mapping needed
    var json = JsonSerializer.Serialize(customer);

    // Client and server share same domain model contract
    Assert.Contains("Direct Corp", json);

    // When to add DTOs:
    // - Different client/server model versions
    // - Sensitive properties to exclude
    // - API versioning requirements
}
```
<!-- endSnippet -->

When to use DTOs:
- Client and server have different domain model versions
- Domain model contains sensitive properties not for client consumption
- Client needs a flattened view of a complex aggregate
- API versioning requires stable contracts

When to serialize domain models directly:
- Client and server share the same codebase (Blazor WebAssembly)
- Domain model is designed for client consumption
- No sensitive data in domain model
- Rapid development with low mapping overhead

Neatoo's default pattern is direct serialization. Add a DTO layer when architectural boundaries require it.

## Dependency Injection Setup

RemoteFactory generates DI registration methods that register factory interfaces and implementations in the service collection.

Register factories in DI container:

<!-- snippet: remotefactory-di-setup -->
```cs
[Fact]
public void DiSetup_RegistersFactoryServices()
{
    var services = new ServiceCollection();

    // Add Neatoo core services
    services.AddNeatooServices(NeatooFactory.Logical, typeof(RfCustomer).Assembly);

    // Generated FactoryServiceRegistrar registers:
    // - IRfCustomerFactory (interface)
    // - RfCustomerFactory (implementation)
    // - IFactorySave<RfCustomer> (save interface)
    // - RfCustomer (entity type)
    RfCustomerFactory.FactoryServiceRegistrar(services, NeatooFactory.Logical);

    var provider = services.BuildServiceProvider();

    // Factory is now available via DI
    var factory = provider.GetService<IRfCustomerFactory>();
    Assert.NotNull(factory);
}
```
<!-- endSnippet -->

DI registration details:
- FactoryServiceRegistrar static method generated on each factory class
- Registers ICustomerFactory, CustomerFactory, IFactorySave<Customer>
- Registers factory method delegates for remote invocation
- NeatooFactory enum selects local vs remote execution mode
- Scoped lifetime recommended for factories (matches request scope)

Call FactoryServiceRegistrar during application startup:

<!-- snippet: remotefactory-di-startup -->
```cs
[Fact]
public void DiStartup_CallsRegistrarDuringStartup()
{
    // In Program.cs or Startup.cs:
    var services = new ServiceCollection();

    // Register Neatoo core services
    services.AddNeatooServices(NeatooFactory.Logical, typeof(RfCustomer).Assembly);

    // Each entity's factory has a generated registrar method
    // NeatooFactory modes:
    // - Logical: All factory methods execute locally
    // - Remote: [Remote] methods execute on server via HTTP
    // - Server: Server-side, all methods execute locally
    RfCustomerFactory.FactoryServiceRegistrar(services, NeatooFactory.Logical);

    var provider = services.BuildServiceProvider();
    var factory = provider.GetRequiredService<IRfCustomerFactory>();

    // Use factory in application code
    var customer = factory.Create();
    Assert.NotNull(customer);
}
```
<!-- endSnippet -->

The generated registrar integrates factories into ASP.NET Core or Blazor dependency injection.

## Factory Core and Base Services

Each entity requires IEntityBaseServices<T> for property management and IFactoryCore<T> for factory coordination. These services are registered by Neatoo's DI extensions.

Core services registration:

<!-- snippet: remotefactory-core-services -->
```cs
[Fact]
public void CoreServices_ProvidedByNeatoo()
{
    var services = new ServiceCollection();

    // AddNeatooServices registers core infrastructure:
    services.AddNeatooServices(NeatooFactory.Logical, typeof(RfCustomer).Assembly);

    var provider = services.BuildServiceProvider();

    // IEntityBaseServices<T> - property management, rule execution
    var entityServices = provider.GetService<IEntityBaseServices<RfCustomer>>();
    Assert.NotNull(entityServices);

    // IValidateBaseServices<T> - validation services
    var validateServices = provider.GetService<IValidateBaseServices<RfCustomer>>();
    Assert.NotNull(validateServices);

    // Application code injects factory interfaces, not core services
}
```
<!-- endSnippet -->

Core services provide:
- **IEntityBaseServices<T>**: Property management, rule execution, task tracking
- **IValidateBaseServices<T>**: Validation services for ValidateBase types
- **IFactoryCore<T>**: Factory lifecycle coordination (FactoryStart/FactoryComplete)
- **PropertyManager**: Property wrapper access and meta-property tracking
- **RuleManager**: Business rule registration and execution

These services are internal infrastructure consumed by entities and factories. Application code injects ICustomerFactory, not IEntityBaseServices.

## Factory Method Lifecycle

Factory methods execute within a controlled lifecycle that manages entity state transitions, validation suspension, and task coordination.

Factory lifecycle phases:

<!-- snippet: remotefactory-lifecycle -->
```cs
[Fact]
public void Lifecycle_ManagedByFactory()
{
    var customer = new RfCustomer(new EntityBaseServices<RfCustomer>());

    // Phase 1: FactoryStart - before method executes
    // - Sets FactoryOperation
    // - Calls PauseAllActions (for Fetch/Create)
    customer.FactoryStart(FactoryOperation.Create);
    Assert.True(customer.IsPaused);

    // Phase 2: Method execution
    // - Services injected, persistence operations run
    customer.Create();

    // Phase 3: FactoryComplete - after method completes
    // - Calls Resume (for Fetch/Create)
    // - Updates IsNew, IsModified based on operation
    customer.FactoryComplete(FactoryOperation.Create);

    Assert.False(customer.IsPaused);
    Assert.True(customer.IsNew);
}
```
<!-- endSnippet -->

Lifecycle coordination:
1. **FactoryStart**: Called before factory method executes
   - Sets FactoryOperation (Create, Fetch, Insert, Update, Delete)
   - Calls PauseAllActions (for Fetch/Create)
2. **Method execution**: Entity factory method runs
   - Services injected
   - Persistence operations execute
   - Properties modified
3. **FactoryComplete**: Called after factory method completes
   - Calls Resume (for Fetch/Create)
   - Updates IsNew, IsModified based on operation
   - Runs validation (for Create)
   - Marks entity as clean (for Fetch)

The lifecycle ensures entities are in correct state after factory operations complete.

## Multiple Fetch Overloads

Entities can declare multiple Fetch overloads to support different query patterns. The factory generates methods for each overload.

Declare multiple Fetch methods:

<!-- snippet: remotefactory-fetch-overloads -->
```cs
[Factory]
public partial class RfCustomerMultiFetch : EntityBase<RfCustomerMultiFetch>
{
    public RfCustomerMultiFetch(IEntityBaseServices<RfCustomerMultiFetch> services) : base(services) { }

    public partial int Id { get; set; }
    public partial string Name { get; set; }
    public partial string Email { get; set; }

    // Multiple Fetch overloads for different query patterns
    [Fetch]
    public async Task FetchById(int id, [Service] IRfCustomerRepository repository)
    {
        var data = await repository.FetchByIdAsync(id);
        if (data != null)
        {
            Id = data.Id;
            Name = data.Name;
            Email = data.Email;
        }
    }

    [Fetch]
    public async Task FetchByEmail(string email, [Service] IRfCustomerRepository repository)
    {
        var data = await repository.FetchByEmailAsync(email);
        if (data != null)
        {
            Id = data.Id;
            Name = data.Name;
            Email = data.Email;
        }
    }
}
```
<!-- endSnippet -->

Factory generates:
- ICustomerFactory.FetchById(int id)
- ICustomerFactory.FetchByEmail(string email)
- Both methods set IsNew = false and IsModified = false
- Both methods use PauseAllActions during loading

Multiple Fetch overloads enable flexible query APIs without proliferating factory interfaces.

## Child Entity Factories

Aggregate roots load child entities through child factories. Parent factories inject child factories as services and use them to load child collections.

Load child collections via child factory:

<!-- snippet: remotefactory-child-factories -->
```cs
[Factory]
public partial class RfOrder : EntityBase<RfOrder>
{
    public RfOrder(IEntityBaseServices<RfOrder> services) : base(services)
    {
        // Initialize child collection
        ItemsProperty.LoadValue(new RfOrderItemList());
    }

    public partial int Id { get; set; }
    public partial string OrderNumber { get; set; }
    public partial DateTime OrderDate { get; set; }
    public partial IRfOrderItemList Items { get; set; }

    // Parent factory injects child factory as service
    [Fetch]
    public async Task FetchAsync(
        int id,
        [Service] IRfOrderRepository repository,
        [Service] IRfOrderItemFactory itemFactory)
    {
        var data = await repository.FetchAsync(id);
        Id = data.Id;
        OrderNumber = data.OrderNumber;
        OrderDate = data.OrderDate;

        // Load child collection via child factory
        var itemsData = await repository.FetchItemsAsync(id);
        foreach (var itemData in itemsData)
        {
            var item = itemFactory.Create();
            item.Id = itemData.Id;
            item.ProductCode = itemData.ProductCode;
            item.Price = itemData.Price;
            item.Quantity = itemData.Quantity;
            item.DoMarkOld();
            item.DoMarkUnmodified();
            Items.Add(item);
        }
    }
}
```
<!-- endSnippet -->

Child factory patterns:
- Parent factory injects IOrderItemListFactory
- Child factory has a Fetch overload accepting a collection of EF entities
- Child factory creates domain model child entities from EF entities
- Parent assigns child collection to property
- Parent-child relationship established during assignment
- Child entities cascade validation and dirty state to parent

Child factories are registered in DI alongside parent factories.

## Factory Authorization

RemoteFactory supports factory method authorization through the [AuthorizeFactory] attribute. Authorization delegates intercept factory method calls and enforce permissions.

Authorize factory methods:

<!-- snippet: remotefactory-authorization -->
```cs
[Factory]
[AuthorizeFactory<IRfCustomerAuth>]
public partial class RfCustomerAuthorized : EntityBase<RfCustomerAuthorized>
{
    public RfCustomerAuthorized(IEntityBaseServices<RfCustomerAuthorized> services) : base(services) { }

    public partial int Id { get; set; }
    public partial string Name { get; set; }
    public partial string Email { get; set; }

    // Authorization checked before Create executes
    [Create]
    public void Create()
    {
        Id = 0;
        Name = "";
        Email = "";
    }

    // Authorization checked before Fetch executes
    [Fetch]
    public async Task FetchAsync(int id, [Service] IRfCustomerRepository repository)
    {
        var data = await repository.FetchByIdAsync(id);
        if (data != null)
        {
            Id = data.Id;
            Name = data.Name;
            Email = data.Email;
        }
    }
}
```
<!-- endSnippet -->

Authorization flow:
1. [AuthorizeFactory<ICustomerAuth>] applied to entity class
2. ICustomerAuth service injected into factory
3. Before each factory method executes, auth delegate is called
4. Auth delegate checks permissions for the operation
5. If authorized, factory method executes
6. If not authorized, exception thrown

Authorization delegates enable role-based access control for entity operations.

---

**UPDATED:** 2026-01-24
