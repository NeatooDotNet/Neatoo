# Field-Level ReadOnly Authorization

**Date:** 2026-04-06
**Related Todo:** [Field-Level ReadOnly Authorization](../todos/field-level-readonly-authorization.md)
**Status:** Complete
**Last Updated:** 2026-04-06

<!-- Valid status values (do not render in plan):
Draft | Under Review (Architect) | Concerns Raised (Architect) | Ready for Implementation |
In Progress | Awaiting Code Review | Code Review Concerns | Awaiting Verification | Sent Back |
Requirements Documented | Documentation Complete | Complete
-->

---

## Overview

Add a `MarkReadOnly()` method to `IValidateProperty` that allows developers to programmatically mark individual properties as permanently read-only during factory methods. This enables field-level authorization: during `[Fetch]`, if the current user lacks permission to edit a field, the developer calls `this["FieldName"].MarkReadOnly()` to lock it down.

The existing infrastructure already handles everything downstream:
- `SetValue()` throws `PropertyReadOnlyException` when `IsReadOnly` is true
- `SetPrivateValue()` bypasses `IsReadOnly` (rules still work)
- `IsReadOnly` is serialized through JSON (client receives the flag)
- All MudNeatoo components bind `ReadOnly="@EntityProperty.IsReadOnly"`

The only missing piece is a public method to set `IsReadOnly = true` at runtime.

---

## Skills

- `~/.claude/skills/neatoo/SKILL.md` — Neatoo domain model patterns, property system, factory operations
- `~/.claude/skills/project-todos/SKILL.md` — Workflow management

---

## Business Rules (Testable Assertions)

1. WHEN `MarkReadOnly()` is called on a property, THEN `IsReadOnly` RETURNS `true` — NEW
2. WHEN `MarkReadOnly()` has been called on a property, THEN `SetValue()` THROWS `PropertyReadOnlyException` — Existing behavior (SetValue already checks IsReadOnly)
3. WHEN `MarkReadOnly()` has been called on a property, THEN `SetPrivateValue()` SUCCEEDS — Existing behavior (SetPrivateValue bypasses IsReadOnly)
4. WHEN `MarkReadOnly()` has been called on a property, THEN `LoadValue()` SUCCEEDS — Existing behavior (LoadValue does not check IsReadOnly)
5. WHEN `MarkReadOnly()` has been called, THEN subsequent calls to set `IsReadOnly = false` have NO EFFECT (one-and-done) — NEW
6. WHEN a property has `IsReadOnly = false` (default), THEN `MarkReadOnly()` sets it to `true` — NEW
7. WHEN a property has `IsReadOnly = true` from `private set`, THEN `MarkReadOnly()` is a no-op (already read-only) — NEW
8. WHEN `MarkReadOnly()` is called during a `[Fetch]` and the entity is serialized to the client, THEN the client-side property has `IsReadOnly = true` — Existing serialization behavior
9. WHEN `MarkReadOnly()` is called, THEN `PropertyChanged` fires for `IsReadOnly` (so MudNeatoo components re-render) — NEW

### Test Scenarios

| # | Scenario | Inputs / State | Rule(s) | Expected Result |
|---|----------|---------------|---------|-----------------|
| 1 | MarkReadOnly sets flag | Property with `IsReadOnly=false`, call `MarkReadOnly()` | 1, 6 | `IsReadOnly` is `true` |
| 2 | MarkReadOnly prevents SetValue | Property marked read-only, call `SetValue("x")` | 2 | Throws `PropertyReadOnlyException` |
| 3 | MarkReadOnly allows SetPrivateValue | Property marked read-only, call `SetPrivateValue("x")` | 3 | Value is set, no exception |
| 4 | MarkReadOnly allows LoadValue | Property marked read-only, call `LoadValue("x")` | 4 | Value is set, no exception |
| 5 | MarkReadOnly is permanent | Property marked read-only, attempt to set `IsReadOnly = false` | 5 | `IsReadOnly` remains `true` |
| 6 | MarkReadOnly on already-readonly | Property with `private set` (already `IsReadOnly=true`), call `MarkReadOnly()` | 7 | No error, still `IsReadOnly = true` |
| 7 | MarkReadOnly via entity indexer | Entity with public-set property, call `this["Name"].MarkReadOnly()` | 1, 6 | Property is read-only, setter throws |
| 8 | MarkReadOnly fires PropertyChanged | Call `MarkReadOnly()` on writable property | 9 | `PropertyChanged` fired for "IsReadOnly" |
| 9 | MarkReadOnly in Fetch serializes | Fetch marks field read-only, entity serialized/deserialized | 8 | Deserialized property has `IsReadOnly = true` |

