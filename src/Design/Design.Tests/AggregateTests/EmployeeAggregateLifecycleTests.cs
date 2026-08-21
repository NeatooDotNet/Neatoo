// -----------------------------------------------------------------------------
// Design.Tests - Employee Aggregate Lifecycle Tests (ISNEW-007)
// -----------------------------------------------------------------------------
// Executes the Entities demo aggregate end to end. Before ISNEW-007 this
// aggregate had no test coverage at all, which is why its lifecycle stayed
// wrong: fetched children came back IsNew=true, child rows were written
// directly with no factory saves, and removed children were never deleted.
// -----------------------------------------------------------------------------

using Design.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Design.Tests.AggregateTests;

[TestClass]
public class EmployeeAggregateLifecycleTests
{
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

    [TestMethod]
    public async Task Fetch_AddressesAreOldAndClean_AggregateNotModified()
    {
        // Act
        var employee = await _employeeFactory.Fetch(1);

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
        var employee = await _employeeFactory.Fetch(1);
        var modified = employee.Addresses![0];
        var removed = employee.Addresses[1];
        var untouched = employee.Addresses[2];

        modified.City = "Shelbyville";
        employee.Addresses.Remove(removed);
        var added = NewAddress("4 New Ave", "Other");
        employee.Addresses.Add(added);

        await employee.WaitForTasks();
        Assert.IsTrue(employee.IsSavable, "Modified aggregate should be savable");

        // Act
        employee = (IEmployee)await employee.Save();

        // Assert - each route fires exactly once, and the untouched child
        // routes nowhere
        Assert.AreEqual(0, _repository.UpdatedEmployeeIds.Count,
            "Employee header untouched - UpdateEmployee must be skipped (IsSelfModified guard)");
        CollectionAssert.AreEqual(new[] { modified.Id }, _repository.UpdatedAddressIds,
            "Only the modified existing address routes to UpdateAddress - the untouched one is skipped");
        Assert.AreEqual(1, _repository.InsertedAddressIds.Count,
            "Only the added address routes to InsertAddress");
        CollectionAssert.AreEqual(new[] { removed.Id }, _repository.DeletedAddressIds,
            "Only the removed address routes to DeleteAddress");
        CollectionAssert.DoesNotContain(_repository.UpdatedAddressIds, untouched.Id,
            "An unmodified existing child must not be written");

        // Assert - generated Id landed on the newly inserted child, and the
        // child was written against the right parent (FK propagation)
        Assert.AreEqual(_repository.InsertedAddressIds[0], added.Id,
            "Generated address Id must land on the entity");
        CollectionAssert.AreEqual(new[] { employee.Id }, _repository.InsertAddressParentIds,
            "The added child must be inserted against the employee's id");

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
        Assert.AreEqual(1, _repository.InsertedEmployeeIds.Count);
        Assert.AreEqual(2, _repository.InsertedAddressIds.Count,
            "Every address of a new aggregate routes to InsertAddress");
        Assert.AreEqual(0, _repository.UpdatedAddressIds.Count);

        // Assert - generated Ids landed on root and children
        Assert.AreEqual(_repository.InsertedEmployeeIds[0], employee.Id,
            "Generated employee Id must land on the root");
        Assert.AreEqual(2, employee.Addresses!.Count);
        CollectionAssert.AreEquivalent(_repository.InsertedAddressIds,
            employee.Addresses.Select(a => a.Id).ToList(),
            "Generated address Ids must land on the entities");

        // Assert - FK propagation: the root must write its own generated Id
        // BEFORE delegating child persistence, or children are orphaned at 0
        CollectionAssert.AreEqual(new[] { employee.Id, employee.Id }, _repository.InsertAddressParentIds,
            "Every child must be inserted against the employee's generated id");

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
