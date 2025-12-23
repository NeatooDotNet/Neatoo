using Microsoft.AspNetCore.Components;
using MudBlazor;
using Neatoo;
using System.ComponentModel;

namespace Neatoo.Blazor.MudNeatoo.Components;

/// <summary>
/// A MudBlazor time picker component that binds to an IEntityProperty
/// and displays Neatoo validation messages.
/// </summary>
public partial class MudNeatooTimePicker : ComponentBase, IDisposable
{
    /// <summary>
    /// The entity property to bind to. This is required.
    /// </summary>
    [Parameter, EditorRequired]
    public IEntityProperty EntityProperty { get; set; } = default!;

    /// <summary>
    /// The visual variant of the time picker.
    /// </summary>
    [Parameter]
    public Variant Variant { get; set; } = Variant.Outlined;

    /// <summary>
    /// The margin around the time picker.
    /// </summary>
    [Parameter]
    public Margin Margin { get; set; } = Margin.Dense;

    /// <summary>
    /// Helper text displayed below the input.
    /// </summary>
    [Parameter]
    public string? HelperText { get; set; }

    /// <summary>
    /// If true, helper text is only shown when the field is focused.
    /// </summary>
    [Parameter]
    public bool HelperTextOnFocus { get; set; }

    /// <summary>
    /// Placeholder text shown when no time is selected.
    /// </summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>
    /// The time format string (e.g., "hh:mm tt" for 12-hour format).
    /// </summary>
    [Parameter]
    public string? TimeFormat { get; set; }

    /// <summary>
    /// If true, allows direct text editing of the time.
    /// </summary>
    [Parameter]
    public bool Editable { get; set; } = false;

    /// <summary>
    /// If true, shows a clear button to reset the value.
    /// </summary>
    [Parameter]
    public bool Clearable { get; set; } = false;

    /// <summary>
    /// If true, uses 12-hour format with AM/PM.
    /// </summary>
    [Parameter]
    public bool AmPm { get; set; } = false;

    /// <summary>
    /// The picker variant (Inline, Dialog, or Static).
    /// </summary>
    [Parameter]
    public PickerVariant PickerVariant { get; set; } = PickerVariant.Inline;

    /// <summary>
    /// The orientation of the picker.
    /// </summary>
    [Parameter]
    public Orientation Orientation { get; set; } = Orientation.Portrait;

    /// <summary>
    /// The primary color of the picker.
    /// </summary>
    [Parameter]
    public Color Color { get; set; } = Color.Primary;

    /// <summary>
    /// The icon for the adornment (defaults to clock icon).
    /// </summary>
    [Parameter]
    public string AdornmentIcon { get; set; } = Icons.Material.Filled.Schedule;

    /// <summary>
    /// The color of the adornment icon.
    /// </summary>
    [Parameter]
    public Color AdornmentColor { get; set; } = Color.Default;

    /// <summary>
    /// Additional CSS class(es) to apply.
    /// </summary>
    [Parameter]
    public string? Class { get; set; }

    private TimeSpan? TypedValue => (TimeSpan?)EntityProperty.Value;

    protected override void OnInitialized()
    {
        EntityProperty.PropertyChanged += OnPropertyChanged;
    }

    private async Task OnValueChanged(TimeSpan? value)
    {
        await EntityProperty.SetValue(value);
    }

    private async Task<IEnumerable<string>> ValidateAsync(TimeSpan? value)
    {
        await EntityProperty.WaitForTasks();
        return EntityProperty.PropertyMessages.Select(m => m.Message).Distinct();
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IValidateProperty.PropertyMessages) ||
            e.PropertyName == nameof(IValidateProperty.IsValid) ||
            e.PropertyName == nameof(IProperty.IsBusy) ||
            e.PropertyName == nameof(IProperty.IsReadOnly) ||
            e.PropertyName == nameof(IProperty.Value))
        {
            InvokeAsync(StateHasChanged);
        }
    }

    public void Dispose()
    {
        EntityProperty.PropertyChanged -= OnPropertyChanged;
    }
}
