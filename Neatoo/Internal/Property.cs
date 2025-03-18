using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Neatoo.Core;

public interface IProperty : INotifyPropertyChanged, INotifyNeatooPropertyChanged
{
    string Name { get; }
    object? Value { get; set; }
    string? StringValue { get; set; }
    Task SetStringValue(string? value);
    Task SetValue<P>(P? newValue);
    internal Task SetPrivateValue<P>(P? newValue, bool quietly = false);
    Task Task { get; }
    bool IsBusy { get; }
    bool IsSelfBusy { get; }
    bool IsReadOnly { get; }
    /// <summary>
    /// Sets the value without running any rules or raising the Neatoo event. It does raise PropertyChanged
    /// </summary>
    /// <param name="value"></param>
    void LoadValue(object? value);
    Task WaitForTasks();
    TaskAwaiter GetAwaiter() => Task.GetAwaiter();
}

public interface IProperty<T> : IProperty
{
    new T? Value { get; set; }
}

public class PropertyChangedBreadCrumbs
{
    public PropertyChangedBreadCrumbs(string propertyName, object source)
    {
        PropertyName = propertyName;
        Source = source;
    }

    public PropertyChangedBreadCrumbs(string propertyName, object source, PropertyChangedBreadCrumbs? previousPropertyName) : this(propertyName, source)
    {
        InnerBreadCrumb = previousPropertyName;
    }

    public string PropertyName { get; private set; }
    public object Source { get; private set; }
    public PropertyChangedBreadCrumbs? InnerBreadCrumb { get; private set; }
    public string FullPropertyName => PropertyName + (InnerBreadCrumb == null ? "" : "." + InnerBreadCrumb.FullPropertyName);
}


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

    [JsonIgnore]
    public virtual string? StringValue
    {
        get
        {
            Console.WriteLine($"Read StringValue: {Value?.ToString()}");
            return Value?.ToString();
        }
        set
        {
            Task = SetStringValue(value);
        }
    }

    public async Task SetStringValue(string? value)
    {
        Console.WriteLine($"Set StringValue: {value}");
        if (value == null)
        {
            await SetValue<T?>(default);
        }
        else
        {
            var converter = TypeDescriptor.GetConverter(typeof(T));
            if (converter != null && converter.IsValid(value))
            {
                await SetValue((T?)converter.ConvertFromString(value));
            }
        }
    }

    object? IProperty.Value { get => Value; set => SetValue(value); }

    [JsonIgnore]
    public Task Task { get; protected set; } = Task.CompletedTask;

    protected IBase? ValueAsBase => Value as IBase;

    public bool IsBusy => ValueAsBase?.IsBusy ?? false || IsSelfBusy;

    public async Task WaitForTasks()
    {
        await (ValueAsBase?.WaitForTasks() ?? Task.CompletedTask);
    }

    [JsonIgnore]
    public bool IsSelfBusy { get; private set; } = false;

    public bool IsReadOnly { get; protected set; } = false;

    public virtual Task SetValue<P>(P? newValue)
    {
        if (IsReadOnly)
        {
            throw new PropertyReadOnlyException();
        }

        return SetPrivateValue(newValue);
    }

    public virtual Task SetPrivateValue<P>(P? newValue, bool quietly = false)
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
            Task = OnValueNeatooPropertyChanged(new PropertyChangedBreadCrumbs(this.Name, this));
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

            Task = OnValueNeatooPropertyChanged(new PropertyChangedBreadCrumbs(this.Name, this));
        }
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected virtual Task PassThruValueNeatooPropertyChanged(PropertyChangedBreadCrumbs breadCrumbs)
    {
        return NeatooPropertyChanged?.Invoke(new PropertyChangedBreadCrumbs(this.Name, breadCrumbs.Source, breadCrumbs)) ?? Task.CompletedTask;
    }

    protected virtual async Task OnValueNeatooPropertyChanged(PropertyChangedBreadCrumbs breadCrumbs)
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

[Serializable]
internal class PropertyReadOnlyException : Exception
{
    public PropertyReadOnlyException() { }
    public PropertyReadOnlyException(string? message) : base(message) { }
    public PropertyReadOnlyException(string? message, Exception? innerException) : base(message, innerException) { }
}


[Serializable]
public class PropertyMissingException : Exception
{
    public PropertyMissingException() { }
    public PropertyMissingException(string message) : base(message) { }
    public PropertyMissingException(string message, Exception inner) : base(message, inner) { }

}