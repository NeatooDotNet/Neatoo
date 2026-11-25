# The framework registers every reaction

**Plan #:** 004
**Date:** 2026-11-24
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-1
**Status:** In Progress
**Last Updated:** 2026-11-24
**Plan-review opt-in:** No (the design is D32, approved by Keith on 2026-11-24)
**Code-review opt-in:** Yes (it changes what every entity registers, the generated code and a public member)
**Branch:** wait-004-framework-registers
**PR:** —

---

## Scope

Moves registration from the caller to the owner: a property reports each write it starts to its entity, and the entity registers each `RunRules(propertyName)` call it is asked for, so a write through the setter, `Set{Name}`, `IValidateProperty.SetValue`, a `RunRules(propertyName)` call, a meta rule or a lazy load is registered the same way. The entity registers what is reported with itself first and then with its parents. Generated code and the hand-written setters stop registering. It does not change how waits are composed or how busy is read, which are WAIT-005.

---

## Intent

- Whatever starts async work on an object, the object's running tasks hold it, and so do its parents'. A caller that awaits tasks only (`WaitForTasks(token)`, and so `Save(token)`) waits for a write made through a MudNeatoo input.
- An object accounts for a task before its parent does, so when a parent's wait completes its children have finished accounting.
- One exception is reported as one exception, however many registered tasks carried it.

---

## Framework & Architectural Alignment

- D32 (the owner registers, not the caller), D34 (a fault surfaces once), D35 (framework-use members may go), D36 (the report path is supplied through the property manager the entity already holds; nothing is `new`ed in a base class).
- Single-threaded object model: registration, accounting and notifications stay on the thread that owns the graph (WAIT-003).
- LazyLoad is pending removal (D24). Its load is registered so that WAIT-005 can stop descending through properties; nothing else is built on it.

---

## Constraints & Invariants

- A setter, `Set{Name}` and `SetValue` return what they returned before, and a synchronous fault still throws from the setter.
- An awaited operation still hands its exception to its awaiter. The tracker reporting it as well is existing behavior for `Set{Name}`; it now holds for the other entry points too.
- `RunRules(RunRulesFlag)` awaits the object's running tasks inside itself; registering a rule run must not make it wait for itself.
- Nothing is registered while an object is paused that was not registered before, unless the work is really in flight.
- The lazy-load tests keep their current outcome on a failed load.

---

## Steps

1. Give the property a way to report a task to its owner, supplied by the property manager at attach; the entity supplies its end once, where it subscribes to its manager.
2. The property reports each write (the whole write, through its own accounting) while it is in flight.
3. The entity registers each `RunRules(propertyName)` run it is asked for (amended, see Plan Amendments).
4. The entity registers what is reported with itself first, then passes it to its parent.
5. The generated setter and `Set{Name}`, `Setter`, the `ObjectInvalid` setter and the meta-rule path stop registering.
6. The tracker reports an exception once when it arrives through more than one task.
7. A lazy load in progress is registered with the owner of its property.
8. Dropped (see Plan Amendments): adopting the work of a busy object that is attached.
9. `AddChildTask` leaves the public interface.

---

## Acceptance

- [x] A write through `IValidateProperty.SetValue` with an async rule is waited for by `WaitForTasks(token)` on the object `[integration]`
- [x] `RunRules(propertyName)`, not awaited, is waited for by `WaitForTasks(token)` on the object `[integration]`
- [x] A write through a child's property is waited for by `WaitForTasks(token)` on the root, through a child property and through a list `[integration]`
- [x] A rule that throws after a setter write surfaces from `WaitForTasks()` once, carrying the exception once `[integration]`
- [x] `Save(token)` on a root succeeds after a write to a child with an async rule, with no synchronization context `[integration]`
- [x] A lazy load in progress is waited for by `WaitForTasks(token)` on the parent `[integration]`
- [x] A lazy load that the property lets go of while it loads stops holding the parent's running tasks (added at the gate) `[integration]`
- [x] The generated setter and `Set{Name}` register nothing `[unit]`
- [x] Build and tests green `[explicit-skip: meta-bullet]`

---

## Current State (Pre-Flight)

