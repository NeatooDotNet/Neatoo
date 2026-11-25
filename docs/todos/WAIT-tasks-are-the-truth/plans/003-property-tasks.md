# A property holds its work as tasks

**Plan #:** 003
**Date:** 2026-11-24
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-6, AC-3
**Status:** In Progress
**Last Updated:** 2026-11-24
**Plan-review opt-in:** No (the design is D31, approved by Keith on 2026-11-24 with the old and new architecture side by side)
**Code-review opt-in:** Yes (it replaces the property's busy state and removes public members)
**Branch:** wait-003-property-tasks
**PR:** —

---

## Scope

Replaces the property's busy bool and its list of busy ids with a task tracker on the property: each write's task and each async rule run that names the property enter it, the property is busy while it is running, and waiting on the property awaits it. The rule manager hands the property a task where it handed it an id. The property manager supplies each property its tracker when the property enters the bag, and its wait awaits the properties' tasks, not their busy flags. The framework-use busy-marking members are removed. It does not change what the entity registers, the entity's own `WaitForTasks` composition, the token overload, lists, or the cached busy flags, which are WAIT-004 and WAIT-005.

---

## Intent

- A property is busy exactly while a write to it or an async rule that names it is in flight: two overlapping writes keep it busy until both finish, and a rule that throws leaves it not busy.
- Waiting on a busy property suspends. So `WaitForTasks()` on the entity returns control while a rule is in flight, however the rule was started: this is where the freeze of #141 ends.
- The property's busy notification is raised when it goes busy and when it goes idle, including after a fault, so nothing that listens is left stale.

---

## Framework & Architectural Alignment

- D31 (one mechanism, the task tracker, at the entity and at the property), D35 (framework-use members may go), D36 (the tracker comes through the factory the property manager already holds).
- A property's tracker reports no fault: faults surface from the entity's `WaitForTasks()` (D34). The property tracks completion only.
- Attach is where a property gets what it needs from its manager, as with subscriptions since 0.36.1: on register, on lazy creation, and when the serializer swaps a property in.

---

## Constraints & Invariants

- `Set{Name}` and `SetValue` still return the task of the whole cascade, and a synchronous fault still throws from the setter.
- A property that holds a child object is busy while the child is, and waiting on it waits for the child.
- Lazy-load properties keep their documented waiting: an in-progress load is waited for.
- Rule execution order, message handling and what rules run are unchanged.
- Entity `IsBusy` is false when `WaitForTasks()` returns, as every existing test that asserts it expects.

---

## Steps

1. Let the property factory create trackers, from the factory registered in DI.
2. Have the property manager give each property its tracker at attach.
3. On the property, track each write's task and each async rule run handed to it; derive busy and the property's wait from the tracker; raise the busy notification on both transitions.
4. Have the rule manager hand each trigger property the task of the rule run, covering the rule and the application of its messages.
5. Remove the busy bool, the id list and the mark and unmark members; bring the lazy-load property classes onto the same tracker.
6. Make the property manager's wait await its properties' waits, not poll their busy state.

---

## Acceptance

- [x] A property is busy while its async rule runs and not busy after `[integration]`
- [x] With two overlapping writes, the property stays busy until both have finished `[integration]`
- [x] After an async rule throws, neither the property nor the entity is busy `[integration]`
- [x] Waiting on a property is incomplete while its rule runs and completes when it does `[integration]`
- [x] `WaitForTasks()` on an entity returns control while a rule started by `RunRules(propertyName)` is in flight, and completes when the rule does `[integration]`
- [x] The same for a write made through `IValidateProperty.SetValue` `[integration]`
- [x] A property on a deserialized object is busy while its async rule runs `[integration]`
- [x] When an awaited write or rule run completes, the property is already idle and has raised its busy notification twice, busy then idle (added at implementation, see Plan Amendments) `[integration]`
- [x] Build and tests green `[explicit-skip: meta-bullet]`

---

## Current State (Pre-Flight)

- `ValidateProperty<T>` (`src/Neatoo/Internal/ValidateProperty.cs`): `IsSelfBusy` (a bool set around the awaited handler task in `OnValueNeatooPropertyChanged`, cleared silently by a failsafe on a fault), `_isMarkedBusy` ids with `AddMarkedBusy` / `RemoveMarkedBusy` / `IsMarkedBusy`, `IsBusy` (with an operator-precedence slip: a property holding a child ignores its own state), `WaitForTasks()` awaiting only a child value.
- `IValidateProperty` (`src/Neatoo/IValidateProperty.cs`) declares `AddMarkedBusy` and `RemoveMarkedBusy`. `IsSelfBusy` and `IsMarkedBusy` are public on the class only.
- `RuleManager.RunRule` (`src/Neatoo/Rules/RuleManager.cs`) marks each trigger property busy with an execution id while the rule's task is incomplete and unmarks in `finally`.
- `LazyLoadValidateProperty` and `LazyLoadEntityProperty` re-declare `IsBusy` and `WaitForTasks` with `new` and read `IsSelfBusy` and `IsMarkedBusy`.
- `ValidatePropertyManager` holds an `IFactory` (`DefaultFactory`, a DI singleton with a parameterless constructor) and attaches properties in `Register`, `GetProperty` and `SetProperties`. Its `WaitForTasks()` polls `IsBusy`.
- Tests that read the removed members: one, `ValidatePropertyTests.IsSelfBusy_InitialState_ReturnsFalse`.
- Docs that name the removed members: `docs/reference/api.md`, `src/Design/CLAUDE-DESIGN.md`. They are WAIT-008's.
- A checked-in generated file follows the interface: the KnockOff stubs in `src/Examples/Person/Person.DomainModel.Tests/Generated/` lose their `AddMarkedBusy` and `RemoveMarkedBusy` members on the next build.

---

## Punchlist

---

## Test Evidence

Red run before the change (`reviews/003-red.log`): 6 of the first 8 tests failed. The two `WaitForTasks_ReturnsControl...` tests failed at the five-second limit, which is the freeze of #141. Two passed before the change because the behavior already held: `Property_IsBusyWhileItsAsyncRuleRuns_AndNotAfter` and `Property_RaisesIsBusyWhenItGoesBusyAndWhenItGoesIdle`. The two `WhenAnAwaited...` tests were added during implementation and seen red with the two accounting awaits disabled (`reviews/003-red-awaited.log`). All are in `Neatoo.UnitTest.Integration.Concepts.ValidateBase.PropertyTasksTests`.

| Acceptance bullet (short) | Priority | Tier declared | Test method | Tier confirmed |
|---|---|---|---|---|
| Busy while its async rule runs, not after | Required | `[integration]` | `Property_IsBusyWhileItsAsyncRuleRuns_AndNotAfter`, `Property_RaisesIsBusyWhenItGoesBusyAndWhenItGoesIdle` | ✓ |
| Two overlapping writes keep it busy until both finish | Required | `[integration]` | `Property_WithTwoOverlappingWrites_StaysBusyUntilBothHaveFinished` | ✓ |
| After a rule throws, property and entity are not busy | Required | `[integration]` | `AfterAnAsyncRuleThrows_NeitherThePropertyNorTheEntityIsBusy` | ✓ |
| Waiting on a property suspends and completes | Required | `[integration]` | `Property_WaitForTasks_IsIncompleteWhileItsRuleRuns_AndCompletesWhenItDoes` | ✓ |
| Entity wait returns control after `RunRules(propertyName)` | Required | `[integration]` | `WaitForTasks_ReturnsControlWhileARuleStartedByRunRulesIsInFlight_AndCompletesWhenItDoes` | ✓ |
| Entity wait returns control after `IValidateProperty.SetValue` | Required | `[integration]` | `WaitForTasks_ReturnsControlWhileAWriteThroughThePropertyIsInFlight_AndCompletesWhenItDoes` | ✓ |
| A deserialized object's property is busy while its rule runs | Required | `[integration]` | `Property_OnADeserializedObject_IsBusyWhileItsAsyncRuleRuns` | ✓ |
| An awaited write or rule run leaves the property idle, and it has said so | Required | `[integration]` | `WhenAnAwaitedRuleRunCompletes_ThePropertyIsAlreadyIdle_AndHasSaidSo`, `WhenAnAwaitedWriteCompletes_ThePropertyIsAlreadyIdle_AndHasSaidSo` | ✓ |
| Build and tests green | Required | `[explicit-skip]` | `reviews/003-build.log`, `003-test.log` (2,062 passed, 2 skipped, 0 failed), `003-design-build.log`, `003-design-test.log` (235 passed) | n/a |

Existing tests changed, each with its reason:

| Test | Claimed before | Claims now | Why |
|---|---|---|---|
| `ValidatePropertyTests.IsSelfBusy_InitialState_ReturnsFalse` | A new property's `IsSelfBusy` is false | Removed | `IsSelfBusy` no longer exists (D31, D35). `IsBusy_InitialState_ReturnsFalse` beside it still pins the initial state. |
| `AsyncRuleFaultTests.ValidateBase_TakesItsTrackerFromTheRegisteredFactory` | The registered factory was called once, and its one tracker is the object's | The object's tracker is one of those the registered factory supplied, and it supplied at least two | The same factory now supplies each property's tracker (D31, D36), so it is called more than once. |
| `TrackerFromServicesTests.EntityBase_TakesItsTrackerFromTheRegisteredFactory` | The same, for `EntityBase` | The same change | The same reason. |

---

## Gate Record

- Not run. The plan stopped before the gate on 2026-11-24 for Keith: an existing test outside this plan, `AsyncFlowTests_Property_PropertyChangedEvents`, fails intermittently, on the arc before this plan as well as after it. See Notes and the todo's Parked row.

---

## Plan Amendments

### 2026-11-24 — An operation is not over until its properties have accounted for it

- **Section affected:** Steps 3 and 4, Acceptance (one bullet added)
- **Original said:** the property tracks each write's task and each rule run handed to it.
- **What changed:** tracking returns a task that completes when the property has accounted for the work: if it was the last in flight, the property is idle and has raised its notification. The write and the rule run await it before their own task completes. `AsyncTasks.AddTask` returns that accounting task in full (unwrapped), as its interface documentation already said.
- **Why:** the tracker does its accounting in a continuation. Left unawaited, an awaited `RunRules` or write returned while the property was still busy, and the idle notification arrived later on another thread, where it raced the caller's next write. The old id list was cleared inside the rule run, so this ordering held before. `SyncIsBusyTrigger_RunsOncePerFlip_AndSettles` failed in 3 runs of 8 without it, and in none of 2,000 iterations of a throwaway probe with it.
- **Discovery Log:** 2026-11-24 / WAIT-003

### 2026-11-24 — The property factory is registered transient

- **Section affected:** Step 1
- **Original said:** let the property factory create trackers, from the factory registered in DI.
- **What changed:** `DefaultFactory` takes the registered `CreateAsyncTasks` and is registered transient, not singleton. Its parameterless constructor stays, with the framework tracker as the default. `IFactory` gains `CreateAsyncTasks()`, which is a compile break for a consumer's own `IFactory`.
- **Why:** `CreateAsyncTasks` is registered transient and resolves from the caller's scope. A singleton that captured it would resolve every property's tracker from the root provider.
- **Discovery Log:** n/a (mechanical)

---

## Notes

- **Stopped before the gate, 2026-11-24.** The code and tests of this plan are complete and the one full-suite run is green. Nothing is merged.
- **Tried and taken back out:** registering a busy property's work in the entity's tracker and reading entity `IsBusy` from the tracker alone. It reduced the first flake without removing it and broke `ParentIsBusy_AfterExplicitChildLoad`, because lazy loads are not registered yet. It is WAIT-004 and WAIT-005's work and was not the cause; the cause is in the first amendment above.
- **The flaky test that stopped the loop.** `AsyncFlowTests.AsyncFlowTests_Property_PropertyChangedEvents` failed at its last entity-level assertion (the entity raised no `IsBusy` after its work finished) in 2 of 15 full-suite repeat runs on this branch. In isolation: 2 of 40 on `wait-arc` before this plan (aa174d2), 0 of 40 on this branch, 0 of 60 on v0.36.1. Likely mechanism, read and not measured: `ValidateBase.CheckIfMetaPropertiesChanged` compares the cached state with the current state, then `ResetMetaState` reads the current state a second time. When another thread ends the last work between the two reads, the baseline becomes "not busy" with no notification raised, and the thread that ended the work then finds nothing changed. The counts cannot say whether WAIT-002 changed the timing or the rate was always low.
