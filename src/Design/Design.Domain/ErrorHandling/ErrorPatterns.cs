// -----------------------------------------------------------------------------
// Design.Domain - Error Handling Strategy
// -----------------------------------------------------------------------------
// This file documents how Neatoo handles errors: what exceptions can be thrown,
// the distinction between validation failures and exceptions, rule exception
// behavior, and error boundary patterns.
// -----------------------------------------------------------------------------

using Neatoo;
using Neatoo.RemoteFactory;
using Neatoo.Rules;

namespace Design.Domain.ErrorHandling;

// =============================================================================
// ERROR HANDLING OVERVIEW
// =============================================================================
// Neatoo distinguishes between TWO types of problems:
//
// 1. VALIDATION FAILURES: Expected business rule violations
//    - Handled via IRuleMessages (rule return values)
//    - Set IsValid=false, populate PropertyMessages
//    - Do NOT throw exceptions
//    - Examples: "Name is required", "Value must be positive"
//
// 2. EXCEPTIONS: Unexpected errors (bugs, infrastructure failures)
//    - Thrown as .NET exceptions
//    - Should be caught by error boundaries
//    - Examples: database connection failed, null reference
//
// DESIGN DECISION: Validation failures are NOT exceptions.
// Users entering invalid data is expected behavior, not an exceptional
// condition. Rules return messages; they don't throw.
//
// DID NOT DO THIS: Throw ValidationException for rule failures.
//
// REJECTED PATTERN:
//   if (string.IsNullOrEmpty(target.Name))
//       throw new ValidationException("Name is required");
//
// ACTUAL PATTERN:
//   if (string.IsNullOrEmpty(target.Name))
//       return (nameof(target.Name), "Name is required").AsRuleMessages();
//
// WHY NOT: Exceptions for validation would:
// - Require try/catch for every property change
// - Make it impossible to show multiple validation errors at once
// - Conflate expected failures with unexpected errors
// =============================================================================

// =============================================================================
// EXCEPTIONS FACTORY METHODS CAN THROW
// =============================================================================
// Factory operations may throw these exception types:
//
// 1. ArgumentNullException
//    - When: Required parameter is null
//    - Example: factory.Fetch(null) when ID is required
//
// 2. InvalidOperationException
//    - When: Operation not valid in current state
//    - Example: Calling Save() on a child entity (IsSavable=false)
//
// 3. NeatooConfigurationException (and subtypes)
//    - When: Framework is misconfigured
//    - Example: [Factory] class missing [Create] method
//
// 4. DI Resolution Exceptions
//    - When: Required service not registered
//    - Example: IEmployeeRepository not in DI container
//
// 5. Infrastructure Exceptions (from your code)
//    - When: External systems fail
//    - Example: DbContext throws on database error
//
// PATTERN: Let infrastructure exceptions bubble up.
// Don't catch and wrap database exceptions in factory methods.
// Let them propagate to error boundary handlers.
// =============================================================================

// =============================================================================
// VALIDATION FAILURES VS EXCEPTIONS - DECISION TABLE
// =============================================================================
//
// +-----------------------------+----------------------+----------------------+
// | Scenario                    | Use Validation       | Use Exception        |
// +-----------------------------+----------------------+----------------------+
// | Required field is empty     | X                    |                      |
// | Value out of range          | X                    |                      |
// | Format invalid (email)      | X                    |                      |
// | Duplicate name exists       | X                    |                      |
// | Cross-field validation      | X                    |                      |
// | Database connection failed  |                      | X                    |
// | Null reference bug          |                      | X                    |
// | Configuration missing       |                      | X                    |
// | External API timeout        |                      | X                    |
// | Concurrent modification     | Context-dependent    | Context-dependent    |
// +-----------------------------+----------------------+----------------------+
//
// CONCURRENT MODIFICATION NOTE:
// - If expected (multiple users editing): Validation with message
// - If unexpected (bug): Exception
// =============================================================================

