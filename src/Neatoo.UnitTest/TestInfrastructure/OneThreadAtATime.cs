namespace Neatoo.UnitTest.TestInfrastructure;

/// <summary>
/// Runs a test body the way a UI application runs Neatoo: one thing at a time.
/// </summary>
/// <remarks>
/// A Neatoo object graph is single-threaded (see "Single-Threaded Object Model" in
/// <c>src/Design/CLAUDE-DESIGN.md</c>). In a UI application every continuation comes back to the
/// one thread that owns the graph. A test host has no such thread: two async rules that finish at
/// the same moment resume on two pool threads and run over the same object at once, which the
/// framework does not support. Inside <see cref="Run"/> every continuation of the body, of the
/// rules it starts and of the framework's own task tracking is queued to one exclusive scheduler,
/// so nothing overlaps.
/// </remarks>
public static class OneThreadAtATime
{
    public static Task Run(Func<Task> body)
    {
        var scheduler = new ConcurrentExclusiveSchedulerPair().ExclusiveScheduler;

        return Task.Factory.StartNew(body, CancellationToken.None, TaskCreationOptions.None, scheduler).Unwrap();
    }
}
