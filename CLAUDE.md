# Claude Code Guidelines for Neatoo

## Required: Load Neatoo Skill

**ALWAYS run `/neatoo` at the start of any Neatoo development session.** The skill contains essential patterns, anti-patterns, and pitfalls for working with Neatoo entities, rules, and factories.

## Documentation Code Samples

**ALWAYS run `/docs-snippets` when working with `docs/` or the neatoo skill.**

### Workflow for Code in Documentation

1. **Add code to samples first** - All code examples live in `docs/samples/`
2. **Mark regions** - Use `#region {snippet-id}` markers in sample files (IDs must be globally unique)
3. **Run sync** - Run `dotnet mdsnippets` to extract and inject snippets
4. **Never copy-paste** - Documentation code must come from compiled, tested samples

### Commands

```powershell
# Sync documentation with code snippets
dotnet mdsnippets

# Verify all code blocks have markers (pseudo:/invalid:)
pwsh scripts/verify-code-blocks.ps1

# Check for duplicate snippet IDs
pwsh scripts/check-duplicate-ids.ps1
```

This ensures all code in documentation is actually compiled and tested.

## Environment

- **Platform:** Windows 11
- **Shell:** PowerShell / cmd

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

### Critical: NuGet Package Updates

**When you observe new commits in RemoteFactory**, you MUST:

1. **Check if a new NuGet package was published** - Look for version bumps in RemoteFactory's `Directory.Build.props`
2. **Update both packages in Neatoo**:
   - `Neatoo.RemoteFactory`
   - `Neatoo.RemoteFactory.AspNetCore`
3. **Build and test Neatoo** to verify compatibility
4. **Document any breaking changes** in this file and create a plan if needed

The packages are referenced in `Directory.Packages.props`. Update command:
```powershell
# Check current versions
dotnet list package | findstr RemoteFactory

# Update to latest (after confirming new version exists on NuGet)
# Edit Directory.Packages.props with new version numbers
```

**Current Status**: Neatoo is up-to-date with the latest RemoteFactory.

### Last Analyzed Commit

| Date | Commit | Description | Breaking? | Plan |
|------|--------|-------------|-----------|------|
| 2026-01-05 | N/A | 10.5.0 - Upgrade Complete | Migrated | `docs/todos/remotefactory-upgrade-blocked.md` |
| 2026-01-04 | `db5d76e` | 10.4.0 - CancellationToken Support | Migrated | `docs/todos/remotefactory-upgrade-blocked.md` |
| 2026-01-04 | `ef20bd3` | 10.2.0 - Ordinal Serialization | Migrated | `docs/todos/remotefactory-upgrade-blocked.md` |
| 2026-01-01 | `27760f8` | 10.1.1 - Record Support | No | `docs/todos/remotefactory-record-support-update.md` |
| 2025-12-31 | `b90ba4d` | Multi-target .NET 8.0, 9.0, 10.0 | No | N/A |
| 2025-12-30 | `9e62dda` | Remove Mapper Functionality | **YES** | `docs/todos/remotefactory-mapper-removal-plan.md` |

### Current Version

**Neatoo.RemoteFactory 10.5.0** (updated 2026-01-05)

### Breaking Change Notes

**Ordinal Serialization (10.2.0)**: The generator creates `FromOrdinalArray()` and `JsonConverter.Read()` methods using object initializer syntax (`new Type { ... }`). **Status: MIGRATED** - Works correctly when entities use proper DI patterns and deserialize to interfaces.

**CancellationToken Support (10.4.0)**: `IMakeRemoteDelegateRequest` interface methods now require `CancellationToken` parameter. **Status: MIGRATED** - Updated test infrastructure and example server.

**Multi-Targeting (10.0.1)**: RemoteFactory now supports .NET 8.0, 9.0, and 10.0. No breaking changes for Neatoo (uses net9.0). Consider adopting multi-targeting for Neatoo in future.

**Mapper Removal (9e62dda)**: RemoteFactory removed the MapperGenerator that auto-generated `MapTo`/`MapFrom` partial method implementations. Neatoo domain objects using these partial methods need manual implementations. See plan document for migration steps. **Status: MIGRATED**

**FactoryHintNameLength (9.20.1)**: New version enforces 50-character limit on fully qualified type names. Added `[assembly: FactoryHintNameLength(100)]` to `Neatoo.UnitTest/AssemblyAttributes.cs` to accommodate long namespace paths.

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
