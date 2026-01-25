using Neatoo;
using Neatoo.Internal;
using Neatoo.RemoteFactory;
using Neatoo.Rules;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace Samples;

// -----------------------------------------------------------------
// Entity classes for validation samples
// -----------------------------------------------------------------

/// <summary>
/// Basic ValidateBase entity demonstrating inheritance.
/// </summary>
#region validation-basic
[Factory]
public partial class ValidationCustomer : ValidateBase<ValidationCustomer>
{
    public ValidationCustomer(IValidateBaseServices<ValidationCustomer> services) : base(services) { }

    public partial string Name { get; set; }

    public partial string Email { get; set; }
}
#endregion

/// <summary>
/// Entity demonstrating partial property declarations with validation.
/// </summary>
#region validation-properties
[Factory]
public partial class ValidationEmployee : ValidateBase<ValidationEmployee>
{
    public ValidationEmployee(IValidateBaseServices<ValidationEmployee> services) : base(services) { }

    // Partial properties - source generator completes implementation
    public partial string FirstName { get; set; }

    public partial string LastName { get; set; }

    [Required]
    public partial string Email { get; set; }

    [Range(0, 200)]
    public partial int Age { get; set; }
}
#endregion

/// <summary>
/// Entity demonstrating DataAnnotations validation attributes.
/// </summary>
#region validation-attributes
[Factory]
public partial class ValidationContact : ValidateBase<ValidationContact>
{
    public ValidationContact(IValidateBaseServices<ValidationContact> services) : base(services) { }

    [Required(ErrorMessage = "Name is required")]
    [MaxLength(100)]
    public partial string Name { get; set; }

    [EmailAddress]
    public partial string Email { get; set; }

    [Phone]
    public partial string PhoneNumber { get; set; }

    [Range(1, 150, ErrorMessage = "Age must be between 1 and 150")]
    public partial int Age { get; set; }

    [RegularExpression(@"^\d{5}(-\d{4})?$", ErrorMessage = "Invalid ZIP code")]
    public partial string ZipCode { get; set; }
}
#endregion

/// <summary>
/// Entity demonstrating custom validation rules.
/// </summary>
[Factory]
public partial class ValidationInvoice : ValidateBase<ValidationInvoice>
{
    #region validation-custom-rule
    public ValidationInvoice(IValidateBaseServices<ValidationInvoice> services) : base(services)
    {
        // Custom validation rule: Amount must be positive
        RuleManager.AddValidation(
            invoice => invoice.Amount > 0 ? "" : "Amount must be greater than zero",
            i => i.Amount);
    }
    #endregion

    public partial decimal Amount { get; set; }

    public partial string CustomerName { get; set; }
}

/// <summary>
/// Custom rule class for cross-property date validation.
/// </summary>
public class ValidationDateRangeRule : RuleBase<ValidationDateRange>
{
    public ValidationDateRangeRule() : base(r => r.StartDate, r => r.EndDate) { }

    protected override IRuleMessages Execute(ValidationDateRange target)
    {
        if (target.StartDate != default && target.EndDate != default)
        {
            if (target.EndDate <= target.StartDate)
            {
                return (nameof(target.EndDate), "End date must be after start date").AsRuleMessages();
            }
        }
        return None;
    }
}

/// <summary>
/// Entity demonstrating cross-property validation.
/// </summary>
[Factory]
public partial class ValidationDateRange : ValidateBase<ValidationDateRange>
{
    #region validation-cross-property
    public ValidationDateRange(IValidateBaseServices<ValidationDateRange> services) : base(services)
    {
        // Cross-property rule: EndDate must be after StartDate
        // Triggers when either StartDate OR EndDate changes
        RuleManager.AddRule(new ValidationDateRangeRule());
    }
    #endregion

    public partial DateTime StartDate { get; set; }

    public partial DateTime EndDate { get; set; }
}

/// <summary>
/// External service for async validation samples.
/// </summary>
public interface IValidationUniquenessService
{
    Task<bool> IsEmailUniqueAsync(string email, CancellationToken token = default);
}

public class MockUniquenessService : IValidationUniquenessService
{
    public Task<bool> IsEmailUniqueAsync(string email, CancellationToken token = default)
    {
        // Simulate: emails containing "taken" are not unique
        return Task.FromResult(!email.Contains("taken"));
    }
}

