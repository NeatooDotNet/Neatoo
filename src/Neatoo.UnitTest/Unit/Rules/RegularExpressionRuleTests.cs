using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Internal;
using Neatoo.RemoteFactory;
using Neatoo.Rules;
using Neatoo.Rules.Rules;
using System.ComponentModel.DataAnnotations;

namespace Neatoo.UnitTest.Unit.Rules;

#region Test Helper Classes

[SuppressFactory]
public class RegularExpressionRuleTestTarget : ValidateBase<RegularExpressionRuleTestTarget>
{
    public RegularExpressionRuleTestTarget() : base(new ValidateBaseServices<RegularExpressionRuleTestTarget>())
    {
    }

    public string? StringProperty { get => Getter<string>(); set => Setter(value); }
    public string? EmailProperty { get => Getter<string>(); set => Setter(value); }
    public string? PhoneProperty { get => Getter<string>(); set => Setter(value); }
}

#endregion

#region Constructor Tests

[TestClass]
public class RegularExpressionRuleConstructorTests
{
    [TestMethod]
    public void Constructor_WithPattern_SetsPatternProperty()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<RegularExpressionRuleTestTarget>(t => t.StringProperty);
        var pattern = @"^[A-Z]{2}\d{4}$";
        var attribute = new RegularExpressionAttribute(pattern);

        // Act
        var rule = new RegularExpressionRule<RegularExpressionRuleTestTarget>(triggerProperty, attribute);

        // Assert
        Assert.AreEqual(pattern, rule.Pattern);
    }

    [TestMethod]
    public void Constructor_WithoutErrorMessage_UsesDefaultMessage()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<RegularExpressionRuleTestTarget>(t => t.StringProperty);
        var attribute = new RegularExpressionAttribute(@"^\d+$");

        // Act
        var rule = new RegularExpressionRule<RegularExpressionRuleTestTarget>(triggerProperty, attribute);

        // Assert
        Assert.AreEqual("StringProperty is not in the correct format.", rule.ErrorMessage);
    }

    [TestMethod]
    public void Constructor_WithCustomErrorMessage_UsesCustomMessage()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<RegularExpressionRuleTestTarget>(t => t.StringProperty);
        var attribute = new RegularExpressionAttribute(@"^\d+$") { ErrorMessage = "Numbers only please!" };

        // Act
        var rule = new RegularExpressionRule<RegularExpressionRuleTestTarget>(triggerProperty, attribute);

        // Assert
        Assert.AreEqual("Numbers only please!", rule.ErrorMessage);
    }
}

#endregion

#region Validation Tests

