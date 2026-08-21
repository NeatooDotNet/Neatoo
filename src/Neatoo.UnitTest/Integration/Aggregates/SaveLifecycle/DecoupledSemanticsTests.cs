using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.RemoteFactory;
using Neatoo.UnitTest.TestInfrastructure;

namespace Neatoo.UnitTest.Integration.Aggregates.SaveLifecycle;

/// <summary>
/// The states the IsNew/IsModified split makes reachable (ISNEW-004).
/// </summary>
/// <remarks>
/// Before the split, IsNew was a term in IsModified, so "new but untouched" was
/// unreachable rather than merely unset. These tests pin what that unlocks:
/// a guard bound to IsModified stays quiet on a fresh create, an opt-in create
/// can still declare itself dirty, and savability no longer depends on
/// IsModified lying.
/// </remarks>
[TestClass]
public class DecoupledSemanticsTests : ClientServerTestBase
{
    private IInvoiceFactory _factory = null!;
    private IInvoiceLineFactory _lineFactory = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        SaveLifecycleStore.Reset();
        InitializeScopes();
        _factory = GetClientService<IInvoiceFactory>();
        _lineFactory = GetClientService<IInvoiceLineFactory>();
    }

    /// <summary>
    /// The motivating scenario: an unsaved-changes guard bound to IsModified.
    /// </summary>
    [TestMethod]
    public async Task UnsavedChangesGuard_QuietOnFreshCreate_SpeaksAfterFirstEdit()
    {
        // A guard written the obvious way, which before the split could never
        // stay quiet on a created object
        static bool WouldLoseWork(IInvoice inv) => inv.IsModified;

        var invoice = _factory.CreateForCustomer("Guarded Co");
        await invoice.WaitForTasks();

        Assert.IsFalse(WouldLoseWork(invoice),
            "Navigating away from an untouched new invoice loses nothing");

        // The user actually edits something
        invoice.Customer = "Guarded Co (edited)";
        await invoice.WaitForTasks();

        Assert.IsTrue(WouldLoseWork(invoice),
            "Now there is work to lose, and the guard says so");
    }

    /// <summary>
    /// The guard must also stay quiet after a save re-derives the object -
    /// the regression that surfaced this whole arc.
    /// </summary>
    [TestMethod]
    public async Task UnsavedChangesGuard_QuietImmediatelyAfterSave()
    {
        var invoice = _factory.CreateForCustomer("Post Save Co");
        await invoice.WaitForTasks();

        invoice = (IInvoice)await invoice.Save();

        Assert.IsFalse(invoice.IsModified,
            "A just-saved aggregate holds no unsaved work");
        Assert.IsFalse(invoice.IsSavable,
            "...and offers nothing further to save");
    }

    /// <summary>
    /// Opt-in: a [Create] whose result IS the user's work declares it.
    /// </summary>
    [TestMethod]
    public async Task CreateThatIsUserWork_OptsIntoModified_AndItSurvivesTheWire()
    {
        var invoice = _factory.CreateAsUserWork("Opt In Co");
        await invoice.WaitForTasks();

        Assert.IsTrue(invoice.IsNew);
        Assert.IsTrue(invoice.IsModified,
            "MarkModified() in the [Create] body declares this create IS user work");
        Assert.IsTrue(invoice.IsSavable);

        // The opt-in rides IsMarkedModified, which is serialized - so it holds
        // across a remote round trip rather than being a client-only flag
        var remoteBefore = RemoteCallCount;
        var fetched = await _factory.FetchOptInProbe("Probe Co");
        Assert.IsTrue(RemoteCallCount > remoteBefore, "Probe crossed the wire");
        Assert.IsTrue(fetched.IsModified,
            "A server-side MarkModified() survives serialization to the client");
    }

    /// <summary>
    /// Savability no longer needs IsModified to lie, but it still refuses the
    /// things it always refused - and reports the accurate reason.
    /// </summary>
    [TestMethod]
    public async Task SaveGuard_ReportsAccurateReason_ForNewButInvalid()
    {
        // New, but invalid: Customer is [Required] and left null
        var invoice = _factory.Create();
        await invoice.WaitForTasks();
        await invoice.RunRules();

        Assert.IsTrue(invoice.IsNew);
        Assert.IsFalse(invoice.IsValid);
        Assert.IsFalse(invoice.IsSavable, "IsNew does not override invalidity");

        var ex = await Assert.ThrowsExactlyAsync<SaveOperationException>(() => invoice.Save());
        Assert.AreEqual(SaveFailureReason.IsInvalid, ex.Reason,
            "The reason must be the real one - not NotModified");
    }

    /// <summary>
    /// Attach-marking must be reversible, not sticky: adding a child and then
    /// removing it returns the parent to clean.
    /// </summary>
    [TestMethod]
    public async Task AttachThenRemoveNewChild_ReturnsParentToClean()
    {
        var invoiceId = SaveLifecycleStore.SeedInvoice("Reversible Co", ("Consulting", 500.00m));
        var invoice = await _factory.Fetch(invoiceId);
        Assert.IsFalse(invoice.IsModified, "Precondition: clean fetched aggregate");

        // Attach dirties the graph...
        var added = _lineFactory.Create("Transient", 1.00m);
        invoice.Lines!.Add(added);
        await invoice.WaitForTasks();
        Assert.IsTrue(invoice.IsModified, "The attach dirties the parent");

        // ...and removing it again cleans it back up. A never-persisted child is
        // discarded rather than queued, so nothing is left to save.
        invoice.Lines.Remove(added);
        await invoice.WaitForTasks();

        Assert.AreEqual(0, invoice.Lines.DeletedCount, "Nothing queued for deletion");
        Assert.IsFalse(invoice.IsModified,
            "Attach-marking is reversible - the graph is back to its baseline");
        Assert.IsFalse(invoice.IsSavable, "Nothing left to save");
    }

    /// <summary>
    /// Lazy-loading a child is a load, not user work.
    /// </summary>
    [TestMethod]
    public async Task LazyLoadingAChild_DoesNotDirtyTheParent()
    {
        var invoiceId = SaveLifecycleStore.SeedInvoice("Lazy Co", ("Consulting", 500.00m));
        var invoice = await _factory.Fetch(invoiceId);

        // Touching the graph the way a UI would when rendering it
        var count = invoice.Lines!.Count;
        var firstAmount = invoice.Lines[0].Amount;

        Assert.AreEqual(1, count);
        Assert.AreEqual(500.00m, firstAmount);
        Assert.IsFalse(invoice.IsModified, "Reading a graph does not dirty it");
        Assert.IsFalse(invoice.IsSavable);
    }

    /// <summary>
    /// A created-then-deleted root: IsDeleted still routes, and the generated
    /// factory short-circuits rather than deleting a row that never existed.
    /// </summary>
    [TestMethod]
    public async Task CreatedThenDeleted_IsStillSavable_AndDeletesNothing()
    {
        var invoice = _factory.CreateForCustomer("Doomed Co");
        await invoice.WaitForTasks();

        invoice.Delete();

        Assert.IsTrue(invoice.IsNew, "Never persisted");
        Assert.IsTrue(invoice.IsDeleted);
        Assert.IsTrue(invoice.IsModified, "IsDeleted remains a term in IsModified");

        // Nothing was ever written, so nothing should be deleted
        Assert.AreEqual(0, SaveLifecycleStore.Invoices.Count);
    }
}
