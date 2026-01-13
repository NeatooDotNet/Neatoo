/// <summary>
/// Tests demonstrating how to unit test Neatoo rules.
///
/// KEY PATTERN: Use rule.RunRule(target) to test rules directly.
/// Do NOT create wrapper classes to expose the protected Execute method.
///
/// Snippets in this file:
/// - docs:testing:sync-rule-test
/// - docs:testing:async-rule-test
/// - docs:testing:rule-with-parent-test
/// </summary>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neatoo;
using Neatoo.Samples.DomainModel.Testing;
using Neatoo.Rules;

namespace Neatoo.Samples.DomainModel.Tests.Testing;

[TestClass]
[TestCategory("Documentation")]
[TestCategory("Testing")]
public class TestingRuleSamplesTests
{
    #region Sync Rule Tests

    #region sync-rule-test
    [TestMethod]
    public void RunRule_WhenNameIsEmpty_ReturnsError()
    {
        // Arrange
        var rule = new NameValidationRule();

        var mockTarget = new Mock<INamedEntity>();
        mockTarget.Setup(e => e.Name).Returns(string.Empty);
        mockTarget.Setup(e => e.Id).Returns(1);

        // Act - Use RunRule directly, NOT a wrapper class
        var result = rule.RunRule(mockTarget.Object);

        // Assert
        Assert.IsNotNull(result);
        var messages = result.Result.ToList();
        Assert.AreEqual(1, messages.Count);
        Assert.AreEqual("Name", messages[0].PropertyName);
        Assert.IsTrue(messages[0].Message.Contains("required"));
    }
    #endregion

    [TestMethod]
    public void RunRule_WhenIdIsInvalid_ReturnsError()
    {
        // Arrange
        var rule = new NameValidationRule();

        var mockTarget = new Mock<INamedEntity>();
        mockTarget.Setup(e => e.Name).Returns("Valid Name");
        mockTarget.Setup(e => e.Id).Returns((int?)null);

        // Act
        var result = rule.RunRule(mockTarget.Object);

        // Assert - Id should have error (Name is valid so ElseIf runs)
        var messages = result.Result.ToList();
        Assert.AreEqual(1, messages.Count);
        Assert.AreEqual("Id", messages[0].PropertyName);
    }

