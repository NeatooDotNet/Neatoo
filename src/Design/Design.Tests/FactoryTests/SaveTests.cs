// -----------------------------------------------------------------------------
// Design.Tests - Save (Insert/Update/Delete) Factory Operation Tests
// -----------------------------------------------------------------------------
// Tests demonstrating Save routing to Insert/Update/Delete based on state.
// -----------------------------------------------------------------------------

using Design.Domain.FactoryOperations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Design.Tests.FactoryTests;

[TestClass]
public class SaveTests
{
    private IServiceScope _scope = null!;
    private ISaveDemoFactory _factory = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _scope = DesignTestServices.GetScope();
        _factory = _scope.GetRequiredService<ISaveDemoFactory>();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _scope.Dispose();
    }

    [TestMethod]
    public void NewEntity_IsSavableWhenValid()
    {
        // Arrange
        var entity = _factory.Create();
        entity.Name = "Valid Name";
        entity.Amount = 100;

        // Assert
        Assert.IsTrue(entity.IsNew);
        Assert.IsTrue(entity.IsValid);
        Assert.IsTrue(entity.IsSavable, "Valid new entity should be savable");
    }

    [TestMethod]
    public async Task NewEntity_NotSavableWhenInvalid()
    {
        // Arrange
        var entity = _factory.Create();
        entity.Name = "Valid First";
        Assert.IsTrue(entity.IsValid);

        // Act - Make it invalid
        entity.Name = null;
        await entity.WaitForTasks();

        // Assert
        Assert.IsTrue(entity.IsNew);
        Assert.IsFalse(entity.IsValid);
        Assert.IsFalse(entity.IsSavable, "Invalid entity should not be savable");
    }

    [TestMethod]
    public async Task FetchedEntity_NotSavableWhenUnmodified()
    {
        // Arrange
        var entity = await _factory.Fetch(1);

        // Assert
        Assert.IsFalse(entity.IsNew);
        Assert.IsFalse(entity.IsModified);
        Assert.IsFalse(entity.IsSavable, "Unmodified fetched entity should not be savable");
    }

    [TestMethod]
    public async Task FetchedEntity_IsSavableWhenModified()
    {
        // Arrange
        var entity = await _factory.Fetch(1);

        // Act
        entity.Name = "Updated Name";

        // Assert
        Assert.IsFalse(entity.IsNew);
        Assert.IsTrue(entity.IsModified);
        Assert.IsTrue(entity.IsValid);
        Assert.IsTrue(entity.IsSavable, "Modified valid entity should be savable");
    }
}
