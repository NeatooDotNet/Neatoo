// -----------------------------------------------------------------------------
// Design.Tests - Job Over Entities
// -----------------------------------------------------------------------------
// Pins the shape of a server job that reuses a Neatoo aggregate (D4): the verb
// triggers the rules, WaitForTasks settles them, IsSavable decides. Three
// outcomes from one run: saved, reported as invalid, skipped as unchanged.
// Nothing is thrown and nothing is caught.
// -----------------------------------------------------------------------------

using Design.Domain.Aggregates.OrderAggregate;
using Design.Domain.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Design.Tests.JobTests;

[TestClass]
public class SubmitDraftOrdersTests
{
    private IServiceScope _scope = null!;
    private MockOrderRepository _repository = null!;
    private SubmitDraftOrders.Run _run = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _scope = DesignTestServices.GetScope();
        _repository = (MockOrderRepository)_scope.GetRequiredService<IOrderRepository>();
        _run = _scope.GetRequiredService<SubmitDraftOrders.Run>();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _scope.Dispose();
    }

    #region skill-job-over-entities-test
    [TestMethod]
    public async Task Run_ValidInvalidAndUnchangedOrders_SavesReportsAndSkipsWithoutThrowing()
    {
        // A draft with items: Submit makes it valid and modified -> saved
        var valid = _repository.SeedOrder();
        // A draft with no items: Submit trips "Order must have at least one item" -> reported
        var invalid = _repository.SeedOrder(items: []);
        // Already submitted: Submit changes nothing -> skipped
        var unchanged = _repository.SeedOrder();
        unchanged.Status = "Submitted";

        var report = await _run([valid.Id, invalid.Id, unchanged.Id]);

        CollectionAssert.AreEqual(new[] { valid.Id }, report.Submitted.ToList());
        Assert.AreEqual("Submitted", _repository.Store[valid.Id].Status);

        Assert.AreEqual(1, report.Rejected.Count);
        Assert.AreEqual(invalid.Id, report.Rejected[0].OrderId);
        Assert.IsTrue(report.Rejected[0].Messages.Any(m => m.Contains("at least one item")));
        Assert.AreEqual("Draft", _repository.Store[invalid.Id].Status, "an invalid order is not written");

        CollectionAssert.AreEqual(new[] { unchanged.Id }, report.Unchanged.ToList());
    }
    #endregion

    [TestMethod]
    public async Task Run_UnknownOrder_Throws()
    {
        // A missing row is an application failure, not a report entry (D2).
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => _run([Guid.NewGuid()]));
    }
}
