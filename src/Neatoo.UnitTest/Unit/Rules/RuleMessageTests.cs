using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Rules;

namespace Neatoo.UnitTest.Unit.Rules;

/// <summary>
/// Unit tests for the RuleMessage record.
/// Tests construction, implicit conversions, and property behavior.
/// </summary>
[TestClass]
public class RuleMessageTests
{
    #region RuleMessage Construction Tests

    [TestMethod]
    public void Constructor_WithPropertyNameOnly_SetsPropertyNameAndNullMessage()
    {
        // Arrange & Act
        var ruleMessage = new RuleMessage("TestProperty");

        // Assert
        Assert.AreEqual("TestProperty", ruleMessage.PropertyName);
        Assert.IsNull(ruleMessage.Message);
        Assert.AreEqual(0u, ruleMessage.RuleIndex);
    }

    [TestMethod]
    public void Constructor_WithPropertyNameAndMessage_SetsBothProperties()
    {
        // Arrange & Act
        var ruleMessage = new RuleMessage("TestProperty", "Error message");

        // Assert
        Assert.AreEqual("TestProperty", ruleMessage.PropertyName);
        Assert.AreEqual("Error message", ruleMessage.Message);
    }

    [TestMethod]
    public void RuleIndex_SetValue_ReturnsSetValue()
    {
        // Arrange
        var ruleMessage = new RuleMessage("TestProperty", "Error");

        // Act
        ruleMessage.RuleIndex = 42u;

        // Assert
        Assert.AreEqual(42u, ruleMessage.RuleIndex);
    }

    #endregion

    #region Implicit Conversion Tests

    [TestMethod]
    public void ImplicitConversion_FromTuple_CreatesRuleMessage()
    {
        // Arrange
        (string name, string errorMessage) tuple = ("PropertyName", "Error occurred");

        // Act
        RuleMessage ruleMessage = tuple;

        // Assert
        Assert.AreEqual("PropertyName", ruleMessage.PropertyName);
        Assert.AreEqual("Error occurred", ruleMessage.Message);
    }

    [TestMethod]
    public void ImplicitConversion_FromTupleLiteral_CreatesRuleMessage()
    {
        // Arrange & Act
        RuleMessage ruleMessage = ("FieldName", "Validation failed");

        // Assert
        Assert.AreEqual("FieldName", ruleMessage.PropertyName);
        Assert.AreEqual("Validation failed", ruleMessage.Message);
    }

    #endregion

    #region Record Equality Tests

    [TestMethod]
    public void Equals_SamePropertyNameAndMessage_ReturnsTrue()
    {
        // Arrange
        var message1 = new RuleMessage("Prop", "Error");
        var message2 = new RuleMessage("Prop", "Error");

        // Act & Assert
        Assert.AreEqual(message1, message2);
    }

    [TestMethod]
    public void Equals_DifferentPropertyName_ReturnsFalse()
    {
        // Arrange
        var message1 = new RuleMessage("Prop1", "Error");
        var message2 = new RuleMessage("Prop2", "Error");

        // Act & Assert
        Assert.AreNotEqual(message1, message2);
    }

    [TestMethod]
    public void Equals_DifferentMessage_ReturnsFalse()
    {
        // Arrange
        var message1 = new RuleMessage("Prop", "Error1");
        var message2 = new RuleMessage("Prop", "Error2");

        // Act & Assert
        Assert.AreNotEqual(message1, message2);
    }

    #endregion
}

/// <summary>
/// Unit tests for the RuleMessages collection class.
/// Tests construction, static properties, and conditional message creation.
/// </summary>
[TestClass]
public class RuleMessagesTests
{
    #region Construction Tests

    [TestMethod]
    public void Constructor_NoParameters_CreatesEmptyCollection()
    {
        // Arrange & Act
        var messages = new RuleMessages();

        // Assert
        Assert.AreEqual(0, messages.Count);
    }

