using Neatoo.Rules;

namespace Neatoo.UnitTest.PersonObjects;

internal interface IRecursiveAsyncRule : IRule<IPersonBase> { }

internal class RecursiveAsyncRule : AsyncRuleBase<IPersonBase>, IRecursiveAsyncRule
{
    public RecursiveAsyncRule() : base()
    {
        AddTriggerProperties(_ => _.ShortName);
    }
    protected override async Task<IRuleMessages> Execute(IPersonBase target, CancellationToken? token)
    {
        await Task.Delay(10);

        if (target.ShortName == "Recursive")
        {
            target.ShortName = "Recursive change";
        }
        else if (target.ShortName == "Recursive Error")
        {
            target.FirstName = "Error"; // trigger the ShortNameRule error
        }

        return None;
    }
}
