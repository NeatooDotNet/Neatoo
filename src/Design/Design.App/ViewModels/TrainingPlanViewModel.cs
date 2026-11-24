// -----------------------------------------------------------------------------
// Design.App - The TrainingPlan Screen's ViewModel
// -----------------------------------------------------------------------------
// The rules-first screen. Nearly every control writes a fact on the aggregate
// directly, through its MudNeatoo input; the ViewModel holds the plan and
// forwards the two gestures that are not a control's: adding an interval and
// approving. It decides nothing: CanApprove is the aggregate's admission fact.
//
// DESIGN DECISION: the default effort of a new interval is an input default the
// person changes with the slider at once, not a decision; the fit follows it.
// -----------------------------------------------------------------------------

using Design.Domain.Aggregates.TrainingPlanAggregate;

namespace Design.App.ViewModels;

#region skill-mud-training-plan-viewmodel
/// <summary>
/// ViewModel for the TrainingPlan screen: holds the plan and forwards its two gestures.
/// </summary>
internal sealed class TrainingPlanViewModel
{
    private readonly ITrainingPlanFactory _factory;

    public TrainingPlanViewModel(ITrainingPlanFactory factory)
    {
        _factory = factory;
    }

    /// <summary>The aggregate the page and its components bind to. Null until created.</summary>
    public ITrainingPlan? Plan { get; private set; }

    public async Task New()
    {
        Plan = await _factory.Create();
    }

    /// <summary>A new interval joins the plan; the fit shares the minutes again.</summary>
    public async Task AddInterval(string name)
    {
        if (Plan is not null)
        {
            await Plan.AddInterval(name, 5);
        }
    }

    /// <summary>Calls the verb. The Approve button is enabled by the plan's own CanApprove.</summary>
    public async Task Approve(string approver)
    {
        if (Plan is not null)
        {
            await Plan.Approve(approver);
        }
    }
}
#endregion
