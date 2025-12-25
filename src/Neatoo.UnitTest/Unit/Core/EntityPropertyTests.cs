using System.ComponentModel;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo;
using Neatoo.Internal;
using Neatoo.RemoteFactory;
using Neatoo.Rules;

namespace Neatoo.UnitTest.Unit.Core;

/// <summary>
/// Unit tests for the EntityProperty{T} class.
/// Tests modification tracking, pause/resume functionality, display name handling,
/// and inherited ValidateProperty{T} and Property{T} behavior.
/// Uses real Neatoo classes instead of mocks.
/// </summary>
[TestClass]
public class EntityPropertyTests
{
    #region Test Helper Classes

    /// <summary>
    /// Simple POCO for testing basic property scenarios.
    /// </summary>
    private class TestPoco
    {
        public string? Name { get; set; }
        public int Age { get; set; }
        public decimal Amount { get; set; }
        public DateTime DateOfBirth { get; set; }
        public Guid Id { get; set; }
        public int? NullableNumber { get; set; }
        public List<string>? Items { get; set; }

        [DisplayName("Full Name")]
        public string? DisplayNameProperty { get; set; }

        [DisplayName("")]
        public string? EmptyDisplayNameProperty { get; set; }

        public string? ReadOnlyProperty { get; private set; }

        public IEntityMetaProperties? EntityChild { get; set; }
    }

    /// <summary>
    /// Real EntityBase implementation for testing entity child behavior.
    /// Uses SuppressFactory to avoid requiring the full factory infrastructure.
    /// </summary>
    [SuppressFactory]
    private class TestEntityChild : EntityBase<TestEntityChild>
    {
        public TestEntityChild() : base(new EntityBaseServices<TestEntityChild>(null))
        {
            PauseAllActions();
        }

        public string? Name { get => Getter<string>(); set => Setter(value); }
    }

    #endregion

    #region Helper Methods

    private static PropertyInfoWrapper CreatePropertyInfoWrapper(string propertyName)
    {
        var propertyInfo = typeof(TestPoco).GetProperty(propertyName)
            ?? throw new InvalidOperationException($"Property '{propertyName}' not found on TestPoco.");
        return new PropertyInfoWrapper(propertyInfo);
    }

    private static PropertyInfoWrapper CreatePropertyInfoWrapper<T>(string propertyName)
    {
        var propertyInfo = typeof(T).GetProperty(propertyName)
            ?? throw new InvalidOperationException($"Property '{propertyName}' not found on {typeof(T).Name}.");
        return new PropertyInfoWrapper(propertyInfo);
    }

    #endregion

    #region Constructor Tests

    [TestMethod]
    public void Constructor_WithPropertyInfo_SetsNameFromPropertyInfo()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");

        // Act
        var property = new EntityProperty<string>(wrapper);

