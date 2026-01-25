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

    // Expose protected methods for testing
    public void DoMarkNew() => MarkNew();
    public void DoMarkOld() => MarkOld();
    public void DoMarkModified() => MarkModified();
    public void DoMarkUnmodified() => MarkUnmodified();
    public void DoMarkDeleted() => MarkDeleted();
    public void DoMarkAsChild() => MarkAsChild();
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
    void DoMarkOld();
    void DoMarkUnmodified();
}

[Factory]
public partial class ApiOrderItem : EntityBase<ApiOrderItem>, IApiOrderItem
{
    public ApiOrderItem(IEntityBaseServices<ApiOrderItem> services) : base(services) { }

    public partial string ProductCode { get; set; }

    public partial decimal Price { get; set; }

    public partial int Quantity { get; set; }

    public void DoMarkOld() => MarkOld();
    public void DoMarkUnmodified() => MarkUnmodified();
}

/// <summary>
/// Entity list for aggregate samples.
/// </summary>
public interface IApiOrderItemList : IEntityListBase<IApiOrderItem>
{
    int DeletedCount { get; }
    void DoFactoryStart(FactoryOperation operation);
    void DoFactoryComplete(FactoryOperation operation);
}

public class ApiOrderItemList : EntityListBase<IApiOrderItem>, IApiOrderItemList
{
    public int DeletedCount => DeletedList.Count;
    public void DoFactoryStart(FactoryOperation operation) => FactoryStart(operation);
    public void DoFactoryComplete(FactoryOperation operation) => FactoryComplete(operation);
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

    public void DoMarkUnmodified() => MarkUnmodified();
    public void DoMarkOld() => MarkOld();
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
}

/// <summary>
/// ValidateListBase for validation samples.
/// </summary>
public class ApiValidateItemList : ValidateListBase<ApiValidateItem>
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

    public void DoMarkOld() => MarkOld();

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
}
#endregion

// -----------------------------------------------------------------
// Test classes for API Reference samples
// -----------------------------------------------------------------

public class ApiReferenceSamplesTests
{
    #region api-validatebase-property-access
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
    #endregion

    #region api-validatebase-runrules
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
    #endregion

    #region api-validatebase-markinvalid
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
    #endregion

    #region api-validatebase-metaproperties
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
    #endregion

    #region api-validatebase-parent
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
    #endregion

    #region api-validatebase-tasks
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
    #endregion

    #region api-validatebase-pause
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
    #endregion

    #region api-entitybase-persistence-state
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
    #endregion

    #region api-entitybase-modification
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
    #endregion

    #region api-entitybase-root
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
    #endregion

    #region api-entitybase-delete
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
    #endregion

    #region api-entitybase-mark-methods
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
    #endregion

    #region api-validatelistbase-parent
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
    #endregion

    #region api-validatelistbase-metaproperties
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
    #endregion

    #region api-validatelistbase-validation
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
    #endregion

    #region api-validatelistbase-collection-ops
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
    #endregion

    #region api-entitylistbase-metaproperties
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
    #endregion

    #region api-entitylistbase-deletedlist
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
    #endregion

    #region api-entitylistbase-add-remove
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
    #endregion

    #region api-interfaces-ivalidatebase
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
    #endregion

    #region api-interfaces-ientitybase
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
    #endregion

    #region api-interfaces-ivalidateproperty
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
    #endregion

    #region api-interfaces-ipropertyinfo
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
    #endregion

    #region api-interfaces-imetaproperties
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
    #endregion

    [Fact]
    public void FactoryAttribute_EnablesSourceGeneration()
    {
        // [Factory] attribute marks class for factory generation
        var product = new ApiProduct(new ValidateBaseServices<ApiProduct>());
        product.Name = "Widget";
        product.Price = 19.99m;

        Assert.Equal("Widget", product.Name);
        Assert.Equal(19.99m, product.Price);
    }

    [Fact]
    public void CreateAttribute_InitializesNewEntity()
    {
        var invoice = new ApiInvoice(new EntityBaseServices<ApiInvoice>());

        invoice.FactoryStart(FactoryOperation.Create);
        invoice.Create();
        invoice.FactoryComplete(FactoryOperation.Create);

        Assert.True(invoice.IsNew);
        Assert.Equal(0, invoice.Id);
        Assert.Contains("INV-", invoice.InvoiceNumber);
    }

