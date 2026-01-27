# Remote Factory

[← Properties](properties.md) | [↑ Guides](index.md) | [Validation →](validation.md)

RemoteFactory is Neatoo's source-generated factory system for entity lifecycle operations. The source generator analyzes attributes on entity methods ([Create], [Fetch], [Insert], [Update], [Delete]) and generates strongly-typed factory interfaces with dependency injection support. For distributed architectures, the [Remote] attribute enables client-server execution where marked methods run on the server while entity state transfers as JSON.

## Factory Method Attributes

Factory methods define entity lifecycle operations. The RemoteFactory source generator discovers methods marked with [Create], [Fetch], [Insert], [Update], and [Delete] attributes and generates factory classes with dependency injection support.

Declare factory methods on an EntityBase class:

<!-- snippet: remotefactory-factory-methods -->
<a id='snippet-remotefactory-factory-methods'></a>
```cs
/// <summary>
/// Customer entity demonstrating factory method attributes.
/// </summary>
[Factory]
public partial class SkillFactoryCustomer : EntityBase<SkillFactoryCustomer>
{
    public SkillFactoryCustomer(IEntityBaseServices<SkillFactoryCustomer> services) : base(services) { }

    public partial int Id { get; set; }
    public partial string Name { get; set; }
    public partial string Email { get; set; }

    // [Create] - Initializes a new entity
    [Create]
    public void Create()
    {
        Id = 0;
        Name = "";
        Email = "";
    }

    // [Fetch] - Loads existing entity from persistence
    [Fetch]
    public async Task FetchByIdAsync(int id, [Service] ISkillCustomerRepository repository)
    {
        var data = await repository.FetchByIdAsync(id);
        if (data != null)
        {
            Id = data.Id;
            Name = data.Name;
            Email = data.Email;
        }
    }

    // [Insert] - Saves new entity (when IsNew = true)
    [Insert]
    public async Task InsertAsync([Service] ISkillCustomerRepository repository)
    {
        await repository.InsertAsync(Id, Name, Email);
    }

    // [Update] - Saves existing entity (when IsNew = false, IsDeleted = false)
    [Update]
    public async Task UpdateAsync([Service] ISkillCustomerRepository repository)
    {
        await repository.UpdateAsync(Id, Name, Email);
    }

    // [Delete] - Removes entity (when IsDeleted = true)
    [Delete]
    public async Task DeleteAsync([Service] ISkillCustomerRepository repository)
    {
        await repository.DeleteAsync(Id);
    }
}
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/FactorySamples.cs#L15-L71' title='Snippet source file'>snippet source</a> | <a href='#snippet-remotefactory-factory-methods' title='Start of snippet'>anchor</a></sup>
<a id='snippet-remotefactory-factory-methods-1'></a>
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
<sup><a href='/src/docs/samples/RemoteFactorySamples.cs#L98-L146' title='Snippet source file'>snippet source</a> | <a href='#snippet-remotefactory-factory-methods-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Factory method patterns:
- **[Factory]** attribute on the entity class enables source generation
- **[Create]** initializes a new entity instance (sets default values)
- **[Fetch]** loads entity state from persistence (repository query)
- **[Insert]** persists a new entity (IsNew == true)
- **[Update]** persists changes to existing entity (IsNew == false && IsModified == true)
- **[Delete]** removes entity from persistence (IsDeleted == true)
- **[Service]** parameter attribute injects dependencies from DI container

The source generator creates a factory interface (IRfCustomerFactory) and internal implementation with methods matching the declared factory methods.

## Generated Factory Interface

RemoteFactory generates a public interface exposing the factory methods declared on the entity class. This interface becomes the primary API for creating, fetching, and saving entities.

Generated factory interface:

<!-- snippet: remotefactory-generated-interface -->
<a id='snippet-remotefactory-generated-interface'></a>
```cs
// The source generator creates a public interface:
//
// public interface ISkillFactoryCustomerFactory
// {
//     SkillFactoryCustomer Create();
//     Task<SkillFactoryCustomer> FetchByIdAsync(int id);
//     Task<SkillFactoryCustomer?> SaveAsync(SkillFactoryCustomer target);
// }
//
// Note: [Service] parameters are NOT exposed in the interface
// They are resolved from DI at runtime
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/FactorySamples.cs#L472-L484' title='Snippet source file'>snippet source</a> | <a href='#snippet-remotefactory-generated-interface' title='Start of snippet'>anchor</a></sup>
<a id='snippet-remotefactory-generated-interface-1'></a>
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

    // Verify the factory is available via DI
    var factory = GetRequiredService<IRfCustomerFactory>();
    Assert.NotNull(factory);

    // Verify we can use the factory methods
    var customer = factory.Create();
    Assert.NotNull(customer);
    Assert.True(customer.IsNew);
}
```
<sup><a href='/src/docs/samples/RemoteFactorySamples.cs#L613-L634' title='Snippet source file'>snippet source</a> | <a href='#snippet-remotefactory-generated-interface-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Interface characteristics:
- Public interface named I{ClassName}Factory
- Create method returns new entity instance (synchronous)
- Fetch methods return Task<T> with loaded entity
- SaveAsync method unifies Insert/Update logic based on IsNew state
- Generated method signatures match entity factory method parameters
- CancellationToken parameter added automatically for async operations
- Registered in DI container as scoped services

Application code injects the factory interface to create, fetch, and save entities without directly calling entity methods.

## Factory Method Generation

The source generator creates both the factory interface and a concrete implementation class. The implementation handles local vs remote execution, dependency injection, state management, and task coordination.

Generated factory implementation:

<!-- snippet: remotefactory-generated-implementation -->
<a id='snippet-remotefactory-generated-implementation'></a>
```cs
// The source generator creates an internal implementation:
//
// internal class SkillFactoryCustomerFactory : ISkillFactoryCustomerFactory
// {
//     private readonly IServiceProvider _serviceProvider;
//
//     public SkillFactoryCustomer Create()
//     {
//         var entity = CreateInstance();
//         using (entity.PauseAllActions())
//         {
//             entity.Create();
//         }
//         entity.FactoryComplete(FactoryOperation.Create);
//         return entity;
//     }
// }
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/FactorySamples.cs#L486-L504' title='Snippet source file'>snippet source</a> | <a href='#snippet-remotefactory-generated-implementation' title='Start of snippet'>anchor</a></sup>
<a id='snippet-remotefactory-generated-implementation-1'></a>
```cs
[Fact]
public void GeneratedImplementation_HandlesLifecycle()
{
    // The source generator creates an internal implementation that
    // handles the entity lifecycle automatically

    var factory = GetRequiredService<IRfCustomerFactory>();
    var customer = factory.Create();

    // Factory coordinates lifecycle - IsNew is set automatically
    Assert.True(customer.IsNew);
    Assert.Equal(0, customer.Id);
    Assert.Equal("", customer.Name);
}
```
<sup><a href='/src/docs/samples/RemoteFactorySamples.cs#L636-L651' title='Snippet source file'>snippet source</a> | <a href='#snippet-remotefactory-generated-implementation-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Implementation details:
- Internal class inheriting from FactorySaveBase<T> for SaveAsync coordination
- Constructor receives IServiceProvider for [Service] parameter resolution
- Entity lifecycle managed: suspends validation during Fetch, updates IsNew/IsModified after operations
- Create method calls entity's [Create] method wrapped in factory lifecycle
- Fetch methods call entity's [Fetch] methods with PauseAllActions during data loading
- SaveAsync determines Insert vs Update based on IsNew state, routes to appropriate entity method

