using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace Neatoo.Rules.Rules;

public interface IMaxLengthRule : IRule
{
    string ErrorMessage { get; }
    int Length { get; }
}

internal class MaxLengthRule<T> : RuleBase<T>, IMaxLengthRule
    where T : class, IValidateBase
{
    public string ErrorMessage { get; }
    public int Length { get; }

    public MaxLengthRule(ITriggerProperty triggerProperty, MaxLengthAttribute attribute) : base()
    {
        this.TriggerProperties.Add(triggerProperty);
        this.Length = attribute.Length;
        this.ErrorMessage = attribute.ErrorMessage ?? $"{triggerProperty.PropertyName} cannot exceed {Length} characters.";
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

        if (actualLength > Length)
        {
            return (this.TriggerProperties.Single().PropertyName, this.ErrorMessage).AsRuleMessages();
        }

        return RuleMessages.None;
    }
}
