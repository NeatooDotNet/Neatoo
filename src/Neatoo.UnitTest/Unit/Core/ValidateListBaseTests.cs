using System.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo;
using Neatoo.Internal;
using Neatoo.RemoteFactory;
using Neatoo.Rules;

namespace Neatoo.UnitTest.Unit.Core;

/// <summary>
/// Unit tests for the ValidateListBase{I} class.
/// Tests validation state propagation, IsValid/IsSelfValid behavior, IsPaused functionality,
/// rule execution across list items, and message aggregation.
/// Uses real Neatoo classes instead of mocks.
/// </summary>
[TestClass]
public class ValidateListBaseTests
{
    #region Test Helper Classes

    /// <summary>
    /// Concrete implementation of ValidateListBase for testing.
    /// </summary>
    [SuppressFactory]
    private class TestValidateList : ValidateListBase<TestValidateItem>
    {
        public TestValidateList() : base() { }

        // Expose protected members for testing
        public new bool IsPaused
        {
            get => base.IsPaused;
            set => base.IsPaused = value;
        }
    }

    /// <summary>
    /// ValidateBase implementation for list items with validation support.
    /// </summary>
    [SuppressFactory]
    private class TestValidateItem : ValidateBase<TestValidateItem>
    {
        public TestValidateItem() : base(new ValidateBaseServices<TestValidateItem>())
        {
            PauseAllActions();
        }

        public string? Name { get => Getter<string>(); set => Setter(value); }
        public int Value { get => Getter<int>(); set => Setter(value); }

        public void Resume() => ResumeAllActions();

        public void AddValidationError(string message)
        {
            MarkInvalid(message);
        }

        public void ClearErrors()
        {
            ClearAllMessages();
        }
    }

    #endregion

    #region Constructor Tests

    [TestMethod]
    public void Constructor_CreatesEmptyList()
    {
        // Act
        var list = new TestValidateList();

        // Assert
        Assert.AreEqual(0, list.Count);
    }

    [TestMethod]
    public void Constructor_IsValidTrue_WhenEmpty()
    {
        // Act
        var list = new TestValidateList();

        // Assert
        Assert.IsTrue(list.IsValid);
    }

    [TestMethod]
    public void Constructor_IsSelfValidTrue()
    {
        // Act
        var list = new TestValidateList();

        // Assert
        Assert.IsTrue(list.IsSelfValid);
    }

    [TestMethod]
    public void Constructor_IsPausedFalse()
    {
        // Act
        var list = new TestValidateList();

        // Assert
        Assert.IsFalse(list.IsPaused);
    }

    [TestMethod]
    public void Constructor_PropertyMessagesEmpty()
    {
        // Act
        var list = new TestValidateList();

        // Assert
        Assert.AreEqual(0, list.PropertyMessages.Count);
    }

    #endregion

    #region IsValid Tests

    [TestMethod]
    public void IsValid_EmptyList_ReturnsTrue()
    {
        // Arrange
        var list = new TestValidateList();

        // Assert
        Assert.IsTrue(list.IsValid);
    }

    [TestMethod]
    public void IsValid_AllItemsValid_ReturnsTrue()
    {
        // Arrange
        var list = new TestValidateList();
        var item1 = new TestValidateItem();
        var item2 = new TestValidateItem();
        list.Add(item1);
        list.Add(item2);

        // Assert
        Assert.IsTrue(list.IsValid);
    }

    [TestMethod]
    public void IsValid_OneItemInvalid_ReturnsFalse()
    {
        // Arrange
        var list = new TestValidateList();
        var item1 = new TestValidateItem();
        var item2 = new TestValidateItem();
        item2.Resume();
        item2.AddValidationError("Name is required");
        list.Add(item1);
        list.Add(item2);

        // Assert
        Assert.IsFalse(list.IsValid);
    }

