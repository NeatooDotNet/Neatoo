using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.UnitTest.TestInfrastructure;

namespace Neatoo.UnitTest.Integration.Aggregates.SaveLifecycle;

/// <summary>
/// List state correctness across factory operations (ISNEW-003).
/// </summary>
/// <remarks>
/// Two defects fixed by that plan, exercised through the canonical fetch path
/// rather than by driving FactoryComplete by hand:
/// 1. Factory completion resumed the list without recalculating cached meta
///    state, so a fetched list reported the state of an empty list.
/// 2. Items added while the list was paused never got child identity, so a
///    fetched child's Delete() silently bypassed list routing.
/// </remarks>
[TestClass]
public class ListFactoryStateTests : ClientServerTestBase
{
    private IInvoiceFactory _factory = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        SaveLifecycleStore.Reset();
        InitializeScopes();
        _factory = GetClientService<IInvoiceFactory>();
    }

    // =========================================================================
    // Defect 1 - cached meta state after a factory operation
    // =========================================================================

    [TestMethod]
    public async Task FetchedInvalidData_OnceRulesRun_AggregateReportsInvalid()
    {
        // Fetched children are loaded without running rules, so they start
        // valid regardless of their data; validity becomes real once rules run,
        // and it must propagate list -> aggregate.
        //
        // Note this does NOT exercise the ISNEW-003 cache-staleness fix: the
        // rule run raises PropertyChanged, which updates the list cache through
        // the normal (unpaused-guarded) handler. The stale window needs an item
        // that is invalid BEFORE the paused add — pinned by
        // EntityListBaseStateTransitionTests
        // .FactoryComplete_AfterPausedAddOfInvalidItem_ListReportsInvalid.
        var invoiceId = SaveLifecycleStore.SeedInvoice("Initech", ("", 500.00m));

        var invoice = await _factory.Fetch(invoiceId);
        await invoice.RunRules();

        Assert.AreEqual(1, invoice.Lines!.Count);
        Assert.IsFalse(invoice.Lines[0].IsValid, "The seeded line violates [Required]");
        Assert.IsFalse(invoice.Lines.IsValid, "The list must reflect its invalid child");
        Assert.IsFalse(invoice.IsValid, "The aggregate must reflect its invalid child");
        Assert.IsFalse(invoice.IsSavable, "An invalid aggregate is not savable");
    }

    [TestMethod]
    public async Task FetchedGraph_IsCompletelyClean()
    {
        // Guards the other side of the fix: recalculating caches on factory
        // completion must not manufacture dirt.
        var invoiceId = SaveLifecycleStore.SeedInvoice("Initech",
            ("Consulting", 500.00m), ("Support", 250.00m));

        var invoice = await _factory.Fetch(invoiceId);

        Assert.IsFalse(invoice.IsModified, "Fetched root is clean");
        Assert.IsFalse(invoice.Lines!.IsModified, "Fetched list is clean");
        Assert.AreEqual(0, invoice.Lines.DeletedCount);
        foreach (var line in invoice.Lines)
        {
            Assert.IsFalse(line.IsModified, $"Fetched line {line.Id} is clean");
            Assert.IsFalse(line.IsNew, $"Fetched line {line.Id} is old");
        }
    }

    // =========================================================================
    // Defect 2 - child identity on the paused add path
    // =========================================================================

    [TestMethod]
    public async Task FetchedChild_IsMarkedAsChild()
    {
        var invoiceId = SaveLifecycleStore.SeedInvoice("Initech", ("Consulting", 500.00m));

        var invoice = await _factory.Fetch(invoiceId);

        Assert.AreEqual(1, invoice.Lines!.Count);
        Assert.IsTrue(invoice.Lines[0].IsChild,
            "A child loaded through the canonical fetch path is a child");
        // Note: this asserts the end state on the client. The mechanism itself -
        // that the PAUSED add applies child identity - is pinned at unit level by
        // EntityListBaseTests.Add_WhenPaused_MarksAsChild.
    }

    [TestMethod]
    public async Task FetchedChild_Delete_RoutesThroughList_AndDeletesOnSave()
    {
        // Arrange - this is the behavior the ISNEW-003 defect silently broke:
        // with no ContainingList, child.Delete() marked the child deleted but
        // never told the list, so the row was never removed at save.
        var invoiceId = SaveLifecycleStore.SeedInvoice("Initech",
            ("Consulting", 500.00m), ("Support", 250.00m));

        var invoice = await _factory.Fetch(invoiceId);
        var target = invoice.Lines![0];
        var targetId = target.Id;

        // Act - delete through the CHILD, not through the list
        target.Delete();
        await invoice.WaitForTasks();

        // Assert - the list took ownership of the deletion
        Assert.AreEqual(1, invoice.Lines.Count, "The child was removed from the list");
        Assert.AreEqual(1, invoice.Lines.DeletedCount, "The child is queued for deletion");
        Assert.IsTrue(invoice.IsModified, "A pending child deletion dirties the aggregate");
        Assert.IsTrue(invoice.IsSavable);

        invoice = (IInvoice)await invoice.Save();

        // Assert - and the row is actually gone
        CollectionAssert.AreEqual(new[] { targetId }, SaveLifecycleStore.DeletedLineIds,
            "child.Delete() must result in a real delete at save");
        Assert.AreEqual(1, SaveLifecycleStore.GetLines(invoiceId).Count());
        Assert.AreEqual(0, invoice.Lines!.DeletedCount, "DeletedList cleared after save");
        Assert.IsFalse(invoice.IsModified);
    }

    [TestMethod]
    public async Task FetchedChild_CrossesTheBoundary_AndKeepsIdentity()
    {
        // The fetched graph on the CLIENT is a deserialized graph. Assert the
        // wire was genuinely used, then that child identity survived it:
        // IsChild rides the wire, ContainingList cannot (not public, not on the
        // interface), so the paused add path is what re-establishes it.
        var invoiceId = SaveLifecycleStore.SeedInvoice("Initech",
            ("Consulting", 500.00m), ("Support", 250.00m));

        var remoteCallsBefore = RemoteCallCount;
        var invoice = await _factory.Fetch(invoiceId);

        Assert.IsTrue(RemoteCallCount > remoteCallsBefore,
            "Fetch must execute remotely - otherwise this asserts nothing about " +
            "deserialization, since both containers share this process");

        Assert.AreEqual(2, invoice.Lines!.Count);
        foreach (var line in invoice.Lines)
        {
            Assert.IsTrue(line.IsChild, $"Deserialized line {line.Id} is still a child");
            Assert.IsFalse(line.IsModified, $"Deserialized line {line.Id} is clean");
        }
        Assert.IsFalse(invoice.IsModified);

        // ContainingList was re-established client-side: Delete() routes
        invoice.Lines[0].Delete();
        Assert.AreEqual(1, invoice.Lines.DeletedCount,
            "Delete() routes through the list on the deserialized client copy");
    }
}
