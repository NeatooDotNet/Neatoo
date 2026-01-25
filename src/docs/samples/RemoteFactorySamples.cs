using Neatoo;
using Neatoo.Internal;
using Neatoo.RemoteFactory;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Principal;
using System.Text.Json;
using Xunit;

namespace Samples;

// -----------------------------------------------------------------
// Mock repositories and services for RemoteFactory samples
// -----------------------------------------------------------------

/// <summary>
/// Data record for customer persistence.
/// </summary>
public record CustomerData(int Id, string Name, string Email);

/// <summary>
/// Generic mock repository for customer-like entities.
/// </summary>
public interface IRfCustomerRepository
{
    Task<CustomerData?> FetchByIdAsync(int id);
    Task<CustomerData?> FetchByEmailAsync(string email);
    Task InsertAsync(int id, string name, string email);
    Task UpdateAsync(int id, string name, string email);
    Task DeleteAsync(int id);
}

public class MockRfCustomerRepository : IRfCustomerRepository
{
    private static readonly Dictionary<int, CustomerData> _store = new()
    {
        { 1, new CustomerData(1, "Acme Corp", "contact@acme.com") },
        { 2, new CustomerData(2, "TechStart Inc", "info@techstart.com") }
    };

    public Task<CustomerData?> FetchByIdAsync(int id)
    {
        if (_store.TryGetValue(id, out var data))
            return Task.FromResult<CustomerData?>(data);
        return Task.FromResult<CustomerData?>(null);
    }

    public Task<CustomerData?> FetchByEmailAsync(string email)
    {
        var match = _store.Values.FirstOrDefault(x => x.Email == email);
        return Task.FromResult<CustomerData?>(match);
    }

    public Task InsertAsync(int id, string name, string email) => Task.CompletedTask;
    public Task UpdateAsync(int id, string name, string email) => Task.CompletedTask;
    public Task DeleteAsync(int id) => Task.CompletedTask;
}

/// <summary>
/// Mock repository for Order entities.
/// </summary>
public interface IRfOrderRepository
{
    Task<(int Id, string OrderNumber, DateTime OrderDate)> FetchAsync(int id);
    Task<List<(int Id, string ProductCode, decimal Price, int Quantity)>> FetchItemsAsync(int orderId);
    Task InsertAsync(int id, string orderNumber, DateTime orderDate);
    Task UpdateAsync(int id, string orderNumber, DateTime orderDate);
    Task DeleteAsync(int id);
}

public class MockRfOrderRepository : IRfOrderRepository
{
    public Task<(int Id, string OrderNumber, DateTime OrderDate)> FetchAsync(int id)
    {
        return Task.FromResult((id, $"ORD-{id:D5}", DateTime.Today));
    }

    public Task<List<(int Id, string ProductCode, decimal Price, int Quantity)>> FetchItemsAsync(int orderId)
    {
        return Task.FromResult(new List<(int, string, decimal, int)>
        {
            (1, "WIDGET-001", 29.99m, 2),
            (2, "GADGET-002", 49.99m, 1)
        });
    }

    public Task InsertAsync(int id, string orderNumber, DateTime orderDate) => Task.CompletedTask;
    public Task UpdateAsync(int id, string orderNumber, DateTime orderDate) => Task.CompletedTask;
    public Task DeleteAsync(int id) => Task.CompletedTask;
}

// -----------------------------------------------------------------
// Entity classes for RemoteFactory guide samples
// -----------------------------------------------------------------

/// <summary>
/// Customer entity demonstrating factory method attributes.
/// </summary>
#region remotefactory-factory-methods
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
#endregion

/// <summary>
/// Customer entity with service injection demonstration.
/// </summary>
#region remotefactory-service-injection
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
#endregion

/// <summary>
/// Customer entity demonstrating Fetch method implementation.
/// </summary>
#region remotefactory-fetch
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
#endregion

/// <summary>
/// Customer entity demonstrating Save (Insert/Update) methods.
/// </summary>
#region remotefactory-save
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
#endregion

/// <summary>
/// Customer entity demonstrating save with validation.
/// </summary>
#region remotefactory-save-validation
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
#endregion

/// <summary>
/// Customer entity demonstrating Delete method.
/// </summary>
#region remotefactory-delete
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
#endregion

/// <summary>
/// Customer entity demonstrating [Remote] attribute for client-server execution.
/// </summary>
#region remotefactory-remote-attribute
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
#endregion

/// <summary>
/// Customer entity with multiple Fetch overloads.
/// </summary>
#region remotefactory-fetch-overloads
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
#endregion

// -----------------------------------------------------------------
// Order aggregate for child factory demonstration
// -----------------------------------------------------------------

/// <summary>
/// Order item interface for child collection.
/// </summary>
public interface IRfOrderItem : IEntityBase
{
    int Id { get; set; }
    string ProductCode { get; set; }
    decimal Price { get; set; }
    int Quantity { get; set; }
    void DoMarkOld();
    void DoMarkUnmodified();
}

