using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Documentation.Samples.FactoryOperations;

namespace Neatoo.Documentation.Samples.Tests.FactoryOperations;

/// <summary>
/// Tests for SaveOperationSamples.cs code snippets.
/// </summary>
[TestClass]
[TestCategory("Documentation")]
[TestCategory("FactoryOperations")]
public class SaveOperationSamplesTests : SamplesTestBase
{
    [TestInitialize]
    public void TestInitialize()
    {
        InitializeScope();
    }

    #region Insert Tests

    [TestMethod]
    public async Task InventoryItem_Insert_ValidItem_Succeeds()
    {
        // Arrange
        var factory = GetRequiredService<IInventoryItemFactory>();
        var db = GetRequiredService<IInventoryDb>();

        var item = factory.Create();
        item.Name = "New Widget";
        item.Quantity = 10;

        // Act
        await factory.Save(item);

        // Assert
        Assert.AreEqual(1, db.Items.Count);
        Assert.AreEqual("New Widget", db.Items[0].Name);
        Assert.AreEqual(10, db.Items[0].Quantity);
    }

    [TestMethod]
    public async Task InventoryItem_Insert_InvalidItem_DoesNotPersist()
    {
        // Arrange
        var factory = GetRequiredService<IInventoryItemFactory>();
        var db = GetRequiredService<IInventoryDb>();

        var item = factory.Create();
        // Name is required but not set

        // Act
        await factory.Save(item);

        // Assert - validation failed, nothing persisted
        Assert.AreEqual(0, db.Items.Count);
        Assert.IsFalse(item.IsSavable);
    }

    #endregion

    #region Update Tests

    [TestMethod]
    public async Task InventoryItem_Update_ModifiesExisting()
    {
        // Arrange
        var factory = GetRequiredService<IInventoryItemFactory>();
        var db = GetRequiredService<IInventoryDb>();

        // Create and insert initial item
        var originalId = Guid.NewGuid();
        db.Items.Add(new InventoryItemEntity
        {
            Id = originalId,
            Name = "Original",
            Quantity = 5,
            LastUpdated = DateTime.UtcNow.AddDays(-1)
        });

        // Fetch the item
        var entity = db.Items[0];
        var item = factory.Fetch(entity);

        // Act - modify and save
        item.Quantity = 15;
        await factory.Save(item);

        // Assert
        Assert.AreEqual(1, db.Items.Count);
        Assert.AreEqual(15, db.Items[0].Quantity);
        Assert.AreEqual("Original", db.Items[0].Name); // Unchanged
    }

    [TestMethod]
    public async Task InventoryItem_Update_OnlyModifiedProperties()
    {
        // Arrange
        var factory = GetRequiredService<IInventoryItemFactory>();
        var db = GetRequiredService<IInventoryDb>();

        var originalId = Guid.NewGuid();
        var originalDate = DateTime.UtcNow.AddDays(-1);
        db.Items.Add(new InventoryItemEntity
        {
            Id = originalId,
            Name = "Original Name",
            Quantity = 5,
            LastUpdated = originalDate
        });

        var item = factory.Fetch(db.Items[0]);

        // Act - only modify Name
        item.Name = "Updated Name";
        await factory.Save(item);

        // Assert
        Assert.AreEqual("Updated Name", db.Items[0].Name);
        Assert.AreEqual(5, db.Items[0].Quantity); // Unchanged
    }

    #endregion

    #region Delete Tests

    [TestMethod]
    public async Task InventoryItem_Delete_RemovesFromDb()
    {
        // Arrange
        var factory = GetRequiredService<IInventoryItemFactory>();
        var db = GetRequiredService<IInventoryDb>();

        var id = Guid.NewGuid();
        db.Items.Add(new InventoryItemEntity
        {
            Id = id,
            Name = "To Delete",
            Quantity = 1
        });

        var item = factory.Fetch(db.Items[0]);

        // Act
        item.Delete();
        await factory.Save(item);

        // Assert
        Assert.AreEqual(0, db.Items.Count);
    }

    [TestMethod]
    public async Task InventoryItem_UnDelete_PreventsRemoval()
    {
        // Arrange
        var factory = GetRequiredService<IInventoryItemFactory>();
        var db = GetRequiredService<IInventoryDb>();

        var id = Guid.NewGuid();
        db.Items.Add(new InventoryItemEntity
        {
            Id = id,
            Name = "To Keep",
            Quantity = 1
        });

        var item = factory.Fetch(db.Items[0]);

        // Act
        item.Delete();
        item.UnDelete(); // Changed mind
        await factory.Save(item);

        // Assert - item still exists
        Assert.AreEqual(1, db.Items.Count);
    }

    #endregion
}
