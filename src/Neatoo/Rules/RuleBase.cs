using System.Diagnostics;
using System.Linq.Expressions;

namespace Neatoo.Rules;

/// <summary>
/// Defines the contract for a business rule that can be executed against a validation target.
/// Rules are the core mechanism for implementing validation and business logic in Neatoo.
/// </summary>
/// <remarks>
/// Rules are managed by <see cref="IRuleManager"/> and are triggered when properties change.
/// Each rule can produce validation messages that are associated with specific properties.
/// </remarks>
public interface IRule
{
    /// <summary>
    /// Gets a value indicating whether this rule has been executed at least once.
    /// </summary>
    /// <value><see langword="true"/> if the rule has been executed; otherwise, <see langword="false"/>.</value>
    bool Executed { get; }

    /// <summary>
    /// Gets the execution order for this rule. Lower values execute first.
    /// </summary>
    /// <value>The rule execution order. Default is 1.</value>
    int RuleOrder { get; }

    /// <summary>
    /// Gets the unique index assigned to this rule by the <see cref="IRuleManager"/>.
    /// Used to track which rule produced specific validation messages.
    /// </summary>
    uint UniqueIndex { get; }

    /// <summary>
    /// Gets the collection of validation messages produced by the last execution of this rule.
    /// </summary>
    IReadOnlyList<IRuleMessage> Messages { get; }

    /// <summary>
    /// Gets the collection of properties that trigger this rule when their values change.
    /// </summary>
    IReadOnlyList<ITriggerProperty> TriggerProperties { get; }

    /// <summary>
    /// Executes the rule against the specified validation target.
    /// </summary>
    /// <param name="target">The validation target to run the rule against.</param>
    /// <param name="token">Optional cancellation token to cancel rule execution.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains
    /// the validation messages produced by the rule, or an empty collection if validation passed.</returns>
    Task<IRuleMessages> RunRule(IValidateBase target, CancellationToken? token = null);

    /// <summary>
    /// Called when the rule is added to a <see cref="IRuleManager"/> to initialize the rule's unique index.
    /// </summary>
    /// <param name="ruleManager">The rule manager that this rule was added to.</param>
    /// <param name="uniqueIndex">The unique index assigned to this rule.</param>
    void OnRuleAdded(IRuleManager ruleManager, uint uniqueIndex);
}

/// <summary>
/// Defines a strongly-typed business rule that operates on a specific validation target type.
/// This interface enables type-safe rule execution while maintaining compatibility with the
/// non-generic <see cref="IRule"/> interface through contravariance.
/// </summary>
/// <typeparam name="T">The type of validation target this rule operates on. Must implement <see cref="IValidateBase"/>.</typeparam>
/// <remarks>
/// The contravariance of this interface allows <see cref="IRuleManager"/> to execute rules
/// even when the generic type parameters differ, as long as they are compatible.
/// </remarks>
public interface IRule<T> : IRule
    where T : IValidateBase
{
    /// <summary>
    /// Executes the rule against the specified strongly-typed validation target.
    /// </summary>
    /// <param name="target">The validation target to run the rule against.</param>
    /// <param name="token">Optional cancellation token to cancel rule execution.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains
    /// the validation messages produced by the rule, or an empty collection if validation passed.</returns>
    Task<IRuleMessages> RunRule(T target, CancellationToken? token = null);
}

