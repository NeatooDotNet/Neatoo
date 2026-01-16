namespace Neatoo;

public record NeatooPropertyChangedEventArgs
{
    public NeatooPropertyChangedEventArgs(string propertyName, object source, ChangeReason reason = ChangeReason.UserEdit)
    {
        this.PropertyName = propertyName;
        this.Source = source;
        this.Reason = reason;
        this.OriginalEventArgs = this;
    }

    public NeatooPropertyChangedEventArgs(IValidateProperty property, ChangeReason reason = ChangeReason.UserEdit)
    {
        ArgumentNullException.ThrowIfNull(property, nameof(property));
        this.PropertyName = property.Name;
        this.Property = property;
        this.Source = property;
        this.Reason = reason;
        this.OriginalEventArgs = this;
    }

    public NeatooPropertyChangedEventArgs(IValidateProperty property, object source, NeatooPropertyChangedEventArgs? previous)
    {
        ArgumentNullException.ThrowIfNull(property, nameof(property));
        ArgumentNullException.ThrowIfNull(source, nameof(source));
        this.PropertyName = property.Name;
        this.Property = property;
        this.Source = source;
        this.InnerEventArgs = previous;
        this.OriginalEventArgs = previous?.OriginalEventArgs ?? this;
        // Inherit reason from original event args
        this.Reason = this.OriginalEventArgs.Reason;
    }

    public string PropertyName { get; init; }
    public IValidateProperty? Property { get; init; }
    public object? Source { get; init; }
    public NeatooPropertyChangedEventArgs OriginalEventArgs { get; init; }
    public NeatooPropertyChangedEventArgs? InnerEventArgs { get; init; }
    public string FullPropertyName => this.PropertyName + (this.InnerEventArgs == null ? "" : "." + this.InnerEventArgs.FullPropertyName);

    /// <summary>
    /// Gets the reason this property change occurred.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="ChangeReason.UserEdit"/> indicates a normal property setter assignment
    /// that should trigger rules and bubble events.
    /// </para>
    /// <para>
    /// <see cref="ChangeReason.Load"/> indicates data loading via <see cref="IValidateProperty.LoadValue"/>
    /// that should only establish structural relationships (SetParent) without running rules.
    /// </para>
    /// </remarks>
    public ChangeReason Reason { get; init; } = ChangeReason.UserEdit;
}
