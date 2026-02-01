# Claude Code Design Guidance

This document provides specific guidance for Claude Code when working with Neatoo framework code. The `src/Design/` projects are your primary reference for understanding Neatoo's API design.

## How to Use Design Projects

### Reading Order

When trying to understand a Neatoo concept:

1. **Start with Design.Domain** - The heavily-commented source files explain design rationale
2. **Check Design.Tests** - Tests show expected behavior and usage patterns
3. **Reference main source** - For implementation details not in Design

### Key Files by Topic

| Topic | Primary File | Tests |
|-------|-------------|-------|
| Base class selection | `BaseClasses/AllBaseClasses.cs` | `BaseClassTests/*` |
| Factory operations | `FactoryOperations/*.cs` | `FactoryTests/*` |
| Aggregate patterns | `Aggregates/OrderAggregate/*` | `AggregateTests/*` |
| Property system | `PropertySystem/*.cs` | `PropertyTests/*` |
| Validation rules | `Rules/*.cs` | `RuleTests/*` |
| Generator behavior | `Generators/TwoGeneratorInteraction.cs` | N/A |
| DI setup | `DI/*.cs` | N/A |

## Critical Patterns to Understand

### 1. The Four Base Classes

**EntityBase<T>** - Use for persistent entities:
- Has IsNew, IsModified, IsDeleted, IsSavable
- Save() routes to Insert/Update/Delete based on state
- Child entities have IsChild=true and cannot save independently

**ValidateBase<T>** - Use for value objects and validation-only:
- Has IsValid, IsSelfValid, IsBusy
- No persistence state tracking
- RuleManager for validation

**EntityListBase<I>** - Use for child entity collections:
- DeletedList tracks removed items for persistence
- IsModified cascades from children
- Enforces aggregate boundaries

**ValidateListBase<I>** - Use for value object collections:
- IsValid aggregates from all children
- No DeletedList (no persistence tracking)

### 2. Constructor vs Method [Service] Injection

This distinction is critical for understanding client-server boundaries:

```csharp
// Constructor [Service] - Available on BOTH client AND server
public Employee([Service] IValidateBaseServices<Employee> services) : base(services)

// Method [Service] - Only available on SERVER
[Remote]
[Fetch]
public void Fetch(int id, [Service] IEmployeeRepository repository)
```

- Constructor services are in the DI container for both client and server
- Method services are only registered on the server
- If a non-[Remote] method has method-injected services and is called on client, you get a DI exception

### 3. DeletedList Lifecycle

When working with EntityListBase, understand this lifecycle:

```
Item removed from list:
  ├── If IsNew=true: Discarded (never persisted)
  └── If IsNew=false:
      ├── MarkDeleted() called
      ├── Added to DeletedList
      └── ContainingList reference preserved

During aggregate Save():
  ├── Each DeletedList item: [Delete] called
  └── FactoryComplete(Update):
      ├── DeletedList.Clear()
      └── ContainingList references cleared

Intra-aggregate move (item from ListA to ListB):
  ├── ListA.Remove(item) → goes to ListA.DeletedList
  ├── ListB.Add(item) → removed from DeletedList, UnDeleted
  └── Result: No persistence delete needed
```

### 4. Factory Operations and PauseAllActions

During factory operations, rules are paused:

```csharp
[Create]
public void Create()
{
    // Rules don't fire during Create - IsPaused=true
    Name = "Default";  // No rule triggered
}
// After Create: IsPaused=false, rules eligible

// To load data without triggering rules or modification:
[Fetch]
public void Fetch(int id, [Service] IRepository repo)
{
    using (PauseAllActions())  // For EntityBase - already paused during Fetch
    {
        this["Name"].LoadValue(repo.Get(id).Name);  // No IsModified=true
    }
}
```

### 5. Two-Generator Interaction

Neatoo uses two source generators:

**Neatoo.BaseGenerator:**
- Detects partial properties on EntityBase/ValidateBase
- Generates property backing fields (IEntityProperty<T>)
- Generates getter/setter implementations
- Generates InitializePropertyBackingFields() override

**RemoteFactory:**
- Detects [Factory] attribute on classes
- Detects [Create], [Fetch], etc. methods
- Generates factory interfaces (IEmployeeFactory)
- Generates factory implementations
- For [Remote] methods: generates HTTP client proxies

Both generators run independently during compilation.

## Common Implementation Tasks

### Adding a New Entity

