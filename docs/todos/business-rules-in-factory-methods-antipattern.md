# Anti-Pattern: Business Rules in Factory Methods

## Summary

When implementing business rules that require database access (uniqueness checks, overlap detection, referential integrity, etc.), developers may instinctively place these validations in `[Insert]` and `[Update]` factory methods. While this "works" in the sense that it prevents invalid data from being persisted, it represents a significant architectural oversight that undermines the Neatoo rule system and degrades user experience.

## The Anti-Pattern

```csharp
[Insert]
[Remote]
public async Task Insert([Service] IShiftRepository repository)
{
    await RunRules();
    if (!IsSavable)
        return;

    // ❌ ANTI-PATTERN: Business rule check in factory method
    if (Start.HasValue && await repository.HasOverlappingShiftAsync(EmployeeId, Start.Value, End, null))
    {
        throw new InvalidOperationException("This shift overlaps with an existing shift for this employee.");
    }

    // ... persistence logic
}
```

## Why This Is Wrong

### 1. Validation Happens Too Late

By the time `Insert`/`Update` executes, the user has already clicked "Save". They only discover the problem after attempting to persist. In contrast, Neatoo rules can validate **during editing**, providing immediate feedback as the user changes values.

### 2. Exception-Based Error Handling

Factory methods that throw exceptions for validation failures:
- Return HTTP 500 errors to the client (server error, not validation error)
- Don't integrate with the Neatoo validation UI components
- Provide a poor user experience (error toast vs. inline field validation)
- Break the expected application flow

### 3. Bypasses the Rule System

Neatoo provides a sophisticated rule system with:
- **Property change triggers** - Rules re-run when specific properties change
- **Async support** - `AsyncRuleBase<T>` for database-dependent validations
- **Rule messages** - `IRuleMessages` that integrate with UI validation display
- **Modified property tracking** - `target[nameof(Property)].IsModified`
- **Savable state** - `IsSavable` reflects all rule results

Putting validation in factory methods bypasses all of these capabilities.

### 4. Inconsistent Architecture

When some rules run during editing (via `RuleManager`) and others only at save time (in factory methods), the application behavior becomes inconsistent and confusing. Users can't predict when they'll see validation errors.

### 5. No Opportunity for Correction

With in-rule validation, users see errors immediately and can correct them. With factory-method validation, users must:
1. Fill out a form
2. Click Save
3. See an error
4. Go back and fix
5. Try again

This creates a frustrating trial-and-error experience.

## The Correct Pattern

### Step 1: Create a Command

Commands are `[Factory]` classes with `[Execute]` methods that run on the server with full service access:

```csharp
[Factory]
public static partial class CheckShiftOverlap
{
    [Execute]
    internal static async Task<bool> _HasOverlap(
        Guid employeeId,
        DateTime start,
        DateTime? end,
        Guid? excludeShiftId,
        [Service] IShiftRepository repository)
    {
        return await repository.HasOverlappingShiftAsync(
            employeeId, start, end, excludeShiftId);
    }
}
```

The source generator creates a delegate `CheckShiftOverlap.HasOverlap` that can be injected and executed remotely.

### Step 2: Create an Async Rule

```csharp
public interface IShiftOverlapRule : IRule<IShiftEdit> { }

public class ShiftOverlapRule : AsyncRuleBase<IShiftEdit>, IShiftOverlapRule
{
    private readonly CheckShiftOverlap.HasOverlap _hasOverlap;

    public ShiftOverlapRule(CheckShiftOverlap.HasOverlap hasOverlap)
    {
        _hasOverlap = hasOverlap;
        AddTriggerProperties(s => s.EmployeeId, s => s.Start, s => s.End);
    }

    protected override async Task<IRuleMessages> Execute(
        IShiftEdit target,
        CancellationToken? token = null)
    {
        if (target.EmployeeId == Guid.Empty || !target.Start.HasValue)
            return None;

        // Only check if relevant properties have been modified
        if (!target.IsNew)
        {
            var employeeModified = target[nameof(target.EmployeeId)].IsModified;
            var startModified = target[nameof(target.Start)].IsModified;
            var endModified = target[nameof(target.End)].IsModified;

            if (!employeeModified && !startModified && !endModified)
                return None;
        }

        var excludeShiftId = target.IsNew ? null : (Guid?)target.ShiftId;

        if (await _hasOverlap(target.EmployeeId, target.Start.Value, target.End, excludeShiftId))
        {
            return (nameof(target.Start),
                "This shift overlaps with an existing shift for this employee.")
                .AsRuleMessages();
        }

        return None;
    }
}
```

