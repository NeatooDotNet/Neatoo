using Neatoo;
using Neatoo.Internal;
using Neatoo.RemoteFactory;
using Neatoo.Rules;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace Samples;

// External service interface for async rule samples
public interface IPricingService
{
    Task<decimal> GetTaxRateAsync(string zipCode, CancellationToken token = default);
}

public class MockPricingService : IPricingService
{
    public Task<decimal> GetTaxRateAsync(string zipCode, CancellationToken token = default)
    {
        // California zip codes get higher tax rate
        var rate = zipCode?.StartsWith("9") == true ? 0.0825m : 0.07m;
        return Task.FromResult(rate);
    }
}

public interface IInventoryService
{
    Task<bool> IsInStockAsync(string productCode, CancellationToken token = default);
}

public class MockInventoryService : IInventoryService
{
    public Task<bool> IsInStockAsync(string productCode, CancellationToken token = default)
    {
        // Products starting with "OUT" are out of stock
        return Task.FromResult(!productCode.StartsWith("OUT"));
    }
}

// -----------------------------------------------------------------
// Entity classes for business rules samples
// -----------------------------------------------------------------

/// <summary>
/// Contact entity demonstrating fluent action rules.
/// </summary>
[Factory]
public partial class RulesContact : ValidateBase<RulesContact>
{
    #region rules-add-action
    public RulesContact(IValidateBaseServices<RulesContact> services) : base(services)
    {
        // Register action that computes FullName from FirstName and LastName
        RuleManager.AddAction(
            contact => contact.FullName = $"{contact.FirstName} {contact.LastName}",
            c => c.FirstName, c => c.LastName);
    }
    #endregion

    public partial string FirstName { get; set; }

    public partial string LastName { get; set; }

    public partial string FullName { get; set; }
}

/// <summary>
/// Product entity demonstrating async action rules.
/// </summary>
[Factory]
public partial class RulesProduct : ValidateBase<RulesProduct>
{
    #region rules-add-action-async
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
    #endregion

    public partial string ZipCode { get; set; }

    public partial decimal TaxRate { get; set; }
}

/// <summary>
/// Invoice entity demonstrating synchronous validation rules.
/// </summary>
[Factory]
public partial class RulesInvoice : ValidateBase<RulesInvoice>
{
    #region rules-add-validation
    public RulesInvoice(IValidateBaseServices<RulesInvoice> services) : base(services)
    {
        // Register validation that checks Amount is positive
        RuleManager.AddValidation(
            invoice => invoice.Amount > 0 ? "" : "Amount must be greater than zero",
            i => i.Amount);
    }
    #endregion

    public partial decimal Amount { get; set; }
}

/// <summary>
/// Order entity demonstrating async validation rules.
/// </summary>
[Factory]
public partial class RulesOrder : ValidateBase<RulesOrder>
{
    #region rules-add-validation-async
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
    #endregion

    public partial string ProductCode { get; set; }
}

/// <summary>
/// Booking entity demonstrating async validation with cancellation token.
/// </summary>
[Factory]
public partial class RulesBooking : ValidateBase<RulesBooking>
{
    #region rules-add-validation-async-token
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
    #endregion

    public partial string ResourceId { get; set; }
}

/// <summary>
/// Event entity demonstrating cross-property validation.
/// </summary>
[Factory]
public partial class RulesEvent : ValidateBase<RulesEvent>
{
    #region rules-cross-property
    public RulesEvent(IValidateBaseServices<RulesEvent> services) : base(services)
    {
        // Use a custom rule class for cross-property validation
        // that triggers on both StartDate and EndDate
        RuleManager.AddRule(new DateRangeValidationRule());
    }
    #endregion

    public partial DateTime StartDate { get; set; }

    public partial DateTime EndDate { get; set; }
}

/// <summary>
/// Custom rule for cross-property date validation.
/// </summary>
public class DateRangeValidationRule : RuleBase<RulesEvent>
{
    public DateRangeValidationRule() : base(e => e.StartDate, e => e.EndDate) { }

    protected override IRuleMessages Execute(RulesEvent target)
    {
        if (target.EndDate <= target.StartDate)
        {
            return (nameof(target.EndDate), "End date must be after start date").AsRuleMessages();
        }
        return None;
    }
}

// -----------------------------------------------------------------
// Custom rule classes
// -----------------------------------------------------------------

#region rules-custom-class
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
#endregion

#region rules-async-custom-class
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
#endregion

