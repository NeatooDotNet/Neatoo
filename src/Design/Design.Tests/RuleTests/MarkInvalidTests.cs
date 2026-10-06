// -----------------------------------------------------------------------------
// Design.Tests - MarkInvalid Tests
// -----------------------------------------------------------------------------
// Pins ErrorHandling/MarkInvalidDemo.cs: MarkInvalid sets ObjectInvalid, the
// message is visible in PropertyMessages, and IsValid is false.
// -----------------------------------------------------------------------------

using Design.Domain.ErrorHandling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo;

namespace Design.Tests.RuleTests;

[TestClass]
public class MarkInvalidTests
{
    private IServiceScope _scope = null!;
    private IPaymentDemoFactory _factory = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _scope = DesignTestServices.GetScope();
        _factory = _scope.GetRequiredService<IPaymentDemoFactory>();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _scope.Dispose();
    }

    #region docs-mark-invalid-test
    [TestMethod]
    public async Task MarkInvalid_SetsAnObjectLevelMessage()
    {
        var payment = _factory.Create();
        payment.Reference = "TXN-001";
        payment.Amount = 100m;
        await payment.WaitForTasks();
        Assert.IsTrue(payment.IsValid);

        payment.RecordGatewayRejection("Payment gateway rejected");
        await payment.WaitForTasks();

        Assert.IsFalse(payment.IsValid);
        Assert.AreEqual("Payment gateway rejected", payment.ObjectInvalid);
        Assert.IsTrue(payment.PropertyMessages.Any(m => m.Message.Contains("Payment gateway rejected")));
    }
    #endregion

    [TestMethod]
    public async Task RunRulesAll_DoesNotClearTheObjectLevelMessage()
    {
        var payment = _factory.Create();
        payment.Reference = "TXN-001";
        payment.Amount = 100m;
        payment.RecordGatewayRejection("Payment gateway rejected");
        await payment.WaitForTasks();
        Assert.IsFalse(payment.IsValid);

        // Observed behaviour, pinned here because the framework's XML doc on
        // MarkInvalid (ValidateBase.cs) claims RunRules(All) clears the error:
        // ClearAllMessages drops the message, but the built-in rule on
        // ObjectInvalid re-reports the still-set value. Only the object itself
        // can reset ObjectInvalid (its setter is protected).
        await payment.RunRules(RunRulesFlag.All);

        Assert.IsFalse(payment.IsValid);
        Assert.AreEqual("Payment gateway rejected", payment.ObjectInvalid);
    }
}
