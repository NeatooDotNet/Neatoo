using Microsoft.AspNetCore.Components;
using MudBlazor;
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
    /// If true, the field is disabled regardless of the entity property's busy state.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

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
    /// The resizing behavior of the input. Use <see cref="InputSizing.Auto"/> for a textarea that grows with its content (requires <see cref="Lines"/> &gt; 1).
    /// </summary>
    [Parameter]
    public InputSizing Sizing { get; set; } = InputSizing.Fixed;

    /// <summary>
    /// The maximum number of lines the textarea will grow to when <see cref="Sizing"/> is <see cref="InputSizing.Auto"/>. 0 means unlimited.
    /// </summary>
    [Parameter]
    public int MaxLines { get; set; }

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

    /// <summary>
    /// Arbitrary HTML attributes forwarded to the underlying MudBlazor component, which
    /// spreads them onto the native <c>&lt;input&gt;</c> or <c>&lt;textarea&gt;</c> element.
    /// Use this as an escape hatch for attributes not exposed as typed parameters --
    /// for example <c>spellcheck="true"</c> or <c>style="resize: vertical;"</c>.
    /// </summary>
    [Parameter]
    public Dictionary<string, object>? UserAttributes { get; set; }

    private T? TypedValue => (T?)this.EntityProperty.Value;

    protected override void OnInitialized()
    {
        this.EntityProperty.PropertyChanged += this.OnPropertyChanged;
    }

    private async Task OnValueChanged(T? value)
    {
        // With Immediate="false", this only fires on blur
        // Sync to Neatoo - this triggers business rules
        await this.EntityProperty.SetValue(value);
    }

    private async Task<IEnumerable<string>> ValidateAsync(T? value)
    {
        // Wait for any async rules to complete before returning validation messages
        await this.EntityProperty.WaitForTasks();
        return this.EntityProperty.PropertyMessages.Select(m => m.Message).Distinct();
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Re-render when validation state, busy state, or read-only changes
        if (e.PropertyName == nameof(IValidateProperty.PropertyMessages) ||
            e.PropertyName == nameof(IValidateProperty.IsValid) ||
            e.PropertyName == nameof(IValidateProperty.IsBusy) ||
            e.PropertyName == nameof(IValidateProperty.IsReadOnly))
        {
            this.InvokeAsync(this.StateHasChanged);
        }
    }

    public void Dispose()
    {
        this.EntityProperty.PropertyChanged -= this.OnPropertyChanged;
    }
}
