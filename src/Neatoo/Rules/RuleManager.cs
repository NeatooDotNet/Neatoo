using Neatoo.Rules.Rules;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.Serialization;

namespace Neatoo.Rules;

/// <summary>
/// Defines the contract for managing and executing business rules on a validation target.
/// The rule manager is responsible for storing rules, triggering them when properties change,
/// and coordinating the application of validation messages to properties.
/// </summary>
/// <remarks>
/// Rules are automatically executed when their trigger properties change.
/// Rules can also be explicitly executed using the various RunRule methods.
/// </remarks>
public interface IRuleManager
{
    /// <summary>
    /// Gets all rules registered with this rule manager.
    /// </summary>
    IEnumerable<IRule> Rules { get; }

    /// <summary>
    /// Runs all rules that are triggered by the specified property.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    /// <param name="token">Optional cancellation token to cancel rule execution.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RunRules(string propertyName, CancellationToken? token = null);

    /// <summary>
    /// Runs rules based on the specified flags.
    /// </summary>
    /// <param name="runRules">Flags indicating which rules to run.</param>
    /// <param name="token">Optional cancellation token to cancel rule execution.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RunRules(RunRulesFlag runRules = Neatoo.RunRulesFlag.All, CancellationToken? token = null);

    /// <summary>
    /// Adds a strongly-typed rule to the rule manager.
    /// </summary>
    /// <typeparam name="T">The type of validation target the rule operates on.</typeparam>
    /// <param name="rule">The rule to add.</param>
    void AddRule<T>(IRule<T> rule) where T : IValidateBase;

    /// <summary>
    /// Adds multiple strongly-typed rules to the rule manager.
    /// </summary>
    /// <typeparam name="T">The type of validation target the rules operate on.</typeparam>
    /// <param name="rules">The rules to add.</param>
    void AddRules<T>(params IRule<T>[] rules) where T : IValidateBase;

    /// <summary>
    /// Runs a specific rule instance.
    /// </summary>
    /// <param name="r">The rule to execute.</param>
    /// <param name="token">Optional cancellation token to cancel rule execution.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RunRule(IRule r, CancellationToken? token = null);

    /// <summary>
    /// Runs all rules of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of rule to execute.</typeparam>
    /// <param name="token">Optional cancellation token to cancel rule execution.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RunRule<T>(CancellationToken? token = null) where T : IRule;
}


/// <summary>
/// Defines a strongly-typed rule manager that provides fluent methods for adding inline rules.
/// Extends <see cref="IRuleManager"/> with convenience methods for common rule patterns.
/// </summary>
/// <typeparam name="T">The type of validation target this rule manager operates on.</typeparam>
public interface IRuleManager<T> : IRuleManager
    where T : class, IValidateBase
{
    /// <summary>
    /// Adds a synchronous action rule that executes when the specified properties change.
    /// Use this for rules that perform side effects like calculating derived values.
    /// </summary>
    /// <param name="func">The action to execute.</param>
    /// <param name="triggerProperties">The properties that trigger this rule when changed.</param>
    /// <returns>The created rule instance.</returns>
    ActionFluentRule<T> AddAction(Action<T> func, params Expression<Func<T, object?>>[] triggerProperties);

    /// <summary>
    /// Adds a synchronous validation rule that executes when the specified property changes.
    /// The function should return an error message string, or an empty/null string if validation passes.
    /// </summary>
    /// <param name="func">The validation function that returns an error message or empty string.</param>
    /// <param name="triggerProperty">The property that triggers this rule and receives the error message.</param>
    /// <returns>The created rule instance.</returns>
    ValidationFluentRule<T> AddValidation(Func<T, string> func, Expression<Func<T, object?>> triggerProperty);

    /// <summary>
    /// Adds an asynchronous action rule that executes when the specified properties change.
    /// Use this for rules that perform async side effects like calling external services.
    /// </summary>
    /// <param name="func">The async function to execute.</param>
    /// <param name="triggerProperties">The properties that trigger this rule when changed.</param>
    /// <returns>The created rule instance.</returns>
    ActionAsyncFluentRule<T> AddActionAsync(Func<T, Task> func, params Expression<Func<T, object?>>[] triggerProperties);

    /// <summary>
    /// Adds an asynchronous validation rule that executes when the specified property changes.
    /// The function should return an error message string, or an empty/null string if validation passes.
    /// </summary>
    /// <param name="func">The async validation function that returns an error message or empty string.</param>
    /// <param name="triggerProperty">The property that triggers this rule and receives the error message.</param>
    /// <returns>The created rule instance.</returns>
    AsyncFluentRule<T> AddValidationAsync(Func<T, Task<string>> func, Expression<Func<T, object?>> triggerProperty);
}

