using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Samples.DomainModel.PropertySystem;

namespace Neatoo.Samples.DomainModel.Tests.PropertySystem;

/// <summary>
/// Tests for PropertyAccessSamples.cs code snippets.
/// </summary>
[TestClass]
[TestCategory("Documentation")]
[TestCategory("PropertySystem")]
public class PropertyAccessSamplesTests : SamplesTestBase
{
    [TestInitialize]
    public void TestInitialize()
    {
        InitializeScope();
    }

    #region PropertyAccessDemo Tests

    [TestMethod]
    public void PropertyAccessDemo_IndexerAccess_ReturnsProperty()
    {
        // Arrange
        var factory = GetRequiredService<IPropertyAccessDemoFactory>();
        var demo = factory.Create();
        demo.Name = "Test";

        // Act - access property via indexer
        var nameProperty = demo[nameof(IPropertyAccessDemo.Name)];

        // Assert
        Assert.IsNotNull(nameProperty);
        Assert.AreEqual("Test", nameProperty.Value);
    }

    [TestMethod]
    public void PropertyAccessDemo_IsModified_TracksChanges()
    {
        // Arrange
        var factory = GetRequiredService<IPropertyAccessDemoFactory>();
        var demo = factory.Create();

        // Act
        demo.Name = "Modified Value";

        // Assert
        var nameProperty = demo[nameof(IPropertyAccessDemo.Name)];
        Assert.IsTrue(nameProperty.IsModified);
    }

    [TestMethod]
    public async Task PropertyAccessDemo_Email_ValidatesFormat()
    {
        // Arrange
        var factory = GetRequiredService<IPropertyAccessDemoFactory>();
        var demo = factory.Create();

        // Act - set invalid email
        demo.Email = "not-an-email";
        await demo.RunRules();

        // Assert
        var emailProperty = demo[nameof(IPropertyAccessDemo.Email)];
        Assert.IsFalse(emailProperty.IsValid);
    }

    [TestMethod]
    public async Task PropertyAccessDemo_Email_ValidFormat_IsValid()
    {
        // Arrange
        var factory = GetRequiredService<IPropertyAccessDemoFactory>();
        var demo = factory.Create();

        // Act
        demo.Email = "test@example.com";
        await demo.RunRules();

        // Assert
        var emailProperty = demo[nameof(IPropertyAccessDemo.Email)];
        Assert.IsTrue(emailProperty.IsValid);
    }

    #endregion

    #region DisplayNameDemo Tests

    [TestMethod]
    public void DisplayNameDemo_DisplayName_ReturnsConfiguredName()
    {
        // Arrange
        var factory = GetRequiredService<IDisplayNameDemoFactory>();
        var demo = factory.Create();

        // Act
        var firstNameProperty = demo[nameof(IDisplayNameDemo.FirstName)];
        var lastNameProperty = demo[nameof(IDisplayNameDemo.LastName)];
        var emailProperty = demo[nameof(IDisplayNameDemo.EmailAddress)];

        // Assert
        Assert.AreEqual("First Name*", firstNameProperty.DisplayName);
        Assert.AreEqual("Last Name*", lastNameProperty.DisplayName);
        Assert.AreEqual("Email Address", emailProperty.DisplayName);
    }

    #endregion

    #region LoadValueDemo Tests

    [TestMethod]
    public void LoadValueDemo_LoadValue_DoesNotMarkModified()
    {
        // Arrange
        var factory = GetRequiredService<ILoadValueDemoFactory>();
        var demo = factory.Create();

        // Act - use LoadValue instead of regular setter
        demo.LoadFromDatabase(
            Guid.NewGuid(),
            "Loaded Name",
            DateTime.UtcNow
        );

        // Assert - LoadValue should NOT mark properties as modified
        Assert.IsFalse(demo[nameof(ILoadValueDemo.Name)].IsModified);
        Assert.IsFalse(demo[nameof(ILoadValueDemo.LastModified)].IsModified);
    }

    [TestMethod]
    public void LoadValueDemo_RegularSetter_MarksModified()
    {
        // Arrange
        var factory = GetRequiredService<ILoadValueDemoFactory>();
        var demo = factory.Create();

        // Act - use regular setter
        demo.Name = "Set via setter";

        // Assert - regular setter DOES mark as modified
        Assert.IsTrue(demo[nameof(ILoadValueDemo.Name)].IsModified);
    }

    #endregion
}
