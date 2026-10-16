// -----------------------------------------------------------------------------
// Design.Tests - TrainingPlan Aggregate Tests
// -----------------------------------------------------------------------------
// Pins Aggregates/TrainingPlanAggregate: facts written plainly, consequences
// that follow whoever wrote them, and the few intents that state cannot hold.
// Every assertion runs after the awaited write: the cascade has settled.
// -----------------------------------------------------------------------------

using Design.Domain.Aggregates.TrainingPlanAggregate;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Design.Tests.AggregateTests;

[TestClass]
public class TrainingPlanTests
{
    private IServiceScope _scope = null!;
    private ITrainingPlanFactory _factory = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _scope = DesignTestServices.GetScope();
        _factory = _scope.GetRequiredService<ITrainingPlanFactory>();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _scope.Dispose();
    }

    /// <summary>A 60-minute plan with efforts 2, 6, 2: the fit gives 12, 36, 12.</summary>
    private async Task<(ITrainingPlan Plan, IInterval WarmUp, IInterval Main, IInterval CoolDown)> ThreeIntervals()
    {
        var plan = await _factory.Create();
        var warmUp = await plan.AddInterval("Warm-up", 2);
        var main = await plan.AddInterval("Main", 6);
        var coolDown = await plan.AddInterval("Cool-down", 2);
        return (plan, warmUp, main, coolDown);
    }

    private static int[] Minutes(params IInterval[] intervals) => intervals.Select(i => i.Minutes).ToArray();

    [TestMethod]
    public async Task Fit_SharesTheApprovedMinutesByEffort()
    {
        var (plan, warmUp, main, coolDown) = await ThreeIntervals();

        CollectionAssert.AreEqual(new[] { 12, 36, 12 }, Minutes(warmUp, main, coolDown));
        Assert.AreEqual(60, plan.PlannedMinutes);
        Assert.IsFalse(plan.IsBusy, "the cascade converged");
    }

    #region skill-test-slider-is-a-fact
    [TestMethod]
    public async Task EffortSlider_IsAFact_TheFitFollowsWhoeverWroteIt()
    {
        var (plan, warmUp, main, coolDown) = await ThreeIntervals();

        await main.SetEffort(8);   // what a slider binding does: a plain, awaited write

        CollectionAssert.AreEqual(new[] { 10, 40, 10 }, Minutes(warmUp, main, coolDown));
        Assert.AreEqual(60, plan.PlannedMinutes);
    }
    #endregion

    #region skill-test-anchor-evaporates
    [TestMethod]
    public async Task SlideMinutes_FitsTheOthersAroundIt_AndTheAnchorEvaporates()
    {
        var (plan, warmUp, main, coolDown) = await ThreeIntervals();

        await warmUp.SlideMinutes(20);
        CollectionAssert.AreEqual(new[] { 20, 30, 10 }, Minutes(warmUp, main, coolDown),
            "the slid interval held for its fit; the others shared the remaining 40 by effort");

        await coolDown.SetEffort(6);
        CollectionAssert.AreEqual(new[] { 9, 26, 25 }, Minutes(warmUp, main, coolDown),
            "the next fact refit everything: the anchor did not outlive its gesture");
        Assert.AreEqual(60, plan.PlannedMinutes);
    }
    #endregion

    #region skill-test-ordered-write
    [TestMethod]
    public async Task LockMinutes_LocksThenSets_AndTheFitSharesTheRest()
    {
        var (plan, warmUp, main, coolDown) = await ThreeIntervals();

        await main.LockMinutes(30);
        CollectionAssert.AreEqual(new[] { 15, 30, 15 }, Minutes(warmUp, main, coolDown));

        await plan.SetApprovedMinutes(90);
        CollectionAssert.AreEqual(new[] { 30, 30, 30 }, Minutes(warmUp, main, coolDown),
            "the locked interval kept its minutes; the free ones shared the new total");

        await main.SetMinutesLocked(false);   // unlocking is a plain write of a fact
        CollectionAssert.AreEqual(new[] { 18, 54, 18 }, Minutes(warmUp, main, coolDown));
    }
    #endregion

    [TestMethod]
    public async Task NextMark_IsAFactTheUiWritesBack()
    {
        var (plan, _, main, _) = await ThreeIntervals();
        Assert.AreEqual(7, main.NextEffortUp);
        Assert.AreEqual(5, main.NextEffortDown);

        await main.SetEffort(main.NextEffortUp);   // the nudge button

        Assert.AreEqual(7, main.Effort);
        Assert.AreEqual(8, main.NextEffortUp);
        Assert.AreEqual(6, main.NextEffortDown);
        CollectionAssert.AreEqual(new[] { 11, 39, 10 }, Minutes(plan.Intervals!.ToArray()),
            "the nudge is a fact like any other: the fit re-weighted by 2, 7, 2");
    }

    [TestMethod]
    public async Task NextMark_StopsAtTheEnds_AndEffortOutsideTheRangeIsInvalid()
    {
        var (_, warmUp, _, _) = await ThreeIntervals();

        await warmUp.SetEffort(10);
        Assert.AreEqual(10, warmUp.NextEffortUp, "the top mark is the ceiling");

        await warmUp.SetEffort(1);
        Assert.AreEqual(1, warmUp.NextEffortDown, "the bottom mark is the floor");

        await warmUp.SetEffort(11);
        Assert.IsFalse(warmUp.IsValid, "Effort is between 1 and 10");
    }

    [TestMethod]
    public async Task AddingAnInterval_IsACountChange_TheFitFollowsEvenWhenNoOtherFactChanges()
    {
        var plan = await _factory.Create();
        var warmUp = await plan.AddInterval("Warm-up", 2);
        var main = await plan.AddInterval("Main", 6);

        // The interval is built with its effort, so the add is the only change:
        // only the list's Count can refit.
        var coolDown = await plan.AddInterval("Cool-down", 5);

        CollectionAssert.AreEqual(new[] { 10, 27, 23 }, Minutes(warmUp, main, coolDown));
        Assert.AreEqual(60, plan.PlannedMinutes);
    }

    [TestMethod]
    public async Task NewIntervalWithABlankName_EntersInvalid_AndAdmissionFollows()
    {
        var plan = await _factory.Create();

        var unnamed = await plan.AddInterval("", 3);

        Assert.IsFalse(unnamed.IsValid, "the interval's Create ran its own rules");
        Assert.IsFalse(plan.CanApprove);
    }

    [TestMethod]
    public async Task AnchorScopes_Nest_AndAnInnerScopeRestoresTheOuterAnchor()
    {
        var (plan, warmUp, main, coolDown) = await ThreeIntervals();
        var fit = (ITrainingPlanFit)plan;

        using (fit.AnchorFit(warmUp))
        {
            using (fit.AnchorFit(main))
            {
            }

            await coolDown.SetEffort(6);
            CollectionAssert.AreEqual(new[] { 12, 24, 24 }, Minutes(warmUp, main, coolDown),
                "the outer anchor came back: warm-up held, the others shared 48 by 6, 6");
        }

        await coolDown.SetEffort(4);
        CollectionAssert.AreEqual(new[] { 10, 30, 20 }, Minutes(warmUp, main, coolDown),
            "outside every scope nothing is held");
    }

    [TestMethod]
    public async Task AnchorScope_EndsWhenTheGestureThrows()
    {
        var (_, warmUp, main, coolDown) = await ThreeIntervals();
        var fit = (ITrainingPlanFit)warmUp.Parent!;

        try
        {
            using (fit.AnchorFit(warmUp))
            {
                throw new InvalidOperationException("the gesture failed");
            }
        }
        catch (InvalidOperationException)
        {
        }

        await main.SetEffort(8);
        CollectionAssert.AreEqual(new[] { 10, 40, 10 }, Minutes(warmUp, main, coolDown),
            "the failed gesture left no anchor behind");
    }

    [TestMethod]
    public async Task EveryIntervalLocked_TheFitHasNothingToShare()
    {
        var (plan, warmUp, main, coolDown) = await ThreeIntervals();

        await warmUp.LockMinutes(10);
        await main.LockMinutes(20);
        await coolDown.LockMinutes(20);   // 50 of the approved 60: ten minutes no free interval can take

        CollectionAssert.AreEqual(new[] { 10, 20, 20 }, Minutes(warmUp, main, coolDown),
            "every interval is held, so the fit has nothing to share the remaining ten into");
        Assert.AreEqual(50, plan.PlannedMinutes);
    }

    [TestMethod]
    public async Task ApprovedMinutesMustBePositive_AndAdmissionFollows()
    {
        var (plan, _, _, _) = await ThreeIntervals();

        await plan.SetApprovedMinutes(0);

        Assert.IsFalse(plan.IsValid);
        Assert.IsFalse(plan.CanApprove);
    }

    #region skill-test-admission-on-meta-state
    [TestMethod]
    public async Task CanApprove_FollowsValidity()
    {
        var (plan, _, main, _) = await ThreeIntervals();
        Assert.IsTrue(plan.CanApprove);

        await main.LockMinutes(80);   // more than the 60 approved

        Assert.IsFalse(plan.IsValid);
        Assert.IsFalse(plan.CanApprove, "validity went false, so the admission fact followed");

        await plan.SetApprovedMinutes(100);

        Assert.IsTrue(plan.IsValid);
        Assert.IsTrue(plan.CanApprove);
    }
    #endregion

    [TestMethod]
    public async Task Approve_StampsThenSetsStatus_AndAdmissionFollows()
    {
        var (plan, _, _, _) = await ThreeIntervals();

        await plan.Approve("ada");

        Assert.AreEqual("ada", plan.ApprovedBy);
        Assert.AreEqual("Approved", plan.Status);
        Assert.IsFalse(plan.CanApprove, "the status's consequence landed before the verb returned");
    }

    [TestMethod]
    public async Task RemovingAnInterval_IsACountChange_TheFitFollows()
    {
        var (plan, warmUp, main, coolDown) = await ThreeIntervals();

        plan.Intervals!.Remove(coolDown);
        await plan.WaitForTasks();

        CollectionAssert.AreEqual(new[] { 15, 45 }, Minutes(warmUp, main));
        Assert.AreEqual(60, plan.PlannedMinutes);
    }
}
