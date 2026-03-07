# LazyLoad State Propagation Bug

**Status:** Complete
**Priority:** High
**Created:** 2026-03-07
**Last Updated:** 2026-03-07

---

## Problem

State changes in child entities loaded via `LazyLoad<T>` do not propagate up to the parent entity. When a child entity inside a `LazyLoad<T>` property is modified, the parent's `IsModified` remains `false` and `IsSavable` remains `false`. This means the aggregate root won't know it needs to be saved.

This is a **framework bug** — the same state propagation works correctly for regular partial properties (via `PropertyManager` and `EntityProperty<T>`), but `LazyLoad<T>` properties bypass this mechanism entirely because they are regular C# properties, not partial properties managed by `PropertyManager`.

### Failing Tests (already written)

`src/Neatoo.UnitTest/Integration/Concepts/EntityBase/LazyLoadStatePropagationTests.cs`:

| Test | Result | What It Proves |
|------|--------|----------------|
| `LazyLoadChild_InitialState_ParentNotModified` | Pass | Baseline: parent starts unmodified |
| `LazyLoadChild_ModifyChild_ParentNotSelfModified` | Pass | Parent itself isn't modified, only child |
| `LazyLoadChild_ModifyChild_ParentIsModified` | **FAIL** | Parent.IsModified stays false when LazyLoad child is modified |
| `LazyLoadChild_ModifyChild_ParentIsSavable` | **FAIL** | Parent.IsSavable stays false — aggregate root won't save |

### Root Cause Analysis

`LazyLoad<T>` is outside `PropertyManager`:
- Regular partial properties go through `Getter<T>()`/`Setter()` which creates `EntityProperty<T>` entries in `PropertyManager`
- `EntityProperty<T>.EntityChild` casts its `Value` to `IEntityMetaProperties` to aggregate `IsModified`
- `EntityBase.IsModified` reads from `PropertyManager.IsModified`
- `LazyLoad<T>` is a regular C# property — never enters `PropertyManager`, so state changes are invisible to the parent

Additionally, `LazyLoad<T>` currently implements `IEntityMetaProperties` which the architect assessment found to be dead weight — no framework code ever casts a `LazyLoad<T>` to `IEntityMetaProperties`.

### Related Design Smell

`LazyLoad<T>` implements `IEntityMetaProperties` (which also brings in `IFactorySaveMeta`), claiming to be an entity when it's a wrapper. All implementations use runtime casts (`_value as IEntityMetaProperties`) with defaults. This was identified as a code smell by architect review — the interface is dead weight with no framework consumer.

## Solution

Fix state propagation so that modifying a child entity inside a `LazyLoad<T>` property causes the parent's `IsModified` and `IsSavable` to update correctly.

The architect should evaluate multiple approaches, including but not limited to:
1. Have `EntityProperty<T>` recognize when its value is a `LazyLoad<T>` and delegate meta properties through it
2. Create a custom `EntityProperty` subclass for `LazyLoad<T>` values that knows how to extract and forward meta properties
3. Have the parent entity subscribe to `PropertyChanged` on `LazyLoad<T>` outside of `PropertyManager`
4. Other architectures the architect recommends

The solution should also address whether `LazyLoad<T>` implementing `IEntityMetaProperties` should be removed as part of this fix.

---

## Requirements Review

**Reviewer:** N/A (framework library — no business requirements docs)
**Reviewed:** 2026-03-07
**Verdict:** APPROVED

### Relevant Requirements Found

Neatoo's design contracts (from CLAUDE.md, Design.Domain, and existing behavior):

1. **State cascades UP automatically** — child entity state changes (IsModified, IsValid, IsBusy) must propagate to the parent entity automatically. This is documented in CLAUDE.md under "Aggregate Save Cascading" and demonstrated by existing `EntityParentChildFetchTests`.

2. **IsSavable = IsValid && IsModified && !IsBusy && !IsChild** — aggregate roots must report `IsSavable = true` when any part of the aggregate is modified. This is the core save eligibility contract.

