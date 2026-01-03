using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.RemoteFactory;
using Neatoo.UnitTest.TestInfrastructure;
using System.ComponentModel;

namespace Neatoo.UnitTest.Integration.Concepts.Serialization;

#region Test Entity Definitions

/// <summary>
/// Interface for testing DisplayName serialization behavior.
/// </summary>
public interface IDisplayNameTestEntity : IEntityBase
{
    Guid Id { get; set; }

    /// <summary>
    /// Property with explicit [DisplayName] attribute.
    /// </summary>
    string? NameWithDisplayName { get; set; }

    /// <summary>
    /// Property without [DisplayName] attribute - should use property name.
    /// </summary>
    string? NameWithoutDisplayName { get; set; }

    /// <summary>
    /// Child entity for testing nested DisplayName preservation.
    /// </summary>
    IDisplayNameTestEntity? Child { get; set; }

    void MarkEntityAsChild();
}

/// <summary>
/// Test entity for DisplayName serialization tests.
/// </summary>
[Factory]
internal partial class DisplayNameTestEntity : EntityBase<DisplayNameTestEntity>, IDisplayNameTestEntity
{
    public DisplayNameTestEntity(IEntityBaseServices<DisplayNameTestEntity> services) : base(services)
    {
    }

    public partial Guid Id { get; set; }

    [DisplayName("Full Name")]
    public partial string? NameWithDisplayName { get; set; }

    public partial string? NameWithoutDisplayName { get; set; }

    public partial IDisplayNameTestEntity? Child { get; set; }

    void IDisplayNameTestEntity.MarkEntityAsChild()
    {
        MarkAsChild();
    }
}

#endregion

/// <summary>
/// Integration tests for DisplayName property serialization and deserialization.
/// Tests verify that DisplayName values are correctly preserved after round-trip serialization.
/// </summary>
[TestClass]
public class DisplayNameSerializationTests : IntegrationTestBase
{
    private IDisplayNameTestEntity _target = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        InitializeScope();
        _target = GetRequiredService<IDisplayNameTestEntity>();
        _target.Id = Guid.NewGuid();
    }

    private IDisplayNameTestEntity DeserializeEntity(string json)
    {
        return Deserialize<IDisplayNameTestEntity>(json);
    }

    /// <summary>
    /// Verifies that a property with [DisplayName("Full Name")] attribute
    /// preserves the display name "Full Name" after serialization round-trip.
    /// </summary>
    [TestMethod]
    public void DisplayName_FromAttribute_PreservedAfterSerialization()
    {
        // Arrange
        _target.NameWithDisplayName = "Test Value";
        var originalDisplayName = _target[nameof(IDisplayNameTestEntity.NameWithDisplayName)]!.DisplayName;

        // Verify original has expected DisplayName from attribute
        Assert.AreEqual("Full Name", originalDisplayName, "Original DisplayName should come from [DisplayName] attribute");

        // Act
        var json = Serialize(_target);
        var deserialized = DeserializeEntity(json);

        // Assert
        var deserializedDisplayName = deserialized[nameof(IDisplayNameTestEntity.NameWithDisplayName)]!.DisplayName;
        Assert.AreEqual("Full Name", deserializedDisplayName, "DisplayName should be preserved after deserialization");
    }

    /// <summary>
    /// Verifies that a property without [DisplayName] attribute
    /// uses the property name as DisplayName after serialization round-trip.
    /// </summary>
    [TestMethod]
    public void DisplayName_NoAttribute_UsesPropertyName()
    {
        // Arrange
        _target.NameWithoutDisplayName = "Test Value";
        var originalDisplayName = _target[nameof(IDisplayNameTestEntity.NameWithoutDisplayName)]!.DisplayName;

        // Verify original uses property name
        Assert.AreEqual("NameWithoutDisplayName", originalDisplayName, "Original DisplayName should be property name");

        // Act
        var json = Serialize(_target);
        var deserialized = DeserializeEntity(json);

        // Assert
        var deserializedDisplayName = deserialized[nameof(IDisplayNameTestEntity.NameWithoutDisplayName)]!.DisplayName;
        Assert.AreEqual("NameWithoutDisplayName", deserializedDisplayName, "DisplayName should be property name after deserialization");
    }

    /// <summary>
    /// Verifies that child entities in an aggregate preserve their DisplayName values
    /// after serialization and deserialization.
    /// </summary>
    [TestMethod]
    public void DisplayName_ChildEntities_PreservedInAggregate()
    {
        // Arrange
        var child = GetRequiredService<IDisplayNameTestEntity>();
        child.Id = Guid.NewGuid();
        child.NameWithDisplayName = "Child Value";
        child.MarkEntityAsChild();
        _target.Child = child;

        var originalChildDisplayName = child[nameof(IDisplayNameTestEntity.NameWithDisplayName)]!.DisplayName;
        Assert.AreEqual("Full Name", originalChildDisplayName, "Child original DisplayName should come from attribute");

        // Act
        var json = Serialize(_target);
        var deserialized = DeserializeEntity(json);

        // Assert
        Assert.IsNotNull(deserialized.Child, "Child should be deserialized");
        var deserializedChildDisplayName = deserialized.Child[nameof(IDisplayNameTestEntity.NameWithDisplayName)]!.DisplayName;
        Assert.AreEqual("Full Name", deserializedChildDisplayName, "Child DisplayName should be preserved after deserialization");
    }

    /// <summary>
    /// Verifies that DisplayName survives a full round-trip serialization.
    /// </summary>
    [TestMethod]
    public void DisplayName_FullRoundTrip_Preserved()
    {
        // Arrange
        _target.NameWithDisplayName = "Value 1";
        _target.NameWithoutDisplayName = "Value 2";

        // Act - Serialize, deserialize, modify, serialize again, deserialize again
        var json1 = Serialize(_target);
        var deserialized1 = DeserializeEntity(json1);

        deserialized1.NameWithDisplayName = "Modified Value";

        var json2 = Serialize(deserialized1);
        var deserialized2 = DeserializeEntity(json2);

        // Assert - All DisplayNames should still be correct
        Assert.AreEqual("Full Name", deserialized2[nameof(IDisplayNameTestEntity.NameWithDisplayName)]!.DisplayName);
        Assert.AreEqual("NameWithoutDisplayName", deserialized2[nameof(IDisplayNameTestEntity.NameWithoutDisplayName)]!.DisplayName);
    }

    /// <summary>
    /// Verifies that the Id property (no DisplayName attribute) uses property name.
    /// </summary>
    [TestMethod]
    public void DisplayName_GuidProperty_UsesPropertyName()
    {
        // Arrange
        _target.Id = Guid.NewGuid();

        // Act
        var json = Serialize(_target);
        var deserialized = DeserializeEntity(json);

        // Assert
        var displayName = deserialized[nameof(IDisplayNameTestEntity.Id)]!.DisplayName;
        Assert.AreEqual("Id", displayName, "Id property should use property name as DisplayName");
    }

    /// <summary>
    /// Verifies that the serialized JSON does NOT contain the displayName field.
    /// This confirms that DisplayName is no longer being serialized, reducing network overhead.
    /// </summary>
    [TestMethod]
    public void SerializedJson_DoesNotContain_DisplayNameField()
    {
        // Arrange
        _target.NameWithDisplayName = "Test Value";
        _target.NameWithoutDisplayName = "Another Value";

        // Act
        var json = Serialize(_target);

        // Assert - JSON should not contain "displayName" as a property key
        Assert.IsFalse(json.Contains("\"displayName\""),
            $"Serialized JSON should not contain displayName field. JSON: {json}");
    }
}