/// <summary>
/// Abstract base class for implementing asynchronous business rules in Neatoo.
/// Provides the foundation for rules that perform validation or business logic that may require async operations.
/// </summary>
/// <typeparam name="T">The type of validation target this rule operates on. Must be a class implementing <see cref="IValidateBase"/>.</typeparam>
/// <remarks>
/// <para>
/// Rules are triggered when the properties specified in <see cref="TriggerProperties"/> change.
/// Override the <see cref="Execute(T, CancellationToken?)"/> method to implement your rule logic.
/// </para>
/// <para>
/// For synchronous rules, consider using <see cref="RuleBase{T}"/> instead.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class UniqueEmailRule : AsyncRuleBase&lt;Customer&gt;
/// {
///     private readonly IEmailService _emailService;
///
///     public UniqueEmailRule(IEmailService emailService)
///         : base(c => c.Email)
///     {
///         _emailService = emailService;
///     }
///
///     protected override async Task&lt;IRuleMessages&gt; Execute(Customer target, CancellationToken? token = null)
///     {
///         if (await _emailService.EmailExistsAsync(target.Email, token ?? CancellationToken.None))
///         {
///             return (nameof(Customer.Email), "Email already exists").AsRuleMessages();
///         }
///         return None;
///     }
/// }
/// </code>
/// </example>
public abstract class AsyncRuleBase<T> : IRule<T>
    where T : class, IValidateBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncRuleBase{T}"/> class with no trigger properties.
    /// Trigger properties can be added later using <see cref="AddTriggerProperties(Expression{Func{T, object?}}[])"/>.
    /// </summary>
    protected AsyncRuleBase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncRuleBase{T}"/> class with the specified trigger properties.
    /// </summary>
    /// <param name="triggerOnPropertyNames">Property expressions that define which properties trigger this rule when changed.</param>
    public AsyncRuleBase(params Expression<Func<T, object?>>[] triggerOnPropertyNames) : this(triggerOnPropertyNames.AsEnumerable()) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncRuleBase{T}"/> class with the specified trigger properties.
    /// </summary>
    /// <param name="triggerOnPropertyNames">Property expressions that define which properties trigger this rule when changed.</param>
    public AsyncRuleBase(IEnumerable<Expression<Func<T, object?>>> triggerOnPropertyNames) : this()
    {
        this.TriggerProperties.AddRange(triggerOnPropertyNames.Select(propertyName => new TriggerProperty<T>(propertyName)));
    }

    /// <summary>
    /// Gets or sets the unique index for this rule. For static rules, set this to a unique value
    /// to ensure proper message tracking across instances.
    /// </summary>
    public uint UniqueIndex { get; protected set; }

    /// <summary>
    /// Gets an empty <see cref="RuleMessages"/> collection, representing no validation errors.
    /// Use this as the return value when validation passes.
    /// </summary>
    protected RuleMessages None = RuleMessages.None;

    /// <summary>
    /// Gets or sets the execution order for this rule. Lower values execute first. Default is 1.
    /// </summary>
    public int RuleOrder { get; protected set; } = 1;

    /// <summary>
    /// Gets a value indicating whether this rule has been executed at least once.
    /// </summary>
    public bool Executed { get; protected set; }

    /// <inheritdoc />
    IReadOnlyList<ITriggerProperty> IRule.TriggerProperties => this.TriggerProperties.AsReadOnly();

    /// <summary>
    /// Gets the mutable list of trigger properties for this rule.
    /// Add properties using <see cref="AddTriggerProperties(Expression{Func{T, object?}}[])"/>.
    /// </summary>
    protected List<ITriggerProperty> TriggerProperties { get; } = new List<ITriggerProperty>();

    /// <summary>
    /// Adds trigger properties to this rule using property expressions.
    /// </summary>
    /// <param name="triggerOnExpression">Property expressions identifying which properties should trigger this rule.</param>
    protected virtual void AddTriggerProperties(params Expression<Func<T, object?>>[] triggerOnExpression)
    {
        this.TriggerProperties.AddRange(triggerOnExpression.Select(expression => new TriggerProperty<T>(expression)));
    }

    /// <summary>
    /// Adds trigger properties to this rule using existing <see cref="ITriggerProperty"/> instances.
    /// </summary>
    /// <param name="triggerProperties">The trigger properties to add.</param>
    protected virtual void AddTriggerProperties(params ITriggerProperty[] triggerProperties)
    {
        this.TriggerProperties.AddRange(triggerProperties);
    }

    /// <summary>
    /// When overridden in a derived class, executes the rule logic against the specified target.
    /// </summary>
    /// <param name="t">The validation target to validate.</param>
    /// <param name="token">Optional cancellation token to cancel the operation.</param>
    /// <returns>A task containing the validation messages, or <see cref="None"/> if validation passed.</returns>
    protected abstract Task<IRuleMessages> Execute(T t, CancellationToken? token = null);

    /// <summary>
    /// Gets or sets the messages from the previous execution of this rule.
    /// </summary>
    protected IRuleMessages? PreviousMessages { get; set; }

    /// <inheritdoc />
    public IReadOnlyList<IRuleMessage> Messages => this.PreviousMessages?.ToList() ?? [];

    /// <inheritdoc />
    public virtual Task<IRuleMessages> RunRule(IValidateBase target, CancellationToken? token = null)
    {
        var typedTarget = target as T;

        if (typedTarget == null)
        {
            throw new InvalidTargetTypeException($"{target.GetType().Name} is not of type {typeof(T).Name}");
        }

        return this.RunRule(typedTarget, token);
    }

    /// <inheritdoc />
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested before execution.</exception>
    public virtual Task<IRuleMessages> RunRule(T target, CancellationToken? token = null)
    {
        if (token?.IsCancellationRequested == true)
        {
            throw new OperationCanceledException(token.Value);
        }

        this.Executed = true;
        return this.Execute(target, token);
    }

    /// <inheritdoc />
    public virtual void OnRuleAdded(IRuleManager ruleManager, uint uniqueIndex)
    {
        // Only set if not already assigned. This protects static rules (shared instances)
        // from having their index overwritten when added to multiple RuleManagers.
        if (this.UniqueIndex == default)
        {
            this.UniqueIndex = uniqueIndex;
        }
    }

    /// <summary>
    /// Sets a property value on the target without triggering any rules.
    /// Use this when a rule needs to update a property as a side effect without causing rule recursion.
    /// </summary>
    /// <param name="target">The validation target containing the property.</param>
    /// <param name="triggerProperty">The trigger property identifying which property to set.</param>
    /// <param name="value">The value to set.</param>
    protected void LoadProperty(T target, ITriggerProperty triggerProperty, object? value)
    {
        target[triggerProperty.PropertyName].LoadValue(value);
    }

    /// <summary>
    /// Sets a property value on the target without triggering any rules.
    /// Use this when a rule needs to update a property as a side effect without causing rule recursion.
    /// </summary>
    /// <typeparam name="P">The type of the property value.</typeparam>
    /// <param name="target">The validation target containing the property.</param>
    /// <param name="triggerProperty">The trigger property identifying which property to set.</param>
    /// <param name="value">The value to set.</param>
    protected void LoadProperty<P>(T target, ITriggerProperty triggerProperty, P value)
    {
        target[triggerProperty.PropertyName].LoadValue(value);
    }

    /// <summary>
    /// Sets a property value on the target without triggering any rules.
    /// Use this when a rule needs to update a property as a side effect without causing rule recursion.
    /// </summary>
    /// <typeparam name="P">The type of the property value.</typeparam>
    /// <param name="target">The validation target containing the property.</param>
    /// <param name="expression">A property expression identifying which property to set.</param>
    /// <param name="value">The value to set.</param>
    protected void LoadProperty<P>(T target, Expression<Func<T, object?>> expression, P value)
    {
        var triggerProperty = new TriggerProperty<T>(expression);

        target[triggerProperty.PropertyName].LoadValue(value);
    }
}


