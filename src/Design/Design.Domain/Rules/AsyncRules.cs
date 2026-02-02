// -----------------------------------------------------------------------------
// Design.Domain - Async Rule Patterns
// -----------------------------------------------------------------------------
// This file demonstrates async validation and action rules that perform
// I/O operations, call external services, or have other async requirements.
// -----------------------------------------------------------------------------

using Neatoo;
using Neatoo.RemoteFactory;
using Neatoo.Rules;

namespace Design.Domain.Rules;

// =============================================================================
// Async Rules Overview
// =============================================================================
// Async rules are crucial for:
// - Database lookups (uniqueness checks, existence validation)
// - External service calls (address validation, credit checks)
// - Complex calculations that should not block UI
//
// When async rules run:
// 1. IsBusy becomes true
// 2. Rule executes asynchronously
// 3. On completion, IsBusy becomes false (when all async operations done)
// 4. IsValid/IsSelfValid updated based on rule result
//
// DESIGN DECISION: Async rules run immediately, not debounced.
// Each property change triggers rules. For expensive operations,
// consider implementing debouncing in the rule logic itself.
//
// COMMON MISTAKE: Not waiting for async rules before checking validity.
//
// WRONG:
//   entity.Name = "Test";  // Triggers async validation
//   if (entity.IsValid) { }  // MIGHT BE STALE - rule still running!
//
// RIGHT:
//   entity.Name = "Test";
//   await entity.WaitForTasks();  // Wait for async rules
//   if (entity.IsValid) { }  // Now accurate
// =============================================================================

/// <summary>
/// Demonstrates: Async validation and action rules.
/// </summary>
[Factory]
public partial class AsyncRulesDemo : EntityBase<AsyncRulesDemo>
{
    public partial string? Email { get; set; }
    public partial string? Username { get; set; }
    public partial bool IsUsernameAvailable { get; set; }
    public partial string? ExternalData { get; set; }

    public AsyncRulesDemo(IEntityBaseServices<AsyncRulesDemo> services) : base(services)
    {
        // Register async rules
        RuleManager.AddRule(new ValidateEmailFormatRule());
        RuleManager.AddRule(new CheckUsernameAvailabilityRule());
        RuleManager.AddRule(new FetchExternalDataRule());
    }

    [Create]
    public void Create() { }

    [Remote]
    [Fetch]
    public void Fetch(int id, [Service] IAsyncRulesRepository repository)
    {
        var data = repository.GetById(id);
        this["Email"].LoadValue(data.Email);
        this["Username"].LoadValue(data.Username);
    }

    [Remote]
    [Insert]
    public void Insert([Service] IAsyncRulesRepository repository) { }

    [Remote]
    [Update]
    public void Update([Service] IAsyncRulesRepository repository) { }

    [Remote]
    [Delete]
    public void Delete([Service] IAsyncRulesRepository repository) { }
}

// =============================================================================
// Async Validation Rule - Database Lookup
// =============================================================================
// This pattern is common: check if a value is unique in the database.
// The rule must be async because it queries external data.
// =============================================================================

/// <summary>
/// Demonstrates: Async uniqueness validation via database lookup.
/// </summary>
public class CheckUsernameAvailabilityRule : AsyncRuleBase<AsyncRulesDemo>
{
    // =========================================================================
    // Service Injection in Rules
    // =========================================================================
    // Rules can have services injected via constructor.
    // Register the rule with DI, or pass the service when creating the rule.
    //
    // DESIGN DECISION: Rules are typically registered in entity constructor.
    // If the rule needs services, pass them via constructor:
    //   RuleManager.AddRule(new CheckUsernameRule(usernameService));
    //
    // Or register the rule in DI and inject it:
    //   public MyEntity(IEntityBaseServices services, CheckUsernameRule rule)
    //   { RuleManager.AddRule(rule); }
    // =========================================================================
    private readonly IUsernameService? _usernameService;

    // =========================================================================
    // TriggerProperties - Specified via Constructor
    // =========================================================================
    // Rules specify trigger properties by passing expressions to the base
    // constructor. The base class maintains the TriggerProperties list.
    // =========================================================================
    public CheckUsernameAvailabilityRule() : base(t => t.Username) { }

    public CheckUsernameAvailabilityRule(IUsernameService usernameService) : base(t => t.Username)
    {
        _usernameService = usernameService;
    }

