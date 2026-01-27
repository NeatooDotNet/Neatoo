using Neatoo;
using Neatoo.RemoteFactory;
using Neatoo.Rules;
using System.ComponentModel.DataAnnotations;

namespace Neatoo.Skills.Domain;

// =============================================================================
// VALIDATION SAMPLES - Demonstrates validation rules, attributes, and patterns
// =============================================================================

// -----------------------------------------------------------------------------
// Basic Validation with RuleManager
// -----------------------------------------------------------------------------

#region validation-basic
/// <summary>
/// Product entity demonstrating basic validation rules.
/// </summary>
[Factory]
public partial class SkillValidProduct : ValidateBase<SkillValidProduct>
{
    public SkillValidProduct(IValidateBaseServices<SkillValidProduct> services) : base(services)
    {
        // Lambda validation rule - returns error message or empty string
        RuleManager.AddValidation(
            product => !string.IsNullOrEmpty(product.Name) ? "" : "Name is required",
            p => p.Name);

        RuleManager.AddValidation(
            product => product.Price >= 0 ? "" : "Price cannot be negative",
            p => p.Price);

        RuleManager.AddValidation(
            product => product.Quantity >= 0 ? "" : "Quantity cannot be negative",
            p => p.Quantity);
    }

    public partial string Name { get; set; }

    public partial decimal Price { get; set; }

    public partial int Quantity { get; set; }

    [Create]
    public void Create() { }
}
#endregion

// -----------------------------------------------------------------------------
// DataAnnotations Validation Attributes
// -----------------------------------------------------------------------------

#region validation-attributes
/// <summary>
/// Registration entity demonstrating DataAnnotations attributes.
/// Neatoo automatically converts these to validation rules.
/// </summary>
[Factory]
public partial class SkillValidRegistration : ValidateBase<SkillValidRegistration>
{
    public SkillValidRegistration(IValidateBaseServices<SkillValidRegistration> services) : base(services) { }

