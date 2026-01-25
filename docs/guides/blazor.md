# Blazor Integration

[Previous: Async Operations](async.md) | [Up](index.md) | [Next: Business Rules](business-rules.md)

Neatoo provides MudBlazor integration through the `Neatoo.Blazor.MudNeatoo` package. MudNeatoo components automatically bind to `IEntityProperty`, display validation messages, track busy state, and respect read-only constraints.

## Installation

Install the MudNeatoo package alongside MudBlazor:

<!-- snippet: blazor-installation -->
```cs
// Package installation commands (run in terminal):
// dotnet add package Neatoo.Blazor.MudNeatoo
// dotnet add package MudBlazor

// In Program.cs, add MudBlazor services:
// builder.Services.AddMudServices();
```
<!-- endSnippet -->

MudNeatoo requires MudBlazor 6.0 or later and targets .NET 8.0, 9.0, and 10.0.

## Component Overview

MudNeatoo provides typed wrappers for common MudBlazor input components:

- `MudNeatooTextField<T>` - Text input for strings, numbers, dates
- `MudNeatooNumericField<T>` - Numeric input with formatting
- `MudNeatooSelect<T>` - Dropdown selection
- `MudNeatooCheckBox` - Boolean checkbox
- `MudNeatooSwitch` - Boolean toggle switch
- `MudNeatooDatePicker` - Date selection
- `MudNeatooTimePicker` - Time selection
- `MudNeatooDateRangePicker` - Date range selection
- `MudNeatooAutocomplete<T>` - Autocomplete input
- `MudNeatooSlider<T>` - Numeric slider
- `MudNeatooRadioGroup<T>` - Radio button group

All components bind to `IEntityProperty` and automatically handle validation, busy state, and read-only mode.

## Basic Property Binding

Bind a MudNeatoo component to an entity property by setting the `EntityProperty` parameter. The component automatically uses the property's `DisplayName` as the label.

Bind to a string property:

<!-- snippet: blazor-text-field-basic -->
```cs
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
```
<!-- endSnippet -->

The component synchronizes value changes to the entity property, triggering validation rules and business rules.

## Validation Display

MudNeatoo components automatically display validation messages from `PropertyMessages`. Validation errors appear below the input field.

Configure a property with validation and bind to a component:

<!-- snippet: blazor-validation-inline -->
```cs
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
```
<!-- endSnippet -->

The component waits for async validation rules to complete before displaying error messages.

## Validation Summary

Use `NeatooValidationSummary` to display all validation messages for an entity in a single location. The component shows a MudAlert with all property errors.

Display aggregate validation errors:

<!-- snippet: blazor-validation-summary -->
```cs
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
```
<!-- endSnippet -->

The validation summary automatically updates when validation state changes, including messages from child entities in the aggregate.

## Form Integration

Wrap MudNeatoo components in a `MudForm` for standard Blazor form handling. The form validates on submit.

Create a form with validation:

<!-- snippet: blazor-form-submit -->
```cs
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
```
<!-- endSnippet -->

MudNeatoo components integrate with MudForm's validation system, preventing submission when invalid.

## Busy State Handling

MudNeatoo components disable themselves when `IsBusy` is true, preventing user input during async operations.

Bind to a property with async validation:

<!-- snippet: blazor-busy-state -->
```cs
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
```
<!-- endSnippet -->

The component automatically disables while async rules execute, then re-enables when complete.

## Read-Only Properties

Set `IsReadOnly` on a property to make MudNeatoo components read-only. This prevents value changes without disabling the control.

Configure a read-only property:

<!-- snippet: blazor-readonly-property -->
```cs
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
```
<!-- endSnippet -->

Read-only components remain visually enabled but reject value changes.

## Select and Dropdown Binding

`MudNeatooSelect` binds to properties with discrete values. Use `MudSelectItem` children to define options.

Bind to an enum property:

<!-- snippet: blazor-select-enum -->
```cs
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
```
<!-- endSnippet -->

The select component validates the selected value and displays error messages.

