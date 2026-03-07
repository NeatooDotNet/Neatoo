using Neatoo.RemoteFactory;

namespace Neatoo.UnitTest.Integration.Concepts.Serialization.LazyLoadTests;

public interface ILazyLoadEntityObject : IEntityBase
{
    Guid ID { get; set; }
    string Name { get; set; }
    LazyLoad<string> LazyDescription { get; set; }
    LazyLoad<ILazyLoadEntityObject> LazyChild { get; set; }
}

[Factory]
public partial class LazyLoadEntityObject : EntityBase<LazyLoadEntityObject>, ILazyLoadEntityObject
{
    public LazyLoadEntityObject(IEntityBaseServices<LazyLoadEntityObject> services) : base(services)
    {
    }

    public partial Guid ID { get; set; }
    public partial string Name { get; set; }

    // LazyLoad<T> is a regular property -- not partial, not in PropertyManager
    public LazyLoad<string> LazyDescription { get; set; } = null!;
    public LazyLoad<ILazyLoadEntityObject> LazyChild { get; set; } = null!;

    [Fetch]
    public Task Fetch(Guid id, string name, [Service] ILazyLoadFactory lazyLoadFactory)
    {
        using (PauseAllActions())
        {
            this["ID"].LoadValue(id);
            this["Name"].LoadValue(name);
        }
        LazyDescription = lazyLoadFactory.Create<string>($"Description for {name}");
        return Task.CompletedTask;
    }

    [Fetch]
    public Task Fetch(Guid id, string name, string description, [Service] ILazyLoadFactory lazyLoadFactory)
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
