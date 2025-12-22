using Microsoft.AspNetCore.Components;
using MudBlazor;
using Neatoo;
using System.ComponentModel;

namespace Neatoo.Blazor.MudNeatoo.Validation;

/// <summary>
/// A validation summary component that displays all PropertyMessages from an IValidateBase entity
/// using a MudAlert component.
/// </summary>
public partial class NeatooValidationSummary : ComponentBase, IDisposable
{
    /// <summary>
    /// The entity to display validation messages for. This is required.
    /// </summary>
    [Parameter, EditorRequired]
    public IValidateMetaProperties Entity { get; set; } = default!;

    /// <summary>
    /// Whether to show the header text above the error list.
    /// </summary>
    [Parameter]
    public bool ShowHeader { get; set; } = true;

    /// <summary>
    /// The header text to display above the error list.
    /// </summary>
    [Parameter]
    public string HeaderText { get; set; } = "Please correct the following errors:";

    /// <summary>
    /// The visual variant of the alert.
    /// </summary>
    [Parameter]
    public Variant Variant { get; set; } = Variant.Filled;

    /// <summary>
    /// Whether to use the dense mode for the alert.
    /// </summary>
    [Parameter]
    public bool Dense { get; set; } = false;

    /// <summary>
    /// The elevation of the alert (0-24).
    /// </summary>
    [Parameter]
    public int Elevation { get; set; } = 0;

    /// <summary>
    /// Additional CSS class(es) to apply.
    /// </summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>
    /// Whether to include property display names in the error messages.
    /// </summary>
    [Parameter]
    public bool IncludePropertyNames { get; set; } = true;

    private bool HasErrors => ErrorMessages.Any();

    private IEnumerable<string> ErrorMessages
    {
        get
        {
            if (IncludePropertyNames)
            {
                return Entity.PropertyMessages
                    .Select(m => $"{GetDisplayName(m.Property)}: {m.Message}")
                    .Distinct();
            }
            else
            {
                return Entity.PropertyMessages
                    .Select(m => m.Message)
                    .Distinct();
            }
        }
    }

    private string GetDisplayName(IProperty property)
    {
        if (property is IEntityProperty entityProperty)
        {
            return entityProperty.DisplayName ?? entityProperty.Name;
        }
        return property.Name;
    }

    protected override void OnInitialized()
    {
        if (Entity is INotifyPropertyChanged notifyPropertyChanged)
        {
            notifyPropertyChanged.PropertyChanged += OnPropertyChanged;
        }
        if (Entity is INotifyNeatooPropertyChanged neatooNotify)
        {
            neatooNotify.NeatooPropertyChanged += OnNeatooPropertyChanged;
        }
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Re-render when validation state changes
        if (e.PropertyName == nameof(IValidateMetaProperties.IsValid) ||
            e.PropertyName == nameof(IValidateMetaProperties.IsSelfValid) ||
            e.PropertyName?.StartsWith("Is") == true)
        {
            InvokeAsync(StateHasChanged);
        }
    }

    private Task OnNeatooPropertyChanged(NeatooPropertyChangedEventArgs e)
    {
        // Re-render on any Neatoo property change to catch validation updates
        InvokeAsync(StateHasChanged);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (Entity is INotifyPropertyChanged notifyPropertyChanged)
        {
            notifyPropertyChanged.PropertyChanged -= OnPropertyChanged;
        }
        if (Entity is INotifyNeatooPropertyChanged neatooNotify)
        {
            neatooNotify.NeatooPropertyChanged -= OnNeatooPropertyChanged;
        }
    }
}
