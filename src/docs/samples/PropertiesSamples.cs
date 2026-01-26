using Neatoo;
using Neatoo.Internal;
using Neatoo.RemoteFactory;
using System.ComponentModel;
using Xunit;

namespace Samples;

// -----------------------------------------------------------------
// Entity classes for properties samples
// -----------------------------------------------------------------

/// <summary>
/// Entity demonstrating partial property declaration.
/// </summary>
#region properties-partial-declaration
[Factory]
public partial class PropEmployee : ValidateBase<PropEmployee>
{
    public PropEmployee(IValidateBaseServices<PropEmployee> services) : base(services) { }

    // Partial property - source generator completes the implementation
    public partial string Name { get; set; }

    public partial string Email { get; set; }

    public partial DateTime HireDate { get; set; }

    [Create]
    public void Create() { }
}
#endregion

/// <summary>
/// Entity demonstrating read-only property.
/// </summary>
#region properties-read-only
[Factory]
public partial class PropContact : ValidateBase<PropContact>
{
    public PropContact(IValidateBaseServices<PropContact> services) : base(services) { }

    public partial string FirstName { get; set; }

    public partial string LastName { get; set; }

    // Read-only property - only getter implementation generated
    public partial string FullName { get; }

    [Create]
    public void Create() { }
}
#endregion

/// <summary>
/// Entity demonstrating custom getter logic.
/// </summary>
[Factory]
public partial class PropCustomer : ValidateBase<PropCustomer>
{
    public PropCustomer(IValidateBaseServices<PropCustomer> services) : base(services)
    {
        // Validation rule for phone number
        RuleManager.AddValidation(
            c => c.PhoneNumber?.Length >= 10 || string.IsNullOrEmpty(c.PhoneNumber) ? "" : "Phone number must be at least 10 digits",
            c => c.PhoneNumber);
    }

    public partial string FirstName { get; set; }

    public partial string LastName { get; set; }

    public partial string PhoneNumber { get; set; }

    #region properties-custom-getter
    // Computed property with custom getter logic
    public string DisplayName
    {
        get
        {
            // Compute value from other properties
            if (string.IsNullOrEmpty(FirstName) && string.IsNullOrEmpty(LastName))
            {
                return "(Unknown)";
            }
            return $"{LastName}, {FirstName}".Trim(',', ' ');
        }
    }
    #endregion

    [Create]
    public void Create() { }
}

/// <summary>
/// Entity for demonstrating property change events.
/// </summary>
public interface IPropOrderItem : IEntityBase
{
    string ProductName { get; set; }
    decimal UnitPrice { get; set; }
    int Quantity { get; set; }
}

[Factory]
public partial class PropOrderItem : EntityBase<PropOrderItem>, IPropOrderItem
{
    public PropOrderItem(IEntityBaseServices<PropOrderItem> services) : base(services)
    {
        RuleManager.AddValidation(
            item => !string.IsNullOrEmpty(item.ProductName) ? "" : "Product name is required",
            i => i.ProductName);
    }

    public partial string ProductName { get; set; }

    public partial decimal UnitPrice { get; set; }

    public partial int Quantity { get; set; }

    [Create]
    public void Create() { }
}

public interface IPropOrderItemList : IEntityListBase<IPropOrderItem> { }

public class PropOrderItemList : EntityListBase<IPropOrderItem>, IPropOrderItemList { }

/// <summary>
/// Order aggregate root for property cascade samples.
/// </summary>
[Factory]
public partial class PropOrder : EntityBase<PropOrder>
{
    public PropOrder(IEntityBaseServices<PropOrder> services) : base(services)
    {
        LineItemsProperty.LoadValue(new PropOrderItemList());
    }

    public partial string OrderNumber { get; set; }

    public partial DateTime OrderDate { get; set; }

    public partial IPropOrderItemList LineItems { get; set; }

    [Create]
    public void Create() { }
}

/// <summary>
/// Entity for async task tracking samples.
/// </summary>
[Factory]
public partial class PropAsyncProduct : ValidateBase<PropAsyncProduct>
{
    public PropAsyncProduct(
        IValidateBaseServices<PropAsyncProduct> services,
        IPricingService pricingService) : base(services)
    {
        // Async rule that fetches pricing
        RuleManager.AddActionAsync(
            async product =>
            {
                if (!string.IsNullOrEmpty(product.ZipCode))
                {
                    product.TaxRate = await pricingService.GetTaxRateAsync(product.ZipCode);
                }
            },
            p => p.ZipCode);
    }