/// <summary>
/// Factory for creating <see cref="RuleManager{T}"/> instances with proper dependency injection of the attribute-to-rule converter.
/// </summary>
/// <typeparam name="T">The type of validation target the created rule managers will operate on.</typeparam>
public class RuleManagerFactory<T>
    where T : class, IValidateBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RuleManagerFactory{T}"/> class.
    /// </summary>
    /// <param name="attributeToRule">The service that converts validation attributes to rules.</param>
    public RuleManagerFactory(IAttributeToRule attributeToRule)
    {
        this.AttributeToRule = attributeToRule;
    }

    /// <summary>
    /// Gets the attribute-to-rule converter used by this factory.
    /// </summary>
    public IAttributeToRule AttributeToRule { get; }

    /// <summary>
    /// Creates a new rule manager for the specified target.
    /// </summary>
    /// <param name="target">The validation target that the rule manager will operate on.</param>
    /// <param name="propertyInfoList">The list of properties on the target for discovering attribute-based rules.</param>
    /// <returns>A new <see cref="IRuleManager{T}"/> instance configured for the target.</returns>
    public IRuleManager<T> CreateRuleManager(T target, IPropertyInfoList propertyInfoList)
    {
        return new RuleManager<T>(target, propertyInfoList, this.AttributeToRule);
    }
}

