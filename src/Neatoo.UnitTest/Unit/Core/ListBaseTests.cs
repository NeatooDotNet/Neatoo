using System.Collections.Specialized;
using System.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo;
using Neatoo.Internal;
using Neatoo.RemoteFactory;

namespace Neatoo.UnitTest.Unit.Core;

/// <summary>
/// Unit tests for the ListBase{I} class.
/// Tests collection operations, parent-child relationships, property change events,
/// and NeatooPropertyChanged event propagation.
/// Uses real Neatoo classes instead of mocks.
/// </summary>
[TestClass]
public class ListBaseTests
{
    #region Test Helper Classes

    /// <summary>
    /// Concrete implementation of ListBase for testing.
    /// ValidateListBase is the minimal concrete list base we can instantiate.
    /// </summary>
    [SuppressFactory]
    private class TestList : ValidateListBase<TestListItem>
    {
        public TestList() : base() { }
    }

    /// <summary>
    /// Simple ValidateBase implementation for list items.
    /// </summary>
    [SuppressFactory]
    private class TestListItem : ValidateBase<TestListItem>
    {
        public TestListItem() : base(new ValidateBaseServices<TestListItem>())
        {
            PauseAllActions();
        }

        public string? Name { get => Getter<string>(); set => Setter(value); }
        public int Value { get => Getter<int>(); set => Setter(value); }

        public void Resume() => ResumeAllActions();
    }

    /// <summary>
    /// Test parent class to verify parent assignment.
    /// </summary>
    [SuppressFactory]
    private class TestParent : ValidateBase<TestParent>
    {
        public TestParent() : base(new ValidateBaseServices<TestParent>())
        {
            PauseAllActions();
        }

        public TestList? Children { get => Getter<TestList>(); set => Setter(value); }
    }

    #endregion

    #region Constructor Tests

    [TestMethod]
    public void Constructor_CreatesEmptyList()
    {
        // Act
        var list = new TestList();

        // Assert
        Assert.AreEqual(0, list.Count);
    }

    [TestMethod]
    public void Constructor_ParentIsNull()
    {
        // Act
        var list = new TestList();

        // Assert
        Assert.IsNull(list.Parent);
    }

    [TestMethod]
    public void Constructor_IsBusyIsFalse()
    {
        // Act
        var list = new TestList();

        // Assert
        Assert.IsFalse(list.IsBusy);
    }

    #endregion

    #region Add Item Tests

    [TestMethod]
    public void Add_SingleItem_IncreasesCount()
    {
        // Arrange
        var list = new TestList();
        var item = new TestListItem();

        // Act
        list.Add(item);

        // Assert
        Assert.AreEqual(1, list.Count);
    }

    [TestMethod]
    public void Add_MultipleItems_IncreasesCount()
    {
        // Arrange
        var list = new TestList();
        var item1 = new TestListItem();
        var item2 = new TestListItem();
        var item3 = new TestListItem();

        // Act
        list.Add(item1);
        list.Add(item2);
        list.Add(item3);

        // Assert
        Assert.AreEqual(3, list.Count);
    }

    [TestMethod]
    public void Add_Item_CanBeAccessedByIndex()
    {
        // Arrange
        var list = new TestList();
        var item = new TestListItem();

        // Act
        list.Add(item);

        // Assert
        Assert.AreSame(item, list[0]);
    }

    [TestMethod]
    public void Add_Item_RaisesCollectionChangedEvent()
    {
        // Arrange
        var list = new TestList();
        var item = new TestListItem();
        NotifyCollectionChangedEventArgs? eventArgs = null;
        list.CollectionChanged += (s, e) => eventArgs = e;

        // Act
        list.Add(item);

        // Assert
        Assert.IsNotNull(eventArgs);
        Assert.AreEqual(NotifyCollectionChangedAction.Add, eventArgs.Action);
    }

