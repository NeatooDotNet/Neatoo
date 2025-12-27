# Blazor Binding

This document covers UI integration patterns for Neatoo entities in Blazor applications.

## Overview

Neatoo entities implement `INotifyPropertyChanged` and provide rich meta-properties for UI binding. The optional `Neatoo.Blazor.MudNeatoo` package provides pre-built MudBlazor components.

## Installation

```bash
dotnet add package Neatoo.Blazor.MudNeatoo
```

## MudNeatoo Components

Pre-built components that bind to `IEntityProperty`:

| Component | MudBlazor Base | Use Case |
|-----------|----------------|----------|
| `MudNeatooTextField<T>` | MudTextField | Text input |
| `MudNeatooNumericField<T>` | MudNumericField | Number input |
| `MudNeatooDatePicker` | MudDatePicker | Date selection |
| `MudNeatooTimePicker` | MudTimePicker | Time selection |
| `MudNeatooSelect<T>` | MudSelect | Dropdown selection |
| `MudNeatooCheckBox` | MudCheckBox | Boolean checkbox |
| `MudNeatooSwitch` | MudSwitch | Boolean toggle |
| `MudNeatooRadioGroup<T>` | MudRadioGroup | Radio button group |
| `MudNeatooAutocomplete<T>` | MudAutocomplete | Autocomplete input |
| `MudNeatooSlider<T>` | MudSlider | Slider input |
| `NeatooValidationSummary` | MudAlert | Validation message list |

### Basic Usage

```razor
@inject IPersonFactory PersonFactory

<MudNeatooTextField T="string" EntityProperty="@person[nameof(IPerson.FirstName)]" />
<MudNeatooTextField T="string" EntityProperty="@person[nameof(IPerson.LastName)]" />
<MudNeatooTextField T="string" EntityProperty="@person[nameof(IPerson.Email)]" />

<NeatooValidationSummary Entity="@person" />

<MudButton Disabled="@(!person.IsSavable)" OnClick="Save">Save</MudButton>

@code {
    private IPerson person = default!;

    protected override void OnInitialized()
    {
        person = PersonFactory.Create();
    }

    private async Task Save()
    {
        person = await PersonFactory.Save(person);
    }
}
```

### Component Features

Each MudNeatoo component:
- Binds to `IEntityProperty` value
- Displays validation messages automatically
- Shows busy indicator during async validation
- Uses `DisplayName` as label
- Respects `IsReadOnly` state
- Re-renders on property/validation changes

### Component Parameters

```razor
<MudNeatooTextField T="string"
    EntityProperty="@person[nameof(IPerson.Notes)]"
    Variant="Variant.Outlined"
    Margin="Margin.Dense"
    Lines="3"
    HelperText="Optional notes"
    Placeholder="Enter notes..."
    Adornment="Adornment.Start"
    AdornmentIcon="@Icons.Material.Filled.Notes"
    Class="my-custom-class" />
```

## Manual Binding

Without MudNeatoo components, bind manually:

### Value Binding

```razor
<MudTextField T="string"
    Value="@((string?)person[nameof(IPerson.FirstName)].Value)"
    ValueChanged="@(async v => await person[nameof(IPerson.FirstName)].SetValue(v))"
    Label="@person[nameof(IPerson.FirstName)].DisplayName"
    Error="@(!person[nameof(IPerson.FirstName)].IsValid)"
    ErrorText="@GetErrorText(person[nameof(IPerson.FirstName)])"
    Disabled="@person[nameof(IPerson.FirstName)].IsReadOnly" />

@code {
    private string GetErrorText(IEntityProperty property)
    {
        return string.Join(", ", property.PropertyMessages.Select(m => m.Message));
    }
}
```

### Two-Way Binding

For simple cases, you can use `@bind-Value`:

```razor
<MudTextField @bind-Value="person.FirstName" Label="First Name" />
```

However, this doesn't show validation messages or busy state.

## Validation Display

### Inline Validation

MudNeatoo components show validation inline:

```razor
<MudNeatooTextField T="string" EntityProperty="@person[nameof(IPerson.Email)]" />
<!-- Shows validation errors below the input -->
```

### Validation Summary

Show all validation messages:

```razor
<NeatooValidationSummary Entity="@person"
    ShowHeader="true"
    HeaderText="Please fix the following:"
    IncludePropertyNames="true"
    Variant="Variant.Filled"
    Dense="true" />
```

### Custom Validation Display