The factory implementation is registered in DI as scoped and provided through the public interface.

## Service Parameter Injection

Factory methods can declare dependencies using the [Service] attribute. The factory implementation resolves these services from IServiceProvider at runtime.

Inject repository dependencies:

<!-- snippet: remotefactory-service-injection -->
<a id='snippet-remotefactory-service-injection'></a>
```cs
/// <summary>
/// Entity demonstrating [Service] attribute for DI injection.
/// </summary>
[Factory]
public partial class SkillFactoryReport : EntityBase<SkillFactoryReport>
{
    public SkillFactoryReport(IEntityBaseServices<SkillFactoryReport> services) : base(services) { }

    public partial int Id { get; set; }
    public partial string ReportName { get; set; }
    public partial byte[] ReportData { get; set; }

    // [Service] parameters are resolved from DI container at runtime
    // They are NOT exposed in the factory interface
    [Fetch]
    public async Task FetchAsync(
        int id,
        [Service] ISkillReportRepository repository,
        [Service] ISkillReportGenerator generator)
    {
        var metadata = await repository.FetchMetadataAsync(id);
        Id = metadata.Id;
        ReportName = metadata.Name;
        ReportData = await generator.GenerateAsync(id);
    }
}
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/FactorySamples.cs#L303-L330' title='Snippet source file'>snippet source</a> | <a href='#snippet-remotefactory-service-injection' title='Start of snippet'>anchor</a></sup>
<a id='snippet-remotefactory-service-injection-1'></a>
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
<sup><a href='/src/docs/samples/RemoteFactorySamples.cs#L151-L176' title='Snippet source file'>snippet source</a> | <a href='#snippet-remotefactory-service-injection-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Service injection patterns:
- **[Service]** parameters are resolved from DI container when factory method executes
- Services can be interfaces or concrete types (e.g., ICustomerRepository, DbContext)
- Multiple [Service] parameters can be declared in a single method
- Service resolution uses IServiceProvider.GetRequiredService at runtime
- Scoped services are resolved within the factory's scope (typically per HTTP request)
- Missing services throw InvalidOperationException with clear error message

Service injection separates domain logic (entity validation and mapping) from infrastructure (persistence and external service calls).

## Fetch: Loading State from Persistence

Fetch methods load entity state from persistence. The factory wraps the Fetch call with PauseAllActions to prevent validation rule execution during data loading.

Implement a Fetch method:

<!-- snippet: remotefactory-fetch -->
<a id='snippet-remotefactory-fetch'></a>
```cs
/// <summary>
/// Entity demonstrating [Fetch] implementation.
/// </summary>
[Factory]
public partial class SkillFactoryProduct : EntityBase<SkillFactoryProduct>
{
    public SkillFactoryProduct(IEntityBaseServices<SkillFactoryProduct> services) : base(services) { }

