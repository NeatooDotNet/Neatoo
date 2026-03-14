# LazyLoad Auto-Trigger on Value Access

**Date:** 2026-03-13
**Related Todo:** [LazyLoad Auto-Trigger on Value Access](../todos/lazyload-auto-trigger-on-value-access.md)
**Status:** Verified
**Last Updated:** 2026-03-13 (requirements verification complete)

---

## Overview

Modify `LazyLoad<T>.Value` getter to auto-trigger `LoadAsync()` as fire-and-forget when accessed and the load hasn't started. Only the `Value` getter triggers -- `IsLoading`, `IsLoaded`, and all other state properties remain side-effect-free. This eliminates the manual `await lazyLoad` boilerplate that was consistently needed in Blazor Razor databinding.

Additionally, modify `ValidateBase.WaitForTasks()` to await in-progress LazyLoad children, so that `await entity.WaitForTasks()` before Save ensures any auto-triggered loads have completed.

---

## Business Requirements Context

**Source:** [Todo Requirements Review](../todos/lazyload-auto-trigger-on-value-access.md#requirements-review)

### Relevant Existing Requirements

#### Behavioral Contracts (from unit tests)

- **REQ-1** `LazyLoadTests.Value_BeforeLoad_ReturnsNull` (`src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs:17`): WHEN Value accessed before load, THEN Value is null. **Semantic intent changes** -- currently tests "no side effect." After the change, Value still returns null synchronously, but it now has the side effect of triggering a fire-and-forget load.

- **REQ-2** `LazyLoadTests.IsLoading_DuringLoad_ReturnsTrue` (line 57): WHEN LoadAsync called, THEN IsLoading is true until completion. Unchanged -- auto-trigger uses the same LoadAsync/LoadAsyncCore path.

- **REQ-3** `LazyLoadTests.LoadAsync_CalledConcurrently_OnlyLoadsOnce` (line 100): WHEN multiple concurrent LoadAsync calls, THEN only one load executes. Auto-triggered loads must share the same task via `_loadLock` and `_loadTask`.

- **REQ-4** `LazyLoadTests.LoadAsync_WhenAlreadyLoaded_ReturnsImmediately` (line 131): WHEN already loaded, THEN LoadAsync returns cached value. Value getter must check `_isLoaded` before triggering.

- **REQ-5** `LazyLoadTests.LoadAsync_RaisesPropertyChangedForAllStateProperties` (line 177): WHEN load completes, THEN PropertyChanged fires for Value, IsLoaded, IsLoading. Same LoadAsyncCore path, identical events.

- **REQ-6** `LazyLoadTests.IsBusy_WhenLoading_ReturnsTrue` (line 209): WHEN loading in progress, THEN IsBusy is true. The auto-triggered load uses the same path.

- **REQ-7** `LazyLoadTests.LoadAsync_OnFailure_SetsErrorState` (line 151): WHEN loader throws, THEN HasLoadError=true, LoadError has message, IsLoaded=false. The fire-and-forget path must catch exceptions to prevent unobserved task exceptions.

#### State Propagation Contracts (from integration tests)

- **REQ-8** `LazyLoadStatePropagationTests` (`src/Neatoo.UnitTest/Integration/Concepts/EntityBase/LazyLoadStatePropagationTests.cs`): Parent entity's IsModified, IsSelfModified, and IsSavable cascade correctly from LazyLoad children. Not directly affected (tests use pre-loaded LazyLoad).

- **REQ-9** `WaitForTasksLazyLoadCrashTests` (`src/Neatoo.UnitTest/Integration/Concepts/Serialization/WaitForTasksLazyLoadCrashTests.cs`): WaitForTasks surfaces results of LazyLoad children loaded via AddActionAsync. Not directly affected -- the explicit `await` path in AddActionAsync remains unchanged.

#### Meta Property Delegation Contracts

- **REQ-10** `ValidateBase.IsBusy` includes `IsAnyLazyLoadChildBusy()` (`src/Neatoo/ValidateBase.cs:174`). When auto-trigger starts a load, `LazyLoad.IsBusy` becomes true (via `IsLoading`), which cascades to parent's `IsBusy`.

- **REQ-11** `ValidateBase.IsValid` includes `IsAllLazyLoadChildrenValid()` (`src/Neatoo/ValidateBase.cs:280`). If an auto-triggered load fails, `HasLoadError=true` makes `LazyLoad.IsValid=false`, cascading to parent's `IsValid=false`.

#### Serialization Contracts

- **REQ-12** `FatClientLazyLoad_PostDeserialization_LoadAsync_Throws` (`src/Neatoo.UnitTest/Integration/Concepts/Serialization/FatClientLazyLoadTests.cs:168`): WHEN LazyLoad deserialized without loader AND LoadAsync called, THEN throws InvalidOperationException. Auto-trigger from Value getter must NOT throw when `_loader == null`.

#### Documentation Contracts

- **REQ-13** Seven documentation locations state "Value never triggers a load" (see todo Contradiction 1 table). All must be updated.

#### Design Precedent

- **REQ-14** The old `Property<T>.OnLoad` system had exactly this fire-and-forget-on-access pattern (`docs/todos/completed/property-backing-fields.md:92-94`). LazyLoad v2 removed it for "no magic" clarity. This change returns to the proven pattern on a dedicated wrapper type.

- **REQ-15** `AsyncTasks` design rationale (`docs/todos/completed/async-tasks-design-rationale.md`): Neatoo's established pattern is fire-and-forget async work from synchronous code paths, with `WaitForTasks()` as the rendezvous point. The Value getter auto-trigger follows this pattern.

### Gaps

**Gap 1: Unobserved task exception handling.**
The fire-and-forget `LoadAsync()` from the Value getter discards the task. If the loader throws, the exception becomes unobserved. The LoadAsyncCore `catch` block sets `HasLoadError`/`LoadError` before rethrowing. The auto-trigger path must catch the outer exception to prevent unobserved task exceptions, relying on `HasLoadError`/`IsValid=false` for error surfacing.

**Gap 2: WaitForTasks integration.**
The parent's `WaitForTasks()` awaits `RunningTasks.AllDone` and `PropertyManager.WaitForTasks()`. It does NOT await LazyLoad children's `_loadTask`. The parent includes LazyLoad children in `IsBusy` via polling (`IsAnyLazyLoadChildBusy`), but `IsBusy` is a boolean -- it cannot be awaited. When a Razor page calls `await entity.WaitForTasks()` before Save, any auto-triggered LazyLoad that is still in progress must be awaited so the save does not proceed with incomplete data.

**Decision:** Add `WaitForLazyLoadChildren()` to `ValidateBase.WaitForTasks()`. This follows the same `GetLazyLoadProperties(GetType())` pattern as `IsAnyLazyLoadChildBusy()` and `IsAllLazyLoadChildrenValid()`. Both `WaitForTasks()` overloads (parameterless and `CancellationToken`) must include this. Auto-triggered loads do NOT register with `RunningTasks`/`AddChildTask` -- the WaitForTasks method polls LazyLoad children directly, same as IsBusy and IsValid.

**Gap 3: Null loader guard.**
Deserialized `LazyLoad<T>` via the parameterless JSON constructor has `_loader == null`. Current `LoadAsync()` throws `InvalidOperationException`. The Value getter auto-trigger must guard against `_loader == null` and return `_value` (which is null) without triggering.

**Gap 4: New test coverage.**
No existing tests verify "accessing Value triggers load." New tests are needed.

### Contradictions

**Contradiction 1: "Value never triggers a load" -- INTENTIONAL REVERSAL.**
The todo explicitly proposes changing this behavior based on real-world usage feedback. The owner confirmed the design intent. Seven documentation locations must be updated. See todo for the complete list.

**Contradiction 2: Test `Value_BeforeLoad_ReturnsNull` semantic intent.**
The test's assertion (Value is null) will still pass, but its semantic intent changes. The test should be split: one for "Value returns null synchronously on first access" and one for "Value access triggers fire-and-forget load."

### Recommendations for Architect

1. Catch exceptions in the fire-and-forget path, relying on HasLoadError/IsValid=false.
2. Guard against `_loader == null` in the auto-trigger.
3. Add `WaitForLazyLoadChildren()` to `ValidateBase.WaitForTasks()` so parent entities await in-progress LazyLoad children.
4. Update all 7 documentation locations.
5. Review and update Design.Domain sample file.
6. Test IsBusy cascading after Value access triggers load.
7. Test WaitForTasks cascading -- parent.WaitForTasks() should await auto-triggered LazyLoad loads.

---

## Business Rules (Testable Assertions)

1. WHEN `Value` getter accessed on a LazyLoad with a loader AND not loaded AND not currently loading, THEN a fire-and-forget `LoadAsync()` is triggered. The `Value` getter returns `null` synchronously. -- Source: NEW (Gap 4)

2. WHEN `Value` getter accessed on a LazyLoad that is already loaded (`_isLoaded == true`), THEN no load is triggered AND the cached value is returned. -- Source: REQ-4

3. WHEN `Value` getter accessed on a LazyLoad that is currently loading (`_loadTask != null && !_loadTask.IsCompleted`), THEN no additional load is triggered AND `null` is returned (the in-progress load will update Value via PropertyChanged). -- Source: REQ-3

4. WHEN `Value` getter accessed on a LazyLoad with `_loader == null` (deserialized without loader), THEN no load is triggered AND `null` is returned AND no exception is thrown. -- Source: NEW (Gap 3), REQ-12

5. WHEN the fire-and-forget load triggered by `Value` getter completes successfully, THEN `Value` holds the loaded data AND `IsLoaded == true` AND `IsLoading == false` AND PropertyChanged fires for Value, IsLoaded, IsLoading. -- Source: REQ-5

6. WHEN the fire-and-forget load triggered by `Value` getter fails (loader throws), THEN `HasLoadError == true` AND `LoadError` contains the exception message AND `IsValid == false` AND no unobserved task exception escapes. -- Source: NEW (Gap 1), REQ-7

7. WHEN `Value` getter is accessed concurrently from multiple callers before load starts, THEN only one load operation executes (existing `_loadLock` and `_loadTask` deduplication applies). -- Source: REQ-3

8. WHEN `IsLoading` or `IsLoaded` properties are accessed, THEN no load is triggered (they remain pure state checks). -- Source: NEW (user design decision)

9. WHEN `Value` getter triggers a fire-and-forget load on a LazyLoad that is a child of an entity, THEN the parent entity's `IsBusy` returns `true` while the load is in progress (via `IsAnyLazyLoadChildBusy()` polling). -- Source: REQ-10

10. WHEN `Value` getter triggers a fire-and-forget load that fails, THEN the parent entity's `IsValid` returns `false` (via `IsAllLazyLoadChildrenValid()` polling). -- Source: REQ-11

11. WHEN `LoadAsync()` is called explicitly (not via Value getter), THEN existing behavior is unchanged: it returns a Task that can be awaited, exceptions propagate to the caller, and the full load lifecycle is identical. -- Source: REQ-2, REQ-5, REQ-6, REQ-7

12. WHEN `WaitForTasks()` is called on a LazyLoad instance after auto-trigger has started a load, THEN it returns the `_loadTask` and the caller can await load completion. -- Source: REQ-15 (fire-and-forget with rendezvous pattern)

13. WHEN `WaitForTasks()` is called on a parent entity (ValidateBase) that has a LazyLoad child with an in-progress load (auto-triggered or explicit), THEN `WaitForTasks()` awaits the LazyLoad child's load task before completing. -- Source: NEW (Gap 2 resolution, user-scoped requirement)

14. WHEN `WaitForTasks(CancellationToken)` is called on a parent entity that has a LazyLoad child with an in-progress load, THEN `WaitForTasks` awaits the LazyLoad child's load task with the cancellation token before completing. -- Source: NEW (Gap 2 resolution, consistent with Rule 13)

15. WHEN `WaitForTasks()` is called on a parent entity whose LazyLoad children have no in-progress loads (either not started, already loaded, or no loader), THEN `WaitForTasks()` completes without error and without triggering any loads. -- Source: NEW (safety invariant -- WaitForTasks must not trigger loads)

### Test Scenarios

| # | Scenario | Inputs / State | Rule(s) | Expected Result |
|---|----------|---------------|---------|-----------------|
| 1 | Value access triggers fire-and-forget load | LazyLoad with loader, not loaded | Rule 1, 5 | Value is null synchronously; after awaiting WaitForTasks, Value holds loaded data |
| 2 | Value access on already-loaded instance | LazyLoad pre-loaded with value "hello" | Rule 2 | Returns "hello" immediately, no load triggered, loader invoke count stays 0 |
| 3 | Value access during in-progress load | LazyLoad with slow loader, load started via explicit LoadAsync | Rule 3 | Returns null, no second load triggered, loader invoke count stays 1 |
| 4 | Value access on deserialized instance (no loader) | `new LazyLoad<T>()` (parameterless constructor) | Rule 4 | Returns null, no exception thrown, IsLoading remains false |
| 5 | Auto-triggered load completes successfully | LazyLoad with loader returning TestValue("loaded") | Rule 1, 5 | After WaitForTasks: Value is TestValue("loaded"), IsLoaded=true, IsLoading=false; PropertyChanged fired for Value, IsLoaded, IsLoading |
| 6 | Auto-triggered load fails | LazyLoad with loader that throws InvalidOperationException("fail") | Rule 6 | After await Task.Delay/WaitForTasks: HasLoadError=true, LoadError="fail", IsValid=false, no unobserved exception |
| 7 | Concurrent Value accesses share one load | LazyLoad with loader, three concurrent .Value accesses from different threads | Rule 7 | Only one loader invocation, all share same _loadTask |
| 8 | IsLoading access does not trigger load | LazyLoad with loader, not loaded; access IsLoading | Rule 8 | IsLoading returns false, no load triggered |
| 9 | IsLoaded access does not trigger load | LazyLoad with loader, not loaded; access IsLoaded | Rule 8 | IsLoaded returns false, no load triggered |
| 10 | Parent IsBusy cascading after auto-trigger | Entity with LazyLoad child; access child's Value | Rule 9 | Parent IsBusy returns true while child load in progress |
| 11 | Parent IsValid cascading after auto-trigger failure | Entity with LazyLoad child; loader throws | Rule 10 | Parent IsValid returns false after load fails |
| 12 | Explicit LoadAsync still works identically | LazyLoad with loader; call LoadAsync() and await | Rule 11 | Same behavior as before: returns loaded value, exceptions propagate |
| 13 | WaitForTasks on LazyLoad after auto-trigger | LazyLoad with slow loader; access Value; call WaitForTasks on the LazyLoad instance | Rule 12 | WaitForTasks awaits the load task, completes when load finishes |
| 14 | Parent WaitForTasks awaits auto-triggered LazyLoad child | Entity with LazyLoad child; access child's Value (triggers load); call parent.WaitForTasks() | Rule 13 | Parent WaitForTasks completes only after LazyLoad child's load finishes; child Value is populated |
| 15 | Parent WaitForTasks with CancellationToken awaits LazyLoad child | Entity with LazyLoad child; access child's Value (triggers load); call parent.WaitForTasks(token) | Rule 14 | Parent WaitForTasks awaits child load; cancellation token is respected |
| 16 | Parent WaitForTasks with no in-progress LazyLoad children | Entity with pre-loaded LazyLoad child; call parent.WaitForTasks() | Rule 15 | WaitForTasks completes immediately without triggering any loads |
| 17 | Parent WaitForTasks with unloaded LazyLoad child (no auto-trigger yet) | Entity with LazyLoad child that has never been accessed; call parent.WaitForTasks() | Rule 15 | WaitForTasks completes without triggering a load on the LazyLoad child (Value was not accessed, so no load started) |

---

## Approach

The change is surgical: modify the `Value` getter in `LazyLoad<T>` to check if a load should be triggered, and add a private method that wraps `LoadAsync()` with exception handling for the fire-and-forget path.

The core principle: the `Value` getter remains synchronous (returns `T?`). The auto-trigger starts `LoadAsync()` as a side effect but does not await it. The existing `PropertyChanged` events from `LoadAsyncCore()` drive UI re-rendering when the load completes.

### Design Decision: Exception Handling in Fire-and-Forget Path

`LoadAsyncCore()` catches exceptions, sets `HasLoadError`/`LoadError`, raises `PropertyChanged`, and then **rethrows**. For the explicit `LoadAsync()` path, this rethrow propagates to the caller. For the auto-trigger path, the rethrow would become an unobserved task exception.

Solution: Add a private `TriggerLoadAsync()` method that wraps `LoadAsync()` in a try-catch, catching any exception (which has already been recorded in `HasLoadError`/`LoadError` by `LoadAsyncCore`). This method is called fire-and-forget from the Value getter. The caller sees errors via `HasLoadError`/`IsValid=false`.

### Design Decision: WaitForTasks Awaits LazyLoad Children (No RunningTasks Registration)

Auto-triggered loads do NOT register with `RunningTasks`/`AddChildTask`. Instead, `ValidateBase.WaitForTasks()` directly polls and awaits LazyLoad children's tasks, following the same `GetLazyLoadProperties(GetType())` pattern used by `IsAnyLazyLoadChildBusy()` and `IsAllLazyLoadChildrenValid()`.

Rationale for polling instead of RunningTasks registration:
1. LazyLoad properties are not part of `PropertyManager` -- they are regular C# properties discovered via reflection. Registering with `RunningTasks`/`AddChildTask` would require `LazyLoad<T>` to know about its parent entity, which it currently does not. The polling pattern is consistent with all existing LazyLoad state propagation (IsBusy, IsValid, IsModified).
2. The polling approach is simple and idempotent -- `WaitForTasks` on a LazyLoad with no `_loadTask` returns `Task.CompletedTask`.
3. `WaitForTasks()` must NOT trigger loads on unaccessed LazyLoad children. It only awaits loads that are already in progress. This preserves the principle that only the `Value` getter triggers loading.

---

## Domain Model Behavioral Design

This change is to the framework's `LazyLoad<T>` infrastructure class, not to a domain model entity. No domain model behavioral properties are introduced.

The change enables a new Razor binding pattern where `Model.OrderLines.Value` in a Razor expression auto-triggers the load, and Blazor re-renders when `PropertyChanged` fires on completion.

---

## Design

### File: `src/Neatoo/LazyLoad.cs`

#### Change 1: Value getter auto-trigger

Current:
```csharp
public T? Value
{
    get => _value;
    private set => _value = value;
}
```

Proposed:
```csharp
public T? Value
{
    get
    {
        if (!_isLoaded && !_isLoading && _loader != null && _loadTask == null)
        {
            _ = TriggerLoadAsync();
        }
        return _value;
    }
    private set => _value = value;
}
```

The guard conditions:
- `!_isLoaded` -- skip if already loaded (Rule 2)
- `!_isLoading` -- skip if load in progress (Rule 3)
- `_loader != null` -- skip if no loader (deserialized instance) (Rule 4)
- `_loadTask == null` -- skip if task already exists (covers race between `_isLoading` being set asynchronously and the lock acquisition in `LoadAsync`)

#### Change 2: TriggerLoadAsync method

```csharp
private async Task TriggerLoadAsync()
{
    try
    {
        await LoadAsync();
    }
    catch
    {
        // Exception already recorded in HasLoadError/LoadError by LoadAsyncCore.
        // Swallow here to prevent unobserved task exception.
        // Error surfaces via HasLoadError=true, IsValid=false, PropertyChanged events.
    }
}
```

#### Change 3: XML doc updates on LazyLoad class

Update the class-level XML doc (line 27) and the Value property XML doc (line 140) to reflect the new behavior.

### File: `src/Neatoo/ValidateBase.cs`

#### Change 4: WaitForTasks awaits LazyLoad children

Current (`ValidateBase.cs:663-668`):
```csharp
public virtual async Task WaitForTasks()
{
    await this.RunningTasks.AllDone;
    // Also wait for property-level tasks (e.g., lazy loading)
    await this.PropertyManager.WaitForTasks();
}
```

Proposed:
```csharp
public virtual async Task WaitForTasks()
{
    await this.RunningTasks.AllDone;
    await this.PropertyManager.WaitForTasks();
    await WaitForLazyLoadChildren();
}
```

#### Change 5: WaitForTasks(CancellationToken) awaits LazyLoad children

Current (`ValidateBase.cs:681-684`):
```csharp
public virtual async Task WaitForTasks(CancellationToken token)
{
    await this.RunningTasks.WaitForCompletion(token);
}
```

Proposed:
```csharp
public virtual async Task WaitForTasks(CancellationToken token)
{
    await this.RunningTasks.WaitForCompletion(token);
    await WaitForLazyLoadChildren(token);
}
```

#### Change 6: Add WaitForLazyLoadChildren methods

Add two private methods following the same pattern as `IsAnyLazyLoadChildBusy()` (`ValidateBase.cs:347-358`):

```csharp
/// <summary>
/// Awaits any in-progress LazyLoad children's tasks.
/// Does NOT trigger loads on unaccessed LazyLoad children.
/// Returns immediately when there are no LazyLoad properties or none have tasks.
/// </summary>
private async Task WaitForLazyLoadChildren()
{
    var props = GetLazyLoadProperties(GetType());
    if (props.Length == 0) return;

    foreach (var prop in props)
    {
        if (prop.GetValue(this) is IValidateMetaProperties vmp)
        {
            await vmp.WaitForTasks();
        }
    }
}

/// <summary>
/// Awaits any in-progress LazyLoad children's tasks with cancellation support.
/// </summary>
private async Task WaitForLazyLoadChildren(CancellationToken token)
{
    var props = GetLazyLoadProperties(GetType());
    if (props.Length == 0) return;

    foreach (var prop in props)
    {
        if (prop.GetValue(this) is IValidateMetaProperties vmp)
        {
            await vmp.WaitForTasks(token);
        }
    }
}
```

This follows the exact same pattern as `IsAnyLazyLoadChildBusy()` and `IsAllLazyLoadChildrenValid()`:
- Uses `GetLazyLoadProperties(GetType())` for cached reflection lookup
- Casts to `IValidateMetaProperties` (which `LazyLoad<T>` implements)
- Calls `WaitForTasks()` on each, which returns `_loadTask` if in progress or `Task.CompletedTask` if not

**Critical invariant:** `WaitForLazyLoadChildren()` calls `vmp.WaitForTasks()` on the LazyLoad instance, NOT `vmp.Value`. It does not access the `Value` property, so it does NOT trigger auto-loading. It only awaits loads that are already in progress.

### Files to update for documentation (REQ-13):

1. `src/Neatoo/LazyLoad.cs` lines 27 and 140 -- XML docs
2. `skills/neatoo/references/lazy-loading.md` lines 3, 7, 145 -- Skill docs
3. `docs/release-notes/v0.11.0.md` line 35 -- Release notes (add a note that this was changed in a later release)
4. `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs` -- Design reference comments
5. `src/samples/LazyLoadSamples.cs` -- Update any comments referencing "never triggers"

Completed todo files (`docs/todos/completed/lazy-loading-v2-design.md`) should NOT be modified -- they are historical records.

### Test files:

1. `src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs` -- Update `Value_BeforeLoad_ReturnsNull` test (split into two tests: sync return value test and auto-trigger test). Add new tests for scenarios 1-9, 12-13.
2. `src/Neatoo.UnitTest/Integration/Concepts/EntityBase/LazyLoadStatePropagationTests.cs` -- Add tests for scenarios 10-11, 14-17 (IsBusy/IsValid cascading and WaitForTasks cascading).

---

## Implementation Steps

1. **Modify `LazyLoad<T>.Value` getter** -- Add the auto-trigger guard and call `TriggerLoadAsync()`.
2. **Add `TriggerLoadAsync()` method** -- Private async method that wraps `LoadAsync()` with exception swallowing.
3. **Update XML docs** -- Class-level and Value property docs in `LazyLoad.cs`.
4. **Modify `ValidateBase.WaitForTasks()`** -- Add `await WaitForLazyLoadChildren()` after existing awaits.
5. **Modify `ValidateBase.WaitForTasks(CancellationToken)`** -- Add `await WaitForLazyLoadChildren(token)` after existing await.
6. **Add `WaitForLazyLoadChildren()` methods** -- Two private methods (parameterless and CancellationToken) following the `IsAnyLazyLoadChildBusy()` pattern.
7. **Update/split existing test** -- `Value_BeforeLoad_ReturnsNull` should be updated to account for the new auto-trigger behavior.
8. **Add new unit tests** -- Cover scenarios 1-9, 12-13 in `LazyLoadTests.cs`.
9. **Add integration tests** -- Cover scenarios 10-11 (IsBusy/IsValid cascading) and 14-17 (WaitForTasks cascading) in `LazyLoadStatePropagationTests.cs`.
10. **Update skill documentation** -- `skills/neatoo/references/lazy-loading.md`.
11. **Update Design.Domain reference** -- `LazyLoadProperty.cs` comments.
12. **Update samples** -- `LazyLoadSamples.cs` comments.
13. **Update release notes** -- Add note to `v0.11.0.md` about behavioral change, or create a new release note for the version containing this change.

---

## Acceptance Criteria

- [ ] `LazyLoad<T>.Value` getter auto-triggers `LoadAsync()` when unloaded, not loading, and loader is present
- [ ] `Value` returns null synchronously on first access (before load completes)
- [ ] No exception escapes the fire-and-forget path; errors surface via `HasLoadError`/`IsValid=false`
- [ ] `IsLoading`/`IsLoaded` access does NOT trigger a load
- [ ] Deserialized instance with no loader (`_loader == null`) does not throw from Value getter
- [ ] Concurrent Value accesses share one load operation
- [ ] Parent entity `IsBusy` cascading works after Value-triggered load
- [ ] Parent entity `IsValid` cascading works after Value-triggered load failure
- [ ] Parent entity `WaitForTasks()` awaits in-progress LazyLoad children
- [ ] Parent entity `WaitForTasks(CancellationToken)` awaits in-progress LazyLoad children with cancellation
- [ ] Parent entity `WaitForTasks()` does NOT trigger loads on unaccessed LazyLoad children
- [ ] All existing tests pass (or are updated with equivalent behavioral coverage)
- [ ] New tests cover all 17 scenarios
- [ ] XML docs, skill docs, Design.Domain comments, and samples updated
- [ ] `dotnet build src/Neatoo.sln` succeeds
- [ ] `dotnet test src/Neatoo.sln` succeeds

---

## Dependencies

- No external dependencies. Changes are self-contained to `LazyLoad<T>` and `ValidateBase<T>`.
- RemoteFactory is not affected -- LazyLoad is not processed by source generators.

---

## Risks / Considerations

1. **Blazor rendering loops.** If a Razor component accesses `Value` during render, auto-trigger starts a load, PropertyChanged fires on completion, re-render accesses `Value` again. This is safe because the second access finds `_isLoaded == true` and returns immediately. No infinite loop.

2. **Side effect from a property getter.** Property getters with side effects are unconventional. However, this is a framework wrapper type specifically designed for async lazy loading in UI binding scenarios. The side effect is well-contained (fire-and-forget load) and the pattern has precedent in the old `Property<T>.OnLoad` system.

3. **Thread safety of the guard condition.** The Value getter checks `!_isLoaded && !_isLoading && _loader != null && _loadTask == null` without locks. This is acceptable because:
   - Neatoo objects are single-threaded per object graph (see CLAUDE-DESIGN.md threading guarantees)
   - The `LoadAsync()` method itself uses `_loadLock` for thread-safe task deduplication
   - Worst case of a race: two calls both pass the guard, both call `LoadAsync()`, which internally deduplicates via the lock

4. **Pre-existing Design.sln build failures.** The Design.sln has NF0105 errors from a separate analyzer enforcement issue. These are unrelated to this change. Design project verification will need to work within this constraint.

---

## Architectural Verification

**Scope Table:**

| Feature | Current Support | After Change | Verified |
|---------|----------------|-------------|----------|
| Value getter returns current state | Yes | Yes (still returns _value synchronously) | Verified (existing test `Value_BeforeLoad_ReturnsNull`) |
| Value getter triggers load | No | Yes (fire-and-forget when unloaded) | Needs Implementation |
| IsLoading/IsLoaded no side effects | Yes | Yes (unchanged) | Needs new tests |
| Null loader guard | N/A (Value had no side effect) | Required | Needs Implementation |
| Exception handling in fire-and-forget | N/A | Required | Needs Implementation |
| Concurrent load deduplication | Yes (in LoadAsync) | Yes (unchanged, guards + lock) | Verified (existing test `LoadAsync_CalledConcurrently_OnlyLoadsOnce`) |
| IsBusy cascading from LazyLoad | Yes | Yes (unchanged, via IsAnyLazyLoadChildBusy polling) | Verified (existing code at ValidateBase.cs:174) |
| IsValid cascading from LazyLoad | Yes | Yes (unchanged, via IsAllLazyLoadChildrenValid polling) | Verified (existing code at ValidateBase.cs:280) |
| WaitForTasks on LazyLoad | Yes (returns _loadTask) | Yes (unchanged) | Verified (existing code at LazyLoad.cs:262-267) |
| Parent WaitForTasks awaits LazyLoad children | No | Yes (new WaitForLazyLoadChildren method) | Needs Implementation |
| Parent WaitForTasks(CancellationToken) awaits LazyLoad children | No | Yes (new WaitForLazyLoadChildren(token) method) | Needs Implementation |
| PropertyChanged events on load | Yes | Yes (same LoadAsyncCore path) | Verified (existing test `LoadAsync_RaisesPropertyChangedForAllStateProperties`) |

**Verification Evidence:**

Design.sln has pre-existing NF0105 errors unrelated to this change. The LazyLoad feature is verified through unit tests in `src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs` and integration tests in `src/Neatoo.UnitTest/Integration/`. The core framework (`src/Neatoo/Neatoo.csproj`) compiles clean.

- Value getter behavior: Verified (existing code at `LazyLoad.cs:143-147`)
- LoadAsync thread safety: Verified (existing code at `LazyLoad.cs:186-204` with `_loadLock`)
- LoadAsyncCore exception handling: Verified (existing code at `LazyLoad.cs:219-226` sets `_loadError`, raises PropertyChanged, rethrows)
- IsBusy cascading: Verified (existing code at `ValidateBase.cs:174`, `ValidateBase.cs:347-358`)
- IsValid cascading: Verified (existing code at `ValidateBase.cs:280`, `ValidateBase.cs:330-341`)
- WaitForTasks on LazyLoad: Verified (existing code at `LazyLoad.cs:262-267`)
- Parent WaitForTasks: Verified (existing code at `ValidateBase.cs:663-668`) -- currently does NOT await LazyLoad children; new `WaitForLazyLoadChildren()` method to be added
- Parent WaitForTasks(CancellationToken): Verified (existing code at `ValidateBase.cs:681-684`) -- same gap, needs update
- WaitForLazyLoadChildren pattern: Verified (follows identical pattern to `IsAnyLazyLoadChildBusy()` at `ValidateBase.cs:347-358`)

**Breaking Changes:** No -- the `Value` getter still returns the same type (`T?`). The behavioral change (triggering a load) is additive. The `WaitForTasks()` change is also additive -- it now awaits LazyLoad children that were previously ignored. This cannot break existing code because previously, any code that relied on LazyLoad children being complete after `WaitForTasks()` was already broken (it just happened to work if the load was fast enough). Existing code that calls `LoadAsync()` explicitly will work identically.

**Codebase Analysis:**

Files examined:
- `src/Neatoo/LazyLoad.cs` -- Implementation target (323 lines)
- `src/Neatoo/ValidateBase.cs` -- IsBusy/IsValid cascading via LazyLoad polling (lines 174, 280, 316-384); WaitForTasks (lines 663-684)
- `src/Neatoo/EntityBase.cs` -- IsModified cascading via LazyLoad polling (lines 153, 159-170)
- `src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs` -- Unit tests (522 lines)
- `src/Neatoo.UnitTest/Integration/Concepts/EntityBase/LazyLoadStatePropagationTests.cs` -- Integration tests (75 lines)
- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/WaitForTasksLazyLoadCrashTests.cs` -- WaitForTasks crash tests (65 lines)
- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/WaitForTasksLazyLoadCrashEntity.cs` -- Test entity (122 lines)
- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/FatClientLazyLoadTests.cs` -- Serialization tests (lines 155-184)
- `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs` -- Design reference (147 lines)
- `src/Design/Design.Tests/TestInfrastructure.cs` -- Design test infrastructure (220 lines)
- `skills/neatoo/references/lazy-loading.md` -- Skill documentation (221 lines)
- `src/samples/LazyLoadSamples.cs` -- Code samples (193 lines)
- `docs/release-notes/v0.11.0.md` -- Release notes (198 lines)
- `docs/todos/completed/async-tasks-design-rationale.md` -- Fire-and-forget precedent (136 lines)

---

## Agent Phasing

| Phase | Agent Type | Fresh Agent? | Rationale | Dependencies |
|-------|-----------|-------------|-----------|--------------|
| Phase 1: Core implementation | developer | Yes | Modify LazyLoad.cs (Value getter + TriggerLoadAsync), ValidateBase.cs (WaitForTasks + WaitForLazyLoadChildren), and update XML docs. | None |
| Phase 2: Tests | developer | No | Resume from Phase 1. Update existing test, add new unit tests and integration tests. Needs to see the implementation to write correct tests. | Phase 1 |
| Phase 3: Documentation | developer | Yes | Update skill docs, Design.Domain comments, samples, release notes. Independent of test results. | Phase 1 |

**Parallelizable phases:** Phase 2 and Phase 3 could run in parallel after Phase 1 completes.

**Notes:** All three phases touch different file sets. Phase 1 and 2 are best done sequentially by the same agent since the test structure depends on the exact implementation. Phase 3 is purely documentation updates.

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-03-13

### My Understanding of This Plan

**Core Change:** Modify `LazyLoad<T>.Value` getter to auto-trigger `LoadAsync()` as fire-and-forget when accessed on an unloaded instance with a loader. Additionally, modify `ValidateBase.WaitForTasks()` to await in-progress LazyLoad children.

**User-Facing API:** `Value` getter now has a side effect (triggers load). `IsLoading`/`IsLoaded` remain side-effect-free. `WaitForTasks()` on parent entities now awaits LazyLoad children. No new API surface -- existing properties gain new behavior.

**Internal Changes:** (1) `LazyLoad<T>.Value` getter gets an auto-trigger guard + `TriggerLoadAsync()` wrapper. (2) `ValidateBase.WaitForTasks()` and `WaitForTasks(CancellationToken)` gain `WaitForLazyLoadChildren()` calls that poll and await LazyLoad children's tasks.

**Base Classes Affected:** `ValidateBase<T>` (WaitForTasks changes). `EntityBase<T>` inherits this change. `LazyLoad<T>` is a standalone class, not a base class.

### Codebase Investigation

**Files Examined:**
- `src/Neatoo/LazyLoad.cs` (323 lines) -- Verified current Value getter at line 145 (`get => _value`), LoadAsync at line 186-204 with `_loadLock` deduplication, LoadAsyncCore at lines 206-232 with catch-rethrow pattern, WaitForTasks at lines 262-275 returning `_loadTask` when in-progress.
- `src/Neatoo/ValidateBase.cs` (1103 lines) -- Verified WaitForTasks at lines 663-668 (parameterless) and 681-684 (CancellationToken). Verified IsBusy at line 174 includes `IsAnyLazyLoadChildBusy()`. Verified IsValid at line 280 includes `IsAllLazyLoadChildrenValid()`. Verified `GetLazyLoadProperties` at lines 316-324 and polling pattern at lines 330-358.
- `src/Neatoo/IMetaProperties.cs` -- Verified `IValidateMetaProperties` includes `WaitForTasks()` and `WaitForTasks(CancellationToken)`.
- `src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs` (523 lines) -- Verified all existing tests: Value_BeforeLoad_ReturnsNull (line 17), IsLoading_DuringLoad_ReturnsTrue (line 57), LoadAsync_CalledConcurrently_OnlyLoadsOnce (line 100), LoadAsync_WhenAlreadyLoaded_ReturnsImmediately (line 131), LoadAsync_OnFailure_SetsErrorState (line 151), LoadAsync_RaisesPropertyChangedForAllStateProperties (line 177), IsBusy_WhenLoading_ReturnsTrue (line 209).
- `src/Neatoo.UnitTest/Integration/Concepts/EntityBase/LazyLoadStatePropagationTests.cs` (75 lines) -- Verified tests use pre-loaded LazyLoad (`new LazyLoad<ILazyLoadEntityObject>(child)`) not auto-trigger path.
- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/WaitForTasksLazyLoadCrashTests.cs` (65 lines) -- Verified crash test uses explicit `await parent.LazyChild` via AddActionAsync, not auto-trigger.
- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/WaitForTasksLazyLoadCrashEntity.cs` (122 lines) -- Verified CrashParent pattern with constructor-based LazyLoad creation.
- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/FatClientLazyLoadTests.cs` (line 168-182) -- Verified test `FatClientLazyLoad_PostDeserialization_LoadAsync_Throws` creates `new LazyLoad<string>()` (no loader) and verifies LoadAsync throws.
- `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs` (147 lines) -- Verified design comments, no "Value never triggers" explicit statement in this file.
- `skills/neatoo/references/lazy-loading.md` (221 lines) -- Verified lines 3 and 7 ("Explicit loading only"), line 145 ("Never triggers a load").
- `src/samples/LazyLoadSamples.cs` (193 lines) -- Verified code samples. No explicit "never triggers" comments in code, but the pattern shows explicit `await parent.LazyChild` usage.
- `docs/release-notes/v0.11.0.md` (line 35) -- Verified ".Value property never triggers a load".

**Searches Performed:**
- Searched for `GetLazyLoadProperties` -- found usages in ValidateBase.cs at lines 316-324 (static method), 332 (IsAllLazyLoadChildrenValid), 349 (IsAnyLazyLoadChildBusy), 373 (SubscribeToLazyLoadProperties). Also found in EntityBase.cs (IsModified cascading). Pattern is consistent.
- Searched for `_loadTask` in LazyLoad.cs -- found at lines 47 (field), 198 (null check in LoadAsync), 199 (return), 201 (assign from LoadAsyncCore), 264-265 (WaitForTasks returns), 272-273 (WaitForTasks(token) returns). The field is `Task<T?>?`, assigned only in `LoadAsync()` lock block.
- Searched for "WaitForTasks" in ValidateBase.cs -- lines 663-668 (parameterless: RunningTasks.AllDone + PropertyManager.WaitForTasks), lines 681-684 (CancellationToken: RunningTasks.WaitForCompletion only -- DOES NOT call PropertyManager.WaitForTasks).

**Design Project Verification:**
- The architect noted pre-existing NF0105 errors in Design.sln. This is a known constraint.
- The "Verified" scope items reference existing test files and source code, which I confirmed exist at the stated locations.
- The "Needs Implementation" items correctly describe features that do not yet exist in the codebase.
- The plan does not claim Design.sln compilation evidence (the architect noted the pre-existing errors). Given that LazyLoad is verified through unit tests rather than Design project compilation, and the architect documented this constraint in the Architectural Verification section, this is acceptable.

**Discrepancies Found:**
- **Minor inconsistency in WaitForTasks(CancellationToken)**: The parameterless `WaitForTasks()` awaits both `RunningTasks.AllDone` AND `PropertyManager.WaitForTasks()`. The `WaitForTasks(CancellationToken)` overload ONLY awaits `RunningTasks.WaitForCompletion(token)` -- it does NOT await `PropertyManager.WaitForTasks()`. This is a pre-existing inconsistency, not introduced by this plan. However, the plan proposes adding `WaitForLazyLoadChildren(token)` to the CancellationToken overload. This is fine -- it makes the CancellationToken overload more complete rather than less. But worth noting that the CancellationToken overload is already inconsistent with the parameterless overload regarding PropertyManager.

### Assertion Trace Verification

| Rule # | Implementation Path (method/condition) | Expected Result | Matches Rule? | Notes |
|--------|---------------------------------------|-----------------|---------------|-------|
| 1 | `LazyLoad<T>.Value` getter: condition `!_isLoaded && !_isLoading && _loader != null && _loadTask == null` triggers `_ = TriggerLoadAsync()`. Returns `_value` (null) synchronously. | Value returns null; fire-and-forget load starts | Yes | Guard conditions correctly cover all four states. `TriggerLoadAsync` calls `LoadAsync()` which enters `LoadAsyncCore()`. |
| 2 | `LazyLoad<T>.Value` getter: condition `!_isLoaded` is false when `_isLoaded == true`. Short-circuits, returns `_value` (cached). | Returns cached value, no load triggered | Yes | `_isLoaded` is set to true in `LoadAsyncCore()` line 215. |
| 3 | `LazyLoad<T>.Value` getter: condition `!_isLoading` is false when `_isLoading == true` (load in progress). Returns `_value` (null). Also `_loadTask == null` fails when task exists. | Returns null, no additional load triggered | Yes | Either `_isLoading` or `_loadTask != null` would prevent trigger. Belt-and-suspenders. |
| 4 | `LazyLoad<T>.Value` getter: condition `_loader != null` is false when `_loader == null` (deserialized). Returns `_value` (null). | Returns null, no exception | Yes | `_loader` is null from parameterless `[JsonConstructor]` at line 87-91. Guard prevents `LoadAsync()` call, which would throw. |
| 5 | `TriggerLoadAsync()` calls `LoadAsync()` which calls `LoadAsyncCore()`. `LoadAsyncCore` sets `_value`, `_isLoaded=true`, fires PropertyChanged for Value, IsLoaded, IsLoading. | Value updated, IsLoaded=true, IsLoading=false, PropertyChanged events fire | Yes | Same path as explicit `LoadAsync()`. Events fire at lines 216-217 (Value, IsLoaded) and 230 (IsLoading). |
| 6 | `TriggerLoadAsync()` wraps `LoadAsync()` in try-catch. `LoadAsyncCore()` catch block (line 220-225) sets `_loadError=ex.Message`, fires PropertyChanged for HasLoadError/LoadError, then rethrows. `TriggerLoadAsync()` catches the rethrown exception (line 235-240), preventing unobserved task exception. | HasLoadError=true, LoadError set, IsValid=false via `!HasLoadError` (line 246), no unobserved exception | Yes | Exception is caught at two levels: error state set in LoadAsyncCore, exception swallowed in TriggerLoadAsync. |
| 7 | `LazyLoad<T>.Value` getter: multiple callers pass the guard and call `TriggerLoadAsync()`, which calls `LoadAsync()`. `LoadAsync()` uses `lock(_loadLock)` (line 196) with `_loadTask != null` check (line 198). Only first caller creates `_loadTask`; subsequent callers get same task. | Only one load executes | Yes | The lock in `LoadAsync()` guarantees single execution. Second callers to `TriggerLoadAsync` get same `_loadTask` from `LoadAsync()`. |
| 8 | `IsLoading` property (line 163): `get => _isLoading`. `IsLoaded` property (line 153-156): `get => _isLoaded`. Neither has any side-effect code. | No load triggered | Yes | Pure field reads, no trigger logic. |
| 9 | After Value getter triggers load, `LazyLoad<T>.IsBusy` (line 243) returns `IsLoading || ...`. `IsLoading` is true during load. Parent's `IsBusy` (ValidateBase line 174) includes `IsAnyLazyLoadChildBusy()` which polls LazyLoad children via `GetLazyLoadProperties()` and checks `vmp.IsBusy`. | Parent IsBusy = true while child loading | Yes | Existing polling mechanism handles this without code changes. |
| 10 | After Value getter triggers a failing load, `LazyLoad<T>.IsValid` (line 246) returns `!HasLoadError && ...`. `HasLoadError` is true. Parent's `IsValid` (ValidateBase line 280) includes `IsAllLazyLoadChildrenValid()` which polls LazyLoad children and checks `vmp.IsValid`. | Parent IsValid = false | Yes | Existing polling mechanism handles this without code changes. |
| 11 | Explicit `LoadAsync()` is unchanged -- same method at line 186-204. Returns `Task<T?>`, exceptions propagate to caller, full lifecycle identical. | Unchanged behavior | Yes | `TriggerLoadAsync()` is a separate private method that wraps `LoadAsync()`. The public `LoadAsync()` API is untouched. |
| 12 | `LazyLoad<T>.WaitForTasks()` (line 262-267): returns `_loadTask` when `_loadTask != null && !_loadTask.IsCompleted`. After auto-trigger, `_loadTask` is assigned in `LoadAsync()` line 201. | WaitForTasks returns the in-progress load task | Yes -- with caveat | See Concern 1 below regarding faulted `_loadTask`. |
| 13 | `ValidateBase.WaitForTasks()` (proposed): calls `WaitForLazyLoadChildren()` which uses `GetLazyLoadProperties()` to find LazyLoad children, casts to `IValidateMetaProperties`, calls `vmp.WaitForTasks()`. `LazyLoad.WaitForTasks()` returns `_loadTask`. Parent awaits it. | Parent WaitForTasks awaits child load | Yes -- with caveat | See Concern 1 below regarding faulted `_loadTask`. |
| 14 | `ValidateBase.WaitForTasks(CancellationToken)` (proposed): calls `WaitForLazyLoadChildren(token)` which calls `vmp.WaitForTasks(token)`. `LazyLoad.WaitForTasks(token)` (line 270-274) returns `_loadTask.WaitAsync(token)`. | Parent WaitForTasks(token) awaits child load with cancellation | Yes | Same pattern as Rule 13 but with cancellation. |
| 15 | `WaitForLazyLoadChildren()` calls `vmp.WaitForTasks()` on each LazyLoad child. `LazyLoad.WaitForTasks()` (line 262-267): if `_loadTask == null` or `_loadTask.IsCompleted`, returns `(_value as IValidateMetaProperties)?.WaitForTasks() ?? Task.CompletedTask`. For unloaded LazyLoad, `_loadTask` is null and `_value` is null, so returns `Task.CompletedTask`. | WaitForTasks completes without triggering load | Yes | Calling `vmp.WaitForTasks()` does NOT access `Value`, so no auto-trigger. For unaccessed LazyLoad, `_loadTask` is null, returns CompletedTask. |

### Concerns

**Concern 1 (Non-Blocking): Faulted `_loadTask` propagation through WaitForTasks**

When an auto-triggered load fails, `LoadAsyncCore()` catches the exception, sets `HasLoadError`/`LoadError`, then **rethrows**. This means `_loadTask` (the `Task<T?>` assigned in `LoadAsync()`) becomes a **faulted** task. The `TriggerLoadAsync()` wrapper catches this to prevent an unobserved task exception on the discarded `_ = TriggerLoadAsync()` task. Good.

However, `LazyLoad<T>.WaitForTasks()` returns `_loadTask` (line 264-265). If a parent's `WaitForLazyLoadChildren()` calls `vmp.WaitForTasks()` on a LazyLoad whose auto-triggered load **already failed**, it returns `_loadTask` which is faulted. But wait -- `_loadTask.IsCompleted` is `true` for faulted tasks, so the condition `_loadTask != null && !_loadTask.IsCompleted` at line 264 would be **false**. It would fall through to `(_value as IValidateMetaProperties)?.WaitForTasks() ?? Task.CompletedTask`, which returns `Task.CompletedTask` since `_value` is null after a failed load. So: **no exception propagation through WaitForTasks for already-faulted loads.** This is correct behavior.

If the parent calls `WaitForTasks()` while the load is **still in progress** and it then fails, the parent awaits `_loadTask`, which will throw when it completes as faulted. The `TriggerLoadAsync()` catch only catches on the fire-and-forget discard path, not on `_loadTask` itself.

**Recommendation:** This is actually acceptable behavior -- if you await a task and it faults, you should see the exception. The plan should document this edge case explicitly: "If `WaitForTasks()` is called while an auto-triggered load is in progress AND the load fails, the exception propagates through `WaitForTasks()`. Callers should handle this, or use `HasLoadError`/`IsValid` to check state after `WaitForTasks()` catches the exception." The developer should wrap the `await vmp.WaitForTasks()` call in a try-catch in `WaitForLazyLoadChildren()` if the desired behavior is to swallow exceptions and rely on error state. Alternatively, if exceptions should propagate through WaitForTasks (consistent with how RunningTasks works), document it.

**Decision needed from the plan:** Should `WaitForLazyLoadChildren()` swallow exceptions from failed LazyLoad children (relying on HasLoadError/IsValid), or should they propagate through the parent's `WaitForTasks()`? The current design does not explicitly address this. Both are valid. I recommend swallowing exceptions in `WaitForLazyLoadChildren()` because:
- The fire-and-forget design intent is "errors surface via state properties, not exceptions"
- Auto-triggered loads are initiated without the user's explicit request, so throwing from `WaitForTasks()` would be surprising
- The explicit `LoadAsync()` path already propagates exceptions for users who want that behavior

However, this is a design question for the architect/user, not a blocking concern. The implementation can proceed either way. I am flagging it so the developer knows to handle this edge case explicitly rather than discovering it during testing.

**Concern 2 (Non-Blocking): Pre-existing inconsistency in `WaitForTasks(CancellationToken)` overload**

The parameterless `WaitForTasks()` awaits `RunningTasks.AllDone` AND `PropertyManager.WaitForTasks()`. The `WaitForTasks(CancellationToken)` overload only awaits `RunningTasks.WaitForCompletion(token)` -- it does NOT await `PropertyManager.WaitForTasks()`. This pre-existing inconsistency means the CancellationToken overload is already less thorough than the parameterless version. The plan adds `WaitForLazyLoadChildren(token)` to the CancellationToken overload, which makes it more complete. This is fine, but the developer should be aware this inconsistency exists and consider whether `PropertyManager.WaitForTasks()` should also be added to the CancellationToken overload as part of this work (or flagged as out-of-scope).

**Recommendation:** Flag as out-of-scope for this change but note in the plan. The developer should NOT try to fix this pre-existing inconsistency as part of this task.

### What Looks Good

- The guard condition `!_isLoaded && !_isLoading && _loader != null && _loadTask == null` is comprehensive and handles all edge cases correctly.
- The `TriggerLoadAsync()` wrapper pattern cleanly separates the fire-and-forget exception handling from the reusable `LoadAsync()` path.
- The `WaitForLazyLoadChildren()` pattern correctly follows the established `IsAnyLazyLoadChildBusy()`/`IsAllLazyLoadChildrenValid()` pattern for consistency.
- The critical invariant that `WaitForLazyLoadChildren()` calls `vmp.WaitForTasks()` (not `vmp.Value`) is correctly stated, preventing accidental load triggers from WaitForTasks.
- The plan correctly identifies that `_loadTask.IsCompleted` is true for faulted tasks, so the WaitForTasks check handles already-failed loads correctly.
- All 17 test scenarios are well-defined with specific inputs and expected results.
- The agent phasing is practical: Phase 1 (core) must precede Phase 2 (tests), Phase 3 (docs) is independent.
- Risk analysis regarding Blazor rendering loops is correct -- second access finds `_isLoaded == true`.
- Thread safety analysis is sound -- Neatoo's single-threaded object model plus `_loadLock` deduplication handles worst-case races.

### Test Scenario Verification

All 17 test scenarios mentally traced against the proposed implementation:

| TS | Result | Notes |
|----|--------|-------|
| 1 | Pass | Value getter triggers TriggerLoadAsync, returns null. WaitForTasks awaits _loadTask. |
| 2 | Pass | _isLoaded=true short-circuits guard. Returns cached value. |
| 3 | Pass | _isLoading=true and _loadTask!=null both prevent re-trigger. |
| 4 | Pass | _loader==null prevents trigger. No exception from getter. |
| 5 | Pass | Same LoadAsyncCore path fires all PropertyChanged events. |
| 6 | Pass | TriggerLoadAsync catches rethrown exception. HasLoadError set by LoadAsyncCore. Note: unobserved exception test should verify TaskScheduler.UnobservedTaskException. |
| 7 | Pass | _loadLock + _loadTask deduplication in LoadAsync handles this. |
| 8 | Pass | IsLoading/IsLoaded are pure field reads. |
| 9 | Pass | IsLoading=true cascades via IsAnyLazyLoadChildBusy polling. |
| 10 | Pass | HasLoadError=true cascades via IsAllLazyLoadChildrenValid polling. |
| 11 | Pass | LoadAsync() API unchanged. |
| 12 | Pass | WaitForTasks returns _loadTask. |
| 13 | Pass | WaitForLazyLoadChildren calls vmp.WaitForTasks which returns _loadTask. See Concern 1 for failure case. |
| 14 | Pass | Same as 13 with CancellationToken. |
| 15 | Pass | _loadTask null, returns CompletedTask. |
| 16 | Pass | _isLoaded=true, _loadTask.IsCompleted=true, falls through to value WaitForTasks. |
| 17 | Pass | _loadTask null (never accessed), returns CompletedTask. No Value access = no trigger. |

### Requirements Context Check

All design decisions are consistent with the documented requirements:
- REQ-1 through REQ-15 are correctly addressed in the Business Requirements Context.
- Gaps 1-4 are all addressed in the design.
- Contradiction 1 (intentional reversal) is handled correctly -- 7 documentation locations identified for update.
- Contradiction 2 (test semantic intent) is handled correctly -- plan calls for test split/update.

### Devil's Advocate Analysis

**Edge cases NOT explicitly covered:**
1. What if `TriggerLoadAsync()` is called from the Value getter during JSON deserialization (when entity is paused)? Answer: Safe because `_loader` would be null on the parameterless constructor path, and the ApplyDeserializedState path sets `_isLoaded=true` for loaded values. No auto-trigger during deserialization.
2. What if the loader itself accesses `this.Value` recursively? Answer: The `_loadTask != null` guard in the Value getter prevents re-entry after the first trigger. The `_loadLock` in `LoadAsync()` provides additional safety. No infinite loop.
3. What happens to the `TestValue("loaded")` assertion in `Value_BeforeLoad_ReturnsNull` after the change? Answer: The test accesses `Value`, which now triggers a fire-and-forget load. Value still returns null synchronously. The load runs in the background. The test assertion still passes, but the test is now semantically different. The plan correctly identifies this and proposes splitting the test.

**Ways this could break existing functionality:**
1. The `Value_BeforeLoad_ReturnsNull` test may have timing issues if the fire-and-forget load completes before the test's next line. With a synchronous `Task.FromResult` loader, the load could complete immediately on the same synchronization context. However, `LoadAsyncCore` is `async`, so even with `Task.FromResult`, the continuation runs after the first `await`, meaning `_value` is still null at the point of return from the getter. Safe.

**Ways users could misunderstand the API:**
1. Users might expect `Value` to block until loaded (it does not -- returns null immediately). The plan's Razor pattern example clarifies this correctly.

### Why This Plan Is Approved

Despite the two non-blocking concerns above, this plan is exceptionally clear:
1. Every business rule is traceable through specific code paths with line numbers.
2. The guard condition in the Value getter is comprehensive and correct.
3. The TriggerLoadAsync wrapper pattern is clean and well-reasoned.
4. WaitForLazyLoadChildren follows the established polling pattern used by IsBusy/IsValid.
5. All 17 test scenarios trace correctly through the implementation.
6. The architect documented the Design.sln constraint (pre-existing NF0105 errors) rather than silently ignoring it.
7. The risk analysis is thorough (Blazor loops, side-effect getter, thread safety).
8. The pre-existing inconsistency in WaitForTasks(CancellationToken) is a separate issue and should not block this work.

The only real question (Concern 1) is whether `WaitForLazyLoadChildren` should swallow exceptions from faulted LazyLoad children. This can be decided during implementation without changing the plan's structure. My recommendation is to swallow them, but either approach works.

### Recommendation

**Approved.** Proceed to implementation. The developer should decide during Phase 1 whether `WaitForLazyLoadChildren()` swallows exceptions from faulted LazyLoad children or lets them propagate. I recommend swallowing with a try-catch, consistent with the fire-and-forget error-surfacing design (HasLoadError/IsValid).

---

## Implementation Contract

**Created:** 2026-03-13
**Approved by:** neatoo-developer

### Verification Acceptance Criteria

- All 15 business rules pass assertion trace verification (see table above).
- All 17 test scenarios have corresponding test methods that pass.
- `dotnet build src/Neatoo/Neatoo.csproj` succeeds after Phase 1.
- `dotnet test src/Neatoo.UnitTest/Neatoo.UnitTest.csproj` succeeds after Phase 2.
- `dotnet test src/Neatoo.sln` succeeds after Phase 3.
- All existing tests continue to pass (or are updated with equivalent behavioral coverage, preserving original intent).

### Implementation Decision: WaitForLazyLoadChildren Exception Handling

The developer must decide during Phase 1 how `WaitForLazyLoadChildren()` handles exceptions from faulted LazyLoad children. Recommended approach: wrap `await vmp.WaitForTasks()` in a try-catch that swallows exceptions, consistent with the fire-and-forget error-surfacing design (errors surface via `HasLoadError`/`IsValid=false`, not exceptions through `WaitForTasks`). If the developer chooses to let exceptions propagate instead, add a test scenario for it and document the behavior.

### Test Scenario Mapping

| Scenario # | Test File | Test Method (suggested name) | Notes |
|------------|-----------|------------------------------|-------|
| 1 | LazyLoadTests.cs | ValueAccess_TriggersFireAndForgetLoad | Unit test: verify Value returns null, then WaitForTasks yields loaded value |
| 2 | LazyLoadTests.cs | ValueAccess_AlreadyLoaded_ReturnsCachedValue | Unit test: pre-load, verify no second load |
| 3 | LazyLoadTests.cs | ValueAccess_DuringLoad_DoesNotStartSecondLoad | Unit test: start explicit load, access Value, verify single load |
| 4 | LazyLoadTests.cs | ValueAccess_NoLoader_ReturnsNullWithoutException | Unit test: parameterless constructor, access Value |
| 5 | LazyLoadTests.cs | ValueAccess_AutoTrigger_CompletesSuccessfully | Unit test: verify post-load state (IsLoaded, PropertyChanged) |
| 6 | LazyLoadTests.cs | ValueAccess_AutoTrigger_LoadFailure_SetsErrorState | Unit test: verify HasLoadError, no unobserved exception |
| 7 | LazyLoadTests.cs | ValueAccess_ConcurrentAccess_SharesOneLoad | Unit test: multiple Value accesses, one loader invocation |
| 8 | LazyLoadTests.cs | IsLoadingAccess_DoesNotTriggerLoad + IsLoadedAccess_DoesNotTriggerLoad | Two unit tests |
| 9 | LazyLoadTests.cs | (same as 8, two separate tests) | |
| 10 | LazyLoadStatePropagationTests.cs | ParentIsBusy_AfterAutoTriggeredChildLoad | Integration test: access child Value, check parent IsBusy |
| 11 | LazyLoadStatePropagationTests.cs | ParentIsValid_AfterAutoTriggeredChildLoadFailure | Integration test: failing loader, check parent IsValid |
| 12 | LazyLoadTests.cs | ExplicitLoadAsync_StillWorksIdentically | Unit test: verify explicit path unchanged |
| 13 | LazyLoadTests.cs | WaitForTasks_AfterAutoTrigger_AwaitsLoad | Unit test on LazyLoad instance |
| 14 | LazyLoadStatePropagationTests.cs | ParentWaitForTasks_AwaitsAutoTriggeredLazyLoadChild | Integration test |
| 15 | LazyLoadStatePropagationTests.cs | ParentWaitForTasksWithToken_AwaitsAutoTriggeredLazyLoadChild | Integration test |
| 16 | LazyLoadStatePropagationTests.cs | ParentWaitForTasks_PreLoadedChild_CompletesImmediately | Integration test |
| 17 | LazyLoadStatePropagationTests.cs | ParentWaitForTasks_UnaccessedChild_CompletesWithoutTrigger | Integration test |

### In Scope

- [ ] `src/Neatoo/LazyLoad.cs`: Modify Value getter with auto-trigger guard, add `TriggerLoadAsync()` method, update XML docs (class-level line 27 and Value property line 140)
- [ ] `src/Neatoo/ValidateBase.cs`: Modify `WaitForTasks()` (line 663) and `WaitForTasks(CancellationToken)` (line 681) to call `WaitForLazyLoadChildren()`; add two `WaitForLazyLoadChildren()` private methods (parameterless and CancellationToken)
- [ ] `src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs`: Update/split `Value_BeforeLoad_ReturnsNull`, add new tests for scenarios 1-9, 12-13
- [ ] `src/Neatoo.UnitTest/Integration/Concepts/EntityBase/LazyLoadStatePropagationTests.cs`: Add tests for scenarios 10-11, 14-17
- [ ] `skills/neatoo/references/lazy-loading.md`: Update key principles (lines 3, 7), state properties table (line 145), Razor example, error handling section
- [ ] `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs`: Update design decision comments
- [ ] `src/samples/LazyLoadSamples.cs`: Update any "never triggers" comments
- [ ] `docs/release-notes/v0.11.0.md`: Add note about behavioral change in later release
- [ ] Checkpoint: Run `dotnet test src/Neatoo.sln` after all phases

### Explicitly Out of Scope

- Any changes to `RunningTasks`/`AddChildTask` for auto-triggered loads (WaitForTasks polls LazyLoad children directly instead)
- Changes to serialization behavior
- Changes to completed todo files (historical records)
- Design.sln pre-existing NF0105 errors
- Fixing pre-existing inconsistency in `WaitForTasks(CancellationToken)` not awaiting `PropertyManager.WaitForTasks()` (separate issue, not introduced by this plan)

### Verification Gates

1. After Phase 1: `dotnet build src/Neatoo/Neatoo.csproj` succeeds. Value getter has auto-trigger guard. TriggerLoadAsync exists. WaitForLazyLoadChildren methods added. WaitForTasks modified.
2. After Phase 2: `dotnet test src/Neatoo.UnitTest/Neatoo.UnitTest.csproj` succeeds. All 17 test scenarios have passing test methods.
3. Final: `dotnet test src/Neatoo.sln` succeeds (all test projects including samples).

### Stop Conditions

If any occur, STOP and report:
- Out-of-scope test failure (any test not in LazyLoadTests.cs or LazyLoadStatePropagationTests.cs starts failing)
- Architectural contradiction discovered
- Unobserved task exception in test runner despite TriggerLoadAsync catch
- `LoadAsync()` public API behavior changes (explicit path must remain identical)

---

## Implementation Progress

**Started:** 2026-03-13
**Developer:** neatoo-developer

**Phase 1: Core implementation**
- [x] Modify Value getter with auto-trigger guard (`!_isLoaded && !_isLoading && _loader != null && _loadTask == null`)
- [x] Add TriggerLoadAsync method (with CA1031 suppression for intentional catch-all)
- [x] Update XML docs on class and Value property
- [x] Add WaitForLazyLoadChildren() methods to ValidateBase (parameterless and CancellationToken)
- [x] Modify WaitForTasks() to call WaitForLazyLoadChildren()
- [x] Modify WaitForTasks(CancellationToken) to call WaitForLazyLoadChildren(token)
- [x] **Verification**: `dotnet build src/Neatoo/Neatoo.csproj` -- PASSED (0 warnings, 0 errors)

**Implementation Decision:** WaitForLazyLoadChildren does NOT swallow exceptions. Per user clarification, exceptions from failed LazyLoad loads propagate through the parent's WaitForTasks(). The fire-and-forget path in TriggerLoadAsync still catches exceptions (that's the getter path).

