using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Internal;

namespace Neatoo.UnitTest.Unit.Core;

/// <summary>
/// Work started by OnFullSequenceComplete (a rule reacting to the object going idle) keeps the
/// sequence open: IsRunning stays true and AllDone waits for it.
/// </summary>
[TestClass]
public class AsyncTasksCompletionCallbackTests
{
    [TestMethod]
    public async Task TaskAddedByCompletionCallback_KeepsTheSequenceOpenUntilItFinishes()
    {
        var tasks = new AsyncTasks();
        var first = new TaskCompletionSource();
        var second = new TaskCompletionSource();
        var callbackAdded = new SemaphoreSlim(0);
        var added = false;

        tasks.OnFullSequenceComplete = () =>
        {
            if (!added)
            {
                added = true;
                tasks.AddTask(second.Task);
                callbackAdded.Release();
            }

            return Task.CompletedTask;
        };

        tasks.AddTask(first.Task);
        var allDone = tasks.AllDone;
        first.SetResult();

        Assert.IsTrue(await callbackAdded.WaitAsync(TimeSpan.FromSeconds(5)), "the completion callback ran and added work");
        Assert.IsTrue(tasks.IsRunning, "the work the callback added is tracked");
        Assert.IsFalse(allDone.IsCompleted, "the sequence waits for it");

        second.SetResult();
        var finished = await Task.WhenAny(allDone, Task.Delay(TimeSpan.FromSeconds(5)));

        Assert.AreSame(allDone, finished, "the sequence completes once the added work does");
        Assert.IsFalse(tasks.IsRunning);
    }
}
