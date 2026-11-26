# The Optional criteria

**Plan #:** 006
**Date:** 2026-11-24
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-10, AC-11 (AC-12 is parked, see Plan Amendments)
**Status:** Done
**Last Updated:** 2026-11-24
**Plan-review opt-in:** No (two small behaviors, both stated by their criteria)
**Code-review opt-in:** Yes (it changes how the framework raises its own event, everywhere)
**Branch:** wait-006-optional-criteria
**PR:** #148

---

## Scope

Takes the three Optional criteria in turn, building what is in reach now that tracking is in place. Raising `NeatooPropertyChanged` awaits every subscriber's task. The order of the idle notification and the wait is pinned. A save in flight being busy is not built here: it is parked, with what was found. It does not change waits, busy or registration.

---

## Intent

- A change is not over while any handler of it is still running, whichever order the handlers subscribed in. A consumer who also subscribes to a property or an object cannot make the framework lose a rule cascade.
- By the time `WaitForTasks()` completes, whoever listens for `IsBusy` has already heard "idle".

---

## Framework & Architectural Alignment

- D31: a task that nobody awaits is work that nobody tracks. This closes the last place the framework itself dropped one (analysis defect D6, the way #140 lost rule runs).
- AC-11 is the answer to #129's question "which is wrong about intent": the framework's ordering is the contract, and the tracker's completion callback already gives it.
- No reflection: the subscribers are enumerated with `Delegate.EnumerateInvocationList<T>`, which is typed.

---

## Constraints & Invariants

- Subscribers are still called in the order they subscribed, on the caller's thread, and a subscriber that throws before returning a task still stops the ones after it.
- One subscriber costs what it cost before: no list, no `WhenAll`.
- A synchronous fault still throws from the setter.

---

## Steps

1. Raise `NeatooPropertyChanged` through one helper that calls every subscriber and awaits all their tasks, at the five places the framework raises it.
2. Pin that the idle notification is raised before the wait completes.
3. Assess a save in flight being busy (AC-12) and build it or park it.

---

## Acceptance

- [x] A write is not over while an earlier subscriber of the object is still running `[integration]`
- [x] A write is not over while its rules run, when a consumer also subscribes to the property `[integration]`
- [x] A write reports what an earlier subscriber threw `[integration]`
- [x] The idle `IsBusy` notification is raised before `WaitForTasks()` completes `[integration]`
- [x] Build and tests green `[explicit-skip: meta-bullet]`

---

## Current State (Pre-Flight)

- `NeatooPropertyChanged` is a delegate that returns `Task`. The framework raises it as `NeatooPropertyChanged?.Invoke(args) ?? Task.CompletedTask` at five places: twice in `ValidateProperty`, and once each in `ValidatePropertyManager`, `ValidateBase` and `ValidateListBase`. With more than one subscriber that returns the last subscriber's task only.
- Since WAIT-002 the tracker runs its completion callback before it completes `AllDone`; since WAIT-005 the object's idle notification is raised from that callback. So AC-11 is expected to hold already.
- `EntityBase.Save()` checks `IsSavable`, then awaits `Factory.Save(this)`. Nothing marks the instance while the save is in flight. `EntityBase.MarkUnmodified`, which the factory calls on the saved instance when a local save completes, throws while `IsBusy`.
- #129 and #103 are the open issues the todo adopts under AC-11 and AC-12.

---

## Punchlist

---

## Test Evidence

Red run before the change (`reviews/006-red.log`): 3 of the 4 tests failed, the three for AC-10. The AC-11 test passed before the change: the ordering was already delivered by WAIT-002 and WAIT-005, and this plan pins it. All are in `Neatoo.UnitTest.Integration.Concepts.EntityBase.EverySubscriberIsAwaitedTests`.

| Acceptance bullet (short) | Priority | Tier declared | Test method | Tier confirmed |
|---|---|---|---|---|
| Not over while an earlier subscriber of the object runs | Optional | `[integration]` | `AWrite_IsNotOver_WhileAnEarlierSubscriberOfTheObjectIsStillRunning` | ✓ |
| Not over while its rules run, with a consumer on the property | Optional | `[integration]` | `AWrite_IsNotOver_WhileItsRulesRun_WhenSomeoneElseAlsoListensToTheProperty` | ✓ |
| Reports what an earlier subscriber threw | Optional | `[integration]` | `AWrite_ReportsWhatAnEarlierSubscriberThrew` | ✓ |
| Idle is announced before the wait completes | Optional | `[integration]` | `TheIdleAnnouncement_IsRaisedBeforeTheWaitCompletes` | ✓ |
| Build and tests green | Required | `[explicit-skip]` | `reviews/006-build.log`, `006-test.log` (2,104 passed, 2 skipped, 0 failed), `006-design-build.log`, `006-design-test.log` (235 passed) | n/a |

One more test holds a constraint rather than a bullet (a subscriber costs and behaves as before): `ASubscriberThatReturnsNoTask_IsTreatedAsFinished`, added at the gate and red before its fix (`reviews/006-red-null.log`).

No existing test was changed.

`AWrite_ReportsWhatAnEarlierSubscriberThrew` runs on one owning thread. Written first as a plain async test it failed in 1 of 15 full runs: on the test host's pool the subscriber could fault before the write had returned, and the fault then threw from the setter, which is the documented behavior for a synchronous fault. On one thread, 0 failures in 25 runs.

---

## Gate Record

- Round 1 (2026-11-24): test review APPROVED, no veto, no callout; its bookkeeping note is fixed (the todo's plan row no longer lists AC-12 as served) — `reviews/006-test-review.md`. Code review APPROVED, no veto, two callouts — `reviews/006-code-review.md`:
  - A subscriber that returns null faulted the write, where the old invocation absorbed it. Fixed, with a test seen red first.
  - A handler that awaits its own object's wait now deadlocks whatever order it subscribed in; before, it escaped when a later subscriber's task replaced its own, which was the defect. Punched to the todo for WAIT-008: the guides and the migration notes say so.
  - It checked the reason for parking AC-12 against the generated save path and found it holds (traced, not run), and agreed that enumerating a delegate's subscribers is not reflection. The gate is closed.

---

## Plan Amendments

### 2026-11-24 — AC-12 is parked: a save in flight is not made busy here

- **Section affected:** Scope, Step 3, Serves
- **Original said:** build the three Optional criteria where they are in reach.
- **What changed:** AC-12 is not built. It stays as #103, with this finding on the todo's Parked list.
- **Why:** making the save the object's work means registering it in the running tasks. When a local save with an async `[Insert]` or `[Update]` completes, the factory calls `MarkUnmodified` on the same instance, and `MarkUnmodified` refuses while the object is busy. So the simple form breaks every single-tier save with an async operation. Building it needs a ruling on what that guard means during the object's own save, and on what state the sent instance is left in after a remote save. That is a design decision about saving, which this todo put outside its plans, and the criterion is Optional.
- **Discovery Log:** 2026-11-24 / WAIT-006

---

## Notes

- The helper is `Neatoo.Internal.NeatooPropertyChangedInvocation.InvokeAll`.
- Subscribers' tasks are awaited together. When more than one faults, the awaiter sees the first, as with any `Task.WhenAll`.
