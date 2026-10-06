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
internal partial class Employee : EntityBase<Employee>, IEmployee
{
    // =========================================================================
    // Partial Properties
    // =========================================================================
    // GENERATOR BEHAVIOR: Each partial property generates:
    // - Backing field: IEntityProperty<T> _propertyNameProperty
    // - Property implementation with get/set calling property methods
    // - Registration in InitializePropertyBackingFields
    // =========================================================================

    public partial Guid Id { get; set; }

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
    // The Employee owns an AddressList. When Employee saves, it hands its
    // row's Addresses collection to the list factory's Save:
    // - New addresses get a new row, added to the collection
    // - Modified addresses write themselves to their existing row
    // - Removed addresses (from DeletedList) have their row removed
    //
    // DESIGN DECISION: Collections are nullable properties, initialized in Create/Fetch.
    // This allows factory methods to create the collection with proper DI.
    // =========================================================================
    public partial IAddressList? Addresses { get; set; }

    #region skill-plain-computed-getter
    // =========================================================================
    // Computed Property (not persisted)
    // =========================================================================
    // This is a regular property, not partial - not tracked by Neatoo and it
    // raises no PropertyChanged. A bound UI does not refresh it when
    // FirstName or LastName changes; for that, use a partial property set by
    // an AddAction rule triggered on both.
    // =========================================================================
    public string FullName => $"{FirstName} {LastName}";
    #endregion

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

