# Business Rules

[← Blazor](blazor.md) | [↑ Guides](index.md) | [Change Tracking →](change-tracking.md)

Business rules in Neatoo implement validation and side effects that execute when properties change. Rules are registered with the `RuleManager` and automatically execute when their trigger properties are modified.

## Fluent Action Rules

Action rules perform side effects like calculating derived properties without producing validation messages.

Register synchronous actions with `AddAction`:

<!-- snippet: rules-add-action -->
```cs
public RulesContact(IValidateBaseServices<RulesContact> services) : base(services)
{
    // Register action that computes FullName from FirstName and LastName
    RuleManager.AddAction(
        contact => contact.FullName = $"{contact.FirstName} {contact.LastName}",
        c => c.FirstName, c => c.LastName);
}
```
<!-- endSnippet -->

The action executes whenever `FirstName` or `LastName` changes.

For async actions that call external services:

<!-- snippet: rules-add-action-async -->
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
<!-- endSnippet -->

Actions always return `RuleMessages.None` since they don't perform validation.

## Fluent Validation Rules

Validation rules return error messages when validation fails.

Register synchronous validation with `AddValidation`:

<!-- snippet: rules-add-validation -->
```cs
public RulesInvoice(IValidateBaseServices<RulesInvoice> services) : base(services)
{
    // Register validation that checks Amount is positive
    RuleManager.AddValidation(
        invoice => invoice.Amount > 0 ? "" : "Amount must be greater than zero",
        i => i.Amount);
}
```
<!-- endSnippet -->

Return an empty or null string to indicate validation passed. Any other value becomes the error message associated with the trigger property.

For async validation:

<!-- snippet: rules-add-validation-async -->
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
<!-- endSnippet -->

Async validation with cancellation token support:

<!-- snippet: rules-add-validation-async-token -->
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
<!-- endSnippet -->

## Cross-Property Validation

Rules can trigger on multiple properties and validate relationships between them.

<!-- snippet: rules-cross-property -->
```cs
public RulesEvent(IValidateBaseServices<RulesEvent> services) : base(services)
{
    // Use a custom rule class for cross-property validation
    // that triggers on both StartDate and EndDate
    RuleManager.AddRule(new DateRangeValidationRule());
}
```
<!-- endSnippet -->

The rule executes when either `StartDate` or `EndDate` changes and validates their relationship.

## Custom Rule Classes

For complex logic, inherit from `RuleBase<T>` or `AsyncRuleBase<T>`.

Synchronous custom rule:

<!-- snippet: rules-custom-class -->
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
<!-- endSnippet -->

Return `None` when validation passes. Create rule messages with the `AsRuleMessages()` extension.

Async custom rule with external dependencies:

<!-- snippet: rules-async-custom-class -->
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
<!-- endSnippet -->

Custom rules support dependency injection through constructor parameters.

## Business Rule Attributes

Neatoo automatically converts validation attributes to rules.

Standard validation attributes work without registration:

