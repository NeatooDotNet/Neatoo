---
name: MudNeatoo
description: This skill should be used when writing or modifying .razor files that bind to Neatoo entities, when using MudBlazor components with Neatoo domain models, when the user mentions "MudNeatooTextField", "MudNeatooSelect", "MudNeatooDatePicker", "MudNeatooNumericField", "MudNeatooCheckBox", "NeatooValidationSummary", "EntityProperty", "@bind-Value with Neatoo", "form binding", "Blazor form", or asks to build a form, page, or dialog that edits a Neatoo entity. Also triggers when reviewing .razor files for correct Neatoo integration patterns. Assumes the Neatoo skill is also loaded for domain model concepts.
version: 2.0.0
---

# MudNeatoo — Blazor Binding for Neatoo Entities

MudNeatoo provides wrapper components around MudBlazor that bind directly to Neatoo's property system. They handle value synchronization, validation display, busy state, and read-only state automatically. The UI is a thin binding layer over the domain model — no POCOs, no manual event handlers, no duplicate validation.

**Required package:** `Neatoo.Blazor.MudNeatoo`

Every block below is compiled in the Neatoo repository's Design.App (Blazor WebAssembly over Design.Domain) or Design.Domain, and the ViewModels are tested.

## Namespaces & @using Directives

Add these `@using` directives to your `_Imports.razor` or individual `.razor` files:

<!-- snippet: skill-mud-usings -->
<a id='snippet-skill-mud-usings'></a>
```razor
@using Neatoo.Blazor.MudNeatoo.Components
@using Neatoo.Blazor.MudNeatoo.Validation
@using Neatoo.Blazor.MudNeatoo.Extensions
```
<sup><a href='/src/Design/Design.App/_Imports.razor#L9-L13' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-mud-usings' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

| Namespace | Contains |
|-----------|----------|
| `Neatoo.Blazor.MudNeatoo.Components` | All MudNeatoo input components (`MudNeatooTextField`, `MudNeatooSelect`, etc.) |
| `Neatoo.Blazor.MudNeatoo.Validation` | `NeatooValidationSummary` |
| `Neatoo.Blazor.MudNeatoo.Extensions` | `EntityPropertyExtensions` (`GetValidationFunc<T>`, `GetErrorText`, `HasErrors`) |

## The Core Pattern

Every MudNeatoo component takes an `EntityProperty` parameter — the `IEntityProperty` object accessed via the entity's indexer `entity[nameof(IEmployee.FirstName)]`. This single binding point gives the component everything: value, label, validation messages, busy state, and read-only state.

`IEntityProperty` is the property type of an `EntityBase` entity. An input model's (`ValidateBase`) properties are `IValidateProperty` only, so an input model cannot be bound through MudNeatoo components ([Neatoo#95](https://github.com/NeatooDotNet/Neatoo/issues/95)); bind it with Manual Metadata Binding (below), which needs nothing beyond `IValidateProperty`.

<!-- snippet: skill-mud-text-field -->
<a id='snippet-skill-mud-text-field'></a>
```razor
<MudNeatooTextField T="string" EntityProperty="@Vm.Employee[nameof(IEmployee.FirstName)]" Variant="Variant.Outlined" Class="mb-3" />
<MudNeatooTextField T="string" EntityProperty="@Vm.Employee[nameof(IEmployee.LastName)]" Variant="Variant.Outlined" Class="mb-3" />
<MudNeatooTextField T="string" EntityProperty="@Vm.Employee[nameof(IEmployee.Email)]" Variant="Variant.Outlined" Class="mb-3" />
```
<sup><a href='/src/Design/Design.App/Pages/EmployeeEdit.razor#L32-L36' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-mud-text-field' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The component automatically:
- Displays `DisplayName` as the label
- Calls `SetValue()` on change (triggers business rules). `MudNeatooTextField` and `MudNeatooNumericField` are `Immediate="false"`: they commit when the field loses focus, so a rule behind them runs once per committed value, not per keystroke
- Shows `PropertyMessages` as validation errors
- Disables when `IsBusy` (async rules running) or when the caller passes `Disabled="true"`
- Sets read-only when `IsReadOnly`

### What NOT to Do

