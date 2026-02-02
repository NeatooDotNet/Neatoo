// -----------------------------------------------------------------------------
// Design.Domain - RuleManager Fluent API
// -----------------------------------------------------------------------------
// This file demonstrates the fluent API for adding inline rules directly
// in the entity constructor. This is the most common way to define rules.
// -----------------------------------------------------------------------------

using Neatoo;
using Neatoo.RemoteFactory;

namespace Design.Domain.Rules;

// =============================================================================
// RuleManager Fluent API Overview
// =============================================================================
// The fluent API provides methods for common rule patterns:
//
// AddValidation(): Synchronous validation returning error message
// AddValidationAsync(): Async validation returning error message
// AddAction(): Synchronous side-effect (compute values)
// AddActionAsync(): Async side-effect
//
// DESIGN DECISION: Separate methods for validation vs action.
// - Validation returns string (error message or empty)
// - Action returns void (just performs work)
//
// This distinction makes intent clear and simplifies the API.
// =============================================================================

/// <summary>
/// Demonstrates: RuleManager fluent API for inline rules.
/// </summary>
[Factory]
public partial class FluentRulesDemo : EntityBase<FluentRulesDemo>
{
    public partial string? Name { get; set; }
    public partial string? Email { get; set; }
    public partial int Quantity { get; set; }
    public partial decimal UnitPrice { get; set; }
    public partial decimal Total { get; set; }
    public partial string? Status { get; set; }

    public FluentRulesDemo(IEntityBaseServices<FluentRulesDemo> services) : base(services)
    {
        // =====================================================================
        // AddValidation - Synchronous Validation Rules
        // =====================================================================
        // Signature: AddValidation(Func<T, string> func, Expression<Func<T, object?>> triggerProperty)
        //
        // func: Returns error message (string) or empty string for valid
        // triggerProperty: Property that triggers this rule when changed
        //
        // The error message is associated with the trigger property.
        // =====================================================================

        RuleManager.AddValidation(
            t => string.IsNullOrWhiteSpace(t.Name) ? "Name is required" : string.Empty,
            t => t.Name);

        RuleManager.AddValidation(
            t => t.Name?.Length > 100 ? "Name cannot exceed 100 characters" : string.Empty,
            t => t.Name);

        // Multiple validation rules on same property - both run when Name changes
        RuleManager.AddValidation(
            t => t.Name?.Contains("test", StringComparison.OrdinalIgnoreCase) == true
                ? "Name cannot contain 'test'" : string.Empty,
            t => t.Name);

        // =====================================================================
        // AddAction - Synchronous Action Rules
        // =====================================================================
        // Signature: AddAction(Action<T> func, Expression<Func<T, object?>> triggerProperty)
        //
        // func: Action to perform (typically compute derived values)
        // triggerProperty: Property that triggers this rule
        //
        // Action rules don't return validation messages - they just do work.
        //
        // DESIGN DECISION: Multiple overloads exist for 1, 2, 3 trigger properties.
        // This is because CallerArgumentExpression is incompatible with params.
        // For 4+ triggers, use the array overload.
        // =====================================================================

        // Single trigger property
        RuleManager.AddAction(
            t => t.Status = $"Name set to: {t.Name}",
            t => t.Name);

        // Two trigger properties
        RuleManager.AddAction(
            t => t.Total = t.Quantity * t.UnitPrice,
            t => t.Quantity,
            t => t.UnitPrice);

        // Three trigger properties (uncommon but supported)
        // RuleManager.AddAction(
        //     t => DoSomething(t.A, t.B, t.C),
        //     t => t.A,
        //     t => t.B,
        //     t => t.C);

        // Array of trigger properties (for 4+)
        // RuleManager.AddAction(
        //     t => DoSomething(t),
        //     new[] { t => t.A, t => t.B, t => t.C, t => t.D });

        // =====================================================================
        // AddValidationAsync - Async Validation Rules
        // =====================================================================
        // Signature: AddValidationAsync(Func<T, Task<string>> func, Expression<Func<T, object?>> triggerProperty)
        //
        // func: Async function returning error message or empty string
        // triggerProperty: Property that triggers this rule
        //
        // Use for: Database lookups, external API calls, I/O operations
        // =====================================================================

        RuleManager.AddValidationAsync(
            async t =>
            {
                if (string.IsNullOrWhiteSpace(t.Email))
                    return string.Empty;  // Skip validation for empty

                // Simulate async email validation
                await Task.Delay(10);
                if (!t.Email.Contains('@'))
                    return "Invalid email format";

                return string.Empty;
            },
            t => t.Email);

        // =====================================================================
        // AddActionAsync - Async Action Rules
        // =====================================================================
        // Signature: AddActionAsync(Func<T, Task> func, Expression<Func<T, object?>> triggerProperty)
        //
        // func: Async action to perform
        // triggerProperty: Property that triggers this rule
        //
        // Use for: Fetching related data, async calculations
        // =====================================================================

        RuleManager.AddActionAsync(
            async t =>
            {
                // Simulate fetching related data
                await Task.Delay(10);
                t.Status = $"Validated: {t.Email}";
            },
            t => t.Email);
    }

