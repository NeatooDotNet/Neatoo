// -----------------------------------------------------------------------------
// Design.Domain - OrderItemList (EntityListBase in Aggregate)
// -----------------------------------------------------------------------------
// This file documents EntityListBase with extensive DeletedList documentation.
// OrderItemList manages OrderItem entities within the Order aggregate, including
// the canonical child fetch and child persistence factory operations.
// -----------------------------------------------------------------------------

using Neatoo;
using Neatoo.RemoteFactory;

namespace Design.Domain.Aggregates.OrderAggregate;

/// <summary>
/// Demonstrates: EntityListBase&lt;I&gt; with DeletedList lifecycle management
/// and the canonical child fetch/persistence pattern.
///
/// Key points:
/// - IsModified = any child modified OR DeletedList.Any()
/// - DeletedList tracks removed non-new items
/// - Root references aggregate root (Order)
/// - Enforces aggregate boundaries on Add
/// - The list's own [Fetch] loads children; its own [Update] persists them
///   through per-item factory saves
/// </summary>
[Factory]
internal partial class OrderItemList : EntityListBase<IOrderItem>, IOrderItemList
{
    /// <summary>
    /// Test helper: Exposes the count of items in DeletedList.
    /// The DeletedList is protected, but tests need to verify deletion behavior.
    /// </summary>
    public int DeletedCount => DeletedList.Count;

    [Create]
    public void Create()
    {
        // Empty list
    }

    // =========================================================================
    // Canonical Child Fetch - Items Load Inside the LIST's Own Factory Op
    // =========================================================================
    // DESIGN DECISION: The list's own [Fetch] populates the list, and each item
    // comes from the ITEM factory's [Fetch]. The list is paused by its own
    // factory operation while items are added, so the adds are baseline loads:
    // nothing is marked modified. Each item completes its own [Fetch], so
    // IsNew=false and IsModified=false - the states Update routing depends on.
    //
    // COMMON MISTAKE: Loading children with itemFactory.Create() + LoadValue
    // inside the parent's [Fetch].
    //   var item = itemFactory.Create();     // FactoryComplete(Create) -> MarkNew()
    //   item["Id"].LoadValue(data.Id); ...   // properties clean, but...
    //   Items.Add(item);                     // item.IsNew is TRUE
    //   // On the next Save, Update routing sees IsNew=true and RE-INSERTS
    //   // every fetched item. Children must be loaded via [Fetch].
    // =========================================================================
    [Fetch]
    internal void Fetch(int orderId,
                        [Service] IOrderRepository repository,
                        [Service] IOrderItemFactory itemFactory)
    {
        foreach (var d in repository.GetItems(orderId))
        {
            Add(itemFactory.Fetch(d.Id, d.ProductName, d.Quantity, d.UnitPrice, d.LineTotal));
        }
    }

    // =========================================================================
    // Canonical Child Persistence - Per-Item Factory Saves
    // =========================================================================
    // Called (via the generated list factory Save) from Order.Insert and
    // Order.Update. The generated Save routes on the LIST's state - lists are
    // never new or deleted, so it always lands here.
    //
    // DESIGN DECISION: Every surviving child goes through the ITEM factory's
    // Save, which routes on the item's own IsNew (insert vs update) and wraps
    // the call with the item's FactoryStart/FactoryComplete - marking the item
    // unmodified and old as its save completes. Deleted children are removed
    // from persistence directly; they get no factory operation.
    //
    // GENERATOR BEHAVIOR: When this [Update] completes, the framework calls
    // FactoryComplete(FactoryOperation.Update) on the LIST, and
    // EntityListBase.FactoryComplete then clears the DeletedList, clears
    // ContainingList on the deleted items, and recalculates the cached
    // modified state. The cleanup happens because the list is saved through
    // its OWN factory operation - there is no graph-wide cascade.
    //
    // The IsNew/IsModified guard: unmodified existing children are skipped -
    // no repository write, no factory call (they are already clean). New
    // children and modified children go through Save.
    // =========================================================================
    [Update]
    internal void Update(int orderId,
                         [Service] IOrderRepository repository,
                         [Service] IOrderItemFactory itemFactory)
    {
        foreach (var item in this.Union(DeletedList).ToList())
        {
            if (item.IsDeleted)
            {
                // The !IsNew check is defensive: a new item removed from the
                // list is discarded (never enters DeletedList), so deleted
                // items reaching here are expected to be persisted ones.
                if (!item.IsNew)
                {
                    repository.DeleteItem(item.Id);
                }
            }
            else if (item.IsNew || item.IsModified)
            {
                itemFactory.Save(item, orderId);
            }
        }
    }
}

