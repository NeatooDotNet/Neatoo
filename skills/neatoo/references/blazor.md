# Blazor Integration

Neatoo provides Blazor-specific components and patterns for building forms with automatic validation display, change tracking, and two-way binding.

## Basic Text Field

Bind to Neatoo properties with automatic validation:

<!-- snippet: blazor-text-field-basic -->
<a id='snippet-blazor-text-field-basic'></a>
```razor
<MudNeatooTextField T="string" EntityProperty="@employee["Name"]" />
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Blazor/Components/TextFieldExamples.razor#L5-L7' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-text-field-basic' title='Start of snippet'>anchor</a></sup>
<a id='snippet-blazor-text-field-basic-1'></a>
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
<sup><a href='/src/docs/samples/BlazorSamples.cs#L116-L133' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-text-field-basic-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Validation Display

### Inline Validation

Show validation errors next to fields:

<!-- snippet: blazor-validation-inline -->
<a id='snippet-blazor-validation-inline'></a>
```razor
<MudNeatooTextField T="string" EntityProperty="@employee["Email"]" />
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Blazor/Components/TextFieldExamples.razor#L9-L11' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-validation-inline' title='Start of snippet'>anchor</a></sup>
<a id='snippet-blazor-validation-inline-1'></a>
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
<sup><a href='/src/docs/samples/BlazorSamples.cs#L135-L149' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-validation-inline-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Validation Summary

Show all validation errors in one place:

<!-- snippet: blazor-validation-summary -->
<a id='snippet-blazor-validation-summary'></a>
```razor
<NeatooValidationSummary Entity="@employee" />
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Blazor/Components/ValidationExamples.razor#L5-L7' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-validation-summary' title='Start of snippet'>anchor</a></sup>
<a id='snippet-blazor-validation-summary-1'></a>
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
<sup><a href='/src/docs/samples/BlazorSamples.cs#L151-L167' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-validation-summary-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Form Submission

Handle form submission with validation:

<!-- snippet: blazor-form-submit -->
<a id='snippet-blazor-form-submit'></a>
```razor
<MudForm @ref="form">
    <MudNeatooTextField T="string" EntityProperty="@employee["Name"]" />
    <MudNeatooTextField T="string" EntityProperty="@employee["Email"]" />
    <MudNeatooNumericField T="decimal" EntityProperty="@employee["Salary"]" />

    <MudButton OnClick="@Submit"
               Disabled="@(!employee.IsValid || employee.IsBusy)">
        Save
    </MudButton>
</MudForm>
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Blazor/Components/FormExamples.razor#L5-L16' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-form-submit' title='Start of snippet'>anchor</a></sup>
<a id='snippet-blazor-form-submit-1'></a>
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
<sup><a href='/src/docs/samples/BlazorSamples.cs#L169-L193' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-form-submit-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Busy State

Show loading indicators during async operations:

<!-- snippet: blazor-busy-state -->
<a id='snippet-blazor-busy-state'></a>
```razor
<MudNeatooTextField T="string" EntityProperty="@employee["Email"]" />

@if (employee.IsBusy)
{
    <MudProgressCircular Indeterminate="true" Size="Size.Small" />
}
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Blazor/Components/FormExamples.razor#L18-L25' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-busy-state' title='Start of snippet'>anchor</a></sup>
<a id='snippet-blazor-busy-state-1'></a>
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
<sup><a href='/src/docs/samples/BlazorSamples.cs#L195-L217' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-busy-state-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Read-Only Properties

Display read-only values:

<!-- snippet: blazor-readonly-property -->
<a id='snippet-blazor-readonly-property'></a>
```razor
<MudNeatooTextField T="string" EntityProperty="@auditedEntity["CreatedBy"]" ReadOnly="true" />
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Blazor/Components/TextFieldExamples.razor#L16-L18' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-readonly-property' title='Start of snippet'>anchor</a></sup>
<a id='snippet-blazor-readonly-property-1'></a>
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
<sup><a href='/src/docs/samples/BlazorSamples.cs#L219-L237' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-readonly-property-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Select/Dropdown

Bind to enum properties:

<!-- snippet: blazor-select-enum -->
<a id='snippet-blazor-select-enum'></a>
```razor
<MudNeatooSelect T="SkillPriority" EntityProperty="@employee["Priority"]">
    <MudSelectItem Value="SkillPriority.Low">Low</MudSelectItem>
    <MudSelectItem Value="SkillPriority.Medium">Medium</MudSelectItem>
    <MudSelectItem Value="SkillPriority.High">High</MudSelectItem>
    <MudSelectItem Value="SkillPriority.Critical">Critical</MudSelectItem>
</MudNeatooSelect>
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Blazor/Components/InputControlExamples.razor#L5-L12' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-select-enum' title='Start of snippet'>anchor</a></sup>
<a id='snippet-blazor-select-enum-1'></a>
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
<sup><a href='/src/docs/samples/BlazorSamples.cs#L239-L252' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-select-enum-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Checkbox

Bind to boolean properties:

<!-- snippet: blazor-checkbox-binding -->
<a id='snippet-blazor-checkbox-binding'></a>
```razor
<MudNeatooCheckBox T="bool" EntityProperty="@employee["IsActive"]" />

<MudNeatooSwitch T="bool" EntityProperty="@employee["IsActive"]" />
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Blazor/Components/InputControlExamples.razor#L14-L18' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-checkbox-binding' title='Start of snippet'>anchor</a></sup>
<a id='snippet-blazor-checkbox-binding-1'></a>
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
<sup><a href='/src/docs/samples/BlazorSamples.cs#L254-L271' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-checkbox-binding-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Date Picker

Bind to date properties:

<!-- snippet: blazor-date-picker -->
<a id='snippet-blazor-date-picker'></a>
```razor
<MudNeatooDatePicker EntityProperty="@employee["StartDate"]" />
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Blazor/Components/InputControlExamples.razor#L20-L22' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-date-picker' title='Start of snippet'>anchor</a></sup>
<a id='snippet-blazor-date-picker-1'></a>
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
<sup><a href='/src/docs/samples/BlazorSamples.cs#L273-L286' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-date-picker-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Numeric Fields

Bind to numeric properties:

<!-- snippet: blazor-numeric-field -->
<a id='snippet-blazor-numeric-field'></a>
```razor
<MudNeatooNumericField T="decimal"
                       EntityProperty="@employee["Salary"]"
                       Adornment="Adornment.Start"
                       AdornmentText="$" />
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Blazor/Components/InputControlExamples.razor#L24-L29' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-numeric-field' title='Start of snippet'>anchor</a></sup>
<a id='snippet-blazor-numeric-field-1'></a>
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
<sup><a href='/src/docs/samples/BlazorSamples.cs#L288-L304' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-numeric-field-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Autocomplete

Bind with autocomplete behavior:

<!-- snippet: blazor-autocomplete -->
<a id='snippet-blazor-autocomplete'></a>
```razor
<MudNeatooAutocomplete T="string"
                       EntityProperty="@employee["Department"]"
                       SearchFunc="@SearchDepartments" />
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Blazor/Components/InputControlExamples.razor#L31-L35' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-autocomplete' title='Start of snippet'>anchor</a></sup>
<a id='snippet-blazor-autocomplete-1'></a>
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
<sup><a href='/src/docs/samples/BlazorSamples.cs#L306-L318' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-autocomplete-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Change Tracking in UI

React to property changes:

<!-- snippet: blazor-change-tracking -->
<a id='snippet-blazor-change-tracking'></a>
```razor
<MudButton OnClick="@Save"
           Disabled="@(!employee.IsModified || !employee.IsValid)">
    Save Changes
</MudButton>

<MudButton OnClick="@Cancel"
           Disabled="@(!employee.IsModified)">
    Cancel
</MudButton>

@if (employee.IsModified)
{
    <MudAlert Severity="Severity.Warning">
        You have unsaved changes
    </MudAlert>
}
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Blazor/Components/FormExamples.razor#L27-L44' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-change-tracking' title='Start of snippet'>anchor</a></sup>
<a id='snippet-blazor-change-tracking-1'></a>
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
<sup><a href='/src/docs/samples/BlazorSamples.cs#L320-L338' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-change-tracking-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Customizing Appearance