    [TestMethod]
    public void Add_Item_RaisesNeatooPropertyChangedForCount()
    {
        // Arrange
        var list = new TestList();
        var item = new TestListItem();
        var propertyNames = new List<string>();
        list.NeatooPropertyChanged += args =>
        {
            propertyNames.Add(args.PropertyName);
            return Task.CompletedTask;
        };

        // Act
        list.Add(item);

        // Assert
        Assert.IsTrue(propertyNames.Contains("Count"));
    }

    #endregion

    #region Remove Item Tests

    [TestMethod]
    public void Remove_ExistingItem_DecreasesCount()
    {
        // Arrange
        var list = new TestList();
        var item = new TestListItem();
        list.Add(item);

        // Act
        list.Remove(item);

        // Assert
        Assert.AreEqual(0, list.Count);
    }

    [TestMethod]
    public void Remove_ExistingItem_RaisesCollectionChangedEvent()
    {
        // Arrange
        var list = new TestList();
        var item = new TestListItem();
        list.Add(item);
        NotifyCollectionChangedEventArgs? eventArgs = null;
        list.CollectionChanged += (s, e) => eventArgs = e;

        // Act
        list.Remove(item);

        // Assert
        Assert.IsNotNull(eventArgs);
        Assert.AreEqual(NotifyCollectionChangedAction.Remove, eventArgs.Action);
    }

    [TestMethod]
    public void RemoveAt_ValidIndex_RemovesItem()
    {
        // Arrange
        var list = new TestList();
        var item1 = new TestListItem();
        var item2 = new TestListItem();
        list.Add(item1);
        list.Add(item2);

        // Act
        list.RemoveAt(0);

        // Assert
        Assert.AreEqual(1, list.Count);
        Assert.AreSame(item2, list[0]);
    }

    [TestMethod]
    public void Remove_Item_RaisesNeatooPropertyChangedForCount()
    {
        // Arrange
        var list = new TestList();
        var item = new TestListItem();
        list.Add(item);
        var propertyNames = new List<string>();
        list.NeatooPropertyChanged += args =>
        {
            propertyNames.Add(args.PropertyName);
            return Task.CompletedTask;
        };

        // Act
        list.Remove(item);

        // Assert
        Assert.IsTrue(propertyNames.Contains("Count"));
    }

    #endregion

    #region IsBusy Tests

    [TestMethod]
    public void IsBusy_EmptyList_ReturnsFalse()
    {
        // Arrange
        var list = new TestList();

        // Assert
        Assert.IsFalse(list.IsBusy);
    }

    [TestMethod]
    public void IsBusy_AllItemsNotBusy_ReturnsFalse()
    {
        // Arrange
        var list = new TestList();
        var item1 = new TestListItem();
        var item2 = new TestListItem();
        list.Add(item1);
        list.Add(item2);

        // Assert
        Assert.IsFalse(list.IsBusy);
    }

    #endregion

    #region PropertyChanged Event Propagation Tests

    [TestMethod]
    public void ItemPropertyChanged_HandledByList()
    {
        // Arrange
        // List handles child PropertyChanged internally to check meta properties,
        // but doesn't relay child property changes directly.
        // It only raises its own PropertyChanged when meta properties (like IsBusy) change.
        var list = new TestList();
        var item = new TestListItem();
        item.Resume();
        list.Add(item);

        // Act - Changing item property shouldn't cause list PropertyChanged unless meta changes
        item.Name = "NewName";

        // Assert - List should still be in consistent state
        Assert.AreEqual(1, list.Count);
        Assert.AreEqual("NewName", list[0].Name);
    }

    [TestMethod]
    public void ItemNeatooPropertyChanged_PropagatesFromChild()
    {
        // Arrange
        var list = new TestList();
        var item = new TestListItem();
        item.Resume();
        list.Add(item);
        var neatooPropertyChangedCalled = false;
        list.NeatooPropertyChanged += args =>
        {
            neatooPropertyChangedCalled = true;
            return Task.CompletedTask;
        };

        // Act
        item.Name = "NewName";

        // Assert
        Assert.IsTrue(neatooPropertyChangedCalled);
    }

