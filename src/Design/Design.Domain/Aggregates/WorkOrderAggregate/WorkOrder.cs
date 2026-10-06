// -----------------------------------------------------------------------------
// Design.Domain - WorkOrder Aggregate Root
// -----------------------------------------------------------------------------
// The rule-placement patterns, compiled: a CanX admission rule, visibility
// flags, aggregation over children, a chained rule, the parent as orchestrator
// (writing children and re-running their rules), a 4-trigger array, and an
// entity verb that sets state without checking and throwing.
//
// Persistence follows the Order aggregate exactly: the root makes or gets its
// row, hands the row's task collection to the list factory's Save, and flushes
// once. See Aggregates/OrderAggregate for the commentary on that cascade.
// -----------------------------------------------------------------------------

using System.Linq.Expressions;
using Neatoo;
using Neatoo.RemoteFactory;

namespace Design.Domain.Aggregates.WorkOrderAggregate;

/// <summary>
/// Work order aggregate root.
/// </summary>
[Factory]
internal partial class WorkOrder : EntityBase<WorkOrder>, IWorkOrder
{
    public partial Guid Id { get; set; }

    // Set by verbs and factory operations only
    public partial string? Status { get; private set; }
    public partial string? ApprovedBy { get; private set; }

    public partial bool IsOnHold { get; set; }
    public partial decimal Budget { get; set; }
    public partial decimal Discount { get; set; }

    // Derived: written by rules, read by everyone
    public partial decimal TotalCost { get; private set; }
    public partial bool IsOverBudget { get; private set; }
    public partial bool CanApprove { get; private set; }
    public partial bool ShowHoldBanner { get; private set; }
    public partial bool HasUnschedulableTasks { get; private set; }
    public partial string? Summary { get; private set; }

    public partial IWorkOrderTaskList? Tasks { get; set; }

    public WorkOrder(IEntityBaseServices<WorkOrder> services) : base(services)
    {
        #region skill-can-x-rule
        // Admission for the Approve verb. The UI binds the button to it; the
        // verb itself does not check and throw.
        RuleManager.AddAction(
            t => t.CanApprove = t.Status == "Pending",
            t => t.Status);
        #endregion

        #region skill-visibility-rules
        // A show/hide decision is domain state. The UI binds @if (ShowHoldBanner).
        RuleManager.AddAction(
            t => t.ShowHoldBanner = t.IsOnHold && t.Status != "Closed",
            t => t.IsOnHold,
            t => t.Status);

        // A flag derived from a child property. The trigger is the child's
        // property path, so any task's IsSchedulable change recomputes it.
        RuleManager.AddAction(
            t => t.HasUnschedulableTasks = t.Tasks?.Any(task => !task.IsSchedulable) ?? false,
            t => t.Tasks![0].IsSchedulable);
        #endregion

        #region skill-child-aggregation
        // Aggregation over the children, recomputed when any task's Cost changes
        RuleManager.AddAction(
            t => t.TotalCost = t.Tasks?.Sum(task => task.Cost) ?? 0,
            t => t.Tasks![0].Cost);
        #endregion

        #region skill-chained-rule
        // Chained: the rule above sets TotalCost, which triggers this one.
        // Hours -> Cost (on the task) -> TotalCost -> IsOverBudget, no handler code.
        RuleManager.AddAction(
            t => t.IsOverBudget = t.TotalCost > t.Budget,
            t => t.TotalCost,
            t => t.Budget);
        #endregion

        #region skill-parent-orchestrator
        // Parent writes children: when the discount changes, every task
        // re-applies it through its own business method.
        RuleManager.AddAction(
            t =>
            {
                foreach (var task in t.Tasks ?? Enumerable.Empty<IWorkOrderTask>())
                {
                    task.ApplyParentDiscount();
                }
            },
            t => t.Discount);

        // A child rule that READS root state (IsSchedulable reads IsOnHold) has
        // no trigger for it. The root re-runs the children's rules when it changes.
        RuleManager.AddActionAsync(
            async t =>
            {
                foreach (var task in t.Tasks ?? Enumerable.Empty<IWorkOrderTask>())
                {
                    await task.RunRules();
                }
            },
            t => t.IsOnHold);
        #endregion

        #region skill-four-trigger-array
        // Four or more triggers take an explicit array: params is incompatible
        // with the CallerArgumentExpression that gives the rule its id.
        RuleManager.AddAction(
            t => t.Summary = $"{t.Status}; on hold: {t.IsOnHold}; budget {t.Budget}; discount {t.Discount}",
            new Expression<Func<WorkOrder, object?>>[]
            {
                t => t.Status, t => t.IsOnHold, t => t.Budget, t => t.Discount
            });
        #endregion
    }

