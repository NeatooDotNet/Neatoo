using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Documentation.Samples.FactoryOperations;

namespace Neatoo.Documentation.Samples.Tests.FactoryOperations;

/// <summary>
/// Tests for FetchOperationSamples.cs code snippets.
/// </summary>
[TestClass]
[TestCategory("Documentation")]
[TestCategory("FactoryOperations")]
public class FetchOperationSamplesTests : SamplesTestBase
{
    [TestInitialize]
    public void TestInitialize()
    {
        InitializeScope();
    }

    #region FetchableProduct Tests

    [TestMethod]
    public void FetchableProduct_Fetch_LoadsFromRepository()
    {
        // Arrange
        var factory = GetRequiredService<IFetchableProductFactory>();

        // Act
        var product = factory.Fetch(1);

        // Assert
        Assert.IsNotNull(product);
        Assert.AreEqual(1, product.Id);
        Assert.AreEqual("Widget", product.Name);
        Assert.AreEqual(9.99m, product.Price);
    }

    [TestMethod]
    public void FetchableProduct_Fetch_ReturnsNullForInvalidId()
    {
        // Arrange
        var factory = GetRequiredService<IFetchableProductFactory>();

        // Act
        var product = factory.Fetch(999);

        // Assert - Fetch returned false, so factory returns null/default
        Assert.IsNull(product);
    }

    [TestMethod]
    public void FetchableProduct_Fetch_IsNotNew()
    {
        // Arrange
        var factory = GetRequiredService<IFetchableProductFactory>();

        // Act
        var product = factory.Fetch(1);

        // Assert
        Assert.IsNotNull(product);
        Assert.IsFalse(product.IsNew);
        Assert.IsFalse(product.IsModified);
    }

    #endregion

    #region ProductWithMultipleFetch Tests

    [TestMethod]
    public void ProductWithMultipleFetch_FetchById_Works()
    {
        // Arrange
        var factory = GetRequiredService<IProductWithMultipleFetchFactory>();

        // Act
        var product = factory.Fetch(2);

        // Assert
        Assert.IsNotNull(product);
        Assert.AreEqual(2, product.Id);
        Assert.AreEqual("Gadget", product.Name);
        Assert.AreEqual("GDG-002", product.Sku);
    }

    [TestMethod]
    public void ProductWithMultipleFetch_FetchBySku_Works()
    {
        // Arrange
        var factory = GetRequiredService<IProductWithMultipleFetchFactory>();

        // Act
        var product = factory.Fetch("GZM-003");

        // Assert
        Assert.IsNotNull(product);
        Assert.AreEqual(3, product.Id);
        Assert.AreEqual("Gizmo", product.Name);
        Assert.AreEqual(29.99m, product.Price);
    }

    [TestMethod]
    public void ProductWithMultipleFetch_FetchInvalidSku_ReturnsNull()
    {
        // Arrange
        var factory = GetRequiredService<IProductWithMultipleFetchFactory>();

        // Act
        var product = factory.Fetch("INVALID-SKU");

        // Assert
        Assert.IsNull(product);
    }

    #endregion
}