**Phase 2: Tests**
- [x] Update Value_BeforeLoad_ReturnsNull (renamed to Value_BeforeLoad_ReturnsNullSynchronously, uses async loader to test null-before-load)
- [x] Add auto-trigger unit tests (scenarios 1-9, 12-13): ValueAccess_TriggersFireAndForgetLoad, ValueAccess_AutoTrigger_CompletesSuccessfully, ValueAccess_AlreadyLoaded_ReturnsCachedValue, ValueAccess_DuringLoad_DoesNotStartSecondLoad, ValueAccess_NoLoader_ReturnsNullWithoutException, ValueAccess_AutoTrigger_LoadFailure_SetsErrorState, ValueAccess_ConcurrentAccess_SharesOneLoad, IsLoadingAccess_DoesNotTriggerLoad, IsLoadedAccess_DoesNotTriggerLoad, ExplicitLoadAsync_StillWorksIdentically, ExplicitLoadAsync_OnFailure_StillPropagatesException, WaitForTasks_AfterAutoTrigger_AwaitsLoad
- [x] Add integration tests for IsBusy/IsValid cascading (scenarios 10-11): ParentIsBusy_AfterAutoTriggeredChildLoad, ParentIsValid_AfterAutoTriggeredChildLoadFailure
- [x] Add integration tests for WaitForTasks cascading (scenarios 14-17): ParentWaitForTasks_AwaitsAutoTriggeredLazyLoadChild, ParentWaitForTasksWithToken_AwaitsAutoTriggeredLazyLoadChild, ParentWaitForTasks_PreLoadedChild_CompletesImmediately, ParentWaitForTasks_UnaccessedChild_CompletesWithoutTrigger
- [x] **Verification**: `dotnet test src/Neatoo.UnitTest/Neatoo.UnitTest.csproj` -- PASSED (1780 passed, 0 failed, 1 skipped)

