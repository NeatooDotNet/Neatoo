// -----------------------------------------------------------------------------
// Design.Domain - Rule System Basics
// -----------------------------------------------------------------------------
// This file documents the Neatoo rule system: RuleBase<T>, AsyncRuleBase<T>,
// trigger properties, rule execution, and rule messages.
// -----------------------------------------------------------------------------

using Neatoo;
using Neatoo.RemoteFactory;
using Neatoo.Rules;

namespace Design.Domain.Rules;

// =============================================================================
// Rule System Overview
// =============================================================================
// Neatoo provides two ways to define rules:
//
// 1. RuleBase<T> Classes: For complex, reusable rules
//    - Inherit from AsyncRuleBase<T> (all rules are async-capable)
//    - Override Execute() to implement rule logic
//    - Register with RuleManager.AddRule(new MyRule())
//
// 2. Fluent API: For simple inline rules
//    - RuleManager.AddValidation() for validation rules
//    - RuleManager.AddAction() for side-effect rules
//    - See FluentRules.cs for fluent API patterns
//
// DESIGN DECISION: All rules extend AsyncRuleBase<T> internally.
// Even synchronous rules are async-capable for consistency.
// "RuleBase<T>" in documentation refers to AsyncRuleBase<T>.
//
// DID NOT DO THIS: Have separate sync and async rule hierarchies.
//
// REJECTED PATTERN:
//   public class SyncRule : SyncRuleBase<T> { ... }
//   public class AsyncRule : AsyncRuleBase<T> { ... }
//
// ACTUAL PATTERN:
//   public class MyRule : AsyncRuleBase<T> { ... }
//   // Execute can be sync or async - framework handles both
//
// WHY NOT: Maintaining two hierarchies adds complexity. Async rules that
// happen to be sync just return completed tasks - no performance penalty.
// =============================================================================

/// <summary>
/// Demonstrates: RuleBase/AsyncRuleBase for custom validation rules.
/// </summary>
[Factory]
public partial class RuleBasicsDemo : EntityBase<RuleBasicsDemo>
{
    public partial string? Name { get; set; }
    public partial int Quantity { get; set; }
    public partial decimal Price { get; set; }
    public partial decimal Total { get; set; }

    public RuleBasicsDemo(IEntityBaseServices<RuleBasicsDemo> services) : base(services)
    {
        // Register class-based rules
        RuleManager.AddRule(new NameRequiredRule());
        RuleManager.AddRule(new CalculateTotalRule());
    }

    [Create]
    public void Create() { }

    [Remote]
    [Fetch]
    public void Fetch(int id, [Service] IRulesDemoRepository repository)
    {
        var data = repository.GetById(id);
        this["Name"].LoadValue(data.Name);
        this["Quantity"].LoadValue(data.Quantity);
        this["Price"].LoadValue(data.Price);
        this["Total"].LoadValue(data.Total);
    }

    [Remote]
    [Insert]
    public void Insert([Service] IRulesDemoRepository repository) { }

    [Remote]
    [Update]
    public void Update([Service] IRulesDemoRepository repository) { }

    [Remote]
    [Delete]
    public void Delete([Service] IRulesDemoRepository repository) { }
}

// =============================================================================
// Class-Based Rules - AsyncRuleBase<T>
// =============================================================================
// For complex rules that need:
// - Multiple trigger properties
// - Reuse across multiple entity types
// - Complex logic that benefits from being a class
// - Testability as a separate unit
//
// Rule class members:
// - TriggerProperties: Properties that trigger this rule when changed
// - Execute(): The rule logic (can be sync or async)
// - Return: IRuleMessages with validation errors or empty for success
// =============================================================================

/// <summary>
/// Demonstrates: Simple validation rule as a class.
/// </summary>
public class NameRequiredRule : AsyncRuleBase<RuleBasicsDemo>
{
    // =========================================================================
    // TriggerProperties - When Does This Rule Run?
    // =========================================================================
    // Rules run when ANY trigger property changes.
    // Specify trigger properties via the base constructor using expressions.
    // =========================================================================
    public NameRequiredRule() : base(t => t.Name) { }

    // =========================================================================
    // Execute - Rule Logic
    // =========================================================================
    // Return IRuleMessages:
    // - (propertyName, message).AsRuleMessages(): Validation failed
    // - None (inherited from AsyncRuleBase): Validation passed (no messages)
    //
    // The messages are associated with the specified property.
    // =========================================================================
    protected override Task<IRuleMessages> Execute(RuleBasicsDemo target, CancellationToken? token = null)
    {
        if (string.IsNullOrWhiteSpace(target.Name))
        {
            // Return error - this makes IsValid=false
            return Task.FromResult<IRuleMessages>(
                (nameof(RuleBasicsDemo.Name), "Name is required").AsRuleMessages());
        }

        // Return None - validation passed (None is inherited from AsyncRuleBase)
        return Task.FromResult<IRuleMessages>(None);
    }
}

