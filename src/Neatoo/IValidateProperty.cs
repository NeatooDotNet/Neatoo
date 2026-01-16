using Neatoo.Rules;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Neatoo;

/// <summary>
/// Defines the interface for a managed property in a Neatoo object that supports validation and business rules.
/// </summary>
/// <remarks>
/// <see cref="IValidateProperty"/> provides value storage, change notification, asynchronous task tracking,
/// busy state management, validation capabilities, and rule execution. Properties are the fundamental
/// unit of data management in Neatoo objects.
/// </remarks>
public interface IValidateProperty : INotifyPropertyChanged, INotifyNeatooPropertyChanged
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

    /// <summary>
    /// Gets a value indicating whether this property is valid without considering child objects.
    /// </summary>
    /// <value><c>true</c> if this property has no error messages; otherwise, <c>false</c>.</value>
    bool IsSelfValid { get; }

    /// <summary>
    /// Gets a value indicating whether this property and all its children are valid.
    /// </summary>
    /// <value><c>true</c> if this property and all child objects are valid; otherwise, <c>false</c>.</value>
    bool IsValid { get; }

    /// <summary>
    /// Executes all validation rules associated with this property.
    /// </summary>
    /// <param name="runRules">Flags controlling which rules to execute.</param>
    /// <param name="token">Optional cancellation token to cancel rule execution.</param>
    /// <returns>A task representing the asynchronous rule execution operation.</returns>
    Task RunRules(RunRulesFlag runRules = Neatoo.RunRulesFlag.All, CancellationToken? token = null);

    /// <summary>
    /// Gets the collection of validation messages for this property.
    /// </summary>
    /// <value>A read-only collection of <see cref="IPropertyMessage"/> instances.</value>
    IReadOnlyCollection<IPropertyMessage> PropertyMessages { get; }

    /// <summary>
    /// Gets a value indicating whether the property value has been loaded.
    /// </summary>
    /// <remarks>
    /// For properties with lazy loading configured, this returns <c>true</c> after the value
    /// has been loaded (either explicitly or via auto-load on first access).
    /// For properties without lazy loading, this always returns <c>true</c>.
    /// </remarks>
    /// <value><c>true</c> if the value is loaded; otherwise, <c>false</c>.</value>
    bool IsLoaded { get; }

    /// <summary>
    /// Gets the task representing a pending lazy load operation.
    /// </summary>
    /// <value>The load task if a load is in progress; otherwise, <c>null</c>.</value>
    Task? LoadTask { get; }

    /// <summary>
    /// Explicitly triggers lazy loading of the property value.
    /// </summary>
    /// <returns>A task that completes when the value is loaded.</returns>
    /// <remarks>
    /// If no <c>OnLoad</c> handler is configured, this method completes immediately.
    /// If the value is already loaded, this method completes immediately.
    /// </remarks>
    Task LoadAsync();
}

/// <summary>
/// Defines the interface for a strongly-typed managed property that supports validation and business rules.
/// </summary>
/// <typeparam name="T">The type of the property value.</typeparam>
public interface IValidateProperty<T> : IValidateProperty
{
    /// <summary>
    /// Gets or sets the strongly-typed value of the property.
    /// </summary>
    /// <value>The current value of the property.</value>
    new T? Value { get; set; }

    /// <summary>
    /// Gets or sets the lazy load handler for this property.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When configured, the handler is invoked automatically on first access to <see cref="Value"/>
    /// if the value is <c>null</c>. The load is fire-and-forget; the getter returns immediately
    /// with the current value while loading occurs in the background.
    /// </para>
    /// <para>
    /// Configure this in the entity constructor after calling the base constructor:
    /// </para>
    /// <code>
    /// public Person(IEntityBaseServices&lt;Person&gt; services, IPhoneDbContext context)
    ///     : base(services)
    /// {
    ///     PhonesProperty.OnLoad = async () => await context.LoadPhones(this.Id);
    /// }
    /// </code>
    /// </remarks>
    Func<Task<T?>>? OnLoad { get; set; }

    /// <summary>
    /// Explicitly triggers lazy loading and returns the loaded value.
    /// </summary>
    /// <returns>A task containing the loaded value.</returns>
    new Task<T?> LoadAsync();
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
