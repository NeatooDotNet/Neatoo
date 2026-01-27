using Neatoo;
using Neatoo.RemoteFactory;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Neatoo.Skills.Domain;

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
// Blazor Component Patterns (Razor markup as comments)
// -----------------------------------------------------------------------------

#region blazor-text-field-basic
// Basic text field binding:
//
// <MudNeatooTextField T="string" EntityProperty="@employee["Name"]" />
//
// The component:
// - Binds to the property value
// - Uses DisplayName from DataAnnotations
// - Shows validation errors automatically
// - Disables while IsBusy is true
#endregion

#region blazor-validation-inline
// Validation errors appear inline:
//
// <MudNeatooTextField T="string" EntityProperty="@employee["Email"]" />
//
// When property fails validation:
// - Error styling applied to input
// - Error message shown below field
// - Component waits for async validation before showing errors
#endregion

#region blazor-validation-summary
// Show all validation errors in one place:
//
// <NeatooValidationSummary Entity="@employee" />
//
// With options:
// <NeatooValidationSummary Entity="@employee"
//                          ShowHeader="true"
//                          HeaderText="Please fix these errors:"
//                          IncludePropertyNames="true" />
//
// entity.PropertyMessages provides all error messages
#endregion

#region blazor-form-submit
// Form with validation and submit:
//
// <MudForm @ref="form">
//     <MudNeatooTextField T="string" EntityProperty="@employee["Name"]" />
//     <MudNeatooTextField T="string" EntityProperty="@employee["Email"]" />
//     <MudNeatooNumericField T="decimal" EntityProperty="@employee["Salary"]" />
//
//     <MudButton OnClick="@Submit"
//                Disabled="@(!employee.IsValid || employee.IsBusy)">
//         Save
//     </MudButton>
// </MudForm>
//
// @code {
//     private MudForm form;
//     private SkillBlazorEmployee employee;
//
//     private async Task Submit()
//     {
//         await form.Validate();
//         if (employee.IsValid)
//         {
//             await employeeFactory.SaveAsync(employee);
//         }
//     }
// }
#endregion

#region blazor-busy-state
// Busy state during async validation:
//
// <MudNeatooTextField T="string"
//                     EntityProperty="@employee["Email"]" />
//
// Component automatically:
// - Sets Disabled="true" while IsBusy is true
// - Shows loading indicator during async validation
// - Re-enables when validation completes
//
// In code:
// if (employee.IsBusy)
// {
//     // Show spinner, disable submit button
// }
#endregion

#region blazor-readonly-property
// Read-only property binding:
//
// <MudNeatooTextField T="string"
//                     EntityProperty="@entity["CreatedBy"]" />
//
// Component respects IsReadOnly property:
// - ReadOnly="@EntityProperty.IsReadOnly"
// - Field displays value but cannot be edited
// - Typically set during entity initialization
#endregion

#region blazor-select-enum
// Dropdown for enum properties:
//
// <MudNeatooSelect T="SkillPriority" EntityProperty="@employee["Priority"]">
//     <MudSelectItem Value="SkillPriority.Low">Low</MudSelectItem>
//     <MudSelectItem Value="SkillPriority.Medium">Medium</MudSelectItem>
//     <MudSelectItem Value="SkillPriority.High">High</MudSelectItem>
//     <MudSelectItem Value="SkillPriority.Critical">Critical</MudSelectItem>
// </MudNeatooSelect>
//
// Or generate items from enum:
// @foreach (var priority in Enum.GetValues<SkillPriority>())
// {
//     <MudSelectItem Value="@priority">@priority.ToString()</MudSelectItem>
// }
#endregion

#region blazor-checkbox-binding
// Checkbox for boolean properties:
//
// <MudNeatooCheckBox T="bool" EntityProperty="@employee["IsActive"]" />
//
// Or switch style:
// <MudNeatooSwitch T="bool" EntityProperty="@employee["IsActive"]" />
//
// Binds to bool property with automatic two-way binding
#endregion

#region blazor-date-picker
// Date picker binding:
//
// <MudNeatooDatePicker EntityProperty="@employee["StartDate"]" />
//
// With options:
// <MudNeatooDatePicker EntityProperty="@employee["StartDate"]"
//                      DateFormat="yyyy-MM-dd"
//                      MinDate="@DateTime.Today"
//                      Editable="true"
//                      Clearable="true" />
#endregion

#region blazor-numeric-field
// Numeric field binding:
//
// <MudNeatooNumericField T="decimal" EntityProperty="@employee["Salary"]" />
//
// With formatting:
// <MudNeatooNumericField T="decimal"
//                        EntityProperty="@employee["Salary"]"
//                        Format="C2"
//                        Min="0"
//                        Max="1000000"
//                        Adornment="Adornment.Start"
//                        AdornmentText="$" />
#endregion

