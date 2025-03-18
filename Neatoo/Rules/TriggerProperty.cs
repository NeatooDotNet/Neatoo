using System.Linq.Expressions;

namespace Neatoo.Rules;

public interface ITriggerProperty
{
    string PropertyName { get; }


    bool IsMatch(string propertyName);
}

public interface ITriggerProperty<T> : ITriggerProperty
{
    object? GetValue(T target);
}

public class TriggerProperty<T> : ITriggerProperty<T>
{
    private readonly Expression<Func<T, object?>> expression;
    private readonly string expressionPropertyName;
    public TriggerProperty(Expression<Func<T, object?>> expression)
    {
        this.expression = expression;
        expressionPropertyName = RecurseMembers(expression.Body, new List<string>());
    }

    public bool IsMatch(string propertyName)
    {
        return string.Equals(expressionPropertyName, propertyName);
    }

    public object? GetValue(T target)
    {
        return expression.Compile()(target);
    }

    public string PropertyName => expressionPropertyName;

    protected string RecurseMembers(Expression? expression, List<string> properties)
    {
        if (expression is UnaryExpression unaryExpression)
        {
            return RecurseMembers(unaryExpression.Operand, properties);
        }
        if (expression is MemberExpression memberExpression)
        {
            properties.Add(memberExpression.Member.Name);
            return RecurseMembers(memberExpression.Expression, properties);
        }

        properties.Reverse();

        return string.Join('.', properties);
    }

    public static TriggerProperty<T1> FromExpression<T1>(Expression<Func<T1, object?>> expression)
    {
        return new TriggerProperty<T1>(expression);
    }
}
