# Requirements Reviewer — Field-Level ReadOnly Authorization

Last updated: 2026-04-06
Current step: Post-implementation verification complete

## Key Context
- Added `void MarkReadOnly()` to `IValidateProperty` interface and implemented in `ValidateProperty<T>`
- One-and-done semantic: once `IsReadOnly` is `true`, it cannot be reverted to `false`
- `IsReadOnly` changed from auto-property to backing field with protected setter guard
- `MarkReadOnly()` fires `PropertyChanged("IsReadOnly")` only when transitioning from `false` to `true`
- 9 unit tests + 5 integration tests cover all business rules
- All 1789 existing Neatoo tests pass; all 104 Design.Tests pass

## Mistakes to Avoid
None so far.

## User Corrections
None so far.

## Requirements Verification

**Verdict: REQUIREMENTS SATISFIED**

### Requirements Compliance

| # | Requirement | Status | Evidence |
|---|-------------|--------|----------|
| 1 | WHEN property has `private set`, THEN `IsReadOnly` is `true` | Satisfied | `ValidateProperty<T>` constructor (line 22) still sets `IsReadOnly = propertyInfo.IsPrivateSetter`. Existing test `PropertyBasicsTests.PrivateSet_IsReadOnlyTrue()` passes. |
| 2 | WHEN `SetValue()` called on read-only property, THEN `PropertyReadOnlyException` thrown | Satisfied | `ValidateProperty<T>.SetValue()` (line 146-148) guard unchanged. Tests: `MarkReadOnly_SetValueThrows`, `PrivateSet_SetValueThrows`, `MarkReadOnly_SetValueThrowsOnReadOnlyField`. |
| 3 | WHEN public-set property created, THEN `IsReadOnly` is `false` | Satisfied | Backing field `_isReadOnly` initializes to `false` (line 122). Test `PrivateSet_PublicPropertyIsReadOnlyFalse()` passes. |
| 4 | `IsReadOnly` only set in constructors (pre-change) | Satisfied | One new mutation point added: `MarkReadOnly()`. This is the intended feature. The one-and-done guard (line 130) ensures no existing code path can revert `true` to `false`. No existing code ever set `IsReadOnly = false` after construction. |
| 5 | LazyLoad properties always `IsReadOnly = true` | Satisfied | `LazyLoadEntityProperty` (line 32) and `LazyLoadValidateProperty` (line 114) still set `IsReadOnly = true` in constructors. One-and-done is compatible: `false->true` and `true->true` both pass the guard. |
| 6 | LazyLoad `SetValue()` bypasses `IsReadOnly` check | Satisfied | No changes to LazyLoad classes. `MarkReadOnly()` on a LazyLoad property is a no-op (already `true`). |
| 7 | JSON deserialization sets `IsReadOnly` from parameter | Satisfied | `NeatooBaseJsonTypeConverter.cs` (lines 270, 294-296, 317-323) reads `"IsReadOnly"` from JSON and passes to constructors. Constructor sets initial value via the setter; `false->true` and `true->true` work; `false->false` works. No path sets `true->false` during deserialization. Test `MarkReadOnly_SerializationRoundTrip`. |
| 8 | MudNeatoo `IsReadOnly` from domain model | Satisfied | `MarkReadOnly()` is called in factory methods (domain-model-owned), consistent with `skills/mudneatoo/SKILL.md` line 426 philosophy. |
| 9 | MudNeatoo components bind `ReadOnly="@EntityProperty.IsReadOnly"` | Satisfied | No changes to MudNeatoo components. `MarkReadOnly()` fires `PropertyChanged("IsReadOnly")` which triggers MudNeatoo re-render. |
| 10 | `IsReadOnly` serialized and survives round-trips | Satisfied | No serialization changes. `IsReadOnly` property still serialized by `System.Text.Json`. Test `MarkReadOnly_SerializationRoundTrip` verifies. |
| 11 | vsCSLA skill-gaps.md gap addressed | Satisfied | `MarkReadOnly()` provides dynamic per-property authorization, directly addressing `docs/vsCSLA/skill-gaps.md` line 144 gap. |
| 12 | `ValidatePropertyTests.IsReadOnly_InheritedFromPropertyInfo` | Satisfied | Test unchanged at line 1114, still passes. |
| 13 | `ValidatePropertyTests.SetValue_WhenReadOnly_ThrowsException` | Satisfied | Test unchanged at line 1127, still passes. |
| 14 | `EntityPropertyTests.JsonConstructor_WithIsReadOnlyTrue_SetsIsReadOnly` | Satisfied | Test unchanged at line 180, still passes. |

### Unintended Side Effects

**State property cascading:** `IsReadOnly` does not cascade through Parent/Root/ContainingList. It is a property-level flag only. No cascading behavior was introduced. No side effects.

**Factory operation lifecycle:** `MarkReadOnly()` does not interact with PauseAllActions/FactoryStart/FactoryComplete. It sets a simple boolean and fires PropertyChanged. No lifecycle impact.

**Serialization round-trip:** `IsReadOnly` was already serialized. The backing field change (`_isReadOnly`) is private; `System.Text.Json` serializes via the public `IsReadOnly` property getter. The `[JsonConstructor]` paths pass `isReadOnly` to the setter. No serialization behavior change.

**Source generator output:** `MarkReadOnly()` is on `IValidateProperty` (consumed by the indexer `this["name"]`). No generated code calls `IsReadOnly` setter directly. The BaseGenerator generates partial property implementations that call `Getter<T>()`/`Setter()` which go through `SetValue()`/`SetPrivateValue()`. No generator impact.

**Rule execution timing:** `MarkReadOnly()` does not trigger rule execution. It fires `PropertyChanged("IsReadOnly")` only, which does not trigger `NeatooPropertyChanged` (rules are triggered by `NeatooPropertyChanged`, not `PropertyChanged`). No rule timing impact.

**Parent-child relationships:** No changes to IsChild, Root, Parent, ContainingList. No impact.

**EntityProperty<T> inheritance:** `EntityProperty<T>` (in `EntityPropertyManager.cs`) extends `ValidateProperty<T>` and does NOT override `IsReadOnly` or add its own `MarkReadOnly()`. The base implementation flows through correctly via inheritance.

### Issues Found

None.
