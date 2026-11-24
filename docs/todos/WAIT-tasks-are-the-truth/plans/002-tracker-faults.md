# The tracker faults once, then recovers

**Plan #:** 002
**Date:** 2026-11-24
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-7
**Status:** Done
**Last Updated:** 2026-11-24
**Plan-review opt-in:** No (the two decisions it implements, D34 and D36, are Keith's; the rest is contained in one class)
**Code-review opt-in:** Yes (it changes what every wait observes after a fault, and adds a member to a public services interface)
**Branch:** wait-002-tracker-faults
**PR:** —

---

## Scope

Changes the task tracker so that a faulted sequence reports its exceptions to the wait that observes it and the next task starts a clean sequence, and so that an exception of any type from the completion callback still completes the wait. Puts the tracker behind an interface that the entity's services supply, so it is no longer constructed inside the base class. Adds the tests for an async rule's fault coming out of `WaitForTasks()`. It does not change what registers tasks, how waits are composed, or the property's busy state after a fault, which is WAIT-003.

---

## Intent

- After an async rule throws, the wait that sees it throws, and the object is usable again: later work is tracked and waited for (D34).
- A fault is reported once. A second wait with no new work does not throw the old exception.
- A fault nobody was waiting for is not lost: the next wait reports it.
- The tracker is a part a consumer can replace through DI (D36).

---

## Framework & Architectural Alignment

- D34 and D36 in `docs/VisionAlignment.md`. D2 stands: a throw is an application failure.
- The services pattern: `IValidateBaseServices<T>` supplies what the base class depends on; a factory delegate is registered in `AddNeatooServices`, as the property managers are.
- The convenience constructors on the services classes that build their own defaults keep doing so.

---

## Constraints & Invariants

- A task that is already faulted when it is added still throws from the add, so a synchronous fault still surfaces from the setter.
- A sequence that completes cleanly behaves as before, including a task added from the completion callback.
- Exceptions from several tasks in one sequence are still reported together.
- No existing tracker test changes its expectation.

---

## Steps

1. Declare the tracker's interface and have the existing tracker implement it.
2. Start a clean sequence for the first task added after a faulted one, carrying forward a fault nobody observed.
3. Hand a completed fault to one wait only: the waits that held the sequence's task while it ran, or else the first wait after it.
4. Report an exception of any type from the completion callback.
5. Supply the tracker through the services, registered in DI, and stop the base class constructing it. Forward the new member in the two services wrappers the repository carries.
6. Write the entity-level tests: an async rule's fault surfaces from `WaitForTasks()`, once, and a later write is tracked and waited for.

---

## Acceptance

- [ ] A task added after a faulted sequence is tracked, and the wait for it completes cleanly `[unit]`
- [ ] A fault observed by a wait in progress is not thrown again by a later wait `[unit]`
- [ ] A fault nobody was waiting for is thrown by the next wait, once `[unit]`
- [ ] A fault nobody observed before the next sequence began is reported with that sequence `[unit]`
- [ ] A completion callback that throws an exception of any type faults the wait and does not hang it `[unit]`
- [ ] An async rule that throws surfaces from the entity's `WaitForTasks()` once, leaves the trigger property invalid with the message, and a later write is tracked and waited for `[integration]`
- [ ] An entity resolved from DI takes its tracker from the registered factory `[integration]`
- [ ] Build and tests green `[explicit-skip: meta-bullet]`

---

## Current State (Pre-Flight)

- `src/Neatoo/Internal/AsyncTasks.cs`: `AddTask` returns the old faulted task when `_allDoneCompletionSource.Task.IsFaulted` (:28-32), so nothing is tracked after a fault. `SequenceCompleted` catches only `AggregateException` from `OnFullSequenceComplete` (:150-157). The `runOnException` parameter is unused.
- `src/Neatoo/ValidateBase.cs:125`: `protected AsyncTasks RunningTasks { get; private set; } = new AsyncTasks();`.
- `IValidateBaseServices<T>` (`src/Neatoo/IValidateBaseServices.cs`) has four properties and `CreateRuleManager`. Implementers: `ValidateBaseServices<T>` (five constructors), `EntityBaseServices<T>` (six), and two wrappers that forward to an inner services object: `PlausibleEntityServices<T>` in `src/Design/Design.Domain/PropertySystem/CustomPropertyType.cs` (a skill snippet, `skill-custom-property-services`) and `TaggedEntityBaseServices<T>` in the unit tests.
- `AddNeatooServices` registers `CreateValidatePropertyManager` and `CreateEntityPropertyManager` as factory delegates.
- `ValidateBaseAsyncTests.ValidateBaseAsync_AsyncRuleThrowsException` is commented out; its fixture `AsyncRuleThrowsException` still exists. That class's `TestCleanup` asserts `IsBusy` is false, which a faulted rule breaks today (WAIT-003), so the new tests go in their own class.
- Four `MarkBusyForTest` helpers call `RunningTasks.AddTask(tcs.Task)`.

---

## Punchlist

---

## Test Evidence

Red run before the tracker change, with the DI plumbing in: 8 of the 11 new tests failed (`AsyncTasksFaultRecoveryTests` all 5; `AsyncRuleFaultTests` `IsNotThrownByASecondWait`, `ALaterWriteIsTrackedAndWaitedFor`, `TheObjectIsValidOnceTheRuleRunsClean`). Unit tests are in `Neatoo.UnitTest.Unit.Core`, integration tests in `Neatoo.UnitTest.Integration.Concepts`.

| Acceptance bullet (short) | Priority | Tier declared | Test method | Tier confirmed |
|---|---|---|---|---|
| A task added after a fault is tracked | Required | `[unit]` | `AsyncTasksFaultRecoveryTests.AddTask_AfterAFaultedSequence_IsTracked_AndItsWaitCompletesCleanly` | ✓ |
| An observed fault is not thrown again | Required | `[unit]` | `AsyncTasksFaultRecoveryTests.Fault_ObservedByAWaitInProgress_IsNotThrownByALaterWait` | ✓ |
| An unobserved fault is thrown by the next wait, once | Required | `[unit]` | `AsyncTasksFaultRecoveryTests.Fault_NobodyWasWaitingFor_IsThrownByTheNextWait_Once` | ✓ |
| An unobserved fault is reported with the next sequence | Required | `[unit]` | `AsyncTasksFaultRecoveryTests.Fault_NotObservedBeforeTheNextSequence_IsReportedWithThatSequence` | ✓ |
| A callback that throws any exception faults the wait | Required | `[unit]` | `AsyncTasksFaultRecoveryTests.CompletionCallback_ThatThrowsAnyException_FaultsTheWait` | ✓ |
| A cancelled wait does not take the fault (added at the gate) | Required | `[unit]` | `AsyncTasksFaultRecoveryTests.CancelledWait_DoesNotTakeTheFault_TheNextWaitReportsIt`, red before its fix | ✓ |
| A thrown async rule surfaces once; property invalid; later write tracked | Required | `[integration]` | `ValidateBase.AsyncRuleFaultTests`: `AsyncRuleThatThrows_SurfacesFromWaitForTasks`, `_LeavesTheTriggerPropertyInvalidWithTheMessage`, `_IsNotThrownByASecondWait`, `AfterAFault_ALaterWriteIsTrackedAndWaitedFor`, `AfterAFault_TheObjectIsValidOnceTheRuleRunsClean` | ✓ |
| An entity takes its tracker from the registered factory | Required | `[integration]` | `ValidateBase.AsyncRuleFaultTests.ValidateBase_TakesItsTrackerFromTheRegisteredFactory`; `EntityBase.TrackerFromServicesTests.EntityBase_TakesItsTrackerFromTheRegisteredFactory`, `_WithNothingRegisteredOverIt_TakesTheFrameworkTracker` | ✓ |
| Build and tests green | Required | `[explicit-skip]` | `reviews/002-build.log`, `002-test.log`, `002-design-build.log`, `002-design-test.log` | n/a |

Two of the integration tests passed before the tracker change, because the behavior they pin already held: `AsyncRuleThatThrows_SurfacesFromWaitForTasks` and `_LeavesTheTriggerPropertyInvalidWithTheMessage`. They replace the commented-out `ValidateBaseAsync_AsyncRuleThrowsException`. The three tracker-from-DI tests could not be run red: before the plumbing they did not compile.
---

## Gate Record

- Round 1 (2026-11-24): test review APPROVED, no veto, no callout; both tech-debt lines fixed (the misnamed test renamed; the two unobserved-fault tests now wait for the sequence to close) — `reviews/002-test-review.md`. Code review APPROVED, no veto; callout 1 fixed (a wait cancelled by its token no longer takes the fault: the next wait reports it, pinned by a new test seen red), callout 2 punched to the todo (the three compile breaks for the migration notes) — `reviews/002-code-review.md`. Could not verify, both reviewers: the full-run logs do not name tests; resolved by the filtered runs, which listed the new tests by name. Two Theoretical lines not triaged. No round 2: nothing veto-tier was open. Done.
---

## Plan Amendments

### 2026-11-24 — A thrown rule's message belongs to the rule

- **Section affected:** Steps, Acceptance bullet 6
- **Original said:** the trigger property keeps the message; nothing about how it is cleared.
- **What changed:** the message recorded when a rule throws now carries the rule's id, so the rule's next clean run clears it. `AfterAFault_TheObjectIsValidOnceTheRuleRunsClean` pins it.
- **Why:** D34 says the object stays invalid "until the rule runs clean". The message had no rule id, so a clean run never cleared it and the object stayed invalid until every message was cleared.
- **Discovery Log:** 2026-11-24 / WAIT-002

### 2026-11-24 — One DI constructor with optional parameters

- **Section affected:** Step 5
- **Original said:** supply the tracker through the services.
- **What changed:** each services class gains one constructor that takes the tracker factory and makes the save factory and the logger factory optional. Every other constructor's parameters are a subset of it, so DI chooses it whether or not logging or a save factory is registered. The convenience constructors default to the framework's tracker.
- **Why:** without the optional parameters, a container with no logging registered fell back to a constructor that did not take the tracker factory.
- **Discovery Log:** 2026-11-24 / WAIT-002
---

## Notes