- Registration sites today, all parent first and then the object itself: the generated setter and `Set{Name}` (`src/Neatoo.BaseGenerator/Generators/PropertyGenerator.cs`, four template strings), `ValidateBase.Setter`, the `ObjectInvalid` setter (which registers twice), `RunMetaPropertyRules`, and `AddChildTask` (public on `IValidateBase`, also on `IValidateBaseInternal`).
- `ValidateProperty.SetPrivateValue` assigns `Task` from `OnValueNeatooPropertyChanged` at three places and throws a synchronous fault. Since WAIT-003 that task completes after the property's own accounting.
- `RuleManager.RunRule` builds the run, hands it to the trigger properties and awaits their accounting. It reports nothing to the target.
- `ValidateBase.RunRules(RunRulesFlag)` awaits `RunningTasks.AllDone` after the rules.
- A list item's parent is the list's parent, so a task passed up from an item reaches the owning object without the list.
- `EntityLazyLoad.WaitForTasks()` returns `LoadTask` while a load runs. The lazy-load property classes translate `IsLoading` into an `IsBusy` notification.
- The generated shape is described in `src/Design/Design.Domain/PropertySystem/PropertyBasics.cs`, the snippet source for four doc and skill files.
- A property refuses a busy child (`ChildObjectBusyException`) and a list refuses a busy item (`InvalidOperationException`), so a parent is never handed an object with work already in flight.
- Punchlist row pulled down from the todo: an object must account before its parent, pinned by `Save(token)` on a root after a child write.

---

## Punchlist

- [x] An object accounts before its parent; `Save(token)` on a root after a child write with an async rule succeeds · the registration order in Step 4 · done when the Acceptance bullet for `Save(token)` is pinned · AC-4 (from the WAIT-003 code review, round 2)

---

## Test Evidence

Red run before the change (`reviews/004-red.log`): 7 of the 9 tests then in `FrameworkRegistersTests` failed. The four "is in the running tasks" tests and the lazy-load test failed because the token wait was already complete. `SaveWithAToken_OnARoot_SucceedsAfterAWriteToAChildWithAnAsyncRule` failed with `SaveOperationException` (busy), which measures the callout the WAIT-003 code review had traced. The seventh was the adoption test, which failed with `ChildObjectBusyException`; it became the guard named below. The two "carrying the exception once" tests passed before the change, because only the write was registered then. With the tracker's de-duplication disabled they fail with two exceptions (`reviews/004-red-round1.log`, which also shows the ValidateBase lazy-load test red with its report disabled). The two lazy-load tests added at the gate were red before their fix (`reviews/004-red-lazy.log`). Integration tests are in `Neatoo.UnitTest.Integration.Concepts.EntityBase.FrameworkRegistersTests`.

| Acceptance bullet (short) | Priority | Tier declared | Test method | Tier confirmed |
|---|---|---|---|---|
| A write through the property is in the object's running tasks | Required | `[integration]` | `AWriteThroughTheProperty_IsInTheObjectsRunningTasks` | ✓ |
| `RunRules(propertyName)`, not awaited, is in the object's running tasks | Required | `[integration]` | `RunRulesByPropertyName_NotAwaited_IsInTheObjectsRunningTasks` | ✓ |
| A child's write is in the root's running tasks, by property and by list | Required | `[integration]` | `AWriteThroughAChildsProperty_IsInTheRootsRunningTasks`, `AWriteThroughAListItemsProperty_IsInTheRootsRunningTasks` | ✓ |
| A thrown rule surfaces once, carrying the exception once | Required | `[integration]` | `ARuleThatThrowsAfterASetterWrite_SurfacesOnce_CarryingTheExceptionOnce`, `AChildsRuleThatThrows_SurfacesFromTheRoot_CarryingTheExceptionOnce` | ✓ |
| `Save(token)` on a root after a child write | Required | `[integration]` | `SaveWithAToken_OnARoot_SucceedsAfterAWriteToAChildWithAnAsyncRule` | ✓ |
| A lazy load in progress is in the parent's running tasks | Required | `[integration]` | `ALazyLoadInProgress_IsInTheParentsRunningTasks`, `ALazyLoadInProgress_OnAValidateObject_IsInItsRunningTasks`, `ALazyLoadAlreadyInProgressWhenItIsAssigned_IsInTheParentsRunningTasks` | ✓ |
| A lazy load replaced while it loads stops holding the parent (added at the gate) | Required | `[integration]` | `ALazyLoadReplacedWhileItLoads_NoLongerHoldsTheParentsRunningTasks` | ✓ |
| Generated code registers nothing | Required | `[unit]` | `Neatoo.BaseGenerator.Tests.AwaitableSetterGenerationTests.GeneratedSetterAndSetName_RegisterNothing_TheOwnerRegisters` (public, private and protected setters) | ✓ |
| Build and tests green | Required | `[explicit-skip]` | `reviews/004-build.log`, `004-test.log` (2,083 passed, 2 skipped, 0 failed), `004-design-build.log`, `004-design-test.log` (235 passed) | n/a |

