// -----------------------------------------------------------------------------
// Design.Tests - A ViewModel Over a Live Aggregate
// -----------------------------------------------------------------------------
// Pins that what the ViewModel exposes follows the aggregate's rules: change
// the aggregate, the ViewModel's values change, and nothing in the ViewModel
// decided them. Runs on the client scope of the pair, where the rules run.
// -----------------------------------------------------------------------------

using Design.App.ViewModels;
using Design.Domain.Aggregates.WorkOrderAggregate;
using Design.Infrastructure.InMemory;
using Design.Tests.CompositionTests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Design.Tests.ViewModelTests;

[TestClass]
public class WorkOrderViewModelTests
{
    private IServiceScope _server = null!;
    private IServiceScope _client = null!;
    private WorkOrderViewModel _vm = null!;

    [TestInitialize]
    public async Task TestInitialize()
    {
        (_server, _client, _) = DesignClientServer.Scopes();
        _vm = _client.ServiceProvider.GetRequiredService<WorkOrderViewModel>();
        await _vm.New();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _client.Dispose();
        _server.Dispose();
    }

    #region skill-viewmodel-over-aggregate-test
    [TestMethod]
    public async Task HoldBanner_FollowsTheAggregatesIsHeldFact()
    {
        var workOrder = _vm.WorkOrder!;
        Assert.IsFalse(_vm.ShowHoldBanner);

        workOrder.IsOnHold = true;
        await workOrder.WaitForTasks();

        Assert.IsTrue(workOrder.IsHeld, "the entity's rule derived the fact");
        Assert.IsTrue(_vm.ShowHoldBanner, "the ViewModel only maps it");
    }

    [TestMethod]
    public async Task Approve_FollowsTheAggregatesAdmissionAndStatus()
    {
        var workOrder = _vm.WorkOrder!;
        Assert.AreEqual("Pending", workOrder.Status);
        Assert.IsTrue(_vm.CanApprove);

        _vm.Approve("reviewer");
        await workOrder.WaitForTasks();

        Assert.AreEqual("Approved", workOrder.Status);
        Assert.AreEqual("reviewer", workOrder.ApprovedBy);
        Assert.IsFalse(_vm.CanApprove, "CanApprove is the entity's rule, recomputed on Status");
    }

    [TestMethod]
    public async Task OverBudget_IsTheAggregatesFact_TheViewModelExposesTheEntity()
    {
        var workOrder = _vm.WorkOrder!;
        workOrder.Budget = 100m;
        _vm.AddTask("Design");
        var task = workOrder.Tasks![0];
        task.Hours = 10m;
        task.Rate = 20m;
        await workOrder.WaitForTasks();

        Assert.AreEqual(200m, workOrder.TotalCost);
        Assert.IsTrue(workOrder.IsOverBudget);

        workOrder.Budget = 500m;
        await workOrder.WaitForTasks();

        Assert.IsFalse(workOrder.IsOverBudget);
    }

    [TestMethod]
    public async Task HasUnschedulableTasks_FollowsTheChildrensRule_OnTheClient()
    {
        var workOrder = _vm.WorkOrder!;
        _vm.AddTask("Design");
        var task = workOrder.Tasks![0];
        task.Hours = 2m;
        await workOrder.WaitForTasks();

        Assert.IsTrue(task.IsSchedulable);
        Assert.IsFalse(workOrder.HasUnschedulableTasks);

        // The child's rule reads the root's IsOnHold; the root's flag follows the child.
        workOrder.IsOnHold = true;
        await workOrder.WaitForTasks();

        Assert.IsFalse(task.IsSchedulable);
        Assert.IsTrue(workOrder.HasUnschedulableTasks, "the page binds this fact; the ViewModel exposes the entity");
    }

    [TestMethod]
    public async Task Save_SavableWorkOrder_CrossesToTheServerAndHoldsTheReturnedInstance()
    {
        var sent = _vm.WorkOrder!;
        sent.Budget = 1000m;
        _vm.AddTask("Design");
        sent.Tasks![0].Hours = 4m;
        sent.Tasks[0].Rate = 50m;
        await sent.WaitForTasks();

        Assert.IsTrue(_vm.CanSave);

        await _vm.Save();

        Assert.IsFalse(ReferenceEquals(sent, _vm.WorkOrder), "Save returned the server's instance");
        Assert.IsFalse(_vm.WorkOrder!.IsNew);
        Assert.IsFalse(_vm.WorkOrder.IsModified);
        Assert.AreEqual(200m, _vm.WorkOrder.TotalCost);

        var store = (InMemoryWorkOrderRepository)_server.ServiceProvider.GetRequiredService<IWorkOrderRepository>();
        Assert.IsTrue(store.Store.ContainsKey(_vm.WorkOrder.Id));

        // And Load brings it back through the same seam
        await _vm.Load(_vm.WorkOrder.Id);
        Assert.AreEqual(1000m, _vm.WorkOrder!.Budget);
        Assert.AreEqual(1, _vm.WorkOrder.Tasks!.Count);
    }
    #endregion
}
