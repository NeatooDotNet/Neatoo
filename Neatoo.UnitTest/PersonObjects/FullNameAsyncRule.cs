using Neatoo.Rules;

namespace Neatoo.UnitTest.PersonObjects;


public interface IFullNameAsyncRule : IRule<IPersonBase> { int RunCount { get; } }

public class FullNameAsyncRule : AsyncRuleBase<IPersonBase>, IFullNameAsyncRule
{

    public FullNameAsyncRule() : base()
    {

        AddTriggerProperties(_ => _.FirstName);
        AddTriggerProperties(_ => _.ShortName);
    }

    public int RunCount { get; private set; }

    public override async Task<PropertyErrors> Execute(IPersonBase target, CancellationToken? token = null)
    {
        RunCount++;

        await Task.Delay(10);

        // System.Diagnostics.Debug.WriteLine($"FullNameAsyncRule {target.Title} {target.ShortName}");

        target.FullName = $"{target.Title} {target.ShortName}";

        return PropertyErrors.None;
    }
}