/// <summary>
/// Entity demonstrating async validation rules.
/// </summary>
[Factory]
public partial class ValidationUser : ValidateBase<ValidationUser>
{
    #region validation-async-rule
    public ValidationUser(
        IValidateBaseServices<ValidationUser> services,
        IValidationUniquenessService uniquenessService) : base(services)
    {
        // Async validation rule: Check email uniqueness
        RuleManager.AddValidationAsync(
            async user =>
            {
                if (string.IsNullOrEmpty(user.Email))
                    return "";

                var isUnique = await uniquenessService.IsEmailUniqueAsync(user.Email);
                return isUnique ? "" : "Email is already in use";
            },
            u => u.Email);
    }
    #endregion

    public partial string Username { get; set; }

    public partial string Email { get; set; }
}

/// <summary>
/// Entity demonstrating manual rule execution.
/// </summary>
[Factory]
public partial class ValidationOrder : ValidateBase<ValidationOrder>
{
    public ValidationOrder(IValidateBaseServices<ValidationOrder> services) : base(services)
    {
        RuleManager.AddValidation(
            order => order.Quantity > 0 ? "" : "Quantity must be positive",
            o => o.Quantity);

        RuleManager.AddValidation(
            order => order.UnitPrice >= 0 ? "" : "Price cannot be negative",
            o => o.UnitPrice);
    }

    public partial string ProductCode { get; set; }

    public partial int Quantity { get; set; }

    public partial decimal UnitPrice { get; set; }
}

/// <summary>
/// Entity demonstrating error message access patterns.
/// </summary>
[Factory]
public partial class ValidationProduct : ValidateBase<ValidationProduct>
{
    public ValidationProduct(IValidateBaseServices<ValidationProduct> services) : base(services)
    {
        RuleManager.AddValidation(
            p => !string.IsNullOrEmpty(p.Name) ? "" : "Name is required",
            p => p.Name);

        RuleManager.AddValidation(
            p => p.Price >= 0 ? "" : "Price cannot be negative",
            p => p.Price);
    }

    public partial string Name { get; set; }

    public partial decimal Price { get; set; }
}

/// <summary>
/// Entity demonstrating property-level validation state.
/// </summary>
[Factory]
public partial class ValidationAccount : ValidateBase<ValidationAccount>
{
    public ValidationAccount(
        IValidateBaseServices<ValidationAccount> services,
        IValidationUniquenessService? uniquenessService = null) : base(services)
    {
        RuleManager.AddValidation(
            a => !string.IsNullOrEmpty(a.AccountNumber) ? "" : "Account number is required",
            a => a.AccountNumber);

        if (uniquenessService != null)
        {
            RuleManager.AddValidationAsync(
                async a =>
                {
                    if (string.IsNullOrEmpty(a.Email)) return "";
                    var isUnique = await uniquenessService.IsEmailUniqueAsync(a.Email);
                    return isUnique ? "" : "Email is already in use";
                },
                a => a.Email);
        }
    }

    public partial string AccountNumber { get; set; }

    public partial string Email { get; set; }

    public partial decimal Balance { get; set; }
}

/// <summary>
/// Entity demonstrating MarkInvalid for object-level validation.
/// </summary>
[Factory]
public partial class ValidationTransaction : ValidateBase<ValidationTransaction>
{
    public ValidationTransaction(IValidateBaseServices<ValidationTransaction> services) : base(services) { }

    public partial string TransactionId { get; set; }

    public partial decimal Amount { get; set; }

    /// <summary>
    /// Marks the transaction as invalid due to external validation failure.
    /// </summary>
    public void MarkTransactionInvalid(string reason)
    {
        MarkInvalid(reason);
    }
}

/// <summary>
/// Child entity for validation cascade samples.
/// </summary>
public interface IValidationLineItem : IValidateBase
{
    string Description { get; set; }
    decimal Amount { get; set; }
}

[Factory]
public partial class ValidationLineItem : ValidateBase<ValidationLineItem>, IValidationLineItem
{
    public ValidationLineItem(IValidateBaseServices<ValidationLineItem> services) : base(services)
    {
        RuleManager.AddValidation(
            item => !string.IsNullOrEmpty(item.Description) ? "" : "Description is required",
            i => i.Description);
    }

