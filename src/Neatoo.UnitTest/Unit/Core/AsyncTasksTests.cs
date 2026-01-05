using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Internal;

namespace Neatoo.UnitTest.Unit.Core;

/// <summary>
/// Unit tests for the AsyncTasks class.
/// Tests task sequencing, completion handling, exception aggregation, and state management.
/// </summary>
[TestClass]
public class AsyncTasksTests
{
    #region AddTask - Completed Task Tests

    [TestMethod]
    public async Task AddTask_CompletedTask_ReturnsImmediately()
    {
        // Arrange
        var asyncTasks = new AsyncTasks();
        var completedTask = Task.CompletedTask;

        // Act
        var result = asyncTasks.AddTask(completedTask);

        // Assert
        Assert.IsTrue(result.IsCompleted, "Adding a completed task should return immediately");
        await result; // Should not throw
    }

    [TestMethod]
    public async Task AddTask_CompletedTaskWithResult_ReturnsImmediately()
    {
        // Arrange
        var asyncTasks = new AsyncTasks();
        var completedTask = Task.FromResult(42);

        // Act
        var result = asyncTasks.AddTask(completedTask);

        // Assert
        Assert.IsTrue(result.IsCompleted);
        await result; // Should not throw
    }

    [TestMethod]
    public void AddTask_CompletedTask_IsRunningRemainsFalse()
    {
        // Arrange
        var asyncTasks = new AsyncTasks();
        var completedTask = Task.CompletedTask;

        // Act
        asyncTasks.AddTask(completedTask);

        // Assert
        Assert.IsFalse(asyncTasks.IsRunning, "IsRunning should remain false for completed tasks");
    }

    #endregion

    #region AddTask - Incomplete Task Tests

    [TestMethod]
    public async Task AddTask_IncompleteTask_SetsIsRunningTrue()
    {
        // Arrange
        var asyncTasks = new AsyncTasks();
        var tcs = new TaskCompletionSource<bool>();

        // Act
        asyncTasks.AddTask(tcs.Task);

        // Assert
        Assert.IsTrue(asyncTasks.IsRunning, "IsRunning should be true when an incomplete task is added");

        // Cleanup
        tcs.SetResult(true);
        await asyncTasks.AllDone;
    }

    [TestMethod]
    public async Task AddTask_IncompleteTask_ReturnsWrappedTask()
    {
        // Arrange
        var asyncTasks = new AsyncTasks();
        var tcs = new TaskCompletionSource<bool>();

        // Act
        var result = asyncTasks.AddTask(tcs.Task);

        // Assert
        Assert.IsFalse(result.IsCompleted, "Returned task should not be completed yet");

        // Cleanup
        tcs.SetResult(true);
        await asyncTasks.AllDone;
    }

    [TestMethod]
    public async Task AddTask_IncompleteTask_WhenCompleted_SetsIsRunningFalse()
    {
        // Arrange
        var asyncTasks = new AsyncTasks();
        var tcs = new TaskCompletionSource<bool>();
        asyncTasks.AddTask(tcs.Task);

        // Act
        tcs.SetResult(true);
        await asyncTasks.AllDone;

        // Assert
        Assert.IsFalse(asyncTasks.IsRunning, "IsRunning should be false after task completes");
    }

    #endregion

    #region AllDone Tests

    [TestMethod]
    public void AllDone_NoTasksAdded_ReturnsCompletedTask()
    {
        // Arrange
        var asyncTasks = new AsyncTasks();

        // Act
        var allDone = asyncTasks.AllDone;

        // Assert
        Assert.IsTrue(allDone.IsCompleted, "AllDone should return completed task when no tasks added");
    }

    [TestMethod]
    public async Task AllDone_SingleTask_CompletesWhenTaskCompletes()
    {
        // Arrange
        var asyncTasks = new AsyncTasks();
        var tcs = new TaskCompletionSource<bool>();
        asyncTasks.AddTask(tcs.Task);

        // Act
        var allDone = asyncTasks.AllDone;
        Assert.IsFalse(allDone.IsCompleted, "AllDone should not be completed before task completes");

        tcs.SetResult(true);
        await allDone;

        // Assert
        Assert.IsTrue(allDone.IsCompleted, "AllDone should be completed after task completes");
    }

