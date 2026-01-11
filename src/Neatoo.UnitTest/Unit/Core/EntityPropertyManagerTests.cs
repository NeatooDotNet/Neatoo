using System.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo;
using Neatoo.Internal;
using Neatoo.RemoteFactory;
using Neatoo.Rules;

namespace Neatoo.UnitTest.Unit.Core;

/// <summary>
/// Unit tests for the EntityPropertyManager class.
/// Tests modification tracking, pause/resume functionality, property management,
/// and inherited ValidatePropertyManager behavior.
/// Uses real Neatoo classes (EntityBase, EntityPropertyManager) instead of mocks.
/// </summary>
[TestClass]
public class EntityPropertyManagerTests
{
    #region Test Entity Classes

    /// <summary>
    /// Simple test entity with basic properties for testing EntityPropertyManager behavior.
    /// Uses SuppressFactory to avoid requiring the full factory infrastructure.
    /// </summary>
    [SuppressFactory]
    private class TestEntityObject : EntityBase<TestEntityObject>
    {
        public TestEntityObject() : base(new EntityBaseServices<TestEntityObject>(null))
        {
            PauseAllActions();
        }

        public string? Name { get => Getter<string>(); set => Setter(value); }
        public int Age { get => Getter<int>(); set => Setter(value); }
        public decimal Amount { get => Getter<decimal>(); set => Setter(value); }
        public DateTime DateOfBirth { get => Getter<DateTime>(); set => Setter(value); }
        public Guid Id { get => Getter<Guid>(); set => Setter(value); }
        public int? NullableNumber { get => Getter<int?>(); set => Setter(value); }
        public List<string>? Items { get => Getter<List<string>>(); set => Setter(value); }

        [DisplayName("Full Name")]
        public string? DisplayNameProperty { get => Getter<string>(); set => Setter(value); }

        /// <summary>
        /// Exposes the internal PropertyManager for direct testing access.
        /// </summary>
        public new IEntityPropertyManager PropertyManager => base.PropertyManager;
    }

    /// <summary>
    /// Test entity with a child entity property for testing nested modification tracking.
    /// </summary>
    [SuppressFactory]
    private class TestParentEntity : EntityBase<TestParentEntity>
    {
        public TestParentEntity() : base(new EntityBaseServices<TestParentEntity>(null))
        {
            PauseAllActions();
        }

        public string? ParentName { get => Getter<string>(); set => Setter(value); }
        public TestChildEntity? Child { get => Getter<TestChildEntity>(); set => Setter(value); }

        public new IEntityPropertyManager PropertyManager => base.PropertyManager;
    }

    /// <summary>
    /// Child entity for testing nested entity relationships.
    /// </summary>
    [SuppressFactory]
    private class TestChildEntity : EntityBase<TestChildEntity>
    {
        public TestChildEntity() : base(new EntityBaseServices<TestChildEntity>(null))
        {
            PauseAllActions();
        }

        public string? ChildName { get => Getter<string>(); set => Setter(value); }
        public int ChildValue { get => Getter<int>(); set => Setter(value); }

        public new IEntityPropertyManager PropertyManager => base.PropertyManager;
    }

    #endregion

    #region IsModified Tests

    [TestMethod]
    public void IsModified_InitialState_ReturnsFalse()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();

