// -----------------------------------------------------------------------------
// Design.Tests - EntityBase Tests
// -----------------------------------------------------------------------------
// Tests demonstrating EntityBase<T> behavior including state properties,
// modification tracking, and persistence lifecycle.
// -----------------------------------------------------------------------------

using Design.Domain.BaseClasses;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Design.Tests.BaseClassTests;

[TestClass]
public class EntityBaseTests
{
    private IServiceScope _scope = null!;
    private IDemoEntityFactory _factory = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _scope = DesignTestServices.GetScope();
        _factory = _scope.GetRequiredService<IDemoEntityFactory>();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _scope.Dispose();
    }

    [TestMethod]
    public void Create_SetsIsNewTrue()
    {
        // Arrange & Act
        var entity = _factory.Create();

        // Assert
        Assert.IsTrue(entity.IsNew, "New entity should have IsNew=true");
    }

    [TestMethod]
    public void Create_SetsIsModifiedTrue()
    {
        // Arrange & Act
        var entity = _factory.Create();

        // Assert
        // New entities are considered modified (they need to be inserted)
        Assert.IsTrue(entity.IsModified, "New entity should have IsModified=true");
    }

    [TestMethod]
    public void Create_IsSavableWhenValid()
    {
        // Arrange
        var entity = _factory.Create();

        // Act
        entity.Name = "Valid Name";
        entity.Value = 10;

        // Assert
        Assert.IsTrue(entity.IsNew);
        Assert.IsTrue(entity.IsModified);
        Assert.IsTrue(entity.IsValid);
        Assert.IsTrue(entity.IsSavable, "Valid new entity should be savable");
    }

    [TestMethod]
    public void Create_NotSavableWhenInvalid()
    {
        // Arrange
        var entity = _factory.Create();

        // Act
        entity.Value = -1; // Triggers validation error: "Value must be non-negative"

        // Assert
        Assert.IsTrue(entity.IsNew);
        Assert.IsTrue(entity.IsModified);
        Assert.IsFalse(entity.IsValid, "Entity with validation error should not be valid");
        Assert.IsFalse(entity.IsSavable, "Invalid entity should not be savable");
    }

    [TestMethod]
    public void PropertyChange_MarksPropertyModified()
    {
        // Arrange
        var entity = _factory.Create();

        // Act
        entity.Name = "Test Name";

        // Assert
        Assert.IsTrue(entity.IsSelfModified);
        Assert.IsTrue(entity.ModifiedProperties.Contains("Name"));
    }

    [TestMethod]
    public async Task Fetch_SetsIsNewFalse()
    {
        // Arrange & Act
        var entity = await _factory.Fetch(1);

        // Assert
        Assert.IsFalse(entity.IsNew, "Fetched entity should have IsNew=false");
    }

    [TestMethod]
    public async Task Fetch_SetsIsModifiedFalse()
    {
        // Arrange & Act
        var entity = await _factory.Fetch(1);

        // Assert
        Assert.IsFalse(entity.IsModified, "Fetched entity should have IsModified=false");
    }

    [TestMethod]
    public void Delete_SetsIsDeletedTrue()
    {
        // Arrange
        var entity = _factory.Create();

        // Act
        entity.Delete();

        // Assert
        Assert.IsTrue(entity.IsDeleted, "Deleted entity should have IsDeleted=true");
    }
}
