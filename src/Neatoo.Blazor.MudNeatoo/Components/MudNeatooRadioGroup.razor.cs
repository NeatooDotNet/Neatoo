using Microsoft.AspNetCore.Components;
using MudBlazor;
using Neatoo;
using System.ComponentModel;

namespace Neatoo.Blazor.MudNeatoo.Components;

/// <summary>
/// A MudBlazor radio group component that binds to an IEntityProperty
/// and displays Neatoo validation messages.
/// </summary>
/// <typeparam name="T">The type of the property value.</typeparam>
public partial class MudNeatooRadioGroup<T> : ComponentBase, IDisposable
{
    /// <summary>
    /// The entity property to bind to. This is required.
    /// </summary>
    [Parameter, EditorRequired]
    public IEntityProperty EntityProperty { get; set; } = default!;

    /// <summary>
    /// The child content containing MudRadio elements.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// If true, shows validation errors below the radio group.
    /// </summary>
    [Parameter]
    public bool ShowValidation { get; set; } = true;

    /// <summary>
    /// Additional CSS class(es) to apply.
    /// </summary>
    [Parameter]
    public string? Class { get; set; }

    private T? TypedValue => (T?)EntityProperty.Value;

    private bool HasErrors => EntityProperty.PropertyMessages.Any();

    protected override void OnInitialized()
    {
        EntityProperty.PropertyChanged += OnPropertyChanged;
    }

    private async Task OnValueChanged(T? value)
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