    public partial string Description { get; set; }

    public partial decimal Amount { get; set; }
}

public interface IValidationLineItemList : IValidateListBase<IValidationLineItem> { }

public class ValidationLineItemList : ValidateListBase<IValidationLineItem>, IValidationLineItemList { }

/// <summary>
/// Parent entity for validation cascade samples.
/// </summary>
[Factory]
public partial class ValidationInvoiceWithItems : ValidateBase<ValidationInvoiceWithItems>
{
    public ValidationInvoiceWithItems(IValidateBaseServices<ValidationInvoiceWithItems> services) : base(services)
    {
        LineItemsProperty.LoadValue(new ValidationLineItemList());
    }

    public partial string InvoiceNumber { get; set; }

    public partial IValidationLineItemList LineItems { get; set; }
}

/// <summary>
/// Custom rule for password confirmation validation.
/// </summary>
public class PasswordMatchRule : RuleBase<ValidationRegistration>
{
    public PasswordMatchRule() : base(r => r.Password, r => r.ConfirmPassword) { }

    protected override IRuleMessages Execute(ValidationRegistration target)
    {
        if (target.Password != target.ConfirmPassword)
        {
            return (nameof(target.ConfirmPassword), "Passwords must match").AsRuleMessages();
        }
        return None;
    }
}

/// <summary>
/// Entity for combined attribute and custom rule samples.
/// </summary>
#region validation-combined
[Factory]
public partial class ValidationRegistration : ValidateBase<ValidationRegistration>
{
    public ValidationRegistration(IValidateBaseServices<ValidationRegistration> services) : base(services)
    {
        // Custom cross-property rule: ConfirmPassword must match Password
        RuleManager.AddRule(new PasswordMatchRule());

        // Custom business rule: Username cannot be "admin"
        RuleManager.AddValidation(
            r => r.Username?.ToLower() != "admin" ? "" : "Username 'admin' is reserved",
            r => r.Username);
    }

    [Required]
    [StringLength(50, MinimumLength = 3)]
    public partial string Username { get; set; }

    [Required]
    [EmailAddress]
    public partial string Email { get; set; }

    [Required]
    [MinLength(8)]
    public partial string Password { get; set; }

    [Required]
    public partial string ConfirmPassword { get; set; }
}
#endregion

/// <summary>
/// Entity for cancellation token samples.
/// </summary>
[Factory]
public partial class ValidationAsyncOrder : ValidateBase<ValidationAsyncOrder>
{
    public ValidationAsyncOrder(
        IValidateBaseServices<ValidationAsyncOrder> services,
        IInventoryService? inventoryService = null) : base(services)
    {
        if (inventoryService != null)
        {
            RuleManager.AddValidationAsync(
                async (order, token) =>
                {
                    if (string.IsNullOrEmpty(order.ProductCode)) return "";
                    var inStock = await inventoryService.IsInStockAsync(order.ProductCode, token);
                    return inStock ? "" : "Product is out of stock";
                },
                o => o.ProductCode);
        }
    }

    public partial string ProductCode { get; set; }

    public partial int Quantity { get; set; }
}

/// <summary>
/// Entity for save validation samples (EntityBase required for IsSavable).
/// </summary>
[Factory]
public partial class ValidationSaveableOrder : EntityBase<ValidationSaveableOrder>
{
    public ValidationSaveableOrder(IEntityBaseServices<ValidationSaveableOrder> services) : base(services)
    {
        RuleManager.AddValidation(
            o => o.Quantity > 0 ? "" : "Quantity must be positive",
            o => o.Quantity);
    }

    public partial string OrderNumber { get; set; }

    public partial int Quantity { get; set; }

    // Expose protected methods for testing
    public void DoMarkNew() => MarkNew();
}

// -----------------------------------------------------------------
// Test classes for validation samples
// -----------------------------------------------------------------

/// <summary>
/// Tests for validation.md snippets.
/// </summary>
public class ValidationSamplesTests
{
    [Fact]
    public void BasicValidateBase_ProvidesValidationInfrastructure()
    {
        var customer = new ValidationCustomer(new ValidateBaseServices<ValidationCustomer>());

        // ValidateBase provides validation infrastructure
        Assert.True(customer.IsValid);      // No validation rules, so valid
        Assert.True(customer.IsSelfValid);  // Same as IsValid for leaf objects
        Assert.Empty(customer.PropertyMessages);

        // Properties work normally
        customer.Name = "Acme Corp";
        customer.Email = "contact@acme.com";

        Assert.Equal("Acme Corp", customer.Name);
    }

