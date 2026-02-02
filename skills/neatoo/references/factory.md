# Factory Methods

Neatoo uses factory attributes to generate client-callable factory methods through source generation. Mark your class with `[Factory]` and methods with operation attributes.

## Factory Attributes Overview

| Attribute | Purpose | Generated Method |
|-----------|---------|------------------|
| `[Factory]` | Marks class for factory generation | - |
| `[Create]` | Initialize new object | `Create()` / `CreateAsync()` |
| `[Fetch]` | Load existing object | `Fetch()` / `FetchAsync()` |
| `[Insert]` | Save new object | Called by `Save()` |
| `[Update]` | Save existing object | Called by `Save()` |
| `[Delete]` | Remove object | Called by `Save()` |

## Basic Factory Setup

Mark your class and add factory methods:

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
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/FactorySamples.cs#L49-L105' title='Snippet source file'>snippet source</a> | <a href='#snippet-remotefactory-factory-methods' title='Start of snippet'>anchor</a></sup>
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

## Create Method

Initialize new objects:

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
<sup><a href='/src/docs/samples/EntitiesSamples.cs#L235-L253' title='Snippet source file'>snippet source</a> | <a href='#snippet-entities-lifecycle-new' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Fetch Method

Load existing objects from persistence:

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
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/FactorySamples.cs#L111-L139' title='Snippet source file'>snippet source</a> | <a href='#snippet-remotefactory-fetch' title='Start of snippet'>anchor</a></sup>
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

## Fetch Overloads

Multiple fetch methods with different parameters:

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
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/FactorySamples.cs#L145-L195' title='Snippet source file'>snippet source</a> | <a href='#snippet-remotefactory-fetch-overloads' title='Start of snippet'>anchor</a></sup>
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

## Save Routing

When `Save()` is called, Neatoo routes to the appropriate method:

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
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/FactorySamples.cs#L201-L254' title='Snippet source file'>snippet source</a> | <a href='#snippet-remotefactory-save' title='Start of snippet'>anchor</a></sup>
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

**Routing Logic:**
- `IsNew == true` → `[Insert]` method
- `IsNew == false && IsDeleted == false` → `[Update]` method
- `IsDeleted == true` → `[Delete]` method

## Save Validation

Save automatically validates before executing:

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
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/FactorySamples.cs#L260-L294' title='Snippet source file'>snippet source</a> | <a href='#snippet-remotefactory-save-validation' title='Start of snippet'>anchor</a></sup>
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

## Delete Method

Handle deletion:

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
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/FactorySamples.cs#L300-L331' title='Snippet source file'>snippet source</a> | <a href='#snippet-remotefactory-delete' title='Start of snippet'>anchor</a></sup>
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

## Service Injection

Inject dependencies using `[Service]`:

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
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/FactorySamples.cs#L337-L364' title='Snippet source file'>snippet source</a> | <a href='#snippet-remotefactory-service-injection' title='Start of snippet'>anchor</a></sup>
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

## Remote Execution

`[Remote]` marks **entry points from the client to the server**. Once execution crosses to the server, it stays there—subsequent method calls don't need `[Remote]`.

Add `[Remote]` to aggregate root factory methods:

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
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/FactorySamples.cs#L392-L435' title='Snippet source file'>snippet source</a> | <a href='#snippet-remotefactory-remote-attribute' title='Start of snippet'>anchor</a></sup>
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

**When to use `[Remote]`:**
- Aggregate root factory methods that are entry points from the client
- Top-level Execute operations initiated by UI

**When `[Remote]` is NOT needed (the common case):**
- Child entity operations within an aggregate
- Any method called from server-side code (after already crossing the boundary via an aggregate root's `[Remote]` method)
- Methods with method-injected services that are only called from server-side code

**Constructor vs Method Injection:**
- Constructor injection (`[Service]` on constructor): Services available on both client and server
- Method injection (`[Service]` on method parameters): Server-only services—the common case for most factory methods

**Entity duality:** An entity can be an aggregate root in one object graph and a child in another. The same class may have `[Remote]` methods for aggregate root scenarios while other methods are server-only.

**Runtime enforcement:** Non-`[Remote]` methods compile for client assemblies but fail at runtime with a "not-registered" DI exception if called—server-only services aren't in the client container.

## Child Factories

Factories for child entities within aggregates:

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
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Domain/FactorySamples.cs#L521-L564' title='Snippet source file'>snippet source</a> | <a href='#snippet-remotefactory-child-factories' title='Start of snippet'>anchor</a></sup>
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

## Generated Interface and Implementation

The generator creates:

**Interface:**
<!-- snippet: remotefactory-generated-interface -->
<a id='snippet-remotefactory-generated-interface'></a>
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
<sup><a href='/src/docs/samples/RemoteFactorySamples.cs#L613-L634' title='Snippet source file'>snippet source</a> | <a href='#snippet-remotefactory-generated-interface' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Implementation:**
<!-- snippet: remotefactory-generated-implementation -->
<a id='snippet-remotefactory-generated-implementation'></a>
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
<sup><a href='/src/docs/samples/RemoteFactorySamples.cs#L636-L651' title='Snippet source file'>snippet source</a> | <a href='#snippet-remotefactory-generated-implementation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Dependency Injection Setup

Register factory services:

<!-- snippet: remotefactory-di-setup -->
<a id='snippet-remotefactory-di-setup'></a>
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
<sup><a href='/src/docs/samples/RemoteFactorySamples.cs#L697-L710' title='Snippet source file'>snippet source</a> | <a href='#snippet-remotefactory-di-setup' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Startup:**
<!-- snippet: remotefactory-di-startup -->
<a id='snippet-remotefactory-di-startup'></a>
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
<sup><a href='/src/docs/samples/RemoteFactorySamples.cs#L712-L730' title='Snippet source file'>snippet source</a> | <a href='#snippet-remotefactory-di-startup' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Core Services

Access Neatoo core services:

<!-- snippet: remotefactory-core-services -->
<a id='snippet-remotefactory-core-services'></a>
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
<sup><a href='/src/docs/samples/RemoteFactorySamples.cs#L732-L748' title='Snippet source file'>snippet source</a> | <a href='#snippet-remotefactory-core-services' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Factory Lifecycle

Object creation and initialization flow:

<!-- snippet: remotefactory-lifecycle -->
<a id='snippet-remotefactory-lifecycle'></a>
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
<sup><a href='/src/docs/samples/RemoteFactorySamples.cs#L750-L767' title='Snippet source file'>snippet source</a> | <a href='#snippet-remotefactory-lifecycle' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Related

- [Entities](entities.md) - Entity lifecycle with factories
- [Authorization](authorization.md) - CanCreate, CanFetch, CanSave
- [Source Generation](source-generation.md) - What gets generated
