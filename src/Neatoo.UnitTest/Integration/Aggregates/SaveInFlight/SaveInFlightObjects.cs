using Neatoo.RemoteFactory;

namespace Neatoo.UnitTest.Integration.Aggregates.SaveInFlight;

/// <summary>
/// What the save operations of both fixtures write to and wait on.
/// </summary>
/// <remarks>
/// Static because the client and server test containers are separate providers that share the
/// process. Tests call <see cref="Reset"/> in TestInitialize.
/// </remarks>
public static class SaveInFlightStore
{
    /// <summary>Insert operations that have started, finished or not.</summary>
    public static int InsertsStarted { get; private set; }

    /// <summary>An insert waits on this before it finishes. A test holds a save open with it.</summary>
    public static Func<Task> Gate { get; set; } = static () => Task.CompletedTask;

    /// <summary>When set, an insert throws this after the gate opens.</summary>
    public static Exception? FailWith { get; set; }

    public static void Reset()
    {
        InsertsStarted = 0;
        Gate = static () => Task.CompletedTask;
        FailWith = null;
    }

    internal static async Task Insert()
    {
        InsertsStarted++;

        await Gate();

        if (FailWith is { } failure)
        {
            throw failure;
        }
    }
}

/// <summary>
/// Aggregate root whose save crosses to the server: the caller gets a different instance back.
/// </summary>
public interface IRemoteDraft : IEntityRoot
{
    string? Name { get; set; }
}

[Factory]
internal partial class RemoteDraft : EntityBase<RemoteDraft>, IRemoteDraft
{
    public RemoteDraft(IEntityBaseServices<RemoteDraft> services) : base(services) { }

    public partial string? Name { get; set; }

    [Create]
    public void Create()
    {
    }

    [Remote]
    [Insert]
    internal Task Insert()
    {
        return SaveInFlightStore.Insert();
    }

    [Remote]
    [Update]
    internal Task Update()
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// Aggregate root whose save runs in-process: the operation runs on the instance that was saved,
/// and the factory completes it on that instance.
/// </summary>
public interface ILocalDraft : IEntityRoot
{
    string? Name { get; set; }
}

[Factory]
internal partial class LocalDraft : EntityBase<LocalDraft>, ILocalDraft
{
    public LocalDraft(IEntityBaseServices<LocalDraft> services) : base(services) { }

    public partial string? Name { get; set; }

    [Create]
    public void Create()
    {
    }

    [Insert]
    internal Task Insert()
    {
        return SaveInFlightStore.Insert();
    }

    [Update]
    internal Task Update()
    {
        return Task.CompletedTask;
    }
}
