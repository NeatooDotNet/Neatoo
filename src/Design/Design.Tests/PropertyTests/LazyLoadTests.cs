// -----------------------------------------------------------------------------
// Design.Tests - LazyLoad Constructor Pattern Tests
// -----------------------------------------------------------------------------
// Pins PropertySystem/LazyLoadProperty.cs: an EntityLazyLoad created in the
// constructor loads its child on the first LoadAsync, reading the parent's Id
// at load time; Value is a passive read.
// -----------------------------------------------------------------------------

using Design.Domain.PropertySystem;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Design.Tests.PropertyTests;

[TestClass]
public class LazyLoadTests
{
    private IServiceScope _scope = null!;
    private ILazyLoadParentDemoFactory _factory = null!;
    private MockLazyLoadChildRepository _childRepository = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _scope = DesignTestServices.GetScope();
        _factory = _scope.GetRequiredService<ILazyLoadParentDemoFactory>();
        _childRepository = (MockLazyLoadChildRepository)_scope.GetRequiredService<ILazyLoadChildRepository>();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _scope.Dispose();
    }

    #region skill-lazy-load-test
    [TestMethod]
    public async Task Details_LoadOnFirstLoadAsync_UsingTheParentsId()
    {
        var id = Guid.NewGuid();
        var parent = await _factory.Fetch(id);

        // Value is a passive read: nothing is loaded until asked
        Assert.IsFalse(parent.Details.IsLoaded);
        Assert.IsNull(parent.Details.Value);

        var details = await parent.Details.LoadAsync();

        Assert.IsNotNull(details);
        Assert.AreEqual(id, details.ParentId, "The loader read this.Id at load time, not in the constructor");
        Assert.IsTrue(parent.Details.IsLoaded);
        Assert.AreSame(details, parent.Details.Value);
    }
    #endregion

    #region skill-lazy-load-set-value-test
    [TestMethod]
    public void Create_PreloadsDetails_SoThereIsNothingToLoad()
    {
        var parent = _factory.Create();

        Assert.IsTrue(parent.Details.IsLoaded);
        Assert.IsNotNull(parent.Details.Value);
        Assert.AreEqual(parent.Id, parent.Details.Value!.ParentId);
        Assert.AreEqual(0, _childRepository.LoadCount, "SetValue bypassed the loader");
    }
    #endregion

    #region skill-lazy-load-wait-for-tasks
    [TestMethod]
    public async Task WaitForTasks_AwaitsAFireAndForgetLoad()
    {
        var parent = await _factory.Fetch(Guid.NewGuid());

        // Fire-and-forget, as a page does in OnInitializedAsync so rendering is not blocked
        _ = parent.Details.LoadAsync();

        // WaitForTasks awaits the in-progress load; it never starts one
        await parent.WaitForTasks();

        Assert.IsTrue(parent.Details.IsLoaded);
        Assert.IsNotNull(parent.Details.Value);
    }
    #endregion

    #region skill-lazy-load-error
    [TestMethod]
    public async Task LoadAsync_WhenTheLoaderThrows_RecordsTheErrorAndRethrows()
    {
        // The mock child repository has no details for an empty parent id
        var parent = await _factory.Fetch(Guid.Empty);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => parent.Details.LoadAsync());

        Assert.IsTrue(parent.Details.HasLoadError);
        Assert.IsNotNull(parent.Details.LoadError);
        Assert.IsFalse(parent.Details.IsLoaded, "A failed load is not loaded; it can be retried");
        Assert.IsNull(parent.Details.Value);
    }
    #endregion

    [TestMethod]
    public async Task LoadAsync_IsIdempotent()
    {
        var parent = await _factory.Fetch(Guid.NewGuid());

        await parent.Details.LoadAsync();
        await parent.Details.LoadAsync();

        Assert.AreEqual(1, _childRepository.LoadCount, "The loader runs once; later calls return the cached value");
    }

    [TestMethod]
    public async Task LoadedChildChange_FlowsIntoTheParentsModifiedState()
    {
        var parent = await _factory.Fetch(Guid.NewGuid());
        var details = await parent.Details.LoadAsync();
        Assert.IsFalse(parent.IsModified, "Loading is not a modification");

        details!.Notes = "edited";
        await parent.WaitForTasks();

        Assert.IsTrue(parent.IsModified, "The look-through property carries the child's state up");
    }
}
