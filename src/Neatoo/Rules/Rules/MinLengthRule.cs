using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace Neatoo.Rules.Rules;

public interface IMinLengthRule : IRule
{
    string ErrorMessage { get; }
    int Length { get; }
}

internal class MinLengthRule<T> : RuleBase<T>, IMinLengthRule
    where T : class, IValidateBase
{
    public string ErrorMessage { get; }
    public int Length { get; }

    public MinLengthRule(ITriggerProperty triggerProperty, MinLengthAttribute attribute) : base()
    {
        this.TriggerProperties.Add(triggerProperty);
        this.Length = attribute.Length;
        this.ErrorMessage = attribute.ErrorMessage ?? $"{triggerProperty.PropertyName} must have at least {Length} characters.";
    }

    protected override IRuleMessages Execute(T target)
    {
        var value = ((ITriggerProperty<T>)this.TriggerProperties[0]).GetValue(target);

        // Null values pass - use Required for null check
        if (value == null)
        {
            return RuleMessages.None;
        }

        int actualLength;

        if (value is string s)
        {
            actualLength = s.Length;
        }
        else if (value is ICollection collection)
        {
            actualLength = collection.Count;
        }
        else if (value is Array array)
        {
            actualLength = array.Length;
        }
        else
        {
            return RuleMessages.None;
        }

        if (actualLength < Length)
        {
            return (this.TriggerProperties.Single().PropertyName, this.ErrorMessage).AsRuleMessages();
        }

        return RuleMessages.None;
    }
}
