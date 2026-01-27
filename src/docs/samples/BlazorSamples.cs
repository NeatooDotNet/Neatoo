using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Neatoo;
using Neatoo.RemoteFactory;
using Neatoo.Rules;
using Xunit;

namespace Samples;

// ============================================================================
// BLAZOR SAMPLE ENTITIES
// These entities are used in the Blazor documentation snippets.
// ============================================================================

/// <summary>
/// Priority levels for demonstration.
/// </summary>
public enum Priority
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Employee entity for Blazor form samples.
/// </summary>
[Factory]
public partial class BlazorEmployee : EntityBase<BlazorEmployee>
{
    public BlazorEmployee(IEntityBaseServices<BlazorEmployee> services) : base(services)
    {
        // Async validation rule: validates email domain
        RuleManager.AddValidationAsync(
            async e =>
            {
                if (string.IsNullOrEmpty(e.Email))
                    return "";

                // Simulate async validation (e.g., checking against external service)
                await Task.Delay(50);

                if (!e.Email.EndsWith("@company.com", StringComparison.OrdinalIgnoreCase))
                {
                    return "Email must be a company email (@company.com)";
                }

                return "";
            },
            e => e.Email);
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
    public partial Priority Priority { get; set; }

    [DisplayName("Department")]
    public partial string Department { get; set; }

    [DisplayName("Notes")]
    public partial string Notes { get; set; }

    [Create]
    public void Create() { }
}

/// <summary>
/// Entity demonstrating read-only property binding.
/// </summary>
[Factory]
public partial class BlazorAuditedEntity : EntityBase<BlazorAuditedEntity>
{
    public BlazorAuditedEntity(IEntityBaseServices<BlazorAuditedEntity> services) : base(services) { }

    [DisplayName("Employee ID")]
    public partial string EmployeeId { get; set; }

    [DisplayName("Name")]
    public partial string Name { get; set; }

    [DisplayName("Created By")]
    public partial string CreatedBy { get; set; }

    [Create]
    public void Create() { }
}

// ============================================================================
// RAZOR SYNTAX SNIPPETS (as string constants)
// These demonstrate the Blazor component usage patterns.
// ============================================================================

/// <summary>
/// Blazor documentation samples showing MudNeatoo component usage patterns.
/// </summary>
public class BlazorSamples : SamplesTestBase
{
    #region blazor-text-field-basic
    [Fact]
    public void TextFieldBindsToEntityProperty()
    {
        var factory = GetRequiredService<IBlazorEmployeeFactory>();
        var employee = factory.Create();

        // Access the property through indexer
        var nameProperty = employee["Name"];

        // Property has display name from DisplayAttribute
        Assert.Equal("Full Name", nameProperty.DisplayName);

        // Set value through property (simulates component binding)
        employee.Name = "Alice Johnson";
        Assert.Equal("Alice Johnson", nameProperty.Value);
    }
    #endregion

    #region blazor-validation-inline
    [Fact]
    public void ValidationDisplaysInlineErrors()
    {
        var factory = GetRequiredService<IBlazorEmployeeFactory>();
        var employee = factory.Create();

        // Invalid email format triggers validation error
        employee.Email = "not-an-email";

        var emailProperty = employee["Email"];
        Assert.False(emailProperty.IsValid);
        Assert.NotEmpty(emailProperty.PropertyMessages);
    }
    #endregion

    #region blazor-validation-summary
    [Fact]
    public void ValidationSummaryShowsAllErrors()
    {
        var factory = GetRequiredService<IBlazorEmployeeFactory>();
        var employee = factory.Create();

        // Create multiple validation errors
        employee.Name = "";
        employee.Email = "invalid";
        employee.Salary = -1000;

        // Entity aggregates all property messages
        Assert.False(employee.IsValid);
        Assert.True(employee.PropertyMessages.Count >= 2);
    }
    #endregion

    #region blazor-form-submit
    [Fact]
    public async Task FormValidationPreventsInvalidSubmit()
    {
        var factory = GetRequiredService<IBlazorEmployeeFactory>();
        var employee = factory.Create();

        // Run validation to trigger required field checks
        await employee.RunRules();

        // Empty form is invalid due to required Name field
        Assert.False(employee.IsValid);

        // Fill required fields
        employee.Name = "Bob Smith";
        employee.Email = "bob@company.com";
        employee.Salary = 50000;

        // Wait for async validation to complete
        await employee.WaitForTasks();

        // Now valid for submission
        Assert.True(employee.IsValid);
    }
    #endregion

    #region blazor-busy-state
    [Fact]
    public async Task BusyStateDisablesComponent()
    {
        var factory = GetRequiredService<IBlazorEmployeeFactory>();
        var employee = factory.Create();

        // Wait for any initial async operations to complete
        await employee.WaitForTasks();

        var emailProperty = employee["Email"];
        Assert.False(emailProperty.IsBusy);

        // Set email to trigger async validation
        employee.Email = "test@example.com";

        // Wait for async rules to complete
        await employee.WaitForTasks();

        // Property is no longer busy after rules complete
        Assert.False(emailProperty.IsBusy);
    }
    #endregion

