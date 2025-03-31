
using Neatoo.Rules;
using Neatoo.UnitTest.Objects;

namespace Neatoo.UnitTest.PersonObjects;

public interface IShortNameDependencyRule : IRule<IPersonBase> { }

public class ShortNameDependencyRule : RuleBase<IPersonBase>, IShortNameDependencyRule
{

    public ShortNameDependencyRule(IDisposableDependency dd) : base()
    {
        AddTriggerProperties(_ => _.FirstName);
        AddTriggerProperties(_ => _.LastName);
        DisposableDependency = dd;
    }

    private IDisposableDependency DisposableDependency { get; }

    public override IRuleMessages Execute(IPersonBase target)
    {

        // System.Diagnostics.Debug.WriteLine($"Run Rule {target.FirstName} {target.LastName}");

        var dd = DisposableDependency ?? throw new ArgumentNullException(nameof(DisposableDependency));

        if (target.FirstName?.StartsWith("Error") ?? false)
        {
            return (nameof(IPersonBase.FirstName), target.FirstName).AsRuleMessages();
        }


        target.ShortName = $"{target.FirstName} {target.LastName}";

        return RuleMessages.None;
    }

}
