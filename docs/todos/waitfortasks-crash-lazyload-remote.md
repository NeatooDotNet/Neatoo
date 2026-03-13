# WaitForTasks crashes when AddActionAsync awaits LazyLoad with [Remote] call

**Status:** In Progress
**Priority:** High
**Created:** 2026-03-13
**Last Updated:** 2026-03-13

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

- [ ] Reproduce the crash with a minimal test case
- [ ] Determine crash type (StackOverflow, AccessViolation, unhandled async exception, etc.)
- [ ] Fix the root cause
- [ ] Verify zTreatment integration tests pass after fix

---

## Progress Log

### 2026-03-13
- Created todo from zTreatment blocker (docs/todos/visit-owns-lazy-load.md)
- Crash confirmed: even a single integration test that calls `NavigateToSpoke()` + `await WaitForTasks()` crashes the test host

---

## Completion Verification

Before marking this todo as Complete, verify:

- [ ] All Neatoo builds pass
- [ ] All Neatoo tests pass
- [ ] zTreatment integration tests pass with the fix (VisitHubReactiveSpokeTests)

**Verification results:**
- Build: [Pending]
- Tests: [Pending]

---

## Results / Conclusions

