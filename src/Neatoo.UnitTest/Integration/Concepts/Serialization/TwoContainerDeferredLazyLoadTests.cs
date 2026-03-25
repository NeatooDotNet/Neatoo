using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.UnitTest.Integration.Concepts.Serialization.DeferredLazyLoadTests;
using Neatoo.UnitTest.TestInfrastructure;

namespace Neatoo.UnitTest.Integration.Concepts.Serialization;

/// <summary>
/// Two-container serialization tests that reproduce a bug: when a LazyLoad
/// property is created with a deferred loader in the entity constructor
/// and the entity goes through the client-server serialization pipeline,
/// IsLoaded incorrectly becomes true even though no load was ever triggered.
///
/// These tests exercise the full RemoteFactory two-container path:
/// Client calls factory -> Server constructs entity + runs factory method ->
/// Server serializes -> Client deserializes (constructor runs again on client).
///
/// These tests are expected to FAIL until the bug is fixed.
/// </summary>
[TestClass]
public class TwoContainerDeferredLazyLoadTests : ClientServerTestBase
{
    [TestInitialize]
    public void TestInitialize()
    {
        InitializeScopes();
    }

    /// <summary>
    /// Bug reproduction: Two-container Create with deferred LazyLoad.
    /// Server: constructor creates LazyLoad with loader, [Create] leaves it in place.
    /// Serialization: LazyLoad serialized with IsLoaded=false, Value=null.
    /// Client: constructor runs again (new LazyLoad with loader from DI).
    /// Deserialization: ApplyDeserializedState merges serialized state.
    /// Expected: IsLoaded should be false on the client.
    /// Actual bug: IsLoaded becomes true.
    /// </summary>
    [TestMethod]
    public async Task TwoContainer_Create_DeferredLazyLoad_IsLoaded_False()
    {
        // Arrange
        var factory = GetClientService<IDeferredLazyLoadEntityFactory>();
        var id = Guid.NewGuid();

        // Act - Create goes through client->server->client pipeline
        var entity = await factory.Create(id, "Created Entity");

        // Assert
        Assert.IsNotNull(entity, "Entity should not be null after two-container Create");
        Assert.AreEqual(id, entity.ID);
        Assert.AreEqual("Created Entity", entity.Name);

        Assert.IsNotNull(entity.LazyDescription,
            "LazyDescription should not be null after two-container Create");
        Assert.IsFalse(entity.LazyDescription.IsLoaded,
            "BUG: LazyDescription.IsLoaded became true after two-container Create, " +
            "but no load was ever triggered. Expected false.");
        Assert.IsNull(entity.LazyDescription.Value,
            "LazyDescription.Value should be null because no load was triggered");
    }

    /// <summary>
    /// Bug reproduction: Two-container Fetch (without eager loading) with deferred LazyLoad.
    /// Server: constructor creates LazyLoad with loader, [Fetch(id, name)] leaves it in place.
    /// Serialization: LazyLoad serialized with IsLoaded=false, Value=null.
    /// Client: constructor runs again (new LazyLoad with loader from DI).
    /// Deserialization: ApplyDeserializedState merges serialized state.
    /// Expected: IsLoaded should be false on the client.
    /// Actual bug: IsLoaded becomes true.
    /// </summary>
    [TestMethod]
    public async Task TwoContainer_Fetch_DeferredLazyLoad_IsLoaded_False()
    {
        // Arrange
        var factory = GetClientService<IDeferredLazyLoadEntityFactory>();
        var id = Guid.NewGuid();

        // Act - Fetch goes through client->server->client pipeline
        var entity = await factory.Fetch(id, "Fetched Entity");

        // Assert
        Assert.IsNotNull(entity, "Entity should not be null after two-container Fetch");
        Assert.AreEqual(id, entity.ID);
        Assert.AreEqual("Fetched Entity", entity.Name);

        Assert.IsNotNull(entity.LazyDescription,
            "LazyDescription should not be null after two-container Fetch");
        Assert.IsFalse(entity.LazyDescription.IsLoaded,
            "BUG: LazyDescription.IsLoaded became true after two-container Fetch, " +
            "but no load was ever triggered. Expected false.");
        Assert.IsNull(entity.LazyDescription.Value,
            "LazyDescription.Value should be null because no load was triggered");
    }

    /// <summary>
    /// Control test: Two-container Fetch with eager loading.
    /// The [Fetch(id, name, description)] overload replaces the deferred LazyLoad
    /// with a pre-loaded one. IsLoaded should be true after the round-trip.
    /// </summary>
    [TestMethod]
    public async Task TwoContainer_Fetch_EagerLazyLoad_IsLoaded_True()
    {
        // Arrange
        var factory = GetClientService<IDeferredLazyLoadEntityFactory>();
        var id = Guid.NewGuid();

        // Act - Fetch with description goes through client->server->client pipeline
        var entity = await factory.Fetch(id, "Eager Entity", "eager description");

        // Assert - eagerly loaded LazyLoad should survive the two-container round-trip
        Assert.IsNotNull(entity);
        Assert.AreEqual(id, entity.ID);
        Assert.AreEqual("Eager Entity", entity.Name);

        Assert.IsNotNull(entity.LazyDescription);
        Assert.IsTrue(entity.LazyDescription.IsLoaded,
            "Eagerly loaded LazyDescription should remain loaded after two-container Fetch");
        Assert.AreEqual("eager description", entity.LazyDescription.Value);
    }
}
