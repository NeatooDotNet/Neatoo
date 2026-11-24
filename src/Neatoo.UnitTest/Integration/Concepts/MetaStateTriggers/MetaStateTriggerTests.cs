using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.UnitTest.TestInfrastructure;

namespace Neatoo.UnitTest.Integration.Concepts.MetaStateTriggers;

/// <summary>
/// Rules may trigger on meta state: IsValid, IsModified, IsBusy.
/// </summary>
[TestClass]
public class MetaStateTriggerTests : IntegrationTestBase
{
    private IMetaGateFactory factory = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        InitializeScope();
        factory = GetRequiredService<IMetaGateFactory>();
    }

    [TestMethod]
    public async Task IsValidTrigger_RunsOncePerFlip_AndMaintainsTheAdmissionFact()
    {
        var gate = await factory.Create();
        Assert.IsTrue(gate.IsValid);
        Assert.IsTrue(gate.CanApprove, "the operation's rules computed the admission fact");
        var runs = gate.CanApproveRuns;

        await gate.SetName(null);
        await gate.WaitForTasks();

        Assert.IsFalse(gate.IsValid);
        Assert.IsFalse(gate.CanApprove, "validity went false, so the IsValid-triggered rule withdrew admission");
        Assert.AreEqual(runs + 1, gate.CanApproveRuns, "one flip, one run");

        await gate.SetName("Gate again");
        await gate.WaitForTasks();

        Assert.IsTrue(gate.CanApprove);
        Assert.AreEqual(runs + 2, gate.CanApproveRuns, "the flip back is the second run");
    }

    [TestMethod]
    public async Task IsModifiedTrigger_RunsWhenTheEntityFirstBecomesModified()
    {
        var gate = await factory.Create();
        Assert.IsFalse(gate.IsModified, "an operation's work is baseline");
        Assert.IsFalse(gate.HasUnsavedWork, "Create's RunRules computed the fact from the baseline");
        var runs = gate.ModifiedRuns;

        await gate.SetStatus("Draft");
        await gate.SetStatus("Pending");
        await gate.WaitForTasks();

        Assert.IsTrue(gate.HasUnsavedWork);
        Assert.AreEqual(runs + 1, gate.ModifiedRuns, "IsModified flipped once; the second edit and the rule's own write did not flip it");
    }

    [TestMethod]
    public async Task MetaTriggers_DoNotRunWhilePaused()
    {
        var gate = await factory.CreateInvalid(true);

        Assert.IsFalse(gate.IsValid, "validity flipped inside the operation");
        Assert.AreEqual(0, gate.CanApproveRuns, "no meta rule runs inside a pause, nor after the operation ends");
        Assert.IsFalse(gate.CanApprove);
    }

    [TestMethod]
    public async Task AsyncMetaRuleOnAChild_IsTracked_SoTheParentIsBusyUntilItFinishes()
    {
        var parent = GetRequiredService<IMetaParentFactory>().Create();
        var child = parent.Child!;

        await child.SetCode(null);

        Assert.IsTrue(parent.IsBusy, "the child's async IsValid rule is still running and the parent tracks it");
        await parent.WaitForTasks();
        Assert.IsFalse(parent.IsBusy);
        Assert.AreEqual(false, child.CheckedValid, "the async meta rule finished inside the parent's WaitForTasks");
    }

    [TestMethod]
    public async Task AsyncMetaRuleStartedWhenTheObjectGoesIdle_IsCoveredByWaitForTasks()
    {
        var root = GetRequiredService<IMetaSavableFactory>().Create();

        await root.SetLookup("x");
        await root.WaitForTasks();

        Assert.IsFalse(root.IsBusy, "nothing is left running");
        Assert.IsTrue(root.Settled, "the idle change ran the sync IsBusy rule");
        Assert.AreEqual("settled after x", root.SettledNote, "the async rule its write started finished inside WaitForTasks");
    }

    [TestMethod]
    public async Task OtherEntityMetaProperties_TriggerRules()
    {
        var parent = GetRequiredService<IMetaParentFactory>().Create();
        var runs = parent.SavableRuns;

        Assert.IsTrue(parent.IsSavable, "a new, valid root is savable");

        await parent.Child!.SetCode(null);
        await parent.WaitForTasks();

        Assert.IsFalse(parent.IsSavable, "the child went invalid, so the root is not savable");
        Assert.IsTrue(parent.SavableRuns > runs, "the IsSavable flip ran its rule");
    }

    [TestMethod]
    public async Task SynchronouslyFaultingMetaRule_SurfacesFromTheWrite()
    {
        var fault = GetRequiredService<IMetaFaultFactory>().Create();

        var thrown = await Assert.ThrowsAsync<Exception>(async () => await fault.SetName(null));

        StringAssert.Contains(thrown.ToString(), "the meta rule failed");
    }

    [TestMethod]
    public async Task SyncIsBusyTrigger_RunsOncePerFlip_AndSettles()
    {
        var gate = await factory.Create();
        var runs = gate.BusyRuns;

        await gate.SetLookup("x");
        await gate.WaitForTasks();

        Assert.AreEqual("found x", gate.LookupResult);
        Assert.IsTrue(gate.WasEverBusy, "the rule saw the object busy");
        Assert.IsFalse(gate.IsBusy, "the object settled");
        Assert.AreEqual(runs + 2, gate.BusyRuns, "busy, then idle: two flips, two runs");
    }
}
