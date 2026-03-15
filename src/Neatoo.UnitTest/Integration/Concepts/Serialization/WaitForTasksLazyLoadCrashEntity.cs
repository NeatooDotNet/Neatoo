using Neatoo.RemoteFactory;

namespace Neatoo.UnitTest.Integration.Concepts.Serialization.LazyLoadCrash;

/// <summary>
/// Child entity fetched via a [Remote] factory method.
/// Loaded by the LazyLoad deferred loader in the parent.
/// </summary>
public interface ICrashChild : IEntityBase
{
    Guid Id { get; }
    string Data { get; set; }
}

[Factory]
internal partial class CrashChild : EntityBase<CrashChild>, ICrashChild
{
    public CrashChild(IEntityBaseServices<CrashChild> services) : base(services)
    {
    }

    public partial Guid Id { get; set; }
    public partial string Data { get; set; }

    [Remote]
    [Fetch]
    internal Task Fetch(Guid parentId)
    {
        using (PauseAllActions())
        {
            this["Id"].LoadValue(parentId);
            this["Data"].LoadValue($"Child data for {parentId}");
        }
        return Task.CompletedTask;
    }
}

/// <summary>
/// Parent entity fetched via [Remote] [Fetch] -- matching the zTreatment pattern.
///
/// The critical flow:
/// 1. Client calls factory.Fetch(id) -- [Remote] means it executes on the server
/// 2. Server: constructor runs (creates LazyChild with loader using this.Id), sets up AddActionAsync
/// 3. Server: [Fetch] runs, sets Id
/// 4. Serialization: LazyChild is serialized (Value=null, IsLoaded=false), but loader delegate is lost
/// 5. Client deserialization: constructor runs AGAIN (from DI), creates NEW LazyChild with loader
/// 6. BUG: Neatoo converter overwrites LazyChild with deserialized instance (NO loader)
/// 7. Client: Trigger is set -> AddActionAsync fires -> await LazyChild -> LoadAsync() -> crash
///
/// The fix: NeatooBaseJsonTypeConverter merges deserialized state into the existing
/// constructor-created LazyLoad instance instead of replacing it, preserving the loader.
///
/// See: docs/todos/waitfortasks-crash-lazyload-remote.md
/// </summary>
public interface ICrashParent : IEntityRoot
{
    string Trigger { get; set; }
    string LoadedData { get; }
    LazyLoad<ICrashChild> LazyChild { get; }
    Guid Id { get; set; }
}

[Factory]
internal partial class CrashParent : EntityBase<CrashParent>, ICrashParent
{
    public CrashParent(
        IEntityBaseServices<CrashParent> services,
        ICrashChildFactory childFactory,
        ILazyLoadFactory lazyLoadFactory) : base(services)
    {
        // Create LazyLoad in the constructor so it survives deserialization.
        // The loader lambda captures the factory from DI and uses this.Id,
        // which will be populated by the time the loader actually executes.
        LazyChild = lazyLoadFactory.Create<ICrashChild>(async () =>
        {
            return await childFactory.Fetch(this.Id);
        });

        // AddActionAsync: when Trigger changes, await the LazyLoad child
        // and store the loaded data. Mimics zTreatment VisitHub pattern.
        RuleManager.AddActionAsync(async parent =>
        {
            var child = await parent.LazyChild;
            if (child != null)
            {
                parent.LoadedData = child.Data;
            }
        }, p => p.Trigger);
    }

    public partial string Trigger { get; set; }
    public partial string LoadedData { get; set; }
    public partial Guid Id { get; set; }

    public partial LazyLoad<ICrashChild> LazyChild { get; set; }

    [Remote]
    [Fetch]
    internal Task Fetch(Guid id)
    {
        using (PauseAllActions())
        {
            this["Id"].LoadValue(id);
        }

        // LazyLoad is created in the constructor with a loader that uses this.Id.
        // The [Fetch] method only sets the Id; the loader resolves it at load-time.
        return Task.CompletedTask;
    }
}
