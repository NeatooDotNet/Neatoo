# Business Rules

[← Blazor](blazor.md) | [↑ Guides](index.md) | [Change Tracking →](change-tracking.md)

Business rules in Neatoo implement validation and side effects that execute when properties change. Rules are registered in the entity constructor via `RuleManager` fluent API or by adding custom rule class instances. The framework automatically executes rules when their trigger properties are modified.

## Fluent Action Rules

Action rules perform side effects like calculating derived properties. Action rules do not produce validation messages or affect the entity's `IsValid` state.

Register synchronous actions with `AddAction`:

<!-- snippet: rules-add-action -->
<a id='snippet-rules-add-action'></a>
```cs
public RulesContact(IValidateBaseServices<RulesContact> services) : base(services)
{
    // Register action that computes FullName from FirstName and LastName
    RuleManager.AddAction(
        contact => contact.FullName = $"{contact.FirstName} {contact.LastName}",
        c => c.FirstName, c => c.LastName);
}
```
<sup><a href='/src/docs/samples/BusinessRulesSamples.cs#L50-L58' title='Snippet source file'>snippet source</a> | <a href='#snippet-rules-add-action' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The action executes whenever `FirstName` or `LastName` changes.

For async actions that call external services:

<!-- snippet: rules-add-action-async -->
<a id='snippet-rules-add-action-async'></a>
```cs
public RulesProduct(
    IValidateBaseServices<RulesProduct> services,
    IPricingService pricingService) : base(services)
{
    // Register async action that fetches tax rate from external service
    RuleManager.AddActionAsync(
        async product =>
        {
            product.TaxRate = await pricingService.GetTaxRateAsync(product.ZipCode);
        },
        p => p.ZipCode);
}
```
<sup><a href='/src/docs/samples/BusinessRulesSamples.cs#L76-L89' title='Snippet source file'>snippet source</a> | <a href='#snippet-rules-add-action-async' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The `RuleManager.AddAction` method creates an `ActionFluentRule<T>` internally that executes the lambda when trigger properties change. The lambda can modify other properties on the entity without creating validation messages.

## Fluent Validation Rules

Validation rules check business constraints and produce error messages when validation fails. Validation messages affect the entity's `IsValid` state and are displayed in the UI.

Register synchronous validation with `AddValidation`:

<!-- snippet: rules-add-validation -->
<a id='snippet-rules-add-validation'></a>
```cs
public RulesInvoice(IValidateBaseServices<RulesInvoice> services) : base(services)
{
    // Register validation that checks Amount is positive
    RuleManager.AddValidation(
        invoice => invoice.Amount > 0 ? "" : "Amount must be greater than zero",
        i => i.Amount);
}
```
<sup><a href='/src/docs/samples/BusinessRulesSamples.cs#L105-L113' title='Snippet source file'>snippet source</a> | <a href='#snippet-rules-add-validation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The `RuleManager.AddValidation` method creates a `ValidationFluentRule<T>` internally that executes the lambda when trigger properties change. Return an empty or null string to indicate validation passed. Any other value becomes the error message associated with the trigger property.

For async validation:

<!-- snippet: rules-add-validation-async -->
<a id='snippet-rules-add-validation-async'></a>
```cs
public RulesOrder(
    IValidateBaseServices<RulesOrder> services,
    IInventoryService inventoryService) : base(services)
{
    // Register async validation that checks inventory
    RuleManager.AddValidationAsync(
        async order =>
        {
            var inStock = await inventoryService.IsInStockAsync(order.ProductCode);
            return inStock ? "" : "Product is out of stock";
        },
        o => o.ProductCode);
}
```
<sup><a href='/src/docs/samples/BusinessRulesSamples.cs#L127-L141' title='Snippet source file'>snippet source</a> | <a href='#snippet-rules-add-validation-async' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Async validation with cancellation token support:

<!-- snippet: rules-add-validation-async-token -->
<a id='snippet-rules-add-validation-async-token'></a>
```cs
public RulesBooking(
    IValidateBaseServices<RulesBooking> services,
    IInventoryService inventoryService) : base(services)
{
    // Register async validation with cancellation token support
    RuleManager.AddValidationAsync(
        async (booking, token) =>
        {
            var available = await inventoryService.IsInStockAsync(booking.ResourceId, token);
            return available ? "" : "Resource is not available";
        },
        b => b.ResourceId);
}
```
<sup><a href='/src/docs/samples/BusinessRulesSamples.cs#L155-L169' title='Snippet source file'>snippet source</a> | <a href='#snippet-rules-add-validation-async-token' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Cross-Property Validation

Rules can trigger on multiple properties and validate relationships between them. Use custom rule classes that inherit from `RuleBase<T>` or `AsyncRuleBase<T>` and declare multiple trigger properties in the constructor.

<!-- snippet: rules-cross-property -->
<a id='snippet-rules-cross-property'></a>
```cs
public RulesEvent(IValidateBaseServices<RulesEvent> services) : base(services)
{
    // Use a custom rule class for cross-property validation
    // that triggers on both StartDate and EndDate
    RuleManager.AddRule(new DateRangeValidationRule());
}
```
<sup><a href='/src/docs/samples/BusinessRulesSamples.cs#L183-L190' title='Snippet source file'>snippet source</a> | <a href='#snippet-rules-cross-property' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The rule executes when either `StartDate` or `EndDate` changes and validates their relationship. See the Custom Rule Classes section below for implementation details.

## Custom Rule Classes

For complex logic, inherit from `RuleBase<T>` or `AsyncRuleBase<T>`.

Synchronous custom rule:

<!-- snippet: rules-custom-class -->
<a id='snippet-rules-custom-class'></a>
```cs
/// <summary>
/// Custom synchronous rule that validates salary is within range.
/// </summary>
public class SalaryRangeRule : RuleBase<RulesEmployee>
{
    private readonly decimal _minSalary;
    private readonly decimal _maxSalary;

    public SalaryRangeRule(decimal minSalary, decimal maxSalary)
        : base(e => e.Salary)
    {
        _minSalary = minSalary;
        _maxSalary = maxSalary;
    }

    protected override IRuleMessages Execute(RulesEmployee target)
    {
        if (target.Salary < _minSalary)
        {
            return (nameof(RulesEmployee.Salary), $"Salary must be at least {_minSalary:C}").AsRuleMessages();
        }

        if (target.Salary > _maxSalary)
        {
            return (nameof(RulesEmployee.Salary), $"Salary cannot exceed {_maxSalary:C}").AsRuleMessages();
        }

        return None;
    }
}
```
<sup><a href='/src/docs/samples/BusinessRulesSamples.cs#L221-L252' title='Snippet source file'>snippet source</a> | <a href='#snippet-rules-custom-class' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Return `None` when validation passes. Create rule messages with the `AsRuleMessages()` extension.

Async custom rule with external dependencies:

<!-- snippet: rules-async-custom-class -->
<a id='snippet-rules-async-custom-class'></a>
```cs
/// <summary>
/// Custom async rule that validates product availability.
/// </summary>
public class ProductAvailabilityRule : AsyncRuleBase<RulesOrderItem>
{
    private readonly IInventoryService _inventoryService;

    public ProductAvailabilityRule(IInventoryService inventoryService)
        : base(o => o.ProductCode)
    {
        _inventoryService = inventoryService;
    }

    protected override async Task<IRuleMessages> Execute(
        RulesOrderItem target,
        CancellationToken? token = null)
    {
        var isAvailable = await _inventoryService.IsInStockAsync(
            target.ProductCode,
            token ?? CancellationToken.None);

        if (!isAvailable)
        {
            return (nameof(RulesOrderItem.ProductCode), "Product is not available").AsRuleMessages();
        }

        return None;
    }
}
```
<sup><a href='/src/docs/samples/BusinessRulesSamples.cs#L254-L284' title='Snippet source file'>snippet source</a> | <a href='#snippet-rules-async-custom-class' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Custom rules support dependency injection through constructor parameters.

