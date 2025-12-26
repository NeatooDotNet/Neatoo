using Microsoft.AspNetCore.Components;
using MudBlazor;
using Neatoo;
using System.ComponentModel;

namespace Neatoo.Blazor.MudNeatoo.Components;

/// <summary>
/// A MudBlazor date range picker component that binds to an IEntityProperty
/// and displays Neatoo validation messages.
/// </summary>
public partial class MudNeatooDateRangePicker : ComponentBase, IDisposable
{
    /// <summary>
    /// The entity property to bind to. This is required.
    /// The property value should be of type DateRange.
    /// </summary>
    [Parameter, EditorRequired]
    public IEntityProperty EntityProperty { get; set; } = default!;

    /// <summary>
    /// The visual variant of the date range picker.
    /// </summary>
    [Parameter]
    public Variant Variant { get; set; } = Variant.Outlined;

    /// <summary>
    /// The margin around the date range picker.
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
    /// Placeholder text shown when no date range is selected.
    /// </summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>
    /// The date format string (e.g., "MM/dd/yyyy").
    /// </summary>
    [Parameter]
    public string? DateFormat { get; set; }

    /// <summary>
    /// If true, allows direct text editing of the date.
    /// </summary>
    [Parameter]
    public bool Editable { get; set; } = false;

    /// <summary>
    /// If true, shows a clear button to reset the value.
    /// </summary>
    [Parameter]
    public bool Clearable { get; set; } = false;

    /// <summary>
    /// The minimum selectable date.
    /// </summary>
    [Parameter]
    public DateTime? MinDate { get; set; }

    /// <summary>
    /// The maximum selectable date.
    /// </summary>
    [Parameter]
    public DateTime? MaxDate { get; set; }

    /// <summary>
    /// The picker variant (Inline, Dialog, or Static).
    /// </summary>
    [Parameter]
    public PickerVariant PickerVariant { get; set; } = PickerVariant.Inline;

    /// <summary>
    /// The primary color of the picker.
    /// </summary>
    [Parameter]
    public Color Color { get; set; } = Color.Primary;

    /// <summary>
    /// The first day of the week for the calendar.
    /// </summary>
    [Parameter]
    public DayOfWeek? FirstDayOfWeek { get; set; }

    /// <summary>
    /// The icon for the adornment (defaults to calendar icon).
    /// </summary>
    [Parameter]
    public string AdornmentIcon { get; set; } = Icons.Material.Filled.DateRange;

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

    private DateRange? TypedValue => (DateRange?)this.EntityProperty.Value;

    protected override void OnInitialized()
    {
        this.EntityProperty.PropertyChanged += this.OnPropertyChanged;
    }

    private async Task OnValueChanged(DateRange? value)
    {
        await this.EntityProperty.SetValue(value);
    }

    private async Task<IEnumerable<string>> ValidateAsync(DateRange? value)
    {
        await this.EntityProperty.WaitForTasks();
        return this.EntityProperty.PropertyMessages.Select(m => m.Message).Distinct();
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IValidateProperty.PropertyMessages) ||
            e.PropertyName == nameof(IValidateProperty.IsValid) ||
            e.PropertyName == nameof(IProperty.IsBusy) ||
            e.PropertyName == nameof(IProperty.IsReadOnly) ||
            e.PropertyName == nameof(IProperty.Value))
        {
            this.InvokeAsync(this.StateHasChanged);
        }
    }

    public void Dispose()
    {
        this.EntityProperty.PropertyChanged -= this.OnPropertyChanged;
    }
}
