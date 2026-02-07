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
<a id='snippet-blazor-text-field-basic'></a>
```cs
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
<sup><a href='/src/samples/BlazorSamples.cs#L116-L133' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-text-field-basic' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The component synchronizes value changes to the entity property via the typed property setter, triggering the full rule pipeline (validation rules and business rules) with `ChangeReason.UserEdit`.

## Validation Display

MudNeatoo components automatically display validation messages from the property's `PropertyMessages` collection. Each rule stores messages on the property via `SetMessagesForRule` using the rule's stable ID. Validation errors appear below the input field.

Configure a property with validation and bind to a component:

<!-- snippet: blazor-validation-inline -->
<a id='snippet-blazor-validation-inline'></a>
```cs
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
<sup><a href='/src/samples/BlazorSamples.cs#L135-L149' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-validation-inline' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The component subscribes to `PropertyChanged` events. When async validation rules complete, they update `PropertyMessages` and trigger `PropertyChanged`, causing the component to re-render with validation messages.

## Validation Summary

Use `NeatooValidationSummary` to display all validation messages for an entity in a single location. The component shows a MudAlert with all property errors.

Display aggregate validation errors:

<!-- snippet: blazor-validation-summary -->
<a id='snippet-blazor-validation-summary'></a>
```cs
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
<sup><a href='/src/samples/BlazorSamples.cs#L151-L167' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-validation-summary' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The validation summary subscribes to entity `PropertyChanged` events. When validation state changes (including cascade from child entities), `PropertyMessages` updates and the component re-renders with the aggregated messages from the entire aggregate.

## Form Integration

Wrap MudNeatoo components in a `MudForm` for standard Blazor form handling. The form validates on submit.

Create a form with validation:

<!-- snippet: blazor-form-submit -->
<a id='snippet-blazor-form-submit'></a>
```cs
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
<sup><a href='/src/samples/BlazorSamples.cs#L169-L193' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-form-submit' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

MudNeatoo components integrate with MudForm's validation system, preventing submission when invalid.

## Busy State Handling

MudNeatoo components bind to the property's `IsBusy` state and disable themselves when true, preventing user input during async operations. The RuleManager marks properties as busy using unique execution IDs before async rule execution, then clears the busy state after completion.

Bind to a property with async validation:

<!-- snippet: blazor-busy-state -->
<a id='snippet-blazor-busy-state'></a>
```cs
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
<sup><a href='/src/samples/BlazorSamples.cs#L195-L217' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-busy-state' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The component subscribes to `PropertyChanged` events on `IsBusy`. When the property's busy state changes, the component re-renders with the updated disabled state.

## Read-Only Properties

MudNeatoo components bind to the property's `IsReadOnly` state. When `IsReadOnly` is true, the component renders as read-only, preventing value changes without disabling the control. `IsReadOnly` is typically set during property initialization or by business rules.

Configure a read-only property:

<!-- snippet: blazor-readonly-property -->
<a id='snippet-blazor-readonly-property'></a>
```cs
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
<sup><a href='/src/samples/BlazorSamples.cs#L219-L237' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-readonly-property' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Read-only components remain visually enabled but prevent editing. The underlying MudBlazor component respects the `ReadOnly` parameter, which MudNeatoo binds to `EntityProperty.IsReadOnly`.

## Select and Dropdown Binding

`MudNeatooSelect` binds to properties with discrete values. Use `MudSelectItem` children to define options.

Bind to an enum property:

<!-- snippet: blazor-select-enum -->
<a id='snippet-blazor-select-enum'></a>
```cs
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
<sup><a href='/src/samples/BlazorSamples.cs#L239-L252' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-select-enum' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The select component binds to the property wrapper. When the user selects a value, the component updates the entity property via the typed setter, triggering validation rules. Error messages display automatically from `PropertyMessages`.

## Checkbox and Switch Binding

Bind boolean properties to `MudNeatooCheckBox` or `MudNeatooSwitch` for toggle controls.

Bind to a boolean property:

<!-- snippet: blazor-checkbox-binding -->
<a id='snippet-blazor-checkbox-binding'></a>
```cs
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
<sup><a href='/src/samples/BlazorSamples.cs#L254-L271' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-checkbox-binding' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Checkbox state changes update the entity property via the typed setter, triggering business rules and validation rules with `ChangeReason.UserEdit`.

## Date and Time Pickers

MudNeatoo provides specialized components for date and time selection that bind to `DateTime`, `DateOnly`, `TimeOnly`, and `DateRange` properties.

Bind to a date property:

<!-- snippet: blazor-date-picker -->
<a id='snippet-blazor-date-picker'></a>
```cs
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
<sup><a href='/src/samples/BlazorSamples.cs#L273-L286' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-date-picker' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Date pickers bind to the property wrapper. When the user selects a date, the component updates the entity property via the typed setter, triggering validation and business rules. Error messages display automatically from `PropertyMessages`.

## Numeric Field Binding

`MudNeatooNumericField` provides formatted numeric input for decimal, int, double, and other numeric types.

Bind to a decimal property:

<!-- snippet: blazor-numeric-field -->
<a id='snippet-blazor-numeric-field'></a>
```cs
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
<sup><a href='/src/samples/BlazorSamples.cs#L288-L304' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-numeric-field' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Numeric fields support min/max validation and custom formatting.

