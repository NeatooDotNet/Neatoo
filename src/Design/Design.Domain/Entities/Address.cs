// -----------------------------------------------------------------------------
// Design.Domain - Address Entity (Child Entity Example)
// -----------------------------------------------------------------------------
// This file demonstrates a child entity that is part of an aggregate.
// Address cannot save independently - it's saved through its parent Employee.
// -----------------------------------------------------------------------------

using Neatoo;
using Neatoo.RemoteFactory;
using System.ComponentModel.DataAnnotations;

namespace Design.Domain.Entities;

/// <summary>
/// Demonstrates: Child entity within an aggregate.
///
/// Key points:
/// - Part of Employee aggregate (added to an AddressList, which owns it)
/// - Cannot be saved by consumers (IAddress has no Save(); child persistence
///   operations require the parent's identity)
/// - Loads through its own [Fetch], persists through its own [Insert]/[Update],
///   both driven by AddressList
/// - Has own validation rules
/// </summary>
[Factory]
internal partial class Address : EntityBase<Address>, IAddress
{
    public partial Guid Id { get; set; }

    #region skill-validation-attributes-and-rules
    [Required(ErrorMessage = "Street is required")]
    [StringLength(100)]
    public partial string? Street { get; set; }

    [Required(ErrorMessage = "City is required")]
    [StringLength(50)]
    public partial string? City { get; set; }

    [Required(ErrorMessage = "State is required")]
    [StringLength(2, MinimumLength = 2, ErrorMessage = "State must be 2 characters")]
    public partial string? State { get; set; }

    [Required(ErrorMessage = "Zip code is required")]
    [RegularExpression(@"^\d{5}(-\d{4})?$", ErrorMessage = "Invalid zip code format")]
    public partial string? ZipCode { get; set; }

    [Required(ErrorMessage = "Address type is required")]
    public partial string? AddressType { get; set; } // "Home", "Work", "Other"

    public Address(IEntityBaseServices<Address> services) : base(services)
    {
        // Validation rules
        RuleManager.AddValidation(
            t => !new[] { "Home", "Work", "Other" }.Contains(t.AddressType)
                ? "Address type must be Home, Work, or Other"
                : string.Empty,
            t => t.AddressType);
    }
    #endregion

    // =========================================================================
    // [Create] - Initialize New Address
    // =========================================================================
    // Used when adding a new address to an existing Employee.
    // Example: employee.Addresses.Add(addressFactory.Create())
    // =========================================================================
    [Create]
    public void Create()
    {
        AddressType = "Home";  // Default
    }

    [Create]
    public void Create(string street, string city, string state, string zipCode, string addressType)
    {
        Street = street;
        City = city;
        State = state;
        ZipCode = zipCode;
        AddressType = addressType;
    }

    // =========================================================================
    // [Fetch] - Called from AddressList.Fetch With This Address's Row
    // =========================================================================
    // DESIGN DECISION: Child entities DO have their own [Fetch]. The list's
    // [Fetch] calls it per row, so every child completes its own factory
    // lifecycle and lands with correct persistence state: IsNew=false
    // (IsNew defaults to false; only a completed [Create] marks an entity new),
    // IsModified=false.
    //
    // Aggregate consistency is preserved by the SIGNATURE and visibility, not
    // by withholding the operation: this [Fetch] takes an already-loaded row
    // and is internal and non-[Remote], so no outside consumer can load an
    // address on its own, and the generated factory surface stays internal.
    //
    // COMMON MISTAKE: loading children with addressFactory.Create() + LoadValue
    // inside the parent's [Fetch]. Create marks the child NEW
    // (FactoryComplete(Create) -> MarkNew()) and nothing ever marks it old, so
    // the next Save gives every fetched child a second row.
    //
    // GENERATOR BEHAVIOR: the object is paused for the duration of the method
    // body, so plain property assignment loads cleanly and rules do not fire.
    // =========================================================================
    [Fetch]
    internal void Fetch(AddressRow row)
    {
        Id = row.Id;
        Street = row.Street;
        City = row.City;
        State = row.State;
        ZipCode = row.ZipCode;
        AddressType = row.AddressType;
    }

