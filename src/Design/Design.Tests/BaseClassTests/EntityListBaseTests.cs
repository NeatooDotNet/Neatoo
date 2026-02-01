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
    private IDemoEntityFactory _itemFactory = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _scope = DesignTestServices.GetScope();
        _listFactory = _scope.GetRequiredService<IDemoEntityListFactory>();
        _itemFactory = _scope.GetRequiredService<IDemoEntityFactory>();
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
    public void Add_MarksItemAsChild()
    {
        // Arrange
        var list = _listFactory.Create();
        var item = _itemFactory.Create();
        item.Name = "Test";

        // Act
        list.Add(item);

        // Assert
        Assert.IsTrue(item.IsChild, "Item added to list should have IsChild=true");
    }

    [TestMethod]
    public void Add_IncreasesCount()
    {
        // Arrange
        var list = _listFactory.Create();
        var item = _itemFactory.Create();
        item.Name = "Test";

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
        var item = _itemFactory.Create();
        item.Name = "Test";
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
        var item = _itemFactory.Create();
        item.Name = "Test";
        list.Add(item);
        // Note: New items start as modified, so the list is already modified

        // Assert
        Assert.IsTrue(list.IsModified, "List should be modified when child is modified");
    }

    [TestMethod]
    public async Task Remove_FetchedItem_AddedToDeletedList()
    {
        // Arrange - Use Fetch to get an existing (non-new) entity
        var list = _listFactory.Create();
        var item = await _itemFactory.Fetch(1);
        list.Add(item);
        // item.IsNew = false after Fetch

        // Act
        list.Remove(item);

        // Assert
        Assert.AreEqual(0, list.Count);
        Assert.AreEqual(1, list.DeletedCount, "Removed fetched item should be in DeletedList");
    }
}