    protected override async Task<IRuleMessages> Execute(AsyncRulesDemo target, CancellationToken? token = null)
    {
        if (string.IsNullOrWhiteSpace(target.Username))
        {
            target.IsUsernameAvailable = false;
            return None;  // Don't check empty usernames - None is inherited from AsyncRuleBase
        }

        // Simulate async database lookup
        // In real code: var available = await _usernameService.IsAvailable(target.Username);
        await Task.Delay(100);  // Simulated I/O
        var available = target.Username != "taken";

        target.IsUsernameAvailable = available;

        if (!available)
        {
            // Create error message: (propertyName, message).AsRuleMessages()
            return (nameof(AsyncRulesDemo.Username), $"Username '{target.Username}' is already taken").AsRuleMessages();
        }

        return None;
    }
}

// =============================================================================
// Sync Rule That Returns Task - Still Async-Capable
// =============================================================================
// Even synchronous rules use the async signature.
// Return Task.FromResult<IRuleMessages>() for sync operations.
// =============================================================================

/// <summary>
/// Demonstrates: Sync validation that uses async signature.
/// </summary>
public class ValidateEmailFormatRule : AsyncRuleBase<AsyncRulesDemo>
{
    public ValidateEmailFormatRule() : base(t => t.Email) { }

    protected override Task<IRuleMessages> Execute(AsyncRulesDemo target, CancellationToken? token = null)
    {
        // This is a sync operation - just string validation
        if (string.IsNullOrWhiteSpace(target.Email))
        {
            return Task.FromResult<IRuleMessages>(
                (nameof(AsyncRulesDemo.Email), "Email is required").AsRuleMessages());
        }

        if (!target.Email.Contains('@'))
        {
            return Task.FromResult<IRuleMessages>(
                (nameof(AsyncRulesDemo.Email), "Email must contain @").AsRuleMessages());
        }

        return Task.FromResult<IRuleMessages>(None);
    }
}

// =============================================================================
// Async Action Rule - Fetch External Data
// =============================================================================
// Action rules perform side effects (compute values, fetch data).
// They typically return Empty since they're not validation.
// =============================================================================

/// <summary>
/// Demonstrates: Async action rule that fetches external data.
/// </summary>
public class FetchExternalDataRule : AsyncRuleBase<AsyncRulesDemo>
{
    public FetchExternalDataRule() : base(t => t.Email) { }

    protected override async Task<IRuleMessages> Execute(AsyncRulesDemo target, CancellationToken? token = null)
    {
        if (string.IsNullOrWhiteSpace(target.Email))
        {
            target.ExternalData = null;
            return None;
        }

        // Simulate fetching data from external service
        await Task.Delay(50);  // Simulated I/O
        target.ExternalData = $"Data for {target.Email}";

        return None;  // Action rules typically return None (no validation messages)
    }
}

// =============================================================================
// Cancellation Support
// =============================================================================
// Rules receive an optional CancellationToken for cancellation support.
// Long-running async rules should check the token.
//
// When rules are cancelled:
// - OperationCanceledException is propagated
// - Object is marked invalid with "Validation cancelled"
// - Must call RunRules(RunRulesFlag.All) to re-validate
// =============================================================================

/// <summary>
/// Demonstrates: Rule with cancellation support.
/// </summary>
public class CancellableRule : AsyncRuleBase<AsyncRulesDemo>
{
    public CancellableRule() : base(t => t.Username) { }

    protected override async Task<IRuleMessages> Execute(AsyncRulesDemo target, CancellationToken? token = null)
    {
        // Check cancellation before expensive operation
        token?.ThrowIfCancellationRequested();

        // Simulate expensive async operation
        await Task.Delay(1000);

        // Check cancellation again for very long operations
        token?.ThrowIfCancellationRequested();

        return None;
    }
}

// =============================================================================
// IsBusy and Async Rule Coordination
// =============================================================================
// When multiple async rules run:
// - IsBusy is true while ANY rule is running
// - WaitForTasks() awaits ALL pending operations
// - IsValid reflects combined result of all completed rules
//
// DESIGN DECISION: Rules can run in parallel.
// If rules must run sequentially, combine them into one rule.
// =============================================================================

// =============================================================================
// Support Interfaces
// =============================================================================

public interface IAsyncRulesRepository
{
    (string Email, string Username) GetById(int id);
}

public interface IUsernameService
{
    Task<bool> IsAvailable(string username);
}
