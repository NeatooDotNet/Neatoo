# Blazor Integration

> **Required package:** Install `Neatoo.Blazor.MudNeatoo` to use the MudNeatoo components shown below.

Neatoo provides Blazor-specific components and patterns for building forms with automatic validation display, change tracking, and two-way binding. Property binding basics are covered in [properties.md](properties.md) — this page covers Blazor-specific patterns.

## Validation Display

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

## Form Submission

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

## Busy State

Disable buttons and show loading indicators while async validation runs:

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

## Manual Binding

For custom controls without MudNeatoo components, use `SetValue` for async property assignment:

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

All MudBlazor parameters pass through to the underlying component (Variant, Margin, HelperText, Adornment, etc.).

## Best Practices

1. **Use Neatoo components** — They handle validation display and change tracking automatically
2. **Handle IsBusy** — Disable buttons and show loading during async operations
3. **Show validation early** — Display errors as user types, not just on submit

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

- [Validation](validation.md) - Validation rules
- [Properties](properties.md) - Property change notifications
- [Entities](entities.md) - IsSavable for submit buttons
