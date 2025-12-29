# Validation and Rules

Neatoo provides a powerful rules engine for implementing business logic and validation. Rules execute automatically when trigger properties change and provide immediate UI feedback.

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

```csharp
public class AgeValidationRule : RuleBase<IPerson>
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

### Asynchronous Rules (AsyncRuleBase)

For validation that requires database lookups, API calls, or other async operations:

```csharp
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

> **Why AsyncRuleBase instead of factory validation?**
>
> Async rules run during editing, providing immediate feedback. If you put this check in a factory method, users only see the error after clicking Save.
>
> For database-dependent validation requiring server-side services, use the Command pattern with `AsyncRuleBase<T>`. See [Database-Dependent Validation](database-dependent-validation.md) for complete examples.

## Registering Rules

Rules are registered in the entity constructor:

```csharp
[Factory]
internal partial class Person : EntityBase<Person>, IPerson
{
    public Person(IEntityBaseServices<Person> services,
                  IUniqueNameRule uniqueNameRule,
                  IAgeValidationRule ageRule) : base(services)
    {
        RuleManager.AddRule(uniqueNameRule);
        RuleManager.AddRule(ageRule);
    }
}
```

### DI Registration

Rules must be registered in your DI container:

```csharp
// Server and Client
builder.Services.AddScoped<IUniqueNameRule, UniqueNameRule>();
builder.Services.AddScoped<IAgeValidationRule, AgeValidationRule>();
```

## Trigger Properties

Specify which properties trigger the rule:

```csharp
// Constructor approach
public UniqueNameRule() : base(p => p.FirstName, p => p.LastName) { }

// Or AddTriggerProperties method
public UniqueNameRule()
{
    AddTriggerProperties(p => p.FirstName, p => p.LastName);
}
```

When any trigger property changes, the rule executes automatically.

## Returning Rule Messages

### Single Message

```csharp
return (nameof(target.Email), "Invalid email format").AsRuleMessages();
```

### Multiple Messages

```csharp
return (new[]
{
    (nameof(target.FirstName), "First and Last name combination is not unique"),
    (nameof(target.LastName), "First and Last name combination is not unique")
}).AsRuleMessages();
```

### Conditional Messages

```csharp
return RuleMessages.If(
    target.Age < 0,
    nameof(target.Age),
    "Age cannot be negative");
```

### Chained Conditions

```csharp
return RuleMessages.If(string.IsNullOrEmpty(target.Name), nameof(target.Name), "Name is required")
    .ElseIf(() => target.Name.Length < 2, nameof(target.Name), "Name must be at least 2 characters");
```

### No Errors

```csharp
return None;  // or RuleMessages.None
```

## Fluent Rules

For simple validation logic, use fluent rules directly in the constructor:

### Validation Rule

```csharp
public Person(IEntityBaseServices<Person> services) : base(services)
{
    RuleManager.AddValidation(
        target => string.IsNullOrEmpty(target.Name) ? "Name is required" : "",
        t => t.Name);
}
```

### Async Validation Rule

```csharp
RuleManager.AddValidationAsync(
    async target => await emailService.ExistsAsync(target.Email) ? "Email in use" : "",
    t => t.Email);
```

### Action Rule (Side Effects)

```csharp
// Calculate derived values
RuleManager.AddAction(
    target => target.FullName = $"{target.FirstName} {target.LastName}",
    t => t.FirstName,
    t => t.LastName);
```

### Async Action Rule

```csharp
RuleManager.AddActionAsync(
    async target => target.TaxRate = await taxService.GetRateAsync(target.ZipCode),
    t => t.ZipCode);
```

## Data Annotations

Standard `System.ComponentModel.DataAnnotations` attributes are automatically converted to Neatoo rules. They execute when the property changes and integrate with the validation UI.

### Supported Attributes

#### Required

Validates that a value is not null, empty, or whitespace (for strings). For value types, checks that the value is not the default.

```csharp
[Required]
public partial string? FirstName { get; set; }

[Required(ErrorMessage = "Customer name is required")]
public partial string? CustomerName { get; set; }
```

**Behavior:**
- Strings: fails for `null`, `""`, or whitespace-only
- Value types (int, DateTime, Guid): fails for default value (0, DateTime.MinValue, Guid.Empty)
- Nullable types: fails for `null`
- Reference types: fails for `null`

#### StringLength

Validates string length is within a range.

```csharp
// Maximum length only
[StringLength(100)]
public partial string? Description { get; set; }

// Minimum and maximum
[StringLength(100, MinimumLength = 2)]
public partial string? Username { get; set; }

// Custom message
[StringLength(50, MinimumLength = 5, ErrorMessage = "Name must be 5-50 characters")]
public partial string? Name { get; set; }
```

