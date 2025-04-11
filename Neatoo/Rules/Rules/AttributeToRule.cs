using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;

namespace Neatoo.Rules.Rules;


public interface IAttributeToRule
{
    IRule GetRule<T>(IPropertyInfo r, object? attribute) where T : class, IValidateBase;
}

public class AttributeToRule : IAttributeToRule
{

    public AttributeToRule()
    {

    }

    public IRule GetRule<T>(IPropertyInfo r, object? attribute) where T : class, IValidateBase
    {
        if (attribute is RequiredAttribute requiredAttribute)
        {

            var parameter = Expression.Parameter(typeof(T));
            var property = Expression.Property(parameter, r.Name);
            var lambda = Expression.Lambda<Func<T, object?>>(Expression.Convert(property, typeof(object)), parameter);
            var tr = new TriggerProperty<T>(lambda);

            return new RequiredRule<T>(tr, requiredAttribute);
        }

        return null;
    }

}
