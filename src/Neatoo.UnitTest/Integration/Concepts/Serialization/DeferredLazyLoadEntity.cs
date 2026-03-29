using Neatoo.RemoteFactory;

namespace Neatoo.UnitTest.Integration.Concepts.Serialization.DeferredLazyLoadTests;

/// <summary>
/// Entity with a deferred LazyLoad created in the constructor.
/// Reproduces a bug: when a LazyLoad with a deferred loader (not pre-loaded)
/// goes through serialization, IsLoaded becomes true even though no load
/// was ever triggered. The existing LazyLoadEntityObject does NOT reproduce
/// this because its constructor doesn't create any LazyLoad -- the Fetch
/// methods always create pre-loaded LazyLoads.
/// </summary>
public interface IDeferredLazyLoadEntity : IEntityBase
{
    Guid ID { get; set; }
    string Name { get; set; }
    EntityLazyLoad<string> LazyDescription { get; }
}

[Factory]
public partial class DeferredLazyLoadEntity : EntityBase<DeferredLazyLoadEntity>, IDeferredLazyLoadEntity
{
    /// <summary>
    /// Constructor creates a LazyLoad with a deferred loader.
    /// This is the pattern that triggers the bug: the loader produces a value
    /// but the LazyLoad is NOT pre-loaded (IsLoaded=false, Value=null).
    /// After serialization round-trip, IsLoaded incorrectly becomes true.
    /// </summary>
    public DeferredLazyLoadEntity(
        IEntityBaseServices<DeferredLazyLoadEntity> services,
        [Service] IEntityLazyLoadFactory lazyLoadFactory) : base(services)
    {
        LazyDescription = lazyLoadFactory.Create<string>(async () =>
        {
            return await Task.FromResult("loaded from deferred loader");
        });
    }

    public partial Guid ID { get; set; }
    public partial string Name { get; set; }
    public partial EntityLazyLoad<string> LazyDescription { get; set; }

    /// <summary>
    /// Create method that leaves the deferred LazyLoad from the constructor in place.
    /// Only sets ID and Name; LazyDescription remains unloaded with its deferred loader.
    /// </summary>
    [Create]
    public Task Create(Guid id, string name)
    {
        using (PauseAllActions())
        {
            this["ID"].LoadValue(id);
            this["Name"].LoadValue(name);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Fetch method that leaves the deferred LazyLoad from the constructor in place.
    /// Simulates fetching an entity where the lazy-loaded data is not eagerly loaded.
    /// </summary>
    [Fetch]
    public Task Fetch(Guid id, string name)
    {
        using (PauseAllActions())
        {
            this["ID"].LoadValue(id);
            this["Name"].LoadValue(name);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Fetch overload that eagerly loads the description.
    /// Replaces the deferred LazyLoad with a pre-loaded one.
    /// Used for comparison -- this path should NOT exhibit the bug.
    /// </summary>
    [Fetch]
    public Task Fetch(Guid id, string name, string description, [Service] IEntityLazyLoadFactory lazyLoadFactory)
    {
        using (PauseAllActions())
        {
            this["ID"].LoadValue(id);
            this["Name"].LoadValue(name);
        }
        LazyDescription = lazyLoadFactory.Create<string>(description);
        return Task.CompletedTask;
    }
}
