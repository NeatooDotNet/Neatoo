using Neatoo.Rules;

namespace Neatoo.UnitTest.Integration.Aggregates.Person;

public interface IShortNameAsyncRule : IRule<IPersonBase> { int RunCount { get; } }

public class ShortNameAsyncRule : AsyncRuleBase<IPersonBase>, IShortNameAsyncRule
{

    private Guid UniqueId = Guid.NewGuid();

    public ShortNameAsyncRule() : base()
    {
        AddTriggerProperties(_ => _.FirstName);
        AddTriggerProperties(_ => _.LastName);
    }

    public int RunCount { get; private set; }

    protected override async Task<IRuleMessages> Execute(IPersonBase target, CancellationToken? token)
    {
        Console.WriteLine($"ShortNameAsyncRule: {UniqueId.ToString()} BEGIN");

        RunCount++;

        await Task.Delay(10);


        // System.Diagnostics.Debug.WriteLine($"ShortNameAsyncRule {target.FirstName} {target.LastName}");

        if (target.FirstName?.StartsWith("Error") ?? false)
        {
            Console.WriteLine($"ShortNameAsyncRule: {UniqueId.ToString()} {target.FirstName} {target.LastName} Error!!");
            return nameof(IPersonBase.FirstName).RuleMessages(target.FirstName);
        }

        Console.WriteLine($"ShortNameAsyncRule: {UniqueId.ToString()} {target.FirstName} {target.LastName}");

        target.ShortName = $"{target.FirstName} {target.LastName}";

        Console.WriteLine($"ShortNameAsyncRule: {UniqueId.ToString()} Done");

        return RuleMessages.None;
    }

}
