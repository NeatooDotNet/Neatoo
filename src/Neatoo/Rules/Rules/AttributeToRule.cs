using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;

namespace Neatoo.Rules.Rules;


public interface IAttributeToRule
{
    IRule? GetRule<T>(IPropertyInfo r, object? attribute) where T : class, IValidateBase;
}

public class AttributeToRule : IAttributeToRule
{
    public AttributeToRule()
    {
    }

    public IRule? GetRule<T>(IPropertyInfo r, object? attribute) where T : class, IValidateBase
    {
        var triggerProperty = CreateTriggerProperty<T>(r);

        return attribute switch
        {
            RequiredAttribute requiredAttribute => new RequiredRule<T>(triggerProperty, requiredAttribute, r.Type),
            StringLengthAttribute stringLengthAttribute => new StringLengthRule<T>(triggerProperty, stringLengthAttribute),
            MinLengthAttribute minLengthAttribute => new MinLengthRule<T>(triggerProperty, minLengthAttribute),
            MaxLengthAttribute maxLengthAttribute => new MaxLengthRule<T>(triggerProperty, maxLengthAttribute),
            RegularExpressionAttribute regexAttribute => new RegularExpressionRule<T>(triggerProperty, regexAttribute),
            RangeAttribute rangeAttribute => new RangeRule<T>(triggerProperty, rangeAttribute),
            EmailAddressAttribute emailAttribute => new EmailAddressRule<T>(triggerProperty, emailAttribute),
            _ => null
        };
    }

    private static TriggerProperty<T> CreateTriggerProperty<T>(IPropertyInfo r) where T : class, IValidateBase
    {
        var parameter = Expression.Parameter(typeof(T));
        var property = Expression.Property(parameter, r.Name);
        var lambda = Expression.Lambda<Func<T, object?>>(Expression.Convert(property, typeof(object)), parameter);
        return new TriggerProperty<T>(lambda);
    }
}
