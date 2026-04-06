# Field-Level ReadOnly Authorization

**Status:** Complete
**Priority:** High
**Created:** 2026-04-06
**Last Updated:** 2026-04-06

---

## Problem

There is no way to programmatically mark individual properties as read-only at runtime based on user authorization. The current `IsReadOnly` flag is only set at construction time from `IsPrivateSetter` — a compile-time decision. Developers need the ability to "turn off" fields during `[Fetch]` (or other factory methods) when the current user lacks permission to edit a specific field.

## Solution

Add a `MarkReadOnly()` method to `IValidateProperty` that sets `IsReadOnly = true` permanently (one-and-done — once read-only, always read-only). Developers call `this["SensitiveField"].MarkReadOnly()` in factory methods based on authorization checks. The existing `IsReadOnly` serialization ensures the flag transfers to the client, and MudNeatoo components already bind to `IsReadOnly` for UI rendering.

---

## Requirements Review

**Reviewer:** neatoo-requirements-reviewer
**Reviewed:** 2026-04-06
**Verdict:** APPROVED

### Relevant Requirements Found

#### Design.Tests Behavioral Contracts (3 found)

1. **WHEN a property has `private set`, THEN `IsReadOnly` is `true`.**
   - Source: `src/Design/Design.Tests/PropertyTests/PropertyBasicsTests.cs` method `PrivateSet_IsReadOnlyTrue()` (line 151)
   - Impact: Not contradicted. `MarkReadOnly()` adds a second way to make `IsReadOnly = true`, but private-set properties continue to get `IsReadOnly = true` from the constructor via `PropertyInfoWrapper.IsPrivateSetter`. The one-and-done protection reinforces this — once `true` from `private set`, cannot be reverted.

2. **WHEN `SetValue()` is called on a read-only property, THEN `PropertyReadOnlyException` is thrown.**
   - Source: `src/Design/Design.Tests/PropertyTests/PropertyBasicsTests.cs` method `PrivateSet_SetValueThrows()` (line 184)
   - Impact: Not contradicted. `MarkReadOnly()` makes `IsReadOnly = true`, which triggers the existing `SetValue()` guard in `ValidateProperty<T>.SetValue()` (line 126). No change to the throw behavior.

3. **WHEN a public-set property is created, THEN `IsReadOnly` is `false`.**
   - Source: `src/Design/Design.Tests/PropertyTests/PropertyBasicsTests.cs` method `PrivateSet_PublicPropertyIsReadOnlyFalse()` (line 168)
   - Impact: Not contradicted. Public-set properties still default to `IsReadOnly = false`. `MarkReadOnly()` is opt-in and only changes this for properties where the developer explicitly calls it.

#### Framework Source Code Contracts (4 found)

4. **`IsReadOnly` is only set in constructors.** Four assignment sites exist in the entire codebase — all in constructors:
   - `ValidateProperty<T>(IPropertyInfo)` — sets from `propertyInfo.IsPrivateSetter` (line 22)
   - `ValidateProperty<T>(string, T, IRuleMessage[], bool)` — JSON constructor, sets from parameter (line 30)
   - `LazyLoadEntityProperty<T>(IPropertyInfo)` — forces `true` after base constructor (line 32)
   - `LazyLoadValidateProperty<T>(IPropertyInfo)` — forces `true` after base constructor (line 114)
   - Impact: No code ever sets `IsReadOnly = false` after it was `true`. The one-and-done change (blocking `true → false`) has no effect on any existing code path.

5. **`LazyLoadEntityProperty` and `LazyLoadValidateProperty` always set `IsReadOnly = true` in their constructors.**
   - Source: `src/Neatoo/Internal/LazyLoadEntityProperty.cs` (line 32), `src/Neatoo/Internal/LazyLoadValidateProperty.cs` (line 114)
   - Impact: Compatible with one-and-done. These set `false → true` (when base property is public) or `true → true` (when base property is private), both of which the one-and-done allows.