    [TestMethod]
    public void IsValid_AllItemsInvalid_ReturnsFalse()
    {
        // Arrange
        var list = new TestValidateList();
        var item1 = new TestValidateItem();
        var item2 = new TestValidateItem();
        item1.Resume();
        item2.Resume();
        item1.AddValidationError("Name is required");
        item2.AddValidationError("Value is required");
        list.Add(item1);
        list.Add(item2);

        // Assert
        Assert.IsFalse(list.IsValid);
    }

    [TestMethod]
    public void IsValid_ItemBecomesInvalid_UpdatesListIsValid()
    {
        // Arrange
        var list = new TestValidateList();
        var item = new TestValidateItem();
        item.Resume();
        list.Add(item);
        Assert.IsTrue(list.IsValid);

        // Act
        item.AddValidationError("Name is required");

        // Assert
        Assert.IsFalse(list.IsValid);
    }

    [TestMethod]
    public void IsValid_ItemBecomesValid_UpdatesListIsValid()
    {
        // Arrange
        var list = new TestValidateList();
        var item = new TestValidateItem();
        item.Resume();
        item.AddValidationError("Name is required");
        list.Add(item);
        Assert.IsFalse(list.IsValid);

        // Act
        item.ClearErrors();

        // Assert
        Assert.IsTrue(list.IsValid);
    }

    [TestMethod]
    public void IsValid_RemoveInvalidItem_BecomesValid()
    {
        // Arrange
        var list = new TestValidateList();
        var validItem = new TestValidateItem();
        var invalidItem = new TestValidateItem();
        invalidItem.Resume();
        invalidItem.AddValidationError("Name is required");
        list.Add(validItem);
        list.Add(invalidItem);
        Assert.IsFalse(list.IsValid);

        // Act
        list.Remove(invalidItem);

        // Assert
        Assert.IsTrue(list.IsValid);
    }

    #endregion

    #region IsSelfValid Tests

    [TestMethod]
    public void IsSelfValid_AlwaysTrue()
    {
        // Lists don't have their own validation rules
        // Arrange
        var list = new TestValidateList();
        var invalidItem = new TestValidateItem();
        invalidItem.Resume();
        invalidItem.AddValidationError("Error");
        list.Add(invalidItem);

        // Assert - IsSelfValid should still be true even with invalid children
        Assert.IsTrue(list.IsSelfValid);
        Assert.IsFalse(list.IsValid);
    }

    #endregion

    #region PropertyMessages Tests

    [TestMethod]
    public void PropertyMessages_EmptyList_ReturnsEmpty()
    {
        // Arrange
        var list = new TestValidateList();

        // Assert
        Assert.AreEqual(0, list.PropertyMessages.Count);
    }

    [TestMethod]
    public void PropertyMessages_AllItemsValid_ReturnsEmpty()
    {
        // Arrange
        var list = new TestValidateList();
        list.Add(new TestValidateItem());
        list.Add(new TestValidateItem());

        // Assert
        Assert.AreEqual(0, list.PropertyMessages.Count);
    }

    [TestMethod]
    public void PropertyMessages_OneItemWithError_ReturnsItemMessages()
    {
        // Arrange
        var list = new TestValidateList();
        var item = new TestValidateItem();
        item.Resume();
        item.AddValidationError("Name is required");
        list.Add(item);

        // Assert
        Assert.AreEqual(1, list.PropertyMessages.Count);
    }

    [TestMethod]
    public void PropertyMessages_MultipleItemsWithErrors_ReturnsAllMessages()
    {
        // Arrange
        var list = new TestValidateList();
        var item1 = new TestValidateItem();
        var item2 = new TestValidateItem();
        item1.Resume();
        item2.Resume();
        item1.AddValidationError("Error 1");
        item2.AddValidationError("Error 2");
        list.Add(item1);
        list.Add(item2);

        // Assert - Each item has 1 ObjectInvalid message
        Assert.AreEqual(2, list.PropertyMessages.Count);
    }

