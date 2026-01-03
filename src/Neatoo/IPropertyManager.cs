using System.ComponentModel;

namespace Neatoo;

/// <summary>
/// Manages the collection of properties for a Neatoo object.
/// </summary>
/// <typeparam name="P">The type of property managed, which must implement <see cref="IProperty"/>.</typeparam>
/// <remarks>
/// <see cref="IPropertyManager{P}"/> is responsible for creating, storing, and providing access to
/// all managed properties on a Neatoo object. It handles property change notifications and
/// coordinates asynchronous operations across all properties.
/// </remarks>
public interface IPropertyManager<out P> : INotifyNeatooPropertyChanged, INotifyPropertyChanged
    where P : IProperty
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
    void SetProperties(IEnumerable<IProperty> properties);
}




