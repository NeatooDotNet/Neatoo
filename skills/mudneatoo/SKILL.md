---
name: MudNeatoo
description: This skill should be used when writing or modifying .razor files that bind to Neatoo entities, when using MudBlazor components with Neatoo domain models, when the user mentions "MudNeatooTextField", "MudNeatooSelect", "MudNeatooDatePicker", "MudNeatooNumericField", "MudNeatooCheckBox", "NeatooValidationSummary", "EntityProperty", "@bind-Value with Neatoo", "form binding", "Blazor form", or asks to build a form, page, or dialog that edits a Neatoo entity. Also triggers when reviewing .razor files for correct Neatoo integration patterns. Assumes the Neatoo skill is also loaded for domain model concepts.
version: 1.0.0
---

# MudNeatoo — Blazor Binding for Neatoo Entities

MudNeatoo provides wrapper components around MudBlazor that bind directly to Neatoo's property system. They handle value synchronization, validation display, busy state, and read-only state automatically. The UI is a thin binding layer over the domain model — no POCOs, no manual event handlers, no duplicate validation.

**Required package:** `Neatoo.Blazor.MudNeatoo`

## The Core Pattern

Every MudNeatoo component takes an `EntityProperty` parameter — the `IEntityProperty` object accessed via the entity's indexer `entity["PropertyName"]`. This single binding point gives the component everything: value, label, validation messages, busy state, and read-only state.

```razor
@* CORRECT: MudNeatoo component with EntityProperty binding *@
<MudNeatooTextField T="string"
                    EntityProperty="@entity[nameof(IPatient.FirstName)]"
                    Variant="Variant.Outlined" />
```

The component automatically:
- Displays `DisplayName` as the label
- Calls `SetValue()` on change (triggers business rules)
- Shows `PropertyMessages` as validation errors
- Disables when `IsBusy` (async rules running)
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
        entity.FirstName = value;  // Bypasses async rule pipeline
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
| `MudNeatooSelect<T>` | `MudSelect<T>` | Enum or value type |
| `MudNeatooDatePicker` | `MudDatePicker` | (no type param — always `DateTime?`) |
| `MudNeatooDateRangePicker` | `MudDateRangePicker` | (no type param) |
| `MudNeatooTimePicker` | `MudTimePicker` | (no type param) |
| `MudNeatooCheckBox<T>` | `MudCheckBox<T>` | `bool`, `bool?` |
| `MudNeatooSwitch<T>` | `MudSwitch<T>` | `bool` |
| `MudNeatooRadioGroup<T>` | `MudRadioGroup<T>` | Enum or value type |
| `MudNeatooSlider<T>` | `MudSlider<T>` | Numeric type |
| `MudNeatooAutocomplete<T>` | `MudAutocomplete<T>` | Any type |
| `NeatooValidationSummary` | `MudAlert` | (entity-level errors) |

All MudBlazor parameters pass through — `Variant`, `Margin`, `HelperText`, `Adornment`, `Class`, `Min`, `Max`, etc.

## Binding Patterns

### Text and Numeric Fields

```razor
<MudNeatooTextField T="string"
                    EntityProperty="@entity[nameof(IPatient.FirstName)]"
                    Variant="Variant.Outlined" />

<MudNeatooNumericField T="decimal"
                       EntityProperty="@entity[nameof(IOrder.UnitPrice)]"
                       Adornment="Adornment.Start"
                       AdornmentText="$" />
```

### Select with Options

```razor
<MudNeatooSelect T="PhoneType?"
                 EntityProperty="@phone[nameof(IPersonPhone.PhoneType)]"
                 Placeholder="Select Phone Type">
    <MudSelectItem Value="@((PhoneType?)PhoneType.Home)">Home</MudSelectItem>
    <MudSelectItem Value="@((PhoneType?)PhoneType.Mobile)">Mobile</MudSelectItem>
    <MudSelectItem Value="@((PhoneType?)PhoneType.Work)">Work</MudSelectItem>
</MudNeatooSelect>
```

### Date Picker

```razor
<MudNeatooDatePicker EntityProperty="@entity[nameof(IPatient.DateOfBirth)]"
                     MaxDate="@DateTime.Today"
                     DateFormat="MM/dd/yyyy"
                     Editable="true" />
```

### CheckBox

```razor
<MudNeatooCheckBox T="bool"
                   EntityProperty="@entity[nameof(IUser.IsActive)]" />
```

### Validation Summary

Display all entity-level validation errors in a `MudAlert`:

```razor
<NeatooValidationSummary Entity="@entity"
                         ShowHeader="false"
                         Dense="true"
                         IncludePropertyNames="false" />
```