    [TestMethod]
    public async Task AllDone_MultipleTasks_CompletesWhenAllTasksComplete()
    {
        // Arrange
        var asyncTasks = new AsyncTasks();
        var tcs1 = new TaskCompletionSource<bool>();
        var tcs2 = new TaskCompletionSource<bool>();
        var tcs3 = new TaskCompletionSource<bool>();

        asyncTasks.AddTask(tcs1.Task);
        asyncTasks.AddTask(tcs2.Task);
        asyncTasks.AddTask(tcs3.Task);

        // Act & Assert
        var allDone = asyncTasks.AllDone;
        Assert.IsFalse(allDone.IsCompleted);

        tcs1.SetResult(true);
        Assert.IsFalse(allDone.IsCompleted, "AllDone should not complete with only first task done");

        tcs2.SetResult(true);
        Assert.IsFalse(allDone.IsCompleted, "AllDone should not complete with two tasks done");

        tcs3.SetResult(true);
        await allDone;
        Assert.IsTrue(allDone.IsCompleted, "AllDone should complete when all tasks are done");
    }

    [TestMethod]
    public async Task AllDone_TasksCompleteInReverseOrder_StillCompletesCorrectly()
    {
        // Arrange
        var asyncTasks = new AsyncTasks();
        var tcs1 = new TaskCompletionSource<bool>();
        var tcs2 = new TaskCompletionSource<bool>();
        var tcs3 = new TaskCompletionSource<bool>();

        asyncTasks.AddTask(tcs1.Task);
        asyncTasks.AddTask(tcs2.Task);
        asyncTasks.AddTask(tcs3.Task);

        // Act - Complete in reverse order
        var allDone = asyncTasks.AllDone;
        tcs3.SetResult(true);
        tcs2.SetResult(true);
        tcs1.SetResult(true);
        await allDone;

        // Assert
        Assert.IsTrue(allDone.IsCompleted);
        Assert.IsFalse(asyncTasks.IsRunning);
    }

    #endregion

    #region Multiple Tasks Tracked Concurrently Tests

    [TestMethod]
    public async Task AddTask_MultipleConcurrentTasks_AllTracked()
    {
        // Arrange
        var asyncTasks = new AsyncTasks();
        var taskCompletionSources = new List<TaskCompletionSource<bool>>();
        const int taskCount = 10;

        // Act
        for (int i = 0; i < taskCount; i++)
        {
            var tcs = new TaskCompletionSource<bool>();
            taskCompletionSources.Add(tcs);
            asyncTasks.AddTask(tcs.Task);
        }

        // Assert
        Assert.IsTrue(asyncTasks.IsRunning);
        var allDone = asyncTasks.AllDone;
        Assert.IsFalse(allDone.IsCompleted);

        // Complete all tasks
        foreach (var tcs in taskCompletionSources)
        {
            tcs.SetResult(true);
        }

        await allDone;
        Assert.IsTrue(allDone.IsCompleted);
        Assert.IsFalse(asyncTasks.IsRunning);
    }

    [TestMethod]
    public async Task AddTask_ConcurrentAdditions_HandledCorrectly()
    {
        // Arrange
        var asyncTasks = new AsyncTasks();
        var taskCompletionSources = new List<TaskCompletionSource<bool>>();
        const int taskCount = 50;

        // Act - Add tasks concurrently from multiple threads
        var addTasks = Enumerable.Range(0, taskCount).Select(_ =>
        {
            return Task.Run(() =>
            {
                var tcs = new TaskCompletionSource<bool>();
                lock (taskCompletionSources)
                {
                    taskCompletionSources.Add(tcs);
                }
                asyncTasks.AddTask(tcs.Task);
            });
        });

        await Task.WhenAll(addTasks);

        // Assert
        Assert.IsTrue(asyncTasks.IsRunning);

        // Complete all tasks
        foreach (var tcs in taskCompletionSources)
        {
            tcs.SetResult(true);
        }

        await asyncTasks.AllDone;
        Assert.IsFalse(asyncTasks.IsRunning);
    }

    #endregion

    #region Exception Aggregation Tests

