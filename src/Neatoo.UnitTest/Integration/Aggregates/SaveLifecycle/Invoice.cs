using Neatoo.RemoteFactory;
using System.ComponentModel.DataAnnotations;

namespace Neatoo.UnitTest.Integration.Aggregates.SaveLifecycle;

/// <summary>
/// Aggregate root for the end-to-end save lifecycle tests.
/// </summary>
/// <remarks>
/// Follows the canonical lifecycle: children load through the list factory's
/// [Fetch], and child persistence is delegated to the list factory's Save so
/// every object in the graph completes its own factory operation.
/// Persistence operations are [Remote] so saves genuinely cross the
/// client/server boundary in the two-container harness.
/// </remarks>
public interface IInvoice : IEntityRoot
{
    int Id { get; }
    string? Customer { get; set; }
    IInvoiceLineList? Lines { get; }
}

[Factory]
internal partial class Invoice : EntityBase<Invoice>, IInvoice
{
    public Invoice(IEntityBaseServices<Invoice> services) : base(services) { }

    public partial int Id { get; set; }

    [Required(ErrorMessage = "Customer is required")]
    public partial string? Customer { get; set; }

    public partial IInvoiceLineList? Lines { get; set; }

    [Create]
    public void Create([Service] IInvoiceLineListFactory lineListFactory)
    {
        Lines = lineListFactory.Create();
    }

    /// <summary>
    /// Rich create: populates the aggregate inside the paused factory operation,
    /// so the result is VALID with no property dirt at all.
    /// </summary>
    /// <remarks>
    /// This is what makes the created-untouched case testable: savability can
    /// only come from the IsNew term, never from a setter the test called.
    /// The child lines are added while the LIST is paused by its own factory
    /// operation, so they are baseline population rather than user work - the
    /// case design.md targets with "Created, untouched, incl. factory-populated
    /// children".
    /// </remarks>
    [Create]
    public void CreateForCustomer(string customer, [Service] IInvoiceLineListFactory lineListFactory)
    {
        Customer = customer;
        Lines = lineListFactory.CreateWithStandardLines();
    }

    /// <summary>
    /// A create whose result IS the user's work - the "New Invoice button" case -
    /// which opts into modified state explicitly.
    /// </summary>
    /// <remarks>
    /// This is the opt-in half of the IsNew/IsModified split. Nothing here is
    /// needed to make the invoice SAVABLE (IsSavable admits IsNew on its own);
    /// MarkModified() says something different — that discarding this object
    /// would lose something the user did, so unsaved-changes guards should speak.
    /// </remarks>
    [Create]
    public void CreateAsUserWork(string customer, [Service] IInvoiceLineListFactory lineListFactory)
    {
        Customer = customer;
        Lines = lineListFactory.Create();
        MarkModified();
    }

    /// <summary>
    /// Probe for whether a server-side MarkModified() survives serialization.
    /// </summary>
    /// <remarks>
    /// Fetch (not Create), so IsNew is false and the only possible source of
    /// modified state on the client is the marked flag crossing the wire.
    /// </remarks>
    [Remote]
    [Fetch]
    internal void FetchOptInProbe(string customer, [Service] IInvoiceLineListFactory lineListFactory)
    {
        this["Customer"].LoadValue(customer);
        Lines = lineListFactory.Create();
        MarkModified();
    }

    [Remote]
    [Fetch]
    internal void Fetch(int id, [Service] IInvoiceLineListFactory lineListFactory)
    {
        var row = SaveLifecycleStore.GetInvoice(id);

        this["Id"].LoadValue(row.Id);
        this["Customer"].LoadValue(row.Customer);

        Lines = lineListFactory.Fetch(id);
    }

    [Remote]
    [Insert]
    internal void Insert([Service] IInvoiceLineListFactory lineListFactory)
    {
        Id = SaveLifecycleStore.InsertInvoice(Customer!);
        lineListFactory.Save(Lines!, Id);
    }

    [Remote]
    [Update]
    internal void Update([Service] IInvoiceLineListFactory lineListFactory)
    {
        if (IsSelfModified)
        {
            SaveLifecycleStore.UpdateInvoice(Id, Customer!);
        }

        lineListFactory.Save(Lines!, Id);
    }

    /// <summary>
    /// Present so the created-then-deleted path can actually be saved.
    /// </summary>
    /// <remarks>
    /// Without a [Delete] in this signature group the generated routing throws
    /// NotImplementedException on the IsDeleted branch; with one, it also gains
    /// the new-and-deleted short-circuit that returns without touching
    /// persistence. Both behaviors are asserted by DecoupledSemanticsTests.
    /// </remarks>
    [Remote]
    [Delete]
    internal void Delete()
    {
        SaveLifecycleStore.DeleteInvoice(Id);
    }
}
