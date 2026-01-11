/// <summary>
/// Code samples for docs/factory-operations.md - Fetch operation section
///
/// Snippets in this file:
/// - docs:factory-operations:fetch-basic
/// - docs:factory-operations:fetch-multiple-overloads
///
/// Corresponding tests: FetchOperationSamplesTests.cs
/// </summary>

using Neatoo.RemoteFactory;

namespace Neatoo.Samples.DomainModel.FactoryOperations;

/// <summary>
/// Simple entity for Fetch examples.
/// Uses in-memory data for testability.
/// </summary>
public class ProductData
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Sku { get; set; } = "";
    public decimal Price { get; set; }
}

/// <summary>
/// Mock repository for fetch examples.
/// </summary>
public interface IProductRepository
{
    ProductData? FindById(int id);
    ProductData? FindBySku(string sku);
}

public class MockProductRepository : IProductRepository
{
    private readonly List<ProductData> _products =
    [
        new() { Id = 1, Name = "Widget", Sku = "WDG-001", Price = 9.99m },
        new() { Id = 2, Name = "Gadget", Sku = "GDG-002", Price = 19.99m },
        new() { Id = 3, Name = "Gizmo", Sku = "GZM-003", Price = 29.99m }
    ];

    public ProductData? FindById(int id) => _products.FirstOrDefault(p => p.Id == id);
    public ProductData? FindBySku(string sku) => _products.FirstOrDefault(p => p.Sku == sku);
}

#region fetch-basic
/// <summary>
/// Entity with basic Fetch operation.
/// </summary>
public partial interface IFetchableProduct : IEntityBase
{
    int Id { get; }
    string? Name { get; set; }
    decimal Price { get; set; }
}

[Factory]
internal partial class FetchableProduct : EntityBase<FetchableProduct>, IFetchableProduct
{
    public FetchableProduct(IEntityBaseServices<FetchableProduct> services) : base(services) { }

    public partial int Id { get; set; }
    public partial string? Name { get; set; }
    public partial decimal Price { get; set; }

    [Create]
    public void Create() { }

    [Fetch]
    public bool Fetch(int id, [Service] IProductRepository repo)
    {
        var data = repo.FindById(id);
        if (data == null)
            return false;

        Id = data.Id;
        Name = data.Name;
        Price = data.Price;
        return true;
    }
}
#endregion

#region fetch-multiple-overloads
/// <summary>
/// Entity with multiple Fetch overloads for different lookup methods.
/// </summary>
public partial interface IProductWithMultipleFetch : IEntityBase
{
    int Id { get; }
    string? Name { get; set; }
    string? Sku { get; set; }
    decimal Price { get; set; }
}

[Factory]
internal partial class ProductWithMultipleFetch : EntityBase<ProductWithMultipleFetch>, IProductWithMultipleFetch
{
    public ProductWithMultipleFetch(IEntityBaseServices<ProductWithMultipleFetch> services) : base(services) { }

    public partial int Id { get; set; }
    public partial string? Name { get; set; }
    public partial string? Sku { get; set; }
    public partial decimal Price { get; set; }

    [Create]
    public void Create() { }

    /// <summary>
    /// Fetch by ID.
    /// </summary>
    [Fetch]
    public bool Fetch(int id, [Service] IProductRepository repo)
    {
        var data = repo.FindById(id);
        if (data == null)
            return false;

        MapFromData(data);
        return true;
    }

    /// <summary>
    /// Fetch by SKU.
    /// </summary>
    [Fetch]
    public bool Fetch(string sku, [Service] IProductRepository repo)
    {
        var data = repo.FindBySku(sku);
        if (data == null)
            return false;

        MapFromData(data);
        return true;
    }

    private void MapFromData(ProductData data)
    {
        Id = data.Id;
        Name = data.Name;
        Sku = data.Sku;
        Price = data.Price;
    }
}
#endregion
