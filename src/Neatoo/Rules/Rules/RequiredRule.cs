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
    private readonly bool isNullableValueType;

    public RequiredRule(ITriggerProperty triggerProperty, RequiredAttribute requiredAttribute, Type propertyType) : base() {
        this.TriggerProperties.Add(triggerProperty);
        this.ErrorMessage = requiredAttribute.ErrorMessage ?? $"{this.TriggerProperties[0].PropertyName} is required.";

        // Check if the property type is a nullable value type (e.g., int?, PhoneType?)
        this.isNullableValueType = propertyType.IsGenericType &&
                                   propertyType.GetGenericTypeDefinition() == typeof(Nullable<>);
    }

    protected override IRuleMessages Execute(T target)
    {
        var value = ((ITriggerProperty<T>) this.TriggerProperties[0]).GetValue(target);

        var isError = false;

        if (value is string s)
        {
            isError = string.IsNullOrWhiteSpace(s);
        }
        else if (value?.GetType().IsValueType ?? false)
        {
            // For nullable value types (e.g., int?, PhoneType?), only check for null
            // The value being equal to the default (e.g., 0 or first enum) is a valid value
            if (isNullableValueType)
            {
                isError = false; // Value is not null (we have a value), so it's valid
            }
            else
            {
                // For non-nullable value types, check if it equals the default
                isError = value.Equals(Activator.CreateInstance(value.GetType()));
            }
        }
        else
        {
            isError = value == null;
        }

        if (isError)
        {
            return (this.TriggerProperties.Single().PropertyName, this.ErrorMessage).AsRuleMessages();
        }
        return RuleMessages.None;
    }
}