    [TestMethod]
    public void RunRule_WhenAllFieldsValid_ReturnsNone()
    {
        // Arrange
        var rule = new NameValidationRule();

        var mockTarget = new Mock<INamedEntity>();
        mockTarget.Setup(e => e.Name).Returns("Valid Name");
        mockTarget.Setup(e => e.Id).Returns(42);

        // Act
        var result = rule.RunRule(mockTarget.Object);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Result.Any(), "Should return no messages for valid data");
    }

    #endregion

    #region Async Rule Tests

    #region async-rule-test
    [TestMethod]
    public async Task RunRule_WhenNameNotUnique_ReturnsError()
    {
        // Arrange - Mock the uniqueness check dependency
        var mockCheckUnique = new Mock<ICheckNameUnique>();
        mockCheckUnique
            .Setup(c => c.IsUnique(It.IsAny<string>(), It.IsAny<int?>()))
            .ReturnsAsync(false); // Name is NOT unique

        var rule = new UniqueNameAsyncRule(mockCheckUnique.Object);

        // Mock the target entity (IEntityBase for IsModified support)
        var mockTarget = new Mock<INamedEntityWithTracking>();
        mockTarget.Setup(e => e.Name).Returns("Duplicate Name");
        mockTarget.Setup(e => e.Id).Returns(1);

        // Mock the property accessor for IsModified check
        var mockProperty = new Mock<IEntityProperty>();
        mockProperty.Setup(p => p.IsModified).Returns(true);
        mockTarget.Setup(e => e[nameof(INamedEntityWithTracking.Name)]).Returns(mockProperty.Object);

        // Act - Use RunRule with await for async rules
        var result = await rule.RunRule(mockTarget.Object);

        // Assert
        var messages = result.ToList();
        Assert.AreEqual(1, messages.Count);
        Assert.AreEqual("Name", messages[0].PropertyName);
        Assert.IsTrue(messages[0].Message.Contains("already exists"));

        // Verify the dependency was called
        mockCheckUnique.Verify(c => c.IsUnique("Duplicate Name", 1), Times.Once);
    }
    #endregion

    [TestMethod]
    public async Task RunRule_WhenNameIsUnique_ReturnsNone()
    {
        // Arrange
        var mockCheckUnique = new Mock<ICheckNameUnique>();
        mockCheckUnique
            .Setup(c => c.IsUnique(It.IsAny<string>(), It.IsAny<int?>()))
            .ReturnsAsync(true); // Name IS unique

        var rule = new UniqueNameAsyncRule(mockCheckUnique.Object);

        var mockTarget = new Mock<INamedEntityWithTracking>();
        mockTarget.Setup(e => e.Name).Returns("Unique Name");
        mockTarget.Setup(e => e.Id).Returns(1);

        var mockProperty = new Mock<IEntityProperty>();
        mockProperty.Setup(p => p.IsModified).Returns(true);
        mockTarget.Setup(e => e[nameof(INamedEntityWithTracking.Name)]).Returns(mockProperty.Object);

        // Act
        var result = await rule.RunRule(mockTarget.Object);

        // Assert
        Assert.IsFalse(result.Any(), "Should return no messages for unique name");
    }

    [TestMethod]
    public async Task RunRule_WhenNameNotModified_SkipsCheck()
    {
        // Arrange
        var mockCheckUnique = new Mock<ICheckNameUnique>();
        var rule = new UniqueNameAsyncRule(mockCheckUnique.Object);

        var mockTarget = new Mock<INamedEntityWithTracking>();
        mockTarget.Setup(e => e.Name).Returns("Existing Name");

        // Property is NOT modified - rule should skip the check
        var mockProperty = new Mock<IEntityProperty>();
        mockProperty.Setup(p => p.IsModified).Returns(false);
        mockTarget.Setup(e => e[nameof(INamedEntityWithTracking.Name)]).Returns(mockProperty.Object);

        // Act
        var result = await rule.RunRule(mockTarget.Object);

        // Assert - Should return None without calling the service
        Assert.IsFalse(result.Any());
        mockCheckUnique.Verify(
            c => c.IsUnique(It.IsAny<string>(), It.IsAny<int?>()),
            Times.Never,
            "Should not call uniqueness check when property not modified");
    }

    #endregion

    #region Rule with Parent Tests

    #region rule-with-parent-test
    [TestMethod]
    public void RunRule_WhenQuantityExceedsParentLimit_ReturnsError()
    {
        // Arrange
        var rule = new QuantityLimitRule();

        // Mock the parent with a quantity limit
        var mockParent = new Mock<IOrderHeader>();
        mockParent.Setup(p => p.MaxQuantityPerLine).Returns(100);

        // Mock the line item with quantity exceeding the limit
        var mockTarget = new Mock<ILineItem>();
        mockTarget.Setup(l => l.Quantity).Returns(150);
        mockTarget.Setup(l => l.Parent).Returns(mockParent.Object);

        // Act
        var result = rule.RunRule(mockTarget.Object);

        // Assert
        var messages = result.Result.ToList();
        Assert.AreEqual(1, messages.Count);
        Assert.AreEqual("Quantity", messages[0].PropertyName);
        Assert.IsTrue(messages[0].Message.Contains("100"));
    }
    #endregion

    [TestMethod]
    public void RunRule_WhenQuantityWithinLimit_ReturnsNone()
    {
        // Arrange
        var rule = new QuantityLimitRule();

        var mockParent = new Mock<IOrderHeader>();
        mockParent.Setup(p => p.MaxQuantityPerLine).Returns(100);

        var mockTarget = new Mock<ILineItem>();
        mockTarget.Setup(l => l.Quantity).Returns(50);
        mockTarget.Setup(l => l.Parent).Returns(mockParent.Object);

        // Act
        var result = rule.RunRule(mockTarget.Object);

        // Assert
        Assert.IsFalse(result.Result.Any(), "Should return no messages when within limit");
    }

    #region testing-null-parent
    [TestMethod]
    public void RunRule_WhenNoParent_ReturnsNone()
    {
        var rule = new QuantityLimitRule();

        var mockTarget = new Mock<ILineItem>();
        mockTarget.Setup(l => l.Quantity).Returns(1000);
        mockTarget.Setup(l => l.Parent).Returns((IOrderHeader?)null);

        var result = rule.RunRule(mockTarget.Object);

        // Rule should handle null parent gracefully
        Assert.IsFalse(result.Result.Any());
    }
    #endregion

    #endregion
}