    [TestMethod]
    public async Task AddTask_SingleFaultedTask_ThrowsAggregateException()
    {
        // Arrange
        var asyncTasks = new AsyncTasks();
        var tcs = new TaskCompletionSource<bool>();
        var expectedException = new InvalidOperationException("Test exception");

        asyncTasks.AddTask(tcs.Task);
        tcs.SetException(expectedException);

        // Act & Assert
        var allDone = asyncTasks.AllDone;
        var aggregateException = await Assert.ThrowsExceptionAsync<AggregateException>(async () => await allDone);

        Assert.AreEqual(1, aggregateException.InnerExceptions.Count);
        Assert.IsInstanceOfType(aggregateException.InnerExceptions[0], typeof(InvalidOperationException));
        Assert.AreEqual("Test exception", aggregateException.InnerExceptions[0].Message);
    }

    [TestMethod]
    public async Task AddTask_MultipleFaultedTasks_AggregatesAllExceptions()
    {
        // Arrange
        var asyncTasks = new AsyncTasks();
        var tcs1 = new TaskCompletionSource<bool>();
        var tcs2 = new TaskCompletionSource<bool>();
        var tcs3 = new TaskCompletionSource<bool>();

        asyncTasks.AddTask(tcs1.Task);
        asyncTasks.AddTask(tcs2.Task);
        asyncTasks.AddTask(tcs3.Task);

        var exception1 = new InvalidOperationException("Exception 1");
        var exception2 = new ArgumentException("Exception 2");
        var exception3 = new NotSupportedException("Exception 3");

        tcs1.SetException(exception1);
        tcs2.SetException(exception2);
        tcs3.SetException(exception3);

        // Act & Assert
        var allDone = asyncTasks.AllDone;
        var aggregateException = await Assert.ThrowsExceptionAsync<AggregateException>(async () => await allDone);

        Assert.AreEqual(3, aggregateException.InnerExceptions.Count);
        Assert.IsTrue(aggregateException.InnerExceptions.Any(e => e is InvalidOperationException));
        Assert.IsTrue(aggregateException.InnerExceptions.Any(e => e is ArgumentException));
        Assert.IsTrue(aggregateException.InnerExceptions.Any(e => e is NotSupportedException));
    }

    [TestMethod]
    public async Task AddTask_MixedSuccessAndFaultedTasks_AggregatesOnlyExceptions()
    {
        // Arrange
        var asyncTasks = new AsyncTasks();
        var tcs1 = new TaskCompletionSource<bool>();
        var tcs2 = new TaskCompletionSource<bool>();
        var tcs3 = new TaskCompletionSource<bool>();

        asyncTasks.AddTask(tcs1.Task);
        asyncTasks.AddTask(tcs2.Task);
        asyncTasks.AddTask(tcs3.Task);

        tcs1.SetResult(true); // Success
        tcs2.SetException(new InvalidOperationException("Only failure"));
        tcs3.SetResult(true); // Success

        // Act & Assert
        var allDone = asyncTasks.AllDone;
        var aggregateException = await Assert.ThrowsExceptionAsync<AggregateException>(async () => await allDone);

        Assert.AreEqual(1, aggregateException.InnerExceptions.Count);
        Assert.IsInstanceOfType(aggregateException.InnerExceptions[0], typeof(InvalidOperationException));
    }

    [TestMethod]
    public void AddTask_TaskWithExceptionAlreadySet_ThrowsImmediately()
    {
        // Arrange
        var asyncTasks = new AsyncTasks();
        var tcs = new TaskCompletionSource<bool>();
        tcs.SetException(new InvalidOperationException("Pre-faulted"));
        var faultedTask = tcs.Task;

        // Act & Assert
        var exception = Assert.ThrowsException<AggregateException>(() => asyncTasks.AddTask(faultedTask));
        Assert.IsInstanceOfType(exception.InnerException, typeof(InvalidOperationException));
    }

    #endregion

    #region OnFullSequenceComplete Callback Tests

    [TestMethod]
    public async Task OnFullSequenceComplete_SingleTask_InvokedWhenAllTasksComplete()
    {
        // Arrange
        var asyncTasks = new AsyncTasks();
        var callbackInvoked = false;
        asyncTasks.OnFullSequenceComplete = () =>
        {
            callbackInvoked = true;
            return Task.CompletedTask;
        };

        var tcs = new TaskCompletionSource<bool>();
        asyncTasks.AddTask(tcs.Task);

        // Act
        tcs.SetResult(true);
        await asyncTasks.AllDone;

        // Assert
        Assert.IsTrue(callbackInvoked, "OnFullSequenceComplete should be invoked when sequence completes");
    }

