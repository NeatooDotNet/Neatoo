// -----------------------------------------------------------------------------
// Design.Tests - Aggregate Lifecycle Tests (ISNEW-001)
// -----------------------------------------------------------------------------
// Pins the canonical aggregate factory lifecycle end to end: children fetched
// through their own [Fetch] land old and clean; saving routes children through
// per-item factory saves so the whole graph is clean afterward; removed items
// have their rows removed exactly once.
//
// Assertions read the rows the aggregate left in MockOrderRepository's
// in-memory store (see TestInfrastructure.cs).
// -----------------------------------------------------------------------------

using Design.Domain.Aggregates.OrderAggregate;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Design.Tests.AggregateTests;

[TestClass]
public class AggregateLifecycleTests
{
    // Written into a stored row after the fetch. A save that writes that row
    // overwrites it, so finding it afterward proves the row was not written.
    private const string NotWritten = "(not written by save)";

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

    private async Task<IOrder> FetchOrder(Guid id)
    {
        var order = await _orderFactory.Fetch(id);
        Assert.IsNotNull(order, "A seeded order should be found");
        return order;
    }

    // =========================================================================
    // Fetch lifecycle
    // =========================================================================

    [TestMethod]
    public async Task Fetch_ItemsAreOldAndClean_AggregateNotModified()
    {
        // Arrange
        var seeded = _repository.SeedOrder();

        // Act
        var order = await FetchOrder(seeded.Id);

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
    public async Task Fetch_UnknownId_ReturnsNull()
    {
        // Order.Fetch returns false when the repository has no row, and the
        // generated factory turns that into null
        var order = await _orderFactory.Fetch(Guid.NewGuid());

        Assert.IsNull(order, "Fetching an order that does not exist returns null");
    }

    [TestMethod]
    public async Task Fetch_LoadsTheChildrenOfTheRequestedOrder_NotSomeOtherOrders()
    {
        // Arrange - until LIST-005, the mock returned the same two child rows for
        // every order. That made this assertion inexpressible: an OrderItemList
        // that loaded the WRONG order's items - or ignored the order entirely -
        // passed every test in this suite.
        var row41 = _repository.SeedOrder(
            MockOrderRepository.Item("Order-41 widget", 1, 5.00m, 5.00m));
        var row42 = _repository.SeedOrder(
            MockOrderRepository.Item("Order-42 gadget", 2, 7.00m, 14.00m),
            MockOrderRepository.Item("Order-42 gizmo", 3, 9.00m, 27.00m));
        var expected41 = row41.Items.Select(r => r.Id).ToArray();
        var expected42 = row42.Items.Select(r => r.Id).ToArray();

        // Act
        var order41 = await FetchOrder(row41.Id);
        var order42 = await FetchOrder(row42.Id);

        // Assert - each order got its own children, keyed by the id it was asked for
        CollectionAssert.AreEquivalent(
            expected41,
            order41.Items!.Select(i => i.Id).ToArray(),
            "Order 41 must load only order 41's items");
        CollectionAssert.AreEquivalent(
            expected42,
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
        var seeded = _repository.SeedOrder();
        var order = await FetchOrder(seeded.Id);
        var existingItemId = order.Items![0].Id;
        var untouchedItemId = order.Items[1].Id;

        // Mark the untouched item's stored row - writing it would overwrite the mark
        seeded.Items.Single(r => r.Id == untouchedItemId).ProductName = NotWritten;

        // Modify an existing item (LineTotal rule -> TotalAmount rule -> root self-modified)
        order.Items[0].Quantity = 3;

        // Add a new item
        var newItem = _itemFactory.Create("Added", 1, 5.00m);
        order.Items.Add(newItem);

        await order.WaitForTasks();
        Assert.IsTrue(order.IsSavable, "Modified aggregate should be savable");

        // Act
        order = (IOrder)await order.Save();

        // Assert - routing: ONLY the modified existing item is written to its
        // row (the untouched existing item is skipped by the IsNew/IsModified
        // guard), ONLY the new item gets a new row, nothing is removed
        var stored = _repository.Store[seeded.Id];
        Assert.AreEqual(3, stored.Items.Single(r => r.Id == existingItemId).Quantity,
            "The modified existing item should be written to its row");
        Assert.AreEqual(NotWritten, stored.Items.Single(r => r.Id == untouchedItemId).ProductName,
            "The untouched existing item must not be written");
        var newRows = stored.Items
            .Where(r => r.Id != existingItemId && r.Id != untouchedItemId)
            .ToList();
        Assert.AreEqual(1, newRows.Count, "The newly added item should get exactly one new row");
        Assert.AreEqual(3, stored.Items.Count, "Nothing was removed");
        Assert.AreEqual(1, _repository.SaveChangesCount, "One flush for the whole aggregate");

        // Assert - the key the new child set in its [Insert] landed on the
        // entity and on its row (a later Update would otherwise not find the row)
        Assert.AreNotEqual(Guid.Empty, newItem.Id, "The child's key must land on the entity");
        Assert.AreEqual(newRows[0].Id, newItem.Id, "The entity and its row must share the key");
        Assert.AreEqual("Added", newRows[0].ProductName, "The new item maps itself into its row");

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
    public async Task InvalidOrder_DirectFactorySave_ServerRulesRefuseBeforeWriting()
    {
        // Arrange: CustomerName is [Required], but rules do not run during
        // [Create], so the new order still reports valid on this side.
        var order = _orderFactory.Create();
        Assert.IsTrue(order.IsValid, "No rule has run yet, so nothing is broken");

        // Act: a direct factory.Save does not check IsSavable, so only the
        // re-run of the rules inside [Insert] stands between this order and
        // the write.
        var ex = await Assert.ThrowsExactlyAsync<Neatoo.SaveOperationException>(
            () => _orderFactory.Save(order));

        // Assert: refused, and nothing was written
        Assert.AreEqual(Neatoo.SaveFailureReason.IsInvalid, ex.Reason);
        Assert.AreEqual(0, _repository.AddedRows.Count, "No row may be added");
        Assert.AreEqual(0, _repository.SaveChangesCount, "No flush may happen");
    }

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

        // Assert - routing: order row added once, one flush
        Assert.AreEqual(1, _repository.AddedRows.Count, "The order row should be added exactly once");
        Assert.AreEqual(1, _repository.SaveChangesCount, "One flush for the whole aggregate");

        // Assert - the key the order set in its [Insert] landed on the root,
        // and the flushed store holds the order row under it
        Assert.AreNotEqual(Guid.Empty, order.Id, "The order's key must land on the root");
        Assert.IsTrue(_repository.Store.TryGetValue(order.Id, out var stored),
            "The store should hold the order row under the root's key");
        Assert.AreSame(_repository.AddedRows[0], stored);

        // Assert - every item got a new row through its [Insert] - the only
        // place an item sets its key - so each key is set, distinct, and on
        // both the entity and its row
        Assert.AreEqual(2, order.Items!.Count);
        Assert.AreEqual(2, stored.Items.Count, "Every item of a new aggregate should get a new row");
        var rowIds = stored.Items.Select(r => r.Id).ToList();
        CollectionAssert.DoesNotContain(rowIds, Guid.Empty, "Every item should have set its key in [Insert]");
        CollectionAssert.AllItemsAreUnique(rowIds);
        CollectionAssert.AreEquivalent(rowIds, order.Items.Select(i => i.Id).ToList(),
            "The items' keys must land on the entities");
        CollectionAssert.AreEquivalent(new[] { "Widget", "Gadget" },
            stored.Items.Select(r => r.ProductName).ToList(),
            "Each item maps itself into its row");

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
    // Delete flow: removed items have their rows removed exactly once
    // =========================================================================

    [TestMethod]
    public async Task RemoveExistingItem_Save_RemovesItsRowExactlyOnce()
    {
        // Arrange
        var seeded = _repository.SeedOrder();
        var order = await FetchOrder(seeded.Id);
        var removed = order.Items![0];
        var removedId = removed.Id;

        order.Items.Remove(removed);
        Assert.AreEqual(1, order.Items.DeletedCount, "Removed existing item goes to DeletedList");

        await order.WaitForTasks();

        // Act - first save processes the deletion
        order = (IOrder)await order.Save();

        // Assert
        var stored = _repository.Store[seeded.Id];
        CollectionAssert.DoesNotContain(stored.Items.Select(r => r.Id).ToList(), removedId,
            "The removed item's row should be removed");
        Assert.AreEqual(1, stored.Items.Count, "Only the removed item's row should go");
        Assert.AreEqual(0, order.Items!.DeletedCount,
            "DeletedList should be cleared by the list's FactoryComplete(Update)");
        Assert.IsFalse(order.IsModified, "Order should be clean after save");

        // Arrange - modify the surviving item and save again
        var survivingId = order.Items[0].Id;
        order.Items[0].Quantity = 7;
        await order.WaitForTasks();

        // Act - a re-issued deletion would look for the removed row again, and
        // OrderItemList.Update would throw on not finding it
        order = (IOrder)await order.Save();

        // Assert - no re-delete on subsequent saves, and the child delegation
        // still runs (surviving item written to its row this time)
        Assert.AreEqual(1, stored.Items.Count,
            "A processed deletion must not be re-issued on the next save");
        Assert.AreEqual(7, stored.Items.Single(r => r.Id == survivingId).Quantity,
            "The modified surviving item should be written to its row on the second save");
    }
}