    [Create]
    public void Create() { }

    [Remote]
    [Fetch]
    public void Fetch(int id, [Service] IFluentRulesRepository repository)
    {
        var data = repository.GetById(id);
        this["Name"].LoadValue(data.Name);
        this["Email"].LoadValue(data.Email);
        this["Quantity"].LoadValue(data.Quantity);
        this["UnitPrice"].LoadValue(data.UnitPrice);
        this["Total"].LoadValue(data.Total);
    }

    [Remote]
    [Insert]
    public void Insert([Service] IFluentRulesRepository repository) { }

    [Remote]
    [Update]
    public void Update([Service] IFluentRulesRepository repository) { }

    [Remote]
    [Delete]
    public void Delete([Service] IFluentRulesRepository repository) { }
}

// =============================================================================
// Rule Trigger Expression Patterns
// =============================================================================
// The trigger property is specified as an expression: t => t.PropertyName
// This provides:
// - Compile-time checking of property name
// - Refactoring support (rename works automatically)
// - Clear association between rule and property
//
// COMMON MISTAKE: Using the wrong property expression.
//
// WRONG:
//   RuleManager.AddValidation(
//       t => t.A + t.B > 100 ? "Too high" : "",
//       t => t.A);  // Only triggers on A, not B!
//
// RIGHT:
//   RuleManager.AddAction(
//       t => { if (t.A + t.B > 100) t.HasWarning = true; },
//       t => t.A,
//       t => t.B);  // Triggers on both A and B
// =============================================================================

/// <summary>
/// Demonstrates: Different trigger property patterns.
/// </summary>
[Factory]
public partial class TriggerPatternsDemo : ValidateBase<TriggerPatternsDemo>
{
    public partial int A { get; set; }
    public partial int B { get; set; }
    public partial int C { get; set; }
    public partial int Sum { get; set; }
    public partial bool IsOverLimit { get; set; }

    public TriggerPatternsDemo(IValidateBaseServices<TriggerPatternsDemo> services) : base(services)
    {
        // Rule that depends on multiple properties
        RuleManager.AddAction(
            t => t.Sum = t.A + t.B + t.C,
            t => t.A,
            t => t.B,
            t => t.C);

        // Validation that checks cross-property constraint
        // Note: Must list ALL properties involved to trigger correctly
        RuleManager.AddValidation(
            t => t.Sum > 100 ? "Sum cannot exceed 100" : string.Empty,
            t => t.Sum);

        // Action triggered by computed property
        RuleManager.AddAction(
            t => t.IsOverLimit = t.Sum > 100,
            t => t.Sum);
    }

    [Create]
    public void Create() { }
}

// =============================================================================
// Fluent Rule Return Values
// =============================================================================
// The fluent methods return the created rule for advanced scenarios:
//
// var rule = RuleManager.AddValidation(...);
// rule.RuleOrder = -10;  // Customize execution order
//
// DESIGN DECISION: Return the rule to enable customization.
// Most code ignores the return value - it's for advanced cases.
// =============================================================================

// =============================================================================
// CallerArgumentExpression and Rule IDs
// =============================================================================
// GENERATOR BEHAVIOR: RuleManager uses CallerArgumentExpression to capture
// the rule expression as a string. This generates stable rule IDs.
//
// Why this matters:
// - Rules are identified by ID for serialization/deserialization
// - Changing the rule expression changes the ID
// - This can cause issues if rules are stored in session state
//
// DID NOT DO THIS: Use reflection to generate rule IDs.
//
// REJECTED PATTERN:
//   // Generate ID from method info at runtime
//   var ruleId = methodInfo.GetHashCode();
//
// ACTUAL PATTERN:
//   // Generate ID from source expression at compile time
//   var ruleId = Hash("t => t.Name == null ? \"Error\" : \"\"");
//
// WHY NOT: Reflection is slow and doesn't work with AOT.
// CallerArgumentExpression provides compile-time string capture.
// =============================================================================

// =============================================================================
// Support Interfaces
// =============================================================================

public interface IFluentRulesRepository
{
    (string Name, string Email, int Quantity, decimal UnitPrice, decimal Total) GetById(int id);
}
