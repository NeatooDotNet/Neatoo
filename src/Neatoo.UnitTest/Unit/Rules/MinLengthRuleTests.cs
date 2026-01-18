using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Internal;
using Neatoo.RemoteFactory;
using Neatoo.Rules;
using Neatoo.Rules.Rules;
using System.ComponentModel.DataAnnotations;

namespace Neatoo.UnitTest.Unit.Rules;

#region Test Helper Classes

[Factory]
public partial class MinLengthRuleTestTarget : ValidateBase<MinLengthRuleTestTarget>
{
    public MinLengthRuleTestTarget() : base(new ValidateBaseServices<MinLengthRuleTestTarget>())
    {
    }

    public partial string? StringProperty { get; set; }
    public partial List<string>? ListProperty { get; set; }
    public partial string[]? ArrayProperty { get; set; }
}

#endregion

#region Constructor Tests

[TestClass]
public class MinLengthRuleConstructorTests
{
    [TestMethod]
    public void Constructor_WithMinLength_SetsLengthProperty()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<MinLengthRuleTestTarget>(t => t.StringProperty);
        var attribute = new MinLengthAttribute(5);

        // Act
        var rule = new MinLengthRule<MinLengthRuleTestTarget>(triggerProperty, attribute);

        // Assert
        Assert.AreEqual(5, rule.Length);
    }

    [TestMethod]
    public void Constructor_WithoutErrorMessage_UsesDefaultMessage()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<MinLengthRuleTestTarget>(t => t.StringProperty);
        var attribute = new MinLengthAttribute(5);

        // Act
        var rule = new MinLengthRule<MinLengthRuleTestTarget>(triggerProperty, attribute);

        // Assert
        Assert.AreEqual("StringProperty must have at least 5 characters.", rule.ErrorMessage);
    }

    [TestMethod]
    public void Constructor_WithCustomErrorMessage_UsesCustomMessage()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<MinLengthRuleTestTarget>(t => t.StringProperty);
        var attribute = new MinLengthAttribute(5) { ErrorMessage = "Too short!" };

        // Act
        var rule = new MinLengthRule<MinLengthRuleTestTarget>(triggerProperty, attribute);

        // Assert
        Assert.AreEqual("Too short!", rule.ErrorMessage);
    }
}

#endregion

#region String Validation Tests