    [TestMethod]
    public async Task OnFullSequenceComplete_MultipleTasks_InvokedOnlyOnce()
    {
        // Arrange
        var asyncTasks = new AsyncTasks();
        var callbackCount = 0;
        asyncTasks.OnFullSequenceComplete = () =>
        {
            callbackCount++;
            return Task.CompletedTask;
        };

        var tcs1 = new TaskCompletionSource<bool>();
        var tcs2 = new TaskCompletionSource<bool>();
        asyncTasks.AddTask(tcs1.Task);
        asyncTasks.AddTask(tcs2.Task);

        // Act
        tcs1.SetResult(true);
        tcs2.SetResult(true);
        await asyncTasks.AllDone;

        // Assert
        Assert.AreEqual(1, callbackCount, "OnFullSequenceComplete should be invoked exactly once");
    }

    [TestMethod]
    public async Task OnFullSequenceComplete_ThrowsException_ExceptionIsAggregated()
    {
        // Arrange
        var asyncTasks = new AsyncTasks();
        asyncTasks.OnFullSequenceComplete = () =>
        {
            throw new AggregateException(new InvalidOperationException("Callback failed"));
        };

        var tcs = new TaskCompletionSource<bool>();
        asyncTasks.AddTask(tcs.Task);
        tcs.SetResult(true);

        // Act & Assert
        var allDone = asyncTasks.AllDone;
        var aggregateException = await Assert.ThrowsExceptionAsync<AggregateException>(async () => await allDone);

        Assert.IsTrue(aggregateException.InnerExceptions.Any(e => e is InvalidOperationException));
    }

    [TestMethod]
    public async Task OnFullSequenceComplete_AsyncCallback_Awaited()
    {
        // Arrange
        var asyncTasks = new AsyncTasks();
        var callbackCompleted = false;
        asyncTasks.OnFullSequenceComplete = async () =>
        {
            await Task.Delay(50);
            callbackCompleted = true;
        };

        var tcs = new TaskCompletionSource<bool>();
        asyncTasks.AddTask(tcs.Task);

        // Act
        tcs.SetResult(true);
        await asyncTasks.AllDone;

        // Assert
        Assert.IsTrue(callbackCompleted, "Async callback should be awaited");
    }

    [TestMethod]
    public async Task OnFullSequenceComplete_DefaultCallback_DoesNotThrow()
    {
        // Arrange
        var asyncTasks = new AsyncTasks();
        // Default callback is () => Task.CompletedTask

        var tcs = new TaskCompletionSource<bool>();
        asyncTasks.AddTask(tcs.Task);

        // Act
        tcs.SetResult(true);
        await asyncTasks.AllDone;

        // Assert - No exception should be thrown
        Assert.IsFalse(asyncTasks.IsRunning);
    }

    #endregion

    #region Faulted Sequencer Behavior Tests

    [TestMethod]
    public async Task AddTask_WhileSequencerHasPendingTasks_TaskIsTracked()
    {
        // Arrange
        var asyncTasks = new AsyncTasks();
        var tcsKeeper = new TaskCompletionSource<bool>(); // Keeps the sequence running
        var tcs1 = new TaskCompletionSource<bool>();
        var tcs2 = new TaskCompletionSource<bool>();

        // Add a "keeper" task first to ensure the sequence stays running
        asyncTasks.AddTask(tcsKeeper.Task);
        asyncTasks.AddTask(tcs1.Task);

        // Fault tcs1 - the sequence stays running because tcsKeeper is still pending
        tcs1.SetException(new InvalidOperationException("First failure"));

        // Add a second task while the sequencer still has tasks pending
        var result = asyncTasks.AddTask(tcs2.Task);

        // The second task should be tracked since sequencer is still running
        Assert.IsFalse(result.IsFaulted, "Task added while sequencer running should be tracked");
        Assert.IsTrue(asyncTasks.IsRunning);

        // Cleanup
        tcs2.SetResult(true);
        tcsKeeper.SetResult(true);
        try { await asyncTasks.AllDone; } catch (AggregateException) { }
    }