## Business Rule Attributes

Neatoo automatically converts DataAnnotations validation attributes to business rules. During ValidateBase construction, the `RuleManager` scans properties for validation attributes and converts them to rules using the `IAttributeToRule` service.

Standard validation attributes work without explicit registration:

<!-- snippet: rules-attribute-standard -->
<a id='snippet-rules-attribute-standard'></a>
```cs
[Factory]
public partial class RulesAttributeEntity : ValidateBase<RulesAttributeEntity>
{
    public RulesAttributeEntity(IValidateBaseServices<RulesAttributeEntity> services) : base(services) { }

    [Required]
    public partial string Name { get; set; }

    [StringLength(100, MinimumLength = 2)]
    public partial string Description { get; set; }

    [EmailAddress]
    public partial string Email { get; set; }

    [Range(0, 150)]
    public partial int Age { get; set; }

    [RegularExpression(@"^\d{5}(-\d{4})?$", ErrorMessage = "Invalid ZIP code format")]
    public partial string ZipCode { get; set; }

    [Create]
    public void Create() { }
}
```
<sup><a href='/src/docs/samples/BusinessRulesSamples.cs#L902-L926' title='Snippet source file'>snippet source</a> | <a href='#snippet-rules-attribute-standard' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Supported attributes include `[Required]`, `[StringLength]`, `[MinLength]`, `[MaxLength]`, `[RegularExpression]`, `[Range]`, and `[EmailAddress]`.

## Aggregate-Level Rules

Rules execute on an entity instance and can access the entire aggregate graph via navigation properties. This allows validation of business invariants that span multiple entities within the aggregate boundary.

<!-- snippet: rules-aggregate-level -->
<a id='snippet-rules-aggregate-level'></a>
```cs
/// <summary>
/// Rule that validates across the entire aggregate.
/// </summary>
public class AggregateValidationRule : RuleBase<RulesAggregateRoot>
{
    public AggregateValidationRule() : base(r => r.TotalBudget) { }

    protected override IRuleMessages Execute(RulesAggregateRoot target)
    {
        // Sum all line item amounts in the aggregate
        var totalAmount = target.LineItems?.Sum(item => item.Amount) ?? 0;

        if (totalAmount > target.TotalBudget)
        {
            return (nameof(target.TotalBudget),
                $"Total line items ({totalAmount:C}) exceed budget ({target.TotalBudget:C})").AsRuleMessages();
        }

        return None;
    }
}
```
<sup><a href='/src/docs/samples/BusinessRulesSamples.cs#L606-L628' title='Snippet source file'>snippet source</a> | <a href='#snippet-rules-aggregate-level' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The `Execute` method receives the target entity instance. Use navigation properties to traverse child entities and collections within the aggregate.

## Rule Execution Order

When a property changes, the framework identifies all rules with that property as a trigger, sorts them by `RuleOrder` (ascending), then executes them sequentially. Lower `RuleOrder` values execute first. Default is 1.

