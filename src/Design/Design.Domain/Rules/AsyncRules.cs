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
// Each property change triggers rules. For a rule that calls the server,
// bind the property so it is set on field commit, not per keystroke.
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
internal partial class AsyncRulesDemo : EntityBase<AsyncRulesDemo>, IAsyncRulesDemo
{
    public partial string? Email { get; set; }
    public partial string? Username { get; set; }
    public partial bool IsUsernameAvailable { get; set; }
    public partial string? ExternalData { get; set; }

    #region skill-rule-injected
    // A rule with a dependency comes from DI through its interface. The
    // dependency must exist on both tiers - here, a command delegate.
    public AsyncRulesDemo(
        IEntityBaseServices<AsyncRulesDemo> services,
        ICheckUsernameAvailabilityRule usernameAvailabilityRule) : base(services)
    {
        // Register async rules
        RuleManager.AddRule(new ValidateEmailFormatRule());
        RuleManager.AddRule(usernameAvailabilityRule);
        RuleManager.AddRule(new FetchExternalDataRule());
    }
    #endregion

    [Create]
    public void Create() { }

    [Remote]
    [Fetch]
    internal void Fetch(int id, [Service] IAsyncRulesRepository repository)
    {
        var data = repository.GetById(id);
        Email = data.Email;
        Username = data.Username;
    }

    [Remote]
    [Insert]
    internal void Insert([Service] IAsyncRulesRepository repository) { }

    [Remote]
    [Update]
    internal void Update([Service] IAsyncRulesRepository repository) { }

    [Remote]
    [Delete]
    internal void Delete([Service] IAsyncRulesRepository repository) { }
}

// =============================================================================
// Async Validation Rule That Calls the Server - Rule Plus Command
// =============================================================================
// Checking a value against the database (is this username taken?) belongs in
// a rule, so the user sees the message on the client as they edit. The rule
// runs on the client, so it cannot take a server-only service. It takes the
// delegate of a [Remote, Execute] command instead; calling the delegate
// crosses to the server, where the command resolves the repository.
//
// DESIGN DECISION: The rule depends on a command delegate, never on a
// server-only service.
//
// DID NOT DO THIS:
//   public CheckUsernameAvailabilityRule(IUsernameRepository repository)
//
// WHY NOT: Rules are constructed by DI with their entity on each tier. A
// server-only service makes the entity impossible to build on the client,
// and the failure appears only in a client build.
//
// Bind the property so it is set on field commit, not per keystroke: each
// change makes one server call.
// =============================================================================

#region skill-rule-command
/// <summary>
/// Command the rule calls. [Remote]: a client call crosses to the server.
/// </summary>
[Factory]
public static partial class UsernameAvailability
{
    [Remote]
    [Execute]
    private static Task<bool> _IsAvailable(string username, [Service] IUsernameRepository repository)
    {
        return Task.FromResult(!repository.UsernameExists(username));
    }
}
#endregion

#region skill-rule-with-command
/// <summary>
/// Rule interface: the entity takes the rule from DI by this interface, and
/// tests can substitute it.
/// </summary>
internal interface ICheckUsernameAvailabilityRule : IRule<AsyncRulesDemo> { }

/// <summary>
/// Demonstrates: async uniqueness validation through a [Remote, Execute] command.
/// </summary>
internal class CheckUsernameAvailabilityRule : AsyncRuleBase<AsyncRulesDemo>, ICheckUsernameAvailabilityRule
{
    private readonly UsernameAvailability.IsAvailable _isAvailable;

    // Trigger properties are passed to the base constructor
    public CheckUsernameAvailabilityRule(UsernameAvailability.IsAvailable isAvailable) : base(t => t.Username)
    {
        _isAvailable = isAvailable;
    }

    protected override async Task<IRuleMessages> Execute(AsyncRulesDemo target, CancellationToken? token = null)
    {
        if (string.IsNullOrWhiteSpace(target.Username))
        {
            target.IsUsernameAvailable = false;
            return None;  // Don't check empty usernames - None is inherited from AsyncRuleBase
        }

        var available = await _isAvailable(target.Username);

        target.IsUsernameAvailable = available;

        if (!available)
        {
            // Create error message: (propertyName, message).AsRuleMessages()
            return (nameof(AsyncRulesDemo.Username), $"Username '{target.Username}' is already taken").AsRuleMessages();
        }

        return None;
    }
}
#endregion

// =============================================================================
// Synchronous Rule Beside Async Ones - RuleBase<T>
// =============================================================================
// A rule with no I/O derives from RuleBase<T> and returns IRuleMessages
// directly. It runs in the same pipeline as the async rules.
// =============================================================================

#region skill-sync-rule
/// <summary>
/// Demonstrates: synchronous validation with RuleBase&lt;T&gt;.
/// </summary>
internal class ValidateEmailFormatRule : RuleBase<AsyncRulesDemo>
{
    public ValidateEmailFormatRule() : base(t => t.Email) { }

    protected override IRuleMessages Execute(AsyncRulesDemo target)
    {
        if (string.IsNullOrWhiteSpace(target.Email))
        {
            return (nameof(AsyncRulesDemo.Email), "Email is required").AsRuleMessages();
        }

        if (!target.Email.Contains('@'))
        {
            return (nameof(AsyncRulesDemo.Email), "Email must contain @").AsRuleMessages();
        }

        return None;
    }
}
#endregion

// =============================================================================
// Async Action Rule - Fetch External Data
// =============================================================================
// Action rules perform side effects (compute values, fetch data).
// They typically return None since they're not validation.
// =============================================================================

/// <summary>
/// Demonstrates: Async action rule that fetches external data.
/// </summary>
internal class FetchExternalDataRule : AsyncRuleBase<AsyncRulesDemo>
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

#region skill-cancellable-rule
/// <summary>
/// Demonstrates: Rule with cancellation support.
/// </summary>
internal class CancellableRule : AsyncRuleBase<AsyncRulesDemo>
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
#endregion

// =============================================================================
// IsBusy and Async Rule Coordination
// =============================================================================
// When multiple async rules run:
// - IsBusy is true while ANY rule is running
// - WaitForTasks() awaits ALL pending operations
// - IsValid reflects combined result of all completed rules
//
// DESIGN DECISION: Rules triggered by one property change run one after
// another, each awaited, in RuleOrder. Rules triggered by different property
// changes can overlap: setting Email and then Username starts the Username
// rules while the Email rules may still be running.
// =============================================================================

// =============================================================================
// Support Interfaces
// =============================================================================

public interface IAsyncRulesRepository
{
    (string Email, string Username) GetById(int id);
}

public interface IUsernameRepository
{
    bool UsernameExists(string username);
}
