// -----------------------------------------------------------------------------
// Design.Tests - The Two Compositions Serve Each Other
// -----------------------------------------------------------------------------
// Pins that Design.App's client composition and Design.Server's server
// composition work together: an aggregate created on the client crosses to the
// server, is serialized both ways, and lands in the server's store; and that
// the client really is in Remote mode - the call leaves through the keyed
// HttpClient, which a Server-mode client never touches.
// -----------------------------------------------------------------------------

using Design.Domain.Aggregates.OrderAggregate;
using Design.Infrastructure.InMemory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Design.Tests.CompositionTests;

[TestClass]
public class ClientServerPairTests
{
    private IServiceScope _server = null!;
    private IServiceScope _client = null!;
    private ServerLink _link = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        (_server, _client, _link) = DesignClientServer.Scopes();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _client.Dispose();
        _server.Dispose();
    }

    #region skill-client-server-pair-test
    [TestMethod]
    public async Task Order_CreatedOnTheClient_SavesThroughTheServerAndFetchesBack()
    {
        var factory = _client.ServiceProvider.GetRequiredService<IOrderFactory>();
        var itemFactory = _client.ServiceProvider.GetRequiredService<IOrderItemFactory>();

        var order = factory.Create();
        order.OrderNumber = "ORD-1001";
        order.CustomerName = "Ada";
        order.Items!.Add(itemFactory.Create("Widget", 2, 10.00m));
        await order.WaitForTasks();

        var saved = (IOrder)await order.Save();

        Assert.IsFalse(saved.IsNew);
        Assert.IsFalse(saved.IsModified);
        Assert.AreEqual(20.00m, saved.TotalAmount);

        // The server's store has the row: Design.Infrastructure, registered by AddDesignServer
        var store = (InMemoryOrderRepository)_server.ServiceProvider.GetRequiredService<IOrderRepository>();
        Assert.IsTrue(store.Store.ContainsKey(saved.Id));
        Assert.AreEqual("Ada", store.Store[saved.Id].CustomerName);

        // And it comes back through the same seam
        var fetched = await factory.Fetch(saved.Id);
        Assert.IsNotNull(fetched);
        Assert.AreEqual("ORD-1001", fetched.OrderNumber);
        Assert.AreEqual(1, fetched.Items!.Count);
    }

    [TestMethod]
    public async Task Client_IsInRemoteMode_TheCallLeavesThroughTheKeyedHttpClient()
    {
        var factory = _client.ServiceProvider.GetRequiredService<IOrderFactory>();

        Assert.AreEqual(0, _link.Calls);

        // Fetch of a missing id: [Remote], returns null, but must still cross.
        var missing = await factory.Fetch(Guid.NewGuid());

        Assert.IsNull(missing);
        Assert.AreEqual(1, _link.Calls, "a Server-mode client would have run the operation locally and never sent a request");
    }
    #endregion
}
