# Validation

Neatoo provides a comprehensive validation system with synchronous and asynchronous rules, attributes, and cross-property validation.

## Basic Validation Rule

Add validation rules in the constructor using RuleManager:

<!-- snippet: validation-basic -->
<a id='snippet-validation-basic'></a>
```cs
[Factory]
public partial class ValidationCustomer : ValidateBase<ValidationCustomer>
{
    public ValidationCustomer(IValidateBaseServices<ValidationCustomer> services) : base(services) { }

    public partial string Name { get; set; }

    public partial string Email { get; set; }

    [Create]
    public void Create() { }
}
```
<sup><a href='/src/samples/ValidationSamples.cs#L16-L29' title='Snippet source file'>snippet source</a> | <a href='#snippet-validation-basic' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Validation Attributes

Use standard .NET validation attributes:

<!-- snippet: validation-attributes -->
<a id='snippet-validation-attributes'></a>
```cs
[Factory]
public partial class ValidationContact : ValidateBase<ValidationContact>
{
    public ValidationContact(IValidateBaseServices<ValidationContact> services) : base(services) { }

    [Required(ErrorMessage = "Name is required")]
    [MaxLength(100)]
    public partial string Name { get; set; }

    [EmailAddress]
    public partial string Email { get; set; }

    [Phone]
    public partial string PhoneNumber { get; set; }

    [Range(1, 150, ErrorMessage = "Age must be between 1 and 150")]
    public partial int Age { get; set; }

    [RegularExpression(@"^\d{5}(-\d{4})?$", ErrorMessage = "Invalid ZIP code")]
    public partial string ZipCode { get; set; }

    [Create]
    public void Create() { }
}
```
<sup><a href='/src/samples/ValidationSamples.cs#L59-L84' title='Snippet source file'>snippet source</a> | <a href='#snippet-validation-attributes' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Custom Validation Rules

Create reusable rule classes:

<!-- snippet: validation-custom-rule -->
<a id='snippet-validation-custom-rule'></a>
```cs
public ValidationInvoice(IValidateBaseServices<ValidationInvoice> services) : base(services)
{
    // Custom validation rule: Amount must be positive
    RuleManager.AddValidation(
        invoice => invoice.Amount > 0 ? "" : "Amount must be greater than zero",
        i => i.Amount);
}
```
<sup><a href='/src/samples/ValidationSamples.cs#L92-L100' title='Snippet source file'>snippet source</a> | <a href='#snippet-validation-custom-rule' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Cross-Property Validation

Rules that depend on multiple properties:

<!-- snippet: validation-cross-property -->
<a id='snippet-validation-cross-property'></a>
```cs
public ValidationDateRange(IValidateBaseServices<ValidationDateRange> services) : base(services)
{
    // Cross-property rule: EndDate must be after StartDate
    // Triggers when either StartDate OR EndDate changes
    RuleManager.AddRule(new ValidationDateRangeRule());
}
```
<sup><a href='/src/samples/ValidationSamples.cs#L136-L143' title='Snippet source file'>snippet source</a> | <a href='#snippet-validation-cross-property' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Async Validation Rules

For rules that need to call services or databases:

<!-- snippet: validation-async-rule -->
<a id='snippet-validation-async-rule'></a>
```cs
public ValidationUser(
    IValidateBaseServices<ValidationUser> services,
    IValidationUniquenessService uniquenessService) : base(services)
{
    // Async validation rule: Check email uniqueness
    RuleManager.AddValidationAsync(
        async user =>
        {
            if (string.IsNullOrEmpty(user.Email))
                return "";

            var isUnique = await uniquenessService.IsEmailUniqueAsync(user.Email);
            return isUnique ? "" : "Email is already in use";
        },
        u => u.Email);
}
```
<sup><a href='/src/samples/ValidationSamples.cs#L176-L193' title='Snippet source file'>snippet source</a> | <a href='#snippet-validation-async-rule' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Class-Based Rules with Dependency Injection

For rules that need injected services or complex logic, create a class inheriting from `AsyncRuleBase<T>` or `RuleBase<T>`. This is the primary mechanism for non-trivial rules — the fluent `AddValidation`/`AddAction` lambdas are shorthand for simple cases.

### Async Class-Based Rule