/// <summary>
/// Order item entity - child of Order aggregate.
/// </summary>
[Factory]
public partial class RfOrderItem : EntityBase<RfOrderItem>, IRfOrderItem
{
    public RfOrderItem(IEntityBaseServices<RfOrderItem> services) : base(services) { }

    public partial int Id { get; set; }
    public partial string ProductCode { get; set; }
    public partial decimal Price { get; set; }
    public partial int Quantity { get; set; }

    [Create]
    public void Create()
    {
        Id = 0;
        ProductCode = "";
        Price = 0m;
        Quantity = 0;
    }

    public void DoMarkOld() => MarkOld();
    public void DoMarkUnmodified() => MarkUnmodified();
}

/// <summary>
/// Order item list for aggregate child collection.
/// </summary>
public interface IRfOrderItemList : IEntityListBase<IRfOrderItem>
{
}

public class RfOrderItemList : EntityListBase<IRfOrderItem>, IRfOrderItemList
{
}

/// <summary>
/// Order aggregate root demonstrating child factory usage.
/// </summary>
#region remotefactory-child-factories
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
#endregion

// -----------------------------------------------------------------
// Authorization demonstration
// -----------------------------------------------------------------

/// <summary>
/// Authorization handler for Customer operations.
/// </summary>
public interface IRfCustomerAuth
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    bool CanCreate();

    [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
    bool CanFetch();

    [AuthorizeFactory(AuthorizeFactoryOperation.Read | AuthorizeFactoryOperation.Write)]
    bool HasFullAccess();
}

/// <summary>
/// Authorization implementation checking user roles.
/// </summary>
public class RfCustomerAuth : IRfCustomerAuth
{
    private readonly IPrincipal _principal;

    public RfCustomerAuth(IPrincipal principal)
    {
        _principal = principal;
    }

    public bool CanCreate() => _principal.IsInRole("Admin") || _principal.IsInRole("Manager");
    public bool CanFetch() => _principal.Identity?.IsAuthenticated ?? false;
    public bool HasFullAccess() => _principal.IsInRole("Admin");
}

/// <summary>
/// Customer entity with factory authorization.
/// </summary>
#region remotefactory-authorization
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
#endregion

// -----------------------------------------------------------------
// Test classes for RemoteFactory guide samples
// -----------------------------------------------------------------

public class RemoteFactorySamplesTests
{
    #region remotefactory-generated-interface
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
    #endregion

    #region remotefactory-generated-implementation
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
    #endregion

    #region remotefactory-serialization
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
    #endregion

    #region remotefactory-dto-pattern
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
    #endregion

    #region remotefactory-di-setup
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
    #endregion

    #region remotefactory-di-startup
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
    #endregion

    #region remotefactory-core-services
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
    #endregion

    #region remotefactory-lifecycle
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
    #endregion

    [Fact]
    public void FactoryMethods_DeclaredOnEntity()
    {
        // Entity declares lifecycle methods with attributes
        var customer = new RfCustomer(new EntityBaseServices<RfCustomer>());

        // Factory would call Create() and manage lifecycle
        customer.FactoryStart(FactoryOperation.Create);
        customer.Create();
        customer.FactoryComplete(FactoryOperation.Create);

        Assert.True(customer.IsNew);
        Assert.Equal(0, customer.Id);
        Assert.Equal("", customer.Name);
    }

    [Fact]
    public async Task ServiceInjection_ResolvedAtRuntime()
    {
        var services = new ServiceCollection();
        services.AddNeatooServices(NeatooFactory.Logical, typeof(RfCustomerWithServices).Assembly);
        services.AddTransient<IRfCustomerRepository, MockRfCustomerRepository>();
        RfCustomerWithServicesFactory.FactoryServiceRegistrar(services, NeatooFactory.Logical);

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IRfCustomerWithServicesFactory>();

        // Factory resolves [Service] parameters from DI
        var customer = await factory.FetchAsync(1);

        Assert.Equal(1, customer.Id);
        Assert.Equal("Acme Corp", customer.Name);
    }

    [Fact]
    public async Task Fetch_LoadsEntityState()
    {
        var services = new ServiceCollection();
        services.AddNeatooServices(NeatooFactory.Logical, typeof(RfCustomerFetch).Assembly);
        services.AddTransient<IRfCustomerRepository, MockRfCustomerRepository>();
        RfCustomerFetchFactory.FactoryServiceRegistrar(services, NeatooFactory.Logical);

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IRfCustomerFetchFactory>();

        // Factory wraps Fetch with PauseAllActions
        var customer = await factory.FetchAsync(1);

        // After Fetch: entity is existing and clean
        Assert.False(customer.IsNew);
        Assert.False(customer.IsModified);
        Assert.Equal("Acme Corp", customer.Name);
    }

