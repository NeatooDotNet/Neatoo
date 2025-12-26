using Neatoo.Rules;

namespace Neatoo;

/// <summary>
/// Manages the collection of validatable properties for a Neatoo object.
/// </summary>
/// <typeparam name="P">The type of property managed, which must implement <see cref="IProperty"/>.</typeparam>
/// <remarks>
/// <see cref="IValidatePropertyManager{P}"/> extends <see cref="IPropertyManager{P}"/> with validation
/// capabilities including rule execution, validity tracking, and validation message management.
/// It coordinates validation across all properties on a Neatoo object.
/// </remarks>
public interface IValidatePropertyManager<out P> : IPropertyManager<P>
    where P : IProperty
{
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
