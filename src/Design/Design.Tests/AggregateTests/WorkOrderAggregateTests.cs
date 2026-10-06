// -----------------------------------------------------------------------------
// Design.Tests - WorkOrder Aggregate Tests
// -----------------------------------------------------------------------------
// Pins Aggregates/WorkOrderAggregate: the rule-placement patterns (CanX rule,
// visibility flags, aggregation, chained rules, parent-as-orchestrator,
// cross-sibling rules on the list) and a child reading Parent in a rule, a
// business method and its persistence mapping.
// -----------------------------------------------------------------------------

using Design.Domain.Aggregates.WorkOrderAggregate;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Design.Tests.AggregateTests;

[TestClass]
public class WorkOrderAggregateTests
{
    private IServiceScope _scope = null!;
    private IWorkOrderFactory _factory = null!;
    private IWorkOrderTaskFactory _taskFactory = null!;
    private MockWorkOrderRepository _repository = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _scope = DesignTestServices.GetScope();
        _factory = _scope.GetRequiredService<IWorkOrderFactory>();
        _taskFactory = _scope.GetRequiredService<IWorkOrderTaskFactory>();
        _repository = (MockWorkOrderRepository)_scope.GetRequiredService<IWorkOrderRepository>();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _scope.Dispose();
    }

    private async Task<(IWorkOrder Order, IWorkOrderTask Design, IWorkOrderTask Build)> CreateWithTwoTasks()
    {
        var order = await _factory.Create();
        var design = _taskFactory.Create("Design", 1);
        var build = _taskFactory.Create("Build", 2);
        order.Tasks!.Add(design);
        order.Tasks.Add(build);
        return (order, design, build);
    }

    #region skill-can-x-test
    [TestMethod]
    public async Task Approve_SetsState_AndTheCanApproveRuleRecomputesAdmission()
    {
        var order = await _factory.Create();
        Assert.AreEqual("Pending", order.Status);
        Assert.IsTrue(order.CanApprove, "Create ran the rules, so the admission flag is already right");

        order.Approve("ada");
        await order.WaitForTasks();

        Assert.AreEqual("Approved", order.Status);
        Assert.AreEqual("ada", order.ApprovedBy);
        Assert.IsFalse(order.CanApprove, "The rule on Status recomputed the admission; nothing threw");
    }
    #endregion

    #region skill-aggregation-test
    [TestMethod]
    public async Task TaskCosts_RollUpToTheRoot_AndChainIntoIsOverBudget()
    {
        var (order, design, build) = await CreateWithTwoTasks();
        order.Budget = 100m;

        design.Hours = 2m;
        design.Rate = 30m;   // Cost 60
        build.Hours = 1m;
        build.Rate = 50m;    // Cost 50
        await order.WaitForTasks();

        Assert.AreEqual(60m, design.Cost, "The task's own rule computed Cost");
        Assert.AreEqual(110m, order.TotalCost, "The root's child-trigger rule summed the tasks");
        Assert.IsTrue(order.IsOverBudget, "Setting TotalCost triggered the chained rule");
    }
    #endregion

    #region skill-orchestrator-test
    [TestMethod]
    public async Task Discount_IsPushedToEveryTask_ByTheRootsRule()
    {
        var (order, design, build) = await CreateWithTwoTasks();
        order.Budget = 100m;
        design.Hours = 2m;
        design.Rate = 30m;
        build.Hours = 1m;
        build.Rate = 50m;
        await order.WaitForTasks();
        Assert.IsTrue(order.IsOverBudget);

        order.Discount = 0.5m;
        await order.WaitForTasks();

        Assert.AreEqual(0.5m, design.Discount, "The root's rule called ApplyParentDiscount on each task");
        Assert.AreEqual(30m, design.Cost, "...which re-ran the task's Cost rule");
        Assert.AreEqual(55m, order.TotalCost, "...which bubbled back up into the root's aggregation");
        Assert.IsFalse(order.IsOverBudget);
    }
    #endregion

    #region skill-parent-read-test
    [TestMethod]
    public async Task ChildRule_ReadsRootStateThroughParent_AndTheRootRerunsItWhenThatStateChanges()
    {
        var (order, design, build) = await CreateWithTwoTasks();
        design.Hours = 2m;
        build.Hours = 1m;
        await order.WaitForTasks();

        Assert.IsTrue(design.IsSchedulable, "Hours > 0 and the parent is not on hold");
        Assert.IsFalse(order.HasUnschedulableTasks);
        Assert.IsFalse(order.ShowHoldBanner);

        order.IsOnHold = true;
        await order.WaitForTasks();

        Assert.IsFalse(design.IsSchedulable, "The root re-ran the tasks' rules; the child rule read Parent.IsOnHold");
        Assert.IsTrue(order.HasUnschedulableTasks, "...and the child's change bubbled into the root's flag");
        Assert.IsTrue(order.ShowHoldBanner);
    }
    #endregion

    #region skill-cross-sibling-test
    [TestMethod]
    public async Task DuplicateSequence_InvalidatesBothSiblings_UntilOneChanges()
    {
        var (order, design, build) = await CreateWithTwoTasks();   // sequences 1 and 2
        await order.WaitForTasks();
        Assert.IsTrue(order.IsValid);

        build.Sequence = 1;
        await order.WaitForTasks();

        Assert.IsFalse(build.IsValid, "The task that changed sees the duplicate");
        Assert.IsFalse(design.IsValid, "The list re-ran the sibling's rules, so it sees it too");
        Assert.IsFalse(order.IsValid);

        build.Sequence = 3;
        await order.WaitForTasks();

        Assert.IsTrue(design.IsValid);
        Assert.IsTrue(build.IsValid);
        Assert.IsTrue(order.IsValid);
    }
    #endregion

    #region skill-parent-in-update-test
    [TestMethod]
    public async Task Save_TaskMapsRootStateIntoItsRow_ThroughParent()
    {
        var row = _repository.SeedWorkOrder(MockWorkOrderRepository.Task("Design", 1, 2m, 30m));

        var order = (await _factory.Fetch(row.Id))!;
        Assert.IsFalse(order.IsModified, "A fetched aggregate is a clean baseline");
        Assert.IsTrue(order.CanApprove, "Fetch re-ran the root's own rules for its derived values");

        order.Tasks![0].Hours = 3m;   // a modified child: the list hands it its row
        order.Approve("ada");
        await order.WaitForTasks();
        Assert.IsTrue(order.IsSavable);

        order = (IWorkOrder)await order.Save();

        var saved = _repository.Store[row.Id];
        Assert.AreEqual("Approved", saved.Status);
        Assert.AreEqual("ada", saved.ApprovedBy);
        Assert.AreEqual(3m, saved.Tasks.Single().Hours);
        Assert.AreEqual("Approved", saved.Tasks.Single().WorkOrderStatus,
            "The task read the root's status through Parent when it mapped itself");
        Assert.IsFalse(order.IsModified);
    }
    #endregion
}
