using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.UnitTest.TestInfrastructure;

namespace Neatoo.UnitTest.Integration.Aggregates.LocalSaveLifecycle;

/// <summary>
/// End-to-end lifecycle tests over a LOCAL (non-remote) aggregate.
/// </summary>
/// <remarks>
/// Exists because every other save-lifecycle fixture is [Remote], and a remote save
/// hides meta-state defects: the returned graph is deserialized and OnDeserialized
/// rebuilds the baselines. LIST-003's stale baseline was invisible to the entire
/// suite for exactly that reason.
/// </remarks>
[TestClass]
public class LocalSaveLifecycleTests : IntegrationTestBase
{
    private ILocalOrderFactory _orderFactory = null!;
    private ILocalOrderLineFactory _lineFactory = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        LocalSaveStore.Reset();
        InitializeScope();
        _orderFactory = GetRequiredService<ILocalOrderFactory>();
        _lineFactory = GetRequiredService<ILocalOrderLineFactory>();
    }

    [TestMethod]
    public async Task LocalSaveCarryingDeletions_ThenChildEdit_MakesAggregateSavableAgain()
    {
        // Arrange - THE USER-VISIBLE SYMPTOM OF LIST-003, at aggregate level.
        //
        // The unit tests prove the LIST announces IsModified again after a save that
        // carried deletions. This proves the announcement actually travels: that the
        // PARENT hears it and becomes savable, which is what a real consumer binds to.
        // Without the fix the list's value self-heals but nothing is raised, so the
        // parent's cached state never refreshes and the aggregate reports not-savable
        // while holding real unsaved work.
        var order = _orderFactory.Fetch(7, "Contoso");
        Assert.IsFalse(order.IsModified, "Precondition: a fetched aggregate is clean");
        Assert.IsFalse(order.IsSavable, "Precondition: nothing to save");
        Assert.AreEqual(2, order.Lines!.Count);

        // The user deletes a line, then saves locally.
        var doomed = order.Lines!.First();
        order.Lines!.Remove(doomed);
        Assert.IsTrue(order.IsModified, "The queued deletion dirties the aggregate");

        var saved = (ILocalOrder)await order.Save();
        Assert.IsFalse(saved.IsModified, "The save cleared the pending deletion");
        Assert.IsFalse(saved.IsSavable, "Nothing left to save right after the save");
        Assert.AreEqual(0, saved.Lines!.DeletedCount, "The DeletedList was drained by the save");
        CollectionAssert.Contains(LocalSaveStore.DeletedLineIds, doomed.Id, "The deletion was persisted");

        // Act - the user edits a surviving line
        saved.Lines!.First().Description = "Edited after the save";

        // Assert - the aggregate must know it has work again
        Assert.IsTrue(saved.Lines!.IsModified, "The list holds an edited child");
        Assert.IsTrue(saved.IsModified, "The edit must reach the aggregate root");
        Assert.IsTrue(saved.IsSavable, "An aggregate with unsaved work must be savable");
    }

    [TestMethod]
    public async Task ReplacingAPersistedChild_ThenSave_IssuesTheDelete()
    {
        // Arrange - LIST-002 end to end. Before it, `list[i] = replacement` dropped the
        // displaced child with no MarkDeleted and no DeletedList entry, so its row was
        // silently orphaned: the save issued an INSERT or UPDATE for the replacement and
        // nothing at all for the row it displaced.
        var order = _orderFactory.Fetch(9, "Northwind");
        var displaced = order.Lines!.First();
        var displacedId = displaced.Id;

        // Act - replace it, then save locally
        var replacement = _lineFactory.Create("Replacement line");
        order.Lines![0] = replacement;

        Assert.IsTrue(order.IsModified, "The replacement is unsaved work");
        Assert.IsTrue(order.IsSavable);

        var saved = (ILocalOrder)await order.Save();

        // Assert - the displaced row was actually deleted
        CollectionAssert.Contains(
            LocalSaveStore.DeletedLineIds,
            displacedId,
            "The displaced persisted child's row must be DELETEd, not orphaned");
        Assert.AreEqual(0, saved.Lines!.DeletedCount, "The save drained the queue");
        Assert.IsFalse(saved.IsModified, "Nothing left pending after the save");
    }

    [TestMethod]
    public async Task LocalSaveWithoutDeletions_ThenChildEdit_MakesAggregateSavableAgain()
    {
        // Arrange - the CONTROL. As close to the test above as possible while carrying
        // NO deletion, so the pair isolates the stale baseline rather than save
        // handling in general. This one passed before LIST-003 as well.
        //
        // The save still has to carry *something*: Save() on an unmodified aggregate
        // throws SaveOperationException(NotModified) by design, so the control edits a
        // child first. That keeps the only difference from the defect case the thing
        // under test - whether the save carried a queued deletion.
        var order = _orderFactory.Fetch(8, "Fabrikam");
        order.Lines!.First().Description = "Edited before the save";
        Assert.IsTrue(order.IsModified, "Precondition: the save carries a child edit, not a deletion");
        Assert.AreEqual(0, order.Lines!.DeletedCount, "Precondition: nothing queued for deletion");

        var saved = (ILocalOrder)await order.Save();
        Assert.IsFalse(saved.IsSavable, "Precondition: the save left nothing to do");

        // Act
        saved.Lines!.First().Description = "Edited after a clean save";

        // Assert
        Assert.IsTrue(saved.IsModified);
        Assert.IsTrue(saved.IsSavable);
    }
}
