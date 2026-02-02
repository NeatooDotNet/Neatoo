
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
- `[Create]` - Initialize new object
- `[Fetch]` - Load existing data
- `[Insert]` - Persist new object
- `[Update]` - Persist changes
- `[Delete]` - Remove object
- `[Execute]` - Run command

### Understanding [Remote] - Client-to-Server Boundary

**Core concept:** `[Remote]` marks entry points from the client to the server. Once execution crosses to the server, it stays there—subsequent method calls don't need `[Remote]`.

**Constructor vs Method Injection:**
- **Constructor injection** (`[Service]` on constructor): Services available on both client and server
- **Method injection** (`[Service]` on method parameters): Server-only services—the common case for most factory methods

**When to use `[Remote]`:**
- Factory methods that are entry points from the client
- Typically aggregate root operations (Create, Fetch, Save)

**When `[Remote]` is NOT needed (the common case):**
- Methods called from server-side code (most methods with method-injected services)
- Child entity operations within an aggregate
- Any method invoked after already crossing to the server

**Entity duality:** An entity can be an aggregate root in one object graph and a child in another. The same class may have `[Remote]` methods for aggregate root scenarios while other methods are server-only.

**Runtime enforcement:** Non-`[Remote]` methods are generated for client assemblies but result in "not-registered" DI exceptions if called—server-only services aren't in the client container.

**Blazor WASM best practice:** Isolate EF Core in a separate infrastructure project and use `PrivateAssets="all"` on the project reference. See the Person example (`src/Examples/Person/`):

```xml
<!-- Infrastructure.csproj - contains EF Core -->
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="..." />

<!-- Domain.csproj - references Infrastructure privately -->
<ProjectReference Include="..\Infrastructure\Infrastructure.csproj" PrivateAssets="all" />

<!-- Server.csproj - explicitly references both -->
<ProjectReference Include="..\Domain\Domain.csproj" />
<ProjectReference Include="..\Infrastructure\Infrastructure.csproj" />

<!-- Client.csproj - only references Domain, never sees Infrastructure -->
<ProjectReference Include="..\Domain\Domain.csproj" />
```

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

- **Framework documentation**: Use `/csharp-docs` skill for creating user-facing docs with MarkdownSnippets
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
