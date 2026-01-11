using Neatoo.Rules.Rules;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
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
    /// <param name="sourceExpression">Captured automatically by CallerArgumentExpression for stable rule ID.</param>
    void AddRule<T>(IRule<T> rule, [CallerArgumentExpression(nameof(rule))] string? sourceExpression = null) where T : IValidateBase;

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
    /// Adds a synchronous action rule that executes when the specified property changes.
    /// Use this for rules that perform side effects like calculating derived values.
    /// </summary>
    /// <param name="func">The action to execute.</param>
    /// <param name="triggerProperty">The property that triggers this rule when changed.</param>
    /// <param name="sourceExpression">Captured automatically by CallerArgumentExpression for stable rule ID.</param>
    /// <returns>The created rule instance.</returns>
    /// <remarks>
    /// <para>
    /// Multiple overloads exist (1, 2, 3 triggers + array) because <c>CallerArgumentExpression</c>
    /// is incompatible with <c>params</c> arrays. C# requires <c>params</c> to be the last parameter,
    /// but <c>CallerArgumentExpression</c> must come after the parameter it captures.
    /// </para>
    /// <para>
    /// For 4+ trigger properties, use the array overload with an explicit array:
    /// <code>AddAction(t => ..., new[] { t => t.A, t => t.B, t => t.C, t => t.D })</code>
    /// </para>
    /// </remarks>
    ActionFluentRule<T> AddAction(
        Action<T> func,
        Expression<Func<T, object?>> triggerProperty,
        [CallerArgumentExpression(nameof(func))] string? sourceExpression = null);

    /// <inheritdoc cref="AddAction(Action{T}, Expression{Func{T, object?}}, string?)"/>
    ActionFluentRule<T> AddAction(
        Action<T> func,
        Expression<Func<T, object?>> triggerProperty1,
        Expression<Func<T, object?>> triggerProperty2,
        [CallerArgumentExpression(nameof(func))] string? sourceExpression = null);

    /// <inheritdoc cref="AddAction(Action{T}, Expression{Func{T, object?}}, string?)"/>
    ActionFluentRule<T> AddAction(
        Action<T> func,
        Expression<Func<T, object?>> triggerProperty1,
        Expression<Func<T, object?>> triggerProperty2,
        Expression<Func<T, object?>> triggerProperty3,
        [CallerArgumentExpression(nameof(func))] string? sourceExpression = null);

    /// <summary>
    /// Adds a synchronous action rule that executes when the specified properties change.
    /// Use this for rules that perform side effects like calculating derived values.
    /// </summary>
    /// <param name="func">The action to execute.</param>
    /// <param name="triggerProperties">The properties that trigger this rule when changed. Must be an explicit array, not <c>params</c>.</param>
    /// <param name="sourceExpression">Captured automatically by CallerArgumentExpression for stable rule ID.</param>
    /// <returns>The created rule instance.</returns>
    /// <remarks>
    /// Use this overload when you have more than 3 trigger properties.
    /// You must explicitly create the array:
    /// <code>
    /// RuleManager.AddAction(
    ///     t => t.Summary = Calculate(t),
    ///     new[] { t => t.A, t => t.B, t => t.C, t => t.D });
    /// </code>
    /// This is required because <c>params</c> is incompatible with <c>CallerArgumentExpression</c>.
    /// </remarks>
    ActionFluentRule<T> AddAction(
        Action<T> func,
        Expression<Func<T, object?>>[] triggerProperties,
        [CallerArgumentExpression(nameof(func))] string? sourceExpression = null);

    /// <summary>
    /// Adds a synchronous validation rule that executes when the specified property changes.
    /// The function should return an error message string, or an empty/null string if validation passes.
    /// </summary>
    /// <param name="func">The validation function that returns an error message or empty string.</param>
    /// <param name="triggerProperty">The property that triggers this rule and receives the error message.</param>
    /// <param name="sourceExpression">Captured automatically by CallerArgumentExpression for stable rule ID.</param>
    /// <returns>The created rule instance.</returns>
    ValidationFluentRule<T> AddValidation(Func<T, string> func, Expression<Func<T, object?>> triggerProperty, [CallerArgumentExpression(nameof(func))] string? sourceExpression = null);

    /// <summary>
    /// Adds an asynchronous action rule that executes when the specified property changes.
    /// Use this for rules that perform async side effects like calling external services.
    /// </summary>
    /// <param name="func">The async function to execute.</param>
    /// <param name="triggerProperty">The property that triggers this rule when changed.</param>
    /// <param name="sourceExpression">Captured automatically by CallerArgumentExpression for stable rule ID.</param>
    /// <returns>The created rule instance.</returns>
    /// <remarks>
    /// <para>
    /// Multiple overloads exist (1, 2, 3 triggers + array) because <c>CallerArgumentExpression</c>
    /// is incompatible with <c>params</c> arrays. C# requires <c>params</c> to be the last parameter,
    /// but <c>CallerArgumentExpression</c> must come after the parameter it captures.
    /// </para>
    /// <para>
    /// For 4+ trigger properties, use the array overload with an explicit array:
    /// <code>AddActionAsync(async t => ..., new[] { t => t.A, t => t.B, t => t.C, t => t.D })</code>
    /// </para>
    /// </remarks>
    ActionAsyncFluentRule<T> AddActionAsync(
        Func<T, Task> func,
        Expression<Func<T, object?>> triggerProperty,
        [CallerArgumentExpression(nameof(func))] string? sourceExpression = null);

    /// <inheritdoc cref="AddActionAsync(Func{T, Task}, Expression{Func{T, object?}}, string?)"/>
    ActionAsyncFluentRule<T> AddActionAsync(
        Func<T, Task> func,
        Expression<Func<T, object?>> triggerProperty1,
        Expression<Func<T, object?>> triggerProperty2,
        [CallerArgumentExpression(nameof(func))] string? sourceExpression = null);

    /// <inheritdoc cref="AddActionAsync(Func{T, Task}, Expression{Func{T, object?}}, string?)"/>
    ActionAsyncFluentRule<T> AddActionAsync(
        Func<T, Task> func,
        Expression<Func<T, object?>> triggerProperty1,
        Expression<Func<T, object?>> triggerProperty2,
        Expression<Func<T, object?>> triggerProperty3,
        [CallerArgumentExpression(nameof(func))] string? sourceExpression = null);

    /// <summary>
    /// Adds an asynchronous action rule that executes when the specified properties change.
    /// Use this for rules that perform async side effects like calling external services.
    /// </summary>
    /// <param name="func">The async function to execute.</param>
    /// <param name="triggerProperties">The properties that trigger this rule when changed. Must be an explicit array, not <c>params</c>.</param>
    /// <param name="sourceExpression">Captured automatically by CallerArgumentExpression for stable rule ID.</param>
    /// <returns>The created rule instance.</returns>
    /// <remarks>
    /// Use this overload when you have more than 3 trigger properties.
    /// You must explicitly create the array:
    /// <code>
    /// RuleManager.AddActionAsync(
    ///     async t => await CalculateAsync(t),
    ///     new[] { t => t.A, t => t.B, t => t.C, t => t.D });
    /// </code>
    /// This is required because <c>params</c> is incompatible with <c>CallerArgumentExpression</c>.
    /// </remarks>
    ActionAsyncFluentRule<T> AddActionAsync(
        Func<T, Task> func,
        Expression<Func<T, object?>>[] triggerProperties,
        [CallerArgumentExpression(nameof(func))] string? sourceExpression = null);

    /// <summary>
    /// Adds an asynchronous validation rule that executes when the specified property changes.
    /// The function should return an error message string, or an empty/null string if validation passes.
    /// </summary>
    /// <param name="func">The async validation function that returns an error message or empty string.</param>
    /// <param name="triggerProperty">The property that triggers this rule and receives the error message.</param>
    /// <param name="sourceExpression">Captured automatically by CallerArgumentExpression for stable rule ID.</param>
    /// <returns>The created rule instance.</returns>
    AsyncFluentRule<T> AddValidationAsync(Func<T, Task<string>> func, Expression<Func<T, object?>> triggerProperty, [CallerArgumentExpression(nameof(func))] string? sourceExpression = null);

    /// <summary>
    /// Adds an asynchronous action rule with cancellation support that executes when the specified property changes.
    /// Use this for rules that perform async side effects and need to respond to cancellation.
    /// </summary>
    /// <param name="func">The async function to execute, receiving the target and cancellation token.</param>
    /// <param name="triggerProperty">The property that triggers this rule when changed.</param>
    /// <param name="sourceExpression">Captured automatically by CallerArgumentExpression for stable rule ID.</param>
    /// <returns>The created rule instance.</returns>
    /// <remarks>
    /// <para>
    /// Multiple overloads exist (1, 2, 3 triggers + array) because <c>CallerArgumentExpression</c>
    /// is incompatible with <c>params</c> arrays. C# requires <c>params</c> to be the last parameter,
    /// but <c>CallerArgumentExpression</c> must come after the parameter it captures.
    /// </para>
    /// <para>
    /// For 4+ trigger properties, use the array overload with an explicit array:
    /// <code>AddActionAsync(async (t, ct) => ..., new[] { t => t.A, t => t.B, t => t.C, t => t.D })</code>
    /// </para>
    /// </remarks>
    ActionAsyncFluentRuleWithToken<T> AddActionAsync(
        Func<T, CancellationToken, Task> func,
        Expression<Func<T, object?>> triggerProperty,
        [CallerArgumentExpression(nameof(func))] string? sourceExpression = null);

    /// <inheritdoc cref="AddActionAsync(Func{T, CancellationToken, Task}, Expression{Func{T, object?}}, string?)"/>
    ActionAsyncFluentRuleWithToken<T> AddActionAsync(
        Func<T, CancellationToken, Task> func,
        Expression<Func<T, object?>> triggerProperty1,
        Expression<Func<T, object?>> triggerProperty2,
        [CallerArgumentExpression(nameof(func))] string? sourceExpression = null);

    /// <inheritdoc cref="AddActionAsync(Func{T, CancellationToken, Task}, Expression{Func{T, object?}}, string?)"/>
    ActionAsyncFluentRuleWithToken<T> AddActionAsync(
        Func<T, CancellationToken, Task> func,
        Expression<Func<T, object?>> triggerProperty1,
        Expression<Func<T, object?>> triggerProperty2,
        Expression<Func<T, object?>> triggerProperty3,
        [CallerArgumentExpression(nameof(func))] string? sourceExpression = null);

    /// <summary>
    /// Adds an asynchronous action rule with cancellation support that executes when the specified properties change.
    /// Use this for rules that perform async side effects and need to respond to cancellation.
    /// </summary>
    /// <param name="func">The async function to execute, receiving the target and cancellation token.</param>
    /// <param name="triggerProperties">The properties that trigger this rule when changed. Must be an explicit array, not <c>params</c>.</param>
    /// <param name="sourceExpression">Captured automatically by CallerArgumentExpression for stable rule ID.</param>
    /// <returns>The created rule instance.</returns>
    /// <remarks>
    /// Use this overload when you have more than 3 trigger properties.
    /// You must explicitly create the array:
    /// <code>
    /// RuleManager.AddActionAsync(
    ///     async (t, ct) => await CalculateAsync(t, ct),
    ///     new[] { t => t.A, t => t.B, t => t.C, t => t.D });
    /// </code>
    /// This is required because <c>params</c> is incompatible with <c>CallerArgumentExpression</c>.
    /// </remarks>
    ActionAsyncFluentRuleWithToken<T> AddActionAsync(
        Func<T, CancellationToken, Task> func,
        Expression<Func<T, object?>>[] triggerProperties,
        [CallerArgumentExpression(nameof(func))] string? sourceExpression = null);

    /// <summary>
    /// Adds an asynchronous validation rule with cancellation support that executes when the specified property changes.
    /// The function should return an error message string, or an empty/null string if validation passes.
    /// </summary>
    /// <param name="func">The async validation function that receives the target and cancellation token, returning an error message or empty string.</param>
    /// <param name="triggerProperty">The property that triggers this rule and receives the error message.</param>
    /// <param name="sourceExpression">Captured automatically by CallerArgumentExpression for stable rule ID.</param>
    /// <returns>The created rule instance.</returns>
    AsyncFluentRuleWithToken<T> AddValidationAsync(Func<T, CancellationToken, Task<string>> func, Expression<Func<T, object?>> triggerProperty, [CallerArgumentExpression(nameof(func))] string? sourceExpression = null);
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
    /// Global counter for generating unique execution IDs for busy tracking.
    /// Each rule execution gets a unique ID to track which executions are in-flight.
    /// </summary>
    private static long _nextExecId = 0;

    /// <summary>
    /// Gets the dictionary of rules indexed by their stable rule ID.
    /// </summary>
    private IDictionary<uint, IRule> Rules { get; } = new Dictionary<uint, IRule>();

    /// <summary>
    /// Registers a rule with the rule manager, assigning it a stable rule ID.
    /// The ID is determined by the source expression captured via CallerArgumentExpression.
    /// </summary>
    /// <typeparam name="TRule">The rule type.</typeparam>
    /// <param name="rule">The rule to register.</param>
    /// <param name="sourceExpression">The source expression captured by CallerArgumentExpression.</param>
    /// <returns>The registered rule.</returns>
    private TRule RegisterRule<TRule>(TRule rule, string sourceExpression) where TRule : IRule
    {
        var ruleId = ((IValidateBaseInternal)this.Target).GetRuleId(sourceExpression);
        this.Rules.Add(ruleId, rule);
        rule.OnRuleAdded(this, ruleId);
        return rule;
    }

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
                    // Attribute rules get a deterministic ID based on attribute type and property name
                    var sourceExpression = $"{a.GetType().Name}_{r.Name}";
                    this.RegisterRule(rule, sourceExpression);
                }
            }
        }
    }

    /// <inheritdoc />
    /// <exception cref="InvalidTargetTypeException">Thrown when the rule's target type is not compatible with this manager's target type.</exception>
    public void AddRule<T1>(
        IRule<T1> rule,
        [CallerArgumentExpression(nameof(rule))] string? sourceExpression = null) where T1 : IValidateBase
    {
        this.AddRuleInternal(rule, sourceExpression ?? "unknown");
    }

    private void AddRuleInternal<T1>(IRule<T1> rule, string sourceExpression) where T1 : IValidateBase
    {
        if (typeof(T1).IsAssignableFrom(typeof(T)))
        {
            this.RegisterRule(rule, sourceExpression);
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

    #region AddAction overloads

    /// <inheritdoc />
    public ActionFluentRule<T> AddAction(
        Action<T> func,
        Expression<Func<T, object?>> triggerProperty,
        [CallerArgumentExpression(nameof(func))] string? sourceExpression = null)
    {
        return this.RegisterRule(new ActionFluentRule<T>(func, [triggerProperty]), sourceExpression ?? "unknown");
    }

    /// <inheritdoc />
    public ActionFluentRule<T> AddAction(
        Action<T> func,
        Expression<Func<T, object?>> triggerProperty1,
        Expression<Func<T, object?>> triggerProperty2,
        [CallerArgumentExpression(nameof(func))] string? sourceExpression = null)
    {
        return this.RegisterRule(new ActionFluentRule<T>(func, [triggerProperty1, triggerProperty2]), sourceExpression ?? "unknown");
    }

    /// <inheritdoc />
    public ActionFluentRule<T> AddAction(
        Action<T> func,
        Expression<Func<T, object?>> triggerProperty1,
        Expression<Func<T, object?>> triggerProperty2,
        Expression<Func<T, object?>> triggerProperty3,
        [CallerArgumentExpression(nameof(func))] string? sourceExpression = null)
    {
        return this.RegisterRule(new ActionFluentRule<T>(func, [triggerProperty1, triggerProperty2, triggerProperty3]), sourceExpression ?? "unknown");
    }

    /// <inheritdoc />
    public ActionFluentRule<T> AddAction(
        Action<T> func,
        Expression<Func<T, object?>>[] triggerProperties,
        [CallerArgumentExpression(nameof(func))] string? sourceExpression = null)
    {
        return this.RegisterRule(new ActionFluentRule<T>(func, triggerProperties), sourceExpression ?? "unknown");
    }

    #endregion

    #region AddActionAsync overloads

    /// <inheritdoc />
    public ActionAsyncFluentRule<T> AddActionAsync(
        Func<T, Task> func,
        Expression<Func<T, object?>> triggerProperty,
        [CallerArgumentExpression(nameof(func))] string? sourceExpression = null)
    {
        return this.RegisterRule(new ActionAsyncFluentRule<T>(func, [triggerProperty]), sourceExpression ?? "unknown");
    }

    /// <inheritdoc />
    public ActionAsyncFluentRule<T> AddActionAsync(
        Func<T, Task> func,
        Expression<Func<T, object?>> triggerProperty1,
        Expression<Func<T, object?>> triggerProperty2,
        [CallerArgumentExpression(nameof(func))] string? sourceExpression = null)
    {
        return this.RegisterRule(new ActionAsyncFluentRule<T>(func, [triggerProperty1, triggerProperty2]), sourceExpression ?? "unknown");
    }

    /// <inheritdoc />
    public ActionAsyncFluentRule<T> AddActionAsync(
        Func<T, Task> func,
        Expression<Func<T, object?>> triggerProperty1,
        Expression<Func<T, object?>> triggerProperty2,
        Expression<Func<T, object?>> triggerProperty3,
        [CallerArgumentExpression(nameof(func))] string? sourceExpression = null)
    {
        return this.RegisterRule(new ActionAsyncFluentRule<T>(func, [triggerProperty1, triggerProperty2, triggerProperty3]), sourceExpression ?? "unknown");
    }

    /// <inheritdoc />
    public ActionAsyncFluentRule<T> AddActionAsync(
        Func<T, Task> func,
        Expression<Func<T, object?>>[] triggerProperties,
        [CallerArgumentExpression(nameof(func))] string? sourceExpression = null)
    {
        return this.RegisterRule(new ActionAsyncFluentRule<T>(func, triggerProperties), sourceExpression ?? "unknown");
    }

    #endregion

    /// <inheritdoc />
    public ValidationFluentRule<T> AddValidation(
        Func<T, string> func,
        Expression<Func<T, object?>> triggerProperty,
        [CallerArgumentExpression(nameof(func))] string? sourceExpression = null)
    {
        return this.RegisterRule(new ValidationFluentRule<T>(func, triggerProperty), sourceExpression ?? "unknown");
    }

    /// <inheritdoc />
    public AsyncFluentRule<T> AddValidationAsync(
        Func<T, Task<string>> func,
        Expression<Func<T, object?>> triggerProperty,
        [CallerArgumentExpression(nameof(func))] string? sourceExpression = null)
    {
        return this.RegisterRule(new AsyncFluentRule<T>(func, triggerProperty), sourceExpression ?? "unknown");
    }

    #region AddActionAsync with CancellationToken overloads

    /// <inheritdoc />
    public ActionAsyncFluentRuleWithToken<T> AddActionAsync(
        Func<T, CancellationToken, Task> func,
        Expression<Func<T, object?>> triggerProperty,
        [CallerArgumentExpression(nameof(func))] string? sourceExpression = null)
    {
        return this.RegisterRule(new ActionAsyncFluentRuleWithToken<T>(func, [triggerProperty]), sourceExpression ?? "unknown");
    }

    /// <inheritdoc />
    public ActionAsyncFluentRuleWithToken<T> AddActionAsync(
        Func<T, CancellationToken, Task> func,
        Expression<Func<T, object?>> triggerProperty1,
        Expression<Func<T, object?>> triggerProperty2,
        [CallerArgumentExpression(nameof(func))] string? sourceExpression = null)
    {
        return this.RegisterRule(new ActionAsyncFluentRuleWithToken<T>(func, [triggerProperty1, triggerProperty2]), sourceExpression ?? "unknown");
    }

    /// <inheritdoc />
    public ActionAsyncFluentRuleWithToken<T> AddActionAsync(
        Func<T, CancellationToken, Task> func,
        Expression<Func<T, object?>> triggerProperty1,
        Expression<Func<T, object?>> triggerProperty2,
        Expression<Func<T, object?>> triggerProperty3,
        [CallerArgumentExpression(nameof(func))] string? sourceExpression = null)
    {
        return this.RegisterRule(new ActionAsyncFluentRuleWithToken<T>(func, [triggerProperty1, triggerProperty2, triggerProperty3]), sourceExpression ?? "unknown");
    }

    /// <inheritdoc />
    public ActionAsyncFluentRuleWithToken<T> AddActionAsync(
        Func<T, CancellationToken, Task> func,
        Expression<Func<T, object?>>[] triggerProperties,
        [CallerArgumentExpression(nameof(func))] string? sourceExpression = null)
    {
        return this.RegisterRule(new ActionAsyncFluentRuleWithToken<T>(func, triggerProperties), sourceExpression ?? "unknown");
    }

    #endregion

    /// <inheritdoc />
    public AsyncFluentRuleWithToken<T> AddValidationAsync(
        Func<T, CancellationToken, Task<string>> func,
        Expression<Func<T, object?>> triggerProperty,
        [CallerArgumentExpression(nameof(func))] string? sourceExpression = null)
    {
        return this.RegisterRule(new AsyncFluentRuleWithToken<T>(func, triggerProperty), sourceExpression ?? "unknown");
    }

    /// <inheritdoc />
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested before a rule executes.</exception>
    public async Task RunRules(string propertyName, CancellationToken? token = null)
    {
        foreach (var rule in this.Rules.Values.Where(r => r.TriggerProperties.Any(t => t.IsMatch(propertyName)))
                                    .OrderBy(r => r.RuleOrder).ToList())
        {
            if (token?.IsCancellationRequested == true)
            {
                throw new OperationCanceledException(token.Value);
            }

            await this.RunRule(rule, token);
        }
    }

    /// <inheritdoc />
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested before a rule executes.</exception>
    public async Task RunRules(RunRulesFlag runRules = Neatoo.RunRulesFlag.All, CancellationToken? token = null)
    {
        foreach (var rule in this.Rules.ToList())
        {
            if (token?.IsCancellationRequested == true)
            {
                throw new OperationCanceledException(token.Value);
            }

            if (ShouldRunRule(rule.Value, runRules))
            {
                await this.RunRule(rule.Value, token);
            }
        }
    }

    /// <summary>
    /// Determines whether a rule should be executed based on the specified flags.
    /// </summary>
    /// <param name="rule">The rule to evaluate.</param>
    /// <param name="flags">The flags indicating which rules should run.</param>
    /// <returns><c>true</c> if the rule should be executed; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <para>Flag evaluation logic:</para>
    /// <list type="bullet">
    /// <item><see cref="RunRulesFlag.All"/> or <see cref="RunRulesFlag.Self"/>: Always run</item>
    /// <item><see cref="RunRulesFlag.NotExecuted"/>: Run if rule has not been executed</item>
    /// <item><see cref="RunRulesFlag.Executed"/>: Run if rule has been executed</item>
    /// <item><see cref="RunRulesFlag.NoMessages"/>: Run if rule has no validation messages</item>
    /// <item><see cref="RunRulesFlag.Messages"/>: Run if rule has validation messages</item>
    /// </list>
    /// <para>Flags can be combined. A rule runs if ANY of the specified conditions match.</para>
    /// <para>
    /// <b>Known issue:</b> The Messages and NoMessages flags check <see cref="IRule.Messages"/>,
    /// but this property is never populated. These flags effectively don't work as intended.
    /// NoMessages always matches (messages is always empty), Messages never matches.
    /// </para>
    /// </remarks>
    public static bool ShouldRunRule(IRule rule, RunRulesFlag flags)
    {
        // All or Self flags mean run everything
        if (flags == RunRulesFlag.All || flags == RunRulesFlag.Self)
        {
            return true;
        }

        // Check individual flags - rule runs if ANY condition matches
        if ((flags & RunRulesFlag.NotExecuted) == RunRulesFlag.NotExecuted && !rule.Executed)
        {
            return true;
        }

        if ((flags & RunRulesFlag.Executed) == RunRulesFlag.Executed && rule.Executed)
        {
            return true;
        }

        var messageCount = rule.Messages.Count;

        if ((flags & RunRulesFlag.NoMessages) == RunRulesFlag.NoMessages && messageCount == 0)
        {
            return true;
        }

        if ((flags & RunRulesFlag.Messages) == RunRulesFlag.Messages && messageCount > 0)
        {
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested before the rule executes.</exception>
    public async Task RunRule(IRule rule, CancellationToken? token = null)
    {
        if (token?.IsCancellationRequested == true)
        {
            throw new OperationCanceledException(token.Value);
        }

        if (!this.Rules.Values.Contains(rule))
        {
            throw new RuleNotAddedException();
        }

        // Create unique ID for this execution (for busy tracking, not serialized).
        // Atomic increment guarantees no collisions between concurrent executions.
        var uniqueExecIndex = Interlocked.Increment(ref _nextExecId);
        var triggerProperties = rule.TriggerProperties;

        try
        {
            var ruleMessageTask = rule.RunRule(this.Target, token);

            if (!ruleMessageTask.IsCompleted)
            {
                foreach (var triggerProperty in triggerProperties)
                {
                    if (this.Target.TryGetProperty(triggerProperty.PropertyName, out var targetProperty))
                    {
                        // Allowing null trigger properties that may be on a child target
                        targetProperty.AddMarkedBusy(uniqueExecIndex);
                    }
                }
            }

            var ruleMessages = (await ruleMessageTask) ?? RuleMessages.None;

            foreach (var propertyName in triggerProperties.Select(t => t.PropertyName).Except(ruleMessages.Select(p => p.PropertyName)))
            {
                if (this.Target.TryGetProperty(propertyName, out var targetProperty))
                {
                    // Cast to internal interface to call ClearMessagesForRule
                    if (targetProperty is IValidatePropertyInternal vpInternal)
                    {
                        vpInternal.ClearMessagesForRule(rule.RuleId);
                    }
                }
            }

            foreach (var ruleMessage in ruleMessages.GroupBy(rm => rm.PropertyName).ToDictionary(g => g.Key, g => g.ToList()))
            {
                if (this.Target.TryGetProperty(ruleMessage.Key, out var targetProperty))
                {
                    // Cast to internal interface to set RuleId
                    ruleMessage.Value.ForEach(rm =>
                    {
                        if (rm is IRuleMessageInternal rmInternal)
                        {
                            rmInternal.RuleId = rule.RuleId;
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
                if (this.Target.TryGetProperty(triggerProperty.PropertyName, out var targetProperty))
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