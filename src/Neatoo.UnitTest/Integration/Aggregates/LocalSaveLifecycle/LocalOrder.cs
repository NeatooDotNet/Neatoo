using Neatoo.RemoteFactory;
using System.ComponentModel.DataAnnotations;

namespace Neatoo.UnitTest.Integration.Aggregates.LocalSaveLifecycle;

/// <summary>
/// Aggregate root for the LOCAL (fat-client) save lifecycle tests.
/// </summary>
/// <remarks>
/// Deliberately the mirror image of <c>Invoice</c>: identical canonical shape, but
/// NO [Remote] on any operation, so a Save() completes entirely in-process with no
/// serialization round trip.
///
/// That difference is the whole point of this fixture. A [Remote] save returns a
/// deserialized graph, and OnDeserialized rebuilds meta-state baselines from scratch -
/// which silently repaired the LIST-003 stale-baseline defect before any assertion
/// could see it. Every SaveLifecycle fixture was [Remote], so the defect was invisible
/// to the whole suite. Keep these operations local.
/// </remarks>
public interface ILocalOrder : IEntityRoot
{
    int Id { get; }
    string? Customer { get; set; }
    ILocalOrderLineList? Lines { get; }
}

[Factory]
internal partial class LocalOrder : EntityBase<LocalOrder>, ILocalOrder
{
    public LocalOrder(IEntityBaseServices<LocalOrder> services) : base(services) { }

    public partial int Id { get; set; }

    [Required(ErrorMessage = "Customer is required")]
    public partial string? Customer { get; set; }

    public partial ILocalOrderLineList? Lines { get; set; }

    [Create]
    public void Create([Service] ILocalOrderLineListFactory lineListFactory)
    {
        Lines = lineListFactory.Create();
    }

    [Fetch]
    internal void Fetch(int id, string customer, [Service] ILocalOrderLineListFactory lineListFactory)
    {
        this["Id"].LoadValue(id);
        this["Customer"].LoadValue(customer);

        Lines = lineListFactory.Fetch(id);
    }

    [Insert]
    internal void Insert([Service] ILocalOrderLineListFactory lineListFactory)
    {
        Id = LocalSaveStore.NextOrderId();
        lineListFactory.Save(Lines!, Id);
    }

    [Update]
    internal void Update([Service] ILocalOrderLineListFactory lineListFactory)
    {
        lineListFactory.Save(Lines!, Id);
    }

    [Delete]
    internal void Delete()
    {
    }
}
