namespace Neatoo;

/// <summary>
/// Manages the collection of entity properties for a Neatoo entity object.
/// </summary>
/// <remarks>
/// <see cref="IEntityPropertyManager"/> extends <see cref="IValidatePropertyManager{P}"/> with entity-specific
/// capabilities for tracking property modifications. It provides aggregate modification state
/// for all properties and supports resetting modification tracking after save operations.
/// </remarks>
public interface IEntityPropertyManager : IValidatePropertyManager<IEntityProperty>
{
    /// <summary>
    /// Gets a value indicating whether any property or child object has been modified.
    /// </summary>
    /// <value><c>true</c> if any modifications exist; otherwise, <c>false</c>.</value>
    bool IsModified { get; }

    /// <summary>
    /// Gets a value indicating whether any property value has been modified, without considering child objects.
    /// </summary>
    /// <value><c>true</c> if any property has been modified; otherwise, <c>false</c>.</value>
    bool IsSelfModified { get; }

    /// <summary>
    /// Gets the names of all properties that have been modified.
    /// </summary>
    /// <value>An enumerable collection of modified property names.</value>
    IEnumerable<string> ModifiedProperties { get; }

    /// <summary>
    /// Marks all properties as unmodified, resetting the modification tracking state.
    /// </summary>
    /// <remarks>
    /// This method is typically called after a successful save operation to reset the dirty state.
    /// </remarks>
    void MarkSelfUnmodified();
}