<!-- snippet: rules-execution-order -->
<a id='snippet-rules-execution-order'></a>
```cs
/// <summary>
/// Rule with explicit order that executes first (order = 0).
/// </summary>
public class FirstExecutionRule : RuleBase<RulesOrderedEntity>
{
    public FirstExecutionRule() : base(e => e.Value)
    {
        // Lower RuleOrder executes first
        RuleOrder = 0;
    }

    protected override IRuleMessages Execute(RulesOrderedEntity target)
    {
        target.ExecutionLog.Add("First");
        return None;
    }
}

/// <summary>
/// Rule with default order that executes second (order = 1).
/// </summary>
public class SecondExecutionRule : RuleBase<RulesOrderedEntity>
{
    public SecondExecutionRule() : base(e => e.Value)
    {
        // Default RuleOrder is 1
    }

    protected override IRuleMessages Execute(RulesOrderedEntity target)
    {
        target.ExecutionLog.Add("Second");
        return None;
    }
}

/// <summary>
/// Rule with higher order that executes last (order = 2).
/// </summary>
public class ThirdExecutionRule : RuleBase<RulesOrderedEntity>
{
    public ThirdExecutionRule() : base(e => e.Value)
    {
        RuleOrder = 2;
    }

    protected override IRuleMessages Execute(RulesOrderedEntity target)
    {
        target.ExecutionLog.Add("Third");
        return None;
    }
}
```
<sup><a href='/src/docs/samples/BusinessRulesSamples.cs#L290-L342' title='Snippet source file'>snippet source</a> | <a href='#snippet-rules-execution-order' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Set `RuleOrder` in the rule constructor to control execution sequence. This ensures rules with dependencies execute in the correct order. Within the same `RuleOrder` value, rules execute in registration order.

Async rules also execute sequentially. Each async rule completes before the next rule begins, even if they have the same `RuleOrder`.

## Conditional Rules

Rules always execute when their trigger properties change, but can use conditional logic to skip validation based on entity state. Return `None` from the `Execute` method to indicate no validation errors.

<!-- snippet: rules-conditional -->
<a id='snippet-rules-conditional'></a>
```cs
/// <summary>
/// Rule that only validates when entity is active.
/// </summary>
public class ConditionalValidationRule : RuleBase<RulesConditionalEntity>
{
    public ConditionalValidationRule() : base(e => e.Value, e => e.IsActive) { }

    protected override IRuleMessages Execute(RulesConditionalEntity target)
    {
        // Skip validation when not active
        if (!target.IsActive)
        {
            return None;
        }

        if (string.IsNullOrEmpty(target.Value))
        {
            return (nameof(RulesConditionalEntity.Value), "Value is required when active").AsRuleMessages();
        }

        return None;
    }
}
```
<sup><a href='/src/docs/samples/BusinessRulesSamples.cs#L348-L372' title='Snippet source file'>snippet source</a> | <a href='#snippet-rules-conditional' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The rule executes but produces no messages when returning `None`. This pattern is useful for state-dependent validation.

For more complex scenarios, check conditions before executing expensive operations:

<!-- snippet: rules-conditional-early-exit -->
<a id='snippet-rules-conditional-early-exit'></a>
```cs
/// <summary>
/// Rule with early exit for performance optimization.
/// </summary>
public class EarlyExitRule : AsyncRuleBase<RulesConditionalEntity>
{
    private readonly IInventoryService _inventoryService;

    public EarlyExitRule(IInventoryService inventoryService)
        : base(e => e.ProductCode)
    {
        _inventoryService = inventoryService;
    }

    protected override async Task<IRuleMessages> Execute(
        RulesConditionalEntity target,
        CancellationToken? token = null)
    {
        // Early exit: skip expensive check if product code is empty
        if (string.IsNullOrEmpty(target.ProductCode))
        {
            return None;
        }

        // Only call external service when necessary
        var isAvailable = await _inventoryService.IsInStockAsync(
            target.ProductCode,
            token ?? CancellationToken.None);

        return isAvailable ? None : (nameof(target.ProductCode), "Product unavailable").AsRuleMessages();
    }
}
```
<sup><a href='/src/docs/samples/BusinessRulesSamples.cs#L374-L406' title='Snippet source file'>snippet source</a> | <a href='#snippet-rules-conditional-early-exit' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Async Business Rules

Async rules support cancellation tokens and long-running operations.