    [Fact]
    public void PartialProperties_WithValidationAttributes()
    {
        var employee = new ValidationEmployee(new ValidateBaseServices<ValidationEmployee>());

        // [Required] attribute on Email
        employee.Email = "";
        Assert.False(employee["Email"].IsValid);

        employee.Email = "test@example.com";
        Assert.True(employee["Email"].IsValid);

        // [Range] attribute on Age
        employee.Age = 250; // Out of range
        Assert.False(employee["Age"].IsValid);

        employee.Age = 30;
        Assert.True(employee["Age"].IsValid);
    }

    [Fact]
    public void DataAnnotations_StandardValidationAttributes()
    {
        var contact = new ValidationContact(new ValidateBaseServices<ValidationContact>());

        // [Required]
        contact.Name = "";
        Assert.False(contact["Name"].IsValid);

        contact.Name = "John";
        Assert.True(contact["Name"].IsValid);

        // [EmailAddress]
        contact.Email = "invalid-email";
        Assert.False(contact["Email"].IsValid);

        contact.Email = "john@example.com";
        Assert.True(contact["Email"].IsValid);

        // [Range]
        contact.Age = 200;
        Assert.False(contact["Age"].IsValid);

        contact.Age = 25;
        Assert.True(contact["Age"].IsValid);

        // [RegularExpression]
        contact.ZipCode = "abcde";
        Assert.False(contact["ZipCode"].IsValid);

        contact.ZipCode = "12345";
        Assert.True(contact["ZipCode"].IsValid);
    }

    [Fact]
    public void CustomValidation_LambdaRule()
    {
        var invoice = new ValidationInvoice(new ValidateBaseServices<ValidationInvoice>());

        // Custom rule: Amount must be positive
        invoice.Amount = -100;
        Assert.False(invoice.IsValid);
        Assert.Contains(invoice.PropertyMessages, m => m.Message.Contains("greater than zero"));

        invoice.Amount = 100;
        Assert.True(invoice.IsValid);
    }

    [Fact]
    public void CrossPropertyValidation_MultiplePropertyDependencies()
    {
        var dateRange = new ValidationDateRange(new ValidateBaseServices<ValidationDateRange>());

        // Set dates out of order
        dateRange.StartDate = new DateTime(2024, 6, 15);
        dateRange.EndDate = new DateTime(2024, 6, 10);

        Assert.False(dateRange.IsValid);
        Assert.Contains(dateRange.PropertyMessages, m => m.Message.Contains("after start date"));

        // Fix the dates
        dateRange.EndDate = new DateTime(2024, 6, 20);
        Assert.True(dateRange.IsValid);
    }

    [Fact]
    public async Task AsyncValidation_ExternalServiceCall()
    {
        var uniquenessService = new MockUniquenessService();
        var user = new ValidationUser(new ValidateBaseServices<ValidationUser>(), uniquenessService);

        // Email that's "taken"
        user.Email = "taken@example.com";
        await user.WaitForTasks();

        Assert.False(user.IsValid);
        Assert.Contains(user.PropertyMessages, m => m.Message.Contains("already in use"));

        // Unique email
        user.Email = "unique@example.com";
        await user.WaitForTasks();

        Assert.True(user.IsValid);
    }

    #region validation-run-rules
    [Fact]
    public async Task RunRulesManually_RevalidateEntity()
    {
        var order = new ValidationOrder(new ValidateBaseServices<ValidationOrder>());

        // Set invalid values
        order.Quantity = -5;
        order.UnitPrice = -10;

        // Validation runs automatically on property set
        Assert.False(order.IsValid);

        // Fix values
        order.Quantity = 10;
        order.UnitPrice = 25.00m;

        // Manually run all rules (clears messages and re-validates)
        await order.RunRules(RunRulesFlag.All);

        Assert.True(order.IsValid);
    }
    #endregion