// -----------------------------------------------------------------
// Rule order demonstration
// -----------------------------------------------------------------

#region rules-execution-order
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
#endregion

// -----------------------------------------------------------------
// Conditional rules
// -----------------------------------------------------------------

#region rules-conditional
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
#endregion

#region rules-conditional-early-exit
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
#endregion

// -----------------------------------------------------------------
// Async cancellation rule
// -----------------------------------------------------------------

#region rules-async-cancellation
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
#endregion

// -----------------------------------------------------------------
// LoadProperty rule
// -----------------------------------------------------------------

#region rules-load-property
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
#endregion

// -----------------------------------------------------------------
// Trigger property rules
// -----------------------------------------------------------------

#region rules-trigger-single
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
#endregion

#region rules-trigger-multiple
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
#endregion

#region rules-trigger-add-later
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
#endregion

// -----------------------------------------------------------------
// Rule messages rules
// -----------------------------------------------------------------

#region rules-messages-none
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
#endregion

#region rules-messages-single
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
#endregion

#region rules-messages-multiple
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
#endregion

// -----------------------------------------------------------------
// Aggregate-level rule
// -----------------------------------------------------------------

#region rules-aggregate-level
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
#endregion

// -----------------------------------------------------------------
// Entity classes used by custom rules
// -----------------------------------------------------------------

/// <summary>
/// Employee entity for salary validation sample.
/// </summary>
[Factory]
public partial class RulesEmployee : ValidateBase<RulesEmployee>
{
    public RulesEmployee(
        IValidateBaseServices<RulesEmployee> services,
        SalaryRangeRule? salaryRule = null) : base(services)
    {
        if (salaryRule != null)
        {
            RuleManager.AddRule(salaryRule);
        }
    }

    public partial decimal Salary { get; set; }

    public partial string Name { get; set; }

    /// <summary>
    /// Runs all rules of the specified type.
    /// </summary>
    public Task RunSalaryRangeRules(CancellationToken? token = null)
    {
        return RuleManager.RunRule<SalaryRangeRule>(token);
    }
}

/// <summary>
/// Order item entity for product availability sample.
/// </summary>
[Factory]
public partial class RulesOrderItem : ValidateBase<RulesOrderItem>
{
    public RulesOrderItem(
        IValidateBaseServices<RulesOrderItem> services,
        ProductAvailabilityRule? availabilityRule = null) : base(services)
    {
        if (availabilityRule != null)
        {
            RuleManager.AddRule(availabilityRule);
        }
    }

    public partial string ProductCode { get; set; }

    public partial int Quantity { get; set; }
}

/// <summary>
/// Entity for rule execution order sample.
/// </summary>
[Factory]
public partial class RulesOrderedEntity : ValidateBase<RulesOrderedEntity>
{
    public RulesOrderedEntity(
        IValidateBaseServices<RulesOrderedEntity> services,
        FirstExecutionRule? firstRule = null,
        SecondExecutionRule? secondRule = null,
        ThirdExecutionRule? thirdRule = null) : base(services)
    {
        ExecutionLog = new List<string>();

        if (firstRule != null) RuleManager.AddRule(firstRule);
        if (secondRule != null) RuleManager.AddRule(secondRule);
        if (thirdRule != null) RuleManager.AddRule(thirdRule);
    }

    public List<string> ExecutionLog { get; }

    public partial string Value { get; set; }
}

/// <summary>
/// Entity for conditional rule sample.
/// </summary>
[Factory]
public partial class RulesConditionalEntity : ValidateBase<RulesConditionalEntity>
{
    public RulesConditionalEntity(
        IValidateBaseServices<RulesConditionalEntity> services,
        ConditionalValidationRule? conditionalRule = null,
        EarlyExitRule? earlyExitRule = null) : base(services)
    {
        if (conditionalRule != null) RuleManager.AddRule(conditionalRule);
        if (earlyExitRule != null) RuleManager.AddRule(earlyExitRule);
    }

    public partial bool IsActive { get; set; }

    public partial string Value { get; set; }

    public partial string ProductCode { get; set; }
}

/// <summary>
/// Entity for cancellation sample.
/// </summary>
[Factory]
public partial class RulesCancellableEntity : ValidateBase<RulesCancellableEntity>
{
    public RulesCancellableEntity(
        IValidateBaseServices<RulesCancellableEntity> services,
        CancellableValidationRule? cancellableRule = null) : base(services)
    {
        if (cancellableRule != null) RuleManager.AddRule(cancellableRule);
    }

