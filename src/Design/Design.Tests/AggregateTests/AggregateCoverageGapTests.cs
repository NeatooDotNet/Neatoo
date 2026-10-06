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

    private async Task<IOrder> FetchOrder(Guid id)
    {
        var order = await _orderFactory.Fetch(id);
        Assert.IsNotNull(order, "A seeded order should be found");
        return order;
    }

    private async Task<IEmployee> FetchEmployee(Guid id)
    {
        var employee = await _employeeFactory.Fetch(id);
        Assert.IsNotNull(employee, "A seeded employee should be found");
        return employee;
    }

    // =========================================================================
    // The IsSelfModified header guard — positive direction
    // =========================================================================
    // Both Order.Update and Employee.Update write the root row only when the
    // root's OWN properties changed. The skip direction was pinned for free by
    // the lifecycle tests; nothing asserted the write actually happens, so
    // deleting the mapping call kept the suite green.

    [TestMethod]
    public async Task OrderUpdate_WhenRootPropertyChanged_WritesHeader()
    {
        var seeded = _orderRepo.SeedOrder();
        var order = await FetchOrder(seeded.Id);
        order.CustomerName = "Renamed Customer";
        await order.WaitForTasks();

        await order.Save();

        Assert.AreEqual("Renamed Customer", _orderRepo.Store[seeded.Id].CustomerName,
            "A changed root property must write the order header");
        Assert.AreEqual(1, _orderRepo.SaveChangesCount, "...and flush it");
    }

    [TestMethod]
    public async Task EmployeeUpdate_WhenRootPropertyChanged_WritesHeader()
    {
        var seeded = _employeeRepo.SeedEmployee();
        var employee = await FetchEmployee(seeded.Id);
        employee.Email = "renamed@example.com";
        await employee.WaitForTasks();

        await employee.Save();

        Assert.AreEqual("renamed@example.com", _employeeRepo.Store[seeded.Id].Email,
            "A changed root property must write the employee header");
        Assert.AreEqual(1, _employeeRepo.SaveChangesCount, "...and flush it");
    }

    // =========================================================================
    // Root delete paths
    // =========================================================================

    [TestMethod]
    public async Task OrderDelete_RemovesTheOrderRowWithItsItemRows()
    {
        var seeded = _orderRepo.SeedOrder();
        var order = await FetchOrder(seeded.Id);
        var itemIds = order.Items!.Select(i => i.Id).ToList();

        order.Delete();
        Assert.IsTrue(order.IsDeleted);
        Assert.IsTrue(order.IsSavable, "A deleted persisted root is savable");

        await order.Save();

        // The order removes its own row; its item rows go with it, as a
        // database cascade delete would
        Assert.AreEqual(1, _orderRepo.RemovedRows.Count, "The order row is removed once");
        var removedRow = _orderRepo.RemovedRows[0];
        Assert.AreEqual(seeded.Id, removedRow.Id, "The removed row is this order's row");
        CollectionAssert.AreEquivalent(itemIds, removedRow.Items.Select(r => r.Id).ToList(),
            "Every item row goes with the order");
        Assert.IsFalse(_orderRepo.Store.ContainsKey(seeded.Id),
            "After the flush the store holds neither the order nor its items");
        Assert.AreEqual(1, _orderRepo.SaveChangesCount);
    }

    [TestMethod]
    public async Task EmployeeDelete_DeletesPersistedAddressesOnly()
    {
        var seeded = _employeeRepo.SeedEmployee();
        var employee = await FetchEmployee(seeded.Id);
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

        // The employee row is removed with its address rows - exactly the
        // persisted addresses. The new address was never written, so it has
        // no row and nothing is deleted on its behalf.
        Assert.AreEqual(1, _employeeRepo.RemovedRows.Count, "The employee row is removed once");
        var removedRow = _employeeRepo.RemovedRows[0];
        Assert.AreEqual(seeded.Id, removedRow.Id, "The removed row is this employee's row");
        CollectionAssert.AreEquivalent(persistedIds, removedRow.Addresses.Select(a => a.Id).ToList(),
            "Only persisted addresses are deleted - the new one was never written");
        Assert.IsFalse(_employeeRepo.Store.ContainsKey(seeded.Id),
            "After the flush the store holds neither the employee nor its addresses");
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
        var seeded = _orderRepo.SeedOrder();  // two item rows
        var order = await FetchOrder(seeded.Id);
        var added = _itemFactory.Create("Added", 1, 5.00m);
        order.Items!.Add(added);
        await order.WaitForTasks();

        order = (IOrder)await order.Save();
        var stored = _orderRepo.Store[seeded.Id];
        Assert.AreEqual(3, stored.Items.Count, "The added child gets one new row");

        // A second save must not re-insert - the child was marked old
        order.CustomerName = "Touched again";
        await order.WaitForTasks();
        await order.Save();

        Assert.AreEqual(3, stored.Items.Count,
            "The child must not be inserted a second time");
        Assert.AreEqual(1, stored.Items.Count(r => r.Id == added.Id),
            "The added child has exactly one row");
    }
}