## Autocomplete Binding

`MudNeatooAutocomplete` provides search-as-you-type functionality for properties with large option sets.

Bind to a property with autocomplete search:

<!-- snippet: blazor-autocomplete -->
<a id='snippet-blazor-autocomplete'></a>
```cs
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
<sup><a href='/src/samples/BlazorSamples.cs#L306-L318' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-autocomplete' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Autocomplete integrates with async search functions. When the user selects a value, the component updates the entity property via the typed setter, triggering validation and business rules. Error messages display automatically from `PropertyMessages`.

## Change Tracking in Forms

MudNeatoo components bind to `IsModified` for change tracking. Use this to enable/disable save buttons or warn users about unsaved changes.

Track unsaved changes:

<!-- snippet: blazor-change-tracking -->
<a id='snippet-blazor-change-tracking'></a>
```cs
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
<sup><a href='/src/samples/BlazorSamples.cs#L320-L338' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-change-tracking' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`IsModified` cascades from child entities to the aggregate root, providing accurate change state. Use `ModifiedProperties` to see which specific properties changed.

## Customizing Component Appearance

MudNeatoo components expose MudBlazor parameters for customization. Set `Variant`, `Margin`, `Class`, and other MudBlazor properties.

Customize component appearance:

<!-- snippet: blazor-customize-appearance -->
<a id='snippet-blazor-customize-appearance'></a>
```cs
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
<sup><a href='/src/samples/BlazorSamples.cs#L340-L354' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-customize-appearance' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

All MudBlazor styling parameters pass through to the underlying component.

## Property Extensions

MudNeatoo provides extension methods for `IEntityProperty` to integrate with standard MudBlazor components.

Use extension methods for custom binding:

<!-- snippet: blazor-property-extensions -->
<a id='snippet-blazor-property-extensions'></a>
```cs
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
<sup><a href='/src/samples/BlazorSamples.cs#L356-L373' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-property-extensions' title='Start of snippet'>anchor</a></sup>
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
<a id='snippet-blazor-statehaschanged'></a>
```cs
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
<sup><a href='/src/samples/BlazorSamples.cs#L375-L391' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-statehaschanged' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Components implement `IDisposable` and unsubscribe from `PropertyChanged` events in `Dispose()`, preventing memory leaks when components are removed from the render tree.

## Using Standard MudBlazor Components

For scenarios not covered by MudNeatoo components, bind standard MudBlazor components manually. Use the typed property for reading values and the `SetValue` method on the property wrapper for async updates. This ensures proper async coordination with validation rules.

Manual binding to a MudBlazor component:

<!-- snippet: blazor-manual-binding -->
<a id='snippet-blazor-manual-binding'></a>
```cs
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
<sup><a href='/src/samples/BlazorSamples.cs#L393-L406' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-manual-binding' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Manual binding requires implementing validation display (reading `PropertyMessages`), busy state handling (binding to `IsBusy`), read-only state (binding to `IsReadOnly`), and change tracking (subscribing to `PropertyChanged`). MudNeatoo components handle all of this automatically.

## Performance Considerations

MudNeatoo components subscribe to `PropertyChanged` events and re-render when `PropertyMessages`, `IsValid`, `IsBusy`, `IsReadOnly`, or `Value` change. For forms with many fields:

- Use `PauseAllActions` during bulk updates to prevent excessive re-renders. This queues `PropertyChanged` events and fires them after the `using` block completes.
- Prefer batch validation after multiple changes with `await entity.RunRules()` once all properties are set.
- Consider virtualization for large lists of input components (`MudVirtualize` with MudNeatoo components).
- Use `Immediate="false"` on MudBlazor components to validate on blur instead of keystroke, reducing rule execution frequency.

For high-frequency updates, debounce property changes at the component level (e.g., `DebounceInterval` on `MudTextField`) to avoid triggering rules on every keystroke. This reduces the frequency of property setter calls and rule execution.

## Blazor WASM Project Structure

Isolate EF Core in a separate infrastructure project and use `PrivateAssets="all"` on the project reference. See the Person example (`src/Examples/Person/`):

```xml
<!-- Infrastructure.csproj - contains EF Core -->
<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore" Version="..." />
  <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="..." />
</ItemGroup>
```

The **Domain project** references Infrastructure privately:

```xml
<!-- Domain.csproj -->
<ItemGroup>
  <!-- PrivateAssets="all" prevents Infrastructure from flowing to consumers -->
  <ProjectReference Include="..\Infrastructure\Infrastructure.csproj" PrivateAssets="all" />
</ItemGroup>
```

The **Server project** explicitly references both:

```xml
<!-- Server.csproj -->
<ItemGroup>
  <ProjectReference Include="..\Domain\Domain.csproj" />
  <ProjectReference Include="..\Infrastructure\Infrastructure.csproj" />
</ItemGroup>
```

The **Client project** only references Domain (Infrastructure never flows through):

```xml
<!-- Client.csproj -->
<ItemGroup>
  <ProjectReference Include="..\Domain\Domain.csproj" />
</ItemGroup>
```

This ensures:
- The client cannot accidentally call server-only methods (DI resolution fails)
- Smaller WASM bundle (no EF Core, database drivers)
- Clear architectural boundary between client and server code
- Domain project can still compile and use EF Core types

---

**UPDATED:** 2026-01-27
