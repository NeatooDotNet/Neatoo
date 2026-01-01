# H2: Document Async Validation Patterns

**Priority:** High
**Category:** Documentation
**Effort:** Medium
**Status:** Not Started

---

## Problem Statement

Async validation is one of Neatoo's strongest features, but developers often struggle with:

1. When to use `AsyncRuleBase<T>` vs `RuleBase<T>`
2. How to properly check `IsModified` before running expensive validations
3. Understanding the interaction between `IsBusy` and validation
4. Handling concurrent async validations
5. Patterns for database-dependent validation

The current documentation doesn't adequately cover these scenarios.

---

## Documentation to Create

### 1. Async Validation Overview

- When to use async rules
- The lifecycle of an async rule execution
- How `IsBusy` propagates through the object graph

### 2. Pattern: Database Uniqueness Check

Complete example showing:
- Creating a Command for the database query
- Creating an `AsyncRuleBase<T>` that uses the command
- Checking `IsModified` to skip unchanged values
- Handling the exclude-self scenario for updates

```csharp
// Full working example
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
        // Skip if not modified (optimization)
        if (!target[nameof(target.Email)].IsModified)
            return None;

        // Skip empty values (let [Required] handle that)
        if (string.IsNullOrEmpty(target.Email))
            return None;

        // Exclude self for updates
        var excludeId = target.IsNew ? null : (Guid?)target.Id;

        if (!await _isUnique(target.Email, excludeId))
            return (nameof(target.Email), "Email already in use").AsRuleMessages();

        return None;
    }
}
```

### 3. Pattern: Cross-Field Async Validation

Example: Validating that a date range doesn't overlap with existing records

### 4. Pattern: External API Validation

Example: Validating an address against a geocoding service

### 5. Pattern: Debouncing Fast Input

Example: Not triggering validation on every keystroke

### 6. Anti-Patterns to Avoid

- Putting async validation in factory methods (covered in CLAUDE.md)
- Not checking `IsModified`
- Not handling cancellation tokens
- Blocking on async operations

---

## Implementation Tasks

- [ ] Create `docs/async-validation.md` main document
- [ ] Write section on when to use async vs sync rules
- [ ] Write uniqueness check pattern with full example
- [ ] Write cross-field async validation pattern
- [ ] Write external API validation pattern
- [ ] Write debouncing pattern
- [ ] Document anti-patterns section
- [ ] Add code samples to `samples/` folder
- [ ] Link from main docs and quick-start
- [ ] Review and align with CLAUDE.md guidance

---

## Success Criteria

1. Developer can implement a uniqueness check by following the documentation
2. Common mistakes are clearly documented as anti-patterns
3. All examples compile and run
4. Integration with Blazor UI (showing busy state) is demonstrated

---

## Files to Create

| File | Description |
|------|-------------|
| `docs/async-validation.md` | Main async validation guide |
| `docs/patterns/uniqueness-check.md` | Detailed uniqueness pattern |
| `docs/patterns/external-api-validation.md` | API validation pattern |
| `samples/AsyncValidation/` | Working sample project |
