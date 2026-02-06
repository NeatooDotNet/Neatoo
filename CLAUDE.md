
The Neatoo Solution is at src/Neatoo.sln



## Neatoo Terminology

### Base Classes
- `EntityBase<T>` - Persistent entities with full CRUD lifecycle
- `ValidateBase<T>` - Value objects, read models, validation-only objects
- `EntityListBase<I>` - Collections of child entities within an aggregate
- `ValidateListBase<I>` - Collections of read models
- Static classes with `[Factory]` and `[Execute]` - Commands

### State Properties
- `IsModified` - True when object has unsaved changes
- `IsSelfModified` - True when this object (not children) has changes
- `IsNew` - True when object hasn't been persisted yet
- `IsValid` - True when all validation rules pass
- `IsSelfValid` - True when this object's rules pass (not children)

### Factory Operations

See the `/RemoteFactory` skill for factory attribute details (`[Create]`, `[Fetch]`, `[Insert]`, `[Update]`, `[Delete]`, `[Execute]`, `[Remote]`).

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

## Documentation and Project Management

- **Framework documentation**: Use `/docs-create`, `/docs-update`, or `/docs-review` commands (powered by `docs-writer` agent) for creating and maintaining user-facing docs with MarkdownSnippets. All code snippets live in `src/samples/`.
- **Project todos/plans**: Use `/project-todos` skill for tracking work and design documents
- **DDD terminology**: See global CLAUDE.md for DDD documentation guidelines

## Design Source of Truth

The `src/Design/` directory contains the **authoritative reference** for Neatoo's API design:

- **Design.Domain** - Heavily-commented demonstrations of all base classes, factory operations, properties, and rules
- **Design.Infrastructure** - Repository interface examples
- **Design.Tests** - Tests verifying documented patterns

When learning about Neatoo concepts, **read Design.Domain files first**. They contain:
- `DESIGN DECISION` - Why the API works this way
- `DID NOT DO THIS` - Rejected alternatives with explanations
- `GENERATOR BEHAVIOR` - What source generators produce
- `COMMON MISTAKE` - Incorrect usage patterns to avoid

**Key files by topic:**
- Base classes: `Design.Domain/BaseClasses/AllBaseClasses.cs`
- Aggregate patterns: `Design.Domain/Aggregates/OrderAggregate/`
- Factory operations: `Design.Domain/FactoryOperations/`
- Validation rules: `Design.Domain/Rules/`
- Property system: `Design.Domain/PropertySystem/`
- Generator interaction: `Design.Domain/Generators/TwoGeneratorInteraction.cs`

See `src/Design/CLAUDE-DESIGN.md` for detailed Claude Code guidance.
