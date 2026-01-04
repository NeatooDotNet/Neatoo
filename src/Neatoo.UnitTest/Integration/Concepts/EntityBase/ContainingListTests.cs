using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.RemoteFactory;

namespace Neatoo.UnitTest.Integration.Concepts.EntityBase;

[TestClass]
public class ContainingListTests
{
    [TestMethod]
    public void ContainingList_NewEntity_IsNull()
    {
        // Arrange
        var entity = new EntityPerson();

        // Act & Assert
        var entityInternal = (IEntityBaseInternal)entity;
        Assert.IsNull(entityInternal.ContainingList);
    }

    [TestMethod]
    public void ContainingList_AfterAddToList_IsSet()
    {
        // Arrange
        var aggregateRoot = new EntityPerson();
        var list = new EntityPersonList();
        aggregateRoot.ChildList = list;

        var item = new EntityPerson();

        // Act
        list.Add(item);

        // Assert
        var itemInternal = (IEntityBaseInternal)item;
        Assert.AreSame(list, itemInternal.ContainingList);
    }

    [TestMethod]
    public void ContainingList_AfterRemove_StaysSet()
    {
        // Arrange
        var aggregateRoot = new EntityPerson();
        var list = new EntityPersonList();
        aggregateRoot.ChildList = list;

        var item = new EntityPerson();
        ((IEntityPerson)item).MarkOld(); // Make it an existing entity
        list.Add(item);
        ((IEntityPerson)item).MarkUnmodified();

        // Act
        list.Remove(item);

        // Assert - ContainingList stays set (item is pending deletion)
        var itemInternal = (IEntityBaseInternal)item;
        Assert.AreSame(list, itemInternal.ContainingList);
        Assert.IsTrue(item.IsDeleted);
        Assert.AreEqual(1, list.DeletedCount);
    }

    [TestMethod]
    public void ContainingList_AfterFactoryComplete_IsCleared()
    {
        // Arrange
        var aggregateRoot = new EntityPerson();
        var list = new EntityPersonList();
        aggregateRoot.ChildList = list;

        var item = new EntityPerson();
        ((IEntityPerson)item).MarkOld();
        list.Add(item);
        ((IEntityPerson)item).MarkUnmodified();

        list.Remove(item);

        // Act - Simulate save completion
        list.FactoryComplete(FactoryOperation.Update);

        // Assert - ContainingList is cleared after save
        var itemInternal = (IEntityBaseInternal)item;
        Assert.IsNull(itemInternal.ContainingList);
        Assert.AreEqual(0, list.DeletedCount);
    }

    [TestMethod]
    public void Delete_WhenInList_RemovesFromList()
    {
        // Arrange
        var aggregateRoot = new EntityPerson();
        var list = new EntityPersonList();
        aggregateRoot.ChildList = list;

        var item = new EntityPerson();
        ((IEntityPerson)item).MarkOld();
        list.Add(item);
        ((IEntityPerson)item).MarkUnmodified();

        // Act
        item.Delete();

        // Assert
        Assert.IsFalse(list.Contains(item));
        Assert.IsTrue(item.IsDeleted);
        Assert.AreEqual(1, list.DeletedCount);
    }

    [TestMethod]
    public void Delete_WhenStandalone_JustMarksDeleted()
    {
        // Arrange
        var entity = new EntityPerson();
        ((IEntityPerson)entity).MarkOld();

        // Act
        entity.Delete();

        // Assert
        Assert.IsTrue(entity.IsDeleted);
        var entityInternal = (IEntityBaseInternal)entity;
        Assert.IsNull(entityInternal.ContainingList);
    }

    [TestMethod]
    public void Delete_Consistency_SameAsList_Remove()
    {
        // Arrange - Two identical setups
        var root1 = new EntityPerson();
        var list1 = new EntityPersonList();
        root1.ChildList = list1;
        var item1 = new EntityPerson();
        ((IEntityPerson)item1).MarkOld();
        list1.Add(item1);
        ((IEntityPerson)item1).MarkUnmodified();

        var root2 = new EntityPerson();
        var list2 = new EntityPersonList();
        root2.ChildList = list2;
        var item2 = new EntityPerson();
        ((IEntityPerson)item2).MarkOld();
        list2.Add(item2);
        ((IEntityPerson)item2).MarkUnmodified();

        // Act - item1 uses Delete(), item2 uses list.Remove()
        item1.Delete();
        list2.Remove(item2);

        // Assert - Both should have identical state
        Assert.AreEqual(item1.IsDeleted, item2.IsDeleted);
        Assert.AreEqual(list1.Contains(item1), list2.Contains(item2));
        Assert.AreEqual(list1.DeletedCount, list2.DeletedCount);
    }

