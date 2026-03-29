# LazyLoad: Extend from RemoteFactory LazyLoad

**Status:** Complete
**Priority:** High
**Created:** 2026-03-29
**Last Updated:** 2026-03-29

---

## Problem

Neatoo's `LazyLoad<T>` duplicates the entire core loading implementation from RemoteFactory's `LazyLoad<T>`. Now that RemoteFactory 0.26.0 has a standalone `LazyLoad<T>` with protected members for inheritance, Neatoo should inherit from it and only add the Neatoo-specific meta-property behavior (`IValidateMetaProperties`, `IEntityMetaProperties`).

## Solution

Refactor Neatoo's `LazyLoad<T>` to inherit from `Neatoo.RemoteFactory.LazyLoad<T>`, removing all duplicated core logic. Update package reference from 0.24.1 to 0.26.0. Switch all `ILazyLoadDeserializable` references to RemoteFactory's now-public interface.

---

## Requirements Review

**Reviewer:** neatoo-requirements-reviewer
**Reviewed:** 2026-03-29
**Verdict:** APPROVED (with one issue for architect to address)

### Relevant Requirements Found

**Source: Design.Domain (3 requirements)**

1. **DESIGN DECISION: LazyLoad<T> is a partial property** (`src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs` lines 9-17) — The generator detects `LazyLoad<T>` by checking `OriginalDefinition.Name == "LazyLoad"` AND `ContainingNamespace?.ToString() == "Neatoo"`. Since the refactored `Neatoo.LazyLoad<T>` remains in namespace `Neatoo`, this contract is respected.

2. **DESIGN DECISION: Value is a passive read** (`src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs` lines 19-23) — `Value` returns current state with no side effects; `LoadAsync()` triggers loading. The plan preserves this because the base class has identical behavior.

3. **DESIGN DECISION: ValidateBase.WaitForTasks() awaits in-progress LazyLoad children** (`src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs` lines 25-29) — WaitForTasks does NOT trigger loads on unaccessed LazyLoad children. The plan uses `LoadTask` (protected property) instead of `_loadTask` (private field), which is equivalent.

**Source: Unit Tests (17 behavioral contracts)**

All 17 unit tests in `src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs` test `Neatoo.LazyLoad<T>` directly by constructing instances with `new LazyLoad<T>(...)`. After the refactoring, these constructors still exist on the `Neatoo.LazyLoad<T>` subclass, delegating to base. Key contracts:

- WHEN Value accessed before LoadAsync(), THEN returns null with no side effects (test: `Value_BeforeLoad_ReturnsNullWithNoSideEffects`)
- WHEN LoadAsync() called concurrently, THEN only one actual load occurs (test: `LoadAsync_CalledConcurrently_OnlyLoadsOnce`)
- WHEN load fails, THEN HasLoadError is true and LoadError contains message (test: `LoadAsync_OnFailure_SetsErrorState`)
- WHEN HasLoadError, THEN IsValid returns false (test: `IsValid_WhenHasLoadError_ReturnsFalse`)
- WHEN pre-loaded via constructor, THEN IsLoaded=true and Value returns the value (test: `Factory_Create_WithValue_CreatesPreLoadedLazyLoad`)
- WHEN serialized/deserialized via STJ, THEN Value and IsLoaded survive round-trip (test: `Serialization_PreserveValueAndLoadedState`)
- WHEN IsBusy is checked during load, THEN returns true (test: `IsBusy_WhenLoading_ReturnsTrue`)
- WHEN IsSelfModified is checked, THEN always returns false (test: `IsSelfModified_AlwaysReturnsFalse`)
- WHEN IEntityMetaProperties delegation checked, THEN delegates to loaded value (tests: `IsModified_DelegatesToValue_WhenLoaded`, `IsNew_DelegatesToValue_WhenLoaded`, `IsDeleted_DelegatesToValue_WhenLoaded`)

All contracts are preserved because the subclass delegates core logic to the identical base class and keeps the Neatoo-specific `IValidateMetaProperties`/`IEntityMetaProperties` implementations.

**Source: Integration Tests (15 behavioral contracts)**

