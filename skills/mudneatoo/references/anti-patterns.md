# MudNeatoo Anti-Patterns

This reference catalogs common mistakes when building Blazor UIs with Neatoo entities. Each anti-pattern includes the incorrect code, why it's wrong, and the correct alternative.

## Anti-Pattern 1: POCO Intermediary

Creating a plain class that mirrors entity properties, binding MudBlazor to the POCO, then manually syncing to the entity on save.

### Wrong

```razor
@* WRONG *@
<EditForm Model="@_model" OnValidSubmit="HandleSave">
    <DataAnnotationsValidator />
    <MudTextField @bind-Value="_model.FirstName"
                  Label="First Name"
                  Required="true"
                  RequiredError="First name is required" />
    <MudTextField @bind-Value="_model.LastName"
                  Label="Last Name" />
</EditForm>

@code {
    private class UserEditModel
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
    }

    private UserEditModel _model = new();

    private async Task LoadUser()
    {
        var user = await UserFactory.Fetch(userId);
        _model = new UserEditModel
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
        };
    }

    private async Task HandleSave()
    {
        // Manual mapping back — entity validation never ran during editing
        entity.FirstName = _model.FirstName;
        entity.LastName = _model.LastName;
        await factory.Save(entity);
    }
}
```

### Why It's Wrong

- Duplicates entity properties in a POCO — two sources of truth
- Neatoo validation rules never run during editing (only on save sync)
- `DataAnnotationsValidator` duplicates/contradicts Neatoo validation
- `IsModified`, `IsValid`, `IsSavable` are meaningless — the entity doesn't see changes until save
- Computed properties (`AddAction` rules) don't fire during editing
- Manual mapping is error-prone and grows with every property

### Correct

Bind each field to the entity's property wrapper; there is no copy to sync:

<!-- snippet: skill-mud-text-field -->
<a id='snippet-skill-mud-text-field'></a>
```razor
<MudNeatooTextField T="string" EntityProperty="@Vm.Employee[nameof(IEmployee.FirstName)]" Variant="Variant.Outlined" Class="mb-3" />
<MudNeatooTextField T="string" EntityProperty="@Vm.Employee[nameof(IEmployee.LastName)]" Variant="Variant.Outlined" Class="mb-3" />
<MudNeatooTextField T="string" EntityProperty="@Vm.Employee[nameof(IEmployee.Email)]" Variant="Variant.Outlined" Class="mb-3" />
```
<sup><a href='/src/Design/Design.App/Pages/EmployeeEdit.razor#L32-L36' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-mud-text-field' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The Save button binds `IsSavable`, and the ViewModel's save handler waits, asks `IsSavable`, saves and reassigns (SKILL.md → Page Structure Pattern).

---

## Anti-Pattern 2: Manual ValueChanged Handlers

Using standard MudBlazor components with `ValueChanged` callbacks that manually assign to entity properties.

### Wrong

```razor
@* WRONG *@
<MudTextField T="string"
              Value="@entity.PrimaryPhone"
              ValueChanged="OnPhoneChanged"
              Label="Telephone" />

<MudSelect T="PhoneType?"
           Value="@entity.PrimaryPhoneType"
           ValueChanged="OnPhoneTypeChanged"
           Label="Phone Type">
    <MudSelectItem Value="@((PhoneType?)PhoneType.Home)">Home</MudSelectItem>
</MudSelect>

@code {
    private void OnPhoneChanged(string value)
    {
        if (entity != null)
            entity.PrimaryPhone = value;
    }

    private void OnPhoneTypeChanged(PhoneType? value)
    {
        if (entity != null)
            entity.PrimaryPhoneType = value;
    }
}
```

### Why It's Wrong

- Direct property assignment (`entity.Prop = value`) runs the same rules as `SetValue()` but returns no `Task`, so the component cannot await them; `SetValue()` is the awaitable path
- No validation display — standard MudBlazor doesn't know about `PropertyMessages`
- No `IsBusy` / `IsReadOnly` binding — manual handlers don't disable during async rules
- Label is hardcoded instead of using `DisplayName`
- Null guards on every handler are boilerplate that MudNeatoo eliminates

### Correct

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

Zero handlers. The component calls `SetValue()` internally, which triggers the async rule pipeline.

---

## Anti-Pattern 3: Direct @bind-Value to Entity Properties

Using Blazor's `@bind-Value` two-way binding directly to entity properties.

### Wrong