```razor
@* WRONG: Standard MudBlazor with manual handler *@
<MudTextField T="string"
              Value="@entity.FirstName"
              ValueChanged="OnFirstNameChanged"
              Label="First Name" />

@code {
    void OnFirstNameChanged(string value) {
        entity.FirstName = value;  // Runs the same rules, but cannot be awaited; no validation, busy or read-only binding
    }
}

@* WRONG: @bind-Value directly to entity property *@
<MudTextField @bind-Value="entity.FirstName" Label="First Name" />

@* WRONG: POCO intermediary *@
<MudTextField @bind-Value="model.FirstName" Label="First Name" />
@code {
    private EditModel model = new();  // Duplicates entity state
    async Task Save() {
        entity.FirstName = model.FirstName;  // Manual sync
    }
}
```

See `references/anti-patterns.md` for the complete anti-pattern catalog with real-world examples.

## Component Reference

| MudNeatoo Component | Wraps | Type Parameter |
|---------------------|-------|----------------|
| `MudNeatooTextField<T>` | `MudTextField<T>` | `string`, `int`, etc. |
| `MudNeatooNumericField<T>` | `MudNumericField<T>` | `int`, `decimal`, `double` |
| `MudNeatooSelect<T>` | `MudSelect<T>` | Enum, value type, or record |
| `MudNeatooDatePicker` | `MudDatePicker` | (no type param — always `DateTime?`) |
| `MudNeatooDateRangePicker` | `MudDateRangePicker` | (no type param) |
| `MudNeatooTimePicker` | `MudTimePicker` | (no type param) |
| `MudNeatooCheckBox<T>` | `MudCheckBox<T>` | `bool`, `bool?` |
| `MudNeatooSwitch<T>` | `MudSwitch<T>` | `bool` |
| `MudNeatooRadioGroup<T>` | `MudRadioGroup<T>` | Enum or value type |
| `MudNeatooSlider<T>` | `MudSlider<T>` | Numeric type |
| `MudNeatooAutocomplete<T>` | `MudAutocomplete<T>` | Any type |
| `NeatooValidationSummary` | `MudAlert` | (entity-level errors) |

Each component forwards a curated set of its wrapped MudBlazor component's parameters — `Variant`, `Margin`, `HelperText`, `Adornment`, `Class`, and others — not every parameter (`MudNeatooTextField` has no `Mask`, `Label` or `Converter`, for example). Two are never forwarded: **`ReadOnly`** (hardcoded to `EntityProperty.IsReadOnly`; see ReadOnly Behavior below) and **`Disabled`** (OR'd with `EntityProperty.IsBusy`; see Disabled Behavior below). A parameter that is not forwarded means Manual Metadata Binding for that field.

In Razor, a component's `T` cannot carry a nullable reference annotation (`T="PayGrade?"` raises CS8669 in the generated code); use the type itself (`T="PayGrade"`) for a nullable reference-type property.

### `MudNeatooTextField` escape hatch: `UserAttributes`

`MudNeatooTextField<T>` forwards a `UserAttributes` (`Dictionary<string, object>`, defaults to an empty dictionary) parameter to `MudTextField`. MudBlazor spreads the dictionary onto the native `<input>` or `<textarea>`, so any HTML attribute works — including ones with no typed MudBlazor parameter. Use it for:

- **Spellcheck** — MudBlazor does NOT expose a `Spellcheck` parameter on `MudTextField` or `MudInput`. Set `["spellcheck"] = "true"` via `UserAttributes`.
- **Drag-handle resize** — CSS `resize: vertical` on the `<textarea>`. Set `["style"] = "resize: vertical;"` via `UserAttributes`.

<!-- snippet: skill-mud-multiline -->
<a id='snippet-skill-mud-multiline'></a>
```razor
@* A textarea that grows with its content up to 20 lines, with the
   browser's spellcheck - an HTML attribute MudBlazor has no
   parameter for, passed through UserAttributes. *@
<MudNeatooTextField T="string"
                    EntityProperty="@Vm.Employee[nameof(IEmployee.Notes)]"
                    Lines="4"
                    Sizing="InputSizing.Auto"
                    MaxLines="20"
                    UserAttributes="@(new() { ["spellcheck"] = "true" })"
                    Class="mb-3" />
```
<sup><a href='/src/Design/Design.App/Pages/EmployeeEdit.razor#L75-L86' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-mud-multiline' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Multi-line text: `Lines`, `Sizing`, `MaxLines`

