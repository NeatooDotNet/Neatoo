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
    // [Fetch] - The List Loads Its Own Children
    // =========================================================================
    // DESIGN DECISION: the list has its own [Fetch], and it populates itself
    // through the ITEM factory's [Fetch]. Two things fall out of this:
    //
    // 1. The list is PAUSED by its own factory operation while items are added,
    //    so the adds are baseline loads - nothing is marked modified.
    // 2. Every child completes its own factory lifecycle, so each lands
    //    IsNew=false / IsModified=false - the states Update routing depends on.
    //
    // The aggregate root still controls WHEN children load: it calls this
    // through the list factory inside its own [Fetch]. The operation is
    // internal and non-[Remote], so no outside consumer can load the list.
    //
    // COMMON MISTAKE: populating the list from the parent's [Fetch] with
    // itemFactory.Create() + LoadValue. Create marks each child NEW, and
    // nothing marks it old - so the next Save re-INSERTS every fetched child.
    // =========================================================================
    [Fetch]
    internal void Fetch(int employeeId,
                        [Service] IEmployeeRepository repository,
                        [Service] IAddressFactory addressFactory)
    {
        foreach (var d in repository.GetAddresses(employeeId))
        {
            Add(addressFactory.Fetch(d.Id, d.Street, d.City, d.State, d.ZipCode, d.AddressType));
        }
    }

    // =========================================================================
    // [Update] - The List Coordinates Child Persistence
    // =========================================================================
    // Called (via the generated list factory Save) from Employee.Insert and
    // Employee.Update. The generated Save routes on the LIST's state - lists
    // are never new or deleted, so it always lands here.
    //
    // DESIGN DECISION: every surviving child goes through the ADDRESS factory's
    // Save, which routes on the child's own IsNew (insert vs update) and wraps
    // the call with the child's FactoryStart/FactoryComplete - marking it
    // unmodified and old as its save completes. Removed children are deleted
    // from persistence directly; they get no factory operation.
    //
    // GENERATOR BEHAVIOR: when this [Update] completes, the framework calls
    // FactoryComplete(FactoryOperation.Update) on the LIST, and
    // EntityListBase.FactoryComplete clears the DeletedList, clears
    // ContainingList on deleted items, and recalculates the cached modified
    // state. That cleanup happens because the list is saved through its OWN
    // factory operation - there is no graph-wide cascade from the parent.
    //
    // Unmodified existing children are skipped: no repository write, no factory
    // call (they are already clean).
    // =========================================================================
    [Update]
    internal void Update(int employeeId,
                         [Service] IEmployeeRepository repository,
                         [Service] IAddressFactory addressFactory)
    {
        foreach (var address in this.Union(DeletedList).ToList())
        {
            if (address.IsDeleted)
            {
                // Defensive: new addresses removed from the list are discarded,
                // never queued for deletion
                if (!address.IsNew)
                {
                    repository.DeleteAddress(address.Id);
                }
            }
            else if (address.IsNew || address.IsModified)
            {
                addressFactory.Save(address, employeeId);
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
//   // In Employee.Update(): delegates to addressListFactory.Save(Addresses, Id)
//   // In AddressList.Update(): repository.DeleteAddress(address.Id) called
//   // In the LIST's FactoryComplete(Update) - fired because the list is a
//   // factory target, never as a cascade from the parent:
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