    public partial string Name { get; set; }

    public partial string ZipCode { get; set; }

    public partial decimal TaxRate { get; set; }

    [Create]
    public void Create() { }
}

/// <summary>
/// Entity for validation integration samples.
/// </summary>
[Factory]
public partial class PropInvoice : ValidateBase<PropInvoice>
{
    public PropInvoice(IValidateBaseServices<PropInvoice> services) : base(services)
    {
        RuleManager.AddValidation(
            inv => inv.Amount > 0 ? "" : "Amount must be positive",
            i => i.Amount);

        RuleManager.AddValidation(
            inv => !string.IsNullOrEmpty(inv.CustomerName) ? "" : "Customer name is required",
            i => i.CustomerName);
    }

    public partial string CustomerName { get; set; }

    public partial decimal Amount { get; set; }

    public partial DateTime InvoiceDate { get; set; }

    [Create]
    public void Create() { }
}

// -----------------------------------------------------------------
// Test classes for properties samples
// -----------------------------------------------------------------

/// <summary>
/// Tests for properties.md snippets demonstrating DI-based factory usage.
/// </summary>
public class PropertiesSamplesTests : SamplesTestBase
{
    [Fact]
    public void PartialPropertyDeclaration_SourceGeneratorCompletesImplementation()
    {
        var factory = GetRequiredService<IPropEmployeeFactory>();
        var employee = factory.Create();

        // Properties work like normal properties
        employee.Name = "Alice Johnson";
        employee.Email = "alice@example.com";
        employee.HireDate = new DateTime(2023, 6, 15);

        Assert.Equal("Alice Johnson", employee.Name);
        Assert.Equal("alice@example.com", employee.Email);
        Assert.Equal(new DateTime(2023, 6, 15), employee.HireDate);
    }

    #region properties-generated-implementation
    [Fact]
    public void GeneratedImplementation_PropertyBackingField()
    {
        var factory = GetRequiredService<IPropEmployeeFactory>();
        var employee = factory.Create();

        // The source generator creates:
        // - NameProperty backing field of type IValidateProperty<string>
        // - Getter that returns NameProperty.Value
        // - Setter that sets NameProperty.Value and tracks tasks
        employee.Name = "Bob Smith";

        // Verify property value is accessible
        Assert.Equal("Bob Smith", employee.Name);

        // Access generated backing field via indexer
        var nameProperty = employee["Name"];
        Assert.Equal("Bob Smith", nameProperty.Value);
    }
    #endregion

    #region properties-backing-field-access
    [Fact]
    public void BackingFieldAccess_PropertyWrapper()
    {
        var factory = GetRequiredService<IPropEmployeeFactory>();
        var employee = factory.Create();
        employee.Name = "Carol Davis";

        // Access property wrapper via indexer
        var nameProperty = employee["Name"];

        // Property wrapper provides:
        Assert.Equal("Carol Davis", nameProperty.Value);        // Value access
        Assert.False(nameProperty.IsBusy);                      // Async status
        Assert.True(nameProperty.IsValid);                      // Validation status
        Assert.Empty(nameProperty.PropertyMessages);            // Error messages
        Assert.False(nameProperty.IsReadOnly);                  // Mutability

        // Strongly-typed access by casting
        var typedProperty = (IValidateProperty<string>)nameProperty;
        Assert.Equal("Carol Davis", typedProperty.Value);
    }
    #endregion

    #region properties-property-changed
    [Fact]
    public void PropertyChanged_StandardNotification()
    {
        var factory = GetRequiredService<IPropEmployeeFactory>();
        var employee = factory.Create();
        var changedProperties = new List<string>();

        // Subscribe to PropertyChanged
        employee.PropertyChanged += (sender, args) =>
        {
            changedProperties.Add(args.PropertyName!);
        };

        // Set properties
        employee.Name = "Dave Wilson";
        employee.Email = "dave@example.com";

        // PropertyChanged fires for each property
        Assert.Contains("Name", changedProperties);
        Assert.Contains("Email", changedProperties);
    }
    #endregion