[TestClass]
public class MinLengthRuleStringValidationTests
{
    private MinLengthRuleTestTarget _target = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _target = new MinLengthRuleTestTarget();
        _target.PauseAllActions();
    }

    [TestMethod]
    public async Task RunRule_NullString_ReturnsNoMessages()
    {
        // Arrange
        _target.StringProperty = null;

        var triggerProperty = new TriggerProperty<MinLengthRuleTestTarget>(t => t.StringProperty);
        var rule = new MinLengthRule<MinLengthRuleTestTarget>(triggerProperty, new MinLengthAttribute(5));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_StringTooShort_ReturnsErrorMessage()
    {
        // Arrange
        _target.StringProperty = "Hi"; // 2 characters

        var triggerProperty = new TriggerProperty<MinLengthRuleTestTarget>(t => t.StringProperty);
        var rule = new MinLengthRule<MinLengthRuleTestTarget>(triggerProperty, new MinLengthAttribute(5));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("StringProperty", result[0].PropertyName);
    }

    [TestMethod]
    public async Task RunRule_StringAtMinLength_ReturnsNoMessages()
    {
        // Arrange
        _target.StringProperty = "Hello"; // 5 characters

        var triggerProperty = new TriggerProperty<MinLengthRuleTestTarget>(t => t.StringProperty);
        var rule = new MinLengthRule<MinLengthRuleTestTarget>(triggerProperty, new MinLengthAttribute(5));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_StringAboveMinLength_ReturnsNoMessages()
    {
        // Arrange
        _target.StringProperty = "Hello World"; // 11 characters

        var triggerProperty = new TriggerProperty<MinLengthRuleTestTarget>(t => t.StringProperty);
        var rule = new MinLengthRule<MinLengthRuleTestTarget>(triggerProperty, new MinLengthAttribute(5));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_EmptyString_WithMinLengthZero_ReturnsNoMessages()
    {
        // Arrange
        _target.StringProperty = "";

        var triggerProperty = new TriggerProperty<MinLengthRuleTestTarget>(t => t.StringProperty);
        var rule = new MinLengthRule<MinLengthRuleTestTarget>(triggerProperty, new MinLengthAttribute(0));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }
}

#endregion

#region Collection Validation Tests

[TestClass]
public class MinLengthRuleCollectionValidationTests
{
    private MinLengthRuleTestTarget _target = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _target = new MinLengthRuleTestTarget();
        _target.PauseAllActions();
    }

    [TestMethod]
    public async Task RunRule_NullList_ReturnsNoMessages()
    {
        // Arrange
        _target.ListProperty = null;

        var triggerProperty = new TriggerProperty<MinLengthRuleTestTarget>(t => t.ListProperty);
        var rule = new MinLengthRule<MinLengthRuleTestTarget>(triggerProperty, new MinLengthAttribute(2));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_ListTooFewItems_ReturnsErrorMessage()
    {
        // Arrange
        _target.ListProperty = new List<string> { "One" };

        var triggerProperty = new TriggerProperty<MinLengthRuleTestTarget>(t => t.ListProperty);
        var rule = new MinLengthRule<MinLengthRuleTestTarget>(triggerProperty, new MinLengthAttribute(2));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("ListProperty", result[0].PropertyName);
    }

    [TestMethod]
    public async Task RunRule_ListAtMinCount_ReturnsNoMessages()
    {
        // Arrange
        _target.ListProperty = new List<string> { "One", "Two" };

        var triggerProperty = new TriggerProperty<MinLengthRuleTestTarget>(t => t.ListProperty);
        var rule = new MinLengthRule<MinLengthRuleTestTarget>(triggerProperty, new MinLengthAttribute(2));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_ListAboveMinCount_ReturnsNoMessages()
    {
        // Arrange
        _target.ListProperty = new List<string> { "One", "Two", "Three" };

        var triggerProperty = new TriggerProperty<MinLengthRuleTestTarget>(t => t.ListProperty);
        var rule = new MinLengthRule<MinLengthRuleTestTarget>(triggerProperty, new MinLengthAttribute(2));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_ArrayTooFewItems_ReturnsErrorMessage()
    {
        // Arrange
        _target.ArrayProperty = new[] { "One" };

        var triggerProperty = new TriggerProperty<MinLengthRuleTestTarget>(t => t.ArrayProperty);
        var rule = new MinLengthRule<MinLengthRuleTestTarget>(triggerProperty, new MinLengthAttribute(2));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(1, result.Count);
    }

    [TestMethod]
    public async Task RunRule_ArrayAtMinLength_ReturnsNoMessages()
    {
        // Arrange
        _target.ArrayProperty = new[] { "One", "Two" };

        var triggerProperty = new TriggerProperty<MinLengthRuleTestTarget>(t => t.ArrayProperty);
        var rule = new MinLengthRule<MinLengthRuleTestTarget>(triggerProperty, new MinLengthAttribute(2));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }
}

#endregion

#region Interface Tests

[TestClass]
public class MinLengthRuleInterfaceTests
{
    [TestMethod]
    public void MinLengthRule_ImplementsIMinLengthRule()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<MinLengthRuleTestTarget>(t => t.StringProperty);
        var rule = new MinLengthRule<MinLengthRuleTestTarget>(triggerProperty, new MinLengthAttribute(5));

        // Assert
        Assert.IsInstanceOfType(rule, typeof(IMinLengthRule));
    }

    [TestMethod]
    public void MinLengthRule_ImplementsIRule()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<MinLengthRuleTestTarget>(t => t.StringProperty);
        var rule = new MinLengthRule<MinLengthRuleTestTarget>(triggerProperty, new MinLengthAttribute(5));

        // Assert
        Assert.IsInstanceOfType(rule, typeof(IRule));
    }
}

#endregion