**Observation:** Tests with synchronous loaders (Task.FromResult) complete the load synchronously in the Value getter -- the value is non-null on first access. Tests that need to observe the null-before-load state use genuinely-async loaders (TaskCompletionSource). This is correct behavior, not a bug.

**Phase 3: Documentation**
- [x] Update skill documentation (`skills/neatoo/references/lazy-loading.md`): key principles, state table, loading section, error handling, Razor example
- [x] Update Design.Domain comments (`src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs`): added design decisions for auto-trigger and WaitForTasks
- [x] Update samples: No "never triggers" comments found in `src/samples/LazyLoadSamples.cs` -- no changes needed
- [x] Update release notes (`docs/release-notes/v0.11.0.md`): added strikethrough note about behavioral change
- [x] **Verification**: `dotnet test src/Neatoo.sln` -- PASSED (all test projects)

---

## Completion Evidence

**Reported:** 2026-03-13

- **Tests Passing:**
  - `dotnet test src/Neatoo.UnitTest/Neatoo.UnitTest.csproj`: 1780 passed, 0 failed, 1 skipped (pre-existing)
  - `dotnet test src/Neatoo.sln`: All test projects pass (Neatoo.UnitTest: 1780 passed; Samples: 250 passed; Person.DomainModel.Tests: 55 passed)
