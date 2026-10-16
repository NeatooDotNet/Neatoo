// -----------------------------------------------------------------------------
// Design.Tests - Row Components and the Page's Own Subscription
// -----------------------------------------------------------------------------
// The Order and WorkOrder pages subscribe only to their root's own
// PropertyChanged; each child row is a component that refreshes for its own
// child. These pin both halves: the rows re-render for their child's facts, and
// the root raises its own PropertyChanged for the totals the page shows.
// -----------------------------------------------------------------------------

using System.ComponentModel;
using System.Globalization;
using Bunit;
using Design.App.Components;
using Design.Domain.Aggregates.OrderAggregate;
using Design.Domain.Aggregates.WorkOrderAggregate;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MudBlazor;
using MudBlazor.Services;

namespace Design.Tests.ComponentTests;

[TestClass]
public sealed class RowComponentTests : IAsyncDisposable
{
    private BunitContext _context = null!;
    private IServiceScope _scope = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _context = new BunitContext();
        _context.Services.AddMudServices();
        _context.JSInterop.Mode = JSRuntimeMode.Loose;
        _context.Render<MudPopoverProvider>();
        _scope = DesignTestServices.GetScope();
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        _scope.Dispose();
    }

    private async Task<(IOrder Order, IOrderItem Item)> OrderWithOneItem()
    {
        var order = await _scope.GetRequiredService<IOrderFactory>().Create();
        var item = _scope.GetRequiredService<IOrderItemFactory>().Create();
        order.Items!.Add(item);
        item.UnitPrice = 10m;
        item.Quantity = 1;
        await order.WaitForTasks();
        return (order, item);
    }

    private async Task<(IWorkOrder WorkOrder, IWorkOrderTask Task)> WorkOrderWithOneTask()
    {
        var workOrder = await _scope.GetRequiredService<IWorkOrderFactory>().Create();
        var task = _scope.GetRequiredService<IWorkOrderTaskFactory>().Create("Design", 1);
        workOrder.Tasks!.Add(task);
        task.Hours = 10m;
        task.Rate = 10m;
        await workOrder.WaitForTasks();
        return (workOrder, task);
    }

    [TestMethod]
    public async Task OrderItemEditor_RefreshesItsLineTotal_WhenItsItemChanges()
    {
        var (order, item) = await OrderWithOneItem();
        var row = _context.Render<OrderItemEditor>(p => p.Add(c => c.Item, item));
        Assert.IsTrue(row.Markup.Contains(10m.ToString("C", CultureInfo.CurrentCulture)));

        await row.InvokeAsync(async () => { item.Quantity = 2; await order.WaitForTasks(); });

        row.WaitForAssertion(() => Assert.IsTrue(row.Markup.Contains(20m.ToString("C", CultureInfo.CurrentCulture)),
            "the row re-rendered for its own item's LineTotal; the page did not redraw"));
    }

    [TestMethod]
    public async Task OrderItemEditor_DeleteButton_RaisesOnRemove()
    {
        var (_, item) = await OrderWithOneItem();
        var removed = false;
        var row = _context.Render<OrderItemEditor>(p => p
            .Add(c => c.Item, item)
            .Add(c => c.OnRemove, () => { removed = true; }));

        // The delete icon, not the numeric fields' spin buttons.
        await row.FindComponent<MudIconButton>().Find("button").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        Assert.IsTrue(removed, "the row forwards the gesture; the page hands it to the ViewModel");
    }

    [TestMethod]
    public async Task WorkOrderTaskEditor_RefreshesItsCost_WhenTheRootPushesADiscountDown()
    {
        var (workOrder, task) = await WorkOrderWithOneTask();
        var row = _context.Render<WorkOrderTaskEditor>(p => p.Add(c => c.Item, task));
        Assert.IsTrue(row.Markup.Contains(100m.ToString("C", CultureInfo.CurrentCulture)));

        await row.InvokeAsync(async () => { workOrder.Discount = 0.5m; await workOrder.WaitForTasks(); });

        row.WaitForAssertion(() => Assert.IsTrue(row.Markup.Contains(50m.ToString("C", CultureInfo.CurrentCulture)),
            "the root's rule wrote the task's discount, and the task's row re-rendered"));
    }

    [TestMethod]
    public async Task Order_RaisesItsOwnPropertyChanged_ForTheTotalThePageShows()
    {
        var (order, item) = await OrderWithOneItem();
        var raised = new List<string?>();
        PropertyChangedEventHandler handler = (_, e) => raised.Add(e.PropertyName);
        order.PropertyChanged += handler;

        item.Quantity = 3;
        await order.WaitForTasks();
        order.PropertyChanged -= handler;

        CollectionAssert.Contains(raised, nameof(IOrder.TotalAmount),
            "the page's own-entity subscription hears the total a child's edit changed");
    }

    [TestMethod]
    public async Task WorkOrder_RaisesItsOwnPropertyChanged_ForTheTotalThePageShows()
    {
        var (workOrder, task) = await WorkOrderWithOneTask();
        var raised = new List<string?>();
        PropertyChangedEventHandler handler = (_, e) => raised.Add(e.PropertyName);
        workOrder.PropertyChanged += handler;

        task.Hours = 20m;
        await workOrder.WaitForTasks();
        workOrder.PropertyChanged -= handler;

        CollectionAssert.Contains(raised, nameof(IWorkOrder.TotalCost));
    }
}
