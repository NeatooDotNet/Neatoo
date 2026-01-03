using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Documentation.Samples.Collections;

namespace Neatoo.Documentation.Samples.Tests.Collections;

/// <summary>
/// Tests for EntityListSamples.cs code snippets.
/// </summary>
[TestClass]
[TestCategory("Documentation")]
[TestCategory("Collections")]
public class EntityListSamplesTests : SamplesTestBase
{
    [TestInitialize]
    public void TestInitialize()
    {
        InitializeScope();
    }

    #region PhoneList Tests

    [TestMethod]
    public void PhoneList_Create_IsEmpty()
    {
        // Arrange
        var factory = GetRequiredService<IPhoneListFactory>();

        // Act
        var list = factory.Create();

        // Assert
        Assert.IsNotNull(list);
        Assert.AreEqual(0, list.Count);
    }

    [TestMethod]
    public void PhoneList_AddPhoneNumber_CreatesAndAddsItem()
    {
        // Arrange
        var factory = GetRequiredService<IPhoneListFactory>();
        var list = factory.Create();

        // Act
        var phone = list.AddPhoneNumber();

        // Assert
        Assert.IsNotNull(phone);
        Assert.AreEqual(1, list.Count);
        Assert.IsTrue(phone.IsNew);
        Assert.AreNotEqual(Guid.Empty, phone.Id);
    }

    [TestMethod]
    public void PhoneList_AddPhoneNumber_MarksItemAsChild()
    {
        // Arrange
        var factory = GetRequiredService<IPhoneListFactory>();
        var list = factory.Create();

        // Act
        var phone = list.AddPhoneNumber();

        // Assert
        Assert.IsTrue(phone.IsChild);
    }

    [TestMethod]
    public void PhoneList_RemoveNewItem_JustRemoves()
    {
        // Arrange
        var factory = GetRequiredService<IPhoneListFactory>();
        var list = factory.Create();
        var phone = list.AddPhoneNumber();

        // Act
        list.RemovePhoneNumber(phone);

        // Assert - new items are just removed, not added to DeletedList
        Assert.AreEqual(0, list.Count);
    }

    [TestMethod]
    public void PhoneList_Fetch_LoadsAllEntities()
    {
        // Arrange
        var factory = GetRequiredService<IPhoneListFactory>();
        var entities = new List<PhoneEntity>
        {
            new() { Id = Guid.NewGuid(), PhoneNumber = "555-0001", PhoneType = "Home" },
            new() { Id = Guid.NewGuid(), PhoneNumber = "555-0002", PhoneType = "Work" },
            new() { Id = Guid.NewGuid(), PhoneNumber = "555-0003", PhoneType = "Mobile" }
        };

        // Act
        var list = factory.Fetch(entities);

        // Assert
        Assert.AreEqual(3, list.Count);
        Assert.AreEqual("555-0001", list[0].PhoneNumber);
        Assert.AreEqual("555-0002", list[1].PhoneNumber);
        Assert.AreEqual("555-0003", list[2].PhoneNumber);
    }

    [TestMethod]
    public void PhoneList_Fetch_ItemsAreNotNew()
    {
        // Arrange
        var factory = GetRequiredService<IPhoneListFactory>();
        var entities = new List<PhoneEntity>
        {
            new() { Id = Guid.NewGuid(), PhoneNumber = "555-0001", PhoneType = "Home" }
        };

        // Act
        var list = factory.Fetch(entities);

        // Assert
        Assert.IsFalse(list[0].IsNew);
    }

    [TestMethod]
    public void PhoneList_RemoveExistingItem_MarksDeleted()
    {
        // Arrange
        var factory = GetRequiredService<IPhoneListFactory>();
        var entities = new List<PhoneEntity>
        {
            new() { Id = Guid.NewGuid(), PhoneNumber = "555-0001", PhoneType = "Home" }
        };
        var list = factory.Fetch(entities);
        var phone = list[0];

        // Act
        list.RemovePhoneNumber(phone);

        // Assert - existing items are marked deleted and moved to DeletedList
        Assert.AreEqual(0, list.Count);
        Assert.IsTrue(phone.IsDeleted);
        // Cast to IEntityListBaseInternal to access DeletedList
        Assert.AreEqual(1, ((IEntityListBaseInternal)list).DeletedList.Cast<object>().Count());
    }

    [TestMethod]
    public void PhoneList_Update_HandlesNewItems()
    {
        // Arrange
        var listFactory = GetRequiredService<IPhoneListFactory>();
        var list = listFactory.Create();

        var phone = list.AddPhoneNumber();
        phone.PhoneNumber = "555-1234";
        phone.PhoneType = "Mobile";

        var entities = new List<PhoneEntity>();

        // Act
        listFactory.Save(list, entities);

        // Assert
        Assert.AreEqual(1, entities.Count);
        Assert.AreEqual("555-1234", entities[0].PhoneNumber);
        Assert.AreEqual("Mobile", entities[0].PhoneType);
    }

    [TestMethod]
    public void PhoneList_Update_HandlesModifiedItems()
    {
        // Arrange
        var listFactory = GetRequiredService<IPhoneListFactory>();
        var existingId = Guid.NewGuid();
        var entities = new List<PhoneEntity>
        {
            new() { Id = existingId, PhoneNumber = "555-0001", PhoneType = "Home" }
        };
        var list = listFactory.Fetch(entities);

        // Modify
        list[0].PhoneNumber = "555-9999";

        // Act
        listFactory.Save(list, entities);

        // Assert
        Assert.AreEqual(1, entities.Count);
        Assert.AreEqual("555-9999", entities[0].PhoneNumber);
    }

    [TestMethod]
    public void PhoneList_Update_HandlesDeletedItems()
    {
        // Arrange
        var listFactory = GetRequiredService<IPhoneListFactory>();
        var existingId = Guid.NewGuid();
        var entities = new List<PhoneEntity>
        {
            new() { Id = existingId, PhoneNumber = "555-0001", PhoneType = "Home" }
        };
        var list = listFactory.Fetch(entities);

        // Delete
        list.RemovePhoneNumber(list[0]);

        // Act
        listFactory.Save(list, entities);

        // Assert
        Assert.AreEqual(0, entities.Count);
    }

    #endregion
}