1. Create class inheriting from `EntityBase<T>`
2. Add `[Factory]` attribute
3. Add partial properties for data
4. Add constructor with `IEntityBaseServices<T>` parameter
5. Add validation rules in constructor
6. Add factory methods: `[Create]`, `[Remote][Fetch]`, `[Remote][Insert]`, `[Remote][Update]`, `[Remote][Delete]`

### Adding a Child Entity

Same as above, but:
- The entity will have `IsChild=true` when added to an EntityListBase
- Do NOT add `[Remote]` to Insert/Update/Delete - parent handles persistence
- Child's Save() will throw because `IsSavable=false` when `IsChild=true`

### Adding Validation Rules

**Fluent API (simple cases):**
```csharp
RuleManager.AddValidation(
    t => string.IsNullOrEmpty(t.Name) ? "Name required" : "",
    t => t.Name);  // Trigger property

RuleManager.AddAction(
    t => t.Total = t.Quantity * t.Price,
    t => t.Quantity,
    t => t.Price);  // Multiple triggers
```

**Class-based rules (complex cases):**
```csharp
public class MyRule : AsyncRuleBase<MyEntity>
{
    public MyRule() : base(t => t.Name) { }  // Trigger in constructor

    protected override Task<IRuleMessages> Execute(MyEntity target, CancellationToken? token)
    {
        if (string.IsNullOrEmpty(target.Name))
            return Task.FromResult<IRuleMessages>(
                (nameof(MyEntity.Name), "Name required").AsRuleMessages());
        return Task.FromResult<IRuleMessages>(None);  // 'None' inherited from base
    }
}
```

### Testing Patterns

Design.Tests shows the testing approach:

```csharp
[TestClass]
public class MyTests
{
    private IServiceScope _scope = null!;
    private IMyEntityFactory _factory = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _scope = DesignTestServices.GetScope();
        _factory = _scope.GetRequiredService<IMyEntityFactory>();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _scope.Dispose();
    }

    [TestMethod]
    public async Task PropertyChange_TriggersRule()
    {
        var entity = _factory.Create();
        entity.Name = "Test";
        await entity.WaitForTasks();  // Wait for async rules
        Assert.IsTrue(entity.IsValid);
    }
}
```

## What NOT to Do

### Do NOT Mock Neatoo Classes

From main CLAUDE.md:
> When writing unit tests for Neatoo:
> 1. Only mock external dependencies
> 2. Use real Neatoo classes - "new up" Neatoo dependencies
> 3. Inherit from Neatoo base classes, don't implement interfaces manually

### Do NOT Call Protected Methods Directly

Methods like `MarkOld()`, `MarkUnmodified()` are protected for a reason. Use factory operations that call them appropriately:
- `Create()` → `MarkNew()`
- `Fetch()` → `MarkOld()`, `MarkUnmodified()`
- `Save()` after Insert → `MarkOld()`, `MarkUnmodified()`
- `Save()` after Update → `MarkUnmodified()`

### Do NOT Use Reflection

From main CLAUDE.md:
> Do NOT use reflection in code without reviewing and getting approval first.
> The goal is to have no reflection, even in tests.

### Do NOT Skip WaitForTasks()

After property changes that trigger async rules:
```csharp
entity.Name = "Test";
await entity.WaitForTasks();  // REQUIRED before checking IsValid
Assert.IsTrue(entity.IsValid);
```

## Design Project Maintenance

When updating Design projects:

1. **Update source files first** - Design.Domain is the source of truth
2. **Update tests** - Design.Tests must pass
3. **Update comments** - Keep DESIGN DECISION, GENERATOR BEHAVIOR, etc. accurate
4. **Update this document** if guidance changes

## Quick Reference: State Properties

| Property | Base Class | Meaning |
|----------|-----------|---------|
| `IsNew` | EntityBase | Not yet persisted |
| `IsModified` | EntityBase | Has unsaved changes (includes children) |
| `IsSelfModified` | EntityBase | This object has changes (excludes children) |
| `IsDeleted` | EntityBase | Marked for deletion |
| `IsSavable` | EntityBase | IsModified && IsValid && !IsBusy && !IsChild |
| `IsChild` | EntityBase | Part of parent aggregate |
| `IsValid` | ValidateBase | All rules pass (includes children) |
| `IsSelfValid` | ValidateBase | This object's rules pass (excludes children) |
| `IsBusy` | ValidateBase | Async operations pending |
| `IsPaused` | ValidateBase | Events/rules suppressed |
| `Root` | EntityBase/List | Aggregate root reference |
| `Parent` | All bases | Parent in object graph |
