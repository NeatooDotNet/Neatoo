// -----------------------------------------------------------------------------
// Design.Domain - ApproveEmployee Command ([Execute] Pattern)
// -----------------------------------------------------------------------------
// This file demonstrates the [Execute] attribute for command operations.
// Commands encapsulate business operations that may span multiple entities.
// -----------------------------------------------------------------------------

using Neatoo.RemoteFactory;

namespace Design.Domain.Commands;

// =============================================================================
// [Execute] - Command Pattern
// =============================================================================
// Use [Execute] for operations that:
// - Perform business logic across multiple entities
// - Don't fit the entity CRUD pattern
// - Need to be invokable as discrete operations
// - Return results (success/failure, generated data)
//
// DESIGN DECISION: Commands are static partial classes with [Factory] and [Execute].
// They're not entities - they don't inherit from ValidateBase/EntityBase.
// The factory pattern still applies for DI and remote execution.
//
// DID NOT DO THIS: Make commands inherit from ValidateBase.
//
// REJECTED PATTERN:
//   public class ApproveEmployeeCommand : ValidateBase<ApproveEmployeeCommand>
//   {
//       public partial int EmployeeId { get; set; }
//       public partial string? Reason { get; set; }
//
//       [Execute]
//       public Task Execute([Service] IRepo repo) { ... }
//   }
//
// ACTUAL PATTERN:
//   [Factory]
//   public static partial class ApproveEmployee
//   {
//       [Execute]
//       public static Task<ApproveEmployeeResult> _Approve(int employeeId, [Service] IRepo repo) { ... }
//   }
//
// WHY NOT: Commands are operations, not domain objects. They don't need
// property tracking, validation rules, or persistence state. A simple
// static class with parameters is cleaner and more explicit.
//
// GENERATOR BEHAVIOR: [Execute] methods MUST return Task or Task<T>.
// Static command classes MUST be marked 'partial' for code generation.
// Method name convention: Use _MethodName - the leading underscore is stripped
// to create the delegate name (e.g., _Approve -> Approve delegate).
// =============================================================================

/// <summary>
/// Demonstrates: [Execute] command pattern for business operations.
///
/// Key points:
/// - Static partial class with [Factory] attribute
/// - [Execute] method performs the operation
/// - Parameters define inputs
/// - Return type must be Task or Task&lt;T&gt;
/// - [Remote] if server execution required
/// </summary>
[Factory]
public static partial class ApproveEmployee
{
    // =========================================================================
    // [Execute] - The Command Operation
    // =========================================================================
    // [Execute] marks this method as a command operation.
    // The factory generates:
    //   interface IApproveEmployeeFactory {
    //       Task<ApproveEmployeeResult> Execute(int employeeId, string? approverName);
    //   }
    //
    // With [Remote], the client factory makes an HTTP call.
    // Without [Remote], it's local execution.
    //
    // GENERATOR BEHAVIOR: [Execute] methods must return Task or Task<T>.
    // This enables consistent async patterns across all factory operations.
    // =========================================================================
    // =========================================================================
    // Delegate Naming Convention
    // =========================================================================
    // GENERATOR BEHAVIOR: The delegate name is derived from the method name.
    // A leading underscore is stripped: _Approve -> Approve delegate.
    //
    // This creates a clean public API:
    //   factory.Approve(employeeId, approverName)
    // While the internal method is _Approve.
    // =========================================================================
    [Remote]
    [Execute]
    public static Task<ApproveEmployeeResult> _Approve(
        int employeeId,
        string? approverName,
        [Service] IApproveEmployeeRepository repository)
    {
        // Load the employee
        var employee = repository.GetEmployee(employeeId);
        if (employee == null)
        {
            return Task.FromResult(ApproveEmployeeResult.Failed($"Employee {employeeId} not found"));
        }

        // Business logic: Check if employee can be approved
        if (employee.Value.IsApproved)
        {
            return Task.FromResult(ApproveEmployeeResult.Failed("Employee is already approved"));
        }

        if (!employee.Value.IsActive)
        {
            return Task.FromResult(ApproveEmployeeResult.Failed("Cannot approve inactive employee"));
        }

        // Perform the approval
        repository.ApproveEmployee(employeeId, approverName, DateTime.UtcNow);

        return Task.FromResult(ApproveEmployeeResult.Succeeded($"Employee {employee.Value.FullName} approved by {approverName}"));
    }
}

