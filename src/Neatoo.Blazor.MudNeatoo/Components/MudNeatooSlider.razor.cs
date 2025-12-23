using Microsoft.AspNetCore.Components;
using MudBlazor;
using Neatoo;
using System.ComponentModel;
using System.Numerics;

namespace Neatoo.Blazor.MudNeatoo.Components;

/// <summary>
/// A MudBlazor slider component that binds to an IEntityProperty
/// and displays Neatoo validation messages.
/// </summary>
/// <typeparam name="T">The numeric type of the property value (e.g., int, decimal, double).</typeparam>
public partial class MudNeatooSlider<T> : ComponentBase, IDisposable where T : struct, INumber<T>
{
    /// <summary>
    /// The entity property to bind to. This is required.
    /// </summary>
    [Parameter, EditorRequired]
    public IEntityProperty EntityProperty { get; set; } = default!;

    /// <summary>
    /// The label text displayed above the slider.
    /// If not specified, uses the EntityProperty.DisplayName.
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>
    /// Child content to display (e.g., current value display).
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The minimum value of the slider. This is required.
    /// </summary>
    [Parameter, EditorRequired]
    public T Min { get; set; } = default!;

    /// <summary>
    /// The maximum value of the slider. This is required.
    /// </summary>
    [Parameter, EditorRequired]
    public T Max { get; set; } = default!;

    /// <summary>
    /// The step increment of the slider.
    /// </summary>
    [Parameter]
    public T Step { get; set; } = default!;

    /// <summary>
    /// The visual variant of the slider.
    /// </summary>
    [Parameter]
    public Variant Variant { get; set; } = Variant.Filled;

    /// <summary>
    /// The color of the slider.
    /// </summary>
    [Parameter]
    public Color Color { get; set; } = Color.Primary;

    /// <summary>
    /// The size of the slider.
    /// </summary>
    [Parameter]
    public Size Size { get; set; } = Size.Medium;

    /// <summary>
    /// If true, updates the value immediately while dragging.
    /// </summary>
    [Parameter]
    public bool Immediate { get; set; } = true;

    /// <summary>
    /// If true, shows tick marks along the slider.
    /// </summary>
    [Parameter]
    public bool TickMarks { get; set; } = false;

    /// <summary>
    /// Labels to display at tick mark positions.
    /// </summary>
    [Parameter]
    public string[]? TickMarkLabels { get; set; }

    /// <summary>
    /// If true, shows the current value label while dragging.
    /// </summary>
    [Parameter]
    public bool ValueLabel { get; set; } = false;

    /// <summary>
    /// If true, shows validation errors below the slider.
    /// </summary>
    [Parameter]
    public bool ShowValidation { get; set; } = true;

    /// <summary>
    /// Additional CSS class(es) to apply.
    /// </summary>
    [Parameter]
    public string? Class { get; set; }

    private string? DisplayLabel => Label ?? EntityProperty.DisplayName;

    private T TypedValue => EntityProperty.Value is T val ? val : default;

    private bool HasErrors => EntityProperty.PropertyMessages.Any();

    protected override void OnInitialized()
    {
        EntityProperty.PropertyChanged += OnPropertyChanged;
    }

    private async Task OnValueChanged(T value)
    {
        await EntityProperty.SetValue(value);
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