/// <summary>
/// Default implementation of <see cref="IRuleManager{T}"/> that manages and executes business rules for a validation target.
/// Handles rule storage, trigger property matching, async rule execution with busy indicators, and validation message management.
/// </summary>
/// <typeparam name="T">The type of validation target this rule manager operates on.</typeparam>
/// <remarks>
/// <para>
/// The rule manager automatically discovers and adds rules from validation attributes on properties.
/// Additional rules can be added using <see cref="AddRule{T1}(IRule{T1})"/> or the fluent methods.
/// </para>
/// <para>
/// When async rules are executing, the affected properties are marked as busy to provide UI feedback.
/// </para>
/// </remarks>
public class RuleManager<T> : IRuleManager<T>
    where T : class, IValidateBase
{
    /// <summary>
    /// Gets the validation target this rule manager operates on.
    /// </summary>
    protected T Target { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RuleManager{T}"/> class.
    /// </summary>
    /// <param name="target">The validation target to manage rules for.</param>
    /// <param name="propertyInfoList">The list of properties for discovering attribute-based rules.</param>
    /// <param name="attributeToRule">The service that converts validation attributes to rules.</param>
    /// <exception cref="TargetIsNullException">Thrown when <paramref name="target"/> is null.</exception>
    public RuleManager(T target, IPropertyInfoList propertyInfoList, IAttributeToRule attributeToRule)
    {
        this.Target = target ?? throw new TargetIsNullException();
        this.AddAttributeRules(attributeToRule, propertyInfoList);
    }

    /// <inheritdoc />
    IEnumerable<IRule> IRuleManager.Rules => this.Rules.Values;

    /// <summary>
    /// The current rule index counter. Used to assign unique indices to rules for message tracking.
    /// Rules are indexed for serialization so broken rules transfer correctly.
    /// This assumes that rules are added in the same order across instances.
    /// </summary>
    protected uint _ruleIndex = 1;

    /// <summary>
    /// Gets the dictionary of rules indexed by their unique index.
    /// </summary>
    private IDictionary<uint, IRule> Rules { get; } = new Dictionary<uint, IRule>();

    /// <summary>
    /// Discovers and adds rules from validation attributes on the target's properties.
    /// </summary>
    /// <param name="attributeToRule">The service that converts attributes to rules.</param>
    /// <param name="propertyInfoList">The list of properties to scan for attributes.</param>
    protected virtual void AddAttributeRules(IAttributeToRule attributeToRule, IPropertyInfoList propertyInfoList)
    {
        var requiredRegisteredProp = propertyInfoList.Properties();

        foreach (var r in requiredRegisteredProp)
        {
            foreach (var a in r.GetCustomAttributes())
            {
                var rule = attributeToRule.GetRule<T>(r, a);
                if (rule != null)
                {
                    this.Rules.Add(this._ruleIndex++, rule);
                    rule.OnRuleAdded(this, this._ruleIndex);
                }
            }
        }
    }

    /// <inheritdoc />
    public void AddRules<T1>(params IRule<T1>[] rules) where T1 : IValidateBase
    {
        foreach (var r in rules) { this.AddRule(r); }
    }

    /// <inheritdoc />
    /// <exception cref="InvalidTargetTypeException">Thrown when the rule's target type is not compatible with this manager's target type.</exception>
    public void AddRule<T1>(IRule<T1> rule) where T1 : IValidateBase
    {
        if (typeof(T1).IsAssignableFrom(typeof(T)))
        {
            this.Rules.Add(this._ruleIndex++, rule);
            rule.OnRuleAdded(this, this._ruleIndex);
        }
        else
        {
            throw new InvalidTargetTypeException($"{typeof(T1).FullName} is not assignable from {typeof(T).FullName}");
        }
    }

    /// <inheritdoc />
    public async Task RunRule<T1>(CancellationToken? token = null) where T1 : IRule
    {
        foreach (var rule in this.Rules)
        {
            if (rule.Value is T1 r)
            {
                await this.RunRule(r, token);
            }
        }
    }

    /// <inheritdoc />
    public ActionAsyncFluentRule<T> AddActionAsync(Func<T, Task> func, params Expression<Func<T, object?>>[] triggerProperties)
    {
        var rule = new ActionAsyncFluentRule<T>(func, triggerProperties);
        this.Rules.Add(this._ruleIndex++, rule);
        rule.OnRuleAdded(this, this._ruleIndex);
        return rule;
    }

    /// <inheritdoc />
    public ActionFluentRule<T> AddAction(Action<T> func, params Expression<Func<T, object?>>[] triggerProperties)
    {
        var rule = new ActionFluentRule<T>(func, triggerProperties);
        this.Rules.Add(this._ruleIndex++, rule);
        rule.OnRuleAdded(this, this._ruleIndex);
        return rule;
    }

    /// <inheritdoc />
    public ValidationFluentRule<T> AddValidation(Func<T, string> func, Expression<Func<T, object?>> triggerProperty)
    {
        var rule = new ValidationFluentRule<T>(func, triggerProperty);
        this.Rules.Add(this._ruleIndex++, rule);
        rule.OnRuleAdded(this, this._ruleIndex);
        return rule;
    }

    /// <inheritdoc />
    public AsyncFluentRule<T> AddValidationAsync(Func<T, Task<string>> func, Expression<Func<T, object?>> triggerProperty)
    {
        var rule = new AsyncFluentRule<T>(func, triggerProperty);
        this.Rules.Add(this._ruleIndex++, rule);
        rule.OnRuleAdded(this, this._ruleIndex);
        return rule;
    }

    /// <inheritdoc />
    public async Task RunRules(string propertyName, CancellationToken? token = null)
    {
        foreach (var rule in this.Rules.Values.Where(r => r.TriggerProperties.Any(t => t.IsMatch(propertyName)))
                                    .OrderBy(r => r.RuleOrder).ToList())
        {
            await this.RunRule(rule, token);
        }
    }

    /// <inheritdoc />
    public async Task RunRules(RunRulesFlag runRules = Neatoo.RunRulesFlag.All, CancellationToken? token = null)
    {
        foreach (var rule in this.Rules.ToList())
        {
            var messages = rule.Value.Messages;
            if (runRules == Neatoo.RunRulesFlag.All ||
                runRules == Neatoo.RunRulesFlag.Self ||
                ((runRules & Neatoo.RunRulesFlag.NotExecuted) == Neatoo.RunRulesFlag.NotExecuted && rule.Value.Executed == false) ||
                ((runRules & Neatoo.RunRulesFlag.Executed) == Neatoo.RunRulesFlag.Executed && rule.Value.Executed) ||
                ((runRules & Neatoo.RunRulesFlag.NoMessages) == Neatoo.RunRulesFlag.NoMessages && messages.Count == 0) ||
                ((runRules & Neatoo.RunRulesFlag.Messages) == Neatoo.RunRulesFlag.Messages && messages.Count > 0))
                await this.RunRule(rule.Value, token);
        }
    }

    /// <inheritdoc />
    /// <exception cref="InvalidRuleTypeException">Thrown when the rule cannot be executed for this target type.</exception>
    public async Task RunRule(IRule r, CancellationToken? token = null)
    {
        if (!this.Rules.Values.Contains(r))
        {
            throw new RuleNotAddedException();
        }

        if (r is IRule rule)
        {
            var uniqueExecIndex = rule.UniqueIndex + Random.Shared.Next(10000, 100000);
            var triggerProperties = rule.TriggerProperties;

            try
            {
                var ruleMessageTask = r.RunRule(this.Target, token);

                if (!ruleMessageTask.IsCompleted)
                {
                    foreach (var triggerProperty in triggerProperties)
                    {
                        if(this.Target.TryGetProperty(triggerProperty.PropertyName, out var targetProperty))
                        {
                            // Allowing null trigger properties that may be on a child target
                            targetProperty.AddMarkedBusy(uniqueExecIndex);
                        }
                    }
                }

                var ruleMessages = (await ruleMessageTask) ?? RuleMessages.None;

                foreach (var propertyName in triggerProperties.Select(t => t.PropertyName).Except(ruleMessages.Select(p => p.PropertyName)))
                {
                    if(this.Target.TryGetProperty(propertyName, out var targetProperty))
                    {
                        // Cast to internal interface to call ClearMessagesForRule
                        if (targetProperty is IValidatePropertyInternal vpInternal)
                        {
                            vpInternal.ClearMessagesForRule(rule.UniqueIndex);
                        }
                    }
                }

                foreach (var ruleMessage in ruleMessages.GroupBy(rm => rm.PropertyName).ToDictionary(g => g.Key, g => g.ToList()))
                {
                    if(this.Target.TryGetProperty(ruleMessage.Key, out var targetProperty))
                    {
                        // Cast to internal interface to set RuleIndex
                        ruleMessage.Value.ForEach(rm =>
                        {
                            if (rm is IRuleMessageInternal rmInternal)
                            {
                                rmInternal.RuleIndex = rule.UniqueIndex;
                            }
                        });

                        // Cast to internal interface to call SetMessagesForRule
                        if (targetProperty is IValidatePropertyInternal vpInternal)
                        {
                            vpInternal.SetMessagesForRule(ruleMessage.Value);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                foreach (var triggerProperty in triggerProperties)
                {
                    if(this.Target.TryGetProperty(triggerProperty.PropertyName, out var targetProperty))
                    {
                        // Cast to internal interface to call SetMessagesForRule
                        if (targetProperty is IValidatePropertyInternal vpInternal)
                        {
                            // Allow children to be trigger properties
                            vpInternal.SetMessagesForRule(triggerProperty.PropertyName.RuleMessages(ex.Message).AsReadOnly());
                        }
                    }
                }

                throw;
            }
            finally
            {
                foreach (var triggerProperty in triggerProperties)
                {
                    if (this.Target.TryGetProperty(triggerProperty.PropertyName, out var targetProperty))
                    {
                        // Allowing null trigger properties that may be on a child target
                        targetProperty.RemoveMarkedBusy(uniqueExecIndex);
                    }
                }
            }
        }
        else
        {
            throw new InvalidRuleTypeException($"{r.GetType().FullName} cannot be executed for {typeof(T).FullName}");
        }

        if (token?.IsCancellationRequested ?? false)
        {
            return;
        }
    }
}

/// <summary>
/// Exception thrown when a rule attempts to change a property that triggered it,
/// which could cause infinite recursion.
/// </summary>
[Serializable]
public class TargetRulePropertyChangeException : RuleException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TargetRulePropertyChangeException"/> class.
    /// </summary>
    public TargetRulePropertyChangeException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="TargetRulePropertyChangeException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public TargetRulePropertyChangeException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="TargetRulePropertyChangeException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="inner">The exception that is the cause of the current exception.</param>
    public TargetRulePropertyChangeException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// Exception thrown when a rule cannot be executed because it is not compatible with the target type.
/// </summary>
[Serializable]
public class InvalidRuleTypeException : RuleException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidRuleTypeException"/> class.
    /// </summary>
    public InvalidRuleTypeException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidRuleTypeException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public InvalidRuleTypeException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidRuleTypeException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="inner">The exception that is the cause of the current exception.</param>
    public InvalidRuleTypeException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// Exception thrown when attempting to add a rule whose target type is not compatible with the rule manager's target type.
/// </summary>
[Serializable]
public class InvalidTargetTypeException : RuleException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidTargetTypeException"/> class.
    /// </summary>
    public InvalidTargetTypeException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidTargetTypeException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public InvalidTargetTypeException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidTargetTypeException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="inner">The exception that is the cause of the current exception.</param>
    public InvalidTargetTypeException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// Exception thrown when a <see cref="RuleManager{T}"/> is created with a null target.
/// </summary>
[Serializable]
public class TargetIsNullException : RuleException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TargetIsNullException"/> class.
    /// </summary>
    public TargetIsNullException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="TargetIsNullException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public TargetIsNullException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="TargetIsNullException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="inner">The exception that is the cause of the current exception.</param>
    public TargetIsNullException(string message, Exception inner) : base(message, inner) { }
}