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

        // Assert - routing
        Assert.AreEqual(1, _repository.InsertedParentIds.Count);
        Assert.AreEqual(1, _repository.InsertedChildIds.Count);
        Assert.AreEqual(0, _repository.UpdatedChildIds.Count);

        // Assert - generated-Id writeback (root and child)
        Assert.AreEqual(_repository.InsertedParentIds[0], demo.Id,
            "Generated parent Id must land on the root");
        Assert.AreEqual(1, demo.Items!.Count);
        Assert.AreEqual(_repository.InsertedChildIds[0], demo.Items[0].Id,
            "Generated child Id must land on the child (Update would otherwise target Id 0)");

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
        var demo = await _factory.Fetch(1);
        Assert.AreEqual(2, demo.Items!.Count, "Precondition: two fetched children");
        foreach (var child in demo.Items)
        {
            Assert.IsFalse(child.IsNew, $"Fetched child {child.Id} should not be new");
            Assert.IsFalse(child.IsModified, $"Fetched child {child.Id} should not be modified");
        }
        Assert.IsFalse(demo.IsModified, "Fetched aggregate should not be modified");

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
        Assert.AreEqual(0, _repository.UpdatedParentIds.Count,
            "Root header untouched - UpdateParent must be skipped (IsSelfModified guard)");
        CollectionAssert.AreEqual(new[] { modified.Id }, _repository.UpdatedChildIds,
            "Only the modified existing child routes to UpdateChild");
        Assert.AreEqual(1, _repository.InsertedChildIds.Count,
            "Only the added child routes to InsertChild");
        CollectionAssert.AreEqual(new[] { removed.Id }, _repository.DeletedChildIds,
            "Only the removed child routes to DeleteChild");

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