---

## Approach

Minimal, surgical change:

1. Add `void MarkReadOnly()` to the `IValidateProperty` interface
2. Implement in `ValidateProperty<T>` — set `IsReadOnly = true`, fire `PropertyChanged("IsReadOnly")`
3. Make the `IsReadOnly` setter enforce one-and-done: once `true`, ignore attempts to set `false`
4. Add Design.Domain documentation demonstrating the authorization pattern
5. Add tests in both Design.Tests and Neatoo.UnitTest

---

## Domain Model Behavioral Design

N/A — This is a framework-level feature, not a domain model change. No computed properties, visibility flags, or reactive rules needed.

---

## Design

### Interface Change

```csharp
// IValidateProperty - add method
public interface IValidateProperty
{
    // ... existing members ...
    
    /// <summary>
    /// Permanently marks this property as read-only.
    /// Once called, SetValue() will throw PropertyReadOnlyException.
    /// SetPrivateValue() and LoadValue() continue to work.
    /// This cannot be reversed — once read-only, always read-only.
    /// </summary>
    void MarkReadOnly();
}
```

### Implementation Change

```csharp
// ValidateProperty<T> - modify IsReadOnly setter, add MarkReadOnly()
public bool IsReadOnly { get; protected set; } = false;
// becomes:
private bool _isReadOnly = false;
public bool IsReadOnly
{
    get => _isReadOnly;
    protected set
    {
        // One-and-done: once true, cannot be set back to false
        if (_isReadOnly && !value) return;
        _isReadOnly = value;
    }
}

public void MarkReadOnly()
{
    if (!IsReadOnly)
    {
        IsReadOnly = true;
        OnPropertyChanged(nameof(IsReadOnly));
    }
}
```

### JSON Constructor

The existing `JsonConstructor` already accepts `bool isReadOnly` and sets `this.IsReadOnly = isReadOnly`. This continues to work unchanged — the one-and-done protection only prevents setting `true -> false`, and the constructor sets the initial value.

### Serialization

No changes needed. `IsReadOnly` is already serialized/deserialized in:
- `ValidateProperty<T>` constructor (`bool isReadOnly` parameter)
- `NeatooBaseJsonTypeConverter` (reads `"IsReadOnly"` from JSON)

### Design.Domain Documentation

Add a new file `Design.Domain/PropertySystem/FieldLevelAuthorization.cs` demonstrating the pattern with a `[Fetch]` example.

---

## Implementation Steps

1. Modify `IsReadOnly` property in `ValidateProperty<T>` to use a backing field with one-and-done protection
2. Add `void MarkReadOnly()` to `IValidateProperty` interface
3. Implement `MarkReadOnly()` in `ValidateProperty<T>`
4. Add unit tests in `Neatoo.UnitTest/Unit/Core/ValidatePropertyTests.cs`
5. Add Design.Domain documentation file for field-level authorization
6. Add Design.Tests for the authorization pattern
7. Build and run all tests

---

## Acceptance Criteria

- [ ] `IValidateProperty` has `void MarkReadOnly()` method
- [ ] `MarkReadOnly()` sets `IsReadOnly = true` permanently
- [ ] Once `IsReadOnly` is `true`, it cannot be set back to `false`
- [ ] `MarkReadOnly()` fires `PropertyChanged` for "IsReadOnly"
- [ ] Existing `private set` behavior unchanged
- [ ] Existing `SetValue()` / `SetPrivateValue()` / `LoadValue()` behavior unchanged
- [ ] All existing tests pass
- [ ] New tests cover all business rules
- [ ] Design.Domain documents the field-level authorization pattern

---

## Dependencies

None — this builds entirely on existing infrastructure.

---

## Risks / Considerations

- **EntityProperty<T> inherits from ValidateProperty<T>** — `MarkReadOnly()` will work for both entity and validate properties automatically through inheritance. Need to verify EntityProperty doesn't override `IsReadOnly` in a way that conflicts.
- **Thread safety** — `MarkReadOnly()` is expected to be called during factory methods (single-threaded context). The one-and-done semantic (once true, stays true) is inherently thread-safe for the important direction (can't accidentally un-read-only). No locking needed.
- **No "MarkWritable" needed** — The user explicitly wants one-and-done. If a future use case requires toggling, that's a separate feature.
