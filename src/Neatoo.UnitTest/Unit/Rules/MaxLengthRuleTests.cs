using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Internal;
using Neatoo.RemoteFactory;
using Neatoo.Rules;
using Neatoo.Rules.Rules;
using System.ComponentModel.DataAnnotations;

namespace Neatoo.UnitTest.Unit.Rules;

#region Test Helper Classes

[Factory]
public partial class MaxLengthRuleTestTarget : ValidateBase<MaxLengthRuleTestTarget>
{
    public MaxLengthRuleTestTarget() : base(new ValidateBaseServices<MaxLengthRuleTestTarget>())
    {
    }

    public partial string? StringProperty { get; set; }
    public partial List<string>? ListProperty { get; set; }
    public partial string[]? ArrayProperty { get; set; }
}

#endregion

#region Constructor Tests

[TestClass]
public class MaxLengthRuleConstructorTests
{
    [TestMethod]
    public void Constructor_WithMaxLength_SetsLengthProperty()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<MaxLengthRuleTestTarget>(t => t.StringProperty);
        var attribute = new MaxLengthAttribute(100);

        // Act
        var rule = new MaxLengthRule<MaxLengthRuleTestTarget>(triggerProperty, attribute);

        // Assert
        Assert.AreEqual(100, rule.Length);
    }

    [TestMethod]
    public void Constructor_WithoutErrorMessage_UsesDefaultMessage()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<MaxLengthRuleTestTarget>(t => t.StringProperty);
        var attribute = new MaxLengthAttribute(100);

        // Act
        var rule = new MaxLengthRule<MaxLengthRuleTestTarget>(triggerProperty, attribute);

        // Assert
        Assert.AreEqual("StringProperty cannot exceed 100 characters.", rule.ErrorMessage);
    }

    [TestMethod]
    public void Constructor_WithCustomErrorMessage_UsesCustomMessage()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<MaxLengthRuleTestTarget>(t => t.StringProperty);
        var attribute = new MaxLengthAttribute(100) { ErrorMessage = "Too long!" };

        // Act
        var rule = new MaxLengthRule<MaxLengthRuleTestTarget>(triggerProperty, attribute);

        // Assert
        Assert.AreEqual("Too long!", rule.ErrorMessage);
    }
}

#endregion

#region String Validation Tests

