// -----------------------------------------------------------------------------
// Design.Tests - Aggregate Lifecycle Tests (ISNEW-001)
// -----------------------------------------------------------------------------
// Pins the canonical aggregate factory lifecycle end to end: children fetched
// through their own [Fetch] land old and clean; saving routes children through
// per-item factory saves so the whole graph is clean afterward; removed items
// are deleted from persistence exactly once.
// -----------------------------------------------------------------------------

using Design.Domain.Aggregates.OrderAggregate;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Design.Tests.AggregateTests;

[TestClass]
public class AggregateLifecycleTests
{
    private IServiceScope _scope = null!;
    private IOrderFactory _orderFactory = null!;
    private IOrderItemFactory _itemFactory = null!;
    private MockOrderRepository _repository = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _scope = DesignTestServices.GetScope();
        _orderFactory = _scope.GetRequiredService<IOrderFactory>();
        _itemFactory = _scope.GetRequiredService<IOrderItemFactory>();
        // Scoped registration: same instance the factory operations receive
        _repository = (MockOrderRepository)_scope.GetRequiredService<IOrderRepository>();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _scope.Dispose();
    }

    // =========================================================================
    // Fetch lifecycle
    // =========================================================================

    [TestMethod]
    public async Task Fetch_ItemsAreOldAndClean_AggregateNotModified()
    {
        // Act
        var order = await _orderFactory.Fetch(1);

        // Assert - root state
        Assert.IsFalse(order.IsNew, "Fetched order should not be new");
        Assert.IsFalse(order.IsModified, "Fetched order should not be modified");

        // Assert - child state: each item completed its own [Fetch]
        Assert.AreEqual(2, order.Items!.Count);
        foreach (var item in order.Items)
        {
            Assert.IsFalse(item.IsNew, $"Fetched item {item.Id} should not be new");
            Assert.IsFalse(item.IsModified, $"Fetched item {item.Id} should not be modified");
        }

        Assert.AreEqual(0, order.Items.DeletedCount, "No deletions pending after fetch");
    }

    [TestMethod]
    public async Task Fetch_LoadsTheChildrenOfTheRequestedOrder_NotSomeOtherOrders()
    {
        // Arrange - until LIST-005, MockOrderRepository.GetItems ignored its orderId and
        // returned the same two rows for every order. That made this assertion
        // inexpressible: an OrderItemList that fetched the WRONG order's items - or
        // ignored the id entirely - passed every test in this suite.
        _repository.ItemsByOrderId[41] = new[] { (410, "Order-41 widget", 1, 5.00m, 5.00m) };
        _repository.ItemsByOrderId[42] = new[]
        {
            (420, "Order-42 gadget", 2, 7.00m, 14.00m),
            (421, "Order-42 gizmo", 3, 9.00m, 27.00m),
        };

        // Act
        var order41 = await _orderFactory.Fetch(41);
        var order42 = await _orderFactory.Fetch(42);

        // Assert - each order got its own children, keyed by the id it was asked for
        CollectionAssert.AreEquivalent(
            new[] { 410 },
            order41.Items!.Select(i => i.Id).ToArray(),
            "Order 41 must load only order 41's items");
        CollectionAssert.AreEquivalent(
            new[] { 420, 421 },
            order42.Items!.Select(i => i.Id).ToArray(),
            "Order 42 must load only order 42's items");
    }

    // =========================================================================
    // Update path: fetch -> modify -> add -> save
    // =========================================================================

    [TestMethod]
    public async Task FetchModifyAddItem_Save_GraphIsCleanAndRoutingCorrect()
    {
        // Arrange
        var order = await _orderFactory.Fetch(1);
        var existingItemId = order.Items![0].Id;

        // Modify an existing item (LineTotal rule -> TotalAmount rule -> root self-modified)
        order.Items[0].Quantity = 3;

        // Add a new item
        var newItem = _itemFactory.Create("Added", 1, 5.00m);
        order.Items.Add(newItem);

        await order.WaitForTasks();
        Assert.IsTrue(order.IsSavable, "Modified aggregate should be savable");

        // Act
        order = (IOrder)await order.Save();

        // Assert - repository routing: ONLY the modified existing item updated
        // (the untouched existing item is skipped by the IsNew/IsModified guard),
        // ONLY the new item inserted
        CollectionAssert.AreEqual(new[] { existingItemId }, _repository.UpdatedItemIds,
            "Exactly the modified existing item should be routed to UpdateItem");
        Assert.AreEqual(1, _repository.InsertedItemIds.Count,
            "Newly added item should be routed to InsertItem exactly once");
        Assert.AreEqual(0, _repository.DeletedItemIds.Count, "Nothing was removed");

        // Assert - generated-Id writeback on the inserted child
        Assert.AreEqual(_repository.InsertedItemIds[0], newItem.Id,
            "Generated child Id must land on the entity (a later Update would otherwise target Id 0)");

        // Assert - whole graph clean after save (per-item factory completions)
        Assert.AreEqual(3, order.Items!.Count, "Two fetched + one added item");
        Assert.IsFalse(order.IsModified, "Order should not be modified after save");
        foreach (var item in order.Items)
        {
            Assert.IsFalse(item.IsNew, $"Item {item.Id} should be old after save");
            Assert.IsFalse(item.IsModified, $"Item {item.Id} should be clean after save");
        }
    }

    // =========================================================================
    // Insert path: create -> add items -> save
    // =========================================================================

    [TestMethod]
    public async Task CreateWithItems_Save_InsertsAllAndGraphIsClean()
    {
        // Arrange
        var order = _orderFactory.Create();
        order.CustomerName = "Test Customer";
        order.Items!.Add(_itemFactory.Create("Widget", 2, 10.00m));
        order.Items.Add(_itemFactory.Create("Gadget", 1, 50.00m));

        await order.WaitForTasks();
        Assert.IsTrue(order.IsNew, "Created order is new");
        Assert.IsTrue(order.IsSavable, "Valid new aggregate should be savable");

        // Act
        order = (IOrder)await order.Save();

        // Assert - routing: order inserted once, both items inserted
        Assert.AreEqual(1, _repository.InsertedOrderIds.Count);
        Assert.AreEqual(2, _repository.InsertedItemIds.Count,
            "Every item of a new aggregate should be routed to InsertItem");
        Assert.AreEqual(0, _repository.UpdatedItemIds.Count);

        // Assert - generated-Id writeback on root and children
        Assert.AreEqual(_repository.InsertedOrderIds[0], order.Id,
            "Generated order Id must land on the root");
        Assert.AreEqual(2, order.Items!.Count);
        CollectionAssert.AreEquivalent(_repository.InsertedItemIds, order.Items.Select(i => i.Id).ToList(),
            "Generated child Ids must land on the entities");

        // Assert - whole graph old and clean after save
        Assert.IsFalse(order.IsNew, "Order should be old after insert");
        Assert.IsFalse(order.IsModified, "Order should be clean after insert");
        foreach (var item in order.Items)
        {
            Assert.IsFalse(item.IsNew, $"Item {item.Id} should be old after insert");
            Assert.IsFalse(item.IsModified, $"Item {item.Id} should be clean after insert");
        }
    }

    // =========================================================================
    // Delete flow: removed items are deleted from persistence exactly once
    // =========================================================================

    [TestMethod]
    public async Task RemoveExistingItem_Save_DeletesFromRepositoryExactlyOnce()
    {
        // Arrange
        var order = await _orderFactory.Fetch(1);
        var removed = order.Items![0];
        var removedId = removed.Id;

        order.Items.Remove(removed);
        Assert.AreEqual(1, order.Items.DeletedCount, "Removed existing item goes to DeletedList");

        await order.WaitForTasks();

        // Act - first save processes the deletion
        order = (IOrder)await order.Save();

        // Assert
        CollectionAssert.AreEqual(new[] { removedId }, _repository.DeletedItemIds,
            "Removed item should be deleted from the repository");
        Assert.AreEqual(0, order.Items!.DeletedCount,
            "DeletedList should be cleared by the list's FactoryComplete(Update)");
        Assert.IsFalse(order.IsModified, "Order should be clean after save");

        // Arrange - modify the surviving item and save again
        var survivingId = order.Items[0].Id;
        order.Items[0].Quantity = 7;
        await order.WaitForTasks();

        // Act
        order = (IOrder)await order.Save();

        // Assert - no re-delete on subsequent saves, and the child delegation
        // still runs (surviving item routed to UpdateItem this time)
        Assert.AreEqual(1, _repository.DeletedItemIds.Count,
            "A processed deletion must not be re-issued on the next save");
        CollectionAssert.AreEqual(new[] { survivingId }, _repository.UpdatedItemIds,
            "The modified surviving item should be routed to UpdateItem on the second save");
    }
}