    // =========================================================================
    // Child Insert/Update - Called (via the generated factory Save) from
    // AddressList.Update With This Address's Row
    // =========================================================================
    // The address maps itself: AddressList.Update finds (or, for a new
    // address, makes and adds) its row in the employee row's Addresses
    // collection and passes it here. No repository is involved - the employee
    // flushes once after the list is done.
    //
    // These are local (no [Remote]) and internal, and their signatures take
    // the address's row - which only the list's [Update] can supply. That is
    // what keeps child persistence inside the aggregate: IAddress extends
    // IEntityBase (no Save()), and the child factory's Save needs a row an
    // outside consumer does not have.
    //
    // GENERATOR BEHAVIOR: because Insert and Update share a parameter list, the
    // generated factory exposes a single Save(IAddress target, AddressRow row)
    // that routes on the CHILD's own IsDeleted/IsNew. Each routed call wraps the
    // method with FactoryStart/FactoryComplete on the child, and
    // FactoryComplete(Insert/Update) calls MarkUnmodified() + MarkOld(). THAT is
    // how children come out clean after an aggregate save: per-item factory
    // saves, not a cascade.
    //
    // No [Delete]: a removed address's row is removed by AddressList.Update.
    // =========================================================================
    [Insert]
    internal void Insert(AddressRow row)
    {
        // The entity sets its own key. Paused - plain assignment stays clean.
        Id = Guid.NewGuid();
        MapTo(row);
    }

    [Update]
    internal void Update(AddressRow row)
    {
        MapTo(row);
    }

    private void MapTo(AddressRow row)
    {
        row.Id = Id;
        row.Street = Street!;
        row.City = City!;
        row.State = State!;
        row.ZipCode = ZipCode!;
        row.AddressType = AddressType!;
    }

    // =========================================================================
    // CHILD-ONLY BY CHOICE
    // =========================================================================
    // Address has no parent-less operations, so it can be saved only through
    // an Employee. That is a modelling choice, not a framework rule: an entity
    // type may also play both roles. If an address screen ever
    // needed to save an address on its own, this class would gain [Remote]
    // root operations beside the row-taking child operations above, and
    // IAddress would extend IEntityRoot instead of IEntityBase. The factory
    // method signatures tell the two roles apart; see
    // FactoryOperations/RemoteBoundary.cs for the both-roles shape.
    //
    // What child-only buys: IAddress extends IEntityBase, so no consumer can
    // call Save() on an address, and the generated IAddressFactory exposes no
    // public Save(target) - only the row-scoped Save(target, row) the list's
    // [Update] uses. Choose it when the type has no life outside its parent.
    // =========================================================================
}

// =============================================================================
// Child Entity Lifecycle
// =============================================================================
// When an Address is added to a LIVE AddressList (user flow, list not paused):
// 1. InsertItem() is called on the list
// 2. SetContainingList() tracks ownership
// 3. If address was deleted, UnDelete() is called
//
// When addresses are loaded by AddressList.Fetch, the list is paused by its own
// factory operation. The paused add path skips the DIRT-producing steps but
// still applies child identity (step 2), so fetched addresses have a
// ContainingList - address.Delete() routes through the list exactly as it
// does for a live add. Parent/Root are established one step
// later, when the parent assigns Addresses = addressListFactory.Fetch(row.Addresses).
//
// When an Address is removed from AddressList:
// 1. RemoveItem() is called on the list
// 2. If address.IsNew = false:
//    - MarkDeleted() sets address.IsDeleted = true
//    - Address added to DeletedList
// 3. ContainingList stays set if it was set (for persistence routing)
//
// When Employee.Save() is called:
// 1. Employee's Insert/Update hands its row's Addresses collection to the
//    LIST factory's Save, then flushes once (repository.SaveChanges())
// 2. AddressList.Update removes the rows of removed (persisted) addresses
//    from the collection - no child [Delete] runs - and routes new/modified
//    addresses through the ADDRESS factory's Save with their row (a new
//    address gets a new row first); unmodified addresses are skipped
// 3. FactoryComplete fires per factory target: each saved address is marked
//    unmodified+old, the list clears its DeletedList, the employee is marked
//    unmodified+old. There is no graph-wide cascade.
// =============================================================================

// =============================================================================
// COMMON MISTAKE: Trying to save child entities directly.
//
// WRONG:
//   var employee = await employeeFactory.Fetch(employeeId);
//   employee.Addresses[0].City = "Seattle";
//   await employee.Addresses[0].Save();
//   // Does not compile: IAddress (IEntityBase) has no Save()
//
// RIGHT:
//   var employee = await employeeFactory.Fetch(employeeId);
//   employee.Addresses[0].City = "Seattle";
//   await employee.Save();  // Parent save handles all child changes
// =============================================================================

// IAddressOnlyRepository removed with the standalone-root operations it served
// (see the NO STANDALONE-ROOT OPERATIONS block above). Address is persisted
// exclusively through the Employee aggregate's save flow.