/// <summary>
/// Demonstrates: Action rule that computes derived values.
/// </summary>
public class CalculateTotalRule : AsyncRuleBase<RuleBasicsDemo>
{
    // =========================================================================
    // Multiple Trigger Properties
    // =========================================================================
    // This rule triggers when Quantity OR Price changes.
    // Both properties contribute to the calculated Total.
    // Pass multiple expressions to the base constructor.
    // =========================================================================
    public CalculateTotalRule() : base(t => t.Quantity, t => t.Price) { }

    protected override Task<IRuleMessages> Execute(RuleBasicsDemo target, CancellationToken? token = null)
    {
        // Calculate derived value
        target.Total = target.Quantity * target.Price;

        // Action rules typically return None (no validation message)
        return Task.FromResult<IRuleMessages>(None);
    }
}

// =============================================================================
// Rule Messages - IRuleMessages
// =============================================================================
// Rules return IRuleMessages to communicate results:
//
// None (inherited from AsyncRuleBase): No messages, validation passed
// (propertyName, message).AsRuleMessages(): Error message, makes IsValid=false
//
// Multiple messages can be combined using fluent API:
//   return new RuleMessages()
//       .If(condition1, propertyName, "First error")
//       .If(condition2, propertyName, "Second error");
//
// Or using RuleMessages.If for single conditional message:
//   return RuleMessages.If(condition, propertyName, "Error message");
//
// DESIGN DECISION: Rules return messages, not throw exceptions.
// Validation failures are expected - they're not exceptional conditions.
// Exceptions in rules are actual errors (bugs, external failures).
// =============================================================================

/// <summary>
/// Demonstrates: Rule returning multiple messages.
/// </summary>
public class MultiMessageRule : AsyncRuleBase<RuleBasicsDemo>
{
    public MultiMessageRule() : base(t => t.Quantity, t => t.Price) { }

    protected override Task<IRuleMessages> Execute(RuleBasicsDemo target, CancellationToken? token = null)
    {
        // Use fluent API to build multiple conditional messages
        var result = new RuleMessages()
            .If(target.Quantity < 0, nameof(RuleBasicsDemo.Quantity), "Quantity cannot be negative")
            .If(target.Price < 0, nameof(RuleBasicsDemo.Price), "Price cannot be negative")
            .If(target.Quantity > 1000, nameof(RuleBasicsDemo.Quantity), "Quantity exceeds maximum order limit");

        return Task.FromResult<IRuleMessages>(result);
    }
}

// =============================================================================
// Rule Execution Order
// =============================================================================
// Rules execute in registration order by default.
// Use RuleOrder property to control execution order.
//
// DESIGN DECISION: Lower RuleOrder values execute first.
// Default is 0. Use negative values for early rules, positive for late.
//
// Example:
//   RuleOrder = -10  // Runs early
//   RuleOrder = 0    // Default
//   RuleOrder = 10   // Runs late
// =============================================================================

/// <summary>
/// Demonstrates: Rule ordering.
/// </summary>
public class EarlyValidationRule : AsyncRuleBase<RuleBasicsDemo>
{
    public EarlyValidationRule() : base(t => t.Name)
    {
        // This rule runs before rules with default RuleOrder (1)
        // Lower values execute first
        RuleOrder = -10;
    }

    protected override Task<IRuleMessages> Execute(RuleBasicsDemo target, CancellationToken? token = null)
    {
        // Early validation - check preconditions
        return Task.FromResult<IRuleMessages>(None);
    }
}

// =============================================================================
// Running Rules Manually
// =============================================================================
// Rules normally run automatically when trigger properties change.
// You can also run rules manually:
//
// RunRules(propertyName): Run rules for specific property
// RunRules(RunRulesFlag.All): Run all rules, clear all messages first
// RunRules(RunRulesFlag.Self): Run this object's rules only
// RunRules(RunRulesFlag.Children): Run children's rules only
//
// await WaitForTasks(): Wait for async rules to complete
// =============================================================================

// =============================================================================
// Support Interfaces
// =============================================================================

public interface IRulesDemoRepository
{
    (string Name, int Quantity, decimal Price, decimal Total) GetById(int id);
}
