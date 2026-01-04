using Neatoo.Rules;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Neatoo.Internal;

public delegate IValidatePropertyManager<IValidateProperty> CreateValidatePropertyManager(IPropertyInfoList propertyInfoList);

/// <summary>
/// Internal interface for framework coordination within <see cref="IValidatePropertyManager{P}"/> implementations.
/// </summary>
/// <typeparam name="P">The type of property managed.</typeparam>
/// <remarks>
/// This interface exposes members used only by framework code (e.g., serialization, deserialization).
/// External consumers should not implement or depend on this interface.
/// </remarks>
internal interface IValidatePropertyManagerInternal<out P> where P : IValidateProperty
{
    /// <summary>
    /// Gets metadata about all registered properties.
    /// Used during deserialization and rule setup.
    /// </summary>
    IPropertyInfoList PropertyInfoList { get; }

    /// <summary>
    /// Gets all instantiated properties.
    /// Used during serialization and deserialization.
    /// </summary>
    IEnumerable<P> GetProperties { get; }
}

public class ValidatePropertyManager<P> : IValidatePropertyManager<P>, IValidatePropertyManagerInternal<P>, IJsonOnDeserialized
    where P : IValidateProperty
{
    protected IFactory Factory { get; }

    protected readonly IPropertyInfoList PropertyInfoList;

    IPropertyInfoList IValidatePropertyManagerInternal<P>.PropertyInfoList => this.PropertyInfoList;

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

    public ValidatePropertyManager(IPropertyInfoList propertyInfoList, IFactory factory)
    {
        this.PropertyInfoList = propertyInfoList;
        this.Factory = factory;
    }

    protected IValidateProperty CreateProperty<PV>(IPropertyInfo propertyInfo)
    {
        return this.Factory.CreateValidateProperty<PV>(propertyInfo);
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
        if (this.IsPaused)
        {
            return;
        }

        if (e.PropertyName == nameof(IValidateProperty.IsBusy))
        {
            this.IsBusy = this.PropertyBag.Any(p => p.Value.IsBusy);
        }

        if (sender is IValidateProperty property)
        {
            var raiseIsValid = this.IsValid;

            if (e.PropertyName == nameof(IValidateProperty.IsValid)
                    || e.PropertyName == nameof(IValidateProperty.Value))
            {
                this.IsValid = !this.PropertyBag.Any(p => !p.Value.IsValid);
            }

            if (raiseIsValid != this.IsValid)
            {
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsValid)));
            }

            var raiseIsSelfValid = this.IsSelfValid;

            if (e.PropertyName == nameof(IValidateProperty.IsSelfValid)
                    || e.PropertyName == nameof(IValidateProperty.Value))
            {
                this.IsSelfValid = !this.PropertyBag.Any(p => !p.Value.IsSelfValid);
            }

            if (raiseIsSelfValid != this.IsSelfValid)
            {
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelfValid)));
            }
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

    void IValidatePropertyManager<P>.SetProperties(IEnumerable<IValidateProperty> properties)
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
        this.IsValid = !this.PropertyBag.Any(p => !p.Value.IsValid);
        this.IsSelfValid = !this.PropertyBag.Any(p => !p.Value.IsSelfValid);
    }

    void IJsonOnDeserialized.OnDeserialized()
    {
        this.OnDeserialized();
    }

    IEnumerable<P> IValidatePropertyManagerInternal<P>.GetProperties => this.PropertyBag.Select(p => p.Value);

    // Validation-specific members

    [JsonIgnore]
    public bool IsSelfValid { get; protected set; } = true;
    [JsonIgnore]
    public bool IsValid { get; protected set; } = true;
    [JsonIgnore]
    public bool IsPaused { get; protected set; }

    public IReadOnlyCollection<IPropertyMessage> PropertyMessages => this.PropertyBag.SelectMany(_ => _.Value.PropertyMessages).ToList().AsReadOnly();

    public async Task RunRules(RunRulesFlag runRules = Neatoo.RunRulesFlag.All, CancellationToken? token = null)
    {
        foreach (var p in this.PropertyBag)
        {
            await p.Value.RunRules(runRules, token);
        }
    }

    public void ClearSelfMessages()
    {
        foreach (var p in this.PropertyBag)
        {
            // Cast to internal interface to call ClearSelfMessages
            if (p.Value is IValidatePropertyInternal vpInternal)
            {
                vpInternal.ClearSelfMessages();
            }
        }
    }

    public void ClearAllMessages()
    {
        foreach (var p in this.PropertyBag)
        {
            // Cast to internal interface to call ClearAllMessages
            if (p.Value is IValidatePropertyInternal vpInternal)
            {
                vpInternal.ClearAllMessages();
            }
        }
    }

    public virtual void PauseAllActions()
    {
        if (!this.IsPaused)
        {
            this.IsPaused = true;
        }
    }

    public virtual void ResumeAllActions()
    {
        if (this.IsPaused)
        {
            this.IsPaused = false;
        }
    }
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

/// <summary>
/// Exception thrown when a child object's data type is incompatible in a validation context.
/// </summary>
[Serializable]
public class PropertyValidateChildDataWrongTypeException : PropertyException
{
    public PropertyValidateChildDataWrongTypeException() { }
    public PropertyValidateChildDataWrongTypeException(string message) : base(message) { }
    public PropertyValidateChildDataWrongTypeException(string message, Exception inner) : base(message, inner) { }
}
