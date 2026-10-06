// -----------------------------------------------------------------------------
// Design.Domain - Read Model (Plain [Factory] Class)
// -----------------------------------------------------------------------------
// A read model is server truth shaped for one screen: a list, a dashboard
// tile, the flags a page gates on. It is a plain [Factory] class with [Fetch]
// only - no Neatoo base class, no rules, no change tracking, no Save.
// -----------------------------------------------------------------------------

using Neatoo.RemoteFactory;

namespace Design.Domain.ReadModels;

// =============================================================================
// DESIGN DECISION: A read model has no Neatoo base class.
// =============================================================================
// Neatoo's base classes exist for an object a person edits: change tracking,
// rules that react to edits, IsSavable. A read model is never edited, so all
// of that is weight with nothing to do. RemoteFactory works with any class:
// [Factory] on a plain class gives it a factory and carries it across the
// wire.
//
// DID NOT DO THIS: ValidateBase<T> / ValidateListBase<I> for read models.
//
// REJECTED PATTERN:
//   internal partial class EmployeeListItem : ValidateBase<EmployeeListItem> { ... }
//   internal partial class EmployeeList : ValidateListBase<IEmployeeListItem> { ... }
//
// WHY NOT: Rules, validity and PropertyChanged on every row of a grid, for
// data nobody edits. Use ValidateBase<T> only for an object that needs rules.
//
// Shape:
// - Public interface with getters only; the class is internal (interface-first).
// - Properties have internal setters: only [Fetch] writes them, and the
//   generated serializer lives in the same assembly.
// - [Remote][Fetch] - the client asks, the server answers with one query.
// - The rows are immutable records.
// - The read model computes what the screen shows or gates on (see
//   ActiveCount) so the UI reads an answer instead of re-deriving it.
// =============================================================================

/// <summary>
/// One row of the employee directory.
/// </summary>
public sealed record EmployeeSummary(int Id, string FullName, string Email, string Department, bool IsActive);

/// <summary>
/// Read model for the employee directory screen.
/// </summary>
public interface IEmployeeDirectory
{
    IReadOnlyList<EmployeeSummary> Employees { get; }

    /// <summary>
    /// Server-computed: how many of the returned employees are active.
    /// </summary>
    int ActiveCount { get; }
}

/// <summary>
/// Demonstrates: a read model as a plain [Factory] class with [Fetch] only.
/// </summary>
[Factory]
internal partial class EmployeeDirectory : IEmployeeDirectory
{
    public IReadOnlyList<EmployeeSummary> Employees { get; internal set; } = [];

    public int ActiveCount { get; internal set; }

    // =========================================================================
    // [Fetch] - All Employees
    // =========================================================================
    [Remote]
    [Fetch]
    internal void Fetch([Service] IEmployeeDirectoryRepository repository)
    {
        Load(repository.GetAll());
    }

    // =========================================================================
    // [Fetch] with Criteria - Filtered Results
    // =========================================================================
    // A criteria class keeps the signature short. It is a request parameter,
    // so on a trimmed client it needs a preserve entry (see the RemoteFactory
    // skill's trimming reference).
    // =========================================================================
    [Remote]
    [Fetch]
    internal void Fetch(EmployeeSearchCriteria criteria, [Service] IEmployeeDirectoryRepository repository)
    {
        Load(repository.Search(criteria.SearchTerm, criteria.Department, criteria.ActiveOnly));
    }

    private void Load(IEnumerable<EmployeeSummary> rows)
    {
        Employees = rows.ToList();
        ActiveCount = Employees.Count(e => e.IsActive);
    }

    // =========================================================================
    // No [Create], [Insert], [Update] or [Delete]
    // =========================================================================
    // A read model is not edited and not saved. To change an employee, fetch
    // the aggregate, change it, save it, then fetch the read model again:
    //
    //   var directory = await directoryFactory.Fetch(criteria);
    //   var employee = await employeeFactory.Fetch(selectedId);
    //   employee.Department = "Engineering";
    //   await employee.Save();
    //   directory = await directoryFactory.Fetch(criteria);
    // =========================================================================
}

/// <summary>
/// Search criteria for the employee directory.
/// </summary>
public class EmployeeSearchCriteria
{
    public string? SearchTerm { get; set; }
    public string? Department { get; set; }
    public bool ActiveOnly { get; set; } = true;
}

/// <summary>
/// Server-side query for the employee directory. One call per fetch.
/// </summary>
public interface IEmployeeDirectoryRepository
{
    IEnumerable<EmployeeSummary> GetAll();
    IEnumerable<EmployeeSummary> Search(string? searchTerm, string? department, bool activeOnly);
}
