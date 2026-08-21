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
}
