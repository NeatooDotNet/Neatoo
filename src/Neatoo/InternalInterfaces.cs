using System.Collections;
using Neatoo.Rules;

namespace Neatoo;

/// <summary>
/// Internal interface for framework coordination within <see cref="IBase"/> implementations.
/// </summary>
/// <remarks>
/// This interface exposes members used only by framework code (e.g., property managers, serializers).
/// External consumers should not implement or depend on this interface.
/// Cast a base object to this interface when you need internal framework access.
/// </remarks>
internal interface IBaseInternal
{
    /// <summary>
    /// Adds a child task to be tracked for completion.
    /// Called by child objects to bubble tasks up the object graph.
    /// </summary>
    /// <param name="task">The task to track.</param>
    void AddChildTask(Task task);

    /// <summary>
    /// Gets a property by name. Framework-internal access.
    /// </summary>
    /// <param name="propertyName">The name of the property to retrieve.</param>
    /// <returns>The <see cref="IProperty"/> instance for the specified property.</returns>
    IProperty GetProperty(string propertyName);

    /// <summary>
    /// Gets the property with the specified name using indexer syntax.
    /// </summary>
    /// <param name="propertyName">The name of the property to retrieve.</param>
    /// <returns>The <see cref="IProperty"/> instance for the specified property.</returns>
    IProperty this[string propertyName] { get; }

    /// <summary>
    /// Gets the property manager. Used by serialization and rules.
    /// </summary>
    IPropertyManager<IProperty> PropertyManager { get; }
}

/// <summary>
/// Internal interface for framework coordination within <see cref="IValidateBase"/> implementations.
/// </summary>
/// <remarks>
/// This interface exposes members used only by framework code (e.g., rule manager).
/// External consumers should not implement or depend on this interface.
/// </remarks>
internal interface IValidateBaseInternal : IBaseInternal
{
    /// <summary>
    /// Object-level validation error message set via MarkInvalid().
    /// Read by RuleManager for object-level validation.
    /// </summary>
    string? ObjectInvalid { get; }
}

/// <summary>
/// Internal interface for framework coordination within <see cref="IEntityBase"/> implementations.
/// </summary>
/// <remarks>
/// This interface exposes members used only by framework code (e.g., EntityListBase).
/// External consumers should not implement or depend on this interface.
/// </remarks>
internal interface IEntityBaseInternal : IValidateBaseInternal
{
    /// <summary>
    /// Explicitly marks the entity as modified.
    /// Called by EntityListBase when items are added.
    /// </summary>
    void MarkModified();

    /// <summary>
    /// Marks the entity as a child within an aggregate.
    /// Called by EntityListBase when items are added.
    /// </summary>
    void MarkAsChild();
}

/// <summary>
/// Internal interface for framework coordination within <see cref="IProperty"/> implementations.
/// </summary>
/// <remarks>
/// This interface exposes members used only by framework code (e.g., Base{T}.Setter).
/// External consumers should not implement or depend on this interface.
/// </remarks>
internal interface IPropertyInternal
{
    /// <summary>
    /// Sets value bypassing IsReadOnly checks.
    /// Called by Base{T}.Setter. The "quietly" param suppresses events during init/deserialization.
    /// </summary>
    /// <param name="newValue">The new value to set.</param>
    /// <param name="quietly">If <c>true</c>, suppresses change notifications.</param>
    /// <returns>A task that represents the asynchronous set operation.</returns>
    Task SetPrivateValue(object? newValue, bool quietly = false);
}

/// <summary>
/// Internal interface for framework coordination within <see cref="IValidateProperty"/> implementations.
/// </summary>
/// <remarks>
/// This interface exposes members used only by framework code (e.g., RuleManager, ValidateBase).
/// External consumers should not implement or depend on this interface.
/// </remarks>
internal interface IValidatePropertyInternal : IPropertyInternal
{
    /// <summary>
    /// Sets validation messages produced by a specific rule.
    /// Called exclusively by RuleManager during rule execution.
    /// </summary>
    /// <param name="ruleMessages">The messages to set for the rule.</param>
    void SetMessagesForRule(IReadOnlyList<IRuleMessage> ruleMessages);

    /// <summary>
    /// Clears messages from a specific rule by index.
    /// Called by RuleManager when a rule clears its previous messages.
    /// </summary>
    /// <param name="ruleIndex">The index of the rule whose messages should be cleared.</param>
    void ClearMessagesForRule(uint ruleIndex);

    /// <summary>
    /// Clears all validation messages including child messages.
    /// Called during RunRules(RunRulesFlag.All) to reset validation state.
    /// </summary>
    void ClearAllMessages();

    /// <summary>
    /// Clears only this property's messages, not child messages.
    /// Called by ValidateBase.ClearSelfMessages().
    /// </summary>
    void ClearSelfMessages();
}

/// <summary>
/// Internal interface for framework coordination within <see cref="IPropertyManager{P}"/> implementations.
/// </summary>
/// <typeparam name="P">The type of property managed.</typeparam>
/// <remarks>
/// This interface exposes members used only by framework code (e.g., serialization, deserialization).
/// External consumers should not implement or depend on this interface.
/// </remarks>
internal interface IPropertyManagerInternal<out P> where P : IProperty
{
    /// <summary>
    /// Gets metadata about all registered properties.
    /// Used during deserialization and rule setup.
    /// </summary>
    IPropertyInfoList PropertyInfoList { get; }

    /// <summary>
    /// Gets all instantiated properties.
    /// Used during serialization and deserialization.
    /// </summary>
    IEnumerable<P> GetProperties { get; }
}

/// <summary>
/// Internal interface for framework coordination within <see cref="IEntityListBase"/> implementations.
/// </summary>
/// <remarks>
/// This interface exposes members used only by framework code (e.g., save operations).
/// External consumers should not implement or depend on this interface.
/// </remarks>
internal interface IEntityListBaseInternal
{
    /// <summary>
    /// Items removed but needing persistence deletion.
    /// Used during save operations.
    /// </summary>
    IEnumerable DeletedList { get; }
}

/// <summary>
/// Internal interface for framework coordination within <see cref="IRuleMessage"/> implementations.
/// </summary>
/// <remarks>
/// This interface exposes the RuleIndex setter used only by RuleManager.
/// External consumers should not implement or depend on this interface.
/// </remarks>
internal interface IRuleMessageInternal
{
    /// <summary>
    /// Sets the rule index. Called by RuleManager when processing rule results.
    /// </summary>
    uint RuleIndex { set; }
}