    [TestMethod]
    public void PropertyMessages_ItemRemoved_NoLongerIncludesItemMessages()
    {
        // Arrange
        var list = new TestValidateList();
        var item = new TestValidateItem();
        item.Resume();
        item.AddValidationError("Name is required");
        list.Add(item);
        Assert.AreEqual(1, list.PropertyMessages.Count);

        // Act
        list.Remove(item);

        // Assert
        Assert.AreEqual(0, list.PropertyMessages.Count);
    }

    #endregion

    #region IsPaused Tests

    [TestMethod]
    public void IsPaused_InitialState_ReturnsFalse()
    {
        // Arrange
        var list = new TestValidateList();

        // Assert
        Assert.IsFalse(list.IsPaused);
    }

    [TestMethod]
    public void IsPaused_CanBeSetToTrue()
    {
        // Arrange
        var list = new TestValidateList();

        // Act
        list.IsPaused = true;

        // Assert
        Assert.IsTrue(list.IsPaused);
    }

    [TestMethod]
    public void IsPaused_CanBeSetToFalse()
    {
        // Arrange
        var list = new TestValidateList();
        list.IsPaused = true;

        // Act
        list.IsPaused = false;

        // Assert
        Assert.IsFalse(list.IsPaused);
    }

    #endregion

    #region ResumeAllActions Tests

    [TestMethod]
    public void ResumeAllActions_WhenPaused_SetsIsPausedFalse()
    {
        // Arrange
        var list = new TestValidateList();
        list.IsPaused = true;

        // Act
        list.ResumeAllActions();

        // Assert
        Assert.IsFalse(list.IsPaused);
    }

    [TestMethod]
    public void ResumeAllActions_WhenNotPaused_StaysNotPaused()
    {
        // Arrange
        var list = new TestValidateList();

        // Act
        list.ResumeAllActions();

        // Assert
        Assert.IsFalse(list.IsPaused);
    }

    #endregion

    #region ClearAllMessages Tests

    [TestMethod]
    public void ClearAllMessages_ClearsMessagesFromAllItems()
    {
        // Arrange
        var list = new TestValidateList();
        var item1 = new TestValidateItem();
        var item2 = new TestValidateItem();
        item1.Resume();
        item2.Resume();
        item1.AddValidationError("Error 1");
        item2.AddValidationError("Error 2");
        list.Add(item1);
        list.Add(item2);
        Assert.AreEqual(2, list.PropertyMessages.Count);

        // Act
        list.ClearAllMessages();

        // Assert
        Assert.AreEqual(0, list.PropertyMessages.Count);
        Assert.IsTrue(list.IsValid);
    }

    #endregion

    #region ClearSelfMessages Tests

    [TestMethod]
    public void ClearSelfMessages_ClearsSelfMessagesFromAllItems()
    {
        // Arrange
        var list = new TestValidateList();
        var item = new TestValidateItem();
        item.Resume();
        item.AddValidationError("Error");
        list.Add(item);

        // Act
        list.ClearSelfMessages();

        // Assert
        Assert.AreEqual(0, list.PropertyMessages.Count);
    }

    #endregion

    #region RunRules Tests

    [TestMethod]
    public async Task RunRules_WithPropertyName_RunsOnAllItems()
    {
        // Arrange
        var list = new TestValidateList();
        var item1 = new TestValidateItem();
        var item2 = new TestValidateItem();
        item1.Resume();
        item2.Resume();
        list.Add(item1);
        list.Add(item2);

        // Act - Should not throw
        await list.RunRules("Name");

        // Assert - Basic check that it completed
        Assert.IsTrue(true);
    }

    [TestMethod]
    public async Task RunRules_WithFlag_RunsOnAllItems()
    {
        // Arrange
        var list = new TestValidateList();
        var item1 = new TestValidateItem();
        var item2 = new TestValidateItem();
        item1.Resume();
        item2.Resume();
        list.Add(item1);
        list.Add(item2);

        // Act - Should not throw
        await list.RunRules(RunRulesFlag.All);

        // Assert - Basic check that it completed
        Assert.IsTrue(true);
    }

