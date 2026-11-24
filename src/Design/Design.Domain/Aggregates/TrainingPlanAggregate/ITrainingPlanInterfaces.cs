// -----------------------------------------------------------------------------
// Design.Domain - TrainingPlan aggregate interfaces
// -----------------------------------------------------------------------------
// The rules-first aggregate at the size of a real editing screen: a plan whose
// approved minutes are shared across its intervals by effort, a slider per
// interval, locks, and a gesture that fits the others around the interval the
// person just moved.
//
// Read every member as one of three things:
//   - a FACT: a property a later reader needs. Writing it is open; the rules
//     that follow do not care who wrote it (ApprovedMinutes, Effort,
//     MinutesLocked).
//   - a CONSEQUENCE: a property a rule maintains (Minutes from the fit,
//     PlannedMinutes, CanApprove, NextEffortUp).
//   - an INTENT: a verb, kept only where state cannot hold what the gesture
//     carries (SlideMinutes, LockMinutes, AddInterval, Approve).
//
// The interfaces are partial so the generator adds the awaitable Set{Name}
// for each public-set fact: other types write through the interface.
// -----------------------------------------------------------------------------

using Neatoo;

namespace Design.Domain.Aggregates.TrainingPlanAggregate;

#region skill-training-plan-interface
/// <summary>
/// Training plan aggregate root.
/// </summary>
public partial interface ITrainingPlan : IEntityRoot
{
    /// <summary>Fact: the minutes the session is approved for. The fit shares them out.</summary>
    int ApprovedMinutes { get; set; }

    /// <summary>Consequence: the sum of the intervals' minutes.</summary>
    int PlannedMinutes { get; }

    string Status { get; }
    string? ApprovedBy { get; }

    /// <summary>Consequence: admission for Approve, maintained by a rule that reads IsValid.</summary>
    bool CanApprove { get; }

    IIntervalList? Intervals { get; }

    /// <summary>Intent: a new interval and its effort land together.</summary>
    Task<IInterval> AddInterval(string name, int effort);

    /// <summary>Intent: a stamp and a status, in that order.</summary>
    Task Approve(string approver);
}

/// <summary>
/// One interval of the plan.
/// </summary>
public partial interface IInterval : IEntityBase
{
    string Name { get; set; }

    /// <summary>Fact: the slider. Writing it re-weights the fit.</summary>
    int Effort { get; set; }

    /// <summary>Fact: a locked interval keeps its minutes; the fit shares the rest.</summary>
    bool MinutesLocked { get; set; }

    /// <summary>Consequence of the fit, or the value a gesture below set.</summary>
    int Minutes { get; }

    /// <summary>Consequences the UI writes into Effort to nudge it: a nudge is a plain write.</summary>
    int NextEffortUp { get; }
    int NextEffortDown { get; }

    /// <summary>Intent: set the minutes and fit the others around this interval, for this one fit.</summary>
    Task SlideMinutes(int minutes);

    /// <summary>Intent: lock, then set the minutes. The order is the meaning.</summary>
    Task LockMinutes(int minutes);
}

public interface IIntervalList : IEntityListBase<IInterval> { }
#endregion

/// <summary>
/// How the root writes a child's minutes: parents write children through the child's own method.
/// </summary>
internal interface IIntervalFit
{
    Task ApplyFit(int minutes);
}

/// <summary>
/// How a child's gesture tells the root's fit which interval to hold for one fit.
/// </summary>
internal interface ITrainingPlanFit
{
    IDisposable AnchorFit(IInterval interval);
}