// =============================================================================
// Command Result Pattern
// =============================================================================
// Commands return result objects that indicate success/failure and provide data.
// This is cleaner than throwing exceptions for expected failures.
// =============================================================================

public class ApproveEmployeeResult
{
    public bool Success { get; private set; }
    public string Message { get; private set; } = string.Empty;

    public static ApproveEmployeeResult Succeeded(string message)
        => new() { Success = true, Message = message };

    public static ApproveEmployeeResult Failed(string message)
        => new() { Success = false, Message = message };
}

// =============================================================================
// Additional Command Examples
// =============================================================================

/// <summary>
/// Command that returns data.
/// </summary>
[Factory]
public static partial class GenerateEmployeeReport
{
    [Remote]
    [Execute]
    public static Task<EmployeeReportResult> _Generate(
        int departmentId,
        DateTime startDate,
        DateTime endDate,
        [Service] IReportRepository repository)
    {
        var data = repository.GetEmployeeStats(departmentId, startDate, endDate);

        return Task.FromResult(new EmployeeReportResult
        {
            DepartmentName = data.DepartmentName,
            EmployeeCount = data.EmployeeCount,
            TotalSalary = data.TotalSalary,
            AverageSalary = data.AverageSalary
        });
    }
}

public class EmployeeReportResult
{
    public string? DepartmentName { get; set; }
    public int EmployeeCount { get; set; }
    public decimal TotalSalary { get; set; }
    public decimal AverageSalary { get; set; }
}

/// <summary>
/// Command that sends an email.
/// </summary>
/// <remarks>
/// DESIGN DECISION: [Execute] methods should return Task&lt;T&gt; rather than Task.
/// This allows the caller to receive a result indicating success/failure.
/// If no data is needed, return a simple boolean or result object.
/// </remarks>
[Factory]
public static partial class SendWelcomeEmail
{
    [Remote]
    [Execute]
    public static Task<bool> _Send(
        int employeeId,
        [Service] IEmailService emailService,
        [Service] IEmployeeQueryRepository repository)
    {
        var employee = repository.GetEmailInfo(employeeId);
        if (employee != null)
        {
            emailService.SendWelcome(employee.Value.Email, employee.Value.FullName);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }
}

/// <summary>
/// Async command (returns Task).
/// </summary>
[Factory]
public static partial class ProcessBatchApproval
{
    [Remote]
    [Execute]
    public static async Task<BatchApprovalResult> _Process(
        int[] employeeIds,
        string? approverName,
        [Service] IApproveEmployeeRepository repository)
    {
        var results = new List<(int Id, bool Success, string Message)>();

        foreach (var id in employeeIds)
        {
            var employee = repository.GetEmployee(id);
            if (employee == null)
            {
                results.Add((id, false, "Not found"));
                continue;
            }

            if (employee.Value.IsApproved)
            {
                results.Add((id, false, "Already approved"));
                continue;
            }

            repository.ApproveEmployee(id, approverName, DateTime.UtcNow);
            results.Add((id, true, "Approved"));

            // Simulate async work
            await Task.Delay(10);
        }

        return new BatchApprovalResult
        {
            TotalProcessed = results.Count,
            SuccessCount = results.Count(r => r.Success),
            FailedCount = results.Count(r => !r.Success),
            Details = results
        };
    }
}

public class BatchApprovalResult
{
    public int TotalProcessed { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<(int Id, bool Success, string Message)> Details { get; set; } = new();
}

// =============================================================================
// Support Interfaces
// =============================================================================

public interface IApproveEmployeeRepository
{
    (int Id, string FullName, bool IsActive, bool IsApproved)? GetEmployee(int id);
    void ApproveEmployee(int id, string? approverName, DateTime approvedDate);
}

public interface IReportRepository
{
    (string DepartmentName, int EmployeeCount, decimal TotalSalary, decimal AverageSalary)
        GetEmployeeStats(int departmentId, DateTime startDate, DateTime endDate);
}

public interface IEmailService
{
    void SendWelcome(string? email, string? fullName);
}

public interface IEmployeeQueryRepository
{
    (string? Email, string? FullName)? GetEmailInfo(int employeeId);
}