    #region properties-neatoo-property-changed
    [Fact]
    public async Task NeatooPropertyChanged_ExtendedNotification()
    {
        var factory = GetRequiredService<IPropOrderFactory>();
        var order = factory.Create();
        var receivedEvents = new List<NeatooPropertyChangedEventArgs>();

        // Subscribe to NeatooPropertyChanged
        order.NeatooPropertyChanged += (args) =>
        {
            receivedEvents.Add(args);
            return Task.CompletedTask;
        };

        // Set property
        order.OrderNumber = "ORD-001";

        // Wait for async event handling
        await order.WaitForTasks();

        // Event provides extended information
        var orderNumberEvent = receivedEvents.FirstOrDefault(e => e.PropertyName == "OrderNumber");
        Assert.NotNull(orderNumberEvent);
        Assert.Equal("OrderNumber", orderNumberEvent.PropertyName);
        Assert.Equal("OrderNumber", orderNumberEvent.FullPropertyName);
        Assert.Equal(ChangeReason.UserEdit, orderNumberEvent.Reason);
    }
    #endregion

    #region properties-change-reason-useredit
    [Fact]
    public void ChangeReasonUserEdit_NormalPropertyAssignment()
    {
        var factory = GetRequiredService<IPropInvoiceFactory>();
        var invoice = factory.Create();
        ChangeReason capturedReason = ChangeReason.Load; // Initialize to opposite

        invoice.NeatooPropertyChanged += (args) =>
        {
            if (args.PropertyName == "Amount")
            {
                capturedReason = args.Reason;
            }
            return Task.CompletedTask;
        };

        // Standard assignment uses UserEdit
        invoice.Amount = 100.00m;

        // Reason is UserEdit for normal setter assignment
        Assert.Equal(ChangeReason.UserEdit, capturedReason);

        // Amount property's validation rule executes with UserEdit
        // (Amount > 0 passes, so Amount property is valid)
        Assert.True(invoice["Amount"].IsValid);
    }
    #endregion

    #region properties-load-value
    [Fact]
    public void LoadValue_DataLoadingWithoutRules()
    {
        var factory = GetRequiredService<IPropInvoiceFactory>();
        var invoice = factory.Create();

        // Use LoadValue during data loading (e.g., in Fetch factory method)
        // LoadValue:
        // - Does NOT trigger validation rules
        // - Does NOT mark entity as modified
        // - DOES fire PropertyChanged (for UI binding)
        // - DOES establish parent-child relationships
        invoice["CustomerName"].LoadValue("Acme Corp");
        invoice["Amount"].LoadValue(500.00m);

        // Property values are set
        Assert.Equal("Acme Corp", invoice.CustomerName);
        Assert.Equal(500.00m, invoice.Amount);
    }
    #endregion

    #region properties-meta-properties
    [Fact]
    public async Task MetaProperties_QueryPropertyState()
    {
        var factory = GetRequiredService<IPropInvoiceFactory>();
        var invoice = factory.Create();

        // Set valid data
        invoice.CustomerName = "Beta Inc";
        invoice.Amount = 250.00m;

        await invoice.WaitForTasks();

        // Access meta-properties on property wrapper
        var amountProperty = invoice["Amount"];

        // Available meta-properties:
        Assert.False(amountProperty.IsBusy);           // No async operations pending
        Assert.True(amountProperty.IsValid);           // Property passes validation
        Assert.True(amountProperty.IsSelfValid);       // Property itself is valid
        Assert.Empty(amountProperty.PropertyMessages); // No validation errors
        Assert.False(amountProperty.IsReadOnly);       // Can be modified

        // Set invalid data
        invoice.Amount = -100.00m;
        await invoice.WaitForTasks();

        // Meta-properties update
        Assert.False(invoice["Amount"].IsValid);
        Assert.True(invoice["Amount"].PropertyMessages.Any());
    }
    #endregion

    [Fact]
    public void CustomGetter_ComputedProperty()
    {
        var factory = GetRequiredService<IPropCustomerFactory>();
        var customer = factory.Create();

        // Empty names return default
        Assert.Equal("(Unknown)", customer.DisplayName);

        // First name only
        customer.FirstName = "Jane";
        Assert.Equal("Jane", customer.DisplayName);

        // Both names
        customer.LastName = "Doe";
        Assert.Equal("Doe, Jane", customer.DisplayName);
    }

