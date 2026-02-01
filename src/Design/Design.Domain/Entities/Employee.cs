// -----------------------------------------------------------------------------
// Design.Domain - Employee Entity (Full CRUD Example)
// -----------------------------------------------------------------------------
// This file demonstrates a complete EntityBase implementation with all
// factory operations, validation rules, and child entity management.
// -----------------------------------------------------------------------------

using Neatoo;
using Neatoo.RemoteFactory;
using System.ComponentModel.DataAnnotations;

namespace Design.Domain.Entities;

/// <summary>
/// Demonstrates: Complete EntityBase&lt;T&gt; with full CRUD lifecycle.
///
/// Key points:
/// - Partial properties with validation attributes
/// - Child entity collection (Addresses)
/// - All factory operations: Create, Fetch, Insert, Update, Delete
/// - Validation rules (fluent API)
/// - Aggregate root pattern (owns Addresses collection)
/// </summary>
[Factory]
public partial class Employee : EntityBase<Employee>
{
    // =========================================================================
    // Partial Properties
    // =========================================================================
    // GENERATOR BEHAVIOR: Each partial property generates:
    // - Backing field: IEntityProperty<T> _propertyNameProperty
    // - Property implementation with get/set calling property methods
    // - Registration in InitializePropertyBackingFields
    // =========================================================================

    public partial int Id { get; set; }

