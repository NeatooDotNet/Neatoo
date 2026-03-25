using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.UnitTest.Integration.Concepts.Serialization.DeferredLazyLoadTests;
using Neatoo.UnitTest.TestInfrastructure;

namespace Neatoo.UnitTest.Integration.Concepts.Serialization;

/// <summary>
/// Fat-client serialization tests that reproduce a bug: when a LazyLoad
/// property is created with a deferred loader in the entity constructor
/// and the entity goes through a serialization round-trip, IsLoaded
/// incorrectly becomes true even though no load was ever triggered.
///
/// These tests are expected to FAIL until the bug is fixed.
/// </summary>
[TestClass]
public class FatClientDeferredLazyLoadTests : IntegrationTestBase
{
    [TestInitialize]
    public void TestInitialize()
    {
        InitializeScope();
    }

    /// <summary>
    /// Bug reproduction: After Create (which leaves the deferred loader in place),
    /// serialize and deserialize the entity. The deferred LazyLoad should remain
    /// unloaded (IsLoaded=false) after the round-trip.
    /// </summary>
    [TestMethod]
    public async Task FatClient_Create_DeferredLazyLoad_IsLoaded_False_AfterRoundTrip()
    {
        // Arrange - Create entity via factory; the [Create] method leaves
        // the deferred LazyLoad from the constructor in place.
        var factory = GetRequiredService<IDeferredLazyLoadEntityFactory>();
        var id = Guid.NewGuid();
        var entity = await factory.Create(id, "TestEntity");

        // Verify pre-condition: before serialization, IsLoaded is false
        Assert.IsNotNull(entity.LazyDescription, "LazyDescription should exist from constructor");
        Assert.IsFalse(entity.LazyDescription.IsLoaded,
            "Pre-condition: LazyDescription should NOT be loaded before serialization");

        // Act - serialize and deserialize (fat-client round-trip)
        var json = Serialize(entity);
        var deserialized = Deserialize<IDeferredLazyLoadEntity>(json);

        // Assert - IsLoaded should remain false after round-trip
        Assert.IsNotNull(deserialized.LazyDescription,
            "LazyDescription should not be null after deserialization");
        Assert.IsFalse(deserialized.LazyDescription.IsLoaded,
            "BUG: LazyDescription.IsLoaded became true after serialization round-trip, " +
            "but no load was ever triggered. Expected false.");
        Assert.IsNull(deserialized.LazyDescription.Value,
            "LazyDescription.Value should be null because no load was triggered");
    }

    /// <summary>
    /// Bug reproduction: After Fetch (which leaves the deferred loader in place),
    /// serialize and deserialize the entity. The deferred LazyLoad should remain
    /// unloaded (IsLoaded=false) after the round-trip.
    /// </summary>
    [TestMethod]
    public async Task FatClient_Fetch_DeferredLazyLoad_IsLoaded_False_AfterRoundTrip()
    {
        // Arrange - Fetch entity via factory; the [Fetch(id, name)] overload
        // leaves the deferred LazyLoad from the constructor in place.
        var factory = GetRequiredService<IDeferredLazyLoadEntityFactory>();
        var id = Guid.NewGuid();
        var entity = await factory.Fetch(id, "TestEntity");

        // Verify pre-condition: before serialization, IsLoaded is false
        Assert.IsNotNull(entity.LazyDescription, "LazyDescription should exist from constructor");
        Assert.IsFalse(entity.LazyDescription.IsLoaded,
            "Pre-condition: LazyDescription should NOT be loaded before serialization");

        // Act - serialize and deserialize (fat-client round-trip)
        var json = Serialize(entity);
        var deserialized = Deserialize<IDeferredLazyLoadEntity>(json);

        // Assert - IsLoaded should remain false after round-trip
        Assert.IsNotNull(deserialized.LazyDescription,
            "LazyDescription should not be null after deserialization");
        Assert.IsFalse(deserialized.LazyDescription.IsLoaded,
            "BUG: LazyDescription.IsLoaded became true after serialization round-trip, " +
            "but no load was ever triggered. Expected false.");
        Assert.IsNull(deserialized.LazyDescription.Value,
            "LazyDescription.Value should be null because no load was triggered");
    }

    /// <summary>
    /// Control test: eagerly loaded LazyLoad should remain loaded after round-trip.
    /// This exercises the non-buggy path for comparison.
    /// </summary>
    [TestMethod]
    public async Task FatClient_Fetch_EagerLazyLoad_IsLoaded_True_AfterRoundTrip()
    {
        // Arrange - Fetch with description replaces the deferred LazyLoad
        // with a pre-loaded one.
        var factory = GetRequiredService<IDeferredLazyLoadEntityFactory>();
        var id = Guid.NewGuid();
        var entity = await factory.Fetch(id, "TestEntity", "eager description");

        // Verify pre-condition: before serialization, IsLoaded is true
        Assert.IsTrue(entity.LazyDescription.IsLoaded,
            "Pre-condition: eagerly loaded LazyDescription should be loaded");
        Assert.AreEqual("eager description", entity.LazyDescription.Value);

        // Act
        var json = Serialize(entity);
        var deserialized = Deserialize<IDeferredLazyLoadEntity>(json);

        // Assert - eagerly loaded LazyLoad should survive round-trip
        Assert.IsNotNull(deserialized.LazyDescription);
        Assert.IsTrue(deserialized.LazyDescription.IsLoaded,
            "Eagerly loaded LazyDescription should remain loaded after round-trip");
        Assert.AreEqual("eager description", deserialized.LazyDescription.Value);
    }
}
