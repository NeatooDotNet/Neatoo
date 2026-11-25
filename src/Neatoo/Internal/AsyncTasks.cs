namespace Neatoo.Internal;

/// <summary>
/// The framework's task tracker: when every task of a sequence has completed, it completes one
/// task that callers can await. For the async forks in property setters, where the task is not
/// awaited.
/// </summary>
/// <remarks>
/// DESIGN DECISION: a faulted sequence is reported once and the tracker recovers. The waits that
/// held <see cref="AllDone"/> while the sequence ran see the fault; if nobody was waiting, the
/// first wait afterwards does, or the next sequence carries it. The next task added starts a
/// clean sequence, so the object keeps tracking and waiting after a rule has thrown.
/// DID NOT DO THIS: stay faulted. Every later wait threw the old exception, and every later task
/// went untracked, so one thrown rule left the object unusable for waiting.
/// </remarks>
public sealed class AsyncTasks : IAsyncTasks
{
    private readonly object _lockObject = new object();
    private TaskCompletionSource<bool>? _allDoneCompletionSource;

    // How many callers were handed the current sequence's task while it ran and still wait on it.
    // Above zero, its outcome has an observer. A cancelled wait gives its place back.
    private int _allDoneObservers;

    // True once the current sequence has decided its outcome. Its task completes right after,
    // outside the lock, so a task added from here on starts a new sequence.
    private bool _allDoneClosed;

    // The exceptions of a closed sequence that no wait was holding. The next wait is handed
    // them, or the next sequence reports them.
    private List<Exception>? _unobservedFault;

    private Dictionary<Guid, Task> _tasks = new Dictionary<Guid, Task>();
    private List<Exception> _exceptions = new List<Exception>();

    /// <summary>
    /// Function to be called when the full sequence is complete.
    /// </summary>
    public Func<Task> OnFullSequenceComplete { get; set; } = () => Task.CompletedTask;

    /// <summary>
    /// Adds a new task to the sequence.
    /// </summary>
    /// <param name="task">The task to be added.</param>
    /// <returns>A task representing the added task.</returns>
    public Task AddTask(Task task)
    {
        lock (this._lockObject)
        {
            if (task.Exception != null)
            {
                throw task.Exception;
            }

            if (task.IsCompleted)
            {
                return task;
            }

            // If the sequencer is not running or has closed, start a new sequence.
            if (this._allDoneCompletionSource == null || this._allDoneClosed)
            {
                // A fault no wait was handed is not lost: the new sequence reports it.
                if (this._unobservedFault != null)
                {
                    this._exceptions.AddRange(this._unobservedFault);
                    this._unobservedFault = null;
                }

                this._allDoneCompletionSource = new TaskCompletionSource<bool>();
                this._allDoneObservers = 0;
                this._allDoneClosed = false;
            }

            // Also true for a task added while OnFullSequenceComplete runs (a rule reacting to the
            // object going idle): the finishing sequence stays open for it, see SequenceCompleted.
            this.IsRunning = true;

            var id = Guid.NewGuid();

            this._tasks.Add(id, task);

            // Unwrapped, so that the returned task covers the whole of the accounting: when this was
            // the last task, OnFullSequenceComplete has run and AllDone has completed.
            return task.ContinueWith((completedTask) =>
            {
                return this.SequenceCompleted(id, completedTask);
            }).Unwrap();
        }
    }

    /// <summary>
    /// Gets a task that completes when all tasks in the sequence are done.
    /// </summary>
    /// <remarks>
    /// A sequence that faulted with nobody waiting hands its fault to the first caller after it,
    /// once. Later callers get a completed task.
    /// </remarks>
    public Task AllDone
    {
        get
        {
            lock (this._lockObject)
            {
                var completionSource = this._allDoneCompletionSource;

                if (completionSource == null)
                {
                    return Task.CompletedTask;
                }

                if (!this._allDoneClosed)
                {
                    this._allDoneObservers++;
                }
                else if (this._unobservedFault != null)
                {
                    // This caller is the one wait that is handed the fault.
                    this._unobservedFault = null;
                    this._allDoneCompletionSource = null;
                }

                return completionSource.Task;
            }
        }
    }

    public bool IsRunning { get; private set; }

    /// <summary>
    /// Waits for all tasks to complete, with optional cancellation support.
    /// </summary>
    /// <param name="token">Optional cancellation token. If cancelled, throws <see cref="OperationCanceledException"/>.</param>
    /// <returns>A task that completes when all tasks are done or cancellation is requested.</returns>
    /// <remarks>
    /// Cancellation only affects waiting - running tasks will complete to avoid inconsistent state.
    /// </remarks>
    public async Task WaitForCompletion(CancellationToken? token = null)
    {
        var allDone = this.AllDone;

        if (token == null || !token.Value.CanBeCanceled)
        {
            await allDone;
            return;
        }

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = token.Value.Register(() => tcs.TrySetCanceled(token.Value));

        var completedTask = await Task.WhenAny(allDone, tcs.Task);

        if (completedTask == tcs.Task && this.TryLeaveWait(allDone))
        {
            token.Value.ThrowIfCancellationRequested();
        }

        // Propagate any exceptions from AllDone
        await allDone;
    }

    /// <summary>
    /// A cancelled wait stops observing the sequence, so that a fault it would have seen is kept
    /// for the next wait. Returns false when the sequence's outcome is already decided: the
    /// caller is then the wait that reports it, and must not leave.
    /// </summary>
    private bool TryLeaveWait(Task allDone)
    {
        lock (this._lockObject)
        {
            if (this._allDoneClosed || !ReferenceEquals(this._allDoneCompletionSource?.Task, allDone))
            {
                return false;
            }

            this._allDoneObservers--;
            return true;
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Whatever the completion callback throws is reported through AllDone. An exception that escaped here would leave AllDone incomplete for good.")]
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
        catch (Exception ex)
        {
            // Whatever the callback threw, the waits on this sequence must still complete.
            exceptionsToReport.Add(ex);
        }

        lock (this._lockObject)
        {
            // The completion callback started more work (a meta-state rule triggered by the object
            // going idle). The sequence is not done: it completes when that work does, carrying the
            // exceptions gathered so far.
            if (this._tasks.Count > 0)
            {
                this._exceptions.AddRange(exceptionsToReport);
                return;
            }

            this._allDoneClosed = true;

            if (exceptionsToReport.Count > 0)
            {
                if (this._allDoneObservers > 0)
                {
                    // A wait holds this sequence's task and will see the fault. Let go of it, so
                    // that no later wait is handed the same fault.
                    this._allDoneCompletionSource = null;
                }
                else
                {
                    this._unobservedFault = exceptionsToReport;
                }
            }
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
