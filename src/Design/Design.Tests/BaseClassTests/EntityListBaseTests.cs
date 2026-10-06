// -----------------------------------------------------------------------------
// Design.Tests - EntityListBase Tests
// -----------------------------------------------------------------------------
// Tests demonstrating EntityListBase<I> behavior including child management,
// DeletedList, and modification tracking.
// -----------------------------------------------------------------------------

using Design.Domain.BaseClasses;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Design.Tests.BaseClassTests;

[TestClass]
public class EntityListBaseTests
{
    private IServiceScope _scope = null!;
    private IDemoEntityListFactory _listFactory = null!;
    private IDemoChildFactory _itemFactory = null!;
    private IDemoParentFactory _parentFactory = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _scope = DesignTestServices.GetScope();
        _listFactory = _scope.GetRequiredService<IDemoEntityListFactory>();
        _itemFactory = _scope.GetRequiredService<IDemoChildFactory>();
        _parentFactory = _scope.GetRequiredService<IDemoParentFactory>();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _scope.Dispose();
    }

    [TestMethod]
    public void Create_InitializesEmptyList()
    {
        // Arrange & Act
        var list = _listFactory.Create();

        // Assert
        Assert.AreEqual(0, list.Count);
    }

    [TestMethod]
    public void Add_AttachesItemToList()
    {
        // Arrange
        var list = _listFactory.Create();
        var item = _itemFactory.Create("Test");

        // Act
        list.Add(item);

        // Assert - child identity is observable through routing: Delete() on a
        // child goes through its containing list rather than marking the child
        // alone. This is what the removed IsChild flag used to stand in for.
        item.Delete();
        Assert.AreEqual(0, list.Count, "Deleting a child removes it from its list");
    }

    [TestMethod]
    public void Add_IncreasesCount()
    {
        // Arrange
        var list = _listFactory.Create();
        var item = _itemFactory.Create("Test");

        // Act
        list.Add(item);

        // Assert
        Assert.AreEqual(1, list.Count);
    }

    [TestMethod]
    public void Remove_NewItemNotAddedToDeletedList()
    {
        // Arrange
        var list = _listFactory.Create();
        var item = _itemFactory.Create("Test");
        list.Add(item);
        // Item is still IsNew=true

        // Act
        list.Remove(item);

        // Assert
        Assert.AreEqual(0, list.Count);
        Assert.AreEqual(0, list.DeletedCount, "Removed new item should not be in DeletedList");
    }

    [TestMethod]
    public void IsModified_TrueWhenChildModified()
    {
        // Arrange
        var list = _listFactory.Create();
        var item = _itemFactory.Create("Test");
        list.Add(item);
        item.Name = "Changed";

        // Assert
        Assert.IsTrue(list.IsModified, "List should be modified when child is modified");
    }

    [TestMethod]
    public async Task Remove_FetchedItem_AddedToDeletedList()
    {
        // Arrange - Fetch the root so its list holds existing (non-new) children
        var parent = await _parentFactory.Fetch();
        var list = parent.Children!;
        var item = list[0];
        Assert.IsFalse(item.IsNew, "A fetched child is not new");

        // Act
        list.Remove(item);

        // Assert
        Assert.AreEqual(2, list.Count);
        Assert.AreEqual(1, list.DeletedCount, "Removed fetched item should be in DeletedList");
    }
}
