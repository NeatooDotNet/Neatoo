// -----------------------------------------------------------------------------
// Design.Tests - SaveAggregateDemo Lifecycle Tests (ISNEW-001)
// -----------------------------------------------------------------------------
// Executes the SavePatterns.cs aggregate demo end to end. This demo is
// reference code for the canonical aggregate save pattern; the shape it
// replaced was wrong for its whole life precisely because nothing ran it.
// -----------------------------------------------------------------------------

using Design.Domain.FactoryOperations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Design.Tests.FactoryTests;

[TestClass]
public class SaveAggregateLifecycleTests
{
    // Written into a stored row after the fetch. A save that writes that row
    // overwrites it, so finding it afterward proves the row was not written.
    private const string NotWritten = "(not written by save)";

    private IServiceScope _scope = null!;
    private ISaveAggregateDemoFactory _factory = null!;
    private ISaveDemoItemFactory _itemFactory = null!;
    private MockSaveAggregateRepository _repository = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _scope = DesignTestServices.GetScope();
        _factory = _scope.GetRequiredService<ISaveAggregateDemoFactory>();
        _itemFactory = _scope.GetRequiredService<ISaveDemoItemFactory>();
        _repository = (MockSaveAggregateRepository)_scope.GetRequiredService<ISaveAggregateRepository>();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _scope.Dispose();
    }


    [TestMethod]
    public async Task CreateWithItems_Save_InsertsAllWithIdWriteback_AndGraphIsClean()
    {
        // Arrange
        var demo = _factory.Create();
        demo.Title = "New Aggregate";

        var item = _itemFactory.Create();
        item.Name = "Child A";
        item.Quantity = 3;
        demo.Items!.Add(item);

        await demo.WaitForTasks();

        // Act
        demo = (ISaveAggregateDemo)await demo.Save();

        // Assert - routing: root row added once, one flush, one child row
        Assert.AreEqual(1, _repository.AddedRows.Count, "The root row is added exactly once");
        Assert.AreEqual(1, _repository.SaveChangesCount, "One flush for the whole aggregate");
        Assert.IsTrue(_repository.Store.TryGetValue(demo.Id, out var stored),
            "The store holds the root row under the root's key");
        Assert.AreEqual(1, stored.Items.Count, "The new child gets one new row");

        // Assert - key writeback (root and child): each set its key in its own
        // [Insert], and entity and row share it
        Assert.AreNotEqual(Guid.Empty, demo.Id, "The root's key must land on the root");
        Assert.AreEqual(1, demo.Items!.Count);
        Assert.AreNotEqual(Guid.Empty, demo.Items[0].Id,
            "The child's key must land on the child (a later Update would otherwise not find its row)");
        Assert.AreEqual(stored.Items[0].Id, demo.Items[0].Id, "The child and its row must share the key");
        Assert.AreEqual("Child A", stored.Items[0].Name, "The child maps itself into its row");

        // Assert - graph old and clean
        Assert.IsFalse(demo.IsNew);
        Assert.IsFalse(demo.IsModified);
        Assert.IsFalse(demo.Items[0].IsNew);
        Assert.IsFalse(demo.Items[0].IsModified);
    }

    [TestMethod]
    public async Task FetchModifyAddRemove_Save_RoutesAllPathsAndGraphIsClean()
    {
        // Arrange - fetched children come via the list/item [Fetch] chain
        var seeded = _repository.SeedAggregate();
        var demo = await _factory.Fetch(seeded.Id);
        Assert.IsNotNull(demo, "A seeded aggregate should be found");
        Assert.AreEqual(2, demo.Items!.Count, "Precondition: two fetched children");
        foreach (var child in demo.Items)
        {
            Assert.IsFalse(child.IsNew, $"Fetched child {child.Id} should not be new");
            Assert.IsFalse(child.IsModified, $"Fetched child {child.Id} should not be modified");
        }
        Assert.IsFalse(demo.IsModified, "Fetched aggregate should not be modified");

        // Mark the root's stored row - writing it would overwrite the mark
        seeded.Title = NotWritten;

        // Modify one child, remove the other, add a new one
        var modified = demo.Items[0];
        var removed = demo.Items[1];
        modified.Quantity = 99;
        demo.Items.Remove(removed);
        var added = _itemFactory.Create();
        added.Name = "Added";
        added.Quantity = 1;
        demo.Items.Add(added);

        await demo.WaitForTasks();

        // Act
        demo = (ISaveAggregateDemo)await demo.Save();

        // Assert - every path routed exactly once
        var stored = _repository.Store[seeded.Id];
        Assert.AreEqual(NotWritten, stored.Title,
            "Root header untouched - its row must not be written (IsSelfModified guard)");
        Assert.AreEqual(99, stored.Items.Single(r => r.Id == modified.Id).Quantity,
            "The modified existing child is written to its row");
        CollectionAssert.DoesNotContain(stored.Items.Select(r => r.Id).ToList(), removed.Id,
            "The removed child has its row removed");
        var addedRows = stored.Items.Where(r => r.Id != modified.Id).ToList();
        Assert.AreEqual(1, addedRows.Count, "Only the added child gets a new row");
        Assert.AreEqual(added.Id, addedRows[0].Id, "The added child and its row share the key");
        Assert.AreEqual(1, _repository.SaveChangesCount, "One flush for the whole aggregate");

        // Assert - graph clean after save
        Assert.AreEqual(2, demo.Items!.Count);
        Assert.IsFalse(demo.IsModified);
        foreach (var child in demo.Items)
        {
            Assert.IsFalse(child.IsNew, $"Child {child.Id} should be old after save");
            Assert.IsFalse(child.IsModified, $"Child {child.Id} should be clean after save");
        }
    }
}
