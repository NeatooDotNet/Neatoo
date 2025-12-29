# Claude Code Guidelines for Neatoo

## Environment

- **Platform:** Windows 11
- **Shell:** PowerShell / cmd

## Critical: Business Rules Architecture

### Where Business Rules Belong

| Validation Type | Correct Location | WRONG Location |
|-----------------|------------------|----------------|
| Required fields | `[Required]` attribute | Factory methods |
| Format validation | `[RegularExpression]`, `[EmailAddress]` | Factory methods |
| Range/length checks | `[Range]`, `[StringLength]` | Factory methods |
| Cross-property rules | `RuleBase<T>` | Factory methods |
| Database lookups (uniqueness, overlap) | `AsyncRuleBase<T>` + Command | Factory methods |

### Anti-Pattern: Validation in Factory Methods

**NEVER put business validation in `[Insert]` or `[Update]` factory methods.**

```csharp
// BAD - Anti-pattern!
[Insert]
public async Task Insert([Service] IRepository repo)
{
    await RunRules();
    if (!IsSavable) return;

    // DON'T DO THIS - validation only runs at save time!
    if (await repo.EmailExistsAsync(Email))
        throw new InvalidOperationException("Email in use");

    // ... persistence
}
```

**Why this is wrong:**
1. Users only see errors after clicking Save (poor UX)
2. Throws exceptions instead of validation messages
3. Bypasses Neatoo's rule system (no IsBusy, no UI integration)
4. Returns HTTP 500 instead of validation error

### Correct Pattern: AsyncRuleBase + Command

```csharp
// 1. Create a Command for server-side logic
[Factory]
public static partial class CheckEmailUnique
{
    [Execute]
    internal static async Task<bool> _IsUnique(
        string email, Guid? excludeId,
        [Service] IUserRepository repo)
    {
        return !await repo.EmailExistsAsync(email, excludeId);
    }
}

// 2. Create an async rule
public class UniqueEmailRule : AsyncRuleBase<IUser>, IUniqueEmailRule
{
    private readonly CheckEmailUnique.IsUnique _isUnique;

    public UniqueEmailRule(CheckEmailUnique.IsUnique isUnique)
    {
        _isUnique = isUnique;
        AddTriggerProperties(u => u.Email);
    }

    protected override async Task<IRuleMessages> Execute(IUser target, CancellationToken? token = null)
    {
        if (string.IsNullOrEmpty(target.Email))
            return None;

        if (!target[nameof(target.Email)].IsModified)
            return None;  // Skip if not changed

        var excludeId = target.IsNew ? null : (Guid?)target.Id;

        if (!await _isUnique(target.Email, excludeId))
            return (nameof(target.Email), "Email already in use").AsRuleMessages();

        return None;
    }
}

// 3. Factory method contains ONLY persistence
[Insert]
public async Task Insert([Service] IDbContext db)
{
    await RunRules();
    if (!IsSavable) return;

    // Only persistence logic here
    var entity = new UserEntity();
    MapTo(entity);
    db.Users.Add(entity);
    await db.SaveChangesAsync();
}
```

### Decision Guide

When implementing validation that requires database access:

1. **Ask**: "Does this need to check the database?"
   - No → Use `[Required]`, `[Range]`, `RuleBase<T>`, etc.
   - Yes → Continue to step 2

2. **Create a Command** with `[Execute]` method for the database logic

3. **Create an AsyncRuleBase** that calls the command

4. **Register the rule** in DI and add to RuleManager

**See**: `docs/database-dependent-validation.md` for complete examples.

## Testing Philosophy

### Unit Tests - No Mocking Neatoo Classes

When writing unit tests for Neatoo:

1. **Only mock external dependencies** - Do not mock Neatoo interfaces or classes
2. **Use real Neatoo classes** - "New up" Neatoo dependencies directly
3. **Inherit from Neatoo base classes** - Don't manually implement Neatoo interfaces with stub logic
4. **Test cohesive behavior** - Ensure Neatoo works as a cohesive unit

**Critical:** Never recreate Neatoo library logic within test classes. If you need a test class that implements `IValidateMetaProperties`, inherit from `ValidateBase<T>` - don't manually implement the interface. This ensures:
- If ValidateBase behavior changes, the tests will catch breaking changes
- Tests validate real Neatoo behavior, not stubbed behavior
- No duplicate/divergent logic between library and tests

This approach:
- Validates actual framework integration, not just isolated behavior
- Reduces mock maintenance overhead
- Catches integration issues between Neatoo classes
- Is more representative of real usage

### Example - Using Real Classes

Instead of mocking `IPropertyInfo`:
```csharp
// DO: Use real PropertyInfoWrapper
var propertyInfo = typeof(TestPoco).GetProperty("Name");
var wrapper = new PropertyInfoWrapper(propertyInfo);
var property = new Property<string>(wrapper);

// DON'T: Mock Neatoo interfaces
var mockPropertyInfo = new Mock<IPropertyInfo>();
mockPropertyInfo.Setup(p => p.Name).Returns("Name");
```

Instead of mocking `IValidateBase`:
```csharp
// DO: Create a real ValidateBase implementation
[SuppressFactory]
public class TestValidateObject : ValidateBase<TestValidateObject>
{
    public string Name { get => Getter<string>(); set => Setter(value); }
}

// DON'T: Mock IValidateBase
var mockTarget = new Mock<IValidateBase>();
```

### Test Organization

Tests are organized into:
- `Unit/` - True unit tests of individual classes (using real Neatoo dependencies)
- `Integration/Concepts/` - Integration tests for each base class, rules, validation
- `Integration/Aggregates/` - Full DDD aggregate tests including rules

### Test Infrastructure

- `TestInfrastructure/IntegrationTestBase.cs` - Base class for integration tests with DI
- `TestInfrastructure/UnitTestBase.cs` - Base class for unit tests
- Use MSTest with `[TestClass]` and `[TestMethod]`
- Naming convention: `MethodName_Scenario_ExpectedResult`
