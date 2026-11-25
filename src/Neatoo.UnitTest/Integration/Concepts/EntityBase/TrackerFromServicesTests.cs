using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Internal;
using Neatoo.RemoteFactory;

namespace Neatoo.UnitTest.Integration.Concepts.EntityBase;

public interface ITrackerEntity : IEntityRoot
{
    string? Name { get; set; }
}

[Factory]
internal partial class TrackerEntity : EntityBase<TrackerEntity>, ITrackerEntity
{
    public TrackerEntity(IEntityBaseServices<TrackerEntity> services) : base(services) { }

    public partial string? Name { get; set; }

    /// <summary>The tracker the services supplied.</summary>
    internal IAsyncTasks Tracker => this.RunningTasks;

    [Create]
    public void Create() { }
}

/// <summary>
/// An entity takes its task tracker from the factory registered in DI, so a consumer who
/// registers a tracker of their own replaces the framework's.
/// </summary>
[TestClass]
public class TrackerFromServicesTests
{
    [TestMethod]
    public void EntityBase_TakesItsTrackerFromTheRegisteredFactory()
    {
        var supplied = new List<IAsyncTasks>();
        var services = new ServiceCollection();
        services.AddNeatooServices(NeatooFactory.Logical, typeof(TrackerEntity).Assembly);
        services.AddTransient<CreateAsyncTasks>(_ => () =>
        {
            var tracker = new AsyncTasks();
            supplied.Add(tracker);
            return tracker;
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var created = (TrackerEntity)scope.ServiceProvider.GetRequiredService<ITrackerEntityFactory>().Create();

        // The same factory supplies each property's tracker, so it is called more than once.
        CollectionAssert.Contains(supplied, created.Tracker);
        Assert.IsTrue(supplied.Count >= 2, "the entity's tracker and one for each of its properties");
    }

    [TestMethod]
    public void EntityBase_WithNothingRegisteredOverIt_TakesTheFrameworkTracker()
    {
        var services = new ServiceCollection();
        services.AddNeatooServices(NeatooFactory.Logical, typeof(TrackerEntity).Assembly);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var created = (TrackerEntity)scope.ServiceProvider.GetRequiredService<ITrackerEntityFactory>().Create();

        Assert.IsInstanceOfType<AsyncTasks>(created.Tracker);
    }
}
