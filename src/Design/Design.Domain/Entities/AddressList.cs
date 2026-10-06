// -----------------------------------------------------------------------------
// Design.Domain - AddressList (EntityListBase Example)
// -----------------------------------------------------------------------------
// This file demonstrates EntityListBase for managing child entity collections.
// -----------------------------------------------------------------------------

using Neatoo;
using Neatoo.RemoteFactory;

namespace Design.Domain.Entities;

/// <summary>
/// Demonstrates: EntityListBase&lt;I&gt; for child entity collections, including
/// the canonical child fetch and child persistence factory operations.
///
/// Key points:
/// - IsModified aggregates from children + DeletedList.Any()
/// - DeletedList tracks removed items pending deletion
/// - Root property references aggregate root (Employee)
/// - Items added to a live list are marked as children
/// - The list's own [Fetch] loads children; its own [Update] persists them
/// </summary>
[Factory]
internal partial class AddressList : EntityListBase<IAddress>, IAddressList
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
    // [Fetch] - The List Loads Its Own Children From the Employee's Rows
    // =========================================================================
    // DESIGN DECISION: the list has its own [Fetch], and it populates itself
    // through the ITEM factory's [Fetch](row). Two things fall out of this:
    //
    // 1. The list is PAUSED by its own factory operation while items are added,
    //    so the adds are baseline loads - nothing is marked modified.
    // 2. Every child completes its own factory lifecycle, so each lands
    //    IsNew=false / IsModified=false - the states Update routing depends on.
    //
    // The aggregate root still controls WHEN children load: it calls this
    // through the list factory inside its own [Fetch], handing over the
    // employee row's Addresses. The operation is internal and non-[Remote], so
    // no outside consumer can load the list.
    //
    // COMMON MISTAKE: populating the list from the parent's [Fetch] with
    // itemFactory.Create() + LoadValue. Create marks each child NEW, and
    // nothing marks it old - so the next Save gives every fetched child a
    // second row.
    // =========================================================================
    [Fetch]
    internal void Fetch(IEnumerable<AddressRow> rows,
                        [Service] IAddressFactory addressFactory)
    {
        foreach (var row in rows)
        {
            Add(addressFactory.Fetch(row));
        }
    }

    // =========================================================================
    // [Update] - The List Brings the Employee's Address Rows in Line
    // =========================================================================
    // Called (via the generated list factory Save) from Employee.Insert and
    // Employee.Update with the employee row's Addresses collection. The
    // generated Save routes on the LIST's state - lists are never new or
    // deleted, so it always lands here. The employee flushes once after this
    // returns.
    //
    // For every address in the list and in DeletedList:
    // - Deleted, not new: find its row and REMOVE it from the collection. No
    //   child [Delete] exists - removing the row is the delete.
    // - New: make a new row, add it to the collection, and hand it to the
    //   ADDRESS factory's Save (routes to the address's [Insert]).
    // - Modified existing: find its row and hand it to the ADDRESS factory's
    //   Save (routes to the address's [Update]).
    // - Unmodified existing: skip - no write, no factory call (already clean).
    //
    // DESIGN DECISION: every surviving child that changed goes through the
    // ADDRESS factory's Save, which wraps the call with the child's
    // FactoryStart/FactoryComplete - marking it unmodified and old as its save
    // completes. The address maps itself into the row it is handed.
    //
    // GENERATOR BEHAVIOR: when this [Update] completes, the framework calls
    // FactoryComplete(FactoryOperation.Update) on the LIST, and
    // EntityListBase.FactoryComplete clears the DeletedList, clears
    // ContainingList on deleted items, and recalculates the cached modified
    // state. That cleanup happens because the list is saved through its OWN
    // factory operation - there is no graph-wide cascade from the parent.
    // =========================================================================
    [Update]
    internal void Update(ICollection<AddressRow> rows,
                         [Service] IAddressFactory addressFactory)
    {
        foreach (var address in this.Union(DeletedList))
        {
            if (address.IsDeleted)
            {
                // Defensive: new addresses removed from the list are discarded,
                // never queued for deletion
                if (!address.IsNew)
                {
                    rows.Remove(rows.Single(r => r.Id == address.Id));
                }
            }
            else if (address.IsNew)
            {
                var row = new AddressRow();
                rows.Add(row);
                addressFactory.Save(address, row);
            }
            else if (address.IsModified)
            {
                addressFactory.Save(address, rows.Single(r => r.Id == address.Id));
            }
        }
    }
}

// =============================================================================
// DeletedList Lifecycle (Detailed)
// =============================================================================
// The DeletedList is the key to how EntityListBase handles removed items.
//
// SCENARIO 1: Remove existing item (was fetched from DB)
//   var employee = await employeeFactory.Fetch(employeeId);
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
//   // In Employee.Update(): gets its row, delegates to
//   //   addressListFactory.Save(Addresses, row.Addresses), then SaveChanges()
//   // In AddressList.Update(): the address's row is removed from
//   //   row.Addresses - no child [Delete] runs
//   // In the LIST's FactoryComplete(Update) - fired because the list is a
//   // factory target, never as a cascade from the parent:
//   // - DeletedList.Clear()
//   // - ContainingList cleared on deleted items
//
// SCENARIO 2: Remove new item (never persisted)
//   var employee = await employeeFactory.Fetch(employeeId);
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
//   // No row to remove - newAddress was never persisted
//
// SCENARIO 3: Intra-aggregate move (item moves between lists)
//   var order = await orderFactory.Fetch(orderId);
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
//   // item moved - its row is not removed, just updated
// =============================================================================

// =============================================================================
// Aggregate Boundary Enforcement
// =============================================================================
// EntityListBase enforces aggregate boundaries:
//
// COMMON MISTAKE: Moving items between aggregates.
//
// WRONG:
//   var emp1 = await employeeFactory.Fetch(emp1Id);
//   var emp2 = await employeeFactory.Fetch(emp2Id);
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