3. **LazyLoad<T> is a regular property, not partial, not in PropertyManager** — this is a documented design decision in `Design.Domain/PropertySystem/LazyLoadProperty.cs`. Any fix must respect this decision or explicitly change it with justification.

4. **Interface-first design** — all references use interfaces, concretes are internal. The fix must not require consumers to know concrete types.

### Gaps

- No documented requirement for how `LazyLoad<T>` state propagation should work. The feature was added without this being specified.
- No documented requirement for whether `LazyLoad<T>` should implement `IEntityMetaProperties`.

### Contradictions

None — the proposed fix aligns with the existing "state cascades UP" contract. The current behavior (no propagation) is the contradiction.

### Recommendations for Architect

- The fix must make the failing tests pass without breaking the 1,749 existing tests
- Consider whether `IEntityMetaProperties` should be removed from `LazyLoad<T>` as part of this fix
- The serialization path for `LazyLoad<T>` (added in v0.16.0) must not be broken
- Consider `LazyLoad<T>` where T is not an entity (e.g., `LazyLoad<string>`) — state propagation should be a no-op in that case

---

## Plans

- [LazyLoad State Propagation Fix](../plans/completed/lazyload-state-propagation-fix.md)

---

## Tasks

- [x] Identify the bug (LazyLoad state not propagating)
- [x] Write failing tests
- [x] Architect review of LazyLoad implementing IEntityMetaProperties
- [x] Architect designs fix
- [x] Developer reviews plan
- [x] Implementation
- [x] Verification

---

## Progress Log

### 2026-03-07
- Discovered that `LazyLoad<T>` implementing `IEntityMetaProperties` is dead weight — no framework code uses it
- Confirmed via architect agent that `EntityProperty<T>.EntityChild` never sees `LazyLoad<T>` values (they're outside PropertyManager)
- Wrote 4 failing integration tests proving the bug
- 2 tests fail: `ParentIsModified` and `ParentIsSavable`
- 2 tests pass: `InitialState` and `ParentNotSelfModified`
- Architect completed full design evaluation of 7 architectural approaches (A through G)
- Rejected: EntityProperty recognizes LazyLoad (impossible -- LazyLoad not in PropertyManager), custom EntityProperty subclass (serialization conflicts, generator changes), parent subscribes outside PropertyManager (no setter hook), pure IsModified override (no event propagation)
- Recommended: Two-part fix -- (1) LazyLoad<T> forwards child PropertyChanged events, (2) EntityBase<T>.IsModified includes LazyLoad children via cached-per-type reflection
- Decision: KEEP IEntityMetaProperties on LazyLoad<T> -- serves as delegation contract for the polling override
- Plan created: `docs/plans/lazyload-state-propagation-fix.md`

---

## Completion Verification

Before marking this todo as Complete, verify:

- [x] All builds pass
- [x] All tests pass (including the 4 new LazyLoad propagation tests)
- [x] Design project builds successfully
- [x] Design project tests pass

**Verification results:**
- Build: Pass (all projects)
- Tests: 2,168 passed, 0 failed (Neatoo.UnitTest: 1,753, Samples: 245, Person.DomainModel.Tests: 55, BaseGenerator.Tests: 26, Design.Tests: 89)

---

## Results / Conclusions

Fixed `LazyLoad<T>` state propagation with a two-part approach:

1. **LazyLoad<T> forwards PropertyChanged** from its wrapped value, making it a transparent wrapper for state observation
2. **EntityBase/ValidateBase poll LazyLoad children** via cached-per-type reflection for IsModified, IsValid, and IsBusy, plus subscribe to PropertyChanged for reactive UI updates

Key decisions:
- **Kept IEntityMetaProperties on LazyLoad<T>** — initially assessed as dead weight, but it provides the delegation contract that the polling override reads. The original design was forward-looking.
- **No generator changes required** — the fix uses cached reflection matching the existing pattern in NeatooBaseJsonTypeConverter
- **LazyLoad<T> stays outside PropertyManager** — the documented design decision is respected; propagation works alongside PropertyManager, not through it
- 5 architectural approaches were evaluated; the recommended approach was the simplest that handles both polling correctness and reactive event propagation
