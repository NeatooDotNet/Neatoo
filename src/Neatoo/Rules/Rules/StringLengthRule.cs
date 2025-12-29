using System.ComponentModel.DataAnnotations;

namespace Neatoo.Rules.Rules;

public interface IStringLengthRule : IRule
{
    string ErrorMessage { get; }
    int MinimumLength { get; }
    int MaximumLength { get; }
}

internal class StringLengthRule<T> : RuleBase<T>, IStringLengthRule
    where T : class, IValidateBase
{
    public string ErrorMessage { get; }
    public int MinimumLength { get; }
    public int MaximumLength { get; }

    public StringLengthRule(ITriggerProperty triggerProperty, StringLengthAttribute attribute) : base()
    {
        this.TriggerProperties.Add(triggerProperty);
        this.MinimumLength = attribute.MinimumLength;
        this.MaximumLength = attribute.MaximumLength;

        if (!string.IsNullOrEmpty(attribute.ErrorMessage))
        {
            this.ErrorMessage = attribute.ErrorMessage;
        }
        else if (MinimumLength > 0)
        {
            this.ErrorMessage = $"{triggerProperty.PropertyName} must be between {MinimumLength} and {MaximumLength} characters.";
        }
        else
        {
            this.ErrorMessage = $"{triggerProperty.PropertyName} cannot exceed {MaximumLength} characters.";
        }
    }

    protected override IRuleMessages Execute(T target)
    {
        var value = ((ITriggerProperty<T>)this.TriggerProperties[0]).GetValue(target);

        if (value is not string s)
        {
            return RuleMessages.None;
        }

        // Null or empty strings pass - use Required for null check
        if (string.IsNullOrEmpty(s))
        {
            return RuleMessages.None;
        }

        var length = s.Length;

        if (length < MinimumLength || length > MaximumLength)
        {
            return (this.TriggerProperties.Single().PropertyName, this.ErrorMessage).AsRuleMessages();
        }

        return RuleMessages.None;
    }
}