// =============================================================================
// DELETEDLIST LIFECYCLE - COMPLETE DOCUMENTATION
// =============================================================================
// The DeletedList is a protected List<I> that tracks items removed from the
// collection that need to be deleted from persistence.
//
// KEY INSIGHT: When you remove an item from EntityListBase, it doesn't
// immediately delete from the database. Instead:
// - If item was never persisted (IsNew=true): item is just discarded
// - If item exists in DB (IsNew=false): item goes to DeletedList
//
// The DeletedList is processed during the aggregate's Save operation.
// =============================================================================

// =============================================================================
// SCENARIO 1: REMOVE EXISTING ITEM
// =============================================================================
//
// Setup:
//   var order = await orderFactory.Fetch(1);
//   // order.Items has items loaded via the list's [Fetch]: all IsNew=false
//   var itemToRemove = order.Items[0];
//
// Remove:
//   order.Items.RemoveAt(0);
//   // OR: order.Items.Remove(itemToRemove);
//
// After Remove:
//   // itemToRemove: IsNew=false, IsDeleted=true
//   // order.Items.Count decremented
//   // order.Items.DeletedList.Count = 1
//   // order.Items.IsModified = true (because DeletedList.Any())
//   // order.IsModified = true (child list is modified)
//
// Save:
//   await order.Save();
//
// In Order.Update():
//   // Delegates to orderItemListFactory.Save(Items, Id)
//
// In OrderItemList.Update():
//   // Deleted item: repository.DeleteItem(id)
//   // Other items: per-item factory Save if new or modified
//
// In the LIST's FactoryComplete(Update):
//   // DeletedList.Clear()
//   // For each deleted item: SetContainingList(null)
//   // order.Items.IsModified = false (no deletedlist, no modified children)
// =============================================================================

// =============================================================================
// SCENARIO 2: REMOVE NEW ITEM (NEVER PERSISTED)
// =============================================================================
//
// Setup:
//   var order = await orderFactory.Fetch(1);
//   var newItem = orderItemFactory.Create("New Product", 5, 10.00m);
//   order.Items.Add(newItem);
//   // newItem: IsNew=true, IsChild=true, ContainingList=order.Items
//
// Remove:
//   order.Items.Remove(newItem);
//
// After Remove:
//   // newItem: IsNew=true, NOT in DeletedList (never persisted)
//   // newItem is just discarded - no persistence action needed
//   // order.Items.DeletedList.Count = 0
//
// Save:
//   await order.Save();
//   // No delete call for newItem - it was never in the database
// =============================================================================

// =============================================================================
// SCENARIO 3: ADD, MODIFY, REMOVE IN SAME SESSION
// =============================================================================
//
// Setup:
//   var order = await orderFactory.Fetch(1);
//   // Items[0]: IsNew=false (existing)
//
// Modify existing:
//   order.Items[0].Quantity = 100;
//   // Items[0]: IsNew=false, IsSelfModified=true
//
// Add new:
//   var newItem = orderItemFactory.Create("New", 1, 5.00m);
//   order.Items.Add(newItem);
//   // newItem: IsNew=true, IsChild=true
//
// Remove existing:
//   var removedItem = order.Items[1];
//   order.Items.Remove(removedItem);
//   // removedItem: IsNew=false, IsDeleted=true, in DeletedList
//
// State before Save:
//   // order.Items contains: modified item, new item
//   // order.Items.DeletedList contains: removed existing item
//   // order.IsModified = true
//
// Save:
//   await order.Save();
//   // In OrderItemList.Update():
//   //   - Items[0] (modified): itemFactory.Save -> routed to Update
//   //   - newItem (new): itemFactory.Save -> routed to Insert
//   //   - removedItem (deleted): repository.DeleteItem
//   // After: every surviving item IsNew=false, IsModified=false
// =============================================================================