    [TestMethod]
    public async Task RunRules_EmptyList_CompletesWithoutError()
    {
        // Arrange
        var list = new TestValidateList();

        // Act & Assert - Should complete without exception
        await list.RunRules(RunRulesFlag.All);
    }

    #endregion

    #region FactoryStart/FactoryComplete Tests

    [TestMethod]
    public void FactoryStart_SetsIsPausedTrue()
    {
        // Arrange
        var list = new TestValidateList();
        Assert.IsFalse(list.IsPaused);

        // Act
        list.FactoryStart(FactoryOperation.Fetch);

        // Assert
        Assert.IsTrue(list.IsPaused);
    }

    [TestMethod]
    public void FactoryComplete_SetsIsPausedFalse()
    {
        // Arrange
        var list = new TestValidateList();
        list.FactoryStart(FactoryOperation.Fetch);
        Assert.IsTrue(list.IsPaused);

        // Act
        list.FactoryComplete(FactoryOperation.Fetch);

        // Assert
        Assert.IsFalse(list.IsPaused);
    }

    #endregion

    #region MetaState Change Notification Tests

    [TestMethod]
    public void IsValid_Change_RaisesPropertyChanged()
    {
        // Arrange
        var list = new TestValidateList();
        var item = new TestValidateItem();
        item.Resume();
        list.Add(item);
        var propertyNames = new List<string>();
        ((INotifyPropertyChanged)list).PropertyChanged += (s, e) => propertyNames.Add(e.PropertyName!);

        // Act
        item.AddValidationError("Error");

        // Assert
        Assert.IsTrue(propertyNames.Contains("IsValid"));
    }

    [TestMethod]
    public void IsValid_Change_RaisesNeatooPropertyChanged()
    {
        // Arrange
        var list = new TestValidateList();
        var item = new TestValidateItem();
        item.Resume();
        list.Add(item);
        var propertyNames = new List<string>();
        list.NeatooPropertyChanged += args =>
        {
            propertyNames.Add(args.PropertyName);
            return Task.CompletedTask;
        };

        // Act
        item.AddValidationError("Error");

        // Assert
        Assert.IsTrue(propertyNames.Contains("IsValid"));
    }

    #endregion

    #region IsBusy Propagation Tests

    [TestMethod]
    public void IsBusy_NoItemsBusy_ReturnsFalse()
    {
        // Arrange
        var list = new TestValidateList();
        list.Add(new TestValidateItem());
        list.Add(new TestValidateItem());

        // Assert
        Assert.IsFalse(list.IsBusy);
    }

    #endregion

    #region Deserialization Tests

    [TestMethod]
    public void OnDeserializing_SetsIsPausedTrue()
    {
        // Arrange
        var list = new TestValidateList();

        // Act
        list.OnDeserializing();

        // Assert
        Assert.IsTrue(list.IsPaused);
    }

    [TestMethod]
    public void OnDeserialized_ResumesAllActions()
    {
        // Arrange
        var list = new TestValidateList();
        list.OnDeserializing();
        Assert.IsTrue(list.IsPaused);

        // Act
        list.OnDeserialized();

        // Assert
        Assert.IsFalse(list.IsPaused);
    }

    #endregion

    #region Interface Implementation Tests

    [TestMethod]
    public void ImplementsIValidateListBaseInterface()
    {
        // Act
        var list = new TestValidateList();

        // Assert
        Assert.IsInstanceOfType(list, typeof(IValidateListBase));
    }

    [TestMethod]
    public void ImplementsIValidateListBaseGenericInterface()
    {
        // Act
        var list = new TestValidateList();

        // Assert
        Assert.IsInstanceOfType(list, typeof(IValidateListBase<TestValidateItem>));
    }