    #region validation-error-messages
    [Fact]
    public void AccessValidationMessages_PropertyAndObject()
    {
        var product = new ValidationProduct(new ValidateBaseServices<ValidationProduct>());

        // Trigger validation failures
        product.Name = "";
        product.Price = -50;

        // Access all messages on the object
        Assert.True(product.PropertyMessages.Any());

        // Filter messages by property
        var nameMessages = product.PropertyMessages
            .Where(m => m.Property.Name == "Name")
            .ToList();
        Assert.NotEmpty(nameMessages);

        // Access property-specific messages via indexer
        var priceProperty = product["Price"];
        Assert.NotEmpty(priceProperty.PropertyMessages);
        Assert.Contains(priceProperty.PropertyMessages, m => m.Message.Contains("negative"));
    }
    #endregion

    #region validation-property-state
    [Fact]
    public async Task PropertyValidationState_IndividualPropertyTracking()
    {
        var uniquenessService = new MockUniquenessService();
        var account = new ValidationAccount(
            new ValidateBaseServices<ValidationAccount>(),
            uniquenessService);

        // Set valid account number
        account.AccountNumber = "ACC-001";

        // Check individual property state
        var accountNumberProperty = account["AccountNumber"];
        Assert.True(accountNumberProperty.IsValid);
        Assert.True(accountNumberProperty.IsSelfValid);
        Assert.Empty(accountNumberProperty.PropertyMessages);

        // Trigger async validation on email
        account.Email = "taken@example.com";

        // Wait for async validation
        await account.WaitForTasks();

        var emailProperty = account["Email"];
        Assert.False(emailProperty.IsValid);
        Assert.NotEmpty(emailProperty.PropertyMessages);
    }
    #endregion

    #region validation-object-invalid
    [Fact]
    public void MarkObjectInvalid_ObjectLevelValidation()
    {
        var transaction = new ValidationTransaction(new ValidateBaseServices<ValidationTransaction>());
        transaction.TransactionId = "TXN-001";
        transaction.Amount = 100;

        // Initially valid
        Assert.True(transaction.IsValid);

        // Mark as invalid due to external validation failure
        transaction.MarkTransactionInvalid("Transaction rejected by payment gateway");

        // Object is now invalid
        Assert.False(transaction.IsValid);

        // Error message appears in PropertyMessages
        Assert.Contains(transaction.PropertyMessages,
            m => m.Message.Contains("rejected by payment gateway"));

        // ObjectInvalid property contains the message
        Assert.Equal("Transaction rejected by payment gateway", transaction.ObjectInvalid);
    }
    #endregion

    #region validation-pause-actions
    [Fact]
    public void PauseAllActions_BatchUpdatesWithoutValidation()
    {
        var order = new ValidationOrder(new ValidateBaseServices<ValidationOrder>());

        // Pause validation during batch updates
        using (order.PauseAllActions())
        {
            // These assignments do NOT trigger validation rules
            order.ProductCode = "PROD-001";
            order.Quantity = 10;
            order.UnitPrice = 25.00m;

            // IsPaused is true during the using block
            Assert.True(order.IsPaused);
        }

        // After resume (automatic when using block ends):
        // - Validation rules execute for changed properties
        // - PropertyChanged events fire
        Assert.False(order.IsPaused);
        Assert.Equal("PROD-001", order.ProductCode);
    }
    #endregion

    #region validation-load-value
    [Fact]
    public void LoadValue_DataLoadingWithoutValidation()
    {
        var invoice = new ValidationInvoice(new ValidateBaseServices<ValidationInvoice>());

        // LoadValue sets property without triggering validation
        // Typically used during Fetch factory operations
        invoice["Amount"].LoadValue(-100m); // Would fail validation rule
        invoice["CustomerName"].LoadValue("Test Customer");

        // Value is set but validation rule did not execute
        Assert.Equal(-100m, invoice.Amount);

        // Property is not marked as invalid until rules run
        // (In real usage, factory method would call RunRules after loading)
    }
    #endregion

