using Neatoo.RemoteFactory;

namespace Neatoo.UnitTest.Integration.Aggregates.SaveLifecycle;

/// <summary>
/// Child collection for the end-to-end save lifecycle tests.
/// </summary>
/// <remarks>
/// The list's own [Fetch] populates it (paused by its own factory operation,
/// so the adds are baseline loads); its own [Update] deletes removed children
/// from persistence and routes survivors through per-item factory saves. The
/// list's FactoryComplete(Update) is what clears the DeletedList.
/// </remarks>
public interface IInvoiceLineList : IEntityListBase<IInvoiceLine>
{
    int DeletedCount { get; }
}

[Factory]
internal partial class InvoiceLineList : EntityListBase<IInvoiceLine>, IInvoiceLineList
{
    public int DeletedCount => DeletedList.Count;

    [Create]
    public void Create() { }

    /// <summary>
    /// Rich create: populates default lines while the list is paused by its own
    /// factory operation, so the adds are baseline population, not user work.
    /// </summary>
    [Create]
    public void CreateWithStandardLines([Service] IInvoiceLineFactory lineFactory)
    {
        Add(lineFactory.Create("Standard setup", 50.00m));
        Add(lineFactory.Create("Standard support", 75.00m));
    }

    [Fetch]
    internal void Fetch(int invoiceId, [Service] IInvoiceLineFactory lineFactory)
    {
        foreach (var row in SaveLifecycleStore.GetLines(invoiceId))
        {
            Add(lineFactory.Fetch(row.Id, row.Description, row.Amount));
        }
    }

    [Update]
    internal void Update(int invoiceId, [Service] IInvoiceLineFactory lineFactory)
    {
        foreach (var line in this.Union(DeletedList).ToList())
        {
            if (line.IsDeleted)
            {
                // Defensive: new lines removed from the list are discarded,
                // never queued for deletion
                if (!line.IsNew)
                {
                    SaveLifecycleStore.DeleteLine(line.Id);
                }
            }
            else if (line.IsNew || line.IsModified)
            {
                lineFactory.Save(line, invoiceId);
            }
        }
    }
}
