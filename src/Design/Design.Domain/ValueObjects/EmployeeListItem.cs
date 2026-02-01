// -----------------------------------------------------------------------------
// Design.Domain - EmployeeListItem (ValidateBase Read Model)
// -----------------------------------------------------------------------------
// This file demonstrates ValidateBase for read models and value objects.
// These are lightweight objects for display that don't track persistence state.
// -----------------------------------------------------------------------------

using Neatoo;
using Neatoo.RemoteFactory;

namespace Design.Domain.ValueObjects;

/// <summary>
/// Demonstrates: ValidateBase&lt;T&gt; for read models and DTOs.
///
/// Key points:
/// - No persistence tracking (no IsModified, IsNew, IsSavable)
/// - Validation still works (IsValid, rules)
/// - Lighter weight than EntityBase
/// - Used for: List items, search results, read-only views
/// </summary>
[Factory]
public partial class EmployeeListItem : ValidateBase<EmployeeListItem>
{
    // =========================================================================
    // Properties - Read Model Data
    // =========================================================================
    // Even read models use partial properties for consistency.
    // They get validation but not modification tracking.
    // =========================================================================

    public partial int Id { get; set; }
    public partial string? FullName { get; set; }
    public partial string? Email { get; set; }
    public partial string? Department { get; set; }
    public partial bool IsActive { get; set; }

    public EmployeeListItem(IValidateBaseServices<EmployeeListItem> services) : base(services)
    {
        // Read models can have validation rules
        // (though typically they're already validated data from DB)
        RuleManager.AddValidation(
            t => string.IsNullOrWhiteSpace(t.FullName) ? "Full name is required" : string.Empty,
            t => t.FullName);
    }

    // =========================================================================
    // [Create] - Empty Construction
    // =========================================================================
    [Create]
    public void Create()
    {
        // Empty item
    }

    // =========================================================================
    // [Fetch] - Load Read Model Data
    // =========================================================================
    // Read models CAN have Fetch to load from server.
    // They just don't have Insert/Update/Delete.
    // =========================================================================
    [Remote]
    [Fetch]
    public void Fetch(int id, [Service] IEmployeeListRepository repository)
    {
        var data = repository.GetById(id);

        // Still use LoadValue for consistency
        // (though ValidateBase doesn't track modification)
        this["Id"].LoadValue(data.Id);
        this["FullName"].LoadValue(data.FullName);
        this["Email"].LoadValue(data.Email);
        this["Department"].LoadValue(data.Department);
        this["IsActive"].LoadValue(data.IsActive);
    }

    // =========================================================================
    // No [Insert]/[Update]/[Delete]
    // =========================================================================
    // Read models don't persist. To change data, use the full entity:
    //
    //   var listItem = await employeeListItemFactory.Fetch(1);
    //   // listItem shows current state
    //
    //   // To edit, fetch the full entity:
    //   var employee = await employeeFactory.Fetch(listItem.Id);
    //   employee.Email = "new@email.com";
    //   await employee.Save();
    // =========================================================================
}

// =============================================================================
// When to Use ValidateBase vs EntityBase
// =============================================================================
//
// Use ValidateBase<T> when:
// - Displaying data in lists/grids (read-only)
// - Search results
// - DTOs transferred between layers
// - Value objects (e.g., Money, DateRange)
// - Form wizard steps that validate but don't persist directly
//
// Use EntityBase<T> when:
// - Full CRUD operations needed
// - Tracking changes for optimistic concurrency
// - Aggregate roots and child entities
// - Data that will be saved back to database
//
// DESIGN DECISION: ValidateBase is intentionally limited.
// No IsModified/IsNew/IsSavable reduces memory and complexity.
// If you need those features, use EntityBase.
// =============================================================================

// Note: IEmployeeListRepository is defined in EmployeeList.cs
