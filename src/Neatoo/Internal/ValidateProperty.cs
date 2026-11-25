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

    // This property's in-flight work, as tasks: each write's task and each async rule run that
    // names the property. The property manager supplies it when the property enters its bag.
    private IAsyncTasks? _tasks;

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

    /// <summary>
    /// True while a child object held as the value is busy, or while this property has work in
    /// flight: a write to it, or an async rule that names it.
    /// </summary>
    /// <remarks>
    /// DESIGN DECISION: busy is read from the property's tasks. Two overlapping writes are two
    /// tasks, so the property stays busy until both finish, and a rule that throws is a finished
    /// task, so the property is not left busy.
    /// DID NOT DO THIS: a bool set around a write plus a list of ids the rule manager added and
    /// removed. The bool was cleared by the first of two writes to finish, and cleared without a
    /// notification when a rule threw, which left the entity reading busy for good.
    /// </remarks>
    public bool IsBusy => (this.ValueAsBase?.IsBusy ?? false) || this.HasTasksInFlight;

    /// <summary>
    /// Completes when a child object held as the value has finished its work and this property
    /// has none in flight. It never faults: a rule's exception surfaces from the entity's wait.
    /// </summary>
    public async Task WaitForTasks()
    {
        await (this.ValueAsBase?.WaitForTasks() ?? Task.CompletedTask);
        await this.WaitForOwnTasks();
    }

    /// <summary>
    /// True while this property itself has a write or an async rule run in flight.
    /// </summary>
    protected bool HasTasksInFlight => this._tasks?.IsRunning ?? false;

    /// <summary>
    /// Completes when this property's own work in flight has finished. Awaiting it suspends
    /// exactly while <see cref="HasTasksInFlight"/> is true.
    /// </summary>
    protected Task WaitForOwnTasks() => this._tasks?.AllDone ?? Task.CompletedTask;

    /// <summary>
    /// Gives the property its task tracker. Called by the property manager when the property
    /// enters its bag; a property keeps the first tracker it is given.
    /// </summary>
    /// <remarks>
    /// A property that is in no property manager has no tracker and is never busy on its own
    /// account: nothing would wait for it or read its busy state.
    /// </remarks>
    void IValidatePropertyInternal.UseTasks(IAsyncTasks tasks)
    {
        ArgumentNullException.ThrowIfNull(tasks, nameof(tasks));

        if (this._tasks != null)
        {
            return;
        }

        this._tasks = tasks;

        // The tracker calls this when the last task finishes, before it completes the waits on
        // it. So whatever listens for IsBusy has heard "idle" by the time a wait returns.
        tasks.OnFullSequenceComplete = () =>
        {
            this.OnPropertyChanged(nameof(IsBusy));
            return Task.CompletedTask;
        };
    }

    /// <summary>
    /// Tracks a task as work in flight on this property: the property is busy until it finishes,
    /// faulted or not.
    /// </summary>
    /// <returns>
    /// A task that completes when the property has accounted for <paramref name="task"/>: if it
    /// was the last in flight, the property is idle and has said so. It never faults.
    /// </returns>
    /// <remarks>
    /// DESIGN DECISION: whoever starts the work awaits this before its own task completes, so an
    /// awaited write or rule run leaves the property idle. The tracker does its accounting in a
    /// continuation; left unawaited, that continuation would still be running, on another thread,
    /// after the caller had moved on to its next write.
    /// </remarks>
    protected Task TrackTask(Task task)
    {
        ArgumentNullException.ThrowIfNull(task, nameof(task));

        var tasks = this._tasks;

        if (tasks == null || task.IsCompleted)
        {
            return Task.CompletedTask;
        }

        var wasRunning = tasks.IsRunning;

        // Completion only. The tracker would otherwise report the task's exception from the
        // property's wait as well as from the entity's.
        var accounted = tasks.AddTask(task.ContinueWith(
            static _ => { }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default));

        if (!wasRunning)
        {
            this.OnPropertyChanged(nameof(IsBusy));
        }

        return accounted;
    }

    Task IValidatePropertyInternal.TrackTask(Task task) => this.TrackTask(task);

    private bool _isReadOnly = false;

    public bool IsReadOnly
    {
        get => _isReadOnly;
        protected set
        {
            // One-and-done: once true, cannot be set back to false
            if (_isReadOnly && !value) return;
            _isReadOnly = value;
        }
    }

    public void MarkReadOnly()
    {
        if (!IsReadOnly)
        {
            IsReadOnly = true;
            OnPropertyChanged(nameof(IsReadOnly));
        }
    }

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
        var task = this.NeatooPropertyChanged?.Invoke(eventArgs) ?? Task.CompletedTask;

        // The task of the whole cascade this change started. While it runs, the write is in
        // flight and the property is busy.
        var accounted = this.TrackTask(task);

        try
        {
            await task;
        }
        finally
        {
            // The write is not over until the property has accounted for it.
            await accounted;
        }
    }

    protected virtual bool AreSame<P>(P? oldValue, P? newValue)
    {
        // Use value equality (via EqualityComparer<P>.Default) so reference types that override
        // Equals -- string, decimal-as-reference, any IEquatable<T> -- are compared by value.
        // For reference types without an Equals override (e.g. EntityBase/ValidateBase children),
        // EqualityComparer<T>.Default falls back to object.Equals -> reference equality, preserving
        // the prior behavior. The leading null guard mirrors the original method shape and avoids
        // any difference in how EqualityComparer handles two nulls vs. one-null comparisons.
        if (oldValue == null && newValue == null)
        {
            return true;
        }
        else if (oldValue == null || newValue == null)
        {
            return false;
        }

        return EqualityComparer<P>.Default.Equals(oldValue, newValue);
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