[TestClass]
public class RegularExpressionRuleValidationTests
{
    private RegularExpressionRuleTestTarget _target = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _target = new RegularExpressionRuleTestTarget();
        _target.PauseAllActions();
    }

    [TestMethod]
    public async Task RunRule_NullString_ReturnsNoMessages()
    {
        // Arrange
        _target.StringProperty = null;

        var triggerProperty = new TriggerProperty<RegularExpressionRuleTestTarget>(t => t.StringProperty);
        var rule = new RegularExpressionRule<RegularExpressionRuleTestTarget>(triggerProperty, new RegularExpressionAttribute(@"^\d+$"));

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

        var triggerProperty = new TriggerProperty<RegularExpressionRuleTestTarget>(t => t.StringProperty);
        var rule = new RegularExpressionRule<RegularExpressionRuleTestTarget>(triggerProperty, new RegularExpressionAttribute(@"^\d+$"));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_MatchingPattern_ReturnsNoMessages()
    {
        // Arrange
        _target.StringProperty = "12345";

        var triggerProperty = new TriggerProperty<RegularExpressionRuleTestTarget>(t => t.StringProperty);
        var rule = new RegularExpressionRule<RegularExpressionRuleTestTarget>(triggerProperty, new RegularExpressionAttribute(@"^\d+$"));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_NonMatchingPattern_ReturnsErrorMessage()
    {
        // Arrange
        _target.StringProperty = "abc123";

        var triggerProperty = new TriggerProperty<RegularExpressionRuleTestTarget>(t => t.StringProperty);
        var rule = new RegularExpressionRule<RegularExpressionRuleTestTarget>(triggerProperty, new RegularExpressionAttribute(@"^\d+$"));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("StringProperty", result[0].PropertyName);
    }

    [TestMethod]
    public async Task RunRule_CodePattern_MatchingValue_ReturnsNoMessages()
    {
        // Arrange - Pattern: 2 uppercase letters followed by 4 digits
        _target.StringProperty = "AB1234";

        var triggerProperty = new TriggerProperty<RegularExpressionRuleTestTarget>(t => t.StringProperty);
        var rule = new RegularExpressionRule<RegularExpressionRuleTestTarget>(triggerProperty, new RegularExpressionAttribute(@"^[A-Z]{2}\d{4}$"));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_CodePattern_LowercaseLetters_ReturnsErrorMessage()
    {
        // Arrange
        _target.StringProperty = "ab1234";

        var triggerProperty = new TriggerProperty<RegularExpressionRuleTestTarget>(t => t.StringProperty);
        var rule = new RegularExpressionRule<RegularExpressionRuleTestTarget>(triggerProperty, new RegularExpressionAttribute(@"^[A-Z]{2}\d{4}$"));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(1, result.Count);
    }

    [TestMethod]
    public async Task RunRule_CodePattern_WrongLength_ReturnsErrorMessage()
    {
        // Arrange
        _target.StringProperty = "AB12345"; // Too many digits

        var triggerProperty = new TriggerProperty<RegularExpressionRuleTestTarget>(t => t.StringProperty);
        var rule = new RegularExpressionRule<RegularExpressionRuleTestTarget>(triggerProperty, new RegularExpressionAttribute(@"^[A-Z]{2}\d{4}$"));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(1, result.Count);
    }
}

#endregion

#region Email Pattern Tests

[TestClass]
public class RegularExpressionRuleEmailPatternTests
{
    private RegularExpressionRuleTestTarget _target = null!;
    private const string EmailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

    [TestInitialize]
    public void TestInitialize()
    {
        _target = new RegularExpressionRuleTestTarget();
        _target.PauseAllActions();
    }

    [TestMethod]
    public async Task RunRule_ValidEmail_ReturnsNoMessages()
    {
        // Arrange
        _target.EmailProperty = "test@example.com";

        var triggerProperty = new TriggerProperty<RegularExpressionRuleTestTarget>(t => t.EmailProperty);
        var rule = new RegularExpressionRule<RegularExpressionRuleTestTarget>(triggerProperty, new RegularExpressionAttribute(EmailPattern));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_EmailWithoutAt_ReturnsErrorMessage()
    {
        // Arrange
        _target.EmailProperty = "testexample.com";

        var triggerProperty = new TriggerProperty<RegularExpressionRuleTestTarget>(t => t.EmailProperty);
        var rule = new RegularExpressionRule<RegularExpressionRuleTestTarget>(triggerProperty, new RegularExpressionAttribute(EmailPattern));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(1, result.Count);
    }

    [TestMethod]
    public async Task RunRule_EmailWithoutDomain_ReturnsErrorMessage()
    {
        // Arrange
        _target.EmailProperty = "test@";

        var triggerProperty = new TriggerProperty<RegularExpressionRuleTestTarget>(t => t.EmailProperty);
        var rule = new RegularExpressionRule<RegularExpressionRuleTestTarget>(triggerProperty, new RegularExpressionAttribute(EmailPattern));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(1, result.Count);
    }
}

#endregion

#region Phone Pattern Tests

[TestClass]
public class RegularExpressionRulePhonePatternTests
{
    private RegularExpressionRuleTestTarget _target = null!;
    private const string PhonePattern = @"^\d{3}-\d{3}-\d{4}$";

    [TestInitialize]
    public void TestInitialize()
    {
        _target = new RegularExpressionRuleTestTarget();
        _target.PauseAllActions();
    }

    [TestMethod]
    public async Task RunRule_ValidPhoneFormat_ReturnsNoMessages()
    {
        // Arrange
        _target.PhoneProperty = "555-123-4567";

        var triggerProperty = new TriggerProperty<RegularExpressionRuleTestTarget>(t => t.PhoneProperty);
        var rule = new RegularExpressionRule<RegularExpressionRuleTestTarget>(triggerProperty, new RegularExpressionAttribute(PhonePattern));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_PhoneWithoutDashes_ReturnsErrorMessage()
    {
        // Arrange
        _target.PhoneProperty = "5551234567";

        var triggerProperty = new TriggerProperty<RegularExpressionRuleTestTarget>(t => t.PhoneProperty);
        var rule = new RegularExpressionRule<RegularExpressionRuleTestTarget>(triggerProperty, new RegularExpressionAttribute(PhonePattern));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(1, result.Count);
    }

    [TestMethod]
    public async Task RunRule_PhoneWithLetters_ReturnsErrorMessage()
    {
        // Arrange
        _target.PhoneProperty = "555-ABC-4567";

        var triggerProperty = new TriggerProperty<RegularExpressionRuleTestTarget>(t => t.PhoneProperty);
        var rule = new RegularExpressionRule<RegularExpressionRuleTestTarget>(triggerProperty, new RegularExpressionAttribute(PhonePattern));

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(1, result.Count);
    }
}

#endregion

#region Interface Tests

[TestClass]
public class RegularExpressionRuleInterfaceTests
{
    [TestMethod]
    public void RegularExpressionRule_ImplementsIRegularExpressionRule()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<RegularExpressionRuleTestTarget>(t => t.StringProperty);
        var rule = new RegularExpressionRule<RegularExpressionRuleTestTarget>(triggerProperty, new RegularExpressionAttribute(@"^\d+$"));

        // Assert
        Assert.IsInstanceOfType(rule, typeof(IRegularExpressionRule));
    }

    [TestMethod]
    public void RegularExpressionRule_ImplementsIRule()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<RegularExpressionRuleTestTarget>(t => t.StringProperty);
        var rule = new RegularExpressionRule<RegularExpressionRuleTestTarget>(triggerProperty, new RegularExpressionAttribute(@"^\d+$"));

        // Assert
        Assert.IsInstanceOfType(rule, typeof(IRule));
    }
}

#endregion
