using Neatoo.Rules;

namespace Neatoo.UnitTest.Example.SimpleValidate;

public interface IShortNameRule : IRule<ISimpleValidateObject> { }

internal class ShortNameRule : RuleBase<ISimpleValidateObject>, IShortNameRule
{
    public ShortNameRule() : base()
    {
        AddTriggerProperties(_ => _.FirstName);
        AddTriggerProperties(_ => _.LastName);
    }

    public override PropertyErrors Execute(ISimpleValidateObject target)
    {

        var propertyErrors = new PropertyErrors();

        if (string.IsNullOrWhiteSpace(target.FirstName))
        {
            propertyErrors.Add(nameof(ISimpleValidateObject.FirstName), $"{nameof(ISimpleValidateObject.FirstName)} is required.");
        }

        if (string.IsNullOrWhiteSpace(target.LastName))
        {
            propertyErrors.Add(nameof(ISimpleValidateObject.LastName), $"{nameof(ISimpleValidateObject.LastName)} is required.");
        }

        target.ShortName = $"{target.FirstName} {target.LastName}";

        return propertyErrors;
    }

}
