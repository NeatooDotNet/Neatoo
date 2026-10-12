// -----------------------------------------------------------------------------
// Design.Infrastructure - EmployeeDirectory Read Model Query
// -----------------------------------------------------------------------------

using Design.Domain.ReadModels;

namespace Design.Infrastructure.InMemory;

/// <summary>
/// In-memory IEmployeeDirectoryRepository for Design.Server. Seeded with
/// three employees so the directory has rows to list and search.
/// </summary>
internal sealed class InMemoryEmployeeDirectoryRepository : IEmployeeDirectoryRepository
{
    internal List<EmployeeSummary> Rows { get; } =
    [
        new(1, "Ada Lovelace", "ada@example.com", "Engineering", true),
        new(2, "Grace Hopper", "grace@example.com", "Engineering", true),
        new(3, "Alan Turing", "alan@example.com", "Research", false),
    ];

    public IEnumerable<EmployeeSummary> GetAll() => Rows.ToList();

    public IEnumerable<EmployeeSummary> Search(string? searchTerm, string? department, bool activeOnly)
        => Rows.Where(r => (searchTerm == null || r.FullName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                        && (department == null || r.Department == department)
                        && (!activeOnly || r.IsActive))
               .ToList();
}
