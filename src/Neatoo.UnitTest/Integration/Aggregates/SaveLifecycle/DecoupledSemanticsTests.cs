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

        // And the opt-in must CLEAR on save - otherwise every consumer following
        // this idiom would end up with a permanently dirty aggregate
        invoice = (IInvoice)await invoice.Save();

        Assert.IsFalse(invoice.IsModified,
            "The opt-in mark is cleared by MarkUnmodified() at factory completion");
        Assert.IsFalse(invoice.IsSavable, "Nothing left to save");
        Assert.AreEqual(1, SaveLifecycleStore.InsertedInvoiceIds.Count);
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

        // Re-attaching dirties it again. This path is newly reachable: dropping
        // the !IsNew exemption means MarkModified() now runs for new items, and a
        // removed item keeps its ContainingList, so the upward notification fires
        // for a state that never triggered it before.
        invoice.Lines.Add(added);
        await invoice.WaitForTasks();

        Assert.IsTrue(invoice.IsModified, "Re-attaching dirties the parent again");
        Assert.IsTrue(invoice.IsSavable);
    }

    /// <summary>
    /// Reading a fetched graph is not user work.
    /// </summary>
    /// <remarks>
    /// Named for what it actually does: <c>Invoice.Lines</c> is a plain property,
    /// not an <c>EntityLazyLoad</c> wrapper, so this covers graph traversal rather
    /// than lazy loading. Genuine lazy-load state propagation is covered by the
    /// pre-existing <c>LazyLoadStatePropagationTests</c>.
    /// </remarks>
    [TestMethod]
    public async Task ReadingAFetchedGraph_DoesNotDirtyIt()
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
    public async Task CreatedThenDeleted_IsSavable_AndSaveDeletesNothing()
    {
        var invoice = _factory.CreateForCustomer("Doomed Co");
        await invoice.WaitForTasks();

        invoice.Delete();

        Assert.IsTrue(invoice.IsNew, "Never persisted");
        Assert.IsTrue(invoice.IsDeleted);
        Assert.IsTrue(invoice.IsModified, "IsDeleted remains a term in IsModified");
        Assert.IsTrue(invoice.IsSavable, "...and it is savable - routing decides what happens");

        // Act - actually save it. The generated routing checks IsDeleted first,
        // then short-circuits on IsNew, because deleting a row that was never
        // written is meaningless. (That short-circuit exists only in a signature
        // group that HAS a [Delete] - see the ISNEW-007 finding.)
        await invoice.Save();

        // Assert - the delete was recognised and skipped, not attempted
        Assert.AreEqual(0, SaveLifecycleStore.DeletedInvoiceIds.Count,
            "A never-persisted invoice must not be deleted from persistence");
        Assert.AreEqual(0, SaveLifecycleStore.InsertedInvoiceIds.Count,
            "...and IsDeleted must win over IsNew, so it is not inserted either");
    }

    [TestMethod]
    public async Task FetchedThenDeleted_Save_RoutesToDelete()
    {
        // The other half: a persisted root marked for deletion really is deleted
        var invoiceId = SaveLifecycleStore.SeedInvoice("Real Co", ("Consulting", 500.00m));
        var invoice = await _factory.Fetch(invoiceId);

        invoice.Delete();
        Assert.IsTrue(invoice.IsModified, "IsDeleted makes it modified");
        Assert.IsTrue(invoice.IsSavable);

        await invoice.Save();

        CollectionAssert.AreEqual(new[] { invoiceId }, SaveLifecycleStore.DeletedInvoiceIds);
        Assert.AreEqual(0, SaveLifecycleStore.UpdatedInvoiceIds.Count,
            "IsDeleted is checked before IsNew and before Update");
    }
}