```razor
@* WRONG *@
<MudTextField @bind-Value="entity.FirstName"
              Label="First Name"
              Required="true"
              RequiredError="First name is required" />

<MudDatePicker @bind-Date="entity.DateOfBirth"
               Label="Date of Birth" />

<MudRadioGroup @bind-Value="entity.Gender">
    <MudRadio Value="@Gender.Male">Male</MudRadio>
    <MudRadio Value="@Gender.Female">Female</MudRadio>
</MudRadioGroup>
```

### Why It's Wrong

- `@bind-Value` uses the CLR property setter, which writes the backing `IValidateProperty<T>.Value` — the rules run, but the component cannot await them, and it has no validation display
- No validation error display — MudBlazor doesn't know about `PropertyMessages`
- `Required="true"` and `RequiredError` duplicate Neatoo's `[Required]` validation
- No `IsBusy` or `IsReadOnly` binding
- Label is hardcoded instead of using `DisplayName`
### Correct

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

The text fields, select and checkbox bind the same way (SKILL.md → Page Structure Pattern).

---

## Anti-Pattern 4: EditForm with DataAnnotationsValidator

Using Blazor's `EditForm` and `DataAnnotationsValidator` instead of `MudForm` with MudNeatoo components.

### Wrong

```razor
@* WRONG *@
<EditForm Model="@entity" OnValidSubmit="HandleSave">
    <DataAnnotationsValidator />
    <ValidationSummary />

    <MudTextField @bind-Value="entity.FirstName"
                  Label="First Name" />

    <MudButton ButtonType="ButtonType.Submit">Save</MudButton>
</EditForm>
```

### Why It's Wrong

- `DataAnnotationsValidator` validates data annotations only — ignores Neatoo's `AddValidation`, `AddValidationAsync`, and class-based rules
- `ValidationSummary` shows annotation errors, not `PropertyMessages`
- `OnValidSubmit` uses annotation validity, not `entity.IsValid`
- The entity might appear "valid" to `EditForm` while failing Neatoo business rules
- Completely bypasses `IsSavable` (`(IsModified || IsNew) && IsValid && !IsBusy`)

### Correct

No form wrapper and no validator: the summary and each field show the entity's own messages, and the Save button binds `IsSavable`.

<!-- snippet: skill-mud-validation-summary -->
<a id='snippet-skill-mud-validation-summary'></a>
```razor
<NeatooValidationSummary Entity="@Vm.Employee" ShowHeader="false" Dense="true" Class="mb-4" />
```
<sup><a href='/src/Design/Design.App/Pages/EmployeeEdit.razor#L28-L30' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-mud-validation-summary' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

---

## Anti-Pattern 5: Local Variables for Form State

Storing form values in local component variables instead of binding to the entity.

### Wrong

```razor
@* WRONG *@
@code {
    private int _durationMinutes = 20;
    private string _laser = "POWERBOX";
    private double _power = 12.0;

    // Manual IsValid computation — duplicates/contradicts domain rules
    private bool IsValid =>
        _durationMinutes > 0 &&
        _durationMinutes <= 60 &&
        _power >= 0 &&
        _power <= 60 &&
        !string.IsNullOrEmpty(_laser);

    private async Task ApplyChanges()
    {
        if (!IsValid) return;
        entity.Duration = _durationMinutes;
        entity.Laser = _laser;
        entity.Power = _power;
        await factory.Save(entity);
    }
}
```

### Why It's Wrong

- Local variables duplicate entity state
- Manual `IsValid` duplicates/contradicts domain validation rules
- Entity's `AddAction` computed properties don't fire until `ApplyChanges`
- If domain rules change validity thresholds, UI validation is out of sync
- `IsSavable` is meaningless since entity doesn't see changes until apply

### Correct

Bind directly to entity properties. If the entity needs these fields, they should be entity properties with domain validation rules.

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

Validation lives in the domain model:

<!-- snippet: skill-validation-attributes -->
<a id='snippet-skill-validation-attributes'></a>
```cs
[Required(ErrorMessage = "Product name is required")]
[StringLength(100)]
public partial string? ProductName { get; set; }

[Range(1, 10000, ErrorMessage = "Quantity must be between 1 and 10000")]
public partial int Quantity { get; set; }

[Range(0.01, 1000000, ErrorMessage = "Unit price must be positive")]
public partial decimal UnitPrice { get; set; }
```
<sup><a href='/src/Design/Design.Domain/Aggregates/OrderAggregate/OrderItem.cs#L32-L42' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-validation-attributes' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

---

## Anti-Pattern 6: Banner Error on Save Failure

Checking `IsSavable` in the save handler and showing a snackbar/banner when invalid, instead of preventing the save entirely.

### Wrong

