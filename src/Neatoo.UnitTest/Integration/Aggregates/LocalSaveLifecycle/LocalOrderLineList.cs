using Neatoo.RemoteFactory;

namespace Neatoo.UnitTest.Integration.Aggregates.LocalSaveLifecycle;

/// <summary>
/// Child collection of the local-save aggregate.
/// </summary>
public interface ILocalOrderLineList : IEntityListBase<ILocalOrderLine>
{
    int DeletedCount { get; }
}

[Factory]
internal partial class LocalOrderLineList : EntityListBase<ILocalOrderLine>, ILocalOrderLineList
{
    public int DeletedCount => DeletedList.Count;

    [Create]
    public void Create() { }

    [Fetch]
    internal void Fetch(int orderId, [Service] ILocalOrderLineFactory lineFactory)
    {
        Add(lineFactory.Fetch(1, "Fetched line one"));
        Add(lineFactory.Fetch(2, "Fetched line two"));
    }

    /// <summary>
    /// Persists removals and survivors. FactoryComplete(Update) is what clears the
    /// DeletedList afterwards - and, since LIST-003, corrects the meta-state baseline
    /// that the resume captured while the DeletedList was still populated.
    /// </summary>
    [Update]
    internal void Update(int orderId, [Service] ILocalOrderLineFactory lineFactory)
    {
        foreach (var line in this.Union(DeletedList).ToList())
        {
            if (line.IsDeleted)
            {
                if (!line.IsNew)
                {
                    LocalSaveStore.DeleteLine(line.Id);
                }
            }
            else if (line.IsNew || line.IsModified)
            {
                lineFactory.Save(line, orderId);
            }
        }
    }
}
