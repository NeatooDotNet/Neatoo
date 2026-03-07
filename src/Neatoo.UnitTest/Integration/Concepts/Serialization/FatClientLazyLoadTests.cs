using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.UnitTest.Integration.Concepts.Serialization.LazyLoadTests;
using Neatoo.UnitTest.TestInfrastructure;

namespace Neatoo.UnitTest.Integration.Concepts.Serialization;

/// <summary>
/// Integration tests for LazyLoad property serialization and deserialization
/// through NeatooBaseJsonTypeConverter. Covers EntityBase and ValidateBase
/// with pre-loaded, unloaded, and nested Neatoo entity LazyLoad values.
/// </summary>
[TestClass]
public class FatClientLazyLoadTests : IntegrationTestBase
{
    [TestInitialize]
    public void TestInitialize()
    {
        InitializeScope();
    }

    #region EntityBase with LazyLoad

    /// <summary>
    /// Rule 1+2: Pre-loaded LazyLoad on EntityBase survives serialization round-trip.
    /// </summary>
    [TestMethod]
    public void FatClientLazyLoad_EntityBase_PreLoaded_RoundTrip()
    {
        // Arrange
        var entity = GetRequiredService<ILazyLoadEntityObject>();
        entity.ID = Guid.NewGuid();
        entity.Name = "TestEntity";
        entity.LazyDescription = new LazyLoad<string>("hello");

        // Act
        var json = Serialize(entity);
        var deserialized = Deserialize<ILazyLoadEntityObject>(json);

        // Assert
        Assert.IsNotNull(deserialized.LazyDescription);
        Assert.IsTrue(deserialized.LazyDescription.IsLoaded);
        Assert.AreEqual("hello", deserialized.LazyDescription.Value);
    }

    /// <summary>
    /// Rule 1: Verify JSON output contains the LazyLoad property.
    /// </summary>
    [TestMethod]
    public void FatClientLazyLoad_EntityBase_Serialize_ContainsLazyLoadProperty()
    {
        // Arrange
        var entity = GetRequiredService<ILazyLoadEntityObject>();
        entity.ID = Guid.NewGuid();
        entity.Name = "TestEntity";
        entity.LazyDescription = new LazyLoad<string>("hello");

        // Act
        var json = Serialize(entity);

        // Assert
        Assert.IsTrue(json.Contains("LazyDescription"), "JSON should contain the LazyDescription property");
        Assert.IsTrue(json.Contains("hello"), "JSON should contain the LazyLoad value");
    }

    #endregion

    #region ValidateBase with LazyLoad

    /// <summary>
    /// Rule 3: Pre-loaded LazyLoad on ValidateBase survives serialization round-trip.
    /// </summary>
    [TestMethod]
    public void FatClientLazyLoad_ValidateBase_PreLoaded_RoundTrip()
    {
        // Arrange
        var validate = GetRequiredService<ILazyLoadValidateObject>();
        validate.ID = Guid.NewGuid();
        validate.Name = "TestValidate";
        validate.LazyContent = new LazyLoad<string>("test content");

        // Act
        var json = Serialize(validate);
        var deserialized = Deserialize<ILazyLoadValidateObject>(json);

        // Assert
        Assert.IsNotNull(deserialized.LazyContent);
        Assert.IsTrue(deserialized.LazyContent.IsLoaded);
        Assert.AreEqual("test content", deserialized.LazyContent.Value);
    }

    #endregion

    #region Unloaded LazyLoad

    /// <summary>
    /// Rule 4: Unloaded LazyLoad (IsLoaded=false, Value=null) survives round-trip.
    /// </summary>
    [TestMethod]
    public void FatClientLazyLoad_Unloaded_RoundTrip()
    {
        // Arrange
        var entity = GetRequiredService<ILazyLoadEntityObject>();
        entity.ID = Guid.NewGuid();
        entity.Name = "TestEntity";
        entity.LazyDescription = new LazyLoad<string>(); // Not loaded

        // Act
        var json = Serialize(entity);
        var deserialized = Deserialize<ILazyLoadEntityObject>(json);

        // Assert
        Assert.IsNotNull(deserialized.LazyDescription);
        Assert.IsFalse(deserialized.LazyDescription.IsLoaded);
        Assert.IsNull(deserialized.LazyDescription.Value);
    }

