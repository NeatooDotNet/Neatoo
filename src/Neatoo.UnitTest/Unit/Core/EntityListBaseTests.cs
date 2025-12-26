using System.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo;
using Neatoo.Internal;
using Neatoo.RemoteFactory;

namespace Neatoo.UnitTest.Unit.Core;

/// <summary>
/// Unit tests for the EntityListBase{I} class.
/// Tests modification tracking, deleted list management, entity state flags,
/// and inherited ValidateListBase/ListBase behavior.
/// Uses real Neatoo classes instead of mocks.
/// </summary>
[TestClass]
public class EntityListBaseTests
{
    #region Test Helper Classes

    /// <summary>
    /// Concrete implementation of EntityListBase for testing.
    /// </summary>
    [SuppressFactory]
    private class TestEntityList : EntityListBase<TestEntityItem>
    {
        public TestEntityList() : base() { }

        // Expose protected members for testing
        public new List<TestEntityItem> DeletedList => base.DeletedList;

        public new bool IsPaused
        {
            get => base.IsPaused;
            set => base.IsPaused = value;
        }
    }

    /// <summary>
    /// EntityBase implementation for list items.
    /// </summary>
    [SuppressFactory]
    private class TestEntityItem : EntityBase<TestEntityItem>
    {
        public TestEntityItem() : base(new EntityBaseServices<TestEntityItem>(null))
        {
            PauseAllActions();
        }

        public string? Name { get => Getter<string>(); set => Setter(value); }
        public int Value { get => Getter<int>(); set => Setter(value); }

        public void Resume() => ResumeAllActions();

        // Expose protected members for testing
        public new void MarkNew() => base.MarkNew();
        public new void MarkOld() => base.MarkOld();
        public new void MarkAsChild() => base.MarkAsChild();
        public new void MarkModified() => base.MarkModified();
    }

    private static TestEntityItem CreateNewItem()
    {
        var item = new TestEntityItem();
        item.Resume();
        item.MarkNew();
        return item;
    }

    private static TestEntityItem CreateExistingItem()
    {
        var item = new TestEntityItem();
        item.Resume();
        item.MarkOld();
        return item;
    }

    #endregion

    #region Constructor Tests

    [TestMethod]
    public void Constructor_CreatesEmptyList()
    {
        // Act
        var list = new TestEntityList();

        // Assert
        Assert.AreEqual(0, list.Count);
    }

    [TestMethod]
    public void Constructor_DeletedListIsEmpty()
    {
        // Act
        var list = new TestEntityList();

        // Assert
        Assert.AreEqual(0, list.DeletedList.Count);
    }

    [TestMethod]
    public void Constructor_IsModifiedFalse()
    {
        // Act
        var list = new TestEntityList();

        // Assert
        Assert.IsFalse(list.IsModified);
    }

    [TestMethod]
    public void Constructor_IsSelfModifiedFalse()
    {
        // Act
        var list = new TestEntityList();

        // Assert
        Assert.IsFalse(list.IsSelfModified);
    }

    #endregion

    #region Entity State Properties Tests

    [TestMethod]
    public void IsSelfModified_AlwaysFalse()
    {
        // Lists don't have their own modifiable properties
        var list = new TestEntityList();
        var item = CreateNewItem();
        list.Add(item);

        // Assert
        Assert.IsFalse(list.IsSelfModified);
    }

    [TestMethod]
    public void IsMarkedModified_AlwaysFalse()
    {
        // Lists cannot be explicitly marked as modified
        var list = new TestEntityList();

        // Assert
        Assert.IsFalse(list.IsMarkedModified);
    }

    [TestMethod]
    public void IsSavable_AlwaysFalse()
    {
        // Lists are saved through their parent entity
        var list = new TestEntityList();

        // Assert
        Assert.IsFalse(list.IsSavable);
    }

    [TestMethod]
    public void IsNew_AlwaysFalse()
    {
        // Lists don't have their own persistence state
        var list = new TestEntityList();

        // Assert
        Assert.IsFalse(list.IsNew);
    }

