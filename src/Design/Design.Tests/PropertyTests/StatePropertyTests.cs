// -----------------------------------------------------------------------------
// Design.Tests - State Property Tests
// -----------------------------------------------------------------------------
// Tests demonstrating SetValue vs LoadValue and modification tracking.
// -----------------------------------------------------------------------------

using Design.Domain.PropertySystem;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Design.Tests.PropertyTests;

[TestClass]
public class StatePropertyTests
{
    private IServiceScope _scope = null!;
    private ISetValueVsLoadValueDemoFactory _factory = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _scope = DesignTestServices.GetScope();
        _factory = _scope.GetRequiredService<ISetValueVsLoadValueDemoFactory>();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _scope.Dispose();
    }

    [TestMethod]
    public void SetValue_MarksPropertyModified()
    {
        // Arrange
        var entity = _factory.Create();

        // Act
        entity.Name = "Modified";

        // Assert
        Assert.IsTrue(entity.IsSelfModified, "SetValue should mark property as modified");
        Assert.IsTrue(entity["Name"].IsModified, "Property should be marked modified");
    }

    [TestMethod]
    public void LoadValue_DoesNotMarkPropertyModified()
    {
        // Arrange
        var entity = _factory.Create();

        // Act
        entity["Name"].LoadValue("Loaded");

        // Assert - Note: New entities are already modified, but check the specific property
        Assert.IsFalse(entity["Name"].IsModified, "Property should not be marked modified via LoadValue");
    }

    [TestMethod]
    public async Task Fetch_UsesLoadValue_NotModified()
    {
        // Arrange & Act
        var entity = await _factory.Fetch(1);

        // Assert
        Assert.IsFalse(entity.IsModified, "Fetched entity should not be modified");
        Assert.IsFalse(entity.IsSelfModified, "Fetched entity should not be self-modified");
    }

    [TestMethod]
    public async Task Fetch_ThenModify_IsModified()
    {
        // Arrange
        var entity = await _factory.Fetch(1);

        // Act
        entity.Name = "Changed";

        // Assert
        Assert.IsTrue(entity.IsModified, "Entity should be modified after change");
        Assert.IsTrue(entity["Name"].IsModified, "Name property should be modified");
    }
}