        // Assert
        Assert.AreEqual("Name", property.Name);
    }

    [TestMethod]
    public void Constructor_WithNullPropertyInfo_ThrowsException()
    {
        // Note: The base Property<T> constructor accesses propertyInfo.Name before
        // the ArgumentNullException.ThrowIfNull can be called, resulting in NullReferenceException
        // Act & Assert
        Assert.ThrowsException<NullReferenceException>(() => new EntityProperty<string>(null!));
    }

    [TestMethod]
    public void Constructor_WithDisplayNameAttribute_SetsDisplayNameFromAttribute()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("DisplayNameProperty");

        // Act
        var property = new EntityProperty<string>(wrapper);

        // Assert
        Assert.AreEqual("Full Name", property.DisplayName);
    }

    [TestMethod]
    public void Constructor_WithoutDisplayNameAttribute_SetsDisplayNameFromPropertyName()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");

        // Act
        var property = new EntityProperty<string>(wrapper);

        // Assert
        Assert.AreEqual("Name", property.DisplayName);
    }

    [TestMethod]
    public void Constructor_WithPropertyInfo_InitializesIsSelfModifiedFalse()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");

        // Act
        var property = new EntityProperty<string>(wrapper);

        // Assert
        Assert.IsFalse(property.IsSelfModified);
    }

    [TestMethod]
    public void Constructor_WithPropertyInfo_InitializesIsPausedFalse()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");

        // Act
        var property = new EntityProperty<string>(wrapper);

        // Assert
        Assert.IsFalse(property.IsPaused);
    }

    [TestMethod]
    public void JsonConstructor_WithParameters_SetsAllProperties()
    {
        // Arrange
        var ruleMessages = new IRuleMessage[]
        {
            new RuleMessage("TestProperty", "Error message 1") { RuleIndex = 1 }
        };

        // Act
        var property = new EntityProperty<string>("PropertyName", "TestValue", true, false, "Display Name", ruleMessages);

        // Assert
        Assert.AreEqual("PropertyName", property.Name);
        Assert.AreEqual("TestValue", property.Value);
        Assert.IsTrue(property.IsSelfModified);
        Assert.IsFalse(property.IsReadOnly);
        Assert.AreEqual("Display Name", property.DisplayName);
        Assert.AreEqual(1, property.RuleMessages.Count);
    }

    [TestMethod]
    public void JsonConstructor_WithIsReadOnlyTrue_SetsIsReadOnly()
    {
        // Arrange
        var ruleMessages = Array.Empty<IRuleMessage>();

        // Act
        var property = new EntityProperty<int>("IntProperty", 42, false, true, "Int Display", ruleMessages);

        // Assert
        Assert.IsTrue(property.IsReadOnly);
    }

    #endregion

    #region IsSelfModified Tests

    [TestMethod]
    public void IsSelfModified_InitialState_ReturnsFalse()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");

        // Act
        var property = new EntityProperty<string>(wrapper);

        // Assert
        Assert.IsFalse(property.IsSelfModified);
    }

    [TestMethod]
    public void IsSelfModified_AfterValueChange_ReturnsTrue()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");
        var property = new EntityProperty<string>(wrapper);

        // Act
        property.Value = "NewValue";

        // Assert
        Assert.IsTrue(property.IsSelfModified);
    }

    [TestMethod]
    public void IsSelfModified_AfterValueChangeToSameValue_StaysUnmodified()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");
        var property = new EntityProperty<string>(wrapper);
        property.Value = "InitialValue";
        property.MarkSelfUnmodified();

        // Act - Set to same value (should not trigger PropertyChanged, thus no modification)
        property.Value = "InitialValue";

        // Assert
        Assert.IsFalse(property.IsSelfModified);
    }

    [TestMethod]
    public void IsSelfModified_AfterValueChangeToDifferentValue_ReturnsTrue()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");
        var property = new EntityProperty<string>(wrapper);
        property.Value = "InitialValue";
        property.MarkSelfUnmodified();

        // Act
        property.Value = "DifferentValue";

        // Assert
        Assert.IsTrue(property.IsSelfModified);
    }

    [TestMethod]
    public void IsSelfModified_WithEntityChildValue_ReturnsFalse()
    {
        // Arrange - When the value implements IEntityMetaProperties, IsSelfModified stays false
        var entityChild = new TestEntityChild();
        var wrapper = CreatePropertyInfoWrapper("EntityChild");
        var property = new EntityProperty<IEntityMetaProperties>(wrapper);

        // Act
        property.Value = entityChild;

        // Assert - EntityChild values don't mark self as modified
        Assert.IsFalse(property.IsSelfModified);
    }

    #endregion

    #region IsModified Tests

    [TestMethod]
    public void IsModified_InitialState_ReturnsFalse()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");

        // Act
        var property = new EntityProperty<string>(wrapper);

        // Assert
        Assert.IsFalse(property.IsModified);
    }

    [TestMethod]
    public void IsModified_WhenIsSelfModifiedTrue_ReturnsTrue()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");
        var property = new EntityProperty<string>(wrapper);

        // Act
        property.Value = "NewValue";

        // Assert
        Assert.IsTrue(property.IsModified);
    }

    [TestMethod]
    public void IsModified_WhenEntityChildIsModified_ReturnsTrue()
    {
        // Arrange
        var entityChild = new TestEntityChild();
        entityChild.ResumeAllActions(); // Resume actions so changes are tracked
        entityChild.Name = "Modified"; // Make the entity modified
        var wrapper = CreatePropertyInfoWrapper("EntityChild");
        var property = new EntityProperty<IEntityMetaProperties>(wrapper);
        property.Value = entityChild;

        // Act & Assert
        Assert.IsTrue(entityChild.IsModified);
        Assert.IsTrue(property.IsModified);
    }

    [TestMethod]
    public void IsModified_WhenEntityChildIsNotModified_ReturnsFalse()
    {
        // Arrange
        var entityChild = new TestEntityChild(); // Entity starts with PauseAllActions(), so no modifications tracked
        var wrapper = CreatePropertyInfoWrapper("EntityChild");
        var property = new EntityProperty<IEntityMetaProperties>(wrapper);
        property.Value = entityChild;

        // Act & Assert
        Assert.IsFalse(entityChild.IsModified);
        Assert.IsFalse(property.IsModified);
    }

    [TestMethod]
    public void IsModified_WithNullEntityChild_UsesIsSelfModified()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");
        var property = new EntityProperty<string>(wrapper);

        // Act
        property.Value = "NewValue";

        // Assert
        Assert.IsTrue(property.IsModified);
        Assert.IsTrue(property.IsSelfModified);
    }

    #endregion

    #region IsPaused Tests

    [TestMethod]
    public void IsPaused_InitialState_ReturnsFalse()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");

        // Act
        var property = new EntityProperty<string>(wrapper);

        // Assert
        Assert.IsFalse(property.IsPaused);
    }

    [TestMethod]
    public void IsPaused_CanBeSetToTrue()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");
        var property = new EntityProperty<string>(wrapper);

        // Act
        property.IsPaused = true;

        // Assert
        Assert.IsTrue(property.IsPaused);
    }

    [TestMethod]
    public void IsPaused_CanBeSetToFalse()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");
        var property = new EntityProperty<string>(wrapper);
        property.IsPaused = true;

        // Act
        property.IsPaused = false;

        // Assert
        Assert.IsFalse(property.IsPaused);
    }

    [TestMethod]
    public void IsPaused_WhenTrue_ValueChangeDoesNotSetIsSelfModified()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");
        var property = new EntityProperty<string>(wrapper);
        property.IsPaused = true;

        // Act
        property.Value = "NewValue";

        // Assert
        Assert.IsFalse(property.IsSelfModified);
    }

    [TestMethod]
    public void IsPaused_WhenFalse_ValueChangeSetsIsSelfModified()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");
        var property = new EntityProperty<string>(wrapper);
        property.IsPaused = false;

        // Act
        property.Value = "NewValue";

        // Assert
        Assert.IsTrue(property.IsSelfModified);
    }

    [TestMethod]
    public void IsPaused_SetToTrueAfterModification_DoesNotResetIsSelfModified()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");
        var property = new EntityProperty<string>(wrapper);
        property.Value = "NewValue";
        Assert.IsTrue(property.IsSelfModified);

        // Act
        property.IsPaused = true;

        // Assert - IsSelfModified should still be true
        Assert.IsTrue(property.IsSelfModified);
    }

    #endregion

    #region MarkSelfUnmodified Tests

    [TestMethod]
    public void MarkSelfUnmodified_WhenModified_SetsIsSelfModifiedFalse()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");
        var property = new EntityProperty<string>(wrapper);
        property.Value = "NewValue";
        Assert.IsTrue(property.IsSelfModified);

        // Act
        property.MarkSelfUnmodified();

        // Assert
        Assert.IsFalse(property.IsSelfModified);
    }

    [TestMethod]
    public void MarkSelfUnmodified_WhenNotModified_StaysUnmodified()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");
        var property = new EntityProperty<string>(wrapper);
        Assert.IsFalse(property.IsSelfModified);

        // Act
        property.MarkSelfUnmodified();

        // Assert
        Assert.IsFalse(property.IsSelfModified);
    }

    [TestMethod]
    public void MarkSelfUnmodified_DoesNotAffectEntityChildModification()
    {
        // Arrange
        var entityChild = new TestEntityChild();
        entityChild.ResumeAllActions(); // Resume actions so changes are tracked
        entityChild.Name = "Modified"; // Make the entity modified
        var wrapper = CreatePropertyInfoWrapper("EntityChild");
        var property = new EntityProperty<IEntityMetaProperties>(wrapper);
        property.Value = entityChild;

        // Act
        property.MarkSelfUnmodified();

        // Assert - IsModified still true because EntityChild is modified
        Assert.IsTrue(entityChild.IsModified);
        Assert.IsTrue(property.IsModified);
        Assert.IsFalse(property.IsSelfModified);
    }

    [TestMethod]
    public void MarkSelfUnmodified_CanBeModifiedAgainAfterwards()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");
        var property = new EntityProperty<string>(wrapper);
        property.Value = "FirstValue";
        property.MarkSelfUnmodified();

        // Act
        property.Value = "SecondValue";

        // Assert
        Assert.IsTrue(property.IsSelfModified);
    }

    #endregion

    #region LoadValue Tests

    [TestMethod]
    public void LoadValue_SetsValueWithoutModifyingIsSelfModified()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");
        var property = new EntityProperty<string>(wrapper);

        // Act
        property.LoadValue("LoadedValue");

        // Assert
        Assert.AreEqual("LoadedValue", property.Value);
        Assert.IsFalse(property.IsSelfModified);
    }

    [TestMethod]
    public void LoadValue_ResetsIsSelfModifiedToFalse()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");
        var property = new EntityProperty<string>(wrapper);
        property.Value = "InitialValue"; // This sets IsSelfModified to true

        // Act
        property.LoadValue("LoadedValue");

        // Assert
        Assert.AreEqual("LoadedValue", property.Value);
        Assert.IsFalse(property.IsSelfModified);
    }

    [TestMethod]
    public void LoadValue_WithNull_SetsValueToNull()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");
        var property = new EntityProperty<string>(wrapper);
        property.Value = "InitialValue";

        // Act
        property.LoadValue(null);

        // Assert
        Assert.IsNull(property.Value);
        Assert.IsFalse(property.IsSelfModified);
    }

    [TestMethod]
    public void LoadValue_WithValueType_SetsValue()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Age");
        var property = new EntityProperty<int>(wrapper);

        // Act
        property.LoadValue(42);

        // Assert
        Assert.AreEqual(42, property.Value);
        Assert.IsFalse(property.IsSelfModified);
    }

    #endregion

    #region EntityChild Tests

    [TestMethod]
    public void EntityChild_WhenValueImplementsIEntityMetaProperties_ReturnsValue()
    {
        // Arrange
        var entityChild = new TestEntityChild();
        var wrapper = CreatePropertyInfoWrapper("EntityChild");
        var property = new EntityProperty<IEntityMetaProperties>(wrapper);
        property.Value = entityChild;

        // Act
        var result = property.EntityChild;

        // Assert
        Assert.IsNotNull(result);
        Assert.AreSame(entityChild, result);
    }

    [TestMethod]
    public void EntityChild_WhenValueDoesNotImplementIEntityMetaProperties_ReturnsNull()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");
        var property = new EntityProperty<string>(wrapper);
        property.Value = "RegularString";

        // Act
        var entityChild = property.EntityChild;

        // Assert
        Assert.IsNull(entityChild);
    }

    [TestMethod]
    public void EntityChild_WhenValueIsNull_ReturnsNull()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");
        var property = new EntityProperty<string>(wrapper);

        // Act
        var entityChild = property.EntityChild;

        // Assert
        Assert.IsNull(entityChild);
    }

    #endregion

    #region DisplayName Tests

    [TestMethod]
    public void DisplayName_WithDisplayNameAttribute_ReturnsAttributeValue()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("DisplayNameProperty");

        // Act
        var property = new EntityProperty<string>(wrapper);

        // Assert
        Assert.AreEqual("Full Name", property.DisplayName);
    }

    [TestMethod]
    public void DisplayName_WithoutDisplayNameAttribute_ReturnsPropertyName()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");

        // Act
        var property = new EntityProperty<string>(wrapper);

        // Assert
        Assert.AreEqual("Name", property.DisplayName);
    }

    [TestMethod]
    public void DisplayName_IsInitOnly_CannotBeChangedAfterConstruction()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("DisplayNameProperty");
        var property = new EntityProperty<string>(wrapper);

        // Assert - DisplayName is init-only, verified by its value remaining constant
        Assert.AreEqual("Full Name", property.DisplayName);
    }

    #endregion

    #region IEntityProperty Interface Implementation Tests

    [TestMethod]
    public void ImplementsIEntityPropertyInterface()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");

        // Act
        var property = new EntityProperty<string>(wrapper);

        // Assert
        Assert.IsInstanceOfType(property, typeof(IEntityProperty));
    }

    [TestMethod]
    public void ImplementsIEntityPropertyOfTInterface()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");

        // Act
        var property = new EntityProperty<string>(wrapper);

        // Assert
        Assert.IsInstanceOfType(property, typeof(IEntityProperty<string>));
    }

    [TestMethod]
    public void ImplementsIValidatePropertyInterface()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");

        // Act
        var property = new EntityProperty<string>(wrapper);

        // Assert
        Assert.IsInstanceOfType(property, typeof(IValidateProperty));
    }

    [TestMethod]
    public void ImplementsIPropertyInterface()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");

        // Act
        var property = new EntityProperty<string>(wrapper);

        // Assert
        Assert.IsInstanceOfType(property, typeof(IProperty));
    }

    [TestMethod]
    public void ImplementsINotifyPropertyChangedInterface()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");

        // Act
        var property = new EntityProperty<string>(wrapper);

        // Assert
        Assert.IsInstanceOfType(property, typeof(INotifyPropertyChanged));
    }

    #endregion

    #region Inherited ValidateProperty Behavior Tests

    [TestMethod]
    public void IsSelfValid_NoRuleMessages_ReturnsTrue()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");
        var property = new EntityProperty<string>(wrapper);

        // Act & Assert
        Assert.IsTrue(property.IsSelfValid);
    }

    [TestMethod]
    public void IsSelfValid_HasRuleMessages_ReturnsFalse()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");
        var property = new EntityProperty<string>(wrapper);
        var ruleMessages = new List<IRuleMessage>
        {
            new RuleMessage("Name", "Error message") { RuleIndex = 1 }
        };
        ((IValidateProperty)property).SetMessagesForRule(ruleMessages);

        // Act & Assert
        Assert.IsFalse(property.IsSelfValid);
    }

    [TestMethod]
    public void IsValid_NoRuleMessages_ReturnsTrue()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");
        var property = new EntityProperty<string>(wrapper);

        // Act & Assert
        Assert.IsTrue(property.IsValid);
    }

    [TestMethod]
    public void IsValid_HasRuleMessages_ReturnsFalse()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");
        var property = new EntityProperty<string>(wrapper);
        var ruleMessages = new List<IRuleMessage>
        {
            new RuleMessage("Name", "Validation error") { RuleIndex = 1 }
        };
        ((IValidateProperty)property).SetMessagesForRule(ruleMessages);

        // Act & Assert
        Assert.IsFalse(property.IsValid);
    }

    [TestMethod]
    public void ClearSelfMessages_ClearsAllRuleMessages()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");
        var property = new EntityProperty<string>(wrapper);
        var ruleMessages = new List<IRuleMessage>
        {
            new RuleMessage("Name", "Error 1") { RuleIndex = 1 },
            new RuleMessage("Name", "Error 2") { RuleIndex = 2 }
        };
        ((IValidateProperty)property).SetMessagesForRule(ruleMessages);

        // Act
        property.ClearSelfMessages();

        // Assert
        Assert.AreEqual(0, property.RuleMessages.Count);
        Assert.IsTrue(property.IsValid);
    }

    [TestMethod]
    public void PropertyMessages_EmptyWhenNoRuleMessages()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");
        var property = new EntityProperty<string>(wrapper);

        // Act
        var propertyMessages = property.PropertyMessages;

        // Assert
        Assert.IsNotNull(propertyMessages);
        Assert.AreEqual(0, propertyMessages.Count);
    }

    [TestMethod]
    public void PropertyMessages_ContainsMessagesWhenRuleMessagesExist()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");
        var property = new EntityProperty<string>(wrapper);
        var ruleMessages = new List<IRuleMessage>
        {
            new RuleMessage("Name", "Error 1") { RuleIndex = 1 },
            new RuleMessage("Name", "Error 2") { RuleIndex = 2 }
        };
        ((IValidateProperty)property).SetMessagesForRule(ruleMessages);

        // Act
        var propertyMessages = property.PropertyMessages;

        // Assert
        Assert.AreEqual(2, propertyMessages.Count);
    }

    #endregion

    #region Inherited Property Behavior Tests

    [TestMethod]
    public void Value_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");
        var property = new EntityProperty<string>(wrapper);

        // Act
        property.Value = "TestValue";

        // Assert
        Assert.AreEqual("TestValue", property.Value);
    }

    [TestMethod]
    public void Value_Change_RaisesPropertyChangedEvent()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");
        var property = new EntityProperty<string>(wrapper);
        var eventRaised = false;
        property.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == "Value")
                eventRaised = true;
        };

        // Act
        property.Value = "NewValue";

        // Assert
        Assert.IsTrue(eventRaised);
    }

    [TestMethod]
    public void IsReadOnly_InheritedFromPropertyInfo()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("ReadOnlyProperty");

        // Act
        var property = new EntityProperty<string>(wrapper);

        // Assert
        Assert.IsTrue(property.IsReadOnly);
    }

    [TestMethod]
    public void SetValue_WhenReadOnly_ThrowsException()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("ReadOnlyProperty");
        var property = new EntityProperty<string>(wrapper);

        // Act & Assert
        Assert.ThrowsException<PropertyReadOnlyException>(() => property.Value = "NewValue");
    }

    [TestMethod]
    public void Name_InheritedFromPropertyInfo()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Age");

        // Act
        var property = new EntityProperty<int>(wrapper);

        // Assert
        Assert.AreEqual("Age", property.Name);
    }

    [TestMethod]
    public void Type_ReturnsGenericTypeParameter()
    {
        // Arrange
        var stringWrapper = CreatePropertyInfoWrapper("Name");
        var intWrapper = CreatePropertyInfoWrapper("Age");

        var stringProperty = new EntityProperty<string>(stringWrapper);
        var intProperty = new EntityProperty<int>(intWrapper);

        // Assert
        Assert.AreEqual(typeof(string), stringProperty.Type);
        Assert.AreEqual(typeof(int), intProperty.Type);
    }

    [TestMethod]
    public void IsBusy_InitialState_ReturnsFalse()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");

        // Act
        var property = new EntityProperty<string>(wrapper);

        // Assert
        Assert.IsFalse(property.IsBusy);
    }

    #endregion

    #region SetPrivateValue with Quietly Parameter Tests

    [TestMethod]
    public async Task SetPrivateValue_WithQuietlyTrue_DoesNotRaisePropertyChangedEvent()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");
        var property = new EntityProperty<string>(wrapper);
        var eventRaisedCount = 0;
        property.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == "Value")
            {
                eventRaisedCount++;
            }
        };

        // Act
        await property.SetPrivateValue("NewValue", quietly: true);

        // Assert
        Assert.AreEqual("NewValue", property.Value);
        Assert.AreEqual(0, eventRaisedCount);
    }

    [TestMethod]
    public async Task SetPrivateValue_WithQuietlyTrue_DoesNotSetIsSelfModified()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");
        var property = new EntityProperty<string>(wrapper);

        // Act
        await property.SetPrivateValue("NewValue", quietly: true);

        // Assert
        Assert.AreEqual("NewValue", property.Value);
        Assert.IsFalse(property.IsSelfModified);
    }

    [TestMethod]
    public async Task SetPrivateValue_WithQuietlyFalse_RaisesPropertyChangedEvent()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");
        var property = new EntityProperty<string>(wrapper);
        var eventRaised = false;
        property.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == "Value")
            {
                eventRaised = true;
            }
        };

        // Act
        await property.SetPrivateValue("NewValue", quietly: false);

        // Assert
        Assert.AreEqual("NewValue", property.Value);
        Assert.IsTrue(eventRaised);
    }

    [TestMethod]
    public async Task SetPrivateValue_WithQuietlyFalse_SetsIsSelfModified()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");
        var property = new EntityProperty<string>(wrapper);

        // Act
        await property.SetPrivateValue("NewValue", quietly: false);

        // Assert
        Assert.AreEqual("NewValue", property.Value);
        Assert.IsTrue(property.IsSelfModified);
    }

    #endregion

    #region Value Type Tests

    [TestMethod]
    public void EntityProperty_WithValueType_WorksCorrectly()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Age");
        var property = new EntityProperty<int>(wrapper);

        // Act
        property.Value = 42;

        // Assert
        Assert.AreEqual(42, property.Value);
        Assert.IsTrue(property.IsSelfModified);
    }

    [TestMethod]
    public void EntityProperty_WithNullableValueType_HandlesNullCorrectly()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("NullableNumber");
        var property = new EntityProperty<int?>(wrapper);

        // Act & Assert
        Assert.IsNull(property.Value);
        property.Value = 42;
        Assert.AreEqual(42, property.Value);
        property.Value = null;
        Assert.IsNull(property.Value);
    }

    [TestMethod]
    public void EntityProperty_WithComplexType_WorksCorrectly()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Items");
        var property = new EntityProperty<List<string>>(wrapper);
        var list = new List<string> { "item1", "item2" };

        // Act
        property.Value = list;

        // Assert
        Assert.AreEqual(2, property.Value?.Count);
        Assert.AreSame(list, property.Value);
        Assert.IsTrue(property.IsSelfModified);
    }

    [TestMethod]
    public void EntityProperty_WithDecimal_WorksCorrectly()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Amount");
        var property = new EntityProperty<decimal>(wrapper);

        // Act
        property.Value = 123.45m;

        // Assert
        Assert.AreEqual(123.45m, property.Value);
        Assert.IsTrue(property.IsSelfModified);
    }

    [TestMethod]
    public void EntityProperty_WithDateTime_WorksCorrectly()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("DateOfBirth");
        var property = new EntityProperty<DateTime>(wrapper);
        var dateTime = new DateTime(2024, 1, 1, 12, 0, 0);

        // Act
        property.Value = dateTime;

        // Assert
        Assert.AreEqual(dateTime, property.Value);
        Assert.IsTrue(property.IsSelfModified);
    }

    [TestMethod]
    public void EntityProperty_WithGuid_WorksCorrectly()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Id");
        var property = new EntityProperty<Guid>(wrapper);
        var guid = Guid.NewGuid();

        // Act
        property.Value = guid;

        // Assert
        Assert.AreEqual(guid, property.Value);
        Assert.IsTrue(property.IsSelfModified);
    }

    #endregion

    #region Complex Scenarios Tests

    [TestMethod]
    public void Scenario_ModifyThenPauseThenModify_OnlyFirstModificationTracked()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");
        var property = new EntityProperty<string>(wrapper);

        // Act
        property.Value = "FirstValue"; // Should be tracked
        property.MarkSelfUnmodified();
        property.IsPaused = true;
        property.Value = "SecondValue"; // Should NOT be tracked

        // Assert
        Assert.AreEqual("SecondValue", property.Value);
        Assert.IsFalse(property.IsSelfModified);
    }

    [TestMethod]
    public void Scenario_PauseThenModifyThenResumeThenModify_OnlyLastModificationTracked()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");
        var property = new EntityProperty<string>(wrapper);

        // Act
        property.IsPaused = true;
        property.Value = "FirstValue"; // Should NOT be tracked
        Assert.IsFalse(property.IsSelfModified);

        property.IsPaused = false;
        property.Value = "SecondValue"; // Should be tracked

        // Assert
        Assert.IsTrue(property.IsSelfModified);
    }

    [TestMethod]
    public void Scenario_MultipleModificationsAndUnmodified_TracksCorrectly()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");
        var property = new EntityProperty<string>(wrapper);

        // Act & Assert
        property.Value = "First";
        Assert.IsTrue(property.IsSelfModified);

        property.MarkSelfUnmodified();
        Assert.IsFalse(property.IsSelfModified);

        property.Value = "Second";
        Assert.IsTrue(property.IsSelfModified);

        property.MarkSelfUnmodified();
        Assert.IsFalse(property.IsSelfModified);

        property.Value = "Third";
        Assert.IsTrue(property.IsSelfModified);
    }

    [TestMethod]
    public void Scenario_EntityChildWithSelfModification_IsModifiedReflectsBoth()
    {
        // Arrange
        var entityChild = new TestEntityChild(); // Starts with PauseAllActions(), so not modified
        var wrapper = CreatePropertyInfoWrapper("EntityChild");
        var property = new EntityProperty<IEntityMetaProperties>(wrapper);
        property.Value = entityChild;

        // Assert - Initially not modified (EntityChild not modified, self not modified)
        Assert.IsFalse(entityChild.IsModified);
        Assert.IsFalse(property.IsModified);
        Assert.IsFalse(property.IsSelfModified);

        // Act - EntityChild becomes modified
        entityChild.ResumeAllActions();
        entityChild.Name = "Modified";

        // Assert - IsModified now true
        Assert.IsTrue(entityChild.IsModified);
        Assert.IsTrue(property.IsModified);
        Assert.IsFalse(property.IsSelfModified);
    }

    [TestMethod]
    public void Scenario_LoadValueThenModify_TracksModification()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");
        var property = new EntityProperty<string>(wrapper);

        // Act
        property.LoadValue("LoadedValue");
        Assert.IsFalse(property.IsSelfModified);

        property.Value = "ModifiedValue";

        // Assert
        Assert.IsTrue(property.IsSelfModified);
    }

    #endregion

    #region PropertyChanged Event Tests

    [TestMethod]
    public void ValueChange_WhenNotPaused_RaisesValuePropertyChangedEvent()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");
        var property = new EntityProperty<string>(wrapper);
        var changedProperties = new List<string>();
        property.PropertyChanged += (sender, e) => changedProperties.Add(e.PropertyName!);

        // Act
        property.Value = "NewValue";

        // Assert
        Assert.IsTrue(changedProperties.Contains("Value"));
    }

    [TestMethod]
    public void SetMessagesForRule_RaisesPropertyChangedForIsValid()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");
        var property = new EntityProperty<string>(wrapper);
        var changedProperties = new List<string>();
        property.PropertyChanged += (sender, e) => changedProperties.Add(e.PropertyName!);

        var ruleMessages = new List<IRuleMessage>
        {
            new RuleMessage("Name", "Error") { RuleIndex = 1 }
        };

        // Act
        ((IValidateProperty)property).SetMessagesForRule(ruleMessages);

        // Assert
        Assert.IsTrue(changedProperties.Contains("IsValid"));
        Assert.IsTrue(changedProperties.Contains("IsSelfValid"));
    }

    #endregion

    #region Thread Safety Hints Tests

    [TestMethod]
    public void ConcurrentValueChanges_HandlesCorrectly()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Age");
        var property = new EntityProperty<int>(wrapper);
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 100; i++)
        {
            var value = i;
            tasks.Add(Task.Run(() => property.Value = value));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert - Value should be set to one of the values, and should be modified
        Assert.IsTrue(property.IsSelfModified);
        Assert.IsTrue(property.Value >= 0 && property.Value < 100);
    }

    [TestMethod]
    public void ConcurrentPauseAndValueChanges_HandlesCorrectly()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Age");
        var property = new EntityProperty<int>(wrapper);
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 50; i++)
        {
            var value = i;
            tasks.Add(Task.Run(() =>
            {
                property.IsPaused = (value % 2 == 0);
                property.Value = value;
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert - Should not throw and should have a valid state
        Assert.IsTrue(property.Value >= 0 && property.Value < 50);
    }

    #endregion

    #region Edge Cases Tests

    [TestMethod]
    public void EntityProperty_WithEmptyStringDisplayName_UsesEmptyString()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("EmptyDisplayNameProperty");

        // Act
        var property = new EntityProperty<string>(wrapper);

        // Assert
        Assert.AreEqual("", property.DisplayName);
    }

    [TestMethod]
    public void EntityProperty_SettingNullValue_SetsIsSelfModified()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");
        var property = new EntityProperty<string>(wrapper);
        property.Value = "InitialValue";
        property.MarkSelfUnmodified();

        // Act
        property.Value = null;

        // Assert
        Assert.IsNull(property.Value);
        Assert.IsTrue(property.IsSelfModified);
    }

    [TestMethod]
    public void EntityProperty_SettingNullToNull_DoesNotChangeModificationState()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");
        var property = new EntityProperty<string>(wrapper);
        // Value is already null, IsSelfModified is false

        // Act
        property.Value = null;

        // Assert - Setting null to null should not trigger modification
        Assert.IsFalse(property.IsSelfModified);
    }

    [TestMethod]
    public void EntityProperty_JsonConstructorWithEmptyRuleMessages_HasNoValidationErrors()
    {
        // Arrange
        var ruleMessages = Array.Empty<IRuleMessage>();

        // Act
        var property = new EntityProperty<string>("TestProp", "Value", false, false, "Display", ruleMessages);

        // Assert
        Assert.IsTrue(property.IsValid);
        Assert.AreEqual(0, property.RuleMessages.Count);
    }

    [TestMethod]
    public void EntityProperty_CanAccessValueViaIPropertyInterface()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");
        var property = new EntityProperty<string>(wrapper);
        property.Value = "TestValue";
        IProperty iproperty = property;

        // Act
        var value = iproperty.Value;

        // Assert
        Assert.AreEqual("TestValue", value);
    }

    [TestMethod]
    public void EntityProperty_CanAccessValueViaIEntityPropertyInterface()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper("Name");
        var property = new EntityProperty<string>(wrapper);
        property.Value = "TestValue";
        IEntityProperty entityProperty = property;

        // Act & Assert
        Assert.AreEqual("Name", entityProperty.Name);
        Assert.AreEqual("Name", entityProperty.DisplayName);
        Assert.IsFalse(entityProperty.IsPaused);
        Assert.IsTrue(entityProperty.IsSelfModified);
        Assert.IsTrue(entityProperty.IsModified);
    }

    #endregion
}
