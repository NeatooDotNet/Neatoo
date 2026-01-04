using Neatoo.RemoteFactory;

namespace Neatoo;

/// <summary>
/// Provides meta-properties for tracking validation state on Neatoo objects.
/// </summary>
/// <remarks>
/// <see cref="IValidateMetaProperties"/> defines the fundamental state properties that all Neatoo objects
/// expose for tracking pending asynchronous operations, validation state, and rule execution.
/// These properties enable UI binding and workflow coordination.
/// </remarks>
public interface IValidateMetaProperties
{
    /// <summary>
    /// Gets a value indicating whether the object has pending asynchronous operations.
    /// </summary>
    /// <value><c>true</c> if the object or any of its properties are busy; otherwise, <c>false</c>.</value>
    bool IsBusy { get; }

    /// <summary>
    /// Waits for all pending asynchronous operations to complete.
    /// </summary>
    /// <returns>A task that completes when all pending operations are finished.</returns>
    Task WaitForTasks();

    /// <summary>
    /// Gets a value indicating whether the object and all its children are valid.
    /// </summary>
    /// <value><c>true</c> if the object and all child objects are valid; otherwise, <c>false</c>.</value>
    bool IsValid { get; }

    /// <summary>
    /// Gets a value indicating whether the object itself is valid, without considering child objects.
    /// </summary>
    /// <value><c>true</c> if the object has no validation errors; otherwise, <c>false</c>.</value>
    bool IsSelfValid { get; }

    /// <summary>
    /// Gets the collection of all validation messages for this object.
    /// </summary>
    /// <value>A read-only collection of <see cref="IPropertyMessage"/> instances.</value>
    IReadOnlyCollection<IPropertyMessage> PropertyMessages { get; }

    /// <summary>
    /// Executes validation rules for the specified property.
    /// </summary>
    /// <param name="propertyName">The name of the property whose rules should be executed.</param>
    /// <param name="token">Optional cancellation token to cancel rule execution.</param>
    /// <returns>A task representing the asynchronous rule execution operation.</returns>
    Task RunRules(string propertyName, CancellationToken? token = null);

    /// <summary>
    /// Executes validation rules for the object based on the specified flags.
    /// </summary>
    /// <param name="runRules">Flags controlling which rules to execute.</param>
    /// <param name="token">Optional cancellation token to cancel rule execution.</param>
    /// <returns>A task representing the asynchronous rule execution operation.</returns>
    Task RunRules(RunRulesFlag runRules = Neatoo.RunRulesFlag.All, CancellationToken? token = null);

    /// <summary>
    /// Clears all validation messages from the object and its children.
    /// </summary>
    void ClearAllMessages();

    /// <summary>
    /// Clears validation messages that apply directly to this object, excluding child object messages.
    /// </summary>
    void ClearSelfMessages();
}

/// <summary>
/// Provides meta-properties for tracking entity state on Neatoo objects.
/// </summary>
/// <remarks>
/// <see cref="IEntityMetaProperties"/> defines state properties specific to entity objects
/// that participate in persistence operations. These properties track modification state,
/// parent-child relationships, and save eligibility.
/// </remarks>
public interface IEntityMetaProperties : IFactorySaveMeta
{
    /// <summary>
    /// Gets a value indicating whether this object is a child within an aggregate.
    /// </summary>
    /// <value><c>true</c> if the object is a child entity; otherwise, <c>false</c>.</value>
    /// <remarks>
    /// Child entities are saved as part of their parent aggregate and cannot be saved independently.
    /// </remarks>
    bool IsChild { get; }

    /// <summary>
    /// Gets a value indicating whether the object or any of its children have been modified.
    /// </summary>
    /// <value><c>true</c> if any modifications exist; otherwise, <c>false</c>.</value>
    bool IsModified { get; }

    /// <summary>
    /// Gets a value indicating whether the object itself has been modified, without considering children.
    /// </summary>
    /// <value><c>true</c> if the object has been modified; otherwise, <c>false</c>.</value>
    bool IsSelfModified { get; }

    /// <summary>
    /// Gets a value indicating whether the object has been explicitly marked as modified.
    /// </summary>
    /// <value><c>true</c> if explicitly marked modified; otherwise, <c>false</c>.</value>
    bool IsMarkedModified { get; }

    /// <summary>
    /// Gets a value indicating whether the object can be saved.
    /// </summary>
    /// <value><c>true</c> if the object is modified, valid, not busy, and not a child; otherwise, <c>false</c>.</value>
    bool IsSavable { get; }
}
