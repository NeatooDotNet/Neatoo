namespace Neatoo;

public class NeatooPropertyChangedEventArgs
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
        PropertyName = property.Name;
        Property = property;
        Source = source;
        InnerEventArgs = previous;
        OriginalEventArgs = previous?.OriginalEventArgs ?? this;
    }

    public string PropertyName { get; }
    public IProperty? Property { get; private set; }
    public object? Source { get; private set; }
    public NeatooPropertyChangedEventArgs OriginalEventArgs { get; private set; }
    public NeatooPropertyChangedEventArgs? InnerEventArgs { get; private set; }
    public string FullPropertyName => PropertyName + (InnerEventArgs == null ? "" : "." + InnerEventArgs.FullPropertyName);
}
