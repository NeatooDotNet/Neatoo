using System.ComponentModel.DataAnnotations;

namespace Neatoo.Rules.Rules;

public interface IRequiredRule : IRule
{
    string ErrorMessage { get; }
}

internal class RequiredRule<T> : RuleBase<T>, IRequiredRule
    where T : class, IValidateBase
{
    public string ErrorMessage { get; }

    public RequiredRule(ITriggerProperty triggerProperty, RequiredAttribute requiredAttribute) : base() {
        TriggerProperties.Add(triggerProperty);
        ErrorMessage = requiredAttribute.ErrorMessage ?? $"{TriggerProperties[0].PropertyName} is required.";
    }

    protected override IRuleMessages Execute(T target)
    {
        var value = ((ITriggerProperty<T>) TriggerProperties[0]).GetValue(target);

        bool isError = false;

        if (value is string s)
        {
            isError = string.IsNullOrWhiteSpace(s);
        }
        else if (value?.GetType().IsValueType ?? false)
        {
            isError = value.Equals(Activator.CreateInstance(value.GetType()));
        }
        else
        {
            isError = value == null;
        }

        if (isError)
        {
            return (TriggerProperties.Single().PropertyName, ErrorMessage).AsRuleMessages();
        }
        return RuleMessages.None;
    }
}