    [TestMethod]
    public void Remove_Item_UnsubscribesFromPropertyChanged()
    {
        // Arrange
        var list = new TestList();
        var item = new TestListItem();
        item.Resume();
        list.Add(item);
        list.Remove(item);
        var propertyChangedCalled = false;
        ((INotifyPropertyChanged)list).PropertyChanged += (s, e) => propertyChangedCalled = true;

        // Act - Change property after removal
        item.Name = "NewName";

        // Assert - List should not receive event
        Assert.IsFalse(propertyChangedCalled);
    }

    [TestMethod]
    public void Remove_Item_UnsubscribesFromNeatooPropertyChanged()
    {
        // Arrange
        var list = new TestList();
        var item = new TestListItem();
        item.Resume();
        list.Add(item);
        list.Remove(item);
        var neatooPropertyChangedCalled = false;
        list.NeatooPropertyChanged += args =>
        {
            neatooPropertyChangedCalled = true;
            return Task.CompletedTask;
        };

        // Act - Change property after removal
        item.Name = "NewName";

        // Assert - List should not receive event
        Assert.IsFalse(neatooPropertyChangedCalled);
    }

    #endregion

    #region Clear Tests

    [TestMethod]
    public void Clear_RemovesAllItems()
    {
        // Arrange
        var list = new TestList();
        list.Add(new TestListItem());
        list.Add(new TestListItem());
        list.Add(new TestListItem());

        // Act
        list.Clear();

        // Assert
        Assert.AreEqual(0, list.Count);
    }

    [TestMethod]
    public void Clear_RaisesCollectionChangedEvent()
    {
        // Arrange
        var list = new TestList();
        list.Add(new TestListItem());
        NotifyCollectionChangedEventArgs? eventArgs = null;
        list.CollectionChanged += (s, e) => eventArgs = e;

        // Act
        list.Clear();

        // Assert
        Assert.IsNotNull(eventArgs);
        Assert.AreEqual(NotifyCollectionChangedAction.Reset, eventArgs.Action);
    }

    #endregion

    #region Index Access Tests

    [TestMethod]
    public void Indexer_Get_ReturnsCorrectItem()
    {
        // Arrange
        var list = new TestList();
        var item1 = new TestListItem();
        var item2 = new TestListItem();
        list.Add(item1);
        list.Add(item2);

        // Assert
        Assert.AreSame(item1, list[0]);
        Assert.AreSame(item2, list[1]);
    }

