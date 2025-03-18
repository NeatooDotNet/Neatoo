namespace Neatoo.Rules.Rules;

public interface IRequiredRule : IRule
{

}

internal class RequiredRule<T> : RuleBase<T>, IRequiredRule
    where T : class, IValidateBase
{
    public RequiredRule(ITriggerProperty triggerProperty) : base() {
        TriggerProperties.Add(triggerProperty);
    }

    public override PropertyErrors Execute(T target)
    {
        var value = ((ITriggerProperty<T>) TriggerProperties[0]).GetValue(target);

        bool isError = false;

        if (value is string s)
        {
            isError = string.IsNullOrWhiteSpace(s);
        }
        else if (value?.GetType().IsValueType ?? false)
        {
            isError = value == Activator.CreateInstance(value.GetType());
        }
        else
        {
            isError = value == null;
        }

        if (isError)
        {
            return new PropertyError(TriggerProperties.Single().PropertyName, $"{TriggerProperties[0].PropertyName} is required.");
        }
        return PropertyErrors.None;
    }
}
