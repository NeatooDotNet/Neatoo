using System.Collections;
using Neatoo.Rules;

namespace Neatoo;

/// <summary>
/// Internal interface for framework coordination within <see cref="IValidateBase"/> implementations.
/// </summary>
/// <remarks>
/// This interface exposes members used only by framework code (e.g., property managers, serializers, rule manager).
/// External consumers should not implement or depend on this interface.
/// Cast a base object to this interface when you need internal framework access.
/// </remarks>
internal interface IValidateBaseInternal
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
    /// <returns>The <see cref="IValidateProperty"/> instance for the specified property.</returns>
    IValidateProperty GetProperty(string propertyName);

    /// <summary>
    /// Gets the property with the specified name using indexer syntax.
    /// </summary>
    /// <param name="propertyName">The name of the property to retrieve.</param>
    /// <returns>The <see cref="IValidateProperty"/> instance for the specified property.</returns>
    IValidateProperty this[string propertyName] { get; }

    /// <summary>
    /// Gets the property manager. Used by serialization and rules.
    /// </summary>
    IValidatePropertyManager<IValidateProperty> PropertyManager { get; }

    /// <summary>
    /// Object-level validation error message set via MarkInvalid().
    /// Read by RuleManager for object-level validation.
    /// </summary>
    string? ObjectInvalid { get; }

    /// <summary>
    /// Gets the stable rule ID for a source expression.
    /// Called by RuleManager during rule registration to get deterministic IDs.
    /// The generated code overrides this to use the compile-time RuleIdRegistry.
    /// </summary>
    /// <param name="sourceExpression">The source expression captured by CallerArgumentExpression.</param>
    /// <returns>A stable uint ID for the rule.</returns>
    uint GetRuleId(string sourceExpression);
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

    /// <summary>
    /// Marks the entity for deletion without triggering list removal.
    /// Called by EntityListBase.RemoveItem to avoid recursion with Delete().
    /// </summary>
    void MarkDeleted();

    /// <summary>
    /// Gets the list that contains this entity.
    /// Used for Delete/Remove consistency and intra-aggregate moves.
    /// </summary>
    IEntityListBase? ContainingList { get; }

    /// <summary>
    /// Sets the containing list for this entity.
    /// Called by EntityListBase during InsertItem and FactoryComplete.
    /// </summary>
    /// <param name="list">The list that now contains this entity, or null to clear.</param>
    void SetContainingList(IEntityListBase? list);
}

/// <summary>
/// Internal interface for framework coordination within <see cref="IValidateProperty"/> implementations.
/// </summary>
/// <remarks>
/// This interface exposes members used only by framework code (e.g., RuleManager, ValidateBase).
/// External consumers should not implement or depend on this interface.
/// </remarks>
internal interface IValidatePropertyInternal
{
    /// <summary>
    /// Sets value bypassing IsReadOnly checks.
    /// Called by ValidateBase{T}.Setter. The "quietly" param suppresses events during init/deserialization.
    /// </summary>
    /// <param name="newValue">The new value to set.</param>
    /// <param name="quietly">If <c>true</c>, suppresses change notifications.</param>
    /// <returns>A task that represents the asynchronous set operation.</returns>
    Task SetPrivateValue(object? newValue, bool quietly = false);

    /// <summary>
    /// Sets validation messages produced by a specific rule.
    /// Called exclusively by RuleManager during rule execution.
    /// </summary>
    /// <param name="ruleMessages">The messages to set for the rule.</param>
    void SetMessagesForRule(IReadOnlyList<IRuleMessage> ruleMessages);

    /// <summary>
    /// Clears messages from a specific rule by ID.
    /// Called by RuleManager when a rule clears its previous messages.
    /// </summary>
    /// <param name="ruleId">The ID of the rule whose messages should be cleared.</param>
    void ClearMessagesForRule(uint ruleId);

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

    /// <summary>
    /// Removes an item from the deleted list.
    /// Called during intra-aggregate moves when an entity is added to a different list.
    /// </summary>
    /// <param name="item">The entity to remove from the deleted list.</param>
    void RemoveFromDeletedList(IEntityBase item);
}

/// <summary>
/// Internal interface for framework coordination within <see cref="IRuleMessage"/> implementations.
/// </summary>
/// <remarks>
/// This interface exposes the RuleId setter used only by RuleManager.
/// External consumers should not implement or depend on this interface.
/// </remarks>
internal interface IRuleMessageInternal
{
    /// <summary>
    /// Sets the rule ID. Called by RuleManager when processing rule results.
    /// </summary>
    uint RuleId { set; }
}
