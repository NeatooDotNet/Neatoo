using Neatoo;
using Neatoo.Internal;
using Neatoo.RemoteFactory;
using Neatoo.Rules;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace Samples;

// -----------------------------------------------------------------
// Entity classes for API Reference samples
// -----------------------------------------------------------------

/// <summary>
/// Entity demonstrating partial property declarations for ValidateBase.
/// </summary>
#region api-validatebase-partial-properties
[Factory]
public partial class ApiCustomer : ValidateBase<ApiCustomer>
{
    public ApiCustomer(IValidateBaseServices<ApiCustomer> services) : base(services) { }

    // Partial properties - source generator creates backing fields and implementation
    public partial string Name { get; set; }

    public partial string Email { get; set; }

    public partial DateTime BirthDate { get; set; }

    [Create]
    public void Create() { }
}
#endregion

/// <summary>
/// Entity demonstrating property access patterns.
/// </summary>
[Factory]
public partial class ApiCustomerSearch : ValidateBase<ApiCustomerSearch>
{
    public ApiCustomerSearch(IValidateBaseServices<ApiCustomerSearch> services) : base(services) { }

    public partial string SearchTerm { get; set; }

    public partial string Category { get; set; }

    [Create]
    public void Create() { }
}

/// <summary>
/// Entity demonstrating RuleManager usage.
/// </summary>
#region api-validatebase-rulemanager
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

    [Create]
    public void Create() { }
}
#endregion

/// <summary>
/// Mock repository for API samples.
/// </summary>
public interface IApiCustomerRepository
{
    Task<(int Id, string Name, string Email)> FetchAsync(int id);
    Task InsertAsync(int id, string name, string email);
    Task UpdateAsync(int id, string name, string email);
    Task DeleteAsync(int id);
}

public class MockApiCustomerRepository : IApiCustomerRepository
{
    public Task<(int Id, string Name, string Email)> FetchAsync(int id)
    {
        return Task.FromResult((id, $"Customer {id}", $"customer{id}@example.com"));
    }

    public Task InsertAsync(int id, string name, string email) => Task.CompletedTask;
    public Task UpdateAsync(int id, string name, string email) => Task.CompletedTask;
    public Task DeleteAsync(int id) => Task.CompletedTask;
}

/// <summary>
/// Entity demonstrating EntityBase persistence state.
/// </summary>
[Factory]
public partial class ApiEmployee : EntityBase<ApiEmployee>
{
    public ApiEmployee(IEntityBaseServices<ApiEmployee> services) : base(services) { }

    public partial int Id { get; set; }

    public partial string Name { get; set; }

    public partial string Department { get; set; }

    [Create]
    public void Create() { }

    [Fetch]
    public void Fetch(int id, string name, string department)
    {
        Id = id;
        Name = name;
        Department = department;
    }
}

/// <summary>
/// Entity demonstrating full EntityBase functionality with factory methods.
/// </summary>
#region api-entitybase-save
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
#endregion

/// <summary>
/// Child entity for aggregate pattern samples.
/// </summary>
public interface IApiOrderItem : IEntityBase
{
    string ProductCode { get; set; }
    decimal Price { get; set; }
    int Quantity { get; set; }
}

[Factory]
public partial class ApiOrderItem : EntityBase<ApiOrderItem>, IApiOrderItem
{
    public ApiOrderItem(IEntityBaseServices<ApiOrderItem> services) : base(services) { }

    public partial string ProductCode { get; set; }

    public partial decimal Price { get; set; }

    public partial int Quantity { get; set; }

    [Create]
    public void Create() { }

    [Fetch]
    public void Fetch(string productCode, decimal price, int quantity)
    {
        ProductCode = productCode;
        Price = price;
        Quantity = quantity;
    }
}

/// <summary>
/// Entity list for aggregate samples.
/// </summary>
public interface IApiOrderItemList : IEntityListBase<IApiOrderItem>
{
    int DeletedCount { get; }
}

public class ApiOrderItemList : EntityListBase<IApiOrderItem>, IApiOrderItemList
{
    public int DeletedCount => DeletedList.Count;
}

