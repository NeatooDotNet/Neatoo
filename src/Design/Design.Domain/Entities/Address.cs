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
    public partial int Id { get; set; }

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
    // [Fetch] - Called from AddressList.Fetch
    // =========================================================================
    // DESIGN DECISION: Child entities DO have their own [Fetch]. The list's
    // [Fetch] calls it per row, so every child completes its own factory
    // lifecycle and lands with correct persistence state: IsNew=false
    // (IsNew defaults to false; only a completed [Create] marks an entity new),
    // IsModified=false.
    //
    // Aggregate consistency is preserved by the SIGNATURE and visibility, not
    // by withholding the operation: this [Fetch] takes already-loaded row data
    // and is internal and non-[Remote], so no outside consumer can load an
    // address on its own, and the generated factory surface stays internal.
    //
    // COMMON MISTAKE: loading children with addressFactory.Create() + LoadValue
    // inside the parent's [Fetch]. Create marks the child NEW
    // (FactoryComplete(Create) -> MarkNew()) and nothing ever marks it old, so
    // the next Save RE-INSERTS every fetched child.
    //
    // GENERATOR BEHAVIOR: the object is paused for the duration of the method
    // body, so plain property assignment loads cleanly and rules do not fire.
    // =========================================================================
    [Fetch]
    internal void Fetch(int id, string street, string city, string state, string zipCode, string addressType)
    {
        Id = id;
        Street = street;
        City = city;
        State = state;
        ZipCode = zipCode;
        AddressType = addressType;
    }

    // =========================================================================
    // Child Insert/Update - Called (via the generated factory Save) from
    // AddressList.Update
    // =========================================================================
    // These are local (no [Remote]) and internal, and their signatures require
    // the parent's identity (employeeId) - which only the aggregate's save flow
    // can supply. That is what keeps child persistence inside the aggregate:
    // IAddress extends IEntityBase (no Save()), and the child factory's Save
    // needs a parameter an outside consumer does not have.
    //
    // GENERATOR BEHAVIOR: because Insert and Update share a parameter list, the
    // generated factory exposes a single Save(IAddress target, int employeeId)
    // that routes on the CHILD's own IsDeleted/IsNew. Each routed call wraps the
    // method with FactoryStart/FactoryComplete on the child, and
    // FactoryComplete(Insert/Update) calls MarkUnmodified() + MarkOld(). THAT is
    // how children come out clean after an aggregate save: per-item factory
    // saves, not a cascade.
    // =========================================================================
    [Insert]
    internal void Insert(int employeeId, [Service] IEmployeeRepository repository)
    {
        // Object is paused (factory operation) - plain assignment stays clean
        Id = repository.InsertAddress(employeeId, Street!, City!, State!, ZipCode!, AddressType!);
    }

    [Update]
    internal void Update(int employeeId, [Service] IEmployeeRepository repository)
    {
        // employeeId is unused here - it exists so Insert and Update share a
        // signature and the generator produces a single Save(target, employeeId)
        repository.UpdateAddress(Id, Street!, City!, State!, ZipCode!, AddressType!);
    }

    // =========================================================================
    // NO STANDALONE-ROOT OPERATIONS - and why that is a hard rule, not a
    // simplification
    // =========================================================================
    // DID NOT DO THIS: give Address a second, parent-less set of [Remote]
    // persistence operations so it could also be saved as its own aggregate
    // root ("entity duality").
    //
    // REJECTED PATTERN:
    //   [Remote] [Insert] internal void Insert([Service] IAddressOnlyRepository repo)
    //   [Remote] [Delete] internal void Delete([Service] IAddressOnlyRepository repo)
    //
    // WHY NOT - two independent reasons, either one decisive:
    //
    // 1. It punches a hole in the aggregate boundary. Operations whose
    //    signatures carry no parent identity make the generator emit a PUBLIC
    //    Save(IAddress target) on IAddressFactory. A consumer holding a child
    //    out of employee.Addresses could then persist or delete it directly,
    //    bypassing the aggregate's save flow entirely. The parent-scoped
    //    Save(target, employeeId) above cannot be misused that way: it is
    //    internal AND it demands an id the consumer has no business supplying.
    //    Compare the canonical: IOrderItemFactory exposes nothing public.
    //
    // 2. The interface already settled the question. IAddress extends
    //    IEntityBase, which is how a type declares "child, not root" - so no
    //    consumer can Save() it as a root regardless. A type that genuinely
    //    plays both roles needs a second interface extending IEntityRoot;
    //    keeping root-shaped operations behind a child-shaped interface just
    //    produces runtime NotImplementedExceptions from the generated routing.
    //
    // For the remote/local boundary aspects of dual-use types, see
    // FactoryOperations/RemoteBoundary.cs.
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
// later, when the parent assigns Addresses = addressListFactory.Fetch(id).
//
// When an Address is removed from AddressList:
// 1. RemoveItem() is called on the list
// 2. If address.IsNew = false:
//    - MarkDeleted() sets address.IsDeleted = true
//    - Address added to DeletedList
// 3. ContainingList stays set if it was set (for persistence routing)
//
// When Employee.Save() is called:
// 1. Employee's Insert/Update delegates to the LIST factory's Save
// 2. AddressList.Update deletes removed children from persistence and routes
//    new/modified children through the ADDRESS factory's Save
// 3. FactoryComplete fires per factory target: each saved address is marked
//    unmodified+old, the list clears its DeletedList, the employee is marked
//    unmodified+old. There is no graph-wide cascade.
// =============================================================================

// =============================================================================
// COMMON MISTAKE: Trying to save child entities directly.
//
// WRONG:
//   var employee = await employeeFactory.Fetch(1);
//   employee.Addresses[0].City = "Seattle";
//   await employee.Addresses[0].Save();
//   // Does not compile: IAddress (IEntityBase) has no Save()
//
// RIGHT:
//   var employee = await employeeFactory.Fetch(1);
//   employee.Addresses[0].City = "Seattle";
//   await employee.Save();  // Parent save handles all child changes
// =============================================================================

// IAddressOnlyRepository removed with the standalone-root operations it served
// (see the NO STANDALONE-ROOT OPERATIONS block above). Address is persisted
// exclusively through the Employee aggregate's save flow.
