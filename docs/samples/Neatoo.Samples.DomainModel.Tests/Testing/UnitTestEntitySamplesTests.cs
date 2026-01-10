/// <summary>
/// Tests for UnitTestEntitySamples demonstrating direct entity unit testing.
///
/// These tests verify that entities can be tested without factories by using
/// EntityBaseServices parameterless constructor.
///
/// Corresponding samples: UnitTestEntitySamples.cs
/// </summary>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Samples.DomainModel.Testing;

namespace Neatoo.Samples.DomainModel.Tests.Testing;

[TestClass]
[TestCategory("Documentation")]
[TestCategory("Testing")]
public class UnitTestEntitySamplesTests
{
    [TestMethod]
    public void TestableProduct_WhenCreated_CanSetProperties()
    {
        // Arrange & Act - Create entity without factory
        var product = new TestableProduct();
        product.Name = "Widget";
        product.Price = 10.00m;
        product.Quantity = 5;

        // Assert
        Assert.AreEqual("Widget", product.Name);
        Assert.AreEqual(10.00m, product.Price);
        Assert.AreEqual(5, product.Quantity);
    }

    [TestMethod]
    public void TestableProduct_WhenPropertyChanged_IsModifiedTrue()
    {
        // Arrange
        var product = new TestableProduct();

        // Act
        product.Name = "Changed";

        // Assert
        Assert.IsTrue(product.IsModified, "Entity should be modified after property change");
    }

    [TestMethod]
    public void TestableProduct_TotalValue_CalculatesCorrectly()
    {
        // Arrange
        var product = new TestableProduct();
        product.Price = 15.50m;
        product.Quantity = 4;

        // Act
        var total = product.TotalValue;

        // Assert
        Assert.AreEqual(62.00m, total);
    }

    [TestMethod]
    public void TestableProduct_SetAsNew_SetsIsNewTrue()
    {
        // Arrange
        var product = new TestableProduct();

        // Act
        product.SetAsNew();

        // Assert
        Assert.IsTrue(product.IsNew);
    }

    [TestMethod]
    public void TestableProduct_SetAsExisting_SetsIsNewFalse()
    {
        // Arrange
        var product = new TestableProduct();
        product.SetAsNew();

        // Act
        product.SetAsExisting();

        // Assert
        Assert.IsFalse(product.IsNew);
    }

    [TestMethod]
    public void TestableProduct_SetAsChild_SetsIsChildTrue()
    {
        // Arrange
        var product = new TestableProduct();

        // Act
        product.SetAsChild();

        // Assert
        Assert.IsTrue(product.IsChild);
    }

    [TestMethod]
    public void EntityUnitTestExample_TestEntityStateTracking_RunsWithoutException()
    {
        // Act & Assert - Should not throw
        EntityUnitTestExample.TestEntityStateTracking();
    }
}
