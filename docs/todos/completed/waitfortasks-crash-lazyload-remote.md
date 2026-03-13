# WaitForTasks crashes when AddActionAsync awaits LazyLoad with [Remote] call

**Status:** Complete
**Priority:** High
**Created:** 2026-03-13
**Last Updated:** 2026-03-13

**Plans:** [Fix LazyLoad Deserialization Overwrite](../plans/fix-lazyload-deserialization-overwrite.md)

---

## Problem

Test host process crashes (not deadlock) when `WaitForTasks()` is called after setting a property that triggers an `AddActionAsync` handler, and that handler `await`s a `LazyLoad<T>` deferred loader that calls a `[Remote]` factory method.

This is the production flow pattern in the zTreatment application. The crash only manifests in the `ClientServerTestBase` integration test infrastructure — the same pattern works in unit tests (where `[Remote]` calls are local) and presumably in production (where the client-server round-trip is real HTTP).

### Reproduction

In zTreatment (`feature/lazy-load-symptoms` branch), after the "Visit Owns Lazy-Loaded Child Entities" refactoring:

1. `VisitHub` registers an `AddActionAsync` on `ActiveSpoke` property:
```csharp
RuleManager.AddActionAsync(async hub =>
{
    switch (hub.ActiveSpoke)
    {
        case ActiveSpoke.SYMPTOMS:
            await hub.Visit.Symptoms;  // LazyLoad<ISymptomsAssessment>
            break;
        case ActiveSpoke.SIGNS:
            await hub.Visit.Signs;  // LazyLoad<ISignsAssessment>
            break;
        case ActiveSpoke.THERAPY:
            await hub.Visit.TreatmentLazy;  // LazyLoad<ITreatment>
            break;
    }
}, h => h.ActiveSpoke);
```

2. `Visit.Symptoms` is a `LazyLoad<ISymptomsAssessment>` in deferred mode. The loader delegate calls:
```csharp
private async Task<ISymptomsAssessment> LoadSymptomsAsync()
{
    var symptoms = await SymptomsAssessmentEntityFactory.FetchOrCreateForVisit(Id, patientId);
    SymptomsAssessmentEntity = symptoms;
    return symptoms;
}
```

3. `FetchOrCreateForVisit` is a `[Fetch][Remote]` method on `SymptomsAssessment`.

4. In integration tests:
```csharp
hub.NavigateToSpoke(ActiveSpoke.SYMPTOMS);  // sets ActiveSpoke → triggers AddActionAsync
await hub.WaitForTasks();  // should wait for the async action to complete
```

5. **Result:** Test host process crashes immediately. No exception, no stack trace — just `"Test host process crashed"`.

### What works

- Unit tests where `LazyLoad<T>` deferred loaders call factory methods locally (no `[Remote]` round-trip)
- `AddActionAsync` handlers that do NOT call `[Remote]` methods
- `WaitForTasks()` with non-LazyLoad async actions

### Environment

- Neatoo 0.20.0
- .NET 10.0
- xUnit 3.1.5
- `ClientServerTestBase` integration test infrastructure

---

## Solution

Investigate why `WaitForTasks()` crashes when an `AddActionAsync` handler awaits a `LazyLoad<T>` deferred loader that calls a `[Remote]` factory method through the `ClientServerTestBase` round-trip.

Possible areas to investigate:
- StackOverflowException from recursive property change notifications (LazyLoad setting `Value` triggers change → re-enters action)
- Synchronization context issues in the test infrastructure async chain
- Thread pool exhaustion from blocking on async in the client-server simulation

---

## Tasks

- [x] Reproduce the crash with a minimal test case
- [x] Determine crash type (StackOverflow, AccessViolation, unhandled async exception, etc.)
- [x] Fix the root cause
- [ ] Verify zTreatment integration tests pass after fix (pending zTreatment update)

---

## Progress Log

### 2026-03-13
- Created todo from zTreatment blocker (docs/todos/visit-owns-lazy-load.md)
- Crash confirmed: even a single integration test that calls `NavigateToSpoke()` + `await WaitForTasks()` crashes the test host
- **Reproduction built** in `src/Neatoo.UnitTest/Integration/Concepts/Serialization/`:
  - `WaitForTasksLazyLoadCrashEntity.cs` — minimal `CrashParent` (with `LazyLoad<ICrashChild>` + `AddActionAsync`) and `CrashChild` (`[Remote] [Fetch]`)
  - `WaitForTasksLazyLoadCrashTests.cs` — `ClientServerTestBase` integration test
- **Root cause identified**: `NeatooBaseJsonTypeConverter` (lines 130-136, 196-200) overwrites constructor-created `LazyLoad<T>` instances during deserialization. The `_loader` delegate is `[JsonIgnore]` (non-serializable), so the deserialized instance has `_loader = null`. Awaiting it throws `InvalidOperationException: Cannot load: no loader delegate is configured`.
- In zTreatment's more complex aggregate hierarchy, this exception likely escalates to a hard process crash via re-entrant notification cycles

---

## Completion Verification

Before marking this todo as Complete, verify:

- [ ] All Neatoo builds pass
- [ ] All Neatoo tests pass
- [ ] zTreatment integration tests pass with the fix (VisitHubReactiveSpokeTests)

**Verification results:**
- Build: 0 errors, 0 warnings
- Tests: 2092 passed, 0 failed, 1 skipped (pre-existing)

---

## Results / Conclusions

**Root cause:** `NeatooBaseJsonTypeConverter.Read` replaced constructor-created `LazyLoad<T>` instances during deserialization with new instances that had `_loader = null`. The loader delegate is `[JsonIgnore]` (non-serializable), so the deserialized replacement could never load.

**Fix:** Added `ILazyLoadDeserializable` internal interface to `LazyLoad<T>` with `ApplyDeserializedState()`. Changed the converter to merge serialized state (Value, IsLoaded) into the existing constructor-created instance instead of replacing it, preserving the loader delegate.

**Files changed:**
- `src/Neatoo/LazyLoad.cs` — added `ILazyLoadDeserializable` interface and implementation
- `src/Neatoo/RemoteFactory/Internal/NeatooBaseJsonTypeConverter.cs` — merge instead of replace

**Additional finding:** zTreatment's `Visit.cs` uses `OnDeserialized()` + `ReinitializeLazyLoaders()` as a workaround for this bug. With the converter fix, `LazyLoad` creation can simply go in the constructor and survive deserialization. The zTreatment workaround should be simplified in a follow-up.