Parameters: `Entity` (required, `IValidateMetaProperties`), `ShowHeader`, `HeaderText`, `Dense`, `IncludePropertyNames`, `Variant`, `Elevation`, `Class`.

## Page Structure Pattern

A complete form page follows this structure:

```razor
@implements IDisposable
@inject IPatientEditFactory PatientEditFactory

@if (entity != null)
{
    <MudForm @ref="form">
        <NeatooValidationSummary Entity="@entity" ShowHeader="false" Dense="true" />

        <MudNeatooTextField T="string"
                            EntityProperty="@entity[nameof(IPatientEdit.FirstName)]" />
        <MudNeatooTextField T="string"
                            EntityProperty="@entity[nameof(IPatientEdit.LastName)]" />

        <MudButton OnClick="Save"
                   Disabled="@(!entity.IsSavable)"
                   Variant="Variant.Filled"
                   Color="Color.Primary">
            Save
        </MudButton>
    </MudForm>
}

@code {
    private MudForm? form;
    private IPatientEdit? entity;

    protected override async Task OnInitializedAsync()
    {
        entity = await PatientEditFactory.Fetch(patientId);
        if (entity != null)
            entity.PropertyChanged += OnEntityPropertyChanged;
    }

    private async Task Save()
    {
        await entity!.WaitForTasks();
        if (!entity.IsSavable) return;

        var saved = await PatientEditFactory.Save(entity);
        SetEntity(saved);
    }

    private void OnEntityPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Re-render when state properties change (IsSavable, IsValid, etc.)
        if (e.PropertyName?.StartsWith("Is") == true)
            InvokeAsync(StateHasChanged);
    }

    private void SetEntity(IPatientEdit? newEntity)
    {
        if (entity != null)
            entity.PropertyChanged -= OnEntityPropertyChanged;
        entity = newEntity;
        if (entity != null)
            entity.PropertyChanged += OnEntityPropertyChanged;
    }

    public void Dispose()
    {
        if (entity != null)
            entity.PropertyChanged -= OnEntityPropertyChanged;
    }
}
```

### Key Points in the Page Structure

1. **Inject the factory interface** — not the concrete class
2. **Use `IsSavable` to disable save button** — it combines `IsValid && IsModified && !IsBusy && !IsChild`
3. **Subscribe to `PropertyChanged`** — so Blazor re-renders when `IsSavable` changes
4. **Call `WaitForTasks()` before save** — ensures async validation completes
5. **Replace the entity reference after save** — `Save()` returns the updated entity
6. **Implement `IDisposable`** — unsubscribe from `PropertyChanged`
7. **No `EditForm`, no `DataAnnotationsValidator`** — use `MudForm` with MudNeatoo components

### Child Entity Collections

Iterate over child collections and bind each child's properties:

```razor
@foreach (var item in entity.Items)
{
    <MudGrid Spacing="2">
        <MudItem xs="6">
            <MudNeatooTextField T="string"
                                EntityProperty="@item[nameof(IOrderItem.ProductName)]" />
        </MudItem>
        <MudItem xs="3">
            <MudNeatooNumericField T="int"
                                   EntityProperty="@item[nameof(IOrderItem.Quantity)]" />
        </MudItem>
        <MudItem xs="3">
            <MudIconButton Icon="@Icons.Material.Filled.Delete"
                           OnClick="@(() => entity.Items.Remove(item))" />
        </MudItem>
    </MudGrid>
}
```

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

### Manual Metadata Binding

For custom UI elements not covered by MudNeatoo components, access property metadata directly:

```razor
@{ var emailProp = entity["Email"]; }
<MudTextField Value="@((string?)emailProp.Value)"
              ValueChanged="@(async (string v) => await emailProp.SetValue(v))"
              Label="@emailProp.DisplayName"
              Disabled="@emailProp.IsBusy"
              ReadOnly="@emailProp.IsReadOnly" />
@if (!emailProp.IsValid)
{
    @foreach (var msg in emailProp.PropertyMessages)
    { <MudText Color="Color.Error">@msg.Message</MudText> }
}
```

This is what MudNeatoo components do internally — prefer the components, fall back to manual binding only for unsupported controls.

## Display-Only Binding

For read-only display of entity values (not form inputs), bind directly to entity properties. The entity implements `INotifyPropertyChanged`, so Blazor re-renders automatically:

```razor
<MudText>@entity.Total</MudText>
<MudText>@entity.Status</MudText>
<MudChip Color="@(entity.IsValid ? Color.Success : Color.Error)">
    @(entity.IsValid ? "Valid" : "Has Errors")
</MudChip>
```

## Reference Documentation

- **`references/anti-patterns.md`** — Complete catalog of anti-patterns with correct alternatives
