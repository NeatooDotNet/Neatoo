// -----------------------------------------------------------------------------
// Design.Tests - Rule Exception Tests
// -----------------------------------------------------------------------------
// Pins ErrorHandling/ErrorPatterns.cs RULE EXCEPTION BEHAVIOR: a rule that
// throws marks its trigger property invalid and the exception surfaces to the
// caller when the entity's tasks are awaited.
// -----------------------------------------------------------------------------

using Design.Domain.ErrorHandling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Design.Tests.RuleTests;

[TestClass]
public class RuleExceptionTests
{
    private IServiceScope _scope = null!;
    private IValidationFailureDemoFactory _factory = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _scope = DesignTestServices.GetScope();
        _factory = _scope.GetRequiredService<IValidationFailureDemoFactory>();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _scope.Dispose();
    }

    #region skill-rule-exception
    [TestMethod]
    public void RuleThatThrows_SurfacesToTheCaller_AndMarksPropertyInvalid()
    {
        var entity = _factory.Create();

        // The rule throws before its first await, so the exception reaches the
        // setter's caller, wrapped in an AggregateException. A rule that throws
        // after an await surfaces the same way from WaitForTasks().
        var exception = Assert.ThrowsExactly<AggregateException>(
            () => entity.Name = "ThrowException");
        Assert.IsInstanceOfType<InvalidOperationException>(exception.InnerException);

        // A rule bug is also visible as a message on the trigger property
        Assert.IsFalse(entity["Name"].IsValid);
        Assert.IsFalse(entity.IsValid);
    }
    #endregion

    [TestMethod]
    public async Task RuleThatDoesNotThrow_LeavesEntityValid()
    {
        var entity = _factory.Create();

        entity.Name = "Fine";
        await entity.WaitForTasks();

        Assert.IsTrue(entity["Name"].IsValid);
    }
}
