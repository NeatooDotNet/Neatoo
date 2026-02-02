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
/// - IsChild=true when in OrderItemList (cannot save independently)
/// - ContainingList tracks which list owns this item
/// - Root property points to Order (aggregate root)
/// - Delete/UnDelete managed by list operations
/// </summary>
[Factory]
public partial class OrderItem : EntityBase<OrderItem>
{
    public partial int Id { get; set; }

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
    // Child Entity State Properties
    // =========================================================================
    // When OrderItem is in a list:
    //
    // IsChild = true
    //   - Set by list.Add() calling MarkAsChild()
    //   - Makes IsSavable = false (cannot save independently)
    //
    // ContainingList = the OrderItemList
    //   - Set by list.Add() calling SetContainingList()
    //   - Used for Delete() routing and intra-aggregate moves
    //   - Stays set when removed (until FactoryComplete clears it)
    //
    // Root = the Order
    //   - Computed from Parent chain
    //   - Used for aggregate boundary enforcement
    //
    // IsDeleted = true when removed from list (and not new)
    //   - Set by list.Remove() calling MarkDeleted()
    //   - Item goes to DeletedList
    //   - Order's Update method will delete from DB
    // =========================================================================

    // =========================================================================
    // No Insert/Update/Delete with [Remote]
    // =========================================================================
    // Child entities don't have their own remote persistence methods.
    // The parent (Order) handles all persistence.
    //
    // DID NOT DO THIS: Give child entities independent [Remote] persistence.
    //
    // REJECTED PATTERN:
    //   [Remote]
    //   [Insert]
    //   public void Insert([Service] IOrderItemRepository repo) { ... }
    //
    // WHY NOT: This would allow calling orderItem.Save() which shouldn't work.
    // Child entities are part of the aggregate - persistence goes through root.
    //
    // These empty methods exist only for the interface contract.
    // The Order's Insert/Update/Delete does the actual persistence.
    // =========================================================================
}

// =============================================================================
// Child Entity Lifecycle - Detailed Documentation
// =============================================================================
//
// ADDING NEW ITEM:
//   var item = orderItemFactory.Create("Widget", 5, 10.00m);
//   order.Items.Add(item);
//
//   What happens in list.InsertItem():
//   1. Check: item not already in list (throws if duplicate)
//   2. Check: item not busy (throws if IsBusy=true)
//   3. Check: item.Root == this.Root OR item.Root == null
//      (throws if item belongs to different aggregate)
//   4. If item.ContainingList != null (was in another list):
//      - Remove from old list's DeletedList (intra-aggregate move)
//   5. If item.IsDeleted: item.UnDelete()
//   6. If !item.IsNew: item.MarkModified()
//   7. item.MarkAsChild() -> IsChild = true
//   8. item.SetContainingList(this)
//   9. Add to collection
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
//   3. ContainingList stays set (for save routing)
//   4. Remove from collection
//
// SAVING THE AGGREGATE:
//   await order.Save();  // Order.IsModified = true (item removed)
//
//   In Order.Update():
//   1. Update order header if IsSelfModified
//   2. For each item in Items:
//      - If item.IsNew: Insert
//      - If item.IsSelfModified && !item.IsNew: Update
//   3. For each item in Items.DeletedList:
//      - Call repository.DeleteItem()
//
//   In FactoryComplete(Update):
//   1. MarkUnmodified() on Order
//   2. For each item: MarkUnmodified()
//   3. DeletedList.Clear()
//   4. For each deleted item: SetContainingList(null)
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
//   // THROWS: SaveOperationException(SaveFailureReason.IsChildObject)
//   // Because: order.Items[0].IsChild = true -> IsSavable = false
//
// RIGHT:
//   order.Items[0].ProductName = "New Name";
//   await order.Save();  // Parent save handles child changes
//
// COMMON MISTAKE: Calling Delete() on child expecting immediate DB delete.
//
// WRONG:
//   order.Items[0].Delete();
//   // Expecting item is deleted from DB - IT IS NOT
//   // What actually happens:
//   // - Delete() delegates to list.Remove() (because ContainingList is set)
//   // - Item is marked deleted and goes to DeletedList
//   // - Item is still in memory, still has data
//
// RIGHT:
//   order.Items[0].Delete();  // OR order.Items.Remove(item)
//   await order.Save();        // NOW the [Delete] is called
// =============================================================================