/// <summary>
/// Order aggregate root for parent-child samples.
/// </summary>
[Factory]
public partial class ApiOrder : EntityBase<ApiOrder>
{
    public ApiOrder(IEntityBaseServices<ApiOrder> services) : base(services)
    {
        ItemsProperty.LoadValue(new ApiOrderItemList());
    }

    public partial int Id { get; set; }

    public partial string OrderNumber { get; set; }

    public partial IApiOrderItemList Items { get; set; }

    // Expose protected method for samples
    public void DoMarkUnmodified() => MarkUnmodified();

    [Create]
    public void Create() { }

    [Fetch]
    public void Fetch(int id, string orderNumber)
    {
        Id = id;
        OrderNumber = orderNumber;
    }
}

/// <summary>
/// Validate item for ValidateListBase samples.
/// </summary>
public interface IApiValidateItem : IValidateBase
{
    string Name { get; set; }
    int Value { get; set; }
}

[Factory]
public partial class ApiValidateItem : ValidateBase<ApiValidateItem>, IApiValidateItem
{
    public ApiValidateItem(IValidateBaseServices<ApiValidateItem> services) : base(services)
    {
        RuleManager.AddValidation(
            item => !string.IsNullOrEmpty(item.Name) ? "" : "Name is required",
            i => i.Name);
    }

    public partial string Name { get; set; }

    public partial int Value { get; set; }

    [Create]
    public void Create() { }
}

/// <summary>
/// ValidateListBase for validation samples.
/// </summary>
public class ApiValidateItemList : ValidateListBase<IApiValidateItem>
{
}

/// <summary>
/// Parent entity for child collection samples.
/// </summary>
[Factory]
public partial class ApiAddress : ValidateBase<ApiAddress>
{
    public ApiAddress(IValidateBaseServices<ApiAddress> services) : base(services)
    {
        ItemsProperty.LoadValue(new ApiValidateItemList());
    }

    public partial string Street { get; set; }

    public partial ApiValidateItemList Items { get; set; }

    [Create]
    public void Create() { }
}

/// <summary>
/// Entity demonstrating MarkInvalid usage.
/// </summary>
[Factory]
public partial class ApiTransaction : ValidateBase<ApiTransaction>
{
    public ApiTransaction(IValidateBaseServices<ApiTransaction> services) : base(services) { }

    public partial string TransactionId { get; set; }

    public partial decimal Amount { get; set; }

    public void MarkTransactionInvalid(string reason)
    {
        MarkInvalid(reason);
    }

    [Create]
    public void Create() { }
}

/// <summary>
/// Entity demonstrating async task tracking.
/// </summary>
[Factory]
public partial class ApiAsyncContact : ValidateBase<ApiAsyncContact>
{
    public ApiAsyncContact(
        IValidateBaseServices<ApiAsyncContact> services,
        IPricingService? pricingService = null) : base(services)
    {
        if (pricingService != null)
        {
            RuleManager.AddActionAsync(
                async contact =>
                {
                    if (!string.IsNullOrEmpty(contact.ZipCode))
                    {
                        contact.TaxRate = await pricingService.GetTaxRateAsync(contact.ZipCode);
                    }
                },
                c => c.ZipCode);
        }
    }

    public partial string Name { get; set; }

    public partial string ZipCode { get; set; }

    public partial decimal TaxRate { get; set; }

    [Create]
    public void Create() { }
}

/// <summary>
/// Entity demonstrating standard validation attributes.
/// </summary>
#region api-attributes-validation
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

    [Create]
    public void Create() { }
}
#endregion

/// <summary>
/// Entity demonstrating [Factory] attribute.
/// </summary>
#region api-attributes-factory
[Factory]
public partial class ApiProduct : ValidateBase<ApiProduct>
{
    public ApiProduct(IValidateBaseServices<ApiProduct> services) : base(services) { }

    public partial string Name { get; set; }

    public partial decimal Price { get; set; }

    [Create]
    public void Create() { }
}
#endregion

/// <summary>
/// Entity demonstrating Create attribute.
/// </summary>
#region api-attributes-create
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
#endregion

/// <summary>
/// Entity demonstrating Fetch attribute.
/// </summary>
#region api-attributes-fetch
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
#endregion

/// <summary>
/// Entity demonstrating Insert attribute.
/// </summary>
#region api-attributes-insert
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
#endregion