`MudNeatooTextField<T>` also forwards MudBlazor's multi-line parameters:

- `Lines` (`int`, default `1`) — set greater than `1` for a `<textarea>`.
- `Sizing` (`InputSizing`, default `Fixed`) — `InputSizing.Auto` grows the textarea with its content (requires `Lines > 1`).
- `MaxLines` (`int`, default `0`) — upper bound on auto-grow. `0` means unlimited.

## Binding Patterns

### Numeric Field

<!-- snippet: skill-mud-numeric-field -->
<a id='snippet-skill-mud-numeric-field'></a>
```razor
<MudNeatooNumericField T="decimal"
                       EntityProperty="@Vm.Employee[nameof(IEmployee.Salary)]"
                       Adornment="Adornment.Start"
                       AdornmentText="$"
                       Class="mb-3" />
```
<sup><a href='/src/Design/Design.App/Pages/EmployeeEdit.razor#L46-L52' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-mud-numeric-field' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Select with Options

The options are values of the property's type; selecting one assigns it. Here the property is a `record` value object, so each option is a `PayGrade`:

<!-- snippet: skill-mud-select -->
<a id='snippet-skill-mud-select'></a>
```razor
@* A record bound through MudNeatoo: the options are values, and
   selecting one assigns it. Disabled binds a business fact as it
   is - an inactive employee's grade is frozen - with no
   condition of the page's own. T is the record type: Razor-generated code
   cannot carry a nullable annotation. *@
<MudNeatooSelect T="PayGrade"
                 EntityProperty="@Vm.Employee[nameof(IEmployee.Grade)]"
                 Disabled="@(!Vm.Employee.IsActive)"
                 Class="mb-3">
    @foreach (var grade in Vm.Grades)
    {
        <MudSelectItem T="PayGrade" Value="@grade">@grade.Code</MudSelectItem>
    }
</MudNeatooSelect>
```
<sup><a href='/src/Design/Design.App/Pages/EmployeeEdit.razor#L58-L73' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-mud-select' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Date Picker

<!-- snippet: skill-mud-date-picker -->
<a id='snippet-skill-mud-date-picker'></a>
```razor
<MudNeatooDatePicker EntityProperty="@Vm.Employee[nameof(IEmployee.HireDate)]"
                     MaxDate="@DateTime.Today"
                     DateFormat="yyyy-MM-dd"
                     Editable="true"
                     Class="mb-3" />
```
<sup><a href='/src/Design/Design.App/Pages/EmployeeEdit.razor#L38-L44' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-mud-date-picker' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### CheckBox

<!-- snippet: skill-mud-checkbox -->
<a id='snippet-skill-mud-checkbox'></a>
```razor
<MudNeatooCheckBox T="bool" EntityProperty="@Vm.Employee[nameof(IEmployee.IsActive)]" Label="Active" Class="mb-3" />
```
<sup><a href='/src/Design/Design.App/Pages/EmployeeEdit.razor#L54-L56' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-mud-checkbox' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Validation Summary

Display all entity-level validation errors in a `MudAlert`:

<!-- snippet: skill-mud-validation-summary -->
<a id='snippet-skill-mud-validation-summary'></a>
```razor
<NeatooValidationSummary Entity="@Vm.Employee" ShowHeader="false" Dense="true" Class="mb-4" />
```
<sup><a href='/src/Design/Design.App/Pages/EmployeeEdit.razor#L28-L30' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-mud-validation-summary' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Parameters: `Entity` (required, `IValidateMetaProperties`), `ShowHeader`, `HeaderText`, `Dense`, `IncludePropertyNames`, `Variant`, `Elevation`, `Class`.

## Page Structure Pattern

A complete edit page: the ViewModel holds the aggregate and saves it; the page binds the entity's property wrappers and owns the subscription that re-renders it.