- `src/Neatoo.UnitTest/Integration/Concepts/EntityBase/LazyLoadStatePropagationTests.cs` (5 tests) — State propagation from LazyLoad child to parent (IsModified, IsSelfModified, IsSavable, IsBusy, IsValid). These depend on `IValidateMetaProperties` and `IEntityMetaProperties` implementations on `LazyLoad<T>`, which are kept in the subclass.

- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/FatClientLazyLoadTests.cs` (6 tests) — NeatooBaseJsonTypeConverter serialization/deserialization of LazyLoad properties. These test the converter's `typeof(LazyLoad<>)` detection and `ILazyLoadDeserializable` merge logic.

- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/TwoContainerLazyLoadTests.cs` (2 tests) — Client-server round-trip with LazyLoad properties.

- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/WaitForTasksLazyLoadCrashTests.cs` (1 test) — WaitForTasks with AddActionAsync and LazyLoad through two-container pipeline.

- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/FatClientDeferredLazyLoadTests.cs` and `TwoContainerDeferredLazyLoadTests.cs` (5 tests) — Deferred loader serialization bug tests.

**Source: Skill Documentation (1 reference)**

- `skills/neatoo/references/lazy-loading.md` — Documents the LazyLoad API surface (`Value`, `IsLoaded`, `IsLoading`, `HasLoadError`, `LoadError`, `LoadAsync()`, `SetValue()`, `ILazyLoadFactory`), meta property delegation table, serialization rules, and constructor-based creation pattern. All documented behavior is preserved by the refactoring.

**Source: Generator (1 contract)**

- `src/Neatoo.BaseGenerator/Extractors/PropertyExtractor.cs` lines 56-71 — Detects LazyLoad<T> by `OriginalDefinition.Name == "LazyLoad"` AND `ContainingNamespace?.ToString() == "Neatoo"`. The refactored class stays in namespace `Neatoo`, so detection is unaffected. Plan explicitly addresses this (plan section 6).

### Gaps

1. **No Design.Tests for LazyLoad** — The `src/Design/Design.Tests/` project has no LazyLoad-specific tests. All behavioral contracts are verified through `src/Neatoo.UnitTest/` tests only. This is not a gap introduced by this change, just a pre-existing absence.

### Contradictions

**None that would block implementation.** However, there is one issue the architect must address:

**Serializer `typeof(LazyLoad<>)` namespace resolution issue** — The plan (section 7, lines 117-124) states that `typeof(LazyLoad<>)` in `NeatooBaseJsonTypeConverter.cs` "uses `Neatoo.LazyLoad<>`, which is the declared property type. Still works." This analysis is incomplete.

`NeatooBaseJsonTypeConverter.cs` is in namespace `Neatoo.RemoteFactory.Internal` (`src/Neatoo/RemoteFactory/Internal/NeatooBaseJsonTypeConverter.cs` line 8). After upgrading to RemoteFactory 0.26.0, which introduces `Neatoo.RemoteFactory.LazyLoad<T>`, the C# namespace resolution for unqualified `LazyLoad<>` in that file would find `Neatoo.RemoteFactory.LazyLoad<>` first (closer enclosing namespace), NOT `Neatoo.LazyLoad<>`. Meanwhile, entity property types remain `Neatoo.LazyLoad<T>`, so `property.PropertyType.GetGenericTypeDefinition()` returns `Neatoo.LazyLoad<>`. The comparison `GetGenericTypeDefinition() == typeof(LazyLoad<>)` would compare `Neatoo.LazyLoad<>` against `Neatoo.RemoteFactory.LazyLoad<>` -- these are DIFFERENT types, so the check FAILS.

This would silently break LazyLoad serialization in both the Read path (line 136) and Write path (line 408). LazyLoad properties would be silently dropped from JSON, exactly the bug fixed by the completed todo `docs/todos/completed/lazyload-serialization-bug.md`.

**Fix:** Use fully-qualified `typeof(Neatoo.LazyLoad<>)` or `typeof(global::Neatoo.LazyLoad<>)` in both locations in NeatooBaseJsonTypeConverter.cs. Alternatively, the check could use `IsAssignableTo(typeof(Neatoo.RemoteFactory.LazyLoad<>))`, but the fully-qualified approach is simpler and preserves the exact existing semantics.