    public partial string ZipCode { get; set; }

    public partial decimal ComputedTaxRate { get; set; }
}

/// <summary>
/// Entity for LoadProperty sample.
/// </summary>
[Factory]
public partial class RulesOrderWithTotal : ValidateBase<RulesOrderWithTotal>
{
    public RulesOrderWithTotal(
        IValidateBaseServices<RulesOrderWithTotal> services,
        ComputedTotalRule? totalRule = null) : base(services)
    {
        if (totalRule != null) RuleManager.AddRule(totalRule);
    }

    public partial int Quantity { get; set; }

    public partial decimal UnitPrice { get; set; }

    public partial decimal Total { get; set; }
}

/// <summary>
/// Entity for trigger property samples.
/// </summary>
[Factory]
public partial class RulesTriggerEntity : ValidateBase<RulesTriggerEntity>
{
    public RulesTriggerEntity(
        IValidateBaseServices<RulesTriggerEntity> services,
        SingleTriggerRule? singleRule = null,
        MultipleTriggerRule? multipleRule = null,
        DynamicTriggerRule? dynamicRule = null) : base(services)
    {
        if (singleRule != null) RuleManager.AddRule(singleRule);
        if (multipleRule != null) RuleManager.AddRule(multipleRule);
        if (dynamicRule != null) RuleManager.AddRule(dynamicRule);
    }

    public partial string Email { get; set; }

    public partial string EmailLower { get; set; }

    public partial string City { get; set; }

    public partial string State { get; set; }

    public partial string ZipCode { get; set; }

    public partial string FullAddress { get; set; }

    public partial string FirstName { get; set; }

    public partial string LastName { get; set; }

    public partial string DisplayName { get; set; }
}

/// <summary>
/// Entity for rule messages samples.
/// </summary>
[Factory]
public partial class RulesMessageEntity : ValidateBase<RulesMessageEntity>
{
    public RulesMessageEntity(
        IValidateBaseServices<RulesMessageEntity> services,
        PassingValidationRule? passingRule = null,
        SingleMessageRule? singleRule = null,
        MultipleMessagesRule? multipleRule = null) : base(services)
    {
        if (passingRule != null) RuleManager.AddRule(passingRule);
        if (singleRule != null) RuleManager.AddRule(singleRule);
        if (multipleRule != null) RuleManager.AddRule(multipleRule);
    }

    public partial string Status { get; set; }

    public partial int Age { get; set; }

    public partial DateTime StartDate { get; set; }

    public partial DateTime EndDate { get; set; }
}

/// <summary>
/// Line item entity for aggregate validation.
/// </summary>
public interface IRulesLineItem : IValidateBase
{
    decimal Amount { get; set; }
}

[Factory]
public partial class RulesLineItem : ValidateBase<RulesLineItem>, IRulesLineItem
{
    public RulesLineItem(IValidateBaseServices<RulesLineItem> services) : base(services) { }

    public partial decimal Amount { get; set; }
}

public interface IRulesLineItemList : IValidateListBase<IRulesLineItem> { }

public class RulesLineItemList : ValidateListBase<IRulesLineItem>, IRulesLineItemList { }

/// <summary>
/// Aggregate root for aggregate-level validation sample.
/// </summary>
[Factory]
public partial class RulesAggregateRoot : ValidateBase<RulesAggregateRoot>
{
    public RulesAggregateRoot(
        IValidateBaseServices<RulesAggregateRoot> services,
        AggregateValidationRule? aggregateRule = null) : base(services)
    {
        LineItems = new RulesLineItemList();
        if (aggregateRule != null) RuleManager.AddRule(aggregateRule);
    }

    public partial decimal TotalBudget { get; set; }

    public IRulesLineItemList LineItems { get; }
}

/// <summary>
/// Entity demonstrating standard validation attributes.
/// </summary>
#region rules-attribute-standard
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
#endregion

/// <summary>
/// Entity for manual rule execution samples.
/// </summary>
[Factory]
public partial class RulesManualEntity : ValidateBase<RulesManualEntity>
{
    public RulesManualEntity(IValidateBaseServices<RulesManualEntity> services) : base(services)
    {
        RuleManager.AddValidation(
            e => e.Value > 0 ? "" : "Value must be positive",
            e => e.Value);
    }

    public partial int Value { get; set; }

    public partial string Name { get; set; }
}

