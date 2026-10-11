// -----------------------------------------------------------------------------
// Design.Tests - Rule With a Domain Service
// -----------------------------------------------------------------------------
// Pins D21: a rule may take an interface-factory domain service. In Server
// mode the interface resolves to the implementation, so the test observes the
// rule's behaviour: the message and the derived quote follow the service's
// answer. The implementation carries no [Factory]; that is for code review.
// -----------------------------------------------------------------------------

using Design.Domain.Rules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Design.Tests.ServiceTests;

[TestClass]
public class ShippingQuoteRuleTests
{
    private IServiceScope _scope = null!;
    private IShipmentRequestFactory _factory = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _scope = DesignTestServices.GetScope();
        _factory = _scope.GetRequiredService<IShipmentRequestFactory>();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _scope.Dispose();
    }

    #region skill-rule-with-domain-service-test
    [TestMethod]
    public async Task Rule_DestinationServed_QuotesFromTheService()
    {
        var request = _factory.Create();

        request.Destination = "CA";
        request.WeightKg = 2m;
        await request.WaitForTasks();

        Assert.IsTrue(request.IsValid);
        Assert.AreEqual(13.00m, request.Quote);  // 6.50 per kg from the service
    }

    [TestMethod]
    public async Task Rule_DestinationNotServed_ReportsTheServiceAnswer()
    {
        var request = _factory.Create();

        request.Destination = "AQ";
        request.WeightKg = 2m;
        await request.WaitForTasks();

        Assert.IsFalse(request.IsValid);
        Assert.IsTrue(request.PropertyMessages.Any(m => m.Message.Contains("do not ship to AQ")));
        Assert.AreEqual(0m, request.Quote);
    }
    #endregion
}
