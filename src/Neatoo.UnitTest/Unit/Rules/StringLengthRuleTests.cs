using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Internal;
using Neatoo.RemoteFactory;
using Neatoo.Rules;
using Neatoo.Rules.Rules;
using System.ComponentModel.DataAnnotations;

namespace Neatoo.UnitTest.Unit.Rules;

#region Test Helper Classes

[Factory]
public partial class StringLengthRuleTestTarget : ValidateBase<StringLengthRuleTestTarget>
{
    public StringLengthRuleTestTarget() : base(new ValidateBaseServices<StringLengthRuleTestTarget>())
    {
    }

    public partial string? StringProperty { get; set; }
}

#endregion

#region Constructor Tests

[TestClass]
public class StringLengthRuleConstructorTests
{
    [TestMethod]
    public void Constructor_WithMaxLengthOnly_UsesDefaultErrorMessage()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<StringLengthRuleTestTarget>(t => t.StringProperty);
        var attribute = new StringLengthAttribute(100);

        // Act
        var rule = new StringLengthRule<StringLengthRuleTestTarget>(triggerProperty, attribute);

        // Assert
        Assert.AreEqual("StringProperty cannot exceed 100 characters.", rule.ErrorMessage);
        Assert.AreEqual(0, rule.MinimumLength);
        Assert.AreEqual(100, rule.MaximumLength);
    }

    [TestMethod]
    public void Constructor_WithMinAndMaxLength_UsesRangeErrorMessage()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<StringLengthRuleTestTarget>(t => t.StringProperty);
        var attribute = new StringLengthAttribute(100) { MinimumLength = 5 };

        // Act
        var rule = new StringLengthRule<StringLengthRuleTestTarget>(triggerProperty, attribute);

        // Assert
        Assert.AreEqual("StringProperty must be between 5 and 100 characters.", rule.ErrorMessage);
        Assert.AreEqual(5, rule.MinimumLength);
        Assert.AreEqual(100, rule.MaximumLength);
    }

    [TestMethod]
    public void Constructor_WithCustomErrorMessage_UsesCustomMessage()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<StringLengthRuleTestTarget>(t => t.StringProperty);
        var attribute = new StringLengthAttribute(100) { ErrorMessage = "Custom error message" };

        // Act
        var rule = new StringLengthRule<StringLengthRuleTestTarget>(triggerProperty, attribute);

        // Assert
        Assert.AreEqual("Custom error message", rule.ErrorMessage);
    }
}

#endregion

#region Validation Tests

[TestClass]
public class StringLengthRuleValidationTests
{
    private StringLengthRuleTestTarget _target = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _target = new StringLengthRuleTestTarget();
        _target.PauseAllActions();
    }

    [TestMethod]
    public async Task RunRule_NullString_ReturnsNoMessages()
    {
        // Arrange
        _target.StringProperty = null;

        var triggerProperty = new TriggerProperty<StringLengthRuleTestTarget>(t => t.StringProperty);
        var rule = new StringLengthRule<StringLengthRuleTestTarget>(triggerProperty, new StringLengthAttribute(10));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_EmptyString_ReturnsNoMessages()
    {
        // Arrange
        _target.StringProperty = string.Empty;

        var triggerProperty = new TriggerProperty<StringLengthRuleTestTarget>(t => t.StringProperty);
        var rule = new StringLengthRule<StringLengthRuleTestTarget>(triggerProperty, new StringLengthAttribute(10));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_StringWithinMaxLength_ReturnsNoMessages()
    {
        // Arrange
        _target.StringProperty = "Hello";

        var triggerProperty = new TriggerProperty<StringLengthRuleTestTarget>(t => t.StringProperty);
        var rule = new StringLengthRule<StringLengthRuleTestTarget>(triggerProperty, new StringLengthAttribute(10));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_StringExceedsMaxLength_ReturnsErrorMessage()
    {
        // Arrange
        _target.StringProperty = "This string is too long";

        var triggerProperty = new TriggerProperty<StringLengthRuleTestTarget>(t => t.StringProperty);
        var rule = new StringLengthRule<StringLengthRuleTestTarget>(triggerProperty, new StringLengthAttribute(10));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("StringProperty", result[0].PropertyName);
    }

    [TestMethod]
    public async Task RunRule_StringAtExactMaxLength_ReturnsNoMessages()
    {
        // Arrange
        _target.StringProperty = "1234567890"; // 10 characters

        var triggerProperty = new TriggerProperty<StringLengthRuleTestTarget>(t => t.StringProperty);
        var rule = new StringLengthRule<StringLengthRuleTestTarget>(triggerProperty, new StringLengthAttribute(10));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_StringBelowMinLength_ReturnsErrorMessage()
    {
        // Arrange
        _target.StringProperty = "Hi"; // 2 characters

        var triggerProperty = new TriggerProperty<StringLengthRuleTestTarget>(t => t.StringProperty);
        var attribute = new StringLengthAttribute(100) { MinimumLength = 5 };
        var rule = new StringLengthRule<StringLengthRuleTestTarget>(triggerProperty, attribute);

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("StringProperty", result[0].PropertyName);
    }

    [TestMethod]
    public async Task RunRule_StringAtExactMinLength_ReturnsNoMessages()
    {
        // Arrange
        _target.StringProperty = "Hello"; // 5 characters

        var triggerProperty = new TriggerProperty<StringLengthRuleTestTarget>(t => t.StringProperty);
        var attribute = new StringLengthAttribute(100) { MinimumLength = 5 };
        var rule = new StringLengthRule<StringLengthRuleTestTarget>(triggerProperty, attribute);

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_StringWithinMinMaxRange_ReturnsNoMessages()
    {
        // Arrange
        _target.StringProperty = "Hello World"; // 11 characters

        var triggerProperty = new TriggerProperty<StringLengthRuleTestTarget>(t => t.StringProperty);
        var attribute = new StringLengthAttribute(20) { MinimumLength = 5 };
        var rule = new StringLengthRule<StringLengthRuleTestTarget>(triggerProperty, attribute);

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }
}

#endregion

#region Interface Tests

[TestClass]
public class StringLengthRuleInterfaceTests
{
    [TestMethod]
    public void StringLengthRule_ImplementsIStringLengthRule()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<StringLengthRuleTestTarget>(t => t.StringProperty);
        var rule = new StringLengthRule<StringLengthRuleTestTarget>(triggerProperty, new StringLengthAttribute(100));

        // Assert
        Assert.IsInstanceOfType(rule, typeof(IStringLengthRule));
    }

    [TestMethod]
    public void StringLengthRule_ImplementsIRule()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<StringLengthRuleTestTarget>(t => t.StringProperty);
        var rule = new StringLengthRule<StringLengthRuleTestTarget>(triggerProperty, new StringLengthAttribute(100));

        // Assert
        Assert.IsInstanceOfType(rule, typeof(IRule));
    }
}

#endregion