- **Verification Resources Pass:** N/A (Design.sln has pre-existing NF0105 errors unrelated to this change)
- **All Contract Items:** Confirmed 100% complete

### Files Modified

**Production code:**
- `src/Neatoo/LazyLoad.cs` -- Value getter auto-trigger guard, TriggerLoadAsync method, updated XML docs
- `src/Neatoo/ValidateBase.cs` -- WaitForLazyLoadChildren methods (parameterless + CancellationToken), WaitForTasks modifications

**Tests:**
- `src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs` -- Updated Value_BeforeLoad_ReturnsNull (renamed), added 12 new auto-trigger tests
- `src/Neatoo.UnitTest/Integration/Concepts/EntityBase/LazyLoadStatePropagationTests.cs` -- Added LazyLoadAutoTriggerPropagationTests class with 6 new integration tests

**Documentation:**
- `skills/neatoo/references/lazy-loading.md` -- Updated key principles, state table, loading section, error handling, Razor example
- `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs` -- Added design decision comments
- `docs/release-notes/v0.11.0.md` -- Added strikethrough note about behavioral change

**No changes needed:**
- `src/samples/LazyLoadSamples.cs` -- No "never triggers" comments found

---

## Documentation

**Agent:** neatoo-developer
**Completed:** 2026-03-13