    [TestMethod]
    public async Task AddTask_AfterSequenceCompletesWithSuccess_StartsNewSequence()
    {
        // Arrange
        var asyncTasks = new AsyncTasks();
        var tcs1 = new TaskCompletionSource<bool>();
        asyncTasks.AddTask(tcs1.Task);
        tcs1.SetResult(true);

        await asyncTasks.AllDone;

        // Act - After sequence completes successfully, a new sequence can start
        var tcs2 = new TaskCompletionSource<bool>();
        asyncTasks.AddTask(tcs2.Task);

        // Assert - A new sequence starts because the previous one completed
        Assert.IsTrue(asyncTasks.IsRunning, "New sequence should start after previous sequence completes");

        // Cleanup
        tcs2.SetResult(true);
        await asyncTasks.AllDone;
    }

    [TestMethod]
    public async Task AddTask_ExceptionAccumulatesAcrossTasks_AllExceptionsReported()
    {
        // Arrange
        var asyncTasks = new AsyncTasks();
        var tcs1 = new TaskCompletionSource<bool>();
        var tcs2 = new TaskCompletionSource<bool>();

        asyncTasks.AddTask(tcs1.Task);
        asyncTasks.AddTask(tcs2.Task);

        // Act
        tcs1.SetException(new InvalidOperationException("Error 1"));
        tcs2.SetException(new ArgumentException("Error 2"));

        // Assert
        var allDone = asyncTasks.AllDone;
        var aggregateException = await Assert.ThrowsExceptionAsync<AggregateException>(async () => await allDone);
        Assert.AreEqual(2, aggregateException.InnerExceptions.Count);
    }

    #endregion

    #region IsRunning State Transition Tests

    [TestMethod]
    public void IsRunning_InitialState_IsFalse()
    {
        // Arrange & Act
        var asyncTasks = new AsyncTasks();

        // Assert
        Assert.IsFalse(asyncTasks.IsRunning);
    }

    [TestMethod]
    public async Task IsRunning_AfterAddingIncompleteTask_IsTrue()
    {
        // Arrange
        var asyncTasks = new AsyncTasks();
        var tcs = new TaskCompletionSource<bool>();

        // Act
        asyncTasks.AddTask(tcs.Task);

        // Assert
        Assert.IsTrue(asyncTasks.IsRunning);

        // Cleanup
        tcs.SetResult(true);
        await asyncTasks.AllDone;
    }

    [TestMethod]
    public async Task IsRunning_AfterAllTasksComplete_IsFalse()
    {
        // Arrange
        var asyncTasks = new AsyncTasks();
        var tcs = new TaskCompletionSource<bool>();
        asyncTasks.AddTask(tcs.Task);

        // Act
        tcs.SetResult(true);
        await asyncTasks.AllDone;

        // Assert
        Assert.IsFalse(asyncTasks.IsRunning);
    }

    [TestMethod]
    public async Task IsRunning_TransitionsFromFalseToTrueToFalse()
    {
        // Arrange
        var asyncTasks = new AsyncTasks();
        var states = new List<bool>();

        states.Add(asyncTasks.IsRunning); // Initial state

        var tcs = new TaskCompletionSource<bool>();
        asyncTasks.AddTask(tcs.Task);
        states.Add(asyncTasks.IsRunning); // After adding task

        tcs.SetResult(true);
        await asyncTasks.AllDone;
        states.Add(asyncTasks.IsRunning); // After completion

        // Assert
        Assert.IsFalse(states[0], "Initial state should be false");
        Assert.IsTrue(states[1], "State after adding task should be true");
        Assert.IsFalse(states[2], "State after completion should be false");
    }

    [TestMethod]
    public async Task IsRunning_AddingCompletedTask_DoesNotChangeState()
    {
        // Arrange
        var asyncTasks = new AsyncTasks();

        // Act
        asyncTasks.AddTask(Task.CompletedTask);

        // Assert
        Assert.IsFalse(asyncTasks.IsRunning, "Adding completed task should not change IsRunning");
        await asyncTasks.AllDone;
    }

    #endregion

    #region Sequence Restart Tests

