# Validation and Rules

Neatoo provides a rules engine for business logic and validation. Rules execute automatically when trigger properties change.

## Contents

| Section | Description |
|---------|-------------|
| [Rule Categories](#rule-categories) | Data annotations, inline rules, RuleBase, AsyncRuleBase |
| [Running Rules](#running-rules) | Automatic execution, manual execution, when rules don't trigger |
| [Cancellation Support](#cancellation-support) | CancellationToken for async rules, shutdown, navigation |
| [Trigger Properties](#trigger-properties) | Specifying which properties trigger a rule |
| [Returning Messages](#returning-rule-messages) | Single, multiple, conditional, chained messages |
| [Data Annotations](#data-annotations) | Standard validation attributes |
| [Cross-Property Validation](#cross-property-validation) | Rules checking multiple properties |
| [Parent-Child Validation](#parent-child-validation) | Accessing parent from child rules |
| [Action Rules](#action-rules-transformations) | Using rules for computed values and transformations |

---

## Rule Categories

| Category | When to Use | Example |
|----------|-------------|---------|
| **Data Annotations** | Required, format, range | `[Required]`, `[EmailAddress]` |
| **Inline Rules** | Simple logic in constructor | `RuleManager.AddValidation(...)` |
| **RuleBase<T>** | Cross-property, reusable rules | Start date before end date |
| **AsyncRuleBase<T>** | Database lookups, API calls | Uniqueness checks |

> **Important**: Database-dependent validation belongs in `AsyncRuleBase<T>`, not in factory methods. See [Database-Dependent Validation](database-dependent-validation.md).

## Quick Reference: Where to Put Validation

| Validation Type | Where | Example |
|-----------------|-------|---------|
| Required fields | `[Required]` attribute | `[Required] public string Name` |
| Format validation | `[RegularExpression]`, `[EmailAddress]` | `[EmailAddress] public string Email` |
| Range/length checks | `[Range]`, `[StringLength]` | `[Range(0, 150)] public int Age` |
| Cross-property rules | `RuleBase<T>` | Start date before end date |
| Database lookups | `AsyncRuleBase<T>` + Command | Uniqueness, overlap detection |
| **Never in factories** | ~~`[Insert]`/`[Update]`~~ | See warning below |

> **Important**: Database-dependent validation (uniqueness checks, overlap detection, etc.) belongs in `AsyncRuleBase<T>`, not in factory methods. See [Database-Dependent Validation](database-dependent-validation.md) for the correct pattern.

## Rule Types

### Synchronous Rules (RuleBase)

For validation logic that doesn't require async operations:

<!-- snippet: age-validation-rule -->
```cs
public interface IAgeValidationRule : IRule<IPerson> { }

public class AgeValidationRule : RuleBase<IPerson>, IAgeValidationRule
{
    public AgeValidationRule() : base(p => p.Age) { }

    protected override IRuleMessages Execute(IPerson target)
    {
        if (target.Age < 0)
        {
            return (nameof(target.Age), "Age cannot be negative").AsRuleMessages();
        }
        if (target.Age > 150)
        {
            return (nameof(target.Age), "Age seems unrealistic").AsRuleMessages();
        }
        return None;
    }
}
```
<!-- endSnippet -->

### Asynchronous Rules (AsyncRuleBase)

For validation that requires database lookups, API calls, or other async operations:

<!-- snippet: unique-email-rule -->
```cs
public interface IUniqueEmailRule : IRule<IPerson> { }

public class UniqueEmailRule : AsyncRuleBase<IPerson>, IUniqueEmailRule
{
    private readonly IEmailService _emailService;

    public UniqueEmailRule(IEmailService emailService) : base(p => p.Email)
    {
        _emailService = emailService;
    }

    protected override async Task<IRuleMessages> Execute(IPerson target, CancellationToken? token = null)
    {
        if (string.IsNullOrEmpty(target.Email))
            return None;

        if (await _emailService.EmailExistsAsync(target.Email, target.Id))
        {
            return (nameof(target.Email), "Email already in use").AsRuleMessages();
        }
        return None;
    }
}
```
<!-- endSnippet -->

> **Why AsyncRuleBase instead of factory validation?**
>
> Async rules run during editing, providing immediate feedback. If you put this check in a factory method, users only see the error after clicking Save.
>
> For database-dependent validation requiring server-side services, use the Command pattern with `AsyncRuleBase<T>`. See [Database-Dependent Validation](database-dependent-validation.md) for complete examples.

## Registering Rules

Rules are registered in the entity constructor and in DI.

### Define Rule Interfaces

<!-- snippet: rule-interface-definition -->
```cs
// Rule interfaces
public interface IAgeValidationRule : IRule<IRuleRegistrationPerson> { }
public interface IUniqueNameValidationRule : IRule<IRuleRegistrationPerson> { }
```
<!-- endSnippet -->

### Inject and Register in Constructor

<!-- snippet: entity-rule-injection -->
```cs
public RuleRegistrationPerson(
    IValidateBaseServices<RuleRegistrationPerson> services,
    IUniqueNameValidationRule uniqueNameRule,
    IAgeValidationRule ageRule) : base(services)
{
    RuleManager.AddRule(uniqueNameRule);
    RuleManager.AddRule(ageRule);
}
```
<!-- endSnippet -->

The key pattern:

<!-- snippet: rule-manager-addrule -->
```cs
RuleManager.AddRule(uniqueNameRule);
RuleManager.AddRule(ageRule);
```
<!-- endSnippet -->

### DI Registration

```csharp
// In DI setup
builder.Services.AddScoped<IUniqueNameValidationRule, UniqueNameValidationRuleImpl>();
builder.Services.AddScoped<IAgeValidationRule, AgeValidationRuleImpl>();
```

## Trigger Properties

Specify which properties trigger the rule:

<!-- snippet: trigger-properties -->
```cs
public class TriggerPropertiesConstructorExample : RuleBase<IPerson>
{
    // Constructor approach - pass trigger properties to base
    public TriggerPropertiesConstructorExample() : base(p => p.FirstName, p => p.LastName) { }

    protected override IRuleMessages Execute(IPerson target) => None;
}

public class TriggerPropertiesMethodExample : RuleBase<IPerson>
{
    // Or use AddTriggerProperties method
    public TriggerPropertiesMethodExample()
    {
        AddTriggerProperties(p => p.FirstName, p => p.LastName);
    }

    protected override IRuleMessages Execute(IPerson target) => None;
}
```
<!-- endSnippet -->

When any trigger property changes, the rule executes automatically.

## Returning Rule Messages

### Single Message

<!-- snippet: returning-messages-single -->
```cs
public class SingleMessageExample : RuleBase<IPerson>
{
    public SingleMessageExample() : base(p => p.Email) { }

    protected override IRuleMessages Execute(IPerson target)
    {
        // Return a single validation message
        return (nameof(target.Email), "Invalid email format").AsRuleMessages();
    }
}
```
<!-- endSnippet -->

### Multiple Messages

<!-- snippet: returning-messages-multiple -->
```cs
public class MultipleMessagesExample : RuleBase<IPerson>
{
    public MultipleMessagesExample() : base(p => p.FirstName, p => p.LastName) { }

    protected override IRuleMessages Execute(IPerson target)
    {
        // Return multiple validation messages
        return (new[]
        {
            (nameof(target.FirstName), "First and Last name combination is not unique"),
            (nameof(target.LastName), "First and Last name combination is not unique")
        }).AsRuleMessages();
    }
}
```
<!-- endSnippet -->

### Conditional Messages (Fluent API)

<!-- snippet: returning-messages-conditional -->
```cs
public class ConditionalMessageExample : RuleBase<IPerson>
{
    public ConditionalMessageExample() : base(p => p.Age) { }

    protected override IRuleMessages Execute(IPerson target)
    {
        // Conditional message using RuleMessages.If
        return RuleMessages.If(
            target.Age < 0,
            nameof(target.Age),
            "Age cannot be negative");
    }
}
```
<!-- endSnippet -->

### Chained Conditions

<!-- snippet: returning-messages-chained -->
```cs
public class ChainedConditionsExample : RuleBase<IPerson>
{
    public ChainedConditionsExample() : base(p => p.FirstName) { }

    protected override IRuleMessages Execute(IPerson target)
    {
        // Chained conditions with ElseIf
        return RuleMessages.If(string.IsNullOrEmpty(target.FirstName), nameof(target.FirstName), "Name is required")
            .ElseIf(() => target.FirstName!.Length < 2, nameof(target.FirstName), "Name must be at least 2 characters");
    }
}
```
<!-- endSnippet -->

### No Errors

Return `None` (or `RuleMessages.None`) when validation passes.

## Inline Rules

Define simple rules directly in the constructor without creating separate rule classes:

### Inline Validation

<!-- snippet: fluent-validation -->
```cs
// Inline validation rule
RuleManager.AddValidation(
    target => string.IsNullOrEmpty(target.Name) ? "Name is required" : "",
    t => t.Name);
```
<!-- endSnippet -->

### Async Validation Rule

<!-- snippet: fluent-validation-async -->
```cs
// Async validation rule
RuleManager.AddValidationAsync(
    async target => await emailService.EmailExistsAsync(target.Email!) ? "Email in use" : "",
    t => t.Email);
```
<!-- endSnippet -->

## Action Rules (Transformations)

Action rules compute derived values or transform data when trigger properties change. Unlike validation rules, they don't return error messages.

### Computed Values

<!-- snippet: fluent-action -->
```cs
// Action rule for calculated values
RuleManager.AddAction(
    target => target.FullName = $"{target.FirstName} {target.LastName}",
    t => t.FirstName,
    t => t.LastName);
```
<!-- endSnippet -->

### Async Transformations

```csharp
// Lookup and transform on property change
RuleManager.AddActionAsync(
    async target => target.TaxRate = await taxService.GetRateAsync(target.ZipCode),
    t => t.ZipCode);
```

### Use Cases

| Scenario | Example |
|----------|---------|
| Calculated fields | `FullName` from `FirstName` + `LastName` |
| Format normalization | Uppercase product codes, trim whitespace |
| Derived totals | `LineTotal` from `Quantity * UnitPrice` |
| Async lookups | Tax rate from ZIP code, address validation |

## Data Annotations

Standard `System.ComponentModel.DataAnnotations` attributes are automatically converted to Neatoo rules.

### Supported Attributes

For detailed examples of each attribute, see [Data Annotations Reference](data-annotations-reference.md).

#### Required

Validates that a value is not null, empty, or whitespace (for strings). For value types, checks that the value is not the default.

<!-- snippet: required-attribute -->
```cs
[Required]
public partial string? FirstName { get; set; }

[Required(ErrorMessage = "Customer name is required")]
public partial string? CustomerName { get; set; }
```
<!-- endSnippet -->

**Behavior:**
- Strings: fails for `null`, `""`, or whitespace-only
- Value types (int, DateTime, Guid): fails for default value (0, DateTime.MinValue, Guid.Empty)
- Nullable types: fails for `null`
- Reference types: fails for `null`

#### StringLength

Validates string length is within a range.

<!-- snippet: stringlength-attribute -->
```cs
// Maximum length only
[StringLength(100)]
public partial string? Description { get; set; }

// Minimum and maximum
[StringLength(100, MinimumLength = 2)]
public partial string? Username { get; set; }

// Custom message
[StringLength(50, MinimumLength = 5, ErrorMessage = "Name must be 5-50 characters")]
public partial string? NameWithLength { get; set; }
```
<!-- endSnippet -->

**Behavior:**
- Null or empty strings pass (use `[Required]` for null checks)
- Only validates string properties

#### MinLength / MaxLength

Validates minimum or maximum length of strings or collections.

<!-- snippet: minmaxlength-attribute -->
```cs
// String minimum length
[MinLength(3)]
public partial string? Code { get; set; }

// String maximum length
[MaxLength(500)]
public partial string? Notes { get; set; }

// Collection minimum count
[MinLength(1, ErrorMessage = "At least one item required")]
public partial List<string>? Tags { get; set; }

// Array maximum count
[MaxLength(10)]
public partial string[]? Categories { get; set; }
```
<!-- endSnippet -->

**Behavior:**
- Works with strings (character count)
- Works with collections and arrays (item count)
- Null values pass (use `[Required]` for null checks)

#### Range

Validates numeric values fall within a range. Supports integers, decimals, doubles, and dates.

<!-- snippet: range-attribute -->
```cs
// Integer range
[Range(1, 100)]
public partial int Quantity { get; set; }

// Double range
[Range(0.0, 100.0)]
public partial double Percentage { get; set; }

// Decimal range (use type-based constructor)
[Range(typeof(decimal), "0.01", "999.99")]
public partial decimal Price { get; set; }

// Date range
[Range(typeof(DateTime), "2020-01-01", "2030-12-31")]
public partial DateTime AppointmentDate { get; set; }

// Custom message
[Range(0, 150, ErrorMessage = "Age must be between 0 and 150")]
public partial int Age { get; set; }
```
<!-- endSnippet -->

**Behavior:**
- Null values pass (use `[Required]` for null checks)
- Inclusive bounds (min and max values are valid)
- Date strings parsed using invariant culture

#### RegularExpression

Validates string matches a regex pattern.

<!-- snippet: regularexpression-attribute -->
```cs
// Code format: 2 letters + 4 digits
[RegularExpression(@"^[A-Z]{2}\d{4}$")]
public partial string? ProductCode { get; set; }

// Phone format
[RegularExpression(@"^\d{3}-\d{3}-\d{4}$", ErrorMessage = "Format: 555-123-4567")]
public partial string? Phone { get; set; }

// Alphanumeric only
[RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Letters and numbers only")]
public partial string? UsernameAlphanumeric { get; set; }
```
<!-- endSnippet -->

**Behavior:**
- Null or empty strings pass (use `[Required]` for null checks)
- Pattern must match the entire string
- Uses compiled regex for performance

#### EmailAddress

Validates email address format.

<!-- snippet: emailaddress-attribute -->
```cs
[EmailAddress]
public partial string? Email { get; set; }

[EmailAddress(ErrorMessage = "Please enter a valid email")]
public partial string? ContactEmail { get; set; }
```
<!-- endSnippet -->

**Behavior:**
- Uses `MailAddress.TryCreate()` for RFC-compliant validation
- Null or empty strings pass (use `[Required]` for null checks)
- Rejects display name format like `"John <john@example.com>"`
- Accepts technically valid addresses like `user@localhost` (per RFC)

### Combining Attributes

Attributes can be combined for comprehensive validation:

<!-- snippet: combining-attributes -->
```cs
[Required(ErrorMessage = "Email is required")]
[EmailAddress(ErrorMessage = "Invalid email format")]
[StringLength(254, ErrorMessage = "Email too long")]
public partial string? CombinedEmail { get; set; }

[Required]
[Range(1, 1000)]
public partial int CombinedQuantity { get; set; }

[StringLength(100, MinimumLength = 2)]
[RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Letters only")]
public partial string? FullName { get; set; }
```
<!-- endSnippet -->

### Custom Error Messages

All attributes support custom error messages:

```csharp
// Default message: "PropertyName is required."
[Required]
public partial string? Name { get; set; }

// Custom message
[Required(ErrorMessage = "Please enter your full name")]
public partial string? Name { get; set; }
```

### Attribute vs Rule Comparison

| Use Attribute When | Use RuleBase/AsyncRuleBase When |
|--------------------|--------------------------------|
| Simple format validation | Cross-property validation |
| Standard patterns (email, range) | Complex business logic |
| No external dependencies | Database lookups required |
| Single property scope | Multiple properties involved |

## Running Rules

### Automatic Execution

Rules run automatically when trigger properties change through the `Setter`:

```csharp
person.FirstName = "John";  // Rules triggered automatically
```

### When Rules DON'T Trigger

Rules do **not** execute in these scenarios:

| Scenario | Why | How to Run Rules |
|----------|-----|------------------|
| Using `LoadValue()` | Silent load, no events | Call `RunRules()` after |
| During `PauseAllActions()` | Explicitly paused | Rules run on resume |
| During factory operations | Paused via `FactoryStart()` | Rules run on `FactoryComplete()` |
| During JSON deserialization | Paused via `OnDeserializing()` | Rules run on `OnDeserialized()` |
| Property set to same value | No change detected | N/A |

**Example - LoadValue doesn't trigger:**
```csharp
// Setter - triggers rules (outside factory operations)
person.FirstName = "John";          // Rules execute

// LoadValue - no rules, no modification tracking
person[nameof(IPerson.FirstName)].LoadValue("John");  // No rules
```

> **Note:** In `[Fetch]`, `[Create]`, and other factory methods, you don't need `LoadValue`.
> Rules are automatically paused via `FactoryStart()`, so regular setters work fine:
> ```csharp
> [Fetch]
> public async Task<bool> Fetch([Service] IPersonDbContext context)
> {
>     var entity = await context.FindPerson();
>     this.FirstName = entity.FirstName;  // OK - rules paused automatically
>     this.LastName = entity.LastName;    // OK - no modification tracking during fetch
>     return true;
> }
> ```

**Example - PauseAllActions:**
```csharp
using (person.PauseAllActions())
{
    person.FirstName = "John";      // No rules yet
    person.LastName = "Doe";        // No rules yet
    person.Email = "john@doe.com";  // No rules yet
}
// All rules run now when disposed
```

### Manual Execution

Run rules explicitly before save operations:

```csharp
[Insert]
public async Task Insert([Service] IDbContext db)
{
    await RunRules();  // Run all rules

    if (!IsSavable)
        return;  // Don't save if invalid

    // ... persist
}
```

### Run Specific Property Rules

```csharp
await RunRules(nameof(Email));
```

### Run Rules with Flags

```csharp
await RunRules(RunRulesFlag.All);           // All rules
await RunRules(RunRulesFlag.Self);          // Only this object's rules
await RunRules(RunRulesFlag.NotExecuted);   // Only rules that haven't run
```

### Cancellation Support

All async rule operations support `CancellationToken` for graceful cancellation during shutdown or navigation.

**Running rules with cancellation:**
```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

try
{
    await entity.RunRules(RunRulesFlag.All, cts.Token);
}
catch (OperationCanceledException)
{
    // Validation was cancelled - entity is now marked invalid
    // entity.IsValid == false
}
```

**Waiting for async tasks with cancellation:**
```csharp
// Wait for pending async property setter tasks
await entity.WaitForTasks(cancellationToken);
```

**Design Philosophy:**
- **Cancellation is for stopping, not recovering.** When validation is cancelled, the object is marked invalid via `MarkInvalid()`.
- **Running tasks complete.** Cancellation only affects waiting; tasks already executing finish to maintain consistency.
- **Recovery requires explicit action.** Call `RunRules(RunRulesFlag.All)` to clear the cancelled state and re-validate.

**When to use cancellation:**
| Scenario | Pattern |
|----------|---------|
| Component disposal | `_cts.Cancel()` in `Dispose()` |
| Navigation away | Cancel before navigating |
| Request timeout | `CancellationTokenSource(TimeSpan.FromSeconds(n))` |
| User-initiated cancel | Button triggers `_cts.Cancel()` |

**Async rules can use the token:**
```csharp
protected override async Task<IRuleMessages> Execute(
    IPerson target,
    CancellationToken? token = null)
{
    // Pass token to async operations
    var result = await _httpClient.GetAsync(url, token ?? CancellationToken.None);

    // Or check manually
    token?.ThrowIfCancellationRequested();

    return None;
}
```

**Fluent rules with cancellation:**
```csharp
// Token-accepting overloads pass CancellationToken to your delegate
RuleManager.AddActionAsync(
    async (target, token) =>
    {
        target.Rate = await service.GetRateAsync(target.ZipCode, token);
    },
    t => t.ZipCode);

RuleManager.AddValidationAsync(
    async (target, token) =>
    {
        return await service.ExistsAsync(target.Email, token) ? "In use" : "";
    },
    t => t.Email);
```

## Cross-Property Validation

Rules can check multiple properties:

<!-- snippet: date-range-rule -->
```cs
public interface IDateRangeRule : IRule<IEvent> { }

public class DateRangeRule : RuleBase<IEvent>, IDateRangeRule
{
    public DateRangeRule() : base(e => e.StartDate, e => e.EndDate) { }

    protected override IRuleMessages Execute(IEvent target)
    {
        if (target.StartDate > target.EndDate)
        {
            return (new[]
            {
                (nameof(target.StartDate), "Start date must be before end date"),
                (nameof(target.EndDate), "End date must be after start date")
            }).AsRuleMessages();
        }
        return None;
    }
}
```
<!-- endSnippet -->

## Parent-Child Validation

Access parent entity from child validation rules. This enables cross-item validation within collections.

### Rule Class with Parent Access

<!-- snippet: parent-child-rule-class -->
```cs
public class UniquePhoneTypeRule : RuleBase<IContactPhone>, IUniquePhoneTypeRule
{
    public UniquePhoneTypeRule() : base(p => p.PhoneType) { }

    protected override IRuleMessages Execute(IContactPhone target)
    {
        if (target.ParentContact == null)
            return None;

        var hasDuplicate = target.ParentContact.PhoneList
            .Where(p => p != target)
            .Any(p => p.PhoneType == target.PhoneType);

        if (hasDuplicate)
        {
            return (nameof(target.PhoneType), "Phone type must be unique").AsRuleMessages();
        }
        return None;
    }
}
```
<!-- endSnippet -->

### Accessing Parent's Collection

The key pattern - access siblings through parent:

<!-- snippet: parent-access-in-rule -->
```cs
var hasDuplicate = target.ParentContact.PhoneList
    .Where(p => p != target)
    .Any(p => p.PhoneType == target.PhoneType);
```
<!-- endSnippet -->

### Child Entity Setup

The child entity exposes its parent through a property:

```csharp
public IParentContact? ParentContact => Parent as IParentContact;
```

This works because `Parent` is set automatically when the child is added to a parent collection.

## Cascading Rules - A Key Feature

When a rule sets a property, dependent rules run automatically. **This cascading behavior is intentional** and essential for maintaining domain consistency.

```csharp
public class OrderTotalRule : RuleBase<IOrder>
{
    public OrderTotalRule() : base(o => o.Lines) { }

    protected override IRuleMessages Execute(IOrder target)
    {
        var total = target.Lines?.Sum(l => l.Quantity * l.UnitPrice) ?? 0;

        // Property setter triggers any rules that depend on Total - this is correct!
        target.Total = total;

        return None;
    }
}
```

If `Total` has dependent rules (e.g., discount calculation, credit limit check), they run automatically. This is the expected behavior.

### LoadProperty - Rare Use Cases Only

Use `LoadProperty()` **only** to prevent circular references:

```csharp
protected override IRuleMessages Execute(IOrder target)
{
    // Rule A triggers Rule B which triggers Rule A - break the cycle
    LoadProperty(target, t => t.InternalValue, calculated);
    return None;
}
```

**Do NOT use LoadProperty for:**
- Factory methods (`[Fetch]`, `[Create]`, etc.) - rules are already paused
- Normal rule execution - cascading is the correct behavior

## Busy State During Async Rules

When async rules execute:
- Trigger properties are marked `IsBusy = true`
- The entity's `IsBusy` becomes true
- `IsSavable` becomes false until rules complete

```razor
@if (person[nameof(IPerson.Email)].IsBusy)
{
    <MudProgressCircular Size="Size.Small" Indeterminate="true" />
}
```

## Validation Messages

Access validation messages through the property or entity:

```csharp
// Property-level messages
var emailMessages = person[nameof(IPerson.Email)].PropertyMessages;

// All messages for entity
var allMessages = person.PropertyMessages;

// Check validity
if (!person.IsValid)
{
    foreach (var msg in person.PropertyMessages)
    {
        Console.WriteLine($"{msg.Property.Name}: {msg.Message}");
    }
}
```

## Clearing Messages

```csharp
// Clear all messages (including children)
person.ClearAllMessages();

// Clear only this entity's messages
person.ClearSelfMessages();

// Clear specific property
person[nameof(IPerson.Email)].ClearAllMessages();
```

## Complete Rule Example

<!-- snippet: complete-rule-example -->
```cs
public interface IUniqueNameRule : IRule<IPerson> { }

public class UniqueNameRule : AsyncRuleBase<IPerson>, IUniqueNameRule
{
    private readonly Func<Guid?, string, string, Task<bool>> _isUniqueName;

    public UniqueNameRule(Func<Guid?, string, string, Task<bool>> isUniqueName)
    {
        _isUniqueName = isUniqueName;
        AddTriggerProperties(p => p.FirstName, p => p.LastName);
    }

    protected override async Task<IRuleMessages> Execute(IPerson target, CancellationToken? token = null)
    {
        // Skip if properties haven't been modified
        if (!target[nameof(target.FirstName)].IsModified &&
            !target[nameof(target.LastName)].IsModified)
        {
            return None;
        }

        // Skip if values are empty
        if (string.IsNullOrEmpty(target.FirstName) || string.IsNullOrEmpty(target.LastName))
        {
            return None;
        }

        // Check uniqueness
        if (!await _isUniqueName(target.Id, target.FirstName, target.LastName))
        {
            return (new[]
            {
                (nameof(target.FirstName), "First and Last name combination is not unique"),
                (nameof(target.LastName), "First and Last name combination is not unique")
            }).AsRuleMessages();
        }

        return None;
    }
}
```
<!-- endSnippet -->

## Common Mistakes

### Putting Validation in Factory Methods

Never put business validation in `[Insert]` or `[Update]` methods. Validation should run during editing via rules, not at save time. See [Database-Dependent Validation](database-dependent-validation.md) for the correct pattern using `AsyncRuleBase` with Commands.

### Throwing Exceptions for Validation

Validation failures should return `IRuleMessages`, not throw exceptions. Exceptions:
- Don't integrate with UI validation display
- Return HTTP 500 errors
- Break the expected user flow

### Forgetting to Check IsModified

For expensive async rules, check if the property was actually modified:

```csharp
protected override async Task<IRuleMessages> Execute(IUser target, ...)
{
    // Skip expensive check if email hasn't changed
    if (!target[nameof(target.Email)].IsModified)
        return None;

    // ... expensive database check
}
```

## See Also

- [Testing](testing.md) - Unit testing rules with `RunRule`
- [Database-Dependent Validation](database-dependent-validation.md) - Async validation with Commands
- [Property System](property-system.md) - Property meta-properties and IsBusy
- [Blazor Binding](blazor-binding.md) - Displaying validation in UI
- [Meta-Properties Reference](meta-properties.md) - IsValid, IsBusy details
