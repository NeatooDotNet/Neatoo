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
/// - Child loading and child persistence are delegated to the child factories
/// - Root property allows children to find the aggregate root
/// - Aggregate boundaries are enforced
/// </summary>
[Factory]
internal partial class Order : EntityBase<Order>, IOrder
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
    // The mechanics live in OrderItemList.Update - see that file.
    // =========================================================================
    public partial IOrderItemList? Items { get; set; }

    public Order(IEntityBaseServices<Order> services) : base(services)
    {
        // Validation rules
        RuleManager.AddValidation(
            t => t.Items?.Count == 0 && t.Status != "Draft"
                ? "Order must have at least one item"
                : string.Empty,
            t => t.Status);

        // =====================================================================
        // Child Property Trigger - Reacting to Child Changes via AddAction
        // =====================================================================
        // The trigger expression t => t.Items![0].LineTotal resolves to the
        // property path "Items.LineTotal". The [0] indexer is a syntactic
        // placeholder — it does NOT mean "only the first item." Any child
        // item whose LineTotal changes bubbles up as "Items.LineTotal" and
        // matches this trigger.
        //
        // DESIGN DECISION: Use child property triggers for parent-child
        // reactivity. This is type-safe and expression-based, just like
        // same-object AddAction triggers.
        //
        // COMMON MISTAKE: Using t => t.Items as the trigger.
        //   RuleManager.AddAction(t => ..., t => t.Items);
        //   // WRONG: Only fires when Items property is reassigned (new list),
        //   // NOT when child items within the list change their properties.
        //   // TriggerProperty.IsMatch uses exact string equality:
        //   // "Items" != "Items.LineTotal" — rule never fires.
        //
        // For reacting to multiple child properties, register multiple triggers:
        //   RuleManager.AddAction(t => ...,
        //       t => t.Items![0].LineTotal,
        //       t => t.Items![0].Quantity);
        //
        // DID NOT DO THIS: Use NeatooPropertyChanged event subscription.
        //   NeatooPropertyChanged += (args) => {
        //       if (args.OriginalPropertyName == "LineTotal") { ... }
        //   };
        //   // Works but is verbose, string-based, and error-prone.
        //   // Reserve NeatooPropertyChanged for cases where you need the
        //   // event args (ChangeReason, Source) or need to react to ANY
        //   // child change regardless of which property.
        // =====================================================================
        RuleManager.AddAction(
            t => t.TotalAmount = t.Items?.Sum(i => i.LineTotal) ?? 0,
            t => t.Items![0].LineTotal);
    }

    [Create]
    public void Create([Service] IOrderItemListFactory itemsFactory)
    {
        Items = itemsFactory.Create();
        OrderDate = DateTime.Today;
        Status = "Draft";
        OrderNumber = $"ORD-{DateTime.Now:yyyyMMddHHmmss}";
    }

    // =========================================================================
    // Aggregate Fetch - Load Root, Delegate Children to the List Factory
    // =========================================================================
    // GENERATOR BEHAVIOR: instance factory methods run inside
    // FactoryStart/FactoryComplete on this object — the Order is paused for
    // the duration of the body. No explicit PauseAllActions() is needed (and
    // wrapping the body in `using (PauseAllActions())` would actually resume
    // EARLY, when the using disposes, before the factory operation completes).
    //
    // DESIGN DECISION: Children load through the LIST factory's [Fetch], which
    // in turn loads each item through the ITEM factory's [Fetch]. Every object
    // in the graph gets its own factory lifecycle, so every object lands with
    // the right persistence state: IsNew=false, IsModified=false throughout.
    //
    // This[...].LoadValue is shown for the root's own properties — plain
    // property assignment is equally clean here because the object is paused;
    // LoadValue makes the load explicit and works even when not paused.
    // =========================================================================
    [Remote]
    [Fetch]
    internal void Fetch(int id,
        [Service] IOrderRepository repository,
        [Service] IOrderItemListFactory itemsFactory)
    {
        var data = repository.GetById(id);

        this["Id"].LoadValue(data.Id);
        this["OrderNumber"].LoadValue(data.OrderNumber);
        this["CustomerName"].LoadValue(data.CustomerName);
        this["OrderDate"].LoadValue(data.OrderDate);
        this["Status"].LoadValue(data.Status);
        this["TotalAmount"].LoadValue(data.TotalAmount);

        // Items and every item within: IsNew=false, IsModified=false
        Items = itemsFactory.Fetch(id);

        // After Fetch completes: Order.IsNew=false, Order.IsModified=false
    }

    // =========================================================================
    // Aggregate Insert - Save Root, Delegate Children to the List Factory
    // =========================================================================
    // DESIGN DECISION: Insert and Update delegate child persistence to the
    // SAME list factory Save. The list is never new or deleted, so the list
    // factory routes to OrderItemList.Update, and each item's own IsNew
    // decides insert vs update — for a brand-new order, every item is new and
    // gets inserted.
    // =========================================================================
    [Remote]
    [Insert]
    internal void Insert([Service] IOrderRepository repository,
        [Service] IOrderItemListFactory itemsFactory)
    {
        // Insert order first to get ID (object is paused — assignment is clean)
        Id = repository.InsertOrder(
            OrderNumber!, CustomerName!, OrderDate, Status!, TotalAmount);

        itemsFactory.Save(Items!, Id);
    }

    // =========================================================================
    // Aggregate Update - Delegate Child Persistence to the List Factory
    // =========================================================================
    // The list factory Save runs OrderItemList.Update inside the LIST's own
    // factory operation: deleted items are removed from persistence, new and
    // modified items go through per-item factory saves, and the list's
    // FactoryComplete(Update) clears the DeletedList and recalculates its
    // modified cache. Each saved item's FactoryComplete marks it unmodified
    // and old. When this Update returns, the framework calls
    // FactoryComplete(Update) on the ORDER — MarkUnmodified() + MarkOld() —
    // and the whole graph reads IsModified=false.
    //
    // COMMON MISTAKE: Writing child rows to the repository directly here
    // (foreach item -> repository.UpdateItem(...)). The rows get written, but
    // no child factory operation runs, so no child is ever marked unmodified
    // or old: the aggregate still reports IsModified=true after Save, new
    // children keep IsNew=true and re-insert on the next Save, and the
    // DeletedList never clears (FactoryComplete fires per factory target —
    // never as a cascade from the parent).
    // =========================================================================
    [Remote]
    [Update]
    internal void Update([Service] IOrderRepository repository,
        [Service] IOrderItemListFactory itemsFactory)
    {
        // Update order header if changed
        if (IsSelfModified)
        {
            repository.UpdateOrder(Id, OrderNumber!, CustomerName!, OrderDate, Status!, TotalAmount);
        }

        itemsFactory.Save(Items!, Id);
    }

    [Remote]
    [Delete]
    internal void Delete([Service] IOrderRepository repository)
    {
        // Delete items first (FK constraint). Direct repository deletes are
        // INTENTIONAL here - deleted children get no factory operation
        // (see OrderItemList.Update), and the whole aggregate is going away.
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