    #region validation-cascade
    [Fact]
    public void ValidationCascade_ChildToParent()
    {
        var invoice = new ValidationInvoiceWithItems(new ValidateBaseServices<ValidationInvoiceWithItems>());
        invoice.InvoiceNumber = "INV-001";

        // Parent starts valid
        Assert.True(invoice.IsValid);

        // Add invalid child (empty description)
        var lineItem = new ValidationLineItem(new ValidateBaseServices<ValidationLineItem>());
        lineItem.Description = ""; // Triggers validation failure
        invoice.LineItems.Add(lineItem);

        // Parent's IsValid reflects child's invalid state
        Assert.False(invoice.IsValid);

        // Parent's IsSelfValid ignores children
        Assert.True(invoice.IsSelfValid);

        // Fix child
        lineItem.Description = "Valid description";

        // Parent is valid again
        Assert.True(invoice.IsValid);
    }
    #endregion

    #region validation-meta-properties
    [Fact]
    public async Task MetaProperties_TrackValidationState()
    {
        var uniquenessService = new MockUniquenessService();
        var account = new ValidationAccount(
            new ValidateBaseServices<ValidationAccount>(),
            uniquenessService);

        // Set required field
        account.AccountNumber = "ACC-001";

        // IsValid: True if all properties and children pass validation
        Assert.True(account.IsValid);

        // IsSelfValid: True if this object's properties pass (ignores children)
        Assert.True(account.IsSelfValid);

        // Trigger async validation
        account.Email = "taken@example.com";

        // IsBusy: True while async validation runs
        // (may be false if validation completes very fast)

        // Wait for async completion
        await account.WaitForTasks();

        // PropertyMessages: All validation errors
        Assert.NotEmpty(account.PropertyMessages);
    }
    #endregion

    #region validation-before-save
    [Fact]
    public async Task ValidateBeforeSave_IsSavableCheck()
    {
        var order = new ValidationSaveableOrder(new EntityBaseServices<ValidationSaveableOrder>());
        order.DoMarkNew(); // Mark as new entity needing insert

        // Set invalid quantity (negative to trigger validation)
        order.Quantity = -5;

        // IsSavable combines IsModified, IsValid, and IsBusy
        Assert.False(order.IsSavable); // Invalid due to negative quantity

        // Fix the value
        order.Quantity = 5;

        // Re-run all rules before save
        await order.RunRules(RunRulesFlag.All);

        // Now savable
        Assert.True(order.IsValid);
        Assert.True(order.IsModified);
        Assert.False(order.IsBusy);
        Assert.True(order.IsSavable);
    }
    #endregion

    #region validation-cancellation
    [Fact]
    public async Task CancellationToken_CancelAsyncValidation()
    {
        var inventoryService = new MockInventoryService();
        var order = new ValidationAsyncOrder(
            new ValidateBaseServices<ValidationAsyncOrder>(),
            inventoryService);

        // Set product code to trigger async validation
        order.ProductCode = "PROD-001";

        // Run with cancellation token
        var cts = new CancellationTokenSource();

        // In real scenario, would cancel during long-running validation
        // cts.Cancel();

        await order.RunRules(RunRulesFlag.All, cts.Token);

        // Validation completed without cancellation
        Assert.True(order.IsValid);

        // If cancellation occurred, object would be marked invalid:
        // Assert.False(order.IsValid);
        // Assert.Equal("Validation cancelled", order.ObjectInvalid);
    }
    #endregion

    #region validation-messages-collection
    [Fact]
    public async Task WorkWithValidationMessages_FilterAndAccess()
    {
        var product = new ValidationProduct(new ValidateBaseServices<ValidationProduct>());

        // Trigger multiple validation failures
        product.Name = "";
        product.Price = -25;

        await product.WaitForTasks();

        // PropertyMessages contains all errors
        var allMessages = product.PropertyMessages.ToList();
        Assert.Equal(2, allMessages.Count);

        // Filter by property name
        var nameErrors = product.PropertyMessages
            .Where(m => m.Property.Name == "Name")
            .ToList();
        Assert.Single(nameErrors);

        // Clear messages and re-validate
        product.ClearAllMessages();
        Assert.Empty(product.PropertyMessages);

        // Run rules to repopulate messages
        await product.RunRules(RunRulesFlag.All);
        Assert.Equal(2, product.PropertyMessages.Count);
    }
    #endregion

