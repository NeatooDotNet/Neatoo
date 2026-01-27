using Neatoo;
using Neatoo.Internal;
using Neatoo.RemoteFactory;
using System.ComponentModel.DataAnnotations;

namespace Neatoo.Skills.Domain;

// =============================================================================
// PROPERTY SAMPLES - Demonstrates property declaration and change tracking
// =============================================================================

// -----------------------------------------------------------------------------
// Partial Property Declaration
// -----------------------------------------------------------------------------

#region properties-partial-declaration
/// <summary>
/// Customer entity demonstrating partial property declarations.
/// Partial properties let the source generator create backing fields.
/// </summary>
[Factory]
public partial class SkillPropCustomer : ValidateBase<SkillPropCustomer>
{
    public SkillPropCustomer(IValidateBaseServices<SkillPropCustomer> services) : base(services) { }

    // Partial properties - source generator completes the implementation
    public partial string FirstName { get; set; }

    public partial string LastName { get; set; }

    public partial string Email { get; set; }

    public partial DateTime BirthDate { get; set; }

    [Create]
    public void Create() { }
}
#endregion

// -----------------------------------------------------------------------------
// Read-Only Properties
// -----------------------------------------------------------------------------

#region properties-read-only
/// <summary>
/// Contact entity demonstrating read-only properties.
/// </summary>
[Factory]
public partial class SkillPropContact : ValidateBase<SkillPropContact>
{
    public SkillPropContact(IValidateBaseServices<SkillPropContact> services) : base(services) { }

    public partial string FirstName { get; set; }

    public partial string LastName { get; set; }

    // Read-only property - only getter, value set via LoadValue
    public partial string FullName { get; }

    [Create]
    public void Create() { }

    [Fetch]
    public void Fetch(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
        // Use LoadValue to set read-only properties during fetch
        this["FullName"].LoadValue($"{firstName} {lastName}");
    }
}
#endregion

// -----------------------------------------------------------------------------
// Custom Getter Logic
// -----------------------------------------------------------------------------

#region properties-custom-getter
/// <summary>
/// Order entity demonstrating computed properties.
/// </summary>
[Factory]
public partial class SkillPropOrder : ValidateBase<SkillPropOrder>
{
    public SkillPropOrder(IValidateBaseServices<SkillPropOrder> services) : base(services) { }

    public partial int Quantity { get; set; }

    public partial decimal UnitPrice { get; set; }

    public partial decimal DiscountPercent { get; set; }

    // Computed property with custom getter logic
    public decimal TotalPrice
    {
        get
        {
            var subtotal = Quantity * UnitPrice;
            var discount = subtotal * (DiscountPercent / 100);
            return subtotal - discount;
        }
    }

    // Formatted display property
    public string DisplayName
    {
        get
        {
            if (Quantity == 0 || UnitPrice == 0)
                return "(No items)";
            return $"{Quantity} x {UnitPrice:C} = {TotalPrice:C}";
        }
    }

    [Create]
    public void Create() { }
}
#endregion

// -----------------------------------------------------------------------------
// Property Change Notifications
// -----------------------------------------------------------------------------

/// <summary>
/// Entity demonstrating property change notification patterns.
/// </summary>
[Factory]
public partial class SkillPropNotifyEntity : EntityBase<SkillPropNotifyEntity>
{
    public SkillPropNotifyEntity(IEntityBaseServices<SkillPropNotifyEntity> services) : base(services) { }

    public partial string Name { get; set; }

    public partial decimal Amount { get; set; }

    [Create]
    public void Create() { }
}

// The test file demonstrates the notification patterns:
// #region properties-property-changed - Standard PropertyChanged event
// #region properties-neatoo-property-changed - Extended NeatooPropertyChanged with ChangeReason

// -----------------------------------------------------------------------------
// LoadValue - Data Loading Without Triggering Rules
// -----------------------------------------------------------------------------

/// <summary>
/// Entity demonstrating LoadValue for data loading.
/// </summary>
[Factory]
public partial class SkillPropInvoice : ValidateBase<SkillPropInvoice>
{
    public SkillPropInvoice(IValidateBaseServices<SkillPropInvoice> services) : base(services)
    {
        // Validation rule that we don't want to run during data loading
        RuleManager.AddValidation(
            inv => inv.Amount > 0 ? "" : "Amount must be positive",
            i => i.Amount);
    }

    public partial string InvoiceNumber { get; set; }

    public partial decimal Amount { get; set; }

    public partial DateTime InvoiceDate { get; set; }

    [Create]
    public void Create() { }
}

// The test file demonstrates LoadValue:
// #region properties-load-value - Using LoadValue during data loading

// -----------------------------------------------------------------------------
// Meta Properties
// -----------------------------------------------------------------------------

/// <summary>
/// Entity demonstrating meta property access patterns.
/// </summary>
[Factory]
public partial class SkillPropAccount : ValidateBase<SkillPropAccount>
{
    public SkillPropAccount(
        IValidateBaseServices<SkillPropAccount> services,
        ISkillAccountValidationService? validationService = null) : base(services)
    {
        // Sync validation
        RuleManager.AddValidation(
            acc => !string.IsNullOrEmpty(acc.AccountNumber) ? "" : "Account number is required",
            a => a.AccountNumber);

        // Async validation if service provided
        if (validationService != null)
        {
            RuleManager.AddValidationAsync(
                async acc =>
                {
                    if (string.IsNullOrEmpty(acc.Email)) return "";
                    var isValid = await validationService.IsEmailValidAsync(acc.Email);
                    return isValid ? "" : "Email is not valid";
                },
                a => a.Email);
        }
    }

    public partial string AccountNumber { get; set; }

    public partial string Email { get; set; }

    public partial decimal Balance { get; set; }

    [Create]
    public void Create() { }
}

// The test file demonstrates meta properties:
// #region properties-meta-properties - IsBusy, IsValid, PropertyMessages, etc.
// #region properties-backing-field-access - Accessing property wrapper via indexer
// #region properties-suppress-events - PauseAllActions for batch updates
// #region properties-change-reason-useredit - ChangeReason tracking

// -----------------------------------------------------------------------------
// Service interfaces
// -----------------------------------------------------------------------------

public interface ISkillAccountValidationService
{
    Task<bool> IsEmailValidAsync(string email);
}

// -----------------------------------------------------------------------------
// Generated Implementation Sample (for source-generation.md)
// -----------------------------------------------------------------------------

#region properties-generated-implementation
// The source generator creates backing fields for each partial property:
//
// private IValidateProperty<string> NameProperty;
//
// public partial string Name
// {
//     get => NameProperty.Value;
//     set
//     {
//         NameProperty.Value = value;
//         TaskManager.Add(NameProperty.Task);
//     }
// }
//
// Access the generated property wrapper via indexer:
// var property = entity["Name"];
// property.Value, property.IsValid, property.PropertyMessages, etc.
#endregion