// =============================================================================
// SCENARIO 4: INTRA-AGGREGATE MOVE (SAME ROOT, DIFFERENT LISTS)
// =============================================================================
// This scenario applies when an aggregate has multiple lists of the same type.
// Example: Order with PendingItems and CompletedItems lists.
//
// DESIGN DECISION: Moving items within the same aggregate doesn't delete.
// The item moves from one list to another without persistence delete.
//
// Setup (hypothetical - Order with two lists):
//   var order = await orderFactory.Fetch(1);
//   // order.PendingItems has item: IsNew=false, IsDeleted=false
//   // order.CompletedItems is empty
//   var item = order.PendingItems[0];
//
// Move:
//   order.PendingItems.Remove(item);
//   // item: IsDeleted=true, in PendingItems.DeletedList
//
//   order.CompletedItems.Add(item);
//   // In InsertItem():
//   //   1. Check item.Root == this.Root (same aggregate - OK)
//   //   2. item.ContainingList != null (was PendingItems)
//   //   3. PendingItems.DeletedList.Remove(item) - removed from delete list
//   //   4. item.UnDelete() -> IsDeleted = false
//   //   5. item.ContainingList = CompletedItems
//   //   6. Add to CompletedItems
//
// After Move:
//   // item: IsDeleted=false, ContainingList=CompletedItems
//   // PendingItems.DeletedList is empty
//   // On Save: item is updated (maybe with new location flag), NOT deleted
// =============================================================================

// =============================================================================
// SCENARIO 5: CROSS-AGGREGATE MOVE (BLOCKED)
// =============================================================================
// Items CANNOT move between different aggregates.
//
// Setup:
//   var order1 = await orderFactory.Fetch(1);
//   var order2 = await orderFactory.Fetch(2);
//   var item = order1.Items[0];
//   // item.Root = order1
//
// Attempt Move:
//   order1.Items.Remove(item);
//   // item in order1.Items.DeletedList
//
//   order2.Items.Add(item);  // THROWS InvalidOperationException!
//   // "Cannot add OrderItem to list: item belongs to aggregate 'Order',
//   //  but this list belongs to aggregate 'Order'."
//
// Why Blocked:
//   - item.Root = order1
//   - order2.Items.Root = order2
//   - item.Root != this.Root -> exception
//
// DESIGN DECISION: Aggregate boundaries are hard boundaries.
// Moving data between aggregates requires explicit copy-and-delete pattern:
//
// RIGHT WAY to "move" between aggregates:
//   var item = order1.Items[0];
//   var newItem = orderItemFactory.Create(
//       item.ProductName, item.Quantity, item.UnitPrice);
//   order2.Items.Add(newItem);
//   order1.Items.Remove(item);
//   await order1.Save();  // Deletes item from order1
//   await order2.Save();  // Inserts newItem in order2
// =============================================================================

// =============================================================================
// CONTAININGLIST LIFECYCLE
// =============================================================================
// ContainingList is a protected property on EntityBase that tracks which
// list contains the entity. It's used for:
//
// 1. Delete() routing: When you call entity.Delete(), if ContainingList is set,
//    it delegates to ContainingList.Remove(this) for consistency.
//
// 2. Intra-aggregate moves: When adding an item, if ContainingList is set,
//    the item is removed from old list's DeletedList.
//
// 3. Save routing: During aggregate save, ContainingList helps track which
//    list an item belongs to (even if removed).
//
// IMPORTANT: ContainingList stays SET when item is removed.
// It's only cleared in the list's FactoryComplete after successful persistence.
//
// IMPORTANT: ContainingList (and IsChild) are set by the LIVE add path -
// items loaded while the list is paused by its own [Fetch] do not currently
// get them (framework gap tracked as ISNEW-003).
//
// Timeline (live add):
//   Add to list      -> ContainingList = list
//   Remove from list -> ContainingList = list (stays set!)
//   Save completes   -> ContainingList = null (cleared)
// =============================================================================

// =============================================================================
// FACTORYCOMPLETE AND CLEANUP
// =============================================================================
// EntityListBase.FactoryComplete(FactoryOperation.Update) does cleanup:
//
//   - Clear ContainingList on deleted items
//   - Clear DeletedList
//   - Recalculate cached IsModified
//
// It runs because the list is saved through its OWN factory operation
// (orderItemListFactory.Save -> [Update] -> FactoryComplete on the list).
// Factory lifecycle hooks fire only on the single factory target: a parent's
// FactoryComplete does NOT cascade to lists or items. An aggregate whose
// update flow bypasses the list factory never triggers this cleanup - the
// DeletedList would survive the save and re-delete on the next one.
// =============================================================================
