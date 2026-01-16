# Adding Validation

How to implement validation rules in Neatoo entities.

## Validation Options

| Option | Use When | Runs |
|--------|----------|------|
| Data Annotations | Simple field validation | Immediately on change |
| Inline Validation | Quick one-off rules | Immediately on change |
| RuleBase<T> | Reusable sync rules | Immediately on change |
| AsyncRuleBase<T> | Rules that need async operations | After debounce, updates IsBusy |

## Data Annotations

Standard .NET attributes work on partial properties:

```csharp
[Factory]
internal partial class Contact : EntityBase<Contact>, IContact
{
    public Contact(IEntityBaseServices<Contact> services) : base(services) { }

    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 2)]
    public partial string? Name { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email")]
    public partial string? Email { get; set; }

    [Range(18, 120)]
    public partial int? Age { get; set; }

    [RegularExpression(@"^\d{3}-\d{4}$", ErrorMessage = "Format: 123-4567")]
    public partial string? Phone { get; set; }

    [Create]
    public void Create() { }
}
```

**Supported Attributes:**
- `[Required]` - Value cannot be null/empty
- `[StringLength]` - Min/max string length
- `[Range]` - Numeric range
- `[EmailAddress]` - Email format
- `[RegularExpression]` - Pattern match
- `[MinLength]`, `[MaxLength]` - Collection size

## Inline Validation

Quick rules defined in the constructor:

```csharp
public Contact(IEntityBaseServices<Contact> services) : base(services)
{
    // Single property validation
    RuleManager.AddValidation(
        t => string.IsNullOrEmpty(t.Name) ? "Name is required" : "",
        t => t.Name);

    // Cross-property validation
    RuleManager.AddValidation(
        t => t.EndDate < t.StartDate ? "End date must be after start date" : "",
        t => t.StartDate, t => t.EndDate);
}
```

**Syntax:** `AddValidation(Func<T, string> validator, params Expression<Func<T, object?>>[] triggerProperties)`

- Return empty string = valid
- Return message = invalid
- Specify which properties trigger re-validation

## Custom Sync Rules (RuleBase<T>)

For reusable validation logic:

```csharp
// 1. Define interface (optional but recommended for DI)
public interface IAgeValidationRule : IRule<IPerson> { }

// 2. Implement rule
public class AgeValidationRule : RuleBase<IPerson>, IAgeValidationRule
{
    // Specify which properties trigger this rule
    public AgeValidationRule() : base(p => p.Age, p => p.BirthDate) { }

    protected override IRuleMessages Execute(IPerson target)
    {
        // Use RuleMessages.If for conditional checks
        return RuleMessages
            .If(target.Age < 0, nameof(target.Age), "Age cannot be negative")
            .ElseIf(() => target.Age > 150, nameof(target.Age), "Age seems unrealistic");
    }
}

// 3. Register in DI
builder.Services.AddScoped<IAgeValidationRule, AgeValidationRule>();

// 4. Inject and add in constructor
public Person(IEntityBaseServices<Person> services, IAgeValidationRule ageRule)
    : base(services)
{
    RuleManager.AddRule(ageRule);
}
```

### Rule Return Patterns

```csharp
protected override IRuleMessages Execute(IPerson target)
{
    // Pattern 1: Simple conditional
    if (target.Age < 0)
        return (nameof(target.Age), "Age cannot be negative").AsRuleMessages();
    return None;

    // Pattern 2: Chained conditionals
    return RuleMessages
        .If(string.IsNullOrEmpty(target.Name), nameof(target.Name), "Name required")
        .ElseIf(() => target.Name.Length < 2, nameof(target.Name), "Name too short");

    // Pattern 3: Multiple errors
    var messages = new List<RuleMessage>();
    if (string.IsNullOrEmpty(target.FirstName))
        messages.Add((nameof(target.FirstName), "First name required"));
    if (string.IsNullOrEmpty(target.LastName))
        messages.Add((nameof(target.LastName), "Last name required"));
    return messages.AsRuleMessages();
}
```

## Async Rules (AsyncRuleBase<T>)

For rules that need to call databases, APIs, or services:

```csharp
// Interface with factory method attribute for remote execution
public interface ICheckEmailUnique
{
    Task<bool> IsUnique(string email, Guid? excludeId);
}

// Async rule implementation
public class UniqueEmailRule : AsyncRuleBase<IPerson>, IUniqueEmailRule
{
    private readonly ICheckEmailUnique _checker;

    public UniqueEmailRule(ICheckEmailUnique checker) : base(p => p.Email)
    {
        _checker = checker;
    }

    protected override async Task<IRuleMessages> Execute(
        IPerson target, CancellationToken? token = null)
    {
        // Skip if value hasn't changed (optimization)
        if (!target[nameof(target.Email)].IsModified)
            return None;

        // Skip if empty (let [Required] handle that)
        if (string.IsNullOrEmpty(target.Email))
            return None;

        var isUnique = await _checker.IsUnique(target.Email, target.Id);

        if (!isUnique)
            return (nameof(target.Email), "Email already in use").AsRuleMessages();

        return None;
    }
}
```