    public partial int Id { get; set; }
    public partial string Name { get; set; }
    public partial decimal Price { get; set; }

    // Fetch loads entity state from persistence
    // Factory wraps with PauseAllActions to prevent validation during load
    [Fetch]
    public async Task FetchAsync(int id, [Service] ISkillProductRepository repository)
    {
        var data = await repository.FetchByIdAsync(id);
        if (data != null)
        {
            Id = data.Id;
            Name = data.Name;
            Price = data.Price;
        }
        // After Fetch: IsNew = false, IsModified = false
    }
}
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/FactorySamples.cs#L77-L105' title='Snippet source file'>snippet source</a> | <a href='#snippet-remotefactory-fetch' title='Start of snippet'>anchor</a></sup>
<a id='snippet-remotefactory-fetch-1'></a>
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
<sup><a href='/src/docs/samples/RemoteFactorySamples.cs#L181-L207' title='Snippet source file'>snippet source</a> | <a href='#snippet-remotefactory-fetch-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Fetch behavior:
- Factory calls PauseAllActions before executing the entity's Fetch method
- PauseAllActions suspends validation rules, business rules, and modification tracking
- Property assignments during Fetch use ChangeReason.Load (no validation execution)
- Child entities are loaded by injecting child factories and calling their Fetch methods
- Parent-child relationships are established when child entities are added to parent collections
- Factory automatically sets IsNew = false and IsModified = false after Fetch completes
- Validation rules are not executed during data loading

Property assignment during Fetch:
- All property setters within PauseAllActions scope defer event handling
- PropertyChanged events are queued and fire after PauseAllActions completes
- Validation rules do not execute (even after resume) because ChangeReason is Load
- Entity state is clean (IsModified = false) reflecting loaded data without triggering modification tracking

## Save: Persisting Entity Changes

Save methods unify Insert/Update logic. The generated factory determines whether to call Insert or Update based on IsNew, then persists changes through the entity's factory method.

Implement Insert and Update methods:

<!-- snippet: remotefactory-save -->
<a id='snippet-remotefactory-save'></a>
```cs
/// <summary>
/// Entity demonstrating save routing logic.
/// </summary>
[Factory]
public partial class SkillFactoryAccount : EntityBase<SkillFactoryAccount>
{
    public SkillFactoryAccount(IEntityBaseServices<SkillFactoryAccount> services) : base(services) { }

    public partial int Id { get; set; }
    public partial string AccountName { get; set; }
    public partial decimal Balance { get; set; }

    [Create]
    public void Create()
    {
        Id = 0;
        Balance = 0;
    }

    [Fetch]
    public void Fetch(int id, string name, decimal balance)
    {
        Id = id;
        AccountName = name;
        Balance = balance;
    }

    // Save routing based on entity state:
    // - IsNew == true           → Insert
    // - IsNew == false, !Deleted → Update
    // - IsDeleted == true       → Delete

    [Insert]
    public async Task InsertAsync([Service] ISkillAccountRepository repository)
    {
        await repository.InsertAsync(Id, AccountName, Balance);
        // After Insert: IsNew = false
    }

    [Update]
    public async Task UpdateAsync([Service] ISkillAccountRepository repository)
    {
        await repository.UpdateAsync(Id, AccountName, Balance);
        // After Update: IsModified = false
    }

    [Delete]
    public async Task DeleteAsync([Service] ISkillAccountRepository repository)
    {
        await repository.DeleteAsync(Id);
    }
}
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/FactorySamples.cs#L167-L220' title='Snippet source file'>snippet source</a> | <a href='#snippet-remotefactory-save' title='Start of snippet'>anchor</a></sup>
<a id='snippet-remotefactory-save-1'></a>
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
<sup><a href='/src/docs/samples/RemoteFactorySamples.cs#L212-L248' title='Snippet source file'>snippet source</a> | <a href='#snippet-remotefactory-save-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Save coordination:
1. Application calls factory.SaveAsync(customer)
2. Factory examines customer.IsDeleted, customer.IsNew, and customer.IsModified
3. If IsDeleted == true, factory calls entity's [Delete] method
4. If IsNew == true, factory calls entity's [Insert] method
5. If IsNew == false and IsModified == true, factory calls entity's [Update] method
6. Entity method executes persistence logic (typically maps to EF entity and calls SaveChanges)
7. After successful save: IsModified = false, IsNew = false (if Insert), IsDeleted remains true (if Delete)

Save methods should validate before persisting:

<!-- snippet: remotefactory-save-validation -->
<a id='snippet-remotefactory-save-validation'></a>
```cs
/// <summary>
/// Entity demonstrating validation before save.
/// </summary>
[Factory]
public partial class SkillFactoryValidatedOrder : EntityBase<SkillFactoryValidatedOrder>
{
    public SkillFactoryValidatedOrder(IEntityBaseServices<SkillFactoryValidatedOrder> services) : base(services)
    {
        RuleManager.AddValidation(
            order => order.Quantity > 0 ? "" : "Quantity must be positive",
            o => o.Quantity);
    }

    public partial int Id { get; set; }
    public partial int Quantity { get; set; }
    public partial decimal UnitPrice { get; set; }

