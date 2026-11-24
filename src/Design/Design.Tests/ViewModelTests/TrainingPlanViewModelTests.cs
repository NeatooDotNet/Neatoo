// -----------------------------------------------------------------------------
// Design.Tests - The TrainingPlan Screen's ViewModel
// -----------------------------------------------------------------------------
// Pins that the ViewModel only forwards gestures: the plan's rules decide the
// minutes and the admission. Runs on the client scope of the pair, as the
// screen does.
// -----------------------------------------------------------------------------

using Design.App.ViewModels;
using Design.Tests.CompositionTests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Design.Tests.ViewModelTests;

[TestClass]
public class TrainingPlanViewModelTests
{
    private IServiceScope _server = null!;
    private IServiceScope _client = null!;
    private TrainingPlanViewModel _vm = null!;

    [TestInitialize]
    public async Task TestInitialize()
    {
        (_server, _client, _) = DesignClientServer.Scopes();
        _vm = _client.ServiceProvider.GetRequiredService<TrainingPlanViewModel>();
        await _vm.New();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _client.Dispose();
        _server.Dispose();
    }

    [TestMethod]
    public void New_CreatesADraftPlanWithNoIntervals()
    {
        Assert.IsNotNull(_vm.Plan);
        Assert.AreEqual("Draft", _vm.Plan.Status);
        Assert.AreEqual(0, _vm.Plan.Intervals!.Count);
        Assert.IsTrue(_vm.Plan.CanApprove);
    }

    [TestMethod]
    public async Task AddInterval_JoinsThePlan_AndTheFitSharesTheMinutes()
    {
        await _vm.AddInterval("Warm-up");
        await _vm.AddInterval("Main");

        var plan = _vm.Plan!;
        CollectionAssert.AreEqual(new[] { 30, 30 }, plan.Intervals!.Select(i => i.Minutes).ToArray(),
            "both intervals start at the default effort, so the fit shares 60 evenly");
        Assert.AreEqual(60, plan.PlannedMinutes);
    }

    [TestMethod]
    public async Task VerbBoundSlider_WritesThroughSlideMinutes_AndTheFitHoldsTheSlidInterval()
    {
        await _vm.AddInterval("Warm-up");
        await _vm.AddInterval("Main");
        var (warmUp, main) = (_vm.Plan!.Intervals![0], _vm.Plan.Intervals[1]);

        // What the row's minutes slider calls through its Setter.
        Func<int, Task> setter = warmUp.SlideMinutes;
        await setter(20);

        Assert.AreEqual(20, warmUp.Minutes);
        Assert.AreEqual(40, main.Minutes, "the fit held the slid interval and gave the rest to the other");
    }

    [TestMethod]
    public async Task Approve_CallsTheVerb_AndAdmissionFollows()
    {
        await _vm.AddInterval("Warm-up");

        await _vm.Approve("ada");

        Assert.AreEqual("Approved", _vm.Plan!.Status);
        Assert.AreEqual("ada", _vm.Plan.ApprovedBy);
        Assert.IsFalse(_vm.Plan.CanApprove, "the plan's admission fact, which the button binds");
    }
}