### Server-Side Check with Command Pattern

The uniqueness check should run on the server (access to database):

```csharp
// Command interface
public interface ICheckEmailUnique
{
    [Remote]  // Executes on server
    Task<bool> IsUnique(string email, Guid? excludeId);
}

// Command implementation (registered on server)
[Factory]
internal class CheckEmailUnique : ICheckEmailUnique
{
    [Remote]
    public async Task<bool> IsUnique(string email, Guid? excludeId, [Service] IDbContext db)
    {
        return !await db.Persons
            .Where(p => p.Email == email && p.Id != excludeId)
            .AnyAsync();
    }
}
```

## Cross-Property Validation

Rules that validate relationships between properties:

```csharp
public class DateRangeRule : RuleBase<IEvent>
{
    public DateRangeRule() : base(e => e.StartDate, e => e.EndDate) { }

    protected override IRuleMessages Execute(IEvent target)
    {
        if (target.EndDate < target.StartDate)
        {
            return (nameof(target.EndDate), "End date must be after start date").AsRuleMessages();
        }
        return None;
    }
}
```

## Parent-Based Validation

Rules that validate against parent entity:

```csharp
public class QuantityLimitRule : RuleBase<IOrderLine>
{
    public QuantityLimitRule() : base(l => l.Quantity) { }

    protected override IRuleMessages Execute(IOrderLine target)
    {
        // Access parent through Parent property
        var order = target.Parent as IOrder;
        if (order == null) return None;

        if (target.Quantity > order.MaxQuantityPerLine)
        {
            return (nameof(target.Quantity),
                $"Quantity cannot exceed {order.MaxQuantityPerLine}").AsRuleMessages();
        }
        return None;
    }
}
```

## Handling Async Rule State

Async rules affect `IsBusy` and `IsSavable`:

```csharp
// In Blazor component
<MudButton Disabled="@(!entity.IsSavable)" OnClick="@Save">Save</MudButton>

@if (entity.IsBusy)
{
    <MudProgressCircular Indeterminate="true" />
}

@code {
    private async Task Save()
    {
        // Wait for async rules to complete
        await entity.WaitForTasks();

        // Re-check savability after async rules
        if (!entity.IsSavable) return;

        entity = await entityFactory.Save(entity);
    }
}
```

## Running Rules Manually

```csharp
// Run all rules for entity
await entity.RunRules();

// Check specific property validation
var isEmailValid = entity[nameof(entity.Email)].IsValid;
var emailErrors = entity[nameof(entity.Email)].PropertyMessages;
```

## Common Mistakes

### Validation in Factory Methods

```csharp
// WRONG - user only sees error after clicking Save
[Insert]
public async Task Insert([Service] IDbContext db)
{
    if (await db.EmailExistsAsync(Email))
        throw new InvalidOperationException("Email in use");  // Too late!
}

// CORRECT - user sees error immediately when typing
public class UniqueEmailRule : AsyncRuleBase<IPerson>
{
    protected override async Task<IRuleMessages> Execute(IPerson target, ...)
    {
        if (await _checker.EmailExists(target.Email))
            return (nameof(target.Email), "Email in use").AsRuleMessages();
        return None;
    }
}
```

### Forgetting to Register Rules

```csharp
// WRONG - rule exists but not added
public Person(IEntityBaseServices<Person> services, IAgeRule ageRule)
    : base(services)
{
    // Oops! Forgot to add the rule
}

// CORRECT - add to RuleManager
public Person(IEntityBaseServices<Person> services, IAgeRule ageRule)
    : base(services)
{
    RuleManager.AddRule(ageRule);  // Now it runs!
}
```

### Not Registering Rule in DI

```csharp
// WRONG - rule not registered in DI
// (will get DI resolution error)

// CORRECT - register in Program.cs
builder.Services.AddScoped<IAgeRule, AgeRule>();
```

## Best Practices

1. **Use annotations for simple cases** - Required, StringLength, Range
2. **Use inline for one-off cross-property rules** - Quick, no extra class
3. **Use RuleBase for reusable sync validation** - Can inject into multiple entities
4. **Use AsyncRuleBase for database/API validation** - Unique checks, external validation
5. **Check IsModified in async rules** - Skip unnecessary server calls
6. **Return empty string or None for valid** - Not null

## Next Steps

- [saving-data.md](saving-data.md) - Persist validated entities
- [troubleshooting.md](troubleshooting.md) - Debug validation issues
