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
    public partial Guid Id { get; set; }

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
    // When Order.Save() is called, the order hands its row's Items collection
    // to the list factory's Save, and OrderItemList.Update brings that
    // collection in line with the list:
    // - New items get a new row, added to the collection
    // - Modified items write themselves to their existing row
    // - Removed items (in DeletedList) have their row removed from the collection
    // The order then flushes once. See OrderItemList.Update.
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
        #region skill-child-property-trigger
        RuleManager.AddAction(
            t => t.TotalAmount = t.Items?.Sum(i => i.LineTotal) ?? 0,
            t => t.Items![0].LineTotal);
        #endregion
    }

    #region skill-root-create
    [Create]
    public void Create([Service] IOrderItemListFactory itemsFactory)
    {
        Items = itemsFactory.Create();
        OrderDate = DateTime.Today;
        Status = "Draft";
        OrderNumber = $"ORD-{DateTime.Now:yyyyMMddHHmmss}";
    }
    #endregion

    // =========================================================================
    // Aggregate Fetch - Load Root Row, Hand Child Rows to the List Factory
    // =========================================================================
    // GENERATOR BEHAVIOR: instance factory methods run inside
    // FactoryStart/FactoryComplete on this object — the Order is paused for
    // the duration of the body. No explicit PauseAllActions() is needed (and
    // wrapping the body in `using (PauseAllActions())` would actually resume
    // EARLY, when the using disposes, before the factory operation completes).
    //
    // DESIGN DECISION: The repository returns the order row together with its
    // item rows. The order assigns its own properties from its row and hands
    // row.Items to the LIST factory's [Fetch], which builds each item through
    // the ITEM factory's [Fetch](row). Every object in the graph gets its own
    // factory lifecycle, so every object lands with the right persistence
    // state: IsNew=false, IsModified=false throughout.
    //
    // The root's own properties are loaded by plain assignment: the object is
    // paused, so assignment marks nothing modified and runs no rules.
    //
    // GENERATOR BEHAVIOR: a [Fetch] that returns bool reports whether the
    // object was found. Returning false makes the generated factory return
    // null - "no such order" is an answer, not an exception.
    // =========================================================================
    #region skill-root-fetch
    [Remote]
    [Fetch]
    internal bool Fetch(Guid id,
        [Service] IOrderRepository repository,
        [Service] IOrderItemListFactory itemsFactory)
    {
        var row = repository.Get(id);
        if (row == null)
        {
            return false;
        }

        Id = row.Id;
        OrderNumber = row.OrderNumber;
        CustomerName = row.CustomerName;
        OrderDate = row.OrderDate;
        Status = row.Status;
        TotalAmount = row.TotalAmount;

        // Items and every item within: IsNew=false, IsModified=false
        Items = itemsFactory.Fetch(row.Items);

        // After Fetch completes: Order.IsNew=false, Order.IsModified=false
        return true;
    }
    #endregion

    // =========================================================================
    // Aggregate Insert - Make the Row, Hand Its Item Collection to the List
    // =========================================================================
    // The order sets its own key, makes its row, maps itself into it and adds
    // it to the repository. It then hands row.Items to the list factory's
    // Save and flushes ONCE - the order row and every item row are written
    // together.
    //
    // DESIGN DECISION: Insert and Update delegate child persistence to the
    // SAME list factory Save. The list is never new or deleted, so the list
    // factory routes to OrderItemList.Update, and each item's own IsNew
    // decides insert vs update — for a brand-new order, every item is new and
    // gets a new row.
    //
    // DESIGN DECISION: The entity sets its own key (Guid.NewGuid()) - no
    // round trip to the database for a generated id, and no foreign key to
    // pass down: an item row belongs to the order because it sits in the
    // order row's Items collection.
    // =========================================================================
    #region skill-root-insert
    [Remote]
    [Insert]
    internal async Task Insert([Service] IOrderRepository repository,
        [Service] IOrderItemListFactory itemsFactory)
    {
        // Re-run every rule on the server and refuse an invalid aggregate.
        // Recommended - the framework does not do this for you. Throw, never
        // return: after [Insert]/[Update] returns, the framework marks the
        // entity saved whether or not anything was written.
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        // Object is paused — assignment is clean
        Id = Guid.NewGuid();

        var row = new OrderRow();
        MapTo(row);
        repository.Add(row);

        itemsFactory.Save(Items!, row.Items);

        repository.SaveChanges();
    }
    #endregion

    // =========================================================================
    // Aggregate Update - Get the Row, Hand Its Item Collection to the List
    // =========================================================================
    // The order gets its existing row (a missing row is an application
    // failure: KeyNotFoundException), maps itself into it only if its OWN
    // properties changed, hands row.Items to the list factory's Save, and
    // flushes once.
    //
    // The list factory Save runs OrderItemList.Update inside the LIST's own
    // factory operation: removed items have their rows removed, new and
    // modified items go through per-item factory saves, and the list's
    // FactoryComplete(Update) clears the DeletedList and recalculates its
    // modified cache. Each saved item's FactoryComplete marks it unmodified
    // and old. When this Update returns, the framework calls
    // FactoryComplete(Update) on the ORDER — MarkUnmodified() + MarkOld() —
    // and the whole graph reads IsModified=false.
    //
    // COMMON MISTAKE: Writing the item rows here (foreach item -> find its
    // row, copy the values across). The rows get written, but no child
    // factory operation runs, so no child is ever marked unmodified or old:
    // the aggregate still reports IsModified=true after Save, new children
    // keep IsNew=true and get another row on the next Save, and the
    // DeletedList never clears (FactoryComplete fires per factory target —
    // never as a cascade from the parent). Each item maps itself.
    // =========================================================================
    #region skill-root-update
    [Remote]
    [Update]
    internal async Task Update([Service] IOrderRepository repository,
        [Service] IOrderItemListFactory itemsFactory)
    {
        // Re-run every rule on the server and refuse an invalid aggregate.
        // Recommended - the framework does not do this for you. Throw, never
        // return: after [Insert]/[Update] returns, the framework marks the
        // entity saved whether or not anything was written.
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        var row = repository.Get(Id)
            ?? throw new KeyNotFoundException($"Order {Id} not found");

        // Write the order's own columns only if they changed
        if (IsSelfModified)
        {
            MapTo(row);
        }

        itemsFactory.Save(Items!, row.Items);

        repository.SaveChanges();
    }
    #endregion

    // =========================================================================
    // Aggregate Delete - Remove the Row, Its Item Rows Go With It
    // =========================================================================
    // repository.Remove(row) removes the order row together with its item
    // rows, as a database cascade delete would. The order does not loop its
    // items: no child [Delete] exists, and the whole aggregate is going away.
    // =========================================================================
    #region skill-root-delete
    [Remote]
    [Delete]
    internal void Delete([Service] IOrderRepository repository)
    {
        var row = repository.Get(Id)
            ?? throw new KeyNotFoundException($"Order {Id} not found");

        repository.Remove(row);

        repository.SaveChanges();
    }
    #endregion

    private void MapTo(OrderRow row)
    {
        row.Id = Id;
        row.OrderNumber = OrderNumber!;
        row.CustomerName = CustomerName!;
        row.OrderDate = OrderDate;
        row.Status = Status!;
        row.TotalAmount = TotalAmount;
    }
}

// =============================================================================
// Persistence - Rows and Repository
// =============================================================================
// Modeled on an EF Core unit of work: the row classes stand in for EF
// entities, Get returns the root row with its child rows loaded, and
// SaveChanges flushes everything added, changed or removed since the last
// flush. The aggregate persists through ONE repository - its root's.
// =============================================================================

public class OrderRow
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public List<OrderItemRow> Items { get; } = new();
}

public class OrderItemRow
{
    public Guid Id { get; set; }
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public interface IOrderRepository
{
    /// <summary>The order row with its item rows, or null if there is none.</summary>
    OrderRow? Get(Guid id);

    void Add(OrderRow row);

    /// <summary>Removes the order row and its item rows.</summary>
    void Remove(OrderRow row);

    void SaveChanges();
}
