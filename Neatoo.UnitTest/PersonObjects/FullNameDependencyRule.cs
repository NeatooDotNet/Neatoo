using Neatoo.Rules;
using Neatoo.UnitTest.Objects;


namespace Neatoo.UnitTest.PersonObjects;

public interface IFullNameDependencyRule : IRule<IPersonBase> { }

public class FullNameDependencyRule : RuleBase<IPersonBase>, IFullNameDependencyRule
{

    public FullNameDependencyRule(IDisposableDependency dd) : base()
    {
        AddTriggerProperties(_ => _.Title);
        AddTriggerProperties(_ => _.ShortName);

        this.DisposableDependency = dd;
    }

    private IDisposableDependency DisposableDependency { get; }

    public override PropertyErrors Execute(IPersonBase target)
    {

        var dd = DisposableDependency ?? throw new ArgumentNullException(nameof(DisposableDependency));

        target.FullName = $"{target.Title} {target.ShortName}";

        return PropertyErrors.None;

    }

}