    [TestMethod]
    public void IntraAggregateMove_CleansUpDeletedList()
    {
        // Arrange
        var aggregateRoot = new EntityPerson();
        var list1 = new EntityPersonList();
        aggregateRoot.ChildList = list1;

        var childWithList = new EntityPerson();
        list1.Add(childWithList);
        var list2 = new EntityPersonList();
        childWithList.ChildList = list2;

        var item = new EntityPerson();
        ((IEntityPerson)item).MarkOld();
        list1.Add(item);
        ((IEntityPerson)item).MarkUnmodified();

        // Act - Remove from list1 and add to list2 (same aggregate)
        list1.Remove(item);
        Assert.AreEqual(1, list1.DeletedCount);
        Assert.IsTrue(item.IsDeleted);

        list2.Add(item);

        // Assert - Item is now in list2, not deleted, and removed from list1's DeletedList
        Assert.IsTrue(list2.Contains(item));
        Assert.IsFalse(item.IsDeleted);
        Assert.AreEqual(0, list1.DeletedCount); // Cleaned up!

        var itemInternal = (IEntityBaseInternal)item;
        Assert.AreSame(list2, itemInternal.ContainingList);
    }

    [TestMethod]
    public void IntraAggregateMove_NewItem_NoDeletedListInvolvement()
    {
        // Arrange
        var aggregateRoot = new EntityPerson();
        var list1 = new EntityPersonList();
        aggregateRoot.ChildList = list1;

        var childWithList = new EntityPerson();
        list1.Add(childWithList);
        var list2 = new EntityPersonList();
        childWithList.ChildList = list2;

        var newItem = new EntityPerson();
        ((IEntityPerson)newItem).MarkNew(); // Mark as new entity
        list1.Add(newItem);

        // Act - Remove from list1 and add to list2
        list1.Remove(newItem);
        Assert.AreEqual(0, list1.DeletedCount); // New items don't go to DeletedList

        list2.Add(newItem);

        // Assert - Item is now in list2
        Assert.IsTrue(list2.Contains(newItem));
        Assert.AreEqual(0, list1.DeletedCount);

        var itemInternal = (IEntityBaseInternal)newItem;
        Assert.AreSame(list2, itemInternal.ContainingList);
    }

    [TestMethod]
    public void ContainingList_WhenPaused_NotSet()
    {
        // Arrange - Paused mode (deserialization)
        var list = new EntityPersonList();
        list.FactoryStart(FactoryOperation.Fetch);

        var item = new EntityPerson();

        // Act
        list.Add(item);

        // Assert - ContainingList not set when paused
        var itemInternal = (IEntityBaseInternal)item;
        Assert.IsNull(itemInternal.ContainingList);

        list.FactoryComplete(FactoryOperation.Fetch);
    }

    [TestMethod]
    public void RemoveNewItem_DoesNotGoToDeletedList()
    {
        // Arrange
        var aggregateRoot = new EntityPerson();
        var list = new EntityPersonList();
        aggregateRoot.ChildList = list;

        var newItem = new EntityPerson();
        ((IEntityPerson)newItem).MarkNew(); // Mark as new entity
        list.Add(newItem);

        // Act
        list.Remove(newItem);

        // Assert - New items don't go to DeletedList
        Assert.AreEqual(0, list.DeletedCount);
        Assert.IsFalse(list.Contains(newItem));
    }

    [TestMethod]
    public void UnDelete_WhenContainingListSet_RestoresProperState()
    {
        // Arrange
        var aggregateRoot = new EntityPerson();
        var list = new EntityPersonList();
        aggregateRoot.ChildList = list;

        var item = new EntityPerson();
        ((IEntityPerson)item).MarkOld();
        list.Add(item);
        ((IEntityPerson)item).MarkUnmodified();

        // Remove (marks deleted)
        list.Remove(item);
        Assert.IsTrue(item.IsDeleted);

        // Act - UnDelete the item
        item.UnDelete();

        // Assert
        Assert.IsFalse(item.IsDeleted);
        // ContainingList should still be set
        var itemInternal = (IEntityBaseInternal)item;
        Assert.AreSame(list, itemInternal.ContainingList);
    }
}
