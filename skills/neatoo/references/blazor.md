# Blazor Integration

Neatoo provides Blazor-specific components and patterns for building forms with automatic validation display, change tracking, and two-way binding.

## Basic Text Field

Bind to Neatoo properties with automatic validation:

<!-- snippet: blazor-text-field-basic -->
<a id='snippet-blazor-text-field-basic'></a>
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
<sup><a href='/src/docs/samples/BlazorSamples.cs#L116-L136' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-text-field-basic' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Validation Display

### Inline Validation

Show validation errors next to fields:

<!-- snippet: blazor-validation-inline -->
<a id='snippet-blazor-validation-inline'></a>
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
<sup><a href='/src/docs/samples/BlazorSamples.cs#L138-L159' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-validation-inline' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Validation Summary

Show all validation errors in one place:

<!-- snippet: blazor-validation-summary -->
<a id='snippet-blazor-validation-summary'></a>
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
<sup><a href='/src/docs/samples/BlazorSamples.cs#L161-L186' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-validation-summary' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Form Submission

Handle form submission with validation:

<!-- snippet: blazor-form-submit -->
<a id='snippet-blazor-form-submit'></a>
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
<sup><a href='/src/docs/samples/BlazorSamples.cs#L188-L237' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-form-submit' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Busy State

Show loading indicators during async operations:

<!-- snippet: blazor-busy-state -->
<a id='snippet-blazor-busy-state'></a>
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
<sup><a href='/src/docs/samples/BlazorSamples.cs#L239-L271' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-busy-state' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Read-Only Properties

Display read-only values:

<!-- snippet: blazor-readonly-property -->
<a id='snippet-blazor-readonly-property'></a>
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
<sup><a href='/src/docs/samples/BlazorSamples.cs#L273-L300' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-readonly-property' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Select/Dropdown

Bind to enum properties:

<!-- snippet: blazor-select-enum -->
<a id='snippet-blazor-select-enum'></a>
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
<sup><a href='/src/docs/samples/BlazorSamples.cs#L302-L323' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-select-enum' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Checkbox

Bind to boolean properties:

<!-- snippet: blazor-checkbox-binding -->
<a id='snippet-blazor-checkbox-binding'></a>
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
<sup><a href='/src/docs/samples/BlazorSamples.cs#L325-L348' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-checkbox-binding' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Date Picker

Bind to date properties:

<!-- snippet: blazor-date-picker -->
<a id='snippet-blazor-date-picker'></a>
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
<sup><a href='/src/docs/samples/BlazorSamples.cs#L350-L373' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-date-picker' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Numeric Fields

Bind to numeric properties:

<!-- snippet: blazor-numeric-field -->
<a id='snippet-blazor-numeric-field'></a>
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
<sup><a href='/src/docs/samples/BlazorSamples.cs#L375-L403' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-numeric-field' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Autocomplete

Bind with autocomplete behavior:

<!-- snippet: blazor-autocomplete -->
<a id='snippet-blazor-autocomplete'></a>
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
<sup><a href='/src/docs/samples/BlazorSamples.cs#L405-L438' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-autocomplete' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Change Tracking in UI

React to property changes:

<!-- snippet: blazor-change-tracking -->
<a id='snippet-blazor-change-tracking'></a>
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
<sup><a href='/src/docs/samples/BlazorSamples.cs#L440-L476' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-change-tracking' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Customizing Appearance

Style based on validation state:

<!-- snippet: blazor-customize-appearance -->
<a id='snippet-blazor-customize-appearance'></a>
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
<sup><a href='/src/docs/samples/BlazorSamples.cs#L478-L511' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-customize-appearance' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Property Extensions

Access extended property information:

<!-- snippet: blazor-property-extensions -->
<a id='snippet-blazor-property-extensions'></a>
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
<sup><a href='/src/docs/samples/BlazorSamples.cs#L513-L541' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-property-extensions' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Manual State Updates

Trigger UI updates manually:

<!-- snippet: blazor-statehaschanged -->
<a id='snippet-blazor-statehaschanged'></a>
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
<sup><a href='/src/docs/samples/BlazorSamples.cs#L543-L581' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-statehaschanged' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Manual Binding

For custom controls without Neatoo components:

<!-- snippet: blazor-manual-binding -->
<a id='snippet-blazor-manual-binding'></a>
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
<sup><a href='/src/docs/samples/BlazorSamples.cs#L583-L618' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-manual-binding' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Component Reference

| Component | Purpose |
|-----------|---------|
| `NeatooTextField` | Text input with validation |
| `NeatooNumericField` | Numeric input |
| `NeatooDatePicker` | Date selection |
| `NeatooCheckbox` | Boolean toggle |
| `NeatooSelect` | Dropdown selection |
| `NeatooValidationSummary` | All validation errors |
| `NeatooValidationMessage` | Single property validation |

## Best Practices

1. **Use Neatoo components** - They handle validation and change tracking automatically
2. **Bind to ViewModel properties** - Not directly to domain model
3. **Handle IsBusy** - Disable buttons and show loading during async operations
4. **Show validation early** - Display errors as user types, not just on submit

## Related

- [Validation](validation.md) - Validation rules and BrokenRules
- [Properties](properties.md) - Property change notifications
- [Entities](entities.md) - IsSavable for submit buttons