/// <summary>
/// Demonstrates: Validation failure patterns (not exceptions).
/// </summary>
[Factory]
public partial class ValidationFailureDemo : EntityBase<ValidationFailureDemo>
{
    public partial string? Name { get; set; }
    public partial int Quantity { get; set; }
    public partial string? Email { get; set; }

    public ValidationFailureDemo(IEntityBaseServices<ValidationFailureDemo> services) : base(services)
    {
        // =====================================================================
        // VALIDATION RULES: Return messages, don't throw
        // =====================================================================
        RuleManager.AddValidation(
            t => string.IsNullOrWhiteSpace(t.Name) ? "Name is required" : string.Empty,
            t => t.Name);

        RuleManager.AddValidation(
            t => t.Quantity < 0 ? "Quantity must be non-negative" : string.Empty,
            t => t.Quantity);

        RuleManager.AddValidation(
            t => !string.IsNullOrEmpty(t.Email) && !t.Email.Contains('@')
                ? "Email must be a valid email address"
                : string.Empty,
            t => t.Email);
    }

    [Create]
    public void Create() { }

    [Remote]
    [Fetch]
    public void Fetch(int id, [Service] IErrorDemoRepository repository)
    {
        var data = repository.GetById(id);
        this["Name"].LoadValue(data.Name);
        this["Quantity"].LoadValue(data.Quantity);
        this["Email"].LoadValue(data.Email);
    }

    [Remote]
    [Insert]
    public void Insert([Service] IErrorDemoRepository repository) { }

    [Remote]
    [Update]
    public void Update([Service] IErrorDemoRepository repository) { }

    [Remote]
    [Delete]
    public void Delete([Service] IErrorDemoRepository repository) { }
}

// =============================================================================
// RULE EXCEPTION BEHAVIOR
// =============================================================================
// What happens when a rule throws an exception (bug, not validation)?
//
// 1. Exception is caught by RuleManager.RunRule()
// 2. Exception message is added to trigger property as a rule message
// 3. Exception is RE-THROWN to caller
// 4. IsValid becomes false (due to the error message)
// 5. Property is marked with the exception message
//
// This means:
// - Rule bugs show up as validation errors (visible to user)
// - Exception also propagates (can be caught by error boundary)
// - Object is in a known state (invalid with error message)
//
// DESIGN DECISION: Re-throw rule exceptions after adding message.
// This allows both:
// - User to see something went wrong (validation message)
// - Developer to catch and log the exception (stack trace)
//
// COMMON MISTAKE: Ignoring rule exceptions.
//
// WRONG:
//   try {
//       entity.Name = "Test";
//   } catch {
//       // Swallow exception - user sees validation error, that's enough
//   }
//
// RIGHT:
//   try {
//       entity.Name = "Test";
//       await entity.WaitForTasks();
//   } catch (Exception ex) {
//       Logger.Error(ex, "Rule execution failed");
//       // Show user-friendly error in UI
//       // Object is invalid, cannot save
//   }
// =============================================================================

/// <summary>
/// Demonstrates: What happens when a rule throws an exception.
/// </summary>
public class ExceptionThrowingRule : AsyncRuleBase<ValidationFailureDemo>
{
    public ExceptionThrowingRule() : base(t => t.Name) { }

    protected override Task<IRuleMessages> Execute(ValidationFailureDemo target, CancellationToken? token = null)
    {
        // =====================================================================
        // DON'T DO THIS IN REAL CODE
        // This simulates a bug in a rule (e.g., null reference, divide by zero)
        // =====================================================================
        if (target.Name == "ThrowException")
        {
            throw new InvalidOperationException("Simulated rule bug");
        }

        return Task.FromResult<IRuleMessages>(None);
    }
}

