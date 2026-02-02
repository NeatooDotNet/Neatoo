// -----------------------------------------------------------------------------
// Design.Tests - Property Basics Tests
// -----------------------------------------------------------------------------
// Tests demonstrating partial property behavior and property system.
// -----------------------------------------------------------------------------

using Design.Domain.PropertySystem;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Design.Tests.PropertyTests;

[TestClass]
public class PropertyBasicsTests
{
    private IServiceScope _scope = null!;
    private IPropertyBasicsDemoFactory _factory = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _scope = DesignTestServices.GetScope();
        _factory = _scope.GetRequiredService<IPropertyBasicsDemoFactory>();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _scope.Dispose();
    }

    [TestMethod]
    public void Property_GetReturnsSetValue()
    {
        // Arrange
        var entity = _factory.Create();

        // Act
        entity.Name = "Test";

        // Assert
        Assert.AreEqual("Test", entity.Name);
    }

    [TestMethod]
    public void Property_SetTriggersPropertyChanged()
    {
        // Arrange
        var entity = _factory.Create();
        var changedProperties = new List<string>();
        entity.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName!);

        // Act
        entity.Name = "Test";

        // Assert
        Assert.IsTrue(changedProperties.Contains("Name"));
    }

    [TestMethod]
    public void Property_Indexer_ReturnsPropertyInterface()
    {
        // Arrange
        var entity = _factory.Create();

        // Act
        var nameProperty = entity["Name"];

        // Assert
        Assert.IsNotNull(nameProperty);
    }

    [TestMethod]
    public void Property_Indexer_CanLoadValue()
    {
        // Arrange
        var entity = _factory.Create();

        // Act
        entity["Name"].LoadValue("Loaded");

        // Assert
        Assert.AreEqual("Loaded", entity.Name);
    }
}
