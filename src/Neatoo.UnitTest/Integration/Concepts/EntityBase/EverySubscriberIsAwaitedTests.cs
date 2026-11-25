using System.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.UnitTest.Integration.Concepts.EntityBase.FrameworkRegisters;
using Neatoo.UnitTest.TestInfrastructure;

namespace Neatoo.UnitTest.Integration.Concepts.EntityBase;

/// <summary>
/// NeatooPropertyChanged is an event whose handlers return tasks. Raising it awaits every
/// subscriber's task, not only the last one's: a write is not over while any handler of it is
/// still running, whichever order the handlers subscribed in.
/// </summary>
[TestClass]
public class EverySubscriberIsAwaitedTests : IntegrationTestBase
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

    [TestMethod]
    public async Task AWrite_IsNotOver_WhileAnEarlierSubscriberOfTheObjectIsStillRunning()
    {
        // Two subscribers. The first is slow, the second is done at once. A multicast delegate
        // returns only the second one's task.
        var slow = NewGate();
        root.NeatooPropertyChanged += _ => slow.Task;
        root.NeatooPropertyChanged += _ => Task.CompletedTask;

        var writing = root[nameof(IRegisteredRoot.Value)].SetValue("a");

        Assert.IsFalse(writing.IsCompleted, "the first subscriber is still running");
        slow.SetResult();
        await writing.WaitAsync(Limit);
    }

    [TestMethod]
    public async Task AWrite_IsNotOver_WhileItsRulesRun_WhenSomeoneElseAlsoListensToTheProperty()
    {
        // The property's own subscriber list: the property manager, which runs the rules, and
        // then a consumer. The consumer's finished task must not stand in for the rules'.
        var rule = NewGate();
        root.Gate = () => rule.Task;
        var property = root[nameof(IRegisteredRoot.Value)];
        property.NeatooPropertyChanged += _ => Task.CompletedTask;

        var writing = property.SetValue("a");

        Assert.IsFalse(writing.IsCompleted, "the rule is still running");
        Assert.IsTrue(root.IsBusy, "and the object knows it");
        rule.SetResult();
        await writing.WaitAsync(Limit);
        await root.WaitForTasks().WaitAsync(Limit);
        Assert.IsFalse(root.IsBusy);
    }

    [TestMethod]
    public Task AWrite_ReportsWhatAnEarlierSubscriberThrew() => OneThreadAtATime.Run(async () =>
    {
        // On one thread, as the object runs: the subscriber cannot finish on another thread
        // before the write has returned its task.
        root.NeatooPropertyChanged += async _ =>
        {
            await Task.Yield();
            throw new InvalidOperationException("Subscriber failed");
        };
        root.NeatooPropertyChanged += _ => Task.CompletedTask;

        var writing = root[nameof(IRegisteredRoot.Value)].SetValue("a");

        var thrown = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => writing.WaitAsync(Limit));
        Assert.AreEqual("Subscriber failed", thrown.Message);
    });

    [TestMethod]
    public async Task ASubscriberThatReturnsNoTask_IsTreatedAsFinished()
    {
        // Code compiled without nullable checks can return null from a handler. It always could.
        root.NeatooPropertyChanged += _ => null!;
        var alone = root[nameof(IRegisteredRoot.Value)].SetValue("a");
        await alone.WaitAsync(Limit);

        root.NeatooPropertyChanged += _ => Task.CompletedTask;
        var withAnother = root[nameof(IRegisteredRoot.Value)].SetValue("b");
        await withAnother.WaitAsync(Limit);

        Assert.AreEqual("b", root.Value);
    }

    [TestMethod]
    public Task TheIdleAnnouncement_IsRaisedBeforeTheWaitCompletes() => OneThreadAtATime.Run(async () =>
    {
        var rule = NewGate();
        root.Gate = () => rule.Task;
        Task? wait = null;
        bool? waitWasDoneWhenIdleWasAnnounced = null;
        ((INotifyPropertyChanged)root).PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IRegisteredRoot.IsBusy) && !root.IsBusy)
            {
                waitWasDoneWhenIdleWasAnnounced = wait!.IsCompleted;
            }
        };

        _ = root[nameof(IRegisteredRoot.Value)].SetValue("a");
        wait = root.WaitForTasks();
        rule.SetResult();
        await wait.WaitAsync(Limit);

        Assert.AreEqual(false, waitWasDoneWhenIdleWasAnnounced, "idle was announced, and the wait had not completed yet");
        Assert.IsFalse(root.IsBusy);
    });
}
