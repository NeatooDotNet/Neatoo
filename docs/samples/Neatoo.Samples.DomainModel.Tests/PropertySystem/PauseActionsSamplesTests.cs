using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Samples.DomainModel.PropertySystem;

namespace Neatoo.Samples.DomainModel.Tests.PropertySystem;

/// <summary>
/// Tests for PauseActionsSamples.cs code snippets.
/// </summary>
[TestClass]
[TestCategory("Documentation")]
[TestCategory("PropertySystem")]
public class PauseActionsSamplesTests : SamplesTestBase
{
    [TestInitialize]
    public void TestInitialize()
    {
        InitializeScope();
    }

    #region BulkUpdateDemo Tests

    [TestMethod]
    public void BulkUpdateDemo_Create_InitializesEmpty()
    {
        // Arrange
        var factory = GetRequiredService<IBulkUpdateDemoFactory>();

        // Act
        var demo = factory.Create();

        // Assert
        Assert.IsNotNull(demo);
        Assert.IsNull(demo.FirstName);
        Assert.IsNull(demo.LastName);
    }

    [TestMethod]
    public void BulkUpdateDemo_IsPaused_InitiallyFalse()
    {
        // Arrange
        var factory = GetRequiredService<IBulkUpdateDemoFactory>();

        // Act
        var demo = factory.Create();

        // Assert - IsPaused is available on interface
        Assert.IsFalse(demo.IsPaused);
    }

    [TestMethod]
    public void BulkUpdateDemo_CanSetProperties()
    {
        // Arrange
        var factory = GetRequiredService<IBulkUpdateDemoFactory>();
        var demo = factory.Create();

        // Act
        demo.FirstName = "John";
        demo.LastName = "Doe";
        demo.Email = "john@example.com";
        demo.Age = 30;

        // Assert
        Assert.AreEqual("John", demo.FirstName);
        Assert.AreEqual("Doe", demo.LastName);
        Assert.IsTrue(demo.IsModified);
    }

    [TestMethod]
    public async Task BulkUpdateDemo_ValidData_IsValid()
    {
        // Arrange
        var factory = GetRequiredService<IBulkUpdateDemoFactory>();
        var demo = factory.Create();

        // Act
        demo.FirstName = "John";
        demo.LastName = "Doe";
        demo.Email = "john@example.com";
        demo.Age = 30;
        await demo.RunRules();

        // Assert
        Assert.IsTrue(demo.IsValid);
    }

    [TestMethod]
    public async Task BulkUpdateDemo_InvalidEmail_NotValid()
    {
        // Arrange
        var factory = GetRequiredService<IBulkUpdateDemoFactory>();
        var demo = factory.Create();

        // Act
        demo.FirstName = "John";
        demo.LastName = "Doe";
        demo.Email = "invalid-email"; // Invalid format
        demo.Age = 30;
        await demo.RunRules();

        // Assert
        Assert.IsFalse(demo.IsValid);
    }

    [TestMethod]
    public async Task BulkUpdateDemo_InvalidAge_NotValid()
    {
        // Arrange
        var factory = GetRequiredService<IBulkUpdateDemoFactory>();
        var demo = factory.Create();

        // Act
        demo.FirstName = "John";
        demo.LastName = "Doe";
        demo.Email = "john@example.com";
        demo.Age = 200; // Out of range (0-150)
        await demo.RunRules();

        // Assert
        Assert.IsFalse(demo.IsValid);
    }

    #endregion
}
