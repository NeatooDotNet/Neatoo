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
│   ├── ReadModels/            # Plain [Factory] read model, [Fetch] only
│   ├── Commands/              # Static [Execute] command pattern
│   ├── FactoryOperations/     # [Create], [Fetch], [Insert], [Update], [Delete]
│   ├── PropertySystem/        # Partial properties, pause state, MarkReadOnly, LazyLoad
│   ├── Rules/                 # Validation rules and RuleManager
│   ├── Generators/            # Two-generator interaction documentation
│   ├── ErrorHandling/         # Exceptions vs validation
│   ├── DI/                    # Service registration and contracts
│   └── CommonGotchas.cs       # Gotchas with tests in Design.Tests/GotchaTests
├── Design.Infrastructure/     # Empty; repository interfaces live in Design.Domain
└── Design.Tests/              # Test coverage for all patterns
    ├── BaseClassTests/
    ├── AggregateTests/
    ├── FactoryTests/
    ├── GotchaTests/
    ├── PropertyTests/
    ├── ReadModelTests/
    └── RuleTests/
```

## Key Concepts

### Four Base Classes

| Base Class | Purpose |
|------------|---------|
| `EntityBase<T>` | Persistent entities with full CRUD lifecycle (IsNew, IsModified, IsSavable) |
| `ValidateBase<T>` | Objects that need validation rules but have no persistence lifecycle of their own (IsValid, IsBusy) |
| `EntityListBase<I>` | Collections of child entities with DeletedList for removal tracking |
| `ValidateListBase<I>` | Collections of `ValidateBase` objects with validation aggregation |

A read model is not on this list: it is a plain `[Factory]` class with `[Fetch]` only (`Design.Domain/ReadModels/`).

### Factory Operations

| Attribute | Purpose |
|-----------|---------|
| `[Create]` | Initialize new object (typically runs locally) |
| `[Fetch]` | Load existing data from persistence |
| `[Insert]` | Persist new object (called by Save when IsNew=true) |
| `[Update]` | Persist changes (called by Save when the object is neither new nor deleted) |
| `[Delete]` | Remove from persistence (called by Save when IsDeleted=true and the object is not new) |
| `[Execute]` | Run static command operations; add `[Remote]` when the command needs the server |

### The [Remote] Boundary

`[Remote]` marks a client entry point: a client call to the operation crosses to the server. Key rules:

- Only the root operations the client calls carry it; child operations are `internal` and never `[Remote]`
- Once execution crosses to the server, it stays there
- Constructor `[Service]` injection = resolved on both tiers, so it must be registered on both
- Method `[Service]` injection = resolved on the tier where the operation runs (the server, for `[Remote]` and `internal` operations)

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
// WRONG: await employee.Addresses[0].Save();  // Does not compile: the child interface has no Save()
// RIGHT: await employee.Save();  // The root's save reaches each child's own Insert/Update
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
