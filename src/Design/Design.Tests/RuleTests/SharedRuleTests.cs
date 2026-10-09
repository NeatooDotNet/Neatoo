// -----------------------------------------------------------------------------
// Design.Tests - Shared Rule Tests
// -----------------------------------------------------------------------------
// Pins Rules/SharedRules.cs: one rule typed on IHasUniqueCode validates two
// entity types, and reaches the server through a [Remote, Execute] command.
// -----------------------------------------------------------------------------

using Design.Domain.Rules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Design.Tests.RuleTests;

[TestClass]
public class SharedRuleTests
{
    private IServiceScope _scope = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _scope = DesignTestServices.GetScope();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _scope.Dispose();
    }

    #region skill-shared-rule-test
    [TestMethod]
    public async Task SharedRule_RunsOnBothEntityTypes()
    {
        var warehouse = _scope.GetRequiredService<IWarehouseFactory>().Create();
        var supplier = _scope.GetRequiredService<ISupplierFactory>().Create();

        warehouse.Code = "TAKEN";
        supplier.Code = "TAKEN";
        await warehouse.WaitForTasks();
        await supplier.WaitForTasks();

        Assert.IsFalse(warehouse.IsValid, "The command reported the code taken");
        Assert.IsFalse(supplier.IsValid, "Same rule, other entity type");

        warehouse.Code = "WH-1";
        await warehouse.WaitForTasks();
        Assert.IsTrue(warehouse.IsValid);
    }
    #endregion

    [TestMethod]
    public async Task SharedRule_EmptyCode_IsNotChecked()
    {
        var warehouse = _scope.GetRequiredService<IWarehouseFactory>().Create();

        warehouse.Code = "";
        await warehouse.WaitForTasks();

        Assert.IsTrue(warehouse.IsValid, "Nothing to check until a code is entered");
    }
}
