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

        /// <summary>
        /// Test helper: makes the item busy by adding a pending task.
        /// Call the returned action to release the busy state.
        /// </summary>
        public Action MarkBusyForTest()
        {
            var tcs = new TaskCompletionSource<bool>();
            RunningTasks.AddTask(tcs.Task);
            return () => tcs.SetResult(true);
        }

        // Expose protected members for testing
        public new void MarkNew() => base.MarkNew();
        public new void MarkOld() => base.MarkOld();
        public new void MarkAsChild() => base.MarkAsChild();
        public new void MarkModified() => base.MarkModified();
        public new void MarkUnmodified() => base.MarkUnmodified();
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

    // IsSavable_AlwaysFalse removed — IsSavable no longer on IEntityMetaProperties or IEntityListBase.
    // Lists are never savable; they don't expose IsSavable through any interface.

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
    public void Add_WhenPaused_MarksAsChild()
    {
        // Arrange
        var list = new TestEntityList();
        list.IsPaused = true;
        var item = CreateNewItem();

        // Act
        list.Add(item);

        // Assert - child identity is baseline-neutral state, applied on every
        // add. Paused adds skip the dirt-producing steps (MarkModified), not
        // the identity ones: a child loaded by a factory [Fetch] is still a
        // child, and without this Delete() would bypass list routing (ISNEW-003;
        // previously this asserted IsChild stayed false).
        Assert.IsTrue(item.IsChild);
    }

    [TestMethod]
    public void SetItem_ReplaceWithNewItem_ListBecomesModified()
    {
        // Replacement dirtied the graph for NEW items before the IsNew/IsModified
        // split - the cache arithmetic reads item.IsModified, which the weld made
        // true for any fresh item. Attach-marking has to carry that, or
        // `list[i] = newItem` on a clean root leaves it unsavable.
        var list = new TestEntityList();
        var existing = CreateExistingItem();

        list.IsPaused = true;
        list.Add(existing);
        list.ResumeAllActions();
        existing.MarkUnmodified();
        Assert.IsFalse(list.IsModified, "Precondition: clean list");

        // Act
        list[0] = CreateNewItem();

        // Assert
        Assert.IsTrue(list.IsModified, "Replacing with a new item dirties the list");
    }

    [TestMethod]
    public void FactoryComplete_AfterPausedAddOfModifiedItem_ListReportsModified()
    {
        // Arrange - the POSITIVE direction of the cached-modified recalculation.
        // InsertItem skips its cache update while paused, so before ISNEW-003
        // routed FactoryComplete through ResumeAllActions, _cachedChildrenModified
        // stayed at its initial false for every Fetch and Create - a list holding
        // a modified child reported IsModified=false.
        //
        // This is also the ISNEW-004 baseline: once IsNew is decoupled from
        // IsModified, a list holding only NEW children reports false again,
        // while a list holding a genuinely edited child (this test) stays true.
        var list = new TestEntityList();
        var item = CreateExistingItem();
        item.Name = "Edited";
        Assert.IsTrue(item.IsModified, "Precondition: item is modified before the add");

        list.FactoryStart(FactoryOperation.Fetch);

        // Act
        list.Add(item);
        list.FactoryComplete(FactoryOperation.Fetch);

        // Assert
        Assert.IsTrue(list.IsModified, "A list holding a modified child is modified");
    }

    [TestMethod]
    public void Add_WhenPaused_DoesNotMarkModified()
    {
        // Arrange - the other half of the paused-add contract: identity yes,
        // dirt no. An existing item added while paused must stay clean, which
        // is what keeps a factory [Fetch] baseline-clean.
        var list = new TestEntityList();
        list.IsPaused = true;
        var item = CreateExistingItem();

        // Act
        list.Add(item);

        // Assert - during the paused window...
        Assert.IsFalse(item.IsModified, "A paused add must not dirty the item");
        Assert.IsFalse(list.IsModified, "A paused add must not dirty the list");

        // ...and the resume must not manufacture dirt either
        list.FactoryComplete(FactoryOperation.Fetch);
        Assert.IsFalse(item.IsModified, "Factory completion must not dirty the item");
        Assert.IsFalse(list.IsModified, "Factory completion must not dirty the list");
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

    #region Multiple Children State Transitions Tests

    [TestMethod]
    public void IsModified_MultipleChildrenTransitions_TracksCorrectly()
    {
        // Arrange - Start with 3 unmodified existing items
        var list = new TestEntityList();
        var item1 = CreateExistingItem();
        var item2 = CreateExistingItem();
        var item3 = CreateExistingItem();

        // Add items while paused so they don't get marked modified
        list.IsPaused = true;
        list.Add(item1);
        list.Add(item2);
        list.Add(item3);
        list.IsPaused = false;

        // Mark all unmodified
        item1.MarkUnmodified();
        item2.MarkUnmodified();
        item3.MarkUnmodified();
        Assert.IsFalse(list.IsModified, "All items unmodified - list should not be modified");

        // Act/Assert - First item becomes modified
        item1.Name = "Modified1";
        Assert.IsTrue(list.IsModified, "One item modified - list should be modified");

        // Act/Assert - Second item also becomes modified
        item2.Name = "Modified2";
        Assert.IsTrue(list.IsModified, "Two items modified - list should be modified");

        // Act/Assert - First item becomes unmodified (but second still modified)
        item1.MarkUnmodified();
        Assert.IsTrue(list.IsModified, "One item still modified - list should be modified");

        // Act/Assert - Second item becomes unmodified (all unmodified now)
        item2.MarkUnmodified();
        Assert.IsFalse(list.IsModified, "All items unmodified again - list should not be modified");
    }

    [TestMethod]
    public void IsModified_DeletedListChanges_TracksCorrectly()
    {
        // Arrange - Create a fresh list with items added while paused
        var list = new TestEntityList();
        var item1 = CreateExistingItem();
        var item2 = CreateExistingItem();

        list.IsPaused = true;
        list.Add(item1);
        list.Add(item2);
        list.IsPaused = false;
        item1.MarkUnmodified();
        item2.MarkUnmodified();

        Assert.IsFalse(list.IsModified, "Starting state: no modifications");

        // Act - Remove item (adds to deleted list)
        list.Remove(item1);
        Assert.IsTrue(list.IsModified, "Deleted item exists - list should be modified");
        Assert.AreEqual(1, list.DeletedList.Count);

        // Act - Simulate save completion
        list.FactoryComplete(FactoryOperation.Update);
        Assert.IsFalse(list.IsModified, "After save - list should not be modified");
    }

    [TestMethod]
    public void IsModified_ChildBecomesModifiedThenUnmodifiedMultipleTimes_TracksCorrectly()
    {
        // Arrange
        var list = new TestEntityList();
        var item = CreateExistingItem();

        list.IsPaused = true;
        list.Add(item);
        list.IsPaused = false;
        item.MarkUnmodified();

        // Act/Assert - Toggle modification multiple times
        for (int i = 0; i < 5; i++)
        {
            Assert.IsFalse(list.IsModified, $"Iteration {i}: Item unmodified - list should not be modified");

            item.Name = $"Modified{i}";
            Assert.IsTrue(list.IsModified, $"Iteration {i}: Item modified - list should be modified");

            item.MarkUnmodified();
        }

        Assert.IsFalse(list.IsModified, "Final state: Item unmodified - list should not be modified");
    }

    // The NOTE that stood here said PropertyChanged tests for IsModified were not
    // included because "EntityListBase.IsModified is computed (uses Any()) and ...
    // does not raise PropertyChanged". That predates _cachedChildrenModified and
    // EntityListBase.CheckIfMetaPropertiesChanged, and LIST-003 disproved it: the
    // control test below shows a list DOES announce IsModified. The tests the NOTE
    // deferred are the four that follow.

    /// <summary>
    /// Captures PropertyChanged names raised by a list. The event is protected on
    /// ObservableCollection, so it is reachable only through the interface.
    /// </summary>
    private static List<string> CaptureNotifications(TestEntityList list)
    {
        var raised = new List<string>();
        ((INotifyPropertyChanged)list).PropertyChanged += (s, e) =>
        {
            if (e.PropertyName is { } name)
            {
                raised.Add(name);
            }
        };
        return raised;
    }

    /// <summary>
    /// Fetches a list holding the given persisted, unmodified children, the way a
    /// factory would - paused, then completed.
    /// </summary>
    private static TestEntityList FetchListWith(params TestEntityItem[] items)
    {
        var list = new TestEntityList();
        list.FactoryStart(FactoryOperation.Fetch);
        foreach (var item in items)
        {
            list.Add(item);
        }
        list.FactoryComplete(FactoryOperation.Fetch);
        return list;
    }

    [TestMethod]
    public void SaveCarryingDeletions_ThenChildEdit_AnnouncesIsModified()
    {
        // Arrange - THE LIST-003 DEFECT.
        //
        // FactoryComplete(Update) resumes first, and the resume snapshots the
        // meta-state baseline while DeletedList is still populated - so the baseline
        // records IsModified = true. The Update branch then clears DeletedList, making
        // the real value false. Before LIST-003 nothing corrected the baseline, so the
        // list sat at actual-false / baseline-true and the user's NEXT edit compared
        // true-against-true and announced nothing. The value self-healed (the meta
        // check at the end of HandlePropertyChanged is unguarded) but no parent or
        // binding ever heard, so an aggregate holding real unsaved work kept reporting
        // not-savable.
        //
        // Only reachable on a LOCAL save: a [Remote] save deserializes the returned
        // graph and OnDeserialized rebuilds a correct baseline, which is why every
        // [Remote] SaveLifecycle fixture missed this.
        var keep = CreateExistingItem();
        var drop = CreateExistingItem();
        var list = FetchListWith(keep, drop);
        Assert.IsFalse(list.IsModified, "Precondition: a fetched list is clean");

        list.Remove(drop);
        Assert.IsTrue(list.IsModified, "Precondition: the queued deletion dirties the list");
        Assert.AreEqual(1, list.DeletedList.Count);

        // Act - a local save, no serialization round trip
        list.FactoryStart(FactoryOperation.Update);
        list.FactoryComplete(FactoryOperation.Update);
        Assert.IsFalse(list.IsModified, "Precondition: the save cleared the deletion");

        var raised = CaptureNotifications(list);
        keep.Name = "Edited after the save";

        // Assert
        Assert.IsTrue(list.IsModified, "The edit dirties the list");
        CollectionAssert.Contains(
            raised,
            nameof(IEntityMetaProperties.IsModified),
            $"The list must announce the edit, not just compute it. Raised: [{string.Join(", ", raised)}]");
    }

    [TestMethod]
    public void SaveWithoutDeletions_ThenChildEdit_AnnouncesIsModified()
    {
        // Arrange - THE CONTROL for the test above, and the reason that test means
        // anything. It is the identical sequence minus the deletion. Because this one
        // announced even before LIST-003, the pair isolates the stale baseline as the
        // cause rather than "lists never announce IsModified" - which is exactly what
        // the NOTE removed from this file used to claim.
        var keep = CreateExistingItem();
        var list = FetchListWith(keep);

        // Act - a local save that carried no deletions
        list.FactoryStart(FactoryOperation.Update);
        list.FactoryComplete(FactoryOperation.Update);
        Assert.IsFalse(list.IsModified, "Precondition: nothing was pending");

        var raised = CaptureNotifications(list);
        keep.Name = "Edited after a clean save";

        // Assert
        Assert.IsTrue(list.IsModified);
        CollectionAssert.Contains(
            raised,
            nameof(IEntityMetaProperties.IsModified),
            $"Raised: [{string.Join(", ", raised)}]");
    }

    [TestMethod]
    public void FactoryComplete_Fetch_AnnouncesNothing()
    {
        // Arrange - the silence invariant that bounds the LIST-003 fix.
        //
        // Resume is deliberately silent: after a factory operation the post-factory
        // state IS the new baseline, so ResumeAllActions snapshots without announcing.
        // A fetch is not news. LIST-003 added a compare-and-announce to the UPDATE
        // branch only; if that ever migrates out of the Update branch, this fails.
        var item = CreateExistingItem();
        item.Name = "Modified before the fetch completes";

        var list = new TestEntityList();
        list.FactoryStart(FactoryOperation.Fetch);
        list.Add(item);

        var raised = CaptureNotifications(list);
        list.FactoryComplete(FactoryOperation.Fetch);

        // Assert
        Assert.IsTrue(list.IsModified, "The list does hold a modified child...");
        CollectionAssert.DoesNotContain(
            raised,
            nameof(IEntityMetaProperties.IsModified),
            $"...but completing a fetch must not announce it. Raised: [{string.Join(", ", raised)}]");
    }

    [TestMethod]
    public void FactoryComplete_Create_AnnouncesNothing()
    {
        // Arrange - the Create half of the silence invariant.
        //
        // Added on the LIST-003 test gate's recommendation. The fix sits inside
        // `if (factoryOperation == Update)`, so Create is unreachable *by construction*
        // today - but "structurally unreachable" is a property of the current guard, not
        // of the design. A future refactor that consolidated the guard the other way
        // round (say `if (factoryOperation != Fetch)`) would start announcing on Create
        // and the Fetch test alone would not notice.
        var item = CreateExistingItem();
        item.Name = "Modified before the create completes";

        var list = new TestEntityList();
        list.FactoryStart(FactoryOperation.Create);
        list.Add(item);

        var raised = CaptureNotifications(list);
        list.FactoryComplete(FactoryOperation.Create);

        // Assert
        Assert.IsTrue(list.IsModified, "The list does hold a modified child...");
        CollectionAssert.DoesNotContain(
            raised,
            nameof(IEntityMetaProperties.IsModified),
            $"...but completing a create must not announce it. Raised: [{string.Join(", ", raised)}]");
    }

    [TestMethod]
    public void HandlePropertyChanged_MetaCheckIsNotPauseGuarded()
    {
        // Arrange - pins a deliberate ASYMMETRY that looks like an oversight.
        //
        // HandlePropertyChanged guards its cache arithmetic on pause state but ends
        // with an UNGUARDED CheckIfMetaPropertiesChanged(). Adding an `if (!IsPaused)`
        // around that call "for symmetry" would reopen the ISNEW-003 defect with a
        // green suite, because it is what lets a list's meta state converge after
        // items change during a paused window. This test is the tripwire: it fails if
        // someone adds that guard.
        var item = CreateExistingItem();
        var list = FetchListWith(item);
        item.MarkUnmodified();

        // Act - mutate a child while the list is paused
        list.FactoryStart(FactoryOperation.Fetch);
        var raised = CaptureNotifications(list);
        item.Name = "Edited during the paused window";

        // Assert - the meta check ran despite the pause
        CollectionAssert.Contains(
            raised,
            nameof(IEntityMetaProperties.IsModified),
            "The meta check at the end of HandlePropertyChanged is intentionally NOT "
            + $"pause-guarded. Raised: [{string.Join(", ", raised)}]");

        list.FactoryComplete(FactoryOperation.Fetch);
    }

    #endregion

    #region Paused-Path Guards (LIST-004)

    [TestMethod]
    public void Delete_WhenListPaused_RecordsTheDeletionInsteadOfDiscardingIt()
    {
        // Arrange - THE LIST-004 DEFECT.
        //
        // Delete() used to delegate unconditionally to ContainingList.Remove(this).
        // RemoveItem does its mark-deleted-and-queue work inside `if (!IsPaused)`, so
        // delegating into a paused list removed the child and recorded NOTHING: no
        // MarkDeleted, no DeletedList entry, and therefore no DELETE at save time. The
        // row was silently orphaned.
        //
        // Reachable only because ISNEW-003 began setting ContainingList on children
        // added during a paused window.
        var item = CreateExistingItem();
        var list = new TestEntityList();
        list.FactoryStart(FactoryOperation.Fetch);
        list.Add(item);
        Assert.IsTrue(list.IsPaused, "Precondition: the list is inside a paused window");

        // Act
        item.Delete();

        // Assert - the intent survives
        Assert.IsTrue(item.IsDeleted, "The deletion must be recorded, not discarded");
        Assert.IsTrue(
            list.Contains(item),
            "Marked in place rather than removed: the canonical list [Update] iterates "
            + "this.Union(DeletedList) and filters on IsDeleted, so a marked item still in "
            + "the list is persisted correctly");

        list.FactoryComplete(FactoryOperation.Fetch);
    }

    [TestMethod]
    public void Delete_WhenListIsLive_StillRoutesThroughTheList()
    {
        // Arrange - the other half of the LIST-004 contract: the live path is UNCHANGED.
        // Delete() on a child of a live list must still route through Remove, so the item
        // leaves the list and lands in DeletedList exactly as before.
        var item = CreateExistingItem();
        var list = FetchListWith(item);
        Assert.IsFalse(list.IsPaused, "Precondition: the list is live");

        // Act
        item.Delete();

        // Assert
        Assert.IsFalse(list.Contains(item), "A live delete removes the item from the list");
        Assert.AreEqual(1, list.DeletedList.Count, "...and queues it for persistence deletion");
        Assert.IsTrue(item.IsDeleted);
    }

    [TestMethod]
    public void Delete_WhenParentless_MarksDeleted()
    {
        // Arrange - the third routing case, unchanged by LIST-004: an entity with no
        // containing list marks itself.
        var item = CreateExistingItem();

        // Act
        item.Delete();

        // Assert
        Assert.IsTrue(item.IsDeleted);
    }

    [TestMethod]
    public void Add_WhenPaused_AllowsDuplicate_UnlikeTheLivePath()
    {
        // Arrange - dispositions one of the guards the paused InsertItem branch skips.
        //
        // The live branch rejects a duplicate add outright. The paused branch skips that
        // check, along with the busy-item and cross-aggregate checks, because factory and
        // deserialization input is trusted and the checks cost a scan per add on a path
        // that loads whole graphs.
        //
        // Asserted in the direction the code actually behaves so the skip is a recorded
        // decision rather than an open question. If the paused branch is ever made to
        // enforce this, this test fails and the decision gets revisited deliberately.
        var item = CreateExistingItem();
        var list = new TestEntityList();
        list.FactoryStart(FactoryOperation.Fetch);

        // Act
        list.Add(item);
        list.Add(item);

        // Assert
        Assert.AreEqual(2, list.Count, "The paused branch does not screen duplicates");

        list.FactoryComplete(FactoryOperation.Fetch);
    }

    [TestMethod]
    public void Add_WhenPaused_AllowsBusyItem_UnlikeTheLivePath()
    {
        // Arrange - the busy half of the same disposition. The live branch refuses a
        // busy item because adding one mid-async-rule would fold an indeterminate
        // IsValid into the list's cache. The paused branch skips the check: factory
        // input is trusted, and the resume recalculates the caches wholesale anyway.
        var item = CreateExistingItem();
        var release = item.MarkBusyForTest();
        Assert.IsTrue(item.IsBusy, "Precondition: the item is busy");

        var list = new TestEntityList();
        list.FactoryStart(FactoryOperation.Fetch);

        // Act
        list.Add(item);

        // Assert
        Assert.AreEqual(1, list.Count, "The paused branch does not screen busy items");

        release();
        list.FactoryComplete(FactoryOperation.Fetch);
    }

    [TestMethod]
    public void Add_WhenLive_RejectsBusyItem()
    {
        // Arrange - the live-path counterpart of the busy skip.
        var item = CreateExistingItem();
        var release = item.MarkBusyForTest();
        var list = new TestEntityList();

        // Act & Assert
        var ex = Assert.ThrowsExactly<InvalidOperationException>(() => list.Add(item));
        Assert.IsTrue(
            ex.Message.Contains("busy"),
            $"The refusal should name the reason: {ex.Message}");

        release();
    }

    [TestMethod]
    public void Add_WhenLive_RejectsDuplicate()
    {
        // Arrange - the live-path counterpart, so the pair shows the asymmetry is real
        // and deliberate rather than an accident of which branch got the check.
        var item = CreateExistingItem();
        var list = FetchListWith(item);

        // Act & Assert
        Assert.ThrowsExactly<InvalidOperationException>(() => list.Add(item));
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

    #region Caching Edge Cases Tests

    [TestMethod]
    public void SetItem_ReplaceUnmodifiedWithModified_ListBecomesModified()
    {
        // Arrange
        var list = new TestEntityList();
        var item1 = CreateExistingItem();
        var item2 = CreateExistingItem();

        list.IsPaused = true;
        list.Add(item1);
        list.Add(item2);
        list.IsPaused = false;
        item1.MarkUnmodified();
        item2.MarkUnmodified();
        Assert.IsFalse(list.IsModified);

        // Act - Replace first item with a modified item
        var modifiedItem = CreateExistingItem();
        modifiedItem.Name = "Modified";
        list[0] = modifiedItem;

        // Assert
        Assert.IsTrue(list.IsModified);
    }

    [TestMethod]
    public void SetItem_ReplaceModifiedWithUnmodified_WhenOnlyModified_ListBecomesUnmodified()
    {
        // Arrange
        var list = new TestEntityList();
        var modifiedItem = CreateExistingItem();
        var unmodifiedItem = CreateExistingItem();

        list.IsPaused = true;
        list.Add(modifiedItem);
        list.Add(unmodifiedItem);
        list.ResumeAllActions();
        unmodifiedItem.MarkUnmodified();
        modifiedItem.Name = "Modified";
        Assert.IsTrue(list.IsModified);

        // Act - Replace modified item with an unmodified one (not paused, so cache updates)
        var newUnmodifiedItem = CreateExistingItem();
        newUnmodifiedItem.MarkUnmodified();
        list[0] = newUnmodifiedItem;

        // Assert
        Assert.IsFalse(list.IsModified);
    }

    [TestMethod]
    public void PauseThenResume_WithModifiedItems_CacheRecalculatedOnResume()
    {
        // Arrange
        var list = new TestEntityList();
        list.IsPaused = true;

        var modifiedItem = CreateExistingItem();
        modifiedItem.Name = "Modified while paused";
        list.Add(modifiedItem);

        // While paused, cache is not updated, but after resume it should be correct
        list.ResumeAllActions();

        // Assert
        Assert.IsTrue(list.IsModified);
    }

    [TestMethod]
    public void FactoryComplete_Update_RecalculatesCache()
    {
        // Arrange
        var list = new TestEntityList();
        var item = CreateExistingItem();

        list.IsPaused = true;
        list.Add(item);
        list.IsPaused = false;
        item.MarkUnmodified();
        Assert.IsFalse(list.IsModified);

        // Make item modified, then remove to DeletedList
        item.Name = "Modified";
        list.Remove(item);
        Assert.IsTrue(list.IsModified, "Should be modified due to DeletedList");

        // Act - Simulate save
        list.FactoryComplete(FactoryOperation.Update);

        // Assert
        Assert.IsFalse(list.IsModified);
    }

    [TestMethod]
    public void Clear_ResetsModifiedCache()
    {
        // Arrange
        var list = new TestEntityList();
        var item = CreateNewItem();
        item.Name = "Modified";
        list.Add(item);
        Assert.IsTrue(list.IsModified);

        // Act
        list.Clear();

        // Assert - No children, no deleted items, not modified
        Assert.IsFalse(list.IsModified);
    }

    #endregion

    #region Large List Performance Tests

    [TestMethod]
    public void LargeList_AddManyItems_IsModifiedTracksCorrectly()
    {
        // Arrange
        var list = new TestEntityList();
        const int itemCount = 1000;

        // Act - Add 1000 new items (all will be modified since they're new)
        for (int i = 0; i < itemCount; i++)
        {
            var item = CreateNewItem();
            list.Add(item);
        }

        // Assert
        Assert.AreEqual(itemCount, list.Count);
        Assert.IsTrue(list.IsModified);
    }

    [TestMethod]
    public void LargeList_UnmodifiedItems_IsModifiedFalse()
    {
        // Arrange
        var list = new TestEntityList();
        const int itemCount = 1000;

        list.IsPaused = true;
        for (int i = 0; i < itemCount; i++)
        {
            var item = CreateExistingItem();
            list.Add(item);
        }
        list.ResumeAllActions();

        // Mark all as unmodified
        foreach (var item in list)
        {
            item.MarkUnmodified();
        }

        // Assert
        Assert.IsFalse(list.IsModified);
    }

    [TestMethod]
    public void LargeList_OneModifiedAmongMany_IsModifiedTrue()
    {
        // Arrange
        var list = new TestEntityList();
        const int itemCount = 1000;

        list.IsPaused = true;
        for (int i = 0; i < itemCount; i++)
        {
            var item = CreateExistingItem();
            list.Add(item);
        }
        list.ResumeAllActions();

        foreach (var item in list)
        {
            item.MarkUnmodified();
        }
        Assert.IsFalse(list.IsModified);

        // Act - Modify one item in the middle
        list[500].Name = "Modified";

        // Assert
        Assert.IsTrue(list.IsModified);
    }

    [TestMethod]
    public void LargeList_MultipleModifiedItems_MarkUnmodifiedOneByOne()
    {
        // Arrange
        var list = new TestEntityList();
        const int itemCount = 1000;
        const int modifiedCount = 100;

        list.IsPaused = true;
        for (int i = 0; i < itemCount; i++)
        {
            var item = CreateExistingItem();
            list.Add(item);
        }
        list.ResumeAllActions();

        // Mark all unmodified first
        foreach (var item in list)
        {
            item.MarkUnmodified();
        }

        // Make first 100 items modified
        for (int i = 0; i < modifiedCount; i++)
        {
            list[i].Name = $"Modified{i}";
        }
        Assert.IsTrue(list.IsModified);

        // Act - Mark unmodified all but last modified item
        for (int i = 0; i < modifiedCount - 1; i++)
        {
            list[i].MarkUnmodified();
            Assert.IsTrue(list.IsModified, $"Should still be modified after unmarking item {i}");
        }

        // Mark last modified item as unmodified
        list[modifiedCount - 1].MarkUnmodified();

        // Assert
        Assert.IsFalse(list.IsModified);
    }

    [TestMethod]
    public void LargeList_RapidModificationChanges_CacheStaysConsistent()
    {
        // Arrange
        var list = new TestEntityList();
        const int itemCount = 500;

        list.IsPaused = true;
        for (int i = 0; i < itemCount; i++)
        {
            var item = CreateExistingItem();
            list.Add(item);
        }
        list.ResumeAllActions();

        foreach (var item in list)
        {
            item.MarkUnmodified();
        }

        // Act - Rapidly toggle modification on multiple items
        for (int round = 0; round < 10; round++)
        {
            // Modify items 0-99
            for (int i = 0; i < 100; i++)
            {
                list[i].Name = $"Modified{round}_{i}";
            }
            Assert.IsTrue(list.IsModified, $"Round {round}: Should be modified after changes");

            // Mark them unmodified again
            for (int i = 0; i < 100; i++)
            {
                list[i].MarkUnmodified();
            }
            Assert.IsFalse(list.IsModified, $"Round {round}: Should be unmodified after marking");
        }
    }

    [TestMethod]
    public void LargeList_RemoveItems_IsModifiedUpdatesCorrectly()
    {
        // Arrange
        var list = new TestEntityList();
        const int itemCount = 500;

        list.IsPaused = true;
        for (int i = 0; i < itemCount; i++)
        {
            var item = CreateExistingItem();
            list.Add(item);
        }
        list.ResumeAllActions();

        foreach (var item in list)
        {
            item.MarkUnmodified();
        }
        Assert.IsFalse(list.IsModified);

        // Act - Remove items (they go to DeletedList)
        list.RemoveAt(400);
        Assert.IsTrue(list.IsModified, "Should be modified with 1 deleted item");

        list.RemoveAt(300);
        Assert.IsTrue(list.IsModified, "Should be modified with 2 deleted items");

        list.RemoveAt(200);
        Assert.IsTrue(list.IsModified, "Should be modified with 3 deleted items");

        // Simulate save
        list.FactoryComplete(FactoryOperation.Update);

        // Assert
        Assert.IsFalse(list.IsModified);
        Assert.AreEqual(0, list.DeletedList.Count);
    }

    [TestMethod]
    public void LargeList_ClearList_ResetsModifiedState()
    {
        // Arrange
        var list = new TestEntityList();
        const int itemCount = 1000;

        for (int i = 0; i < itemCount; i++)
        {
            var item = CreateNewItem();
            item.Name = $"Item{i}";
            list.Add(item);
        }
        Assert.IsTrue(list.IsModified);

        // Act
        list.Clear();

        // Assert - No children, no deleted items (new items don't go to DeletedList)
        Assert.IsFalse(list.IsModified);
        Assert.AreEqual(0, list.Count);
    }

    [TestMethod]
    public void LargeList_SetItem_UpdatesCacheCorrectly()
    {
        // Arrange
        var list = new TestEntityList();
        const int itemCount = 500;

        list.IsPaused = true;
        for (int i = 0; i < itemCount; i++)
        {
            var item = CreateExistingItem();
            list.Add(item);
        }
        list.ResumeAllActions();

        foreach (var item in list)
        {
            item.MarkUnmodified();
        }
        Assert.IsFalse(list.IsModified);

        // Act - Replace item at position 250 with modified item
        var modifiedItem = CreateExistingItem();
        modifiedItem.Name = "Modified";
        list[250] = modifiedItem;

        // Assert
        Assert.IsTrue(list.IsModified);

        // Act - Replace with unmodified item
        var unmodifiedItem = CreateExistingItem();
        list.IsPaused = true;
        list[250] = unmodifiedItem;
        list.ResumeAllActions();
        unmodifiedItem.MarkUnmodified();

        // Assert
        Assert.IsFalse(list.IsModified);
    }

    [TestMethod]
    public void LargeList_MixedOperations_CacheStaysConsistent()
    {
        // Arrange
        var list = new TestEntityList();
        const int itemCount = 300;

        list.IsPaused = true;
        for (int i = 0; i < itemCount; i++)
        {
            var item = CreateExistingItem();
            list.Add(item);
        }
        list.ResumeAllActions();

        foreach (var item in list)
        {
            item.MarkUnmodified();
        }
        Assert.IsFalse(list.IsModified);

        // Act - Mix of operations
        // 1. Modify some items
        list[50].Name = "Modified50";
        list[100].Name = "Modified100";
        Assert.IsTrue(list.IsModified);

        // 2. Remove an item (goes to DeletedList)
        list.RemoveAt(200);
        Assert.IsTrue(list.IsModified);

        // 3. Add a new item
        var newItem = CreateNewItem();
        list.Add(newItem);
        Assert.IsTrue(list.IsModified);

        // 4. Mark modified items as unmodified
        list[50].MarkUnmodified();
        list[100].MarkUnmodified();
        Assert.IsTrue(list.IsModified, "Still modified due to DeletedList and new item");

        // 5. Remove the new item (doesn't go to DeletedList)
        list.Remove(newItem);
        Assert.IsTrue(list.IsModified, "Still modified due to DeletedList");

        // 6. Simulate save
        list.FactoryComplete(FactoryOperation.Update);
        Assert.IsFalse(list.IsModified);
    }

    #endregion
}
