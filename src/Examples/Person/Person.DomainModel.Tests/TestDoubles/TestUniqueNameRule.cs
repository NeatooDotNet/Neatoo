using DomainModel;
using Neatoo.Rules;

namespace DomainModel.Tests.TestDoubles;

/// <summary>
/// Test stub for IUniqueNameRule that tracks OnRuleAdded calls.
/// Inherits from AsyncRuleBase so real rule behavior works.
/// </summary>
internal class TestUniqueNameRule : AsyncRuleBase<IPerson>, IUniqueNameRule
{
    public int OnRuleAddedCallCount { get; private set; }
    public IRuleManager? LastRuleManager { get; private set; }
    public uint LastUniqueIndex { get; private set; }

    protected override Task<IRuleMessages> Execute(IPerson t, CancellationToken? token = null)
        => Task.FromResult<IRuleMessages>(None);

    public override void OnRuleAdded(IRuleManager ruleManager, uint uniqueIndex)
    {
        OnRuleAddedCallCount++;
        LastRuleManager = ruleManager;
        LastUniqueIndex = uniqueIndex;
        base.OnRuleAdded(ruleManager, uniqueIndex);
    }
}
