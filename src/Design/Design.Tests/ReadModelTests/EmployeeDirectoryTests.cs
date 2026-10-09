// -----------------------------------------------------------------------------
// Design.Tests - Read Model Tests
// -----------------------------------------------------------------------------
// Pins the read-model pattern: a plain [Factory] class with [Fetch] only, no
// Neatoo base class, that computes what the screen shows.
// -----------------------------------------------------------------------------

using Design.Domain.ReadModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo;

namespace Design.Tests.ReadModelTests;

[TestClass]
public class EmployeeDirectoryTests
{
    private IServiceScope _scope = null!;
    private IEmployeeDirectoryFactory _factory = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _scope = DesignTestServices.GetScope();
        _factory = _scope.GetRequiredService<IEmployeeDirectoryFactory>();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _scope.Dispose();
    }

    [TestMethod]
    public async Task Fetch_All_ReturnsEveryRowAndTheServerComputedCount()
    {
        var directory = await _factory.Fetch();

        Assert.AreEqual(3, directory.Employees.Count);
        Assert.AreEqual(2, directory.ActiveCount, "The read model computes the count; the UI reads it");
    }

    [TestMethod]
    public async Task Fetch_WithCriteria_ReturnsOnlyMatchingRows()
    {
        var directory = await _factory.Fetch(new EmployeeSearchCriteria { Department = "Engineering", ActiveOnly = true });

        Assert.AreEqual(2, directory.Employees.Count);
        Assert.IsTrue(directory.Employees.All(e => e.Department == "Engineering" && e.IsActive));
        Assert.AreEqual(2, directory.ActiveCount);
    }

    [TestMethod]
    public async Task ReadModel_HasNoNeatooBaseClass()
    {
        var directory = await _factory.Fetch();

        Assert.IsNotInstanceOfType<IValidateBase>(directory, "A read model is a plain [Factory] class");
    }
}
