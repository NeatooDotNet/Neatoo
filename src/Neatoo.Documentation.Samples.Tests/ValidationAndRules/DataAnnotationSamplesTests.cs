using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Documentation.Samples.ValidationAndRules;

namespace Neatoo.Documentation.Samples.Tests.ValidationAndRules;

/// <summary>
/// Tests for DataAnnotationSamples.cs code snippets.
/// Verifies that all data annotation attributes work correctly.
/// </summary>
[TestClass]
[TestCategory("Documentation")]
[TestCategory("ValidationAndRules")]
public class DataAnnotationSamplesTests : SamplesTestBase
{
    private IDataAnnotationsEntity _target = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        InitializeScope();
        _target = GetRequiredService<IDataAnnotationsEntity>();
    }

    #region Required Attribute Tests

    [TestMethod]
    public async Task Required_NullValue_ReturnsError()
    {
        // Act - run rules to trigger Required validation on null value
        await _target.RunRules();

        // Assert
        var prop = _target[nameof(IDataAnnotationsEntity.FirstName)];
        Assert.IsFalse(prop.IsValid);
    }

    [TestMethod]
    public void Required_EmptyString_ReturnsError()
    {
        // Act
        _target.FirstName = "";

        // Assert
        var prop = _target[nameof(IDataAnnotationsEntity.FirstName)];
        Assert.IsFalse(prop.IsValid);
    }

    [TestMethod]
    public void Required_ValidValue_NoError()
    {
        // Act
        _target.FirstName = "John";

        // Assert
        var prop = _target[nameof(IDataAnnotationsEntity.FirstName)];
        Assert.IsTrue(prop.IsValid);
    }

    [TestMethod]
    public async Task Required_CustomMessage_UsesProvidedMessage()
    {
        // Act - run rules to trigger Required validation
        await _target.RunRules();

        // Assert
        var prop = _target[nameof(IDataAnnotationsEntity.CustomerName)];
        Assert.IsFalse(prop.IsValid);
        Assert.IsTrue(prop.PropertyMessages.Any(m => m.Message == "Customer name is required"));
    }

    #endregion

    #region StringLength Attribute Tests

    [TestMethod]
    public void StringLength_TooLong_ReturnsError()
    {
        // Act
        _target.Description = new string('a', 101); // Max is 100

        // Assert
        var prop = _target[nameof(IDataAnnotationsEntity.Description)];
        Assert.IsFalse(prop.IsValid);
    }

    [TestMethod]
    public void StringLength_TooShort_ReturnsError()
    {
        // Act
        _target.Username = "a"; // Min is 2

        // Assert
        var prop = _target[nameof(IDataAnnotationsEntity.Username)];
        Assert.IsFalse(prop.IsValid);
    }

    [TestMethod]
    public void StringLength_ValidLength_NoError()
    {
        // Act
        _target.Username = "john123"; // Between 2-100

        // Assert
        var prop = _target[nameof(IDataAnnotationsEntity.Username)];
        Assert.IsTrue(prop.IsValid);
    }

    #endregion

    #region Range Attribute Tests

    [TestMethod]
    public void Range_BelowMin_ReturnsError()
    {
        // Act - Set to valid value first, then invalid
        // (default int is 0, so setting to 0 doesn't trigger change)
        _target.Quantity = 50;
        _target.Quantity = 0; // Min is 1

        // Assert
        var prop = _target[nameof(IDataAnnotationsEntity.Quantity)];
        Assert.IsFalse(prop.IsValid);
    }

    [TestMethod]
    public void Range_AboveMax_ReturnsError()
    {
        // Act
        _target.Quantity = 101; // Max is 100

        // Assert
        var prop = _target[nameof(IDataAnnotationsEntity.Quantity)];
        Assert.IsFalse(prop.IsValid);
    }

    [TestMethod]
    public void Range_ValidValue_NoError()
    {
        // Act
        _target.Quantity = 50;

        // Assert
        var prop = _target[nameof(IDataAnnotationsEntity.Quantity)];
        Assert.IsTrue(prop.IsValid);
    }

    [TestMethod]
    public void Range_CustomMessage_UsesProvidedMessage()
    {
        // Act
        _target.Age = -1;

        // Assert
        var prop = _target[nameof(IDataAnnotationsEntity.Age)];
        Assert.IsFalse(prop.IsValid);
        Assert.IsTrue(prop.PropertyMessages.Any(m => m.Message == "Age must be between 0 and 150"));
    }

    #endregion

    #region RegularExpression Attribute Tests

    [TestMethod]
    public void RegularExpression_InvalidFormat_ReturnsError()
    {
        // Act
        _target.ProductCode = "invalid"; // Should be 2 letters + 4 digits

        // Assert
        var prop = _target[nameof(IDataAnnotationsEntity.ProductCode)];
        Assert.IsFalse(prop.IsValid);
    }

    [TestMethod]
    public void RegularExpression_ValidFormat_NoError()
    {
        // Act
        _target.ProductCode = "AB1234";

        // Assert
        var prop = _target[nameof(IDataAnnotationsEntity.ProductCode)];
        Assert.IsTrue(prop.IsValid);
    }

    [TestMethod]
    public void RegularExpression_CustomMessage_UsesProvidedMessage()
    {
        // Act
        _target.Phone = "invalid-phone";

        // Assert
        var prop = _target[nameof(IDataAnnotationsEntity.Phone)];
        Assert.IsFalse(prop.IsValid);
        Assert.IsTrue(prop.PropertyMessages.Any(m => m.Message == "Format: 555-123-4567"));
    }

    [TestMethod]
    public void RegularExpression_ValidPhone_NoError()
    {
        // Act
        _target.Phone = "555-123-4567";

        // Assert
        var prop = _target[nameof(IDataAnnotationsEntity.Phone)];
        Assert.IsTrue(prop.IsValid);
    }

    #endregion

    #region EmailAddress Attribute Tests

    [TestMethod]
    public void EmailAddress_InvalidFormat_ReturnsError()
    {
        // Act
        _target.Email = "not-an-email";

        // Assert
        var prop = _target[nameof(IDataAnnotationsEntity.Email)];
        Assert.IsFalse(prop.IsValid);
    }

    [TestMethod]
    public void EmailAddress_ValidFormat_NoError()
    {
        // Act
        _target.Email = "user@example.com";

        // Assert
        var prop = _target[nameof(IDataAnnotationsEntity.Email)];
        Assert.IsTrue(prop.IsValid);
    }

    #endregion

    #region Combined Attributes Tests

    [TestMethod]
    public void CombinedAttributes_AllValid_NoErrors()
    {
        // Act
        _target.CombinedEmail = "valid@example.com";

        // Assert
        var prop = _target[nameof(IDataAnnotationsEntity.CombinedEmail)];
        Assert.IsTrue(prop.IsValid);
    }

    [TestMethod]
    public void CombinedAttributes_InvalidEmail_ReturnsError()
    {
        // Act
        _target.CombinedEmail = "not-an-email";

        // Assert
        var prop = _target[nameof(IDataAnnotationsEntity.CombinedEmail)];
        Assert.IsFalse(prop.IsValid);
    }

    [TestMethod]
    public async Task CombinedAttributes_Required_ReturnsError()
    {
        // Act - run rules to trigger Required validation
        await _target.RunRules();

        // Assert
        var prop = _target[nameof(IDataAnnotationsEntity.CombinedEmail)];
        Assert.IsFalse(prop.IsValid);
        Assert.IsTrue(prop.PropertyMessages.Any(m => m.Message == "Email is required"));
    }

    #endregion
}
