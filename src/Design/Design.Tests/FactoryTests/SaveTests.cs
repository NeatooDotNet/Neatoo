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
    private MockSaveDemoRepository _repository = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _scope = DesignTestServices.GetScope();
        _factory = _scope.GetRequiredService<ISaveDemoFactory>();
        _repository = (MockSaveDemoRepository)_scope.GetRequiredService<ISaveDemoRepository>();
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

    // =========================================================================
    // Save() routing — this file is named for Save but never called it
    // (ISNEW-006). The four tests above assert IsSavable flags only, so
    // SaveDemo's [Insert]/[Update]/[Delete] bodies were never executed.
    // =========================================================================

    [TestMethod]
    public async Task Save_WhenNew_RoutesToInsert_AndMarksOld()
    {
        // Arrange
        var entity = _factory.Create();
        entity.Name = "Inserted";
        entity.Amount = 42m;

        // Act
        entity = (ISaveDemo)await entity.Save();

        // Assert - routed to Insert, and the generated Id landed on the entity
        Assert.AreEqual(1, _repository.InsertedIds.Count, "Should route to Insert");
        Assert.AreEqual(0, _repository.UpdatedIds.Count);
        Assert.AreEqual(_repository.InsertedIds[0], entity.Id,
            "The generated Id must land on the entity");

        // Assert - FactoryComplete(Insert) marked it unmodified and old
        Assert.IsFalse(entity.IsNew, "Inserted entity is no longer new");
        Assert.IsFalse(entity.IsModified);
        Assert.IsFalse(entity.IsSavable, "Nothing left to save");
    }

    [TestMethod]
    public async Task Save_WhenModifiedExisting_RoutesToUpdate()
    {
        // Arrange
        var entity = await _factory.Fetch(7);
        entity.Name = "Updated Name";

        // Act
        entity = (ISaveDemo)await entity.Save();

        // Assert
        CollectionAssert.AreEqual(new[] { 7 }, _repository.UpdatedIds, "Should route to Update");
        Assert.AreEqual(0, _repository.InsertedIds.Count);
        Assert.IsFalse(entity.IsModified, "Clean after save");
    }

    [TestMethod]
    public async Task Save_WhenDeleted_RoutesToDelete()
    {
        // Arrange - a persisted entity marked for deletion
        var entity = await _factory.Fetch(9);
        entity.Delete();

        Assert.IsTrue(entity.IsDeleted);
        Assert.IsTrue(entity.IsModified, "IsDeleted remains a term in IsModified");
        Assert.IsTrue(entity.IsSavable);

        // Act
        await entity.Save();

        // Assert - routed to Delete, not Update
        CollectionAssert.AreEqual(new[] { 9 }, _repository.DeletedIds, "Should route to Delete");
        Assert.AreEqual(0, _repository.UpdatedIds.Count);
    }

    [TestMethod]
    public async Task Save_WhenNewAndUntouched_StillInserts()
    {
        // The ISNEW behavior at the routing level: a created entity carries no
        // property dirt, so it reports not-modified - and still inserts, because
        // savability and routing both run off IsNew.
        var entity = _factory.Create();
        entity.Name = "Untouched-ish";  // required for validity only

        Assert.IsTrue(entity.IsNew);
        Assert.IsTrue(entity.IsSavable);

        entity = (ISaveDemo)await entity.Save();

        Assert.AreEqual(1, _repository.InsertedIds.Count);
        Assert.IsFalse(entity.IsNew);
    }
}
