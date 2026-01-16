using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Internal;
using Neatoo.RemoteFactory;
using Neatoo.Rules;
using Neatoo.Rules.Rules;
using System.ComponentModel.DataAnnotations;

namespace Neatoo.UnitTest.Unit.Rules;

#region Test Helper Classes

[SuppressFactory]
public partial class RangeRuleTestTarget : ValidateBase<RangeRuleTestTarget>
{
    public RangeRuleTestTarget() : base(new ValidateBaseServices<RangeRuleTestTarget>())
    {
    }

    public partial int IntProperty { get; set; }
    public partial int? NullableIntProperty { get; set; }
    public partial double DoubleProperty { get; set; }
    public partial decimal DecimalProperty { get; set; }
    public partial DateTime DateTimeProperty { get; set; }
}

#endregion

#region Constructor Tests

[TestClass]
public class RangeRuleConstructorTests
{
    [TestMethod]
    public void Constructor_WithIntRange_SetsMinAndMax()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<RangeRuleTestTarget>(t => t.IntProperty);
        var attribute = new RangeAttribute(1, 100);

        // Act
        var rule = new RangeRule<RangeRuleTestTarget>(triggerProperty, attribute);

        // Assert
        Assert.AreEqual(1, rule.Minimum);
        Assert.AreEqual(100, rule.Maximum);
    }

    [TestMethod]
    public void Constructor_WithDoubleRange_SetsMinAndMax()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<RangeRuleTestTarget>(t => t.DoubleProperty);
        var attribute = new RangeAttribute(0.5, 99.9);

        // Act
        var rule = new RangeRule<RangeRuleTestTarget>(triggerProperty, attribute);

        // Assert
        Assert.AreEqual(0.5, rule.Minimum);
        Assert.AreEqual(99.9, rule.Maximum);
    }

    [TestMethod]
    public void Constructor_WithoutErrorMessage_UsesDefaultMessage()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<RangeRuleTestTarget>(t => t.IntProperty);
        var attribute = new RangeAttribute(1, 100);

        // Act
        var rule = new RangeRule<RangeRuleTestTarget>(triggerProperty, attribute);

        // Assert
        Assert.AreEqual("IntProperty must be between 1 and 100.", rule.ErrorMessage);
    }

    [TestMethod]
    public void Constructor_WithCustomErrorMessage_UsesCustomMessage()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<RangeRuleTestTarget>(t => t.IntProperty);
        var attribute = new RangeAttribute(1, 100) { ErrorMessage = "Value out of range!" };

        // Act
        var rule = new RangeRule<RangeRuleTestTarget>(triggerProperty, attribute);

        // Assert
        Assert.AreEqual("Value out of range!", rule.ErrorMessage);
    }
}

#endregion

#region Int Validation Tests

