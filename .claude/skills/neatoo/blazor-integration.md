# Neatoo Blazor Integration Reference

## Overview

Neatoo entities implement `INotifyPropertyChanged`, enabling automatic UI updates in Blazor. The `Neatoo.Blazor.MudNeatoo` package provides MudBlazor-integrated components for seamless entity binding.

## Key Concepts

| Feature | Description |
|---------|-------------|
| `INotifyPropertyChanged` | All entities support automatic UI binding |
| `IsSavable` | Use to enable/disable Save buttons |
| `IsValid` | Use to show validation state |
| `PropertyMessages` | Collection of validation messages |

## Property Binding

Bind directly to entity properties - change notification is automatic:

<!-- snippet: property-binding -->
```csharp
*@
        @* Bind directly to entity properties - change notification is automatic *@
        <label>@Person[nameof(Person.FirstName)].DisplayName</label>
        <input type="text" @bind="Person.FirstName" @bind:event="oninput"
               class="@(Person[nameof(Person.FirstName)].IsValid ? "" : "is-invalid")" />
        @*
```
<!-- /snippet -->

## Validation Display

Display validation messages from PropertyMessages collection:

<!-- snippet: validation-display -->
```csharp
*@
        @* Display validation messages from PropertyMessages collection *@
        @if (!Person[nameof(Person.FirstName)].IsValid)
        {
            @foreach (var message in Person[nameof(Person.FirstName)].PropertyMessages)
            {
                <div class="validation-error">@message.Message</div>
            }
        }
        @*
```
<!-- /snippet -->

## Save Button with IsSavable

Use IsSavable for Save button enablement (checks IsModified, IsValid, !IsBusy, !IsChild):

<!-- snippet: issavable-button -->
```csharp
*@
    @* IsSavable checks: IsModified, IsValid, !IsBusy, !IsChild *@
    <button type="button"
            disabled="@(!Person.IsSavable)"
            @onclick="SavePerson">
        Save
    </button>
    @*
```
<!-- /snippet -->

## IsBusy Handling

Show loading indicators during async validation:

<!-- snippet: isbusy-handling -->
```csharp
*@
    @if (Person.IsBusy)
    {
        @* Show loading indicator while async rules execute *@
        <div class="spinner">Validating...</div>
    }
    @*
```
<!-- /snippet -->

## Async Loading

Load entities in OnInitializedAsync:

<!-- snippet: async-loading -->
```csharp
*@
    protected override async Task OnInitializedAsync()
    {
        // Create a new entity using the factory
        Person = PersonFactory.Create();

        // For fetching existing: Person = await PersonFactory.Fetch(id);
        await base.OnInitializedAsync();
    }
    @*
```
<!-- /snippet -->

## Save Pattern

Always reassign after Save - it returns a new instance:

<!-- snippet: save-pattern -->
```csharp
*@
    private async Task SavePerson()
    {
        if (Person is null || !Person.IsSavable) return;

        // Wait for any async rules to complete
        await Person.WaitForTasks();

        // Re-check IsSavable after waiting
        if (!Person.IsSavable) return;

        // IMPORTANT: Always reassign - Save returns a new instance
        // Person = await PersonFactory.Save(Person);

        // Note: This sample Person entity doesn't have Insert/Update methods.
        // In a real entity with [Insert] and [Update] methods, the factory
        // generates a Save method that should be called like above.
    }
    @*
```
<!-- /snippet -->

## MudNeatoo Components

The `Neatoo.Blazor.MudNeatoo` package provides:

| Component | Purpose |
|-----------|---------|
| `MudNeatooTextField` | Text input with automatic validation |
| `MudNeatooSelect` | Dropdown with entity binding |
| `MudNeatooDatePicker` | Date input with validation |
| `MudNeatooNumericField` | Numeric input with validation |

## Best Practices

1. **Use IsSavable for Save buttons** - Don't just check IsValid
2. **Handle async validation** - Show loading indicators during IsBusy
3. **Display validation messages** - Use PropertyMessages for user feedback
4. **Wait for tasks before save** - Call `await entity.WaitForTasks()`