One more test is a guard, not a bullet: `ABusyObject_CannotBeAssignedToAParent_SoAParentNeverHoldsWorkItWasNotToldOf` pins the existing refusal that makes Step 8 unnecessary. It passed before the change.

Existing tests changed, each with its reason:

| Test | Claimed before | Claims now | Why |
|---|---|---|---|
| `AwaitableSetterGenerationTests.PublicSet_GeneratesPublicSetterOnClassAndInterface` | The generated `SetName` contains `return task;` ("returns the cascade's Task") | It contains `return NameProperty.SetValue(value);` | The same claim, with the generated text D32 produces: the method no longer holds the task in a local to register it. |

Checked-in generated files follow the change: the Person example's two generated classes (setters only write) and its KnockOff stubs (`AddChildTask` is off `IValidateBase`).

---

## Gate Record

- Round 1 (2026-11-24): test review APPROVED, no veto; both callouts taken (the protected setter is in the generator test; the ValidateBase lazy-load report has its own test) and its open question answered by a run (the "once" tests fail with de-duplication disabled) — `reviews/004-test-review.md`. Code review APPROVED, no veto; both callouts fixed with tests seen red first — `reviews/004-code-review.md`:
  - A lazy value replaced while it loaded left the owner busy for good, because the property had unsubscribed from it. Fixed: the report follows the value the property holds.
  - A lazy value already loading when it was assigned was never reported. Fixed by the same change.
  - Its note that an un-awaited `RunRules(RunRulesFlag)` is busy on its properties but not in the object's running tasks is punched to the todo for WAIT-005. The gate is closed.

---

## Plan Amendments

### 2026-11-24 — The entity registers its rule runs; the rule manager reports nothing

- **Section affected:** Step 3
- **Original said:** the rule manager reports each rule run to its target.
- **What changed:** `ValidateBase.RunRules(propertyName)` registers its own task when it is in flight. The meta-rule path calls it and no longer registers. The rule manager is unchanged.
- **Why:** the run that matters to a wait is the whole `RunRules(propertyName)` call, which recalculates validity and checks the meta state after the rules. Registering inside the rule manager would have let a wait complete before that. It is also one registration for every caller, where the rule manager would have registered once per rule.
- **Discovery Log:** n/a (same intent, different seam)

### 2026-11-24 — No adoption of a busy object

- **Section affected:** Step 8, Acceptance (one bullet removed)
- **Original said:** a busy object attached to a parent has its work in flight registered with that parent.
- **What changed:** nothing is built. The step and its bullet are dropped.
- **Why:** a property refuses a busy child and a list refuses a busy item, so the case cannot arise. The first is now pinned by a guard test; it had none.
- **Discovery Log:** 2026-11-24 / WAIT-004

### 2026-11-24 — A lazy load is reported as a task that ends with the load

- **Section affected:** Step 7
- **Original said:** a lazy load in progress is registered with the owner of its property.
- **What changed:** the lazy-load property reports a task of its own that it completes when `IsLoading` falls. It does not report the load's task.
- **Why:** the load's task is not yet assigned when `IsLoading` is raised, and a failed load is reported through `HasLoadError`, not through a wait. A task that only ends keeps both.
- **Discovery Log:** n/a (mechanical)

---

## Notes

- `AddChildTask` is now `protected virtual` on `ValidateBase` and stays on the internal interface. It registers with the object first and then passes the task up.
- `RunRules(RunRulesFlag)` is not registered: it awaits the object's running tasks itself, and it runs its rules through the rule manager, not through `RunRules(propertyName)`. Called and not awaited, its runs are busy on their properties but not in the object's running tasks; that is on the todo's Punchlist for WAIT-005.
- Doc and skill snippets that show the generated setter were regenerated by `dotnet mdsnippets` from `PropertyBasics.cs`. The prose around them is WAIT-008's.