/// <summary>
/// Entity demonstrating fluent rule registration patterns.
/// </summary>
[Factory]
public partial class RulesFluentEntity : ValidateBase<RulesFluentEntity>
{
    #region rules-registration-fluent
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
    #endregion

    public partial string FirstName { get; set; }

    public partial string LastName { get; set; }

    public partial string FullName { get; set; }

    public partial int Age { get; set; }

    public partial bool Processed { get; set; }
}

/// <summary>
/// Entity demonstrating custom rule class registration.
/// </summary>
[Factory]
public partial class RulesCustomEntity : ValidateBase<RulesCustomEntity>
{
    #region rules-registration-custom
    public RulesCustomEntity(
        IValidateBaseServices<RulesCustomEntity> services,
        CustomBusinessRule businessRule) : base(services)
    {
        // Register injected custom rule class
        RuleManager.AddRule(businessRule);
    }
    #endregion

    public partial decimal Amount { get; set; }
}

/// <summary>
/// Custom business rule class for registration sample.
/// </summary>
public class CustomBusinessRule : RuleBase<RulesCustomEntity>
{
    public CustomBusinessRule() : base(e => e.Amount) { }

    protected override IRuleMessages Execute(RulesCustomEntity target)
    {
        if (target.Amount < 0)
        {
            return (nameof(target.Amount), "Amount cannot be negative").AsRuleMessages();
        }
        return None;
    }
}

// -----------------------------------------------------------------
// Test classes
// -----------------------------------------------------------------

/// <summary>
/// Tests for business-rules.md snippets.
/// </summary>
public class BusinessRulesSamplesTests
{
    [Fact]
    public void AddAction_ComputesFullName()
    {
        var contact = new RulesContact(new ValidateBaseServices<RulesContact>());

        contact.FirstName = "John";
        contact.LastName = "Doe";

        Assert.Equal("John Doe", contact.FullName);
    }

    [Fact]
    public async Task AddActionAsync_FetchesTaxRate()
    {
        var pricingService = new MockPricingService();
        var product = new RulesProduct(new ValidateBaseServices<RulesProduct>(), pricingService);

        product.ZipCode = "90210";
        await product.WaitForTasks();

        Assert.Equal(0.0825m, product.TaxRate);
    }

    [Fact]
    public void AddValidation_ValidatesAmount()
    {
        var invoice = new RulesInvoice(new ValidateBaseServices<RulesInvoice>());

        invoice.Amount = -100;
        Assert.False(invoice.IsValid);

        invoice.Amount = 100;
        Assert.True(invoice.IsValid);
    }

    [Fact]
    public async Task AddValidationAsync_ChecksInventory()
    {
        var inventoryService = new MockInventoryService();
        var order = new RulesOrder(new ValidateBaseServices<RulesOrder>(), inventoryService);

        order.ProductCode = "OUT-001";
        await order.WaitForTasks();
        Assert.False(order.IsValid);

        order.ProductCode = "PROD-001";
        await order.WaitForTasks();
        Assert.True(order.IsValid);
    }

    [Fact]
    public async Task AddValidationAsyncWithToken_SupportsCancellation()
    {
        var inventoryService = new MockInventoryService();
        var booking = new RulesBooking(new ValidateBaseServices<RulesBooking>(), inventoryService);

        booking.ResourceId = "ROOM-001";
        await booking.WaitForTasks();

        Assert.True(booking.IsValid);
    }

    [Fact]
    public void CrossPropertyValidation_ValidatesDateRange()
    {
        var evt = new RulesEvent(new ValidateBaseServices<RulesEvent>());

        evt.StartDate = new DateTime(2024, 6, 15);
        evt.EndDate = new DateTime(2024, 6, 10);
        Assert.False(evt.IsValid);

        evt.EndDate = new DateTime(2024, 6, 20);
        Assert.True(evt.IsValid);
    }

    [Fact]
    public void CustomRuleClass_ValidatesSalaryRange()
    {
        var salaryRule = new SalaryRangeRule(30000m, 200000m);
        var employee = new RulesEmployee(new ValidateBaseServices<RulesEmployee>(), salaryRule);

        employee.Salary = 25000m;
        Assert.False(employee.IsValid);

        employee.Salary = 75000m;
        Assert.True(employee.IsValid);

        employee.Salary = 250000m;
        Assert.False(employee.IsValid);
    }

