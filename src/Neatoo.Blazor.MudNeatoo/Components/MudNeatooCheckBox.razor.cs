using Microsoft.AspNetCore.Components;
using MudBlazor;
using Neatoo;
using System.ComponentModel;

namespace Neatoo.Blazor.MudNeatoo.Components;

/// <summary>
/// A MudBlazor checkbox component that binds to an IEntityProperty
/// and displays Neatoo validation messages.
/// </summary>
/// <typeparam name="T">The type of the property value (typically bool or bool?).</typeparam>
public partial class MudNeatooCheckBox<T> : ComponentBase, IDisposable
{
    /// <summary>
    /// The entity property to bind to. This is required.
    /// </summary>
    [Parameter, EditorRequired]
    public IEntityProperty EntityProperty { get; set; } = default!;

    /// <summary>
    /// The label text displayed next to the checkbox.
    /// If not specified, uses the EntityProperty.DisplayName.
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>
    /// The color of the checkbox when checked.
    /// </summary>
    [Parameter]
    public Color Color { get; set; } = Color.Primary;

    /// <summary>
    /// The color of the checkbox when unchecked.
    /// </summary>
    [Parameter]
    public Color UncheckedColor { get; set; } = Color.Default;

    /// <summary>
    /// The size of the checkbox.
    /// </summary>
    [Parameter]
    public Size Size { get; set; } = Size.Medium;

    /// <summary>
    /// If true, uses dense padding.
    /// </summary>
    [Parameter]
    public bool Dense { get; set; } = false;

    /// <summary>
    /// If true, allows three states: true, false, and null.
    /// </summary>
    [Parameter]
    public bool TriState { get; set; } = false;

    /// <summary>
    /// If true, shows validation errors below the checkbox.
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