/// <summary>
/// Entity demonstrating Update attribute.
/// </summary>
#region api-attributes-update
[Factory]
public partial class ApiLead : EntityBase<ApiLead>
{
    public ApiLead(IEntityBaseServices<ApiLead> services) : base(services) { }

    public partial int Id { get; set; }

    public partial string LeadName { get; set; }

    [Create]
    public void Create() { }

    [Fetch]
    public void Fetch(int id, string leadName)
    {
        Id = id;
        LeadName = leadName;
    }

    [Update]
    public async Task UpdateAsync([Service] IApiCustomerRepository repository)
    {
        await repository.UpdateAsync(Id, LeadName, "");
    }
}
#endregion

/// <summary>
/// Entity demonstrating Delete attribute.
/// </summary>
#region api-attributes-delete
[Factory]
public partial class ApiProject : EntityBase<ApiProject>
{
    public ApiProject(IEntityBaseServices<ApiProject> services) : base(services) { }

    public partial int Id { get; set; }

    public partial string ProjectName { get; set; }

    [Create]
    public void Create() { }

    [Fetch]
    public void Fetch(int id, string projectName)
    {
        Id = id;
        ProjectName = projectName;
    }

    [Delete]
    public async Task DeleteAsync([Service] IApiCustomerRepository repository)
    {
        await repository.DeleteAsync(Id);
    }
}
#endregion

/// <summary>
/// Entity demonstrating Service attribute.
/// </summary>
#region api-attributes-service
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
#endregion

/// <summary>
/// Entity demonstrating SuppressFactory attribute.
/// </summary>
#region api-attributes-suppressfactory
[SuppressFactory]
public class ApiTestObject : ValidateBase<ApiTestObject>
{
    public ApiTestObject(IValidateBaseServices<ApiTestObject> services) : base(services) { }

    public string Name { get => Getter<string>(); set => Setter(value); }
}
#endregion

/// <summary>
/// Entity demonstrating partial property generation.
/// </summary>
#region api-generator-partial-property
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

    [Create]
    public void Create() { }
}
#endregion

/// <summary>
/// Entity demonstrating factory method generation.
/// </summary>
#region api-generator-factory-methods
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
#endregion

/// <summary>
/// Entity demonstrating save factory generation.
/// </summary>
#region api-generator-save-factory
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
#endregion

/// <summary>
/// Entity demonstrating RuleIdRegistry generation.
/// </summary>
#region api-generator-ruleid
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

    [Create]
    public void Create() { }
}
#endregion

// -----------------------------------------------------------------
// Test classes for API Reference samples
// -----------------------------------------------------------------

