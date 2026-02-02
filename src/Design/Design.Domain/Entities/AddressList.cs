// -----------------------------------------------------------------------------
// Design.Domain - AddressList (EntityListBase Example)
// -----------------------------------------------------------------------------
// This file demonstrates EntityListBase for managing child entity collections.
// -----------------------------------------------------------------------------

using Neatoo;
using Neatoo.RemoteFactory;

namespace Design.Domain.Entities;

/// <summary>
/// Demonstrates: EntityListBase&lt;I&gt; for child entity collections.
///
/// Key points:
/// - IsModified aggregates from children + DeletedList.Any()
/// - DeletedList tracks removed items pending deletion
/// - Root property references aggregate root (Employee)
/// - Items marked as children when added
/// </summary>
[Factory]
public partial class AddressList : EntityListBase<Address>
{
    // =========================================================================
    // EntityListBase provides:
    // - IsModified: True if any child modified OR DeletedList has items
    // - IsSelfModified: Always false (lists don't have own properties)
    // - DeletedList: Protected collection of removed non-new items
    // - Root: Reference to aggregate root
    //
    // It extends ValidateListBase which provides:
    // - IsValid: All children are valid
    // - IsBusy: Any child is busy
    // - ObservableCollection<T> behavior
    // =========================================================================

    // =========================================================================
    // [Create] - Empty List
    // =========================================================================
    // Lists have simple Create - just initialize empty.
    // Items are added later through Add().
    // =========================================================================
    [Create]
    public void Create()
    {
        // Empty list - ready for Add() calls
    }

    // =========================================================================
    // No [Fetch] - Lists Don't Fetch Independently
    // =========================================================================
    // The parent's Fetch creates the list and populates it.
    //
    // DID NOT DO THIS: Give lists their own Fetch.
    //
    // REJECTED PATTERN:
    //   [Remote]
    //   [Fetch]
    //   public void Fetch(int employeeId, [Service] IAddressRepository repo) { ... }
    //
    // WHY NOT: The list is part of the aggregate. The aggregate root (Employee)
    // controls when and how child data is loaded.
    // =========================================================================

    // =========================================================================
    // No [Insert]/[Update]/[Delete] - Lists Don't Persist Directly
    // =========================================================================
    // The parent coordinates all child persistence in its Insert/Update/Delete.
    // Lists don't have their own Save() or persistence methods.
    // =========================================================================
}

// =============================================================================
// DeletedList Lifecycle (Detailed)
// =============================================================================
// The DeletedList is the key to how EntityListBase handles removed items.
//
// SCENARIO 1: Remove existing item (was fetched from DB)
//   var employee = await employeeFactory.Fetch(1);
//   // employee.Addresses[0].IsNew = false (came from DB)
//
//   employee.Addresses.RemoveAt(0);
//   // What happens:
//   // 1. address.MarkDeleted() -> address.IsDeleted = true
//   // 2. address added to DeletedList
//   // 3. address.ContainingList stays = Addresses (for routing)
//   // 4. address removed from main list
//
//   await employee.Save();
//   // In Employee.Update():
//   // - Repository.DeleteAddress(address.Id) called
//   // In FactoryComplete(Update):
//   // - DeletedList.Clear()
//   // - ContainingList cleared on deleted items
//
// SCENARIO 2: Remove new item (never persisted)
//   var employee = await employeeFactory.Fetch(1);
//   var newAddress = addressFactory.Create();
//   employee.Addresses.Add(newAddress);
//   // newAddress.IsNew = true
//
//   employee.Addresses.Remove(newAddress);
//   // What happens:
//   // 1. newAddress.IsNew = true, so NO DeletedList entry
//   // 2. newAddress just discarded
//
//   await employee.Save();
//   // Nothing to delete - newAddress was never persisted
//
// SCENARIO 3: Intra-aggregate move (item moves between lists)
//   var order = await orderFactory.Fetch(1);
//   var item = order.ActiveItems[0];
//   // item.IsNew = false
//
//   order.ActiveItems.Remove(item);
//   // item in ActiveItems.DeletedList
//   // item.IsDeleted = true
//
//   order.CompletedItems.Add(item);
//   // What happens in InsertItem:
//   // 1. item.ContainingList != null (was ActiveItems)
//   // 2. item.Root == this.Root (same aggregate)
//   // 3. ActiveItems.DeletedList.Remove(item)
//   // 4. item.UnDelete() -> item.IsDeleted = false
//   // 5. item.ContainingList = CompletedItems
//   // 6. item added to CompletedItems
//
//   await order.Save();
//   // item moved - no delete, just update location
// =============================================================================

// =============================================================================
// Aggregate Boundary Enforcement
// =============================================================================
// EntityListBase enforces aggregate boundaries:
//
// COMMON MISTAKE: Moving items between aggregates.
//
// WRONG:
//   var emp1 = await employeeFactory.Fetch(1);
//   var emp2 = await employeeFactory.Fetch(2);
//   var address = emp1.Addresses[0];
//   emp2.Addresses.Add(address);  // THROWS!
//   // "Cannot add Address to list: item belongs to aggregate 'Employee',
//   //  but this list belongs to aggregate 'Employee'."
//
// WHY: Different aggregate roots. Moving items between aggregates would
// create inconsistent state - the address would be in two places.
//
// RIGHT: If you need to move data between aggregates:
//   var address = emp1.Addresses[0];
//   var newAddress = addressFactory.Create(
//       address.Street, address.City, address.State, address.ZipCode, address.AddressType);
//   emp2.Addresses.Add(newAddress);
//   emp1.Addresses.Remove(address);
//   await emp1.Save();
//   await emp2.Save();
// =============================================================================
