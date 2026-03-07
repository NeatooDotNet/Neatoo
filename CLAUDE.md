## Neatoo Skill

The neatoo skill source lives in `skills/neatoo/`. Edit it here, then copy to `~/.claude/skills/neatoo/`. Do NOT load the skill via the Skill tool in this repo.

## Commands

```bash
# Build
dotnet build src/Neatoo.sln

# Test (all tests)
dotnet test src/Neatoo.sln

# Test (specific project)
dotnet test src/Neatoo.UnitTest/Neatoo.UnitTest.csproj
dotnet test src/Design/Design.Tests/Design.Tests.csproj
```

The Neatoo Solution is at src/Neatoo.sln

## Project Map

| Project | Purpose |
|---------|---------|
| `Neatoo` | Core framework library (EntityBase, ValidateBase, rules, properties) |
| `Neatoo.BaseGenerator` | Roslyn source generator for partial properties and backing fields |
| `Neatoo.BaseGenerator.Tests` | Tests for the source generator |
| `Neatoo.Analyzers` | Roslyn analyzers for compile-time Neatoo pattern validation |
| `Neatoo.CodeFixes` | Code fixes paired with analyzers |
| `Neatoo.Blazor.MudNeatoo` | MudBlazor integration for Neatoo entities |
| `Neatoo.Console` | Console app for testing/debugging |
| `Neatoo.UnitTest` | Main test project (Unit/, Integration/) |
| `Neatoo.UnitTest.Demo` | Demo tests |
| `samples` | Code samples for documentation (MarkdownSnippets) |
| `Design.Domain` | Authoritative API design reference (heavily commented) |
| `Design.Infrastructure` | Repository interface examples |
| `Design.Tests` | Tests verifying design patterns |
| `Examples/Person/*` | Full example application (App, DomainModel, Ef, Server) |

## Central Pillar: Interface-First Design

**Every entity/list gets a matched public interface. Concretes are `internal`. All references use interfaces, never concretes.** This is how `IEntityRoot` vs `IEntityBase` separation works -- without it, `IsSavable` on child entities silently returns `false` (real zTreatment bug).

### The Rules

1. Every entity class gets a matched public interface (`IOrder`, `IOrderItem`, `IOrderItemList`)
2. Concrete classes are `internal`
3. All references use interfaces -- properties, parameters, list type parameters
4. Root interfaces extend `IEntityRoot` (exposes `IsSavable`, `Save()`)
5. Child interfaces extend `IEntityBase` (no `IsSavable`, no `Save()`)
6. List interfaces extend `IEntityListBase<IChild>` -- parameterized on child interface
7. ValidateBase entities follow the same pattern

```csharp
public interface IOrder : IEntityRoot { IOrderItemList? Items { get; } }
public interface IOrderItem : IEntityBase { string ProductName { get; set; } }
public interface IOrderItemList : IEntityListBase<IOrderItem> { }

internal partial class Order : EntityBase<Order>, IOrder { ... }
internal partial class OrderItem : EntityBase<OrderItem>, IOrderItem { ... }
internal class OrderItemList : EntityListBase<IOrderItem>, IOrderItemList { ... }

// WRONG: public concrete, concrete type in property, list on concrete, factory taking concrete
// public class Order : EntityBase<Order> { ... }         -- must be internal
// public partial OrderItemList? Items { get; set; }      -- use IOrderItemList
// EntityListBase<OrderItem>                              -- use IOrderItem
// void Insert(Order parent, ...) { ... }                 -- use IOrder
```

## Neatoo Terminology

### Base Classes
- `EntityBase<T>` - Persistent entities with full CRUD lifecycle
- `ValidateBase<T>` - Value objects, read models, validation-only objects
- `EntityListBase<I>` - Collections of child entities within an aggregate
- `ValidateListBase<I>` - Collections of read models
- Static classes with `[Factory]` and `[Execute]` - Commands

### Entity Interfaces: Root vs Child
- `IEntityRoot : IEntityBase` - Aggregate root interface. Adds `IsSavable` and `Save()`. User-defined root entity interfaces extend this.
- `IEntityBase` - Child entity interface. No `IsSavable`, no `Save()`. User-defined child entity interfaces extend this.

The user signals root vs child by choosing which interface their entity interface extends. This is explicit -- no attributes, no inference, no RemoteFactory involvement. `EntityBase<T>` implements both `IEntityBase` and `IEntityRoot`, but entity classes should be `internal` with only the public interface exposed. The interface controls what consumers can access.

```csharp
// Aggregate root -- exposes IsSavable and Save()
public interface IOrder : IEntityRoot { ... }

// Child entity -- no IsSavable, no Save()
public interface IOrderLine : IEntityBase { ... }
```

**Why this exists:** `IsSavable` on `EntityBase` includes a `!IsChild` check, making it always false for child entities. Developers naturally used `IsSavable` in save cascade logic to check whether children need persisting -- but it silently returned false, skipping saves (real bug in zTreatment). The fix is not to make `IsSavable` work on children -- it is to remove it from the child interface entirely. Child entity factory methods (`[Insert]`/`[Update]`) have signatures that outside consumers cannot fulfill (they often need the parent entity or parent ID), and entity classes are `internal`, so external callers should not be able to save children at all.

### State Properties
- `IsModified` - True when object has unsaved changes
- `IsSelfModified` - True when this object (not children) has changes
- `IsNew` - True when object hasn't been persisted yet
- `IsValid` - True when all validation rules pass
- `IsSelfValid` - True when this object's rules pass (not children)
- `IsSavable` - True when entity can be saved (IsModified && IsValid && !IsBusy && !IsChild). **Only on `IEntityRoot`** -- not on `IEntityBase` or `IEntityListBase`. Child entities and entity lists never expose this property through their interfaces.

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
- Root vs child interfaces: `Design.Domain/Aggregates/OrderAggregate/IOrderInterfaces.cs`
- Aggregate patterns: `Design.Domain/Aggregates/OrderAggregate/`
- Factory operations: `Design.Domain/FactoryOperations/`
- Validation rules: `Design.Domain/Rules/`
- Property system: `Design.Domain/PropertySystem/`
- Generator interaction: `Design.Domain/Generators/TwoGeneratorInteraction.cs`
- Commands: `Design.Domain/Commands/ApproveEmployee.cs`
- DI/service registration: `Design.Domain/DI/`
- Error handling: `Design.Domain/ErrorHandling/`
- Common gotchas: `Design.Domain/CommonGotchas.cs`
- Entities (standalone): `Design.Domain/Entities/`
- Value objects: `Design.Domain/ValueObjects/`

See `src/Design/CLAUDE-DESIGN.md` for detailed Claude Code guidance.