### Recommendations for Architect

1. **Address the `typeof(LazyLoad<>)` namespace resolution issue** in NeatooBaseJsonTypeConverter.cs. This is not a requirement contradiction -- it is a technical risk in the plan that would cause a serialization regression if not handled. The plan's section 7 analysis is incorrect and should be corrected.

2. **Ensure `[JsonConstructor]` on Neatoo's parameterless constructor.** The plan's risk section (line 166) correctly identifies this, but the code snippet in the Design section (lines 69-71) omits the attribute. The architect should ensure the implementation includes it.

3. **Verify meta-property implementations compile with protected members.** The plan uses `LoadTask` and `ClearLoadError()` from the base class. Verify that:
   - `WaitForTasks()` uses `LoadTask` (returns `Task<T?>?`) correctly where the current code uses `_loadTask`
   - `ClearAllMessages()` and `ClearSelfMessages()` use `ClearLoadError()` where the current code uses `_loadError = null`

4. **Run all 43+ LazyLoad tests after implementation.** The comprehensive test suite covers the exact behavioral contracts that must be preserved. Tests span:
   - `src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs` (17 tests)
   - `src/Neatoo.UnitTest/Integration/Concepts/EntityBase/LazyLoadStatePropagationTests.cs` (8 tests)
   - `src/Neatoo.UnitTest/Integration/Concepts/Serialization/FatClientLazyLoadTests.cs` (6 tests)
   - `src/Neatoo.UnitTest/Integration/Concepts/Serialization/TwoContainerLazyLoadTests.cs` (2 tests)
   - `src/Neatoo.UnitTest/Integration/Concepts/Serialization/WaitForTasksLazyLoadCrashTests.cs` (1 test)
   - `src/Neatoo.UnitTest/Integration/Concepts/Serialization/FatClientDeferredLazyLoadTests.cs` (3 tests)
   - `src/Neatoo.UnitTest/Integration/Concepts/Serialization/TwoContainerDeferredLazyLoadTests.cs` (3 tests)

---

## Plans

- [LazyLoad Extend RemoteFactory Plan](../plans/lazyload-extend-remotefactory-plan.md)

---

## Tasks

- [x] Requirements review
- [x] Architect review and plan
- [x] Developer review
- [x] Implementation (30 files, rename LazyLoad → EntityLazyLoad)
- [x] Verification (Architect: VERIFIED, Requirements: SATISFIED)
- [x] Documentation (skill files updated)

---

## Progress Log

### 2026-03-29
- Created todo from discussion about extending LazyLoad from RemoteFactory
- RemoteFactory 0.26.0 published with protected members (`LoadTask`, `ClearLoadError()`, `OnPropertyChanged` virtual) and public `ILazyLoadDeserializable`
- Neatoo currently on RemoteFactory 0.24.1
- Identified affected files: LazyLoad.cs, ILazyLoadFactory.cs, LazyLoadValidateProperty.cs, LazyLoadEntityProperty.cs, NeatooBaseJsonTypeConverter.cs, generator PropertyExtractor.cs

---

## Completion Verification

Before marking this todo as Complete, verify:

- [x] All builds pass
- [x] All tests pass

**Verification results:**
- Build: 0 errors (Neatoo.sln + Design.sln)
- Tests: 2129 passed, 0 failed, 2 pre-existing skips (Neatoo.sln); 98 passed, 0 failed (Design.sln)

---

## Results / Conclusions

Neatoo's `LazyLoad<T>` has been refactored to `EntityLazyLoad<T>`, inheriting from `Neatoo.RemoteFactory.LazyLoad<T>` (RemoteFactory 0.26.0). All duplicated core loading logic removed — the subclass contains only Neatoo-specific meta-property implementations (`IValidateMetaProperties`, `IEntityMetaProperties`).

The rename from `LazyLoad` to `EntityLazyLoad` was necessary to avoid CS0104 namespace collision with `Neatoo.RemoteFactory.LazyLoad<T>` in files that import both namespaces.

**30 files modified** across core library, source generator, analyzer, tests, design projects, samples, and examples. All 23 behavioral contracts preserved. Zero test regressions.
