# MudNeatoo Anti-Patterns

This reference catalogs common mistakes when building Blazor UIs with Neatoo entities. Each anti-pattern includes the incorrect code, why it's wrong, and the correct alternative.

## Anti-Pattern 1: POCO Intermediary

Creating a plain class that mirrors entity properties, binding MudBlazor to the POCO, then manually syncing to the entity on save.

### Wrong

```razor
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

```razor
<MudForm @ref="form">
    <MudNeatooTextField T="string"
                        EntityProperty="@entity[nameof(IUser.FirstName)]" />
    <MudNeatooTextField T="string"
                        EntityProperty="@entity[nameof(IUser.LastName)]" />
</MudForm>

<MudButton OnClick="Save" Disabled="@(!entity.IsSavable)">Save</MudButton>

@code {
    private IUser? entity;

    private async Task Save()
    {
        await entity!.WaitForTasks();
        if (!entity.IsSavable) return;
        entity = await UserFactory.Save(entity);
    }
}
```

---

## Anti-Pattern 2: Manual ValueChanged Handlers

Using standard MudBlazor components with `ValueChanged` callbacks that manually assign to entity properties.

### Wrong

```razor
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

- Direct property assignment (`entity.Prop = value`) bypasses async rule pipeline — `SetValue()` is the correct async path
- No validation display — standard MudBlazor doesn't know about `PropertyMessages`
- No `IsBusy` / `IsReadOnly` binding — manual handlers don't disable during async rules
- Label is hardcoded instead of using `DisplayName`
- Null guards on every handler are boilerplate that MudNeatoo eliminates

### Correct

```razor
<MudNeatooTextField T="string"
                    EntityProperty="@entity[nameof(IPatient.PrimaryPhone)]" />

<MudNeatooSelect T="PhoneType?"
                 EntityProperty="@entity[nameof(IPatient.PrimaryPhoneType)]">
    <MudSelectItem Value="@((PhoneType?)PhoneType.Home)">Home</MudSelectItem>
</MudNeatooSelect>
```

Zero handlers. The component calls `SetValue()` internally, which triggers the async rule pipeline.

---

## Anti-Pattern 3: Direct @bind-Value to Entity Properties

Using Blazor's `@bind-Value` two-way binding directly to entity properties.

### Wrong

```razor
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

- `@bind-Value` uses the CLR property setter, which calls `Setter()` synchronously — this works for triggering synchronous rules but skips the component-level validation display pipeline
- No validation error display — MudBlazor doesn't know about `PropertyMessages`
- `Required="true"` and `RequiredError` duplicate Neatoo's `[Required]` validation
- No `IsBusy` or `IsReadOnly` binding
- Label is hardcoded instead of using `DisplayName`
### Correct

```razor
<MudNeatooTextField T="string"
                    EntityProperty="@entity[nameof(IPatient.FirstName)]" />

<MudNeatooDatePicker EntityProperty="@entity[nameof(IPatient.DateOfBirth)]" />

<MudNeatooRadioGroup T="Gender"
                     EntityProperty="@entity[nameof(IPatient.Gender)]">
    <MudRadio Value="@Gender.Male">Male</MudRadio>
    <MudRadio Value="@Gender.Female">Female</MudRadio>
</MudNeatooRadioGroup>
```

---

## Anti-Pattern 4: EditForm with DataAnnotationsValidator

Using Blazor's `EditForm` and `DataAnnotationsValidator` instead of `MudForm` with MudNeatoo components.

### Wrong

```razor
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
- Completely bypasses `IsSavable` (which includes `IsModified && !IsBusy`)

### Correct

```razor
<MudForm @ref="form">
    <NeatooValidationSummary Entity="@entity" ShowHeader="false" Dense="true" />

    <MudNeatooTextField T="string"
                        EntityProperty="@entity[nameof(IPatient.FirstName)]" />

    <MudButton OnClick="Save" Disabled="@(!entity.IsSavable)">Save</MudButton>
</MudForm>
```

---

## Anti-Pattern 5: Local Variables for Form State

Storing form values in local component variables instead of binding to the entity.

### Wrong

```razor
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

```razor
<MudNeatooNumericField T="int"
                       EntityProperty="@entity[nameof(ITreatment.DurationMinutes)]" />

