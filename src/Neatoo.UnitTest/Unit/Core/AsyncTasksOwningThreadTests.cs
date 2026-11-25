using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Internal;
using Neatoo.UnitTest.TestInfrastructure;

namespace Neatoo.UnitTest.Unit.Core;

/// <summary>
/// The tracker accounts for a task where the task was added, not where it finished. An object
/// graph has one owning thread; a reply that arrives on another thread must not move the
/// "sequence complete" callback, and the notifications raised from it, off the owning thread.
/// </summary>
[TestClass]
public class AsyncTasksOwningThreadTests
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(5);

    [TestMethod]
    public async Task CompletionCallback_RunsOnTheThreadThatAddedTheTask_WhenTheTaskFinishesOnAnother()
    {
        int? owningThread = null;
        int? callbackThread = null;

        await OneThreadAtATime.Run(async thread =>
        {
            owningThread = thread;
            var tracker = new AsyncTasks();
            tracker.OnFullSequenceComplete = () =>
            {
                callbackThread = Environment.CurrentManagedThreadId;
                return Task.CompletedTask;
            };
            var work = new TaskCompletionSource();

            _ = tracker.AddTask(work.Task);
            await Task.Run(() => work.SetResult());
            await tracker.AllDone.WaitAsync(Limit);
        });

        Assert.IsNotNull(callbackThread, "the callback ran");
        Assert.AreEqual(owningThread, callbackThread);
    }
}