    [TestMethod]
    public async Task AddTask_AfterSequenceCompletes_StartsNewSequenceSuccessfully()
    {
        // Arrange
        var asyncTasks = new AsyncTasks();

        // First sequence
        var tcs1 = new TaskCompletionSource<bool>();
        asyncTasks.AddTask(tcs1.Task);
        tcs1.SetResult(true);
        await asyncTasks.AllDone;
        Assert.IsFalse(asyncTasks.IsRunning);

        // Act - Start new sequence
        var tcs2 = new TaskCompletionSource<bool>();
        asyncTasks.AddTask(tcs2.Task);

        // Assert
        Assert.IsTrue(asyncTasks.IsRunning, "New sequence should start after previous completes");

        // Cleanup
        tcs2.SetResult(true);
        await asyncTasks.AllDone;
    }

    [TestMethod]
    public async Task OnFullSequenceComplete_MultipleSequences_InvokedForEach()
    {
        // Arrange
        var asyncTasks = new AsyncTasks();
        var callbackCount = 0;
        asyncTasks.OnFullSequenceComplete = () =>
        {
            callbackCount++;
            return Task.CompletedTask;
        };

        // First sequence
        var tcs1 = new TaskCompletionSource<bool>();
        asyncTasks.AddTask(tcs1.Task);
        tcs1.SetResult(true);
        await asyncTasks.AllDone;

        // Second sequence
        var tcs2 = new TaskCompletionSource<bool>();
        asyncTasks.AddTask(tcs2.Task);
        tcs2.SetResult(true);
        await asyncTasks.AllDone;

        // Assert
        Assert.AreEqual(2, callbackCount, "Callback should be invoked for each sequence");
    }

    #endregion

    #region Thread Safety Tests

