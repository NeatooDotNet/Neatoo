using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.UnitTest.Integration.Concepts.Serialization.LazyLoadTests;
using Neatoo.UnitTest.TestInfrastructure;

namespace Neatoo.UnitTest.Integration.Concepts.EntityBase;

/// <summary>
/// Tests that state changes in a child entity loaded via LazyLoad&lt;T&gt;
/// propagate up to the parent entity, just like regular partial properties.
/// </summary>
[TestClass]
public class LazyLoadStatePropagationTests : IntegrationTestBase
{
    private ILazyLoadEntityObject parent = null!;
    private ILazyLoadEntityObject child = null!;

    [TestInitialize]
    public async Task TestInitialize()
    {
        InitializeScope();

        var factory = GetRequiredService<ILazyLoadEntityObjectFactory>();

        // Fetch creates entities in a persisted (not new, not modified) state
        parent = await factory.Fetch(Guid.NewGuid(), "Parent", "parent desc");
        child = await factory.Fetch(Guid.NewGuid(), "Child", "child desc");

        // Set child into parent's LazyLoad property (pre-loaded)
        parent.LazyChild = new LazyLoad<ILazyLoadEntityObject>(child);
    }

    [TestMethod]
    public void LazyLoadChild_InitialState_ParentNotModified()
    {
        // After Fetch, the partial properties are not modified.
        // Verify child is not modified.
        Assert.IsFalse(child.IsModified, "Child should not be modified after Fetch");
    }

    [TestMethod]
    public async Task LazyLoadChild_ModifyChild_ParentIsModified()
    {
        // Act - modify the child entity inside LazyLoad
        child.Name = "Modified Child";
        await parent.WaitForTasks();

        // Assert - parent should reflect child's modification
        Assert.IsTrue(child.IsModified, "Child should be modified after Name change");
        Assert.IsTrue(parent.IsModified, "Parent should be modified when LazyLoad child is modified");
    }

    [TestMethod]
    public async Task LazyLoadChild_ModifyChild_ParentNotSelfModified()
    {
        // Act
        child.Name = "Modified Child";
        await parent.WaitForTasks();

        // Assert - parent itself wasn't modified, only its child was
        Assert.IsFalse(parent.IsSelfModified, "Parent should not be self-modified when only child changed");
        Assert.IsTrue(child.IsSelfModified, "Child should be self-modified");
    }

    [TestMethod]
    public async Task LazyLoadChild_ModifyChild_ParentIsSavable()
    {
        // Act
        child.Name = "Modified Child";
        await parent.WaitForTasks();

        // Assert - parent should be savable since it has modified children
        Assert.IsTrue(((IEntityRoot)parent).IsSavable, "Parent should be savable when LazyLoad child is modified");
    }
}

/// <summary>
/// Tests that explicit LoadAsync on LazyLoad children propagates IsBusy and IsValid
/// to the parent entity, and that WaitForTasks on the parent awaits in-progress
/// LazyLoad children.
/// </summary>
[TestClass]
public class LazyLoadExplicitLoadPropagationTests : IntegrationTestBase
{
    private ILazyLoadEntityObject parent = null!;

    [TestInitialize]
    public async Task TestInitialize()
    {
        InitializeScope();

        var factory = GetRequiredService<ILazyLoadEntityObjectFactory>();

        // Fetch creates entity in a persisted state
        parent = await factory.Fetch(Guid.NewGuid(), "Parent", "parent desc");
    }

    [TestMethod]
    public async Task ParentIsBusy_AfterExplicitChildLoad()
    {
        // Arrange -- set up a LazyLoad with a slow loader
        var continueLoad = new TaskCompletionSource<ILazyLoadEntityObject?>();

        var factory = GetRequiredService<ILazyLoadEntityObjectFactory>();
        var childEntity = await factory.Fetch(Guid.NewGuid(), "Child", "child desc");

        parent.LazyChild = new LazyLoad<ILazyLoadEntityObject>(async () => await continueLoad.Task);

        // Act -- start explicit load (fire-and-forget)
        _ = parent.LazyChild.LoadAsync();

        // Assert -- parent IsBusy while child load is in progress
        Assert.IsTrue(parent.LazyChild.IsBusy, "LazyChild should be busy during load");
        Assert.IsTrue(parent.IsBusy, "Parent should be busy when LazyLoad child is loading");

        // Cleanup
        continueLoad.SetResult(childEntity);
        await parent.WaitForTasks();

        Assert.IsFalse(parent.IsBusy, "Parent should not be busy after load completes");
    }

