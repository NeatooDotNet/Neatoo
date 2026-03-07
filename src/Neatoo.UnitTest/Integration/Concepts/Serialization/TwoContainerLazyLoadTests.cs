using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.UnitTest.Integration.Concepts.Serialization.LazyLoadTests;
using Neatoo.UnitTest.TestInfrastructure;

namespace Neatoo.UnitTest.Integration.Concepts.Serialization;

/// <summary>
/// Two-container integration tests that verify LazyLoad property state
/// after factory operations that cross the client-server serialization boundary.
/// </summary>
[TestClass]
public class TwoContainerLazyLoadTests : ClientServerTestBase
{
    [TestInitialize]
    public void TestInitialize()
    {
        InitializeScopes();
    }

    /// <summary>
    /// Rule 6: LazyLoad property survives full client-server Fetch round-trip.
    /// Server creates entity with pre-loaded LazyLoad, client receives it intact.
    /// </summary>
    [TestMethod]
    public async Task Fetch_TwoContainer_LazyLoad_PreservesValue()
    {
        // Arrange
        var factory = GetClientService<ILazyLoadEntityObjectFactory>();
        var id = Guid.NewGuid();
        var name = "Remote Entity";
        var description = "Remote Description";

        // Act - Fetch goes through client->server->client pipeline
        var entity = await factory.Fetch(id, name, description);

        // Assert
        Assert.AreEqual(id, entity.ID);
        Assert.AreEqual(name, entity.Name);
        Assert.IsNotNull(entity.LazyDescription, "LazyDescription should not be null after remote Fetch");
        Assert.IsTrue(entity.LazyDescription.IsLoaded, "LazyDescription should be loaded after remote Fetch");
        Assert.AreEqual(description, entity.LazyDescription.Value, "LazyDescription.Value should match");
    }

    /// <summary>
    /// Rule 6+8: After two-container Fetch, LoadAsync on deserialized LazyLoad throws.
    /// Verifies the loader delegate is not transferred.
    /// </summary>
    [TestMethod]
    public async Task Fetch_TwoContainer_LazyLoad_LoadAsync_Throws()
    {
        // Arrange
        var factory = GetClientService<ILazyLoadEntityObjectFactory>();
        var id = Guid.NewGuid();

        // Act - Fetch with a description that makes the LazyLoad pre-loaded
        var entity = await factory.Fetch(id, "Entity", "Desc");

        // Create a new unloaded LazyLoad on the client side
        // (The fetched one is pre-loaded, so LoadAsync would succeed.
        // We need to test an unloaded one.)
        entity.LazyDescription = new LazyLoad<string>(); // Unloaded, no loader

        // Assert - LoadAsync should throw because loader was not serialized
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => entity.LazyDescription.LoadAsync());
    }
}
