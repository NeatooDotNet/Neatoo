using Microsoft.AspNetCore.Components;
using MudBlazor;
using Neatoo;
using System.ComponentModel;

namespace Neatoo.Blazor.MudNeatoo.Components;

/// <summary>
/// A MudBlazor switch component that binds to an IEntityProperty
/// and displays Neatoo validation messages.
/// </summary>
/// <typeparam name="T">The type of the property value (typically bool or bool?).</typeparam>
public partial class MudNeatooSwitch<T> : ComponentBase, IDisposable
{
    /// <summary>
    /// The entity property to bind to. This is required.
    /// </summary>
    [Parameter, EditorRequired]
    public IEntityProperty EntityProperty { get; set; } = default!;

    /// <summary>
    /// The label text displayed next to the switch.
    /// If not specified, uses the EntityProperty.DisplayName.
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>
    /// The color of the switch when on.
    /// </summary>
    [Parameter]
    public Color Color { get; set; } = Color.Primary;

    /// <summary>
    /// The color of the switch when off.
    /// </summary>
    [Parameter]
    public Color UncheckedColor { get; set; } = Color.Default;

    /// <summary>
    /// The size of the switch.
    /// </summary>
    [Parameter]
    public Size Size { get; set; } = Size.Medium;

    /// <summary>
    /// The icon to display on the thumb.
    /// </summary>
    [Parameter]
    public string? ThumbIcon { get; set; }

    /// <summary>
    /// The color of the thumb icon.
    /// </summary>
    [Parameter]
    public Color ThumbIconColor { get; set; } = Color.Default;

    /// <summary>
    /// If true, shows validation errors below the switch.
    /// </summary>
    [Parameter]
    public bool ShowValidation { get; set; } = true;

    /// <summary>
    /// Additional CSS class(es) to apply.
    /// </summary>
    [Parameter]
    public string? Class { get; set; }

    private string DisplayLabel => this.Label ?? this.EntityProperty.DisplayName;

    private T? TypedValue => (T?)this.EntityProperty.Value;

    private bool HasErrors => this.EntityProperty.PropertyMessages.Any();

    protected override void OnInitialized()
    {
        this.EntityProperty.PropertyChanged += this.OnPropertyChanged;
    }

    private async Task OnValueChanged(T? value)
    {
        await this.EntityProperty.SetValue(value);
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