    [TestMethod]
    public void ImplementsIValidateMetaPropertiesInterface()
    {
        // Act
        var list = new TestValidateList();

        // Assert
        Assert.IsInstanceOfType(list, typeof(IValidateMetaProperties));
    }

    #endregion

    #region Multiple Children State Transitions Tests

    [TestMethod]
    public void IsValid_MultipleChildrenTransitions_TracksCorrectly()
    {
        // Arrange - Start with 3 valid items
        var list = new TestValidateList();
        var item1 = new TestValidateItem(); item1.Resume();
        var item2 = new TestValidateItem(); item2.Resume();
        var item3 = new TestValidateItem(); item3.Resume();

        list.Add(item1);
        list.Add(item2);
        list.Add(item3);
        Assert.IsTrue(list.IsValid, "All items valid - list should be valid");

        // Act/Assert - First item becomes invalid
        item1.AddValidationError("Error1");
        Assert.IsFalse(list.IsValid, "One item invalid - list should be invalid");

        // Act/Assert - Second item also becomes invalid
        item2.AddValidationError("Error2");
        Assert.IsFalse(list.IsValid, "Two items invalid - list should be invalid");

        // Act/Assert - First item becomes valid again (but second still invalid)
        item1.ClearErrors();
        Assert.IsFalse(list.IsValid, "One item still invalid - list should be invalid");

        // Act/Assert - Second item becomes valid (all valid now)
        item2.ClearErrors();
        Assert.IsTrue(list.IsValid, "All items valid again - list should be valid");
    }

    [TestMethod]
    public void IsValid_ChildBecomesInvalidThenValidMultipleTimes_TracksCorrectly()
    {
        // Arrange
        var list = new TestValidateList();
        var item = new TestValidateItem();
        item.Resume();
        list.Add(item);

        // Act/Assert - Toggle validity multiple times
        for (int i = 0; i < 5; i++)
        {
            Assert.IsTrue(list.IsValid, $"Iteration {i}: Item valid - list should be valid");

            item.AddValidationError($"Error{i}");
            Assert.IsFalse(list.IsValid, $"Iteration {i}: Item invalid - list should be invalid");

            item.ClearErrors();
        }

        Assert.IsTrue(list.IsValid, "Final state: Item valid - list should be valid");
    }

    [TestMethod]
    public void IsValid_AllChildrenInvalidThenAllBecomeValid_TracksCorrectly()
    {
        // Arrange - Start with 3 items, all invalid
        var list = new TestValidateList();
        var item1 = new TestValidateItem(); item1.Resume();
        var item2 = new TestValidateItem(); item2.Resume();
        var item3 = new TestValidateItem(); item3.Resume();

        item1.AddValidationError("Error1");
        item2.AddValidationError("Error2");
        item3.AddValidationError("Error3");

        list.Add(item1);
        list.Add(item2);
        list.Add(item3);
        Assert.IsFalse(list.IsValid, "All items invalid - list should be invalid");

        // Act/Assert - Fix items one by one
        item1.ClearErrors();
        Assert.IsFalse(list.IsValid, "Two items still invalid - list should be invalid");

        item2.ClearErrors();
        Assert.IsFalse(list.IsValid, "One item still invalid - list should be invalid");

        item3.ClearErrors();
        Assert.IsTrue(list.IsValid, "All items now valid - list should be valid");
    }