/// <summary>
/// Abstract base class for implementing synchronous business rules in Neatoo.
/// Provides a simpler synchronous API compared to <see cref="AsyncRuleBase{T}"/> for rules that do not require async operations.
/// </summary>
/// <typeparam name="T">The type of validation target this rule operates on. Must be a class implementing <see cref="IValidateBase"/>.</typeparam>
/// <remarks>
/// <para>
/// Rules are triggered when the properties specified in <see cref="AsyncRuleBase{T}.TriggerProperties"/> change.
/// Override the <see cref="Execute(T)"/> method to implement your rule logic.
/// </para>
/// <para>
/// For rules that require async operations (database lookups, API calls, etc.), use <see cref="AsyncRuleBase{T}"/> instead.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class AgeValidationRule : RuleBase&lt;Person&gt;
/// {
///     public AgeValidationRule() : base(p => p.Age) { }
///
///     protected override IRuleMessages Execute(Person target)
///     {
///         if (target.Age &lt; 0)
///         {
///             return (nameof(Person.Age), "Age cannot be negative").AsRuleMessages();
///         }
///         if (target.Age &gt; 150)
///         {
///             return (nameof(Person.Age), "Age seems unrealistic").AsRuleMessages();
///         }
///         return None;
///     }
/// }
/// </code>
/// </example>
public abstract class RuleBase<T> : AsyncRuleBase<T>
    where T : class, IValidateBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RuleBase{T}"/> class with no trigger properties.
    /// </summary>
    protected RuleBase() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="RuleBase{T}"/> class with the specified trigger properties.
    /// </summary>
    /// <param name="triggerOnPropertyNames">Property expressions that define which properties trigger this rule when changed.</param>
    protected RuleBase(params Expression<Func<T, object?>>[] triggerOnPropertyNames) : base(triggerOnPropertyNames)
    {
    }

    /// <summary>
    /// When overridden in a derived class, executes the synchronous rule logic against the specified target.
    /// </summary>
    /// <param name="target">The validation target to validate.</param>
    /// <returns>The validation messages, or <see cref="AsyncRuleBase{T}.None"/> if validation passed.</returns>
    protected abstract IRuleMessages Execute(T target);

    /// <inheritdoc />
    /// <remarks>
    /// This method wraps the synchronous <see cref="Execute(T)"/> method in a completed task.
    /// </remarks>
    protected sealed override Task<IRuleMessages> Execute(T target, CancellationToken? token = null)
    {
        return Task.FromResult(this.Execute(target));
    }
}

