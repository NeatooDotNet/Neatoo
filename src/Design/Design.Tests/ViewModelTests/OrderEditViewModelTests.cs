// -----------------------------------------------------------------------------
// Design.Tests - The Save Handler
// -----------------------------------------------------------------------------
// Pins the UI contract for Save, run through the client/server pair so the
// handler's reassignment can be told from a dropped result (the local Save
// path hands back the same instance; the remote one cannot). A savable order
// is saved and the ViewModel holds the server's instance afterwards; an invalid
// one never reaches Save and keeps its messages. Nothing validates first.
// -----------------------------------------------------------------------------

using Design.App.ViewModels;
using Design.Domain.Aggregates.OrderAggregate;
using Design.Infrastructure.InMemory;
using Design.Tests.CompositionTests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Design.Tests.ViewModelTests;

[TestClass]
public class OrderEditViewModelTests
{
    private IServiceScope _server = null!;
    private IServiceScope _client = null!;
    private ServerLink _link = null!;
    private OrderEditViewModel _vm = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        (_server, _client, _link) = DesignClientServer.Scopes();
        _vm = _client.ServiceProvider.GetRequiredService<OrderEditViewModel>();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _client.Dispose();
        _server.Dispose();
    }

    #region skill-save-handler-test
    [TestMethod]
    public async Task Save_SavableOrder_SavesAndHoldsTheReturnedInstance()
    {
        _vm.New();
        var sent = _vm.Order!;
        sent.OrderNumber = "ORD-2001";
        sent.CustomerName = "Grace";
        _vm.AddItem();
        var item = sent.Items![0];
        item.ProductName = "Gadget";
        item.Quantity = 1;
        item.UnitPrice = 5.00m;
        await sent.WaitForTasks();

        Assert.IsTrue(_vm.CanSave);

        await _vm.Save();

        Assert.AreEqual(1, _link.Calls, "one round trip");
        Assert.IsFalse(ReferenceEquals(sent, _vm.Order), "the handler holds the instance Save returned, not the one it sent");
        Assert.IsFalse(_vm.Order!.IsNew);
        Assert.IsFalse(_vm.Order.IsModified);

        var store = (InMemoryOrderRepository)_server.ServiceProvider.GetRequiredService<IOrderRepository>();
        Assert.AreEqual("Grace", store.Store[_vm.Order.Id].CustomerName);
    }

    [TestMethod]
    public async Task Save_InvalidOrder_DoesNotCallSaveAndKeepsTheMessages()
    {
        _vm.New();
        var order = _vm.Order!;
        order.Submit();  // no items: "Order must have at least one item"
        await order.WaitForTasks();

        Assert.IsFalse(order.IsValid);
        Assert.IsFalse(_vm.CanSave);

        await _vm.Save();

        Assert.AreEqual(0, _link.Calls, "an invalid order never reaches Save");
        Assert.IsTrue(ReferenceEquals(order, _vm.Order));
        Assert.IsTrue(order.PropertyMessages.Any(m => m.Message.Contains("at least one item")));
    }
    #endregion
}
