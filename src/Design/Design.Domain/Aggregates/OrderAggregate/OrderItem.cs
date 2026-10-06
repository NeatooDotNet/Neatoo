// -----------------------------------------------------------------------------
// Design.Domain - OrderItem (Child Entity in Aggregate)
// -----------------------------------------------------------------------------
// This file demonstrates a child entity within an aggregate.
// OrderItem cannot save independently - it's saved through Order.
// -----------------------------------------------------------------------------

using Neatoo;
using Neatoo.RemoteFactory;
using System.ComponentModel.DataAnnotations;

namespace Design.Domain.Aggregates.OrderAggregate;

/// <summary>
/// Demonstrates: Child entity within an aggregate.
///
/// Key points:
/// - Cannot be saved independently: IOrderItem extends IEntityBase, which has
///   no Save() - the barrier is the interface, not a runtime flag
/// - ContainingList tracks which list owns this item
/// - Root property points to Order (aggregate root)
/// - Delete/UnDelete managed by list operations
/// - Maps itself: local (non-[Remote]) Fetch/Insert/Update ops take its own
///   OrderItemRow, which only the list's factory operations can supply
/// - No [Delete]: a removed item's row is removed by OrderItemList.Update
/// </summary>
[Factory]
internal partial class OrderItem : EntityBase<OrderItem>, IOrderItem
{
    public partial Guid Id { get; set; }

    [Required(ErrorMessage = "Product name is required")]
    [StringLength(100)]
    public partial string? ProductName { get; set; }

    [Range(1, 10000, ErrorMessage = "Quantity must be between 1 and 10000")]
    public partial int Quantity { get; set; }

    [Range(0.01, 1000000, ErrorMessage = "Unit price must be positive")]
    public partial decimal UnitPrice { get; set; }

    public partial decimal LineTotal { get; set; }

    public OrderItem(IEntityBaseServices<OrderItem> services) : base(services)
    {
        // Action rule to calculate line total
        RuleManager.AddAction(
            t => t.LineTotal = t.Quantity * t.UnitPrice,
            t => t.Quantity,
            t => t.UnitPrice);
    }

    [Create]
    public void Create()
    {
        Quantity = 1;
    }

    [Create]
    public void Create(string productName, int quantity, decimal unitPrice)
    {
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
        // LineTotal calculated by rule
    }

    // =========================================================================
    // Child Fetch - Called from OrderItemList.Fetch With This Item's Row
    // =========================================================================
    // GENERATOR BEHAVIOR: instance [Create]/[Fetch]/[Insert]/[Update] methods
    // run inside FactoryStart/FactoryComplete on THIS object - the object is
    // paused for the duration of the method body, so plain property assignment
    // loads values without marking anything modified, and rules do not fire.
    //
    // After Fetch: IsNew=false (IsNew defaults to false; only a completed
    // [Create] marks an entity new), IsModified=false.
    //
    // DESIGN DECISION: LineTotal is loaded from persistence rather than
    // recalculated - rules do not run while the factory operation is paused.
    // =========================================================================
    [Fetch]
    internal void Fetch(OrderItemRow row)
    {
        Id = row.Id;
        ProductName = row.ProductName;
        Quantity = row.Quantity;
        UnitPrice = row.UnitPrice;
        LineTotal = row.LineTotal;
    }

    // =========================================================================
    // Child Insert/Update - Called (via the generated factory Save) from
    // OrderItemList.Update With This Item's Row
    // =========================================================================
    // DESIGN DECISION: The child maps itself. OrderItemList.Update finds (or,
    // for a new item, makes and adds) the item's row in the order row's Items
    // collection and passes it here; the item writes its own columns into it.
    // No repository is involved - the order flushes once after the list is
    // done.
    //
    // These operations are local (no [Remote]) and internal, and their
    // signatures take the item's row, which only the list's [Update] can
    // supply. Outside consumers cannot persist a child: IOrderItem extends
    // IEntityBase (no Save()), and the child factory's Save needs a row the
    // consumer does not have.
    //
    // GENERATOR BEHAVIOR: because Insert and Update share the same parameter
    // list, the generated IOrderItemFactory exposes a single
    // Save(IOrderItem target, OrderItemRow row) that routes on the ITEM's own
    // IsDeleted/IsNew. Each routed call wraps the method with
    // FactoryStart/FactoryComplete on the item - and FactoryComplete(Insert)
    // and FactoryComplete(Update) call MarkUnmodified() and MarkOld().
    // THAT is how child entities come out clean after an aggregate save:
    // per-item factory saves, not a cascade. Lifecycle hooks fire only on the
    // single factory target - there is no FactoryComplete cascade through the
    // object graph.
    //
    // DESIGN DECISION: No [Delete]. A removed item is never routed to the item
    // factory: OrderItemList.Update removes its row from the collection.
    //
    // DID NOT DO THIS: Give child entities [Remote] persistence.
    //
    // REJECTED PATTERN:
    //   [Remote]
    //   [Insert]
    //   public void Insert([Service] IOrderRepository repo) { ... }
    //
    // WHY NOT: [Remote] + a signature any consumer can fulfill would let a
    // child be persisted outside its aggregate. Child persistence is
    // coordinated by the aggregate root's save.
    // =========================================================================
    [Insert]
    internal void Insert(OrderItemRow row)
    {
        // The entity sets its own key. Paused - plain assignment stays clean.
        Id = Guid.NewGuid();
        MapTo(row);
    }