### Step 3: Register and Use the Rule

```csharp
// In domain model constructor
public ShiftEdit(
    IEntityBaseServices<ShiftEdit> services,
    IShiftEditRule editRule,
    IShiftOverlapRule overlapRule) : base(services)
{
    RuleManager.AddRule(editRule);
    RuleManager.AddRule(overlapRule);
}

// In DI registration
builder.Services.AddScoped<IShiftOverlapRule, ShiftOverlapRule>();
```

### Step 4: Clean Factory Methods

Factory methods should only contain persistence logic:

```csharp
[Insert]
[Remote]
public async Task Insert([Service] IShiftRepository repository)
{
    await RunRules();
    if (!IsSavable)
        return;

    // ✅ Only persistence logic - validation handled by rules
    var entity = new ShiftEntity { ShiftId = Guid.NewGuid() };
    MapTo(entity);
    await repository.InsertAsync(entity);
    ShiftId = entity.ShiftId;
}
```

## Benefits of the Correct Pattern

| Aspect | Factory Method Approach | Command + Rule Approach |
|--------|------------------------|------------------------|
| **Timing** | Only at save | During editing |
| **Error Display** | Exception/500 error | Inline validation messages |
| **User Experience** | Trial and error | Immediate feedback |
| **UI Integration** | None | Full validation binding |
| **Consistency** | Inconsistent with other rules | Unified rule system |
| **Testability** | Harder to test | Rules are independently testable |

## Why Developers Fall Into This Trap

1. **Convenience**: Services are readily available in factory methods via `[Service]`
2. **It "Works"**: Invalid data is indeed prevented from persisting
3. **Discovery**: `AsyncRuleBase<T>` and the Command pattern may not be immediately obvious
4. **Complexity**: The correct pattern requires more files and concepts
5. **Documentation Gap**: Examples may focus on simple synchronous rules

## Recommendations for Neatoo Project

### 1. Documentation Enhancement
- Add a dedicated guide for "Database-Dependent Validation"
- Include the Command + AsyncRuleBase pattern in getting-started docs
- Show before/after examples like this document

### 2. Code Analyzer (Optional)
Consider a Roslyn analyzer that warns when:
- Factory methods (`[Insert]`, `[Update]`) throw `InvalidOperationException`
- Factory methods contain validation-like patterns after `RunRules()`

### 3. Templates/Snippets
Provide IDE snippets for:
- Command class with `[Execute]` method
- `AsyncRuleBase<T>` implementation
- Common validation patterns (uniqueness, overlap, referential integrity)

### 4. Example Project
Ensure the example project (Person) demonstrates:
- ✅ `UniqueName` command + `UniqueNameRule` (already exists)
- Document this pattern prominently in the README

## Conclusion

The Neatoo framework provides powerful, well-designed tools for async validation through Commands and `AsyncRuleBase<T>`. When developers bypass these tools by putting validation in factory methods, they sacrifice user experience, architectural consistency, and the full benefits of the rule system.

The extra effort to implement the Command + Rule pattern pays dividends in:
- Better user experience (immediate feedback)
- Cleaner architecture (single responsibility)
- Consistent behavior (all validation through rules)
- Better testability (rules can be unit tested)

**Rule of thumb**: If you're about to throw an exception in a factory method for a validation failure, stop and ask: "Should this be an AsyncRuleBase with a Command instead?"

The answer is almost always **yes**.