    #region skill-entity-verb
    /// <summary>
    /// The verb sets state. Admission is the CanApprove rule, which the UI binds;
    /// the verb does not check and throw.
    /// </summary>
    public void Approve(string approver)
    {
        ApprovedBy = approver;
        Status = "Approved";
    }
    #endregion

    #region skill-create-with-run-rules
    [Create]
    public async Task Create([Service] IWorkOrderTaskListFactory tasksFactory)
    {
        Tasks = tasksFactory.Create();
        Status = "Pending";

        // The object is paused: the assignment above ran no rule, so CanApprove
        // is still false. Force the rules so the new object is consistent.
        await RunRules(RunRulesFlag.All);
    }
    #endregion

    [Remote]
    [Fetch]
    internal async Task<bool> Fetch(Guid id,
        [Service] IWorkOrderRepository repository,
        [Service] IWorkOrderTaskListFactory tasksFactory)
    {
        var row = repository.Get(id);
        if (row == null)
        {
            return false;
        }

        Id = row.Id;
        Status = row.Status;
        ApprovedBy = row.ApprovedBy;
        IsOnHold = row.IsOnHold;
        Budget = row.Budget;
        Discount = row.Discount;
        TotalCost = row.TotalCost;

        Tasks = tasksFactory.Fetch(row.Tasks);

        // The root's own derived values (CanApprove, ShowHoldBanner, IsOverBudget,
        // Summary) are not persisted: re-run the rules that compute them. Only
        // rules that write THIS object - the tasks finished their own [Fetch]
        // and are no longer paused, so a rule that wrote them would mark them
        // modified. Their derived values (Cost, IsSchedulable) are loaded from
        // their rows, as OrderItem loads LineTotal.
        await RunRules(nameof(Status));
        await RunRules(nameof(Budget));
        return true;
    }

    [Remote]
    [Insert]
    internal async Task Insert([Service] IWorkOrderRepository repository,
        [Service] IWorkOrderTaskListFactory tasksFactory)
    {
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        Id = Guid.NewGuid();

        var row = new WorkOrderRow();
        MapTo(row);
        repository.Add(row);

        tasksFactory.Save(Tasks!, row.Tasks);

        repository.SaveChanges();
    }

    [Remote]
    [Update]
    internal async Task Update([Service] IWorkOrderRepository repository,
        [Service] IWorkOrderTaskListFactory tasksFactory)
    {
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        var row = repository.Get(Id)
            ?? throw new KeyNotFoundException($"Work order {Id} not found");

        if (IsSelfModified)
        {
            MapTo(row);
        }

        tasksFactory.Save(Tasks!, row.Tasks);

        repository.SaveChanges();
    }

    [Remote]
    [Delete]
    internal void Delete([Service] IWorkOrderRepository repository)
    {
        var row = repository.Get(Id)
            ?? throw new KeyNotFoundException($"Work order {Id} not found");

        repository.Remove(row);

        repository.SaveChanges();
    }

    private void MapTo(WorkOrderRow row)
    {
        row.Id = Id;
        row.Status = Status!;
        row.ApprovedBy = ApprovedBy;
        row.IsOnHold = IsOnHold;
        row.Budget = Budget;
        row.Discount = Discount;
        row.TotalCost = TotalCost;
    }
}

// =============================================================================
// Persistence - Rows and Repository (see OrderAggregate for the model)
// =============================================================================

public class WorkOrderRow
{
    public Guid Id { get; set; }
    public string Status { get; set; } = "";
    public string? ApprovedBy { get; set; }
    public bool IsOnHold { get; set; }
    public decimal Budget { get; set; }
    public decimal Discount { get; set; }
    public decimal TotalCost { get; set; }
    public List<WorkOrderTaskRow> Tasks { get; } = new();
}

public class WorkOrderTaskRow
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public int Sequence { get; set; }
    public decimal Hours { get; set; }
    public decimal Rate { get; set; }
    public decimal Discount { get; set; }
    public decimal Cost { get; set; }
    public bool IsSchedulable { get; set; }

    /// <summary>Denormalized from the root at save time (see WorkOrderTask.MapTo).</summary>
    public string WorkOrderStatus { get; set; } = "";
}

public interface IWorkOrderRepository
{
    /// <summary>The work order row with its task rows, or null if there is none.</summary>
    WorkOrderRow? Get(Guid id);

    void Add(WorkOrderRow row);

    /// <summary>Removes the work order row and its task rows.</summary>
    void Remove(WorkOrderRow row);

    void SaveChanges();
}
