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

    /// <summary>
    /// Waits until the running sequence has finished, without reading AllDone, which would make
    /// this caller an observer of its fault. The completion callback runs just before the
    /// sequence closes; the short delay after it lets the sequence close and complete its task.
    /// </summary>
    private static async Task UntilTheSequenceHasClosed(AsyncTasks tracker, Action complete)
    {
        var callbackRan = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        tracker.OnFullSequenceComplete = () =>
        {
            callbackRan.TrySetResult();
            return Task.CompletedTask;
        };

        complete();

        await callbackRan.Task.WaitAsync(Limit);
        await Task.Delay(50);
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
        await UntilTheSequenceHasClosed(tracker, () => work.SetException(new InvalidOperationException("unobserved")));

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
        await UntilTheSequenceHasClosed(tracker, () => first.SetException(new InvalidOperationException("carried")));

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
    public async Task CancelledWait_DoesNotTakeTheFault_TheNextWaitReportsIt()
    {
        var tracker = new AsyncTasks();
        var work = new TaskCompletionSource();
        tracker.AddTask(work.Task);

        using var cancel = new CancellationTokenSource();
        var cancelledWait = tracker.WaitForCompletion(cancel.Token);
        cancel.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => cancelledWait.WaitAsync(Limit));

        await UntilTheSequenceHasClosed(tracker, () => work.SetException(new InvalidOperationException("after the cancel")));

        var thrown = await Assert.ThrowsExactlyAsync<AggregateException>(() => tracker.AllDone.WaitAsync(Limit));
        Assert.AreEqual("after the cancel", thrown.InnerExceptions.Single().Message);

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
