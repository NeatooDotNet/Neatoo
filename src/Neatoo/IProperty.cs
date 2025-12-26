using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Neatoo;

/// <summary>
/// Defines the interface for a managed property in a Neatoo object.
/// </summary>
/// <remarks>
/// <see cref="IProperty"/> provides value storage, change notification, asynchronous task tracking,
/// and busy state management. Properties are the fundamental unit of data management in Neatoo objects.
/// </remarks>
public interface IProperty : INotifyPropertyChanged, INotifyNeatooPropertyChanged
{
    /// <summary>
    /// Gets the name of the property.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets or sets the value of the property.
    /// </summary>
    /// <value>The current value of the property, or <c>null</c>.</value>
    object? Value { get; set; }

    /// <summary>
    /// Sets the value of the property asynchronously.
    /// </summary>
    /// <param name="newValue">The new value to set.</param>
    /// <returns>A task that represents the asynchronous set operation.</returns>
    Task SetValue(object? newValue);

    /// <summary>
    /// Sets the value of the property internally with optional quiet mode.
    /// </summary>
    /// <param name="newValue">The new value to set.</param>
    /// <param name="quietly">If <c>true</c>, suppresses change notifications.</param>
    /// <returns>A task that represents the asynchronous set operation.</returns>
    internal Task SetPrivateValue(object? newValue, bool quietly = false);

    /// <summary>
    /// Gets the task representing any pending asynchronous operations on this property.
    /// </summary>
    Task Task { get; }

    /// <summary>
    /// Gets a value indicating whether the property has pending asynchronous operations.
    /// </summary>
    /// <value><c>true</c> if the property is busy; otherwise, <c>false</c>.</value>
    bool IsBusy { get; }

    /// <summary>
    /// Gets a value indicating whether the property is read-only.
    /// </summary>
    /// <value><c>true</c> if the property is read-only; otherwise, <c>false</c>.</value>
    bool IsReadOnly { get; }

    /// <summary>
    /// Marks the property as busy with the specified identifier.
    /// </summary>
    /// <param name="id">A unique identifier for the busy operation.</param>
    void AddMarkedBusy(long id);

    /// <summary>
    /// Removes the busy mark with the specified identifier.
    /// </summary>
    /// <param name="id">The identifier of the busy operation to remove.</param>
    void RemoveMarkedBusy(long id);

    /// <summary>
    /// Sets the value without running any rules or raising the Neatoo event.
    /// </summary>
    /// <remarks>
    /// This method raises <see cref="INotifyPropertyChanged.PropertyChanged"/> but does not
    /// trigger rule execution or Neatoo-specific property change notifications.
    /// </remarks>
    /// <param name="value">The value to load.</param>
    void LoadValue(object? value);

    /// <summary>
    /// Waits for all pending tasks on this property to complete.
    /// </summary>
    /// <returns>A task that completes when all pending operations are finished.</returns>
    Task WaitForTasks();

    /// <summary>
    /// Gets an awaiter for the pending task on this property.
    /// </summary>
    /// <returns>A <see cref="TaskAwaiter"/> for the pending task.</returns>
    TaskAwaiter GetAwaiter() => Task.GetAwaiter();

    /// <summary>
    /// Gets the declared type of the property.
    /// </summary>
    Type Type { get; }

    /// <summary>
    /// Gets the string representation of the property value.
    /// </summary>
    /// <value>The string representation of <see cref="Value"/>, or <c>null</c> if the value is <c>null</c>.</value>
    public string? StringValue => Value?.ToString();
}

/// <summary>
/// Defines the interface for a strongly-typed managed property in a Neatoo object.
/// </summary>
/// <typeparam name="T">The type of the property value.</typeparam>
public interface IProperty<T> : IProperty
{
    /// <summary>
    /// Gets or sets the strongly-typed value of the property.
    /// </summary>
    /// <value>The current value of the property.</value>
    new T? Value { get; set; }
}

/// <summary>
/// Exception thrown when attempting to set a read-only property.
/// </summary>
[Serializable]
internal class PropertyReadOnlyException : PropertyException
{
    public PropertyReadOnlyException() : base("Cannot set a read-only property.") { }
    public PropertyReadOnlyException(string? message) : base(message ?? "Cannot set a read-only property.") { }
    public PropertyReadOnlyException(string? message, Exception? innerException) : base(message ?? "Cannot set a read-only property.", innerException!) { }
}

/// <summary>
/// Exception thrown when a required property cannot be found.
/// </summary>
[Serializable]
public class PropertyMissingException : PropertyException
{
    public PropertyMissingException() { }
    public PropertyMissingException(string message) : base(message) { }
    public PropertyMissingException(string message, Exception inner) : base(message, inner) { }
}