    [TestMethod]
    public void Constructor_WithSingleRuleMessage_ContainsMessage()
    {
        // Arrange
        var ruleMessage = new RuleMessage("Prop", "Error");

        // Act
        var messages = new RuleMessages(ruleMessage);

        // Assert
        Assert.AreEqual(1, messages.Count);
        Assert.AreEqual("Prop", messages[0].PropertyName);
        Assert.AreEqual("Error", messages[0].Message);
    }

    [TestMethod]
    public void Constructor_WithMultipleRuleMessages_ContainsAllMessages()
    {
        // Arrange
        var message1 = new RuleMessage("Prop1", "Error1");
        var message2 = new RuleMessage("Prop2", "Error2");
        var message3 = new RuleMessage("Prop3", "Error3");

        // Act
        var messages = new RuleMessages(message1, message2, message3);

        // Assert
        Assert.AreEqual(3, messages.Count);
    }

    #endregion

    #region None Static Property Tests

    [TestMethod]
    public void None_StaticProperty_ReturnsEmptyCollection()
    {
        // Arrange & Act
        var none = RuleMessages.None;

        // Assert
        Assert.IsNotNull(none);
        Assert.AreEqual(0, none.Count);
    }

    [TestMethod]
    public void None_Interface_ReturnsEmptyCollection()
    {
        // Arrange & Act
        var none = IRuleMessages.None;

        // Assert
        Assert.IsNotNull(none);
        Assert.AreEqual(0, none.Count);
    }

    #endregion

    #region Add Method Tests

    [TestMethod]
    public void Add_WithPropertyNameAndMessage_AddsRuleMessage()
    {
        // Arrange
        var messages = new RuleMessages();

        // Act
        messages.Add("TestProperty", "Test error");

        // Assert
        Assert.AreEqual(1, messages.Count);
        Assert.AreEqual("TestProperty", messages[0].PropertyName);
        Assert.AreEqual("Test error", messages[0].Message);
    }

    [TestMethod]
    public void Add_MultipleMessages_AddsAllMessages()
    {
        // Arrange
        var messages = new RuleMessages();

        // Act
        messages.Add("Prop1", "Error1");
        messages.Add("Prop2", "Error2");

        // Assert
        Assert.AreEqual(2, messages.Count);
    }

    #endregion

    #region If Static Method Tests

    [TestMethod]
    public void If_ConditionTrue_ReturnsCollectionWithMessage()
    {
        // Arrange & Act
        var messages = RuleMessages.If(true, "PropertyName", "Error message");

        // Assert
        Assert.AreEqual(1, messages.Count);
        Assert.AreEqual("PropertyName", messages[0].PropertyName);
        Assert.AreEqual("Error message", messages[0].Message);
    }

    [TestMethod]
    public void If_ConditionFalse_ReturnsEmptyCollection()
    {
        // Arrange & Act
        var messages = RuleMessages.If(false, "PropertyName", "Error message");

        // Assert
        Assert.AreEqual(0, messages.Count);
    }

    [TestMethod]
    public void If_AlwaysReturnsRuleMessagesInstance()
    {
        // Arrange & Act
        var trueResult = RuleMessages.If(true, "Prop", "Msg");
        var falseResult = RuleMessages.If(false, "Prop", "Msg");

        // Assert
        Assert.IsInstanceOfType(trueResult, typeof(RuleMessages));
        Assert.IsInstanceOfType(falseResult, typeof(RuleMessages));
    }

    #endregion
}

/// <summary>
/// Unit tests for the RuleMessagesBuilder extension methods.
/// Tests fluent API for conditional message building.
/// </summary>
[TestClass]
public class RuleMessagesBuilderTests
{
    #region If Extension Method Tests

    [TestMethod]
    public void If_ConditionTrue_AddsMessageToCollection()
    {
        // Arrange
        IRuleMessages messages = new RuleMessages();

        // Act
        var result = messages.If(true, "TestProp", "Test error");

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("TestProp", result[0].PropertyName);
    }

