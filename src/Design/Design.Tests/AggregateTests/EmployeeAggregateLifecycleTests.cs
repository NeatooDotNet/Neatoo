// -----------------------------------------------------------------------------
// Design.Tests - Employee Aggregate Lifecycle Tests (ISNEW-007)
// -----------------------------------------------------------------------------
// Executes the Entities demo aggregate end to end. Before ISNEW-007 this
// aggregate had no test coverage at all, which is why its lifecycle stayed
// wrong: fetched children came back IsNew=true, child rows were written
// directly with no factory saves, and removed children were never deleted.
//
// Assertions read the rows the aggregate left in MockEmployeeRepository's
// in-memory store (see TestInfrastructure.cs).
// -----------------------------------------------------------------------------

using Design.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Design.Tests.AggregateTests;

[TestClass]
public class EmployeeAggregateLifecycleTests
{
    // Written into a stored row after the fetch. A save that writes that row
    // overwrites it, so finding it afterward proves the row was not written.
    private const string NotWritten = "(not written by save)";

    private IServiceScope _scope = null!;
    private IEmployeeFactory _employeeFactory = null!;
    private IAddressFactory _addressFactory = null!;
    private MockEmployeeRepository _repository = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _scope = DesignTestServices.GetScope();
        _employeeFactory = _scope.GetRequiredService<IEmployeeFactory>();
        _addressFactory = _scope.GetRequiredService<IAddressFactory>();
        _repository = (MockEmployeeRepository)_scope.GetRequiredService<IEmployeeRepository>();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _scope.Dispose();
    }

    private IAddress NewAddress(string street, string type = "Home")
    {
        var address = _addressFactory.Create();
        address.Street = street;
        address.City = "Springfield";
        address.State = "IL";
        address.ZipCode = "62701";
        address.AddressType = type;
        return address;
    }


    private async Task<IEmployee> FetchEmployee(Guid id)
    {
        var employee = await _employeeFactory.Fetch(id);
        Assert.IsNotNull(employee, "A seeded employee should be found");
        return employee;
    }

    [TestMethod]
    public async Task Fetch_AddressesAreOldAndClean_AggregateNotModified()
    {
        // Arrange
        var seeded = _repository.SeedEmployee();

        // Act
        var employee = await FetchEmployee(seeded.Id);

        // Assert - root state
        Assert.IsFalse(employee.IsNew, "Fetched employee should not be new");
        Assert.IsFalse(employee.IsModified, "Fetched employee should not be modified");

        // Assert - child state: each address completed its own [Fetch]
        Assert.AreEqual(3, employee.Addresses!.Count);
        foreach (var address in employee.Addresses)
        {
            Assert.IsFalse(address.IsNew, $"Fetched address {address.Id} should not be new");
            Assert.IsFalse(address.IsModified, $"Fetched address {address.Id} should not be modified");
        }
    }

    [TestMethod]
    public async Task FetchModifyAddRemove_Save_RoutesAllPathsAndGraphIsClean()
    {
        // Arrange - three fetched addresses: one modified, one removed, one
        // deliberately left untouched so the clean-child skip is pinned
        var seeded = _repository.SeedEmployee();
        var employee = await FetchEmployee(seeded.Id);
        var modified = employee.Addresses![0];
        var removed = employee.Addresses[1];
        var untouched = employee.Addresses[2];

        // Mark the stored rows this save must leave alone - writing a row
        // overwrites its mark
        seeded.FirstName = NotWritten;
        seeded.Addresses.Single(r => r.Id == untouched.Id).Street = NotWritten;

        modified.City = "Shelbyville";
        employee.Addresses.Remove(removed);
        var added = NewAddress("4 New Ave", "Other");
        employee.Addresses.Add(added);

        await employee.WaitForTasks();
        Assert.IsTrue(employee.IsSavable, "Modified aggregate should be savable");

        // Act
        employee = (IEmployee)await employee.Save();

        // Assert - each path fires exactly once, and the untouched child
        // routes nowhere. The row read is the one stored under the employee's
        // own key, so finding the added child in it also pins that the child
        // was written against the right parent.
        var stored = _repository.Store[employee.Id];
        Assert.AreEqual(NotWritten, stored.FirstName,
            "Employee header untouched - its row must not be written (IsSelfModified guard)");
        Assert.AreEqual("Shelbyville", stored.Addresses.Single(r => r.Id == modified.Id).City,
            "The modified existing address is written to its row");
        Assert.AreEqual(NotWritten, stored.Addresses.Single(r => r.Id == untouched.Id).Street,
            "An unmodified existing child must not be written");
        CollectionAssert.DoesNotContain(stored.Addresses.Select(r => r.Id).ToList(), removed.Id,
            "Only the removed address has its row removed");
        var addedRows = stored.Addresses
            .Where(r => r.Id != modified.Id && r.Id != untouched.Id)
            .ToList();
        Assert.AreEqual(1, addedRows.Count, "Only the added address gets a new row");
        Assert.AreEqual(1, _repository.SaveChangesCount, "One flush for the whole aggregate");

        // Assert - the key the new child set in its [Insert] landed on the
        // entity and on its row, in this employee's row
        Assert.AreNotEqual(Guid.Empty, added.Id, "The address's key must land on the entity");
        Assert.AreEqual(added.Id, addedRows[0].Id, "The entity and its row must share the key");
        Assert.AreEqual("4 New Ave", addedRows[0].Street,
            "The added child must be written into the employee's own row");

        // Assert - whole graph clean after save
        Assert.AreEqual(3, employee.Addresses!.Count);
        Assert.IsFalse(employee.IsModified, "Employee should not be modified after save");
        foreach (var address in employee.Addresses)
        {
            Assert.IsFalse(address.IsNew, $"Address {address.Id} should be old after save");
            Assert.IsFalse(address.IsModified, $"Address {address.Id} should be clean after save");
        }
    }

    [TestMethod]
    public async Task CreateWithAddresses_Save_InsertsAllWithIdWriteback_AndGraphIsClean()
    {
        // Arrange
        var employee = _employeeFactory.Create();
        employee.FirstName = "Grace";
        employee.LastName = "Hopper";
        employee.Email = "grace@example.com";
        employee.Salary = 150000m;
        employee.Addresses!.Add(NewAddress("1 First St"));
        employee.Addresses.Add(NewAddress("2 Second St", "Work"));

        await employee.WaitForTasks();
        Assert.IsTrue(employee.IsNew, "Created employee is new");
        Assert.IsTrue(employee.IsSavable, "Valid new aggregate should be savable");

        // Act
        employee = (IEmployee)await employee.Save();

        // Assert - routing
        Assert.AreEqual(1, _repository.AddedRows.Count, "The employee row is added exactly once");
        Assert.AreEqual(1, _repository.SaveChangesCount, "One flush for the whole aggregate");

        // Assert - keys landed on root and children
        Assert.AreNotEqual(Guid.Empty, employee.Id, "The employee's key must land on the root");
        Assert.IsTrue(_repository.Store.TryGetValue(employee.Id, out var stored),
            "The store holds the employee row under the root's key");
        Assert.AreEqual(2, employee.Addresses!.Count);
        Assert.AreEqual(2, stored.Addresses.Count,
            "Every address of a new aggregate gets a new row");
        var rowIds = stored.Addresses.Select(r => r.Id).ToList();
        CollectionAssert.DoesNotContain(rowIds, Guid.Empty,
            "Every address set its key in [Insert] - none was routed to Update");
        CollectionAssert.AllItemsAreUnique(rowIds);
        CollectionAssert.AreEquivalent(rowIds,
            employee.Addresses.Select(a => a.Id).ToList(),
            "The addresses' keys must land on the entities");

        // Assert - parent link: the root keys its own row BEFORE delegating
        // child persistence, and every child is written into that row
        CollectionAssert.AreEquivalent(new[] { "1 First St", "2 Second St" },
            stored.Addresses.Select(r => r.Street).ToList(),
            "Every child must be written into the employee's own row");

        // Assert - graph old and clean
        Assert.IsFalse(employee.IsNew, "Employee should be old after insert");
        Assert.IsFalse(employee.IsModified, "Employee should be clean after insert");
        foreach (var address in employee.Addresses)
        {
            Assert.IsFalse(address.IsNew, $"Address {address.Id} should be old after insert");
            Assert.IsFalse(address.IsModified, $"Address {address.Id} should be clean after insert");
        }
    }
}