    [Fact]
    public async Task AsyncCustomRuleClass_ValidatesAvailability()
    {
        var inventoryService = new MockInventoryService();
        var availabilityRule = new ProductAvailabilityRule(inventoryService);
        var orderItem = new RulesOrderItem(new ValidateBaseServices<RulesOrderItem>(), availabilityRule);

        orderItem.ProductCode = "OUT-001";
        await orderItem.WaitForTasks();
        Assert.False(orderItem.IsValid);

        orderItem.ProductCode = "PROD-001";
        await orderItem.WaitForTasks();
        Assert.True(orderItem.IsValid);
    }

    [Fact]
    public void AttributeValidation_EnforcesConstraints()
    {
        var entity = new RulesAttributeEntity(new ValidateBaseServices<RulesAttributeEntity>());

        // Required validation
        entity.Name = "";
        Assert.False(entity["Name"].IsValid);

        entity.Name = "Test";
        Assert.True(entity["Name"].IsValid);

        // Email validation
        entity.Email = "invalid";
        Assert.False(entity["Email"].IsValid);

        entity.Email = "test@example.com";
        Assert.True(entity["Email"].IsValid);

        // Range validation
        entity.Age = 200;
        Assert.False(entity["Age"].IsValid);

        entity.Age = 25;
        Assert.True(entity["Age"].IsValid);
    }

    [Fact]
    public async Task AggregateRule_ValidatesTotalBudget()
    {
        var aggregateRule = new AggregateValidationRule();
        var root = new RulesAggregateRoot(new ValidateBaseServices<RulesAggregateRoot>(), aggregateRule);

        root.TotalBudget = 1000m;

        // Add items within budget
        var item1 = new RulesLineItem(new ValidateBaseServices<RulesLineItem>()) { Amount = 400m };
        var item2 = new RulesLineItem(new ValidateBaseServices<RulesLineItem>()) { Amount = 300m };
        root.LineItems.Add(item1);
        root.LineItems.Add(item2);

        // Run all rules to validate after adding items
        await root.RunRules(RunRulesFlag.All);

        Assert.True(root.IsValid);

        // Add item that exceeds budget
        var item3 = new RulesLineItem(new ValidateBaseServices<RulesLineItem>()) { Amount = 500m };
        root.LineItems.Add(item3);

        // Run rules again
        await root.RunRules(RunRulesFlag.All);

        Assert.False(root.IsValid);
    }

    [Fact]
    public void RuleExecutionOrder_RulesExecuteInOrder()
    {
        var firstRule = new FirstExecutionRule();
        var secondRule = new SecondExecutionRule();
        var thirdRule = new ThirdExecutionRule();

        var entity = new RulesOrderedEntity(
            new ValidateBaseServices<RulesOrderedEntity>(),
            firstRule,
            secondRule,
            thirdRule);

        entity.Value = "trigger";

        Assert.Equal(new[] { "First", "Second", "Third" }, entity.ExecutionLog);
    }

    [Fact]
    public void ConditionalRule_SkipsWhenInactive()
    {
        var conditionalRule = new ConditionalValidationRule();
        var entity = new RulesConditionalEntity(
            new ValidateBaseServices<RulesConditionalEntity>(),
            conditionalRule);

        // When inactive, empty value is allowed
        entity.IsActive = false;
        entity.Value = "";
        Assert.True(entity.IsValid);

        // When active, value is required
        entity.IsActive = true;
        Assert.False(entity.IsValid);

        entity.Value = "something";
        Assert.True(entity.IsValid);
    }

    [Fact]
    public async Task EarlyExitRule_SkipsExpensiveCheck()
    {
        var inventoryService = new MockInventoryService();
        var earlyExitRule = new EarlyExitRule(inventoryService);
        var entity = new RulesConditionalEntity(
            new ValidateBaseServices<RulesConditionalEntity>(),
            earlyExitRule: earlyExitRule);

        // Empty product code skips inventory check
        entity.ProductCode = "";
        await entity.WaitForTasks();
        Assert.True(entity.IsValid);

        // Non-empty triggers the check
        entity.ProductCode = "OUT-001";
        await entity.WaitForTasks();
        Assert.False(entity.IsValid);
    }

    [Fact]
    public async Task CancellableRule_ComputesTaxRate()
    {
        var pricingService = new MockPricingService();
        var cancellableRule = new CancellableValidationRule(pricingService);
        var entity = new RulesCancellableEntity(
            new ValidateBaseServices<RulesCancellableEntity>(),
            cancellableRule);

        entity.ZipCode = "90210";
        await entity.WaitForTasks();

        Assert.Equal(0.0825m, entity.ComputedTaxRate);
    }

