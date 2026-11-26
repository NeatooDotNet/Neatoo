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
    /// Called with each write one of the manager's properties starts, while it is in flight.
    /// The object that owns the manager sets it, and registers the task in its running tasks.
    /// </summary>
    Action<Task>? TaskStarted { get; set; }
}

public class ValidatePropertyManager<P> : IValidatePropertyManager<P>, IValidatePropertyManagerInternal<P>, IJsonOnDeserialized
    where P : IValidateProperty
{
    protected IFactory Factory { get; }

    protected readonly IPropertyInfoList PropertyInfoList;

    IPropertyInfoList IValidatePropertyManagerInternal<P>.PropertyInfoList => this.PropertyInfoList;

    /// <summary>
    /// True while one of the properties is busy. Read from the properties, each of which reads
    /// its own tasks; nothing is cached, so nothing can go stale.
    /// </summary>
    public bool IsBusy => this.PropertyBag.Values.Any(property => property.IsBusy);
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

    /// <summary>
    /// Completes when no property has work in flight: a write, an async rule, a load, or a busy
    /// child object.
    /// </summary>
    /// <remarks>
    /// DESIGN DECISION: await the waits of the properties that are busy, and go round again only
    /// while one of those waits had something left to await, because finishing work can start
    /// more. A wait that has already faulted is awaited too, so the fault comes through.
    /// DID NOT DO THIS: loop while a property reports IsBusy. A busy flag gives nothing to await,
    /// so the loop never suspended, and on one thread nothing could ever clear the flag (#141).
    /// DID NOT DO THIS: ask every property for its wait. Asking an idle child object for its wait
    /// takes the fault its tracker holds for its own first wait.
    /// </remarks>
    public async Task WaitForTasks()
    {
        while (true)
        {
            var waits = this.PropertyBag.Values
                .Where(property => property.IsBusy)
                .Select(property => property.WaitForTasks())
                .ToList();

            if (waits.All(wait => wait.IsCompletedSuccessfully))
            {
                return;
            }

            await Task.WhenAll(waits);
        }
    }

    /// <summary>
    /// The owner's end of the report path. The object that owns this manager sets it where it
    /// subscribes to the manager, and each property calls it with a write it has started.
    /// </summary>
    public Action<Task>? TaskStarted { get; set; }

    private void ReportTaskStarted(Task task)
    {
        this.TaskStarted?.Invoke(task);
    }

    /// <summary>
    /// What a property gets when it enters the bag: this manager's subscription, the task
    /// tracker it holds its work in flight in, and the way to report a write to its owner.
    /// </summary>
    private void Attach(P property)
    {
        property.NeatooPropertyChanged += this._Property_NeatooPropertyChanged;
        property.PropertyChanged += this._Property_PropertyChanged;

        if (property is IValidatePropertyInternal propertyInternal)
        {
            propertyInternal.UseTasks(this.Factory.CreateAsyncTasks(), this.ReportTaskStarted);
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
        return this.NeatooPropertyChanged.InvokeAll(eventArgs);
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

            this.Attach(newProperty);

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

    /// <summary>
    /// Replaces properties with the ones the serializer built, moving this manager's subscription
    /// from each outgoing property to the one that takes its place.
    /// </summary>
    /// <remarks>
    /// A subscription is made where a property enters the bag: here, in <see cref="Register"/> and
    /// in <see cref="GetProperty"/>. <see cref="OnDeserialized"/> subscribes to nothing, because a
    /// property the serializer did not replace (a lazy-load one) is already subscribed.
    /// </remarks>
    void IValidatePropertyManager<P>.SetProperties(IEnumerable<IValidateProperty> properties)
    {
        lock (this._propertyBagLock)
        {
            foreach (var p in properties.Cast<P>())
            {
                if (this.PropertyBag.TryGetValue(p.Name, out var outgoing))
                {
                    if (ReferenceEquals(outgoing, p))
                    {
                        continue;
                    }

                    outgoing.NeatooPropertyChanged -= this._Property_NeatooPropertyChanged;
                    outgoing.PropertyChanged -= this._Property_PropertyChanged;
                }

                this.Attach(p);

                this.PropertyBag[p.Name] = p;
            }
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

            this.Attach((P)property);

            // Add to property bag
            this.PropertyBag[property.Name] = (P)property;
        }
    }

    /// <summary>
    /// Recalculates validity from the deserialized properties. The subscriptions were made when
    /// each property entered the bag, so none is made here.
    /// </summary>
    public virtual void OnDeserialized()
    {
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