    [Fact]
    public async Task Save_RoutesToInsertOrUpdate()
    {
        var services = new ServiceCollection();
        services.AddNeatooServices(NeatooFactory.Logical, typeof(RfCustomerSave).Assembly);
        services.AddTransient<IRfCustomerRepository, MockRfCustomerRepository>();
        RfCustomerSaveFactory.FactoryServiceRegistrar(services, NeatooFactory.Logical);

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IRfCustomerSaveFactory>();

        // Create new customer
        var customer = factory.Create();
        Assert.True(customer.IsNew);

        customer.Name = "New Customer";
        customer.Email = "new@example.com";

        // Save routes to Insert for new entities
        var saved = await factory.SaveAsync(customer);
        Assert.NotNull(saved);
        Assert.False(saved.IsNew); // After Insert, no longer new
    }

    [Fact]
    public async Task Delete_MarksAndRemoves()
    {
        var services = new ServiceCollection();
        services.AddNeatooServices(NeatooFactory.Logical, typeof(RfCustomerDelete).Assembly);
        services.AddTransient<IRfCustomerRepository, MockRfCustomerRepository>();
        RfCustomerDeleteFactory.FactoryServiceRegistrar(services, NeatooFactory.Logical);

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IRfCustomerDeleteFactory>();

        // Fetch existing customer
        var customer = await factory.FetchAsync(1);
        Assert.False(customer.IsDeleted);

        // Mark for deletion
        customer.Delete();
        Assert.True(customer.IsDeleted);
        Assert.True(customer.IsModified);

        // Save routes to Delete method
        var result = await factory.SaveAsync(customer);
        // After delete, entity is removed
    }

    [Fact]
    public async Task MultipleFetchOverloads_SupportDifferentQueries()
    {
        var services = new ServiceCollection();
        services.AddNeatooServices(NeatooFactory.Logical, typeof(RfCustomerMultiFetch).Assembly);
        services.AddTransient<IRfCustomerRepository, MockRfCustomerRepository>();
        RfCustomerMultiFetchFactory.FactoryServiceRegistrar(services, NeatooFactory.Logical);

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IRfCustomerMultiFetchFactory>();

        // Fetch by ID
        var byId = await factory.FetchById(1);
        Assert.Equal("Acme Corp", byId.Name);

        // Fetch by email
        var byEmail = await factory.FetchByEmail("contact@acme.com");
        Assert.Equal(1, byEmail.Id);
    }

    [Fact]
    public async Task ChildFactories_LoadAggregateGraph()
    {
        var services = new ServiceCollection();
        services.AddNeatooServices(NeatooFactory.Logical, typeof(RfOrder).Assembly);
        services.AddTransient<IRfOrderRepository, MockRfOrderRepository>();
        RfOrderFactory.FactoryServiceRegistrar(services, NeatooFactory.Logical);
        RfOrderItemFactory.FactoryServiceRegistrar(services, NeatooFactory.Logical);

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IRfOrderFactory>();

        // Parent factory uses child factory to load items
        var order = await factory.FetchAsync(1);

        Assert.Equal("ORD-00001", order.OrderNumber);
        Assert.Equal(2, order.Items.Count);
        Assert.Equal("WIDGET-001", order.Items[0].ProductCode);

        // Child entities have parent relationship
        Assert.Same(order, order.Items[0].Parent);
    }

    [Fact]
    public void RemoteAttribute_MarksServerMethods()
    {
        // [Remote] attribute marks methods for server execution
        var customer = new RfCustomerRemote(new EntityBaseServices<RfCustomerRemote>());

        // Create executes locally (no [Remote])
        customer.FactoryStart(FactoryOperation.Create);
        customer.Create();
        customer.FactoryComplete(FactoryOperation.Create);

        Assert.True(customer.IsNew);

        // In NeatooFactory.Remote mode:
        // - Fetch, Insert, Update, Delete execute on server
        // - Create executes locally on client
    }

    [Fact]
    public void Authorization_ChecksBeforeExecution()
    {
        // [AuthorizeFactory<IRfCustomerAuth>] on entity class
        // IRfCustomerAuth methods check permissions

        var auth = new RfCustomerAuth(new GenericPrincipal(
            new GenericIdentity("testuser"),
            new[] { "Admin" }));

        // Admin can create
        Assert.True(auth.CanCreate());
        Assert.True(auth.CanFetch());
        Assert.True(auth.HasFullAccess());

        // Non-admin cannot create
        var limitedAuth = new RfCustomerAuth(new GenericPrincipal(
            new GenericIdentity("viewer"),
            new[] { "Viewer" }));

        Assert.False(limitedAuth.CanCreate());
        Assert.True(limitedAuth.CanFetch()); // Authenticated
        Assert.False(limitedAuth.HasFullAccess());
    }
}
