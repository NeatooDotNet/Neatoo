// -----------------------------------------------------------------------------
// Design.Tests - Order Aggregate Tests
// -----------------------------------------------------------------------------
// Tests demonstrating aggregate root patterns including parent-child
// relationships, child entity management, and aggregate boundaries.
// -----------------------------------------------------------------------------

using Design.Domain.Aggregates.OrderAggregate;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Design.Tests.AggregateTests;

[TestClass]
public class OrderAggregateTests
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
    public void Create_InitializesWithEmptyItemsList()
    {
        // Arrange & Act
        var order = _orderFactory.Create();

        // Assert
        Assert.IsNotNull(order.Items, "Items list should be initialized");
        Assert.AreEqual(0, order.Items.Count, "Items list should be empty");
    }

    [TestMethod]
    public void Create_SetsDefaultValues()
    {
        // Arrange & Act
        var order = _orderFactory.Create();

        // Assert
        Assert.IsNotNull(order.OrderNumber, "OrderNumber should be set");
        Assert.AreEqual("Draft", order.Status);
        Assert.AreEqual(DateTime.Today, order.OrderDate);
    }

    [TestMethod]
    public void AddItem_ItemBecomesChild()
    {
        // Arrange
        var order = _orderFactory.Create();
        var item = _itemFactory.Create("Widget", 5, 10.00m);

        // Act
        order.Items!.Add(item);

        // Assert
        Assert.IsTrue(item.IsChild, "Added item should have IsChild=true");
        Assert.AreEqual(1, order.Items.Count);
    }

    [TestMethod]
    public void Item_CalculatesLineTotal_OnPropertyChange()
    {
        // Arrange - Create item with initial values
        // Note: Factory operations run with PauseAllActions, so rules don't fire during Create.
        var item = _itemFactory.Create("Widget", 5, 10.00m);

        // Act - Change a trigger property to a NEW value to fire the rule
        // Setting the same value might not trigger if Neatoo optimizes no-change sets
        item.Quantity = 6;

        // Assert - LineTotal = 6 * 10 = 60
        Assert.AreEqual(60.00m, item.LineTotal, "LineTotal should be Quantity * UnitPrice");
    }

    [TestMethod]
    public void ItemQuantityChange_RecalculatesLineTotal()
    {
        // Arrange - Create item and trigger initial calculation by changing Quantity
        var item = _itemFactory.Create("Widget", 5, 10.00m);
        item.Quantity = 5; // Force trigger even if same value (or try a different approach)

        // If same-value set doesn't trigger, change to different value first
        item.Quantity = 1; // Change to trigger rule: 1 * 10 = 10
        Assert.AreEqual(10.00m, item.LineTotal, "Initial LineTotal should be 10 after change");

        // Act - Change quantity to target value
        item.Quantity = 10;

        // Assert
        Assert.AreEqual(100.00m, item.LineTotal, "LineTotal should update to 100 after quantity change");
    }

    [TestMethod]
    public void ChildItem_CannotSaveIndependently()
    {
        // Arrange
        var order = _orderFactory.Create();
        order.CustomerName = "Test Customer";
        var item = _itemFactory.Create("Widget", 5, 10.00m);
        order.Items!.Add(item);

        // Assert — IOrderItem extends IEntityBase, which does NOT have IsSavable or Save().
        // This is the interface-first pattern at work: the interface controls access.
        // The concrete OrderItem has IsSavable (inherited from EntityBase), but consumers
        // only see IOrderItem, so IsSavable is invisible.
        Assert.IsTrue(item.IsChild);
        // item.IsSavable — does not compile: IOrderItem (IEntityBase) has no IsSavable
        // item.Save()    — does not compile: IOrderItem (IEntityBase) has no Save()
    }

    [TestMethod]
    public void Order_IsValidWhenRequiredFieldsSet()
    {
        // Arrange
        var order = _orderFactory.Create();

        // Act
        order.CustomerName = "Test Customer";
        // Note: OrderNumber is set by Create

        // Assert - Draft orders are valid without items
        Assert.IsTrue(order.IsValid, "Draft order with customer name should be valid");
    }

    [TestMethod]
    public async Task Fetch_LoadsOrder()
    {
        // Arrange & Act
        var order = await _orderFactory.Fetch(1);

        // Assert
        Assert.IsNotNull(order);
        Assert.IsNotNull(order.Items);
        Assert.AreEqual(2, order.Items.Count, "Should load 2 items from mock repository");
    }

    [TestMethod]
    public async Task ChildItemLineTotalChange_RecalculatesOrderTotalAmount()
    {
        // Arrange - Create order with an item
        var order = _orderFactory.Create();
        order.CustomerName = "Test Customer";
        var item = _itemFactory.Create("Widget", 1, 10.00m);
        order.Items!.Add(item);

        // Act - Change child item quantity, which triggers LineTotal recalculation
        item.Quantity = 5;
        await order.WaitForTasks();

        // Assert - Order.TotalAmount should reflect the child's updated LineTotal (5 * 10 = 50)
        Assert.AreEqual(50.00m, item.LineTotal, "Child LineTotal should be 50");
        Assert.AreEqual(50.00m, order.TotalAmount, "Order TotalAmount should recalculate when child LineTotal changes");
    }
}
