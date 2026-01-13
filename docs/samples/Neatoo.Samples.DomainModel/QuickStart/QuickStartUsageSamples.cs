using Neatoo;

namespace Neatoo.Samples.DomainModel.QuickStart;

/// <summary>
/// Factory usage examples for quick-start guide.
/// </summary>
public class QuickStartUsageSamples
{
    private readonly IOrderFactory _orderFactory;

    public QuickStartUsageSamples(IOrderFactory orderFactory)
    {
        _orderFactory = orderFactory;
    }

    #region qs-factory-usage
    public async Task FactoryUsageExample(int orderId)
    {
        // Create a new order
        var order = _orderFactory.Create();
        order.CustomerName = "Acme Corp";
        order.Total = 150.00m;

        // Save returns the updated entity (with generated ID, etc.)
        order = await _orderFactory.Save(order);

        // Fetch an existing order
        var existingOrder = await _orderFactory.Fetch(orderId);

        // Modify and save
        existingOrder.Total = 175.00m;
        existingOrder = await _orderFactory.Save(existingOrder);
    }
    #endregion
}
