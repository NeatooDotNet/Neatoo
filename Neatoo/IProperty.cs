using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Neatoo;

public interface IProperty : INotifyPropertyChanged, INotifyNeatooPropertyChanged
{
    string Name { get; }
    object? Value { get; set; }
    Task SetValue(object? newValue);
    internal Task SetPrivateValue(object? newValue, bool quietly = false);
    Task Task { get; }
    bool IsBusy { get; }
    bool IsReadOnly { get; }
    void AddMarkedBusy(long id);
    void RemoveMarkedBusy(long id);
    /// <summary>
    /// Sets the value without running any rules or raising the Neatoo event. It does raise PropertyChanged
    /// </summary>
    /// <param name="value"></param>
    void LoadValue(object? value);
    Task WaitForTasks();
    TaskAwaiter GetAwaiter() => Task.GetAwaiter();
    Type Type { get; }
    public string? StringValue => Value?.ToString();
}

public interface IProperty<T> : IProperty
{
    new T? Value { get; set; }
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