### Expected Deliverables

- [x] `skills/neatoo/references/lazy-loading.md` -- Updated key principles, state table, loading section, error handling, Razor example
- [x] `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs` -- Added design decision comments for auto-trigger and WaitForTasks
- [x] `src/samples/LazyLoadSamples.cs` -- No "never triggers" comments found, no changes needed
- [x] `docs/release-notes/v0.11.0.md` -- Added strikethrough note about behavioral change
- [x] Skill updates: Yes
- [x] Sample updates: N/A (no relevant comments to update)

### Files Updated

- `skills/neatoo/references/lazy-loading.md`
- `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs`
- `docs/release-notes/v0.11.0.md`

---

## Architect Verification

**Verified:** 2026-03-13
**Verdict:** VERIFIED

**Independent build results:**
- `dotnet build src/Neatoo.sln` -- 0 warnings, 0 errors, all projects built successfully

**Independent test results:**
- Neatoo.BaseGenerator.Tests: 26 passed, 0 failed, 0 skipped
- Samples: 250 passed, 0 failed, 0 skipped
- Person.DomainModel.Tests: 55 passed, 0 failed, 0 skipped
- Neatoo.UnitTest: 1780 passed, 0 failed, 1 skipped (pre-existing `AsyncFlowTests_CheckAllRules`)
- Total: 2111 passed, 0 failed