6. **`LazyLoadEntityProperty.SetValue()` overrides the base and does NOT check `IsReadOnly`.**
   - Source: `src/Neatoo/Internal/LazyLoadEntityProperty.cs` (line 133), `src/Neatoo/Internal/LazyLoadValidateProperty.cs` (line 209)
   - Impact: `MarkReadOnly()` on a LazyLoad property would be a no-op (already `IsReadOnly = true` from constructor). The overridden `SetValue()` bypasses the `IsReadOnly` guard anyway. No conflict.

7. **JSON deserialization uses `[JsonConstructor]` which sets `IsReadOnly` from a parameter.**
   - Source: `src/Neatoo/RemoteFactory/Internal/NeatooBaseJsonTypeConverter.cs` (lines 294, 315-324)
   - Impact: Compatible. The constructor sets the initial value. One-and-done only blocks `true → false`, and the JSON constructor sets the value directly before any subsequent changes.

#### Skill Documentation Contracts (3 found)

8. **MudNeatoo skill documents `IsReadOnly` as "determined by `propertyInfo.IsPrivateSetter`".**
   - Source: `skills/mudneatoo/SKILL.md` (lines 360-363)
   - Impact: This documentation is accurate for current behavior but will need updating after implementation. `IsReadOnly` will also be settable via `MarkReadOnly()`. The skill should be updated to reflect "set from `IsPrivateSetter` OR via `MarkReadOnly()` in factory methods."

9. **MudNeatoo skill states "ReadOnly state belongs to the domain model (via private setters), not the UI."**
   - Source: `skills/mudneatoo/SKILL.md` (line 426)
   - Impact: The proposed feature is consistent with this philosophy. `MarkReadOnly()` is called in factory methods (domain model), not from UI code. ReadOnly state remains domain-model-driven.

10. **Neatoo properties skill documents `IsReadOnly` as serialized and surviving round-trips.**
    - Source: `skills/neatoo/references/properties.md` (line 183)
    - Impact: Not contradicted. `MarkReadOnly()` sets `IsReadOnly = true` which is already serialized. No serialization changes needed.

#### Documentation — Known Gap (1 found)

11. **vsCSLA skill-gaps.md explicitly identifies "No Per-Property Authorization (CanRead/CanWrite)" as a gap.**
    - Source: `docs/vsCSLA/skill-gaps.md` (lines 144-154)
    - Quote: "Neatoo has `IsReadOnly` on properties (structural, from private setter) but no dynamic role-based per-property authorization."
    - The recommended fix in that document says: "For dynamic read-only, set `IsReadOnly` in rules or factory methods."
    - Impact: The proposed `MarkReadOnly()` directly addresses this documented gap.

#### Unit Test Contracts (3 found)

12. **`ValidatePropertyTests.IsReadOnly_InheritedFromPropertyInfo` and `EntityPropertyTests.IsReadOnly_InheritedFromPropertyInfo`**
    - Source: `src/Neatoo.UnitTest/Unit/Core/ValidatePropertyTests.cs` (line 1114), `src/Neatoo.UnitTest/Unit/Core/EntityPropertyTests.cs` (line 888)
    - Impact: Not contradicted. These test that `IsReadOnly` is set from `PropertyInfo` at construction time, which remains unchanged.

13. **`ValidatePropertyTests.SetValue_WhenReadOnly_ThrowsException` and `EntityPropertyTests.SetValue_WhenReadOnly_ThrowsException`**
    - Source: `src/Neatoo.UnitTest/Unit/Core/ValidatePropertyTests.cs` (line 1127), `src/Neatoo.UnitTest/Unit/Core/EntityPropertyTests.cs` (line 900)
    - Impact: Not contradicted. The `SetValue()` guard is unchanged.

14. **`EntityPropertyTests.JsonConstructor_WithIsReadOnlyTrue_SetsIsReadOnly`**
    - Source: `src/Neatoo.UnitTest/Unit/Core/EntityPropertyTests.cs` (line 180)
    - Impact: Not contradicted. JSON constructor continues to set `IsReadOnly` from parameter.

