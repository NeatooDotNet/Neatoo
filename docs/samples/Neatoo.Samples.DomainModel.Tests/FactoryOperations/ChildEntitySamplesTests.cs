using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Samples.DomainModel.FactoryOperations;

namespace Neatoo.Samples.DomainModel.Tests.FactoryOperations;

/// <summary>
/// Tests for ChildEntitySamples.cs code snippets.
/// </summary>
[TestClass]
[TestCategory("Documentation")]
[TestCategory("FactoryOperations")]
public class ChildEntitySamplesTests : SamplesTestBase
{
    [TestInitialize]
    public void TestInitialize()
    {
        InitializeScope();
    }

    #region InvoiceLine Tests

    [TestMethod]
    public void InvoiceLine_Create_InitializesId()
    {
        // Arrange
        var factory = GetRequiredService<IInvoiceLineFactory>();

        // Act
        var line = factory.Create();

        // Assert
        Assert.AreNotEqual(Guid.Empty, line.Id);
        Assert.IsTrue(line.IsNew);
    }

    [TestMethod]
    public void InvoiceLine_Fetch_LoadsFromEntity()
    {
        // Arrange
        var factory = GetRequiredService<IInvoiceLineFactory>();
        var entity = new InvoiceLineEntity
        {
            Id = Guid.NewGuid(),
            Description = "Widget x 5",
            Amount = 49.95m
        };

        // Act
        var line = factory.Fetch(entity);

        // Assert
        Assert.AreEqual(entity.Id, line.Id);
        Assert.AreEqual("Widget x 5", line.Description);
        Assert.AreEqual(49.95m, line.Amount);
        Assert.IsFalse(line.IsNew);
    }

    [TestMethod]
    public void InvoiceLine_Insert_PopulatesEntity()
    {
        // Arrange
        var factory = GetRequiredService<IInvoiceLineFactory>();
        var line = factory.Create();
        line.Description = "New Line";
        line.Amount = 25.00m;

        var entity = new InvoiceLineEntity();

        // Act
        factory.Save(line, entity);

        // Assert
        Assert.AreEqual(line.Id, entity.Id);
        Assert.AreEqual("New Line", entity.Description);
        Assert.AreEqual(25.00m, entity.Amount);
    }

    [TestMethod]
    public void InvoiceLine_Update_OnlyModifiedProperties()
    {
        // Arrange
        var factory = GetRequiredService<IInvoiceLineFactory>();
        var originalEntity = new InvoiceLineEntity
        {
            Id = Guid.NewGuid(),
            Description = "Original",
            Amount = 10.00m
        };

        var line = factory.Fetch(originalEntity);

        // Modify only Amount
        line.Amount = 20.00m;

        var entity = new InvoiceLineEntity
        {
            Id = originalEntity.Id,
            Description = "Original",
            Amount = 10.00m
        };

        // Act
        factory.Save(line, entity);

        // Assert - only Amount changed
        Assert.AreEqual(20.00m, entity.Amount);
        Assert.AreEqual("Original", entity.Description);
    }

    #endregion

    #region InvoiceLineList Tests

    [TestMethod]
    public void InvoiceLineList_Create_IsEmpty()
    {
        // Arrange
        var factory = GetRequiredService<IInvoiceLineListFactory>();

        // Act
        var list = factory.Create();

        // Assert
        Assert.IsNotNull(list);
        Assert.AreEqual(0, list.Count);
    }

    [TestMethod]
    public void InvoiceLineList_Fetch_LoadsAllEntities()
    {
        // Arrange
        var factory = GetRequiredService<IInvoiceLineListFactory>();
        var entities = new List<InvoiceLineEntity>
        {
            new() { Id = Guid.NewGuid(), Description = "Line 1", Amount = 10m },
            new() { Id = Guid.NewGuid(), Description = "Line 2", Amount = 20m },
            new() { Id = Guid.NewGuid(), Description = "Line 3", Amount = 30m }
        };

        // Act
        var list = factory.Fetch(entities);

        // Assert
        Assert.AreEqual(3, list.Count);
        Assert.AreEqual("Line 1", list[0].Description);
        Assert.AreEqual("Line 2", list[1].Description);
        Assert.AreEqual("Line 3", list[2].Description);
    }

    [TestMethod]
    public void InvoiceLineList_Update_HandlesNewItems()
    {
        // Arrange
        var listFactory = GetRequiredService<IInvoiceLineListFactory>();
        var lineFactory = GetRequiredService<IInvoiceLineFactory>();

        var list = listFactory.Create();
        var newLine = lineFactory.Create();
        newLine.Description = "New Line";
        newLine.Amount = 15m;
        list.Add(newLine);

        var entities = new List<InvoiceLineEntity>();

        // Act
        listFactory.Save(list, entities);

        // Assert
        Assert.AreEqual(1, entities.Count);
        Assert.AreEqual("New Line", entities[0].Description);
    }

    [TestMethod]
    public void InvoiceLineList_Update_HandlesDeletedItems()
    {
        // Arrange
        var factory = GetRequiredService<IInvoiceLineListFactory>();
        var existingId = Guid.NewGuid();
        var entities = new List<InvoiceLineEntity>
        {
            new() { Id = existingId, Description = "To Delete", Amount = 10m }
        };

        var list = factory.Fetch(entities);

        // Mark for deletion
        list[0].Delete();
        list.Remove(list[0]); // Move to DeletedList

        // Act
        factory.Save(list, entities);

        // Assert
        Assert.AreEqual(0, entities.Count);
    }

    #endregion
}