    #endregion

    #region Nested Neatoo Entity in LazyLoad

    /// <summary>
    /// Rule 5: LazyLoad with nested Neatoo entity (IValidateBase) Value survives round-trip.
    /// The inner entity is serialized through the Neatoo converter with $id, PropertyManager.
    /// </summary>
    [TestMethod]
    public void FatClientLazyLoad_NestedNeatooEntity_RoundTrip()
    {
        // Arrange
        var entity = GetRequiredService<ILazyLoadEntityObject>();
        entity.ID = Guid.NewGuid();
        entity.Name = "Parent";
        entity.LazyDescription = new LazyLoad<string>("parent description");

        var childEntity = GetRequiredService<ILazyLoadEntityObject>();
        childEntity.ID = Guid.NewGuid();
        childEntity.Name = "Child";
        childEntity.LazyDescription = new LazyLoad<string>("child description");

        // Set the LazyChild to point to the child entity
        entity.LazyChild = new LazyLoad<ILazyLoadEntityObject>(childEntity);

        // Act
        var json = Serialize(entity);
        var deserialized = Deserialize<ILazyLoadEntityObject>(json);

        // Assert
        Assert.IsNotNull(deserialized.LazyChild);
        Assert.IsTrue(deserialized.LazyChild.IsLoaded);
        Assert.IsNotNull(deserialized.LazyChild.Value);
        Assert.AreEqual("Child", deserialized.LazyChild.Value.Name);
        Assert.AreEqual(childEntity.ID, deserialized.LazyChild.Value.ID);

        // Verify the nested entity's own LazyLoad also survived
        Assert.IsNotNull(deserialized.LazyChild.Value.LazyDescription);
        Assert.IsTrue(deserialized.LazyChild.Value.LazyDescription.IsLoaded);
        Assert.AreEqual("child description", deserialized.LazyChild.Value.LazyDescription.Value);
    }

    #endregion

    #region Post-deserialization LoadAsync

    /// <summary>
    /// Rule 8: Post-deserialization LoadAsync throws InvalidOperationException
    /// because the loader delegate is not serialized.
    /// </summary>
    [TestMethod]
    public async Task FatClientLazyLoad_PostDeserialization_LoadAsync_Throws()
    {
        // Arrange
        var entity = GetRequiredService<ILazyLoadEntityObject>();
        entity.ID = Guid.NewGuid();
        entity.Name = "TestEntity";
        entity.LazyDescription = new LazyLoad<string>(); // Not loaded, no loader

        var json = Serialize(entity);
        var deserialized = Deserialize<ILazyLoadEntityObject>(json);

        // Act & Assert - LoadAsync should throw because loader was not serialized
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => deserialized.LazyDescription.LoadAsync());
    }

    #endregion

    #region PropertyManager properties still work

    /// <summary>
    /// Rule 7: Entity without LazyLoad properties still works (no regression).
    /// This is covered by all existing FatClientEntityTests/FatClientValidateTests,
    /// but this test verifies the LazyLoad entity's PropertyManager properties
    /// (ID, Name) are also unaffected.
    /// </summary>
    [TestMethod]
    public void FatClientLazyLoad_PropertyManagerProperties_StillWork()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity = GetRequiredService<ILazyLoadEntityObject>();
        entity.ID = id;
        entity.Name = "TestEntity";
        entity.LazyDescription = new LazyLoad<string>("desc");

        // Act
        var json = Serialize(entity);
        var deserialized = Deserialize<ILazyLoadEntityObject>(json);

        // Assert - PropertyManager properties are still correct
        Assert.AreEqual(id, deserialized.ID);
        Assert.AreEqual("TestEntity", deserialized.Name);
    }

    #endregion
}
