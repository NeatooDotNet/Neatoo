// -----------------------------------------------------------------------------
// Design.Domain - WorkOrder Aggregate Interfaces
// -----------------------------------------------------------------------------
// Interface-first, as for the Order aggregate: public interfaces, internal
// concretes. Derived values (CanApprove, TotalCost, Cost, ...) are get-only
// here; the concretes declare them `private set` and rules write them.
// -----------------------------------------------------------------------------

using Neatoo;

namespace Design.Domain.Aggregates.WorkOrderAggregate;

/// <summary>
/// Work order aggregate root.
/// </summary>
public interface IWorkOrder : IEntityRoot
{
    Guid Id { get; }
    string? Status { get; }
    string? ApprovedBy { get; }
    bool IsOnHold { get; set; }
    decimal Budget { get; set; }
    decimal Discount { get; set; }
    decimal TotalCost { get; }
    bool IsOverBudget { get; }
    bool CanApprove { get; }
    bool ShowHoldBanner { get; }
    bool HasUnschedulableTasks { get; }
    string? Summary { get; }
    IWorkOrderTaskList? Tasks { get; }

    void Approve(string approver);
}

/// <summary>
/// Work order task. Child entity: no Save(), no IsSavable.
/// </summary>
public interface IWorkOrderTask : IEntityBase
{
    Guid Id { get; }
    string? Name { get; set; }
    int Sequence { get; set; }
    decimal Hours { get; set; }
    decimal Rate { get; set; }
    decimal Discount { get; }
    decimal Cost { get; }
    bool IsSchedulable { get; }

    void ApplyParentDiscount();
}

public interface IWorkOrderTaskList : IEntityListBase<IWorkOrderTask>
{
}
