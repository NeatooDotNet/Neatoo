// -----------------------------------------------------------------------------
// Design.Tests - Aggregate Boundary Enforcement (ISNEW-006)
// -----------------------------------------------------------------------------
// OrderItemList.cs and AddressList.cs both document at length that moving a
// child between aggregates throws. Nothing exercised it, which is how the
// exception message came to render both aggregates with the same type name
// ("belongs to aggregate 'Order' ... but this list belongs to aggregate
// 'Order'") - unhelpful enough to read as a framework bug.
// -----------------------------------------------------------------------------

using Design.Domain.Aggregates.OrderAggregate;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Design.Tests.AggregateTests;

[TestClass]
public class AggregateBoundaryTests
{
    private IServiceScope _scope = null!;
    private IOrderFactory _orderFactory = null!;
    private IOrderItemFactory _itemFactory = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _scope = DesignTestServices.GetScope();
        _orderFactory = _scope.GetRequiredService<IOrderFactory>();
        _itemFactory = _scope.GetRequiredService<IOrderItemFactory>();
    }

    [TestCleanup]
    public void TestCleanup() => _scope.Dispose();

    [TestMethod]
    public async Task AddItemFromAnotherAggregate_Throws_WithDistinguishingMessage()
    {
        // Arrange - two separate Order aggregates, each with fetched children
        var order1 = await _orderFactory.Fetch(1);
        var order2 = await _orderFactory.Fetch(2);
        var itemFromOrder1 = order1.Items![0];

        Assert.AreNotSame(order1, order2);
        Assert.AreSame(order1, itemFromOrder1.Root, "Item's Root is its own aggregate");

        // Act & Assert - the boundary is enforced
        var ex = Assert.ThrowsExactly<InvalidOperationException>(
            () => order2.Items!.Add(itemFromOrder1));

        // The message must distinguish the two aggregates. Both are Orders, so
        // naming types alone would say "'Order' ... 'Order'" and read as a bug.
        // "Order" as a literal: the concrete type is internal (interface-first),
        // so the test cannot reference it - which is itself the pattern working
        StringAssert.Contains(ex.Message, "different");
        StringAssert.Contains(ex.Message, "Order");
        Assert.IsFalse(
            ex.Message.Contains("belongs to aggregate 'Order', but this list belongs to aggregate 'Order'"),
            "The message must not render both aggregates identically");
    }

    [TestMethod]
    public void AddItemWithNoAggregate_Succeeds()
    {
        // A freshly created item has no Root yet, so it can join any aggregate
        var order = _orderFactory.Create();
        var item = _itemFactory.Create("Widget", 1, 10.00m);
        Assert.IsNull(item.Root, "A created item is not part of an aggregate yet");

        order.Items!.Add(item);

        Assert.AreEqual(1, order.Items.Count);
        Assert.AreSame(order, item.Root, "Adding establishes the aggregate");
    }

    [TestMethod]
    public async Task CopyAndRemove_IsTheSupportedWayToMoveBetweenAggregates()
    {
        // The pattern OrderItemList.cs documents as RIGHT: copy the data into a
        // new child of the target aggregate, remove the original from the source
        var order1 = await _orderFactory.Fetch(1);
        var order2 = await _orderFactory.Fetch(2);
        var original = order1.Items![0];

        var copy = _itemFactory.Create(original.ProductName!, original.Quantity, original.UnitPrice);
        order2.Items!.Add(copy);
        order1.Items.Remove(original);

        Assert.AreSame(order2, copy.Root);
        Assert.AreEqual(1, order1.Items.DeletedCount, "The original is queued for deletion");
        Assert.IsTrue(order1.IsModified);
        Assert.IsTrue(order2.IsModified);
    }
}