<!-- snippet: skill-mud-page -->
<a id='snippet-skill-mud-page'></a>
```razor
@* @key: Save returns a new instance; keying on it rebuilds the bound
   components so each field subscribes to the new instance's wrappers. *@
<div @key="Vm.Employee">
@* View or edit is the screen's own choice of components. Read-only
   state is the entity's, and each component binds it itself. *@
@if (Vm.IsEditing)
{
    <NeatooValidationSummary Entity="@Vm.Employee" ShowHeader="false" Dense="true" Class="mb-4" />

    <MudNeatooTextField T="string" EntityProperty="@Vm.Employee[nameof(IEmployee.FirstName)]" Variant="Variant.Outlined" Class="mb-3" />
    <MudNeatooTextField T="string" EntityProperty="@Vm.Employee[nameof(IEmployee.LastName)]" Variant="Variant.Outlined" Class="mb-3" />
    <MudNeatooTextField T="string" EntityProperty="@Vm.Employee[nameof(IEmployee.Email)]" Variant="Variant.Outlined" Class="mb-3" />

    <MudNeatooDatePicker EntityProperty="@Vm.Employee[nameof(IEmployee.HireDate)]"
                         MaxDate="@DateTime.Today"
                         DateFormat="yyyy-MM-dd"
                         Editable="true"
                         Class="mb-3" />

    <MudNeatooNumericField T="decimal"
                           EntityProperty="@Vm.Employee[nameof(IEmployee.Salary)]"
                           Adornment="Adornment.Start"
                           AdornmentText="$"
                           Class="mb-3" />

    <MudNeatooCheckBox T="bool" EntityProperty="@Vm.Employee[nameof(IEmployee.IsActive)]" Label="Active" Class="mb-3" />

    @* A record bound through MudNeatoo: the options are values, and
       selecting one assigns it. Disabled binds a business fact as it
       is - an inactive employee's grade is frozen - with no
       condition of the page's own. T is the record type: Razor-generated code
       cannot carry a nullable annotation. *@
    <MudNeatooSelect T="PayGrade"
                     EntityProperty="@Vm.Employee[nameof(IEmployee.Grade)]"
                     Disabled="@(!Vm.Employee.IsActive)"
                     Class="mb-3">
        @foreach (var grade in Vm.Grades)
        {
            <MudSelectItem T="PayGrade" Value="@grade">@grade.Code</MudSelectItem>
        }
    </MudNeatooSelect>

    @* A textarea that grows with its content up to 20 lines, with the
       browser's spellcheck - an HTML attribute MudBlazor has no
       parameter for, passed through UserAttributes. *@
    <MudNeatooTextField T="string"
                        EntityProperty="@Vm.Employee[nameof(IEmployee.Notes)]"
                        Lines="4"
                        Sizing="InputSizing.Auto"
                        MaxLines="20"
                        UserAttributes="@(new() { ["spellcheck"] = "true" })"
                        Class="mb-3" />

    <MudButton Variant="Variant.Filled"
               Color="Color.Primary"
               Disabled="@(!Vm.CanSave)"
               OnClick="SaveAsync"
               Class="mr-2">
        Save
    </MudButton>
    <MudButton Variant="Variant.Text" OnClick="CancelAsync">Cancel</MudButton>
}
else
{
    <MudText Typo="Typo.h6">@Vm.Employee.FirstName @Vm.Employee.LastName</MudText>
    <MudText Class="mb-3">@Vm.Employee.Email</MudText>
    <MudButton Variant="Variant.Text" OnClick="Vm.StartEdit">Edit</MudButton>
}
</div>
```
<sup><a href='/src/Design/Design.App/Pages/EmployeeEdit.razor#L20-L104' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-mud-page' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: skill-mud-page-code -->
<a id='snippet-skill-mud-page-code'></a>
```razor
@code {
    [Parameter] public Guid? Id { get; set; }

    private IEmployee? _subscribed;

    protected override async Task OnParametersSetAsync()
    {
        if (Id is Guid id)
        {
            await Vm.Load(id);
        }
        else
        {
            Vm.New();
        }

        Resubscribe();
    }

    // The page owns the subscription: the input components subscribe to their
    // own property wrappers, but the Save button and the view-mode text read
    // the entity, so the page re-renders on the graph-wide event.
    private void Resubscribe()
    {
        if (_subscribed is not null)
        {
            _subscribed.NeatooPropertyChanged -= OnEntityChanged;
        }

        _subscribed = Vm.Employee;

        if (_subscribed is not null)
        {
            _subscribed.NeatooPropertyChanged += OnEntityChanged;
        }
    }

    // Save and Cancel each leave the ViewModel holding a new instance.
    private async Task SaveAsync()
    {
        await Vm.Save();
        Resubscribe();
        StateHasChanged();
    }

    private async Task CancelAsync()
    {
        await Vm.CancelEdit();
        Resubscribe();
        StateHasChanged();
    }

    private Task OnEntityChanged(NeatooPropertyChangedEventArgs e)
    {
        return InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        if (_subscribed is not null)
        {
            _subscribed.NeatooPropertyChanged -= OnEntityChanged;
        }
    }
}
```
<sup><a href='/src/Design/Design.App/Pages/EmployeeEdit.razor#L109-L175' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-mud-page-code' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The ViewModel it injects:

