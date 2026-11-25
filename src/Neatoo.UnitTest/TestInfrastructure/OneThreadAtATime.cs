using System.Collections.Concurrent;

namespace Neatoo.UnitTest.TestInfrastructure;

/// <summary>
/// Runs a test body the way a UI application runs Neatoo: on one thread that owns the object graph.
/// </summary>
/// <remarks>
/// A Neatoo object graph is single-threaded (see "Single-Threaded Object Model" in
/// <c>src/Design/CLAUDE-DESIGN.md</c>). In a UI application every continuation comes back to the
/// one thread that owns the graph, through its synchronization context. A test host has no such
/// thread: two async rules that finish at the same moment resume on two pool threads and run over
/// the same object at once, which the framework does not support. <see cref="Run(Func{Task})"/>
/// gives the body a thread of its own with a synchronization context, so the body, the rules it
/// starts and the framework's task tracking all continue on that thread, one at a time.
/// </remarks>
public static class OneThreadAtATime
{
    public static Task Run(Func<Task> body)
    {
        return Run(_ => body());
    }

    /// <summary>
    /// Runs the body on the owning thread and hands it that thread's id, for a test that asserts
    /// where a notification was raised.
    /// </summary>
    public static async Task Run(Func<int, Task> body)
    {
        using var owner = new OwningThread();
        await owner.Run(body);
    }

    private sealed class OwningThread : SynchronizationContext, IDisposable
    {
        private readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> queue = new();
        private readonly Thread thread;

        public OwningThread()
        {
            this.thread = new Thread(this.Pump) { IsBackground = true, Name = nameof(OneThreadAtATime) };
            this.thread.Start();
        }

        public Task Run(Func<int, Task> body)
        {
            var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            this.Post(async _ =>
            {
                try
                {
                    await body(this.thread.ManagedThreadId);
                    finished.SetResult();
                }
                catch (Exception ex)
                {
                    finished.SetException(ex);
                }
            }, null);

            return finished.Task;
        }

        public override void Post(SendOrPostCallback d, object? state)
        {
            try
            {
                this.queue.Add((d, state));
            }
            catch (InvalidOperationException)
            {
                // The body has finished and the thread is gone. Work that outlived the body
                // (a test's cleanup waits for it) continues on the pool, as it would have
                // without this helper.
                ThreadPool.QueueUserWorkItem(_ => d(state));
            }
        }

        public override void Send(SendOrPostCallback d, object? state)
        {
            if (Thread.CurrentThread == this.thread)
            {
                d(state);
                return;
            }

            using var done = new ManualResetEventSlim();
            this.Post(_ =>
            {
                try
                {
                    d(state);
                }
                finally
                {
                    done.Set();
                }
            }, null);
            done.Wait();
        }

        public override SynchronizationContext CreateCopy()
        {
            return this;
        }

        public void Dispose()
        {
            this.queue.CompleteAdding();
        }

        private void Pump()
        {
            SetSynchronizationContext(this);

            foreach (var (callback, state) in this.queue.GetConsumingEnumerable())
            {
                callback(state);
            }
        }
    }
}