#region blazor-autocomplete
// Autocomplete binding:
//
// <MudNeatooAutocomplete T="string"
//                        EntityProperty="@employee["Department"]"
//                        SearchFunc="@SearchDepartments"
//                        MinCharacters="2"
//                        DebounceInterval="300" />
//
// @code {
//     private async Task<IEnumerable<string>> SearchDepartments(
//         string value, CancellationToken token)
//     {
//         var departments = new[] { "Engineering", "Sales", "Marketing", "HR" };
//         if (string.IsNullOrEmpty(value))
//             return departments;
//         return departments.Where(d =>
//             d.Contains(value, StringComparison.OrdinalIgnoreCase));
//     }
// }
#endregion

#region blazor-change-tracking
// React to changes with IsModified:
//
// <MudButton OnClick="@Save"
//            Disabled="@(!employee.IsModified || !employee.IsValid)">
//     Save Changes
// </MudButton>
//
// <MudButton OnClick="@Cancel"
//            Disabled="@(!employee.IsModified)">
//     Cancel
// </MudButton>
//
// @if (employee.IsModified)
// {
//     <MudAlert Severity="Severity.Warning">
//         You have unsaved changes
//     </MudAlert>
// }
//
// Track specific changes:
// @if (employee.ModifiedProperties.Contains("Salary"))
// {
//     <MudText>Salary was changed</MudText>
// }
#endregion

#region blazor-customize-appearance
// MudBlazor styling options pass through:
//
// <MudNeatooTextField T="string"
//                     EntityProperty="@employee["Name"]"
//                     Variant="Variant.Filled"
//                     Margin="Margin.Normal"
//                     HelperText="Enter your full legal name"
//                     Placeholder="John Doe"
//                     Adornment="Adornment.Start"
//                     AdornmentIcon="@Icons.Material.Filled.Person"
//                     Class="my-custom-field" />
//
// <MudNeatooNumericField T="decimal"
//                        EntityProperty="@employee["Salary"]"
//                        Variant="Variant.Outlined"
//                        Format="N2"
//                        Adornment="Adornment.Start"
//                        AdornmentText="$"
//                        HideSpinButtons="true" />
#endregion

#region blazor-property-extensions
// Extension methods for custom binding:
//
// @using Neatoo.Blazor.MudNeatoo.Extensions
//
// <MudTextField T="string"
//               Value="@employee.Name"
//               ValueChanged="@(async v => await employee["Name"].SetValue(v))"
//               Error="@employee["Name"].HasErrors()"
//               ErrorText="@employee["Name"].GetErrorText()"
//               Validation="@employee["Name"].GetValidationFunc<string>()" />
//
// Extension methods available:
// - HasErrors() - returns true if property has validation errors
// - GetErrorText() - returns error messages as string
// - GetValidationFunc<T>() - returns validation function for MudBlazor
#endregion

#region blazor-statehaschanged
// Components auto-subscribe to property changes:
//
// MudNeatoo components handle PropertyChanged internally:
//
// protected override void OnInitialized()
// {
//     EntityProperty.PropertyChanged += OnPropertyChanged;
// }
//
// private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
// {
//     if (e.PropertyName is "PropertyMessages" or "IsValid" or "IsBusy" or "IsReadOnly")
//     {
//         InvokeAsync(StateHasChanged);
//     }
// }
//
// public void Dispose()
// {
//     EntityProperty.PropertyChanged -= OnPropertyChanged;
// }
//
// This ensures UI updates when:
// - Validation state changes
// - Async operations start/complete
// - Property becomes read-only
#endregion

#region blazor-manual-binding
// Manual binding for non-standard scenarios:
//
// <MudTextField T="string"
//               Value="@employee.Name"
//               ValueChanged="@OnNameChanged"
//               Error="@(!employee["Name"].IsValid)"
//               ErrorText="@GetErrorText(employee["Name"])"
//               Disabled="@employee["Name"].IsBusy"
//               ReadOnly="@employee["Name"].IsReadOnly" />
//
// @code {
//     private async Task OnNameChanged(string value)
//     {
//         await employee["Name"].SetValue(value);
//     }
//
//     private string GetErrorText(IEntityProperty property)
//     {
//         return string.Join("; ",
//             property.PropertyMessages.Select(m => m.Message));
//     }
// }
//
// Use SetValue for async operations (triggers rules, waits for completion)
// Use LoadValue for direct assignment (skips rules)
#endregion

// -----------------------------------------------------------------------------
// Service Interface
// -----------------------------------------------------------------------------

public interface ISkillEmailValidationService
{
    Task<bool> IsCompanyEmailAsync(string email);
}
