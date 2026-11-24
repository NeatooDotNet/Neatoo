// -----------------------------------------------------------------------------
// Design.Domain - Interval (child of TrainingPlan)
// -----------------------------------------------------------------------------
// The slider is a fact: Effort has a public setter and no verb, and every rule
// that follows from it runs whoever wrote it. A nudge button writes
// NextEffortUp into Effort; the domain publishes the next mark, the UI writes it.
//
// Minutes is the one value two gestures write with different meanings, so it
// has two verbs and one property, never a second property:
//   - SlideMinutes: "this many, and fit the others around me, this once".
//   - LockMinutes:  "this many, and keep them". Lock first, then minutes: if the
//     minutes landed first, the fit would share them away before the lock held.
// Unlocking is a plain write of MinutesLocked: the fit takes the interval back.
// -----------------------------------------------------------------------------

using Neatoo;
using Neatoo.RemoteFactory;

namespace Design.Domain.Aggregates.TrainingPlanAggregate;

[Factory]
internal partial class Interval : EntityBase<Interval>, IInterval, IIntervalFit
{
    public partial string Name { get; set; }
    public partial int Effort { get; set; }
    public partial bool MinutesLocked { get; set; }
    public partial int Minutes { get; private set; }
    public partial int NextEffortUp { get; private set; }
    public partial int NextEffortDown { get; private set; }

    public Interval(IEntityBaseServices<Interval> services) : base(services)
    {
        RuleManager.AddValidation(
            t => string.IsNullOrWhiteSpace(t.Name) ? "Name is required" : string.Empty,
            t => t.Name);

        RuleManager.AddValidation(
            t => t.Effort is >= 1 and <= 10 ? string.Empty : "Effort is between 1 and 10",
            t => t.Effort);

        #region skill-next-mark-facts
        // Consequences the UI writes back: a nudge is a plain write of Effort.
        RuleManager.AddActionAsync(
            async t =>
            {
                await t.SetNextEffortUp(Math.Min(t.Effort + 1, 10));
                await t.SetNextEffortDown(Math.Max(t.Effort - 1, 1));
            },
            t => t.Effort);
        #endregion
    }

    #region skill-intent-slide-with-anchor
    /// <summary>
    /// Intent: set the minutes and fit the others around this interval. The anchor lives for this
    /// one cascade (every nested fit sees it) and evaporates when the gesture ends.
    /// </summary>
    public async Task SlideMinutes(int minutes)
    {
        using (((ITrainingPlanFit)Parent!).AnchorFit(this))
        {
            await SetMinutes(minutes);
        }
    }
    #endregion

    #region skill-intent-ordered-write
    /// <summary>
    /// Intent: lock, then the minutes. Each write is awaited, so the lock's consequences (the fit
    /// holding this interval) are in place before the minutes land.
    /// </summary>
    public async Task LockMinutes(int minutes)
    {
        await SetMinutesLocked(true);
        await SetMinutes(minutes);
    }
    #endregion

    /// <summary>
    /// The root's fit writes this interval's share.
    /// </summary>
    Task IIntervalFit.ApplyFit(int minutes) => SetMinutes(minutes);

    [Create]
    public async Task Create(string name, int effort)
    {
        Name = name;
        Effort = effort;

        // Paused: no rule ran for the assignments above. Run this interval's own rules, so it
        // shows its next-mark facts and enters invalid if its name is blank. It has no children,
        // so running all of them marks nothing else.
        await RunRules();
    }
}

[Factory]
internal partial class IntervalList : EntityListBase<IInterval>, IIntervalList
{
    [Create]
    public void Create()
    {
    }
}