```csharp
// WRONG
private async Task Save()
{
    if (!entity.IsSavable)
    {
        Snackbar.Add("Please fix validation errors before saving", Severity.Error);
        return;
    }
    await factory.Save(entity);
}
```

With the save button always enabled:

```razor
@* WRONG: the button is enabled whatever the entity says *@
<MudButton OnClick="Save" Variant="Variant.Filled">Save</MudButton>
```

### Why It's Wrong

- The user can click Save, see an error banner, but may not know which fields are invalid
- `IsSavable` should disable the button proactively, not be checked reactively
- The banner is redundant — inline validation on each MudNeatoo component already shows errors
- `NeatooValidationSummary` already aggregates all errors visibly

### Correct

The summary (Anti-Pattern 4) shows every message, each field shows its own, and the Save button binds `IsSavable`, so an invalid entity never reaches `Save`. The save handler's `IsSavable` check is a guard, not a validation step (SKILL.md → Page Structure Pattern).

---

## Anti-Pattern 7: Mixed Approach in Same Form

Using MudNeatoo components for some fields and standard MudBlazor for others in the same form.

### Wrong

```razor
@* WRONG *@
@* Some fields use MudNeatoo correctly *@
<MudNeatooTextField T="string"
                    EntityProperty="@entity[nameof(IPatient.FirstName)]" />

@* But phone fields use standard MudBlazor with manual handlers *@
<MudTextField T="string"
              Value="@entity.PrimaryPhone"
              ValueChanged="OnPhoneChanged"
              Label="Telephone"
              Mask="@(new PatternMask("(000) 000-0000"))" />

@code {
    private void OnPhoneChanged(string value)
    {
        entity.PrimaryPhone = value;
    }
}
```

### Why It's Wrong

- Inconsistent validation behavior — some fields show errors automatically, others don't
- Phone field doesn't disable when busy, doesn't show read-only state
- Mixing approaches confuses future maintainers about which pattern to follow

### Correct

Use MudNeatoo for all fields. If a MudNeatoo component needs an additional parameter (like `Mask`), check if the parameter passes through. If not, use manual metadata binding (see SKILL.md → "Manual Metadata Binding").

If a pass-through parameter is missing (a `Mask`, a `Converter` — [#93](https://github.com/NeatooDotNet/Neatoo/issues/93)), bind that one field to the property object by hand and add the parameter to the plain `MudTextField`, rather than dropping to `@bind-Value`:

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

---

## Anti-Pattern 8: Subscribing to PropertyChanged for Computed Values

Subscribing to `PropertyChanged` in Blazor to compute derived values, instead of using `AddAction` in the domain model.

### Wrong

```csharp
// WRONG
entity.PropertyChanged += (s, e) => {
    if (e.PropertyName == "Hours" || e.PropertyName == "Rate")
        totalDisplay = $"${entity.Hours * entity.Rate:F2}";
    InvokeAsync(StateHasChanged);
};
```

### Why It's Wrong

- Business logic (computation) lives in the UI
- Not testable without Blazor
- The Neatoo skill covers this — use `AddAction` in the domain model

### Correct

Domain model:

<!-- snippet: skill-add-action-computed -->
<a id='snippet-skill-add-action-computed'></a>
```cs
public OrderItem(IEntityBaseServices<OrderItem> services) : base(services)
{
    // Action rule to calculate line total
    RuleManager.AddAction(
        t => t.LineTotal = t.Quantity * t.UnitPrice,
        t => t.Quantity,
        t => t.UnitPrice);
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/OrderAggregate/OrderItem.cs#L46-L55' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-add-action-computed' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Razor binds the result as it is — the child collection in SKILL.md shows `@item.LineTotal`.

The `PropertyChanged` subscription in Blazor should only trigger `StateHasChanged` — never compute values. See the Neatoo skill's `references/domain-logic-placement.md` for the full pattern.

---

## Summary: The Decision Checklist

When binding a MudBlazor component to a Neatoo entity property:

1. **Is there a MudNeatoo wrapper?** → Use it with `EntityProperty="@entity[nameof(IEntity.Prop)]"`
2. **No wrapper but parameters pass through?** → Use manual metadata binding (call `SetValue`, bind `IsBusy`/`IsReadOnly`/`DisplayName`)
3. **Display-only (no editing)?** → Bind directly to `@entity.Prop`; the page's `PropertyChanged` subscription re-renders it (Blazor does not subscribe on its own)
4. **Never:** Create a POCO, use `@bind-Value` to entity, use `EditForm`/`DataAnnotationsValidator`, or write `ValueChanged` handlers that assign to entity properties directly