/// <summary>
/// A fluent rule that executes a synchronous action without producing validation messages.
/// Useful for rules that perform side effects like calculating derived values.
/// </summary>
/// <typeparam name="T">The type of validation target this rule operates on.</typeparam>
/// <remarks>
/// This rule is created through <see cref="IRuleManager{T}.AddAction(Action{T}, Expression{Func{T, object?}}[])"/>
/// and always returns <see cref="RuleMessages.None"/> since it is intended for actions, not validation.
/// </remarks>
/// <example>
/// <code>
/// // In your ValidateBase constructor:
/// RuleManager.AddAction(
///     target => target.FullName = $"{target.FirstName} {target.LastName}",
///     t => t.FirstName,
///     t => t.LastName);
/// </code>
/// </example>
public class ActionFluentRule<T> : RuleBase<T>
where T : class, IValidateBase
{
    private Action<T> ExecuteFunc { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ActionFluentRule{T}"/> class.
    /// </summary>
    /// <param name="execute">The action to execute when the rule is triggered.</param>
    /// <param name="triggerProperties">Property expressions that define which properties trigger this rule when changed.</param>
    public ActionFluentRule(Action<T> execute, params Expression<Func<T, object?>>[] triggerProperties) : base(triggerProperties)
    {
        this.ExecuteFunc = execute;
    }

    /// <inheritdoc />
    protected override IRuleMessages Execute(T target)
    {
        this.ExecuteFunc(target);
        return RuleMessages.None;
    }
}

/// <summary>
/// A fluent rule that executes an asynchronous action without producing validation messages.
/// Useful for rules that perform async side effects like calling external services to update derived values.
/// </summary>
/// <typeparam name="T">The type of validation target this rule operates on.</typeparam>
/// <remarks>
/// This rule is created through <see cref="IRuleManager{T}.AddActionAsync(Func{T, Task}, Expression{Func{T, object?}}[])"/>
/// and always returns <see cref="RuleMessages.None"/> since it is intended for actions, not validation.
/// </remarks>
/// <example>
/// <code>
/// // In your ValidateBase constructor:
/// RuleManager.AddActionAsync(
///     async target => target.TaxRate = await taxService.GetTaxRateAsync(target.ZipCode),
///     t => t.ZipCode);
/// </code>
/// </example>
public class ActionAsyncFluentRule<T> : AsyncRuleBase<T>
    where T : class, IValidateBase
{
    private Func<T, Task> ExecuteFunc { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ActionAsyncFluentRule{T}"/> class.
    /// </summary>
    /// <param name="execute">The async function to execute when the rule is triggered.</param>
    /// <param name="triggerProperties">Property expressions that define which properties trigger this rule when changed.</param>
    public ActionAsyncFluentRule(Func<T, Task> execute, params Expression<Func<T, object?>>[] triggerProperties) : base(triggerProperties)
    {
        this.ExecuteFunc = execute;
    }

    /// <inheritdoc />
    protected override async Task<IRuleMessages> Execute(T target, CancellationToken? token = null)
    {
        await this.ExecuteFunc(target);
        return RuleMessages.None;
    }
}

/// <summary>
/// A fluent rule that executes a synchronous validation function and produces a validation message.
/// The validation function returns a string: an empty or null string indicates success, any other value is the error message.
/// </summary>
/// <typeparam name="T">The type of validation target this rule operates on.</typeparam>
/// <remarks>
/// This rule is created through <see cref="IRuleManager{T}.AddValidation(Func{T, string}, Expression{Func{T, object?}})"/>.
/// The error message is automatically associated with the trigger property.
/// </remarks>
/// <example>
/// <code>
/// // In your ValidateBase constructor:
/// RuleManager.AddValidation(
///     target => string.IsNullOrEmpty(target.Name) ? "Name is required" : "",
///     t => t.Name);
/// </code>
/// </example>
public class ValidationFluentRule<T> : RuleBase<T>
    where T : class, IValidateBase
{
    private Func<T, string> ExecuteFunc { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationFluentRule{T}"/> class.
    /// </summary>
    /// <param name="execute">The validation function that returns an error message or empty string for success.</param>
    /// <param name="triggerProperty">The property expression that triggers this rule and receives any error message.</param>
    public ValidationFluentRule(Func<T, string> execute, Expression<Func<T, object?>> triggerProperty) : base([triggerProperty])
    {
        this.ExecuteFunc = execute;
    }

    /// <inheritdoc />
    protected override IRuleMessages Execute(T target)
    {
        var result = this.ExecuteFunc(target);

        if (string.IsNullOrWhiteSpace(result))
        {
            return RuleMessages.None;
        }
        else
        {
            return (this.TriggerProperties.Single().PropertyName, result).AsRuleMessages();
        }
    }
}

/// <summary>
/// A fluent rule that executes an asynchronous validation function and produces a validation message.
/// The validation function returns a string: an empty or null string indicates success, any other value is the error message.
/// </summary>
/// <typeparam name="T">The type of validation target this rule operates on.</typeparam>
/// <remarks>
/// This rule is created through <see cref="IRuleManager{T}.AddValidationAsync(Func{T, Task{string}}, Expression{Func{T, object?}})"/>.
/// The error message is automatically associated with the trigger property.
/// </remarks>
/// <example>
/// <code>
/// // In your ValidateBase constructor:
/// RuleManager.AddValidationAsync(
///     async target => await emailService.ExistsAsync(target.Email) ? "Email already in use" : "",
///     t => t.Email);
/// </code>
/// </example>
public class AsyncFluentRule<T> : AsyncRuleBase<T>
where T : class, IValidateBase
{
    private Func<T, Task<string>> ExecuteFunc { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncFluentRule{T}"/> class.
    /// </summary>
    /// <param name="execute">The async validation function that returns an error message or empty string for success.</param>
    /// <param name="triggerProperty">The property expression that triggers this rule and receives any error message.</param>
    public AsyncFluentRule(Func<T, Task<string>> execute, Expression<Func<T, object?>> triggerProperty) : base([triggerProperty])
    {
        this.ExecuteFunc = execute;
    }

    /// <inheritdoc />
    protected override async Task<IRuleMessages> Execute(T target, CancellationToken? token = null)
    {
        var result = await this.ExecuteFunc(target);

        if (string.IsNullOrWhiteSpace(result))
        {
            return RuleMessages.None;
        }
        else
        {
            return (this.TriggerProperties.Single().PropertyName, result).AsRuleMessages();
        }
    }
}

/// <summary>
/// A fluent rule that executes an asynchronous action with cancellation support without producing validation messages.
/// Useful for rules that perform async side effects and need to respond to cancellation tokens.
/// </summary>
/// <typeparam name="T">The type of validation target this rule operates on.</typeparam>
/// <remarks>
/// This rule is created through <see cref="IRuleManager{T}.AddActionAsync(Func{T, CancellationToken, Task}, Expression{Func{T, object?}}[])"/>
/// and always returns <see cref="RuleMessages.None"/> since it is intended for actions, not validation.
/// </remarks>
/// <example>
/// <code>
/// // In your ValidateBase constructor:
/// RuleManager.AddActionAsync(
///     async (target, token) => target.TaxRate = await taxService.GetTaxRateAsync(target.ZipCode, token),
///     t => t.ZipCode);
/// </code>
/// </example>
public class ActionAsyncFluentRuleWithToken<T> : AsyncRuleBase<T>
    where T : class, IValidateBase
{
    private Func<T, CancellationToken, Task> ExecuteFunc { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ActionAsyncFluentRuleWithToken{T}"/> class.
    /// </summary>
    /// <param name="execute">The async function to execute when the rule is triggered, receiving the cancellation token.</param>
    /// <param name="triggerProperties">Property expressions that define which properties trigger this rule when changed.</param>
    public ActionAsyncFluentRuleWithToken(Func<T, CancellationToken, Task> execute, params Expression<Func<T, object?>>[] triggerProperties) : base(triggerProperties)
    {
        this.ExecuteFunc = execute;
    }

    /// <inheritdoc />
    protected override async Task<IRuleMessages> Execute(T target, CancellationToken? token = null)
    {
        await this.ExecuteFunc(target, token ?? CancellationToken.None);
        return RuleMessages.None;
    }
}

/// <summary>
/// A fluent rule that executes an asynchronous validation function with cancellation support and produces a validation message.
/// The validation function returns a string: an empty or null string indicates success, any other value is the error message.
/// </summary>
/// <typeparam name="T">The type of validation target this rule operates on.</typeparam>
/// <remarks>
/// This rule is created through <see cref="IRuleManager{T}.AddValidationAsync(Func{T, CancellationToken, Task{string}}, Expression{Func{T, object?}})"/>.
/// The error message is automatically associated with the trigger property.
/// </remarks>
/// <example>
/// <code>
/// // In your ValidateBase constructor:
/// RuleManager.AddValidationAsync(
///     async (target, token) => await emailService.ExistsAsync(target.Email, token) ? "Email already in use" : "",
///     t => t.Email);
/// </code>
/// </example>
public class AsyncFluentRuleWithToken<T> : AsyncRuleBase<T>
where T : class, IValidateBase
{
    private Func<T, CancellationToken, Task<string>> ExecuteFunc { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncFluentRuleWithToken{T}"/> class.
    /// </summary>
    /// <param name="execute">The async validation function that receives the cancellation token and returns an error message or empty string for success.</param>
    /// <param name="triggerProperty">The property expression that triggers this rule and receives any error message.</param>
    public AsyncFluentRuleWithToken(Func<T, CancellationToken, Task<string>> execute, Expression<Func<T, object?>> triggerProperty) : base([triggerProperty])
    {
        this.ExecuteFunc = execute;
    }

    /// <inheritdoc />
    protected override async Task<IRuleMessages> Execute(T target, CancellationToken? token = null)
    {
        var result = await this.ExecuteFunc(target, token ?? CancellationToken.None);

        if (string.IsNullOrWhiteSpace(result))
        {
            return RuleMessages.None;
        }
        else
        {
            return (this.TriggerProperties.Single().PropertyName, result).AsRuleMessages();
        }
    }
}

