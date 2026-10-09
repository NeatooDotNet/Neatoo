// -----------------------------------------------------------------------------
// Design.Domain - Product (Minimal Aggregate Root)
// -----------------------------------------------------------------------------
// The smallest complete root: a public interface, an internal partial class,
// a local [Create], and [Remote] internal persistence operations that take a
// server-side repository. No children. This is the shape the skill's quick
// start shows; Employee and Order add child collections to it.
// -----------------------------------------------------------------------------

using Neatoo;
using Neatoo.RemoteFactory;
using System.ComponentModel.DataAnnotations;

namespace Design.Domain.Entities;

#region skill-quick-start-interface
/// <summary>
/// Aggregate root interface. Extends IEntityRoot: exposes IsSavable and Save().
/// </summary>
public interface IProduct : IEntityRoot
{
    Guid Id { get; }
    string? Name { get; set; }
    decimal Price { get; set; }
}
#endregion

#region skill-quick-start
/// <summary>
/// Demonstrates: the minimal aggregate root. Concrete is internal; consumers
/// hold IProduct.
/// </summary>
[Factory]
internal partial class Product : EntityBase<Product>, IProduct
{
    public partial Guid Id { get; set; }

    [Required(ErrorMessage = "Name is required")]
    public partial string? Name { get; set; }

    [Range(0, 1000000, ErrorMessage = "Price cannot be negative")]
    public partial decimal Price { get; set; }

    public Product(IEntityBaseServices<Product> services) : base(services) { }

    // Local: creating a product needs nothing from the server
    [Create]
    public void Create() { }

    // [Remote]: the client fetches this root, so the call crosses to the
    // server, where the repository resolves. Returning false makes the
    // generated factory return null: "no such product" is an answer.
    [Remote]
    [Fetch]
    internal bool Fetch(Guid id, [Service] IProductRepository repository)
    {
        var row = repository.Get(id);
        if (row == null)
        {
            return false;
        }

        // Paused for the length of the body: assignment is a clean baseline load
        Id = row.Id;
        Name = row.Name;
        Price = row.Price;
        return true;
    }

    [Remote]
    [Insert]
    internal async Task Insert([Service] IProductRepository repository)
    {
        // Re-run the rules on the server and refuse an invalid aggregate.
        // Throw, never return: after [Insert] returns, the framework marks
        // the entity saved whether or not anything was written.
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        Id = Guid.NewGuid();  // the entity sets its own key

        var row = new ProductRow();
        MapTo(row);
        repository.Add(row);
        repository.SaveChanges();
    }

    [Remote]
    [Update]
    internal async Task Update([Service] IProductRepository repository)
    {
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        var row = repository.Get(Id)
            ?? throw new KeyNotFoundException($"Product {Id} not found");

        MapTo(row);
        repository.SaveChanges();
    }

    [Remote]
    [Delete]
    internal void Delete([Service] IProductRepository repository)
    {
        var row = repository.Get(Id)
            ?? throw new KeyNotFoundException($"Product {Id} not found");

        repository.Remove(row);
        repository.SaveChanges();
    }

    private void MapTo(ProductRow row)
    {
        row.Id = Id;
        row.Name = Name!;
        row.Price = Price;
    }
}
#endregion

// =============================================================================
// Persistence - Row and Repository
// =============================================================================
// Modeled on an EF Core unit of work: the row stands in for an EF entity and
// SaveChanges flushes everything added, changed or removed since the last flush.
// =============================================================================

public class ProductRow
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
}

public interface IProductRepository
{
    /// <summary>The product row, or null if there is none.</summary>
    ProductRow? Get(Guid id);

    void Add(ProductRow row);

    void Remove(ProductRow row);

    void SaveChanges();
}