<MudNeatooTextField T="string"
                    EntityProperty="@entity[nameof(ITreatment.Laser)]" />

<MudNeatooNumericField T="double"
                       EntityProperty="@entity[nameof(ITreatment.Power)]" />

<MudButton OnClick="Save" Disabled="@(!entity.IsSavable)">Save</MudButton>
```

Validation lives in the domain model:

```csharp
RuleManager.AddValidation(
    t => t.Power < 0 || t.Power > 60 ? "Power must be between 0 and 60" : "",
    t => t.Power);
```

---

## Anti-Pattern 6: Banner Error on Save Failure

Checking `IsSavable` in the save handler and showing a snackbar/banner when invalid, instead of preventing the save entirely.

### Wrong

```csharp
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
<MudButton OnClick="Save" Variant="Variant.Filled">Save</MudButton>
```

### Why It's Wrong

- The user can click Save, see an error banner, but may not know which fields are invalid
- `IsSavable` should disable the button proactively, not be checked reactively
- The banner is redundant — inline validation on each MudNeatoo component already shows errors
- `NeatooValidationSummary` already aggregates all errors visibly

### Correct

```razor
<NeatooValidationSummary Entity="@entity" ShowHeader="false" Dense="true" />

@* Individual fields show their own errors via MudNeatoo *@
<MudNeatooTextField T="string" EntityProperty="@entity[nameof(IPatient.Name)]" />

@* Button disabled when entity can't be saved *@
<MudButton OnClick="Save"
           Disabled="@(!entity.IsSavable)"
           Variant="Variant.Filled">
    Save
</MudButton>
```

```csharp
private async Task Save()
{
    await entity!.WaitForTasks();
    if (!entity.IsSavable) return;  // Guard only — button should already be disabled
    entity = await factory.Save(entity);
}
```

---

## Anti-Pattern 7: Mixed Approach in Same Form

Using MudNeatoo components for some fields and standard MudBlazor for others in the same form.

### Wrong

```razor
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

```razor
<MudNeatooTextField T="string"
                    EntityProperty="@entity[nameof(IPatient.FirstName)]" />

@* MudNeatooTextField supports most MudTextField parameters *@
<MudNeatooTextField T="string"
                    EntityProperty="@entity[nameof(IPatient.PrimaryPhone)]" />
```

If a pass-through parameter is missing (like `Mask`), fall back to manual metadata binding rather than dropping to plain MudBlazor:

```razor
@{ var phoneProp = entity[nameof(IPatient.PrimaryPhone)]; }
<MudTextField T="string"
              Value="@((string?)phoneProp.Value)"
              ValueChanged="@(async (string v) => await phoneProp.SetValue(v))"
              Label="@phoneProp.DisplayName"
              Disabled="@phoneProp.IsBusy"
              ReadOnly="@phoneProp.IsReadOnly"
              Mask="@(new PatternMask("(000) 000-0000"))" />
```

---

## Anti-Pattern 8: Subscribing to PropertyChanged for Computed Values

Subscribing to `PropertyChanged` in Blazor to compute derived values, instead of using `AddAction` in the domain model.

### Wrong

```csharp
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
```csharp
RuleManager.AddAction(
    t => t.TotalPay = t.Hours * t.Rate,
    t => t.Hours, t => t.Rate);
```

Razor:
```razor
<MudText>@entity.TotalPay</MudText>
```

The `PropertyChanged` subscription in Blazor should only trigger `StateHasChanged` — never compute values. See the Neatoo skill's `references/domain-logic-placement.md` for the full pattern.

---

## Summary: The Decision Checklist

When binding a MudBlazor component to a Neatoo entity property:

1. **Is there a MudNeatoo wrapper?** → Use it with `EntityProperty="@entity[nameof(IEntity.Prop)]"`
2. **No wrapper but parameters pass through?** → Use manual metadata binding (call `SetValue`, bind `IsBusy`/`IsReadOnly`/`DisplayName`)
3. **Display-only (no editing)?** → Bind directly to `@entity.Prop` (INotifyPropertyChanged handles re-render)
4. **Never:** Create a POCO, use `@bind-Value` to entity, use `EditForm`/`DataAnnotationsValidator`, or write `ValueChanged` handlers that assign to entity properties directly
