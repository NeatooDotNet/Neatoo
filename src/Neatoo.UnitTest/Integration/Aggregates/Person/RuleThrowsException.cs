using Neatoo.Rules;

namespace Neatoo.UnitTest.Integration.Aggregates.Person;

public interface IRuleThrowsException : IRule<IPersonBase>
{
}

public class RuleThrowsException : RuleBase<IPersonBase>, IRuleThrowsException
{
    public RuleThrowsException() : base()
    {
        AddTriggerProperties(_ => _.FirstName);
    }

    protected override IRuleMessages Execute(IPersonBase target)
    {
        if (target.FirstName == "Throw")
        {
            throw new Exception("Rule Failed");
        }
        return RuleMessages.None;
    }
}
