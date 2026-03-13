using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.UnitTest.Integration.Concepts.Serialization.LazyLoadCrash;
using Neatoo.UnitTest.TestInfrastructure;

namespace Neatoo.UnitTest.Integration.Concepts.Serialization;

/// <summary>
/// Reproduces crash: WaitForTasks() crashes when AddActionAsync handler
/// awaits a LazyLoad&lt;T&gt; deferred loader that calls a [Remote] factory method.
///
/// The parent entity is fetched via [Remote] [Fetch], meaning it goes through
/// the client-server serialization pipeline. During deserialization, the Neatoo
/// converter (NeatooBaseJsonTypeConverter) overwrites LazyLoad properties with
/// deserialized instances that have no loader delegate, because the loader
/// (Func&lt;Task&lt;T&gt;&gt;) is [JsonIgnore] on LazyLoad&lt;T&gt;.
///
/// See: docs/todos/waitfortasks-crash-lazyload-remote.md
/// </summary>
[TestClass]
public class WaitForTasksLazyLoadCrashTests : ClientServerTestBase
{
    [TestInitialize]
    public void TestInitialize()
    {
        InitializeScopes();
    }

    /// <summary>
    /// Reproduction matching zTreatment pattern:
    /// 1. Fetch parent via [Remote] -- goes through client-server serialization
    /// 2. On client: LazyChild has been overwritten by deserialized instance (no loader)
    /// 3. Set Trigger -> AddActionAsync fires -> await LazyChild -> LoadAsync()
    /// 4. Expected: crash or exception because loader delegate is null after deserialization
    /// </summary>
    [TestMethod]
    public async Task WaitForTasks_AddActionAsync_AwaitsLazyLoad_WithRemoteFetch_Crashes()
    {
        // Arrange - Fetch the parent entity through the client-server pipeline.
        // [Remote] [Fetch] means:
        //   Server: constructor runs (creates LazyChild with loader), Fetch runs (sets Id, creates LazyChild)
        //   Serialization: LazyChild written to JSON (Value=null, IsLoaded=false, NO loader)
        //   Client: constructor runs again (creates LazyChild with loader from DI-injected factory)
        //   Client deserialization: Neatoo converter finds LazyChild property, overwrites with
        //     deserialized LazyLoad instance -> loader delegate is LOST
        var factory = GetClientService<ICrashParentFactory>();
        var parentId = Guid.NewGuid();
        var parent = await factory.Fetch(parentId);

        // Act - Set Trigger to fire the AddActionAsync.
        // The handler awaits LazyChild, which now has no loader delegate.
        // This should trigger the crash/exception.
        parent.Trigger = "trigger";

        // WaitForTasks() should surface whatever happened in the async action
        await parent.WaitForTasks();

        // Assert - if we get here without crash, verify the state
        Assert.IsNotNull(parent.LazyChild, "LazyChild should not be null");
        Assert.IsTrue(parent.LazyChild.IsLoaded, "LazyChild should be loaded after WaitForTasks");
        Assert.IsNotNull(parent.LazyChild.Value, "LazyChild.Value should not be null");
        Assert.AreEqual($"Child data for {parentId}", parent.LazyChild.Value.Data);
        Assert.AreEqual($"Child data for {parentId}", parent.LoadedData,
            "LoadedData should be set from child after lazy load completes");
    }
}