<!-- snippet: skill-mud-viewmodel -->
<a id='snippet-skill-mud-viewmodel'></a>
```cs
/// <summary>
/// ViewModel for the Employee screen: holds the aggregate, toggles the screen
/// between viewing and editing, saves.
/// </summary>
internal sealed class EmployeeEditViewModel
{
    private readonly IEmployeeFactory _employeeFactory;

    public EmployeeEditViewModel(IEmployeeFactory employeeFactory)
    {
        _employeeFactory = employeeFactory;
    }

    /// <summary>The aggregate the page binds to. Null until loaded or created.</summary>
    public IEmployee? Employee { get; private set; }

    /// <summary>What the grade select offers.</summary>
    public IReadOnlyList<PayGrade> Grades => PayGrade.All;

    /// <summary>The screen shows the edit components; a new employee starts there.</summary>
    public bool IsEditing { get; private set; }

    public void New()
    {
        Employee = _employeeFactory.Create();
        IsEditing = true;
    }

    public async Task Load(Guid id)
    {
        Employee = await _employeeFactory.Fetch(id);
        IsEditing = false;
    }

    public void StartEdit()
    {
        IsEditing = true;
    }

    public async Task CancelEdit()
    {
        if (Employee is null)
        {
            return;
        }

        if (Employee.IsNew)
        {
            New();  // nothing saved to go back to: discarding the edit is a fresh form
            return;
        }

        await Load(Employee.Id);
    }

    public bool CanSave => Employee?.IsSavable ?? false;

    public async Task Save()
    {
        if (Employee is null)
        {
            return;
        }

        await Employee.WaitForTasks();

        if (!Employee.IsSavable)
        {
            return;
        }

        Employee = (IEmployee)await Employee.Save();
        IsEditing = false;
    }
}
```
<sup><a href='/src/Design/Design.App/ViewModels/EmployeeEditViewModel.cs#L23-L99' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-mud-viewmodel' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Key Points in the Page Structure

