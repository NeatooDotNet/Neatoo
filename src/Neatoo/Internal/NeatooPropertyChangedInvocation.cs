namespace Neatoo.Internal;

internal static class NeatooPropertyChangedInvocation
{
    /// <summary>
    /// Raises the event and returns one task that completes when every subscriber's task has.
    /// </summary>
    /// <remarks>
    /// DESIGN DECISION: every subscriber is called, in the order it subscribed, and every task
    /// they return is awaited. A change is not over while any handler of it is still running.
    /// DID NOT DO THIS: invoke the event as a delegate. A delegate with several subscribers
    /// returns only the last one's task, so the others' work was awaited by nobody: a rule
    /// cascade could be dropped because someone else had also subscribed.
    /// A subscriber that throws before it returns a task stops the ones after it, as it does
    /// for any event.
    /// </remarks>
    public static Task InvokeAll(this NeatooPropertyChanged? subscribers, NeatooPropertyChangedEventArgs eventArgs)
    {
        if (subscribers == null)
        {
            return Task.CompletedTask;
        }

        if (subscribers.HasSingleTarget)
        {
            return subscribers(eventArgs);
        }

        var tasks = new List<Task>();

        foreach (var subscriber in Delegate.EnumerateInvocationList(subscribers))
        {
            tasks.Add(subscriber(eventArgs));
        }

        return Task.WhenAll(tasks);
    }
}
