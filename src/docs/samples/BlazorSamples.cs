using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Neatoo;
using Neatoo.Internal;
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
}

// ============================================================================
// RAZOR SYNTAX SNIPPETS (as string constants)
// These demonstrate the Blazor component usage patterns.
// ============================================================================

/// <summary>
/// Blazor documentation samples showing MudNeatoo component usage patterns.
/// </summary>
public class BlazorSamples
{
    #region blazor-installation
    // Package installation commands (run in terminal):
    // dotnet add package Neatoo.Blazor.MudNeatoo
    // dotnet add package MudBlazor

    // In Program.cs, add MudBlazor services:
    // builder.Services.AddMudServices();
    #endregion

    #region blazor-text-field-basic
    // Razor component markup:
    // <MudNeatooTextField T="string" EntityProperty="@employee["Name"]" />

    [Fact]
    public void TextFieldBindsToEntityProperty()
    {
        var employee = new BlazorEmployee(new EntityBaseServices<BlazorEmployee>());

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
    // Razor component markup with validation:
    // <MudNeatooTextField T="string" EntityProperty="@employee["Email"]" />
    //
    // Validation errors appear automatically below the input when the
    // property fails validation. The component waits for async validation
    // to complete before displaying error messages.

    [Fact]
    public void ValidationDisplaysInlineErrors()
    {
        var employee = new BlazorEmployee(new EntityBaseServices<BlazorEmployee>());

        // Invalid email format triggers validation error
        employee.Email = "not-an-email";

        var emailProperty = employee["Email"];
        Assert.False(emailProperty.IsValid);
        Assert.NotEmpty(emailProperty.PropertyMessages);
    }
    #endregion

    #region blazor-validation-summary
    // Razor component markup:
    // <NeatooValidationSummary Entity="@employee" />
    //
    // Displays all validation errors in a MudAlert:
    // <NeatooValidationSummary Entity="@employee"
    //                          ShowHeader="true"
    //                          HeaderText="Please fix these errors:"
    //                          IncludePropertyNames="true" />

    [Fact]
    public void ValidationSummaryShowsAllErrors()
    {
        var employee = new BlazorEmployee(new EntityBaseServices<BlazorEmployee>());

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
    // Razor component markup:
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
    //
    //     private async Task Submit()
    //     {
    //         await form.Validate();
    //         if (employee.IsValid)
    //         {
    //             // Save the entity
    //         }
    //     }
    // }

    [Fact]
    public async Task FormValidationPreventsInvalidSubmit()
    {
        var employee = new BlazorEmployee(new EntityBaseServices<BlazorEmployee>());

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
    // Razor component markup:
    // <MudNeatooTextField T="string"
    //                     EntityProperty="@employee["Email"]" />
    //
    // The component automatically disables while IsBusy is true.
    // This prevents user input during async rule execution.
    //
    // Component renders as disabled:
    // <MudTextField ... Disabled="@EntityProperty.IsBusy" ... />

    [Fact]
    public async Task BusyStateDisablesComponent()
    {
        var employee = new BlazorEmployee(new EntityBaseServices<BlazorEmployee>());

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
    // Razor component markup:
    // <MudNeatooTextField T="string" EntityProperty="@entity["CreatedBy"]" />
    //
    // The component respects IsReadOnly:
    // <MudTextField ... ReadOnly="@EntityProperty.IsReadOnly" ... />
    //
    // When IsReadOnly is true, the component displays as read-only.
    // This is typically set during entity initialization or by business rules.

    [Fact]
    public void ReadOnlyPropertyBindsToComponent()
    {
        var entity = new BlazorAuditedEntity(new EntityBaseServices<BlazorAuditedEntity>());

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
    // Razor component markup:
    // <MudNeatooSelect T="Priority" EntityProperty="@employee["Priority"]">
    //     <MudSelectItem Value="Priority.Low">Low</MudSelectItem>
    //     <MudSelectItem Value="Priority.Medium">Medium</MudSelectItem>
    //     <MudSelectItem Value="Priority.High">High</MudSelectItem>
    //     <MudSelectItem Value="Priority.Critical">Critical</MudSelectItem>
    // </MudNeatooSelect>

    [Fact]
    public void SelectBindsToEnumProperty()
    {
        var employee = new BlazorEmployee(new EntityBaseServices<BlazorEmployee>());

        // Set enum value
        employee.Priority = Priority.High;

        var priorityProperty = employee["Priority"];
        Assert.Equal(Priority.High, priorityProperty.Value);
    }
    #endregion

    #region blazor-checkbox-binding
    // Razor component markup:
    // <MudNeatooCheckBox T="bool" EntityProperty="@employee["IsActive"]" />
    //
    // Or with switch style:
    // <MudNeatooSwitch T="bool" EntityProperty="@employee["IsActive"]" />

    [Fact]
    public void CheckboxBindsToBooleanProperty()
    {
        var employee = new BlazorEmployee(new EntityBaseServices<BlazorEmployee>());

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
    // Razor component markup:
    // <MudNeatooDatePicker EntityProperty="@employee["StartDate"]" />
    //
    // With additional options:
    // <MudNeatooDatePicker EntityProperty="@employee["StartDate"]"
    //                      DateFormat="yyyy-MM-dd"
    //                      MinDate="@DateTime.Today"
    //                      Editable="true"
    //                      Clearable="true" />

    [Fact]
    public void DatePickerBindsToDateProperty()
    {
        var employee = new BlazorEmployee(new EntityBaseServices<BlazorEmployee>());

        var startDate = new DateTime(2024, 1, 15);
        employee.StartDate = startDate;

        var startDateProperty = employee["StartDate"];
        Assert.Equal(startDate, startDateProperty.Value);
    }
    #endregion

    #region blazor-numeric-field
    // Razor component markup:
    // <MudNeatooNumericField T="decimal" EntityProperty="@employee["Salary"]" />
    //
    // With formatting and constraints:
    // <MudNeatooNumericField T="decimal"
    //                        EntityProperty="@employee["Salary"]"
    //                        Format="C2"
    //                        Min="0"
    //                        Max="1000000"
    //                        Adornment="Adornment.Start"
    //                        AdornmentText="$" />

    [Fact]
    public void NumericFieldBindsToDecimalProperty()
    {
        var employee = new BlazorEmployee(new EntityBaseServices<BlazorEmployee>());

        employee.Salary = 75000.50m;

        var salaryProperty = employee["Salary"];
        Assert.Equal(75000.50m, salaryProperty.Value);

        // Validation enforces range
        employee.Salary = -1000;
        Assert.False(salaryProperty.IsValid);
    }
    #endregion

    #region blazor-autocomplete
    // Razor component markup:
    // <MudNeatooAutocomplete T="string"
    //                        EntityProperty="@employee["Department"]"
    //                        SearchFunc="@SearchDepartments"
    //                        MinCharacters="2"
    //                        DebounceInterval="300" />
    //
    // @code {
    //     private async Task<IEnumerable<string>> SearchDepartments(string value,
    //         CancellationToken token)
    //     {
    //         var departments = new[] { "Engineering", "Sales", "Marketing", "HR" };
    //
    //         if (string.IsNullOrEmpty(value))
    //             return departments;
    //
    //         return departments.Where(d =>
    //             d.Contains(value, StringComparison.OrdinalIgnoreCase));
    //     }
    // }

    [Fact]
    public void AutocompleteBindsToStringProperty()
    {
        var employee = new BlazorEmployee(new EntityBaseServices<BlazorEmployee>());

        employee.Department = "Engineering";

        var deptProperty = employee["Department"];
        Assert.Equal("Engineering", deptProperty.Value);
    }
    #endregion

    #region blazor-change-tracking
    // Razor component markup:
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

    [Fact]
    public void ChangeTrackingDetectsModifications()
    {
        var employee = new BlazorEmployee(new EntityBaseServices<BlazorEmployee>());

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
    // Razor component markup with MudBlazor styling:
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

    [Fact]
    public void ComponentAcceptsStyleParameters()
    {
        var employee = new BlazorEmployee(new EntityBaseServices<BlazorEmployee>());

        // Properties are accessible for binding
        var nameProperty = employee["Name"];
        Assert.NotNull(nameProperty.DisplayName);

        // All MudBlazor parameters pass through to the underlying component
        // (Variant, Margin, HelperText, Adornment, etc.)
    }
    #endregion

    #region blazor-property-extensions
    // Using extension methods for custom binding to standard MudBlazor components:
    //
    // @using Neatoo.Blazor.MudNeatoo.Extensions
    //
    // <MudTextField T="string"
    //               Value="@employee.Name"
    //               ValueChanged="@(async v => await employee["Name"].SetValue(v))"
    //               Error="@employee["Name"].HasErrors()"
    //               ErrorText="@employee["Name"].GetErrorText()"
    //               Validation="@employee["Name"].GetValidationFunc<string>()" />

    [Fact]
    public void ExtensionMethodsProvideValidationInfo()
    {
        var employee = new BlazorEmployee(new EntityBaseServices<BlazorEmployee>());
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
    // MudNeatoo components automatically subscribe to property change events.
    // When validation state, busy state, or values change, the component re-renders.
    //
    // Internal component lifecycle:
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

    [Fact]
    public void PropertyChangesNotifyComponents()
    {
        var employee = new BlazorEmployee(new EntityBaseServices<BlazorEmployee>());
        var nameProperty = employee["Name"];

        var changedProperties = new List<string>();
        nameProperty.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName ?? "");

        // Setting value triggers PropertyChanged
        employee.Name = "Test";

        Assert.NotEmpty(changedProperties);
    }
    #endregion

    #region blazor-manual-binding
    // For scenarios not covered by MudNeatoo components, bind manually:
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
    //         return string.Join("; ", property.PropertyMessages.Select(m => m.Message));
    //     }
    // }

    [Fact]
    public async Task ManualBindingUsesSetValueAsync()
    {
        var employee = new BlazorEmployee(new EntityBaseServices<BlazorEmployee>());
        var nameProperty = employee["Name"];

        // Manual binding pattern: use SetValue for async
        await nameProperty.SetValue("Manual Value");

        Assert.Equal("Manual Value", employee.Name);
    }
    #endregion
}