<!-- snippet: skill-async-rule-class -->
<a id='snippet-skill-async-rule-class'></a>
```cs
public class SkillGapUniqueEmailRule : AsyncRuleBase<SkillGapEmployee>
{
    private readonly ISkillUserValidationService _validationService;

    public SkillGapUniqueEmailRule(ISkillUserValidationService validationService)
        : base(e => e.Email)  // trigger property
    {
        _validationService = validationService;
    }

    protected override async Task<IRuleMessages> Execute(
        SkillGapEmployee target, CancellationToken? token = null)
    {
        if (string.IsNullOrEmpty(target.Email))
            return None;

        var isUnique = await _validationService.IsEmailUniqueAsync(target.Email);
        return isUnique
            ? None
            : (nameof(target.Email), "Email is already in use").AsRuleMessages();
    }
}
```
<sup><a href='/src/samples/SkillGapSamples.cs#L18-L41' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-async-rule-class' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Synchronous Class-Based Rule

<!-- snippet: skill-sync-rule-class -->
<a id='snippet-skill-sync-rule-class'></a>
```cs
public class SkillGapHireDateRule : RuleBase<SkillGapEmployee>
{
    public SkillGapHireDateRule()
        : base(e => e.HireDate, e => e.TermDate)  // trigger properties
    { }

    protected override IRuleMessages Execute(SkillGapEmployee target)
    {
        if (target.TermDate != default && target.TermDate <= target.HireDate)
        {
            return (nameof(target.TermDate), "Termination date must be after hire date")
                .AsRuleMessages();
        }
        return None;
    }
}
```
<sup><a href='/src/samples/SkillGapSamples.cs#L47-L64' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-sync-rule-class' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Registration Pattern

Inject dependencies into the entity constructor and pass them to the rule:

<!-- snippet: skill-rule-registration -->
<a id='snippet-skill-rule-registration'></a>
```cs
public SkillGapEmployee(
    IEntityBaseServices<SkillGapEmployee> services,
    ISkillUserValidationService validationService) : base(services)
{
    // Class-based rules — inject deps into entity, pass to rule constructor
    RuleManager.AddRule(new SkillGapUniqueEmailRule(validationService));
    RuleManager.AddRule(new SkillGapHireDateRule());
}
```
<sup><a href='/src/samples/SkillGapSamples.cs#L73-L82' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-rule-registration' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

For rules shared across multiple entity types, see [shared-rules.md](shared-rules.md) — the rule operates on a shared interface and is DI-injected directly into entities, eliminating the need to forward services through entity constructors.

### Fluent vs Class-Based Rules

| Aspect | Fluent (`AddValidation`/`AddAction`) | Class-Based (`RuleBase<T>`/`AsyncRuleBase<T>`) |
|--------|--------------------------------------|-----------------------------------------------|
| Best for | Simple single-expression rules | Complex rules, rules needing DI services |
| DI access | Only via closure over constructor params | Direct constructor injection |
| Reusability | Inline, not reusable | Reusable across entities |
| Multi-property writes | Awkward | Natural — full access to target |

Rules operate directly on the target `T` — there is no `IRuleContext`, no `LoadProperty`/`SetProperty` indirection. Return validation messages via `(propertyName, message).AsRuleMessages()`, or `None` when validation passes.

See [domain-logic-placement.md](domain-logic-placement.md) for guidance on when to use rules vs computed properties vs domain methods.

## Running Rules

Rules run automatically on property change. Manually run rules when needed:

<!-- snippet: validation-run-rules -->
<a id='snippet-validation-run-rules'></a>
```cs
[Fact]
public async Task RunRulesManually_RevalidateEntity()
{
    var factory = GetRequiredService<IValidationOrderFactory>();
    var order = factory.Create();

    // Set invalid values
    order.Quantity = -5;
    order.UnitPrice = -10;

    // Validation runs automatically on property set
    Assert.False(order.IsValid);

    // Fix values
    order.Quantity = 10;
    order.UnitPrice = 25.00m;

    // Manually run all rules (clears messages and re-validates)
    await order.RunRules(RunRulesFlag.All);

    Assert.True(order.IsValid);
}
```
<sup><a href='/src/samples/ValidationSamples.cs#L610-L633' title='Snippet source file'>snippet source</a> | <a href='#snippet-validation-run-rules' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Checking Validation State

Access validation state through standard properties:

<!-- snippet: validation-properties -->
<a id='snippet-validation-properties'></a>
```cs
[Factory]
public partial class ValidationEmployee : ValidateBase<ValidationEmployee>
{
    public ValidationEmployee(IValidateBaseServices<ValidationEmployee> services) : base(services) { }

    // Partial properties - source generator completes implementation
    public partial string FirstName { get; set; }

    public partial string LastName { get; set; }

    [Required]
    public partial string Email { get; set; }

    [Range(0, 200)]
    public partial int Age { get; set; }

    [Create]
    public void Create() { }
}
```
<sup><a href='/src/samples/ValidationSamples.cs#L34-L54' title='Snippet source file'>snippet source</a> | <a href='#snippet-validation-properties' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## BrokenRules Collection

Access detailed validation errors:

<!-- snippet: validation-error-messages -->
<a id='snippet-validation-error-messages'></a>
```cs
[Fact]
public void AccessValidationMessages_PropertyAndObject()
{
    var factory = GetRequiredService<IValidationProductFactory>();
    var product = factory.Create();

    // Trigger validation failures
    product.Name = "";
    product.Price = -50;

    // Access all messages on the object
    Assert.True(product.PropertyMessages.Any());

    // Filter messages by property
    var nameMessages = product.PropertyMessages
        .Where(m => m.Property.Name == "Name")
        .ToList();
    Assert.NotEmpty(nameMessages);

    // Access property-specific messages via indexer
    var priceProperty = product["Price"];
    Assert.NotEmpty(priceProperty.PropertyMessages);
    Assert.Contains(priceProperty.PropertyMessages, m => m.Message.Contains("negative"));
}
```
<sup><a href='/src/samples/ValidationSamples.cs#L635-L660' title='Snippet source file'>snippet source</a> | <a href='#snippet-validation-error-messages' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Property-Level Validation State

Check validation state for individual properties:

<!-- snippet: validation-property-state -->
<a id='snippet-validation-property-state'></a>
```cs
[Fact]
public async Task PropertyValidationState_IndividualPropertyTracking()
{
    // Factory resolves ValidationAccount with IValidationUniquenessService injected
    var factory = GetRequiredService<IValidationAccountFactory>();
    var account = factory.Create();

    // Set valid account number
    account.AccountNumber = "ACC-001";

    // Check individual property state
    var accountNumberProperty = account["AccountNumber"];
    Assert.True(accountNumberProperty.IsValid);
    Assert.True(accountNumberProperty.IsSelfValid);
    Assert.Empty(accountNumberProperty.PropertyMessages);

    // Trigger async validation on email
    account.Email = "taken@example.com";

    // Wait for async validation
    await account.WaitForTasks();

    var emailProperty = account["Email"];
    Assert.False(emailProperty.IsValid);
    Assert.NotEmpty(emailProperty.PropertyMessages);
}
```
<sup><a href='/src/samples/ValidationSamples.cs#L662-L689' title='Snippet source file'>snippet source</a> | <a href='#snippet-validation-property-state' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Pausing Validation

Temporarily pause validation during bulk operations:

<!-- snippet: validation-pause-actions -->
<a id='snippet-validation-pause-actions'></a>
```cs
[Fact]
public void PauseAllActions_BatchUpdatesWithoutValidation()
{
    var factory = GetRequiredService<IValidationOrderFactory>();
    var order = factory.Create();

    // Pause validation during batch updates
    using (order.PauseAllActions())
    {
        // These assignments do NOT trigger validation rules
        order.ProductCode = "PROD-001";
        order.Quantity = 10;
        order.UnitPrice = 25.00m;

        // IsPaused is true during the using block
        Assert.True(order.IsPaused);
    }

    // After resume (automatic when using block ends):
    // - Validation rules execute for changed properties
    // - PropertyChanged events fire
    Assert.False(order.IsPaused);
    Assert.Equal("PROD-001", order.ProductCode);
}
```
<sup><a href='/src/samples/ValidationSamples.cs#L718-L743' title='Snippet source file'>snippet source</a> | <a href='#snippet-validation-pause-actions' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Cascading Validation

Validation cascades through parent-child relationships:

<!-- snippet: validation-cascade -->
<a id='snippet-validation-cascade'></a>
```cs
[Fact]
public void ValidationCascade_ChildToParent()
{
    var invoiceFactory = GetRequiredService<IValidationInvoiceWithItemsFactory>();
    var invoice = invoiceFactory.Create();
    invoice.InvoiceNumber = "INV-001";

    // Parent starts valid
    Assert.True(invoice.IsValid);

    // Add invalid child (empty description)
    var lineItemFactory = GetRequiredService<IValidationLineItemFactory>();
    var lineItem = lineItemFactory.Create();
    lineItem.Description = ""; // Triggers validation failure
    invoice.LineItems.Add(lineItem);

    // Parent's IsValid reflects child's invalid state
    Assert.False(invoice.IsValid);

    // Parent's IsSelfValid ignores children
    Assert.True(invoice.IsSelfValid);

    // Fix child
    lineItem.Description = "Valid description";

    // Parent is valid again
    Assert.True(invoice.IsValid);
}
```
<sup><a href='/src/samples/ValidationSamples.cs#L765-L794' title='Snippet source file'>snippet source</a> | <a href='#snippet-validation-cascade' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Meta Properties for Validation

Access validation metadata:

<!-- snippet: validation-meta-properties -->
<a id='snippet-validation-meta-properties'></a>
```cs
[Fact]
public async Task MetaProperties_TrackValidationState()
{
    var factory = GetRequiredService<IValidationAccountFactory>();
    var account = factory.Create();

    // Set required field
    account.AccountNumber = "ACC-001";

    // IsValid: True if all properties and children pass validation
    Assert.True(account.IsValid);

    // IsSelfValid: True if this object's properties pass (ignores children)
    Assert.True(account.IsSelfValid);

    // Trigger async validation
    account.Email = "taken@example.com";

    // IsBusy: True while async validation runs
    // (may be false if validation completes very fast)

    // Wait for async completion
    await account.WaitForTasks();

    // PropertyMessages: All validation errors
    Assert.NotEmpty(account.PropertyMessages);
}
```
<sup><a href='/src/samples/ValidationSamples.cs#L796-L824' title='Snippet source file'>snippet source</a> | <a href='#snippet-validation-meta-properties' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Validation Before Save

Neatoo automatically checks `IsValid` before allowing save operations:

<!-- snippet: validation-before-save -->
<a id='snippet-validation-before-save'></a>
```cs
[Fact]
public async Task ValidateBeforeSave_IsSavableCheck()
{
    // Use factory to create new entity with proper lifecycle
    var factory = GetRequiredService<IValidationSaveableOrderFactory>();
    var order = factory.Create();

    // Set invalid quantity (negative to trigger validation)
    order.Quantity = -5;

    // IsSavable combines IsModified, IsValid, and IsBusy
    Assert.False(order.IsSavable); // Invalid due to negative quantity

    // Fix the value
    order.Quantity = 5;

    // Re-run all rules before save
    await order.RunRules(RunRulesFlag.All);

    // Now savable
    Assert.True(order.IsValid);
    Assert.True(order.IsModified);
    Assert.False(order.IsBusy);
    Assert.True(order.IsSavable);
}
```
<sup><a href='/src/samples/ValidationSamples.cs#L826-L852' title='Snippet source file'>snippet source</a> | <a href='#snippet-validation-before-save' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Async Action Rules (AddActionAsync)

Use `AddActionAsync` for async side-effects that compute or fetch values when a property changes. Unlike `AddValidationAsync` (which returns an error message), `AddActionAsync` returns void — it performs work, not validation.

<!-- snippet: async-action-rule -->
<a id='snippet-async-action-rule'></a>
```cs
public AsyncActionContact(IValidateBaseServices<AsyncActionContact> services)
    : base(services)
{
    // Register an async action that fires when ZipCode changes
    RuleManager.AddActionAsync(
        async contact =>
        {
            // Simulate async lookup (e.g., tax service API call)
            await Task.Delay(10);
            contact.TaxRate = contact.ZipCode?.StartsWith("9") == true ? 0.0825m : 0.07m;
        },
        c => c.ZipCode);
}
```
<sup><a href='/src/samples/AsyncSamples.cs#L136-L150' title='Snippet source file'>snippet source</a> | <a href='#snippet-async-action-rule' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### CancellationToken Variant

For cooperative cancellation support, use the overload that receives a `CancellationToken`:

<!-- snippet: async-action-rule-with-token -->
<a id='snippet-async-action-rule-with-token'></a>
```cs
public AsyncCancellableContact(IValidateBaseServices<AsyncCancellableContact> services)
    : base(services)
{
    // Async action with CancellationToken support
    RuleManager.AddActionAsync(
        async (contact, token) =>
        {
            // Check cancellation before expensive operation
            token.ThrowIfCancellationRequested();

            await Task.Delay(50, token); // Honors cancellation
            contact.Status = "Validated";
        },
        c => c.Email);
}
```
<sup><a href='/src/samples/AsyncSamples.cs#L164-L180' title='Snippet source file'>snippet source</a> | <a href='#snippet-async-action-rule-with-token' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Multiple Trigger Properties