    [Create]
    public void Create() { }

    [Insert]
    public async Task InsertAsync([Service] ISkillOrderRepository repository)
    {
        // IsSavable verifies: IsValid && !IsBusy && IsModified && !IsChild
        if (!IsSavable)
        {
            // Validation failed - don't persist
            return;
        }

        await repository.InsertOrderAsync(Id, Quantity, UnitPrice);
    }
}
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/FactorySamples.cs#L226-L260' title='Snippet source file'>snippet source</a> | <a href='#snippet-remotefactory-save-validation' title='Start of snippet'>anchor</a></sup>
<a id='snippet-remotefactory-save-validation-1'></a>
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
<sup><a href='/src/docs/samples/RemoteFactorySamples.cs#L253-L297' title='Snippet source file'>snippet source</a> | <a href='#snippet-remotefactory-save-validation-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

IsSavable is a computed property: IsModified && IsValid && !IsBusy && !IsChild. Checking IsSavable before persisting prevents invalid entities, busy entities (with running async rules), unmodified entities, and child entities (which must save through the aggregate root) from attempting direct persistence.

## Delete: Removing Entities

Delete methods remove entities from persistence. The factory marks the entity as deleted before calling the Delete method.

Implement a Delete method:

<!-- snippet: remotefactory-delete -->
<a id='snippet-remotefactory-delete'></a>
```cs
/// <summary>
/// Entity demonstrating delete pattern.
/// </summary>
[Factory]
public partial class SkillFactoryProject : EntityBase<SkillFactoryProject>
{
    public SkillFactoryProject(IEntityBaseServices<SkillFactoryProject> services) : base(services) { }

    public partial int Id { get; set; }
    public partial string ProjectName { get; set; }

    [Fetch]
    public void Fetch(int id, string name)
    {
        Id = id;
        ProjectName = name;
    }

    // Delete: Called when IsDeleted == true during Save
    [Delete]
    public async Task DeleteAsync([Service] ISkillProjectRepository repository)
    {
        await repository.DeleteAsync(Id);
        // Entity cannot be modified or saved after delete completes
    }
}
// Usage:
// var project = await factory.Fetch(1, "My Project");
// project.Delete();           // Marks for deletion
// await factory.SaveAsync(project);  // Routes to DeleteAsync
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/FactorySamples.cs#L266-L297' title='Snippet source file'>snippet source</a> | <a href='#snippet-remotefactory-delete' title='Start of snippet'>anchor</a></sup>
<a id='snippet-remotefactory-delete-1'></a>
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
<sup><a href='/src/docs/samples/RemoteFactorySamples.cs#L302-L331' title='Snippet source file'>snippet source</a> | <a href='#snippet-remotefactory-delete-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Delete behavior:
- Application calls entity.Delete() to mark for deletion
- entity.Delete() sets IsDeleted = true and IsModified = true
- Application calls factory.SaveAsync(entity)
- Factory detects IsDeleted == true and routes to entity's [Delete] method
- Delete method executes persistence removal logic (e.g., DbContext.Remove, repository.DeleteAsync)
- After delete completes: IsDeleted remains true, entity cannot be modified further

Aggregate root deletion pattern:
1. Mark root entity for deletion: aggregateRoot.Delete()
2. Handle child cascades in [Delete] method or via database cascade rules
3. Call factory.SaveAsync(aggregateRoot) to execute deletion
4. Factory routes to [Delete] method which removes root and cascades to children

## Remote vs Local Execution

RemoteFactory supports both local (in-process) and remote (client-server) execution. The [Remote] attribute marks methods that should execute on the server when running in a distributed architecture.

Mark factory methods for remote execution:

<!-- snippet: remotefactory-remote-attribute -->
<a id='snippet-remotefactory-remote-attribute'></a>
```cs
/// <summary>
/// Entity demonstrating [Remote] attribute for client-server execution.
/// </summary>
[Factory]
public partial class SkillFactoryRemoteEntity : EntityBase<SkillFactoryRemoteEntity>
{
    public SkillFactoryRemoteEntity(IEntityBaseServices<SkillFactoryRemoteEntity> services) : base(services) { }

    public partial int Id { get; set; }
    public partial string Data { get; set; }

    // [Create] without [Remote] - executes locally on client
    [Create]
    public void Create()
    {
        Id = 0;
        Data = "";
    }

    // [Remote] marks methods for server execution
    // In NeatooFactory.Remote mode, this executes on server via HTTP
    [Remote]
    [Fetch]
    public async Task FetchAsync(int id, [Service] ISkillDataRepository repository)
    {
        var data = await repository.FetchAsync(id);
        Id = data.Id;
        Data = data.Data;
    }

    [Remote]
    [Insert]
    public async Task InsertAsync([Service] ISkillDataRepository repository)
    {
        await repository.InsertAsync(Id, Data);
    }
}
// Without [Remote], methods execute locally (client-side)
// Use [Remote] when:
// - Accessing database or server-only resources
// - Performing operations that shouldn't run on the client
// - Needing server-side services
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/FactorySamples.cs#L336-L379' title='Snippet source file'>snippet source</a> | <a href='#snippet-remotefactory-remote-attribute' title='Start of snippet'>anchor</a></sup>
<a id='snippet-remotefactory-remote-attribute-1'></a>
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
<sup><a href='/src/docs/samples/RemoteFactorySamples.cs#L336-L391' title='Snippet source file'>snippet source</a> | <a href='#snippet-remotefactory-remote-attribute-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

