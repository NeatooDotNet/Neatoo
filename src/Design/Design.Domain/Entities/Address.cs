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
/// - Part of Employee aggregate (IsChild=true when in AddressList)
/// - Cannot call Save() directly (IsSavable always false when IsChild)
/// - Insert/Update/Delete called by parent's persistence methods
/// - Has own validation rules
/// </summary>
[Factory]
public partial class Address : EntityBase<Address>
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
    // No [Fetch] - Child Entities
    // =========================================================================
    // Child entities don't have their own Fetch.
    // They're populated by the parent's Fetch method.
    //
    // DID NOT DO THIS: Give child entities their own Fetch.
    //
    // REJECTED PATTERN:
    //   [Remote]
    //   [Fetch]
    //   public void Fetch(int id, [Service] IAddressRepository repo) { ... }
    //
    // WHY NOT: Child entities are part of an aggregate. Fetching them
    // independently would break aggregate consistency. The parent's
    // Fetch loads all children together to ensure consistent state.
    // =========================================================================

    // =========================================================================
    // Insert/Update/Delete - Called by Parent
    // =========================================================================
    // These methods are NOT called through the factory when Address is a child.
    // The parent (Employee) calls the repository directly in its Insert/Update/Delete.
    //
    // These methods exist for when Address is used as an aggregate root
    // (entity duality - same class, different contexts).
    // =========================================================================
    [Remote]
    [Insert]
    public void Insert([Service] IAddressOnlyRepository repository)
    {
        // Only called if Address is used as aggregate root (rare)
        var generatedId = repository.Insert(Street!, City!, State!, ZipCode!, AddressType!);
        this["Id"].LoadValue(generatedId);
    }

    [Remote]
    [Update]
    public void Update([Service] IAddressOnlyRepository repository)
    {
        repository.Update(Id, Street!, City!, State!, ZipCode!, AddressType!);
    }

    [Remote]
    [Delete]
    public void Delete([Service] IAddressOnlyRepository repository)
    {
        repository.Delete(Id);
    }
}

// =============================================================================
// Child Entity Lifecycle
// =============================================================================
// When an Address is added to AddressList:
// 1. InsertItem() is called on the list
// 2. MarkAsChild() sets address.IsChild = true
// 3. SetContainingList() tracks ownership
// 4. If address was deleted, UnDelete() is called
//
// When an Address is removed from AddressList:
// 1. RemoveItem() is called on the list
// 2. If address.IsNew = false:
//    - MarkDeleted() sets address.IsDeleted = true
//    - Address added to DeletedList
// 3. ContainingList stays set (for persistence routing)
//
// When Employee.Save() is called:
// 1. Employee's Insert/Update/Delete iterates Addresses
// 2. For deleted addresses, iterates DeletedList
// 3. After successful save, DeletedList is cleared
// =============================================================================

// =============================================================================
// COMMON MISTAKE: Trying to save child entities directly.
//
// WRONG:
//   var employee = await employeeFactory.Fetch(1);
//   employee.Addresses[0].City = "Seattle";
//   await employee.Addresses[0].Save();  // THROWS SaveOperationException!
//   // SaveFailureReason.IsChildObject
//
// RIGHT:
//   var employee = await employeeFactory.Fetch(1);
//   employee.Addresses[0].City = "Seattle";
//   await employee.Save();  // Parent save handles all child changes
// =============================================================================

public interface IAddressOnlyRepository
{
    int Insert(string street, string city, string state, string zipCode, string addressType);
    void Update(int id, string street, string city, string state, string zipCode, string addressType);
    void Delete(int id);
}