    #region blazor-readonly-property
    [Fact]
    public void ReadOnlyPropertyBindsToComponent()
    {
        var factory = GetRequiredService<IBlazorAuditedEntityFactory>();
        var entity = factory.Create();

        // Set value
        entity.CreatedBy = "admin";

        // Property has IsReadOnly property that components bind to
        var createdByProperty = entity["CreatedBy"];

        // When IsReadOnly is true, MudNeatoo components render as read-only
        // The default value depends on property configuration
        Assert.NotNull(createdByProperty);
        Assert.Equal("admin", entity.CreatedBy);
    }
    #endregion

    #region blazor-select-enum
    [Fact]
    public void SelectBindsToEnumProperty()
    {
        var factory = GetRequiredService<IBlazorEmployeeFactory>();
        var employee = factory.Create();

        // Set enum value
        employee.Priority = Priority.High;

        var priorityProperty = employee["Priority"];
        Assert.Equal(Priority.High, priorityProperty.Value);
    }
    #endregion

    #region blazor-checkbox-binding
    [Fact]
    public void CheckboxBindsToBooleanProperty()
    {
        var factory = GetRequiredService<IBlazorEmployeeFactory>();
        var employee = factory.Create();

        // Toggle boolean value
        employee.IsActive = true;

        var isActiveProperty = employee["IsActive"];
        Assert.Equal(true, isActiveProperty.Value);

        // Toggle again
        employee.IsActive = false;
        Assert.Equal(false, isActiveProperty.Value);
    }
    #endregion

    #region blazor-date-picker
    [Fact]
    public void DatePickerBindsToDateProperty()
    {
        var factory = GetRequiredService<IBlazorEmployeeFactory>();
        var employee = factory.Create();

        var startDate = new DateTime(2024, 1, 15);
        employee.StartDate = startDate;

        var startDateProperty = employee["StartDate"];
        Assert.Equal(startDate, startDateProperty.Value);
    }
    #endregion

    #region blazor-numeric-field
    [Fact]
    public void NumericFieldBindsToDecimalProperty()
    {
        var factory = GetRequiredService<IBlazorEmployeeFactory>();
        var employee = factory.Create();

        employee.Salary = 75000.50m;

        var salaryProperty = employee["Salary"];
        Assert.Equal(75000.50m, salaryProperty.Value);

        // Validation enforces range
        employee.Salary = -1000;
        Assert.False(salaryProperty.IsValid);
    }
    #endregion

    #region blazor-autocomplete
    [Fact]
    public void AutocompleteBindsToStringProperty()
    {
        var factory = GetRequiredService<IBlazorEmployeeFactory>();
        var employee = factory.Create();

        employee.Department = "Engineering";

        var deptProperty = employee["Department"];
        Assert.Equal("Engineering", deptProperty.Value);
    }
    #endregion

    #region blazor-change-tracking
    [Fact]
    public void ChangeTrackingDetectsModifications()
    {
        var factory = GetRequiredService<IBlazorEmployeeFactory>();
        var employee = factory.Create();

        // New entity starts unmodified
        Assert.False(employee.IsSelfModified);

        // Making changes sets IsModified
        employee.Name = "Changed Name";
        Assert.True(employee.IsSelfModified);
        Assert.True(employee.IsModified);

        // Track which properties changed
        Assert.Contains("Name", employee.ModifiedProperties);
    }
    #endregion

    #region blazor-customize-appearance
    [Fact]
    public void ComponentAcceptsStyleParameters()
    {
        var factory = GetRequiredService<IBlazorEmployeeFactory>();
        var employee = factory.Create();

        // Properties are accessible for binding
        var nameProperty = employee["Name"];
        Assert.NotNull(nameProperty.DisplayName);

        // All MudBlazor parameters pass through to the underlying component
        // (Variant, Margin, HelperText, Adornment, etc.)
    }
    #endregion

    #region blazor-property-extensions
    [Fact]
    public void ExtensionMethodsProvideValidationInfo()
    {
        var factory = GetRequiredService<IBlazorEmployeeFactory>();
        var employee = factory.Create();
        employee.Email = "invalid";

        var emailProperty = employee["Email"];

        // Extension method pattern (simulated - actual extensions in Neatoo.Blazor.MudNeatoo)
        var hasErrors = emailProperty.PropertyMessages.Any();
        var errorText = string.Join("; ", emailProperty.PropertyMessages.Select(m => m.Message));

        Assert.True(hasErrors);
        Assert.NotEmpty(errorText);
    }
    #endregion

    #region blazor-statehaschanged
    [Fact]
    public void PropertyChangesNotifyComponents()
    {
        var factory = GetRequiredService<IBlazorEmployeeFactory>();
        var employee = factory.Create();
        var nameProperty = employee["Name"];

        var changedProperties = new List<string>();
        nameProperty.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName ?? "");

        // Setting value triggers PropertyChanged
        employee.Name = "Test";

        Assert.NotEmpty(changedProperties);
    }
    #endregion

    #region blazor-manual-binding
    [Fact]
    public async Task ManualBindingUsesSetValueAsync()
    {
        var factory = GetRequiredService<IBlazorEmployeeFactory>();
        var employee = factory.Create();
        var nameProperty = employee["Name"];

        // Manual binding pattern: use SetValue for async
        await nameProperty.SetValue("Manual Value");

        Assert.Equal("Manual Value", employee.Name);
    }
    #endregion
}