    [TestMethod]
    public async Task AddTask_ConcurrentAddAndComplete_NoDeadlock()
    {
        // Arrange
        var asyncTasks = new AsyncTasks();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var taskCompletionSources = new List<TaskCompletionSource<bool>>();
        const int iterationCount = 100;

        // Act
        var addTask = Task.Run(async () =>
        {
            for (int i = 0; i < iterationCount && !cts.Token.IsCancellationRequested; i++)
            {
                var tcs = new TaskCompletionSource<bool>();
                lock (taskCompletionSources)
                {
                    taskCompletionSources.Add(tcs);
                }
                asyncTasks.AddTask(tcs.Task);
                await Task.Yield();
            }
        }, cts.Token);

        var completeTask = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                TaskCompletionSource<bool>? tcs = null;
                lock (taskCompletionSources)
                {
                    if (taskCompletionSources.Count > 0)
                    {
                        tcs = taskCompletionSources[0];
                        taskCompletionSources.RemoveAt(0);
                    }
                }
                if (tcs != null)
                {
                    tcs.SetResult(true);
                }
                await Task.Yield();
            }
        }, cts.Token);

        // Assert - Should complete without deadlock
        await addTask;

        // Complete remaining tasks
        lock (taskCompletionSources)
        {
            foreach (var tcs in taskCompletionSources)
            {
                tcs.SetResult(true);
            }
            taskCompletionSources.Clear();
        }

        await cts.CancelAsync();
        await asyncTasks.AllDone;
        Assert.IsFalse(asyncTasks.IsRunning);
    }

    [TestMethod]
    public async Task AllDone_ConcurrentAccess_ReturnsConsistentTask()
    {
        // Arrange
        var asyncTasks = new AsyncTasks();
        var tcs = new TaskCompletionSource<bool>();
        asyncTasks.AddTask(tcs.Task);

        // Act - Access AllDone from multiple threads
        var results = new Task[10];
        var completionEvents = new TaskCompletionSource<bool>[10];
        for (int i = 0; i < 10; i++)
        {
            completionEvents[i] = new TaskCompletionSource<bool>();
            var index = i;
            _ = Task.Run(() =>
            {
                results[index] = asyncTasks.AllDone;
                completionEvents[index].SetResult(true);
            });
        }

        // Wait for all threads to access AllDone
        await Task.WhenAll(completionEvents.Select(ce => ce.Task));

        // Assert - All should return the same task reference
        var firstTask = results[0];
        Assert.IsTrue(results.All(t => ReferenceEquals(t, firstTask)),
            "Concurrent access should return the same task reference");

        // Cleanup
        tcs.SetResult(true);
        await asyncTasks.AllDone;
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public async Task AddTask_VeryShortTask_HandledCorrectly()
    {
        // Arrange
        var asyncTasks = new AsyncTasks();
        var shortTask = Task.Run(() => { }); // Very short task

        // Act
        asyncTasks.AddTask(shortTask);
        await asyncTasks.AllDone;

        // Assert
        Assert.IsFalse(asyncTasks.IsRunning);
    }

    [TestMethod]
    public async Task AddTask_TaskWithCancellation_CompletesWithoutExceptionInAllDone()
    {
        // Arrange
        var asyncTasks = new AsyncTasks();
        var tcs = new TaskCompletionSource<bool>();
        asyncTasks.AddTask(tcs.Task);

        // Act
        tcs.SetCanceled();

        // Assert - Cancelled tasks don't have an Exception property set,
        // so they are handled as successful completions (no exception aggregated).
        // The task.Exception is null for cancelled tasks.
        await asyncTasks.AllDone;
        Assert.IsFalse(asyncTasks.IsRunning);
    }

    [TestMethod]
    public async Task AddTask_TaskWithCancellationException_HandledAsException()
    {
        // Arrange
        var asyncTasks = new AsyncTasks();
        var tcs = new TaskCompletionSource<bool>();
        asyncTasks.AddTask(tcs.Task);

        // Act - Set exception with TaskCanceledException explicitly
        tcs.SetException(new TaskCanceledException("Cancelled"));

        // Assert
        var allDone = asyncTasks.AllDone;
        var aggregateException = await Assert.ThrowsExceptionAsync<AggregateException>(async () => await allDone);
        Assert.IsTrue(aggregateException.InnerExceptions.Any(e => e is TaskCanceledException));
    }

    [TestMethod]
    public async Task AddTask_LongRunningTask_TrackedCorrectly()
    {
        // Arrange
        var asyncTasks = new AsyncTasks();
        var longTask = Task.Delay(100);

        // Act
        asyncTasks.AddTask(longTask);

        // Assert
        Assert.IsTrue(asyncTasks.IsRunning);
        await asyncTasks.AllDone;
        Assert.IsFalse(asyncTasks.IsRunning);
    }

    [TestMethod]
    public async Task AddTask_TaskFromResult_HandledAsCompleted()
    {
        // Arrange
        var asyncTasks = new AsyncTasks();
        var taskFromResult = Task.FromResult("result");

        // Act
        var result = asyncTasks.AddTask(taskFromResult);

        // Assert
        Assert.IsTrue(result.IsCompleted);
        Assert.IsFalse(asyncTasks.IsRunning);
        await asyncTasks.AllDone;
    }

    [TestMethod]
    public async Task AddTask_MultipleSequentialSequences_EachSequenceIndependent()
    {
        // Arrange
        var asyncTasks = new AsyncTasks();
        var sequenceCount = 0;
        asyncTasks.OnFullSequenceComplete = () =>
        {
            sequenceCount++;
            return Task.CompletedTask;
        };

        // Act - Run three independent sequences
        for (int i = 0; i < 3; i++)
        {
            var tcs = new TaskCompletionSource<bool>();
            asyncTasks.AddTask(tcs.Task);
            Assert.IsTrue(asyncTasks.IsRunning);
            tcs.SetResult(true);
            await asyncTasks.AllDone;
            Assert.IsFalse(asyncTasks.IsRunning);
        }

        // Assert
        Assert.AreEqual(3, sequenceCount);
    }

    #endregion

    #region WaitForCompletion with CancellationToken Tests

    [TestMethod]
    public async Task WaitForCompletion_NullToken_BehavesLikeAllDone()
    {
        // Arrange
        var asyncTasks = new AsyncTasks();
        var tcs = new TaskCompletionSource<bool>();
        asyncTasks.AddTask(tcs.Task);

        // Act
        var waitTask = asyncTasks.WaitForCompletion(null);

        // Complete the pending task
        tcs.SetResult(true);
        await waitTask;

        // Assert
        Assert.IsFalse(asyncTasks.IsRunning);
    }

    [TestMethod]
    public async Task WaitForCompletion_NonCancelableToken_BehavesLikeAllDone()
    {
        // Arrange
        var asyncTasks = new AsyncTasks();
        var tcs = new TaskCompletionSource<bool>();
        asyncTasks.AddTask(tcs.Task);

        // Act - CancellationToken.None is not cancelable
        var waitTask = asyncTasks.WaitForCompletion(CancellationToken.None);

        // Complete the pending task
        tcs.SetResult(true);
        await waitTask;

        // Assert
        Assert.IsFalse(asyncTasks.IsRunning);
    }

    [TestMethod]
    public async Task WaitForCompletion_AlreadyCancelledToken_ThrowsImmediately()
    {
        // Arrange
        var asyncTasks = new AsyncTasks();
        var tcs = new TaskCompletionSource<bool>();
        asyncTasks.AddTask(tcs.Task);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () =>
            await asyncTasks.WaitForCompletion(cts.Token));

        // Task should still be running
        Assert.IsTrue(asyncTasks.IsRunning);
    }

    [TestMethod]
    public async Task WaitForCompletion_TokenCancelledWhileWaiting_ThrowsOperationCanceledException()
    {
        // Arrange
        var asyncTasks = new AsyncTasks();
        var tcs = new TaskCompletionSource<bool>();
        asyncTasks.AddTask(tcs.Task);

        using var cts = new CancellationTokenSource();

        // Start waiting
        var waitTask = asyncTasks.WaitForCompletion(cts.Token);

        // Cancel while waiting
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () =>
            await waitTask);

        // Task should still be running (only the wait was cancelled)
        Assert.IsTrue(asyncTasks.IsRunning);
    }

    [TestMethod]
    public async Task WaitForCompletion_TaskCompletesBeforeCancel_CompletesNormally()
    {
        // Arrange
        var asyncTasks = new AsyncTasks();
        var tcs = new TaskCompletionSource<bool>();
        asyncTasks.AddTask(tcs.Task);

        using var cts = new CancellationTokenSource();

        // Complete the task immediately
        tcs.SetResult(true);

        // Act - Wait should complete successfully
        await asyncTasks.WaitForCompletion(cts.Token);

        // Assert
        Assert.IsFalse(asyncTasks.IsRunning);
    }

    [TestMethod]
    public async Task WaitForCompletion_MultipleTasks_WaitsForAll()
    {
        // Arrange
        var asyncTasks = new AsyncTasks();
        var tcs1 = new TaskCompletionSource<bool>();
        var tcs2 = new TaskCompletionSource<bool>();
        asyncTasks.AddTask(tcs1.Task);
        asyncTasks.AddTask(tcs2.Task);

        using var cts = new CancellationTokenSource();

        // Complete first task
        tcs1.SetResult(true);

        // Still running because tcs2 is not complete
        Assert.IsTrue(asyncTasks.IsRunning);

        // Complete second task
        tcs2.SetResult(true);

        // Act - Should complete successfully
        await asyncTasks.WaitForCompletion(cts.Token);

        // Assert
        Assert.IsFalse(asyncTasks.IsRunning);
    }

    [TestMethod]
    public async Task WaitForCompletion_NoTasksRunning_CompletesImmediately()
    {
        // Arrange
        var asyncTasks = new AsyncTasks();

        using var cts = new CancellationTokenSource();

        // Act - Should complete immediately
        await asyncTasks.WaitForCompletion(cts.Token);

        // Assert
        Assert.IsFalse(asyncTasks.IsRunning);
    }

    [TestMethod]
    public async Task WaitForCompletion_TaskThrowsException_PropagatesException()
    {
        // Arrange
        var asyncTasks = new AsyncTasks();
        var tcs = new TaskCompletionSource<bool>();
        asyncTasks.AddTask(tcs.Task);

        using var cts = new CancellationTokenSource();

        // Set an exception on the task
        tcs.SetException(new InvalidOperationException("Test error"));

        // Act & Assert - The AggregateException from AllDone should propagate
        var ex = await Assert.ThrowsExceptionAsync<AggregateException>(async () =>
            await asyncTasks.WaitForCompletion(cts.Token));

        Assert.IsTrue(ex.InnerExceptions.Any(e => e is InvalidOperationException));
    }

    #endregion
}