<!-- snippet: rules-async-cancellation -->
<a id='snippet-rules-async-cancellation'></a>
```cs
/// <summary>
/// Async rule with proper cancellation token handling.
/// </summary>
public class CancellableValidationRule : AsyncRuleBase<RulesCancellableEntity>
{
    private readonly IPricingService _pricingService;

    public CancellableValidationRule(IPricingService pricingService)
        : base(e => e.ZipCode)
    {
        _pricingService = pricingService;
    }

    protected override async Task<IRuleMessages> Execute(
        RulesCancellableEntity target,
        CancellationToken? token = null)
    {
        var ct = token ?? CancellationToken.None;

        // Check cancellation before expensive operation
        ct.ThrowIfCancellationRequested();

        // Pass token to async calls
        var taxRate = await _pricingService.GetTaxRateAsync(target.ZipCode, ct);
        target.ComputedTaxRate = taxRate;

        return None;
    }
}
```
<sup><a href='/src/docs/samples/BusinessRulesSamples.cs#L412-L442' title='Snippet source file'>snippet source</a> | <a href='#snippet-rules-async-cancellation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The framework passes cancellation tokens through the entire rule execution chain, allowing rules to cooperate with cancellation.

Async rules automatically track busy state. The framework marks trigger properties as busy before execution and clears the busy state after completion. This provides automatic visual feedback in UI scenarios through the `IsBusy` property.

## LoadProperty - Preventing Rule Recursion

When a rule needs to set a property without triggering other rules, use `LoadProperty`:

<!-- snippet: rules-load-property -->
<a id='snippet-rules-load-property'></a>
```cs
/// <summary>
/// Rule that uses LoadProperty to set values without triggering other rules.
/// </summary>
public class ComputedTotalRule : RuleBase<RulesOrderWithTotal>
{
    public ComputedTotalRule() : base(e => e.Quantity, e => e.UnitPrice) { }

    protected override IRuleMessages Execute(RulesOrderWithTotal target)
    {
        var total = target.Quantity * target.UnitPrice;

        // LoadProperty sets the value without triggering rules on Total
        LoadProperty(target, t => t.Total, total);

        return None;
    }
}
```
<sup><a href='/src/docs/samples/BusinessRulesSamples.cs#L448-L466' title='Snippet source file'>snippet source</a> | <a href='#snippet-rules-load-property' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`LoadProperty` is a protected method on `RuleBase<T>` that bypasses the normal property setter. It writes directly to the backing field via the property wrapper, preventing cascading rule execution and infinite loops. Use this when a rule needs to update a property without triggering rules registered on that property.

## Rule Registration Patterns

Rules are registered in the target class constructor.

Fluent rules for simple scenarios:

<!-- snippet: rules-registration-fluent -->
<a id='snippet-rules-registration-fluent'></a>
```cs
public RulesFluentEntity(IValidateBaseServices<RulesFluentEntity> services) : base(services)
{
    // Action rule: compute derived value
    RuleManager.AddAction(
        e => e.FullName = $"{e.FirstName} {e.LastName}",
        e => e.FirstName, e => e.LastName);

    // Validation rule: check business constraint
    RuleManager.AddValidation(
        e => e.Age >= 18 ? "" : "Must be 18 or older",
        e => e.Age);

    // Async action with external service call
    RuleManager.AddActionAsync(
        async e =>
        {
            await Task.Delay(1); // Simulate async operation
            e.Processed = true;
        },
        e => e.FirstName);
}
```
<sup><a href='/src/docs/samples/BusinessRulesSamples.cs#L955-L977' title='Snippet source file'>snippet source</a> | <a href='#snippet-rules-registration-fluent' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Custom rule classes for complex logic with dependencies:

<!-- snippet: rules-registration-custom -->
<a id='snippet-rules-registration-custom'></a>
```cs
public RulesCustomEntity(
    IValidateBaseServices<RulesCustomEntity> services,
    CustomBusinessRule businessRule) : base(services)
{
    // Register injected custom rule class
    RuleManager.AddRule(businessRule);
}
```
<sup><a href='/src/docs/samples/BusinessRulesSamples.cs#L999-L1007' title='Snippet source file'>snippet source</a> | <a href='#snippet-rules-registration-custom' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Custom rules injected via constructor support dependency injection and can be shared across multiple targets.

## Trigger Properties