    [Update]
    internal void Update(OrderItemRow row)
    {
        MapTo(row);
    }

    private void MapTo(OrderItemRow row)
    {
        row.Id = Id;
        row.ProductName = ProductName!;
        row.Quantity = Quantity;
        row.UnitPrice = UnitPrice;
        row.LineTotal = LineTotal;
    }
}

// =============================================================================
// Child Entity Lifecycle - Detailed Documentation
// =============================================================================
//
// ADDING NEW ITEM (user flow, live list):
//   var item = orderItemFactory.Create("Widget", 5, 10.00m);
//   order.Items.Add(item);
//
//   What happens in list.InsertItem() (list is not paused):
//   1. Check: item not already in list (throws if duplicate)
//   2. Check: item not busy (throws if IsBusy=true)
//   3. Check: item.Root == this.Root OR item.Root == null
//      (throws if item belongs to different aggregate)
//   4. If item.ContainingList != null (was in another list):
//      - Remove from old list's DeletedList (intra-aggregate move)
//   5. If item.IsDeleted: item.UnDelete()
//   6. item.MarkModified() - unconditionally. Attaching to a live list is a
//      change to this graph whatever the item's own persistence state, and it
//      is the ONLY channel by which a new child's arrival reaches the parent
//      (IsNew never aggregates upward)
//   7. item.SetContainingList(this)
//   8. Add to collection
//
// FETCHED ITEM (factory flow, paused list):
//   Items loaded inside OrderItemList.Fetch are added while the list is paused
//   by its own factory operation. The paused add path skips the DIRT-producing
//   steps above (notably MarkModified) but still applies child IDENTITY:
//   fetched items get a ContainingList, so item.Delete() routes through the
//   list exactly as it does for a live add.
//
//   Parent/Root are established one step later than the adds: during the list's
//   own [Fetch] the list has no Parent yet, so each add propagates null; when
//   the parent assigns `Items = itemsFactory.Fetch(row.Items)`, SetParent flows
//   through the list to every item.
//
// REMOVING EXISTING ITEM:
//   var item = order.Items[0];  // item.IsNew = false
//   order.Items.Remove(item);
//
//   What happens in list.RemoveItem():
//   1. If !item.IsNew:
//      - item.MarkDeleted() -> IsDeleted = true
//      - Add item to DeletedList
//   2. If item.IsNew:
//      - Just remove (never persisted, nothing to delete)
//   3. ContainingList stays set if it was set (for save routing)
//   4. Remove from collection
//
// SAVING THE AGGREGATE:
//   await order.Save();  // Order.IsModified = true (item removed)
//
//   In Order.Update():
//   1. Get the order row (with its item rows) from the repository
//   2. Map the order into its row if IsSelfModified
//   3. Delegate child persistence: orderItemListFactory.Save(Items, row.Items)
//   4. repository.SaveChanges() - one flush for the whole aggregate
//
//   In OrderItemList.Update() (list runs its own factory operation):
//   - Deleted, not new: its row is removed from row.Items (no child [Delete])
//   - New: a new OrderItemRow is added to row.Items, then
//     orderItemFactory.Save(item, newRow) -> routed to Insert
//   - Modified existing: orderItemFactory.Save(item, itsRow) -> Update
//   - Unmodified existing: skipped
//
//   FactoryComplete runs per factory target as each save completes:
//   - Each saved item: MarkUnmodified() + MarkOld()
//   - The list (FactoryOperation.Update): DeletedList cleared,
//     ContainingList cleared on deleted items, modified-cache recalculated
//   - The order: MarkUnmodified() + MarkOld()
//   There is NO graph-wide cascade - each object is cleaned by ITS factory
//   call. An update flow that writes the item rows itself, without per-item
//   factory saves, leaves child state dirty after save.
//
// INTRA-AGGREGATE MOVE:
//   // If Order had two lists (hypothetically)
//   var item = order.PendingItems[0];  // item.IsNew = false
//   order.PendingItems.Remove(item);   // item in PendingItems.DeletedList
//   order.CompletedItems.Add(item);    // item removed from DeletedList, UnDeleted
//
//   Result:
//   - item.IsDeleted = false
//   - item.ContainingList = CompletedItems
//   - item NOT in any DeletedList
//   - On Save: item is updated (moved), not deleted
// =============================================================================

// =============================================================================
// COMMON MISTAKE: Trying to save child entity directly.
//
// WRONG:
//   order.Items[0].ProductName = "New Name";
//   await order.Items[0].Save();
//   // Does not compile: IOrderItem (IEntityBase) has no Save()
//
// RIGHT:
//   order.Items[0].ProductName = "New Name";
//   await order.Save();  // Parent save handles child changes
//
// COMMON MISTAKE: Calling Delete() on a child and expecting the row to be gone.
//
// WRONG - stopping at the Delete() call:
//   order.Items[0].Delete();
//   // The row is NOT deleted yet. What actually happened:
//   // - Delete() delegated to list.Remove(this) for consistency
//   // - The item is marked deleted and moved to DeletedList
//   // - It is still in memory, still holding its data
//   // Nothing reaches persistence until the aggregate root is saved.
//
// RIGHT - Delete() marks, Save() persists:
//   order.Items[0].Delete();   // OR order.Items.Remove(order.Items[0])
//   await order.Save();        // NOW OrderItemList.Update removes its row
// =============================================================================
