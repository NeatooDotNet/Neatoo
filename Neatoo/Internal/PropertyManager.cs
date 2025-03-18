using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Neatoo.Core;

public interface IPropertyManager<out P> : INotifyNeatooPropertyChanged, INotifyPropertyChanged
    where P : IProperty
{
    bool IsBusy { get; }
    bool IsSelfBusy { get; }
    Task WaitForTasks();
    bool HasProperty(string propertyName);
    P GetProperty(string propertyName);
    public P this[string propertyName] { get => GetProperty(propertyName); }
    internal IPropertyInfoList PropertyInfoList { get; }
    internal IEnumerable<P> GetProperties { get; }
    void SetProperties(IEnumerable<IProperty> properties);
}

public delegate IPropertyManager<IProperty> CreatePropertyManager(IPropertyInfoList propertyInfoList);

public class PropertyManager<P> : IPropertyManager<P>, IJsonOnDeserialized
    where P : IProperty
{
    protected IFactory Factory { get; }

    protected readonly IPropertyInfoList PropertyInfoList;

    IPropertyInfoList IPropertyManager<P>.PropertyInfoList => PropertyInfoList;

    public bool IsBusy => PropertyBag.Any(_ => _.Value.IsBusy);
    public bool IsSelfBusy => PropertyBag.Any(_ => _.Value.IsSelfBusy);

    public bool HasProperty(string propertyName)
    {
        return PropertyInfoList.HasProperty(propertyName);
    }

    protected IDictionary<string, P> _propertyBag = new ConcurrentDictionary<string, P>();

    protected IDictionary<string, P> PropertyBag
    {
        get => _propertyBag;
        set
        {
            _propertyBag = value;
        }
    }
    
    public async Task WaitForTasks() {

        var busyTask = PropertyBag.FirstOrDefault(x => x.Value.IsBusy);

        while (busyTask.Value != null)
        {
            await busyTask.Value.WaitForTasks();
            busyTask = PropertyBag.FirstOrDefault(x => x.Value.IsBusy);
        }
    }

    public event NeatooPropertyChanged? NeatooPropertyChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

    private Task _Property_NeatooPropertyChanged(PropertyChangedBreadCrumbs breadCrumbs)
    {
        return NeatooPropertyChanged?.Invoke(breadCrumbs) ?? Task.CompletedTask;
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

    private MethodInfo? createPropertyMethod;

    public virtual P GetProperty(string propertyName)
    {
        if (PropertyBag.TryGetValue(propertyName, out var property))
        {
            return property;
        }

        if (createPropertyMethod == null)
        {
            createPropertyMethod = GetType().GetMethod(nameof(CreateProperty), BindingFlags.NonPublic | BindingFlags.Instance);
        }

        var propertyInfo = PropertyInfoList.GetPropertyInfo(propertyName);

        var newProperty = (P)createPropertyMethod!.MakeGenericMethod(propertyInfo.Type).Invoke(this, new object[] { propertyInfo })!;

        newProperty.NeatooPropertyChanged += _Property_NeatooPropertyChanged;
        newProperty.PropertyChanged += _Property_PropertyChanged;

        PropertyBag[propertyName] = newProperty;

        return newProperty;
    }

    private void _Property_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        PropertyChanged?.Invoke(sender, e);
    }

    public P this[string propertyName]
    {
        get => GetProperty(propertyName);
    }

    void IPropertyManager<P>.SetProperties(IEnumerable<IProperty> properties)
    {
        foreach (var p in properties.Cast<P>())
        {
            if (PropertyBag.TryGetValue(p.Name, out var fd))
            {
                throw new InvalidOperationException("Property already set");
            }
            PropertyBag[p.Name] = p;
        }
    }

    public void OnDeserialized()
    {
        foreach (var p in PropertyBag)
        {
            p.Value.NeatooPropertyChanged += _Property_NeatooPropertyChanged;
            p.Value.PropertyChanged += _Property_PropertyChanged;
        }
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




