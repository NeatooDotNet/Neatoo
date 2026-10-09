// -----------------------------------------------------------------------------
// Design.Domain - WorkOrderTaskList
// -----------------------------------------------------------------------------
// The list's own [Fetch] and [Update] follow OrderItemList exactly. What this
// list adds is cross-sibling consistency: when one task's Sequence changes,
// the other tasks' uniqueness rule has to be re-evaluated, and only the list
// sees all the siblings.
// -----------------------------------------------------------------------------

using Neatoo;
using Neatoo.RemoteFactory;

namespace Design.Domain.Aggregates.WorkOrderAggregate;

/// <summary>
/// Work order task collection.
/// </summary>
[Factory]
internal partial class WorkOrderTaskList : EntityListBase<IWorkOrderTask>, IWorkOrderTaskList
{
    [Create]
    public void Create()
    {
    }

    #region skill-cross-sibling-rules
    // Cross-sibling consistency lives on the LIST: an entity cannot override
    // HandleNeatooPropertyChanged, and only the list sees every sibling. When a
    // task's Sequence changes, re-run the siblings' rules so their uniqueness
    // messages update too.
    protected override async Task HandleNeatooPropertyChanged(NeatooPropertyChangedEventArgs eventArgs)
    {
        await base.HandleNeatooPropertyChanged(eventArgs);

        if (eventArgs.PropertyName == nameof(IWorkOrderTask.Sequence)
            && eventArgs.Source is IWorkOrderTask changed)
        {
            await Task.WhenAll(this.Except([changed])
                .Select(sibling => sibling.RunRules(nameof(IWorkOrderTask.Sequence))));
        }
    }
    #endregion

    [Fetch]
    internal void Fetch(IEnumerable<WorkOrderTaskRow> rows,
                        [Service] IWorkOrderTaskFactory taskFactory)
    {
        foreach (var row in rows)
        {
            Add(taskFactory.Fetch(row));
        }
    }

    [Update]
    internal void Update(ICollection<WorkOrderTaskRow> rows,
                         [Service] IWorkOrderTaskFactory taskFactory)
    {
        foreach (var task in this.Union(DeletedList))
        {
            if (task.IsDeleted)
            {
                if (!task.IsNew)
                {
                    rows.Remove(rows.Single(r => r.Id == task.Id));
                }
            }
            else if (task.IsNew)
            {
                var row = new WorkOrderTaskRow();
                rows.Add(row);
                taskFactory.Save(task, row);
            }
            else if (task.IsModified)
            {
                taskFactory.Save(task, rows.Single(r => r.Id == task.Id));
            }
        }
    }
}