// =============================================================================
// ERROR BOUNDARY PATTERNS
// =============================================================================
// Where to catch exceptions in a typical Neatoo application:
//
// 1. UI LAYER (Blazor/WPF)
//    - Catch exceptions from Save() operations
//    - Display user-friendly error message
//    - Log exception for developers
//
// 2. API LAYER (ASP.NET Core)
//    - Global exception handler middleware
//    - Return appropriate HTTP status codes
//    - Don't expose internal details to client
//
// 3. FACTORY OPERATIONS
//    - DON'T catch exceptions here (usually)
//    - Let them bubble up to error boundary
//    - Exception: Retry logic for transient failures
//
// PATTERN: Single error boundary, not distributed try/catch.
//
// WRONG (distributed error handling):
//   try {
//       entity.Name = "Test";
//   } catch { /* handle */ }
//
//   try {
//       await entity.Save();
//   } catch { /* handle */ }
//
// RIGHT (single error boundary):
//   try {
//       entity.Name = "Test";
//       await entity.WaitForTasks();
//       if (!entity.IsValid) {
//           // Show validation errors from PropertyMessages
//           return;
//       }
//       await entity.Save();
//       // Success
//   } catch (Exception ex) {
//       // Single place to handle unexpected errors
//       Logger.Error(ex);
//       ShowErrorDialog("An unexpected error occurred");
//   }
// =============================================================================

// =============================================================================
// BLAZOR ERROR BOUNDARY EXAMPLE
// =============================================================================
//
// @* In App.razor or layout *@
// <ErrorBoundary @ref="_errorBoundary">
//     <ChildContent>
//         @Body
//     </ChildContent>
//     <ErrorContent Context="exception">
//         <div class="alert alert-danger">
//             An error occurred. Please try again.
//         </div>
//     </ErrorContent>
// </ErrorBoundary>
//
// @* In component with Save button *@
// private async Task OnSaveClicked()
// {
//     try
//     {
//         await _entity.WaitForTasks();
//         if (!_entity.IsValid)
//         {
//             // Validation errors shown via PropertyMessages binding
//             return;
//         }
//         await _entity.Save();
//         NavigationManager.NavigateTo("/success");
//     }
//     catch (Exception ex)
//     {
//         Logger.LogError(ex, "Save failed");
//         _errorBoundary.Recover();  // Reset error boundary
//         _errorMessage = "Failed to save. Please try again.";
//     }
// }
// =============================================================================

// =============================================================================
// FACTORY EXCEPTION TYPES
// =============================================================================
// Neatoo defines these exception types:
//
// ConfigurationException (base)
//   - Base class for framework configuration errors
//
// TypeNotRegisteredException : ConfigurationException
//   - Service type not registered in DI
//
// RuleException (base)
//   - Base class for rule-related errors
//
// TargetIsNullException : RuleException
//   - RuleManager created with null target
//
// InvalidTargetTypeException : RuleException
//   - Rule target type doesn't match
//
// RuleNotAddedException : RuleException
//   - Attempted to run a rule not registered with RuleManager
//
// SaveOperationException
//   - Invalid save operation (e.g., Save() on child entity)
//
// FactoryException
//   - General factory operation error
// =============================================================================

// =============================================================================
// ASYNC RULE CANCELLATION
// =============================================================================
// Rules can be cancelled via CancellationToken:
//
// - Passed to RunRules(propertyName, token) and RunRules(flags, token)
// - Rules check token.IsCancellationRequested before executing
// - Cancelled rules throw OperationCanceledException
// - Object is marked invalid with "Validation cancelled" message
//
// PATTERN: Use cancellation for long-running validation (e.g., server calls).
//
//   var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
//   try {
//       await entity.RunRules(RunRulesFlag.All, cts.Token);
//   } catch (OperationCanceledException) {
//       // Validation timed out
//       // entity.IsValid = false with "Validation cancelled" message
//   }
//
// After cancellation, you must explicitly re-run rules:
//   await entity.RunRules(RunRulesFlag.All);  // Clears cancellation message
// =============================================================================

// =============================================================================
// Support Interfaces
// =============================================================================

public interface IErrorDemoRepository
{
    (string Name, int Quantity, string Email) GetById(int id);
}
