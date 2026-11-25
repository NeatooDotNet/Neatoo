using System.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.UnitTest.Integration.Concepts.EntityBase.FrameworkRegisters;
using Neatoo.UnitTest.TestInfrastructure;

namespace Neatoo.UnitTest.Integration.Concepts.EntityBase;

/// <summary>
/// Waiting awaits tasks, and busy reads tasks: WaitForTasks() is the object's running tasks and
/// nothing else, the token overload is the same wait with cancellation, and when the wait
/// completes nothing on the object or below it is busy.
/// </summary>
[TestClass]
public class WaitsAwaitTasksTests : IntegrationTestBase
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(5);

    private IRegisteredRoot root = null!;
    private readonly List<TaskCompletionSource> gates = new();

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
    }

    private TaskCompletionSource NewGate()
    {
        var gate = new TaskCompletionSource();
        gates.Add(gate);
        return gate;
    }

    private TaskCompletionSource HoldTheRootsRule()
    {
        var gate = NewGate();
        root.Gate = () => gate.Task;
        return gate;
    }

    [TestMethod]
    [Timeout(20000, CooperativeCancellation = true)]
    public Task OnOneThread_WaitForTasks_ReturnsAnIncompleteTask_AndCompletesWhenTheWorkDoes() => OneThreadAtATime.Run(async () =>
    {
        // However the work was started. On one thread a wait that polled could never return.
        var starts = new (string Name, Action Start)[]
        {
            ("the setter", () => root.Value = "a"),
            ("SetValue on the property", () => _ = root[nameof(IRegisteredRoot.Value)].SetValue("b")),
            ("RunRules by property name", () => _ = root.RunRules(nameof(IRegisteredRoot.Value))),
            ("RunRules with a flag", () => _ = root.RunRules()),
        };

        foreach (var (name, start) in starts)
        {
            var gate = HoldTheRootsRule();

            start();
            var wait = root.WaitForTasks();
            Assert.IsFalse(wait.IsCompleted, $"work started by {name} is in flight");
            Assert.IsTrue(root.IsBusy, $"busy while work started by {name} is in flight");

            gate.SetResult();
            await wait.WaitAsync(Limit);
            Assert.IsFalse(root.IsBusy, $"not busy once the wait for work started by {name} completes");
        }
    });

    [TestMethod]
    public async Task RunRulesWithAFlag_NotAwaited_IsInTheObjectsRunningTasks()
    {
        var gate = HoldTheRootsRule();
        using var neverCancelled = new CancellationTokenSource();

        var running = root.RunRules();
        var waiting = root.WaitForTasks(neverCancelled.Token);
        Assert.IsFalse(waiting.IsCompleted, "the object's running tasks hold the rules");

        gate.SetResult();
        await waiting.WaitAsync(Limit);
        await running.WaitAsync(Limit);
    }

    [TestMethod]
    public async Task TheTwoWaits_WaitForTheSameWork()
    {
        var gate = HoldTheRootsRule();
        using var neverCancelled = new CancellationTokenSource();

        _ = root[nameof(IRegisteredRoot.Value)].SetValue("a");
        var plain = root.WaitForTasks();
        var withToken = root.WaitForTasks(neverCancelled.Token);
        Assert.IsFalse(plain.IsCompleted);
        Assert.IsFalse(withToken.IsCompleted);

        gate.SetResult();
        await plain.WaitAsync(Limit);
        await withToken.WaitAsync(Limit);
    }

    [TestMethod]
    public async Task ATokenWait_Cancelled_Throws_AndTheWorkStillFinishes()
    {
        var gate = HoldTheRootsRule();
        using var cancelled = new CancellationTokenSource();

        root.Value = "a";
        var waiting = root.WaitForTasks(cancelled.Token);
        cancelled.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => waiting.WaitAsync(Limit));

        Assert.IsTrue(root.IsBusy, "cancelling the wait does not cancel the work");
        gate.SetResult();
        await root.WaitForTasks().WaitAsync(Limit);
        Assert.IsFalse(root.IsBusy);
    }

    [TestMethod]
    public Task WhenTheRootsWaitCompletes_NothingOnItOrBelowItIsBusy() => OneThreadAtATime.Run(async () =>
    {
        var child = root.Child!;
        var item = root.Children![0];
        var childGate = NewGate();
        var itemGate = NewGate();
        child.Gate = () => childGate.Task;
        item.Gate = () => itemGate.Task;

        _ = child[nameof(IRegisteredChild.Value)].SetValue("a");
        _ = item[nameof(IRegisteredChild.Value)].SetValue("b");

        Assert.IsTrue(root.IsBusy, "the root is busy while work below it is in flight");
        Assert.IsTrue(root.Children.IsBusy, "the list is busy while one of its items is");

        var wait = root.WaitForTasks();
        childGate.SetResult();
        Assert.IsFalse(wait.IsCompleted, "the item's work is still in flight");
        itemGate.SetResult();
        await wait.WaitAsync(Limit);

        Assert.IsFalse(root.IsBusy, "the root");
        Assert.IsFalse(child.IsBusy, "the child");
        Assert.IsFalse(item.IsBusy, "the list item");
        Assert.IsFalse(root.Children.IsBusy, "the list");
        Assert.IsFalse(child[nameof(IRegisteredChild.Value)].IsBusy, "the child's property");
        Assert.IsFalse(root[nameof(IRegisteredRoot.Child)].IsBusy, "the root's property that holds the child");
    });

    [TestMethod]
    public Task AListsWait_ReturnsAnIncompleteTaskWhileAnItemIsBusy_AndCompletesWhenItIsNot() => OneThreadAtATime.Run(async () =>
    {
        var item = root.Children![0];
        var gate = NewGate();
        item.Gate = () => gate.Task;

        _ = item[nameof(IRegisteredChild.Value)].SetValue("a");
        var wait = root.Children.WaitForTasks();
        Assert.IsFalse(wait.IsCompleted);

        gate.SetResult();
        await wait.WaitAsync(Limit);
        Assert.IsFalse(root.Children.IsBusy);
    });

    [TestMethod]
    public Task AnObject_AnnouncesBusyOnceAndIdleOnce_ForAWriteThroughTheProperty() => OneThreadAtATime.Run(async () =>
    {
        var gate = HoldTheRootsRule();
        var busyAtEachNotification = new List<bool>();
        ((INotifyPropertyChanged)root).PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IRegisteredRoot.IsBusy))
            {
                busyAtEachNotification.Add(root.IsBusy);
            }
        };

        _ = root[nameof(IRegisteredRoot.Value)].SetValue("a");
        gate.SetResult();
        await root.WaitForTasks().WaitAsync(Limit);

        CollectionAssert.AreEqual(new[] { true, false }, busyAtEachNotification);
    });
}