    [TestMethod]
    public void PropertyChanged_FiredOncePerTransition_NotMultipleTimes()
    {
        // Arrange
        var list = new TestValidateList();
        var item = new TestValidateItem();
        item.Resume();
        list.Add(item);

        var isValidChangedCount = 0;
        ((INotifyPropertyChanged)list).PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == "IsValid") isValidChangedCount++;
        };

        // Act - Make invalid (should fire once)
        isValidChangedCount = 0;
        item.AddValidationError("Error");
        Assert.AreEqual(1, isValidChangedCount, "IsValid should fire exactly once when becoming invalid");

        // Act - Make valid (should fire once)
        isValidChangedCount = 0;
        item.ClearErrors();
        Assert.AreEqual(1, isValidChangedCount, "IsValid should fire exactly once when becoming valid");
    }

    [TestMethod]
    public void PropertyChanged_NotFiredWhenValidStateUnchanged()
    {
        // Arrange - List with two invalid items
        var list = new TestValidateList();
        var item1 = new TestValidateItem(); item1.Resume();
        var item2 = new TestValidateItem(); item2.Resume();

        item1.AddValidationError("Error1");
        item2.AddValidationError("Error2");
        list.Add(item1);
        list.Add(item2);

        var isValidChangedCount = 0;
        ((INotifyPropertyChanged)list).PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == "IsValid") isValidChangedCount++;
        };

        // Act - Fix one item (list is still invalid because item2 is invalid)
        isValidChangedCount = 0;
        item1.ClearErrors();

        // Assert - IsValid should NOT fire because list was invalid before and is still invalid
        Assert.IsFalse(list.IsValid, "List should still be invalid");
        Assert.AreEqual(0, isValidChangedCount, "IsValid should not fire when state doesn't change");
    }

    #endregion

    #region Edge Cases Tests

    [TestMethod]
    public void Add_ThenClear_ResetsValidState()
    {
        // Arrange
        var list = new TestValidateList();
        var invalidItem = new TestValidateItem();
        invalidItem.Resume();
        invalidItem.AddValidationError("Error");
        list.Add(invalidItem);
        Assert.IsFalse(list.IsValid);

        // Act
        list.Clear();

        // Assert
        Assert.IsTrue(list.IsValid);
    }

    [TestMethod]
    public void PropertyMessages_IsReadOnlyCollection()
    {
        // Arrange
        var list = new TestValidateList();

        // Assert
        Assert.IsInstanceOfType(list.PropertyMessages, typeof(IReadOnlyCollection<IPropertyMessage>));
    }

    #endregion

    #region Caching Edge Cases Tests

    [TestMethod]
    public void SetItem_ReplaceValidWithInvalid_ListBecomesInvalid()
    {
        // Arrange
        var list = new TestValidateList();
        var validItem1 = new TestValidateItem();
        var validItem2 = new TestValidateItem();
        list.Add(validItem1);
        list.Add(validItem2);
        Assert.IsTrue(list.IsValid);

        // Act - Replace first item with an invalid item
        var invalidItem = new TestValidateItem();
        invalidItem.Resume();
        invalidItem.AddValidationError("Error");
        list[0] = invalidItem;

        // Assert
        Assert.IsFalse(list.IsValid);
    }

    [TestMethod]
    public void SetItem_ReplaceInvalidWithValid_WhenOnlyInvalid_ListBecomesValid()
    {
        // Arrange
        var list = new TestValidateList();
        var invalidItem = new TestValidateItem();
        invalidItem.Resume();
        invalidItem.AddValidationError("Error");
        var validItem = new TestValidateItem();
        list.Add(invalidItem);
        list.Add(validItem);
        Assert.IsFalse(list.IsValid);

        // Act - Replace invalid item with a valid one
        var newValidItem = new TestValidateItem();
        list[0] = newValidItem;

        // Assert
        Assert.IsTrue(list.IsValid);
    }

    [TestMethod]
    public void SetItem_ReplaceInvalidWithValid_WhenOthersInvalid_ListStaysInvalid()
    {
        // Arrange
        var list = new TestValidateList();
        var invalidItem1 = new TestValidateItem();
        var invalidItem2 = new TestValidateItem();
        invalidItem1.Resume();
        invalidItem2.Resume();
        invalidItem1.AddValidationError("Error1");
        invalidItem2.AddValidationError("Error2");
        list.Add(invalidItem1);
        list.Add(invalidItem2);
        Assert.IsFalse(list.IsValid);

        // Act - Replace first invalid item with a valid one (second still invalid)
        var newValidItem = new TestValidateItem();
        list[0] = newValidItem;

        // Assert
        Assert.IsFalse(list.IsValid);
    }

    [TestMethod]
    public void SetItem_ReplaceValidWithValid_ListStaysValid()
    {
        // Arrange
        var list = new TestValidateList();
        var validItem = new TestValidateItem();
        list.Add(validItem);
        Assert.IsTrue(list.IsValid);

        // Act
        var newValidItem = new TestValidateItem();
        list[0] = newValidItem;

        // Assert
        Assert.IsTrue(list.IsValid);
    }

    [TestMethod]
    public void PauseThenResume_WithInvalidItems_CacheRecalculatedOnResume()
    {
        // Arrange - Create list, pause, add items directly (simulating deserialization)
        var list = new TestValidateList();
        list.IsPaused = true;

        var invalidItem = new TestValidateItem();
        invalidItem.Resume();
        invalidItem.AddValidationError("Error");
        list.Add(invalidItem);

        // While paused, cache is not updated, but after resume it should be correct
        list.ResumeAllActions();

        // Assert
        Assert.IsFalse(list.IsValid);
    }

    [TestMethod]
    public void RemoveMultipleInvalidItems_LastRemovalMakesValid()
    {
        // Arrange
        var list = new TestValidateList();
        var invalid1 = new TestValidateItem();
        var invalid2 = new TestValidateItem();
        invalid1.Resume();
        invalid2.Resume();
        invalid1.AddValidationError("Error1");
        invalid2.AddValidationError("Error2");
        list.Add(invalid1);
        list.Add(invalid2);
        Assert.IsFalse(list.IsValid);

        // Act - Remove first invalid
        list.Remove(invalid1);
        Assert.IsFalse(list.IsValid, "Still invalid with one invalid item");

        // Act - Remove second invalid
        list.Remove(invalid2);

        // Assert
        Assert.IsTrue(list.IsValid);
    }

    #endregion

    #region Large List Performance Tests

    [TestMethod]
    public void LargeList_AddManyItems_IsValidRemainsCorrect()
    {
        // Arrange
        var list = new TestValidateList();
        const int itemCount = 1000;

        // Act - Add 1000 valid items
        for (int i = 0; i < itemCount; i++)
        {
            var item = new TestValidateItem();
            list.Add(item);
        }

        // Assert
        Assert.AreEqual(itemCount, list.Count);
        Assert.IsTrue(list.IsValid);
    }

    [TestMethod]
    public void LargeList_OneInvalidAmongMany_IsValidFalse()
    {
        // Arrange
        var list = new TestValidateList();
        const int itemCount = 1000;

        for (int i = 0; i < itemCount; i++)
        {
            var item = new TestValidateItem();
            list.Add(item);
        }
        Assert.IsTrue(list.IsValid);

        // Act - Make one item in the middle invalid
        var middleItem = list[500];
        middleItem.Resume();
        middleItem.AddValidationError("Error");

        // Assert
        Assert.IsFalse(list.IsValid);
    }

    [TestMethod]
    public void LargeList_MakeInvalidThenValid_TracksCorrectly()
    {
        // Arrange
        var list = new TestValidateList();
        const int itemCount = 1000;

        for (int i = 0; i < itemCount; i++)
        {
            var item = new TestValidateItem();
            item.Resume();
            list.Add(item);
        }

        // Act/Assert - Make last item invalid
        var lastItem = list[999];
        lastItem.AddValidationError("Error");
        Assert.IsFalse(list.IsValid);

        // Act/Assert - Make it valid again
        lastItem.ClearErrors();
        Assert.IsTrue(list.IsValid);
    }

    [TestMethod]
    public void LargeList_MultipleInvalidItems_FixOneByOne()
    {
        // Arrange
        var list = new TestValidateList();
        const int itemCount = 1000;
        const int invalidCount = 100;

        for (int i = 0; i < itemCount; i++)
        {
            var item = new TestValidateItem();
            item.Resume();
            list.Add(item);
        }

        // Make first 100 items invalid
        for (int i = 0; i < invalidCount; i++)
        {
            list[i].AddValidationError($"Error{i}");
        }
        Assert.IsFalse(list.IsValid);

        // Act - Fix all but last invalid item
        for (int i = 0; i < invalidCount - 1; i++)
        {
            list[i].ClearErrors();
            Assert.IsFalse(list.IsValid, $"Should still be invalid after fixing item {i}");
        }

        // Fix last invalid item
        list[invalidCount - 1].ClearErrors();

        // Assert
        Assert.IsTrue(list.IsValid);
    }

    [TestMethod]
    public void LargeList_RapidStateChanges_CacheStaysConsistent()
    {
        // Arrange
        var list = new TestValidateList();
        const int itemCount = 500;

        for (int i = 0; i < itemCount; i++)
        {
            var item = new TestValidateItem();
            item.Resume();
            list.Add(item);
        }

        // Act - Rapidly toggle validity on multiple items
        for (int round = 0; round < 10; round++)
        {
            // Make items 0-99 invalid
            for (int i = 0; i < 100; i++)
            {
                list[i].AddValidationError($"Error{round}");
            }
            Assert.IsFalse(list.IsValid, $"Round {round}: Should be invalid after adding errors");

            // Make them valid again
            for (int i = 0; i < 100; i++)
            {
                list[i].ClearErrors();
            }
            Assert.IsTrue(list.IsValid, $"Round {round}: Should be valid after clearing errors");
        }
    }

    [TestMethod]
    public void LargeList_RemoveItems_IsValidUpdatesCorrectly()
    {
        // Arrange
        var list = new TestValidateList();
        const int itemCount = 500;

        for (int i = 0; i < itemCount; i++)
        {
            var item = new TestValidateItem();
            item.Resume();
            list.Add(item);
        }

        // Make items at positions 100, 200, 300 invalid
        list[100].AddValidationError("Error100");
        list[200].AddValidationError("Error200");
        list[300].AddValidationError("Error300");
        Assert.IsFalse(list.IsValid);

        // Act - Remove invalid items (remove from end to preserve indices)
        list.RemoveAt(300);
        Assert.IsFalse(list.IsValid, "Still invalid with 2 invalid items");

        list.RemoveAt(200);
        Assert.IsFalse(list.IsValid, "Still invalid with 1 invalid item");

        list.RemoveAt(100);

        // Assert
        Assert.IsTrue(list.IsValid);
    }

    [TestMethod]
    public void LargeList_ClearList_ResetsToValid()
    {
        // Arrange
        var list = new TestValidateList();
        const int itemCount = 1000;

        for (int i = 0; i < itemCount; i++)
        {
            var item = new TestValidateItem();
            item.Resume();
            if (i % 10 == 0) // Every 10th item is invalid
            {
                item.AddValidationError($"Error{i}");
            }
            list.Add(item);
        }
        Assert.IsFalse(list.IsValid);

        // Act
        list.Clear();

        // Assert
        Assert.IsTrue(list.IsValid);
        Assert.AreEqual(0, list.Count);
    }

    [TestMethod]
    public void LargeList_SetItem_UpdatesCacheCorrectly()
    {
        // Arrange
        var list = new TestValidateList();
        const int itemCount = 500;

        for (int i = 0; i < itemCount; i++)
        {
            var item = new TestValidateItem();
            list.Add(item);
        }
        Assert.IsTrue(list.IsValid);

        // Act - Replace item at position 250 with invalid item
        var invalidItem = new TestValidateItem();
        invalidItem.Resume();
        invalidItem.AddValidationError("Error");
        list[250] = invalidItem;

        // Assert
        Assert.IsFalse(list.IsValid);

        // Act - Replace with valid item
        var validItem = new TestValidateItem();
        list[250] = validItem;

        // Assert
        Assert.IsTrue(list.IsValid);
    }

    #endregion
}
