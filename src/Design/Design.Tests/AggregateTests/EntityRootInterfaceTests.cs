// -----------------------------------------------------------------------------
// Design.Tests - IEntityRoot vs IEntityBase Interface Tests
// -----------------------------------------------------------------------------
// Tests verifying that the IEntityRoot/IEntityBase interface distinction
// correctly separates root and child entity capabilities.
// -----------------------------------------------------------------------------

using Design.Domain.Aggregates.OrderAggregate;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo;

namespace Design.Tests.AggregateTests;

[TestClass]
public class EntityRootInterfaceTests
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
    public void RootInterface_ExposesIsSavable()
    {
        // Arrange — IOrder extends IEntityRoot, so IsSavable is accessible
        IOrder order = _orderFactory.Create();
        order.CustomerName = "Test";

        // Act & Assert — IsSavable is available on IEntityRoot
        IEntityRoot root = order;
        Assert.IsTrue(root.IsSavable, "Root entity should be savable when modified and valid");
    }

    [TestMethod]
    public void ChildInterface_DoesNotExposeIsSavable()
    {
        // Arrange — IOrderItem extends IEntityBase, not IEntityRoot
        var order = _orderFactory.Create();
        var item = _itemFactory.Create("Widget", 5, 10.00m);
        order.Items!.Add(item);

        // Act — Cast to IEntityBase (which IOrderItem extends)
        // Intentionally using interface type to demonstrate the pattern
#pragma warning disable CA1859
        IEntityBase entityBase = item;
#pragma warning restore CA1859

        // Assert — IEntityBase does NOT have IsSavable
        // This is verified by the fact that the following would NOT compile:
        //   entityBase.IsSavable  // CS1061: IEntityBase does not contain IsSavable
        //   entityBase.Save()     // CS1061: IEntityBase does not contain Save
        Assert.IsTrue(entityBase.IsChild, "Child entity should be a child");
        Assert.IsTrue(entityBase.IsModified, "Child entity should be modified");
    }

    [TestMethod]
    public void RootInterface_ExposessSave()
    {
        // Arrange — IEntityRoot exposes Save()
        IOrder order = _orderFactory.Create();
        order.CustomerName = "Test";

        // Act — Save is accessible through IEntityRoot
        IEntityRoot root = order;

        // Assert — Save method exists on IEntityRoot
        // We just verify it's accessible, not that it succeeds (no persistence configured)
        Assert.IsNotNull((Func<Task<IEntityBase>>)root.Save,
            "Save() should be accessible on IEntityRoot");
    }

    [TestMethod]
    public void OrderImplementsIEntityRoot()
    {
        // Arrange & Act
        var order = _orderFactory.Create();

        // Assert — Order is assignable to IEntityRoot
        Assert.IsInstanceOfType<IEntityRoot>(order,
            "Order (aggregate root) should implement IEntityRoot");
    }

    [TestMethod]
    public void OrderItemImplementsIEntityBase_NotIEntityRoot()
    {
        // Arrange & Act
        var item = _itemFactory.Create();

        // Assert — OrderItem implements IEntityBase
        Assert.IsInstanceOfType<IEntityBase>(item,
            "OrderItem should implement IEntityBase");

        // The interface IOrderItem : IEntityBase does NOT extend IEntityRoot
        // So when working with IOrderItem, IsSavable and Save() are not available
    }
}
