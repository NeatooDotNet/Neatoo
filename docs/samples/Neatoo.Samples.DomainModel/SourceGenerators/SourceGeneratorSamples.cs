/// <summary>
/// Code samples for source-generators skill - What developers write vs what gets generated
///
/// Full snippets:
/// - docs:source-generators:entity-input
/// - docs:source-generators:partial-property
/// - docs:source-generators:factory-attribute
/// - docs:source-generators:complete-entity
///
/// Corresponding tests: SourceGeneratorSamplesTests.cs
/// </summary>

using Neatoo.RemoteFactory;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Neatoo.Samples.DomainModel.SourceGenerators;

#region docs:source-generators:complete-entity
/// <summary>
/// Complete entity example - what the developer writes.
/// The source generators create:
/// - IProductFactory interface with Create, Fetch, Save methods
/// - ProductFactory implementation
/// - Property backing fields and Getter/Setter implementations
/// - Meta-property access (IsModified, IsBusy, PropertyMessages)
/// </summary>
public partial interface IProduct : IEntityBase
{
    int Id { get; }
    string? Name { get; set; }
    decimal Price { get; set; }
    int StockCount { get; set; }
}

// [Factory] - Triggers factory interface and implementation generation
[Factory]
internal partial class Product : EntityBase<Product>, IProduct
{
    public Product(IEntityBaseServices<Product> services) : base(services) { }

    // partial keyword triggers property implementation generation
    public partial int Id { get; set; }

    [DisplayName("Product Name")]
    [Required(ErrorMessage = "Name is required")]
    public partial string? Name { get; set; }

    [Range(0.01, 10000, ErrorMessage = "Price must be between $0.01 and $10,000")]
    public partial decimal Price { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Stock count cannot be negative")]
    public partial int StockCount { get; set; }

    [Create]
    public void Create()
    {
        // Initialize defaults
    }

    [Fetch]
    public void Fetch(int id, string name, decimal price, int stockCount)
    {
        Id = id;
        Name = name;
        Price = price;
        StockCount = stockCount;
    }

    [Insert]
    public Task Insert()
    {
        // Persist new product to database
        return Task.CompletedTask;
    }

    [Update]
    public Task Update()
    {
        // Update existing product in database
        return Task.CompletedTask;
    }

    [Delete]
    public Task Delete()
    {
        // Remove product from database
        return Task.CompletedTask;
    }
}
#endregion

#region docs:source-generators:entity-input
/// <summary>
/// Minimal entity showing required elements for source generation.
/// </summary>
public partial interface IMinimalEntity : IEntityBase
{
    // Interface properties are optional - generated from partial class
}

[Factory]
internal partial class MinimalEntity : EntityBase<MinimalEntity>, IMinimalEntity
{
    // Required: Constructor with IEntityBaseServices<T>
    public MinimalEntity(IEntityBaseServices<MinimalEntity> services)
        : base(services) { }

    // Required: At least one factory method with attribute
    [Create]
    public void Create() { }
}
#endregion

#region docs:source-generators:factory-attribute
// The [Factory] attribute marks a class for factory generation.
// Place it on the class declaration:
//
// [Factory]
// internal partial class MyEntity : EntityBase<MyEntity>, IMyEntity
//
// This generates:
// - IMyEntityFactory interface
// - MyEntityFactory implementation class
// - DI registration extension methods
#endregion

#region docs:source-generators:partial-property
// The 'partial' keyword on properties triggers implementation generation:
//
// public partial string? Name { get; set; }
//
// Generator creates:
// - private string? _name;  (backing field)
// - get => Getter<string?>(); (with change notification)
// - set => Setter(value);     (with rule triggering)
//
// The generated code integrates with Neatoo's:
// - Change tracking (IsModified)
// - Validation system (PropertyMessages)
// - Busy state (IsBusy during async rules)
#endregion

/*
    What gets generated (simplified):

    // Generated factory interface
    public interface IProductFactory
    {
        IProduct Create();
        IProduct Fetch(int id, string name, decimal price, int stockCount);
        Task<IProduct?> Save(IProduct target);
        Task<IProduct?> TrySave(IProduct target);
    }

    // Generated factory implementation
    internal class ProductFactory : IProductFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public ProductFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IProduct Create()
        {
            var product = _serviceProvider.GetRequiredService<Product>();
            product.Create();
            product.MarkUnmodified();
            return product;
        }

        // ... Fetch, Save, TrySave implementations
    }

    // Generated property implementation (in partial class)
    internal partial class Product
    {
        private int _id;
        public partial int Id
        {
            get => Getter(ref _id);
            set => Setter(ref _id, value);
        }
    }
*/
