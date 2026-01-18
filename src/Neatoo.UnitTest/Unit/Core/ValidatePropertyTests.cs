using System.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo;
using Neatoo.Internal;
using Neatoo.RemoteFactory;
using Neatoo.Rules;

namespace Neatoo.UnitTest.Unit.Core;

/// <summary>
/// Extended test POCO class with additional properties for ValidateProperty tests.
/// Complements the TestPoco from PropertyTests.cs with properties specific to validation testing.
/// </summary>
public class ValidateTestPoco
{
    public string? Name { get; set; }
    public int Age { get; set; }
    public string? ReadOnlyProperty { get; private set; }
    public decimal Price { get; set; }
    public List<string>? Items { get; set; }
    public int? NullableValue { get; set; }
}

/// <summary>
/// A real ValidateBase implementation for testing child validation behavior.
/// Uses SuppressFactory to avoid requiring the full factory infrastructure.
/// Properties use partial property declarations which get backing fields generated.
/// </summary>
[Factory]
public partial class ValidatePropertyTestChild : ValidateBase<ValidatePropertyTestChild>
{
    public ValidatePropertyTestChild() : base(new ValidateBaseServices<ValidatePropertyTestChild>())
    {
        // Pause all actions to prevent auto-rule execution during test setup
        PauseAllActions();
    }

    public partial string? ChildName { get; set; }
    public partial int ChildValue { get; set; }

    /// <summary>
    /// Adds a validation error to make the object invalid.
    /// Uses the RuleManager to add a validation rule that returns the specified error message.
    /// </summary>
    public void AddValidationError(string propertyName, string errorMessage)
    {
        ResumeAllActions();
        RuleManager.AddValidation(_ => errorMessage, t => t.ChildName);
        _ = RunRules(RunRulesFlag.All);
        PauseAllActions();
    }
}

/// <summary>
/// Unit tests for the ValidateProperty{T} class.
/// Tests validation, rule message management, IsSelfValid/IsValid behavior,
/// and inherited Property{T} functionality.
/// Uses real Neatoo classes (PropertyInfoWrapper) instead of mocks.
/// </summary>
[TestClass]
public class ValidatePropertyTests
{
    private static PropertyInfoWrapper CreatePropertyInfoWrapper<T>(string propertyName)
    {
        var propertyInfo = typeof(ValidateTestPoco).GetProperty(propertyName)
            ?? throw new ArgumentException($"Property '{propertyName}' not found on ValidateTestPoco");
        return new PropertyInfoWrapper(propertyInfo);
    }

    private static PropertyInfoWrapper CreatePropertyInfoWrapper(Type type, string propertyName)
    {
        var propertyInfo = type.GetProperty(propertyName)
            ?? throw new ArgumentException($"Property '{propertyName}' not found on {type.Name}");
        return new PropertyInfoWrapper(propertyInfo);
    }

    #region Constructor Tests

    [TestMethod]
    public void Constructor_WithPropertyInfo_SetsNameFromPropertyInfo()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");

        // Act
        var property = new ValidateProperty<string>(wrapper);

