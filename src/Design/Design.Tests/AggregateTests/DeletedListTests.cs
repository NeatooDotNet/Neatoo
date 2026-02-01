// -----------------------------------------------------------------------------
// Design.Tests - DeletedList Tests
// -----------------------------------------------------------------------------
// Tests demonstrating DeletedList behavior in EntityListBase including
// item removal, new item handling, and modification tracking.
//
// NOTE: These tests focus on new item behavior since the Design.Domain
// Order entity does not expose methods to mark items as Old. For full
// DeletedList testing with existing items, see Neatoo.UnitTest which uses
// test entities that expose MarkOld/MarkUnmodified.
// -----------------------------------------------------------------------------

using Design.Domain.Aggregates.OrderAggregate;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Design.Tests.AggregateTests;

[TestClass]
public class DeletedListTests
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
    public void TestCleanup()
    {
        _scope.Dispose();
    }

    [TestMethod]
    public void RemoveNewItem_NotAddedToDeletedList()
    {
        // Arrange
        var order = _orderFactory.Create();
        var item = _itemFactory.Create("Widget", 1, 10.00m);
        order.Items!.Add(item);
        Assert.IsTrue(item.IsNew, "Created item should be new");

        // Act
        order.Items.Remove(item);

        // Assert
        Assert.AreEqual(0, order.Items.Count);
        Assert.AreEqual(0, order.Items.DeletedCount, "New items should not go to DeletedList");
    }

    [TestMethod]
    public void RemoveNewItem_ItemIsDiscarded()
    {
        // Arrange
        var order = _orderFactory.Create();
        var item = _itemFactory.Create("Widget", 1, 10.00m);
        order.Items!.Add(item);

        // Act
        order.Items.Remove(item);

        // Assert - New item is simply removed, no persistence delete needed
        Assert.AreEqual(0, order.Items.Count);
        Assert.AreEqual(0, order.Items.DeletedCount);
    }

    [TestMethod]
    public void AddMultipleNewItems_RemoveAll_NoDeletedListEntries()
    {
        // Arrange
        var order = _orderFactory.Create();
        var item1 = _itemFactory.Create("Widget1", 1, 10.00m);
        var item2 = _itemFactory.Create("Widget2", 2, 20.00m);
        order.Items!.Add(item1);
        order.Items.Add(item2);

        // Act
        order.Items.Clear();

        // Assert - All items are new, so none go to DeletedList
        Assert.AreEqual(0, order.Items.Count);
        Assert.AreEqual(0, order.Items.DeletedCount);
    }

    [TestMethod]
    public void RemoveAt_NewItem_NotAddedToDeletedList()
    {
        // Arrange
        var order = _orderFactory.Create();
        var item1 = _itemFactory.Create("Widget1", 1, 10.00m);
        var item2 = _itemFactory.Create("Widget2", 2, 20.00m);
        order.Items!.Add(item1);
        order.Items.Add(item2);

        // Act
        order.Items.RemoveAt(0);

        // Assert
        Assert.AreEqual(1, order.Items.Count);
        Assert.AreEqual(0, order.Items.DeletedCount, "New items should not go to DeletedList");
    }

    [TestMethod]
    public void IsModified_TrueWhenNewItemAdded()
    {
        // Arrange
        var order = _orderFactory.Create();

        // Act
        var item = _itemFactory.Create("Widget", 1, 10.00m);
        order.Items!.Add(item);

        // Assert - New order with new items is modified
        Assert.IsTrue(order.Items.IsModified);
        Assert.IsTrue(order.IsModified);
    }

    [TestMethod]
    public void IsModified_TrueWhenNewItemRemoved()
    {
        // Arrange
        var order = _orderFactory.Create();
        var item = _itemFactory.Create("Widget", 1, 10.00m);
        order.Items!.Add(item);

        // Act
        order.Items.Remove(item);

        // Assert - Order is still modified (it's new)
        Assert.IsTrue(order.IsModified, "New order is always modified");
    }
}
