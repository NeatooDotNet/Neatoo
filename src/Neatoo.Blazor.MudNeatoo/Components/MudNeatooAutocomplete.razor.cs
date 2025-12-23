using Microsoft.AspNetCore.Components;
using MudBlazor;
using Neatoo;
using System.ComponentModel;

namespace Neatoo.Blazor.MudNeatoo.Components;

/// <summary>
/// A MudBlazor autocomplete component that binds to an IEntityProperty
/// and displays Neatoo validation messages.
/// </summary>
/// <typeparam name="T">The type of the property value.</typeparam>
public partial class MudNeatooAutocomplete<T> : ComponentBase, IDisposable
{
    /// <summary>
    /// The entity property to bind to. This is required.
    /// </summary>
    [Parameter, EditorRequired]
    public IEntityProperty EntityProperty { get; set; } = default!;

    /// <summary>
    /// The search function that returns matching items. This is required.
    /// </summary>
    [Parameter, EditorRequired]
    public Func<string?, CancellationToken, Task<IEnumerable<T>>> SearchFunc { get; set; } = default!;

    /// <summary>
    /// Function to convert an item to its display string.
    /// </summary>
    [Parameter]
    public Func<T?, string>? ToStringFunc { get; set; }

    /// <summary>
    /// Template for rendering each item in the dropdown.
    /// </summary>
    [Parameter]
    public RenderFragment<T>? ItemTemplate { get; set; }

    /// <summary>
    /// The visual variant of the autocomplete.
    /// </summary>
    [Parameter]
    public Variant Variant { get; set; } = Variant.Outlined;

    /// <summary>
    /// The margin around the autocomplete.
    /// </summary>
    [Parameter]
    public Margin Margin { get; set; } = Margin.Dense;

    /// <summary>
    /// If true, uses dense padding.
    /// </summary>
    [Parameter]
    public bool Dense { get; set; } = false;

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
    /// If true, shows a clear button to reset the value.
    /// </summary>
    [Parameter]
    public bool Clearable { get; set; } = false;

    /// <summary>
    /// If true, resets the value when the text is cleared.
    /// </summary>
    [Parameter]
    public bool ResetValueOnEmptyText { get; set; } = false;

    /// <summary>
    /// If true, forces the text to match the selected value's display text.
    /// </summary>
    [Parameter]
    public bool CoerceText { get; set; } = true;

    /// <summary>
    /// If true, tries to coerce the value from the entered text.
    /// </summary>
    [Parameter]
    public bool CoerceValue { get; set; } = false;

    /// <summary>
    /// The debounce interval in milliseconds before triggering a search.
    /// </summary>
    [Parameter]
    public int DebounceInterval { get; set; } = 300;

    /// <summary>
    /// Minimum number of characters required before searching.
    /// </summary>
    [Parameter]
    public int MinCharacters { get; set; } = 0;

    /// <summary>
    /// Maximum number of items to display in the dropdown.
    /// </summary>
    [Parameter]
    public int? MaxItems { get; set; }

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
    /// If true, shows a progress indicator while searching.
    /// </summary>
    [Parameter]
    public bool ShowProgressIndicator { get; set; } = true;

    /// <summary>
    /// The color of the progress indicator.
    /// </summary>
    [Parameter]
    public Color ProgressIndicatorColor { get; set; } = Color.Primary;

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
        await EntityProperty.SetValue(value);
    }

    private async Task<IEnumerable<string>> ValidateAsync(T? value)
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
