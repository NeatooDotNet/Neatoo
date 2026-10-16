// -----------------------------------------------------------------------------
// Design.Tests - The TrainingPlan Screen's Components
// -----------------------------------------------------------------------------
// The page subscribes to nothing; each component refreshes for what it shows.
// These render the row and the summary with bUnit and change the aggregate the
// way the screen does, then read the rendered markup.
// -----------------------------------------------------------------------------

using Bunit;
using Design.App.Components;
using Design.Domain.Aggregates.TrainingPlanAggregate;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MudBlazor;
using MudBlazor.Services;

namespace Design.Tests.ComponentTests;

[TestClass]
public sealed class TrainingPlanComponentTests : IAsyncDisposable
{
    private BunitContext _context = null!;
    private IServiceScope _scope = null!;
    private ITrainingPlanFactory _factory = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _context = new BunitContext();
        _context.Services.AddMudServices();
        _context.JSInterop.Mode = JSRuntimeMode.Loose;
        _context.Render<MudPopoverProvider>();

        _scope = DesignTestServices.GetScope();
        _factory = _scope.GetRequiredService<ITrainingPlanFactory>();
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        _scope.Dispose();
    }

    [TestMethod]
    public async Task IntervalRow_RefreshesWhenASiblingsSlideChangesItsMinutes()
    {
        var plan = await _factory.Create();
        var warmUp = await plan.AddInterval("Warm-up", 5);
        var main = await plan.AddInterval("Main", 5);
        var mainRow = _context.Render<IntervalRow>(p => p.Add(c => c.Interval, main));
        Assert.IsTrue(mainRow.Markup.Contains("30 min"), "60 shared by two equal efforts");

        // A gesture on the other row: the fit writes this row's interval.
        await mainRow.InvokeAsync(() => warmUp.SlideMinutes(20));

        mainRow.WaitForAssertion(() => Assert.IsTrue(mainRow.Markup.Contains("40 min"),
            "the row re-rendered for its own interval's change; nothing redrew the page"));
    }

    [TestMethod]
    public async Task IntervalRow_FollowsTheIntervalItIsGiven()
    {
        var plan = await _factory.Create();
        var warmUp = await plan.AddInterval("Warm-up", 5);
        var main = await plan.AddInterval("Main", 5);
        var coolDown = await plan.AddInterval("Cool-down", 5);
        await warmUp.SetMinutesLocked(true);   // 20/20/20, and warm-up is held from here on
        var row = _context.Render<IntervalRow>(p => p.Add(c => c.Interval, warmUp));

        row.Render(p => p.Add(c => c.Interval, main));
        await row.InvokeAsync(() => coolDown.SlideMinutes(30));   // changes Main only: 20/10/30

        row.WaitForAssertion(() => Assert.IsTrue(row.Markup.Contains("10 min"),
            "the row moved its subscription to Main; warm-up, which it showed first, did not change"));
    }

    [TestMethod]
    public async Task PlanSummary_RefreshesForThePlansOwnFacts()
    {
        var plan = await _factory.Create();
        await plan.AddInterval("Warm-up", 5);
        var summary = _context.Render<PlanSummary>(p => p.Add(c => c.Plan, plan));
        Assert.IsTrue(summary.Markup.Contains("Planned 60 of 60 minutes"));

        await summary.InvokeAsync(() => plan.SetApprovedMinutes(90));

        summary.WaitForAssertion(() => Assert.IsTrue(summary.Markup.Contains("Planned 90 of 90 minutes"),
            "the summary re-rendered for the facts the rules wrote on the plan"));
    }

    [TestMethod]
    public async Task PlanSummary_ApproveButtonFollowsCanApprove()
    {
        var plan = await _factory.Create();
        await plan.AddInterval("Warm-up", 5);
        var summary = _context.Render<PlanSummary>(p => p.Add(c => c.Plan, plan));
        Assert.IsFalse(summary.Find("button").HasAttribute("disabled"));

        await summary.InvokeAsync(() => plan.Approve("ada"));

        summary.WaitForAssertion(() => Assert.IsTrue(summary.Find("button").HasAttribute("disabled"),
            "CanApprove went false and the button followed"));
    }
}
