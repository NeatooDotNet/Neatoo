namespace Neatoo.Internal;

/// <summary>
/// When all tasks have completed set a TaskCompletionsSource
/// For Async Forks in Property Setters where the task is not awaited
/// </summary>
public sealed class AsyncTasks
{
    // TODO: Add cancellation token

    private readonly object _lockObject = new object();
    private TaskCompletionSource<bool>? _allDoneCompletionSource;
    private Dictionary<Guid, Task> _tasks = new Dictionary<Guid, Task>();
    private List<Exception> _exceptions = new List<Exception>();

    /// <summary>
    /// Function to be called when the full sequence is complete.
    /// </summary>
    public Func<Task> OnFullSequenceComplete { get; set; } = () => Task.CompletedTask;
    /// <summary>
    /// Adds a new task to the sequence.
    /// </summary>
    /// <param name="task">The task to be added. Needs to be a Func because it may not actually be time to execute</param>
    /// <param name="runOnException">Indicates whether the task should run on exception.</param>
    /// <returns>A task representing the added task.</returns>
    public Task AddTask(Task task, bool runOnException = false)
    {
        lock (this._lockObject)
        {
            // If the sequencer is faulted, return the faulted task.
            if (this._allDoneCompletionSource != null && this._allDoneCompletionSource.Task.IsFaulted)
            {
                return this._allDoneCompletionSource.Task;
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
            if (this._allDoneCompletionSource == null || this._allDoneCompletionSource.Task.IsCompleted)
            {
                this._allDoneCompletionSource = new TaskCompletionSource<bool>();
                this.IsRunning = true;
            }

            var id = Guid.NewGuid();

            this._tasks.Add(id, task);

            return task.ContinueWith((completedTask) =>
            {
                return this.SequenceCompleted(id, completedTask);
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
            lock (this._lockObject)
            {
                return this._allDoneCompletionSource?.Task ?? Task.CompletedTask;
            }
        }
    }

    public bool IsRunning { get; protected set; }


    private async Task SequenceCompleted(Guid id, Task task)
    {
        TaskCompletionSource<bool> completionSource;
        List<Exception> exceptionsToReport;

        lock (this._lockObject)
        {
            completionSource = this._allDoneCompletionSource ?? throw new ArgumentNullException($"{nameof(this._allDoneCompletionSource)} should not be null");

            if (task.Exception != null)
            {
                this._exceptions.AddRange(task.Exception.InnerExceptions);
            }

            if (!this._tasks.Remove(id))
            {
                throw new InvalidOperationException("Task was not found in the task collection. This indicates an internal state inconsistency.");
            }

            if (this._tasks.Count > 0)
            {
                return;
            }

            // Capture exceptions before exiting lock, as a new sequence could start
            exceptionsToReport = new List<Exception>(this._exceptions);
            this._exceptions.Clear();

            // Note: We intentionally do NOT set _allDoneCompletionSource to null here.
            // It will be set to null when the next sequence starts (line 47-50).
            // This ensures AllDone returns the correct task until result/exception is set.
            this.IsRunning = false;
        }

        try
        {
            await this.OnFullSequenceComplete();
        }
        catch (AggregateException ex)
        {
            exceptionsToReport.AddRange(ex.InnerExceptions);
        }

        if (exceptionsToReport.Count > 0)
        {
            completionSource.SetException(new AggregateException(exceptionsToReport));
        }
        else
        {
            completionSource.SetResult(true);
        }
    }

}