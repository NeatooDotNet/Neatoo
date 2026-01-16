using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Internal;
using Neatoo.RemoteFactory;
using Neatoo.Rules;
using Neatoo.Rules.Rules;
using System.ComponentModel.DataAnnotations;

namespace Neatoo.UnitTest.Unit.Rules;

#region Test Helper Classes

[SuppressFactory]
public partial class EmailAddressRuleTestTarget : ValidateBase<EmailAddressRuleTestTarget>
{
    public EmailAddressRuleTestTarget() : base(new ValidateBaseServices<EmailAddressRuleTestTarget>())
    {
    }

    public partial string? EmailProperty { get; set; }
}

#endregion

#region Constructor Tests

[TestClass]
public class EmailAddressRuleConstructorTests
{
    [TestMethod]
    public void Constructor_WithoutErrorMessage_UsesDefaultMessage()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<EmailAddressRuleTestTarget>(t => t.EmailProperty);
        var attribute = new EmailAddressAttribute();

        // Act
        var rule = new EmailAddressRule<EmailAddressRuleTestTarget>(triggerProperty, attribute);

        // Assert
        Assert.AreEqual("EmailProperty is not a valid email address.", rule.ErrorMessage);
    }

    [TestMethod]
    public void Constructor_WithCustomErrorMessage_UsesCustomMessage()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<EmailAddressRuleTestTarget>(t => t.EmailProperty);
        var attribute = new EmailAddressAttribute { ErrorMessage = "Please enter a valid email!" };

        // Act
        var rule = new EmailAddressRule<EmailAddressRuleTestTarget>(triggerProperty, attribute);

        // Assert
        Assert.AreEqual("Please enter a valid email!", rule.ErrorMessage);
    }
}

#endregion

#region Valid Email Tests