[TestClass]
public class RangeRuleIntValidationTests
{
    private RangeRuleTestTarget _target = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _target = new RangeRuleTestTarget();
        _target.PauseAllActions();
    }

    [TestMethod]
    public async Task RunRule_NullableIntNull_ReturnsNoMessages()
    {
        // Arrange
        _target.NullableIntProperty = null;

        var triggerProperty = new TriggerProperty<RangeRuleTestTarget>(t => t.NullableIntProperty);
        var rule = new RangeRule<RangeRuleTestTarget>(triggerProperty, new RangeAttribute(1, 100));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_IntWithinRange_ReturnsNoMessages()
    {
        // Arrange
        _target.IntProperty = 50;

        var triggerProperty = new TriggerProperty<RangeRuleTestTarget>(t => t.IntProperty);
        var rule = new RangeRule<RangeRuleTestTarget>(triggerProperty, new RangeAttribute(1, 100));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_IntAtMinimum_ReturnsNoMessages()
    {
        // Arrange
        _target.IntProperty = 1;

        var triggerProperty = new TriggerProperty<RangeRuleTestTarget>(t => t.IntProperty);
        var rule = new RangeRule<RangeRuleTestTarget>(triggerProperty, new RangeAttribute(1, 100));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_IntAtMaximum_ReturnsNoMessages()
    {
        // Arrange
        _target.IntProperty = 100;

        var triggerProperty = new TriggerProperty<RangeRuleTestTarget>(t => t.IntProperty);
        var rule = new RangeRule<RangeRuleTestTarget>(triggerProperty, new RangeAttribute(1, 100));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_IntBelowMinimum_ReturnsErrorMessage()
    {
        // Arrange
        _target.IntProperty = 0;

        var triggerProperty = new TriggerProperty<RangeRuleTestTarget>(t => t.IntProperty);
        var rule = new RangeRule<RangeRuleTestTarget>(triggerProperty, new RangeAttribute(1, 100));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("IntProperty", result[0].PropertyName);
    }

    [TestMethod]
    public async Task RunRule_IntAboveMaximum_ReturnsErrorMessage()
    {
        // Arrange
        _target.IntProperty = 101;

        var triggerProperty = new TriggerProperty<RangeRuleTestTarget>(t => t.IntProperty);
        var rule = new RangeRule<RangeRuleTestTarget>(triggerProperty, new RangeAttribute(1, 100));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("IntProperty", result[0].PropertyName);
    }

    [TestMethod]
    public async Task RunRule_NegativeRange_WorksCorrectly()
    {
        // Arrange
        _target.IntProperty = -50;

        var triggerProperty = new TriggerProperty<RangeRuleTestTarget>(t => t.IntProperty);
        var rule = new RangeRule<RangeRuleTestTarget>(triggerProperty, new RangeAttribute(-100, -1));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }
}

#endregion

#region Double Validation Tests

[TestClass]
public class RangeRuleDoubleValidationTests
{
    private RangeRuleTestTarget _target = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _target = new RangeRuleTestTarget();
        _target.PauseAllActions();
    }

    [TestMethod]
    public async Task RunRule_DoubleWithinRange_ReturnsNoMessages()
    {
        // Arrange
        _target.DoubleProperty = 50.5;

        var triggerProperty = new TriggerProperty<RangeRuleTestTarget>(t => t.DoubleProperty);
        var rule = new RangeRule<RangeRuleTestTarget>(triggerProperty, new RangeAttribute(0.0, 100.0));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_DoubleBelowMinimum_ReturnsErrorMessage()
    {
        // Arrange
        _target.DoubleProperty = -0.1;

        var triggerProperty = new TriggerProperty<RangeRuleTestTarget>(t => t.DoubleProperty);
        var rule = new RangeRule<RangeRuleTestTarget>(triggerProperty, new RangeAttribute(0.0, 100.0));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(1, result.Count);
    }

    [TestMethod]
    public async Task RunRule_DoubleAboveMaximum_ReturnsErrorMessage()
    {
        // Arrange
        _target.DoubleProperty = 100.1;

        var triggerProperty = new TriggerProperty<RangeRuleTestTarget>(t => t.DoubleProperty);
        var rule = new RangeRule<RangeRuleTestTarget>(triggerProperty, new RangeAttribute(0.0, 100.0));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(1, result.Count);
    }
}

#endregion

#region Decimal Validation Tests

[TestClass]
public class RangeRuleDecimalValidationTests
{
    private RangeRuleTestTarget _target = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _target = new RangeRuleTestTarget();
        _target.PauseAllActions();
    }

    [TestMethod]
    public async Task RunRule_DecimalWithinRange_ReturnsNoMessages()
    {
        // Arrange
        _target.DecimalProperty = 50.50m;

        var triggerProperty = new TriggerProperty<RangeRuleTestTarget>(t => t.DecimalProperty);
        var rule = new RangeRule<RangeRuleTestTarget>(triggerProperty, new RangeAttribute(typeof(decimal), "0.01", "999.99"));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_DecimalAtMinimum_ReturnsNoMessages()
    {
        // Arrange
        _target.DecimalProperty = 0.01m;

        var triggerProperty = new TriggerProperty<RangeRuleTestTarget>(t => t.DecimalProperty);
        var rule = new RangeRule<RangeRuleTestTarget>(triggerProperty, new RangeAttribute(typeof(decimal), "0.01", "999.99"));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_DecimalBelowMinimum_ReturnsErrorMessage()
    {
        // Arrange
        _target.DecimalProperty = 0.001m;

        var triggerProperty = new TriggerProperty<RangeRuleTestTarget>(t => t.DecimalProperty);
        var rule = new RangeRule<RangeRuleTestTarget>(triggerProperty, new RangeAttribute(typeof(decimal), "0.01", "999.99"));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(1, result.Count);
    }

    [TestMethod]
    public async Task RunRule_DecimalAboveMaximum_ReturnsErrorMessage()
    {
        // Arrange
        _target.DecimalProperty = 1000.00m;

        var triggerProperty = new TriggerProperty<RangeRuleTestTarget>(t => t.DecimalProperty);
        var rule = new RangeRule<RangeRuleTestTarget>(triggerProperty, new RangeAttribute(typeof(decimal), "0.01", "999.99"));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(1, result.Count);
    }
}

#endregion

#region DateTime Validation Tests

[TestClass]
public class RangeRuleDateTimeValidationTests
{
    private RangeRuleTestTarget _target = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _target = new RangeRuleTestTarget();
        _target.PauseAllActions();
    }

    [TestMethod]
    public async Task RunRule_DateTimeWithinRange_ReturnsNoMessages()
    {
        // Arrange
        _target.DateTimeProperty = new DateTime(2025, 6, 15);

        var triggerProperty = new TriggerProperty<RangeRuleTestTarget>(t => t.DateTimeProperty);
        var rule = new RangeRule<RangeRuleTestTarget>(triggerProperty, new RangeAttribute(typeof(DateTime), "2020-01-01", "2030-12-31"));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_DateTimeBeforeMinimum_ReturnsErrorMessage()
    {
        // Arrange
        _target.DateTimeProperty = new DateTime(2019, 12, 31);

        var triggerProperty = new TriggerProperty<RangeRuleTestTarget>(t => t.DateTimeProperty);
        var rule = new RangeRule<RangeRuleTestTarget>(triggerProperty, new RangeAttribute(typeof(DateTime), "2020-01-01", "2030-12-31"));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(1, result.Count);
    }

    [TestMethod]
    public async Task RunRule_DateTimeAfterMaximum_ReturnsErrorMessage()
    {
        // Arrange
        _target.DateTimeProperty = new DateTime(2031, 1, 1);

        var triggerProperty = new TriggerProperty<RangeRuleTestTarget>(t => t.DateTimeProperty);
        var rule = new RangeRule<RangeRuleTestTarget>(triggerProperty, new RangeAttribute(typeof(DateTime), "2020-01-01", "2030-12-31"));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(1, result.Count);
    }
}

#endregion

#region Interface Tests

[TestClass]
public class RangeRuleInterfaceTests
{
    [TestMethod]
    public void RangeRule_ImplementsIRangeRule()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<RangeRuleTestTarget>(t => t.IntProperty);
        var rule = new RangeRule<RangeRuleTestTarget>(triggerProperty, new RangeAttribute(1, 100));

        // Assert
        Assert.IsInstanceOfType(rule, typeof(IRangeRule));
    }

    [TestMethod]
    public void RangeRule_ImplementsIRule()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<RangeRuleTestTarget>(t => t.IntProperty);
        var rule = new RangeRule<RangeRuleTestTarget>(triggerProperty, new RangeAttribute(1, 100));

        // Assert
        Assert.IsInstanceOfType(rule, typeof(IRule));
    }
}

#endregion