    [Fact]
    public async Task FetchAttribute_LoadsExistingEntity()
    {
        var contact = new ApiContact(new EntityBaseServices<ApiContact>());
        var repository = new MockApiCustomerRepository();

        contact.FactoryStart(FactoryOperation.Fetch);
        await contact.FetchAsync(1, repository);
        contact.FactoryComplete(FactoryOperation.Fetch);

        Assert.False(contact.IsNew);
        Assert.Equal(1, contact.Id);
        Assert.Equal("Customer 1", contact.Name);
    }

    [Fact]
    public async Task InsertAttribute_PersistsNewEntity()
    {
        var account = new ApiAccount(new EntityBaseServices<ApiAccount>());
        var repository = new MockApiCustomerRepository();

        account.FactoryStart(FactoryOperation.Create);
        account.Create();
        account.FactoryComplete(FactoryOperation.Create);

        account.AccountName = "New Account";

        // Insert persists new entity
        await account.InsertAsync(repository);
    }

    [Fact]
    public async Task UpdateAttribute_PersistsChanges()
    {
        var lead = new ApiLead(new EntityBaseServices<ApiLead>());
        var repository = new MockApiCustomerRepository();

        lead.Id = 1;
        lead.LeadName = "Updated Lead";
        lead.DoMarkOld();

        // Update persists changes
        await lead.UpdateAsync(repository);
    }

    [Fact]
    public async Task DeleteAttribute_RemovesEntity()
    {
        var project = new ApiProject(new EntityBaseServices<ApiProject>());
        var repository = new MockApiCustomerRepository();

        project.Id = 1;

        // Delete removes entity
        await project.DeleteAsync(repository);
    }

    [Fact]
    public async Task ServiceAttribute_InjectsFromDI()
    {
        var report = new ApiReport(new EntityBaseServices<ApiReport>());
        var repository = new MockApiCustomerRepository();

        // [Service] parameter is resolved from DI container
        await report.FetchAsync(1, repository);

        Assert.Equal(1, report.Id);
        Assert.Equal("Customer 1", report.ReportName);
    }

    [Fact]
    public void SuppressFactoryAttribute_PreventsGeneration()
    {
        // [SuppressFactory] prevents factory method generation
        // Used for test classes that inherit from Neatoo base classes
        var testObj = new ApiTestObject(new ValidateBaseServices<ApiTestObject>());
        testObj.Name = "Test";

        Assert.Equal("Test", testObj.Name);
    }

    [Fact]
    public void ValidationAttributes_AutoConverted()
    {
        var registration = new ApiRegistration(new ValidateBaseServices<ApiRegistration>());

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
        var customer = new ApiGeneratedCustomer(new ValidateBaseServices<ApiGeneratedCustomer>());

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
        var entity = new ApiGeneratedEntity(new EntityBaseServices<ApiGeneratedEntity>());

        // Source generator creates factory methods
        entity.FactoryStart(FactoryOperation.Create);
        entity.Create();
        entity.FactoryComplete(FactoryOperation.Create);

        Assert.True(entity.IsNew);
        Assert.Equal(0, entity.Id);
    }

    [Fact]
    public async Task GeneratedSaveFactory_RoutesToMethod()
    {
        var entity = new ApiGeneratedSaveEntity(new EntityBaseServices<ApiGeneratedSaveEntity>());
        var repository = new MockApiCustomerRepository();

        entity.FactoryStart(FactoryOperation.Create);
        entity.Create();
        entity.FactoryComplete(FactoryOperation.Create);

        entity.Name = "Test";

        // Save factory routes to Insert for new entities
        await entity.InsertAsync(repository);
    }

    [Fact]
    public void GeneratedRuleId_StableAcrossCompilations()
    {
        var entity = new ApiRuleIdEntity(new ValidateBaseServices<ApiRuleIdEntity>());

        // Rule validates Value > 0
        entity.Value = -1;
        Assert.False(entity.IsValid);

        entity.Value = 1;
        Assert.True(entity.IsValid);
    }
}