```razor
@if (!person.IsValid)
{
    <MudAlert Severity="Severity.Error">
        <ul>
            @foreach (var msg in person.PropertyMessages)
            {
                <li>@msg.Property.DisplayName: @msg.Message</li>
            }
        </ul>
    </MudAlert>
}
```

## Busy State

Show loading indicators during async operations:

### Entity-Level

```razor
@if (person.IsBusy)
{
    <MudProgressLinear Indeterminate="true" />
}

<MudButton Disabled="@(person.IsBusy || !person.IsSavable)" OnClick="Save">
    @if (person.IsBusy)
    {
        <MudProgressCircular Size="Size.Small" Indeterminate="true" Class="mr-2" />
    }
    Save
</MudButton>
```

### Property-Level

```razor
<MudTextField @bind-Value="person.Email">
    @if (person[nameof(IPerson.Email)].IsBusy)
    {
        <MudProgressCircular Size="Size.Small" Indeterminate="true" />
    }
</MudTextField>
```

## Save Button Pattern

Standard save button with all state checks:

```razor
<MudButton
    Color="Color.Primary"
    Variant="Variant.Filled"
    Disabled="@(!person.IsSavable)"
    OnClick="Save">
    @if (person.IsBusy)
    {
        <MudProgressCircular Size="Size.Small" Indeterminate="true" Class="mr-2" />
        <span>Validating...</span>
    }
    else
    {
        <span>Save</span>
    }
</MudButton>

@code {
    private async Task Save()
    {
        // Ensure all async validation is complete
        await person.WaitForTasks();

        if (!person.IsSavable)
            return;

        person = await PersonFactory.Save(person);
    }
}
```

## Collection Binding

Bind child collections:

```razor
<MudTable Items="@person.PersonPhoneList">
    <HeaderContent>
        <MudTh>Type</MudTh>
        <MudTh>Number</MudTh>
        <MudTh></MudTh>
    </HeaderContent>
    <RowTemplate>
        <MudTd>
            <MudNeatooSelect T="PhoneType?" EntityProperty="@context[nameof(IPersonPhone.PhoneType)]">
                @foreach (PhoneType type in Enum.GetValues<PhoneType>())
                {
                    <MudSelectItem Value="@((PhoneType?)type)">@type</MudSelectItem>
                }
            </MudNeatooSelect>
        </MudTd>
        <MudTd>
            <MudNeatooTextField T="string" EntityProperty="@context[nameof(IPersonPhone.PhoneNumber)]" />
        </MudTd>
        <MudTd>
            <MudIconButton Icon="@Icons.Material.Filled.Delete"
                           OnClick="@(() => RemovePhone(context))" />
        </MudTd>
    </RowTemplate>
</MudTable>

<MudButton OnClick="AddPhone">Add Phone</MudButton>

@code {
    private void AddPhone()
    {
        person.PersonPhoneList.AddPhoneNumber();
    }

    private void RemovePhone(IPersonPhone phone)
    {
        person.PersonPhoneList.Remove(phone);
    }
}
```

## Change Notifications

Subscribe to property changes:

```razor
@implements IDisposable

@code {
    private IPerson person = default!;

    protected override void OnInitialized()
    {
        person = PersonFactory.Create();
        person.PropertyChanged += OnPropertyChanged;
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Re-render when properties change
        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        person.PropertyChanged -= OnPropertyChanged;
    }
}
```

### NeatooPropertyChanged

For more detailed change information:

```razor
@code {
    protected override void OnInitialized()
    {
        person = PersonFactory.Create();
        person.NeatooPropertyChanged += OnNeatooPropertyChanged;
    }

    private Task OnNeatooPropertyChanged(NeatooPropertyChangedEventArgs e)
    {
        Console.WriteLine($"Property changed: {e.FullPropertyName}");
        Console.WriteLine($"Source: {e.Source}");
        return InvokeAsync(StateHasChanged);
    }
}
```

## Authorization UI

