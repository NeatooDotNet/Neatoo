// -----------------------------------------------------------------------------
// Design.Infrastructure - ApproveEmployee Command Services
// -----------------------------------------------------------------------------

using Design.Domain.Commands;

namespace Design.Infrastructure.InMemory;

/// <summary>
/// In-memory IApproveEmployeeRepository for Design.Server. Seeded with the
/// same three employees as the directory; approval is recorded in place.
/// </summary>
internal sealed class InMemoryApproveEmployeeRepository : IApproveEmployeeRepository
{
    internal Dictionary<int, ApprovalCandidate> Employees { get; } = new()
    {
        [1] = new(1, "Ada Lovelace", IsActive: true, IsApproved: false),
        [2] = new(2, "Grace Hopper", IsActive: true, IsApproved: true),
        [3] = new(3, "Alan Turing", IsActive: false, IsApproved: false),
    };

    public ApprovalCandidate? GetEmployee(int id) => Employees.GetValueOrDefault(id);

    public void ApproveEmployee(int id, string? approverName, DateTime approvedDate)
    {
        if (Employees.TryGetValue(id, out var employee))
        {
            Employees[id] = employee with { IsApproved = true };
        }
    }
}

/// <summary>
/// In-memory IReportRepository for Design.Server. No salary data is stored,
/// so the stats are zero for every department.
/// </summary>
internal sealed class InMemoryReportRepository : IReportRepository
{
    public (string DepartmentName, int EmployeeCount, decimal TotalSalary, decimal AverageSalary)
        GetEmployeeStats(int departmentId, DateTime startDate, DateTime endDate)
        => ($"Department-{departmentId}", 0, 0m, 0m);
}

/// <summary>
/// In-memory IEmailService for Design.Server. Records each welcome message
/// instead of sending it.
/// </summary>
internal sealed class InMemoryEmailService : IEmailService
{
    internal List<(string? Email, string? FullName)> Sent { get; } = new();

    public void SendWelcome(string? email, string? fullName) => Sent.Add((email, fullName));
}

/// <summary>
/// In-memory IEmployeeQueryRepository for Design.Server. Seeded with the same
/// three employees as the directory.
/// </summary>
internal sealed class InMemoryEmployeeQueryRepository : IEmployeeQueryRepository
{
    internal Dictionary<int, EmailRecipient> Recipients { get; } = new()
    {
        [1] = new("ada@example.com", "Ada Lovelace"),
        [2] = new("grace@example.com", "Grace Hopper"),
        [3] = new("alan@example.com", "Alan Turing"),
    };

    public EmailRecipient? GetEmailInfo(int employeeId) => Recipients.GetValueOrDefault(employeeId);
}
