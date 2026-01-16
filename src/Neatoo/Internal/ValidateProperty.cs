using Neatoo.Rules;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Neatoo.Internal;

/// <summary>
/// Represents a managed property that supports validation and business rules.
/// </summary>
/// <typeparam name="T">The type of the property value.</typeparam>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2119:SealMethodsThatSatisfyPrivateInterfaces", Justification = "Class intentionally non-sealed for inheritance by EntityProperty")]
public class ValidateProperty<T> : IValidateProperty<T>, IValidatePropertyInternal, INotifyPropertyChanged, IJsonOnDeserialized
{
    protected T? _value = default;
    private readonly object _isMarkedBusyLock = new object();
    private readonly List<long> _isMarkedBusy = new List<long>();

    // Lazy loading fields
    private readonly object _lazyLoadLock = new object();
    private Func<Task<T?>>? _onLoad;
    private Task<T?>? _loadTask;
    private bool _isLoaded = true; // Default true for non-lazy properties
    private const uint LazyLoadRuleId = 0xFFFFFFFF; // Reserved rule ID for lazy load failures

    public ValidateProperty(IPropertyInfo propertyInfo)
    {
        this.Name = propertyInfo.Name;
        this.IsReadOnly = propertyInfo.IsPrivateSetter;
    }

    [JsonConstructor]
    public ValidateProperty(string name, T value, IRuleMessage[] serializedRuleMessages, bool isReadOnly)
    {
        this.Name = name;
        this._value = value;
        this.IsReadOnly = isReadOnly;
        this.RuleMessages = serializedRuleMessages.ToList();
    }

    public string Name { get; }

