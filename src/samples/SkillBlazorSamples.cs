using Neatoo;
using Neatoo.RemoteFactory;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Samples;

// =============================================================================
// BLAZOR SAMPLES - Demonstrates Neatoo entities used with Blazor components
// =============================================================================

/// <summary>
/// Priority enum for select/dropdown samples.
/// </summary>
public enum SkillPriority
{
    Low,
    Medium,
    High,
    Critical
}

// -----------------------------------------------------------------------------
// Entity for Form Binding
// -----------------------------------------------------------------------------

/// <summary>
/// Employee entity for Blazor form binding samples.
/// </summary>
[Factory]
public partial class SkillBlazorEmployee : EntityBase<SkillBlazorEmployee>
{
    public SkillBlazorEmployee(
        IEntityBaseServices<SkillBlazorEmployee> services,
        ISkillEmailValidationService? emailService = null) : base(services)
    {
        // Async validation for email domain
        if (emailService != null)
        {
            RuleManager.AddValidationAsync(
                async e =>
                {
                    if (string.IsNullOrEmpty(e.Email)) return "";

                    var isValid = await emailService.IsCompanyEmailAsync(e.Email);
                    return isValid ? "" : "Email must be a company email";
                },
                e => e.Email);
        }
    }

    [Required(ErrorMessage = "Name is required")]
    [DisplayName("Full Name")]
    public partial string Name { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email format")]
    [DisplayName("Email Address")]
    public partial string Email { get; set; }

    [Range(0, 1000000, ErrorMessage = "Salary must be between 0 and 1,000,000")]
    [DisplayName("Annual Salary")]
    public partial decimal Salary { get; set; }

    [DisplayName("Start Date")]
    public partial DateTime? StartDate { get; set; }

    [DisplayName("Is Active")]
    public partial bool IsActive { get; set; }

    [DisplayName("Priority Level")]
    public partial SkillPriority Priority { get; set; }

    [DisplayName("Department")]
    public partial string Department { get; set; }

    [DisplayName("Notes")]
    public partial string Notes { get; set; }

    [Create]
    public void Create() { }
}

/// <summary>
/// Audited entity for read-only property samples.
/// </summary>
[Factory]
public partial class SkillBlazorAuditedEntity : EntityBase<SkillBlazorAuditedEntity>
{
    public SkillBlazorAuditedEntity(IEntityBaseServices<SkillBlazorAuditedEntity> services) : base(services) { }

    [DisplayName("Entity ID")]
    public partial string EntityId { get; set; }

    [DisplayName("Name")]
    public partial string Name { get; set; }

    [DisplayName("Created By")]
    public partial string CreatedBy { get; set; }

    [DisplayName("Created At")]
    public partial DateTime CreatedAt { get; set; }

    [Create]
    public void Create()
    {
        CreatedAt = DateTime.UtcNow;
    }
}

// -----------------------------------------------------------------------------
// Service Interface
// -----------------------------------------------------------------------------

public interface ISkillEmailValidationService
{
    Task<bool> IsCompanyEmailAsync(string email);
}
