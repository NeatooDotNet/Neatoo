// -----------------------------------------------------------------------------
// Design.Domain - Order Aggregate Root
// -----------------------------------------------------------------------------
// This file demonstrates a complete aggregate pattern with Order as the root,
// OrderItem as child entities, and OrderItemList as the child collection.
// -----------------------------------------------------------------------------

using Neatoo;
using Neatoo.RemoteFactory;
using System.ComponentModel.DataAnnotations;

namespace Design.Domain.Aggregates.OrderAggregate;

/// <summary>
/// Demonstrates: Aggregate root pattern with child entity management.
///
/// Key points:
/// - Aggregate root owns the Save() operation
/// - Child entities (OrderItems) cannot save independently
/// - DeletedList tracks removed items for persistence
/// - Root property allows children to find the aggregate root
/// - Aggregate boundaries are enforced
/// </summary>
[Factory]
public partial class Order : EntityBase<Order>
{
    public partial int Id { get; set; }

    [Required(ErrorMessage = "Order number is required")]
    public partial string? OrderNumber { get; set; }

    [Required(ErrorMessage = "Customer name is required")]
    public partial string? CustomerName { get; set; }

    public partial DateTime OrderDate { get; set; }

    public partial string? Status { get; set; }  // "Draft", "Submitted", "Approved", "Shipped"

    public partial decimal TotalAmount { get; set; }

    // =========================================================================
    // Child Collection - OrderItems
    // =========================================================================
    // The Order aggregate owns the OrderItems collection.
    // When Order.Save() is called:
    // - New items are inserted
    // - Modified items are updated
    // - Removed items (in DeletedList) are deleted
    // =========================================================================
    public partial OrderItemList? Items { get; set; }

    public Order(IEntityBaseServices<Order> services) : base(services)
    {
        // Validation rules
        RuleManager.AddValidation(
            t => t.Items?.Count == 0 && t.Status != "Draft"
                ? "Order must have at least one item"
                : string.Empty,
            t => t.Status);

        // Action rule to calculate total
        RuleManager.AddAction(
            t => t.TotalAmount = t.Items?.Sum(i => i.LineTotal) ?? 0,
            t => t.Items);
    }

    [Create]
    public void Create([Service] IOrderItemListFactory itemsFactory)
    {
        Items = itemsFactory.Create();
        OrderDate = DateTime.Today;
        Status = "Draft";
        OrderNumber = $"ORD-{DateTime.Now:yyyyMMddHHmmss}";
    }

    [Remote]
    [Fetch]
    public void Fetch(int id,
        [Service] IOrderRepository repository,
        [Service] IOrderItemListFactory itemsFactory,
        [Service] IOrderItemFactory itemFactory)
    {
        using (PauseAllActions())
        {
            var data = repository.GetById(id);

            this["Id"].LoadValue(data.Id);
            this["OrderNumber"].LoadValue(data.OrderNumber);
            this["CustomerName"].LoadValue(data.CustomerName);
            this["OrderDate"].LoadValue(data.OrderDate);
            this["Status"].LoadValue(data.Status);
            this["TotalAmount"].LoadValue(data.TotalAmount);

            // Load items
            Items = itemsFactory.Create();
            foreach (var itemData in repository.GetItems(id))
            {
                var item = itemFactory.Create();
                item["Id"].LoadValue(itemData.Id);
                item["ProductName"].LoadValue(itemData.ProductName);
                item["Quantity"].LoadValue(itemData.Quantity);
                item["UnitPrice"].LoadValue(itemData.UnitPrice);
                item["LineTotal"].LoadValue(itemData.LineTotal);

                Items.Add(item);
                // item.IsChild = true, item.ContainingList = Items
            }
        }
        // After Fetch: Order.IsNew=false, Order.IsModified=false
        // Each item: IsNew=false, IsModified=false, IsChild=true
    }

