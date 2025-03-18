namespace Neatoo.Core;

/// <summary>
/// When all tasks have completed set a TaskCompletionsSource
/// For Async Forks in Property Setters where the task is not awaited
/// </summary>
public sealed class AsyncTasks
{
    // TODO: Add cancellation token

    private readonly object lockObject = new object();
    //private SequencedAsyncFunction? lastTask;
    private TaskCompletionSource<bool>? allDoneCompletionSource;

    /// <summary>
    /// Function to be called when the full sequence is complete.
    /// </summary>
    public Func<Task> OnFullSequenceComplete { get; set; } = () => Task.CompletedTask;

    private Dictionary<Guid, Task> tasks = new Dictionary<Guid, Task>();
    private List<Exception> Exceptions = new List<Exception>();
    /// <summary>
    /// Adds a new task to the sequence.
    /// </summary>
    /// <param name="task">The task to be added. Needs to be a Func because it may not actually be time to execute</param>
    /// <param name="runOnException">Indicates whether the task should run on exception.</param>
    /// <returns>A task representing the added task.</returns>
    public Task AddTask(Task task, bool runOnException = false)
    {
        lock (lockObject)
        {
            // If the sequencer is faulted, return the faulted task.
            if (allDoneCompletionSource != null && allDoneCompletionSource.Task.IsFaulted)
            {
                return allDoneCompletionSource.Task;
            }

            if (task.Exception != null)
            {
                throw task.Exception;
            }

            if (task.IsCompleted)
            {
                return task;
            }

            // If the sequencer is not running or has completed, start a new sequence.
            if (allDoneCompletionSource == null || allDoneCompletionSource.Task.IsCompleted)
            {
                allDoneCompletionSource = new TaskCompletionSource<bool>();
            }

            var id = Guid.NewGuid();

            tasks.Add(id, task);

            return task.ContinueWith((completedTask) =>
            {
                return SequenceCompleted(id, completedTask);
            });
        }
    }

    /// <summary>
    /// Gets a task that completes when all tasks in the sequence are done.
    /// </summary>
    public Task AllDone
    {
        get
        {
            lock (lockObject)
            {
                return allDoneCompletionSource?.Task ?? Task.CompletedTask;
            }
        }
    }


    private async Task SequenceCompleted(Guid id, Task task)
    {
        var completionSource = this.allDoneCompletionSource ?? throw new ArgumentNullException($"{nameof(allDoneCompletionSource)} should not be null");

        lock (lockObject)
        {
            if (task.Exception != null)
            {
                Exceptions.AddRange(task.Exception.InnerExceptions);
            }

            if (!tasks.Remove(id))
            {
                throw new Exception("Very unexpected");
            }

            if (tasks.Count > 0)
            {
                return;
            }

            this.allDoneCompletionSource = null; // What if another starts while we are finishing here? 

        }


        try
        {
            await OnFullSequenceComplete();
        }
        catch (AggregateException ex)
        {
            Exceptions.AddRange(ex.InnerExceptions);
        }

        if (Exceptions.Count > 0)
        {
            completionSource.SetException(new AggregateException(Exceptions));
        }
        else
        {
            completionSource.SetResult(true);
        }

        await Task.CompletedTask;
    }

}