**Design match:** Yes -- implementation matches the original plan in every detail.

1. **LazyLoad.cs Value getter** (line 155): 4-condition guard `!_isLoaded && !_isLoading && _loader != null && _loadTask == null` matches plan exactly. Fire-and-forget `_ = TriggerLoadAsync()` at line 157.

2. **LazyLoad.cs TriggerLoadAsync** (line 259): Private async method wrapping `LoadAsync()` with catch-all to prevent unobserved task exceptions. CA1031 suppression with clear justification. Matches plan.

3. **LazyLoad.cs XML docs** (lines 22-34 class-level, lines 143-149 Value property): Both correctly describe auto-trigger behavior and state that IsLoading/IsLoaded do NOT trigger loads. Old "never triggers" language fully replaced.

4. **ValidateBase.cs WaitForTasks()** (line 703): Calls `await this.WaitForLazyLoadChildren()` at line 709 after existing RunningTasks and PropertyManager awaits.

5. **ValidateBase.cs WaitForTasks(CancellationToken)** (line 723): Calls `await this.WaitForLazyLoadChildren(token)` at line 727 after RunningTasks await. Exceptions propagate (not swallowed), matching user clarification.

6. **ValidateBase.cs WaitForLazyLoadChildren** (lines 367, 386): Both overloads follow the established `GetLazyLoadProperties(GetType())` polling pattern used by IsAnyLazyLoadChildBusy and IsAllLazyLoadChildrenValid. Critical invariant maintained: calls `vmp.WaitForTasks()` not `vmp.Value`, so no accidental load triggering.