    // =========================================================================
    // Aggregate Insert - Save Root and All Children
    // =========================================================================
    [Remote]
    [Insert]
    public void Insert([Service] IOrderRepository repository)
    {
        // Insert order first to get ID
        var generatedId = repository.InsertOrder(
            OrderNumber!, CustomerName!, OrderDate, Status!, TotalAmount);
        this["Id"].LoadValue(generatedId);

        // Insert all items (all are new for a new order)
        foreach (var item in Items!)
        {
            var itemId = repository.InsertItem(
                Id, item.ProductName!, item.Quantity, item.UnitPrice, item.LineTotal);
            item["Id"].LoadValue(itemId);
        }
    }

    // =========================================================================
    // Aggregate Update - Coordinate All Child Persistence
    // =========================================================================
    // This is where the DeletedList pattern is most important.
    // We must handle: modified items, new items, AND deleted items.
    // =========================================================================
    [Remote]
    [Update]
    public void Update([Service] IOrderRepository repository)
    {
        // Update order header if changed
        if (IsSelfModified)
        {
            repository.UpdateOrder(Id, OrderNumber!, CustomerName!, OrderDate, Status!, TotalAmount);
        }

        // Process active items
        foreach (var item in Items!)
        {
            if (item.IsNew)
            {
                // New item - insert
                var itemId = repository.InsertItem(
                    Id, item.ProductName!, item.Quantity, item.UnitPrice, item.LineTotal);
                item["Id"].LoadValue(itemId);
            }
            else if (item.IsSelfModified)
            {
                // Existing item modified - update
                repository.UpdateItem(item.Id, item.ProductName!, item.Quantity, item.UnitPrice, item.LineTotal);
            }
            // Unmodified items - no action needed
        }

        // =====================================================================
        // Process Deleted Items - The DeletedList Pattern
        // =====================================================================
        // Items removed from the collection are in DeletedList.
        // We must delete them from the database.
        //
        // DESIGN DECISION: The aggregate root coordinates deletion.
        // DeletedList is protected, accessed here via internal mechanism.
        //
        // In real generated code, the framework handles this in FactoryComplete.
        // This demonstrates the pattern explicitly.
        // =====================================================================
        ProcessDeletedItems(repository);

        // After Update completes, FactoryComplete(Update) will:
        // - Call MarkUnmodified() on Order
        // - Clear DeletedList
        // - Clear ContainingList on deleted items
    }

    private void ProcessDeletedItems(IOrderRepository repository)
    {
        // Access DeletedList through internal interface
        // In production code, this is handled by the framework
        // Here we demonstrate what happens conceptually

        // foreach (var deletedItem in Items!.GetDeletedItemsForPersistence())
        // {
        //     repository.DeleteItem(deletedItem.Id);
        // }

        // The framework iterates DeletedList and calls delete
    }

    [Remote]
    [Delete]
    public void Delete([Service] IOrderRepository repository)
    {
        // Delete items first (FK constraint)
        foreach (var item in Items!)
        {
            repository.DeleteItem(item.Id);
        }

        // Delete order
        repository.DeleteOrder(Id);
    }
}

// =============================================================================
// Repository Interface
// =============================================================================

public interface IOrderRepository
{
    (int Id, string OrderNumber, string CustomerName, DateTime OrderDate, string Status, decimal TotalAmount) GetById(int id);
    IEnumerable<(int Id, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal)> GetItems(int orderId);

    int InsertOrder(string orderNumber, string customerName, DateTime orderDate, string status, decimal totalAmount);
    void UpdateOrder(int id, string orderNumber, string customerName, DateTime orderDate, string status, decimal totalAmount);
    void DeleteOrder(int id);

    int InsertItem(int orderId, string productName, int quantity, decimal unitPrice, decimal lineTotal);
    void UpdateItem(int id, string productName, int quantity, decimal unitPrice, decimal lineTotal);
    void DeleteItem(int id);
}
