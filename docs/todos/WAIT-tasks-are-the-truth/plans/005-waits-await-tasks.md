# Waits await tasks; busy reads tasks

**Plan #:** 005
**Date:** 2026-11-24
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-2, AC-3, AC-4, AC-5
**Status:** In Progress
**Last Updated:** 2026-11-24
**Plan-review opt-in:** No (the design is D33, approved by Keith on 2026-11-24)
**Code-review opt-in:** Yes (it changes what every wait and every busy read means)
**Branch:** wait-005-waits-await-tasks
**PR:** —

---

## Scope

Makes `WaitForTasks()` on an object the object's running tasks and nothing else, with the token overload the same wait plus cancellation. Object busy is read from the running tasks; list busy and the property manager's busy are read from their members; the cached busy flags and the list's polling loops go. `RunRules(RunRulesFlag)` registers its rules, so an un-awaited call is waited for. It adopts the legacy cancellation todo. It does not touch event invocation, save or MudNeatoo.

---

## Intent

- One wait: with or without a token, `WaitForTasks` awaits what the framework registered. On one thread it always returns an incomplete task while work is in flight.
- When the wait completes, nothing on the object or below it is busy, because busy is the same tasks.
- Nothing that says "busy" is kept up to date by notifications, so nothing can go stale.

---

## Framework & Architectural Alignment

- D33 (waiting awaits tasks, and busy reads tasks), D31 (one mechanism).
- WAIT-004 made the precondition true: every write, rule run and lazy load is registered with its object and the object's parents, and an object accounts before its parent.
- Single-threaded object model: the object announces busy where its running tasks start running, and idle from the tracker's completion callback, both on the thread that owns it.

---

## Constraints & Invariants

- `Save()` and `Save(token)` behave as before for work that was already registered.
- `RunRules(RunRulesFlag)` still waits for the object's running tasks before it returns, and still hands the rules' own exception to its awaiter.
- An object raises its `IsBusy` notification once when it goes busy and once when it goes idle.
- A list still reports busy while an item is, and its waits still wait for its items.
- A wait does not take the fault an idle child or item holds for its own first wait.

---

## Steps

1. Read object busy from the running tasks alone; announce busy where the running tasks start running.
2. Make `WaitForTasks()` the running tasks alone; the token overload already is.
3. Register the rules of `RunRules(RunRulesFlag)` as the object's work, outside its own wait.
4. Read list busy and the property manager's busy from their members; remove the cached flags.
5. Replace the list's polling loops with waits on its busy items.
6. Close the legacy cancellation todo.

---

## Acceptance

- [x] On one thread, `WaitForTasks()` returns an incomplete task while work is in flight and completes when it does, for work started by the setter, by `SetValue` on the property, by `RunRules(propertyName)` and by `RunRules()` `[integration]`
- [x] `RunRules()` with a flag, not awaited, is waited for by `WaitForTasks(token)` `[integration]`
- [x] `WaitForTasks()` and `WaitForTasks(token)` wait for the same work `[integration]`
- [x] A cancelled token wait throws, and the work still finishes `[integration]`
- [x] When a root's `WaitForTasks()` completes, the root, its child, its list, the list's item and their properties are not busy `[integration]`
- [x] A list's wait returns an incomplete task while an item is busy and completes when it is not `[integration]`
- [x] An object announces busy once and idle once for a write through the property `[integration]`
- [x] Build and tests green `[explicit-skip: meta-bullet]`

---

## Current State (Pre-Flight)

- `ValidateBase.IsBusy` is `RunningTasks.IsRunning || PropertyManager.IsBusy`. `WaitForTasks()` awaits the running tasks and then the property manager's scan; `WaitForTasks(token)` awaits the running tasks only.
- The object's rising `IsBusy` notification comes from a property's notification through the property manager; nothing checks the meta state when the running tasks start running.
- `RunRules(RunRulesFlag)` runs its rules through the rule manager and then awaits `RunningTasks.AllDone`. It registers nothing.
- `ValidateListBase` keeps `_cachedIsBusy` up to date at seven places and polls `IsBusy` in both waits. `ValidatePropertyManager.IsBusy` is a flag recomputed on property notifications.
- `docs/todos/waitfortasks-cancellation-missing-propertymanager.md` is the open legacy todo for the token overload's gap.
- Punchlist row pulled down from the todo: `RunRules(RunRulesFlag)` called and not awaited.

---

## Punchlist

- [x] `RunRules(RunRulesFlag)` called and not awaited is in the object's running tasks · Step 3 · done when the Acceptance bullet is pinned · AC-1 (from the WAIT-004 code review)

---

## Test Evidence

Red run before the change (`reviews/005-red.log`): 1 of the 7 tests failed, `RunRulesWithAFlag_NotAwaited_IsInTheObjectsRunningTasks`. The other six passed before the change, and that is the point of them: this plan removes a second source of truth and two polling loops without changing what a caller sees, and these tests hold the behavior fixed across the removal. WAIT-003 and WAIT-004 made them pass (their red runs are `003-red.log` and `004-red.log`). All are in `Neatoo.UnitTest.Integration.Concepts.EntityBase.WaitsAwaitTasksTests`.

| Acceptance bullet (short) | Priority | Tier declared | Test method | Tier confirmed |
|---|---|---|---|---|
| On one thread, the wait is incomplete while work is in flight, for four ways to start it | Required | `[integration]` | `OnOneThread_WaitForTasks_ReturnsAnIncompleteTask_AndCompletesWhenTheWorkDoes` (runs on one owning thread, with a timeout) | ✓ |
| `RunRules()` not awaited is in the running tasks | Required | `[integration]` | `RunRulesWithAFlag_NotAwaited_IsInTheObjectsRunningTasks`, red before | ✓ |
| The two waits wait for the same work | Required | `[integration]` | `TheTwoWaits_WaitForTheSameWork`; and every test in `FrameworkRegistersTests`, which waits with a token | ✓ |
| A cancelled token wait throws and the work finishes | Required | `[integration]` | `ATokenWait_Cancelled_Throws_AndTheWorkStillFinishes` | ✓ |
| When the root's wait completes, nothing on or below it is busy | Required | `[integration]` | `WhenTheRootsWaitCompletes_NothingOnItOrBelowItIsBusy` | ✓ |
| A list's wait suspends and completes | Required | `[integration]` | `AListsWait_ReturnsAnIncompleteTaskWhileAnItemIsBusy_AndCompletesWhenItIsNot` | ✓ |
| An object announces busy once and idle once | Required | `[integration]` | `AnObject_AnnouncesBusyOnceAndIdleOnce_ForAWriteThroughTheProperty`; `MetaStateTriggerTests.SyncIsBusyTrigger_RunsOncePerFlip_AndSettles` (existing) | ✓ |
| Build and tests green | Required | `[explicit-skip]` | `reviews/005-build.log`, `005-test.log` (2,090 passed, 2 skipped, 0 failed), `005-design-build.log`, `005-design-test.log` (235 passed) | n/a |

No existing test was changed.

---

## Gate Record

---

## Plan Amendments

---

## Notes

- `ValidatePropertyManager.WaitForTasks()` stays on the public interface and is no longer called by the object's wait.
- An awaited `RunRules()` whose rule throws hands the exception to its awaiter and, as its rules are now registered, the object's next `WaitForTasks()` reports it too. That matches `RunRules(propertyName)` and `Set{Name}`.
- A custom property (D36) that reports busy without tracking a task no longer makes its object busy and is not waited for. The object's busy and wait are its running tasks.