    [Required(ErrorMessage = "Username is required")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be 3-50 characters")]
    public partial string Username { get; set; }

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public partial string Email { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    public partial string Password { get; set; }

    [Phone(ErrorMessage = "Invalid phone number")]
    public partial string PhoneNumber { get; set; }

    [Range(18, 120, ErrorMessage = "Age must be between 18 and 120")]
    public partial int Age { get; set; }

    [RegularExpression(@"^\d{5}(-\d{4})?$", ErrorMessage = "Invalid ZIP code format")]
    public partial string ZipCode { get; set; }

    [Create]
    public void Create() { }
}
#endregion

// -----------------------------------------------------------------------------
// Custom Validation Rule Classes
// -----------------------------------------------------------------------------

/// <summary>
/// Custom rule class for reusable validation logic.
/// </summary>
public class SkillSalaryRangeRule : RuleBase<SkillValidEmployee>
{
    private readonly decimal _minSalary;
    private readonly decimal _maxSalary;

    public SkillSalaryRangeRule(decimal minSalary, decimal maxSalary) : base(e => e.Salary)
    {
        _minSalary = minSalary;
        _maxSalary = maxSalary;
    }

    protected override IRuleMessages Execute(SkillValidEmployee target)
    {
        if (target.Salary < _minSalary)
        {
            return (nameof(target.Salary), $"Salary must be at least {_minSalary:C}").AsRuleMessages();
        }

        if (target.Salary > _maxSalary)
        {
            return (nameof(target.Salary), $"Salary cannot exceed {_maxSalary:C}").AsRuleMessages();
        }

        return None;
    }
}

#region validation-custom-rule
/// <summary>
/// Employee entity with custom rule class.
/// </summary>
[Factory]
public partial class SkillValidEmployee : ValidateBase<SkillValidEmployee>
{
    public SkillValidEmployee(IValidateBaseServices<SkillValidEmployee> services) : base(services)
    {
        // Register custom rule class
        RuleManager.AddRule(new SkillSalaryRangeRule(30000m, 500000m));

        // Lambda rule for name
        RuleManager.AddValidation(
            emp => !string.IsNullOrEmpty(emp.Name) ? "" : "Name is required",
            e => e.Name);
    }

    public partial string Name { get; set; }

    public partial string Department { get; set; }

    public partial decimal Salary { get; set; }

    [Create]
    public void Create() { }
}
#endregion

// -----------------------------------------------------------------------------
// Cross-Property Validation
// -----------------------------------------------------------------------------

/// <summary>
/// Date range rule that validates end date is after start date.
/// </summary>
public class SkillDateRangeRule : RuleBase<SkillValidDateRange>
{
    // Rule triggers when either StartDate OR EndDate changes
    public SkillDateRangeRule() : base(r => r.StartDate, r => r.EndDate) { }

    protected override IRuleMessages Execute(SkillValidDateRange target)
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

#region validation-cross-property
/// <summary>
/// Entity demonstrating cross-property validation.
/// </summary>
[Factory]
public partial class SkillValidDateRange : ValidateBase<SkillValidDateRange>
{
    public SkillValidDateRange(IValidateBaseServices<SkillValidDateRange> services) : base(services)
    {
        // Cross-property rule: validates relationship between two properties
        RuleManager.AddRule(new SkillDateRangeRule());
    }

    public partial DateTime StartDate { get; set; }

    public partial DateTime EndDate { get; set; }

    public partial string Description { get; set; }

    [Create]
    public void Create() { }
}
#endregion

// -----------------------------------------------------------------------------
// Async Validation Rules
// -----------------------------------------------------------------------------

#region validation-async-rule
/// <summary>
/// User entity with async validation for email uniqueness.
/// </summary>
[Factory]
public partial class SkillValidUser : ValidateBase<SkillValidUser>
{
    public SkillValidUser(
        IValidateBaseServices<SkillValidUser> services,
        ISkillUserValidationService validationService) : base(services)
    {
        // Async validation rule - checks external service
        RuleManager.AddValidationAsync(
            async user =>
            {
                if (string.IsNullOrEmpty(user.Email))
                    return "";

                var isUnique = await validationService.IsEmailUniqueAsync(user.Email);
                return isUnique ? "" : "Email is already in use";
            },
            u => u.Email);
    }

    [Required]
    public partial string Username { get; set; }

    [Required]
    [EmailAddress]
    public partial string Email { get; set; }

    [Create]
    public void Create() { }
}
#endregion

// -----------------------------------------------------------------------------
// Validation Cascade (Parent-Child)
// -----------------------------------------------------------------------------

/// <summary>
/// Line item for cascade validation samples.
/// </summary>
public interface ISkillValidLineItem : IValidateBase
{
    string Description { get; set; }
    decimal Amount { get; set; }
}

[Factory]
public partial class SkillValidLineItem : ValidateBase<SkillValidLineItem>, ISkillValidLineItem
{
    public SkillValidLineItem(IValidateBaseServices<SkillValidLineItem> services) : base(services)
    {
        RuleManager.AddValidation(
            item => !string.IsNullOrEmpty(item.Description) ? "" : "Description is required",
            i => i.Description);
    }

    public partial string Description { get; set; }

    public partial decimal Amount { get; set; }

    [Create]
    public void Create() { }
}

public interface ISkillValidLineItemList : IValidateListBase<ISkillValidLineItem> { }

public class SkillValidLineItemList : ValidateListBase<ISkillValidLineItem>, ISkillValidLineItemList { }

#region validation-cascade
/// <summary>
/// Invoice entity demonstrating validation cascade to children.
/// </summary>
[Factory]
public partial class SkillValidInvoice : ValidateBase<SkillValidInvoice>
{
    public SkillValidInvoice(IValidateBaseServices<SkillValidInvoice> services) : base(services)
    {
        LineItemsProperty.LoadValue(new SkillValidLineItemList());

        RuleManager.AddValidation(
            inv => !string.IsNullOrEmpty(inv.InvoiceNumber) ? "" : "Invoice number is required",
            i => i.InvoiceNumber);
    }

    public partial string InvoiceNumber { get; set; }

    public partial ISkillValidLineItemList LineItems { get; set; }

    [Create]
    public void Create() { }
}
// Parent.IsValid reflects child validation state:
// - If any child is invalid, parent.IsValid is false
// - Parent.IsSelfValid only checks parent's own properties
#endregion

#region validation-meta-properties
// Validation meta properties available:
//
// entity.IsValid          - Object and children pass validation
// entity.IsSelfValid      - Only this object's properties
// entity.IsBusy           - Async operations running
// entity.PropertyMessages - All error messages
// entity.ObjectInvalid    - Object-level error message (from MarkInvalid)
#endregion

#region validation-before-save
// EntityBase checks validation before save:
//
// entity.IsSavable = entity.IsValid && entity.IsModified && !entity.IsBusy && !entity.IsChild
//
// Save() will fail if !IsSavable
#endregion

// -----------------------------------------------------------------------------
// Service interfaces
// -----------------------------------------------------------------------------

public interface ISkillUserValidationService
{
    Task<bool> IsEmailUniqueAsync(string email);
}