<!-- snippet: rules-attribute-standard -->
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
}
```
<!-- endSnippet -->

Supported attributes include `[Required]`, `[StringLength]`, `[MinLength]`, `[MaxLength]`, `[RegularExpression]`, `[Range]`, and `[EmailAddress]`.

## Aggregate-Level Rules

Rules can access the entire aggregate and validate business invariants.

<!-- snippet: rules-aggregate-level -->
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
<!-- endSnippet -->

Aggregate-level rules receive the root entity and can traverse the entire object graph.

## Rule Execution Order

Rules execute in order based on the `RuleOrder` property. Lower values execute first (default is 1).

<!-- snippet: rules-execution-order -->
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
<!-- endSnippet -->

Set `RuleOrder` in the constructor to control execution sequence. This ensures dependencies between rules execute in the correct order.

Within the same order value, rules execute in registration order.

## Conditional Rules

Rules can contain conditional logic to execute validation only when certain conditions are met.

<!-- snippet: rules-conditional -->
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
<!-- endSnippet -->

Return `None` to skip validation. The rule still executes but produces no messages.

For more complex scenarios, check conditions before executing expensive operations:

<!-- snippet: rules-conditional-early-exit -->
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
<!-- endSnippet -->

## Async Business Rules

Async rules support cancellation tokens and long-running operations.

<!-- snippet: rules-async-cancellation -->
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
<!-- endSnippet -->

The framework passes cancellation tokens through the entire rule execution chain, allowing rules to cooperate with cancellation.

Async rules automatically mark properties as busy during execution, providing visual feedback in UI scenarios.

## LoadProperty - Preventing Rule Recursion

When a rule needs to set a property without triggering other rules, use `LoadProperty`:

<!-- snippet: rules-load-property -->
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
<!-- endSnippet -->

`LoadProperty` bypasses the normal property setter, preventing cascading rule execution and infinite loops.

## Rule Registration Patterns

Rules are registered in the target class constructor.

Fluent rules for simple scenarios:

<!-- snippet: rules-registration-fluent -->
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
<!-- endSnippet -->

Custom rule classes for complex logic with dependencies:

<!-- snippet: rules-registration-custom -->
```cs
public RulesCustomEntity(
    IValidateBaseServices<RulesCustomEntity> services,
    CustomBusinessRule businessRule) : base(services)
{
    // Register injected custom rule class
    RuleManager.AddRule(businessRule);
}
```
<!-- endSnippet -->

Custom rules injected via constructor support dependency injection and can be shared across multiple targets.

## Trigger Properties

Rules specify which properties trigger their execution.

Single trigger property:

<!-- snippet: rules-trigger-single -->
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
<!-- endSnippet -->

Multiple trigger properties:

<!-- snippet: rules-trigger-multiple -->
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
<!-- endSnippet -->

Add triggers after construction:

<!-- snippet: rules-trigger-add-later -->
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
<!-- endSnippet -->

## Rule Messages

Rules return `IRuleMessages` containing zero or more validation messages.

Return no messages when validation passes:

<!-- snippet: rules-messages-none -->
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
<!-- endSnippet -->

Return a single message:

<!-- snippet: rules-messages-single -->
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
<!-- endSnippet -->

Return multiple messages for different properties:

<!-- snippet: rules-messages-multiple -->
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
<!-- endSnippet -->

Messages are automatically associated with properties and displayed through the validation UI.

## Manual Rule Execution

While rules execute automatically on property changes, you can also run them manually.

Run all rules:

<!-- snippet: rules-run-all -->
```cs
[Fact]
public async Task RunAllRules_ExecutesAllRegisteredRules()
{
    var entity = new RulesManualEntity(new ValidateBaseServices<RulesManualEntity>());

    // Set invalid value
    entity.Value = -1;

    // Manually run all rules
    await entity.RunRules(RunRulesFlag.All);

    Assert.False(entity.IsValid);
}
```
<!-- endSnippet -->

Run rules for a specific property:

<!-- snippet: rules-run-property -->
```cs
[Fact]
public async Task RunRulesForProperty_ExecutesPropertyRules()
{
    var entity = new RulesManualEntity(new ValidateBaseServices<RulesManualEntity>());

    entity.Value = -1;

    // Run rules only for the Value property using public method
    await entity.RunRules(nameof(entity.Value));

    Assert.False(entity.IsValid);
}
```
<!-- endSnippet -->

Run a specific rule instance:

<!-- snippet: rules-run-specific -->
```cs
[Fact]
public async Task RunSpecificRule_ExecutesSingleRule()
{
    var salaryRule = new SalaryRangeRule(30000m, 200000m);
    var employee = new RulesEmployee(new ValidateBaseServices<RulesEmployee>(), salaryRule);

    employee.Salary = 25000m;

    // Run only rules of a specific type
    await employee.RunSalaryRangeRules();

    Assert.False(employee.IsValid);
}
```
<!-- endSnippet -->

Manual execution is useful when re-validating after external state changes or before saving.

## Advanced: Stable Rule IDs

Neatoo assigns deterministic rule IDs based on the source expression used to register rules. This ensures validation messages can be matched between client and server in RemoteFactory scenarios.

Rule IDs are generated automatically using `CallerArgumentExpression` and remain stable across application restarts.

---

**UPDATED:** 2026-01-24