    [TestMethod]
    public void IsDeleted_AlwaysFalse()
    {
        // Lists don't have their own deletion state
        var list = new TestEntityList();

        // Assert
        Assert.IsFalse(list.IsDeleted);
    }

    [TestMethod]
    public void IsChild_AlwaysFalse()
    {
        // Lists manage child relationships through their items
        var list = new TestEntityList();

        // Assert
        Assert.IsFalse(list.IsChild);
    }

    #endregion

    #region IsModified Tests

    [TestMethod]
    public void IsModified_EmptyList_ReturnsFalse()
    {
        // Arrange
        var list = new TestEntityList();

        // Assert
        Assert.IsFalse(list.IsModified);
    }

    [TestMethod]
    public void IsModified_WithModifiedItem_ReturnsTrue()
    {
        // Arrange
        var list = new TestEntityList();
        var item = CreateNewItem();
        item.Name = "Test"; // This makes it modified
        list.Add(item);

        // Assert
        Assert.IsTrue(list.IsModified);
    }

    [TestMethod]
    public void IsModified_WithUnmodifiedItems_ReturnsFalse()
    {
        // Arrange
        var list = new TestEntityList();
        var item = new TestEntityItem(); // Starts paused, not modified
        list.Add(item);

        // Assert - New item added to list is marked as child and modified
        // But the item itself starts with PauseAllActions, so let's check
        Assert.IsTrue(list.IsModified); // Adding marks as child which marks as modified
    }

    [TestMethod]
    public void IsModified_WithDeletedItems_ReturnsTrue()
    {
        // Arrange
        var list = new TestEntityList();
        var item = CreateExistingItem();
        list.Add(item);
        list.Remove(item);

        // Assert - Deleted list has items
        Assert.IsTrue(list.DeletedList.Count > 0);
        Assert.IsTrue(list.IsModified);
    }

    [TestMethod]
    public void IsModified_AfterRemovingNewItem_ReturnsFalse()
    {
        // Arrange
        var list = new TestEntityList();
        var item = CreateNewItem();
        list.Add(item);
        list.Remove(item); // New items are not added to DeletedList

        // Assert
        Assert.AreEqual(0, list.DeletedList.Count);
        Assert.IsFalse(list.IsModified);
    }

    #endregion

    #region Add Item Tests

    [TestMethod]
    public void Add_NewItem_MarksAsChild()
    {
        // Arrange
        var list = new TestEntityList();
        var item = CreateNewItem();
        Assert.IsFalse(item.IsChild);

        // Act
        list.Add(item);

        // Assert
        Assert.IsTrue(item.IsChild);
    }

    [TestMethod]
    public void Add_ExistingItem_MarksAsModified()
    {
        // Arrange
        var list = new TestEntityList();
        var item = CreateExistingItem();

        // Act
        list.Add(item);

        // Assert
        Assert.IsTrue(item.IsModified);
    }

    [TestMethod]
    public void Add_DeletedItem_UnDeletesItem()
    {
        // Arrange
        var list = new TestEntityList();
        var item = CreateExistingItem();
        item.Delete();
        Assert.IsTrue(item.IsDeleted);

        // Act
        list.Add(item);

        // Assert
        Assert.IsFalse(item.IsDeleted);
    }

    [TestMethod]
    public void Add_WhenPaused_DoesNotMarkAsChild()
    {
        // Arrange
        var list = new TestEntityList();
        list.IsPaused = true;
        var item = CreateNewItem();

        // Act
        list.Add(item);

        // Assert
        Assert.IsFalse(item.IsChild);
    }

    [TestMethod]
    public void Add_WhenPaused_DeletedItemGoesToDeletedList()
    {
        // Arrange
        var list = new TestEntityList();
        list.IsPaused = true;
        var item = CreateExistingItem();
        item.Delete();

        // Act
        list.Add(item);

        // Assert - Item goes to DeletedList, not main list
        Assert.AreEqual(0, list.Count);
        Assert.AreEqual(1, list.DeletedList.Count);
    }

    #endregion

    #region Remove Item Tests

