using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Documentation.Samples.FactoryOperations;

namespace Neatoo.Documentation.Samples.Tests.FactoryOperations;

/// <summary>
/// Tests for SaveUsageSamples.cs code snippets.
/// </summary>
[TestClass]
[TestCategory("Documentation")]
[TestCategory("FactoryOperations")]
public class SaveUsageSamplesTests : SamplesTestBase
{
    [TestInitialize]
    public void TestInitialize()
    {
        InitializeScope();
    }

    #region SaveableItem Tests

    [TestMethod]
    public void SaveableItem_Create_InitializesId()
    {
        // Arrange
        var factory = GetRequiredService<ISaveableItemFactory>();

        // Act
        var item = factory.Create();

        // Assert
        Assert.AreNotEqual(Guid.Empty, item.Id);
        Assert.IsTrue(item.IsNew);
    }

    [TestMethod]
    public async Task SaveableItem_Save_ReturnsInstance()
    {
        // Arrange
        var factory = GetRequiredService<ISaveableItemFactory>();
        var item = factory.Create();
        item.Name = "Test Item";

        // Act
        var saved = await factory.Save(item);

        // Assert - in local testing, same instance is returned
        // In remote scenarios, this would be a new deserialized instance
        Assert.IsNotNull(saved);
        Assert.AreEqual("Test Item", saved.Name);
    }

    [TestMethod]
    public async Task CorrectSavePattern_ReturnsItem()
    {
        // Arrange
        var factory = GetRequiredService<ISaveableItemFactory>();

        // Act
        var result = await SaveUsageExamples.CorrectSavePattern(factory);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("New Item", result.Name);
    }

    [TestMethod]
    public async Task DeletePattern_MarksDeleted()
    {
        // Arrange
        var factory = GetRequiredService<ISaveableItemFactory>();
        var item = factory.Create();
        item.Name = "To Delete";

        // Save first to make it not new
        item = await factory.Save(item);

        // Act
        await SaveUsageExamples.DeletePattern(item, factory);

        // Assert - after delete, item was marked deleted
        Assert.IsTrue(item.IsDeleted);
    }

    [TestMethod]
    public void UnDeletePattern_RestoresItem()
    {
        // Arrange
        var factory = GetRequiredService<ISaveableItemFactory>();
        var item = factory.Create();
        item.Name = "Maybe Delete";

        // Act
        SaveUsageExamples.UnDeletePattern(item);

        // Assert
        Assert.IsFalse(item.IsDeleted);
    }

    #endregion
}