[TestClass]
public class MaxLengthRuleStringValidationTests
{
    private MaxLengthRuleTestTarget _target = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _target = new MaxLengthRuleTestTarget();
        _target.PauseAllActions();
    }

    [TestMethod]
    public async Task RunRule_NullString_ReturnsNoMessages()
    {
        // Arrange
        _target.StringProperty = null;

        var triggerProperty = new TriggerProperty<MaxLengthRuleTestTarget>(t => t.StringProperty);
        var rule = new MaxLengthRule<MaxLengthRuleTestTarget>(triggerProperty, new MaxLengthAttribute(10));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_StringTooLong_ReturnsErrorMessage()
    {
        // Arrange
        _target.StringProperty = "This string is way too long";

        var triggerProperty = new TriggerProperty<MaxLengthRuleTestTarget>(t => t.StringProperty);
        var rule = new MaxLengthRule<MaxLengthRuleTestTarget>(triggerProperty, new MaxLengthAttribute(10));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("StringProperty", result[0].PropertyName);
    }

    [TestMethod]
    public async Task RunRule_StringAtMaxLength_ReturnsNoMessages()
    {
        // Arrange
        _target.StringProperty = "1234567890"; // 10 characters

        var triggerProperty = new TriggerProperty<MaxLengthRuleTestTarget>(t => t.StringProperty);
        var rule = new MaxLengthRule<MaxLengthRuleTestTarget>(triggerProperty, new MaxLengthAttribute(10));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_StringBelowMaxLength_ReturnsNoMessages()
    {
        // Arrange
        _target.StringProperty = "Hello"; // 5 characters

        var triggerProperty = new TriggerProperty<MaxLengthRuleTestTarget>(t => t.StringProperty);
        var rule = new MaxLengthRule<MaxLengthRuleTestTarget>(triggerProperty, new MaxLengthAttribute(10));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_EmptyString_ReturnsNoMessages()
    {
        // Arrange
        _target.StringProperty = "";

        var triggerProperty = new TriggerProperty<MaxLengthRuleTestTarget>(t => t.StringProperty);
        var rule = new MaxLengthRule<MaxLengthRuleTestTarget>(triggerProperty, new MaxLengthAttribute(10));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }
}

#endregion

#region Collection Validation Tests

[TestClass]
public class MaxLengthRuleCollectionValidationTests
{
    private MaxLengthRuleTestTarget _target = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _target = new MaxLengthRuleTestTarget();
        _target.PauseAllActions();
    }

    [TestMethod]
    public async Task RunRule_NullList_ReturnsNoMessages()
    {
        // Arrange
        _target.ListProperty = null;

        var triggerProperty = new TriggerProperty<MaxLengthRuleTestTarget>(t => t.ListProperty);
        var rule = new MaxLengthRule<MaxLengthRuleTestTarget>(triggerProperty, new MaxLengthAttribute(3));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_ListTooManyItems_ReturnsErrorMessage()
    {
        // Arrange
        _target.ListProperty = new List<string> { "One", "Two", "Three", "Four" };

        var triggerProperty = new TriggerProperty<MaxLengthRuleTestTarget>(t => t.ListProperty);
        var rule = new MaxLengthRule<MaxLengthRuleTestTarget>(triggerProperty, new MaxLengthAttribute(3));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("ListProperty", result[0].PropertyName);
    }

    [TestMethod]
    public async Task RunRule_ListAtMaxCount_ReturnsNoMessages()
    {
        // Arrange
        _target.ListProperty = new List<string> { "One", "Two", "Three" };

        var triggerProperty = new TriggerProperty<MaxLengthRuleTestTarget>(t => t.ListProperty);
        var rule = new MaxLengthRule<MaxLengthRuleTestTarget>(triggerProperty, new MaxLengthAttribute(3));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_ListBelowMaxCount_ReturnsNoMessages()
    {
        // Arrange
        _target.ListProperty = new List<string> { "One", "Two" };

        var triggerProperty = new TriggerProperty<MaxLengthRuleTestTarget>(t => t.ListProperty);
        var rule = new MaxLengthRule<MaxLengthRuleTestTarget>(triggerProperty, new MaxLengthAttribute(3));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_ArrayTooManyItems_ReturnsErrorMessage()
    {
        // Arrange
        _target.ArrayProperty = new[] { "One", "Two", "Three", "Four" };

        var triggerProperty = new TriggerProperty<MaxLengthRuleTestTarget>(t => t.ArrayProperty);
        var rule = new MaxLengthRule<MaxLengthRuleTestTarget>(triggerProperty, new MaxLengthAttribute(3));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(1, result.Count);
    }

    [TestMethod]
    public async Task RunRule_ArrayAtMaxLength_ReturnsNoMessages()
    {
        // Arrange
        _target.ArrayProperty = new[] { "One", "Two", "Three" };

        var triggerProperty = new TriggerProperty<MaxLengthRuleTestTarget>(t => t.ArrayProperty);
        var rule = new MaxLengthRule<MaxLengthRuleTestTarget>(triggerProperty, new MaxLengthAttribute(3));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_EmptyList_ReturnsNoMessages()
    {
        // Arrange
        _target.ListProperty = new List<string>();

        var triggerProperty = new TriggerProperty<MaxLengthRuleTestTarget>(t => t.ListProperty);
        var rule = new MaxLengthRule<MaxLengthRuleTestTarget>(triggerProperty, new MaxLengthAttribute(3));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }
}

#endregion

#region Interface Tests

[TestClass]
public class MaxLengthRuleInterfaceTests
{
    [TestMethod]
    public void MaxLengthRule_ImplementsIMaxLengthRule()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<MaxLengthRuleTestTarget>(t => t.StringProperty);
        var rule = new MaxLengthRule<MaxLengthRuleTestTarget>(triggerProperty, new MaxLengthAttribute(100));

        // Assert
        Assert.IsInstanceOfType(rule, typeof(IMaxLengthRule));
    }

    [TestMethod]
    public void MaxLengthRule_ImplementsIRule()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<MaxLengthRuleTestTarget>(t => t.StringProperty);
        var rule = new MaxLengthRule<MaxLengthRuleTestTarget>(triggerProperty, new MaxLengthAttribute(100));

        // Assert
        Assert.IsInstanceOfType(rule, typeof(IRule));
    }
}

#endregion
