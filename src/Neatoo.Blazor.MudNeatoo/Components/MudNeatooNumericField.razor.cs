using Microsoft.AspNetCore.Components;
using MudBlazor;
using Neatoo;
using System.ComponentModel;

namespace Neatoo.Blazor.MudNeatoo.Components;

/// <summary>
/// A MudBlazor numeric field component that binds to an IEntityProperty
/// and displays Neatoo validation messages.
/// </summary>
/// <typeparam name="T">The numeric type of the property value (e.g., int, decimal, double).</typeparam>
public partial class MudNeatooNumericField<T> : ComponentBase, IDisposable
{
    /// <summary>
    /// The entity property to bind to. This is required.
    /// </summary>
    [Parameter, EditorRequired]
    public IEntityProperty EntityProperty { get; set; } = default!;

    /// <summary>
    /// If true, the field is disabled regardless of the entity property's busy state.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// The visual variant of the numeric field.
    /// </summary>
    [Parameter]
    public Variant Variant { get; set; } = Variant.Outlined;

    /// <summary>
    /// The margin around the numeric field.
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
    /// Placeholder text shown when the input is empty.
    /// </summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>
    /// The adornment location (Start, End, or None).
    /// </summary>
    [Parameter]
    public Adornment Adornment { get; set; } = Adornment.None;

    /// <summary>
    /// The icon for the adornment.
    /// </summary>
    [Parameter]
    public string? AdornmentIcon { get; set; }

    /// <summary>
    /// Text to display as an adornment.
    /// </summary>
    [Parameter]
    public string? AdornmentText { get; set; }

    /// <summary>
    /// The color of the adornment.
    /// </summary>
    [Parameter]
    public Color AdornmentColor { get; set; } = Color.Default;

    /// <summary>
    /// The format string for displaying the value.
    /// </summary>
    [Parameter]
    public string? Format { get; set; }

    /// <summary>
    /// The minimum allowed value. When unset, MudBlazor's default (<c>T.MinValue</c>) is used.
    /// </summary>
    [Parameter]
    public T? Min { get; set; }

    /// <summary>
    /// The maximum allowed value. When unset, MudBlazor's default (<c>T.MaxValue</c>) is used.
    /// </summary>
    [Parameter]
    public T? Max { get; set; }

    /// <summary>
    /// The increment/decrement step when using spin buttons. When unset, MudBlazor's default (<c>1</c>) is used.
    /// </summary>
    [Parameter]
    public T? Step { get; set; }

    /// <summary>
    /// If true, hides the spin buttons.
    /// </summary>
    [Parameter]
    public bool HideSpinButtons { get; set; } = false;

    /// <summary>
    /// Additional CSS class(es) to apply.
    /// </summary>
    [Parameter]
    public string? Class { get; set; }

    private T? TypedValue => (T?)this.EntityProperty.Value;

    // Splat only the Min/Max/Step values the caller actually set.
    // Forwarding null would be converted to default(T) (e.g. 0 for int) and silently clamp values --
    // instead we omit the attribute entirely so MudNumericField uses its own T.MinValue/T.MaxValue/1 defaults.
    // This works for both non-nullable (int) and nullable (int?) T.
    private Dictionary<string, object> MinMaxStepAttributes
    {
        get
        {
            var attrs = new Dictionary<string, object>();
            if (this.Min is not null) attrs["Min"] = this.Min;
            if (this.Max is not null) attrs["Max"] = this.Max;
            if (this.Step is not null) attrs["Step"] = this.Step;
            return attrs;
        }
    }

    protected override void OnInitialized()
    {
        this.EntityProperty.PropertyChanged += this.OnPropertyChanged;
    }

    private async Task OnValueChanged(T? value)
    {
        await this.EntityProperty.SetValue(value);
    }

    private async Task<IEnumerable<string>> ValidateAsync(T? value)
    {
        await this.EntityProperty.WaitForTasks();
        return this.EntityProperty.PropertyMessages.Select(m => m.Message).Distinct();
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IValidateProperty.PropertyMessages) ||
            e.PropertyName == nameof(IValidateProperty.IsValid) ||
            e.PropertyName == nameof(IValidateProperty.IsBusy) ||
            e.PropertyName == nameof(IValidateProperty.IsReadOnly) ||
            e.PropertyName == nameof(IValidateProperty.Value))
        {
            this.InvokeAsync(this.StateHasChanged);
        }
    }

    public void Dispose()
    {
        this.EntityProperty.PropertyChanged -= this.OnPropertyChanged;
    }
}