1. **Inject the ViewModel; the ViewModel takes the factory interface** — never the concrete class.
2. **Bind the Save button to `IsSavable`** (through the ViewModel's `CanSave`) — it combines `IsValid && (IsModified || IsNew) && !IsBusy`, so a freshly created entity enables the button without reporting unsaved changes.
3. **The page owns the subscription.** Input components subscribe to their own property wrappers; the Save button and any display binding read the entity, so the page subscribes to the entity and calls `StateHasChanged`. `NeatooPropertyChanged` carries every change in the graph; `PropertyChanged` alone is enough when only root-level flags (`IsSavable`, `IsValid`, `IsBusy`) are read, because they bubble through it. See `references/property-change-events.md`.
4. **`WaitForTasks()` before save** — the ViewModel's save handler waits for async rules, then asks `IsSavable`.
5. **Replace the entity reference after save** — `Save()` returns a new instance. The ViewModel reassigns; the page moves its subscription to the new instance, and `@key` rebuilds the bound components so each subscribes to the new instance's wrappers.
6. **Implement `IDisposable`** — unsubscribe on dispose.
7. **No `EditForm`, no `DataAnnotationsValidator`, no `MudForm` validation** — MudNeatoo components show the entity's own messages; nothing wraps them.

`LazyLoad<T>` / `EntityLazyLoad` are being removed from RemoteFactory and Neatoo; build no page on them. The replacement is a `[Remote] [Fetch]` on the child list's factory, called when the data is needed (RemoteFactory skill).

### Child Entity Collections

Iterate over the child list and bind each child's properties; removing a child is a gesture the ViewModel forwards to the list:

<!-- snippet: skill-mud-child-collection -->
<a id='snippet-skill-mud-child-collection'></a>
```razor
@if (Vm.Order.Items is not null)
{
    @foreach (var item in Vm.Order.Items)
    {
        <MudGrid Class="mb-2" Spacing="2">
            <MudItem xs="11" sm="5">
                <MudNeatooTextField T="string" EntityProperty="@item[nameof(IOrderItem.ProductName)]" />
            </MudItem>
            <MudItem xs="1" Class="d-flex align-center">
                @* A gesture: the list moves a fetched item to DeletedList *@
                <MudIconButton Icon="@Icons.Material.Filled.Delete" OnClick="@(() => Vm.RemoveItem(item))" />
            </MudItem>
            <MudItem xs="6" sm="2">
                <MudNeatooNumericField T="int" EntityProperty="@item[nameof(IOrderItem.Quantity)]" />
            </MudItem>
            <MudItem xs="6" sm="2">
                <MudNeatooNumericField T="decimal" EntityProperty="@item[nameof(IOrderItem.UnitPrice)]" />
            </MudItem>
            <MudItem xs="12" sm="2" Class="d-flex align-center">
                <MudText>@item.LineTotal.ToString("C")</MudText>
            </MudItem>
        </MudGrid>
    }
}
```
<sup><a href='/src/Design/Design.App/Pages/OrderEdit.razor#L35-L60' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-mud-child-collection' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## IEntityProperty Metadata

The `EntityProperty` parameter exposes these metadata properties, all automatically wired by MudNeatoo components:

| Property | Type | What It Does |
|----------|------|-------------|
| `Value` | `object?` | Current property value |
| `DisplayName` | `string` | Label text (from `[DisplayName]` attribute) |
| `IsValid` | `bool` | No validation errors on this property |
| `PropertyMessages` | `IReadOnlyCollection<IPropertyMessage>` | Validation error messages |
| `IsBusy` | `bool` | Async rules running (shows disabled/spinner) |
| `IsReadOnly` | `bool` | Property cannot be edited |
| `IsModified` | `bool` | Property has unsaved changes |
| `SetValue(object?)` | `Task` | Async value assignment (triggers rules) |
| `WaitForTasks()` | `Task` | Wait for async rules to complete |

`DisplayName` and `IsModified` are `IEntityProperty` members; the rest are on `IValidateProperty`, which every Neatoo property implements.

### Manual Metadata Binding

For a control MudNeatoo does not cover — an input model's property ([#95](https://github.com/NeatooDotNet/Neatoo/issues/95)), or a parameter a component does not forward, such as a `Converter` ([#93](https://github.com/NeatooDotNet/Neatoo/issues/93)) — bind the property object directly:

<!-- snippet: skill-input-model-manual-binding -->
<a id='snippet-skill-input-model-manual-binding'></a>
```razor
@* MudNeatoo fields take an IEntityProperty; an input model's properties are
   IValidateProperty, so the page binds the property object by hand: the value,
   then the messages, busy and read-only state the property object carries; the value commits through SetValue, as MudNeatoo does. *@
@{ var name = _model[nameof(IDemoInputModel.Name)]; }
<MudTextField T="string"
              Label="Name"
              Value="@_model.Name"
              ValueChanged="@(async (string v) => await name.SetValue(v))"
              Error="@(!name.IsValid)"
              ErrorText="@string.Join(" ", name.PropertyMessages.Select(m => m.Message))"
              Disabled="@name.IsReadOnly" />
@if (name.IsBusy)
{
    <MudProgressLinear Indeterminate="true" />
}
```
<sup><a href='/src/Design/Design.App/Pages/InputModelPage.razor#L13-L29' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-input-model-manual-binding' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

This is what MudNeatoo components do internally — prefer the components, fall back to manual binding only for unsupported controls. On an entity property, `DisplayName` is available for the label.

### ReadOnly Behavior

`ReadOnly` is **not** a pass-through parameter on MudNeatoo components that wrap a MudBlazor input with a `ReadOnly` parameter. These components hardcode `ReadOnly="@EntityProperty.IsReadOnly"` in their Razor templates. You cannot override it via a component parameter.

**Exception:** `MudNeatooSlider` does not bind `ReadOnly`. MudBlazor's `MudSlider` has no `ReadOnly` parameter — the slider uses `Disabled="@(EntityProperty.IsBusy || Disabled)"`, following the same Disabled Behavior as all other MudNeatoo components.

`IsReadOnly` is determined by `propertyInfo.IsPrivateSetter`:
- Property with `private set` or get-only (no setter) -> `IsReadOnly = true`
- Property with public setter -> `IsReadOnly = false`
- Property with `internal set` -> `IsReadOnly = false` (not treated as read-only; `PropertyInfoWrapper` checks `SetMethod?.IsPrivate`, and `internal` setters are not private)

Read-only state is owned by the domain model, in two forms. The property declaration (`private set` or get-only) makes a property read-only on every instance. `IValidateProperty.MarkReadOnly()` makes it read-only on one instance, permanently — the entity calls it during `[Fetch]` from a server-side permission service, so a field is editable for one user and locked for another. Either way the component binds `IsReadOnly` and the UI has nothing to decide.

### Disabled Behavior

Every MudNeatoo component accepts an optional `Disabled` parameter (default `false`). The inner MudBlazor component is disabled when EITHER `EntityProperty.IsBusy` is true (async rules running) OR the caller passes `Disabled="true"` — the template binds `Disabled="@(EntityProperty.IsBusy || Disabled)"`.

This means:
- Without a `Disabled` parameter, components disable only during async rule execution
- Passing `Disabled="true"` (or binding to a condition) forces the field disabled regardless of `IsBusy`
- You cannot force a field *enabled* while `IsBusy` is true — `IsBusy` always wins

The common use is a field disabled by a domain fact, bound as it is. The grade select above does this: `Disabled="@(!Vm.Employee.IsActive)"` — an inactive employee's grade is frozen. Bind a fact the entity owns; a condition composed in the page is business logic in the UI.

**Contrast with ReadOnly:** `ReadOnly` is hardcoded to `EntityProperty.IsReadOnly` with no caller override — read-only state is owned entirely by the domain model. `Disabled` allows both domain-driven (`IsBusy`) and UI-driven disabling.

## Display-Only Binding

For read-only display of entity values (not form inputs), bind directly to entity properties — the view branch of the page above shows `@Vm.Employee.FirstName` and `@Vm.Employee.Email`, and the child collection shows `@item.LineTotal`. Blazor does **not** auto-subscribe to `INotifyPropertyChanged`; these bindings re-render because the page subscribes to the entity (Page Structure Pattern), and display bindings piggyback on that re-render. A view-only page must subscribe itself — see `references/property-change-events.md`.

A value worth displaying that the entity does not have — a label chosen by a condition, a total, a status colour — is a rule on the entity, not an expression in the markup.

## View/Edit Mode Pattern

Since most MudNeatoo components bind `ReadOnly` from `IsReadOnly` automatically (with no override), the pattern for view/edit mode switching is conditional rendering — not toggling a ReadOnly parameter. The page above does it: `@if (Vm.IsEditing)` shows the MudNeatoo components with Save and Cancel; otherwise plain `MudText` and an Edit button. `IsEditing` is screen state on the ViewModel; Cancel fetches the saved employee again, because the entity's tracked state *is* the edit; a new employee has nothing saved, so Cancel gives a fresh form.

This is intentional — ReadOnly state belongs to the domain model (a private setter, or `MarkReadOnly()` during `[Fetch]`), not the UI. View/edit toggling is a UI concern handled with Razor conditionals.

## MudBlazor 9.x Compatibility

MudBlazor 9.x renamed `ShowMessageBox` to `ShowMessageBoxAsync` on `IDialogService`. If you use message box dialogs alongside MudNeatoo forms, update your calls accordingly. This is a MudBlazor API change, not a MudNeatoo change.

## Reference Documentation

- **`references/anti-patterns.md`** — Complete catalog of anti-patterns with correct alternatives
- **`references/property-change-events.md`** — `PropertyChanged` vs `NeatooPropertyChanged`: which to use, why MudNeatoo components only need `PropertyChanged`, and how a page or container component subscribes to deep-graph changes
- **`references/aggregate-reactive-vm.md`** — A ViewModel over a live aggregate: derived values are entity rules the component binds to; the ViewModel holds the aggregate and forwards gestures
