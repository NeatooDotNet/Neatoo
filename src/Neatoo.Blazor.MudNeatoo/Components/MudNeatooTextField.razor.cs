using Microsoft.AspNetCore.Components;
using MudBlazor;
using Neatoo;
using System.ComponentModel;

namespace Neatoo.Blazor.MudNeatoo.Components;

/// <summary>
/// A MudBlazor text field component that binds to an IEntityProperty
/// and displays Neatoo validation messages.
/// </summary>
/// <typeparam name="T">The type of the property value (e.g., string, int, decimal).</typeparam>
public partial class MudNeatooTextField<T> : ComponentBase, IDisposable
{
    /// <summary>
    /// The entity property to bind to. This is required.
    /// </summary>
    [Parameter, EditorRequired]
    public IEntityProperty EntityProperty { get; set; } = default!;

    /// <summary>
    /// The visual variant of the text field.
    /// </summary>
    [Parameter]
    public Variant Variant { get; set; } = Variant.Outlined;

    /// <summary>
    /// The margin around the text field.
    /// </summary>
    [Parameter]
    public Margin Margin { get; set; } = Margin.Dense;

    /// <summary>
    /// Number of lines for multiline input. Set to greater than 1 for textarea.
    /// </summary>
    [Parameter]
    public int Lines { get; set; } = 1;

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
    /// The color of the adornment icon.
    /// </summary>
    [Parameter]
    public Color AdornmentColor { get; set; } = Color.Default;

    /// <summary>
    /// Additional CSS class(es) to apply.
    /// </summary>
    [Parameter]
    public string? Class { get; set; }

    private T? TypedValue => (T?)EntityProperty.Value;

    protected override void OnInitialized()
    {
        EntityProperty.PropertyChanged += OnPropertyChanged;
    }

    private async Task OnValueChanged(T? value)
    {
        // With Immediate="false", this only fires on blur
        // Sync to Neatoo - this triggers business rules
        await EntityProperty.SetValue(value);
    }

    private async Task<IEnumerable<string>> ValidateAsync(T? value)
    {
        // Wait for any async rules to complete before returning validation messages
        await EntityProperty.WaitForTasks();
        return EntityProperty.PropertyMessages.Select(m => m.Message).Distinct();
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Re-render when validation state, busy state, or read-only changes
        if (e.PropertyName == nameof(IValidateProperty.PropertyMessages) ||
            e.PropertyName == nameof(IValidateProperty.IsValid) ||
            e.PropertyName == nameof(IProperty.IsBusy) ||
            e.PropertyName == nameof(IProperty.IsReadOnly))
        {
            InvokeAsync(StateHasChanged);
        }
    }

    public void Dispose()
    {
        EntityProperty.PropertyChanged -= OnPropertyChanged;
    }
}