    [Required(ErrorMessage = "First name is required")]
    [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
    public partial string? FirstName { get; set; }

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
    public partial string? LastName { get; set; }

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public partial string? Email { get; set; }

    public partial DateTime? HireDate { get; set; }

    public partial decimal Salary { get; set; }

    public partial bool IsActive { get; set; }

    // =========================================================================
    // Child Collection
    // =========================================================================
    // The Employee owns an AddressList. When Employee saves:
    // - New addresses are inserted
    // - Modified addresses are updated
    // - Removed addresses are deleted (from DeletedList)
    //
    // DESIGN DECISION: Collections are nullable properties, initialized in Create/Fetch.
    // This allows factory methods to create the collection with proper DI.
    // =========================================================================
    public partial AddressList? Addresses { get; set; }

    // =========================================================================
    // Computed Property (not persisted)
    // =========================================================================
    // This is a regular property, not partial - not tracked by Neatoo.
    // Use for UI display or computed values.
    // =========================================================================
    public string FullName => $"{FirstName} {LastName}";

    // =========================================================================
    // Constructor - Service Injection and Rules
    // =========================================================================
    public Employee(IEntityBaseServices<Employee> services) : base(services)
    {
        // Validation rules using fluent API
        RuleManager.AddValidation(
            t => t.Salary < 0 ? "Salary cannot be negative" : string.Empty,
            t => t.Salary);

        RuleManager.AddValidation(
            t => t.HireDate > DateTime.Today ? "Hire date cannot be in the future" : string.Empty,
            t => t.HireDate);

        // Action rule to set default values
        RuleManager.AddAction(
            t =>
            {
                if (t.HireDate == null && t.IsNew)
                {
                    t.HireDate = DateTime.Today;
                }
            },
            t => t.IsActive);
    }

    // =========================================================================
    // [Create] - Initialize New Employee
    // =========================================================================
    // No [Remote] - runs on client or server.
    // Creates empty Addresses collection for new employees.
    // =========================================================================
    [Create]
    public void Create([Service] IAddressListFactory addressListFactory)
    {
        Addresses = addressListFactory.Create();
        IsActive = true;  // Default new employees to active
    }

    // =========================================================================
    // [Fetch] - Load Existing Employee
    // =========================================================================
    // Has [Remote] - requires database access.
    // Loads employee data and all addresses.
    // =========================================================================
    [Remote]
    [Fetch]
    public void Fetch(int id,
        [Service] IEmployeeRepository repository,
        [Service] IAddressListFactory addressListFactory,
        [Service] IAddressFactory addressFactory)
    {
        using (PauseAllActions())
        {
            var data = repository.GetById(id);

            this["Id"].LoadValue(data.Id);
            this["FirstName"].LoadValue(data.FirstName);
            this["LastName"].LoadValue(data.LastName);
            this["Email"].LoadValue(data.Email);
            this["HireDate"].LoadValue(data.HireDate);
            this["Salary"].LoadValue(data.Salary);
            this["IsActive"].LoadValue(data.IsActive);

            // Create and populate addresses
            Addresses = addressListFactory.Create();
            foreach (var addrData in repository.GetAddresses(id))
            {
                var address = addressFactory.Create();
                address["Id"].LoadValue(addrData.Id);
                address["Street"].LoadValue(addrData.Street);
                address["City"].LoadValue(addrData.City);
                address["State"].LoadValue(addrData.State);
                address["ZipCode"].LoadValue(addrData.ZipCode);
                address["AddressType"].LoadValue(addrData.AddressType);

                Addresses.Add(address);
                // Add sets: address.IsChild=true, address.ContainingList=Addresses
            }
        }
        // After Fetch: IsNew=false, IsModified=false for Employee and all Addresses
    }

    // =========================================================================
    // [Insert] - Persist New Employee
    // =========================================================================
    // Called by Save() when IsNew=true.
    // Inserts employee, then inserts all addresses.
    // =========================================================================
    [Remote]
    [Insert]
    public void Insert([Service] IEmployeeRepository repository)
    {
        // Insert employee and get generated ID
        var generatedId = repository.InsertEmployee(
            FirstName!, LastName!, Email!, HireDate, Salary, IsActive);
        this["Id"].LoadValue(generatedId);

        // Insert all addresses (all are new for a new employee)
        foreach (var address in Addresses!)
        {
            var addrId = repository.InsertAddress(
                Id, address.Street!, address.City!, address.State!, address.ZipCode!, address.AddressType!);
            address["Id"].LoadValue(addrId);
        }

        // FactoryComplete(Insert) will call MarkUnmodified() and MarkOld()
    }

    // =========================================================================
    // [Update] - Persist Changes
    // =========================================================================
    // Called by Save() when IsNew=false && IsModified=true.
    // Updates employee, handles address insert/update/delete.
    // =========================================================================
    [Remote]
    [Update]
    public void Update([Service] IEmployeeRepository repository)
    {
        // Update employee if self modified
        if (IsSelfModified)
        {
            repository.UpdateEmployee(Id, FirstName!, LastName!, Email!, HireDate, Salary, IsActive);
        }

        // Process addresses
        foreach (var address in Addresses!)
        {
            if (address.IsNew)
            {
                var addrId = repository.InsertAddress(
                    Id, address.Street!, address.City!, address.State!, address.ZipCode!, address.AddressType!);
                address["Id"].LoadValue(addrId);
            }
            else if (address.IsSelfModified)
            {
                repository.UpdateAddress(
                    address.Id, address.Street!, address.City!, address.State!, address.ZipCode!, address.AddressType!);
            }
        }

        // Process deleted addresses (those removed from list)
        // Note: DeletedList is accessed through internal mechanism in real code
        // This demonstrates the pattern
        ProcessDeletedAddresses(repository);

        // FactoryComplete(Update) will call MarkUnmodified() and clear DeletedList
    }

    private void ProcessDeletedAddresses(IEmployeeRepository repository)
    {
        // In real implementation, this iterates Addresses.DeletedList
        // and calls repository.DeleteAddress for each
        // The framework handles this through FactoryComplete
    }

    // =========================================================================
    // [Delete] - Remove from Persistence
    // =========================================================================
    // Called by Save() when IsDeleted=true && IsNew=false.
    // Deletes addresses first (FK constraint), then employee.
    // =========================================================================
    [Remote]
    [Delete]
    public void Delete([Service] IEmployeeRepository repository)
    {
        // Delete all addresses first (FK constraint)
        foreach (var address in Addresses!)
        {
            if (!address.IsNew)  // Only delete persisted addresses
            {
                repository.DeleteAddress(address.Id);
            }
        }

        // Delete employee
        repository.DeleteEmployee(Id);
    }
}

// =============================================================================
// Repository Interface
// =============================================================================

public interface IEmployeeRepository
{
    (int Id, string FirstName, string LastName, string Email, DateTime? HireDate, decimal Salary, bool IsActive) GetById(int id);
    IEnumerable<(int Id, string Street, string City, string State, string ZipCode, string AddressType)> GetAddresses(int employeeId);

    int InsertEmployee(string firstName, string lastName, string email, DateTime? hireDate, decimal salary, bool isActive);
    void UpdateEmployee(int id, string firstName, string lastName, string email, DateTime? hireDate, decimal salary, bool isActive);
    void DeleteEmployee(int id);

    int InsertAddress(int employeeId, string street, string city, string state, string zipCode, string addressType);
    void UpdateAddress(int id, string street, string city, string state, string zipCode, string addressType);
    void DeleteAddress(int id);
}
