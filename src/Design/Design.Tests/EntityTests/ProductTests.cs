// -----------------------------------------------------------------------------
// Design.Tests - Product (Minimal Root) Tests
// -----------------------------------------------------------------------------
// Pins the minimal aggregate root the skill's quick start shows: create, fetch
// by key, save through the root, and the server-side rules gate.
// -----------------------------------------------------------------------------

using Design.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Design.Tests.EntityTests;

[TestClass]
public class ProductTests
{
    private IServiceScope _scope = null!;
    private IProductFactory _factory = null!;
    private MockProductRepository _repository = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _scope = DesignTestServices.GetScope();
        _factory = _scope.GetRequiredService<IProductFactory>();
        _repository = (MockProductRepository)_scope.GetRequiredService<IProductRepository>();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _scope.Dispose();
    }

    #region skill-quick-start-create
    [TestMethod]
    public async Task Create_ThenEdit_IsSavable()
    {
        var product = _factory.Create();
        Assert.IsTrue(product.IsNew);
        Assert.IsFalse(product.IsModified, "A created object holds no user work");

        product.Name = "Widget";
        product.Price = 9.99m;
        await product.WaitForTasks();

        Assert.IsTrue(product.IsValid);
        Assert.IsTrue(product.IsSavable);
    }
    #endregion

    #region skill-quick-start-save
    [TestMethod]
    public async Task Save_ThenFetch_RoundTrips()
    {
        var product = _factory.Create();
        product.Name = "Widget";
        product.Price = 9.99m;
        await product.WaitForTasks();

        // Save returns the saved instance; keep that one
        product = (IProduct)await product.Save();
        Assert.IsFalse(product.IsNew);
        Assert.IsFalse(product.IsModified);

        var fetched = await _factory.Fetch(product.Id);
        Assert.IsNotNull(fetched);
        Assert.AreEqual("Widget", fetched.Name);
        Assert.IsFalse(fetched.IsModified, "A fetched object is a clean baseline");
    }
    #endregion

    [TestMethod]
    public async Task Fetch_UnknownId_ReturnsNull()
    {
        var product = await _factory.Fetch(Guid.NewGuid());

        Assert.IsNull(product, "Fetch returned false, so the factory returns null");
    }

    [TestMethod]
    public async Task Update_WritesTheRow()
    {
        var seeded = _repository.SeedProduct();
        var product = await _factory.Fetch(seeded.Id);
        Assert.IsNotNull(product);

        product.Price = 19.99m;
        await product.WaitForTasks();
        await product.Save();

        Assert.AreEqual(19.99m, _repository.Store[seeded.Id].Price);
        Assert.AreEqual(1, _repository.SaveChangesCount);
    }

    [TestMethod]
    public async Task InvalidProduct_DirectFactorySave_IsRefusedBeforeWriting()
    {
        // Name is [Required]; rules did not run during [Create]
        var product = _factory.Create();

        var ex = await Assert.ThrowsExactlyAsync<Neatoo.SaveOperationException>(
            () => _factory.Save(product));

        Assert.AreEqual(Neatoo.SaveFailureReason.IsInvalid, ex.Reason);
        Assert.AreEqual(0, _repository.SaveChangesCount, "Nothing was written");
    }
}
