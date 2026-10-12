// -----------------------------------------------------------------------------
// Design.App - A ViewModel Over a Live Aggregate
// -----------------------------------------------------------------------------
// THE QUESTION THE HABIT ASKS: "Where do the screen's derived values and
// visibility go?"
//
// On the entity, as business facts; the screen binds to them. WorkOrder's rules
// already compute CanApprove, IsHeld, IsOverBudget and HasUnschedulableTasks and
// recompute them whenever their triggers change, on this tier, with no round
// trip. The ViewModel holds the aggregate, exposes its verbs, and saves. It
// decides nothing.
//
// DESIGN DECISION: the one thing a ViewModel may add is a screen's own name
// for a fact, mapped one-to-one. ShowHoldBanner below is that: the entity says
// IsHeld (a business fact), the banner is this screen's concern. The mapping
// contains no condition of its own. The entity never carries a property named
// for a screen element.
//
// DID NOT DO THIS: `public bool CanApprove => WorkOrder.Status == "Pending";`
// WHY NOT: that is a business decision made in the UI. When the rule changes,
// this copy of it does not. The entity's CanApprove is the one that is tested.
// -----------------------------------------------------------------------------

using Design.Domain.Aggregates.WorkOrderAggregate;

namespace Design.App.ViewModels;

#region skill-viewmodel-over-aggregate
/// <summary>
/// ViewModel for the WorkOrder screen: holds the aggregate; the page binds to
/// the aggregate's facts and this class's one screen-named mapping.
/// </summary>
internal sealed class WorkOrderViewModel
{
    private readonly IWorkOrderFactory _workOrderFactory;
    private readonly IWorkOrderTaskFactory _taskFactory;

    public WorkOrderViewModel(IWorkOrderFactory workOrderFactory, IWorkOrderTaskFactory taskFactory)
    {
        _workOrderFactory = workOrderFactory;
        _taskFactory = taskFactory;
    }

    /// <summary>The aggregate the page binds to. Null until loaded or created.</summary>
    public IWorkOrder? WorkOrder { get; private set; }

    public async Task New()
    {
        WorkOrder = await _workOrderFactory.Create();
    }

    public async Task Load(Guid id)
    {
        WorkOrder = await _workOrderFactory.Fetch(id);
    }

    public void AddTask(string name)
    {
        if (WorkOrder?.Tasks is null)
        {
            return;
        }

        WorkOrder.Tasks.Add(_taskFactory.Create(name, WorkOrder.Tasks.Count + 1));
    }

    /// <summary>
    /// This screen's name for the entity's IsHeld fact. A mapping, not a decision.
    /// </summary>
    public bool ShowHoldBanner => WorkOrder?.IsHeld ?? false;

    /// <summary>The Approve button binds to the entity's own admission fact.</summary>
    public bool CanApprove => WorkOrder?.CanApprove ?? false;

    /// <summary>Calls the verb; the entity's rules update CanApprove and Status.</summary>
    public void Approve(string approver)
    {
        WorkOrder?.Approve(approver);
    }

    public bool CanSave => WorkOrder?.IsSavable ?? false;

    public async Task Save()
    {
        if (WorkOrder is null)
        {
            return;
        }

        await WorkOrder.WaitForTasks();

        if (!WorkOrder.IsSavable)
        {
            return;
        }

        WorkOrder = (IWorkOrder)await WorkOrder.Save();
    }
}
#endregion
