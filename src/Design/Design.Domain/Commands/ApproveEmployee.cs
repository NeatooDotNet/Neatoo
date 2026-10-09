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
// - Return data the caller needs (generated values, computed results)
//
// DESIGN DECISION: A command is a static partial class with [Factory]. Each
// operation is [Remote, Execute] private static _Name when it needs the server.
// Commands are not entities - they don't inherit from ValidateBase/EntityBase.
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
//       [Remote]
//       [Execute]
//       private static Task<ApprovalReceipt> _Approve(int employeeId, [Service] IRepo repo) { ... }
//   }
//
// WHY NOT: Commands are operations, not domain objects. They don't need
// property tracking, validation rules, or persistence state. A simple
// static class with parameters is cleaner and more explicit.
//
// GENERATOR BEHAVIOR: [Execute] methods MUST return Task<T>.
// Static command classes MUST be marked 'partial' for code generation.
// Method name convention: Use _MethodName - the leading underscore is stripped
// to create the delegate name (e.g., _Approve -> Approve delegate).
// =============================================================================

/// <summary>
/// Demonstrates: [Execute] command pattern for business operations.
///
/// Key points:
/// - Static partial class with [Factory] attribute
/// - [Remote, Execute] private static _Name performs the operation on the server
/// - Parameters define inputs
/// - Return type must be Task or Task&lt;T&gt;
/// </summary>
[Factory]
public static partial class ApproveEmployee
{
    // =========================================================================
    // [Remote, Execute] - The Command Operation
    // =========================================================================
    // With [Remote], a client call crosses to the server (RemoteFactory 1.9+).
    // Without [Remote], the command runs on the calling tier, so a server-only
    // [Service] such as this repository would not resolve on the client.
    //
    // private static: the generated delegate is the public API; the method
    // itself is never called directly. With [Remote], the body and its server
    // dependencies are trimmed from a WASM client.
    //
    // GENERATOR BEHAVIOR: The delegate name is derived from the method name.
    // A leading underscore is stripped: _Approve -> ApproveEmployee.Approve.
    //   var receipt = await approve(employeeId, approverName);
    // =========================================================================
    //
    // DESIGN DECISION: The command does not check data and return a failure.
    // =========================================================================
    // Whether an employee can be approved is decided before the command is
    // called: the screen offers Approve only when server truth says so (a
    // read model's CanApprove flag, or a rule on the Employee aggregate). If
    // the command is reached with an employee that cannot be approved, the
    // client and server disagree - an application failure, so it throws.
    //
    // DID NOT DO THIS: Return ApproveEmployeeResult.Failed("Employee is already
    // approved") and have the UI show the message.
    //
    // WHY NOT: That makes the result object a third validation channel beside
    // rules and exceptions, and the user learns the action was invalid only
    // after a round trip. Validation is a rule; an exception is an application
    // failure and is never caught to produce a validation message.
    // =========================================================================
    [Remote]
    [Execute]
    private static Task<ApprovalReceipt> _Approve(
        int employeeId,
        string? approverName,
        [Service] IApproveEmployeeRepository repository)
    {
        var employee = repository.GetEmployee(employeeId)
            ?? throw new InvalidOperationException($"Employee {employeeId} not found");

        if (employee.IsApproved || !employee.IsActive)
        {
            throw new InvalidOperationException(
                $"Employee {employeeId} cannot be approved (approved: {employee.IsApproved}, active: {employee.IsActive})");
        }

        var approvedOn = DateTime.UtcNow;
        repository.ApproveEmployee(employeeId, approverName, approvedOn);

        return Task.FromResult(new ApprovalReceipt(employee.FullName, approverName, approvedOn));
    }
}

/// <summary>
/// What the approval produced. Returned data, not a success/failure flag.
/// </summary>
public sealed record ApprovalReceipt(string FullName, string? ApproverName, DateTime ApprovedOn);

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
    private static Task<EmployeeReportResult> _Generate(
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
/// Command that performs a side effect.
/// </summary>
/// <remarks>
/// GENERATOR BEHAVIOR: [Execute] must return Task&lt;T&gt;, not Task. A plain
/// Task generates a delegate returning Task&lt;Task&gt; that does not compile
/// (RemoteFactory 1.9). A side-effect command returns a confirmation.
///
/// A missing employee is an application failure: the caller passed an id it
/// got from the server, so the command throws instead of returning false.
/// </remarks>
#region skill-command
[Factory]
public static partial class SendWelcomeEmail
{
    [Remote]
    [Execute]
    private static Task<bool> _Send(
        int employeeId,
        [Service] IEmailService emailService,
        [Service] IEmployeeQueryRepository repository)
    {
        var employee = repository.GetEmailInfo(employeeId)
            ?? throw new InvalidOperationException($"Employee {employeeId} not found");

        emailService.SendWelcome(employee.Email, employee.FullName);
        return Task.FromResult(true);
    }
}
#endregion

/// <summary>
/// Async command over several employees.
/// </summary>
/// <remarks>
/// Approving an employee who is already approved is a no-op, so a batch can
/// be re-run. The result reports what happened; it is not a validation channel.
/// </remarks>
[Factory]
public static partial class ProcessBatchApproval
{
    [Remote]
    [Execute]
    private static async Task<BatchApprovalResult> _Process(
        int[] employeeIds,
        string? approverName,
        [Service] IApproveEmployeeRepository repository)
    {
        var approved = 0;
        var alreadyApproved = 0;

        foreach (var id in employeeIds)
        {
            var employee = repository.GetEmployee(id)
                ?? throw new InvalidOperationException($"Employee {id} not found");

            if (employee.IsApproved)
            {
                alreadyApproved++;
                continue;
            }

            repository.ApproveEmployee(id, approverName, DateTime.UtcNow);
            approved++;

            // Simulate async work
            await Task.Delay(10);
        }

        return new BatchApprovalResult { ApprovedCount = approved, AlreadyApprovedCount = alreadyApproved };
    }
}

public class BatchApprovalResult
{
    public int ApprovedCount { get; set; }
    public int AlreadyApprovedCount { get; set; }
}

// =============================================================================
// Support Interfaces
// =============================================================================

public interface IApproveEmployeeRepository
{
    ApprovalCandidate? GetEmployee(int id);
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
    EmailRecipient? GetEmailInfo(int employeeId);
}

public sealed record ApprovalCandidate(int Id, string FullName, bool IsActive, bool IsApproved);

public sealed record EmailRecipient(string? Email, string? FullName);
