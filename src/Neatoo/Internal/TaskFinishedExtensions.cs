namespace Neatoo.Internal;

internal static class TaskFinishedExtensions
{
    /// <summary>
    /// A task that completes when <paramref name="task"/> finishes, however it finishes. It never
    /// faults and never cancels: it is for waiting on work whose outcome is reported elsewhere.
    /// </summary>
    public static Task WhenFinished(this Task task)
    {
        // The exception is read, so the faulted task is not reported as unobserved.
        return task.ContinueWith(
            static finished => { _ = finished.Exception; },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }
}