NeatooFactory execution modes:
- **NeatooFactory.Logical**: All factory methods execute in-process (for server apps, console apps, tests)
- **NeatooFactory.Remote**: Methods marked [Remote] execute on server via HTTP; methods without [Remote] execute locally (for Blazor WebAssembly clients)
- **NeatooFactory.Server**: Server-side configuration where all methods execute locally, but infrastructure is configured to receive remote calls (for ASP.NET Core server hosting the API)

Remote execution flow (Blazor WebAssembly calling server):
1. Client calls factory.FetchById(id) on Blazor WebAssembly
2. Factory implementation checks if [Remote] is present on method
3. If [Remote]: serializes method parameters as JSON, sends HTTP POST to server endpoint
4. Server receives request, deserializes parameters, resolves factory from DI
5. Server factory executes entity's [Fetch] method locally (with database access)
6. Server serializes result entity as JSON and returns HTTP response
7. Client deserializes response entity and returns to caller

The [Remote] pattern enables Blazor WebAssembly clients to execute server-side persistence logic without exposing repositories or DbContext to the client.

## Client-Server Serialization

Entities transfer between client and server as JSON. RemoteFactory includes custom JSON converters for ValidateBase and EntityBase types that preserve entity state (IsDirty, IsValid, validation messages) across serialization.

JSON serialization preserves entity state:

<!-- snippet: remotefactory-serialization -->
<a id='snippet-remotefactory-serialization'></a>
```cs
[Fact]
public async Task Serialization_PreservesPropertyValues()
{
    var factory = GetRequiredService<IRfCustomerFactory>();

    // Fetch creates a populated entity
    var customer = await factory.FetchById(1);

    // Serialize to JSON
    var json = JsonSerializer.Serialize(customer);

    // Property values are preserved in JSON
    Assert.Contains("\"Id\":1", json);
    Assert.Contains("\"Name\":\"Acme Corp\"", json);
    Assert.Contains("\"Email\":\"contact@acme.com\"", json);

    // Note: Meta-properties (IsNew, IsDirty, IsValid) are NOT serialized
    // They are recalculated after deserialization
}
```
<sup><a href='/src/docs/samples/RemoteFactorySamples.cs#L653-L673' title='Snippet source file'>snippet source</a> | <a href='#snippet-remotefactory-serialization' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Serialization behavior:
- Property values serialize as standard JSON properties
- Meta-properties (IsModified, IsValid, IsNew, IsBusy) are NOT serialized (transient client state)
- Validation messages (PropertyMessages) are NOT serialized
- Parent-child relationships are preserved through nested object/array structure
- Child collections serialize as JSON arrays
- Custom JsonConverter handles IValidateBase and IEntityBase serialization

After deserialization on client:
- Property values are restored from JSON
- IsValid is recalculated by executing validation rules on the client
- IsModified starts as false (deserialized entities are considered clean)
- Parent-child relationships are re-established by walking object graph
- Business rules are not automatically executed (only validation rules)

The serialization model transfers domain data (property values and object structure) without transferring transient validation state or client-side UI concerns.

## DTOs vs Domain Models

RemoteFactory serializes domain models directly without intermediate DTOs. This reduces mapping overhead but couples client and server to the same domain model contract.

Direct domain model serialization:

<!-- snippet: remotefactory-dto-pattern -->
<a id='snippet-remotefactory-dto-pattern'></a>
```cs
[Fact]
public async Task DirectSerialization_NoIntermediateDtos()
{
    var factory = GetRequiredService<IRfCustomerFactory>();

    // Fetch entity via factory
    var customer = await factory.FetchById(1);

    // Serialize entity directly - no DTO mapping needed
    var json = JsonSerializer.Serialize(customer);

    // Client and server share same domain model contract
    Assert.Contains("Acme Corp", json);

    // When to add DTOs:
    // - Different client/server model versions
    // - Sensitive properties to exclude
    // - API versioning requirements
}
```
<sup><a href='/src/docs/samples/RemoteFactorySamples.cs#L675-L695' title='Snippet source file'>snippet source</a> | <a href='#snippet-remotefactory-dto-pattern' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

When to use DTOs (separate data transfer objects):
- Client and server have different domain model versions (separate assemblies)
- Domain model contains sensitive server-only properties (password hashes, internal IDs)
- Client needs a flattened projection of a complex aggregate
- API versioning requires stable contracts independent of domain model evolution
- Public-facing API consumed by third parties

When to serialize domain models directly (Neatoo default):
- Client and server share the same codebase (Blazor WebAssembly with shared project)
- Domain model is designed for client consumption (no sensitive properties)
- First-party client only (no third-party API consumers)
- Rapid development with minimal mapping overhead

Neatoo's default pattern is direct domain model serialization. This reduces mapping code and keeps client and server synchronized. Add a DTO layer when architectural boundaries, security requirements, or versioning concerns require separation.

