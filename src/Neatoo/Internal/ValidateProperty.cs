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

    public virtual T? Value
    {
        get => this._value;
        set
        {
            this.SetValue(value);
        }
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
                    || this._isMarkedBusy.Count > 0;
            }
        }
    }

    public async Task WaitForTasks()
    {
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

        // Fire NeatooPropertyChanged with ChangeReason.Load - SetParent will be called but rules will be skipped
        // Note: We intentionally do NOT fire PropertyChanged here to avoid triggering UI updates during load
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
