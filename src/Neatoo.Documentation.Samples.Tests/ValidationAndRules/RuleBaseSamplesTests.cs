using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Documentation.Samples.SampleDomain;

namespace Neatoo.Documentation.Samples.Tests.ValidationAndRules;

/// <summary>
/// Tests for RuleBaseSamples.cs code snippets.
/// Verifies that all rule examples work correctly.
/// </summary>
[TestClass]
[TestCategory("Documentation")]
[TestCategory("ValidationAndRules")]
public class RuleBaseSamplesTests : SamplesTestBase
{
    [TestInitialize]
    public void TestInitialize()
    {
        InitializeScope();
    }

    #region AgeValidationRule Tests

    [TestMethod]
    public void AgeValidationRule_NegativeAge_ReturnsError()
    {
        // Arrange
        var person = GetRequiredService<IPerson>();

        // Act
        person.Age = -5;

        // Assert
        Assert.IsFalse(person.IsValid);
        var ageProp = person[nameof(IPerson.Age)];
        Assert.IsFalse(ageProp.IsValid);
        Assert.IsTrue(ageProp.PropertyMessages.Any(m => m.Message.Contains("negative")));
    }

    [TestMethod]
    public void AgeValidationRule_UnrealisticAge_ReturnsError()
    {
        // Arrange
        var person = GetRequiredService<IPerson>();

        // Act
        person.Age = 200;

        // Assert
        Assert.IsFalse(person.IsValid);
        var ageProp = person[nameof(IPerson.Age)];
        Assert.IsFalse(ageProp.IsValid);
        Assert.IsTrue(ageProp.PropertyMessages.Any(m => m.Message.Contains("unrealistic")));
    }

    [TestMethod]
    public void AgeValidationRule_ValidAge_NoError()
    {
        // Arrange
        var person = GetRequiredService<IPerson>();

        // Act
        person.Age = 30;

        // Assert - Age property should be valid
        var ageProp = person[nameof(IPerson.Age)];
        Assert.IsTrue(ageProp.IsValid, "Age property should be valid for age 30");
    }

    #endregion

    #region UniqueEmailRule Tests

    [TestMethod]
    public async Task UniqueEmailRule_ExistingEmail_ReturnsError()
    {
        // Arrange
        var person = GetRequiredService<IPerson>();

        // Act
        person.Email = "taken@example.com"; // This email exists in MockEmailService
        await person.WaitForTasks();

        // Assert
        var emailProp = person[nameof(IPerson.Email)];
        Assert.IsFalse(emailProp.IsValid);
        Assert.IsTrue(emailProp.PropertyMessages.Any(m => m.Message.Contains("in use")));
    }

    [TestMethod]
    public async Task UniqueEmailRule_NewEmail_NoError()
    {
        // Arrange
        var person = GetRequiredService<IPerson>();

        // Act
        person.Email = "new.user@example.com";
        await person.WaitForTasks();

        // Assert
        var emailProp = person[nameof(IPerson.Email)];
        Assert.IsTrue(emailProp.IsValid, "Email property should be valid for new email");
    }

    [TestMethod]
    public async Task UniqueEmailRule_EmptyEmail_NoError()
    {
        // Arrange
        var person = GetRequiredService<IPerson>();

        // Act
        person.Email = "";
        await person.WaitForTasks();

        // Assert - Empty email should not trigger uniqueness check
        var emailProp = person[nameof(IPerson.Email)];
        // Note: There's also an EmailAddress attribute that validates format
        // The uniqueness check skips empty emails
        Assert.IsTrue(!emailProp.PropertyMessages.Any(m => m.Message.Contains("in use")));
    }

    #endregion

    #region DateRangeRule Tests

    [TestMethod]
    public void DateRangeRule_StartAfterEnd_ReturnsError()
    {
        // Arrange
        var evt = GetRequiredService<IEvent>();

        // Act
        evt.StartDate = DateTime.Today.AddDays(5);
        evt.EndDate = DateTime.Today;

        // Assert
        Assert.IsFalse(evt.IsValid);
        var startProp = evt[nameof(IEvent.StartDate)];
        var endProp = evt[nameof(IEvent.EndDate)];
        Assert.IsFalse(startProp.IsValid);
        Assert.IsFalse(endProp.IsValid);
    }

    [TestMethod]
    public void DateRangeRule_ValidRange_NoError()
    {
        // Arrange
        var evt = GetRequiredService<IEvent>();

        // Act
        evt.StartDate = DateTime.Today;
        evt.EndDate = DateTime.Today.AddDays(5);

        // Assert
        var startProp = evt[nameof(IEvent.StartDate)];
        var endProp = evt[nameof(IEvent.EndDate)];
        Assert.IsTrue(startProp.IsValid);
        Assert.IsTrue(endProp.IsValid);
    }

    #endregion
}
