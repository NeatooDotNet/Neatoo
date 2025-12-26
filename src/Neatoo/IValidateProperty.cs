using Neatoo.Rules;
using System.ComponentModel;

namespace Neatoo;

/// <summary>
/// Defines the interface for a property that supports validation and business rules.
/// </summary>
/// <remarks>
/// <see cref="IValidateProperty"/> extends <see cref="IProperty"/> with validation capabilities,
/// including storing validation messages, executing rules, and tracking validity state.
/// Properties implementing this interface can participate in the Neatoo validation pipeline.
/// </remarks>
public interface IValidateProperty : IProperty, INotifyPropertyChanged
{
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
    /// Sets the validation messages produced by a specific rule.
    /// </summary>
    /// <param name="ruleMessages">The messages to set for the rule.</param>
    internal void SetMessagesForRule(IReadOnlyList<IRuleMessage> ruleMessages);

    /// <summary>
    /// Clears all validation messages produced by the specified rule.
    /// </summary>
    /// <param name="ruleIndex">The index of the rule whose messages should be cleared.</param>
    internal void ClearMessagesForRule(uint ruleIndex);

    /// <summary>
    /// Clears all validation messages from this property, including child object messages.
    /// </summary>
    internal void ClearAllMessages();

    /// <summary>
    /// Clears validation messages that apply directly to this property, excluding child object messages.
    /// </summary>
    internal void ClearSelfMessages();
}

/// <summary>
/// Defines the interface for a strongly-typed property that supports validation and business rules.
/// </summary>
/// <typeparam name="T">The type of the property value.</typeparam>
public interface IValidateProperty<T> : IValidateProperty, IProperty<T>
{
}