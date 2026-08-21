using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.RemoteFactory;
using Neatoo.UnitTest.TestInfrastructure;

namespace Neatoo.UnitTest.Integration.Aggregates.SaveLifecycle;

/// <summary>
/// End-to-end aggregate save lifecycle across the client/server boundary (ISNEW-002).
/// </summary>
/// <remarks>
/// The client calls Save() on the root; the operation executes server-side and
/// the resulting graph is serialized back. Assertions run against the CLIENT's
/// copy - the state that actually crossed the wire - which is what application
/// code binds to.
///
/// These assertions pin CURRENT (pre-flip) semantics. ISNEW-004 updates the
/// specific values the IsNew/IsModified split changes; the tests are shaped so
/// that is an assertion edit, not a restructure.
/// </remarks>
[TestClass]
public class AggregateSaveLifecycleTests : ClientServerTestBase
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

    // =========================================================================
    // Create -> Insert
    // =========================================================================

    [TestMethod]
    public async Task CreateWithLines_Save_InsertsGraph_ClientCopyIsOldAndClean()
    {
        // Arrange
        var invoice = _factory.Create();
        invoice.Customer = "Acme";
        invoice.Lines!.Add(_lineFactory.Create("Widget", 10.00m));
        invoice.Lines.Add(_lineFactory.Create("Gadget", 25.00m));
        await invoice.WaitForTasks();

        // Assert - pre-save state (pre-flip: a new aggregate reports modified
        // via the IsNew weld; ISNEW-004 changes IsModified here to false)
        Assert.IsTrue(invoice.IsNew, "Created aggregate is new before save");
        Assert.IsTrue(invoice.IsModified, "Pre-flip: a created aggregate reports modified");
        Assert.IsTrue(invoice.IsSavable, "Created aggregate with valid data is savable");

        var preSave = invoice;
        var preSaveLine = invoice.Lines[0];

        var remoteCallsBefore = RemoteCallCount;

        // Act
        invoice = (IInvoice)await invoice.Save();

        // Assert - the save genuinely crossed the wire, proven two ways: the
        // harness recorded a remote call, and the client is holding deserialized
        // instances rather than the objects it sent
        Assert.IsTrue(RemoteCallCount > remoteCallsBefore,
            "Save must execute remotely - the harness recorded no remote call");
        Assert.AreNotSame(preSave, invoice,
            "Save must round-trip through serialization - the client's post-save root " +
            "should be a different instance than the one it sent");
        Assert.AreNotSame(preSaveLine, invoice.Lines![0],
            "Child instances must also be the deserialized copies");

        // Assert - persistence actually happened, exactly once per object
        Assert.AreEqual(1, SaveLifecycleStore.InsertedInvoiceIds.Count);
        Assert.AreEqual(2, SaveLifecycleStore.InsertedLineIds.Count);
        Assert.AreEqual(0, SaveLifecycleStore.UpdatedLineIds.Count);

        // Assert - generated identity survived the round trip to the client copy
        Assert.AreEqual(SaveLifecycleStore.InsertedInvoiceIds[0], invoice.Id,
            "Generated invoice Id must be present on the client's copy");
        Assert.AreEqual(2, invoice.Lines!.Count);
        CollectionAssert.AreEquivalent(SaveLifecycleStore.InsertedLineIds,
            invoice.Lines.Select(l => l.Id).ToList(),
            "Generated line Ids must be present on the client's copies");

        // Assert - client-side graph is old and clean after the round trip
        Assert.IsFalse(invoice.IsNew, "Saved invoice should be old on the client");
        Assert.IsFalse(invoice.IsModified, "Saved invoice should be clean on the client");
        foreach (var line in invoice.Lines)
        {
            Assert.IsFalse(line.IsNew, $"Line {line.Id} should be old on the client");
            Assert.IsFalse(line.IsModified, $"Line {line.Id} should be clean on the client");
        }
    }

    // =========================================================================
    // Fetch -> modify/add/remove -> Update
    // =========================================================================

    [TestMethod]
    public async Task FetchModifyAddRemove_Save_RoutesEachPathOnce_ClientCopyIsClean()
    {
        // Arrange - two persisted lines
        var invoiceId = SaveLifecycleStore.SeedInvoice("Initech",
            ("Consulting", 500.00m),
            ("Support", 250.00m));

        var invoice = await _factory.Fetch(invoiceId);
        Assert.AreEqual(2, invoice.Lines!.Count, "Precondition: two fetched lines");
        foreach (var line in invoice.Lines)
        {
            Assert.IsFalse(line.IsNew, "Precondition: fetched lines are old");
            Assert.IsFalse(line.IsModified, "Precondition: fetched lines are clean");
        }
        Assert.IsFalse(invoice.IsModified, "Precondition: fetched aggregate is clean");

        var modified = invoice.Lines[0];
        var removed = invoice.Lines[1];

        // Act - modify one line, remove one line, add one line, then save
        modified.Amount = 600.00m;
        invoice.Lines.Remove(removed);
        var added = _lineFactory.Create("Training", 100.00m);
        invoice.Lines.Add(added);
        await invoice.WaitForTasks();

        Assert.IsTrue(invoice.IsSavable, "Modified aggregate should be savable");

        var preSave = invoice;
        var remoteCallsBefore = RemoteCallCount;
        invoice = (IInvoice)await invoice.Save();

        // Assert - the save crossed the wire
        Assert.IsTrue(RemoteCallCount > remoteCallsBefore,
            "Save must execute remotely - the harness recorded no remote call");
        Assert.AreNotSame(preSave, invoice,
            "Save must round-trip through serialization");

        // Assert - each persistence path fired exactly once
        Assert.AreEqual(0, SaveLifecycleStore.UpdatedInvoiceIds.Count,
            "Invoice header untouched - UpdateInvoice must be skipped (IsSelfModified guard)");
        CollectionAssert.AreEqual(new[] { modified.Id }, SaveLifecycleStore.UpdatedLineIds,
            "Only the modified existing line routes to UpdateLine");
        Assert.AreEqual(1, SaveLifecycleStore.InsertedLineIds.Count,
            "Only the added line routes to InsertLine");
        CollectionAssert.AreEqual(new[] { removed.Id }, SaveLifecycleStore.DeletedLineIds,
            "Only the removed line routes to DeleteLine");

        // Assert - the inserted line's generated Id reached the client copy
        // (a lost writeback would route the next save to UpdateLine(0))
        CollectionAssert.Contains(invoice.Lines!.Select(l => l.Id).ToList(),
            SaveLifecycleStore.InsertedLineIds[0],
            "The added line's generated Id must be present on the client copy");

        // Assert - the store reflects the intended end state
        Assert.AreEqual(2, SaveLifecycleStore.GetLines(invoiceId).Count());
        Assert.AreEqual(600.00m, SaveLifecycleStore.Lines[modified.Id].Amount);

        // Assert - client-side graph is clean, DeletedList cleared
        Assert.AreEqual(2, invoice.Lines!.Count);
        Assert.AreEqual(0, invoice.Lines.DeletedCount,
            "DeletedList should be cleared by the list's FactoryComplete(Update)");
        Assert.IsFalse(invoice.IsModified, "Saved invoice should be clean on the client");
        foreach (var line in invoice.Lines)
        {
            Assert.IsFalse(line.IsNew, $"Line {line.Id} should be old on the client");
            Assert.IsFalse(line.IsModified, $"Line {line.Id} should be clean on the client");
        }
    }

    [TestMethod]
    public async Task FetchModifyRoot_Save_UpdatesHeader()
    {
        // Arrange
        var invoiceId = SaveLifecycleStore.SeedInvoice("Initech", ("Consulting", 500.00m));
        var invoice = await _factory.Fetch(invoiceId);

        // Act - change the root's own property only
        invoice.Customer = "Initech Global";
        await invoice.WaitForTasks();
        invoice = (IInvoice)await invoice.Save();

        // Assert - header written, no child operations
        CollectionAssert.AreEqual(new[] { invoiceId }, SaveLifecycleStore.UpdatedInvoiceIds);
        Assert.AreEqual("Initech Global", SaveLifecycleStore.GetInvoice(invoiceId).Customer);
        Assert.AreEqual(0, SaveLifecycleStore.UpdatedLineIds.Count,
            "Unmodified children must not be routed to UpdateLine");
        Assert.AreEqual(0, SaveLifecycleStore.InsertedLineIds.Count);
        Assert.IsFalse(invoice.IsModified);
    }

    // =========================================================================
    // Save guards (pre-flip anchors for ISNEW-004)
    // =========================================================================

    [TestMethod]
    public async Task FetchedUnmodified_Save_ThrowsNotModified()
    {
        // Arrange
        var invoiceId = SaveLifecycleStore.SeedInvoice("Initech", ("Consulting", 500.00m));
        var invoice = await _factory.Fetch(invoiceId);
        await invoice.WaitForTasks();

        Assert.IsFalse(invoice.IsModified, "Fetched aggregate is not modified");
        Assert.IsFalse(invoice.IsSavable, "Unmodified fetched aggregate is not savable");

        // Act & Assert
        var ex = await Assert.ThrowsExactlyAsync<SaveOperationException>(() => invoice.Save());
        Assert.AreEqual(SaveFailureReason.NotModified, ex.Reason);
        Assert.AreEqual(0, SaveLifecycleStore.UpdatedInvoiceIds.Count, "Nothing persisted");
    }

    [TestMethod]
    public async Task RichCreate_Untouched_IsSavableFromIsNewAlone_AndSaveInserts()
    {
        // Arrange - a fully populated create: everything set INSIDE the paused
        // factory operation, so there is no property dirt whatsoever. This is
        // what makes the test meaningful: savability here can only come from
        // the IsNew term, never from a setter this test called.
        var invoice = _factory.CreateForCustomer("Rich Create Co");
        await invoice.WaitForTasks();

        // Assert - the aggregate is valid and completely clean of user work...
        Assert.IsTrue(invoice.IsValid, "Rich create produces a valid aggregate");
        Assert.IsFalse(invoice.IsSelfModified,
            "Factory-op writes are paused - no property dirt on the root");
        Assert.AreEqual(2, invoice.Lines!.Count, "Factory-populated children");

        // ...yet it is savable. This is the motivating case for the whole ISNEW
        // arc: a rich Create that fully populates an aggregate lands with nothing
        // for an unsaved-changes guard to complain about, while still being
        // savable so the insert can happen.
        Assert.IsTrue(invoice.IsNew);
        Assert.IsFalse(invoice.IsModified,
            "A richly-created aggregate holds no user work - guards must stay quiet");
        Assert.IsTrue(invoice.IsSavable,
            "...yet it is savable, carried by the IsNew term rather than by dirt");

        // The child list too: factory-built children are baseline population, not
        // user work, so the list is clean. (Pre-flip this read true, because each
        // child's IsNew was welded into its IsModified.)
        Assert.IsFalse(invoice.Lines.IsModified,
            "Factory-populated children are not user work");

        // Act
        invoice = (IInvoice)await invoice.Save();

        // Assert - it inserted the root and its factory-built children
        Assert.AreEqual(1, SaveLifecycleStore.InsertedInvoiceIds.Count);
        Assert.AreEqual(2, SaveLifecycleStore.InsertedLineIds.Count,
            "Factory-populated children must be inserted too");
        Assert.IsFalse(invoice.IsNew);
        Assert.IsFalse(invoice.IsModified);
    }

    [TestMethod]
    public async Task FetchedRoot_AddOneNewChild_IsModifiedAndSavable_AndChildInserts()
    {
        // Arrange - a clean fetched aggregate; the ONLY change is one attached
        // child. Nothing else dirties the graph, so this isolates the channel
        // design.md calls mandatory: a user-attached new child must make the
        // parent modified and savable. Post-flip that channel is attach-marking
        // rather than the IsNew weld - if ISNEW-004 changes IsModified but
        // misses the InsertItem exemption, this test is what fails.
        var invoiceId = SaveLifecycleStore.SeedInvoice("Initech", ("Consulting", 500.00m));
        var invoice = await _factory.Fetch(invoiceId);
        Assert.IsFalse(invoice.IsModified, "Precondition: fetched aggregate is clean");
        Assert.IsFalse(invoice.IsSavable, "Precondition: clean aggregate is not savable");

        // Act - attach one new child and nothing else
        var added = _lineFactory.Create("Added only", 42.00m);
        invoice.Lines!.Add(added);
        await invoice.WaitForTasks();

        // Assert - the attach alone dirties the parent
        Assert.IsTrue(invoice.IsModified,
            "A user-attached new child must make the parent modified");
        Assert.IsTrue(invoice.IsSavable,
            "A user-attached new child must enable Save on the parent");

        invoice = (IInvoice)await invoice.Save();

        // Assert - the child's insert was not skipped
        Assert.AreEqual(1, SaveLifecycleStore.InsertedLineIds.Count,
            "The attached child must be inserted exactly once");
        Assert.AreEqual(0, SaveLifecycleStore.UpdatedLineIds.Count,
            "The untouched existing child must not be written");
        Assert.AreEqual(2, invoice.Lines!.Count);
        Assert.IsFalse(invoice.IsModified, "Graph is clean after save");
    }

    [TestMethod]
    public async Task FetchedRoot_RemoveOneChild_IsModifiedAndSavable_AndChildDeletes()
    {
        // Arrange - removal is the only change, isolating the DeletedList
        // channel (the list cache path ISNEW-003/004 touch)
        var invoiceId = SaveLifecycleStore.SeedInvoice("Initech",
            ("Consulting", 500.00m),
            ("Support", 250.00m));
        var invoice = await _factory.Fetch(invoiceId);
        Assert.IsFalse(invoice.IsModified, "Precondition: fetched aggregate is clean");

        // Act - remove one persisted child and nothing else
        var removed = invoice.Lines![1];
        invoice.Lines.Remove(removed);
        await invoice.WaitForTasks();

        Assert.IsTrue(invoice.IsModified,
            "A removed persisted child must make the parent modified");
        Assert.IsTrue(invoice.IsSavable);

        invoice = (IInvoice)await invoice.Save();

        // Assert
        CollectionAssert.AreEqual(new[] { removed.Id }, SaveLifecycleStore.DeletedLineIds);
        Assert.AreEqual(0, SaveLifecycleStore.UpdatedLineIds.Count,
            "The surviving untouched child must not be written");
        Assert.AreEqual(1, invoice.Lines!.Count);
        Assert.AreEqual(0, invoice.Lines.DeletedCount, "DeletedList cleared after save");
        Assert.IsFalse(invoice.IsModified);
    }

    [TestMethod]
    public async Task SavedAggregate_SecondSave_ThrowsNotModified()
    {
        // Arrange - save once so the client holds a freshly persisted graph
        var invoice = _factory.CreateForCustomer("Save Twice Co");
        await invoice.WaitForTasks();
        invoice = (IInvoice)await invoice.Save();

        // Assert - the post-save client copy is genuinely clean and old
        Assert.IsFalse(invoice.IsNew, "MarkOld must have crossed the wire");
        Assert.IsFalse(invoice.IsModified);
        Assert.IsFalse(invoice.IsSavable, "A saved, unchanged aggregate is not savable");

        // Act & Assert - a second save is refused rather than duplicating rows
        var ex = await Assert.ThrowsExactlyAsync<SaveOperationException>(() => invoice.Save());
        Assert.AreEqual(SaveFailureReason.NotModified, ex.Reason);
        Assert.AreEqual(1, SaveLifecycleStore.InsertedInvoiceIds.Count,
            "A stale IsNew would have re-inserted a duplicate");
    }

    [TestMethod]
    public async Task RemoveNeverPersistedLine_Save_IssuesNoDelete()
    {
        // Arrange - fetched aggregate plus a line that only ever existed in memory
        var invoiceId = SaveLifecycleStore.SeedInvoice("Initech", ("Consulting", 500.00m));
        var invoice = await _factory.Fetch(invoiceId);

        var transient = _lineFactory.Create("Never saved", 1.00m);
        invoice.Lines!.Add(transient);
        await invoice.WaitForTasks();

        // Act - remove it again, then make a real change and save
        invoice.Lines.Remove(transient);
        Assert.AreEqual(0, invoice.Lines.DeletedCount,
            "A never-persisted line is discarded, not queued for deletion");

        invoice.Customer = "Initech Global";
        await invoice.WaitForTasks();
        invoice = (IInvoice)await invoice.Save();

        // Assert - no delete was issued for the transient line
        Assert.AreEqual(0, SaveLifecycleStore.DeletedLineIds.Count,
            "Removing a never-persisted line must not issue a delete");
        Assert.AreEqual(0, SaveLifecycleStore.InsertedLineIds.Count,
            "The discarded line must not be inserted either");
        Assert.AreEqual(1, invoice.Lines!.Count);
    }
}
