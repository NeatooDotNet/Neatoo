using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Neatoo.Internal;

public class Property<T> : IProperty<T>, IProperty, INotifyPropertyChanged, IJsonOnDeserialized
{
    protected T? _value = default;
    private readonly object _isMarkedBusyLock = new object();
    private readonly List<long> _isMarkedBusy = new List<long>();

    public Property(IPropertyInfo propertyInfo)
    {
        this.Name = propertyInfo.Name;
        this.IsReadOnly = propertyInfo.IsPrivateSetter;
    }

    [JsonConstructor]
    public Property(string name, T value, bool isReadOnly)
    {
        this.Name = name;
        this._value = value;
        this.IsReadOnly = isReadOnly;
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

    object? IProperty.Value { get => this.Value; set => this.SetValue(value); }

    [JsonIgnore]
    public Type Type => typeof(T);

    [JsonIgnore]
    public Task Task { get; protected set; } = Task.CompletedTask;

    protected IBase? ValueAsBase => this.Value as IBase;

    public bool IsBusy
    {
        get
        {
            lock (this._isMarkedBusyLock)
            {
                return this.ValueAsBase?.IsBusy ?? false || this.IsSelfBusy || this._isMarkedBusy.Count > 0;
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
        this.SetPrivateValue((T?)value, true);
        this._value = (T?)value;
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


                if (this._value is IBase _valueBase)
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

                if (value is IBase valueBase)
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
            this.IsSelfBusy = true;

            try
            {
                this.OnPropertyChanged(nameof(IsBusy));

                await task;

                this.IsSelfBusy = false;
                this.OnPropertyChanged(nameof(IsBusy));
            }
            finally
            {
                this.IsSelfBusy = false;
            }
        }

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
}
