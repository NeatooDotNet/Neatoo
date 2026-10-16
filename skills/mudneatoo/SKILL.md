---
name: MudNeatoo
description: This skill should be used when writing or modifying .razor files that bind to Neatoo entities, when using MudBlazor components with Neatoo domain models, when the user mentions "MudNeatooTextField", "MudNeatooSelect", "MudNeatooDatePicker", "MudNeatooNumericField", "MudNeatooCheckBox", "NeatooValidationSummary", "EntityProperty", "@bind-Value with Neatoo", "form binding", "Blazor form", "Setter", "verb-bound control", "re-render", "StateHasChanged", or asks to build a form, page, or dialog that edits a Neatoo entity. Also triggers when reviewing .razor files for correct Neatoo integration patterns. Assumes the Neatoo skill is also loaded for domain model concepts.
version: 2.1.0
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
<sup><a href='/src/Design/Design.App/Pages/EmployeeEdit.razor#L34-L38' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-mud-text-field' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The component automatically:
- Displays `DisplayName` as the label
- Calls `SetValue()` on change (triggers business rules), or the `Setter` you pass ([Verb-Bound Controls](#verb-bound-controls-setter)). `MudNeatooTextField` and `MudNeatooNumericField` are `Immediate="false"`: they commit when the field loses focus, so a rule behind them runs once per committed value, not per keystroke
- Shows `PropertyMessages` as validation errors
- Disables when `IsBusy` (async rules running) or when the caller passes `Disabled="true"`
- Sets read-only when `IsReadOnly` (every input except the slider, which has no read-only binding)

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

Each component declares its parameters; none captures unmatched attributes, so a MudBlazor parameter that is not in this table is not accepted. Every input also takes `EntityProperty`, `Disabled`, `Setter` and `Class`.

| Component | Forwards |
|---|---|
| `MudNeatooTextField<T>` | `Variant`, `Margin`, `Lines`, `Sizing`, `MaxLines`, `HelperText`, `HelperTextOnFocus`, `Placeholder`, `Adornment`, `AdornmentIcon`, `AdornmentColor`, `UserAttributes` |
| `MudNeatooNumericField<T>` | `Variant`, `Margin`, `HelperText`, `HelperTextOnFocus`, `Placeholder`, `Adornment`, `AdornmentIcon`, `AdornmentText`, `AdornmentColor`, `Format`, `Min`, `Max`, `Step`, `HideSpinButtons` |
| `MudNeatooSelect<T>` | `ChildContent`, `Variant`, `Margin`, `Dense`, `HelperText`, `Placeholder`, `Clearable` |
| `MudNeatooAutocomplete<T>` | `SearchFunc`, `ToStringFunc`, `ItemTemplate`, `Variant`, `Margin`, `Dense`, `HelperText`, `HelperTextOnFocus`, `Placeholder`, `Clearable`, `ResetValueOnEmptyText`, `CoerceText`, `CoerceValue`, `DebounceInterval`, `MinCharacters`, `MaxItems`, `Adornment`, `AdornmentIcon`, `AdornmentColor`, `ShowProgressIndicator`, `ProgressIndicatorColor` |
| `MudNeatooDatePicker` | `Variant`, `Margin`, `HelperText`, `HelperTextOnFocus`, `Placeholder`, `DateFormat`, `Editable`, `Clearable`, `MinDate`, `MaxDate`, `PickerVariant`, `Orientation`, `Color`, `FirstDayOfWeek`, `AdornmentIcon`, `AdornmentColor` |
| `MudNeatooDateRangePicker` | the date picker's, without `Orientation` |
| `MudNeatooTimePicker` | `Variant`, `Margin`, `HelperText`, `HelperTextOnFocus`, `Placeholder`, `TimeFormat`, `Editable`, `Clearable`, `AmPm`, `PickerVariant`, `Orientation`, `Color`, `AdornmentIcon`, `AdornmentColor` |
| `MudNeatooCheckBox<T>` | `Label`, `Color`, `UncheckedColor`, `Size`, `Dense`, `TriState`, `ShowValidation` |
| `MudNeatooSwitch<T>` | `Label`, `Color`, `UncheckedColor`, `Size`, `ThumbIcon`, `ThumbIconColor`, `ShowValidation` |
| `MudNeatooRadioGroup<T>` | `ChildContent`, `ShowValidation` |
| `MudNeatooSlider<T>` | `Label`, `ChildContent`, `Min`, `Max`, `Step`, `Variant`, `Color`, `Size`, `Immediate`, `TickMarks`, `TickMarkLabels`, `ValueLabel`, `ShowValidation` |

Two are never forwarded: **`ReadOnly`** (hardcoded to `EntityProperty.IsReadOnly`; see ReadOnly Behavior below) and **`Disabled`** (OR'd with `EntityProperty.IsBusy`; see Disabled Behavior below). A parameter that is not forwarded means Manual Metadata Binding for that field.

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
<sup><a href='/src/Design/Design.App/Pages/EmployeeEdit.razor#L77-L88' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-mud-multiline' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/Design/Design.App/Pages/EmployeeEdit.razor#L48-L54' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-mud-numeric-field' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/Design/Design.App/Pages/EmployeeEdit.razor#L60-L75' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-mud-select' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/Design/Design.App/Pages/EmployeeEdit.razor#L40-L46' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-mud-date-picker' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### CheckBox

<!-- snippet: skill-mud-checkbox -->
<a id='snippet-skill-mud-checkbox'></a>
```razor
<MudNeatooCheckBox T="bool" EntityProperty="@Vm.Employee[nameof(IEmployee.IsActive)]" Label="Active" Class="mb-3" />
```
<sup><a href='/src/Design/Design.App/Pages/EmployeeEdit.razor#L56-L58' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-mud-checkbox' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Validation Summary

Display all entity-level validation errors in a `MudAlert`:

<!-- snippet: skill-mud-validation-summary -->
<a id='snippet-skill-mud-validation-summary'></a>
```razor
<NeatooValidationSummary Entity="@Vm.Employee" ShowHeader="false" Dense="true" Class="mb-4" />
```
<sup><a href='/src/Design/Design.App/Pages/EmployeeEdit.razor#L30-L32' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-mud-validation-summary' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Parameters: `Entity` (required, `IValidateMetaProperties`), `ShowHeader`, `HeaderText`, `Dense`, `IncludePropertyNames`, `Variant`, `Elevation`, `Class`.

## Verb-Bound Controls: `Setter`

A control usually writes a fact: the input binds a property and writes it. When one fact is written with two meanings, the property has two verbs, and a control that means one of them reads the property and writes through its verb. Every input takes an optional `Setter` (`Func<T, Task>`). When it is set, a change calls it, awaited, instead of `EntityProperty.SetValue`; the value, label, messages and busy state still come from `EntityProperty`, so the control looks and refreshes like any other.

In the TrainingPlan row, the effort slider and the lock switch write facts, a nudge button writes the next mark the domain publishes, and the minutes slider reads `Minutes` and writes `SlideMinutes`, which fits the other intervals around this one for one cascade:

<!-- snippet: skill-mud-verb-bound-control -->
<a id='snippet-skill-mud-verb-bound-control'></a>
```razor
@* One interval's row. Every input refreshes itself from its own property.
   The row re-renders for its own interval's changes (its own properties and
   meta flags), never for the rest of the plan; the minutes text needs it. *@
<MudGrid Spacing="2" Class="mb-2">
    <MudItem xs="12" sm="3">
        <MudNeatooTextField T="string" EntityProperty="@Interval[nameof(IInterval.Name)]" />
    </MudItem>
    <MudItem xs="12" sm="4">
        @* A fact: the slider writes Effort, and the fit follows whoever wrote it. *@
        <MudNeatooSlider T="int" EntityProperty="@Interval[nameof(IInterval.Effort)]" Min="1" Max="10" Step="1" />
        @* A nudge is the same fact: the domain publishes the next mark, the button writes it. *@
        <MudIconButton Icon="@Icons.Material.Filled.Remove" OnClick="@(() => Interval.SetEffort(Interval.NextEffortDown))" />
        <MudIconButton Icon="@Icons.Material.Filled.Add" OnClick="@(() => Interval.SetEffort(Interval.NextEffortUp))" />
    </MudItem>
    <MudItem xs="12" sm="4">
        @* An intent: the slider reads Minutes and writes through SlideMinutes,
           which fits the other intervals around this one for one cascade. *@
        <MudNeatooSlider T="int" EntityProperty="@Interval[nameof(IInterval.Minutes)]"
                         Setter="@Interval.SlideMinutes"
                         Min="0" Max="240" Step="1" />
        <MudText Typo="Typo.caption">@Interval.Minutes min</MudText>
    </MudItem>
    <MudItem xs="12" sm="1">
        @* Locking and unlocking are plain writes of a fact. *@
        <MudNeatooSwitch T="bool" EntityProperty="@Interval[nameof(IInterval.MinutesLocked)]" />
    </MudItem>
</MudGrid>
```
<sup><a href='/src/Design/Design.App/Components/IntervalRow.razor#L5-L33' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-mud-verb-bound-control' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The decision is the domain's, not the control's: if the verb carries no intent the property cannot hold, drop the `Setter` and bind the fact. Never hand-write a control to call a verb; the `Setter` keeps the busy, message and refresh handling the component already has.

**Read-only applies as before.** Every input except the slider binds `ReadOnly` to `EntityProperty.IsReadOnly`, and a `private set` property is read-only. So today a verb-bound text, numeric or picker field needs a property with a public setter; the slider, which has no read-only binding, takes a `private set` property, as `Minutes` is.

## The Component Is the Unit of Refresh

Blazor re-renders by component; there is no binding engine. Every MudNeatoo input subscribes to its own property wrapper and re-renders itself, so a page never has to redraw for an edit inside an input. What a page or component displays outside an input - a total, a status, a button's enabled state - re-renders only when that component does. So each region that displays facts is a component that subscribes for what it shows, and the page subscribes to nothing it does not display itself.

The TrainingPlan page has no subscription at all. The row component above re-renders for its own interval's `PropertyChanged` (its own properties and meta flags). The summary re-renders for the plan's own facts, which the rules wrote:

<!-- snippet: skill-mud-component-is-the-unit-of-refresh -->
<a id='snippet-skill-mud-component-is-the-unit-of-refresh'></a>
```razor
@* The page subscribes to nothing on the aggregate. Each input refreshes
   from its own property; each component refreshes for what it shows.
   The page re-renders only after its own gestures (an added row). *@
<MudNeatooNumericField T="int" EntityProperty="@Vm.Plan[nameof(ITrainingPlan.ApprovedMinutes)]" Class="mb-3" />

@foreach (var interval in Vm.Plan.Intervals!)
{
    <IntervalRow @key="interval" Interval="@interval" />
}

<MudButton Variant="Variant.Outlined" OnClick="@(() => Vm.AddInterval("New interval"))" Class="mb-3">Add Interval</MudButton>

<PlanSummary Plan="@Vm.Plan" OnApprove="@(() => Vm.Approve("ada"))" />
```
<sup><a href='/src/Design/Design.App/Pages/TrainingPlanPage.razor#L19-L33' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-mud-component-is-the-unit-of-refresh' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The row's subscription takes its entity as a parameter, so it subscribes in `OnParametersSet`, moves when the parameter changes, and unsubscribes on dispose. Key each row on its item (`@key="interval"`): a MudNeatoo input subscribes to its property wrapper once, when it is first rendered, so a row reused for another item would keep listening to the old item's wrappers.

<!-- snippet: skill-mud-row-subscription -->
<a id='snippet-skill-mud-row-subscription'></a>
```razor
[Parameter, EditorRequired]
public IInterval Interval { get; set; } = default!;

private IInterval? _subscribed;

// The row subscribes to its own interval's PropertyChanged (its own
// properties and meta flags), not to the plan's graph.
protected override void OnParametersSet()
{
    if (ReferenceEquals(_subscribed, Interval))
    {
        return;
    }

    if (_subscribed is not null)
    {
        _subscribed.PropertyChanged -= OnIntervalChanged;
    }

    _subscribed = Interval;
    _subscribed.PropertyChanged += OnIntervalChanged;
}

private void OnIntervalChanged(object? sender, PropertyChangedEventArgs e) => InvokeAsync(StateHasChanged);

public void Dispose()
{
    if (_subscribed is not null)
    {
        _subscribed.PropertyChanged -= OnIntervalChanged;
    }
}
```
<sup><a href='/src/Design/Design.App/Components/IntervalRow.razor#L36-L69' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-mud-row-subscription' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: skill-mud-summary-component -->
<a id='snippet-skill-mud-summary-component'></a>
```razor
@* The plan's own facts, in the one component that displays them. It
   re-renders when the plan's own properties or meta flags change; an
   interval's edit reaches it only through the facts the rules wrote here. *@
<MudText Typo="Typo.subtitle1">Planned @Plan.PlannedMinutes of @Plan.ApprovedMinutes minutes · @Plan.Status</MudText>
<MudButton Variant="Variant.Filled" Disabled="@(!Plan.CanApprove)" OnClick="@OnApprove">Approve</MudButton>
```
<sup><a href='/src/Design/Design.App/Components/PlanSummary.razor#L5-L11' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-mud-summary-component' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The ViewModel holds the plan and forwards the two gestures that are not a control's:

<!-- snippet: skill-mud-training-plan-viewmodel -->
<a id='snippet-skill-mud-training-plan-viewmodel'></a>
```cs
/// <summary>
/// ViewModel for the TrainingPlan screen: holds the plan and forwards its two gestures.
/// </summary>
internal sealed class TrainingPlanViewModel
{
    private readonly ITrainingPlanFactory _factory;

    public TrainingPlanViewModel(ITrainingPlanFactory factory)
    {
        _factory = factory;
    }

    /// <summary>The aggregate the page and its components bind to. Null until created.</summary>
    public ITrainingPlan? Plan { get; private set; }

    public async Task New()
    {
        Plan = await _factory.Create();
    }

    /// <summary>A new interval joins the plan; the fit shares the minutes again.</summary>
    public async Task AddInterval(string name)
    {
        if (Plan is not null)
        {
            await Plan.AddInterval(name, 5);
        }
    }

    /// <summary>Calls the verb. The Approve button is enabled by the plan's own CanApprove.</summary>
    public async Task Approve(string approver)
    {
        if (Plan is not null)
        {
            await Plan.Approve(approver);
        }
    }
}
```
<sup><a href='/src/Design/Design.App/ViewModels/TrainingPlanViewModel.cs#L17-L56' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-mud-training-plan-viewmodel' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**A page never subscribes to the root's `NeatooPropertyChanged`.** That event carries every change anywhere in the graph, so a page subscribed to it redraws everything for each one. A page subscribes to its root's own `PropertyChanged` (the root's own properties and meta flags) for what it shows of the root: a total, a status, the Save button. Every child row that shows a child's fact is a component that subscribes to that child. The Employee, Order and WorkOrder pages below follow this rule, as the TrainingPlan page does.

## Page Structure Pattern

A complete edit page: the ViewModel holds the aggregate and saves it; the page binds the entity's property wrappers and, because it displays the Save button and the view-mode text, subscribes to the employee's own `PropertyChanged`. A screen with more regions follows [The Component Is the Unit of Refresh](#the-component-is-the-unit-of-refresh).

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
<sup><a href='/src/Design/Design.App/Pages/EmployeeEdit.razor#L22-L106' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-mud-page' title='Start of snippet'>anchor</a></sup>
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

    // The input components subscribe to their own property wrappers. The Save
    // button and the view-mode text read the employee itself, so the page
    // re-renders on the employee's own PropertyChanged: its own properties and
    // meta flags. It has no child list, so nothing else is needed.
    private void Resubscribe()
    {
        if (_subscribed is not null)
        {
            _subscribed.PropertyChanged -= OnEntityChanged;
        }

        _subscribed = Vm.Employee;

        if (_subscribed is not null)
        {
            _subscribed.PropertyChanged += OnEntityChanged;
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

    private void OnEntityChanged(object? sender, PropertyChangedEventArgs e) => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        if (_subscribed is not null)
        {
            _subscribed.PropertyChanged -= OnEntityChanged;
        }
    }
}
```
<sup><a href='/src/Design/Design.App/Pages/EmployeeEdit.razor#L111-L175' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-mud-page-code' title='Start of snippet'>anchor</a></sup>
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
3. **Subscribe only for what this component displays.** Input components subscribe to their own property wrappers. This page displays the Save button and the view-mode text, so it subscribes to the employee's own `PropertyChanged` and calls `StateHasChanged`. It never subscribes to `NeatooPropertyChanged`; a child's facts belong to the child's row component.
4. **`WaitForTasks()` before save** — the ViewModel's save handler waits for async rules, then asks `IsSavable`.
5. **Replace the entity reference after save** — `Save()` returns a new instance. The ViewModel reassigns; the page moves its subscription to the new instance, and `@key` rebuilds the bound components so each subscribes to the new instance's wrappers.
6. **Implement `IDisposable`** — unsubscribe on dispose.
7. **No `EditForm`, no `DataAnnotationsValidator`, no `MudForm` validation** — MudNeatoo components show the entity's own messages; nothing wraps them.

`LazyLoad<T>` / `EntityLazyLoad` are being removed from RemoteFactory and Neatoo; build no page on them. The replacement is a `[Remote] [Fetch]` on the child list's factory, called when the data is needed (RemoteFactory skill).

### Child Entity Collections

Each row is a component keyed on its child, and the page forwards the remove gesture to the ViewModel:

<!-- snippet: skill-mud-child-collection -->
<a id='snippet-skill-mud-child-collection'></a>
```razor
@if (Vm.Order.Items is not null)
{
    @foreach (var item in Vm.Order.Items)
    {
        @* Each row is a component that refreshes for its own item. *@
        <OrderItemEditor @key="item" Item="@item" OnRemove="@(() => Vm.RemoveItem(item))" />
    }
}
```
<sup><a href='/src/Design/Design.App/Pages/OrderEdit.razor#L37-L46' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-mud-child-collection' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The row binds the child's inputs and shows its line total. It subscribes to its own item, so it re-renders when the total changes, and the page does not:

<!-- snippet: skill-mud-child-row -->
<a id='snippet-skill-mud-child-row'></a>
```razor
@* One order item. The inputs refresh themselves; the row re-renders for its
   own item's changes, which is what keeps the line total current. *@
<MudGrid Class="mb-2" Spacing="2">
    <MudItem xs="11" sm="5">
        <MudNeatooTextField T="string" EntityProperty="@Item[nameof(IOrderItem.ProductName)]" />
    </MudItem>
    <MudItem xs="1" Class="d-flex align-center">
        @* A gesture: the page forwards it to the ViewModel, which removes the item from the list *@
        <MudIconButton Icon="@Icons.Material.Filled.Delete" OnClick="@OnRemove" />
    </MudItem>
    <MudItem xs="6" sm="2">
        <MudNeatooNumericField T="int" EntityProperty="@Item[nameof(IOrderItem.Quantity)]" />
    </MudItem>
    <MudItem xs="6" sm="2">
        <MudNeatooNumericField T="decimal" EntityProperty="@Item[nameof(IOrderItem.UnitPrice)]" />
    </MudItem>
    <MudItem xs="12" sm="2" Class="d-flex align-center">
        <MudText>@Item.LineTotal.ToString("C")</MudText>
    </MudItem>
</MudGrid>
```
<sup><a href='/src/Design/Design.App/Components/OrderItemEditor.razor#L5-L26' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-mud-child-row' title='Start of snippet'>anchor</a></sup>
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
- **`references/property-change-events.md`** — `PropertyChanged` vs `NeatooPropertyChanged`: which to use, why MudNeatoo components only need `PropertyChanged`, how a page subscribes to its root's own changes, and how a container component that spans the graph, such as the validation summary, subscribes to deep-graph changes
- **`references/aggregate-reactive-vm.md`** — A ViewModel over a live aggregate: derived values are entity rules the component binds to; the ViewModel holds the aggregate and forwards gestures
