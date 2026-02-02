# Design Source of Truth

This directory contains the **Design Source of Truth** for the Neatoo framework - a set of C# projects that serve as the authoritative reference for Neatoo's API design.

## Purpose

The Design projects exist to:

1. **Document Every API** - Demonstrate all base classes, factory operations, state properties, validation rules, and aggregate patterns
2. **Explain Design Decisions** - Provide rationale for why the API is designed the way it is
3. **Show Rejected Alternatives** - Document what was NOT done and why
4. **Illustrate Generator Behavior** - Explain what code the source generators produce
5. **Serve as Test Coverage** - Verify the documented patterns work correctly

## Who Should Use This

| Audience | Usage |
|----------|-------|
| **Claude Code** | Primary consumer - understands Neatoo API design through these projects |
| **Framework Developers** | Reference when making API changes (update Design first) |
| **New Contributors** | Learn the framework's design philosophy |

## Directory Structure

```
src/Design/
├── Design.sln                  # Solution file
├── README.md                   # This file
├── CLAUDE-DESIGN.md           # Claude Code specific guidance
├── Design.Domain/             # API demonstrations with extensive comments
│   ├── BaseClasses/           # All four base classes side-by-side
│   ├── Aggregates/            # Complete aggregate root pattern
│   ├── Entities/              # EntityBase examples
│   ├── ValueObjects/          # ValidateBase for value objects
│   ├── Commands/              # Static [Execute] command pattern
│   ├── FactoryOperations/     # [Create], [Fetch], [Insert], [Update], [Delete]
│   ├── PropertySystem/        # Partial properties, Getter/Setter, LoadValue
│   ├── Rules/                 # Validation rules and RuleManager
│   ├── Generators/            # Two-generator interaction documentation
│   └── DI/                    # Service registration and contracts
├── Design.Infrastructure/     # Repository interface examples
└── Design.Tests/              # Test coverage for all patterns
    ├── BaseClassTests/
    ├── AggregateTests/
    ├── FactoryTests/
    ├── PropertyTests/
    └── RuleTests/
```

## Key Concepts

### Four Base Classes

| Base Class | Purpose |
|------------|---------|
| `EntityBase<T>` | Persistent entities with full CRUD lifecycle (IsNew, IsModified, IsSavable) |
| `ValidateBase<T>` | Value objects, read models, validation-only objects (IsValid, IsBusy) |
| `EntityListBase<I>` | Collections of child entities with DeletedList for removal tracking |
| `ValidateListBase<I>` | Collections of read models/value objects with validation aggregation |

### Factory Operations

| Attribute | Purpose |
|-----------|---------|
| `[Create]` | Initialize new object (typically runs locally) |
| `[Fetch]` | Load existing data from persistence |
| `[Insert]` | Persist new object (called by Save when IsNew=true) |
| `[Update]` | Persist changes (called by Save when IsModified=true) |
| `[Delete]` | Remove from persistence (called by Save when IsDeleted=true) |
| `[Execute]` | Run static command operations |

### The [Remote] Boundary

`[Remote]` marks methods that must execute on the server. Key rules:

- Once execution crosses to the server, it stays there
- Constructor `[Service]` injection = available on both client and server
- Method `[Service]` injection = server-only (common case)

## Comment Standards

The Design projects use four types of documentation comments:

### DESIGN DECISION
Explains why the API is designed this way:
```csharp
// DESIGN DECISION: [Remote] is applied to methods, not classes.
// - Entry point marking: Client knows which operations require server
// - Granular control: [Create] can be local-only
```

### DID NOT DO THIS
Documents rejected alternatives:
```csharp
// DID NOT DO THIS: Mark entire class as [Remote]
// REJECTED PATTERN:
//   [Remote]
//   public class Employee : EntityBase<Employee> { ... }
```

### GENERATOR BEHAVIOR
Shows what source generators produce:
```csharp
// GENERATOR BEHAVIOR: For this partial property:
//   public partial string? Name { get; set; }
// Neatoo.BaseGenerator produces backing field and implementation
```

### COMMON MISTAKE
Warns about incorrect usage:
```csharp
// COMMON MISTAKE: Calling Save() on child entities
// WRONG: await employee.Addresses[0].Save();  // Throws
// RIGHT: await employee.Save();  // Parent saves children
```

## Evolution Process

When the Neatoo API changes:

1. **Update Design.* projects first** - This is the source of truth
2. **Add "was/now" comments** for changed behavior
3. **Update main codebase** to implement the change
4. **Update user documentation** last

## Building and Testing

```bash
# Build Design projects
dotnet build src/Design/Design.sln

# Run Design tests
dotnet test src/Design/Design.Tests/Design.Tests.csproj

# Run all Neatoo tests (includes Design.Tests)
dotnet test src/Neatoo.sln
```

## Related Resources

- Main CLAUDE.md - Framework guidelines for Claude Code
- src/Examples/ - User-facing sample applications
- src/Neatoo/ - Framework source code
