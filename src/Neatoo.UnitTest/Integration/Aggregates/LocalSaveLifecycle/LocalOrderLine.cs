using Neatoo.RemoteFactory;

namespace Neatoo.UnitTest.Integration.Aggregates.LocalSaveLifecycle;

/// <summary>
/// Child entity of the local-save aggregate.
/// </summary>
public interface ILocalOrderLine : IEntityBase
{
    int Id { get; }
    string? Description { get; set; }
}

[Factory]
internal partial class LocalOrderLine : EntityBase<LocalOrderLine>, ILocalOrderLine
{
    public LocalOrderLine(IEntityBaseServices<LocalOrderLine> services) : base(services) { }

    public partial int Id { get; set; }

    public partial string? Description { get; set; }

    [Create]
    public void Create(string description)
    {
        Description = description;
    }

    /// <summary>
    /// Loads a persisted line. Uses LoadValue so the fetch leaves no property dirt.
    /// </summary>
    [Fetch]
    internal void Fetch(int id, string description)
    {
        this["Id"].LoadValue(id);
        this["Description"].LoadValue(description);
    }

    [Insert]
    internal void Insert(int orderId)
    {
        Id = LocalSaveStore.InsertLine();
    }

    [Update]
    internal void Update(int orderId)
    {
        LocalSaveStore.UpdateLine(Id);
    }
}
