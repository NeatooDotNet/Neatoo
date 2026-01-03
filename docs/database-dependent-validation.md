# Database-Dependent Validation

This guide covers the correct pattern for implementing validation rules that require database access, such as uniqueness checks, overlap detection, and referential integrity validation.

## Quick Reference

| Validation Type | Correct Location | Wrong Location |
|-----------------|------------------|----------------|
| Uniqueness checks | `AsyncRuleBase<T>` | Factory methods |
| Overlap detection | `AsyncRuleBase<T>` | Factory methods |
| Referential integrity | `AsyncRuleBase<T>` | Factory methods |
| External API validation | `AsyncRuleBase<T>` | Factory methods |

## The Anti-Pattern: Validation in Factory Methods

When developers need database access for validation, they often place the logic in `[Insert]` or `[Update]` methods because services are readily available there:

```csharp
[Insert]
[Remote]
public async Task Insert([Service] IShiftRepository repository)
{
    await RunRules();
    if (!IsSavable)
        return;

    // BAD: Business rule check in factory method
    if (await repository.HasOverlappingShiftAsync(EmployeeId, Start, End, null))
    {
        throw new InvalidOperationException(
            "This shift overlaps with an existing shift.");
    }

    // ... persistence logic
}
```

**This pattern is wrong.** While it prevents invalid data from being persisted, it creates significant problems.

## Why Factory Validation Is Wrong

### 1. Validation Happens Too Late

By the time `Insert`/`Update` executes, the user has already filled out a form and clicked "Save". They only discover the problem after attempting to persist.

With Neatoo rules, validation runs **during editing** - users see errors immediately as they change values.

### 2. Exception-Based Error Handling

Factory methods that throw exceptions for validation:
- Return HTTP 500 errors (server error, not validation error)
- Don't integrate with Neatoo validation UI components
- Provide poor UX (error toast vs. inline field validation)
- Break the expected application flow

### 3. Bypasses the Rule System

Neatoo's rule system provides:
- **Property change triggers** - Rules re-run when properties change
- **Async support** - `AsyncRuleBase<T>` for database operations
- **Rule messages** - Integrate with UI validation display
- **Busy state** - `IsBusy` shows loading indicators during async validation
- **Savable state** - `IsSavable` reflects all rule results

Factory validation bypasses all of these.

### 4. Inconsistent User Experience

When some rules run during editing and others only at save time, users can't predict when they'll see errors. This creates a frustrating experience.

## The Correct Pattern

### Step 1: Create a Command

Commands are `[Factory]` classes with `[Execute]` methods that run on the server:

<!-- snippet: docs:database-dependent-validation:command-pattern -->
```csharp
/// <summary>
/// Command for checking email uniqueness.
/// The source generator creates a delegate that can be injected and executed remotely.
/// </summary>
[Factory]
public static partial class CheckEmailUnique
{
    [Execute]
    internal static async Task<bool> _IsUnique(
        string email,
        Guid? excludeId,
        [Service] IUserRepository repo)
    {
        return !await repo.EmailExistsAsync(email, excludeId);
    }
}
```
<!-- /snippet -->

The source generator creates a delegate `CheckShiftOverlap.HasOverlap` that can be injected and executed remotely.

### Step 2: Create an Async Rule

<!-- snippet: docs:database-dependent-validation:async-rule -->
```csharp
/// <summary>
/// Async rule that validates email uniqueness using the command.
/// </summary>
public interface IAsyncUniqueEmailRule : IRule<IUserWithEmail> { }

public class AsyncUniqueEmailRule : AsyncRuleBase<IUserWithEmail>, IAsyncUniqueEmailRule
{
    private readonly CheckEmailUnique.IsUnique _isUnique;

    public AsyncUniqueEmailRule(CheckEmailUnique.IsUnique isUnique)
    {
        _isUnique = isUnique;
        AddTriggerProperties(u => u.Email);
    }

    protected override async Task<IRuleMessages> Execute(
        IUserWithEmail target, CancellationToken? token = null)
    {
        if (string.IsNullOrEmpty(target.Email))
            return None;

        // Skip if property not modified (optimization)
        if (!target.IsNew && !target[nameof(target.Email)].IsModified)
            return None;

        var excludeId = target.IsNew ? null : (Guid?)target.Id;

        if (!await _isUnique(target.Email, excludeId))
        {
            return (nameof(target.Email), "Email already in use").AsRuleMessages();
        }

        return None;
    }
}
```
<!-- /snippet -->

