// -----------------------------------------------------------------------------
// Design.Domain - TrainingPlan (aggregate root)
// -----------------------------------------------------------------------------
// Rules-first. Nearly everything a person does on this screen is a FACT they
// write; what follows from it is a CONSEQUENCE a rule computes, whoever wrote
// it; a VERB exists only for intent the state cannot hold.
//
// Rules are code running inside the cascade. A write inside a rule runs the
// rules it triggers before the writing rule's next statement (awaited Set{Name}),
// a child settles before its parent hears, and a write of an equal value raises
// nothing. That last property is how the fit below converges: the fit writes
// each free interval, each write re-triggers the fit, and the nested fit
// computes the same shares and writes nothing new.
//
// DESIGN DECISION: the fit reads the anchor, a scoped transient a gesture sets
// for one cascade (SlideMinutes on the child). The rule cannot know which
// interval moved, and must not: a rule that branches on who called it is the
// caller-knowledge this model removes. The anchor is the canonical intent that
// is not a property: no later reader needs it, the next gesture must not
// inherit it, and only the outermost scope knows the cascade is over.
//
// DID NOT DO THIS: suspend the fit while a verb writes several facts. The rule
// reads state instead: a locked interval is fixed, a free one is shared.
// -----------------------------------------------------------------------------

using Neatoo;
using Neatoo.RemoteFactory;
using Neatoo.Rules;
using System.Linq.Expressions;

namespace Design.Domain.Aggregates.TrainingPlanAggregate;

[Factory]
internal partial class TrainingPlan : EntityBase<TrainingPlan>, ITrainingPlan, ITrainingPlanFit
{
    private readonly IIntervalFactory _intervalFactory;

    // The interval the current gesture holds for its fit; null outside a gesture.
    private IInterval? _anchor;

    public partial int ApprovedMinutes { get; set; }
    public partial int PlannedMinutes { get; private set; }
    public partial string Status { get; private set; }
    public partial string? ApprovedBy { get; private set; }
    public partial bool CanApprove { get; private set; }
    public partial IIntervalList? Intervals { get; private set; }

    public TrainingPlan(IEntityBaseServices<TrainingPlan> services, IIntervalFactory intervalFactory) : base(services)
    {
        _intervalFactory = intervalFactory;

        #region skill-consequence-fit
        // Consequence: share the approved minutes across the free intervals by
        // effort. Triggered by every fact it reads, including the intervals'
        // own Minutes (a slide, a lock) and the list's Count (an add, a remove).
        RuleManager.AddActionAsync(
            async t => await t.Fit(),
            new Expression<Func<TrainingPlan, object?>>[]
            {
                t => t.ApprovedMinutes,
                t => t.Intervals!.Count,
                t => t.Intervals![0].Effort,
                t => t.Intervals![0].MinutesLocked,
                t => t.Intervals![0].Minutes,
            });
        #endregion

        RuleManager.AddActionAsync(
            async t => await t.SetPlannedMinutes(t.Intervals?.Sum(i => i.Minutes) ?? 0),
            t => t.Intervals![0].Minutes,
            t => t.Intervals!.Count);

        RuleManager.AddValidation(
            t => t.ApprovedMinutes > 0 ? string.Empty : "Approved minutes must be positive",
            t => t.ApprovedMinutes);

        // A validation over three facts is a rule class; AddValidation takes one trigger.
        RuleManager.AddRule(new LockedMinutesFitRule());

        #region skill-admission-on-meta-state
        // Consequence: admission for Approve. It reads IsValid, so IsValid is a
        // trigger: when validity flips, the fact follows. The Approve button
        // binds CanApprove; the verb never checks and throws.
        RuleManager.AddActionAsync(
            async t => await t.SetCanApprove(t.Status == "Draft" && t.IsValid),
            t => t.Status,
            t => t.IsValid);
        #endregion
    }

