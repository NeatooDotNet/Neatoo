namespace Neatoo;

/// <summary>
/// Tracks async work that was started and not awaited, and offers one task that completes when
/// all of it has finished.
/// </summary>
/// <remarks>
/// <para>
/// A property setter cannot be awaited, so the work it starts is handed to a tracker and the
/// caller meets it again at <see cref="AllDone"/>. Tasks added while others are running join the
/// same sequence; the sequence completes when the last one does.
/// </para>
/// <para>
/// A sequence in which a task faulted reports its exceptions once, to the waits that held
/// <see cref="AllDone"/> while it ran, or else to the first wait after it. The next task added
/// starts a clean sequence.
/// </para>
/// <para>
/// The framework's implementation is registered in DI through <see cref="CreateAsyncTasks"/>.
/// Register your own to replace it.
/// </para>
/// </remarks>
public interface IAsyncTasks
{
    /// <summary>
    /// Called when a sequence has no tasks left, before <see cref="AllDone"/> completes. Work it
    /// adds keeps the sequence open.
    /// </summary>
    Func<Task> OnFullSequenceComplete { get; set; }

    /// <summary>
    /// Adds a task to the running sequence, starting one if none is running.
    /// </summary>
    /// <param name="task">The task to track. A completed task is not tracked.</param>
    /// <returns>A task that completes when <paramref name="task"/> has been accounted for.</returns>
    /// <exception cref="AggregateException">Thrown when <paramref name="task"/> has already faulted.</exception>
    Task AddTask(Task task);

    /// <summary>
    /// Gets a task that completes when every task in the running sequence is done, or a completed
    /// task when nothing is running.
    /// </summary>
    Task AllDone { get; }

    /// <summary>
    /// Gets a value indicating whether a sequence is running.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Waits for <see cref="AllDone"/>, with cancellation of the wait only.
    /// </summary>
    /// <param name="token">Cancels the wait. The tracked tasks run to completion.</param>
    Task WaitForCompletion(CancellationToken? token = null);
}

/// <summary>
/// Creates a task tracker for one entity or one property.
/// </summary>
public delegate IAsyncTasks CreateAsyncTasks();