    [TestMethod]
    public void If_ConditionFalse_DoesNotAddMessage()
    {
        // Arrange
        IRuleMessages messages = new RuleMessages();

        // Act
        var result = messages.If(false, "TestProp", "Test error");

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void If_ReturnsSameInstance_ForChaining()
    {
        // Arrange
        IRuleMessages messages = new RuleMessages();

        // Act
        var result = messages.If(true, "Prop", "Error");

        // Assert
        Assert.AreSame(messages, result);
    }

    [TestMethod]
    public void If_ChainedCalls_AddsMultipleMessages()
    {
        // Arrange
        IRuleMessages messages = new RuleMessages();

        // Act
        var result = messages
            .If(true, "Prop1", "Error1")
            .If(true, "Prop2", "Error2")
            .If(false, "Prop3", "Error3")
            .If(true, "Prop4", "Error4");

        // Assert
        Assert.AreEqual(3, result.Count);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void If_NullRuleMessages_ThrowsArgumentNullException()
    {
        // Arrange
        IRuleMessages? messages = null;

        // Act
        messages!.If(true, "Prop", "Error");
    }

    #endregion

    #region ElseIf Extension Method Tests

    [TestMethod]
    public void ElseIf_EmptyCollection_EvaluatesCondition()
    {
        // Arrange
        IRuleMessages messages = new RuleMessages();
        var expressionEvaluated = false;

        // Act
        var result = messages.ElseIf(() =>
        {
            expressionEvaluated = true;
            return true;
        }, "Prop", "Error");

        // Assert
        Assert.IsTrue(expressionEvaluated);
        Assert.AreEqual(1, result.Count);
    }

    [TestMethod]
    public void ElseIf_NonEmptyCollection_ShortCircuitsAndDoesNotEvaluateCondition()
    {
        // Arrange
        IRuleMessages messages = new RuleMessages();
        messages.Add("ExistingProp", "Existing error");
        var expressionEvaluated = false;

        // Act
        var result = messages.ElseIf(() =>
        {
            expressionEvaluated = true;
            return true;
        }, "NewProp", "New error");

        // Assert
        Assert.IsFalse(expressionEvaluated, "Expression should not be evaluated when collection is not empty");
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("ExistingProp", result[0].PropertyName);
    }

    [TestMethod]
    public void ElseIf_EmptyCollectionConditionFalse_DoesNotAddMessage()
    {
        // Arrange
        IRuleMessages messages = new RuleMessages();

        // Act
        var result = messages.ElseIf(() => false, "Prop", "Error");

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void ElseIf_ChainedWithIf_FirstTrueWins()
    {
        // Arrange
        IRuleMessages messages = new RuleMessages();

        // Act
        var result = messages
            .If(true, "FirstProp", "First error")
            .ElseIf(() => true, "SecondProp", "Second error");

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("FirstProp", result[0].PropertyName);
    }

    [TestMethod]
    public void ElseIf_ChainedWithFalseIf_EvaluatesElseIf()
    {
        // Arrange
        IRuleMessages messages = new RuleMessages();

        // Act
        var result = messages
            .If(false, "FirstProp", "First error")
            .ElseIf(() => true, "SecondProp", "Second error");

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("SecondProp", result[0].PropertyName);
    }

    [TestMethod]
    public void ElseIf_MultipleChainedElseIf_FirstTrueWins()
    {
        // Arrange
        IRuleMessages messages = new RuleMessages();
        var secondEvaluated = false;
        var thirdEvaluated = false;

        // Act
        var result = messages
            .If(false, "First", "First error")
            .ElseIf(() => true, "Second", "Second error")
            .ElseIf(() =>
            {
                secondEvaluated = true;
                return true;
            }, "Third", "Third error")
            .ElseIf(() =>
            {
                thirdEvaluated = true;
                return true;
            }, "Fourth", "Fourth error");

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("Second", result[0].PropertyName);
        Assert.IsFalse(secondEvaluated);
        Assert.IsFalse(thirdEvaluated);
    }

    [TestMethod]
    public void ElseIf_ReturnsSameInstance_ForChaining()
    {
        // Arrange
        IRuleMessages messages = new RuleMessages();

        // Act
        var result = messages.ElseIf(() => false, "Prop", "Error");

        // Assert
        Assert.AreSame(messages, result);
    }

    #endregion
}

/// <summary>
/// Unit tests for the PropertyRuleMessageExtension static class.
/// Tests extension methods for creating rule messages from strings and tuples.
/// </summary>
[TestClass]
public class PropertyRuleMessageExtensionTests
{
    #region ClearRuleMessageForProperty Tests

    [TestMethod]
    public void ClearRuleMessageForProperty_ReturnsRuleMessagesWithPropertyNameOnly()
    {
        // Arrange
        var propertyName = "TestProperty";

        // Act
        var result = propertyName.ClearRuleMessageForProperty();

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("TestProperty", result[0].PropertyName);
        Assert.IsNull(result[0].Message);
    }

    [TestMethod]
    public void ClearRuleMessageForProperty_ReturnsIRuleMessages()
    {
        // Arrange & Act
        var result = "PropertyName".ClearRuleMessageForProperty();

        // Assert
        Assert.IsInstanceOfType(result, typeof(IRuleMessages));
    }

    #endregion

    #region RuleMessage Extension Tests

    [TestMethod]
    public void RuleMessage_Extension_CreatesRuleMessageWithNameAndMessage()
    {
        // Arrange
        var propertyName = "TestProperty";
        var message = "Error message";

        // Act
        var result = propertyName.RuleMessage(message);

        // Assert
        Assert.AreEqual("TestProperty", result.PropertyName);
        Assert.AreEqual("Error message", result.Message);
    }

    [TestMethod]
    public void RuleMessage_Extension_ReturnsIRuleMessage()
    {
        // Arrange & Act
        var result = "Prop".RuleMessage("Error");

        // Assert
        Assert.IsInstanceOfType(result, typeof(IRuleMessage));
    }

    #endregion

    #region RuleMessages Extension Tests

    [TestMethod]
    public void RuleMessages_StringExtension_CreatesCollectionWithSingleMessage()
    {
        // Arrange
        var propertyName = "TestProperty";
        var message = "Error message";

        // Act
        var result = propertyName.RuleMessages(message);

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("TestProperty", result[0].PropertyName);
        Assert.AreEqual("Error message", result[0].Message);
    }

    #endregion

    #region AsRuleMessages Tuple Extension Tests

    [TestMethod]
    public void AsRuleMessages_SingleTuple_CreatesCollectionWithOneMessage()
    {
        // Arrange
        var tuple = ("PropertyName", "Error message");

        // Act
        var result = tuple.AsRuleMessages();

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("PropertyName", result[0].PropertyName);
        Assert.AreEqual("Error message", result[0].Message);
    }

    [TestMethod]
    public void AsRuleMessages_SingleTuple_ReturnsIRuleMessages()
    {
        // Arrange & Act
        var result = ("Prop", "Error").AsRuleMessages();

        // Assert
        Assert.IsInstanceOfType(result, typeof(IRuleMessages));
    }

    #endregion

    #region AsRuleMessages Array Extension Tests

    [TestMethod]
    public void AsRuleMessages_TupleArray_CreatesCollectionWithAllMessages()
    {
        // Arrange
        var tuples = new (string propertyName, string errorMessage)[]
        {
            ("Prop1", "Error1"),
            ("Prop2", "Error2"),
            ("Prop3", "Error3")
        };

        // Act
        var result = tuples.AsRuleMessages();

        // Assert
        Assert.AreEqual(3, result.Count);
        Assert.AreEqual("Prop1", result[0].PropertyName);
        Assert.AreEqual("Error1", result[0].Message);
        Assert.AreEqual("Prop2", result[1].PropertyName);
        Assert.AreEqual("Error2", result[1].Message);
        Assert.AreEqual("Prop3", result[2].PropertyName);
        Assert.AreEqual("Error3", result[2].Message);
    }

    [TestMethod]
    public void AsRuleMessages_EmptyArray_ReturnsEmptyCollection()
    {
        // Arrange
        var tuples = Array.Empty<(string propertyName, string errorMessage)>();

        // Act
        var result = tuples.AsRuleMessages();

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void AsRuleMessages_TupleArray_ReturnsIRuleMessages()
    {
        // Arrange
        var tuples = new (string, string)[] { ("Prop", "Error") };

        // Act
        var result = tuples.AsRuleMessages();

        // Assert
        Assert.IsInstanceOfType(result, typeof(IRuleMessages));
    }

    #endregion
}

/// <summary>
/// Unit tests for IRuleMessage interface implementation.
/// </summary>
[TestClass]
public class IRuleMessageInterfaceTests
{
    [TestMethod]
    public void RuleMessage_ImplementsIRuleMessage()
    {
        // Arrange & Act
        var ruleMessage = new RuleMessage("Prop", "Error");

        // Assert
        Assert.IsInstanceOfType(ruleMessage, typeof(IRuleMessage));
    }

    [TestMethod]
    public void IRuleMessage_RuleIndex_CanBeSetViaInternalInterface()
    {
        // Arrange
        var ruleMessage = new RuleMessage("Prop", "Error");

        // Act - RuleIndex setter is on the internal interface
        ((IRuleMessageInternal)ruleMessage).RuleIndex = 100u;

        // Assert - RuleIndex getter is on the public interface
        Assert.AreEqual(100u, ruleMessage.RuleIndex);
    }
}

/// <summary>
/// Unit tests for RuleMessages as IList implementation.
/// </summary>
[TestClass]
public class RuleMessagesListBehaviorTests
{
    [TestMethod]
    public void RuleMessages_ImplementsIListOfIRuleMessage()
    {
        // Arrange & Act
        var messages = new RuleMessages();

        // Assert
        Assert.IsInstanceOfType(messages, typeof(IList<IRuleMessage>));
    }

    [TestMethod]
    public void RuleMessages_Indexer_ReturnsCorrectMessage()
    {
        // Arrange
        var messages = new RuleMessages(
            new RuleMessage("Prop1", "Error1"),
            new RuleMessage("Prop2", "Error2")
        );

        // Act & Assert
        Assert.AreEqual("Prop1", messages[0].PropertyName);
        Assert.AreEqual("Prop2", messages[1].PropertyName);
    }

    [TestMethod]
    public void RuleMessages_Clear_RemovesAllMessages()
    {
        // Arrange
        var messages = new RuleMessages(
            new RuleMessage("Prop1", "Error1"),
            new RuleMessage("Prop2", "Error2")
        );

        // Act
        messages.Clear();

        // Assert
        Assert.AreEqual(0, messages.Count);
    }

    [TestMethod]
    public void RuleMessages_Remove_RemovesSpecificMessage()
    {
        // Arrange
        var message1 = new RuleMessage("Prop1", "Error1");
        var message2 = new RuleMessage("Prop2", "Error2");
        var messages = new RuleMessages(message1, message2);

        // Act
        var removed = messages.Remove(message1);

        // Assert
        Assert.IsTrue(removed);
        Assert.AreEqual(1, messages.Count);
        Assert.AreEqual("Prop2", messages[0].PropertyName);
    }

    [TestMethod]
    public void RuleMessages_Contains_ReturnsTrueForExistingMessage()
    {
        // Arrange
        var message = new RuleMessage("Prop", "Error");
        var messages = new RuleMessages(message);

        // Act & Assert
        Assert.IsTrue(messages.Contains(message));
    }

    [TestMethod]
    public void RuleMessages_Contains_ReturnsFalseForNonExistingMessage()
    {
        // Arrange
        var message = new RuleMessage("Prop", "Error");
        var otherMessage = new RuleMessage("Other", "Different");
        var messages = new RuleMessages(message);

        // Act & Assert
        Assert.IsFalse(messages.Contains(otherMessage));
    }
}