### Step 3: Register the Rule

```csharp
// In domain model constructor
public ShiftEdit(
    IEntityBaseServices<ShiftEdit> services,
    IShiftOverlapRule overlapRule) : base(services)
{
    RuleManager.AddRule(overlapRule);
}

// In DI registration
builder.Services.AddScoped<IShiftOverlapRule, ShiftOverlapRule>();
```

### Step 4: Clean Factory Methods

Factory methods should contain **only** persistence logic:

<!-- snippet: docs:database-dependent-validation:clean-factory -->
```csharp
/// <summary>
/// Entity with clean factory methods - validation is in rules, not here.
/// </summary>
[Factory]
internal partial class UserWithEmail : EntityBase<UserWithEmail>, IUserWithEmail
{
    public UserWithEmail(
        IEntityBaseServices<UserWithEmail> services,
        IAsyncUniqueEmailRule uniqueEmailRule) : base(services)
    {
        // Register the async validation rule
        RuleManager.AddRule(uniqueEmailRule);
    }

    public partial Guid Id { get; set; }
    public partial string? Email { get; set; }
    public partial string? Name { get; set; }

    [Create]
    public void Create()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Clean Insert - only persistence logic, no validation.
    /// Validation is handled by rules during editing.
    /// </summary>
    [Insert]
    public async Task Insert()
    {
        await RunRules();
        if (!IsSavable)
            return;

        // Only persistence - validation already handled by rules
        // In real code: await repository.InsertAsync(entity);
    }
}
```
<!-- /snippet -->

## Benefits Comparison

| Aspect | Factory Validation | Command + Rule Pattern |
|--------|-------------------|----------------------|
| **When errors appear** | After clicking Save | During editing |
| **Error display** | Exception/500 error | Inline validation |
| **User experience** | Trial and error | Immediate feedback |
| **UI integration** | None | Full validation binding |
| **Loading indicators** | None | `IsBusy` on properties |
| **Testability** | Harder | Rules independently testable |

## Common Validation Patterns

### Uniqueness Check

See the complete example above (Steps 1-4) for email uniqueness validation.

### Date Range Overlap

```csharp
public class BookingOverlapRule : AsyncRuleBase<IBooking>, IBookingOverlapRule
{
    private readonly CheckBookingOverlap.HasOverlap _hasOverlap;

    public BookingOverlapRule(CheckBookingOverlap.HasOverlap hasOverlap)
    {
        _hasOverlap = hasOverlap;
        AddTriggerProperties(b => b.RoomId, b => b.StartDate, b => b.EndDate);
    }

    protected override async Task<IRuleMessages> Execute(
        IBooking target, CancellationToken? token = null)
    {
        if (target.RoomId == Guid.Empty ||
            !target.StartDate.HasValue ||
            !target.EndDate.HasValue)
            return None;

        if (await _hasOverlap(
            target.RoomId,
            target.StartDate.Value,
            target.EndDate.Value,
            target.IsNew ? null : target.BookingId))
        {
            return (new[]
            {
                (nameof(target.StartDate), "Room is already booked for these dates"),
                (nameof(target.EndDate), "Room is already booked for these dates")
            }).AsRuleMessages();
        }

        return None;
    }
}
```

## Why Developers Fall Into This Trap

1. **Convenience** - Services are readily available in factory methods
2. **It "works"** - Invalid data is prevented from persisting
3. **Discovery** - The Command + AsyncRuleBase pattern isn't immediately obvious
4. **Complexity** - The correct pattern requires more code
5. **Examples** - Many tutorials show simple synchronous validation

## Rule of Thumb

> **If you're about to throw an exception in a factory method for a validation failure, stop and ask: "Should this be an AsyncRuleBase with a Command instead?"**
>
> The answer is almost always **yes**.

## See Also

- [Validation and Rules](validation-and-rules.md) - Complete rules documentation
- [Factory Operations](factory-operations.md) - Factory method patterns
- [Troubleshooting](troubleshooting.md) - Common issues and solutions
