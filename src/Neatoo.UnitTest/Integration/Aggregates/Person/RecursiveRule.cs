using Neatoo.Rules;

namespace Neatoo.UnitTest.Integration.Aggregates.Person;

internal interface IRecursiveRule : IRule<IPersonBase> { }

internal class RecursiveRule : RuleBase<IPersonBase>, IRecursiveRule
{
    public RecursiveRule() : base()
    {
        AddTriggerProperties(_ => _.ShortName);
    }
    protected override IRuleMessages Execute(IPersonBase target)
    {
        if (target.ShortName == "Recursive")
        {
            target.ShortName = "Recursive change";
        }
        else if (target.ShortName == "Recursive Error")
        {
            target.FirstName = "Error"; // trigger the ShortNameRule error
        }
        return RuleMessages.None;
    }
}
