using Neatoo.RemoteFactory;
using System.ComponentModel.DataAnnotations;

namespace Neatoo.UnitTest.Integration.Aggregates.SaveLifecycle;

/// <summary>
/// Child entity for the end-to-end save lifecycle tests.
/// </summary>
/// <remarks>
/// Loads through its own [Fetch] and persists through local (non-[Remote])
/// [Insert]/[Update] that require the parent's identity - so only the
/// aggregate's save flow can reach them. Insert and Update share a parameter
/// list, so the generated factory exposes a single Save(target, invoiceId)
/// routed on the child's own IsDeleted/IsNew.
/// </remarks>
public interface IInvoiceLine : IEntityBase
{
    int Id { get; }
    string? Description { get; set; }
    decimal Amount { get; set; }
}

[Factory]
internal partial class InvoiceLine : EntityBase<InvoiceLine>, IInvoiceLine
{
    public InvoiceLine(IEntityBaseServices<InvoiceLine> services) : base(services) { }

    public partial int Id { get; set; }

    [Required(ErrorMessage = "Description is required")]
    public partial string? Description { get; set; }

    public partial decimal Amount { get; set; }

    [Create]
    public void Create() { }

    [Create]
    public void Create(string description, decimal amount)
    {
        Description = description;
        Amount = amount;
    }

    [Fetch]
    internal void Fetch(int id, string description, decimal amount)
    {
        Id = id;
        Description = description;
        Amount = amount;
    }

    [Insert]
    internal void Insert(int invoiceId)
    {
        Id = SaveLifecycleStore.InsertLine(invoiceId, Description!, Amount);
    }

    [Update]
    internal void Update(int invoiceId)
    {
        // invoiceId unused - shared signature so the generator emits one Save overload
        SaveLifecycleStore.UpdateLine(Id, Description!, Amount);
    }
}