    [Fact]
    public void LoadPropertyRule_ComputesTotalWithoutTriggeringRules()
    {
        var totalRule = new ComputedTotalRule();
        var order = new RulesOrderWithTotal(
            new ValidateBaseServices<RulesOrderWithTotal>(),
            totalRule);

        order.Quantity = 5;
        order.UnitPrice = 10.00m;

        Assert.Equal(50.00m, order.Total);
    }

    [Fact]
    public void SingleTriggerRule_TriggersOnEmail()
    {
        var singleRule = new SingleTriggerRule();
        var entity = new RulesTriggerEntity(
            new ValidateBaseServices<RulesTriggerEntity>(),
            singleRule);

        entity.Email = "TEST@Example.COM";

        Assert.Equal("test@example.com", entity.EmailLower);
    }

    [Fact]
    public void MultipleTriggerRule_TriggersOnAnyProperty()
    {
        var multipleRule = new MultipleTriggerRule();
        var entity = new RulesTriggerEntity(
            new ValidateBaseServices<RulesTriggerEntity>(),
            multipleRule: multipleRule);

        entity.City = "Seattle";
        entity.State = "WA";
        entity.ZipCode = "98101";

        Assert.Equal("Seattle, WA 98101", entity.FullAddress);
    }

    [Fact]
    public void DynamicTriggerRule_TriggersOnAddedProperties()
    {
        var dynamicRule = new DynamicTriggerRule();
        var entity = new RulesTriggerEntity(
            new ValidateBaseServices<RulesTriggerEntity>(),
            dynamicRule: dynamicRule);

        entity.FirstName = "Jane";
        entity.LastName = "Smith";

        Assert.Equal("Smith, Jane", entity.DisplayName);
    }

    [Fact]
    public void PassingValidationRule_ReturnsNone()
    {
        var passingRule = new PassingValidationRule();
        var entity = new RulesMessageEntity(
            new ValidateBaseServices<RulesMessageEntity>(),
            passingRule);

        entity.Status = "Active";
        Assert.True(entity["Status"].IsValid);

        entity.Status = "Invalid";
        Assert.False(entity["Status"].IsValid);
    }

    [Fact]
    public void SingleMessageRule_ReturnsOneMessage()
    {
        var singleRule = new SingleMessageRule();
        var entity = new RulesMessageEntity(
            new ValidateBaseServices<RulesMessageEntity>(),
            singleRule: singleRule);

        entity.Age = -5;
        Assert.False(entity["Age"].IsValid);
    }

    [Fact]
    public async Task MultipleMessagesRule_ReturnsMultipleMessages()
    {
        var multipleRule = new MultipleMessagesRule();
        var entity = new RulesMessageEntity(
            new ValidateBaseServices<RulesMessageEntity>(),
            multipleRule: multipleRule);

        // Run rules to validate the default values
        await entity.RunRules(RunRulesFlag.All);

        Assert.False(entity["StartDate"].IsValid);
        Assert.False(entity["EndDate"].IsValid);
    }

    #region rules-run-all
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
    #endregion

    #region rules-run-property
    [Fact]
    public async Task RunRulesForProperty_ExecutesPropertyRules()
    {
        var entity = new RulesManualEntity(new ValidateBaseServices<RulesManualEntity>());

        entity.Value = -1;

        // Run rules only for the Value property using public method
        await entity.RunRules(nameof(entity.Value));

        Assert.False(entity.IsValid);
    }
    #endregion

    #region rules-run-specific
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
    #endregion

    [Fact]
    public async Task FluentEntity_AllRulesWork()
    {
        var entity = new RulesFluentEntity(new ValidateBaseServices<RulesFluentEntity>());

        entity.FirstName = "John";
        entity.LastName = "Doe";
        entity.Age = 25;

        await entity.WaitForTasks();

        Assert.Equal("John Doe", entity.FullName);
        Assert.True(entity.Processed);
        Assert.True(entity.IsValid);

        entity.Age = 15;
        Assert.False(entity.IsValid);
    }

    [Fact]
    public void CustomEntity_InjectedRuleWorks()
    {
        var businessRule = new CustomBusinessRule();
        var entity = new RulesCustomEntity(new ValidateBaseServices<RulesCustomEntity>(), businessRule);

        entity.Amount = -100m;
        Assert.False(entity.IsValid);

        entity.Amount = 100m;
        Assert.True(entity.IsValid);
    }
}