    [Fact]
    public void ReadOnlyProperty_OnlyGetter()
    {
        var factory = GetRequiredService<IPropContactFactory>();
        var contact = factory.Create();

        // Set writable properties
        contact.FirstName = "John";
        contact.LastName = "Smith";

        // Read-only property FullName is accessible through LoadValue
        // (typically set during deserialization or factory operations)
        contact["FullName"].LoadValue("John Smith");

        Assert.Equal("John Smith", contact.FullName);
    }

    #region properties-suppress-events
    [Fact]
    public void SuppressEvents_PauseAllActions()
    {
        var factory = GetRequiredService<IPropInvoiceFactory>();
        var invoice = factory.Create();
        var changeCount = 0;

        invoice.PropertyChanged += (_, _) => changeCount++;

        // Pause property events during batch updates
        using (invoice.PauseAllActions())
        {
            invoice.CustomerName = "Gamma LLC";
            invoice.Amount = 750.00m;
            invoice.InvoiceDate = DateTime.Today;

            // Events are suppressed during pause
            // (changeCount may have some events from internal operations,
            // but rule execution is deferred)
        }

        // After Resume (automatic when using statement ends):
        // - All deferred events fire
        // - Validation rules execute
        // - Dirty state recalculates

        // Verify properties are set
        Assert.Equal("Gamma LLC", invoice.CustomerName);
        Assert.Equal(750.00m, invoice.Amount);
    }
    #endregion

    #region properties-indexer-access
    [Fact]
    public void IndexerAccess_DynamicPropertyAccess()
    {
        var factory = GetRequiredService<IPropEmployeeFactory>();
        var employee = factory.Create();
        employee.Name = "Eva Martinez";

        // Access property by name using indexer
        var property = employee["Name"];
        Assert.Equal("Eva Martinez", property.Value);

        // Cast to strongly-typed for type-safe access
        var typedProperty = (IValidateProperty<string>)property;
        typedProperty.Value = "Eva M. Martinez";

        // Use TryGetProperty for safe access
        if (employee.TryGetProperty("Email", out var emailProperty))
        {
            emailProperty.Value = "eva@example.com";
        }

        Assert.Equal("Eva M. Martinez", employee.Name);
        Assert.Equal("eva@example.com", employee.Email);
    }
    #endregion

    #region properties-task-tracking
    [Fact]
    public async Task TaskTracking_AsyncOperations()
    {
        var factory = GetRequiredService<IPropAsyncProductFactory>();
        var product = factory.Create();

        product.Name = "Widget";

        // Setting ZipCode triggers async rule
        product.ZipCode = "90210";

        // IsBusy is true while async operations run
        // (may be false if rule completes very fast)

        // Wait for all property tasks to complete
        await product.WaitForTasks();

        // After tasks complete:
        Assert.False(product.IsBusy);
        Assert.Equal(0.0825m, product.TaxRate);

        // Access property-level task
        var zipProperty = product["ZipCode"];
        Assert.True(zipProperty.Task.IsCompleted);
    }
    #endregion

    #region properties-validation-integration
    [Fact]
    public async Task ValidationIntegration_PropertyValidation()
    {
        var factory = GetRequiredService<IPropInvoiceFactory>();
        var invoice = factory.Create();

        // Set invalid value
        invoice.Amount = -50.00m;

        // Property-level validation state
        var amountProperty = invoice["Amount"];
        Assert.False(amountProperty.IsValid);
        Assert.True(amountProperty.PropertyMessages.Any());

        // Object-level validation reflects property state
        Assert.False(invoice.IsValid);

        // Fix the value
        invoice.Amount = 100.00m;
        await invoice.WaitForTasks();

        // Validation passes
        Assert.True(invoice["Amount"].IsValid);
        Assert.Empty(invoice["Amount"].PropertyMessages);

        // Set required field to trigger full validity
        invoice.CustomerName = "Test Customer";
        await invoice.WaitForTasks();

        Assert.True(invoice.IsValid);
    }
    #endregion

