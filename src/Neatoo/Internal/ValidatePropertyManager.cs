using Neatoo.Rules;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
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

    /// <summary>
    /// Checks if a property with the given name exists in PropertyBag (not just PropertyInfoList).
    /// Used by LazyLoad registration to detect reassignment of LazyLoad properties.
    /// </summary>
    bool TryGetRegisteredProperty(string propertyName, out IValidateProperty? property);

    /// <summary>
    /// Discovers all LazyLoad properties on the owner via cached reflection and registers them
    /// with PropertyManager as look-through property subclasses.
    /// </summary>
    /// <param name="owner">The entity instance (needed to read property values via reflection).</param>
    /// <param name="concreteType">The concrete runtime type of the entity (from GetType()).</param>
    void RegisterLazyLoadProperties(object owner, Type concreteType);

    /// <summary>
    /// Registers a single LazyLoad property with PropertyManager explicitly (no reflection discovery).
    /// </summary>
    /// <typeparam name="TInner">The inner type of the LazyLoad wrapper.</typeparam>
    /// <param name="owner">The entity instance (needed for GetType().GetProperty()).</param>
    /// <param name="name">The property name (must match the C# property name).</param>
    /// <param name="lazyLoad">The LazyLoad instance to register.</param>
    void RegisterLazyLoadProperty<TInner>(object owner, string name, LazyLoad<TInner> lazyLoad) where TInner : class?;
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

    protected void RaisePropertyChanged(string propertyName)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

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

    [UnconditionalSuppressMessage("Trimming", "IL2075",
        Justification = "GetType() returns ValidatePropertyManager<P> or EntityPropertyManager, " +
        "both are framework types whose methods are always preserved.")]
    [UnconditionalSuppressMessage("Trimming", "IL2060",
        Justification = "MakeGenericMethod creates CreateProperty<T> where T is a property type " +
        "discovered from PropertyInfoList. The properties are preserved by [DynamicallyAccessedMembers] " +
        "on the owning type's type parameter.")]
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

    /// <summary>
    /// Registers a property with the property manager.
    /// </summary>
    /// <param name="property">The property to register.</param>
    /// <remarks>
    /// Called by generated code during InitializePropertyBackingFields to register
    /// properties created via IPropertyFactory.
    /// If the property is already registered, this method does nothing (idempotent).
    /// </remarks>
    public virtual void Register(IValidateProperty property)
    {
        ArgumentNullException.ThrowIfNull(property, nameof(property));

        lock (this._propertyBagLock)
        {
            // Skip if already registered (idempotent - allows sharing services between instances)
            if (this.PropertyBag.ContainsKey(property.Name))
            {
                return;
            }

            // Subscribe to property events
            property.NeatooPropertyChanged += this._Property_NeatooPropertyChanged;
            property.PropertyChanged += this._Property_PropertyChanged;

            // Add to property bag
            this.PropertyBag[property.Name] = (P)property;
        }
    }

    /// <summary>
    /// Checks if a property with the given name exists in PropertyBag (not just PropertyInfoList).
    /// Used by LazyLoad registration to detect reassignment of LazyLoad properties.
    /// </summary>
    bool IValidatePropertyManagerInternal<P>.TryGetRegisteredProperty(string propertyName, out IValidateProperty? property)
    {
        lock (this._propertyBagLock)
        {
            if (this.PropertyBag.TryGetValue(propertyName, out var typedProperty))
            {
                property = typedProperty;
                return true;
            }
            property = null;
            return false;
        }
    }

    public virtual void OnDeserialized()
    {
        foreach (var p in this.PropertyBag)
        {
            p.Value.NeatooPropertyChanged += this._Property_NeatooPropertyChanged;
            p.Value.PropertyChanged += this._Property_PropertyChanged;
        }
        RecalculateValidity();
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

    /// <summary>
    /// Recalculates the cached IsValid and IsSelfValid values from current property state.
    /// Called after explicit RunRules() to ensure caches are accurate regardless of paused state.
    /// Does not raise PropertyChanged events (caller is responsible for meta-state notifications).
    /// </summary>
    public void RecalculateValidity()
    {
        this.IsValid = !this.PropertyBag.Any(p => !p.Value.IsValid);
        this.IsSelfValid = !this.PropertyBag.Any(p => !p.Value.IsSelfValid);
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

            // Recalculate cached validity from current property state.
            // Events received while paused were dropped, so caches may be stale.
            var wasValid = this.IsValid;
            var wasSelfValid = this.IsSelfValid;

            RecalculateValidity();

            if (wasValid != this.IsValid)
            {
                RaisePropertyChanged(nameof(IsValid));
            }
            if (wasSelfValid != this.IsSelfValid)
            {
                RaisePropertyChanged(nameof(IsSelfValid));
            }

            this.IsBusy = this.PropertyBag.Any(p => p.Value.IsBusy);
        }
    }

    #region LazyLoad Registration

    /// <summary>
    /// Static cache of LazyLoad properties per concrete type.
    /// Same reflection pattern used by NeatooBaseJsonTypeConverter for serialization.
    /// Reflection moves from N calls per object lifetime (every IsBusy/IsValid check in old approach)
    /// to 1 call per object lifetime (at registration). Full elimination requires generator changes.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _lazyLoadPropertyCache = new();

    [UnconditionalSuppressMessage("Trimming", "IL2070",
        Justification = "Callers pass GetType() which returns a concrete type whose properties are preserved " +
        "by [DynamicallyAccessedMembers] on the T type parameter of ValidateBase<T>. " +
        "The trimmer cannot statically verify this because GetType() returns Type without annotations, " +
        "but at runtime the concrete type is always T (or a subclass) which has its properties preserved.")]
    private protected static PropertyInfo[] GetLazyLoadProperties(Type concreteType)
    {
        return _lazyLoadPropertyCache.GetOrAdd(concreteType, type =>
            type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(p => p.PropertyType.IsGenericType
                    && p.PropertyType.GetGenericTypeDefinition() == typeof(LazyLoad<>)
                    && p.GetMethod != null)
                .ToArray());
    }

    /// <summary>
    /// Creates a LazyLoad property wrapper for the given inner type.
    /// Override in EntityPropertyManager to create LazyLoadEntityProperty instead.
    /// </summary>
    /// <param name="innerType">The inner type of the LazyLoad wrapper (the T in LazyLoad&lt;T&gt;).</param>
    /// <param name="propertyInfo">The property metadata.</param>
    /// <returns>A new LazyLoad property instance.</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2055",
        Justification = "MakeGenericType creates LazyLoadValidateProperty<T>/LazyLoadEntityProperty<T> " +
        "with inner types from LazyLoad<T> properties. These inner types are preserved by " +
        "[DynamicallyAccessedMembers] on the owning entity's type parameter.")]
    protected virtual IValidateProperty CreateLazyLoadProperty(Type innerType, IPropertyInfo propertyInfo)
    {
        var validatePropertyType = typeof(LazyLoadValidateProperty<>).MakeGenericType(innerType);
        return (IValidateProperty)Activator.CreateInstance(validatePropertyType, propertyInfo)!;
    }

    /// <summary>
    /// Discovers all LazyLoad properties on the owner via cached reflection and registers them
    /// with PropertyManager as look-through property subclasses.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2070",
        Justification = "GetGenericArguments() on LazyLoad<T> property types discovered via cached " +
        "reflection. The T type argument is a domain type preserved by [DynamicallyAccessedMembers] " +
        "on the owning entity's type parameter.")]
    void IValidatePropertyManagerInternal<P>.RegisterLazyLoadProperties(object owner, Type concreteType)
    {
        var props = GetLazyLoadProperties(concreteType);
        if (props.Length == 0) return;

        foreach (var prop in props)
        {
            var lazyLoadValue = prop.GetValue(owner);
            if (lazyLoadValue == null) continue;

            // If already registered as a LazyLoad property, reload the value
            // (handles reassignment of the LazyLoad instance in custom setters)
            lock (this._propertyBagLock)
            {
                if (this.PropertyBag.TryGetValue(prop.Name, out var existingTyped)
                    && existingTyped is ILazyLoadProperty)
                {
                    ((IValidateProperty)existingTyped).LoadValue(lazyLoadValue);
                    continue;
                }
            }

            var innerType = prop.PropertyType.GetGenericArguments()[0];
            var propertyInfoWrapper = new PropertyInfoWrapper(prop);

            var lazyLoadProperty = CreateLazyLoadProperty(innerType, propertyInfoWrapper);

            // Load the current LazyLoad value into the property (this connects subscriptions
            // and inner child without triggering rules -- LoadValue fires ChangeReason.Load)
            lazyLoadProperty.LoadValue(lazyLoadValue);

            // Register with PropertyManager (subscribes to NeatooPropertyChanged/PropertyChanged events)
            this.Register(lazyLoadProperty);
        }
    }

    /// <summary>
    /// Registers a single LazyLoad property with PropertyManager explicitly (no reflection discovery).
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2075",
        Justification = "GetType().GetProperty() on a concrete type whose properties are preserved " +
        "by [DynamicallyAccessedMembers] on the T type parameter of ValidateBase<T>.")]
    void IValidatePropertyManagerInternal<P>.RegisterLazyLoadProperty<TInner>(object owner, string name, LazyLoad<TInner> lazyLoad)
    {
        ArgumentNullException.ThrowIfNull(lazyLoad, nameof(lazyLoad));

        // If already registered as a LazyLoad property, reload the value
        // (handles reassignment of the LazyLoad instance in custom setters)
        lock (this._propertyBagLock)
        {
            if (this.PropertyBag.TryGetValue(name, out var existingTyped)
                && existingTyped is ILazyLoadProperty)
            {
                ((IValidateProperty)existingTyped).LoadValue(lazyLoad);
                return;
            }
        }

        var reflectionProp = owner.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (reflectionProp == null)
        {
            throw new PropertyMissingException($"Property '{name}' not found on type '{owner.GetType().Name}'");
        }
        var propertyInfoWrapper = new PropertyInfoWrapper(reflectionProp);

        var lazyLoadProperty = CreateLazyLoadProperty(typeof(TInner), propertyInfoWrapper);

        lazyLoadProperty.LoadValue(lazyLoad);
        this.Register(lazyLoadProperty);
    }

    #endregion
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