        // Act & Assert
        Assert.IsFalse(entity.PropertyManager.IsModified);
    }

    [TestMethod]
    public void IsModified_AfterPropertyChange_ReturnsTrue()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();

        // Act
        entity.Name = "Test";

        // Assert
        Assert.IsTrue(entity.PropertyManager.IsModified);
    }

    [TestMethod]
    public void IsModified_MultiplePropertyChanges_ReturnsTrue()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();

        // Act
        entity.Name = "Test";
        entity.Age = 25;
        entity.Amount = 100.50m;

        // Assert
        Assert.IsTrue(entity.PropertyManager.IsModified);
    }

    [TestMethod]
    public void IsModified_AfterMarkSelfUnmodified_ReturnsFalse()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();
        entity.Name = "Test";
        Assert.IsTrue(entity.PropertyManager.IsModified);

        // Act
        entity.PropertyManager.MarkSelfUnmodified();

        // Assert
        Assert.IsFalse(entity.PropertyManager.IsModified);
    }

    [TestMethod]
    public void IsModified_WithModifiedEntityChild_ReturnsTrue()
    {
        // Arrange
        var parent = new TestParentEntity();
        var child = new TestChildEntity();
        parent.ResumeAllActions();
        parent.Child = child;
        parent.PropertyManager.MarkSelfUnmodified();
        Assert.IsFalse(parent.PropertyManager.IsModified);

        // Act - Modify the child
        child.ResumeAllActions();
        child.ChildName = "Modified Child";

        // Assert
        Assert.IsTrue(parent.PropertyManager.IsModified);
    }

    [TestMethod]
    public void IsModified_WithUnmodifiedEntityChild_ReturnsFalse()
    {
        // Arrange
        var parent = new TestParentEntity();
        var child = new TestChildEntity();
        parent.ResumeAllActions();
        parent.Child = child;
        parent.PropertyManager.MarkSelfUnmodified();

        // Act & Assert - Child is not modified, so parent should not be modified
        Assert.IsFalse(child.IsModified);
        Assert.IsFalse(parent.PropertyManager.IsModified);
    }

    #endregion

    #region IsSelfModified Tests

    [TestMethod]
    public void IsSelfModified_InitialState_ReturnsFalse()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();

        // Act & Assert
        Assert.IsFalse(entity.PropertyManager.IsSelfModified);
    }

    [TestMethod]
    public void IsSelfModified_AfterPropertyChange_ReturnsTrue()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();

        // Act
        entity.Name = "Test";

        // Assert
        Assert.IsTrue(entity.PropertyManager.IsSelfModified);
    }

    [TestMethod]
    public void IsSelfModified_AfterMarkSelfUnmodified_ReturnsFalse()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();
        entity.Name = "Test";
        Assert.IsTrue(entity.PropertyManager.IsSelfModified);

        // Act
        entity.PropertyManager.MarkSelfUnmodified();

        // Assert
        Assert.IsFalse(entity.PropertyManager.IsSelfModified);
    }

    [TestMethod]
    public void IsSelfModified_WithModifiedEntityChild_ReturnsFalse()
    {
        // Arrange - IsSelfModified should NOT include child modifications
        var parent = new TestParentEntity();
        var child = new TestChildEntity();
        parent.ResumeAllActions();
        parent.Child = child;
        parent.PropertyManager.MarkSelfUnmodified();

        // Act - Modify the child
        child.ResumeAllActions();
        child.ChildName = "Modified Child";

        // Assert - Parent's IsSelfModified should be false (only child is modified)
        Assert.IsFalse(parent.PropertyManager.IsSelfModified);
        Assert.IsTrue(parent.PropertyManager.IsModified); // But IsModified should be true
    }

    [TestMethod]
    public void IsSelfModified_SettingEntityChildProperty_ReturnsFalse()
    {
        // Arrange - Setting an entity child property should NOT mark self as modified
        var parent = new TestParentEntity();
        var child = new TestChildEntity();
        parent.ResumeAllActions();

        // Act
        parent.Child = child;

        // Assert - IsSelfModified should be false for entity child properties
        Assert.IsFalse(parent.PropertyManager.IsSelfModified);
    }

    #endregion

    #region MarkSelfUnmodified Tests

    [TestMethod]
    public void MarkSelfUnmodified_ClearsAllPropertyModificationFlags()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();
        entity.Name = "Test";
        entity.Age = 25;
        entity.Amount = 100.50m;
        Assert.IsTrue(entity.PropertyManager.IsSelfModified);

        // Act
        entity.PropertyManager.MarkSelfUnmodified();

        // Assert
        Assert.IsFalse(entity.PropertyManager.IsSelfModified);
        Assert.IsFalse(entity.PropertyManager.IsModified);
    }

    [TestMethod]
    public void MarkSelfUnmodified_DoesNotAffectEntityChildModification()
    {
        // Arrange
        var parent = new TestParentEntity();
        var child = new TestChildEntity();
        parent.ResumeAllActions();
        parent.Child = child;

        child.ResumeAllActions();
        child.ChildName = "Modified Child";
        Assert.IsTrue(child.IsModified);

        // Act
        parent.PropertyManager.MarkSelfUnmodified();

        // Assert - Child should still be modified
        Assert.IsTrue(child.IsModified);
        Assert.IsTrue(parent.PropertyManager.IsModified); // IsModified includes child
    }

    [TestMethod]
    public void MarkSelfUnmodified_CanBeModifiedAgain()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();
        entity.Name = "First";
        entity.PropertyManager.MarkSelfUnmodified();
        Assert.IsFalse(entity.PropertyManager.IsSelfModified);

        // Act
        entity.Name = "Second";

        // Assert
        Assert.IsTrue(entity.PropertyManager.IsSelfModified);
    }

    [TestMethod]
    public void MarkSelfUnmodified_WhenNotModified_DoesNotThrow()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();
        Assert.IsFalse(entity.PropertyManager.IsSelfModified);

        // Act & Assert - Should not throw
        entity.PropertyManager.MarkSelfUnmodified();
        Assert.IsFalse(entity.PropertyManager.IsSelfModified);
    }

    #endregion

    #region ModifiedProperties Tests

    [TestMethod]
    public void ModifiedProperties_InitialState_ReturnsEmpty()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();

        // Act
        var modifiedProperties = entity.PropertyManager.ModifiedProperties.ToList();

        // Assert
        Assert.AreEqual(0, modifiedProperties.Count);
    }

    [TestMethod]
    public void ModifiedProperties_AfterSinglePropertyChange_ReturnsThatProperty()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();

        // Act
        entity.Name = "Test";
        var modifiedProperties = entity.PropertyManager.ModifiedProperties.ToList();

        // Assert
        Assert.AreEqual(1, modifiedProperties.Count);
        Assert.IsTrue(modifiedProperties.Contains("Name"));
    }

    [TestMethod]
    public void ModifiedProperties_AfterMultiplePropertyChanges_ReturnsAllModified()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();

        // Act
        entity.Name = "Test";
        entity.Age = 25;
        entity.Amount = 100.50m;
        var modifiedProperties = entity.PropertyManager.ModifiedProperties.ToList();

        // Assert
        Assert.AreEqual(3, modifiedProperties.Count);
        Assert.IsTrue(modifiedProperties.Contains("Name"));
        Assert.IsTrue(modifiedProperties.Contains("Age"));
        Assert.IsTrue(modifiedProperties.Contains("Amount"));
    }

    [TestMethod]
    public void ModifiedProperties_AfterMarkSelfUnmodified_ReturnsEmpty()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();
        entity.Name = "Test";
        entity.Age = 25;

        // Act
        entity.PropertyManager.MarkSelfUnmodified();
        var modifiedProperties = entity.PropertyManager.ModifiedProperties.ToList();

        // Assert
        Assert.AreEqual(0, modifiedProperties.Count);
    }

    [TestMethod]
    public void ModifiedProperties_WithModifiedEntityChild_IncludesChildProperty()
    {
        // Arrange
        var parent = new TestParentEntity();
        var child = new TestChildEntity();
        parent.ResumeAllActions();
        parent.Child = child;
        parent.PropertyManager.MarkSelfUnmodified();

        // Act - Modify the child
        child.ResumeAllActions();
        child.ChildName = "Modified Child";
        var modifiedProperties = parent.PropertyManager.ModifiedProperties.ToList();

        // Assert - Child property should be listed as modified
        Assert.IsTrue(modifiedProperties.Contains("Child"));
    }

    #endregion

    #region IsPaused and PauseAllActions Tests

    [TestMethod]
    public void IsPaused_InitialState_ReturnsFalse()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();

        // Act & Assert
        Assert.IsFalse(entity.IsPaused);
    }

    [TestMethod]
    public void PauseAllActions_SetsIsPausedTrue()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();

        // Act
        entity.PauseAllActions();

        // Assert
        Assert.IsTrue(entity.IsPaused);
    }

    [TestMethod]
    public void PauseAllActions_PropertyChangesDoNotSetModified()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();
        entity.PauseAllActions();

        // Act
        entity.Name = "Test";

        // Assert
        Assert.IsFalse(entity.PropertyManager.IsSelfModified);
    }

    [TestMethod]
    public void PauseAllActions_ExistingModificationsArePreserved()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();
        entity.Name = "First";
        Assert.IsTrue(entity.PropertyManager.IsSelfModified);

        // Act
        entity.PauseAllActions();

        // Assert - Modification state should be preserved
        Assert.IsTrue(entity.PropertyManager.IsSelfModified);
    }

    [TestMethod]
    public void ResumeAllActions_AfterPause_SetsIsPausedFalse()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();
        entity.PauseAllActions();
        Assert.IsTrue(entity.IsPaused);

        // Act
        entity.ResumeAllActions();

        // Assert
        Assert.IsFalse(entity.IsPaused);
    }

    [TestMethod]
    public void ResumeAllActions_PropertyChangesThenSetModified()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();
        entity.PauseAllActions();
        entity.Name = "Test1"; // Not tracked
        Assert.IsFalse(entity.PropertyManager.IsSelfModified);

        // Act
        entity.ResumeAllActions();
        entity.Name = "Test2"; // Now should be tracked

        // Assert
        Assert.IsTrue(entity.PropertyManager.IsSelfModified);
    }

    [TestMethod]
    public void PauseAllActions_ThenResume_SequenceWorks()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();

        // Act - Multiple pause/resume cycles
        entity.PauseAllActions();
        entity.Name = "Value1"; // Not tracked
        Assert.IsFalse(entity.PropertyManager.IsSelfModified);

        entity.ResumeAllActions();
        entity.Name = "Value2"; // Tracked
        Assert.IsTrue(entity.PropertyManager.IsSelfModified);

        entity.PropertyManager.MarkSelfUnmodified();
        Assert.IsFalse(entity.PropertyManager.IsSelfModified);

        entity.PauseAllActions();
        entity.Name = "Value3"; // Not tracked
        Assert.IsFalse(entity.PropertyManager.IsSelfModified);

        entity.ResumeAllActions();
        entity.Name = "Value4"; // Tracked
        Assert.IsTrue(entity.PropertyManager.IsSelfModified);
    }

    #endregion

    #region LoadProperty Tests

    [TestMethod]
    public void LoadProperty_DoesNotSetModified()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();

        // Act - Use LoadProperty through the property's LoadValue method
        entity["Name"].LoadValue("Loaded Value");

        // Assert
        Assert.AreEqual("Loaded Value", entity.Name);
        Assert.IsFalse(entity.PropertyManager.IsSelfModified);
    }

    [TestMethod]
    public void LoadProperty_MultipleTimes_DoesNotSetModified()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();

        // Act
        entity["Name"].LoadValue("Value1");
        entity["Age"].LoadValue(25);
        entity["Amount"].LoadValue(100.50m);

        // Assert
        Assert.AreEqual("Value1", entity.Name);
        Assert.AreEqual(25, entity.Age);
        Assert.AreEqual(100.50m, entity.Amount);
        Assert.IsFalse(entity.PropertyManager.IsSelfModified);
    }

    [TestMethod]
    public void LoadProperty_ThenModify_SetsModified()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();
        entity["Name"].LoadValue("Loaded Value");
        Assert.IsFalse(entity.PropertyManager.IsSelfModified);

        // Act
        entity.Name = "Modified Value";

        // Assert
        Assert.IsTrue(entity.PropertyManager.IsSelfModified);
    }

    [TestMethod]
    public void LoadProperty_ResetsModificationState()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();
        entity.Name = "First";
        Assert.IsTrue(entity.PropertyManager.IsSelfModified);

        // Act - Loading resets the property's modified state
        entity["Name"].LoadValue("Loaded");

        // Assert
        Assert.IsFalse(entity["Name"].IsSelfModified);
    }

    #endregion

    #region DisplayName Tests

    [TestMethod]
    public void DisplayName_PropertyWithDisplayNameAttribute_ReturnsAttributeValue()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();

        // Act
        var displayName = entity["DisplayNameProperty"].DisplayName;

        // Assert
        Assert.AreEqual("Full Name", displayName);
    }

    [TestMethod]
    public void DisplayName_PropertyWithoutDisplayNameAttribute_ReturnsPropertyName()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();

        // Act
        var displayName = entity["Name"].DisplayName;

        // Assert
        Assert.AreEqual("Name", displayName);
    }

    [TestMethod]
    public void DisplayName_DifferentProperties_ReturnCorrectDisplayNames()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();

        // Assert
        Assert.AreEqual("Name", entity["Name"].DisplayName);
        Assert.AreEqual("Age", entity["Age"].DisplayName);
        Assert.AreEqual("Amount", entity["Amount"].DisplayName);
        Assert.AreEqual("Full Name", entity["DisplayNameProperty"].DisplayName);
    }

    #endregion

    #region EntityChild Property Tracking Tests

    [TestMethod]
    public void EntityChild_SettingChild_DoesNotMarkSelfModified()
    {
        // Arrange
        var parent = new TestParentEntity();
        var child = new TestChildEntity();
        parent.ResumeAllActions();

        // Act
        parent.Child = child;

        // Assert - Setting an entity child should NOT mark self as modified
        Assert.IsFalse(parent.PropertyManager.IsSelfModified);
    }

    [TestMethod]
    public void EntityChild_SettingChildThenModifyingChild_ParentIsModified()
    {
        // Arrange
        var parent = new TestParentEntity();
        var child = new TestChildEntity();
        parent.ResumeAllActions();
        parent.Child = child;

        // Act
        child.ResumeAllActions();
        child.ChildName = "Modified";

        // Assert
        Assert.IsTrue(parent.IsModified);
        Assert.IsFalse(parent.PropertyManager.IsSelfModified);
    }

    [TestMethod]
    public void EntityChild_ReplacingChild_UpdatesModificationTracking()
    {
        // Arrange
        var parent = new TestParentEntity();
        var child1 = new TestChildEntity();
        var child2 = new TestChildEntity();

        parent.ResumeAllActions();
        parent.Child = child1;
        child1.ResumeAllActions();
        child1.ChildName = "Modified";
        Assert.IsTrue(parent.IsModified);

        parent.PropertyManager.MarkSelfUnmodified();

        // Act - Replace with unmodified child
        parent.Child = child2;

        // Assert - With unmodified child2, parent should reflect child2's state
        Assert.IsFalse(child2.IsModified);
    }

    [TestMethod]
    public void EntityChild_SettingToNull_MarksAsModified()
    {
        // Arrange
        var parent = new TestParentEntity();
        var child = new TestChildEntity();
        parent.ResumeAllActions();
        parent.Child = child;
        parent.PropertyManager.MarkSelfUnmodified();

        // Act
        parent.Child = null;

        // Assert - Setting to null IS tracked as a modification because null is not an IEntityMetaProperties
        // The EntityProperty logic: IsSelfModified = true && EntityChild == null
        // When value is null, EntityChild is null, so IsSelfModified becomes true
        Assert.IsTrue(parent.PropertyManager.IsSelfModified);
    }

    [TestMethod]
    public void EntityChild_Property_ReturnsCorrectChild()
    {
        // Arrange
        var parent = new TestParentEntity();
        var child = new TestChildEntity();
        parent.ResumeAllActions();
        parent.Child = child;

        // Act - Access EntityChild through cast to EntityProperty<T>
        var property = parent["Child"];
        var entityProperty = property as EntityProperty<TestChildEntity>;

        // Assert
        Assert.IsNotNull(entityProperty);
        Assert.IsNotNull(entityProperty.EntityChild);
        Assert.AreSame(child, entityProperty.EntityChild);
    }

    [TestMethod]
    public void EntityChild_Property_ReturnsNullForNonEntityChild()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();
        entity.Name = "Test";

        // Act - Access EntityChild through cast to EntityProperty<T>
        var property = entity["Name"];
        var entityProperty = property as EntityProperty<string>;

        // Assert
        Assert.IsNotNull(entityProperty);
        Assert.IsNull(entityProperty.EntityChild);
    }

    #endregion

    #region Inherited ValidatePropertyManager Behavior Tests

    [TestMethod]
    public void IsValid_InitialState_ReturnsTrue()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();

        // Act & Assert
        Assert.IsTrue(entity.PropertyManager.IsValid);
    }

    [TestMethod]
    public void IsSelfValid_InitialState_ReturnsTrue()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();

        // Act & Assert
        Assert.IsTrue(entity.PropertyManager.IsSelfValid);
    }

    [TestMethod]
    public void PropertyMessages_InitialState_ReturnsEmpty()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();

        // Act
        var messages = entity.PropertyManager.PropertyMessages;

        // Assert
        Assert.IsNotNull(messages);
        Assert.AreEqual(0, messages.Count);
    }

    [TestMethod]
    public void IsBusy_InitialState_ReturnsFalse()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();

        // Act & Assert
        Assert.IsFalse(entity.PropertyManager.IsBusy);
    }

    [TestMethod]
    public void HasProperty_ExistingProperty_ReturnsTrue()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();

        // Act & Assert
        Assert.IsTrue(entity.PropertyManager.HasProperty("Name"));
        Assert.IsTrue(entity.PropertyManager.HasProperty("Age"));
        Assert.IsTrue(entity.PropertyManager.HasProperty("Amount"));
    }

    [TestMethod]
    public void HasProperty_NonExistingProperty_ReturnsFalse()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();

        // Act & Assert
        Assert.IsFalse(entity.PropertyManager.HasProperty("NonExistentProperty"));
    }

    [TestMethod]
    public void ClearSelfMessages_ClearsAllRuleMessages()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();

        // Add a validation error
        var property = entity["Name"];
        var ruleMessages = new List<IRuleMessage>
        {
            new RuleMessage("Name", "Test error") { RuleId = 1 }
        };
        ((IValidatePropertyInternal)property).SetMessagesForRule(ruleMessages);
        Assert.IsFalse(property.IsValid);

        // Act
        entity.PropertyManager.ClearSelfMessages();

        // Assert
        Assert.IsTrue(property.IsValid);
    }

    [TestMethod]
    public void ClearAllMessages_ClearsAllRuleMessages()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();

        // Add validation errors to multiple properties
        var nameProperty = entity["Name"];
        var ageProperty = entity["Age"];

        ((IValidatePropertyInternal)nameProperty).SetMessagesForRule(new List<IRuleMessage>
        {
            new RuleMessage("Name", "Name error") { RuleId = 1 }
        });
        ((IValidatePropertyInternal)ageProperty).SetMessagesForRule(new List<IRuleMessage>
        {
            new RuleMessage("Age", "Age error") { RuleId = 2 }
        });

        Assert.IsFalse(entity.PropertyManager.IsValid);

        // Act
        entity.PropertyManager.ClearAllMessages();

        // Assert
        Assert.IsTrue(entity.PropertyManager.IsValid);
    }

    #endregion

    #region Property Indexer Tests

    [TestMethod]
    public void Indexer_ExistingProperty_ReturnsProperty()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();

        // Act
        var property = entity["Name"];

        // Assert
        Assert.IsNotNull(property);
        Assert.AreEqual("Name", property.Name);
    }

    [TestMethod]
    public void Indexer_SamePropertyMultipleTimes_ReturnsSameInstance()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();

        // Act
        var property1 = entity["Name"];
        var property2 = entity["Name"];

        // Assert
        Assert.AreSame(property1, property2);
    }

    [TestMethod]
    public void Indexer_DifferentProperties_ReturnsDifferentInstances()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();

        // Act
        var nameProperty = entity["Name"];
        var ageProperty = entity["Age"];

        // Assert
        Assert.AreNotSame(nameProperty, ageProperty);
    }

    #endregion

    #region Value Type Property Tests

    [TestMethod]
    public void ValueTypeProperty_Int_WorksCorrectly()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();

        // Act
        entity.Age = 42;

        // Assert
        Assert.AreEqual(42, entity.Age);
        Assert.IsTrue(entity.PropertyManager.IsSelfModified);
    }

    [TestMethod]
    public void ValueTypeProperty_Decimal_WorksCorrectly()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();

        // Act
        entity.Amount = 123.45m;

        // Assert
        Assert.AreEqual(123.45m, entity.Amount);
        Assert.IsTrue(entity.PropertyManager.IsSelfModified);
    }

    [TestMethod]
    public void ValueTypeProperty_DateTime_WorksCorrectly()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();
        var expectedDate = new DateTime(2024, 1, 1, 12, 0, 0);

        // Act
        entity.DateOfBirth = expectedDate;

        // Assert
        Assert.AreEqual(expectedDate, entity.DateOfBirth);
        Assert.IsTrue(entity.PropertyManager.IsSelfModified);
    }

    [TestMethod]
    public void ValueTypeProperty_Guid_WorksCorrectly()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();
        var expectedGuid = Guid.NewGuid();

        // Act
        entity.Id = expectedGuid;

        // Assert
        Assert.AreEqual(expectedGuid, entity.Id);
        Assert.IsTrue(entity.PropertyManager.IsSelfModified);
    }

    [TestMethod]
    public void ValueTypeProperty_NullableInt_HandlesNullCorrectly()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();

        // Act & Assert
        Assert.IsNull(entity.NullableNumber);

        entity.NullableNumber = 42;
        Assert.AreEqual(42, entity.NullableNumber);
        Assert.IsTrue(entity.PropertyManager.IsSelfModified);

        entity.PropertyManager.MarkSelfUnmodified();
        entity.NullableNumber = null;
        Assert.IsNull(entity.NullableNumber);
        Assert.IsTrue(entity.PropertyManager.IsSelfModified);
    }

    [TestMethod]
    public void ReferenceTypeProperty_List_WorksCorrectly()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();
        var list = new List<string> { "item1", "item2" };

        // Act
        entity.Items = list;

        // Assert
        Assert.AreSame(list, entity.Items);
        Assert.AreEqual(2, entity.Items?.Count);
        Assert.IsTrue(entity.PropertyManager.IsSelfModified);
    }

    #endregion

    #region Complex Scenario Tests

    [TestMethod]
    public void Scenario_ModifyMarkUnmodifiedModifyAgain_TracksCorrectly()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();

        // Act & Assert - Multiple modification cycles
        entity.Name = "First";
        Assert.IsTrue(entity.PropertyManager.IsSelfModified);

        entity.PropertyManager.MarkSelfUnmodified();
        Assert.IsFalse(entity.PropertyManager.IsSelfModified);

        entity.Name = "Second";
        Assert.IsTrue(entity.PropertyManager.IsSelfModified);

        entity.PropertyManager.MarkSelfUnmodified();
        Assert.IsFalse(entity.PropertyManager.IsSelfModified);

        entity.Name = "Third";
        Assert.IsTrue(entity.PropertyManager.IsSelfModified);
    }

    [TestMethod]
    public void Scenario_PauseModifyResume_TracksCorrectly()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();

        // Act
        entity.PauseAllActions();
        entity.Name = "Paused Value"; // Should not be tracked
        Assert.IsFalse(entity.PropertyManager.IsSelfModified);

        entity.ResumeAllActions();
        Assert.IsFalse(entity.PropertyManager.IsSelfModified); // Still not modified

        entity.Name = "Resumed Value"; // Should be tracked
        Assert.IsTrue(entity.PropertyManager.IsSelfModified);
    }

    [TestMethod]
    public void Scenario_ParentWithMultipleChildren_TracksCorrectly()
    {
        // This scenario tests a parent entity with a single child property
        // Arrange
        var parent = new TestParentEntity();
        var child1 = new TestChildEntity();
        var child2 = new TestChildEntity();

        parent.ResumeAllActions();
        parent.Child = child1;
        parent.PropertyManager.MarkSelfUnmodified();

        // Modify first child
        child1.ResumeAllActions();
        child1.ChildName = "Child1 Modified";
        Assert.IsTrue(parent.IsModified);

        // Replace with second child (unmodified)
        parent.Child = child2;
        Assert.IsFalse(child2.IsModified);

        // Modify second child
        child2.ResumeAllActions();
        child2.ChildName = "Child2 Modified";
        Assert.IsTrue(parent.IsModified);
    }

    [TestMethod]
    public void Scenario_LoadThenModifyThenMarkUnmodified_WorksCorrectly()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();

        // Load values (should not be modified)
        entity["Name"].LoadValue("Loaded Name");
        entity["Age"].LoadValue(30);
        Assert.IsFalse(entity.PropertyManager.IsSelfModified);

        // Modify one property
        entity.Name = "Modified Name";
        Assert.IsTrue(entity.PropertyManager.IsSelfModified);
        Assert.IsTrue(entity.PropertyManager.ModifiedProperties.Contains("Name"));
        Assert.IsFalse(entity.PropertyManager.ModifiedProperties.Contains("Age"));

        // Mark unmodified
        entity.PropertyManager.MarkSelfUnmodified();
        Assert.IsFalse(entity.PropertyManager.IsSelfModified);
        Assert.AreEqual(0, entity.PropertyManager.ModifiedProperties.Count());
    }

    [TestMethod]
    public async Task Scenario_ConcurrentPropertyChanges_HandlesCorrectly()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();
        var tasks = new List<Task>();

        // Act - Simulate concurrent property changes
        for (int i = 0; i < 50; i++)
        {
            var value = i;
            tasks.Add(Task.Run(() => entity.Age = value));
        }

        await Task.WhenAll(tasks);

        // Assert - Should have a valid state and be modified
        Assert.IsTrue(entity.PropertyManager.IsSelfModified);
        Assert.IsTrue(entity.Age >= 0 && entity.Age < 50);
    }

    #endregion

    #region Interface Implementation Tests

    [TestMethod]
    public void ImplementsIEntityPropertyManagerInterface()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();

        // Act & Assert
        Assert.IsInstanceOfType(entity.PropertyManager, typeof(IEntityPropertyManager));
    }

    [TestMethod]
    public void ImplementsIValidatePropertyManagerInterface()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();

        // Act & Assert
        Assert.IsInstanceOfType(entity.PropertyManager, typeof(IValidatePropertyManager<IEntityProperty>));
    }

    #endregion

    #region PropertyChanged Event Tests

    [TestMethod]
    public void PropertyChanged_OnValueChange_RaisesEvent()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();
        var changedProperties = new List<string>();
        entity.PropertyChanged += (sender, e) => changedProperties.Add(e.PropertyName!);

        // Act
        entity.Name = "NewValue";

        // Assert
        Assert.IsTrue(changedProperties.Contains("Name"));
    }

    [TestMethod]
    public void PropertyChanged_OnPause_DoesNotRaiseEvent()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();
        entity.PauseAllActions();

        var changedProperties = new List<string>();
        entity.PropertyChanged += (sender, e) => changedProperties.Add(e.PropertyName!);

        // Act
        entity.Name = "NewValue";

        // Assert - No events should be raised when paused
        Assert.IsFalse(changedProperties.Contains("Name"));
    }

    #endregion

    #region GetProperties Tests

    [TestMethod]
    public void GetProperties_ReturnsAllAccessedProperties()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();

        // Access properties to ensure they are created
        _ = entity.Name;
        _ = entity.Age;
        _ = entity.Amount;

        // Act
        var properties = ((IValidatePropertyManagerInternal<IEntityProperty>)entity.PropertyManager).GetProperties.ToList();

        // Assert
        Assert.IsTrue(properties.Count >= 3);
        Assert.IsTrue(properties.Any(p => p.Name == "Name"));
        Assert.IsTrue(properties.Any(p => p.Name == "Age"));
        Assert.IsTrue(properties.Any(p => p.Name == "Amount"));
    }

    #endregion

    #region Edge Cases Tests

    [TestMethod]
    public void SettingPropertyToSameValue_DoesNotSetModified()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();
        entity.Name = "SameValue";
        entity.PropertyManager.MarkSelfUnmodified();
        Assert.IsFalse(entity.PropertyManager.IsSelfModified);

        // Act - Set to same value
        entity.Name = "SameValue";

        // Assert - Should not be marked as modified
        Assert.IsFalse(entity.PropertyManager.IsSelfModified);
    }

    [TestMethod]
    public void SettingNullToNull_DoesNotSetModified()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();
        entity.Name = null;
        entity.PropertyManager.MarkSelfUnmodified();
        Assert.IsFalse(entity.PropertyManager.IsSelfModified);

        // Act - Set null to null
        entity.Name = null;

        // Assert - Should not be marked as modified
        Assert.IsFalse(entity.PropertyManager.IsSelfModified);
    }

    [TestMethod]
    public void SettingEmptyStringToEmptyString_DoesNotSetModified()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();
        entity.Name = string.Empty;
        entity.PropertyManager.MarkSelfUnmodified();
        Assert.IsFalse(entity.PropertyManager.IsSelfModified);

        // Act
        entity.Name = string.Empty;

        // Assert
        Assert.IsFalse(entity.PropertyManager.IsSelfModified);
    }

    [TestMethod]
    public void DefaultValueType_SettingToDefault_DoesNotSetModifiedIfAlreadyDefault()
    {
        // Arrange
        var entity = new TestEntityObject();
        entity.ResumeAllActions();
        // Age starts at default(int) = 0
        Assert.AreEqual(0, entity.Age);

        // Since Age was accessed, the property is created but not modified yet
        // Setting to 0 should not mark as modified
        entity.PropertyManager.MarkSelfUnmodified();
        Assert.IsFalse(entity.PropertyManager.IsSelfModified);

        // Act
        entity.Age = 0;

        // Assert
        Assert.IsFalse(entity.PropertyManager.IsSelfModified);
    }

    #endregion
}