    #region properties-change-propagation
    [Fact]
    public async Task ChangePropagation_ChildToParent()
    {
        var orderFactory = GetRequiredService<IPropOrderFactory>();
        var order = orderFactory.Create();
        order.OrderNumber = "ORD-001";

        var receivedEvents = new List<NeatooPropertyChangedEventArgs>();

        order.NeatooPropertyChanged += (args) =>
        {
            receivedEvents.Add(args);
            return Task.CompletedTask;
        };

        // Add child item
        var itemFactory = GetRequiredService<IPropOrderItemFactory>();
        var item = itemFactory.Create();
        item.ProductName = "Widget";
        item.UnitPrice = 25.00m;
        item.Quantity = 2;

        order.LineItems.Add(item);

        // Change child property
        item.UnitPrice = 30.00m;

        await order.WaitForTasks();

        // Parent receives notification with full breadcrumb path
        var propagatedEvent = receivedEvents
            .FirstOrDefault(e => e.FullPropertyName.Contains("UnitPrice"));

        // FullPropertyName builds breadcrumb: "LineItems.UnitPrice"
        Assert.NotNull(propagatedEvent);
    }
    #endregion

    #region properties-constructor-assignment
    [Fact]
    public void ConstructorAssignment_UseLoadValueInstead()
    {
        // Avoid setting properties directly in constructors
        // outside of factory methods, as they will be tracked
        // as modifications.

        // Instead, use LoadValue for initial values:
        var factory = GetRequiredService<IPropEmployeeFactory>();
        var employee = factory.Create();

        // LoadValue sets value without triggering modification tracking
        employee["Name"].LoadValue("Default Employee");
        employee["Email"].LoadValue("default@example.com");

        // Properties are set but not marked as modified
        // (In a full EntityBase scenario with MarkUnmodified)
        Assert.Equal("Default Employee", employee.Name);
        Assert.Equal("default@example.com", employee.Email);
    }
    #endregion

    // Additional comprehensive tests

    [Fact]
    public async Task CascadeValidation_ChildInvalidMakesParentInvalid()
    {
        var orderFactory = GetRequiredService<IPropOrderFactory>();
        var order = orderFactory.Create();
        order.OrderNumber = "ORD-002";
        await order.RunRules();

        Assert.True(order.IsValid);

        // Add invalid child (empty ProductName)
        var itemFactory = GetRequiredService<IPropOrderItemFactory>();
        var invalidItem = itemFactory.Create();
        invalidItem.ProductName = "";
        invalidItem.UnitPrice = 50.00m;
        await invalidItem.RunRules();

        Assert.False(invalidItem.IsValid);

        order.LineItems.Add(invalidItem);

        // Parent reflects child's invalid state
        Assert.False(order.IsValid);

        // Fix child
        invalidItem.ProductName = "Fixed Product";
        await invalidItem.RunRules();

        Assert.True(order.IsValid);
    }

    [Fact]
    public void PropertyType_ReturnsCorrectType()
    {
        var factory = GetRequiredService<IPropEmployeeFactory>();
        var employee = factory.Create();

        var nameProperty = employee["Name"];
        var hireDateProperty = employee["HireDate"];

        Assert.Equal(typeof(string), nameProperty.Type);
        Assert.Equal(typeof(DateTime), hireDateProperty.Type);
    }

    [Fact]
    public void StringValue_FormatsPropertyValue()
    {
        var factory = GetRequiredService<IPropOrderFactory>();
        var order = factory.Create();
        order.OrderDate = new DateTime(2024, 6, 15);

        var property = order["OrderDate"];

        // StringValue returns ToString() of the value
        Assert.NotNull(property.StringValue);
        Assert.Contains("2024", property.StringValue);
    }

    [Fact]
    public async Task RunRulesOnProperty_ManualValidation()
    {
        var factory = GetRequiredService<IPropInvoiceFactory>();
        var invoice = factory.Create();
        invoice.Amount = -100m;

        // Manually run rules on specific property
        await invoice["Amount"].RunRules();

        Assert.False(invoice["Amount"].IsValid);
    }

    [Fact]
    public void PropertyName_ReturnsName()
    {
        var factory = GetRequiredService<IPropEmployeeFactory>();
        var employee = factory.Create();

        var property = employee["Name"];

        Assert.Equal("Name", property.Name);
    }

    [Fact]
    public async Task WaitForPropertyTasks_AwaitsCompletion()
    {
        var factory = GetRequiredService<IPropAsyncProductFactory>();
        var product = factory.Create();

        product.ZipCode = "94102";

        // Wait for specific property's tasks
        var zipProperty = product["ZipCode"];
        await zipProperty.WaitForTasks();

        Assert.Equal(0.0825m, product.TaxRate);
    }
}
