using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Neatoo.Internal;

public delegate IPropertyManager<IProperty> CreatePropertyManager(IPropertyInfoList propertyInfoList);

public class PropertyManager<P> : IPropertyManager<P>, IJsonOnDeserialized
    where P : IProperty
{
    protected IFactory Factory { get; }

    protected readonly IPropertyInfoList PropertyInfoList;

    IPropertyInfoList IPropertyManager<P>.PropertyInfoList => this.PropertyInfoList;

    public bool IsBusy { get; protected set; }
    public bool HasProperty(string propertyName)
    {
        return this.PropertyInfoList.HasProperty(propertyName);
    }

    protected object _propertyBagLock = new object();
    protected IDictionary<string, P> _propertyBag = new Dictionary<string, P>();

    protected IDictionary<string, P> PropertyBag
    {
        get => this._propertyBag;
        set
        {
            this._propertyBag = value;
        }
    }

    public async Task WaitForTasks()
    {

        var busyTask = this.PropertyBag.FirstOrDefault(x => x.Value.IsBusy);

        while (busyTask.Value != null)
        {
            await busyTask.Value.WaitForTasks();
            busyTask = this.PropertyBag.FirstOrDefault(x => x.Value.IsBusy);
        }
    }

    public event NeatooPropertyChanged? NeatooPropertyChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

    private Task _Property_NeatooPropertyChanged(NeatooPropertyChangedEventArgs eventArgs)
    {
        return this.NeatooPropertyChanged?.Invoke(eventArgs) ?? Task.CompletedTask;
    }

    public PropertyManager(IPropertyInfoList propertyInfoList, IFactory factory)
    {
        this.PropertyInfoList = propertyInfoList;
        this.Factory = factory;
    }

    protected IProperty CreateProperty<PV>(IPropertyInfo propertyInfo)
    {
        return this.Factory.CreateProperty<PV>(propertyInfo);
    }

    private static MethodInfo? _createPropertyMethod;
    private static ConcurrentDictionary<Type, MethodInfo> _createPropertyMethodPropertyType = new();

    public virtual P GetProperty(string propertyName)
    {
        lock (this._propertyBagLock)
        {
            if (this.PropertyBag.TryGetValue(propertyName, out var property))
            {
                return property;
            }

            var propertyInfo = this.PropertyInfoList.GetPropertyInfo(propertyName);

            if (propertyInfo == null)
            {
                throw new PropertyNotFoundException($"Property '{propertyName}' not found in '{this.GetType().Name}'");
            }

            if (_createPropertyMethod == null)
            {
                _createPropertyMethod = this.GetType().GetMethod(nameof(CreateProperty), BindingFlags.NonPublic | BindingFlags.Instance);
            }

            if (!_createPropertyMethodPropertyType.TryGetValue(propertyInfo.Type, out var method))
            {
                method = _createPropertyMethodPropertyType[propertyInfo.Type] = _createPropertyMethod!.MakeGenericMethod(propertyInfo.Type);
            }

            var newProperty = (P)method.Invoke(this, new object[] { propertyInfo })!;

            newProperty.NeatooPropertyChanged += this._Property_NeatooPropertyChanged;
            newProperty.PropertyChanged += this._Property_PropertyChanged;

            this.PropertyBag[propertyName] = newProperty;

            return newProperty;
        }
    }

    protected virtual void Property_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IValidateProperty.IsBusy))
        {
            this.IsBusy = this.PropertyBag.Any(p => p.Value.IsBusy);
        }

        this.PropertyChanged?.Invoke(sender, e);
    }
    private void _Property_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        this.Property_PropertyChanged(sender, e);
    }

    public P? this[string propertyName]
    {
        get => this.GetProperty(propertyName);
    }

    void IPropertyManager<P>.SetProperties(IEnumerable<IProperty> properties)
    {
        foreach (var p in properties.Cast<P>())
        {
            this.PropertyBag[p.Name] = p;
        }
    }

    public virtual void OnDeserialized()
    {
        foreach (var p in this.PropertyBag)
        {
            p.Value.NeatooPropertyChanged += this._Property_NeatooPropertyChanged;
            p.Value.PropertyChanged += this._Property_PropertyChanged;
        }
    }

    void IJsonOnDeserialized.OnDeserialized()
    {
        this.OnDeserialized();
    }

    IEnumerable<P> IPropertyManager<P>.GetProperties => this.PropertyBag.Select(p => p.Value);
}

/// <summary>
/// Exception thrown when a property value type does not match the expected type.
/// </summary>
[Serializable]
public class PropertyTypeMismatchException : PropertyException
{
    public PropertyTypeMismatchException() { }
    public PropertyTypeMismatchException(string message) : base(message) { }
    public PropertyTypeMismatchException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// Exception thrown when attempting to access a property that does not exist.
/// </summary>
[Serializable]
public class PropertyNotFoundException : PropertyException
{
    public PropertyNotFoundException() { }
    public PropertyNotFoundException(string message) : base(message) { }
    public PropertyNotFoundException(string message, Exception inner) : base(message, inner) { }
}