    #region skill-fit-reads-the-anchor
    /// <summary>
    /// The fit: locked intervals and the anchored one keep their minutes; the rest share what is
    /// left of the approved minutes in proportion to effort.
    /// </summary>
    private async Task Fit()
    {
        if (Intervals == null || Intervals.Count == 0)
        {
            return;
        }

        var held = Intervals.Where(i => i.MinutesLocked || ReferenceEquals(i, _anchor)).ToList();
        var free = Intervals.Except(held).ToList();
        if (free.Count == 0)
        {
            return;
        }

        var remaining = Math.Max(ApprovedMinutes - held.Sum(i => i.Minutes), 0);
        var shares = Share(remaining, free.Select(i => Math.Max(i.Effort, 1)).ToList());

        for (var index = 0; index < free.Count; index++)
        {
            // Parent writes child through the child's own method. Each write re-enters
            // this rule; the nested fit computes the same shares and writes nothing new.
            await ((IIntervalFit)free[index]).ApplyFit(shares[index]);
        }
    }

    /// <summary>
    /// The anchor for one gesture. Scopes nest: the previous anchor comes back when this one ends.
    /// </summary>
    IDisposable ITrainingPlanFit.AnchorFit(IInterval interval)
    {
        var previous = _anchor;
        _anchor = interval;
        return new AnchorScope(() => _anchor = previous);
    }
    #endregion

    /// <summary>
    /// Whole minutes in proportion to the weights; the remainder goes one minute each, in order.
    /// </summary>
    private static int[] Share(int minutes, IReadOnlyList<int> weights)
    {
        var total = weights.Sum();
        var shares = weights.Select(w => minutes * w / total).ToArray();
        var left = minutes - shares.Sum();
        for (var index = 0; left > 0; index = (index + 1) % shares.Length, left--)
        {
            shares[index]++;
        }

        return shares;
    }

    #region skill-intent-add-interval
    /// <summary>
    /// Intent: a new interval, built with its effort, joins the plan. The interval's Create makes
    /// name and effort its baseline, so the add is the one change: the list's Count, which the fit
    /// triggers on. One fit runs, with the right effort.
    /// </summary>
    public async Task<IInterval> AddInterval(string name, int effort)
    {
        var interval = await _intervalFactory.Create(name, effort);
        Intervals!.Add(interval);
        await WaitForTasks();
        return interval;
    }
    #endregion

    #region skill-intent-approve
    /// <summary>
    /// Intent: who approved, then the status. Each write is awaited, so the status's
    /// consequences (CanApprove) have landed when the verb returns.
    /// </summary>
    public async Task Approve(string approver)
    {
        await SetApprovedBy(approver);
        await SetStatus("Approved");
    }
    #endregion

    [Create]
    public async Task Create([Service] IIntervalListFactory listFactory)
    {
        Status = "Draft";
        ApprovedMinutes = 60;
        Intervals = listFactory.Create();

        // Inside the operation the object is paused: the assignments above are baseline and no
        // rule ran. Compute the admission fact the new plan needs.
        await RunRules(nameof(Status));
    }

    private sealed class AnchorScope(Action release) : IDisposable
    {
        public void Dispose() => release();
    }
}

/// <summary>
/// Validation: the locked intervals must fit inside the approved minutes. It writes nothing, so
/// it is a synchronous RuleBase; the message lands on ApprovedMinutes.
/// </summary>
internal class LockedMinutesFitRule : RuleBase<TrainingPlan>
{
    public LockedMinutesFitRule()
        : base(t => t.ApprovedMinutes, t => t.Intervals![0].MinutesLocked, t => t.Intervals![0].Minutes)
    {
    }

    protected override IRuleMessages Execute(TrainingPlan target)
    {
        var locked = target.Intervals?.Where(i => i.MinutesLocked).Sum(i => i.Minutes) ?? 0;
        return locked > target.ApprovedMinutes
            ? (nameof(ITrainingPlan.ApprovedMinutes), "Locked intervals exceed the approved minutes").AsRuleMessages()
            : None;
    }
}