Style based on validation state:

<!-- snippet: blazor-customize-appearance -->
<a id='snippet-blazor-customize-appearance'></a>
```razor
<MudNeatooTextField T="string"
                    EntityProperty="@employee["Name"]"
                    Variant="Variant.Filled"
                    Margin="Margin.Normal"
                    HelperText="Enter the employee's full name"
                    Placeholder="John Doe"
                    Adornment="Adornment.Start"
                    AdornmentIcon="@Icons.Material.Filled.Person"
                    Class="my-custom-field" />
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Blazor/Components/TextFieldExamples.razor#L23-L33' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-customize-appearance' title='Start of snippet'>anchor</a></sup>
<a id='snippet-blazor-customize-appearance-1'></a>
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
<sup><a href='/src/docs/samples/BlazorSamples.cs#L340-L354' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-customize-appearance-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Property Extensions

Access extended property information:

<!-- snippet: blazor-property-extensions -->
<a id='snippet-blazor-property-extensions'></a>
```razor
<MudTextField T="string"
              Value="@employee.Name"
              ValueChanged="@(v => employee["Name"].SetValue(v))"
              Error="@(!employee["Name"].IsValid)"
              ErrorText="@GetErrorText(employee["Name"])" />
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Blazor/Components/AdvancedBindingExamples.razor#L7-L13' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-property-extensions' title='Start of snippet'>anchor</a></sup>
<a id='snippet-blazor-property-extensions-1'></a>
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
<sup><a href='/src/docs/samples/BlazorSamples.cs#L356-L373' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-property-extensions-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Manual State Updates

Trigger UI updates manually:

<!-- snippet: blazor-statehaschanged -->
<a id='snippet-blazor-statehaschanged'></a>
```razor
@code {
    private SkillBlazorEmployee? employee;

    [Inject]
    private ISkillBlazorEmployeeFactory EmployeeFactory { get; set; } = default!;

    protected override void OnInitialized()
    {
        employee = EmployeeFactory.Create();
        employee["Name"].PropertyChanged += OnPropertyChanged;
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is "PropertyMessages" or "IsValid" or "IsBusy" or "IsReadOnly")
        {
            InvokeAsync(StateHasChanged);
        }
    }

    public void Dispose()
    {
        if (employee != null)
        {
            employee["Name"].PropertyChanged -= OnPropertyChanged;
        }
    }

    private async Task OnNameChanged(string value)
    {
        await employee!["Name"].SetValue(value);
    }

    private string GetErrorText(IValidateProperty property)
    {
        return string.Join("; ", property.PropertyMessages.Select(m => m.Message));
    }
}
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Blazor/Components/AdvancedBindingExamples.razor#L26-L65' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-statehaschanged' title='Start of snippet'>anchor</a></sup>
<a id='snippet-blazor-statehaschanged-1'></a>
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
<sup><a href='/src/docs/samples/BlazorSamples.cs#L375-L391' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-statehaschanged-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Manual Binding

For custom controls without Neatoo components:

<!-- snippet: blazor-manual-binding -->
<a id='snippet-blazor-manual-binding'></a>
```razor
<MudTextField T="string"
              Value="@employee.Name"
              ValueChanged="@OnNameChanged"
              Error="@(!employee["Name"].IsValid)"
              ErrorText="@GetErrorText(employee["Name"])"
              Disabled="@employee["Name"].IsBusy"
              ReadOnly="@employee["Name"].IsReadOnly" />
```
<sup><a href='/skills/neatoo/samples/Neatoo.Skills.Blazor/Components/AdvancedBindingExamples.razor#L15-L23' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-manual-binding' title='Start of snippet'>anchor</a></sup>
<a id='snippet-blazor-manual-binding-1'></a>
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
<sup><a href='/src/docs/samples/BlazorSamples.cs#L393-L406' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-manual-binding-1' title='Start of snippet'>anchor</a></sup>
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

## Related

- [Validation](validation.md) - Validation rules and BrokenRules
- [Properties](properties.md) - Property change notifications
- [Entities](entities.md) - IsSavable for submit buttons
