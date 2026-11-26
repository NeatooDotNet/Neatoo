using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.UnitTest.Integration.Concepts.EntityBase.FrameworkRegisters;
using Neatoo.UnitTest.Integration.Concepts.Serialization.LazyLoadTests;
using Neatoo.UnitTest.TestInfrastructure;

namespace Neatoo.UnitTest.Integration.Concepts.EntityBase;

/// <summary>
/// The owner registers, not the caller: whatever starts async work on an object, the object's
/// running tasks hold it, and so do its parents'. Each test waits with a token, because that wait
/// is the object's running tasks and nothing else.
/// </summary>
[TestClass]
public class FrameworkRegistersTests : IntegrationTestBase
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(5);

    private IRegisteredRoot root = null!;
    private readonly List<TaskCompletionSource> gates = new();
    private readonly CancellationTokenSource neverCancelled = new();

    [TestInitialize]
    public void TestInitialize()
    {
        InitializeScope();
        root = GetRequiredService<IRegisteredRootFactory>().Create();
    }

    [TestCleanup]
    public void ReleaseEveryGate()
    {
        // A test that failed while a rule was held must not leave it held.
        foreach (var gate in gates)
        {
            gate.TrySetResult();
        }

        neverCancelled.Dispose();
    }

    private TaskCompletionSource NewGate()
    {
        var gate = new TaskCompletionSource();
        gates.Add(gate);
        return gate;
    }

    /// <summary>The object's running tasks, and nothing else.</summary>
    private Task RunningTasksOf(IEntityBase waited)
    {
        return waited.WaitForTasks(neverCancelled.Token);
    }

    private async Task AssertWaitsUntilReleased(IEntityBase waited, TaskCompletionSource gate)
    {
        var waiting = RunningTasksOf(waited);
        Assert.IsFalse(waiting.IsCompleted, "the object's running tasks hold the work");

        gate.SetResult();
        await waiting.WaitAsync(Limit);
    }

    [TestMethod]
    public async Task AWriteThroughTheProperty_IsInTheObjectsRunningTasks()
    {
        // The write a MudNeatoo input makes: through the property, not the generated setter.
        var gate = NewGate();
        root.Gate = () => gate.Task;

        var writing = root[nameof(IRegisteredRoot.Value)].SetValue("a");

        await AssertWaitsUntilReleased(root, gate);
        await writing.WaitAsync(Limit);
    }

    [TestMethod]
    public async Task RunRulesByPropertyName_NotAwaited_IsInTheObjectsRunningTasks()
    {
        var gate = NewGate();
        root.Gate = () => gate.Task;

        var running = root.RunRules(nameof(IRegisteredRoot.Value));

        await AssertWaitsUntilReleased(root, gate);
        await running.WaitAsync(Limit);
    }

    [TestMethod]
    public async Task AWriteThroughAChildsProperty_IsInTheRootsRunningTasks()
    {
        var child = root.Child!;
        var gate = NewGate();
        child.Gate = () => gate.Task;

        var writing = child[nameof(IRegisteredChild.Value)].SetValue("a");

        await AssertWaitsUntilReleased(root, gate);
        await writing.WaitAsync(Limit);
    }

    [TestMethod]
    public async Task AWriteThroughAListItemsProperty_IsInTheRootsRunningTasks()
    {
        var item = root.Children![0];
        var gate = NewGate();
        item.Gate = () => gate.Task;

        var writing = item[nameof(IRegisteredChild.Value)].SetValue("a");

        await AssertWaitsUntilReleased(root, gate);
        await writing.WaitAsync(Limit);
    }

    [TestMethod]
    public async Task ARuleThatThrowsAfterASetterWrite_SurfacesOnce_CarryingTheExceptionOnce()
    {
        var gate = NewGate();
        root.Gate = () => gate.Task;

        root.Value = "throw";
        var wait = root.WaitForTasks();
        gate.SetResult();
        var thrown = await Assert.ThrowsExactlyAsync<AggregateException>(() => wait.WaitAsync(Limit));

        Assert.AreEqual(1, thrown.Flatten().InnerExceptions.Count, "one exception, however many registered tasks carried it");
        await root.WaitForTasks().WaitAsync(Limit);
    }

    [TestMethod]
    public async Task AChildsRuleThatThrows_SurfacesFromTheRoot_CarryingTheExceptionOnce()
    {
        var child = root.Child!;
        var gate = NewGate();
        child.Gate = () => gate.Task;

        child.Value = "throw";
        var wait = root.WaitForTasks();
        gate.SetResult();
        var thrown = await Assert.ThrowsExactlyAsync<AggregateException>(() => wait.WaitAsync(Limit));

        Assert.AreEqual(1, thrown.Flatten().InnerExceptions.Count, "one exception, however many registered tasks carried it");
    }

    [TestMethod]
    public async Task SaveWithAToken_OnARoot_SucceedsAfterAWriteToAChildWithAnAsyncRule()
    {
        // No synchronization context here, so each tracker accounts for the finished write
        // inline. The child must account before the root does: when the root's running tasks
        // are done, nothing below it is still busy.
        Assert.IsNull(SynchronizationContext.Current, "this test is about the case with no synchronization context");
        var child = root.Child!;
        var gate = NewGate();
        child.Gate = () => gate.Task;

        child.Value = "a";
        var saving = root.Save(neverCancelled.Token);
        gate.SetResult();
        await saving.WaitAsync(Limit);

        Assert.AreEqual(1, root.Inserts);
    }

    [TestMethod]
    public void ABusyObject_CannotBeAssignedToAParent_SoAParentNeverHoldsWorkItWasNotToldOf()
    {
        // Why there is no "adopt the work in flight" path: the property refuses a busy child.
        var loose = GetRequiredService<IRegisteredChildFactory>().Create();
        var gate = NewGate();
        loose.Gate = () => gate.Task;
        loose.Value = "a";
        Assert.IsTrue(loose.IsBusy);

        Assert.ThrowsExactly<ChildObjectBusyException>(() => root.Attach(loose));
    }

    [TestMethod]
    public async Task ALazyLoadInProgress_IsInTheParentsRunningTasks()
    {
        var factory = GetRequiredService<ILazyLoadEntityObjectFactory>();
        var parent = await factory.Fetch(Guid.NewGuid(), "Parent", "parent desc");
        var loaded = await factory.Fetch(Guid.NewGuid(), "Child", "child desc");
        var continueLoad = new TaskCompletionSource<ILazyLoadEntityObject?>();
        parent.LazyChild = new EntityLazyLoad<ILazyLoadEntityObject>(async () => await continueLoad.Task);

        _ = parent.LazyChild.LoadAsync();
        var waiting = parent.WaitForTasks(neverCancelled.Token);
        Assert.IsFalse(waiting.IsCompleted, "the parent's running tasks hold the load");

        continueLoad.SetResult(loaded);
        await waiting.WaitAsync(Limit);
    }

    [TestMethod]
    public async Task ALazyLoadReplacedWhileItLoads_NoLongerHoldsTheParentsRunningTasks()
    {
        var factory = GetRequiredService<ILazyLoadEntityObjectFactory>();
        var parent = await factory.Fetch(Guid.NewGuid(), "Parent", "parent desc");
        var loaded = await factory.Fetch(Guid.NewGuid(), "Child", "child desc");
        var neverReplies = new TaskCompletionSource<ILazyLoadEntityObject?>();
        parent.LazyChild = new EntityLazyLoad<ILazyLoadEntityObject>(async () => await neverReplies.Task);
        _ = parent.LazyChild.LoadAsync();

        // The property lets go of the loading wrapper: its load is no longer the parent's work.
        parent.LazyChild = new EntityLazyLoad<ILazyLoadEntityObject>(loaded);

        await parent.WaitForTasks(neverCancelled.Token).WaitAsync(Limit);
        neverReplies.SetResult(null);
    }

    [TestMethod]
    public async Task ALazyLoadAlreadyInProgressWhenItIsAssigned_IsInTheParentsRunningTasks()
    {
        var factory = GetRequiredService<ILazyLoadEntityObjectFactory>();
        var parent = await factory.Fetch(Guid.NewGuid(), "Parent", "parent desc");
        var loaded = await factory.Fetch(Guid.NewGuid(), "Child", "child desc");
        var continueLoad = new TaskCompletionSource<ILazyLoadEntityObject?>();
        var loading = new EntityLazyLoad<ILazyLoadEntityObject>(async () => await continueLoad.Task);
        _ = loading.LoadAsync();

        parent.LazyChild = loading;
        var waiting = parent.WaitForTasks(neverCancelled.Token);
        Assert.IsFalse(waiting.IsCompleted, "the parent's running tasks hold the load");

        continueLoad.SetResult(loaded);
        await waiting.WaitAsync(Limit);
    }

    [TestMethod]
    public async Task ALazyLoadInProgress_OnAValidateObject_IsInItsRunningTasks()
    {
        // The same report from the ValidateBase lazy-load property, which is its own class.
        var owner = await GetRequiredService<ILazyLoadValidateObjectFactory>().Fetch(Guid.NewGuid(), "Owner", "content");
        var continueLoad = new TaskCompletionSource<string?>();
        owner.LazyContent = new EntityLazyLoad<string>(async () => await continueLoad.Task);

        _ = owner.LazyContent.LoadAsync();
        var waiting = owner.WaitForTasks(neverCancelled.Token);
        Assert.IsFalse(waiting.IsCompleted, "the object's running tasks hold the load");

        continueLoad.SetResult("loaded");
        await waiting.WaitAsync(Limit);
    }
}