    [TestMethod]
    public void Indexer_Set_ReplacesItem()
    {
        // Arrange
        var list = new TestList();
        var item1 = new TestListItem();
        var item2 = new TestListItem();
        list.Add(item1);

        // Act
        list[0] = item2;

        // Assert
        Assert.AreSame(item2, list[0]);
        Assert.AreEqual(1, list.Count);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentOutOfRangeException))]
    public void Indexer_Get_InvalidIndex_ThrowsException()
    {
        // Arrange
        var list = new TestList();

        // Act
        var item = list[0];
    }

    #endregion

    #region Insert Tests

    [TestMethod]
    public void Insert_AtBeginning_ShiftsExistingItems()
    {
        // Arrange
        var list = new TestList();
        var item1 = new TestListItem();
        var item2 = new TestListItem();
        list.Add(item1);

        // Act
        list.Insert(0, item2);

        // Assert
        Assert.AreSame(item2, list[0]);
        Assert.AreSame(item1, list[1]);
    }

    [TestMethod]
    public void Insert_AtEnd_AddsToEnd()
    {
        // Arrange
        var list = new TestList();
        var item1 = new TestListItem();
        var item2 = new TestListItem();
        list.Add(item1);

        // Act
        list.Insert(1, item2);

        // Assert
        Assert.AreSame(item1, list[0]);
        Assert.AreSame(item2, list[1]);
    }

    #endregion

    #region Contains Tests

    [TestMethod]
    public void Contains_ExistingItem_ReturnsTrue()
    {
        // Arrange
        var list = new TestList();
        var item = new TestListItem();
        list.Add(item);

        // Assert
        Assert.IsTrue(list.Contains(item));
    }

    [TestMethod]
    public void Contains_NonExistingItem_ReturnsFalse()
    {
        // Arrange
        var list = new TestList();
        var item1 = new TestListItem();
        var item2 = new TestListItem();
        list.Add(item1);

        // Assert
        Assert.IsFalse(list.Contains(item2));
    }

    #endregion

    #region IndexOf Tests

    [TestMethod]
    public void IndexOf_ExistingItem_ReturnsCorrectIndex()
    {
        // Arrange
        var list = new TestList();
        var item1 = new TestListItem();
        var item2 = new TestListItem();
        list.Add(item1);
        list.Add(item2);

        // Assert
        Assert.AreEqual(0, list.IndexOf(item1));
        Assert.AreEqual(1, list.IndexOf(item2));
    }

    [TestMethod]
    public void IndexOf_NonExistingItem_ReturnsNegativeOne()
    {
        // Arrange
        var list = new TestList();
        var item = new TestListItem();

        // Assert
        Assert.AreEqual(-1, list.IndexOf(item));
    }

    #endregion

    #region Enumeration Tests

    [TestMethod]
    public void Enumeration_ReturnsAllItems()
    {
        // Arrange
        var list = new TestList();
        var item1 = new TestListItem();
        var item2 = new TestListItem();
        var item3 = new TestListItem();
        list.Add(item1);
        list.Add(item2);
        list.Add(item3);

        // Act
        var items = list.ToList();

        // Assert
        Assert.AreEqual(3, items.Count);
        Assert.AreSame(item1, items[0]);
        Assert.AreSame(item2, items[1]);
        Assert.AreSame(item3, items[2]);
    }

    #endregion

    #region WaitForTasks Tests

    [TestMethod]
    public async Task WaitForTasks_EmptyList_CompletesImmediately()
    {
        // Arrange
        var list = new TestList();

        // Act & Assert - Should complete without exception
        await list.WaitForTasks();
    }

    [TestMethod]
    public async Task WaitForTasks_NoItemsBusy_CompletesImmediately()
    {
        // Arrange
        var list = new TestList();
        list.Add(new TestListItem());
        list.Add(new TestListItem());

        // Act & Assert - Should complete without exception
        await list.WaitForTasks();
    }

    #endregion

    #region Interface Implementation Tests

    [TestMethod]
    public void ImplementsIListBaseInterface()
    {
        // Act
        var list = new TestList();

        // Assert
        Assert.IsInstanceOfType(list, typeof(IValidateListBase));
    }

    [TestMethod]
    public void ImplementsIListBaseGenericInterface()
    {
        // Act
        var list = new TestList();

        // Assert
        Assert.IsInstanceOfType(list, typeof(IValidateListBase<TestListItem>));
    }

    [TestMethod]
    public void ImplementsINotifyCollectionChangedInterface()
    {
        // Act
        var list = new TestList();

        // Assert
        Assert.IsInstanceOfType(list, typeof(INotifyCollectionChanged));
    }

    [TestMethod]
    public void ImplementsINotifyPropertyChangedInterface()
    {
        // Act
        var list = new TestList();

        // Assert
        Assert.IsInstanceOfType(list, typeof(INotifyPropertyChanged));
    }

    [TestMethod]
    public void ImplementsINeatooObjectInterface()
    {
        // Act
        var list = new TestList();

        // Assert
        Assert.IsInstanceOfType(list, typeof(INeatooObject));
    }

    #endregion
}
