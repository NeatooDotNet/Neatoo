// -----------------------------------------------------------------------------
// Design.Domain - OrderItemList (EntityListBase in Aggregate)
// -----------------------------------------------------------------------------
// This file documents EntityListBase with extensive DeletedList documentation.
// OrderItemList manages OrderItem entities within the Order aggregate.
// -----------------------------------------------------------------------------

using Neatoo;
using Neatoo.RemoteFactory;

namespace Design.Domain.Aggregates.OrderAggregate;

/// <summary>
/// Demonstrates: EntityListBase&lt;I&gt; with DeletedList lifecycle management.
///
/// Key points:
/// - IsModified = any child modified OR DeletedList.Any()
/// - DeletedList tracks removed non-new items
/// - Root references aggregate root (Order)
/// - Enforces aggregate boundaries on Add
/// - Coordinates FactoryComplete cleanup
/// </summary>
[Factory]
public partial class OrderItemList : EntityListBase<OrderItem>
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
//   // order.Items has 3 items, all IsNew=false (from DB)
//   var itemToRemove = order.Items[0];
//   // itemToRemove: IsNew=false, IsDeleted=false, ContainingList=order.Items
//
// Remove:
//   order.Items.RemoveAt(0);
//   // OR: order.Items.Remove(itemToRemove);
//   // OR: itemToRemove.Delete();  // Delegates to list.Remove()
//
// After Remove:
//   // itemToRemove: IsNew=false, IsDeleted=true, ContainingList=order.Items
//   // order.Items.Count = 2
//   // order.Items.DeletedList.Count = 1
//   // order.Items.DeletedList[0] == itemToRemove
//   // order.Items.IsModified = true (because DeletedList.Any())
//   // order.IsModified = true (child list is modified)
//
// Save:
//   await order.Save();
//
// In Order.Update():
//   // Iterate Items - process active items (insert new, update modified)
//   // Iterate DeletedList - call repository.DeleteItem(id) for each
//
// In FactoryComplete(Update):
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
//   // In Update():
//   //   - Items[0] (modified): call UpdateItem
//   //   - newItem (new): call InsertItem
//   //   - removedItem (deleted): call DeleteItem
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
// It's only cleared in FactoryComplete after successful persistence.
//
// Timeline:
//   Add to list     -> ContainingList = list
//   Remove from list -> ContainingList = list (stays set!)
//   Save completes  -> ContainingList = null (cleared)
// =============================================================================

// =============================================================================
// FACTORYCOMPLETE AND CLEANUP
// =============================================================================
// EntityListBase.FactoryComplete(FactoryOperation.Update) does cleanup:
//
// protected override void FactoryComplete(FactoryOperation factoryOperation)
// {
//     base.FactoryComplete(factoryOperation);
//
//     if (factoryOperation == FactoryOperation.Update)
//     {
//         // Clear ContainingList on deleted items
//         foreach (var item in DeletedList)
//         {
//             ((IEntityBaseInternal)item).SetContainingList(null);
//         }
//
//         // Clear DeletedList
//         DeletedList.Clear();
//
//         // Recalculate cached IsModified
//         _cachedChildrenModified = this.Any(c => c.IsModified);
//     }
// }
// =============================================================================
