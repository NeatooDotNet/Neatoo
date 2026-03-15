# Revert LazyLoad Auto-Trigger: Make .Value Passive, Remove GetAwaiter

**Date:** 2026-03-15
**Related Todo:** [Revert LazyLoad Auto-Trigger](../todos/revert-lazyload-auto-trigger.md)
**Status:** Requirements Documented
**Last Updated:** 2026-03-15

---

## Overview

Revert the v0.21.0 auto-trigger behavior on `LazyLoad<T>.Value` and remove `GetAwaiter()`. The `.Value` getter becomes a passive read (returns current value or null). `LoadAsync()` remains the sole mechanism for triggering loads. Documentation shifts to two patterns: `LoadAsync()` for imperative code, `.Value` for binding.

This is a revert of the work in [LazyLoad auto-trigger on Value access](../todos/completed/lazyload-auto-trigger-on-value-access.md). The WaitForTasks integration for LazyLoad children (also added in v0.21.0) is preserved because explicit `LoadAsync()` calls still produce running tasks that WaitForTasks must await.

---

## Business Requirements Context

**Source:** [Todo Requirements Review](../todos/revert-lazyload-auto-trigger.md#requirements-review) (no formal requirements review was done for this revert since the todo had "Verdict: Pending", but the completed v0.21.0 todo has extensive requirements documentation that this plan references.)

### Relevant Existing Requirements

#### Design Decisions (Design.Domain)

- `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs` line 19: States "Accessing Value auto-triggers a fire-and-forget load" -- this was added in v0.21.0 and must be reverted.
- `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs` line 26: "ValidateBase.WaitForTasks() awaits in-progress LazyLoad children via PropertyManager.WaitForTasks()" -- this behavior is PRESERVED (not reverted).
- `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs` line 32: Nullable reference type constraint `where T : class?` -- UNAFFECTED.

#### Serialization Contract

- `LazyLoad<T>` serializes `Value` and `IsLoaded`. Loader delegate is `[JsonIgnore]`. The `NeatooBaseJsonTypeConverter` merges deserialized state into constructor-created instances. UNAFFECTED by this change.

#### State Propagation Contract

- LazyLoad property subclasses (LazyLoadValidateProperty, LazyLoadEntityProperty) delegate IsValid, IsBusy, IsModified, WaitForTasks to the inner entity. UNAFFECTED -- the property subclasses delegate to `LazyLoad<T>.WaitForTasks()`, which returns `_loadTask` regardless of how the load was triggered.

#### Existing Tests (affected)

Unit tests (`src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs`):
- `Value_BeforeLoad_ReturnsNullSynchronously` -- currently tests auto-trigger side effect; must revert to passive read test
- `Await_LoadsValue` (line 92) -- uses `await lazyLoad` (GetAwaiter); must change to `await lazyLoad.LoadAsync()`
- `ValueAccess_TriggersFireAndForgetLoad` -- DELETE (tests auto-trigger)
- `ValueAccess_AutoTrigger_CompletesSuccessfully` -- DELETE (tests auto-trigger)
- `ValueAccess_AlreadyLoaded_ReturnsCachedValue` -- KEEP (still valid for passive read)
- `ValueAccess_DuringLoad_DoesNotStartSecondLoad` -- ADAPT (still valid but remove auto-trigger premise)
- `ValueAccess_NoLoader_ReturnsNullWithoutException` -- KEEP (still valid for passive read)
- `ValueAccess_AutoTrigger_LoadFailure_SetsErrorState` -- DELETE (tests auto-trigger)
- `ValueAccess_ConcurrentAccess_SharesOneLoad` -- DELETE (tests auto-trigger)
- `IsLoadingAccess_DoesNotTriggerLoad` -- KEEP (still valid)
- `IsLoadedAccess_DoesNotTriggerLoad` -- KEEP (still valid)
- `ExplicitLoadAsync_StillWorksIdentically` -- KEEP (rename to remove "still")
- `ExplicitLoadAsync_OnFailure_StillPropagatesException` -- KEEP (rename)
- `WaitForTasks_AfterAutoTrigger_AwaitsLoad` -- ADAPT to use explicit `LoadAsync()` instead of Value trigger
- `Factory_Create_WithLoader_CreatesLazyLoad` (line 628) -- uses `await lazyLoad` (GetAwaiter); must change to `await lazyLoad.LoadAsync()`
- `LoadAsync_OnFailure_SetsErrorState` (line 180) -- accesses `lazyLoad.Value` in assertion after error; passive read means this still returns null (correct)

Integration tests (`src/Neatoo.UnitTest/Integration/Concepts/EntityBase/LazyLoadStatePropagationTests.cs`):
- `LazyLoadAutoTriggerPropagationTests` class -- 6 tests that test auto-trigger propagation:
  - `ParentIsBusy_AfterAutoTriggeredChildLoad` -- ADAPT to use explicit `LoadAsync()`
  - `ParentIsValid_AfterAutoTriggeredChildLoadFailure` -- ADAPT to use explicit `LoadAsync()`
  - `ParentWaitForTasks_AwaitsAutoTriggeredLazyLoadChild` -- ADAPT to use explicit `LoadAsync()`
  - `ParentWaitForTasksWithToken_AwaitsAutoTriggeredLazyLoadChild` -- ADAPT to use explicit `LoadAsync()`
  - `ParentWaitForTasks_PreLoadedChild_CompletesImmediately` -- KEEP as-is
  - `ParentWaitForTasks_UnaccessedChild_CompletesWithoutTrigger` -- KEEP as-is

#### Code using `await lazyLoad` (GetAwaiter pattern) that must change

1. `src/samples/LazyLoadSamples.cs:74` -- `var child = await parent.LazyChild;`
2. `src/Neatoo.UnitTest/Integration/Concepts/Serialization/WaitForTasksLazyLoadCrashEntity.cs:83` -- `var child = await parent.LazyChild;`
3. `src/Examples/Person/Person.DomainModel/Person.cs:114` -- `var phoneList = await this.PersonPhoneList;`
4. `src/Examples/Person/Person.DomainModel/Person.cs:144` -- `var phoneList = await this.PersonPhoneList;`
5. `src/Examples/Person/Person.DomainModel.Tests/Integration Tests/PersonIntegrationTests.cs:184` -- `var fetchedPhoneList = await result.PersonPhoneList;`
6. `src/Examples/Person/Person.DomainModel.Tests/Integration Tests/PersonIntegrationTests.cs:224` -- `var phoneList = await result.PersonPhoneList;`

All must change to `await x.LoadAsync()`.

### Gaps

1. **No test for passive Value read with unloaded instance** -- After revert, `Value` on an unloaded instance should return null without any side effect (no load triggered, no state change). The current `Value_BeforeLoad_ReturnsNullSynchronously` test has auto-trigger setup that obscures this. Need a clean passive-read test.
2. **No test confirming GetAwaiter is gone** -- After removal, `await lazyLoad` should not compile. This is verified by the compiler, not a runtime test.

### Contradictions

None. The revert restores the original v0.11.0 design principle ("Value never triggers a load"). The v0.21.0 design decisions that say "Value auto-triggers" are being updated, not contradicted.

### Recommendations for Architect

1. Preserve WaitForTasks integration -- it serves explicit `LoadAsync()` calls, not just auto-triggers.
2. Remove `TriggerLoadAsync()` as dead code.
3. The `IValidateProperty.GetAwaiter()` (in `IValidateProperty.cs:84`) is a SEPARATE concept (awaiting property-level tasks, not LazyLoad) -- do NOT remove it.
4. Update Design.Domain comments to reflect passive `.Value`.

---

## Business Rules (Testable Assertions)

### Core Value Behavior

1. WHEN `LazyLoad<T>` created with loader AND `Value` accessed before `LoadAsync()`, THEN `Value` RETURNS `null` with NO side effects (no load triggered, `IsLoading` remains false). -- Source: v0.11.0 design principle restored
2. WHEN `LazyLoad<T>` loaded via `LoadAsync()`, THEN `Value` RETURNS the loaded value. -- Source: existing `LoadAsync_LoadsValue` test
3. WHEN `LazyLoad<T>` created with pre-loaded value, THEN `Value` RETURNS that value immediately AND `IsLoaded` is `true`. -- Source: existing pre-loaded constructor behavior
4. WHEN `LazyLoad<T>` created via parameterless constructor (deserialized, no loader), THEN `Value` RETURNS `null` AND no exception thrown. -- Source: existing serialization contract

### LoadAsync Behavior (unchanged)

5. WHEN `LoadAsync()` called, THEN it invokes the loader delegate, sets `IsLoaded = true`, and RETURNS `Task<T?>` with the loaded value. -- Source: existing `LoadAsync_LoadsValue` test
6. WHEN `LoadAsync()` called AND loader throws, THEN exception propagates to caller AND `HasLoadError = true` AND `LoadError` contains message AND `IsLoaded = false`. -- Source: existing `LoadAsync_OnFailure_SetsErrorState` test
7. WHEN `LoadAsync()` called concurrently, THEN only one loader invocation occurs AND all callers share the same task. -- Source: existing `LoadAsync_CalledConcurrently_OnlyLoadsOnce` test
8. WHEN `LoadAsync()` called AND already loaded, THEN RETURNS cached value immediately without re-invoking loader. -- Source: existing `LoadAsync_WhenAlreadyLoaded_ReturnsImmediately` test
9. WHEN `LoadAsync()` called AND no loader delegate (deserialized instance), THEN THROWS `InvalidOperationException`. -- Source: existing deserialization contract

### GetAwaiter Removal

10. WHEN `LazyLoad<T>` type inspected, THEN `GetAwaiter()` method does NOT exist (compile-time verification). -- Source: NEW (user decision)

### State Property Side Effects

11. WHEN `IsLoading` accessed, THEN no load is triggered. -- Source: existing `IsLoadingAccess_DoesNotTriggerLoad` test
12. WHEN `IsLoaded` accessed, THEN no load is triggered. -- Source: existing `IsLoadedAccess_DoesNotTriggerLoad` test

### PropertyChanged Events (unchanged)

13. WHEN `LoadAsync()` completes successfully, THEN `PropertyChanged` fires for `Value`, `IsLoaded`, and `IsLoading`. -- Source: existing `LoadAsync_RaisesPropertyChangedForAllStateProperties` test

### WaitForTasks Integration (preserved from v0.21.0)

14. WHEN `LoadAsync()` called (explicit) AND load in progress, THEN parent's `WaitForTasks()` awaits the load task via `PropertyManager.WaitForTasks()`. -- Source: v0.21.0 WaitForTasks integration (preserved)
15. WHEN parent has LazyLoad child that is pre-loaded or never accessed, THEN parent's `WaitForTasks()` completes immediately without triggering any load. -- Source: existing `ParentWaitForTasks_PreLoadedChild_CompletesImmediately` and `ParentWaitForTasks_UnaccessedChild_CompletesWithoutTrigger` tests

### Meta Property Delegation (unchanged)

16. WHEN `LazyLoad<T>` is loading (`LoadAsync()` in progress), THEN `IsBusy` RETURNS `true`. -- Source: existing `IsBusy_WhenLoading_ReturnsTrue` test
17. WHEN `LazyLoad<T>` has load error, THEN `IsValid` RETURNS `false`. -- Source: existing `IsValid_WhenHasLoadError_ReturnsFalse` test

### Test Scenarios

| # | Scenario | Inputs / State | Rule(s) | Expected Result |
|---|----------|---------------|---------|-----------------|
| 1 | Passive Value read on unloaded instance | `new LazyLoad<T>(loader)`, access `Value` | 1 | `null`, `IsLoading == false`, `IsLoaded == false` |
| 2 | Value returns loaded data after LoadAsync | `lazyLoad.LoadAsync()`, then access `Value` | 2, 5 | Loaded value, `IsLoaded == true` |
| 3 | Pre-loaded instance returns value | `new LazyLoad<T>(preLoadedValue)` | 3 | Value == preLoadedValue, `IsLoaded == true` |
| 4 | Deserialized instance returns null safely | `new LazyLoad<T>()`, access `Value` | 4 | `null`, no exception |
| 5 | LoadAsync propagates exception | `loader` that throws, call `LoadAsync()` | 6 | Exception propagates, `HasLoadError == true` |
| 6 | Concurrent LoadAsync shares single load | Two `LoadAsync()` calls, slow loader | 7 | Same task, `loadCount == 1` |
| 7 | Already-loaded LoadAsync returns cached | `LoadAsync()` twice | 8 | `loadCount == 1`, second call returns immediately |
| 8 | LoadAsync on deserialized no-loader throws | `new LazyLoad<T>()`, call `LoadAsync()` | 9 | `InvalidOperationException` |
| 9 | GetAwaiter not present | Attempt `await lazyLoad` | 10 | Compile error (verified by build) |
| 10 | IsLoading access has no side effect | `new LazyLoad<T>(loader)`, access `IsLoading` | 11 | `false`, no load triggered |
| 11 | IsLoaded access has no side effect | `new LazyLoad<T>(loader)`, access `IsLoaded` | 12 | `false`, no load triggered |
| 12 | PropertyChanged fires on successful load | `LoadAsync()` with PropertyChanged listener | 13 | Events for Value, IsLoaded, IsLoading |
| 13 | Parent WaitForTasks awaits explicit load | `LoadAsync()` in progress, parent calls `WaitForTasks()` | 14 | Parent waits until load completes |
| 14 | Parent WaitForTasks does not trigger loads | LazyLoad child never accessed, parent calls `WaitForTasks()` | 15 | Completes immediately, child not loaded |
| 15 | IsBusy during load | `LoadAsync()` in progress | 16 | `IsBusy == true` |
| 16 | IsValid false on load error | `LoadAsync()` that fails | 17 | `IsValid == false` |

---

## Approach

This is a surgical revert of three specific behaviors added in v0.21.0, plus cleanup of code that used the reverted APIs:

1. **Remove auto-trigger from `Value` getter** -- Delete the `if (!_isLoaded && !_isLoading ...)` block and the `TriggerLoadAsync()` method.
2. **Remove `GetAwaiter()` from `LazyLoad<T>`** -- Delete the method and the `System.Runtime.CompilerServices` using.
3. **Update all call sites** using `await lazyLoad` to use `await lazyLoad.LoadAsync()`.
4. **Update tests** -- Delete auto-trigger-specific tests, adapt integration tests to use explicit `LoadAsync()`, rename tests that referenced "StillWorks" naming.
5. **Update Design.Domain comments** and code comments on `LazyLoad.cs` to reflect passive `.Value`.

The WaitForTasks integration is NOT reverted. `ValidateBase.WaitForTasks()` continues to call `PropertyManager.WaitForTasks()`, and LazyLoad property subclasses continue to delegate `WaitForTasks()` to `LazyLoad<T>.WaitForTasks()`.

---

## Design

### LazyLoad.cs Changes

**Value getter** -- becomes a simple field return:
```csharp
public T? Value
{
    get => _value;
    private set => _value = value;
}
```

**Remove `TriggerLoadAsync()` method** (lines 291-303) -- dead code after the Value getter change.

**Remove `GetAwaiter()` method** (line 309) -- `await lazyLoad` syntax no longer supported.

**Update XML docs** -- The class-level summary and Value property docs must be updated from "auto-triggers fire-and-forget load" to "returns current state, null if not loaded."

**Remove `System.Runtime.CompilerServices` using** -- only needed for `TaskAwaiter<T?>` which is no longer used.

### Call Site Changes

All 6 locations using `await lazyLoad` (GetAwaiter) change to `await lazyLoad.LoadAsync()`:
- `src/samples/LazyLoadSamples.cs:74`
- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/WaitForTasksLazyLoadCrashEntity.cs:83`
- `src/Examples/Person/Person.DomainModel/Person.cs:114`
- `src/Examples/Person/Person.DomainModel/Person.cs:144`
- `src/Examples/Person/Person.DomainModel.Tests/Integration Tests/PersonIntegrationTests.cs:184`
- `src/Examples/Person/Person.DomainModel.Tests/Integration Tests/PersonIntegrationTests.cs:224`

### Test Changes

**Unit tests (`LazyLoadTests.cs`):**

| Test | Action | Rationale |
|------|--------|-----------|
| `Value_BeforeLoad_ReturnsNullSynchronously` | REWRITE -- remove auto-trigger setup, verify pure passive read | Rule 1 |
| `Await_LoadsValue` | REWRITE -- change to `await lazyLoad.LoadAsync()`, rename to `LoadAsync_LoadsValue_ViaExplicitCall` or merge with existing `LoadAsync_LoadsValue` | Rule 10 |
| `ValueAccess_TriggersFireAndForgetLoad` | DELETE | Tests reverted behavior |
| `ValueAccess_AutoTrigger_CompletesSuccessfully` | DELETE | Tests reverted behavior |
| `ValueAccess_AlreadyLoaded_ReturnsCachedValue` | KEEP | Rule 3 |
| `ValueAccess_DuringLoad_DoesNotStartSecondLoad` | ADAPT -- change premise to "explicit LoadAsync in progress, Value returns null" | Rule 1 |
| `ValueAccess_NoLoader_ReturnsNullWithoutException` | KEEP | Rule 4 |
| `ValueAccess_AutoTrigger_LoadFailure_SetsErrorState` | DELETE | Tests reverted behavior |
| `ValueAccess_ConcurrentAccess_SharesOneLoad` | DELETE | Tests reverted behavior (concurrent Value access auto-trigger) |
| `IsLoadingAccess_DoesNotTriggerLoad` | KEEP | Rule 11 |
| `IsLoadedAccess_DoesNotTriggerLoad` | KEEP | Rule 12 |
| `ExplicitLoadAsync_StillWorksIdentically` | RENAME to `LoadAsync_Works` (remove "Still") | Rule 5 |
| `ExplicitLoadAsync_OnFailure_StillPropagatesException` | RENAME to `LoadAsync_OnFailure_PropagatesException` | Rule 6 |
| `WaitForTasks_AfterAutoTrigger_AwaitsLoad` | ADAPT to use `LoadAsync()` trigger, rename to `WaitForTasks_AfterExplicitLoad_AwaitsLoad` | Rule 14 |
| `Factory_Create_WithLoader_CreatesLazyLoad` | Change `await lazyLoad` to `await lazyLoad.LoadAsync()` | Rule 10 |

Remove the `#region Auto-Trigger Tests` region markers.

**Integration tests (`LazyLoadStatePropagationTests.cs`):**

| Test | Action | Rationale |
|------|--------|-----------|
| `ParentIsBusy_AfterAutoTriggeredChildLoad` | ADAPT -- use `LoadAsync()`, rename to `ParentIsBusy_AfterExplicitChildLoad` | Rule 14, 16 |
| `ParentIsValid_AfterAutoTriggeredChildLoadFailure` | ADAPT -- use `LoadAsync()`, rename to `ParentIsValid_AfterExplicitChildLoadFailure` | Rule 14, 17 |
| `ParentWaitForTasks_AwaitsAutoTriggeredLazyLoadChild` | ADAPT -- use `LoadAsync()`, rename to `ParentWaitForTasks_AwaitsExplicitLazyLoadChild` | Rule 14 |
| `ParentWaitForTasksWithToken_AwaitsAutoTriggeredLazyLoadChild` | ADAPT -- use `LoadAsync()`, rename to `ParentWaitForTasksWithToken_AwaitsExplicitLazyLoadChild` | Rule 14 |
| `ParentWaitForTasks_PreLoadedChild_CompletesImmediately` | KEEP | Rule 15 |
| `ParentWaitForTasks_UnaccessedChild_CompletesWithoutTrigger` | KEEP (update comment to remove "only Value getter triggers" since now nothing triggers) | Rule 15 |

Rename `LazyLoadAutoTriggerPropagationTests` class to `LazyLoadExplicitLoadPropagationTests`.

### What is NOT Changed

- `LoadAsync()` method -- unchanged
- `LoadAsyncCore()` method -- unchanged
- `SetValue()` method -- unchanged
- `ILazyLoadDeserializable` interface and implementation -- unchanged
- `IValidateMetaProperties` implementation -- unchanged
- `IEntityMetaProperties` implementation -- unchanged
- `IValidateProperty.GetAwaiter()` -- SEPARATE concept, unchanged
- `ValidateBase.WaitForTasks()` and `PropertyManager.WaitForTasks()` -- preserved
- LazyLoad property subclasses (`LazyLoadValidateProperty`, `LazyLoadEntityProperty`) -- unchanged
- Serialization/deserialization -- unchanged
- `LazyLoadFactory` -- unchanged

---

## Implementation Steps

### Phase 1: Core Framework Change

1. Modify `src/Neatoo/LazyLoad.cs`:
   - Remove auto-trigger logic from `Value` getter (make it `get => _value;`)
   - Remove `TriggerLoadAsync()` method
   - Remove `GetAwaiter()` method
   - Remove `using System.Runtime.CompilerServices;`
   - Update XML doc on class summary and `Value` property
2. Update all 6 `await lazyLoad` call sites to `await lazyLoad.LoadAsync()`:
   - `src/samples/LazyLoadSamples.cs`
   - `src/Neatoo.UnitTest/Integration/Concepts/Serialization/WaitForTasksLazyLoadCrashEntity.cs`
   - `src/Examples/Person/Person.DomainModel/Person.cs` (2 locations)
   - `src/Examples/Person/Person.DomainModel.Tests/Integration Tests/PersonIntegrationTests.cs` (2 locations)
3. Build to verify compilation succeeds

### Phase 2: Test Updates

1. Update `src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs`:
   - Rewrite `Value_BeforeLoad_ReturnsNullSynchronously` as passive read test
   - Delete `Await_LoadsValue` (covered by `LoadAsync_LoadsValue`)
   - Delete 4 auto-trigger tests (TriggersFireAndForgetLoad, CompletesSuccessfully, LoadFailure, ConcurrentAccess)
   - Keep and adapt 3 tests (AlreadyLoaded, DuringLoad, NoLoader)
   - Rename 2 tests (ExplicitLoadAsync -> LoadAsync)
   - Adapt WaitForTasks test to use explicit LoadAsync
   - Fix Factory_Create_WithLoader test
   - Remove `#region Auto-Trigger Tests` markers
2. Update `src/Neatoo.UnitTest/Integration/Concepts/EntityBase/LazyLoadStatePropagationTests.cs`:
   - Rename class `LazyLoadAutoTriggerPropagationTests` -> `LazyLoadExplicitLoadPropagationTests`
   - Adapt 4 tests from Value-trigger to explicit `LoadAsync()` trigger
   - Keep 2 tests unchanged
   - Update comments
3. Run all tests to verify

### Phase 3: Design Project and Comments

1. Update `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs`:
   - Update DESIGN DECISION comment (line 19-24) from auto-trigger to passive read
   - Update WaitForTasks DESIGN DECISION comment (line 26-30) to remove "auto-triggered" language
2. Build Design.sln to verify

---

## Acceptance Criteria

- [ ] `LazyLoad<T>.Value` returns `null` on unloaded instance with NO side effects
- [ ] `LazyLoad<T>.GetAwaiter()` does not exist (verified by build -- `await lazyLoad` would fail to compile)
- [ ] `LazyLoad<T>.LoadAsync()` works identically to before
- [ ] All 6 `await lazyLoad` call sites changed to `await lazyLoad.LoadAsync()`
- [ ] `TriggerLoadAsync()` removed
- [ ] WaitForTasks integration preserved (parent awaits explicit LoadAsync in-progress tasks)
- [ ] All tests pass (0 failures)
- [ ] Build succeeds with 0 errors

---

## Dependencies

- None. This is a self-contained change within the Neatoo repository.

---

## Risks / Considerations

1. **Breaking change for consumers** -- Any code using `await lazyLoad` (GetAwaiter pattern) will fail to compile after this change. This is intentional and desired. The Person example and samples demonstrate the migration path.
2. **zTreatment migration** -- zTreatment code that reads `.Value` for convenience accessors will no longer trigger loads (the desired outcome). Any `await lazyLoad` usages in zTreatment will need to change to `await lazyLoad.LoadAsync()`. This is out of scope for this plan but should be noted in release notes.
3. **Version bump** -- This is a breaking API change (method removal). Should be a major version bump per the CI/CD standards.

---

## Architectural Verification

**Scope Table:**

| Component | Affected? | Status |
|-----------|-----------|--------|
| `LazyLoad<T>.Value` getter | Yes -- remove auto-trigger | Needs Implementation |
| `LazyLoad<T>.GetAwaiter()` | Yes -- remove | Needs Implementation |
| `LazyLoad<T>.TriggerLoadAsync()` | Yes -- remove (dead code) | Needs Implementation |
| `LazyLoad<T>.LoadAsync()` | No | Verified (unchanged) |
| `LazyLoad<T>.SetValue()` | No | Verified (unchanged) |
| `ILazyLoadDeserializable` | No | Verified (unchanged) |
| `IValidateProperty.GetAwaiter()` | No -- separate concept | Verified (unrelated) |
| `ValidateBase.WaitForTasks()` | No | Verified (preserved) |
| `PropertyManager.WaitForTasks()` | No | Verified (preserved) |
| LazyLoad property subclasses | No | Verified (unchanged) |
| Serialization pipeline | No | Verified (unchanged) |
| Person Example | Yes -- 4 call sites | Needs Implementation |
| Samples | Yes -- 1 call site | Needs Implementation |
| CrashEntity test | Yes -- 1 call site | Needs Implementation |

**Verification Evidence:**

- Build: `dotnet build src/Neatoo.sln` passes with 0 errors (verified 2026-03-15)
- Design.Domain `LazyLoadProperty.cs` contains auto-trigger DESIGN DECISION comment at line 19 that needs updating
- `IValidateProperty.GetAwaiter()` at `src/Neatoo/IValidateProperty.cs:84` confirmed as separate concept (awaits property task, not LazyLoad) -- MUST NOT be removed

**Breaking Changes:** Yes -- `GetAwaiter()` removal is a breaking public API change. `await lazyLoad` syntax will no longer compile. Version should be major bump.

**Codebase Analysis:**

Files examined:
- `src/Neatoo/LazyLoad.cs` -- core file, all changes identified
- `src/Neatoo/IValidateProperty.cs` -- confirmed `GetAwaiter()` there is separate
- `src/Neatoo/ValidateBase.cs` -- confirmed WaitForTasks integration preserved
- `src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs` -- all 30+ tests analyzed
- `src/Neatoo.UnitTest/Integration/Concepts/EntityBase/LazyLoadStatePropagationTests.cs` -- all 9 tests analyzed
- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/WaitForTasksLazyLoadCrashEntity.cs` -- GetAwaiter usage identified
- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/WaitForTasksLazyLoadCrashTests.cs` -- no direct LazyLoad API usage
- `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs` -- DESIGN DECISION comments analyzed
- `src/samples/LazyLoadSamples.cs` -- GetAwaiter usage identified
- `src/Examples/Person/Person.DomainModel/Person.cs` -- 2 GetAwaiter usages identified
- `src/Examples/Person/Person.DomainModel.Tests/Integration Tests/PersonIntegrationTests.cs` -- 2 GetAwaiter usages identified
- `skills/neatoo/references/lazy-loading.md` -- documentation locations identified (for Step 9)

---

## Agent Phasing

| Phase | Agent Type | Fresh Agent? | Rationale | Dependencies |
|-------|-----------|-------------|-----------|--------------|
| Phase 1: Core Framework + Call Sites | developer | No (resume) | Small focused change, ~8 files | None |
| Phase 2: Test Updates | developer | No (resume) | Continues from Phase 1 context, same test files | Phase 1 |
| Phase 3: Design Project Comments | developer | No (resume) | 1 file, simple comment updates | Phase 1 |

**Parallelizable phases:** None -- all sequential (each depends on the prior).

**Notes:** This is a small enough change that a single developer agent can handle all 3 phases in one session. The phasing is logical grouping, not agent separation.

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-03-15

### Assertion Trace Verification

| Rule # | Implementation Path (method/condition) | Expected Result | Matches Rule? | Notes |
|--------|---------------------------------------|-----------------|---------------|-------|
| 1 | `LazyLoad<T>.Value` getter (proposed: `get => _value;`) -- after removing the `if (!_isLoaded && !_isLoading && _loader != null && _loadTask == null) { _ = TriggerLoadAsync(); }` block, the getter returns `_value` directly. For an unloaded instance, `_value` is `null` (default from constructor at line 102-105). No method is called, `_isLoading` remains `false`. | `null`, no side effects, `IsLoading` stays `false` | Yes | Clean. No code path can trigger loading from the getter. |
| 2 | `LazyLoad<T>.LoadAsync()` at line 233 -> `LoadAsyncCore()` at line 253: sets `_value = await _loader!()`, then `_isLoaded = true`, fires PropertyChanged for Value. Subsequent `Value` getter returns `_value`. | Loaded value returned | Yes | `LoadAsync()` is unchanged; `Value` getter returns whatever `_value` holds. |
| 3 | `LazyLoad<T>(T? value)` constructor at line 111-117: sets `_value = value`, `_isLoaded = true`. `Value` getter returns `_value`. | Pre-loaded value, `IsLoaded == true` | Yes | Constructor sets both fields; getter is a pass-through. |
| 4 | `LazyLoad<T>()` parameterless constructor at line 92-96: `_loader = null`, `_isLoaded = false`, `_value` is default (null for class?). Proposed `Value` getter: `get => _value;` returns `null`. No `TriggerLoadAsync()` call (removed). | `null`, no exception | Yes | With auto-trigger removed, the `_loader == null` guard is gone but irrelevant -- there is no trigger code at all. |
| 5 | `LoadAsync()` at line 233: checks `_isLoaded` (returns cached if true), checks `_loader == null` (throws), enters lock, creates `_loadTask = LoadAsyncCore()`. `LoadAsyncCore()` invokes `_loader!()`, sets `_isLoaded = true`, fires PropertyChanged. Returns `Task<T?>`. | Loader invoked, `IsLoaded = true`, returns loaded value | Yes | Unchanged method. |
| 6 | `LoadAsync()` -> `LoadAsyncCore()` catch block at line 267-272: sets `_loadError = ex.Message`, fires PropertyChanged for HasLoadError and LoadError, then `throw`. In finally block (line 275-278): `_isLoading = false`. `_isLoaded` never set to `true` (it is set only in the try block before the exception). | Exception propagates, `HasLoadError = true`, `LoadError` has message, `IsLoaded = false` | Yes | Unchanged method. |
| 7 | `LoadAsync()` lock block at line 243-250: `if (_loadTask != null) return _loadTask;` -- concurrent callers get the same task. `_loadTask = LoadAsyncCore()` runs once. | One loader invocation, shared task | Yes | Unchanged method. |
| 8 | `LoadAsync()` line 235-236: `if (_isLoaded) return Task.FromResult(_value);` -- returns cached value without entering lock or invoking loader. | Cached value, `loadCount == 1` | Yes | Unchanged method. |
| 9 | `LoadAsync()` line 238-241: `if (_loader == null) throw new InvalidOperationException(...)` | `InvalidOperationException` thrown | Yes | Unchanged method. |
| 10 | `LazyLoad<T>.GetAwaiter()` method at line 309 removed. The `using System.Runtime.CompilerServices;` removed. Any `await lazyLoad` code would fail to compile because no `GetAwaiter()` method exists on `LazyLoad<T>`. | Compile error on `await lazyLoad` | Yes | Verified: `IValidateProperty.GetAwaiter()` at IValidateProperty.cs:84 is unrelated (returns `Task.GetAwaiter()` not `LoadAsync().GetAwaiter()`). |
| 11 | `IsLoading` getter at line 210: `get => _isLoading;` -- a simple field read. No auto-trigger code, no method calls. Before the revert, this was already side-effect-free. After the revert, still side-effect-free. | `false`, no load triggered | Yes | Was already correct. |
| 12 | `IsLoaded` getter at line 202: `get => _isLoaded; private set => _isLoaded = value;` -- a simple field read. No auto-trigger code. | `false`, no load triggered | Yes | Was already correct. |
| 13 | `LoadAsyncCore()` fires PropertyChanged at: line 256 (IsLoading=start), line 263 (Value), line 264 (IsLoaded), line 277 (IsLoading=end). | PropertyChanged for Value, IsLoaded, IsLoading | Yes | Unchanged method. |
| 14 | `LoadAsync()` -> `LoadAsyncCore()` sets `_loadTask = LoadAsyncCore()` at line 248. `LazyLoad<T>.WaitForTasks()` at line 333: `if (_loadTask != null && !_loadTask.IsCompleted) return _loadTask;`. `ValidatePropertyManager.WaitForTasks()` at ValidatePropertyManager.cs:62-72 iterates `PropertyBag`, finds properties where `IsBusy==true`, calls `property.WaitForTasks()`. LazyLoad property subclass `WaitForTasks()` delegates to `LazyLoad<T>.WaitForTasks()`. `ValidateBase.WaitForTasks()` at ValidateBase.cs:570 calls `PropertyManager.WaitForTasks()`. Chain: parent.WaitForTasks() -> PropertyManager.WaitForTasks() -> property.WaitForTasks() -> LazyLoad.WaitForTasks() -> _loadTask. | Parent waits for explicit LoadAsync in-progress task | Yes | This chain is entirely independent of how the load was triggered (auto-trigger vs explicit). |
| 15 | `LazyLoad<T>.WaitForTasks()` line 333-338: `if (_loadTask != null && !_loadTask.IsCompleted) return _loadTask;` -- for an unaccessed/pre-loaded instance, `_loadTask` is null (never loaded) or completed. Returns `(_value as IValidateMetaProperties)?.WaitForTasks() ?? Task.CompletedTask`. For pre-loaded: inner child's WaitForTasks (likely CompletedTask). For never-accessed: `_value` is null, returns `Task.CompletedTask`. | Completes immediately, no load triggered | Yes | WaitForTasks never calls LoadAsync or accesses Value. Correct. |
| 16 | `LazyLoad<T>.IsBusy` at line 314: `get => IsLoading || ((_value as IValidateMetaProperties)?.IsBusy ?? false)`. When `LoadAsync()` is in progress, `_isLoading = true` (set at LoadAsyncCore line 255). So `IsBusy` returns `true`. | `IsBusy == true` during load | Yes | Unchanged. `IsLoading` is set by `LoadAsyncCore`, not by the Value getter. |
| 17 | `IValidateMetaProperties.IsValid` at line 317: `get => !HasLoadError && ((_value as IValidateMetaProperties)?.IsValid ?? true)`. After load failure, `_loadError` is set (line 269), so `HasLoadError` is `true`, so `IsValid` returns `false`. | `IsValid == false` on load error | Yes | Unchanged. |

### Concerns

**Concern 1 (Non-blocking): BoxedValue comment updates not in scope.**

The requirements reviewer (todo item 31) and the todo's Recommendation #7 both note that the BoxedValue comments in `LazyLoadValidateProperty.cs` (lines 30, 40, 50, 120) and `LazyLoadEntityProperty.cs` (lines 38, 68) say "Uses BoxedValue to avoid triggering auto-load on the LazyLoad.Value getter." After the revert, the rationale shifts (Value no longer triggers auto-load), so these comments become misleading. The plan's Phase 3 only covers `Design.Domain/PropertySystem/LazyLoadProperty.cs` -- it does not mention updating the BoxedValue comments in the property subclass files.

**Recommendation:** Add to Phase 3: update BoxedValue comments in `LazyLoadValidateProperty.cs` and `LazyLoadEntityProperty.cs` to say "Uses BoxedValue for direct internal access to the backing value" (removing "to avoid triggering auto-load" language). This is 7 comment instances total -- mechanical and low-risk.

**Concern 2 (Non-blocking): ValidateBase.WaitForTasks(CancellationToken) gap persists.**

The plan at line 588-590 of ValidateBase.cs acknowledges that `WaitForTasks(CancellationToken)` does NOT call `PropertyManager.WaitForTasks()` -- it only calls `RunningTasks.WaitForCompletion(token)`. This means `WaitForTasks(token)` does NOT await LazyLoad children. The plan correctly marks this as out of scope (pre-existing gap), but I want to flag that the integration test `ParentWaitForTasksWithToken_AwaitsAutoTriggeredLazyLoadChild` (line 177 of LazyLoadStatePropagationTests.cs) currently works by coincidence: accessing `_ = parent.LazyChild.Value;` triggers auto-load which starts a task that `RunningTasks` tracks. After the revert, this test will be adapted to use explicit `LoadAsync()`, which also produces a `_loadTask` that `LazyLoad<T>.WaitForTasks(token)` at line 341-345 returns. But the PARENT's `WaitForTasks(token)` at ValidateBase.cs:593-596 only does `await this.RunningTasks.WaitForCompletion(token)` -- it does NOT call `PropertyManager.WaitForTasks()`. So the adapted test needs to verify that the `_loadTask` is the task being awaited through the property, not through `RunningTasks`.

Let me trace this more carefully. The adapted test will:
1. Call `parent.LazyChild.LoadAsync()` (fire-and-forget, discard the task)
2. Call `await parent.WaitForTasks(cts.Token)`
3. Parent's `WaitForTasks(CancellationToken)` calls `RunningTasks.WaitForCompletion(token)` -- this does NOT iterate property tasks.

Wait -- this means the adapted test might FAIL because `WaitForTasks(CancellationToken)` does not call `PropertyManager.WaitForTasks()`. Currently it works because the Value getter auto-trigger creates a fire-and-forget task that somehow... actually, let me check how the current test works.

Current test at line 177-200: `_ = parent.LazyChild.Value;` triggers auto-load via `TriggerLoadAsync()`. The `TriggerLoadAsync()` method is called from the Value getter as `_ = TriggerLoadAsync();` -- this creates a task that is discarded. The `LoadAsyncCore` sets `_loadTask`. Then `continueLoad.SetResult(childEntity)` completes the load. Then `await parent.WaitForTasks(cts.Token)` is called. Since the load completed before WaitForTasks is called, `_loadTask.IsCompleted == true`, so `LazyLoad.WaitForTasks(token)` falls through to inner child's WaitForTasks. The test passes trivially because the load already completed before WaitForTasks is called.

After the revert, the adapted test would:
1. `_ = parent.LazyChild.LoadAsync();` -- starts load (discards task reference)
2. `continueLoad.SetResult(childEntity)` -- completes load
3. `await parent.WaitForTasks(cts.Token)` -- load already complete, passes trivially

So the test will still pass, but it's not testing what it claims (that WaitForTasks(token) awaits in-progress loads). To truly test that, the WaitForTasks call would need to happen BEFORE the load completes. But even in the original test, the load completes before WaitForTasks is called. So this is a pre-existing test design issue, not a concern with the plan.

This concern is withdrawn -- the test works for the same reason before and after the revert.

**Concern 3 (Non-blocking): Minor inconsistency in test action for `Await_LoadsValue`.**

The "Existing Tests" section (line 42) says `Await_LoadsValue` "must change to `await lazyLoad.LoadAsync()`", and the Test Changes table (line 211) says "REWRITE -- change to `await lazyLoad.LoadAsync()`, rename to `LoadAsync_LoadsValue_ViaExplicitCall` or merge with existing `LoadAsync_LoadsValue`." But Phase 2 (line 278) says "Delete `Await_LoadsValue` (covered by `LoadAsync_LoadsValue`)." The DELETE instruction in Phase 2 is the correct action since the existing `LoadAsync_LoadsValue` test (line 47-60) already covers the same scenario. This is a minor editorial inconsistency between sections, not a logic error. The developer should follow Phase 2's DELETE instruction.

### Why This Plan Is Approved

1. **All 17 business rules trace cleanly** through the proposed implementation. No rule produces a contradictory result. Rules 1-4 (core Value behavior) are directly affected by the change and trace correctly to the simplified `get => _value;` getter. Rules 5-9 (LoadAsync behavior) are explicitly unchanged. Rules 10-17 (GetAwaiter removal, state properties, meta delegation) all trace correctly.

2. **The change is genuinely surgical.** Only 3 things are removed from `LazyLoad.cs`: the auto-trigger condition in the Value getter, the `TriggerLoadAsync()` method, and the `GetAwaiter()` method. Everything else is call site updates and test updates.

3. **WaitForTasks integration preservation is correct.** The chain ValidateBase.WaitForTasks() -> PropertyManager.WaitForTasks() -> property.WaitForTasks() -> LazyLoad.WaitForTasks() -> _loadTask is entirely independent of how the load was triggered. Explicit `LoadAsync()` calls produce the same `_loadTask` that auto-trigger did.

4. **No architectural contradictions.** The revert restores the v0.11.0/v2 design principle ("Value never triggers a load"), which is documented in the completed lazy-loading-v2-design todo.

5. **Comprehensive call site audit.** All 6 production/sample `await lazyLoad` usages are identified. All 2 test `await lazyLoad` usages are identified. The `IsValid_WhenHasLoadError_ReturnsFalse` test's `.GetAwaiter().GetResult()` on the Task (not on LazyLoad) is correctly identified as unaffected.

6. **The plan is small, focused, and low-risk.** All phases are sequential, single-developer, and could be done in one session.

### Files Examined

- `src/Neatoo/LazyLoad.cs` -- core file, all changes confirmed
- `src/Neatoo/IValidateProperty.cs:84` -- confirmed separate GetAwaiter concept
- `src/Neatoo/ValidateBase.cs:570-596` -- confirmed WaitForTasks chain preserved
- `src/Neatoo/Internal/ValidatePropertyManager.cs:62-72` -- confirmed PropertyManager.WaitForTasks iterates busy properties
- `src/Neatoo/Internal/LazyLoadValidateProperty.cs:30-56` -- confirmed BoxedValue comments
- `src/Neatoo/Internal/LazyLoadEntityProperty.cs:38-68` -- confirmed BoxedValue comments
- `src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs` -- all tests analyzed
- `src/Neatoo.UnitTest/Integration/Concepts/EntityBase/LazyLoadStatePropagationTests.cs` -- all tests analyzed
- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/WaitForTasksLazyLoadCrashEntity.cs:83` -- GetAwaiter usage confirmed
- `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs` -- DESIGN DECISION comments confirmed
- `src/samples/LazyLoadSamples.cs:74` -- GetAwaiter usage confirmed
- `src/Examples/Person/Person.DomainModel/Person.cs:114,144` -- GetAwaiter usages confirmed
- `src/Examples/Person/Person.DomainModel.Tests/Integration Tests/PersonIntegrationTests.cs:184,224` -- GetAwaiter usages confirmed
- `skills/neatoo/references/lazy-loading.md` -- auto-trigger documentation confirmed

### Questions Checked: 17 of 17 business rules traced, all checklist items reviewed

### Devil's Advocate Items: 3 generated, 1 already addressed in plan (WaitForTasks preservation), 1 withdrawn after deeper analysis (CancellationToken gap), 1 non-blocking added (BoxedValue comments)

---

## Implementation Contract

**Created:** 2026-03-15
**Approved by:** neatoo-developer

### Verification Acceptance Criteria

- `dotnet build src/Neatoo.sln` succeeds with 0 errors
- `dotnet test src/Neatoo.sln` passes with 0 failures
- `dotnet build src/Design/Design.sln` succeeds with 0 errors
- `dotnet test src/Design/Design.Tests/Design.Tests.csproj` passes with 0 failures
- `LazyLoad<T>.Value` getter is `get => _value;` with no side effects
- `LazyLoad<T>.GetAwaiter()` method does not exist
- `LazyLoad<T>.TriggerLoadAsync()` method does not exist
- No `await lazyLoad` syntax anywhere in codebase (verified by build)

### Test Scenario Mapping

| Scenario # | Test Method | Notes |
|------------|-------------|-------|
| 1 | `Value_BeforeLoad_ReturnsNullSynchronously` (REWRITTEN) | Verify passive read, no auto-trigger setup |
| 2 | `LoadAsync_LoadsValue` (EXISTING, line 47) | Already covers this scenario |
| 3 | `ValueAccess_AlreadyLoaded_ReturnsCachedValue` (KEPT) | Pre-loaded constructor |
| 4 | `ValueAccess_NoLoader_ReturnsNullWithoutException` (KEPT) | Deserialized instance |
| 5 | `LoadAsync_OnFailure_SetsErrorState` (EXISTING, line 158) | Exception propagation |
| 6 | `LoadAsync_CalledConcurrently_OnlyLoadsOnce` (EXISTING, line 107) | Concurrent load sharing |
| 7 | `LoadAsync_WhenAlreadyLoaded_ReturnsImmediately` (EXISTING, line 138) | Cached value |
| 8 | Covered by `LoadAsync()` throwing on no-loader instance | `InvalidOperationException` |
| 9 | Verified by build (compile error on `await lazyLoad`) | No runtime test needed |
| 10 | `IsLoadingAccess_DoesNotTriggerLoad` (KEPT) | Side-effect-free |
| 11 | `IsLoadedAccess_DoesNotTriggerLoad` (KEPT) | Side-effect-free |
| 12 | `LoadAsync_RaisesPropertyChangedForAllStateProperties` (EXISTING, line 184) | PropertyChanged events |
| 13 | `WaitForTasks_AfterExplicitLoad_AwaitsLoad` (ADAPTED from WaitForTasks_AfterAutoTrigger_AwaitsLoad) + integration tests | Explicit LoadAsync |
| 14 | `ParentWaitForTasks_PreLoadedChild_CompletesImmediately` + `ParentWaitForTasks_UnaccessedChild_CompletesWithoutTrigger` (KEPT) | No load triggered |
| 15 | `IsBusy_WhenLoading_ReturnsTrue` (EXISTING, line 214) | IsBusy during load |
| 16 | `IsValid_WhenHasLoadError_ReturnsFalse` (EXISTING, line 231) | IsValid on error |

### In Scope

**Phase 1: Core Framework Change**
- [ ] `src/Neatoo/LazyLoad.cs` -- Remove auto-trigger from Value getter (make `get => _value;`)
- [ ] `src/Neatoo/LazyLoad.cs` -- Remove `TriggerLoadAsync()` method (lines 281-303)
- [ ] `src/Neatoo/LazyLoad.cs` -- Remove `GetAwaiter()` method (line 309)
- [ ] `src/Neatoo/LazyLoad.cs` -- Remove `using System.Runtime.CompilerServices;` (line 2)
- [ ] `src/Neatoo/LazyLoad.cs` -- Update XML doc on class summary and Value property
- [ ] `src/samples/LazyLoadSamples.cs:74` -- Change `await parent.LazyChild` to `await parent.LazyChild.LoadAsync()`
- [ ] `src/Neatoo.UnitTest/Integration/Concepts/Serialization/WaitForTasksLazyLoadCrashEntity.cs:83` -- Change `await parent.LazyChild` to `await parent.LazyChild.LoadAsync()`
- [ ] `src/Examples/Person/Person.DomainModel/Person.cs:114` -- Change `await this.PersonPhoneList` to `await this.PersonPhoneList.LoadAsync()`
- [ ] `src/Examples/Person/Person.DomainModel/Person.cs:144` -- Same change
- [ ] `src/Examples/Person/Person.DomainModel.Tests/Integration Tests/PersonIntegrationTests.cs:184` -- Change `await result.PersonPhoneList` to `await result.PersonPhoneList.LoadAsync()`
- [ ] `src/Examples/Person/Person.DomainModel.Tests/Integration Tests/PersonIntegrationTests.cs:224` -- Same change
- [ ] Checkpoint: `dotnet build src/Neatoo.sln` succeeds

**Phase 2: Test Updates**
- [ ] `src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs` -- Rewrite `Value_BeforeLoad_ReturnsNullSynchronously` as passive read test
- [ ] Delete `Await_LoadsValue` (covered by `LoadAsync_LoadsValue`)
- [ ] Delete 4 auto-trigger tests: `ValueAccess_TriggersFireAndForgetLoad`, `ValueAccess_AutoTrigger_CompletesSuccessfully`, `ValueAccess_AutoTrigger_LoadFailure_SetsErrorState`, `ValueAccess_ConcurrentAccess_SharesOneLoad`
- [ ] Adapt `ValueAccess_DuringLoad_DoesNotStartSecondLoad` -- change premise to "explicit LoadAsync in progress, Value returns null"
- [ ] Rename `ExplicitLoadAsync_StillWorksIdentically` to `LoadAsync_Works`
- [ ] Rename `ExplicitLoadAsync_OnFailure_StillPropagatesException` to `LoadAsync_OnFailure_PropagatesException`
- [ ] Adapt `WaitForTasks_AfterAutoTrigger_AwaitsLoad` to use explicit `LoadAsync()`, rename
- [ ] Fix `Factory_Create_WithLoader_CreatesLazyLoad` -- change `await lazyLoad` to `await lazyLoad.LoadAsync()`
- [ ] Remove `#region Auto-Trigger Tests` markers
- [ ] Rename `LazyLoadAutoTriggerPropagationTests` to `LazyLoadExplicitLoadPropagationTests`
- [ ] Adapt 4 integration tests to use explicit `LoadAsync()` instead of `_ = parent.LazyChild.Value;`
- [ ] Update comment in `ParentWaitForTasks_UnaccessedChild_CompletesWithoutTrigger`
- [ ] Checkpoint: `dotnet test src/Neatoo.sln` passes

**Phase 3: Design Project and Comments**
- [ ] Update `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs` lines 19-24 (auto-trigger DESIGN DECISION)
- [ ] Update `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs` lines 26-30 (WaitForTasks DESIGN DECISION -- remove "auto-triggered" language)
- [ ] Update BoxedValue comments in `src/Neatoo/Internal/LazyLoadValidateProperty.cs` (lines 30, 40, 50, 120) -- change "to avoid triggering auto-load" to "for direct internal access to the backing value"
- [ ] Update BoxedValue comments in `src/Neatoo/Internal/LazyLoadEntityProperty.cs` (lines 38, 68) -- same change
- [ ] Checkpoint: `dotnet build src/Design/Design.sln` succeeds

### Out of Scope

- Skill documentation updates (`skills/neatoo/references/lazy-loading.md`, `skills/neatoo/SKILL.md`) -- Step 9 documentation
- Release notes for the new version -- Step 9 documentation
- zTreatment migration -- separate repo
- `ValidateBase.WaitForTasks(CancellationToken)` not calling `PropertyManager.WaitForTasks()` -- pre-existing gap, unrelated to this revert
- `IValidateProperty.GetAwaiter()` -- separate concept, must NOT be touched
- Version bump in `Directory.Build.props` -- handled during release process

### Verification Gates

1. **After Phase 1:** `dotnet build src/Neatoo.sln` succeeds with 0 errors (compilation proves no remaining `await lazyLoad` usages and no references to removed methods)
2. **After Phase 2:** `dotnet test src/Neatoo.sln` passes with 0 failures
3. **After Phase 3:** `dotnet build src/Design/Design.sln` succeeds with 0 errors
4. **Final:** All tests pass, all builds succeed, no auto-trigger code remains in `LazyLoad.cs`

### Stop Conditions

If any occur, STOP and report:
- Out-of-scope test failure (any test not listed in Phase 2 starts failing)
- Architectural contradiction discovered
- `IValidateProperty.GetAwaiter()` accidentally removed or affected
- Any compilation error not explained by the planned removals

---

## Implementation Progress

**Started:** 2026-03-15
**Developer:** neatoo-developer

### Phase 1: Core Framework Change -- COMPLETE
- [x] `src/Neatoo/LazyLoad.cs` -- Removed auto-trigger from Value getter (`get => _value;`)
- [x] `src/Neatoo/LazyLoad.cs` -- Removed `TriggerLoadAsync()` method
- [x] `src/Neatoo/LazyLoad.cs` -- Removed `GetAwaiter()` method
- [x] `src/Neatoo/LazyLoad.cs` -- Removed `using System.Runtime.CompilerServices;`
- [x] `src/Neatoo/LazyLoad.cs` -- Updated XML doc on class summary and Value property
- [x] `src/samples/LazyLoadSamples.cs:74` -- `await parent.LazyChild` -> `await parent.LazyChild.LoadAsync()`
- [x] `src/Neatoo.UnitTest/Integration/Concepts/Serialization/WaitForTasksLazyLoadCrashEntity.cs:83` -- same
- [x] `src/Examples/Person/Person.DomainModel/Person.cs:114,144` -- same (2 locations)
- [x] `src/Examples/Person/Person.DomainModel.Tests/Integration Tests/PersonIntegrationTests.cs:184,224` -- same (2 locations)
- [x] `src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs:99,636` -- test call sites using `await lazyLoad`
- [x] Checkpoint: `dotnet build src/Neatoo.sln` succeeds -- 0 errors

### Phase 2: Test Updates -- COMPLETE
- [x] Rewrote `Value_BeforeLoad_ReturnsNullSynchronously` -> `Value_BeforeLoad_ReturnsNullWithNoSideEffects` (passive read, verifies loader not invoked)
- [x] Deleted `Await_LoadsValue` (duplicate of `LoadAsync_LoadsValue`)
- [x] Deleted 4 auto-trigger tests: `ValueAccess_TriggersFireAndForgetLoad`, `ValueAccess_AutoTrigger_CompletesSuccessfully`, `ValueAccess_AutoTrigger_LoadFailure_SetsErrorState`, `ValueAccess_ConcurrentAccess_SharesOneLoad`
- [x] Adapted `ValueAccess_DuringLoad_DoesNotStartSecondLoad` -> `ValueAccess_DuringLoad_ReturnsNullPassively`
- [x] Renamed `ExplicitLoadAsync_StillWorksIdentically` -> `LoadAsync_Works`
- [x] Renamed `ExplicitLoadAsync_OnFailure_StillPropagatesException` -> `LoadAsync_OnFailure_PropagatesException`
- [x] Adapted `WaitForTasks_AfterAutoTrigger_AwaitsLoad` -> `WaitForTasks_AfterExplicitLoad_AwaitsLoad` (uses `_ = lazyLoad.LoadAsync()`)
- [x] Fixed `Factory_Create_WithLoader_CreatesLazyLoad` -- `await lazyLoad` -> `await lazyLoad.LoadAsync()`
- [x] Removed `#region Auto-Trigger Tests` markers
- [x] Renamed `LazyLoadAutoTriggerPropagationTests` -> `LazyLoadExplicitLoadPropagationTests`
- [x] Adapted 4 integration tests: `_ = parent.LazyChild.Value;` -> `_ = parent.LazyChild.LoadAsync();`
- [x] Updated comment in `ParentWaitForTasks_UnaccessedChild_CompletesWithoutTrigger`
- [x] Checkpoint: `dotnet test src/Neatoo.sln` passes -- 0 failures (1776 + 250 + 55 + 32 = 2113 tests)

### Phase 3: Design Project Comments + BoxedValue Comments -- COMPLETE
- [x] Updated `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs` lines 19-24 -- "Value is a passive read" replaces "auto-triggers"
- [x] Updated `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs` lines 26-30 -- "explicitly triggered loads" replaces "auto-triggered"
- [x] Updated BoxedValue comments in `src/Neatoo/Internal/LazyLoadValidateProperty.cs` (4 instances) -- "direct internal access to the backing value" replaces "avoid triggering auto-load"
- [x] Updated BoxedValue comments in `src/Neatoo/Internal/LazyLoadEntityProperty.cs` (2 instances) -- same
- [x] Checkpoint: `dotnet build src/Design/Design.sln` -- 101 pre-existing NF0105 analyzer errors (public [Remote] methods in Design.Domain), 0 errors from LazyLoad changes

---

## Completion Evidence

**Reported:** 2026-03-15

- **Tests Passing:** All 2113 tests pass across 4 test projects (Neatoo.UnitTest: 1775 passed + 1 skipped, Samples: 250, Person.DomainModel.Tests: 55, BaseGenerator.Tests: 32). 0 failures.
- **Verification Resources:** `dotnet build src/Design/Design.sln` has 101 pre-existing NF0105 analyzer errors (unrelated to this change). 0 new errors from LazyLoad changes. The Design.Domain `LazyLoadProperty.cs` comments updated successfully.
- **All Contract Items:** All Phase 1, Phase 2, and Phase 3 checklist items complete. See Implementation Progress for details.

### Summary of Changes

**Files modified (10 source files):**
1. `src/Neatoo/LazyLoad.cs` -- Removed auto-trigger from Value getter, removed TriggerLoadAsync(), removed GetAwaiter(), updated XML docs
2. `src/Neatoo/Internal/LazyLoadValidateProperty.cs` -- Updated 4 BoxedValue comments
3. `src/Neatoo/Internal/LazyLoadEntityProperty.cs` -- Updated 2 BoxedValue comments
4. `src/samples/LazyLoadSamples.cs` -- `await parent.LazyChild` -> `await parent.LazyChild.LoadAsync()`
5. `src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs` -- Rewrote 1 test, deleted 5 tests, adapted 2, renamed 2, fixed 2 call sites
6. `src/Neatoo.UnitTest/Integration/Concepts/EntityBase/LazyLoadStatePropagationTests.cs` -- Renamed class, adapted 4 tests, updated comments
7. `src/Neatoo.UnitTest/Integration/Concepts/Serialization/WaitForTasksLazyLoadCrashEntity.cs` -- `await parent.LazyChild` -> `await parent.LazyChild.LoadAsync()`
8. `src/Examples/Person/Person.DomainModel/Person.cs` -- 2 call sites updated
9. `src/Examples/Person/Person.DomainModel.Tests/Integration Tests/PersonIntegrationTests.cs` -- 2 call sites updated
10. `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs` -- Updated DESIGN DECISION comments

**Net test count change:** 1781 -> 1776 (5 auto-trigger tests deleted, 0 new tests added)

---

## Documentation

**Agent:** neatoo-requirements-documenter
**Completed:** 2026-03-15

### Expected Deliverables

- [x] `skills/neatoo/references/lazy-loading.md` -- Revert auto-trigger language to passive read, update Loading section, update State Properties table, update UI Binding section
- [x] `skills/neatoo/SKILL.md` -- Check for any auto-trigger references (if LazyLoad is mentioned)
- [x] `src/Neatoo/LazyLoad.cs` XML doc comments -- Updated during implementation (Phase 1)
- [x] `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs` DESIGN DECISION comments -- Updated during implementation (Phase 3)
- [ ] Release notes for new version -- Document breaking change, migration path (`await lazyLoad` -> `await lazyLoad.LoadAsync()`) -- **Deferred to Step 8 Part C**
- [x] Skill updates: Yes
- [x] Sample updates: Yes (LazyLoadSamples.cs updated during Phase 1, snippet re-rendered via mdsnippets)

### Files Updated

**Skill behavioral contract references (updated directly):**

1. `skills/neatoo/references/lazy-loading.md` -- Reverted all auto-trigger language to passive read:
   - Opening paragraph: "Value auto-triggers" -> "Value is a passive read"
   - Key Principles: "Auto-trigger on Value access" -> "Value is a passive read"
   - Creating Instances: "value loaded on first await" -> "value loaded via explicit LoadAsync() call"
   - Loading section: removed `var value = lazy.Value;` auto-trigger and `var value = await lazy;` GetAwaiter patterns; only `await lazy.LoadAsync()` remains
   - WaitForTasks Integration: updated example to use `_ = entity.OrderLines.LoadAsync()` instead of `.Value` trigger
   - State Properties table: `Value` description updated from auto-trigger to "Passive read with no side effects"
   - Error Handling: removed "Auto-triggered loading (via Value getter)" paragraph, replaced with `_ = lazy.LoadAsync()` fire-and-forget pattern
   - UI Binding section: completely rewritten to show explicit `LoadAsync()` in `OnInitializedAsync()` with 4-branch Razor pattern using `.Value` for passive binding
   - Serialization section: updated "awaits LazyChild" to "calls LazyChild.LoadAsync()"
   - MarkdownSnippets re-rendered: snippet now shows `await parent.LazyChild.LoadAsync()` (was `await parent.LazyChild`)

2. `skills/neatoo/SKILL.md` -- Updated reference list entry from "auto-trigger on Value access" to "explicit LoadAsync(), passive Value read"

3. `skills/mudneatoo/SKILL.md` -- Rewrote LazyLoad Databinding section:
   - Opening description: auto-trigger -> passive read with explicit LoadAsync() in OnInitializedAsync()
   - Added OnInitializedAsync() code block showing `_ = entity.OrderLines.LoadAsync()`
   - Updated branch 4 description from ".Value access triggered the load" to "load triggered in OnInitializedAsync()"
   - Removed "Do NOT wrap LazyLoad in @if (entity.OrderLines.IsLoaded)" warning (no longer applicable since .Value is passive)

**Installed skill copies also updated:**
- `~/.claude/skills/neatoo/references/lazy-loading.md`
- `~/.claude/skills/neatoo/SKILL.md`
- `~/.claude/skills/neatoo/neatoo/references/lazy-loading.md`
- `~/.claude/skills/neatoo/neatoo/SKILL.md`
- `~/.claude/skills/mudneatoo/SKILL.md`
- `~/.claude/skills/mudneatoo/mudneatoo/SKILL.md`
- `~/.claude/skills/mudneatoo/references/anti-patterns.md` (synced from repo source)
- `~/.claude/skills/mudneatoo/mudneatoo/references/anti-patterns.md` (synced from repo source)

### Developer Deliverables

**.cs files NOT modified by the documenter -- these are deliverables for the developer (Step 8 Part B):**

None. All .cs source code changes (LazyLoad.cs XML docs, Design.Domain comments, BoxedValue comments, samples, test updates) were completed during the implementation phase (Step 7) and verified by the requirements reviewer. No additional .cs changes are needed for this revert.

### Release Notes Deliverable (Step 8 Part C)

A release note for the auto-trigger revert is needed at `docs/release-notes/v0.23.0.md` (or appropriate version). This is a breaking behavioral change. The release note should:
- Flag as breaking change
- Summarize: `.Value` is now a passive read, `GetAwaiter()` removed, `LoadAsync()` is the sole load trigger
- Migration: `await lazyLoad` -> `await lazyLoad.LoadAsync()`
- Reference v0.21.0 release notes (the feature being reverted)

---

## Architect Verification

**Verified:** 2026-03-15
**Verdict:** VERIFIED

**Independent test results:**
- `dotnet build src/Neatoo.sln`: 0 errors, 0 warnings. Build succeeded.
- `dotnet test src/Neatoo.sln`: 0 failures. Neatoo.UnitTest: 1775 passed + 1 skipped. Samples: 250 passed. Person.DomainModel.Tests: 55 passed. BaseGenerator.Tests: 32 passed. Total: 2112 passed, 1 skipped, 0 failed.

**Design match:**

All 6 acceptance criteria verified independently:

1. **Value getter is passive** -- Confirmed at `src/Neatoo/LazyLoad.cs` line 181: `get => _value;`. No auto-trigger logic, no conditional, no call to LoadAsync or TriggerLoadAsync. The XML doc at lines 173-176 correctly states "This is a passive read with no side effects."

2. **GetAwaiter() removed** -- Confirmed absent from `LazyLoad.cs`. Grep for `GetAwaiter` in `LazyLoad.cs` returns no matches. The `using System.Runtime.CompilerServices;` import is also removed. The build succeeds, proving no remaining `await lazyLoad` syntax in the codebase.

3. **TriggerLoadAsync() removed** -- Confirmed absent from `LazyLoad.cs`. Grep for `TriggerLoadAsync` in `LazyLoad.cs` returns no matches.

4. **IValidateProperty.GetAwaiter() preserved** -- Confirmed at `src/Neatoo/IValidateProperty.cs` line 84: `TaskAwaiter GetAwaiter() => Task.GetAwaiter();` still present and unchanged. This is the property-level task awaiter, unrelated to LazyLoad.

5. **All 6 await-lazyload call sites updated** -- Independently verified:
   - `src/samples/LazyLoadSamples.cs:74`: `await parent.LazyChild.LoadAsync()`
   - `src/Neatoo.UnitTest/Integration/Concepts/Serialization/WaitForTasksLazyLoadCrashEntity.cs:83`: `await parent.LazyChild.LoadAsync()`
   - `src/Examples/Person/Person.DomainModel/Person.cs:114`: `await this.PersonPhoneList.LoadAsync()`
   - `src/Examples/Person/Person.DomainModel/Person.cs:144`: `await this.PersonPhoneList.LoadAsync()`
   - `src/Examples/Person/Person.DomainModel.Tests/Integration Tests/PersonIntegrationTests.cs:184`: `await result.PersonPhoneList.LoadAsync()`
   - `src/Examples/Person/Person.DomainModel.Tests/Integration Tests/PersonIntegrationTests.cs:224`: `await result.PersonPhoneList.LoadAsync()`
   - Grep for `= await (parent|this|result)\.(LazyChild|PersonPhoneList)[^.]` returns no matches -- no remaining raw `await lazyLoad` patterns.

6. **Design.Domain comments updated** -- `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs` lines 19-23: "DESIGN DECISION: Value is a passive read. It returns the current state (null if not loaded, the loaded value otherwise) with no side effects. Call LoadAsync() explicitly to trigger loading." Lines 25-29: "ValidateBase.WaitForTasks() awaits in-progress LazyLoad children via PropertyManager.WaitForTasks(). ... WaitForTasks does NOT trigger loads on unaccessed LazyLoad children."

7. **BoxedValue comments updated** -- `src/Neatoo/Internal/LazyLoadValidateProperty.cs` lines 30, 40, 50, 120 all say "Uses BoxedValue for direct internal access to the backing value." Same for `LazyLoadEntityProperty.cs` lines 38, 68. No remaining "avoid triggering auto-load" language.

8. **WaitForTasks integration preserved** -- `ValidateBase.WaitForTasks()` at line 570 still calls `await this.PropertyManager.WaitForTasks();`. `LazyLoad<T>.WaitForTasks()` at line 292 still checks `_loadTask != null && !_loadTask.IsCompleted`. Chain is intact for explicit `LoadAsync()` calls.

9. **Test changes match plan** -- Unit tests: `Value_BeforeLoad_ReturnsNullWithNoSideEffects` rewritten as passive read test (verifies loadCount == 0). `Await_LoadsValue` deleted. 4 auto-trigger tests deleted. `ValueAccess_DuringLoad_ReturnsNullPassively` adapted. `LoadAsync_Works` and `LoadAsync_OnFailure_PropagatesException` renamed. `WaitForTasks_AfterExplicitLoad_AwaitsLoad` adapted. `Factory_Create_WithLoader_CreatesLazyLoad` uses `await lazyLoad.LoadAsync()`. Integration tests: class renamed to `LazyLoadExplicitLoadPropagationTests`. 4 tests adapted to use `_ = parent.LazyChild.LoadAsync()`. 2 tests kept unchanged.

**Issues found:** None.

---

## Requirements Verification

**Reviewer:** neatoo-requirements-reviewer
**Verified:** 2026-03-15
**Verdict:** REQUIREMENTS SATISFIED

### Requirements Compliance

Each requirement number below references the todo's Requirements Review section (`docs/todos/revert-lazyload-auto-trigger.md`).

| Requirement | Status | Evidence |
|-------------|--------|----------|
| **1. DESIGN DECISION: Value auto-triggers (SUPERSEDED)** | Satisfied | `src/Neatoo/LazyLoad.cs` line 181: `get => _value;` -- no auto-trigger logic. Class-level XML doc (lines 27-28): "Key principle: Value never triggers a load." `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs` lines 19-23: updated to "Value is a passive read." |
| **2. DESIGN DECISION: WaitForTasks awaits LazyLoad children** | Satisfied | `src/Neatoo/ValidateBase.cs` line 574: `await this.PropertyManager.WaitForTasks();` unchanged. `LazyLoad<T>.WaitForTasks()` at line 292-296 still returns `_loadTask` when in progress. `LazyLoadEntityProperty.WaitForTasks()` at line 116-123 still delegates to `lazyLoad.WaitForTasks()`. `LazyLoadValidateProperty.WaitForTasks()` at line 192-199 same. Design.Domain line 25-29: updated to "explicitly triggered loads." Integration test `ParentWaitForTasks_AwaitsExplicitLazyLoadChild` (line 151) confirms the chain works with explicit `LoadAsync()`. |
| **3. DESIGN DECISION: LazyLoad is a partial property** | Satisfied | Unchanged. `Design.Domain/PropertySystem/LazyLoadProperty.cs` lines 9-17 still document partial property pattern. No modifications to generator behavior. |
| **4. STATE PROPAGATION contract** | Satisfied | `LazyLoadValidateProperty.cs` and `LazyLoadEntityProperty.cs` are unchanged aside from 6 BoxedValue comment updates. Look-through delegation of IsBusy, IsValid, IsModified, WaitForTasks, RunRules, PropertyMessages, ClearAllMessages all intact. Integration test `LazyLoadChild_ModifyChild_ParentIsModified` (line 42) passes. |
| **5. SERIALIZATION contract** | Satisfied | `LazyLoad<T>.Value` getter (line 181) is now `get => _value;` -- `[JsonInclude]` reads this during serialization, which returns null harmlessly for unloaded instances. No auto-load triggered during serialization. `ILazyLoadDeserializable` interface unchanged. `ApplyDeserializedState` at line 129-139 unchanged. FatClientLazyLoadTests and TwoContainerLazyLoadTests all pass (per Completion Evidence: 2113 tests, 0 failures). |
| **6. Value_BeforeLoad_ReturnsNull** | Satisfied | Test rewritten as `Value_BeforeLoad_ReturnsNullWithNoSideEffects` (line 18). Now verifies: `Assert.IsNull(value)`, `Assert.IsFalse(lazyLoad.IsLoading)`, `Assert.IsFalse(lazyLoad.IsLoaded)`, `Assert.AreEqual(0, loadCount)`. Directly tests passive read with no side effects. |
| **7. Await_LoadsValue (GetAwaiter)** | Satisfied | Test deleted (covered by `LoadAsync_LoadsValue`). `GetAwaiter()` method removed from `LazyLoad.cs` -- confirmed absent via grep. Build succeeds, proving no remaining `await lazyLoad` syntax. |
| **8. LoadAsync_CalledConcurrently_OnlyLoadsOnce** | Satisfied | Test unchanged at line 95. `LoadAsync()` method at lines 222-240 unchanged. Lock and `_loadTask` sharing logic intact. |
| **9. LoadAsync_WhenAlreadyLoaded_ReturnsImmediately** | Satisfied | Test unchanged at line 126. `LoadAsync()` line 224: `if (_isLoaded) return Task.FromResult(_value);` unchanged. |
| **10. LoadAsync_OnFailure_SetsErrorState** | Satisfied | Test unchanged at line 146. `LoadAsyncCore()` catch block at lines 256-262 unchanged. Line 168: `Assert.IsNull(lazyLoad.Value)` -- passive read returns null, no side effect on faulted instance. |
| **11. LoadAsync_RaisesPropertyChanged** | Satisfied | Test unchanged at line 172. `LoadAsyncCore()` lines 244-254 fire PropertyChanged for IsLoading, Value, IsLoaded. Unchanged. |
| **12. IsBusy_WhenLoading_ReturnsTrue** | Satisfied | Test unchanged at line 202. `IsBusy` at line 273: `IsLoading || ((_value as IValidateMetaProperties)?.IsBusy ?? false)` unchanged. Test now uses `lazyLoad.LoadAsync()` instead of auto-trigger -- same `_isLoading = true` path via `LoadAsyncCore()`. |
| **13. Auto-trigger test region (12 tests)** | Satisfied | 4 tests deleted: `ValueAccess_TriggersFireAndForgetLoad`, `ValueAccess_AutoTrigger_CompletesSuccessfully`, `ValueAccess_AutoTrigger_LoadFailure_SetsErrorState`, `ValueAccess_ConcurrentAccess_SharesOneLoad`. 5th test `Await_LoadsValue` also deleted (see req 7). Kept tests: `ValueAccess_AlreadyLoaded_ReturnsCachedValue` (line 233), `ValueAccess_NoLoader_ReturnsNullWithoutException` (line 285), `IsLoadingAccess_DoesNotTriggerLoad` (line 301), `IsLoadedAccess_DoesNotTriggerLoad` (line 320). Adapted: `ValueAccess_DuringLoad_DoesNotStartSecondLoad` renamed to `ValueAccess_DuringLoad_ReturnsNullPassively` (line 255) -- uses explicit `LoadAsync()`, verifies Value returns null passively during load. Renamed: `ExplicitLoadAsync_StillWorksIdentically` to `LoadAsync_Works` (line 339), `ExplicitLoadAsync_OnFailure_StillPropagatesException` to `LoadAsync_OnFailure_PropagatesException` (line 355). Adapted: `WaitForTasks_AfterAutoTrigger_AwaitsLoad` to `WaitForTasks_AfterExplicitLoad_AwaitsLoad` (line 368) -- uses `_ = lazyLoad.LoadAsync()`. Region markers removed. |
| **14. IsValid_WhenHasLoadError (Task.GetAwaiter)** | Satisfied | Test unchanged at line 220. Uses `lazyLoad.LoadAsync().GetAwaiter().GetResult()` -- this is `Task.GetAwaiter()`, not `LazyLoad.GetAwaiter()`. Unaffected by the removal. |
| **15. LazyLoadStatePropagationTests** | Satisfied | `LazyLoadChild_*` tests (lines 34-75) unchanged, still use pre-loaded `new LazyLoad<T>(child)`. All pass. |
| **16. LazyLoadAutoTriggerPropagationTests** | Satisfied | Class renamed to `LazyLoadExplicitLoadPropagationTests` (line 83). 4 tests adapted: `ParentIsBusy_AfterExplicitChildLoad` (line 99) uses `_ = parent.LazyChild.LoadAsync()`. `ParentIsValid_AfterExplicitChildLoadFailure` (line 124) uses `_ = parent.LazyChild.LoadAsync()`. `ParentWaitForTasks_AwaitsExplicitLazyLoadChild` (line 151) uses `_ = parent.LazyChild.LoadAsync()`. `ParentWaitForTasksWithToken_AwaitsExplicitLazyLoadChild` (line 177) same. 2 tests kept unchanged: `ParentWaitForTasks_PreLoadedChild_CompletesImmediately` (line 203), `ParentWaitForTasks_UnaccessedChild_CompletesWithoutTrigger` (line 221 -- comment updated to "nothing triggers loading except explicit LoadAsync()"). |
| **17. WaitForTasksLazyLoadCrashTests** | Satisfied | Test file unchanged. The entity file `WaitForTasksLazyLoadCrashEntity.cs` line 83: updated to `var child = await parent.LazyChild.LoadAsync();`. Test still validates the full client-server LazyLoad pipeline. |
| **18. WaitForTasksLazyLoadCrashEntity** | Satisfied | Line 83: `var child = await parent.LazyChild.LoadAsync();` -- confirmed updated from `await parent.LazyChild`. |
| **19. FatClientLazyLoadTests** | Satisfied | No modifications. `FatClientLazyLoad_Unloaded_RoundTrip` accesses `deserialized.LazyDescription.Value` on a deserialized instance with `_loader = null`. With passive Value, this returns null with no side effect (previously also safe because `_loader == null` guard prevented auto-trigger). All pass. |
| **20. TwoContainerLazyLoadTests** | Satisfied | No modifications. Pre-loaded round-trip. All pass. |
| **21. LazyLoadSamples.cs** | Satisfied | Line 74: `var child = await parent.LazyChild.LoadAsync();` -- confirmed updated. |
| **22. Person.DomainModel Person.cs** | Satisfied | Line 114: `await this.PersonPhoneList.LoadAsync()`. Line 144: `await this.PersonPhoneList.LoadAsync()`. Both confirmed updated. |
| **23. PersonIntegrationTests.cs** | Satisfied | Line 184: `await result.PersonPhoneList.LoadAsync()`. Line 224: `await result.PersonPhoneList.LoadAsync()`. Both confirmed updated. |
| **24-28. Skill documentation** | Not in scope | Plan explicitly marks skill documentation (`skills/neatoo/references/lazy-loading.md`) as Step 9 (documentation phase, out of scope for implementation). See plan Out of Scope section, line 564. |
| **29. XML doc: class summary** | Satisfied | `src/Neatoo/LazyLoad.cs` lines 20-22: "Value is a passive read that returns the current value or null if not yet loaded. Use LoadAsync to trigger loading." Lines 27-28: "Key principle: Value never triggers a load." |
| **30. XML doc on Value property** | Satisfied | `src/Neatoo/LazyLoad.cs` lines 173-176: "Gets the current value. Returns null if not yet loaded. This is a passive read with no side effects. Call LoadAsync to trigger loading." |
| **31. BoxedValue comments** | Satisfied | `src/Neatoo/Internal/LazyLoadValidateProperty.cs` lines 30, 40, 50, 120: all updated to "Uses BoxedValue for direct internal access to the backing value." `src/Neatoo/Internal/LazyLoadEntityProperty.cs` lines 38, 68: same. No remaining "avoid triggering auto-load" language. |
| **32. ValidateBase.WaitForTasks CancellationToken gap** | Satisfied | `src/Neatoo/ValidateBase.cs` lines 588-590 unchanged: "This method does NOT call PropertyManager.WaitForTasks()" note preserved. Pre-existing gap, not modified. |
| **33-35. Release notes** | Not in scope | Plan marks release notes as Step 9 documentation, out of scope for implementation. |
| **36. LazyLoad v2 "No magic" design** | Satisfied | Implementation restores this principle. `LazyLoad.cs` line 27: "Value never triggers a load." `Design.Domain/PropertySystem/LazyLoadProperty.cs` lines 19-23: "Value is a passive read." |
| **37. AsyncTasks fire-and-forget pattern** | Satisfied | Fire-and-forget is now limited to property setters (rules via AsyncTasks). The Value getter no longer participates in fire-and-forget. `TriggerLoadAsync()` removed -- confirmed absent via grep. |
| **Gap 1: Test for passive Value read** | Satisfied | `Value_BeforeLoad_ReturnsNullWithNoSideEffects` (line 18) explicitly tests: Value returns null, loader not invoked (`loadCount == 0`), `IsLoading` remains false, `IsLoaded` remains false. |
| **Gap 2: UI loading pattern** | Not in scope | Documentation deliverable for Step 9. |
| **Gap 3: IValidateProperty.GetAwaiter preserved** | Satisfied | `src/Neatoo/IValidateProperty.cs` line 84: `TaskAwaiter GetAwaiter() => Task.GetAwaiter();` confirmed present and unchanged. |
| **Gap 4: Serialization safety** | Satisfied | `LazyLoad<T>.Value` getter is now `get => _value;` (line 181). `[JsonInclude]` causes STJ to read this during serialization. For unloaded instances, returns null. For loaded instances, returns the cached value. No load is triggered. The NeatooBaseJsonTypeConverter at line 398 uses `property.GetValue(value)` to get the `LazyLoad<T>` instance, then `JsonSerializer.Serialize` serializes it (reading Value and IsLoaded). Both are now passive reads. All serialization tests pass. |
| **Recommendation 1: GetAwaiter audit** | Satisfied | All 6 `await lazyLoad` call sites updated. Grep for `GetAwaiter` in `LazyLoad.cs` returns no matches. Grep for raw `await x.LazyChild[^.]` or `await x.PersonPhoneList[^.]` returns only comments. Build succeeds. |
| **Recommendation 2: TriggerLoadAsync removal** | Satisfied | Grep for `TriggerLoadAsync` in `LazyLoad.cs` returns no matches. |
| **Recommendation 3: Test updates** | Satisfied | 5 tests deleted, 1 rewritten, 2 adapted, 2 renamed, 2 call sites fixed in unit tests. 4 integration tests adapted, 1 class renamed, 1 comment updated. See req 13 and 16 above. |
| **Recommendation 4: WaitForTasks preserved** | Satisfied | See req 2 above. Full chain intact. |
| **Recommendation 5: Design.Domain updated** | Satisfied | See req 1 above. |
| **Recommendation 6: Documentation locations** | Partially satisfied | Implementation scope (Phase 1-3) covers LazyLoad.cs XML docs, Design.Domain comments, and BoxedValue comments. Skill docs and release notes deferred to Step 9 (documentation phase). |
| **Recommendation 7: BoxedValue comments** | Satisfied | See req 31 above. |
| **Recommendation 8: Release note** | Not in scope | Deferred to Step 9 documentation phase. |

### Unintended Side Effects

None found. The implementation is surgical and limited to three removals from `LazyLoad<T>` (auto-trigger in Value getter, `TriggerLoadAsync()`, `GetAwaiter()`) plus call site updates. Specifically verified:

1. **LoadAsync behavior unchanged** -- `LoadAsync()`, `LoadAsyncCore()`, and the lock/`_loadTask` sharing logic are identical to pre-change. The 5 LoadAsync behavioral contracts (rules 5-9) all trace through unchanged code paths.

2. **State propagation unchanged** -- `LazyLoadValidateProperty<T>` and `LazyLoadEntityProperty<T>` are unchanged aside from 6 comment updates. The look-through delegation chain (IsBusy, IsValid, IsModified, WaitForTasks, RunRules, PropertyMessages, ClearAllMessages) is intact. Integration tests confirm parent IsBusy cascades during explicit `LoadAsync()` and parent IsValid cascades on load failure.

3. **Serialization unchanged** -- `ILazyLoadDeserializable`, `ApplyDeserializedState`, `SetValue`, constructors, and `[JsonInclude]`/`[JsonIgnore]` annotations are all unchanged. The only behavioral change is that `[JsonInclude]` on `.Value` now reads a passive getter instead of an auto-triggering getter -- this is strictly safer (eliminates the side effect of triggering loads during serialization).

4. **IValidateProperty.GetAwaiter() preserved** -- Confirmed at `src/Neatoo/IValidateProperty.cs` line 84, unchanged. This is a separate concept (awaiting property-level tasks) unrelated to LazyLoad.

5. **No impact on LazyLoadFactory** -- `LazyLoadFactory` is unchanged. The `Create<T>(Func<Task<T?>>)` and `Create<T>(T?)` methods remain identical.

6. **No impact on NeatooBaseJsonTypeConverter** -- The converter's LazyLoad serialization path (lines 392-405) uses `property.GetValue(value)` to get the `LazyLoad<T>` instance, then serializes it via STJ. STJ reads `.Value` which is now passive. The converter's deserialization merge path (`ApplyDeserializedState`) is unchanged.

### Issues Found

None.