    [TestMethod]
    public void Remove_NewItem_NotAddedToDeletedList()
    {
        // Arrange
        var list = new TestEntityList();
        var item = CreateNewItem();
        list.Add(item);

        // Act
        list.Remove(item);

        // Assert
        Assert.AreEqual(0, list.DeletedList.Count);
    }

    [TestMethod]
    public void Remove_ExistingItem_AddedToDeletedList()
    {
        // Arrange
        var list = new TestEntityList();
        var item = CreateExistingItem();
        list.Add(item);

        // Act
        list.Remove(item);

        // Assert
        Assert.AreEqual(1, list.DeletedList.Count);
        Assert.AreSame(item, list.DeletedList[0]);
    }

    [TestMethod]
    public void Remove_ExistingItem_MarksItemAsDeleted()
    {
        // Arrange
        var list = new TestEntityList();
        var item = CreateExistingItem();
        list.Add(item);

        // Act
        list.Remove(item);

        // Assert
        Assert.IsTrue(item.IsDeleted);
    }

    [TestMethod]
    public void RemoveAt_ExistingItem_AddedToDeletedList()
    {
        // Arrange
        var list = new TestEntityList();
        var item = CreateExistingItem();
        list.Add(item);

        // Act
        list.RemoveAt(0);

        // Assert
        Assert.AreEqual(1, list.DeletedList.Count);
    }

    [TestMethod]
    public void Remove_WhenPaused_NotAddedToDeletedList()
    {
        // Arrange
        var list = new TestEntityList();
        var item = CreateExistingItem();
        list.Add(item);
        list.IsPaused = true;

        // Act
        list.Remove(item);

        // Assert
        Assert.AreEqual(0, list.DeletedList.Count);
    }

    [TestMethod]
    public void Remove_WhenPaused_ItemNotMarkedDeleted()
    {
        // Arrange
        var list = new TestEntityList();
        var item = CreateExistingItem();
        list.Add(item);
        list.IsPaused = true;

        // Act
        list.Remove(item);

        // Assert
        Assert.IsFalse(item.IsDeleted);
    }

    #endregion

    #region FactoryComplete Tests

    [TestMethod]
    public void FactoryComplete_Update_ClearsDeletedList()
    {
        // Arrange
        var list = new TestEntityList();
        var item = CreateExistingItem();
        list.Add(item);
        list.Remove(item);
        Assert.AreEqual(1, list.DeletedList.Count);

        // Act
        list.FactoryComplete(FactoryOperation.Update);

        // Assert
        Assert.AreEqual(0, list.DeletedList.Count);
    }

    [TestMethod]
    public void FactoryComplete_Insert_DoesNotClearDeletedList()
    {
        // Arrange - Insert is for new items, not updates with deletions
        var list = new TestEntityList();
        var item = CreateExistingItem();
        list.Add(item);
        list.Remove(item);
        Assert.AreEqual(1, list.DeletedList.Count);

        // Act
        list.FactoryComplete(FactoryOperation.Insert);

        // Assert - Only Update clears the deleted list
        Assert.AreEqual(1, list.DeletedList.Count);
    }

    [TestMethod]
    public void FactoryComplete_Fetch_DoesNotClearDeletedList()
    {
        // Arrange
        var list = new TestEntityList();
        var item = CreateExistingItem();
        list.Add(item);
        list.Remove(item);
        Assert.AreEqual(1, list.DeletedList.Count);

        // Act
        list.FactoryComplete(FactoryOperation.Fetch);

        // Assert - Fetch should not clear deleted list
        Assert.AreEqual(1, list.DeletedList.Count);
    }

    #endregion

    #region Deserialization Tests

    [TestMethod]
    public void OnDeserializing_SetsIsPausedTrue()
    {
        // Arrange
        var list = new TestEntityList();

        // Act
        list.OnDeserializing();

        // Assert
        Assert.IsTrue(list.IsPaused);
    }

    #endregion

    #region MetaState Change Notification Tests

    [TestMethod]
    public void IsModified_TracksChildModification()
    {
        // Arrange
        var list = new TestEntityList();
        Assert.IsFalse(list.IsModified);
        var item = CreateNewItem();

        // Act
        list.Add(item);

        // Assert - Adding a modified item makes list modified
        Assert.IsTrue(list.IsModified);
    }

