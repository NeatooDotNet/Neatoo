using Neatoo.Rules;

namespace Neatoo.UnitTest.PersonObjects;

public interface IFullNameRule : IRule<IPersonBase> { int RunCount { get; } }

public class FullNameRule : RuleBase<IPersonBase>, IFullNameRule
{
    public int RunCount { get; private set; } = 0;

    public FullNameRule() : base()
    {
        AddTriggerProperties(_ => _.Title);
        AddTriggerProperties(_ => _.ShortName);
    }

    public override PropertyErrors Execute(IPersonBase target)
    {
        RunCount++;

        target.FullName = $"{target.Title} {target.ShortName}";

        return PropertyErrors.None;
    }
}