Rules declare which properties trigger their execution. When any trigger property changes, the rule executes. Trigger properties are specified in the rule constructor via lambda expressions.

Single trigger property:

<!-- snippet: rules-trigger-single -->
<a id='snippet-rules-trigger-single'></a>
```cs
/// <summary>
/// Rule triggered by a single property.
/// </summary>
public class SingleTriggerRule : RuleBase<RulesTriggerEntity>
{
    public SingleTriggerRule() : base(e => e.Email) { }

    protected override IRuleMessages Execute(RulesTriggerEntity target)
    {
        target.EmailLower = target.Email?.ToLowerInvariant();
        return None;
    }
}
```
<sup><a href='/src/docs/samples/BusinessRulesSamples.cs#L472-L486' title='Snippet source file'>snippet source</a> | <a href='#snippet-rules-trigger-single' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Multiple trigger properties:

<!-- snippet: rules-trigger-multiple -->
<a id='snippet-rules-trigger-multiple'></a>
```cs
/// <summary>
/// Rule triggered by multiple properties.
/// </summary>
public class MultipleTriggerRule : RuleBase<RulesTriggerEntity>
{
    public MultipleTriggerRule() : base(e => e.City, e => e.State, e => e.ZipCode) { }

    protected override IRuleMessages Execute(RulesTriggerEntity target)
    {
        target.FullAddress = $"{target.City}, {target.State} {target.ZipCode}";
        return None;
    }
}
```
<sup><a href='/src/docs/samples/BusinessRulesSamples.cs#L488-L502' title='Snippet source file'>snippet source</a> | <a href='#snippet-rules-trigger-multiple' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Add triggers after construction:

<!-- snippet: rules-trigger-add-later -->
<a id='snippet-rules-trigger-add-later'></a>
```cs
/// <summary>
/// Rule with triggers added after construction.
/// </summary>
public class DynamicTriggerRule : RuleBase<RulesTriggerEntity>
{
    public DynamicTriggerRule()
    {
        // Add trigger properties after construction
        AddTriggerProperties(e => e.FirstName);
        AddTriggerProperties(e => e.LastName);
    }

    protected override IRuleMessages Execute(RulesTriggerEntity target)
    {
        target.DisplayName = $"{target.LastName}, {target.FirstName}";
        return None;
    }
}
```
<sup><a href='/src/docs/samples/BusinessRulesSamples.cs#L504-L523' title='Snippet source file'>snippet source</a> | <a href='#snippet-rules-trigger-add-later' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Rule Messages

Rules return `IRuleMessages` containing zero or more validation messages.

Return no messages when validation passes:

<!-- snippet: rules-messages-none -->
<a id='snippet-rules-messages-none'></a>
```cs
/// <summary>
/// Rule that returns no messages when validation passes.
/// </summary>
public class PassingValidationRule : RuleBase<RulesMessageEntity>
{
    public PassingValidationRule() : base(e => e.Status) { }

    protected override IRuleMessages Execute(RulesMessageEntity target)
    {
        // Validation passes - return None
        if (target.Status == "Active" || target.Status == "Pending")
        {
            return None;
        }

        return (nameof(target.Status), "Invalid status").AsRuleMessages();
    }
}
```
<sup><a href='/src/docs/samples/BusinessRulesSamples.cs#L529-L548' title='Snippet source file'>snippet source</a> | <a href='#snippet-rules-messages-none' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Return a single message:

<!-- snippet: rules-messages-single -->
<a id='snippet-rules-messages-single'></a>
```cs
/// <summary>
/// Rule that returns a single validation message.
/// </summary>
public class SingleMessageRule : RuleBase<RulesMessageEntity>
{
    public SingleMessageRule() : base(e => e.Age) { }

    protected override IRuleMessages Execute(RulesMessageEntity target)
    {
        if (target.Age < 0)
        {
            return (nameof(target.Age), "Age cannot be negative").AsRuleMessages();
        }

        return None;
    }
}
```
<sup><a href='/src/docs/samples/BusinessRulesSamples.cs#L550-L568' title='Snippet source file'>snippet source</a> | <a href='#snippet-rules-messages-single' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Return multiple messages for different properties:

<!-- snippet: rules-messages-multiple -->
<a id='snippet-rules-messages-multiple'></a>
```cs
/// <summary>
/// Rule that returns multiple validation messages for different properties.
/// </summary>
public class MultipleMessagesRule : RuleBase<RulesMessageEntity>
{
    public MultipleMessagesRule() : base(e => e.StartDate, e => e.EndDate) { }

    protected override IRuleMessages Execute(RulesMessageEntity target)
    {
        var messages = new List<(string, string)>();

        if (target.StartDate == default)
        {
            messages.Add((nameof(target.StartDate), "Start date is required"));
        }

        if (target.EndDate == default)
        {
            messages.Add((nameof(target.EndDate), "End date is required"));
        }

        if (messages.Count > 0)
        {
            return messages.ToArray().AsRuleMessages();
        }

        return None;
    }
}
```
<sup><a href='/src/docs/samples/BusinessRulesSamples.cs#L570-L600' title='Snippet source file'>snippet source</a> | <a href='#snippet-rules-messages-multiple' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Messages are automatically associated with properties and displayed through the validation UI.

## Manual Rule Execution

Rules execute automatically when trigger properties change, but can also be run manually. Manual execution is useful for re-validating after external state changes, running validation before save operations, or executing rules that haven't fired yet.

Run all rules:

<!-- snippet: rules-run-all -->
<a id='snippet-rules-run-all'></a>
```cs
// Run all registered rules regardless of which properties changed
await entity.RunRules(RunRulesFlag.All);
```
<sup><a href='/src/docs/samples/BusinessRulesSamples.cs#L1365-L1368' title='Snippet source file'>snippet source</a> | <a href='#snippet-rules-run-all' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The `RunRulesFlag` enum supports different execution modes:
- `All`: Run all rules
- `NotExecuted`: Run only rules that haven't executed yet
- `Executed`: Run only rules that have already executed
- `NoMessages`: Run rules that produced no validation messages
- `Messages`: Run rules that produced validation messages

Run rules for a specific property:

<!-- snippet: rules-run-property -->
<a id='snippet-rules-run-property'></a>
```cs
// Run rules only for the specified property
await entity.RunRules(nameof(entity.Value));
```
<sup><a href='/src/docs/samples/BusinessRulesSamples.cs#L1381-L1384' title='Snippet source file'>snippet source</a> | <a href='#snippet-rules-run-property' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

This executes only rules that have the specified property as a trigger. The framework looks up all rules registered with that property name as a trigger, sorts by `RuleOrder`, and executes them sequentially.

Run a specific rule type:

<!-- snippet: rules-run-specific -->
<a id='snippet-rules-run-specific'></a>
```cs
// Run rules of a specific type (custom method on entity)
await employee.RunSalaryRangeRules();
```
<sup><a href='/src/docs/samples/BusinessRulesSamples.cs#L1397-L1400' title='Snippet source file'>snippet source</a> | <a href='#snippet-rules-run-specific' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

For targeted rule type execution, expose a custom method on the entity that calls `RuleManager.RunRule<TRule>()` to execute a specific rule class by type.

## Advanced: Stable Rule IDs

Neatoo assigns deterministic rule IDs based on the source expression used to register rules. The `RuleManager` uses `CallerArgumentExpression` to capture the exact lambda expression text when calling `AddAction`, `AddValidation`, or `AddRule`.

These stable IDs ensure:
- Validation messages can be matched between client and server in RemoteFactory scenarios
- Rule tracking remains consistent across application restarts
- The same rule in different instances generates the same ID

For custom rule classes, the rule ID is based on the rule's type name. For fluent rules, the ID is a hash of the source expression text.

---

**UPDATED:** 2026-01-24
