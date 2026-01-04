using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.RemoteFactory;

namespace Neatoo.UnitTest.Integration.Concepts.EntityBase;

[TestClass]
public class EntityListBaseEdgeCaseTests
{
    [TestMethod]
    public void Add_NullItem_ThrowsArgumentNullException()
    {
        // Arrange
        var list = new EntityPersonList();

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => list.Add(null!));
    }

    [TestMethod]
    public void Add_DuplicateItem_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregateRoot = new EntityPerson();
        var list = new EntityPersonList();
        aggregateRoot.ChildList = list;

        var item = new EntityPerson();
        list.Add(item);

        // Act & Assert
        var exception = Assert.ThrowsException<InvalidOperationException>(() => list.Add(item));
        Assert.IsTrue(exception.Message.Contains("already in this list"));
    }

    [TestMethod]
    public void Add_DuplicateItem_WhenPaused_AllowedForDeserialization()
    {
        // Arrange - Paused mode simulates deserialization
        var list = new EntityPersonList();
        list.FactoryStart(FactoryOperation.Fetch);

        var item = new EntityPerson();
        list.Add(item);

        // Act - Adding same item again when paused (trusted source)
        // This tests that the duplicate check is skipped when paused
        list.Add(item);

        list.FactoryComplete(FactoryOperation.Fetch);

        // Assert - Both references are in the list (unusual but allowed during deserialization)
        Assert.AreEqual(2, list.Count);
    }

    [TestMethod]
    public void Insert_NullItem_ThrowsArgumentNullException()
    {
        // Arrange
        var list = new EntityPersonList();

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => list.Insert(0, null!));
    }

    [TestMethod]
    public void Insert_DuplicateItem_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregateRoot = new EntityPerson();
        var list = new EntityPersonList();
        aggregateRoot.ChildList = list;

        var item = new EntityPerson();
        list.Add(item);

        // Act & Assert
        var exception = Assert.ThrowsException<InvalidOperationException>(() => list.Insert(0, item));
        Assert.IsTrue(exception.Message.Contains("already in this list"));
    }

    [TestMethod]
    public void Add_BusyItem_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregateRoot = new EntityPerson();
        var list = new EntityPersonList();
        aggregateRoot.ChildList = list;

        var busyItem = new EntityPerson();
        var releaseBusy = ((IEntityPerson)busyItem).MarkBusyForTest();

        Assert.IsTrue(busyItem.IsBusy);

        // Act & Assert
        var exception = Assert.ThrowsException<InvalidOperationException>(() => list.Add(busyItem));
        Assert.IsTrue(exception.Message.Contains("busy"));

        // Cleanup
        releaseBusy();
    }

    [TestMethod]
    public void Add_BusyItem_WhenPaused_AllowedForDeserialization()
    {
        // Arrange - Paused mode simulates deserialization
        var list = new EntityPersonList();
        list.FactoryStart(FactoryOperation.Fetch);

        var busyItem = new EntityPerson();
        var releaseBusy = ((IEntityPerson)busyItem).MarkBusyForTest();

        Assert.IsTrue(busyItem.IsBusy);

        // Act - Adding busy item when paused (trusted source)
        list.Add(busyItem);

        list.FactoryComplete(FactoryOperation.Fetch);

        // Assert - Item was added
        Assert.IsTrue(list.Contains(busyItem));

        // Cleanup
        releaseBusy();
    }

    [TestMethod]
    public void Add_ItemRemovedFromSameList_CanBeReAdded()
    {
        // Arrange
        var aggregateRoot = new EntityPerson();
        var list = new EntityPersonList();
        aggregateRoot.ChildList = list;

        var item = new EntityPerson();
        list.Add(item);
        list.Remove(item);

        // Act - Re-add the removed item (it was a new item, so not in DeletedList)
        list.Add(item);

        // Assert
        Assert.IsTrue(list.Contains(item));
        Assert.AreEqual(1, list.Count);
    }

    [TestMethod]
    public void Add_ExistingItemRemovedFromSameList_CanBeReAdded()
    {
        // Arrange
        var aggregateRoot = new EntityPerson();
        var list = new EntityPersonList();
        aggregateRoot.ChildList = list;

        var item = new EntityPerson();
        ((IEntityPerson)item).MarkOld(); // Mark as existing (from DB)
        list.Add(item);
        ((IEntityPerson)item).MarkUnmodified();

        list.Remove(item); // Goes to DeletedList

        // Act - Re-add the removed existing item
        list.Add(item);

        // Assert
        Assert.IsTrue(list.Contains(item));
        Assert.IsFalse(item.IsDeleted);
        Assert.AreEqual(0, list.DeletedCount); // Removed from DeletedList
    }

    [TestMethod]
    public void Add_ToEmptyList_SetsParentCorrectly()
    {
        // Arrange
        var aggregateRoot = new EntityPerson();
        var list = new EntityPersonList();
        aggregateRoot.ChildList = list;

        var item = new EntityPerson();

        // Act
        list.Add(item);

        // Assert
        Assert.AreEqual(aggregateRoot, item.Parent);
        Assert.AreEqual(aggregateRoot, item.Root);
        Assert.AreEqual(1, list.Count);
    }

    [TestMethod]
    public void Insert_AtSpecificIndex_WorksCorrectly()
    {
        // Arrange
        var aggregateRoot = new EntityPerson();
        var list = new EntityPersonList();
        aggregateRoot.ChildList = list;

        var item1 = new EntityPerson();
        var item2 = new EntityPerson();
        var item3 = new EntityPerson();

        list.Add(item1);
        list.Add(item2);

        // Act - Insert at index 1
        list.Insert(1, item3);

        // Assert
        Assert.AreEqual(item1, list[0]);
        Assert.AreEqual(item3, list[1]);
        Assert.AreEqual(item2, list[2]);
    }
}
