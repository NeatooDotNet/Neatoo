namespace Neatoo;

/// <summary>
/// Defines the interface for a property that supports entity tracking features including modification state.
/// </summary>
/// <remarks>
/// <see cref="IEntityProperty"/> extends <see cref="IValidateProperty"/> with entity-specific capabilities
/// for tracking changes and supporting persistence scenarios. Properties implementing this interface
/// can track whether they have been modified since creation or last save.
/// </remarks>
public interface IEntityProperty : IValidateProperty
{
    /// <summary>
    /// Gets or sets a value indicating whether events and tracking are paused for this property.
    /// </summary>
    /// <value><c>true</c> if the property is paused; otherwise, <c>false</c>.</value>
    /// <remarks>
    /// When paused, property changes do not trigger events or update modification tracking.
    /// This is useful during bulk loading or deserialization operations.
    /// </remarks>
    bool IsPaused { get; set; }

    /// <summary>
    /// Gets a value indicating whether this property or any child objects have been modified.
    /// </summary>
    /// <value><c>true</c> if the property or any child objects are modified; otherwise, <c>false</c>.</value>
    bool IsModified { get; }

    /// <summary>
    /// Gets a value indicating whether this property value has been modified, without considering child objects.
    /// </summary>
    /// <value><c>true</c> if the property value has been modified; otherwise, <c>false</c>.</value>
    bool IsSelfModified { get; }

    /// <summary>
    /// Marks this property as unmodified, resetting the modification tracking state.
    /// </summary>
    /// <remarks>
    /// This method is typically called after a successful save operation to reset the dirty state.
    /// </remarks>
    void MarkSelfUnmodified();

    /// <summary>
    /// Gets the display name for this property, typically used for UI presentation.
    /// </summary>
    /// <value>The human-readable display name for the property.</value>
    string DisplayName { get; }
}

/// <summary>
/// Defines the interface for a strongly-typed property that supports entity tracking features.
/// </summary>
/// <typeparam name="T">The type of the property value.</typeparam>
public interface IEntityProperty<T> : IEntityProperty, IValidateProperty<T>
{

}