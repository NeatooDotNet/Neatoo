using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Internal;

namespace Neatoo.UnitTest.Unit.Core;

/// <summary>
/// A faulted sequence is reported once, to the wait that observes it, and the tracker then
/// tracks and waits for later work.
/// </summary>
[TestClass]
public class AsyncTasksFaultRecoveryTests
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(5);

    private static async Task UntilIdle(AsyncTasks tracker)
    {
        using var limit = new CancellationTokenSource(Limit);
        while (tracker.IsRunning)
        {
            await Task.Delay(5, limit.Token);
        }
    }

    [TestMethod]
    public async Task AddTask_AfterAFaultedSequence_IsTracked_AndItsWaitCompletesCleanly()
    {
        var tracker = new AsyncTasks();
        var first = new TaskCompletionSource();
        tracker.AddTask(first.Task);
        var firstWait = tracker.AllDone;
        first.SetException(new InvalidOperationException("first"));
        await Assert.ThrowsExactlyAsync<AggregateException>(() => firstWait.WaitAsync(Limit));

        var second = new TaskCompletionSource();
        tracker.AddTask(second.Task);

        Assert.IsTrue(tracker.IsRunning, "the task added after the fault is tracked");
        var secondWait = tracker.AllDone;
        Assert.IsFalse(secondWait.IsCompleted, "the wait is for the new task");

        second.SetResult();
        await secondWait.WaitAsync(Limit);

        Assert.IsFalse(tracker.IsRunning);
    }

    [TestMethod]
    public async Task Fault_ObservedByAWaitInProgress_IsNotThrownByALaterWait()
    {
        var tracker = new AsyncTasks();
        var work = new TaskCompletionSource();
        tracker.AddTask(work.Task);
        var waitInProgress = tracker.AllDone;
        work.SetException(new InvalidOperationException("observed"));
        await Assert.ThrowsExactlyAsync<AggregateException>(() => waitInProgress.WaitAsync(Limit));

        await tracker.AllDone.WaitAsync(Limit);
    }

    [TestMethod]
    public async Task Fault_NobodyWasWaitingFor_IsThrownByTheNextWait_Once()
    {
        var tracker = new AsyncTasks();
        var work = new TaskCompletionSource();
        tracker.AddTask(work.Task);
        work.SetException(new InvalidOperationException("unobserved"));
        await UntilIdle(tracker);

        var thrown = await Assert.ThrowsExactlyAsync<AggregateException>(() => tracker.AllDone.WaitAsync(Limit));
        Assert.AreEqual("unobserved", thrown.InnerExceptions.Single().Message);

        await tracker.AllDone.WaitAsync(Limit);
    }

    [TestMethod]
    public async Task Fault_NotObservedBeforeTheNextSequence_IsReportedWithThatSequence()
    {
        var tracker = new AsyncTasks();
        var first = new TaskCompletionSource();
        tracker.AddTask(first.Task);
        first.SetException(new InvalidOperationException("carried"));
        await UntilIdle(tracker);

        var second = new TaskCompletionSource();
        tracker.AddTask(second.Task);
        var wait = tracker.AllDone;
        Assert.IsFalse(wait.IsCompleted, "the wait is for the new task");
        second.SetResult();

        var thrown = await Assert.ThrowsExactlyAsync<AggregateException>(() => wait.WaitAsync(Limit));
        Assert.AreEqual("carried", thrown.InnerExceptions.Single().Message);

        await tracker.AllDone.WaitAsync(Limit);
    }

    [TestMethod]
    public async Task CompletionCallback_ThatThrowsAnyException_FaultsTheWait()
    {
        var tracker = new AsyncTasks
        {
            OnFullSequenceComplete = () => throw new InvalidOperationException("callback")
        };
        var work = new TaskCompletionSource();
        tracker.AddTask(work.Task);
        var wait = tracker.AllDone;
        work.SetResult();

        var thrown = await Assert.ThrowsExactlyAsync<AggregateException>(() => wait.WaitAsync(Limit));
        Assert.AreEqual("callback", thrown.InnerExceptions.Single().Message);
    }
}