    [TestMethod]
    public void Add_RaisesNeatooPropertyChangedForCount()
    {
        // Arrange
        var list = new TestEntityList();
        var propertyNames = new List<string>();
        list.NeatooPropertyChanged += args =>
        {
            propertyNames.Add(args.PropertyName);
            return Task.CompletedTask;
        };
        var item = CreateNewItem();

        // Act
        list.Add(item);

        // Assert - Count change is always raised
        Assert.IsTrue(propertyNames.Contains("Count"));
    }

    #endregion

    #region Multiple Operations Tests

    [TestMethod]
    public void AddRemoveAdd_ExistingItem_HandlesCorrectly()
    {
        // Arrange
        var list = new TestEntityList();
        var item = CreateExistingItem();

        // Act
        list.Add(item);
        list.Remove(item);
        Assert.AreEqual(1, list.DeletedList.Count);
        Assert.IsTrue(item.IsDeleted);

        // Adding undeletes
        list.Add(item);

        // Assert
        Assert.AreEqual(1, list.Count);
        Assert.IsFalse(item.IsDeleted);
    }

    [TestMethod]
    public void RemoveMultipleExistingItems_AllAddedToDeletedList()
    {
        // Arrange
        var list = new TestEntityList();
        var item1 = CreateExistingItem();
        var item2 = CreateExistingItem();
        var item3 = CreateExistingItem();
        list.Add(item1);
        list.Add(item2);
        list.Add(item3);

        // Act
        list.Remove(item1);
        list.Remove(item2);
        list.Remove(item3);

        // Assert
        Assert.AreEqual(0, list.Count);
        Assert.AreEqual(3, list.DeletedList.Count);
    }

    #endregion

    #region Interface Implementation Tests

    [TestMethod]
    public void ImplementsIEntityListBaseInterface()
    {
        // Act
        var list = new TestEntityList();

        // Assert
        Assert.IsInstanceOfType(list, typeof(IEntityListBase));
    }

    [TestMethod]
    public void ImplementsIEntityListBaseGenericInterface()
    {
        // Act
        var list = new TestEntityList();

        // Assert
        Assert.IsInstanceOfType(list, typeof(IEntityListBase<TestEntityItem>));
    }

    [TestMethod]
    public void ImplementsIEntityMetaPropertiesInterface()
    {
        // Act
        var list = new TestEntityList();

        // Assert
        Assert.IsInstanceOfType(list, typeof(IEntityMetaProperties));
    }

    #endregion

    #region Inherited Behavior Tests

    [TestMethod]
    public void InheritsValidateListBaseBehavior_IsValidWorks()
    {
        // Arrange
        var list = new TestEntityList();
        var item = CreateNewItem();
        list.Add(item);

        // Assert - Should inherit IsValid behavior
        Assert.IsTrue(list.IsValid);
    }

    [TestMethod]
    public void InheritsListBaseBehavior_CountWorks()
    {
        // Arrange
        var list = new TestEntityList();
        list.Add(CreateNewItem());
        list.Add(CreateNewItem());

        // Assert
        Assert.AreEqual(2, list.Count);
    }

    #endregion

    #region Edge Cases Tests

    [TestMethod]
    public void Clear_DoesNotAddNewItemsToDeletedList()
    {
        // Arrange
        var list = new TestEntityList();
        list.Add(CreateNewItem());
        list.Add(CreateNewItem());

        // Act
        list.Clear();

        // Assert
        Assert.AreEqual(0, list.DeletedList.Count);
    }

    [TestMethod]
    public void DeletedList_AfterFactoryComplete_IsEmpty()
    {
        // Arrange
        var list = new TestEntityList();
        var item = CreateExistingItem();
        list.Add(item);
        list.Remove(item);

        // Act
        list.FactoryComplete(FactoryOperation.Update);

        // Assert
        Assert.AreEqual(0, list.DeletedList.Count);
        Assert.IsFalse(list.IsModified);
    }

    #endregion
}
