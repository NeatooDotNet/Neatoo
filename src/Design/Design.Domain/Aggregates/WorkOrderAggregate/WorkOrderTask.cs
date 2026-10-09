// -----------------------------------------------------------------------------
// Design.Domain - WorkOrderTask (Child Entity in the WorkOrder Aggregate)
// -----------------------------------------------------------------------------
// A child that reads ambient root state through Parent in three places: a
// rule, a business method, and its persistence mapping. Children read Parent;
// parents write children (see WorkOrder's orchestrator rule).
// -----------------------------------------------------------------------------

using Neatoo;
using Neatoo.RemoteFactory;
using System.ComponentModel.DataAnnotations;

namespace Design.Domain.Aggregates.WorkOrderAggregate;

/// <summary>
/// Work order task. Saved through its work order; maps itself into its row.
/// </summary>
[Factory]
internal partial class WorkOrderTask : EntityBase<WorkOrderTask>, IWorkOrderTask
{
    public partial Guid Id { get; set; }

    [Required(ErrorMessage = "Task name is required")]
    public partial string? Name { get; set; }

    public partial int Sequence { get; set; }
    public partial decimal Hours { get; set; }
    public partial decimal Rate { get; set; }

    // Derived: written by rules or by the parent through ApplyParentDiscount
    public partial decimal Discount { get; private set; }
    public partial decimal Cost { get; private set; }
    public partial bool IsSchedulable { get; private set; }

    public WorkOrderTask(IEntityBaseServices<WorkOrderTask> services) : base(services)
    {
        #region skill-computed-properties
        // A derived value is a private-set partial property written by a rule.
        // It notifies, it serializes, and the UI sees it read-only.
        RuleManager.AddAction(
            t => t.Cost = t.Hours * t.Rate * (1 - t.Discount),
            t => t.Hours,
            t => t.Rate,
            t => t.Discount);
        #endregion

        #region skill-parent-in-child-rule
        // A child rule reads ambient root state through Parent. Parent is null
        // until the task is attached, so pattern-match instead of casting.
        RuleManager.AddAction(
            t => t.IsSchedulable = t.Hours > 0 && t.Parent is IWorkOrder root && !root.IsOnHold,
            t => t.Hours);
        #endregion

        #region skill-sibling-validation
        // Sibling consistency: the message lands on this task. When a Sequence
        // changes, the list re-runs the siblings' rules (see WorkOrderTaskList).
        RuleManager.AddValidation(
            t => t.Parent is IWorkOrder root
                 && root.Tasks!.Any(other => !ReferenceEquals(other, t) && other.Sequence == t.Sequence)
                ? "Sequence must be unique within the work order"
                : string.Empty,
            t => t.Sequence);
        #endregion
    }

    [Create]
    public void Create(string name, int sequence)
    {
        Name = name;
        Sequence = sequence;
    }

    #region skill-parent-in-child-method
    /// <summary>
    /// Child business method reading root state through Parent. Called by the
    /// root's orchestrator rule whenever the discount changes.
    /// </summary>
    public void ApplyParentDiscount()
    {
        Discount = ((IWorkOrder)this.Parent!).Discount;
    }
    #endregion

    [Fetch]
    internal void Fetch(WorkOrderTaskRow row)
    {
        Id = row.Id;
        Name = row.Name;
        Sequence = row.Sequence;
        Hours = row.Hours;
        Rate = row.Rate;
        // Derived values are loaded, not recalculated: rules do not run while
        // the factory operation is paused (as OrderItem loads LineTotal)
        Discount = row.Discount;
        Cost = row.Cost;
        IsSchedulable = row.IsSchedulable;
    }

    [Insert]
    internal void Insert(WorkOrderTaskRow row)
    {
        Id = Guid.NewGuid();
        MapTo(row);
    }

    #region skill-parent-in-child-update
    [Update]
    internal void Update(WorkOrderTaskRow row)
    {
        MapTo(row);
    }

    private void MapTo(WorkOrderTaskRow row)
    {
        row.Id = Id;
        row.Name = Name!;
        row.Sequence = Sequence;
        row.Hours = Hours;
        row.Rate = Rate;
        row.Discount = Discount;
        row.Cost = Cost;
        row.IsSchedulable = IsSchedulable;

        // A denormalized column decided from root state, read through Parent.
        // On a persistence path the task is always attached.
        row.WorkOrderStatus = ((IWorkOrder)this.Parent!).Status!;
    }
    #endregion
}