## Dependency Injection Setup

RemoteFactory generates DI registration methods that register factory interfaces and implementations in the service collection.

Register factories in DI container:

<!-- snippet: remotefactory-di-setup -->
<a id='snippet-remotefactory-di-setup'></a>
```cs
// Factory services are registered automatically via AddNeatooServices:
//
// services.AddNeatooServices(NeatooFactory.Logical, typeof(Program).Assembly);
//
// This registers:
// - All IXxxFactory interfaces and implementations
// - Core Neatoo services (IEntityBaseServices, IValidateBaseServices, etc.)
// - Authorization handlers
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/FactorySamples.cs#L506-L515' title='Snippet source file'>snippet source</a> | <a href='#snippet-remotefactory-di-setup' title='Start of snippet'>anchor</a></sup>
<a id='snippet-remotefactory-di-setup-1'></a>
```cs
[Fact]
public void DiSetup_RegistersFactoryServices()
{
    // AddNeatooServices automatically registers all factories in the assembly
    // Factory is available via DI
    var factory = GetRequiredService<IRfCustomerFactory>();
    Assert.NotNull(factory);

    // Can also resolve other factories
    var orderFactory = GetRequiredService<IRfOrderFactory>();
    Assert.NotNull(orderFactory);
}
```
<sup><a href='/src/docs/samples/RemoteFactorySamples.cs#L697-L710' title='Snippet source file'>snippet source</a> | <a href='#snippet-remotefactory-di-setup-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

DI registration details:
- AddNeatooServices scans assembly for [Factory] classes and registers factories automatically
- Registers factory interface (IXxxFactory), internal implementation, and IFactorySave<T>
- NeatooFactory mode determines execution routing (Logical, Remote, Server)
- Factories are registered with scoped lifetime (per HTTP request or per Blazor circuit)
- Entity base services (IEntityBaseServices<T>, IValidateBaseServices<T>) are also registered

Application startup registration:

<!-- snippet: remotefactory-di-startup -->
<a id='snippet-remotefactory-di-startup'></a>
```cs
// In Program.cs or Startup.cs:
//
// var builder = WebApplication.CreateBuilder(args);
// builder.Services.AddNeatooServices(NeatooFactory.Logical, typeof(MyEntity).Assembly);
//
// // Register your repositories and services
// builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
//
// NeatooFactory modes:
// - Logical: All factory methods execute locally
// - Remote: [Remote] methods execute on server via HTTP
// - Server: Server-side, all methods execute locally
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/FactorySamples.cs#L517-L530' title='Snippet source file'>snippet source</a> | <a href='#snippet-remotefactory-di-startup' title='Start of snippet'>anchor</a></sup>
<a id='snippet-remotefactory-di-startup-1'></a>
```cs
[Fact]
public void DiStartup_CallsRegistrarDuringStartup()
{
    // In Program.cs or Startup.cs:
    // services.AddNeatooServices(NeatooFactory.Logical, typeof(RfCustomer).Assembly);

    // NeatooFactory modes:
    // - Logical: All factory methods execute locally
    // - Remote: [Remote] methods execute on server via HTTP
    // - Server: Server-side, all methods execute locally

    var factory = GetRequiredService<IRfCustomerFactory>();

    // Use factory in application code
    var customer = factory.Create();
    Assert.NotNull(customer);
}
```
<sup><a href='/src/docs/samples/RemoteFactorySamples.cs#L712-L730' title='Snippet source file'>snippet source</a> | <a href='#snippet-remotefactory-di-startup-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The generated registrar integrates factories into ASP.NET Core or Blazor dependency injection.

## Factory Core and Base Services

Each entity requires IEntityBaseServices<T> for property management and IFactoryCore<T> for factory coordination. These services are registered by Neatoo's DI extensions.

Core services registration:

