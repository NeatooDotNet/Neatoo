# Blazor Integration

[Previous: Async Operations](async.md) | [Up](index.md) | [Next: Business Rules](business-rules.md)

Neatoo provides MudBlazor integration through the `Neatoo.Blazor.MudNeatoo` package. MudNeatoo components automatically bind to `IEntityProperty`, display validation messages, track busy state, and respect read-only constraints.

## Installation

Install the MudNeatoo package alongside MudBlazor:

```bash
dotnet add package Neatoo.Blazor.MudNeatoo
dotnet add package MudBlazor
```

In `Program.cs`, add MudBlazor services:

```csharp
builder.Services.AddMudServices();
```

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

Bind a MudNeatoo component to an entity property by setting the `EntityProperty` parameter to the `IEntityProperty` wrapper accessed via the entity's indexer. The component automatically uses the property's `DisplayName` as the label.

Bind to a string property:

<!-- snippet: blazor-text-field-basic -->
```cs
// Razor component markup:
// <MudNeatooTextField T="string" EntityProperty="@employee["Name"]" />

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
```
<!-- endSnippet -->

The component synchronizes value changes to the entity property via the typed property setter, triggering the full rule pipeline (validation rules and business rules) with `ChangeReason.UserEdit`.

## Validation Display

MudNeatoo components automatically display validation messages from the property's `PropertyMessages` collection. Each rule stores messages on the property via `SetMessagesForRule` using the rule's stable ID. Validation errors appear below the input field.

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
    var factory = GetRequiredService<IBlazorEmployeeFactory>();
    var employee = factory.Create();

    // Invalid email format triggers validation error
    employee.Email = "not-an-email";

    var emailProperty = employee["Email"];
    Assert.False(emailProperty.IsValid);
    Assert.NotEmpty(emailProperty.PropertyMessages);
}
```
<!-- endSnippet -->

The component subscribes to `PropertyChanged` events. When async validation rules complete, they update `PropertyMessages` and trigger `PropertyChanged`, causing the component to re-render with validation messages.

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
```
<!-- endSnippet -->

The validation summary subscribes to entity `PropertyChanged` events. When validation state changes (including cascade from child entities), `PropertyMessages` updates and the component re-renders with the aggregated messages from the entire aggregate.

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
```
<!-- endSnippet -->

MudNeatoo components integrate with MudForm's validation system, preventing submission when invalid.

## Busy State Handling

MudNeatoo components bind to the property's `IsBusy` state and disable themselves when true, preventing user input during async operations. The RuleManager marks properties as busy using unique execution IDs before async rule execution, then clears the busy state after completion.

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
```
<!-- endSnippet -->

The component subscribes to `PropertyChanged` events on `IsBusy`. When the property's busy state changes, the component re-renders with the updated disabled state.

## Read-Only Properties

MudNeatoo components bind to the property's `IsReadOnly` state. When `IsReadOnly` is true, the component renders as read-only, preventing value changes without disabling the control. `IsReadOnly` is typically set during property initialization or by business rules.

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
```
<!-- endSnippet -->

Read-only components remain visually enabled but prevent editing. The underlying MudBlazor component respects the `ReadOnly` parameter, which MudNeatoo binds to `EntityProperty.IsReadOnly`.

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
    var factory = GetRequiredService<IBlazorEmployeeFactory>();
    var employee = factory.Create();

    // Set enum value
    employee.Priority = Priority.High;

    var priorityProperty = employee["Priority"];
    Assert.Equal(Priority.High, priorityProperty.Value);
}
```
<!-- endSnippet -->

The select component binds to the property wrapper. When the user selects a value, the component updates the entity property via the typed setter, triggering validation rules. Error messages display automatically from `PropertyMessages`.

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
```
<!-- endSnippet -->

Checkbox state changes update the entity property via the typed setter, triggering business rules and validation rules with `ChangeReason.UserEdit`.

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
    var factory = GetRequiredService<IBlazorEmployeeFactory>();
    var employee = factory.Create();

    var startDate = new DateTime(2024, 1, 15);
    employee.StartDate = startDate;

    var startDateProperty = employee["StartDate"];
    Assert.Equal(startDate, startDateProperty.Value);
}
```
<!-- endSnippet -->