    [TestMethod]
    public async Task ParentIsValid_AfterExplicitChildLoadFailure()
    {
        // Arrange -- set up a LazyLoad with a failing loader
        parent.LazyChild = new LazyLoad<ILazyLoadEntityObject>(
            () => throw new InvalidOperationException("load failed"));

        // Act -- trigger explicit load (fire-and-forget)
        _ = parent.LazyChild.LoadAsync();

        // Wait for the load to complete (it will fail)
        // WaitForTasks propagates the exception from the faulted _loadTask
        try
        {
            await parent.WaitForTasks();
        }
        catch (InvalidOperationException)
        {
            // Expected -- exception propagates through WaitForTasks
        }

        // Assert -- parent IsValid is false because LazyLoad child has load error
        Assert.IsTrue(parent.LazyChild.HasLoadError, "LazyChild should have load error");
        Assert.IsFalse(((IValidateMetaProperties)parent.LazyChild).IsValid, "LazyChild IsValid should be false");
        Assert.IsFalse(parent.IsValid, "Parent should be invalid when LazyLoad child has load error");
    }

    [TestMethod]
    public async Task ParentWaitForTasks_AwaitsExplicitLazyLoadChild()
    {
        // Arrange -- LazyLoad with async loader
        var factory = GetRequiredService<ILazyLoadEntityObjectFactory>();
        var childEntity = await factory.Fetch(Guid.NewGuid(), "LazyChild", "lazy child desc");

        var continueLoad = new TaskCompletionSource<ILazyLoadEntityObject?>();

        parent.LazyChild = new LazyLoad<ILazyLoadEntityObject>(async () => await continueLoad.Task);

        // Act -- start explicit load (fire-and-forget)
        _ = parent.LazyChild.LoadAsync();
        Assert.IsTrue(parent.LazyChild.IsLoading, "LazyChild should be loading after LoadAsync()");

        // Complete the load
        continueLoad.SetResult(childEntity);

        // WaitForTasks on parent should await the LazyLoad child's load
        await parent.WaitForTasks();

        // Assert -- load completed
        Assert.IsTrue(parent.LazyChild.IsLoaded, "LazyChild should be loaded after parent.WaitForTasks()");
        Assert.AreSame(childEntity, parent.LazyChild.Value, "LazyChild.Value should hold loaded entity");
    }

    [TestMethod]
    public async Task ParentWaitForTasksWithToken_AwaitsExplicitLazyLoadChild()
    {
        // Arrange -- CancellationToken version
        var factory = GetRequiredService<ILazyLoadEntityObjectFactory>();
        var childEntity = await factory.Fetch(Guid.NewGuid(), "LazyChild", "lazy child desc");

        var continueLoad = new TaskCompletionSource<ILazyLoadEntityObject?>();

        parent.LazyChild = new LazyLoad<ILazyLoadEntityObject>(async () => await continueLoad.Task);

        // Act -- start explicit load (fire-and-forget)
        _ = parent.LazyChild.LoadAsync();

        // Complete the load
        continueLoad.SetResult(childEntity);

        // WaitForTasks with token on parent should await the LazyLoad child's load
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await parent.WaitForTasks(cts.Token);

        // Assert
        Assert.IsTrue(parent.LazyChild.IsLoaded, "LazyChild should be loaded after parent.WaitForTasks(token)");
        Assert.AreSame(childEntity, parent.LazyChild.Value);
    }

    [TestMethod]
    public async Task ParentWaitForTasks_PreLoadedChild_CompletesImmediately()
    {
        // Arrange (Scenario 16, Rule 15) -- pre-loaded child, no in-progress load
        var factory = GetRequiredService<ILazyLoadEntityObjectFactory>();
        var childEntity = await factory.Fetch(Guid.NewGuid(), "Child", "child desc");

        parent.LazyChild = new LazyLoad<ILazyLoadEntityObject>(childEntity);
        Assert.IsTrue(parent.LazyChild.IsLoaded, "LazyChild should already be loaded");

        // Act -- WaitForTasks should complete immediately
        await parent.WaitForTasks();

        // Assert -- no exception, no side effects
        Assert.IsTrue(parent.LazyChild.IsLoaded, "LazyChild should still be loaded");
        Assert.AreSame(childEntity, parent.LazyChild.Value);
    }

    [TestMethod]
    public async Task ParentWaitForTasks_UnaccessedChild_CompletesWithoutTrigger()
    {
        // Arrange -- LazyLoad child has never been accessed. Neither Value nor LoadAsync called.
        parent.LazyChild = new LazyLoad<ILazyLoadEntityObject>(async () =>
        {
            // This loader should NOT be invoked by WaitForTasks
            Assert.Fail("Loader should not be invoked by WaitForTasks -- nothing triggers loading except explicit LoadAsync()");
            return await Task.FromResult<ILazyLoadEntityObject?>(null);
        });

        // Act -- call WaitForTasks WITHOUT calling LoadAsync first
        await parent.WaitForTasks();

        // Assert -- no load triggered, child still unloaded
        Assert.IsFalse(parent.LazyChild.IsLoaded, "LazyChild should not be loaded -- WaitForTasks must not trigger loads");
        Assert.IsFalse(parent.LazyChild.IsLoading, "LazyChild should not be loading");
    }
}
