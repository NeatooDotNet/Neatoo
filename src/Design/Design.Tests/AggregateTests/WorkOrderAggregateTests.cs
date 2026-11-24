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

        await order.Approve("ada");
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
        Assert.IsFalse(order.IsHeld);

        order.IsOnHold = true;
        await order.WaitForTasks();

        Assert.IsFalse(design.IsSchedulable, "The root re-ran the tasks' rules; the child rule read Parent.IsOnHold");
        Assert.IsTrue(order.HasUnschedulableTasks, "...and the child's change bubbled into the root's flag");
        Assert.IsTrue(order.IsHeld);
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
        await order.Approve("ada");
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

    #region skill-fetch-computes-derived-test
    [TestMethod]
    public async Task Fetch_RecomputesTheRootsDerivedValues_LoadsTheChildAggregates_AndIsNotModified()
    {
        // Save through the aggregate so every derived value is what the rules
        // computed: one task unschedulable (zero hours), one over budget.
        var (order, design, build) = await CreateWithTwoTasks();
        order.Budget = 100m;
        order.IsOnHold = false;
        design.Hours = 0m;            // not schedulable
        build.Hours = 10m;
        build.Rate = 20m;             // 200 > budget
        await order.WaitForTasks();
        Assert.IsTrue(order.HasUnschedulableTasks);
        Assert.IsTrue(order.IsOverBudget);
        order = (IWorkOrder)await order.Save();

        var fetched = (await _factory.Fetch(order.Id))!;

        // #98: the root recomputed only its own rules (named by trigger) and
        // loaded the child aggregates; no finished child was written, so the
        // fetched graph is a clean baseline.
        Assert.IsFalse(fetched.IsModified, "a fetched aggregate is not modified");
        Assert.IsFalse(fetched.Tasks![0].IsModified);

        // The root's own derived values, recomputed by RunRules(nameof(Status)) / (nameof(Budget))
        Assert.AreEqual(order.CanApprove, fetched.CanApprove);
        Assert.AreEqual(order.IsHeld, fetched.IsHeld);
        Assert.AreEqual(order.IsOverBudget, fetched.IsOverBudget);
        Assert.AreEqual(order.Summary, fetched.Summary);

        // The aggregates over the children, loaded from the row
        Assert.AreEqual(200m, fetched.TotalCost);
        Assert.IsTrue(fetched.HasUnschedulableTasks, "persisted like TotalCost; no rule re-run writes the children");

        // The children's own derived values, loaded from their rows
        Assert.IsFalse(fetched.Tasks[0].IsSchedulable);
        Assert.AreEqual(200m, fetched.Tasks[1].Cost);
    }

    [TestMethod]
    public async Task Fetch_TargetedRunRules_DoesNotWriteTheFinishedChildren()
    {
        // The #98 hazard needs a root rule that would write a DIFFERENT value
        // than the child loaded: a task row whose Discount is not the root's.
        // Divergence is what a changed rule leaves behind in old rows. (Saved through
        // the aggregate the two always agree, so the test above
        // cannot tell the targeted form from RunRulesFlag.All; this one can.)
        var task = MockWorkOrderRepository.Task("Design", 1, 2m, 30m);
        task.Discount = 0.25m;                 // root row's Discount is 0
        var row = _repository.SeedWorkOrder(task);

        var fetched = (await _factory.Fetch(row.Id))!;

        // RunRules(nameof(Status)) / (nameof(Budget)) did not run the Discount
        // orchestrator, so the finished task kept its loaded value and is a clean
        // baseline. RunRulesFlag.All would have pushed 0 into it and marked it modified.
        Assert.AreEqual(0.25m, fetched.Tasks![0].Discount);
        Assert.IsFalse(fetched.Tasks[0].IsModified);
        Assert.IsFalse(fetched.IsModified);
    }
    #endregion
}
