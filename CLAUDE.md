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

## Dependency Tracking: RemoteFactory

Neatoo depends on **RemoteFactory** (`C:\src\neatoodotnet\RemoteFactory`) for source generation of factory methods. Track analyzed commits to catch breaking changes.

### Last Analyzed Commit

| Date | Commit | Description | Breaking? | Plan |
|------|--------|-------------|-----------|------|
| 2026-01-01 | `27760f8` | 10.1.1 - Record Support | No | `docs/todos/remotefactory-record-support-update.md` |
| 2025-12-31 | `b90ba4d` | Multi-target .NET 8.0, 9.0, 10.0 | No | N/A |
| 2025-12-30 | `9e62dda` | Remove Mapper Functionality | **YES** | `docs/todos/remotefactory-mapper-removal-plan.md` |

### Current Version

**Neatoo.RemoteFactory 10.1.1** (updated 2026-01-01)

### Breaking Change Notes

**Multi-Targeting (10.0.1)**: RemoteFactory now supports .NET 8.0, 9.0, and 10.0. No breaking changes for Neatoo (uses net9.0). Consider adopting multi-targeting for Neatoo in future.

**Mapper Removal (9e62dda)**: RemoteFactory removed the MapperGenerator that auto-generated `MapTo`/`MapFrom` partial method implementations. Neatoo domain objects using these partial methods need manual implementations. See plan document for migration steps. **Status: MIGRATED**

**FactoryHintNameLength (9.20.1)**: New version enforces 50-character limit on fully qualified type names. Added `[assembly: FactoryHintNameLength(100)]` to `Neatoo.UnitTest/AssemblyAttributes.cs` to accommodate long namespace paths.

## Release Notes

Maintain release notes in `docs/release-notes/` for each version.

### When Releasing a New Version

1. **Create release notes file**: `docs/release-notes/vX.Y.Z.md`
2. **Update release index**: `docs/release-notes/index.md`
3. **Update version**: `Directory.Build.props`
4. **Update PackageReleaseNotes**: In `.csproj` files (brief summary)

### Release Notes Template

Each release file (`docs/releases/vX.Y.Z.md`) should include:

- Release date and type (Feature/Bugfix/Breaking)
- Summary of changes (1-2 sentences)
- New features with code examples
- Bug fixes
- Dependency updates
- Migration guide (if breaking changes)
- Links to related documentation

### Version Types

| Type | Version Bump | When to Use |
|------|--------------|-------------|
| Breaking | Major (10.x → 11.0) | API changes that break existing code |
| Feature | Minor (10.1 → 10.2) | New features, non-breaking additions |
| Bugfix | Patch (10.1.0 → 10.1.1) | Bug fixes, documentation updates |

### Current Release

See [docs/release-notes/index.md](docs/release-notes/index.md) for version history.

## Documentation Philosophy

Documentation in `docs/` is technical reference material, not marketing or tutorials.

### Audience Assumptions

Readers already understand:
- Domain-Driven Design (aggregates, entities, value objects, repositories)
- C# and .NET (async/await, generics, attributes, DI)
- Data modeling and persistence patterns

### Content Guidelines

| Include | Exclude |
|---------|---------|
| Neatoo syntax and API | "What is DDD" explanations |
| Required patterns | "Why use Neatoo" marketing |
| Anti-patterns with examples | Comparisons to other frameworks |
| Code examples | Conceptual introductions |
| Configuration and setup | |

### When to Explain "Why"

Generally avoid explaining why Neatoo works the way it does. However, when Neatoo has patterns that developers might question or find unusual, briefly explain the reasoning. This helps developers understand correct usage, not just memorize rules.

**Examples requiring "why":**
- Constructor injection vs `[Service]` injection—developers need to understand *when* to use each
- `[Remote]` attribute on child entities—explains the client-server boundary decision
- Validation in rules vs factory methods—prevents a common anti-pattern

Keep "why" explanations brief and focused on correct usage decisions.

### Writing Style

- Lead with code examples, not prose
- Show the pattern, then list constraints/gotchas
- Use tables for quick-reference (attributes, properties, methods)
- Keep explanatory text minimal—let code speak

## Documentation Samples

All code snippets in `docs/*.md` come from compiled, tested code in the samples projects.

### Projects

| Project | Purpose |
|---------|---------|
| `src/Neatoo.Documentation.Samples` | Sample code with region markers |
| `src/Neatoo.Documentation.Samples.Tests` | MSTest tests verifying samples |

### Region Marker Pattern

```csharp
#region docs:document-name:snippet-id
// Code that appears in documentation
#endregion
```

Example: `#region docs:validation-and-rules:required-attribute`

### Extract Snippets Script

```powershell
# List all snippets found
.\scripts\extract-snippets.ps1 -Verbose

# Verify snippets match documentation (CI use)
.\scripts\extract-snippets.ps1 -Verify

# Update documentation with compiled snippets
.\scripts\extract-snippets.ps1 -Update
```

### Key Patterns Discovered

| Pattern | Notes |
|---------|-------|
| `PauseAllActions()` | On concrete class, not interface. Use internal examples with concrete types. |
| `Parent` property | Points to aggregate root, NOT the containing list. |
| `[Required]` on `string?` | Catches null, empty, AND whitespace (stricter than .NET default). Uses `IsNullOrWhiteSpace()`. |
| `MapModifiedTo` | Still generated by Neatoo.BaseGenerator. Declare as `public partial void MapModifiedTo(Entity e);` |

### Adding New Snippets

1. Add code with region markers to appropriate sample file
2. Add corresponding test(s) in the Tests project
3. Run `dotnet test` to verify
4. Run `.\scripts\extract-snippets.ps1 -Verbose` to confirm extraction
5. Run `.\scripts\extract-snippets.ps1 -Update` to update docs