`AddActionAsync` supports 1–3 trigger properties via individual parameters. For 4+ triggers, pass an explicit array:

```csharp
RuleManager.AddActionAsync(
    async t => await CalculateAsync(t),
    new[] { t => t.A, t => t.B, t => t.C, t => t.D });
```

### LoadValue and Deserialization Behavior

`AddActionAsync` rules do **not** fire during `LoadValue` or deserialization:

- `LoadValue` fires `NeatooPropertyChanged` with `ChangeReason.Load`. The framework skips rules for `Load` events — only `SetParent` is called.
- During factory operations (`[Create]`, `[Fetch]`, etc.) and JSON deserialization, `PauseAllActions()` is called. Rules are suppressed until `ResumeAllActions()` runs after the operation completes.

This means properties populated via `LoadValue` in a `[Fetch]` method will not trigger `AddActionAsync` rules. To run rules after loading, call `RunRules(RunRulesFlag.All)` explicitly.

### Exception Propagation

Exceptions thrown inside an `AddActionAsync` lambda are surfaced when `WaitForTasks()` is awaited, wrapped in `AggregateException`. The triggering property is also marked invalid:

<!-- snippet: async-error-handling -->
<a id='snippet-async-error-handling'></a>
```cs
[Fact]
public async Task AsyncRule_ExceptionsAreCaptured()
{
    var factory = GetRequiredService<IAsyncErrorContactFactory>();
    var contact = factory.Create();

    // This value causes the rule to throw
    contact.Value = "error";

    // Exception is surfaced when waiting (wrapped in AggregateException)
    var exception = await Assert.ThrowsAsync<AggregateException>(
        async () => await contact.WaitForTasks());

    // The original exception is available via InnerException
    Assert.IsType<InvalidOperationException>(exception.InnerException);

    // Property is marked invalid with exception message
    Assert.False(contact["Value"].IsValid);
}
```
<sup><a href='/src/samples/AsyncSamples.cs#L462-L482' title='Snippet source file'>snippet source</a> | <a href='#snippet-async-error-handling' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### IsBusy During Async Rules

While an async action rule is executing, `IsBusy` is `true` and `IsSavable` is `false`. Always `await WaitForTasks()` before checking validation state or saving:

<!-- snippet: async-check-busy -->
<a id='snippet-async-check-busy'></a>
```cs
[Fact]
public async Task IsBusy_TracksAsyncOperationState()
{
    var factory = GetRequiredService<IAsyncActionContactFactory>();
    var contact = factory.Create();

    // Trigger async rule
    contact.ZipCode = "90210";

    // Object is busy while async rule executes
    Assert.True(contact.IsBusy);

    // Wait for completion
    await contact.WaitForTasks();

    // No longer busy
    Assert.False(contact.IsBusy);
}
```
<sup><a href='/src/samples/AsyncSamples.cs#L358-L377' title='Snippet source file'>snippet source</a> | <a href='#snippet-async-check-busy' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Rule Execution Order

Async action rules execute in registration order. If a rule sets a property that triggers another rule, the chained rule executes after the first completes:

<!-- snippet: async-recursive-rules -->
<a id='snippet-async-recursive-rules'></a>
```cs
[Fact]
public async Task RecursiveRules_ChainedRulesExecute()
{
    var factory = GetRequiredService<IAsyncRecursiveContactFactory>();
    var contact = factory.Create();

    // Setting FirstName triggers FullName rule
    contact.FirstName = "John";
    contact.LastName = "Doe";

    // WaitForTasks waits for the entire chain
    await contact.WaitForTasks();

    // First rule set FullName
    Assert.Equal("John Doe", contact.FullName);

    // Second rule (triggered by FullName) set Initials
    Assert.Equal("JD", contact.Initials);
}
```
<sup><a href='/src/samples/AsyncSamples.cs#L484-L504' title='Snippet source file'>snippet source</a> | <a href='#snippet-async-recursive-rules' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Related

- [Properties](properties.md) - How properties trigger validation
- [Entities](entities.md) - IsSavable and validation
