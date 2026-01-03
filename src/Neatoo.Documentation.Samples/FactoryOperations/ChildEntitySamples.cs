/// <summary>
/// Code samples for docs/factory-operations.md - Child entity operations section
///
/// Snippets in this file:
/// - docs:factory-operations:child-entity
/// - docs:factory-operations:list-factory
///
/// Corresponding tests: ChildEntitySamplesTests.cs
/// </summary>

using Neatoo.RemoteFactory;

namespace Neatoo.Documentation.Samples.FactoryOperations;

/// <summary>
/// Mock EF entity for invoice line items.
/// </summary>
public class InvoiceLineEntity
{
    public Guid Id { get; set; }
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
}

#region docs:factory-operations:child-entity
/// <summary>
/// Child entity - no [Remote] since managed through parent.
/// </summary>
public partial interface IInvoiceLine : IEntityBase
{
    Guid Id { get; }
    string? Description { get; set; }
    decimal Amount { get; set; }
}

[Factory]
internal partial class InvoiceLine : EntityBase<InvoiceLine>, IInvoiceLine
{
    public InvoiceLine(IEntityBaseServices<InvoiceLine> services) : base(services) { }

    public partial Guid Id { get; set; }
    public partial string? Description { get; set; }
    public partial decimal Amount { get; set; }

    [Create]
    public void Create()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// No [Remote] - called by parent factory.
    /// </summary>
    [Fetch]
    public void Fetch(InvoiceLineEntity entity)
    {
        Id = entity.Id;
        Description = entity.Description;
        Amount = entity.Amount;
    }

    /// <summary>
    /// Insert populates the EF entity for parent to save.
    /// </summary>
    [Insert]
    public void Insert(InvoiceLineEntity entity)
    {
        entity.Id = Id;
        entity.Description = Description ?? "";
        entity.Amount = Amount;
    }

    /// <summary>
    /// Update only transfers modified properties.
    /// </summary>
    [Update]
    public void Update(InvoiceLineEntity entity)
    {
        if (this[nameof(Description)].IsModified)
            entity.Description = Description ?? "";
        if (this[nameof(Amount)].IsModified)
            entity.Amount = Amount;
    }
}
#endregion

#region docs:factory-operations:list-factory
/// <summary>
/// List factory handles collection of child entities.
/// </summary>
public interface IInvoiceLineList : IEntityListBase<IInvoiceLine> { }

[Factory]
internal class InvoiceLineList : EntityListBase<IInvoiceLine>, IInvoiceLineList
{
    [Create]
    public void Create() { }

    /// <summary>
    /// Fetch populates list from EF entities.
    /// </summary>
    [Fetch]
    public void Fetch(IEnumerable<InvoiceLineEntity> entities,
                      [Service] IInvoiceLineFactory lineFactory)
    {
        foreach (var entity in entities)
        {
            var line = lineFactory.Fetch(entity);
            Add(line);
        }
    }

    /// <summary>
    /// Save handles insert/update/delete for all items.
    /// </summary>
    [Update]
    public void Update(ICollection<InvoiceLineEntity> entities,
                       [Service] IInvoiceLineFactory lineFactory)
    {
        foreach (var line in this.Union(DeletedList))
        {
            InvoiceLineEntity entity;

            if (line.IsNew)
            {
                entity = new InvoiceLineEntity();
                entities.Add(entity);
            }
            else
            {
                entity = entities.Single(e => e.Id == line.Id);
            }

            if (line.IsDeleted)
            {
                entities.Remove(entity);
            }
            else
            {
                lineFactory.Save(line, entity);
            }
        }
    }
}
#endregion
