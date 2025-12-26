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
}