[TestClass]
public class EmailAddressRuleValidEmailTests
{
    private EmailAddressRuleTestTarget _target = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _target = new EmailAddressRuleTestTarget();
        _target.PauseAllActions();
    }

    [TestMethod]
    public async Task RunRule_NullEmail_ReturnsNoMessages()
    {
        // Arrange
        _target.EmailProperty = null;

        var triggerProperty = new TriggerProperty<EmailAddressRuleTestTarget>(t => t.EmailProperty);
        var rule = new EmailAddressRule<EmailAddressRuleTestTarget>(triggerProperty, new EmailAddressAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_EmptyEmail_ReturnsNoMessages()
    {
        // Arrange
        _target.EmailProperty = string.Empty;

        var triggerProperty = new TriggerProperty<EmailAddressRuleTestTarget>(t => t.EmailProperty);
        var rule = new EmailAddressRule<EmailAddressRuleTestTarget>(triggerProperty, new EmailAddressAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_SimpleValidEmail_ReturnsNoMessages()
    {
        // Arrange
        _target.EmailProperty = "test@example.com";

        var triggerProperty = new TriggerProperty<EmailAddressRuleTestTarget>(t => t.EmailProperty);
        var rule = new EmailAddressRule<EmailAddressRuleTestTarget>(triggerProperty, new EmailAddressAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_EmailWithSubdomain_ReturnsNoMessages()
    {
        // Arrange
        _target.EmailProperty = "user@mail.example.com";

        var triggerProperty = new TriggerProperty<EmailAddressRuleTestTarget>(t => t.EmailProperty);
        var rule = new EmailAddressRule<EmailAddressRuleTestTarget>(triggerProperty, new EmailAddressAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_EmailWithPlusAddressing_ReturnsNoMessages()
    {
        // Arrange
        _target.EmailProperty = "user+tag@example.com";

        var triggerProperty = new TriggerProperty<EmailAddressRuleTestTarget>(t => t.EmailProperty);
        var rule = new EmailAddressRule<EmailAddressRuleTestTarget>(triggerProperty, new EmailAddressAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_EmailWithDots_ReturnsNoMessages()
    {
        // Arrange
        _target.EmailProperty = "first.last@example.com";

        var triggerProperty = new TriggerProperty<EmailAddressRuleTestTarget>(t => t.EmailProperty);
        var rule = new EmailAddressRule<EmailAddressRuleTestTarget>(triggerProperty, new EmailAddressAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_EmailWithNumbers_ReturnsNoMessages()
    {
        // Arrange
        _target.EmailProperty = "user123@example123.com";

        var triggerProperty = new TriggerProperty<EmailAddressRuleTestTarget>(t => t.EmailProperty);
        var rule = new EmailAddressRule<EmailAddressRuleTestTarget>(triggerProperty, new EmailAddressAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_EmailWithHyphens_ReturnsNoMessages()
    {
        // Arrange
        _target.EmailProperty = "user-name@example-domain.com";

        var triggerProperty = new TriggerProperty<EmailAddressRuleTestTarget>(t => t.EmailProperty);
        var rule = new EmailAddressRule<EmailAddressRuleTestTarget>(triggerProperty, new EmailAddressAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_EmailWithUnderscore_ReturnsNoMessages()
    {
        // Arrange
        _target.EmailProperty = "user_name@example.com";

        var triggerProperty = new TriggerProperty<EmailAddressRuleTestTarget>(t => t.EmailProperty);
        var rule = new EmailAddressRule<EmailAddressRuleTestTarget>(triggerProperty, new EmailAddressAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }
}

#endregion

#region Invalid Email Tests

[TestClass]
public class EmailAddressRuleInvalidEmailTests
{
    private EmailAddressRuleTestTarget _target = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _target = new EmailAddressRuleTestTarget();
        _target.PauseAllActions();
    }

    [TestMethod]
    public async Task RunRule_EmailWithoutAt_ReturnsErrorMessage()
    {
        // Arrange
        _target.EmailProperty = "testexample.com";

        var triggerProperty = new TriggerProperty<EmailAddressRuleTestTarget>(t => t.EmailProperty);
        var rule = new EmailAddressRule<EmailAddressRuleTestTarget>(triggerProperty, new EmailAddressAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("EmailProperty", result[0].PropertyName);
    }

    [TestMethod]
    public async Task RunRule_EmailWithoutDomain_ReturnsErrorMessage()
    {
        // Arrange
        _target.EmailProperty = "test@";

        var triggerProperty = new TriggerProperty<EmailAddressRuleTestTarget>(t => t.EmailProperty);
        var rule = new EmailAddressRule<EmailAddressRuleTestTarget>(triggerProperty, new EmailAddressAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(1, result.Count);
    }

    [TestMethod]
    public async Task RunRule_EmailWithoutLocalPart_ReturnsErrorMessage()
    {
        // Arrange
        _target.EmailProperty = "@example.com";

        var triggerProperty = new TriggerProperty<EmailAddressRuleTestTarget>(t => t.EmailProperty);
        var rule = new EmailAddressRule<EmailAddressRuleTestTarget>(triggerProperty, new EmailAddressAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(1, result.Count);
    }

    [TestMethod]
    public async Task RunRule_EmailWithMultipleAt_ReturnsErrorMessage()
    {
        // Arrange
        _target.EmailProperty = "test@@example.com";

        var triggerProperty = new TriggerProperty<EmailAddressRuleTestTarget>(t => t.EmailProperty);
        var rule = new EmailAddressRule<EmailAddressRuleTestTarget>(triggerProperty, new EmailAddressAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(1, result.Count);
    }

    [TestMethod]
    public async Task RunRule_EmailWithSpaces_ReturnsErrorMessage()
    {
        // Arrange
        _target.EmailProperty = "test @example.com";

        var triggerProperty = new TriggerProperty<EmailAddressRuleTestTarget>(t => t.EmailProperty);
        var rule = new EmailAddressRule<EmailAddressRuleTestTarget>(triggerProperty, new EmailAddressAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(1, result.Count);
    }

    [TestMethod]
    public async Task RunRule_EmailWithoutTld_ReturnsNoMessages()
    {
        // Arrange - "test@example" is technically valid per RFC (local domain names allowed)
        _target.EmailProperty = "test@example";

        var triggerProperty = new TriggerProperty<EmailAddressRuleTestTarget>(t => t.EmailProperty);
        var rule = new EmailAddressRule<EmailAddressRuleTestTarget>(triggerProperty, new EmailAddressAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert - MailAddress.TryCreate accepts this as valid
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_PlainText_ReturnsErrorMessage()
    {
        // Arrange
        _target.EmailProperty = "not an email at all";

        var triggerProperty = new TriggerProperty<EmailAddressRuleTestTarget>(t => t.EmailProperty);
        var rule = new EmailAddressRule<EmailAddressRuleTestTarget>(triggerProperty, new EmailAddressAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(1, result.Count);
    }

    [TestMethod]
    public async Task RunRule_EmailWithDisplayName_ReturnsErrorMessage()
    {
        // Arrange - MailAddress accepts this, but EmailAddressAttribute should reject
        _target.EmailProperty = "John Doe <john@example.com>";

        var triggerProperty = new TriggerProperty<EmailAddressRuleTestTarget>(t => t.EmailProperty);
        var rule = new EmailAddressRule<EmailAddressRuleTestTarget>(triggerProperty, new EmailAddressAttribute());

        // Act
        var result = await rule.RunRule(_target);

        // Assert
        Assert.AreEqual(1, result.Count);
    }
}

#endregion

#region Interface Tests

[TestClass]
public class EmailAddressRuleInterfaceTests
{
    [TestMethod]
    public void EmailAddressRule_ImplementsIEmailAddressRule()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<EmailAddressRuleTestTarget>(t => t.EmailProperty);
        var rule = new EmailAddressRule<EmailAddressRuleTestTarget>(triggerProperty, new EmailAddressAttribute());

        // Assert
        Assert.IsInstanceOfType(rule, typeof(IEmailAddressRule));
    }

    [TestMethod]
    public void EmailAddressRule_ImplementsIRule()
    {
        // Arrange
        var triggerProperty = new TriggerProperty<EmailAddressRuleTestTarget>(t => t.EmailProperty);
        var rule = new EmailAddressRule<EmailAddressRuleTestTarget>(triggerProperty, new EmailAddressAttribute());

        // Assert
        Assert.IsInstanceOfType(rule, typeof(IRule));
    }
}

#endregion