        // Defaults are assigned here, not by a rule: the object is paused
        // during [Create], so a rule triggered by these assignments would
        // not run.
        IsActive = true;
        HireDate = DateTime.Today;
    }

    // =========================================================================
    // [Fetch] - Load Existing Employee
    // =========================================================================
    // [Remote]: the client fetches this root, so the call crosses to the server.
    //
    // GENERATOR BEHAVIOR: instance factory methods run inside
    // FactoryStart/FactoryComplete - this object is PAUSED for the duration of
    // the body. No explicit PauseAllActions() is needed, and wrapping the body
    // in `using (PauseAllActions())` would actually resume EARLY, when the
    // using disposes, before the factory operation completes.
    //
    // DESIGN DECISION: the repository returns the employee row with its
    // address rows. The employee assigns its own properties from its row and
    // hands row.Addresses to the LIST factory's [Fetch], which builds each
    // address through the ADDRESS factory's [Fetch](row). Every object in the
    // graph gets its own factory lifecycle and lands with correct persistence
    // state.
    //
    // The root's own properties are loaded by plain assignment: the object is
    // paused, so assignment marks nothing modified and runs no rules.
    //
    // Returns false when there is no such employee - the generated factory
    // then returns null.
    // =========================================================================
    [Remote]
    [Fetch]
    internal bool Fetch(Guid id,
        [Service] IEmployeeRepository repository,
        [Service] IAddressListFactory addressListFactory)
    {
        var row = repository.Get(id);
        if (row == null)
        {
            return false;
        }

        Id = row.Id;
        FirstName = row.FirstName;
        LastName = row.LastName;
        Email = row.Email;
        HireDate = row.HireDate;
        Salary = row.Salary;
        IsActive = row.IsActive;

        // Addresses and every address within: IsNew=false, IsModified=false
        Addresses = addressListFactory.Fetch(row.Addresses);

        // After Fetch: IsNew=false, IsModified=false for Employee and all Addresses
        return true;
    }

    // =========================================================================
    // [Insert] - Persist New Employee
    // =========================================================================
    // Called by Save() when IsNew=true (routing checks IsDeleted first, then
    // IsNew).
    //
    // The employee sets its own key, makes its row, maps itself into it, adds
    // it to the repository, hands row.Addresses to the list factory's Save,
    // and flushes once.
    //
    // DESIGN DECISION: Insert and Update delegate child persistence to the SAME
    // list factory Save. The list is never new or deleted, so the list factory
    // routes to AddressList.Update, and each address's own IsNew decides insert
    // vs update - for a brand-new employee, every address is new and gets a
    // new row in row.Addresses.
    // =========================================================================
    [Remote]
    [Insert]
    internal async Task Insert([Service] IEmployeeRepository repository,
        [Service] IAddressListFactory addressListFactory)
    {
        // Re-run every rule on the server and refuse an invalid aggregate.
        // Recommended - the framework does not do this for you. Throw, never
        // return: after [Insert]/[Update] returns, the framework marks the
        // entity saved whether or not anything was written.
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        // The entity sets its own key (paused - assignment is clean)
        Id = Guid.NewGuid();

        var row = new EmployeeRow();
        MapTo(row);
        repository.Add(row);

        addressListFactory.Save(Addresses!, row.Addresses);

        repository.SaveChanges();

        // FactoryComplete(Insert) will call MarkUnmodified() and MarkOld()
    }

    // =========================================================================
    // [Update] - Persist Changes
    // =========================================================================
    // Called by Save() when !IsDeleted && !IsNew.
    //
    // The employee gets its existing row (missing = KeyNotFoundException, an
    // application failure), maps itself into it only if its OWN properties
    // changed, hands row.Addresses to the list factory's Save, and flushes
    // once.
    //
    // The list factory Save runs AddressList.Update inside the LIST's own
    // factory operation: removed addresses have their rows removed, new and
    // modified ones go through per-address factory saves, and the list's
    // FactoryComplete(Update) clears the DeletedList. Each saved address's
    // FactoryComplete marks it unmodified and old. When this Update returns,
    // the framework calls FactoryComplete(Update) on the EMPLOYEE, and the
    // whole graph reads IsModified=false.
    //
    // COMMON MISTAKE: iterating Addresses here and writing the address rows
    // directly. The rows get written, but no child factory operation runs, so
    // nothing marks the children unmodified or old: the aggregate still
    // reports IsModified=true after Save, new children get another row on the
    // next Save, and the DeletedList never clears (FactoryComplete fires per
    // factory target - never as a cascade from the parent). Each address maps
    // itself.
    // =========================================================================
    [Remote]
    [Update]
    internal async Task Update([Service] IEmployeeRepository repository,
        [Service] IAddressListFactory addressListFactory)
    {
        // Re-run every rule on the server and refuse an invalid aggregate.
        // Recommended - the framework does not do this for you. Throw, never
        // return: after [Insert]/[Update] returns, the framework marks the
        // entity saved whether or not anything was written.
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        var row = repository.Get(Id)
            ?? throw new KeyNotFoundException($"Employee {Id} not found");

        // Write the employee's own columns only if they changed
        if (IsSelfModified)
        {
            MapTo(row);
        }

        addressListFactory.Save(Addresses!, row.Addresses);

        repository.SaveChanges();
    }

    // =========================================================================
    // [Delete] - Remove from Persistence
    // =========================================================================
    // Called by Save() when IsDeleted=true (checked FIRST, before IsNew).
    //
    // repository.Remove(row) removes the employee row together with its
    // address rows, as a database cascade delete would. The employee does not
    // loop its addresses: no child [Delete] exists, and an address that was
    // never saved has no row to remove.
    // =========================================================================
    [Remote]
    [Delete]
    internal void Delete([Service] IEmployeeRepository repository)
    {
        var row = repository.Get(Id)
            ?? throw new KeyNotFoundException($"Employee {Id} not found");

        repository.Remove(row);

        repository.SaveChanges();
    }

    private void MapTo(EmployeeRow row)
    {
        row.Id = Id;
        row.FirstName = FirstName!;
        row.LastName = LastName!;
        row.Email = Email!;
        row.HireDate = HireDate;
        row.Salary = Salary;
        row.IsActive = IsActive;
    }
}

// =============================================================================
// Persistence - Rows and Repository
// =============================================================================
// Modeled on an EF Core unit of work: the row classes stand in for EF
// entities, Get returns the root row with its child rows loaded, and
// SaveChanges flushes everything added, changed or removed since the last
// flush.
// =============================================================================

public class EmployeeRow
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public DateTime? HireDate { get; set; }
    public decimal Salary { get; set; }
    public bool IsActive { get; set; }
    public List<AddressRow> Addresses { get; } = new();
}

public class AddressRow
{
    public Guid Id { get; set; }
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string ZipCode { get; set; } = "";
    public string AddressType { get; set; } = "";
}

public interface IEmployeeRepository
{
    /// <summary>The employee row with its address rows, or null if there is none.</summary>
    EmployeeRow? Get(Guid id);

    void Add(EmployeeRow row);

    /// <summary>Removes the employee row and its address rows.</summary>
    void Remove(EmployeeRow row);

    void SaveChanges();
}
