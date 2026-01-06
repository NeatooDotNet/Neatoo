/// <summary>
/// Code samples for docs/validation-and-rules.md - Data Annotations section
///
/// Snippets injected into docs:
/// - docs:validation-and-rules:required-attribute
/// - docs:validation-and-rules:stringlength-attribute
/// - docs:validation-and-rules:minmaxlength-attribute
/// - docs:validation-and-rules:range-attribute
/// - docs:validation-and-rules:regularexpression-attribute
/// - docs:validation-and-rules:emailaddress-attribute
/// - docs:validation-and-rules:combining-attributes
///
/// Compile-time validation only (wrapper entity for attribute snippets):
/// - docs:validation-and-rules:data-annotations-entity
///
/// Corresponding tests: DataAnnotationSamplesTests.cs
/// </summary>

using Neatoo.RemoteFactory;
using System.ComponentModel.DataAnnotations;

namespace Neatoo.Samples.DomainModel.ValidationAndRules;

#region docs:validation-and-rules:data-annotations-entity
/// <summary>
/// Sample entity demonstrating all supported data annotation attributes.
/// </summary>
[Factory]
internal partial class DataAnnotationsEntity : EntityBase<DataAnnotationsEntity>, IDataAnnotationsEntity
{
    public DataAnnotationsEntity(IEntityBaseServices<DataAnnotationsEntity> services) : base(services) { }

    public partial Guid? Id { get; set; }

    #region docs:validation-and-rules:required-attribute
    [Required]
    public partial string? FirstName { get; set; }

    [Required(ErrorMessage = "Customer name is required")]
    public partial string? CustomerName { get; set; }
    #endregion

    #region docs:validation-and-rules:stringlength-attribute
    // Maximum length only
    [StringLength(100)]
    public partial string? Description { get; set; }

    // Minimum and maximum
    [StringLength(100, MinimumLength = 2)]
    public partial string? Username { get; set; }

    // Custom message
    [StringLength(50, MinimumLength = 5, ErrorMessage = "Name must be 5-50 characters")]
    public partial string? NameWithLength { get; set; }
    #endregion

    #region docs:validation-and-rules:minmaxlength-attribute
    // String minimum length
    [MinLength(3)]
    public partial string? Code { get; set; }

    // String maximum length
    [MaxLength(500)]
    public partial string? Notes { get; set; }

    // Collection minimum count
    [MinLength(1, ErrorMessage = "At least one item required")]
    public partial List<string>? Tags { get; set; }

    // Array maximum count
    [MaxLength(10)]
    public partial string[]? Categories { get; set; }
    #endregion

    #region docs:validation-and-rules:range-attribute
    // Integer range
    [Range(1, 100)]
    public partial int Quantity { get; set; }

    // Double range
    [Range(0.0, 100.0)]
    public partial double Percentage { get; set; }

    // Decimal range (use type-based constructor)
    [Range(typeof(decimal), "0.01", "999.99")]
    public partial decimal Price { get; set; }

    // Date range
    [Range(typeof(DateTime), "2020-01-01", "2030-12-31")]
    public partial DateTime AppointmentDate { get; set; }

    // Custom message
    [Range(0, 150, ErrorMessage = "Age must be between 0 and 150")]
    public partial int Age { get; set; }
    #endregion

    #region docs:validation-and-rules:regularexpression-attribute
    // Code format: 2 letters + 4 digits
    [RegularExpression(@"^[A-Z]{2}\d{4}$")]
    public partial string? ProductCode { get; set; }

    // Phone format
    [RegularExpression(@"^\d{3}-\d{3}-\d{4}$", ErrorMessage = "Format: 555-123-4567")]
    public partial string? Phone { get; set; }

    // Alphanumeric only
    [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Letters and numbers only")]
    public partial string? UsernameAlphanumeric { get; set; }
    #endregion

    #region docs:validation-and-rules:emailaddress-attribute
    [EmailAddress]
    public partial string? Email { get; set; }

    [EmailAddress(ErrorMessage = "Please enter a valid email")]
    public partial string? ContactEmail { get; set; }
    #endregion

    #region docs:validation-and-rules:combining-attributes
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(254, ErrorMessage = "Email too long")]
    public partial string? CombinedEmail { get; set; }

    [Required]
    [Range(1, 1000)]
    public partial int CombinedQuantity { get; set; }

    [StringLength(100, MinimumLength = 2)]
    [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Letters only")]
    public partial string? FullName { get; set; }
    #endregion

    [Create]
    public void Create()
    {
        Id = Guid.NewGuid();
    }
}
#endregion

public partial interface IDataAnnotationsEntity : IEntityBase
{
    Guid? Id { get; set; }
    string? FirstName { get; set; }
    string? CustomerName { get; set; }
    string? Description { get; set; }
    string? Username { get; set; }
    string? NameWithLength { get; set; }
    string? Code { get; set; }
    string? Notes { get; set; }
    List<string>? Tags { get; set; }
    string[]? Categories { get; set; }
    int Quantity { get; set; }
    double Percentage { get; set; }
    decimal Price { get; set; }
    DateTime AppointmentDate { get; set; }
    int Age { get; set; }
    string? ProductCode { get; set; }
    string? Phone { get; set; }
    string? UsernameAlphanumeric { get; set; }
    string? Email { get; set; }
    string? ContactEmail { get; set; }
    string? CombinedEmail { get; set; }
    int CombinedQuantity { get; set; }
    string? FullName { get; set; }
}
