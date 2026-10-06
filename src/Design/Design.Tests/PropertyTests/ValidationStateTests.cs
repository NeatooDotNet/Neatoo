// -----------------------------------------------------------------------------
// Design.Tests - Validation and Busy State Tests
// -----------------------------------------------------------------------------
// Pins the IsValid / IsSelfValid split documented in
// PropertySystem/StateProperties.cs, and IsBusy around an async rule.
// -----------------------------------------------------------------------------

using Design.Domain.PropertySystem;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Design.Tests.PropertyTests;

[TestClass]
public class ValidationStateTests
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

    #region skill-is-valid-vs-self-valid
    [TestMethod]
    public async Task InvalidChild_MakesParentInvalid_ButNotSelfInvalid()
    {
        var parent = _scope.GetRequiredService<IValidationStateDemoFactory>().Create();
        parent.RequiredField = "set";
        parent.Child!.RequiredField = "set";
        await parent.WaitForTasks();
        Assert.IsTrue(parent.IsValid);

        // Break the child only
        parent.Child.RequiredField = "";
        await parent.WaitForTasks();

        Assert.IsTrue(parent.IsSelfValid, "The parent's own rules pass");
        Assert.IsFalse(parent.IsValid, "IsValid aggregates the child");
        Assert.IsFalse(parent.Child.IsValid);
        Assert.IsTrue(parent.PropertyMessages.Count > 0, "The child's message reaches the parent");
    }
    #endregion

    #region skill-is-busy
    [TestMethod]
    public async Task AsyncRule_SetsIsBusyUntilItCompletes()
    {
        var entity = _scope.GetRequiredService<IBusyStateDemoFactory>().Create();

        entity.Name = "Test";  // triggers the async action rule

        Assert.IsTrue(entity.IsBusy, "The async rule is still running");

        await entity.WaitForTasks();

        Assert.IsFalse(entity.IsBusy);
        Assert.AreEqual("Processed: Test", entity.ComputedValue);
    }
    #endregion
}