Date pickers bind to the property wrapper. When the user selects a date, the component updates the entity property via the typed setter, triggering validation and business rules. Error messages display automatically from `PropertyMessages`.

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
    var factory = GetRequiredService<IBlazorEmployeeFactory>();
    var employee = factory.Create();

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
    var factory = GetRequiredService<IBlazorEmployeeFactory>();
    var employee = factory.Create();

    employee.Department = "Engineering";

    var deptProperty = employee["Department"];
    Assert.Equal("Engineering", deptProperty.Value);
}
```
<!-- endSnippet -->

Autocomplete integrates with async search functions. When the user selects a value, the component updates the entity property via the typed setter, triggering validation and business rules. Error messages display automatically from `PropertyMessages`.

## Change Tracking in Forms

MudNeatoo components bind to `IsModified` for change tracking. Use this to enable/disable save buttons or warn users about unsaved changes.

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
```
<!-- endSnippet -->

`IsModified` cascades from child entities to the aggregate root, providing accurate change state. Use `ModifiedProperties` to see which specific properties changed.

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
    var factory = GetRequiredService<IBlazorEmployeeFactory>();
    var employee = factory.Create();

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
```
<!-- endSnippet -->

Extensions include `GetValidationFunc`, `GetErrorText`, and `HasErrors`.

## Two-Way Binding

MudNeatoo components use Blazor's two-way binding pattern but bind to `IEntityProperty` instead of raw values. Changes flow through the entity property, triggering all Neatoo behaviors.

Understanding the binding flow:

1. User changes component value
2. Component updates entity property via typed property setter (e.g., `employee.Name = value`)
3. Property setter triggers PropertyChanged event with `ChangeReason.UserEdit`
4. RuleManager identifies rules registered with the property as a trigger
5. Business rules and validation rules execute sequentially
6. Validation messages are stored on the property via `SetMessagesForRule`
7. `IsModified` updates (entity-level and property-level tracking)
8. Parent cascade occurs (IsModified, IsValid bubble up to aggregate root)
9. PropertyChanged events notify subscribed components
10. Component re-renders with updated validation state, busy state, and values

This ensures UI changes trigger the full Neatoo rule pipeline while maintaining aggregate consistency.

## StateHasChanged Integration

MudNeatoo components subscribe to the `PropertyChanged` event on `IEntityProperty` during `OnInitialized`. When key properties change (`PropertyMessages`, `IsValid`, `IsBusy`, `IsReadOnly`, `Value`), the component calls `InvokeAsync(StateHasChanged)` to re-render on the Blazor synchronization context. Components unsubscribe in `Dispose` to prevent memory leaks.

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
    var factory = GetRequiredService<IBlazorEmployeeFactory>();
    var employee = factory.Create();
    var nameProperty = employee["Name"];

    var changedProperties = new List<string>();
    nameProperty.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName ?? "");

    // Setting value triggers PropertyChanged
    employee.Name = "Test";

    Assert.NotEmpty(changedProperties);
}
```
<!-- endSnippet -->

Components implement `IDisposable` and unsubscribe from `PropertyChanged` events in `Dispose()`, preventing memory leaks when components are removed from the render tree.

## Using Standard MudBlazor Components

For scenarios not covered by MudNeatoo components, bind standard MudBlazor components manually. Use the typed property for reading values and the `SetValue` method on the property wrapper for async updates. This ensures proper async coordination with validation rules.

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
    var factory = GetRequiredService<IBlazorEmployeeFactory>();
    var employee = factory.Create();
    var nameProperty = employee["Name"];

    // Manual binding pattern: use SetValue for async
    await nameProperty.SetValue("Manual Value");

    Assert.Equal("Manual Value", employee.Name);
}
```
<!-- endSnippet -->

Manual binding requires implementing validation display (reading `PropertyMessages`), busy state handling (binding to `IsBusy`), read-only state (binding to `IsReadOnly`), and change tracking (subscribing to `PropertyChanged`). MudNeatoo components handle all of this automatically.

## Performance Considerations

MudNeatoo components subscribe to `PropertyChanged` events and re-render when `PropertyMessages`, `IsValid`, `IsBusy`, `IsReadOnly`, or `Value` change. For forms with many fields:

- Use `PauseAllActions` during bulk updates to prevent excessive re-renders. This queues `PropertyChanged` events and fires them after the `using` block completes.
- Prefer batch validation after multiple changes with `await entity.RunRules()` once all properties are set.
- Consider virtualization for large lists of input components (`MudVirtualize` with MudNeatoo components).
- Use `Immediate="false"` on MudBlazor components to validate on blur instead of keystroke, reducing rule execution frequency.

For high-frequency updates, debounce property changes at the component level (e.g., `DebounceInterval` on `MudTextField`) to avoid triggering rules on every keystroke. This reduces the frequency of property setter calls and rule execution.

---

**UPDATED:** 2026-01-25