7. **Test coverage** -- All 17 plan scenarios have corresponding passing tests:
   - Scenarios 1-9, 12-13: 12 new unit tests in `LazyLoadTests.cs` plus updated `Value_BeforeLoad_ReturnsNullSynchronously`
   - Scenarios 10-11: 2 integration tests for IsBusy/IsValid cascading in `LazyLoadAutoTriggerPropagationTests`
   - Scenarios 14-17: 4 integration tests for WaitForTasks cascading in `LazyLoadAutoTriggerPropagationTests`

8. **Documentation** -- All 7 documentation locations updated:
   - `skills/neatoo/references/lazy-loading.md` -- Key principles, state table, loading section, error handling, WaitForTasks section, Razor example all reflect auto-trigger behavior
   - `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs` -- Three new DESIGN DECISION comments added (auto-trigger, WaitForTasks, nullable constraint)
   - `docs/release-notes/v0.11.0.md` -- Strikethrough on "Value never triggers" with clear behavioral change note
   - `src/samples/LazyLoadSamples.cs` -- Confirmed no "never triggers" comments to update; no changes needed (correct)

**Issues found:** None

---

## Requirements Verification

**Reviewer:** neatoo-requirements-reviewer
**Verified:** 2026-03-13
**Verdict:** REQUIREMENTS SATISFIED