<!-- snippet: remotefactory-core-services -->
<a id='snippet-remotefactory-core-services'></a>
```cs
// Core services provided by Neatoo:
//
// IEntityBaseServices<T>   - Entity property management, rule execution
// IValidateBaseServices<T> - Validation services
//
// Note: There are no ICommandBaseServices or IReadOnlyBaseServices.
// Commands are static classes, read models use ValidateBase.
//
// Application code injects factory interfaces, not core services
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/FactorySamples.cs#L532-L542' title='Snippet source file'>snippet source</a> | <a href='#snippet-remotefactory-core-services' title='Start of snippet'>anchor</a></sup>
<a id='snippet-remotefactory-core-services-1'></a>
```cs
[Fact]
public void CoreServices_ProvidedByNeatoo()
{
    // IEntityBaseServices<T> - property management, rule execution
    var entityServices = GetRequiredService<IEntityBaseServices<RfCustomer>>();
    Assert.NotNull(entityServices);

    // IValidateBaseServices<T> - validation services
    var validateServices = GetRequiredService<IValidateBaseServices<RfCustomer>>();
    Assert.NotNull(validateServices);

    // Application code injects factory interfaces, not core services
    var factory = GetRequiredService<IRfCustomerFactory>();
    Assert.NotNull(factory);
}
```
<sup><a href='/src/docs/samples/RemoteFactorySamples.cs#L732-L748' title='Snippet source file'>snippet source</a> | <a href='#snippet-remotefactory-core-services-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Core services provide:
- **IEntityBaseServices<T>**: Property management, rule execution, task tracking, and factory lifecycle coordination for EntityBase types
- **IValidateBaseServices<T>**: Property management, validation services, and rule execution for ValidateBase types
- **PropertyManager**: Property wrapper access (via indexer), meta-property aggregation (IsValid, IsBusy), and PropertyChanged events
- **RuleManager**: Business rule registration (AddAction, AddValidation), trigger property tracking, and rule execution

These services are internal framework infrastructure. Entities receive them via constructor injection. Application code injects factory interfaces (ICustomerFactory), not core services.

## Factory Method Lifecycle

Factory methods execute within a controlled lifecycle that manages entity state transitions, validation suspension, and task coordination.

Factory lifecycle phases:

<!-- snippet: remotefactory-lifecycle -->
<a id='snippet-remotefactory-lifecycle'></a>
```cs
// Factory lifecycle phases:
//
// 1. Prepare   - Suspends validation during data loading
// 2. Execute   - Calls your factory method (Create/Fetch/Insert/Update/Delete)
// 3. Finalize  - Resumes validation, updates entity state
//
// After Create: entity.IsNew = true
// After Fetch:  entity.IsNew = false, entity.IsModified = false
// After Insert: entity.IsNew = false, entity.IsModified = false
// After Update: entity.IsModified = false
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/FactorySamples.cs#L544-L555' title='Snippet source file'>snippet source</a> | <a href='#snippet-remotefactory-lifecycle' title='Start of snippet'>anchor</a></sup>
<a id='snippet-remotefactory-lifecycle-1'></a>
```cs
[Fact]
public void Lifecycle_ManagedByFactory()
{
    var factory = GetRequiredService<IRfCustomerFactory>();

    // Factory manages the entire lifecycle:
    // Phase 1: Prepare - suspends validation during data loading
    // Phase 2: Method execution (e.g., Create)
    // Phase 3: Finalize - resumes validation, updates entity state

    var customer = factory.Create();

    // After Create: entity is new and ready for use
    Assert.True(customer.IsNew);
    Assert.False(customer.IsPaused);
}
```
<sup><a href='/src/docs/samples/RemoteFactorySamples.cs#L750-L767' title='Snippet source file'>snippet source</a> | <a href='#snippet-remotefactory-lifecycle-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Lifecycle coordination:
1. **Before method execution**: Factory prepares entity for the operation
   - Calls PauseAllActions for data loading operations (Fetch) to suspend validation and modification tracking
   - Calls BeginEdit for Create operations
   - Tracks the current operation type internally
2. **Method execution**: Entity's factory method runs
   - [Service] parameters are resolved from DI container via IServiceProvider
   - Persistence operations execute (repository calls, DbContext operations)
   - Property setters are called to populate or initialize entity state
3. **After method completion**: Factory finalizes entity state
   - Resumes validation and events (completes PauseAllActions scope)
   - Updates IsNew based on operation (false after Fetch/Insert)
   - Updates IsModified based on operation (false after Fetch/Insert/Update)
   - For Create: runs validation rules to establish initial validation state
   - For Fetch: does not run validation rules (clean loaded state)

The factory lifecycle ensures entities transition to correct state (IsNew, IsModified, IsValid) after each operation without requiring manual state management.

## Multiple Fetch Overloads

Entities can declare multiple Fetch overloads to support different query patterns. The factory generates methods for each overload.

Declare multiple Fetch methods:

<!-- snippet: remotefactory-fetch-overloads -->
<a id='snippet-remotefactory-fetch-overloads'></a>
```cs
/// <summary>
/// Entity with multiple fetch methods for different query patterns.
/// </summary>
[Factory]
public partial class SkillFactoryOrder : EntityBase<SkillFactoryOrder>
{
    public SkillFactoryOrder(IEntityBaseServices<SkillFactoryOrder> services) : base(services) { }

    public partial int Id { get; set; }
    public partial string OrderNumber { get; set; }
    public partial string CustomerEmail { get; set; }
    public partial DateTime OrderDate { get; set; }

    [Create]
    public void Create()
    {
        OrderDate = DateTime.Today;
    }

    // Multiple Fetch overloads for different query patterns
    [Fetch]
    public void FetchById(int id)
    {
        Id = id;
        OrderNumber = $"ORD-{id:D5}";
        OrderDate = DateTime.Today;
    }

    [Fetch]
    public void FetchByOrderNumber(string orderNumber)
    {
        OrderNumber = orderNumber;
        Id = int.Parse(orderNumber.Replace("ORD-", ""));
        OrderDate = DateTime.Today;
    }

    [Fetch]
    public async Task FetchByCustomerAsync(string email, [Service] ISkillOrderRepository repository)
    {
        var data = await repository.FetchByCustomerEmailAsync(email);
        if (data != null)
        {
            Id = data.Id;
            OrderNumber = data.OrderNumber;
            CustomerEmail = email;
            OrderDate = data.OrderDate;
        }
    }
}
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/FactorySamples.cs#L111-L161' title='Snippet source file'>snippet source</a> | <a href='#snippet-remotefactory-fetch-overloads' title='Start of snippet'>anchor</a></sup>
<a id='snippet-remotefactory-fetch-overloads-1'></a>
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
<sup><a href='/src/docs/samples/RemoteFactorySamples.cs#L396-L431' title='Snippet source file'>snippet source</a> | <a href='#snippet-remotefactory-fetch-overloads-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Generated factory interface:
- IRfCustomerMultiFetchFactory.FetchById(int id) → calls entity's FetchById(int id, repository)
- IRfCustomerMultiFetchFactory.FetchByEmail(string email) → calls entity's FetchByEmail(string email, repository)
- Both methods set IsNew = false and IsModified = false after completion
- Both methods use PauseAllActions during data loading to suspend validation
- Method overloads appear on the same factory interface

Multiple Fetch overloads enable flexible query patterns (by ID, by email, by criteria) without creating separate factory interfaces for each query.

## Child Entity Factories

Aggregate roots load child entities through child factories. Parent factories inject child factories as services and use them to load child collections.

Load child collections via child factory:

<!-- snippet: remotefactory-child-factories -->
<a id='snippet-remotefactory-child-factories'></a>
```cs
/// <summary>
/// Order aggregate demonstrating child factory usage.
/// </summary>
[Factory]
public partial class SkillFactoryOrderWithItems : EntityBase<SkillFactoryOrderWithItems>
{
    public SkillFactoryOrderWithItems(IEntityBaseServices<SkillFactoryOrderWithItems> services) : base(services)
    {
        ItemsProperty.LoadValue(new SkillFactoryOrderItemList());
    }