public class ApiReferenceSamplesTests : SamplesTestBase
{
    #region api-validatebase-property-access
    [Fact]
    public void PropertyAccess_ByNameAndIndexer()
    {
        var factory = GetRequiredService<IApiCustomerSearchFactory>();
        var search = factory.Create();
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
    #endregion

    #region api-validatebase-runrules
    [Fact]
    public async Task RunRules_ExecutesValidation()
    {
        var factory = GetRequiredService<IApiCustomerValidatorFactory>();
        var customer = factory.Create();

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
    #endregion

    #region api-validatebase-markinvalid
    [Fact]
    public void MarkInvalid_SetsObjectLevelError()
    {
        var factory = GetRequiredService<IApiTransactionFactory>();
        var transaction = factory.Create();
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
    #endregion

    #region api-validatebase-metaproperties
    [Fact]
    public void MetaProperties_ReflectValidationState()
    {
        var factory = GetRequiredService<IApiCustomerValidatorFactory>();
        var customer = factory.Create();

        // Set invalid value
        customer.Name = "";

        // Check meta-properties
        Assert.False(customer.IsValid);         // Object invalid
        Assert.False(customer.IsSelfValid);     // Own properties invalid
        Assert.False(customer.IsBusy);          // No async operations
        Assert.NotEmpty(customer.PropertyMessages);  // Has error messages
    }
    #endregion

    #region api-validatebase-parent
    [Fact]
    public void Parent_EstablishesHierarchy()
    {
        var addressFactory = GetRequiredService<IApiAddressFactory>();
        var itemFactory = GetRequiredService<IApiValidateItemFactory>();

        var address = addressFactory.Create();

        // Create child item
        var item = itemFactory.Create();
        item.Name = "Test Item";

        // Add to collection establishes parent
        address.Items.Add(item);

        // Item's parent is the address
        Assert.Same(address, item.Parent);
    }
    #endregion

    #region api-validatebase-tasks
    [Fact]
    public async Task Tasks_WaitForAsyncOperations()
    {
        var factory = GetRequiredService<IApiAsyncContactFactory>();
        var contact = factory.Create();

        contact.Name = "Test";

        // Setting ZipCode triggers async rule
        contact.ZipCode = "90210";

        // Wait for all async operations
        await contact.WaitForTasks();

        // Async rule completed
        Assert.Equal(0.0825m, contact.TaxRate);
    }
    #endregion

    #region api-validatebase-pause
    [Fact]
    public void Pause_SuppressesEventsAndRules()
    {
        var factory = GetRequiredService<IApiCustomerValidatorFactory>();
        var customer = factory.Create();

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
    #endregion

    #region api-entitybase-persistence-state
    [Fact]
    public void PersistenceState_TracksEntityLifecycle()
    {
        var factory = GetRequiredService<IApiEmployeeFactory>();

        // Create new entity
        var newEmployee = factory.Create();
        Assert.True(newEmployee.IsNew);   // New entity - will Insert on save
        Assert.False(newEmployee.IsDeleted);
        Assert.False(newEmployee.IsChild);

        // Fetch existing entity
        var existingEmployee = factory.Fetch(1, "Alice", "Engineering");
        Assert.False(existingEmployee.IsNew);  // Now existing - will Update on save

        // Mark for deletion
        existingEmployee.Delete();
        Assert.True(existingEmployee.IsDeleted);  // Will Delete on save
    }
    #endregion

    #region api-entitybase-modification
    [Fact]
    public void ModificationTracking_DetectsChanges()
    {
        var factory = GetRequiredService<IApiEmployeeFactory>();

        // Fetch existing entity
        var employee = factory.Fetch(1, "Original", "Engineering");

        Assert.False(employee.IsModified);
        Assert.False(employee.IsSelfModified);
        Assert.Empty(employee.ModifiedProperties);

        // Change property
        employee.Name = "Modified";

        Assert.True(employee.IsModified);
        Assert.True(employee.IsSelfModified);
        Assert.Contains("Name", employee.ModifiedProperties);
    }
    #endregion

    #region api-entitybase-root
    [Fact]
    public void Root_FindsAggregateRoot()
    {
        var orderFactory = GetRequiredService<IApiOrderFactory>();
        var itemFactory = GetRequiredService<IApiOrderItemFactory>();

        var order = orderFactory.Create();

        // Create child item
        var item = itemFactory.Create();
        item.ProductCode = "WIDGET-001";
        item.Price = 29.99m;

        // Add to collection
        order.Items.Add(item);

        // Root walks Parent chain to find aggregate root
        Assert.Same(order, item.Root);

        // Aggregate root has no root above it
        Assert.Null(order.Root);
    }
    #endregion

    #region api-entitybase-delete
    [Fact]
    public void Delete_MarksForDeletion()
    {
        var factory = GetRequiredService<IApiEmployeeFactory>();

        // Fetch existing entity
        var employee = factory.Fetch(42, "To Delete", "HR");

        Assert.False(employee.IsDeleted);

        // Mark for deletion
        employee.Delete();
        Assert.True(employee.IsDeleted);
        Assert.True(employee.IsModified);

        // UnDelete reverses the mark
        employee.UnDelete();
        Assert.False(employee.IsDeleted);
    }
    #endregion

    #region api-entitybase-mark-methods
    [Fact]
    public void MarkMethods_ControlEntityState()
    {
        var factory = GetRequiredService<IApiEmployeeFactory>();
        var employee = factory.Create();

        // New entity after Create
        Assert.True(employee.IsNew);

        // FactoryComplete(Insert) marks as old
        employee.FactoryComplete(FactoryOperation.Insert);
        Assert.False(employee.IsNew);

        // Mark for deletion
        employee.Delete();
        Assert.True(employee.IsDeleted);

        // UnDelete reverses
        employee.UnDelete();
        Assert.False(employee.IsDeleted);
    }
    #endregion

    #region api-validatelistbase-parent
    [Fact]
    public void ValidateListBase_ParentRelationship()
    {
        var addressFactory = GetRequiredService<IApiAddressFactory>();
        var itemFactory = GetRequiredService<IApiValidateItemFactory>();

        var address = addressFactory.Create();

        var item = itemFactory.Create();
        item.Name = "Test";

        // Add item to collection
        address.Items.Add(item);

        // Item's parent is the address (not the list)
        Assert.Same(address, item.Parent);

        // List's parent is also set
        Assert.Same(address, address.Items.Parent);
    }
    #endregion

    #region api-validatelistbase-metaproperties
    [Fact]
    public async Task ValidateListBase_AggregatesState()
    {
        var list = new ApiValidateItemList();

        var itemFactory = GetRequiredService<IApiValidateItemFactory>();

        var validItem = itemFactory.Create();
        validItem.Name = "Valid";
        await validItem.RunRules();

        var invalidItem = itemFactory.Create();
        // Name is empty - invalid
        await invalidItem.RunRules();

        list.Add(validItem);
        Assert.True(list.IsValid);

        list.Add(invalidItem);
        Assert.False(list.IsValid);    // Aggregates child state
        Assert.True(list.IsSelfValid); // Lists have no own validation
    }
    #endregion

    #region api-validatelistbase-validation
    [Fact]
    public async Task ValidateListBase_RunRulesOnAll()
    {
        var list = new ApiValidateItemList();

        var itemFactory = GetRequiredService<IApiValidateItemFactory>();

        var item1 = itemFactory.Create();
        item1.Name = "";  // Invalid

        var item2 = itemFactory.Create();
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
    #endregion

    #region api-validatelistbase-collection-ops
    [Fact]
    public void ValidateListBase_StandardOperations()
    {
        var list = new ApiValidateItemList();

        var itemFactory = GetRequiredService<IApiValidateItemFactory>();

        // Add
        var item = itemFactory.Create();
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
    #endregion

    #region api-entitylistbase-metaproperties
    [Fact]
    public void EntityListBase_ModificationFromItems()
    {
        var orderFactory = GetRequiredService<IApiOrderFactory>();
        var itemFactory = GetRequiredService<IApiOrderItemFactory>();

        // Fetch existing order (starts clean)
        var order = orderFactory.Fetch(1, "ORD-001");
        Assert.False(order.IsModified);

        // Add a new item to the collection
        var item = itemFactory.Create();
        item.ProductCode = "TEST";
        item.Price = 50.00m;
        item.Quantity = 1;
        order.Items.Add(item);

        // Collection is modified because an item was added
        Assert.True(order.Items.IsModified);

        // Order is modified because collection changed
        Assert.True(order.IsModified);

        // Lists have no own properties, so IsSelfModified is false
        Assert.False(order.Items.IsSelfModified);
    }
    #endregion

    #region api-entitylistbase-deletedlist
    [Fact]
    public void EntityListBase_TracksDeleted()
    {
        var orderFactory = GetRequiredService<IApiOrderFactory>();
        var itemFactory = GetRequiredService<IApiOrderItemFactory>();

        // Fetch existing order
        var order = orderFactory.Fetch(1, "ORD-001");

        // Fetch existing item
        var item = itemFactory.Fetch("DELETE-ME", 30.00m, 1);

        // Add fetched item to order
        order.Items.Add(item);
        order.DoMarkUnmodified();

        // Remove existing item
        order.Items.Remove(item);

        // Item is in DeletedList
        Assert.True(item.IsDeleted);
        Assert.Equal(1, order.Items.DeletedCount);
    }
    #endregion

    #region api-entitylistbase-add-remove
    [Fact]
    public void EntityListBase_AddRemoveBehavior()
    {
        var orderFactory = GetRequiredService<IApiOrderFactory>();
        var itemFactory = GetRequiredService<IApiOrderItemFactory>();

        var order = orderFactory.Create();

        // Add new item via factory
        var newItem = itemFactory.Create();
        newItem.ProductCode = "NEW-001";
        order.Items.Add(newItem);

        // Item is marked as child and is new
        Assert.True(newItem.IsChild);
        Assert.True(newItem.IsNew);
        Assert.Same(order, newItem.Parent);

        // Remove new item - not tracked (was never persisted)
        order.Items.Remove(newItem);
        Assert.Equal(0, order.Items.DeletedCount);

        // Fetch existing item
        var existingItem = itemFactory.Fetch("EXIST-001", 25.00m, 1);

        // Add fetched item
        order.Items.Add(existingItem);
        order.DoMarkUnmodified();

        // Remove existing item - tracked for deletion
        order.Items.Remove(existingItem);
        Assert.Equal(1, order.Items.DeletedCount);
        Assert.True(existingItem.IsDeleted);
    }
    #endregion

    #region api-interfaces-ivalidatebase
    [Fact]
    public void IValidateBase_CoreValidationInterface()
    {
        var factory = GetRequiredService<IApiCustomerFactory>();
        IValidateBase customer = factory.Create();

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
    #endregion

    #region api-interfaces-ientitybase
    [Fact]
    public void IEntityBase_EntityInterface()
    {
        var factory = GetRequiredService<IApiEmployeeFactory>();
        IEntityBase employee = factory.Create();

        // IEntityBase adds persistence properties
        Assert.True(employee.IsNew);  // After Create, IsNew is true
        Assert.False(employee.IsDeleted);
        Assert.False(employee.IsChild);
        Assert.True(employee.IsModified);  // New entity is considered modified

        // Delete and UnDelete methods
        employee.Delete();
        Assert.True(employee.IsDeleted);

        employee.UnDelete();
        Assert.False(employee.IsDeleted);

        // Root property
        Assert.Null(employee.Root);
    }
    #endregion

    #region api-interfaces-ivalidateproperty
    [Fact]
    public async Task IValidateProperty_PropertyInterface()
    {
        var factory = GetRequiredService<IApiCustomerFactory>();
        var customer = factory.Create();
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
    #endregion

    #region api-interfaces-ipropertyinfo
    [Fact]
    public void IPropertyInfo_PropertyMetadata()
    {
        var factory = GetRequiredService<IApiCustomerFactory>();
        var customer = factory.Create();

        // Access property metadata through IValidateProperty
        var property = customer["Name"];

        // IPropertyInfo provides metadata about the property
        Assert.Equal("Name", property.Name);
        Assert.Equal(typeof(string), property.Type);
    }
    #endregion

    #region api-interfaces-imetaproperties
    [Fact]
    public void IMetaProperties_ValidationAndEntityState()
    {
        var customerFactory = GetRequiredService<IApiCustomerFactory>();
        var employeeFactory = GetRequiredService<IApiEmployeeFactory>();

        // IValidateMetaProperties - validation state
        IValidateMetaProperties validateMeta = customerFactory.Create();
        Assert.True(validateMeta.IsValid);
        Assert.True(validateMeta.IsSelfValid);
        Assert.False(validateMeta.IsBusy);
        Assert.Empty(validateMeta.PropertyMessages);

        // IEntityMetaProperties - adds entity state
        IEntityMetaProperties entityMeta = employeeFactory.Create();
        Assert.True(entityMeta.IsNew);  // After Create
        Assert.False(entityMeta.IsDeleted);
        Assert.False(entityMeta.IsChild);
        Assert.True(entityMeta.IsModified);  // New entity
        Assert.False(entityMeta.IsSelfModified);
        Assert.False(entityMeta.IsMarkedModified);
        Assert.True(entityMeta.IsSavable);  // New entity is savable
    }
    #endregion

    [Fact]
    public void FactoryAttribute_EnablesSourceGeneration()
    {
        var factory = GetRequiredService<IApiProductFactory>();

        // [Factory] attribute marks class for factory generation
        var product = factory.Create();
        product.Name = "Widget";
        product.Price = 19.99m;

        Assert.Equal("Widget", product.Name);
        Assert.Equal(19.99m, product.Price);
    }

    [Fact]
    public void CreateAttribute_InitializesNewEntity()
    {
        var factory = GetRequiredService<IApiInvoiceFactory>();
        var invoice = factory.Create();

        Assert.True(invoice.IsNew);
        Assert.Equal(0, invoice.Id);
        Assert.Contains("INV-", invoice.InvoiceNumber);
    }

    [Fact]
    public async Task FetchAttribute_LoadsExistingEntity()
    {
        var factory = GetRequiredService<IApiContactFactory>();

        var contact = await factory.FetchAsync(1);

        Assert.False(contact.IsNew);
        Assert.Equal(1, contact.Id);
        Assert.Equal("Customer 1", contact.Name);
    }

    [Fact]
    public async Task InsertAttribute_PersistsNewEntity()
    {
        var factory = GetRequiredService<IApiAccountFactory>();

        var account = factory.Create();
        account.AccountName = "New Account";

        // Save routes to Insert for new entities
        await factory.SaveAsync(account);
    }

    [Fact]
    public async Task UpdateAttribute_PersistsChanges()
    {
        var factory = GetRequiredService<IApiLeadFactory>();

        var lead = factory.Fetch(1, "Original Lead");

        lead.LeadName = "Updated Lead";

        // Save routes to Update for existing entities
        await factory.SaveAsync(lead);
    }

    [Fact]
    public async Task DeleteAttribute_RemovesEntity()
    {
        var factory = GetRequiredService<IApiProjectFactory>();

        var project = factory.Fetch(1, "Project 1");

        project.Delete();

        // Save routes to Delete
        await factory.SaveAsync(project);
    }

    [Fact]
    public async Task ServiceAttribute_InjectsFromDI()
    {
        var factory = GetRequiredService<IApiReportFactory>();

        // [Service] parameter is resolved from DI container
        var report = await factory.FetchAsync(1);

        Assert.Equal(1, report.Id);
        Assert.Equal("Customer 1", report.ReportName);
    }

    [Fact]
    public void SuppressFactoryAttribute_PreventsGeneration()
    {
        // [SuppressFactory] prevents factory method generation
        // Used for test classes that inherit from Neatoo base classes
        // Since no factory exists, resolve services from DI and construct directly
        var services = GetRequiredService<IValidateBaseServices<ApiTestObject>>();
        var testObj = new ApiTestObject(services);
        testObj.Name = "Test";

        Assert.Equal("Test", testObj.Name);
    }

    [Fact]
    public void ValidationAttributes_AutoConverted()
    {
        var factory = GetRequiredService<IApiRegistrationFactory>();
        var registration = factory.Create();

        // [Required]
        registration.Username = "";
        Assert.False(registration["Username"].IsValid);

        registration.Username = "validuser";
        Assert.True(registration["Username"].IsValid);

        // [EmailAddress]
        registration.Email = "invalid";
        Assert.False(registration["Email"].IsValid);

        registration.Email = "valid@example.com";
        Assert.True(registration["Email"].IsValid);

        // [Range]
        registration.Age = 10;
        Assert.False(registration["Age"].IsValid);

        registration.Age = 25;
        Assert.True(registration["Age"].IsValid);
    }

    [Fact]
    public void GeneratedPartialProperty_HasBackingField()
    {
        var factory = GetRequiredService<IApiGeneratedCustomerFactory>();
        var customer = factory.Create();

        // Setter goes through generated backing field
        customer.Name = "Generated";

        // Getter returns backing field value
        Assert.Equal("Generated", customer.Name);

        // Backing field accessible via indexer
        var property = customer["Name"];
        Assert.Equal("Generated", property.Value);
    }

    [Fact]
    public void GeneratedFactoryMethods_ManageLifecycle()
    {
        var factory = GetRequiredService<IApiGeneratedEntityFactory>();

        // Source generator creates factory methods
        var entity = factory.Create();

        Assert.True(entity.IsNew);
        Assert.Equal(0, entity.Id);
    }

    [Fact]
    public async Task GeneratedSaveFactory_RoutesToMethod()
    {
        var factory = GetRequiredService<IApiGeneratedSaveEntityFactory>();

        var entity = factory.Create();
        entity.Name = "Test";

        // Save factory routes to Insert for new entities
        await factory.SaveAsync(entity);
    }

    [Fact]
    public void GeneratedRuleId_StableAcrossCompilations()
    {
        var factory = GetRequiredService<IApiRuleIdEntityFactory>();
        var entity = factory.Create();

        // Rule validates Value > 0
        entity.Value = -1;
        Assert.False(entity.IsValid);

        entity.Value = 1;
        Assert.True(entity.IsValid);
    }
}