### Requirements Compliance

| Requirement | Status | Evidence |
|-------------|--------|----------|
| REQ-1: Value_BeforeLoad_ReturnsNull (semantic intent changes) | Satisfied | Test renamed to `Value_BeforeLoad_ReturnsNullSynchronously` at `LazyLoadTests.cs:18`. Uses genuinely-async loader (TaskCompletionSource) so Value returns null synchronously on first access. The semantic intent is preserved and clarified: the test now explicitly documents that Value returns null *synchronously* while the async load is in progress. New auto-trigger tests (`ValueAccess_TriggersFireAndForgetLoad` at line 247) separately verify the fire-and-forget side effect. |
| REQ-2: IsLoading_DuringLoad_ReturnsTrue | Satisfied | Test unchanged at `LazyLoadTests.cs:63`. Uses explicit `LoadAsync()` path. The auto-trigger does not alter `LoadAsyncCore`'s `_isLoading` management (set true at `LazyLoad.cs:223`, set false in finally at `LazyLoad.cs:244`). |
| REQ-3: LoadAsync_CalledConcurrently_OnlyLoadsOnce | Satisfied | Test unchanged at `LazyLoadTests.cs:107`. The auto-trigger in the Value getter calls `TriggerLoadAsync()` which calls `LoadAsync()`, entering the same `lock(_loadLock)` at `LazyLoad.cs:211` with `_loadTask != null` check at line 213. New test `ValueAccess_ConcurrentAccess_SharesOneLoad` at line 391 explicitly verifies three concurrent Value accesses share one load. |
| REQ-4: LoadAsync_WhenAlreadyLoaded_ReturnsImmediately | Satisfied | Test unchanged at `LazyLoadTests.cs:138`. Value getter guard checks `!_isLoaded` first at `LazyLoad.cs:155`, short-circuiting when already loaded. New test `ValueAccess_AlreadyLoaded_ReturnsCachedValue` at line 295 verifies no re-load on pre-loaded instance. |
| REQ-5: LoadAsync_RaisesPropertyChangedForAllStateProperties | Satisfied | Test unchanged at `LazyLoadTests.cs:184`. Same `LoadAsyncCore` path fires PropertyChanged for Value (line 231), IsLoaded (line 232), and IsLoading (lines 224, 245). New test `ValueAccess_AutoTrigger_CompletesSuccessfully` at line 273 also verifies PropertyChanged events fire during auto-triggered load. |
| REQ-6: IsBusy_WhenLoading_ReturnsTrue | Satisfied | Test unchanged at `LazyLoadTests.cs:215`. `LazyLoad.IsBusy` at `LazyLoad.cs:282` returns `IsLoading || ...`. Auto-triggered load sets `_isLoading = true` at `LazyLoad.cs:223` via same `LoadAsyncCore`. New integration test `ParentIsBusy_AfterAutoTriggeredChildLoad` at `LazyLoadStatePropagationTests.cs:99` verifies parent cascading. |
| REQ-7: LoadAsync_OnFailure_SetsErrorState | Satisfied | Test unchanged at `LazyLoadTests.cs:158`. Explicit `LoadAsync()` path: exception caught in `LoadAsyncCore` (line 235-240), `_loadError` set, PropertyChanged fired, then rethrown. For auto-trigger path: `TriggerLoadAsync` at `LazyLoad.cs:259-270` catches the rethrown exception to prevent unobserved task exception. New test `ValueAccess_AutoTrigger_LoadFailure_SetsErrorState` at line 362 verifies error state after auto-triggered failure. |
| REQ-8: LazyLoadStatePropagationTests (state cascading) | Satisfied | Tests unchanged at `LazyLoadStatePropagationTests.cs:14-75`. These use pre-loaded LazyLoad (`new LazyLoad<ILazyLoadEntityObject>(child)` at line 30), so `_isLoaded = true` and the auto-trigger guard is never reached. |
| REQ-9: WaitForTasksLazyLoadCrashTests (AddActionAsync path) | Satisfied | Test unchanged at `WaitForTasksLazyLoadCrashTests.cs:36`. Uses explicit `await parent.LazyChild` via `GetAwaiter()` (line 83 of `WaitForTasksLazyLoadCrashEntity.cs`), not the Value getter. The auto-trigger does not affect this code path. |
| REQ-10: ValidateBase.IsBusy includes IsAnyLazyLoadChildBusy() | Satisfied | `ValidateBase.cs:174` unchanged: `IsBusy => this.RunningTasks.IsRunning || this.PropertyManager.IsBusy || IsAnyLazyLoadChildBusy()`. The polling mechanism at lines 347-358 is unchanged. Auto-triggered loads make `LazyLoad.IsBusy = true` (via `IsLoading`), which cascades correctly. Integration test at `LazyLoadStatePropagationTests.cs:99` confirms. |
| REQ-11: ValidateBase.IsValid includes IsAllLazyLoadChildrenValid() | Satisfied | `ValidateBase.cs:280` unchanged: `IsValid => this.PropertyManager.IsValid && IsAllLazyLoadChildrenValid()`. The polling mechanism at lines 330-341 is unchanged. Failed auto-triggered loads make `LazyLoad.IsValid = false` (via `HasLoadError`), which cascades correctly. Integration test at `LazyLoadStatePropagationTests.cs:124` confirms. |
| REQ-12: FatClientLazyLoad_PostDeserialization_LoadAsync_Throws | Satisfied | Test unchanged at `FatClientLazyLoadTests.cs:168`. Calls `LoadAsync()` explicitly on a deserialized instance with `_loader == null`, which still throws `InvalidOperationException` at `LazyLoad.cs:206-209`. The Value getter's auto-trigger guard checks `_loader != null` at `LazyLoad.cs:155`, preventing the throw from the getter. Test `ValueAccess_NoLoader_ReturnsNullWithoutException` at `LazyLoadTests.cs:346` explicitly verifies this. |
| REQ-13: Seven documentation locations updated | Satisfied | (1) `LazyLoad.cs:28` -- class-level XML doc now says "triggers a fire-and-forget load." (2) `LazyLoad.cs:143-149` -- Value property doc now says "Auto-triggers a fire-and-forget LoadAsync." (3) `skills/neatoo/references/lazy-loading.md:3,7` -- "Auto-trigger on Value access" replaces "Explicit loading only." (4) `skills/neatoo/references/lazy-loading.md:165` -- Value table now says "Auto-triggers fire-and-forget load." (5) `docs/release-notes/v0.11.0.md:35` -- strikethrough with change note. (6) `Design.Domain/PropertySystem/LazyLoadProperty.cs:14-19` -- new DESIGN DECISION comment for auto-trigger. (7) Completed todo files (`lazy-loading-v2-design.md`) correctly left unmodified as historical records. |
| REQ-14: Design precedent (old Property<T>.OnLoad pattern) | Satisfied | The implementation follows the same fire-and-forget-on-access pattern as the old system, but on the dedicated `LazyLoad<T>` wrapper. The `TriggerLoadAsync` method at `LazyLoad.cs:259` is analogous to the old `OnLoad` fire-and-forget, with the addition of proper exception catching (which the old system lacked). |
| REQ-15: AsyncTasks fire-and-forget with rendezvous | Satisfied | The implementation follows Neatoo's established fire-and-forget pattern: the Value getter fires async work without awaiting it, and `WaitForTasks()` is the rendezvous point. `ValidateBase.WaitForTasks()` at line 703 now calls `WaitForLazyLoadChildren()` at line 709. `LazyLoad.WaitForTasks()` at line 301 returns `_loadTask` when in progress. New integration tests at `LazyLoadStatePropagationTests.cs:151,177` verify the rendezvous. |

### Unintended Side Effects

1. **WaitForTasks(CancellationToken) now awaits LazyLoad children, broadening its scope.** Before this change, `WaitForTasks(CancellationToken)` at `ValidateBase.cs:723` only awaited `RunningTasks.WaitForCompletion(token)`. It now also awaits `WaitForLazyLoadChildren(token)` at line 727. This is a behavioral change to a public virtual method. However, the change is additive and strictly more correct: any caller relying on `WaitForTasks(CancellationToken)` to ensure all async work is complete now gets LazyLoad children included. The pre-existing inconsistency (parameterless overload awaited PropertyManager, CancellationToken overload did not) is not addressed by this change and remains as a separate issue.

2. **Existing code that accessed `.Value` for read-only inspection now triggers loads as a side effect.** Any code that previously accessed `LazyLoad<T>.Value` expecting no side effect will now trigger a fire-and-forget load if the instance is unloaded and has a loader. This is the intentional behavioral change documented in the todo. The guard conditions (`_isLoaded`, `_isLoading`, `_loader`, `_loadTask`) ensure the load fires at most once and only when meaningful. The risk is contained because: (a) pre-loaded instances (`_isLoaded = true`) short-circuit immediately, (b) deserialized instances without loaders (`_loader == null`) short-circuit immediately, and (c) the load is idempotent.

3. **`LoadAsync_OnFailure_SetsErrorState` test now has a subtlety at line 180.** The test accesses `lazyLoad.Value` after a failed explicit `LoadAsync()`. With the auto-trigger, this Value access could theoretically re-trigger a load. However, it does not: `_loadTask` is non-null (it holds the faulted task from the first load), so the guard condition `_loadTask == null` prevents re-triggering. The test continues to pass with correct semantics. No modification was needed, and no modification was made.

### Issues Found

None. The implementation respects all 15 documented requirements. All existing tests pass without modification to their behavioral intent (the renamed test preserves its original assertion while clarifying its purpose). The 18 new tests (12 unit + 6 integration) cover all 17 planned scenarios. Documentation updates cover all 7 identified locations. The auto-trigger guard conditions are comprehensive and handle all edge cases (pre-loaded, in-progress, no loader, faulted task).