    [Fact]
    public void CombinedValidation_AttributesAndCustomRules()
    {
        var registration = new ValidationRegistration(new ValidateBaseServices<ValidationRegistration>());

        // Attribute validation: Required
        registration.Username = "";
        Assert.False(registration["Username"].IsValid);

        // Attribute validation: StringLength
        registration.Username = "ab"; // Too short
        Assert.False(registration["Username"].IsValid);

        registration.Username = "validuser";
        Assert.True(registration["Username"].IsValid);

        // Custom rule: Username cannot be "admin"
        registration.Username = "admin";
        Assert.False(registration["Username"].IsValid);

        registration.Username = "regularuser";
        Assert.True(registration["Username"].IsValid);

        // Attribute validation: EmailAddress
        registration.Email = "invalid";
        Assert.False(registration["Email"].IsValid);

        registration.Email = "user@example.com";
        Assert.True(registration["Email"].IsValid);

        // Custom cross-property rule: Passwords must match
        registration.Password = "password123";
        registration.ConfirmPassword = "different";
        Assert.False(registration.IsValid); // Cross-property failure

        registration.ConfirmPassword = "password123";

        // All validations pass
        Assert.True(registration.IsValid);
    }

    [Fact]
    public void PropertyMessagesContainErrorDetails()
    {
        var contact = new ValidationContact(new ValidateBaseServices<ValidationContact>());

        contact.Name = "";
        contact.Age = 200;

        // Messages include property name and error text
        var nameMessage = contact.PropertyMessages.FirstOrDefault(m => m.Property.Name == "Name");
        Assert.NotNull(nameMessage);
        Assert.Contains("required", nameMessage.Message, StringComparison.OrdinalIgnoreCase);

        var ageMessage = contact.PropertyMessages.FirstOrDefault(m => m.Property.Name == "Age");
        Assert.NotNull(ageMessage);
        Assert.Contains("between", ageMessage.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunRulesForSpecificProperty()
    {
        var order = new ValidationOrder(new ValidateBaseServices<ValidationOrder>());
        order.Quantity = 0;
        order.UnitPrice = -10;

        // Clear all messages
        order.ClearAllMessages();

        // Run rules for just one property
        await order.RunRules("Quantity");

        // Only Quantity messages appear
        Assert.Contains(order.PropertyMessages, m => m.Property.Name == "Quantity");

        // Run rules for UnitPrice
        await order.RunRules("UnitPrice");
        Assert.Contains(order.PropertyMessages, m => m.Property.Name == "UnitPrice");
    }

    [Fact]
    public async Task RunRulesFlagOptions()
    {
        var invoice = new ValidationInvoiceWithItems(new ValidateBaseServices<ValidationInvoiceWithItems>());

        // Add child with validation error
        var item = new ValidationLineItem(new ValidateBaseServices<ValidationLineItem>());
        item.Description = "";
        invoice.LineItems.Add(item);

        // RunRulesFlag.Self runs only this object's rules (not children)
        await invoice.RunRules(RunRulesFlag.Self);

        // RunRulesFlag.All clears all messages and runs all rules (including children)
        await invoice.RunRules(RunRulesFlag.All);

        Assert.False(invoice.IsValid); // Child is invalid
    }

    [Fact]
    public void IsSelfValidVsIsValid()
    {
        var invoice = new ValidationInvoiceWithItems(new ValidateBaseServices<ValidationInvoiceWithItems>());
        invoice.InvoiceNumber = "INV-001";

        // Add invalid child
        var item = new ValidationLineItem(new ValidateBaseServices<ValidationLineItem>());
        item.Description = "";
        invoice.LineItems.Add(item);

        // IsSelfValid: Only this object's properties
        Assert.True(invoice.IsSelfValid);

        // IsValid: This object AND all children
        Assert.False(invoice.IsValid);
    }

    [Fact]
    public async Task ClearMessagesOptions()
    {
        var invoice = new ValidationInvoiceWithItems(new ValidateBaseServices<ValidationInvoiceWithItems>());

        // Add invalid child
        var item = new ValidationLineItem(new ValidateBaseServices<ValidationLineItem>());
        item.Description = "";
        invoice.LineItems.Add(item);

        // Trigger validation
        await invoice.RunRules(RunRulesFlag.All);
        Assert.NotEmpty(item.PropertyMessages);

        // ClearSelfMessages: Only this object's messages
        invoice.ClearSelfMessages();

        // ClearAllMessages: This object and all children
        invoice.ClearAllMessages();
        Assert.Empty(item.PropertyMessages);
    }
}
