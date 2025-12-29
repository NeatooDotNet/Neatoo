# Validation and Rules

Neatoo provides a powerful rules engine for implementing business logic and validation. Rules execute automatically when trigger properties change and provide immediate UI feedback.

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

Standard validation attributes are converted to rules automatically:

```csharp
[Required(ErrorMessage = "First Name is required")]
public partial string? FirstName { get; set; }

[EmailAddress(ErrorMessage = "Invalid email format")]
public partial string? Email { get; set; }

[StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be 2-100 characters")]
public partial string? Name { get; set; }

[Range(0, 150, ErrorMessage = "Age must be between 0 and 150")]
public partial int Age { get; set; }
```

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

## See Also

- [Property System](property-system.md) - Property meta-properties and IsBusy
- [Blazor Binding](blazor-binding.md) - Displaying validation in UI
- [Meta-Properties Reference](meta-properties.md) - IsValid, IsBusy details