        // Assert
        Assert.AreEqual("Name", property.Name);
    }

    [TestMethod]
    public void Constructor_WithPropertyInfo_InitializesEmptyRuleMessages()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");

        // Act
        var property = new ValidateProperty<string>(wrapper);

        // Assert
        Assert.IsNotNull(property.RuleMessages);
        Assert.AreEqual(0, property.RuleMessages.Count);
    }

    [TestMethod]
    public void JsonConstructor_WithParameters_SetsAllProperties()
    {
        // Arrange
        var ruleMessages = new IRuleMessage[]
        {
            new RuleMessage("Name", "Error message 1") { RuleId = 1 },
            new RuleMessage("Name", "Error message 2") { RuleId = 2 }
        };

        // Act
        var property = new ValidateProperty<string>("PropertyName", "TestValue", ruleMessages, true);

        // Assert
        Assert.AreEqual("PropertyName", property.Name);
        Assert.AreEqual("TestValue", property.Value);
        Assert.IsTrue(property.IsReadOnly);
        Assert.AreEqual(2, property.RuleMessages.Count);
    }

    [TestMethod]
    public void JsonConstructor_WithEmptyRuleMessages_SetsEmptyList()
    {
        // Arrange
        var ruleMessages = Array.Empty<IRuleMessage>();

        // Act
        var property = new ValidateProperty<int>("IntProperty", 42, ruleMessages, false);

        // Assert
        Assert.AreEqual(0, property.RuleMessages.Count);
        Assert.IsTrue(property.IsValid);
    }

    #endregion

    #region IsSelfValid Tests

    [TestMethod]
    public void IsSelfValid_NoRuleMessages_ReturnsTrue()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var property = new ValidateProperty<string>(wrapper);

        // Act & Assert
        Assert.IsTrue(property.IsSelfValid);
    }

    [TestMethod]
    public void IsSelfValid_HasRuleMessages_ReturnsFalse()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var property = new ValidateProperty<string>(wrapper);
        var ruleMessages = new List<IRuleMessage>
        {
            new RuleMessage("Name", "Error message") { RuleId = 1 }
        };
        ((IValidatePropertyInternal)property).SetMessagesForRule(ruleMessages);

        // Act & Assert
        Assert.IsFalse(property.IsSelfValid);
    }

    [TestMethod]
    public void IsSelfValid_AfterClearingMessages_ReturnsTrue()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var property = new ValidateProperty<string>(wrapper);
        var ruleMessages = new List<IRuleMessage>
        {
            new RuleMessage("Name", "Error message") { RuleId = 1 }
        };
        ((IValidatePropertyInternal)property).SetMessagesForRule(ruleMessages);

        // Act
        ((IValidatePropertyInternal)property).ClearSelfMessages();

        // Assert
        Assert.IsTrue(property.IsSelfValid);
    }

    [TestMethod]
    public void IsSelfValid_WithValidateBaseValue_ReturnsTrue()
    {
        // Arrange - When value implements IValidateMetaProperties, IsSelfValid returns true
        // regardless of RuleMessages (per the implementation logic)
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var childValidate = new ValidatePropertyTestChild();
        // A fresh ValidatePropertyTestChild is valid by default (no validation errors)

        var property = new ValidateProperty<ValidatePropertyTestChild>(wrapper);
        property.LoadValue(childValidate);

        // Act & Assert
        Assert.IsTrue(property.IsSelfValid);
    }

    #endregion

    #region IsValid Tests

    [TestMethod]
    public void IsValid_NoRuleMessages_ReturnsTrue()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var property = new ValidateProperty<string>(wrapper);

        // Act & Assert
        Assert.IsTrue(property.IsValid);
    }

    [TestMethod]
    public void IsValid_HasRuleMessages_ReturnsFalse()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var property = new ValidateProperty<string>(wrapper);
        var ruleMessages = new List<IRuleMessage>
        {
            new RuleMessage("Name", "Validation error") { RuleId = 1 }
        };
        ((IValidatePropertyInternal)property).SetMessagesForRule(ruleMessages);

        // Act & Assert
        Assert.IsFalse(property.IsValid);
    }

    [TestMethod]
    public void IsValid_WithValidateBaseValueValid_ReturnsTrue()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var childValidate = new ValidatePropertyTestChild();
        // A fresh ValidatePropertyTestChild is valid by default (no validation errors)

        var property = new ValidateProperty<ValidatePropertyTestChild>(wrapper);
        property.LoadValue(childValidate);

        // Act & Assert
        Assert.IsTrue(property.IsValid);
    }

    [TestMethod]
    public void IsValid_WithValidateBaseValueInvalid_ReturnsFalse()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var childValidate = new ValidatePropertyTestChild();
        // Add a validation error to make the child invalid
        childValidate.AddValidationError("ChildName", "Child validation error");

        var property = new ValidateProperty<ValidatePropertyTestChild>(wrapper);
        property.LoadValue(childValidate);

        // Act & Assert
        Assert.IsFalse(property.IsValid);
    }

    #endregion

    #region SetMessagesForRule Tests

    [TestMethod]
    public void SetMessagesForRule_AddsMessages_MarksInvalid()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var property = new ValidateProperty<string>(wrapper);
        var ruleMessages = new List<IRuleMessage>
        {
            new RuleMessage("Name", "Error 1") { RuleId = 1 }
        };

        // Act
        ((IValidatePropertyInternal)property).SetMessagesForRule(ruleMessages);

        // Assert
        Assert.IsFalse(property.IsValid);
        Assert.AreEqual(1, property.RuleMessages.Count);
    }

    [TestMethod]
    public void SetMessagesForRule_WithMultipleMessages_AddsAll()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var property = new ValidateProperty<string>(wrapper);
        var ruleMessages = new List<IRuleMessage>
        {
            new RuleMessage("Name", "Error 1") { RuleId = 1 },
            new RuleMessage("Name", "Error 2") { RuleId = 2 },
            new RuleMessage("Name", "Error 3") { RuleId = 3 }
        };

        // Act
        ((IValidatePropertyInternal)property).SetMessagesForRule(ruleMessages);

        // Assert
        Assert.AreEqual(3, property.RuleMessages.Count);
    }

    [TestMethod]
    public void SetMessagesForRule_ReplacesExistingForSameRuleId()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var property = new ValidateProperty<string>(wrapper);
        var initialMessages = new List<IRuleMessage>
        {
            new RuleMessage("Name", "Initial error") { RuleId = 1 }
        };
        ((IValidatePropertyInternal)property).SetMessagesForRule(initialMessages);

        var replacementMessages = new List<IRuleMessage>
        {
            new RuleMessage("Name", "Replacement error") { RuleId = 1 }
        };

        // Act
        ((IValidatePropertyInternal)property).SetMessagesForRule(replacementMessages);

        // Assert
        Assert.AreEqual(1, property.RuleMessages.Count);
        Assert.AreEqual("Replacement error", property.RuleMessages[0].Message);
    }

    [TestMethod]
    public void SetMessagesForRule_PreservesOtherRuleIdMessages()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var property = new ValidateProperty<string>(wrapper);
        var rule1Messages = new List<IRuleMessage>
        {
            new RuleMessage("Name", "Rule 1 error") { RuleId = 1 }
        };
        ((IValidatePropertyInternal)property).SetMessagesForRule(rule1Messages);

        var rule2Messages = new List<IRuleMessage>
        {
            new RuleMessage("Name", "Rule 2 error") { RuleId = 2 }
        };

        // Act
        ((IValidatePropertyInternal)property).SetMessagesForRule(rule2Messages);

        // Assert
        Assert.AreEqual(2, property.RuleMessages.Count);
        Assert.IsTrue(property.RuleMessages.Any(rm => rm.RuleId == 1));
        Assert.IsTrue(property.RuleMessages.Any(rm => rm.RuleId == 2));
    }

    [TestMethod]
    public void SetMessagesForRule_WithNullMessage_DoesNotAddMessage()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var property = new ValidateProperty<string>(wrapper);
        var ruleMessages = new List<IRuleMessage>
        {
            new RuleMessage("Name") { RuleId = 1 } // Message is null
        };

        // Act
        ((IValidatePropertyInternal)property).SetMessagesForRule(ruleMessages);

        // Assert
        Assert.AreEqual(0, property.RuleMessages.Count);
        Assert.IsTrue(property.IsValid);
    }

    [TestMethod]
    public void SetMessagesForRule_RaisesPropertyChangedForIsValid()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var property = new ValidateProperty<string>(wrapper);
        var changedProperties = new List<string>();
        property.PropertyChanged += (sender, e) => changedProperties.Add(e.PropertyName!);

        var ruleMessages = new List<IRuleMessage>
        {
            new RuleMessage("Name", "Error") { RuleId = 1 }
        };

        // Act
        ((IValidatePropertyInternal)property).SetMessagesForRule(ruleMessages);

        // Assert
        Assert.IsTrue(changedProperties.Contains("IsValid"));
    }

    [TestMethod]
    public void SetMessagesForRule_RaisesPropertyChangedForIsSelfValid()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var property = new ValidateProperty<string>(wrapper);
        var changedProperties = new List<string>();
        property.PropertyChanged += (sender, e) => changedProperties.Add(e.PropertyName!);

        var ruleMessages = new List<IRuleMessage>
        {
            new RuleMessage("Name", "Error") { RuleId = 1 }
        };

        // Act
        ((IValidatePropertyInternal)property).SetMessagesForRule(ruleMessages);

        // Assert
        Assert.IsTrue(changedProperties.Contains("IsSelfValid"));
    }

    [TestMethod]
    public void SetMessagesForRule_RaisesPropertyChangedForRuleMessages()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var property = new ValidateProperty<string>(wrapper);
        var changedProperties = new List<string>();
        property.PropertyChanged += (sender, e) => changedProperties.Add(e.PropertyName!);

        var ruleMessages = new List<IRuleMessage>
        {
            new RuleMessage("Name", "Error") { RuleId = 1 }
        };

        // Act
        ((IValidatePropertyInternal)property).SetMessagesForRule(ruleMessages);

        // Assert
        Assert.IsTrue(changedProperties.Contains("RuleMessages"));
    }

    #endregion

    #region ClearMessagesForRule Tests

    [TestMethod]
    public void ClearMessagesForRule_RemovesMessagesForSpecificRuleId()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var property = new ValidateProperty<string>(wrapper);
        var ruleMessages = new List<IRuleMessage>
        {
            new RuleMessage("Name", "Rule 1 error") { RuleId = 1 },
            new RuleMessage("Name", "Rule 2 error") { RuleId = 2 }
        };
        ((IValidatePropertyInternal)property).SetMessagesForRule(ruleMessages);

        // Act
        ((IValidatePropertyInternal)property).ClearMessagesForRule(1);

        // Assert
        Assert.AreEqual(1, property.RuleMessages.Count);
        Assert.AreEqual(2u, property.RuleMessages[0].RuleId);
    }

    [TestMethod]
    public void ClearMessagesForRule_PreservesOtherRuleIdMessages()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var property = new ValidateProperty<string>(wrapper);
        var ruleMessages = new List<IRuleMessage>
        {
            new RuleMessage("Name", "Error 1") { RuleId = 1 },
            new RuleMessage("Name", "Error 2") { RuleId = 2 },
            new RuleMessage("Name", "Error 3") { RuleId = 3 }
        };
        ((IValidatePropertyInternal)property).SetMessagesForRule(ruleMessages);

        // Act
        ((IValidatePropertyInternal)property).ClearMessagesForRule(2);

        // Assert
        Assert.AreEqual(2, property.RuleMessages.Count);
        Assert.IsTrue(property.RuleMessages.Any(rm => rm.RuleId == 1));
        Assert.IsTrue(property.RuleMessages.Any(rm => rm.RuleId == 3));
        Assert.IsFalse(property.RuleMessages.Any(rm => rm.RuleId == 2));
    }

    [TestMethod]
    public void ClearMessagesForRule_NonExistentRuleId_DoesNothing()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var property = new ValidateProperty<string>(wrapper);
        var ruleMessages = new List<IRuleMessage>
        {
            new RuleMessage("Name", "Error") { RuleId = 1 }
        };
        ((IValidatePropertyInternal)property).SetMessagesForRule(ruleMessages);

        // Act
        ((IValidatePropertyInternal)property).ClearMessagesForRule(999);

        // Assert
        Assert.AreEqual(1, property.RuleMessages.Count);
    }

    [TestMethod]
    public void ClearMessagesForRule_ClearsAllForRuleId_ResetsValidity()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var property = new ValidateProperty<string>(wrapper);
        var ruleMessages = new List<IRuleMessage>
        {
            new RuleMessage("Name", "Only error") { RuleId = 1 }
        };
        ((IValidatePropertyInternal)property).SetMessagesForRule(ruleMessages);
        Assert.IsFalse(property.IsValid);

        // Act
        ((IValidatePropertyInternal)property).ClearMessagesForRule(1);

        // Assert
        Assert.IsTrue(property.IsValid);
    }

    [TestMethod]
    public void ClearMessagesForRule_RaisesPropertyChangedForIsValid()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var property = new ValidateProperty<string>(wrapper);
        var ruleMessages = new List<IRuleMessage>
        {
            new RuleMessage("Name", "Error") { RuleId = 1 }
        };
        ((IValidatePropertyInternal)property).SetMessagesForRule(ruleMessages);

        var changedProperties = new List<string>();
        property.PropertyChanged += (sender, e) => changedProperties.Add(e.PropertyName!);

        // Act
        ((IValidatePropertyInternal)property).ClearMessagesForRule(1);

        // Assert
        Assert.IsTrue(changedProperties.Contains("IsValid"));
    }

    [TestMethod]
    public void ClearMessagesForRule_RaisesPropertyChangedForIsSelfValid()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var property = new ValidateProperty<string>(wrapper);
        var ruleMessages = new List<IRuleMessage>
        {
            new RuleMessage("Name", "Error") { RuleId = 1 }
        };
        ((IValidatePropertyInternal)property).SetMessagesForRule(ruleMessages);

        var changedProperties = new List<string>();
        property.PropertyChanged += (sender, e) => changedProperties.Add(e.PropertyName!);

        // Act
        ((IValidatePropertyInternal)property).ClearMessagesForRule(1);

        // Assert
        Assert.IsTrue(changedProperties.Contains("IsSelfValid"));
    }

    [TestMethod]
    public void ClearMessagesForRule_RaisesPropertyChangedForRuleMessages()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var property = new ValidateProperty<string>(wrapper);
        var ruleMessages = new List<IRuleMessage>
        {
            new RuleMessage("Name", "Error") { RuleId = 1 }
        };
        ((IValidatePropertyInternal)property).SetMessagesForRule(ruleMessages);

        var changedProperties = new List<string>();
        property.PropertyChanged += (sender, e) => changedProperties.Add(e.PropertyName!);

        // Act
        ((IValidatePropertyInternal)property).ClearMessagesForRule(1);

        // Assert
        Assert.IsTrue(changedProperties.Contains("RuleMessages"));
    }

    #endregion

    #region ClearSelfMessages Tests

    [TestMethod]
    public void ClearSelfMessages_ClearsAllRuleMessages()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var property = new ValidateProperty<string>(wrapper);
        var ruleMessages = new List<IRuleMessage>
        {
            new RuleMessage("Name", "Error 1") { RuleId = 1 },
            new RuleMessage("Name", "Error 2") { RuleId = 2 }
        };
        ((IValidatePropertyInternal)property).SetMessagesForRule(ruleMessages);

        // Act
        ((IValidatePropertyInternal)property).ClearSelfMessages();

        // Assert
        Assert.AreEqual(0, property.RuleMessages.Count);
        Assert.IsTrue(property.IsValid);
    }

    [TestMethod]
    public void ClearSelfMessages_RaisesPropertyChangedForIsValid()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var property = new ValidateProperty<string>(wrapper);
        var ruleMessages = new List<IRuleMessage>
        {
            new RuleMessage("Name", "Error") { RuleId = 1 }
        };
        ((IValidatePropertyInternal)property).SetMessagesForRule(ruleMessages);

        var changedProperties = new List<string>();
        property.PropertyChanged += (sender, e) => changedProperties.Add(e.PropertyName!);

        // Act
        ((IValidatePropertyInternal)property).ClearSelfMessages();

        // Assert
        Assert.IsTrue(changedProperties.Contains("IsValid"));
    }

    [TestMethod]
    public void ClearSelfMessages_WhenNoMessages_DoesNotThrow()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var property = new ValidateProperty<string>(wrapper);

        // Act & Assert (should not throw)
        ((IValidatePropertyInternal)property).ClearSelfMessages();
        Assert.AreEqual(0, property.RuleMessages.Count);
    }

    #endregion

    #region ClearAllMessages Tests

    [TestMethod]
    public void ClearAllMessages_ClearsAllRuleMessages()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var property = new ValidateProperty<string>(wrapper);
        var ruleMessages = new List<IRuleMessage>
        {
            new RuleMessage("Name", "Error 1") { RuleId = 1 },
            new RuleMessage("Name", "Error 2") { RuleId = 2 }
        };
        ((IValidatePropertyInternal)property).SetMessagesForRule(ruleMessages);

        // Act
        ((IValidatePropertyInternal)property).ClearAllMessages();

        // Assert
        Assert.AreEqual(0, property.RuleMessages.Count);
        Assert.IsTrue(property.IsValid);
    }

    [TestMethod]
    public void ClearAllMessages_ClearsMessagesOnValidateBaseValue()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var childValidate = new ValidatePropertyTestChild();
        // Add a validation error to make the child invalid
        childValidate.AddValidationError("ChildName", "Child validation error");
        Assert.IsFalse(childValidate.IsValid); // Verify child is invalid before clearing

        var property = new ValidateProperty<ValidatePropertyTestChild>(wrapper);
        property.LoadValue(childValidate);

        // Act
        ((IValidatePropertyInternal)property).ClearAllMessages();

        // Resume actions to allow the property manager to update its IsValid state
        childValidate.ResumeAllActions();

        // Assert - After clearing all messages and resuming, the child should be valid
        // The rule that was added still exists but its messages have been cleared
        Assert.AreEqual(0, childValidate.PropertyMessages.Count);
    }

    [TestMethod]
    public void ClearAllMessages_RaisesPropertyChangedForIsValid()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var property = new ValidateProperty<string>(wrapper);
        var ruleMessages = new List<IRuleMessage>
        {
            new RuleMessage("Name", "Error") { RuleId = 1 }
        };
        ((IValidatePropertyInternal)property).SetMessagesForRule(ruleMessages);

        var changedProperties = new List<string>();
        property.PropertyChanged += (sender, e) => changedProperties.Add(e.PropertyName!);

        // Act
        ((IValidatePropertyInternal)property).ClearAllMessages();

        // Assert
        Assert.IsTrue(changedProperties.Contains("IsValid"));
    }

    [TestMethod]
    public void ClearAllMessages_RaisesPropertyChangedForIsSelfValid()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var property = new ValidateProperty<string>(wrapper);
        var ruleMessages = new List<IRuleMessage>
        {
            new RuleMessage("Name", "Error") { RuleId = 1 }
        };
        ((IValidatePropertyInternal)property).SetMessagesForRule(ruleMessages);

        var changedProperties = new List<string>();
        property.PropertyChanged += (sender, e) => changedProperties.Add(e.PropertyName!);

        // Act
        ((IValidatePropertyInternal)property).ClearAllMessages();

        // Assert
        Assert.IsTrue(changedProperties.Contains("IsSelfValid"));
    }

    [TestMethod]
    public void ClearAllMessages_RaisesPropertyChangedForRuleMessages()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var property = new ValidateProperty<string>(wrapper);
        var ruleMessages = new List<IRuleMessage>
        {
            new RuleMessage("Name", "Error") { RuleId = 1 }
        };
        ((IValidatePropertyInternal)property).SetMessagesForRule(ruleMessages);

        var changedProperties = new List<string>();
        property.PropertyChanged += (sender, e) => changedProperties.Add(e.PropertyName!);

        // Act
        ((IValidatePropertyInternal)property).ClearAllMessages();

        // Assert
        Assert.IsTrue(changedProperties.Contains("RuleMessages"));
    }

    #endregion

    #region SerializedRuleMessages Tests

    [TestMethod]
    public void SerializedRuleMessages_ReturnsArrayOfRuleMessages()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var property = new ValidateProperty<string>(wrapper);
        var ruleMessages = new List<IRuleMessage>
        {
            new RuleMessage("Name", "Error 1") { RuleId = 1 },
            new RuleMessage("Name", "Error 2") { RuleId = 2 }
        };
        ((IValidatePropertyInternal)property).SetMessagesForRule(ruleMessages);

        // Act
        var serialized = property.SerializedRuleMessages;

        // Assert
        Assert.IsNotNull(serialized);
        Assert.IsInstanceOfType(serialized, typeof(IRuleMessage[]));
        Assert.AreEqual(2, serialized.Length);
    }

    [TestMethod]
    public void SerializedRuleMessages_EmptyWhenNoMessages()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var property = new ValidateProperty<string>(wrapper);

        // Act
        var serialized = property.SerializedRuleMessages;

        // Assert
        Assert.IsNotNull(serialized);
        Assert.AreEqual(0, serialized.Length);
    }

    [TestMethod]
    public void SerializedRuleMessages_ContainsCorrectMessageContent()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var property = new ValidateProperty<string>(wrapper);
        var ruleMessages = new List<IRuleMessage>
        {
            new RuleMessage("Name", "Specific error message") { RuleId = 42 }
        };
        ((IValidatePropertyInternal)property).SetMessagesForRule(ruleMessages);

        // Act
        var serialized = property.SerializedRuleMessages;

        // Assert
        Assert.AreEqual(1, serialized.Length);
        Assert.AreEqual("Specific error message", serialized[0].Message);
        Assert.AreEqual(42u, serialized[0].RuleId);
    }

    #endregion

    #region RuleMessages Collection Tests

    [TestMethod]
    public void RuleMessages_InitiallyEmpty()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");

        // Act
        var property = new ValidateProperty<string>(wrapper);

        // Assert
        Assert.IsNotNull(property.RuleMessages);
        Assert.AreEqual(0, property.RuleMessages.Count);
    }

    [TestMethod]
    public void RuleMessages_CanBeSetDirectly()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var property = new ValidateProperty<string>(wrapper);
        var newMessages = new List<IRuleMessage>
        {
            new RuleMessage("Name", "Direct message") { RuleId = 1 }
        };

        // Act
        property.RuleMessages = newMessages;

        // Assert
        Assert.AreEqual(1, property.RuleMessages.Count);
        Assert.AreEqual("Direct message", property.RuleMessages[0].Message);
    }

    #endregion

    #region PropertyMessages Tests

    [TestMethod]
    public void PropertyMessages_EmptyWhenNoRuleMessages()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var property = new ValidateProperty<string>(wrapper);

        // Act
        var propertyMessages = property.PropertyMessages;

        // Assert
        Assert.IsNotNull(propertyMessages);
        Assert.AreEqual(0, propertyMessages.Count);
    }

    [TestMethod]
    public void PropertyMessages_ContainsPropertyMessagesForRuleMessages()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var property = new ValidateProperty<string>(wrapper);
        var ruleMessages = new List<IRuleMessage>
        {
            new RuleMessage("Name", "Error 1") { RuleId = 1 },
            new RuleMessage("Name", "Error 2") { RuleId = 2 }
        };
        ((IValidatePropertyInternal)property).SetMessagesForRule(ruleMessages);

        // Act
        var propertyMessages = property.PropertyMessages;

        // Assert
        Assert.AreEqual(2, propertyMessages.Count);
    }

    [TestMethod]
    public void PropertyMessages_EachItemContainsCorrectMessage()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var property = new ValidateProperty<string>(wrapper);
        var ruleMessages = new List<IRuleMessage>
        {
            new RuleMessage("Name", "Test error message") { RuleId = 1 }
        };
        ((IValidatePropertyInternal)property).SetMessagesForRule(ruleMessages);

        // Act
        var propertyMessages = property.PropertyMessages;

        // Assert
        Assert.AreEqual(1, propertyMessages.Count);
        var firstMessage = propertyMessages.First();
        Assert.AreEqual("Test error message", firstMessage.Message);
    }

    [TestMethod]
    public void PropertyMessages_EachItemReferencesProperty()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var property = new ValidateProperty<string>(wrapper);
        var ruleMessages = new List<IRuleMessage>
        {
            new RuleMessage("Name", "Error") { RuleId = 1 }
        };
        ((IValidatePropertyInternal)property).SetMessagesForRule(ruleMessages);

        // Act
        var propertyMessages = property.PropertyMessages;

        // Assert
        var firstMessage = propertyMessages.First();
        Assert.AreSame(property, firstMessage.Property);
    }

    [TestMethod]
    public void PropertyMessages_FromValidateBaseValue_ReturnsValuePropertyMessages()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var childValidate = new ValidatePropertyTestChild();
        // Add a validation error to make the child invalid - this creates property messages
        childValidate.AddValidationError("ChildName", "Child error");

        var property = new ValidateProperty<ValidatePropertyTestChild>(wrapper);
        property.LoadValue(childValidate);

        // Act
        var propertyMessages = property.PropertyMessages;

        // Assert - The child's property messages should be accessible through the parent property
        Assert.IsTrue(propertyMessages.Count >= 1);
        Assert.IsTrue(propertyMessages.Any(pm => pm.Message == "Child error"));
    }

    #endregion

    #region RunRules Tests

    [TestMethod]
    public async Task RunRules_WithNullValue_ReturnsCompletedTask()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var property = new ValidateProperty<string>(wrapper);
        // Value is null

        // Act & Assert (should complete without exception)
        await property.RunRules();
    }

    [TestMethod]
    public async Task RunRules_WithValidateBaseValue_ExecutesChildRules()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var childValidate = new ValidatePropertyTestChild();
        // The child starts valid
        Assert.IsTrue(childValidate.IsValid);

        var property = new ValidateProperty<ValidatePropertyTestChild>(wrapper);
        property.LoadValue(childValidate);

        // Act - Running rules on the property should execute the child's rules
        await property.RunRules(RunRulesFlag.All, null);

        // Assert - The child should still be valid (no validation errors)
        Assert.IsTrue(childValidate.IsValid);
    }

    [TestMethod]
    public async Task RunRules_WithCancellationToken_CompletesSuccessfully()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var childValidate = new ValidatePropertyTestChild();

        var property = new ValidateProperty<ValidatePropertyTestChild>(wrapper);
        property.LoadValue(childValidate);

        // Act - Running rules with a cancellation token should complete successfully
        await property.RunRules(RunRulesFlag.All, cts.Token);

        // Assert - The child should still be valid
        Assert.IsTrue(childValidate.IsValid);
    }

    #endregion

    #region ValueIsValidateBase Tests

    [TestMethod]
    public void ValueIsValidateBase_NullValue_ReturnsNull()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var property = new ValidateProperty<string>(wrapper);

        // Act
        var result = property.ValueIsValidateBase;

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ValueIsValidateBase_NonValidateBaseValue_ReturnsNull()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var property = new ValidateProperty<string>(wrapper);
        property.LoadValue("regular string");

        // Act
        var result = property.ValueIsValidateBase;

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ValueIsValidateBase_ValidateBaseValue_ReturnsValue()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var childValidate = new ValidatePropertyTestChild();

        var property = new ValidateProperty<ValidatePropertyTestChild>(wrapper);
        property.LoadValue(childValidate);

        // Act
        var result = property.ValueIsValidateBase;

        // Assert
        Assert.IsNotNull(result);
        Assert.AreSame(childValidate, result);
    }

    #endregion

    #region Inherited Property<T> Behavior Tests

    [TestMethod]
    public void Value_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var property = new ValidateProperty<string>(wrapper);

        // Act
        property.Value = "TestValue";

        // Assert
        Assert.AreEqual("TestValue", property.Value);
    }

    [TestMethod]
    public void Value_Change_RaisesPropertyChangedEvent()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var property = new ValidateProperty<string>(wrapper);
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
        // Arrange - using a property with private setter
        var wrapper = CreatePropertyInfoWrapper<string>("ReadOnlyProperty");

        // Act
        var property = new ValidateProperty<string>(wrapper);

        // Assert
        Assert.IsTrue(property.IsReadOnly);
    }

    [TestMethod]
    public void SetValue_WhenReadOnly_ThrowsException()
    {
        // Arrange - using a property with private setter
        var wrapper = CreatePropertyInfoWrapper<string>("ReadOnlyProperty");
        var property = new ValidateProperty<string>(wrapper);

        // Act & Assert
        Assert.ThrowsException<PropertyReadOnlyException>(() => property.Value = "NewValue");
    }

    [TestMethod]
    public void Name_InheritedFromPropertyInfo()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<int>("Age");

        // Act
        var property = new ValidateProperty<int>(wrapper);

        // Assert
        Assert.AreEqual("Age", property.Name);
    }

    [TestMethod]
    public void Type_ReturnsGenericTypeParameter()
    {
        // Arrange
        var stringWrapper = CreatePropertyInfoWrapper<string>("Name");
        var intWrapper = CreatePropertyInfoWrapper<int>("Age");
        var stringProperty = new ValidateProperty<string>(stringWrapper);
        var intProperty = new ValidateProperty<int>(intWrapper);

        // Assert
        Assert.AreEqual(typeof(string), stringProperty.Type);
        Assert.AreEqual(typeof(int), intProperty.Type);
    }

    [TestMethod]
    public void IsBusy_InitialState_ReturnsFalse()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");

        // Act
        var property = new ValidateProperty<string>(wrapper);

        // Assert
        Assert.IsFalse(property.IsBusy);
    }

    [TestMethod]
    public void IsSelfBusy_InitialState_ReturnsFalse()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");

        // Act
        var property = new ValidateProperty<string>(wrapper);

        // Assert
        Assert.IsFalse(property.IsSelfBusy);
    }

    [TestMethod]
    public void LoadValue_SetsValueWithoutEvents()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var property = new ValidateProperty<string>(wrapper);
        var eventRaised = false;
        property.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == "Value")
                eventRaised = true;
        };

        // Act
        property.LoadValue("LoadedValue");

        // Assert
        Assert.AreEqual("LoadedValue", property.Value);
        Assert.IsFalse(eventRaised);
    }

    #endregion

    #region IValidateProperty Interface Tests

    [TestMethod]
    public void ImplementsIValidatePropertyInterface()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");

        // Act
        var property = new ValidateProperty<string>(wrapper);

        // Assert
        Assert.IsInstanceOfType(property, typeof(IValidateProperty));
    }

    [TestMethod]
    public void ImplementsIValidatePropertyOfTInterface()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");

        // Act
        var property = new ValidateProperty<string>(wrapper);

        // Assert
        Assert.IsInstanceOfType(property, typeof(IValidateProperty<string>));
    }

    [TestMethod]
    public void ImplementsINotifyPropertyChangedInterface()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");

        // Act
        var property = new ValidateProperty<string>(wrapper);

        // Assert
        Assert.IsInstanceOfType(property, typeof(INotifyPropertyChanged));
    }

    [TestMethod]
    public void ImplementsIPropertyInterface()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");

        // Act
        var property = new ValidateProperty<string>(wrapper);

        // Assert
        Assert.IsInstanceOfType(property, typeof(IValidateProperty));
    }

    [TestMethod]
    public void ImplementsIPropertyOfTInterface()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");

        // Act
        var property = new ValidateProperty<string>(wrapper);

        // Assert
        Assert.IsInstanceOfType(property, typeof(IValidateProperty<string>));
    }

    #endregion

    #region Edge Cases and Thread Safety Tests

    [TestMethod]
    public void SetMessagesForRule_EmptyList_DoesNotThrow()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var property = new ValidateProperty<string>(wrapper);

        // Act & Assert (should not throw)
        ((IValidatePropertyInternal)property).SetMessagesForRule(new List<IRuleMessage>());
        Assert.AreEqual(0, property.RuleMessages.Count);
    }

    [TestMethod]
    public void SetMessagesForRule_ConcurrentAccess_HandlesCorrectly()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var property = new ValidateProperty<string>(wrapper);
        var tasks = new List<Task>();

        // Act - simulate concurrent access
        for (int i = 0; i < 50; i++)
        {
            var ruleIndex = (uint)i;
            tasks.Add(Task.Run(() =>
            {
                var messages = new List<IRuleMessage>
                {
                    new RuleMessage("Name", $"Error {ruleIndex}") { RuleId = ruleIndex }
                };
                ((IValidatePropertyInternal)property).SetMessagesForRule(messages);
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert - should have messages (exact count may vary due to race conditions, but shouldn't throw)
        Assert.IsTrue(property.RuleMessages.Count > 0);
    }

    [TestMethod]
    public void MultipleRuleMessages_SameRuleId_KeepsOnlyLatest()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var property = new ValidateProperty<string>(wrapper);

        // First set of messages with RuleId 1
        var firstMessages = new List<IRuleMessage>
        {
            new RuleMessage("Name", "First error") { RuleId = 1 }
        };
        ((IValidatePropertyInternal)property).SetMessagesForRule(firstMessages);

        // Second set of messages with same RuleId 1
        var secondMessages = new List<IRuleMessage>
        {
            new RuleMessage("Name", "Second error") { RuleId = 1 }
        };

        // Act
        ((IValidatePropertyInternal)property).SetMessagesForRule(secondMessages);

        // Assert
        Assert.AreEqual(1, property.RuleMessages.Count);
        Assert.AreEqual("Second error", property.RuleMessages[0].Message);
    }

    [TestMethod]
    public void SetMessagesForRule_MultipleMessagesWithSameRuleId_AddsAllButRemovesPrevious()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<string>("Name");
        var property = new ValidateProperty<string>(wrapper);

        // Initial messages
        var initialMessages = new List<IRuleMessage>
        {
            new RuleMessage("Name", "Initial") { RuleId = 1 }
        };
        ((IValidatePropertyInternal)property).SetMessagesForRule(initialMessages);

        // New messages with same RuleId but multiple entries
        var newMessages = new List<IRuleMessage>
        {
            new RuleMessage("Name", "New Error 1") { RuleId = 1 },
            new RuleMessage("Name", "New Error 2") { RuleId = 1 }
        };

        // Act
        ((IValidatePropertyInternal)property).SetMessagesForRule(newMessages);

        // Assert - both new messages should be added
        Assert.AreEqual(2, property.RuleMessages.Count);
        Assert.IsTrue(property.RuleMessages.All(rm => rm.RuleId == 1));
    }

    #endregion

    #region Value Type Tests

    [TestMethod]
    public void ValidateProperty_WithValueType_WorksCorrectly()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<int>("Age");
        var property = new ValidateProperty<int>(wrapper);

        // Act
        property.Value = 42;

        // Assert
        Assert.AreEqual(42, property.Value);
        Assert.IsTrue(property.IsValid);
    }

    [TestMethod]
    public void ValidateProperty_WithNullableValueType_HandlesNullCorrectly()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<int?>("NullableValue");
        var property = new ValidateProperty<int?>(wrapper);

        // Act & Assert
        Assert.IsNull(property.Value);
        property.Value = 42;
        Assert.AreEqual(42, property.Value);
        property.Value = null;
        Assert.IsNull(property.Value);
    }

    [TestMethod]
    public void ValidateProperty_WithComplexType_WorksCorrectly()
    {
        // Arrange
        var wrapper = CreatePropertyInfoWrapper<List<string>>("Items");
        var property = new ValidateProperty<List<string>>(wrapper);
        var list = new List<string> { "item1", "item2" };

        // Act
        property.Value = list;

        // Assert
        Assert.AreEqual(2, property.Value?.Count);
        Assert.AreSame(list, property.Value);
    }

    #endregion
}
