using Neatoo;
using Neatoo.RemoteFactory;
using System.ComponentModel.DataAnnotations;

namespace Samples;

// =============================================================================
// FACTORY SAMPLES - Demonstrates factory methods and entity lifecycle
// =============================================================================

// -----------------------------------------------------------------------------
// Basic Factory Setup
// -----------------------------------------------------------------------------

// Generic repository interface for skill snippets
public interface IRepository
{
    Task InsertAsync();
    Task UpdateAsync();
    Task DeleteAsync();
}

#region skill-factory-methods
[Factory]
public partial class SkillFactoryEmployee : EntityBase<SkillFactoryEmployee>
{
    public SkillFactoryEmployee(IEntityBaseServices<SkillFactoryEmployee> services) : base(services) { }

    public partial int Id { get; set; }
    public partial string Name { get; set; }

    [Create]
    public void Create() { /* Initialize new */ }

    [Fetch]
    public void Fetch(int id, string name) { Id = id; Name = name; }

    [Insert]
    public async Task InsertAsync([Service] IRepository repo) { /* Save new */ await Task.CompletedTask; }

    [Update]
    public async Task UpdateAsync([Service] IRepository repo) { /* Save changes */ await Task.CompletedTask; }

    [Delete]
    public async Task DeleteAsync([Service] IRepository repo) { /* Remove */ await Task.CompletedTask; }
}
#endregion

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

// -----------------------------------------------------------------------------
// Fetch with Service Injection
// -----------------------------------------------------------------------------

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

// -----------------------------------------------------------------------------
// Multiple Fetch Overloads
// -----------------------------------------------------------------------------

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

// -----------------------------------------------------------------------------
// Save Routing
// -----------------------------------------------------------------------------

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

// -----------------------------------------------------------------------------
// Save Validation
// -----------------------------------------------------------------------------

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

// -----------------------------------------------------------------------------
// Delete Pattern
// -----------------------------------------------------------------------------

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

// -----------------------------------------------------------------------------
// Service Injection
// -----------------------------------------------------------------------------

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

// SKILL.md snippet - just the method pattern
[Factory]
public partial class SkillServiceInjectionExample : EntityBase<SkillServiceInjectionExample>
{
    public SkillServiceInjectionExample(IEntityBaseServices<SkillServiceInjectionExample> services) : base(services) { }

    public partial Guid Id { get; set; }
    public partial string Name { get; set; }

    #region skill-service-injection
    [Fetch]
    public async Task Fetch(Guid id, [Service] ISkillEmployeeRepository repo)
    {
        var data = await repo.FetchByIdAsync((int)id.GetHashCode());
        // Map data to properties
    }
    #endregion

    [Create]
    public void Create() { }
}

// -----------------------------------------------------------------------------
// Remote Execution
// -----------------------------------------------------------------------------

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

// SKILL.md snippet - concise remote vs local pattern
// Child repository interface for the skill snippet
public interface ISkillChildRepository { }

// Demonstrates aggregate root with [Remote] vs child without
[Factory]
public partial class SkillRemoteAggregateRoot : EntityBase<SkillRemoteAggregateRoot>
{
    public SkillRemoteAggregateRoot(IEntityBaseServices<SkillRemoteAggregateRoot> services) : base(services) { }

    public partial Guid Id { get; set; }

    #region skill-remote-execution
    // Aggregate root - needs [Remote] because it's called from client
    [Remote]
    [Fetch]
    public async Task Fetch(Guid id, [Service] ISkillEmployeeRepository repo) { await Task.CompletedTask; }
    #endregion

    [Create]
    public void Create() { }
}

// Child entity demonstrating no [Remote] needed
[Factory]
public partial class SkillRemoteChildEntity : EntityBase<SkillRemoteChildEntity>
{
    public SkillRemoteChildEntity(IEntityBaseServices<SkillRemoteChildEntity> services) : base(services) { }

    public partial int Id { get; set; }
    public partial string Name { get; set; }

    #region skill-remote-child
    // Child entity - no [Remote] needed, called from server-side parent
    [Fetch]
    public void Fetch(int id, string name, [Service] ISkillChildRepository repo) { }
    #endregion

    [Create]
    public void Create() { }
}

// -----------------------------------------------------------------------------
// Child Factories (Aggregate Loading)
// -----------------------------------------------------------------------------

/// <summary>
/// Order item child entity.
/// </summary>
public interface ISkillFactoryOrderItem : IEntityBase
{
    int Id { get; set; }
    string ProductCode { get; set; }
    decimal Price { get; set; }
    int Quantity { get; set; }
}

[Factory]
public partial class SkillFactoryOrderItem : EntityBase<SkillFactoryOrderItem>, ISkillFactoryOrderItem
{
    public SkillFactoryOrderItem(IEntityBaseServices<SkillFactoryOrderItem> services) : base(services) { }

    public partial int Id { get; set; }
    public partial string ProductCode { get; set; }
    public partial decimal Price { get; set; }
    public partial int Quantity { get; set; }

    [Create]
    public void Create() { }

    [Fetch]
    public void Fetch(int id, string productCode, decimal price, int quantity)
    {
        Id = id;
        ProductCode = productCode;
        Price = price;
        Quantity = quantity;
    }
}

public interface ISkillFactoryOrderItemList : IEntityListBase<ISkillFactoryOrderItem> { }

public class SkillFactoryOrderItemList : EntityListBase<ISkillFactoryOrderItem>, ISkillFactoryOrderItemList { }

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

// -----------------------------------------------------------------------------
// Repository and Service Interfaces (for samples above)
// -----------------------------------------------------------------------------

public interface ISkillCustomerRepository
{
    Task<CustomerData?> FetchByIdAsync(int id);
    Task InsertAsync(int id, string name, string email);
    Task UpdateAsync(int id, string name, string email);
    Task DeleteAsync(int id);
}

public interface ISkillProductRepository
{
    Task<ProductData?> FetchByIdAsync(int id);
}

public record ProductData(int Id, string Name, decimal Price);

public interface ISkillOrderRepository
{
    Task<OrderData?> FetchByCustomerEmailAsync(string email);
    Task InsertOrderAsync(int id, int quantity, decimal unitPrice);
}

public record OrderData(int Id, string OrderNumber, DateTime OrderDate);

public interface ISkillAccountRepository
{
    Task InsertAsync(int id, string name, decimal balance);
    Task UpdateAsync(int id, string name, decimal balance);
    Task DeleteAsync(int id);
}

public interface ISkillProjectRepository
{
    Task DeleteAsync(int id);
}

public interface ISkillReportRepository
{
    Task<ReportMetadata> FetchMetadataAsync(int id);
}

public record ReportMetadata(int Id, string Name);

public interface ISkillReportGenerator
{
    Task<byte[]> GenerateAsync(int id);
}

public interface ISkillDataRepository
{
    Task<DataRecord> FetchAsync(int id);
    Task InsertAsync(int id, string data);
}

public record DataRecord(int Id, string Data);

public interface ISkillOrderWithItemsRepository
{
    Task<OrderData> FetchAsync(int id);
    Task<List<OrderItemData>> FetchItemsAsync(int orderId);
}

public record OrderItemData(int Id, string ProductCode, decimal Price, int Quantity);
