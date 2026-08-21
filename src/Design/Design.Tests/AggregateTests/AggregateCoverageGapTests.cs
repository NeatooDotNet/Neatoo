// -----------------------------------------------------------------------------
// Design.Tests - Previously Uncovered Aggregate Behaviors (ISNEW-006)
// -----------------------------------------------------------------------------
// Pre-existing gaps surfaced by the ISNEW-001/007 gates: behaviors the Design
// projects document at length but no test executed. Each of these would have
// stayed green if the behavior were deleted outright.
// -----------------------------------------------------------------------------

using Design.Domain.Aggregates.OrderAggregate;
using Design.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Design.Tests.AggregateTests;

[TestClass]
public class AggregateCoverageGapTests
{
    private IServiceScope _scope = null!;
    private IOrderFactory _orderFactory = null!;
    private IOrderItemFactory _itemFactory = null!;
    private IEmployeeFactory _employeeFactory = null!;
    private IAddressFactory _addressFactory = null!;
    private MockOrderRepository _orderRepo = null!;
    private MockEmployeeRepository _employeeRepo = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _scope = DesignTestServices.GetScope();
        _orderFactory = _scope.GetRequiredService<IOrderFactory>();
        _itemFactory = _scope.GetRequiredService<IOrderItemFactory>();
        _employeeFactory = _scope.GetRequiredService<IEmployeeFactory>();
        _addressFactory = _scope.GetRequiredService<IAddressFactory>();
        _orderRepo = (MockOrderRepository)_scope.GetRequiredService<IOrderRepository>();
        _employeeRepo = (MockEmployeeRepository)_scope.GetRequiredService<IEmployeeRepository>();
    }

    [TestCleanup]
    public void TestCleanup() => _scope.Dispose();

    // =========================================================================
    // The IsSelfModified header guard — positive direction
    // =========================================================================
    // Both Order.Update and Employee.Update write the root row only when the
    // root's OWN properties changed. The skip direction was pinned for free by
    // the lifecycle tests; nothing asserted the write actually happens, so
    // deleting the repository call kept the suite green.

    [TestMethod]
    public async Task OrderUpdate_WhenRootPropertyChanged_WritesHeader()
    {
        var order = await _orderFactory.Fetch(1);
        order.CustomerName = "Renamed Customer";
        await order.WaitForTasks();

        await order.Save();

        CollectionAssert.AreEqual(new[] { 1 }, _orderRepo.UpdatedOrderIds,
            "A changed root property must write the order header");
    }

    [TestMethod]
    public async Task EmployeeUpdate_WhenRootPropertyChanged_WritesHeader()
    {
        var employee = await _employeeFactory.Fetch(1);
        employee.Email = "renamed@example.com";
        await employee.WaitForTasks();

        await employee.Save();

        CollectionAssert.AreEqual(new[] { 1 }, _employeeRepo.UpdatedEmployeeIds,
            "A changed root property must write the employee header");
    }

    // =========================================================================
    // Root delete paths
    // =========================================================================

    [TestMethod]
    public async Task OrderDelete_DeletesChildrenThenRoot()
    {
        var order = await _orderFactory.Fetch(1);
        var itemIds = order.Items!.Select(i => i.Id).ToList();

        order.Delete();
        Assert.IsTrue(order.IsDeleted);
        Assert.IsTrue(order.IsSavable, "A deleted persisted root is savable");

        await order.Save();

        CollectionAssert.AreEquivalent(itemIds, _orderRepo.DeletedItemIds,
            "Children are deleted first (FK constraint)");
    }

    [TestMethod]
    public async Task EmployeeDelete_DeletesPersistedAddressesOnly()
    {
        var employee = await _employeeFactory.Fetch(1);
        var persistedIds = employee.Addresses!.Select(a => a.Id).ToList();

        // A never-persisted address should not produce a delete
        var newAddress = _addressFactory.Create();
        newAddress.Street = "9 Transient Way";
        newAddress.City = "Springfield";
        newAddress.State = "IL";
        newAddress.ZipCode = "62709";
        newAddress.AddressType = "Home";
        employee.Addresses.Add(newAddress);
        await employee.WaitForTasks();

        employee.Delete();
        await employee.Save();

        CollectionAssert.AreEquivalent(persistedIds, _employeeRepo.DeletedAddressIds,
            "Only persisted addresses are deleted - the new one was never written");
    }

    // =========================================================================
    // Validation rules the aggregates document but nothing exercised
    // =========================================================================

    [TestMethod]
    public async Task Order_NonDraftWithNoItems_IsInvalid()
    {
        var order = _orderFactory.Create();
        order.CustomerName = "Test Customer";

        // Draft with no items is fine
        await order.WaitForTasks();
        Assert.IsTrue(order.IsValid, "A Draft order needs no items");

        // Leaving Draft without items is not
        order.Status = "Submitted";
        await order.WaitForTasks();

        Assert.IsFalse(order.IsValid, "A non-Draft order must have at least one item");
        Assert.IsFalse(order.IsSavable, "...and is therefore not savable");
    }

    [TestMethod]
    public async Task Employee_NegativeSalary_IsInvalid()
    {
        var employee = _employeeFactory.Create();
        employee.FirstName = "Ada";
        employee.LastName = "Lovelace";
        employee.Email = "ada@example.com";
        employee.Salary = -1m;
        await employee.WaitForTasks();

        Assert.IsFalse(employee.IsValid, "Salary cannot be negative");
        Assert.IsFalse(employee.IsSavable);
    }

    [TestMethod]
    public async Task Employee_FutureHireDate_IsInvalid()
    {
        var employee = _employeeFactory.Create();
        employee.FirstName = "Ada";
        employee.LastName = "Lovelace";
        employee.Email = "ada@example.com";
        employee.HireDate = DateTime.Today.AddDays(1);
        await employee.WaitForTasks();

        Assert.IsFalse(employee.IsValid, "Hire date cannot be in the future");
    }

    [TestMethod]
    public async Task Address_InvalidAddressType_IsInvalid()
    {
        // Address.Create(street, city, state, zip, type) - the overload
        // AddressList documents as the RIGHT way to copy across aggregates,
        // which nothing called.
        var address = _addressFactory.Create("1 Main St", "Springfield", "IL", "62701", "Vacation");
        await address.WaitForTasks();

        // Factory operations run paused, so no rule has evaluated this data yet -
        // the object reports valid until something asks. This is why a factory
        // method that must not produce invalid objects calls RunRules() itself.
        Assert.IsTrue(address.IsValid, "Rules have not run yet - the factory op was paused");

        await address.RunRules();
        Assert.IsFalse(address.IsValid, "Address type must be Home, Work, or Other");

        // A live edit runs rules automatically
        address.AddressType = "Work";
        await address.WaitForTasks();
        Assert.IsTrue(address.IsValid);
    }

    // =========================================================================
    // Child added to a fetched aggregate then saved twice
    // =========================================================================

    [TestMethod]
    public async Task AddChildToFetchedOrder_SaveTwice_InsertsOnce()
    {
        var order = await _orderFactory.Fetch(1);
        var added = _itemFactory.Create("Added", 1, 5.00m);
        order.Items!.Add(added);
        await order.WaitForTasks();

        order = (IOrder)await order.Save();
        Assert.AreEqual(1, _orderRepo.InsertedItemIds.Count);

        // A second save must not re-insert - the child was marked old
        order.CustomerName = "Touched again";
        await order.WaitForTasks();
        await order.Save();

        Assert.AreEqual(1, _orderRepo.InsertedItemIds.Count,
            "The child must not be inserted a second time");
    }
}