    /// <summary>
    /// Gets or sets the lazy load handler for this property.
    /// </summary>
    [JsonIgnore]
    public Func<Task<T?>>? OnLoad
    {
        get => this._onLoad;
        set
        {
            this._onLoad = value;
            // When OnLoad is configured, mark as not loaded until first access
            if (value != null && this._value == null)
            {
                this._isLoaded = false;
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the property value has been loaded.
    /// </summary>
    [JsonIgnore]
    public bool IsLoaded => this._isLoaded;

    /// <summary>
    /// Gets the task representing a pending lazy load operation.
    /// </summary>
    [JsonIgnore]
    public Task? LoadTask => this._loadTask;

    public virtual T? Value
    {
        get
        {
            // Fire-and-forget lazy loading: trigger load if not loaded and OnLoad is configured
            // Use lock to prevent multiple concurrent load triggers
            if (!this._isLoaded && this._onLoad != null && this._loadTask == null)
            {
                lock (this._lazyLoadLock)
                {
                    // Double-check inside lock
                    if (!this._isLoaded && this._onLoad != null && this._loadTask == null)
                    {
                        _ = this.TriggerLazyLoadAsync();
                    }
                }
            }
            return this._value;
        }
        set
        {
            this.SetValue(value);
        }
    }

    /// <summary>
    /// Triggers lazy loading asynchronously (fire-and-forget from getter).
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Lazy loading can fail for various reasons (network, database, etc.). All failures are captured as broken rules.")]
    private async Task TriggerLazyLoadAsync()
    {
        if (this._onLoad == null || this._loadTask != null)
        {
            return;
        }

        try
        {
            // Clear any previous load failure messages
            this.ClearLazyLoadError();

            this._loadTask = this._onLoad();
            this.OnPropertyChanged(nameof(LoadTask));
            this.OnPropertyChanged(nameof(IsBusy));

            var loadedValue = await this._loadTask;

            // Set the value (this will trigger PropertyChanged and rules)
            this._isLoaded = true;
            this.OnPropertyChanged(nameof(IsLoaded));

            // Use SetPrivateValue to properly handle value assignment
            await this.SetPrivateValue(loadedValue);
        }
        catch (Exception ex)
        {
            // Load failed - add a broken rule
            this._isLoaded = true; // Mark as "loaded" (attempted) to prevent retry loops
            this.OnPropertyChanged(nameof(IsLoaded));
            this.SetLazyLoadError($"Failed to load: {ex.Message}");
        }
        finally
        {
            this._loadTask = null;
            this.OnPropertyChanged(nameof(LoadTask));
            this.OnPropertyChanged(nameof(IsBusy));
        }
    }

    /// <summary>
    /// Explicitly triggers lazy loading and returns the loaded value.
    /// </summary>
    public async Task<T?> LoadAsync()
    {
        if (this._isLoaded)
        {
            return this._value;
        }

        if (this._onLoad == null)
        {
            this._isLoaded = true;
            return this._value;
        }

        // If load is already in progress, await it
        if (this._loadTask != null)
        {
            return await this._loadTask;
        }

        await this.TriggerLazyLoadAsync();
        return this._value;
    }

    /// <summary>
    /// Explicit interface implementation for non-generic LoadAsync.
    /// </summary>
    Task IValidateProperty.LoadAsync() => this.LoadAsync();

    private void SetLazyLoadError(string message)
    {
        var ruleMessage = new RuleMessage(this.Name, message) { RuleId = LazyLoadRuleId };
        this.SetMessagesForRule(new[] { ruleMessage });
    }

    private void ClearLazyLoadError()
    {
        this.RuleMessages.RemoveAll(rm => rm.RuleId == LazyLoadRuleId);
    }

    object? IValidateProperty.Value { get => this.Value; set => this.SetValue(value); }

    [JsonIgnore]
    public Type Type => typeof(T);

    [JsonIgnore]
    public Task Task { get; protected set; } = Task.CompletedTask;

    protected IValidateBase? ValueAsBase => this.Value as IValidateBase;

    [JsonIgnore]
    public virtual IValidateMetaProperties? ValueIsValidateBase => this.Value as IValidateMetaProperties;

    public bool IsBusy
    {
        get
        {
            lock (this._isMarkedBusyLock)
            {
                return this.ValueAsBase?.IsBusy ?? false
                    || this.IsSelfBusy
                    || this._isMarkedBusy.Count > 0
                    || this._loadTask != null; // Include pending lazy load
            }
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Load errors are captured as broken rules; we just need to wait for completion here.")]
    public async Task WaitForTasks()
    {
        // Wait for any pending lazy load
        if (this._loadTask != null)
        {
            try
            {
                await this._loadTask;
            }
            catch
            {
                // Load errors are captured as broken rules, don't throw here
            }
        }

        await (this.ValueAsBase?.WaitForTasks() ?? Task.CompletedTask);
    }

    [JsonIgnore]
    public bool IsSelfBusy { get; private set; } = false;

    /// <summary>
    /// Gets a thread-safe snapshot of the busy operation identifiers.
    /// </summary>
    /// <remarks>
    /// Returns a copy of the internal list to ensure thread safety. Each access
    /// returns a new snapshot that is safe to enumerate without risk of
    /// <see cref="InvalidOperationException"/> from concurrent modifications.
    /// </remarks>
    [JsonIgnore]
    public IReadOnlyList<long> IsMarkedBusy
    {
        get
        {
            lock (this._isMarkedBusyLock)
            {
                return this._isMarkedBusy.ToList().AsReadOnly();
            }
        }
    }

    public void AddMarkedBusy(long id)
    {
        lock (this._isMarkedBusyLock)
        {
            if (!this._isMarkedBusy.Contains(id))
            {
                this._isMarkedBusy.Add(id);
            }
        }
        this.OnPropertyChanged(nameof(IsMarkedBusy));
        this.OnPropertyChanged(nameof(IsBusy));
    }

    public void RemoveMarkedBusy(long id)
    {
        lock (this._isMarkedBusyLock)
        {
            this._isMarkedBusy.Remove(id);
        }
        this.OnPropertyChanged(nameof(IsMarkedBusy));
        this.OnPropertyChanged(nameof(IsBusy));
    }

    public bool IsReadOnly { get; protected set; } = false;

    public virtual Task SetValue(object? newValue)
    {
        if (this.IsReadOnly)
        {
            throw new PropertyReadOnlyException();
        }

        return this.SetPrivateValue(newValue);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2119:SealMethodsThatSatisfyPrivateInterfaces", Justification = "Method intentionally virtual for derived class overrides")]
    public virtual Task SetPrivateValue(object? newValue, bool quietly = false)
    {
        if (newValue == null && this._value == null) { return Task.CompletedTask; }

        this.Task = Task.CompletedTask;

        if (newValue == null)
        {
            this.HandleNullValue(quietly);
        }
        else if (newValue is T value)
        {
            this.HandleNonNullValue(value, quietly);
        }
        else
        {
            throw new PropertyTypeMismatchException($"Type {newValue.GetType()} is not type {typeof(T).FullName}");
        }

        if (this.Task.Exception != null)
        {
            throw this.Task.Exception;
        }

        return this.Task;
    }

    public virtual void LoadValue(object? value)
    {
        if (value == null && this._value == null) { return; }

        // Handle old value cleanup (unsubscribe events, clear parent)
        if (this._value != null && !ReferenceEquals(this._value, value))
        {
            if (this._value is INotifyNeatooPropertyChanged neatooPropertyChanged)
            {
                neatooPropertyChanged.NeatooPropertyChanged -= this.PassThruValueNeatooPropertyChanged;
            }
            if (this._value is INotifyPropertyChanged notifyPropertyChanged)
            {
                notifyPropertyChanged.PropertyChanged -= this.PassThruValuePropertyChanged;
            }
            if (this._value is ISetParent oldSetParent)
            {
                oldSetParent.SetParent(null);
            }
        }

        this._value = (T?)value;

        // Handle new value setup (subscribe events)
        if (value != null)
        {
            if (value is INotifyNeatooPropertyChanged valueNeatooPropertyChanged)
            {
                valueNeatooPropertyChanged.NeatooPropertyChanged += this.PassThruValueNeatooPropertyChanged;
            }
            if (value is INotifyPropertyChanged valueNotifyPropertyChanged)
            {
                valueNotifyPropertyChanged.PropertyChanged += this.PassThruValuePropertyChanged;
            }
        }

        // Fire event with ChangeReason.Load - SetParent will be called but rules will be skipped
        this.OnPropertyChanged(nameof(Value));
        this.Task = this.OnValueNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(this, ChangeReason.Load));
    }

    protected virtual void HandleNullValue(bool quietly = false)
    {
        if (this._value is INotifyNeatooPropertyChanged neatooPropertyChanged)
        {
            neatooPropertyChanged.NeatooPropertyChanged -= this.PassThruValueNeatooPropertyChanged;
        }

        this._value = default;

        if (!quietly)
        {
            this.OnPropertyChanged(nameof(Value));
            this.Task = this.OnValueNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(this));
        }
    }

    protected virtual void HandleNonNullValue(T value, bool quietly = false)
    {
        var isDiff = !this.AreSame(this._value, value);

        if (isDiff)
        {
            if (this._value != null)
            {
                if (this._value is INotifyNeatooPropertyChanged neatooPropertyChanged)
                {
                    neatooPropertyChanged.NeatooPropertyChanged -= this.PassThruValueNeatooPropertyChanged;
                }
                if (this._value is INotifyPropertyChanged notifyPropertyChanged)
                {
                    notifyPropertyChanged.PropertyChanged -= this.PassThruValuePropertyChanged;
                }


                if (this._value is IValidateBase _valueBase)
                {
                    if (_valueBase.IsBusy)
                    {
                        throw new ChildObjectBusyException(isAddOperation: false);
                    }
                }

                if (this._value is ISetParent _valueSetParent)
                {
                    _valueSetParent.SetParent(null);
                }
            }

            if (value != null)
            {
                if (value is INotifyNeatooPropertyChanged valueNeatooPropertyChanged)
                {
                    valueNeatooPropertyChanged.NeatooPropertyChanged += this.PassThruValueNeatooPropertyChanged;
                }
                if (value is INotifyPropertyChanged notifyPropertyChanged)
                {
                    notifyPropertyChanged.PropertyChanged += this.PassThruValuePropertyChanged;
                }

                if (value is IValidateBase valueBase)
                {
                    if (valueBase.IsBusy)
                    {
                        throw new ChildObjectBusyException(isAddOperation: true);
                    }
                }
            }
        }

        this._value = value;

        if (isDiff && !quietly)
        {
            this.OnPropertyChanged(nameof(Value));

            this.Task = this.OnValueNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(this));
        }
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected virtual Task PassThruValueNeatooPropertyChanged(NeatooPropertyChangedEventArgs eventArgs)
    {
        return this.NeatooPropertyChanged?.Invoke(new NeatooPropertyChangedEventArgs(this, this.Value!, eventArgs)) ?? Task.CompletedTask;
    }

    protected virtual void PassThruValuePropertyChanged(object? source, PropertyChangedEventArgs eventArgs)
    {
        this.PropertyChanged?.Invoke(this, eventArgs);
    }

    protected virtual async Task OnValueNeatooPropertyChanged(NeatooPropertyChangedEventArgs eventArgs)
    {
        // ValidateBase sticks Task into AsyncTaskSequencer for us
        // so that it will be awaited by WaitForTasks()
        var task = this.NeatooPropertyChanged?.Invoke(eventArgs) ?? Task.CompletedTask;

        if (!task.IsCompleted && !this.IsSelfBusy)
        {
            // Only track IsBusy if not already busy (prevents duplicate notifications)
            this.IsSelfBusy = true;

            try
            {
                this.OnPropertyChanged(nameof(IsBusy));

                await task;

                // Must set false BEFORE notification so listeners see correct IsBusy value
                this.IsSelfBusy = false;
                this.OnPropertyChanged(nameof(IsBusy));
            }
            finally
            {
                // Failsafe: ensure IsSelfBusy is reset even if exception thrown
                this.IsSelfBusy = false;
            }
        }

        // Always await - handles case when IsSelfBusy was already true (skipped IsBusy tracking)
        await task;
    }

    protected virtual bool AreSame<P>(P? oldValue, P? newValue)
    {
        if (oldValue == null && newValue == null)
        {
            return true;
        }
        else if (oldValue == null || newValue == null)
        {
            return false;
        }

        if (!typeof(P).IsValueType)
        {
            return (ReferenceEquals(oldValue, newValue));
        }
        else
        {
            return oldValue.Equals(newValue);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event NeatooPropertyChanged? NeatooPropertyChanged;

    public void OnDeserialized()
    {
        if (this.Value is INotifyNeatooPropertyChanged neatooPropertyChanged)
        {
            neatooPropertyChanged.NeatooPropertyChanged += this.PassThruValueNeatooPropertyChanged;
        }
        if (this.Value is INotifyPropertyChanged notifyPropertyChanged)
        {
            notifyPropertyChanged.PropertyChanged += this.PassThruValuePropertyChanged;
        }
    }

    // Validation-specific members

    public bool IsSelfValid => this.ValueIsValidateBase != null ? true : this.RuleMessages.Count == 0;
    public bool IsValid => this.ValueIsValidateBase != null ? this.ValueIsValidateBase.IsValid : this.RuleMessages.Count == 0;

    public Task RunRules(RunRulesFlag runRules = Neatoo.RunRulesFlag.All, CancellationToken? token = null) { return this.ValueIsValidateBase?.RunRules(runRules, token) ?? Task.CompletedTask; }

    [JsonIgnore]
    public IReadOnlyCollection<IPropertyMessage> PropertyMessages =>
                            this.ValueIsValidateBase != null ? this.ValueIsValidateBase.PropertyMessages :
                                                                this.RuleMessages.Select(rm => new PropertyMessage(this, rm.Message)).ToList().AsReadOnly();

    [JsonIgnore]
    public List<IRuleMessage> RuleMessages { get; set; } = new List<IRuleMessage>();

    public IRuleMessage[] SerializedRuleMessages => this.RuleMessages.ToArray();

    [JsonIgnore]
    private object RuleMessagesLock { get; } = new object();

    protected void SetMessagesForRule(IReadOnlyList<IRuleMessage> ruleMessages)
    {
        Debug.Assert(this.ValueIsValidateBase == null, "If the Child is IValidateBase then it should be handling the errors");
        lock (this.RuleMessagesLock)
        {
            this.RuleMessages.RemoveAll(rm => ruleMessages.Any(rm2 => rm2.RuleId == rm.RuleId));
            this.RuleMessages.AddRange(ruleMessages.Where(rm => rm.Message != null));
        }
        this.OnPropertyChanged(nameof(IsValid));
        this.OnPropertyChanged(nameof(IsSelfValid));
        this.OnPropertyChanged(nameof(RuleMessages));
    }

    void IValidatePropertyInternal.SetMessagesForRule(IReadOnlyList<IRuleMessage> ruleMessages)
    {
        this.SetMessagesForRule(ruleMessages);
    }

    void IValidatePropertyInternal.ClearMessagesForRule(uint ruleId)
    {
        this.RuleMessages.RemoveAll(rm => rm.RuleId == ruleId);
        this.OnPropertyChanged(nameof(IsValid));
        this.OnPropertyChanged(nameof(IsSelfValid));
        this.OnPropertyChanged(nameof(RuleMessages));
    }

    void IValidatePropertyInternal.ClearSelfMessages()
    {
        this.RuleMessages.Clear();
        this.OnPropertyChanged(nameof(IsValid));
        this.OnPropertyChanged(nameof(IsSelfValid));
        this.OnPropertyChanged(nameof(RuleMessages));
    }

    void IValidatePropertyInternal.ClearAllMessages()
    {
        this.RuleMessages.Clear();
        this.ValueIsValidateBase?.ClearAllMessages();

        this.OnPropertyChanged(nameof(IsValid));
        this.OnPropertyChanged(nameof(IsSelfValid));
        this.OnPropertyChanged(nameof(RuleMessages));
    }
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
