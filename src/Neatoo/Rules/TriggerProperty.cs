using System.Linq.Expressions;

namespace Neatoo.Rules;

/// <summary>
/// Represents a property that triggers a rule when its value changes.
/// Trigger properties define the relationship between properties and the rules that depend on them.
/// </summary>
/// <remarks>
/// When a property on a validation target changes, the rule manager uses trigger properties
/// to determine which rules need to be executed.
/// </remarks>
public interface ITriggerProperty
{
    /// <summary>
    /// Gets the name of the property that triggers the rule.
    /// May include nested property paths separated by dots (e.g., "Address.City").
    /// </summary>
    string PropertyName { get; }

    /// <summary>
    /// Determines whether this trigger property matches the specified property name.
    /// </summary>
    /// <param name="propertyName">The property name to check.</param>
    /// <returns><see langword="true"/> if the property name matches this trigger property; otherwise, <see langword="false"/>.</returns>
    bool IsMatch(string propertyName);
}

/// <summary>
/// A strongly-typed trigger property that can retrieve the property value from a target.
/// </summary>
/// <typeparam name="T">The type of the validation target containing the property.</typeparam>
public interface ITriggerProperty<T> : ITriggerProperty
{
    /// <summary>
    /// Gets the current value of the trigger property from the specified target.
    /// </summary>
    /// <param name="target">The validation target to read the property value from.</param>
    /// <returns>The current value of the property, or null if not accessible.</returns>
    object? GetValue(T target);
}

/// <summary>
/// Default implementation of <see cref="ITriggerProperty{T}"/> that uses expression trees
/// to identify which property should trigger a rule.
/// </summary>
/// <typeparam name="T">The type of the validation target containing the property.</typeparam>
/// <remarks>
/// <para>
/// TriggerProperty extracts the property path from a lambda expression, supporting
/// simple properties (e.g., <c>p => p.Name</c>), nested properties (e.g., <c>p => p.Address.City</c>),
/// and indexed access (e.g., <c>p => p.Items[0].Price</c>).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create a trigger for a simple property
/// var nameTrigger = new TriggerProperty&lt;Person&gt;(p => p.Name);
///
/// // Create a trigger for a nested property
/// var cityTrigger = new TriggerProperty&lt;Person&gt;(p => p.Address.City);
///
/// // Use in a rule constructor
/// public class NameRule : RuleBase&lt;Person&gt;
/// {
///     public NameRule() : base(p => p.FirstName, p => p.LastName) { }
/// }
/// </code>
/// </example>
public class TriggerProperty<T> : ITriggerProperty<T>
{
    private readonly Expression<Func<T, object?>> expression;
    private readonly string expressionPropertyName;

    /// <summary>
    /// Initializes a new instance of the <see cref="TriggerProperty{T}"/> class from a property expression.
    /// </summary>
    /// <param name="expression">A lambda expression identifying the property (e.g., <c>p => p.Name</c>).</param>
    public TriggerProperty(Expression<Func<T, object?>> expression)
    {
        this.expression = expression;
        this.expressionPropertyName = this.RecurseMembers(expression.Body, new List<string>());
    }

    /// <inheritdoc />
    public bool IsMatch(string propertyName)
    {
        return string.Equals(this.expressionPropertyName, propertyName);
    }

    /// <inheritdoc />
    public object? GetValue(T target)
    {
        return this.expression.Compile()(target);
    }

    /// <inheritdoc />
    public string PropertyName => this.expressionPropertyName;

    /// <summary>
    /// Recursively extracts property names from an expression tree to build the property path.
    /// </summary>
    /// <param name="expression">The expression to analyze.</param>
    /// <param name="properties">The list of property names collected so far.</param>
    /// <returns>The complete property path as a dot-separated string.</returns>
    protected string RecurseMembers(Expression? expression, List<string> properties)
    {
        if (expression is UnaryExpression unaryExpression)
        {
            return this.RecurseMembers(unaryExpression.Operand, properties);
        }
        if (expression is MemberExpression memberExpression)
        {
            properties.Add(memberExpression.Member.Name);
            return this.RecurseMembers(memberExpression.Expression, properties);
        }

        if(expression is MethodCallExpression methodCall)
        {
            // allow parent.ChildList[0].PropertyName . Maybe a better way??
            return this.RecurseMembers(methodCall.Object, properties);
        }

        properties.Reverse();

        return string.Join('.', properties);
    }

    /// <summary>
    /// Creates a new <see cref="TriggerProperty{T}"/> from an expression.
    /// </summary>
    /// <typeparam name="T1">The type of the validation target.</typeparam>
    /// <param name="expression">A lambda expression identifying the property.</param>
    /// <returns>A new trigger property instance.</returns>
    public static TriggerProperty<T1> FromExpression<T1>(Expression<Func<T1, object?>> expression)
    {
        return new TriggerProperty<T1>(expression);
    }
}
