# C2: Create Beginner-Friendly Tutorials

**Priority:** Critical
**Category:** Documentation
**Effort:** Medium
**Status:** Not Started

---

## Problem Statement

Neatoo has a steep learning curve with multiple concepts to master simultaneously:

1. Base class hierarchy (`Base<T>` -> `ValidateBase<T>` -> `EntityBase<T>`)
2. Property system (`Getter<T>()` / `Setter(value)` pattern)
3. Rule system (`RuleBase<T>`, `AsyncRuleBase<T>`, trigger properties)
4. Factory pattern (`[Create]`, `[Fetch]`, `[Insert]`, `[Update]`, `[Delete]`)
5. Meta-properties (IsBusy, IsValid, IsSavable, IsModified)
6. Services injection (`IEntityBaseServices<T>`, `IValidateBaseServices<T>`)

The current quick-start documentation jumps into too many concepts at once, creating an adoption barrier.

---

## Proposed Solution

Create a progressive tutorial series that introduces concepts incrementally, with working examples at each stage.

---

## Tutorial Outline

### Tutorial 1: Hello Neatoo (30 min)

**Goal:** Create a minimal working entity with one property

**Concepts Introduced:**
- `[Factory]` attribute
- `EntityBase<T>` inheritance
- Partial properties with `Getter<T>()`/`Setter()`
- Basic DI setup

```csharp
[Factory]
public partial class Greeting : EntityBase<Greeting>
{
    public Greeting(IEntityBaseServices<Greeting> services) : base(services) { }

    public partial string? Message { get; set; }
}
```

### Tutorial 2: Adding Validation (30 min)

**Goal:** Add validation rules to the entity

**Concepts Introduced:**
- Data annotation attributes (`[Required]`, `[StringLength]`)
- `IsValid` meta-property
- `BrokenRules` collection
- UI binding to validation state

### Tutorial 3: Custom Rules (45 min)

**Goal:** Create custom synchronous validation rules

**Concepts Introduced:**
- `RuleBase<T>` class
- `AddTriggerProperties()`
- `Execute()` method
- `RuleManager.AddRule()`
- Rule messages

### Tutorial 4: Async Validation (45 min)

**Goal:** Add database-dependent validation (e.g., uniqueness check)

**Concepts Introduced:**
- `AsyncRuleBase<T>` class
- `IsBusy` meta-property
- Commands with `[Execute]`
- Waiting for async validation

### Tutorial 5: Persistence (1 hour)

**Goal:** Save and load entities from a database

**Concepts Introduced:**
- `[Create]`, `[Fetch]`, `[Insert]`, `[Update]`, `[Delete]` methods
- `[Service]` attribute for dependency injection
- `RunRules()` and `IsSavable`
- `IsNew`, `IsModified`, `IsDeleted` states

### Tutorial 6: Parent-Child Relationships (1 hour)

**Goal:** Create an aggregate with child collections

**Concepts Introduced:**
- `EntityListBase<T, I>` for child collections
- `Parent` property
- `IsChild` flag
- Cascading validation and modification state

### Tutorial 7: Blazor Integration (1 hour)

**Goal:** Build a complete CRUD form with Blazor

**Concepts Introduced:**
- Binding to meta-properties
- Displaying validation errors
- Busy indicators during async validation
- Save button enablement with `IsSavable`

---

## Implementation Tasks

- [ ] Create `docs/tutorials/` folder structure
- [ ] Write Tutorial 1: Hello Neatoo
- [ ] Create working sample project for Tutorial 1
- [ ] Write Tutorial 2: Adding Validation
- [ ] Create working sample project for Tutorial 2
- [ ] Write Tutorial 3: Custom Rules
- [ ] Create working sample project for Tutorial 3
- [ ] Write Tutorial 4: Async Validation
- [ ] Create working sample project for Tutorial 4
- [ ] Write Tutorial 5: Persistence
- [ ] Create working sample project for Tutorial 5
- [ ] Write Tutorial 6: Parent-Child Relationships
- [ ] Create working sample project for Tutorial 6
- [ ] Write Tutorial 7: Blazor Integration
- [ ] Create working sample project for Tutorial 7
- [ ] Create tutorial index/overview page
- [ ] Add links from README and quick-start docs

---

## Sample Project Structure

```
samples/
  Tutorials/
    01-HelloNeatoo/
      HelloNeatoo.csproj
      Greeting.cs
      Program.cs
    02-AddingValidation/
      ...
    03-CustomRules/
      ...
    ...
```

---

## Success Criteria

1. A developer new to Neatoo can complete Tutorial 1 in under 30 minutes
2. Each tutorial builds on the previous without requiring re-reading
3. All sample code compiles and runs without modification
4. Tutorials cover the most common use cases (80% of scenarios)

---

## Files to Create

| File | Description |
|------|-------------|
| `docs/tutorials/index.md` | Tutorial series overview |
| `docs/tutorials/01-hello-neatoo.md` | First tutorial |
| `docs/tutorials/02-adding-validation.md` | Validation tutorial |
| `docs/tutorials/03-custom-rules.md` | Custom rules tutorial |
| `docs/tutorials/04-async-validation.md` | Async validation tutorial |
| `docs/tutorials/05-persistence.md` | Persistence tutorial |
| `docs/tutorials/06-parent-child.md` | Aggregates tutorial |
| `docs/tutorials/07-blazor-integration.md` | Blazor tutorial |
| `samples/Tutorials/**` | Working sample projects |
