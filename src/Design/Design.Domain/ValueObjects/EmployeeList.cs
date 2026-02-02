// -----------------------------------------------------------------------------
// Design.Domain - EmployeeList (ValidateListBase Example)
// -----------------------------------------------------------------------------
// This file demonstrates ValidateListBase for collections of read models.
// -----------------------------------------------------------------------------

using Neatoo;
using Neatoo.RemoteFactory;

namespace Design.Domain.ValueObjects;

/// <summary>
/// Demonstrates: ValidateListBase&lt;I&gt; for read model collections.
///
/// Key points:
/// - No DeletedList (that's EntityListBase)
/// - IsValid aggregates from children
/// - IsBusy aggregates from children
/// - Used for: Search results, list views, read-only collections
/// </summary>
[Factory]
public partial class EmployeeList : ValidateListBase<EmployeeListItem>
{
    // =========================================================================
    // ValidateListBase provides:
    // - IsValid: All items are valid
    // - IsSelfValid: Always true (lists don't have own validation)
    // - IsBusy: Any item is busy
    // - ObservableCollection behavior
    // - PropertyChanged/NeatooPropertyChanged events
    //
    // It does NOT provide:
    // - IsModified (no persistence tracking)
    // - DeletedList (no deletion tracking)
    // - Root (no aggregate concept for read models)
    // =========================================================================

    [Create]
    public void Create()
    {
        // Empty list
    }

    // =========================================================================
    // [Fetch] - Load Collection
    // =========================================================================
    // Lists can have Fetch to populate from server.
    // =========================================================================
    [Remote]
    [Fetch]
    public void Fetch([Service] IEmployeeListRepository repository, [Service] IEmployeeListItemFactory itemFactory)
    {
        // Note: List bases don't have PauseAllActions - items are added directly
        foreach (var data in repository.GetAll())
        {
            var item = itemFactory.Create();
            item["Id"].LoadValue(data.Id);
            item["FullName"].LoadValue(data.FullName);
            item["Email"].LoadValue(data.Email);
            item["Department"].LoadValue(data.Department);
            item["IsActive"].LoadValue(data.IsActive);

            Add(item);
        }
    }

    // =========================================================================
    // [Fetch] with Criteria - Filtered Results
    // =========================================================================
    [Remote]
    [Fetch]
    public void Fetch(EmployeeSearchCriteria criteria,
        [Service] IEmployeeListRepository repository,
        [Service] IEmployeeListItemFactory itemFactory)
    {
        foreach (var data in repository.Search(criteria.SearchTerm, criteria.Department, criteria.ActiveOnly))
        {
            var item = itemFactory.Create();
            item["Id"].LoadValue(data.Id);
            item["FullName"].LoadValue(data.FullName);
            item["Email"].LoadValue(data.Email);
            item["Department"].LoadValue(data.Department);
            item["IsActive"].LoadValue(data.IsActive);

            Add(item);
        }
    }

    // =========================================================================
    // No [Insert]/[Update]/[Delete] or Save()
    // =========================================================================
    // Read model lists don't persist. They're for display only.
    // To modify data, work with the full entity:
    //
    //   var list = await employeeListFactory.Fetch(criteria);
    //   var selectedItem = list[0];
    //
    //   // To edit:
    //   var employee = await employeeFactory.Fetch(selectedItem.Id);
    //   employee.Department = "Engineering";
    //   await employee.Save();
    //
    //   // Refresh the list
    //   list = await employeeListFactory.Fetch(criteria);
    // =========================================================================
}

// =============================================================================
// Search Criteria Pattern
// =============================================================================
// Use a criteria class for complex search parameters.
// This keeps the Fetch signature clean and serializable.
// =============================================================================

public class EmployeeSearchCriteria
{
    public string? SearchTerm { get; set; }
    public string? Department { get; set; }
    public bool ActiveOnly { get; set; } = true;
}

// =============================================================================
// Repository Interface
// =============================================================================

public interface IEmployeeListRepository
{
    (int Id, string FullName, string Email, string Department, bool IsActive) GetById(int id);
    IEnumerable<(int Id, string FullName, string Email, string Department, bool IsActive)> GetAll();
    IEnumerable<(int Id, string FullName, string Email, string Department, bool IsActive)> Search(
        string? searchTerm, string? department, bool activeOnly);
}
