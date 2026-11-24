using Neatoo.RemoteFactory;
using Neatoo.Rules;

namespace Neatoo.UnitTest.Integration.Concepts.AwaitableSetter;

/// <summary>
/// Child whose line total is a consequence computed by an async rule.
/// The interface is partial, so the generator adds SetQuantity to it.
/// </summary>
public partial interface IAwaitChild : IEntityBase
{
    int Quantity { get; set; }
    int LineTotal { get; }
}

[Factory]
internal partial class AwaitChild : EntityBase<AwaitChild>, IAwaitChild
{
    public AwaitChild(IEntityBaseServices<AwaitChild> services) : base(services)
    {
        // A consequence with a delay. LineTotal is private-set, so the awaited write goes
        // through SetPrivateValue past IsReadOnly, as the generated setter does.
        RuleManager.AddActionAsync(async t =>
        {
            await Task.Delay(20);
            await t.SetLineTotal(t.Quantity * 10);
        }, t => t.Quantity);
    }

    public partial int Quantity { get; set; }
    public partial int LineTotal { get; private set; }

    [Create]
    public void Create() { }
}

/// <summary>
/// Root whose total follows its child's line total, with a second consequence on the total.
/// </summary>
public partial interface IAwaitParent : IEntityRoot
{
    IAwaitChild? Child { get; set; }
    int Total { get; }
    string? Band { get; }
    string? BandSeenByTotalRule { get; }
    string? Note { get; set; }
}

[Factory]
internal partial class AwaitParent : EntityBase<AwaitParent>, IAwaitParent
{
    public AwaitParent(IEntityBaseServices<AwaitParent> services) : base(services)
    {
        // Triggered by the child's LineTotal. It awaits SetTotal, whose own cascade runs the
        // async Band rule below, then reads Band: the awaited write re-enters the rule manager
        // and must finish before the next statement.
        RuleManager.AddActionAsync(async t =>
        {
            await Task.Delay(20);
            await t.SetTotal(t.Child?.LineTotal ?? 0);
            await t.SetBandSeenByTotalRule(t.Band);
        }, t => t.Child!.LineTotal);

        RuleManager.AddActionAsync(async t =>
        {
            await Task.Delay(20);
            await t.SetBand(t.Total >= 50 ? "High" : "Low");
        }, t => t.Total);

        RuleManager.AddRule(new NoteFollowsTotalRule());
    }

    public partial IAwaitChild? Child { get; set; }
    public partial int Total { get; private set; }
    public partial string? Band { get; private set; }
    public partial string? BandSeenByTotalRule { get; private set; }
    public partial string? Note { get; set; }

    [Create]
    public void Create() { }
}

/// <summary>
/// A class-based rule typed on the interface: it reaches the write only through IAwaitParent.SetNote.
/// </summary>
internal class NoteFollowsTotalRule : AsyncRuleBase<IAwaitParent>
{
    public NoteFollowsTotalRule() : base(t => t.Total) { }

    protected override async Task<IRuleMessages> Execute(IAwaitParent target, CancellationToken? token = null)
    {
        await Task.Delay(10);
        await target.SetNote($"Total {target.Total}");
        return None;
    }
}