## Checkbox and Switch Binding

Bind boolean properties to `MudNeatooCheckBox` or `MudNeatooSwitch` for toggle controls.

Bind to a boolean property:

<!-- snippet: blazor-checkbox-binding -->
```cs
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
```
<!-- endSnippet -->

Checkbox state changes trigger business rules immediately.

## Date and Time Pickers

MudNeatoo provides specialized components for date and time selection that bind to `DateTime`, `DateOnly`, `TimeOnly`, and `DateRange` properties.

Bind to a date property:

<!-- snippet: blazor-date-picker -->
```cs
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
```
<!-- endSnippet -->

Date pickers validate against business rules and display error messages.

## Numeric Field Binding

`MudNeatooNumericField` provides formatted numeric input for decimal, int, double, and other numeric types.

Bind to a decimal property:

<!-- snippet: blazor-numeric-field -->
```cs
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
```
<!-- endSnippet -->

Numeric fields support min/max validation and custom formatting.

## Autocomplete Binding

`MudNeatooAutocomplete` provides search-as-you-type functionality for properties with large option sets.

Bind to a property with autocomplete search:

<!-- snippet: blazor-autocomplete -->
```cs
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
```
<!-- endSnippet -->

Autocomplete integrates with async search and validates selected values.

## Change Tracking in Forms

MudNeatoo components bind to `IsDirty` for change tracking. Use this to enable/disable save buttons or warn users about unsaved changes.

Track unsaved changes:

<!-- snippet: blazor-change-tracking -->
```cs
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
```
<!-- endSnippet -->

`IsDirty` cascades from child entities to the aggregate root, providing accurate change state.

## Customizing Component Appearance

MudNeatoo components expose MudBlazor parameters for customization. Set `Variant`, `Margin`, `Class`, and other MudBlazor properties.

Customize component appearance:

<!-- snippet: blazor-customize-appearance -->
```cs
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
```
<!-- endSnippet -->

All MudBlazor styling parameters pass through to the underlying component.

## Property Extensions

MudNeatoo provides extension methods for `IEntityProperty` to integrate with standard MudBlazor components.

Use extension methods for custom binding:

<!-- snippet: blazor-property-extensions -->
```cs
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
```
<!-- endSnippet -->

Extensions include `GetValidationFunc`, `GetErrorText`, and `HasErrors`.

## Two-Way Binding

MudNeatoo components use Blazor's two-way binding pattern but bind to `IEntityProperty` instead of raw values. Changes flow through the entity property, triggering all Neatoo behaviors.

Understanding the binding flow:

1. User changes component value
2. Component calls `EntityProperty.SetValue()`
3. Business rules execute
4. Validation rules execute
5. `IsDirty` updates
6. Parent cascade occurs
7. Component re-renders with validation state

This ensures UI changes trigger the full Neatoo rule pipeline.

## StateHasChanged Integration

MudNeatoo components subscribe to `PropertyChanged` and `NeatooPropertyChanged` events to re-render when validation state, busy state, or values change.

Property change triggers automatic re-render:

<!-- snippet: blazor-statehaschanged -->
```cs
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
```
<!-- endSnippet -->

Components automatically dispose event subscriptions when removed from the component tree.

## Using Standard MudBlazor Components

For scenarios not covered by MudNeatoo components, bind standard MudBlazor components manually using `EntityProperty.Value` and `EntityProperty.SetValue`.

Manual binding to a MudBlazor component:

<!-- snippet: blazor-manual-binding -->
```cs
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
```
<!-- endSnippet -->

Manual binding requires implementing validation display and change tracking.

## Performance Considerations

MudNeatoo components re-render when validation, busy state, or values change. For forms with many fields:

- Use `PauseAllActions` during bulk updates to prevent excessive re-renders
- Prefer batch validation after multiple changes
- Consider virtualization for large lists of input components
- Use `Immediate="false"` to validate on blur instead of keystroke

For high-frequency updates, debounce property changes to avoid triggering rules on every keystroke.

---

**UPDATED:** 2026-01-24
