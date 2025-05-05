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

    IPropertyInfoList IPropertyManager<P>.PropertyInfoList => PropertyInfoList;

    public bool IsBusy { get; protected set; }
    public bool HasProperty(string propertyName)
    {
        return PropertyInfoList.HasProperty(propertyName);
    }

    protected object _propertyBagLock = new object();
    protected IDictionary<string, P> _propertyBag = new Dictionary<string, P>();

    protected IDictionary<string, P> PropertyBag
    {
        get => _propertyBag;
        set
        {
            _propertyBag = value;
        }
    }

    public async Task WaitForTasks()
    {

        var busyTask = PropertyBag.FirstOrDefault(x => x.Value.IsBusy);

        while (busyTask.Value != null)
        {
            await busyTask.Value.WaitForTasks();
            busyTask = PropertyBag.FirstOrDefault(x => x.Value.IsBusy);
        }
    }

    public event NeatooPropertyChanged? NeatooPropertyChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

    private Task _Property_NeatooPropertyChanged(NeatooPropertyChangedEventArgs eventArgs)
    {
        return NeatooPropertyChanged?.Invoke(eventArgs) ?? Task.CompletedTask;
    }

    public PropertyManager(IPropertyInfoList propertyInfoList, IFactory factory)
    {
        this.PropertyInfoList = propertyInfoList;
        Factory = factory;
    }

    protected IProperty CreateProperty<PV>(IPropertyInfo propertyInfo)
    {
        return Factory.CreateProperty<PV>(propertyInfo);
    }

    private static MethodInfo? createPropertyMethod;
    private static ConcurrentDictionary<Type, MethodInfo> createPropertyMethodPropertyType = new();

    public virtual P GetProperty(string propertyName)
    {
        lock (_propertyBagLock)
        {
            if (PropertyBag.TryGetValue(propertyName, out var property))
            {
                return property;
            }

            var propertyInfo = PropertyInfoList.GetPropertyInfo(propertyName);

            if (propertyInfo == null)
            {
                throw new PropertyNotFoundException($"Property '{propertyName}' not found in '{GetType().Name}'");
            }

            if (createPropertyMethod == null)
            {
                createPropertyMethod = GetType().GetMethod(nameof(CreateProperty), BindingFlags.NonPublic | BindingFlags.Instance);
            }

            if(!createPropertyMethodPropertyType.TryGetValue(propertyInfo.Type, out var method))
            {
                method = createPropertyMethodPropertyType[propertyInfo.Type] = createPropertyMethod!.MakeGenericMethod(propertyInfo.Type);
            }

            var newProperty = (P)method.Invoke(this, new object[] { propertyInfo })!;

            newProperty.NeatooPropertyChanged += _Property_NeatooPropertyChanged;
            newProperty.PropertyChanged += _Property_PropertyChanged;

            PropertyBag[propertyName] = newProperty;

            return newProperty;
        }
    }

    protected virtual void Property_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IValidateProperty.IsBusy))
        {
            IsBusy = PropertyBag.Any(p => p.Value.IsBusy);
        }

        PropertyChanged?.Invoke(sender, e);
    }
    private void _Property_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Property_PropertyChanged(sender, e);
    }

    public P? this[string propertyName]
    {
        get => GetProperty(propertyName);
    }

    void IPropertyManager<P>.SetProperties(IEnumerable<IProperty> properties)
    {
        foreach (var p in properties.Cast<P>())
        {
            PropertyBag[p.Name] = p;
        }
    }

    public virtual void OnDeserialized()
    {
        foreach (var p in PropertyBag)
        {
            p.Value.NeatooPropertyChanged += _Property_NeatooPropertyChanged;
            p.Value.PropertyChanged += _Property_PropertyChanged;
        }
    }

    void IJsonOnDeserialized.OnDeserialized()
    {
        OnDeserialized();
    }

    IEnumerable<P> IPropertyManager<P>.GetProperties => PropertyBag.Select(p => p.Value);
}

[Serializable]
public class PropertyTypeMismatchException : Exception
{
    public PropertyTypeMismatchException() { }
    public PropertyTypeMismatchException(string message) : base(message) { }
    public PropertyTypeMismatchException(string message, Exception inner) : base(message, inner) { }
}

[Serializable]
public class PropertyNotFoundException : Exception
{
    public PropertyNotFoundException() { }
    public PropertyNotFoundException(string message) : base(message) { }
    public PropertyNotFoundException(string message, Exception inner) : base(message, inner) { }
}