> **Note:** Authorization is provided by [RemoteFactory](https://github.com/NeatooDotNet/RemoteFactory). For comprehensive documentation, see the [RemoteFactory documentation](https://github.com/NeatooDotNet/RemoteFactory/tree/main/docs).

Show/hide UI based on factory authorization:

```razor
@inject IPersonFactory PersonFactory

@if (PersonFactory.CanCreate())
{
    <MudButton OnClick="CreatePerson">New Person</MudButton>
}

@if (person != null && PersonFactory.CanDelete())
{
    <MudButton Color="Color.Error" OnClick="DeletePerson">Delete</MudButton>
}
```

## Read-Only Mode

Display read-only when editing not allowed:

```razor
@if (PersonFactory.CanUpdate())
{
    <MudNeatooTextField T="string" EntityProperty="@person[nameof(IPerson.FirstName)]" />
}
else
{
    <MudText>@person.FirstName</MudText>
}
```

## Complete Page Example

```razor
@page "/person/{Id:int?}"
@inject IPersonFactory PersonFactory
@inject NavigationManager Navigation
@implements IDisposable

<PageTitle>@(person?.IsNew == true ? "New Person" : "Edit Person")</PageTitle>

@if (loading)
{
    <MudProgressCircular Indeterminate="true" />
}
else if (person != null)
{
    <MudCard>
        <MudCardHeader>
            <CardHeaderContent>
                <MudText Typo="Typo.h5">@(person.IsNew ? "New Person" : "Edit Person")</MudText>
            </CardHeaderContent>
        </MudCardHeader>

        <MudCardContent>
            <MudGrid>
                <MudItem xs="6">
                    <MudNeatooTextField T="string" EntityProperty="@person[nameof(IPerson.FirstName)]" />
                </MudItem>
                <MudItem xs="6">
                    <MudNeatooTextField T="string" EntityProperty="@person[nameof(IPerson.LastName)]" />
                </MudItem>
                <MudItem xs="12">
                    <MudNeatooTextField T="string" EntityProperty="@person[nameof(IPerson.Email)]" />
                </MudItem>
            </MudGrid>

            <MudText Typo="Typo.h6" Class="mt-4">Phone Numbers</MudText>

            @foreach (var phone in person.PersonPhoneList)
            {
                <MudGrid>
                    <MudItem xs="4">
                        <MudNeatooSelect T="PhoneType?" EntityProperty="@phone[nameof(IPersonPhone.PhoneType)]">
                            @foreach (var type in Enum.GetValues<PhoneType>())
                            {
                                <MudSelectItem Value="@((PhoneType?)type)">@type</MudSelectItem>
                            }
                        </MudNeatooSelect>
                    </MudItem>
                    <MudItem xs="6">
                        <MudNeatooTextField T="string" EntityProperty="@phone[nameof(IPersonPhone.PhoneNumber)]" />
                    </MudItem>
                    <MudItem xs="2">
                        <MudIconButton Icon="@Icons.Material.Filled.Delete"
                                       OnClick="@(() => person.PersonPhoneList.Remove(phone))" />
                    </MudItem>
                </MudGrid>
            }

            <MudButton StartIcon="@Icons.Material.Filled.Add"
                       OnClick="@(() => person.PersonPhoneList.AddPhoneNumber())">
                Add Phone
            </MudButton>

            <NeatooValidationSummary Entity="@person" Class="mt-4" />
        </MudCardContent>

        <MudCardActions>
            <MudButton OnClick="Cancel">Cancel</MudButton>
            <MudSpacer />
            <MudButton Color="Color.Primary"
                       Variant="Variant.Filled"
                       Disabled="@(!person.IsSavable)"
                       OnClick="Save">
                @if (person.IsBusy)
                {
                    <MudProgressCircular Size="Size.Small" Indeterminate="true" Class="mr-2" />
                }
                Save
            </MudButton>
        </MudCardActions>
    </MudCard>
}

@code {
    [Parameter] public int? Id { get; set; }

    private IPerson? person;
    private bool loading = true;

    protected override async Task OnParametersSetAsync()
    {
        loading = true;

        if (Id.HasValue)
        {
            person = await PersonFactory.Fetch(Id.Value);
        }
        else
        {
            person = PersonFactory.Create();
        }

        if (person != null)
        {
            person.PropertyChanged += OnPropertyChanged;
        }

        loading = false;
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        InvokeAsync(StateHasChanged);
    }

    private async Task Save()
    {
        if (person == null) return;

        await person.WaitForTasks();
        if (!person.IsSavable) return;

        person = await PersonFactory.Save(person);

        if (person.IsNew == false)
        {
            Navigation.NavigateTo($"/person/{person.Id}");
        }
    }

    private void Cancel()
    {
        Navigation.NavigateTo("/people");
    }

    public void Dispose()
    {
        if (person != null)
        {
            person.PropertyChanged -= OnPropertyChanged;
        }
    }
}
```

## See Also

- [Property System](property-system.md) - IEntityProperty details
- [Meta-Properties Reference](meta-properties.md) - IsBusy, IsValid, IsSavable
- [Validation and Rules](validation-and-rules.md) - Validation message sources
