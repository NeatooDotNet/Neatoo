using Neatoo.RemoteFactory;

namespace Neatoo.UnitTest.Integration.Concepts.Serialization.LazyLoadTests;

public interface ILazyLoadValidateObject : IValidateBase
{
    Guid ID { get; set; }
    string Name { get; set; }
    LazyLoad<string> LazyContent { get; set; }
}

[Factory]
public partial class LazyLoadValidateObject : ValidateBase<LazyLoadValidateObject>, ILazyLoadValidateObject
{
    public LazyLoadValidateObject(IValidateBaseServices<LazyLoadValidateObject> services) : base(services)
    {
    }

    public partial Guid ID { get; set; }
    public partial string Name { get; set; }

    public partial LazyLoad<string> LazyContent { get; set; }

    [Fetch]
    public Task Fetch(Guid id, string name, string content, [Service] ILazyLoadFactory lazyLoadFactory)
    {
        ID = id;
        Name = name;
        LazyContent = lazyLoadFactory.Create<string>(content);
        return Task.CompletedTask;
    }
}