### Gaps — Areas With No Existing Requirements

1. **No existing test or documentation covers setting `IsReadOnly` at runtime.** All existing contracts treat `IsReadOnly` as construction-time only. The new `MarkReadOnly()` method is genuinely new territory — no existing tests need modification, but new tests must establish the new contracts.

2. **No existing test covers the behavior when `IsReadOnly` changes and `PropertyChanged` fires.** The MudNeatoo components already listen for `PropertyChanged("IsReadOnly")` (verified in razor.cs files), so firing it is correct, but there's no existing test asserting this.

3. **No documentation on `EntityProperty<T>` inheriting from `ValidateProperty<T>` w.r.t. `IsReadOnly`.** `EntityProperty<T>` does not override `IsReadOnly` — it inherits the base behavior unchanged. This is compatible, but there's no explicit documentation of this inheritance contract.

### Contradictions

None found.

### Recommendations for Architect

1. **Verify inheritance chain:** Confirm that `EntityProperty<T>` does not need any override for `MarkReadOnly()`. Since `EntityProperty<T>` does not override `IsReadOnly`, the base `ValidateProperty<T>.MarkReadOnly()` implementation will work for both validate and entity properties through inheritance. This was verified — no action needed.

2. **Update skill documentation post-implementation:** After implementation, update `skills/mudneatoo/SKILL.md` (lines 360-366) and `skills/neatoo/references/properties.md` (line 71) to document `MarkReadOnly()` as an additional way to set `IsReadOnly = true`.

3. **Update vsCSLA skill-gaps.md post-implementation:** The gap at `docs/vsCSLA/skill-gaps.md` line 144 will be partially addressed by this feature. Update to reflect that dynamic `IsReadOnly` is now supported via `MarkReadOnly()`, though full `CanReadProperty`/`CanWriteProperty` is not.

4. **Design.Tests files to verify against:** `src/Design/Design.Tests/PropertyTests/PropertyBasicsTests.cs` — all 8 existing tests must continue to pass unchanged.

---

## Plans

- [Field-Level ReadOnly Authorization Plan](../plans/field-level-readonly-authorization-plan.md)

---

## Tasks

- [x] Requirements review — APPROVED (14 requirements, 0 contradictions)
- [x] Architect validation — APPROVED
- [x] Implementation — 2 framework files, 9 unit tests, 5 integration tests
- [x] Developer code review — APPROVED (all 9 business rules traced)
- [x] Verification (architect + requirements) — VERIFIED / REQUIREMENTS SATISFIED
- [x] Documentation — Skills and vsCSLA gap doc updated

---

## Progress Log

### 2026-04-06
- Todo created from user request for field-level authorization via `MarkReadOnly()`
- Explored existing property system: `IsReadOnly` exists on `ValidateProperty<T>`, enforced by `SetValue()`, serialized via JSON, already used by MudNeatoo
- User chose `this["SensitiveField"].MarkReadOnly()` API shape
- User confirmed public is fine since one-and-done semantic prevents abuse

---

## Completion Verification

Before marking this todo as Complete, verify:

- [x] All builds pass
- [x] All tests pass

**Verification results:**
- Build: Neatoo.sln 0 errors, Design.sln 0 errors
- Tests: Neatoo.sln 1789 passed / 2 skipped (pre-existing), Design.sln 104 passed

---

## Results / Conclusions

Added `void MarkReadOnly()` to `IValidateProperty` for field-level authorization. Two framework files changed (`IValidateProperty.cs`, `ValidateProperty.cs`), with one-and-done semantics enforced via a backing field guard. The feature leverages the existing `IsReadOnly` infrastructure end-to-end: `SetValue()` throws, `SetPrivateValue()`/`LoadValue()` bypass, JSON serialization round-trips, and all MudNeatoo components bind automatically. Fills the documented vsCSLA gap "No Per-Property Authorization."
