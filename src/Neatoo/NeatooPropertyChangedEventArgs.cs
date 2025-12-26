namespace Neatoo;

public record NeatooPropertyChangedEventArgs
{
    public NeatooPropertyChangedEventArgs(string propertyName, object source)
    {
        this.PropertyName = propertyName;
        this.Source = source;
        this.OriginalEventArgs = this;
    }

    public NeatooPropertyChangedEventArgs(IProperty property)
    {
        ArgumentNullException.ThrowIfNull(property, nameof(property));
        this.PropertyName = property.Name;
        this.Property = property;
        this.Source = property;
        this.OriginalEventArgs = this;
    }

    public NeatooPropertyChangedEventArgs(IProperty property, object source, NeatooPropertyChangedEventArgs? previous)
    {
        ArgumentNullException.ThrowIfNull(property, nameof(property));
        ArgumentNullException.ThrowIfNull(source, nameof(source));
        this.PropertyName = property.Name;
        this.Property = property;
        this.Source = source;
        this.InnerEventArgs = previous;
        this.OriginalEventArgs = previous?.OriginalEventArgs ?? this;
    }

    public string PropertyName { get; init; }
    public IProperty? Property { get; init; }
    public object? Source { get; init; }
    public NeatooPropertyChangedEventArgs OriginalEventArgs { get; init; }
    public NeatooPropertyChangedEventArgs? InnerEventArgs { get; init; }
    public string FullPropertyName => this.PropertyName + (this.InnerEventArgs == null ? "" : "." + this.InnerEventArgs.FullPropertyName);
}