**Behavior:**
- Null or empty strings pass (use `[Required]` for null checks)
- Only validates string properties

#### MinLength / MaxLength

Validates minimum or maximum length of strings or collections.

```csharp
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

**Behavior:**
- Works with strings (character count)
- Works with collections and arrays (item count)
- Null values pass (use `[Required]` for null checks)

#### Range

Validates numeric values fall within a range. Supports integers, decimals, doubles, and dates.

```csharp
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

**Behavior:**
- Null values pass (use `[Required]` for null checks)
- Inclusive bounds (min and max values are valid)
- Date strings parsed using invariant culture

#### RegularExpression

Validates string matches a regex pattern.

```csharp
// Code format: 2 letters + 4 digits
[RegularExpression(@"^[A-Z]{2}\d{4}$")]
public partial string? ProductCode { get; set; }

// Phone format
[RegularExpression(@"^\d{3}-\d{3}-\d{4}$", ErrorMessage = "Format: 555-123-4567")]
public partial string? Phone { get; set; }

// Alphanumeric only
[RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Letters and numbers only")]
public partial string? Username { get; set; }
```

**Behavior:**
- Null or empty strings pass (use `[Required]` for null checks)
- Pattern must match the entire string
- Uses compiled regex for performance

#### EmailAddress

Validates email address format.

```csharp
[EmailAddress]
public partial string? Email { get; set; }

[EmailAddress(ErrorMessage = "Please enter a valid email")]
public partial string? ContactEmail { get; set; }
```

**Behavior:**
- Uses `MailAddress.TryCreate()` for RFC-compliant validation
- Null or empty strings pass (use `[Required]` for null checks)
- Rejects display name format like `"John <john@example.com>"`
- Accepts technically valid addresses like `user@localhost` (per RFC)

### Combining Attributes

Attributes can be combined for comprehensive validation:

```csharp
[Required(ErrorMessage = "Email is required")]
[EmailAddress(ErrorMessage = "Invalid email format")]
[StringLength(254, ErrorMessage = "Email too long")]
public partial string? Email { get; set; }

[Required]
[Range(1, 1000)]
public partial int Quantity { get; set; }

[StringLength(100, MinimumLength = 2)]
[RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Letters only")]
public partial string? FullName { get; set; }
```

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
// Setter - triggers rules
person.FirstName = "John";          // Rules execute

// LoadValue - no rules
person[nameof(IPerson.FirstName)].LoadValue("John");  // No rules

// MapFrom uses LoadValue internally
MapFrom(entity);                     // No rules during mapping

// Manually run rules after bulk load
await RunRules();
```

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

## Cross-Property Validation

Rules can check multiple properties:

```csharp
public class DateRangeRule : RuleBase<IEvent>
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

## Parent-Child Validation

Access parent entity from child validation:

```csharp
public class UniquePhoneTypeRule : RuleBase<IPersonPhone>
{
    public UniquePhoneTypeRule() : base(p => p.PhoneType) { }

    protected override IRuleMessages Execute(IPersonPhone target)
    {
        if (target.ParentPerson == null)
            return None;

        var hasDuplicate = target.ParentPerson.PersonPhoneList
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

## Loading Property Values Without Triggering Rules

Use `LoadProperty` to set values without triggering rules (e.g., in rule side effects):

```csharp
protected override IRuleMessages Execute(IPerson target)
{
    // Set FullName without triggering any FullName rules
    LoadProperty(target, t => t.FullName, $"{target.FirstName} {target.LastName}");
    return None;
}
```

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

```csharp
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

## Common Mistakes

### Putting Validation in Factory Methods

**Wrong:**
```csharp
[Insert]
public async Task Insert([Service] IUserRepository repo)
{
    await RunRules();
    if (!IsSavable) return;

    // DON'T DO THIS - validation too late!
    if (await repo.EmailExistsAsync(Email))
        throw new InvalidOperationException("Email in use");

    // ... persistence
}
```

**Right:**
```csharp
// Use AsyncRuleBase instead - runs during editing
public class UniqueEmailRule : AsyncRuleBase<IUser>, IUniqueEmailRule
{
    protected override async Task<IRuleMessages> Execute(IUser target, ...)
    {
        if (await _checkEmail.IsInUse(target.Email, target.Id))
            return (nameof(target.Email), "Email in use").AsRuleMessages();
        return None;
    }
}
```

See [Database-Dependent Validation](database-dependent-validation.md) for complete guidance.

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

- [Database-Dependent Validation](database-dependent-validation.md) - Async validation with Commands
- [Property System](property-system.md) - Property meta-properties and IsBusy
- [Blazor Binding](blazor-binding.md) - Displaying validation in UI
- [Meta-Properties Reference](meta-properties.md) - IsValid, IsBusy details
