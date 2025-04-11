using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Neatoo.Internal;




public class Property<T> : IProperty<T>, IProperty, INotifyPropertyChanged, IJsonOnDeserialized
{
    public string Name { get; }

    protected T? _value = default;

    public virtual T? Value
    {
        get => _value;
        set
        {
            SetValue(value);
        }
    }

    object? IProperty.Value { get => Value; set => SetValue(value); }

    [JsonIgnore]
    public Type Type => typeof(T);

    [JsonIgnore]
    public Task Task { get; protected set; } = Task.CompletedTask;

    protected IBase? ValueAsBase => Value as IBase;

    public bool IsBusy => ValueAsBase?.IsBusy ?? false || IsSelfBusy || IsMarkedBusy.Count > 0;

    public async Task WaitForTasks()
    {
        await (ValueAsBase?.WaitForTasks() ?? Task.CompletedTask);
    }

    [JsonIgnore]
    public bool IsSelfBusy { get; private set; } = false;

    [JsonIgnore]
    public List<long> IsMarkedBusy { get; } = new List<long>();
    private readonly object _isMarkedBusyLock = new object();

    public void AddMarkedBusy(long id)
    {
        lock (_isMarkedBusyLock)
        {
            if (!IsMarkedBusy.Contains(id))
            {
                IsMarkedBusy.Add(id);
            }
        }
        OnPropertyChanged(nameof(IsMarkedBusy));
        OnPropertyChanged(nameof(IsBusy));
    }

    public void RemoveMarkedBusy(long id)
    {
        lock (_isMarkedBusyLock)
        {
            IsMarkedBusy.Remove(id);
        }
        OnPropertyChanged(nameof(IsMarkedBusy));
        OnPropertyChanged(nameof(IsBusy));
    }

    public bool IsReadOnly { get; protected set; } = false;

    public virtual Task SetValue(object? newValue)
    {
        if (IsReadOnly)
        {
            throw new PropertyReadOnlyException();
        }

        return SetPrivateValue(newValue);
    }

    public virtual Task SetPrivateValue(object? newValue, bool quietly = false)
    {
        if (newValue == null && _value == null) { return Task.CompletedTask; }

        Task = Task.CompletedTask;

        if (newValue == null)
        {
            HandleNullValue(quietly);
        }
        else if (newValue is T value)
        {
            HandleNonNullValue(value, quietly);
        }
        else
        {
            throw new PropertyTypeMismatchException($"Type {newValue.GetType()} is not type {typeof(T).FullName}");
        }

        if (Task.Exception != null)
        {
            throw Task.Exception;
        }

        return Task;
    }

    public virtual void LoadValue(object? value)
    {
        SetPrivateValue((T?)value, true);
        _value = (T?)value;
    }

    protected virtual void HandleNullValue(bool quietly = false)
    {
        if (_value is INotifyNeatooPropertyChanged neatooPropertyChanged)
        {
            neatooPropertyChanged.NeatooPropertyChanged -= PassThruValueNeatooPropertyChanged;
        }

        _value = default;

        if (!quietly)
        {
            OnPropertyChanged(nameof(Value));
            Task = OnValueNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(this));
        }
    }

    protected virtual void HandleNonNullValue(T value, bool quietly = false)
    {
        var isDiff = !AreSame(_value, value);

        if (isDiff)
        {
            if (_value != null)
            {
                if (_value is INotifyNeatooPropertyChanged neatooPropertyChanged)
                {
                    neatooPropertyChanged.NeatooPropertyChanged -= PassThruValueNeatooPropertyChanged;
                }

                if (_value is IBase _valueBase)
                {
                    if (_valueBase.IsBusy)
                    {
                        throw new Exception("Cannot remove a child that is busy");
                    }
                }

                if (_value is ISetParent _valueSetParent)
                {
                    _valueSetParent.SetParent(null);
                }
            }

            if (value != null)
            {
                if (value is INotifyNeatooPropertyChanged valueNeatooPropertyChanged)
                {
                    valueNeatooPropertyChanged.NeatooPropertyChanged += PassThruValueNeatooPropertyChanged;
                }


                if (value is IBase valueBase)
                {
                    if (valueBase.IsBusy)
                    {
                        throw new Exception("Cannot add a child that is busy");
                    }
                }
            }
        }

        _value = value;

        if (isDiff && !quietly)
        {
            OnPropertyChanged(nameof(Value));

            Task = OnValueNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(this));
        }
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected virtual Task PassThruValueNeatooPropertyChanged(NeatooPropertyChangedEventArgs breadCrumbs)
    {
        return NeatooPropertyChanged?.Invoke(new NeatooPropertyChangedEventArgs(this, this.Value!, breadCrumbs)) ?? Task.CompletedTask;
    }

    protected virtual async Task OnValueNeatooPropertyChanged(NeatooPropertyChangedEventArgs breadCrumbs)
    {
        // ValidateBase sticks Task into AsyncTaskSequencer for us
        // so that it will be awaited by WaitForTasks()
        var task = NeatooPropertyChanged?.Invoke(breadCrumbs) ?? Task.CompletedTask;

        if (!task.IsCompleted && !IsSelfBusy)
        {
            IsSelfBusy = true;

            try
            {
                OnPropertyChanged(nameof(IsBusy));
                OnPropertyChanged(nameof(IsSelfBusy));

                await task;

                IsSelfBusy = false;
                OnPropertyChanged(nameof(IsBusy));
                OnPropertyChanged(nameof(IsSelfBusy));
            }
            finally
            {
                IsSelfBusy = false;
            }
        }

        await task;
    }

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
        if (Value is INotifyNeatooPropertyChanged neatooPropertyChanged)
        {
            neatooPropertyChanged.NeatooPropertyChanged += PassThruValueNeatooPropertyChanged;
        }
    }
}
