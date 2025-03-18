using Neatoo.Rules;

namespace Neatoo.UnitTest.PersonObjects;

public interface IRuleThrowsException : IRule<IPersonBase>
{
}

public class RuleThrowsException : RuleBase<IPersonBase>, IRuleThrowsException
{
    public RuleThrowsException() : base()
    {
        AddTriggerProperties(_ => _.FirstName);
    }

    public override PropertyErrors Execute(IPersonBase target)
    {
        if (target.FirstName == "Throw")
        {
            throw new Exception("Rule Failed");
        }
        return PropertyErrors.None;
    }
}
