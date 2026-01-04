using Neatoo.Rules;
using System.ComponentModel;

namespace Neatoo;

/// <summary>
/// Manages the collection of validatable properties for a Neatoo object.
/// </summary>
/// <typeparam name="P">The type of property managed, which must implement <see cref="IValidateProperty"/>.</typeparam>
/// <remarks>
/// <see cref="IValidatePropertyManager{P}"/> is responsible for creating, storing, and providing access to
/// all managed properties on a Neatoo object. It handles property change notifications, coordinates
/// asynchronous operations, and provides validation capabilities including rule execution, validity
/// tracking, and validation message management.
/// </remarks>
public interface IValidatePropertyManager<out P> : INotifyNeatooPropertyChanged, INotifyPropertyChanged
    where P : IValidateProperty
{
    /// <summary>
    /// Gets a value indicating whether any property in this manager has pending asynchronous operations.
    /// </summary>
    /// <value><c>true</c> if any property is busy; otherwise, <c>false</c>.</value>
    bool IsBusy { get; }

    /// <summary>
    /// Waits for all pending asynchronous operations on all managed properties to complete.
    /// </summary>
    /// <returns>A task that completes when all pending operations are finished.</returns>
    Task WaitForTasks();

    /// <summary>
    /// Determines whether a property with the specified name exists in this manager.
    /// </summary>
    /// <param name="propertyName">The name of the property to check.</param>
    /// <returns><c>true</c> if the property exists; otherwise, <c>false</c>.</returns>
    bool HasProperty(string propertyName);

    /// <summary>
    /// Gets the property with the specified name.
    /// </summary>
    /// <param name="propertyName">The name of the property to retrieve.</param>
    /// <returns>The property instance.</returns>
    /// <exception cref="PropertyMissingException">Thrown when the property does not exist.</exception>
    P GetProperty(string propertyName);

    /// <summary>
    /// Gets the property with the specified name using indexer syntax.
    /// </summary>
    /// <param name="propertyName">The name of the property to retrieve.</param>
    /// <returns>The property instance.</returns>
    /// <exception cref="PropertyMissingException">Thrown when the property does not exist.</exception>
    public P this[string propertyName] { get => GetProperty(propertyName); }

    /// <summary>
    /// Sets the properties managed by this instance.
    /// </summary>
    /// <param name="properties">The collection of properties to manage.</param>
    /// <remarks>
    /// This method is typically called during deserialization to restore the property collection.
    /// </remarks>
    void SetProperties(IEnumerable<IValidateProperty> properties);

    /// <summary>
    /// Gets a value indicating whether all properties are valid, without considering child objects.
    /// </summary>
    /// <value><c>true</c> if all properties are valid; otherwise, <c>false</c>.</value>
    bool IsSelfValid { get; }

    /// <summary>
    /// Gets a value indicating whether all properties and their child objects are valid.
    /// </summary>
    /// <value><c>true</c> if all properties and child objects are valid; otherwise, <c>false</c>.</value>
    bool IsValid { get; }

    /// <summary>
    /// Executes validation rules for all properties based on the specified flags.
    /// </summary>
    /// <param name="runRules">Flags controlling which rules to execute.</param>
    /// <param name="token">Optional cancellation token to cancel rule execution.</param>
    /// <returns>A task representing the asynchronous rule execution operation.</returns>
    Task RunRules(RunRulesFlag runRules = Neatoo.RunRulesFlag.All, CancellationToken? token = null);

    /// <summary>
    /// Gets the aggregated collection of validation messages from all properties.
    /// </summary>
    /// <value>A read-only collection of <see cref="IPropertyMessage"/> instances.</value>
    IReadOnlyCollection<IPropertyMessage> PropertyMessages { get; }

    /// <summary>
    /// Gets a value indicating whether event and rule processing is paused.
    /// </summary>
    /// <value><c>true</c> if the manager is paused; otherwise, <c>false</c>.</value>
    bool IsPaused { get; }

    /// <summary>
    /// Pauses all property events, rules, and tracking for all managed properties.
    /// </summary>
    /// <remarks>
    /// Use this method during bulk loading or deserialization to prevent unnecessary event processing.
    /// </remarks>
    void PauseAllActions();

    /// <summary>
    /// Resumes all property events, rules, and tracking for all managed properties.
    /// </summary>
    /// <remarks>
    /// Call this method after <see cref="PauseAllActions"/> to restore normal operation.
    /// </remarks>
    void ResumeAllActions();

    /// <summary>
    /// Clears all validation messages from all properties, including child object messages.
    /// </summary>
    void ClearAllMessages();

    /// <summary>
    /// Clears validation messages that apply directly to properties, excluding child object messages.
    /// </summary>
    void ClearSelfMessages();
}