    public partial int Id { get; set; }
    public partial string OrderNumber { get; set; }
    public partial DateTime OrderDate { get; set; }
    public partial ISkillFactoryOrderItemList Items { get; set; }

    // Parent factory injects child factory as service
    [Fetch]
    public async Task FetchAsync(
        int id,
        [Service] ISkillOrderWithItemsRepository repository,
        [Service] ISkillFactoryOrderItemFactory itemFactory)
    {
        var data = await repository.FetchAsync(id);
        Id = data.Id;
        OrderNumber = data.OrderNumber;
        OrderDate = data.OrderDate;

        // Load child collection via child factory
        var itemsData = await repository.FetchItemsAsync(id);
        foreach (var itemData in itemsData)
        {
            // Use factory.Fetch to load existing items
            var item = itemFactory.Fetch(
                itemData.Id,
                itemData.ProductCode,
                itemData.Price,
                itemData.Quantity);
            Items.Add(item);
        }
    }
}
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/FactorySamples.cs#L423-L466' title='Snippet source file'>snippet source</a> | <a href='#snippet-remotefactory-child-factories' title='Start of snippet'>anchor</a></sup>
<a id='snippet-remotefactory-child-factories-1'></a>
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
            // Use factory.Fetch to load existing items
            var item = itemFactory.Fetch(itemData.Id, itemData.ProductCode, itemData.Price, itemData.Quantity);
            Items.Add(item);
        }
    }
}
```
<sup><a href='/src/docs/samples/RemoteFactorySamples.cs#L494-L531' title='Snippet source file'>snippet source</a> | <a href='#snippet-remotefactory-child-factories-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Child factory patterns:
- Parent Fetch method injects child factory as [Service] parameter (IRfOrderItemFactory)
- For each child record from repository, call childFactory.Fetch(...) to create child entity
- Child factory's Fetch method populates the child entity from persistence data
- Parent adds child entity to collection property: Items.Add(item)
- Parent-child relationship is established automatically during collection Add
- Child validation state (IsValid, IsBusy) cascades to parent
- Child modification state (IsModified) cascades to parent aggregate root

Child factories are registered in DI by AddNeatooServices alongside parent factories. Each [Factory] entity gets its own factory interface.

## Factory Authorization

RemoteFactory supports factory method authorization through the [AuthorizeFactory] attribute. Authorization delegates intercept factory method calls and enforce permissions.

Authorize factory methods:

<!-- snippet: remotefactory-authorization -->
<a id='snippet-remotefactory-authorization'></a>
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
<sup><a href='/src/docs/samples/RemoteFactorySamples.cs#L572-L605' title='Snippet source file'>snippet source</a> | <a href='#snippet-remotefactory-authorization' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Authorization flow:
1. [AuthorizeFactory<IRfCustomerAuth>] attribute applied to entity class
2. IRfCustomerAuth service is resolved from DI when factory is constructed
3. Before each factory method executes, factory checks authorization
4. Factory calls corresponding IRfCustomerAuth method (CanCreate, CanFetch, etc.) based on operation
5. If authorization method returns true, factory method executes normally
6. If authorization method returns false, factory throws UnauthorizedAccessException

Authorization attributes on IRfCustomerAuth interface methods:
- [AuthorizeFactory(AuthorizeFactoryOperation.Create)] → checks before Create
- [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)] → checks before Fetch methods
- [AuthorizeFactory(AuthorizeFactoryOperation.Read | AuthorizeFactoryOperation.Write)] → checks for both read and write operations

Authorization delegates enable declarative role-based access control for entity lifecycle operations.

---

**UPDATED:** 2026-01-24
