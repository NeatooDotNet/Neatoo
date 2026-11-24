# #140 and #141: analysis of the wait-and-busy design, and a staged plan

Everything here comes from reading the code, the docs and the tests, plus the three throwaway tests already run on main. Nothing below marked "predicted" has been run.

## Context

zLaserHub (Blazor WASM) freezes with no exception. Two bugs combine: #140, a deserialized graph subscribes twice per level, and #141, `WaitForTasks()` spins on a busy property. Keith rejected the first #141 design (a counter and a completion source on the property) as a patch in one spot, and asked for an analysis of the whole wait-and-busy design.

## Is #141 still a problem once #140 is fixed?

Yes, but not for the hub.

- **Still reachable:** `RunRules(name)` followed by `WaitForTasks()` spins on an entity that was never deserialized (zLaserHub's test D, measured on 0.36.0). A MudNeatoo edit with an async rule in flight, followed by a Save handler's `WaitForTasks()`, reaches the same loop (read, not measured).
- **The hub's own flow stops reaching it.** With #140 fixed no run is dropped, so every run in that flow is tracked and the loop finds nothing busy. zLaserHub said the same: fixing #140 alone hides #141 there.

## How waiting works today

Neatoo tracks async work twice.

1. **Tasks.** Each entity has `RunningTasks` (`AsyncTasks`): real tasks, one `AllDone` task, `IsRunning`. A task enters it only if the code that started the write registers it: the generated setter, `Set{Name}`, the obsolete `Setter`, the `ObjectInvalid` setter, meta rules. `AddChildTask` copies it to every ancestor.
2. **Flags.** Each property has `IsSelfBusy` (a bool) and marked-busy ids (set by the rule manager around an async rule). `PropertyChanged("IsBusy")` rolls them up into cached flags on the property manager and on lists. Entity `IsBusy` is `RunningTasks.IsRunning || PropertyManager.IsBusy`.

`WaitForTasks()` awaits the tasks, then polls the flags: while a property says busy, await that property's `WaitForTasks()`. A property has no task of its own to give, so that await completes at once and the loop spins. `WaitForTasks(token)` awaits the tasks only.

## Defects

### Design

| # | Defect | Effect |
|---|---|---|
| D1 | Two sources of truth, tasks and flags, bridged by polling. | #141. A flag can't be awaited. |
| D2 | The caller registers work, not the owner. | A write through `IValidateProperty.SetValue` (every MudNeatoo input), `RunRules(name)`, an un-awaited `RunRules(flag)` and a lazy load never enter `RunningTasks`. |
| D3 | The cached busy flags are kept by notifications. | They go stale: notifications are dropped while paused, and a faulted rule clears `IsSelfBusy` without announcing it, so an entity can read busy for good. |
| D4 | The two `WaitForTasks` overloads mean different things. | `(token)`, and so `Save(token)`, skips property-level work. An open todo and a code comment admit it. The list's `(token)` polls flags but awaits task-only waits, so it can spin. |
| D5 | Work reaches ancestors two ways, neither complete. | Up through `AddChildTask` misses untracked writes. Down through the property misses lists, because a list is not an `IValidateBase`. |
| D6 | `NeatooPropertyChanged` returns a task and has several subscribers. | Invoking it returns the last handler's task only. This is how #140 dropped runs. Still reachable with a shared reference or a user subscriber. |
| D7 | `AsyncTasks` stays faulted. | After one faulted sequence, `AddTask` returns the old faulted task: later work is untracked and `AllDone` throws for good. No test, no doc. |

### Code

| # | Defect |
|---|---|
| C1 | #140: three sites subscribe a second time on deserialization (entity, list, property manager). |
| C2 | `IsSelfBusy` is one bool around overlapping writes: the first to finish clears it while the second is in flight. |
| C3 | `ValidateProperty.IsBusy`: operator precedence makes a property that holds a child ignore its own busy state. |
| C4 | `ValidateProperty.Task` keeps only the latest write's task. |
| C5 | The `ObjectInvalid` setter registers its task twice. |
| C6 | `AsyncTasks.SequenceCompleted` catches only `AggregateException` from its callback; any other exception leaves `AllDone` unset. |
| C7 | `LoadValue` with the same instance twice subscribes twice. |
| C8 | The two lazy-load property classes shadow `IsBusy` and `WaitForTasks` with `new`, in the same polling shape. |

### Tests

- No test for an async rule's fault coming out of `WaitForTasks()`; the one that existed is commented out.
- No test for a write through `SetValue` followed by `WaitForTasks()`, for `RunRules(name)` un-awaited, or for cancelling a wait.
- No test writes to a deserialized object and counts.
- No single-thread test and no `[Timeout]` anywhere, so a hang names no test (#133).
- Four `MarkBusyForTest` helpers park a task in `RunningTasks`; a redesign has to keep that working or replace it.

## What the docs promise

- "Every async operation is tracked, so `IsBusy` and `WaitForTasks()` always describe the whole object graph." (`docs/guides/async.md:5`)
- "Task tracking propagates up the graph through `Parent`." (`async.md:156`)
- When `WaitForTasks()` completes, `IsBusy` is false: called a contract in a completed todo, pinned by a Design test.
- "Async rule tasks are tracked per property, not per entity … only that field is busy." (`docs/guides/properties.md:497`)
- A rule that throws is rethrown "from `WaitForTasks()`". Nothing says what the object can do afterwards.

D2 breaks the first promise. So tracking every write at its source restores documented intent; it is not a new rule.

## Target design: tasks are the only truth

1. **The property owns its in-flight work as tasks.** A write's task and each async rule's task that marks the property are held in a task tracker on the property, the same `AsyncTasks` the entity uses. `IsBusy` on the property is "the tracker is running". `IsSelfBusy`, the id list and their notifications are derived from it. (D1, C2, C4)
2. **The owner registers, not the caller.** The property hands each task it starts to its owning entity, which puts it in `RunningTasks` and passes it up through `AddChildTask`, as the generated setter does today. Generated code stops registering. A lazy load registers the same way. (D2, D5)
3. **Waiting awaits tasks.** `WaitForTasks()` is `RunningTasks.AllDone`. No polling, no descent, no flags. `WaitForTasks(token)` is the same wait with cancellation. (D1, D4)
4. **Busy is read from tasks.** Entity `IsBusy` is `RunningTasks.IsRunning`; a list is busy while an item is. The cached flags go. "`WaitForTasks()` done means not busy" then holds by construction. (D3)
5. **Decided separately:** D6 (await every handler), D7 (what a faulted object may do next), C6.

This is a redesign of `ValidateProperty`, the property manager, `ValidateBase`, the list bases, the rule manager's busy marking, the lazy-load properties and the generator. It needs rulings (below) and its own tests. It is not patch-sized.

### Rulings the redesign needs

- **Faults (D7).** After an async rule throws, is the object usable again, and does `WaitForTasks()` throw once or every time?
- **MudNeatoo.** With every write tracked, a UI edit whose rule throws also faults the entity's `WaitForTasks()`. The docs already say so; today it is true only for setter writes.
- **Public surface.** `AddMarkedBusy`, `RemoveMarkedBusy` and `IsMarkedBusy` are public on `IValidateProperty`, documented as framework use. Keep them over the tracker, or remove them.
- **`RunRules(flag)` un-awaited.** Tracked or documented as awaited-only (#89 is the neighbouring open question).

## Staging (decided by Keith, 2026-11-24)

**Stage 1, now: fix #140 and release 0.36.1.** Small, fully understood, and it unblocks zLaserHub. #141 stays open and reachable through MudNeatoo and `RunRules(name)`, as it has been since 10.10.0; the release notes say so.

**Stage 2: the wait-and-busy redesign as an iterative todo, closing #141.** Rulings first, then the design above, then a minor release. The todo is created after 0.36.1 is out; this plan's analysis is its starting material. Nothing in stage 2 is built under this plan.

## What approving this plan starts

1. Stage 1 below, through the PR. Merging and tagging stay with Keith.
2. A comment on #141 recording the analysis: still reachable after #140, the design defects, and that it closes with the redesign.
3. A message to zLaserHub: 0.36.1 carries #140 only, applications change nothing, and #141 remains open.
4. After the release, the iterative todo for stage 2, starting with the rulings.

## Stage 1 detail: #140

Rule: a subscription is made where an object is attached, and nowhere else. `OnDeserialized` subscribes to nothing.

| Link | Who already owns the subscription | Change |
|---|---|---|
| Entity to its property manager | The constructor (`ValidateBase.cs:209-210`). The manager is never replaced. | Delete the two `+=` lines in `ValidateBase.OnDeserialized` (:603-604). |
| List to each item | `InsertItem` (`ValidateListBase.cs:142-143`), reached by the converter's `list.Add`, paused or not. | Delete the two `+=` lines in `ValidateListBase.OnDeserialized` (:308-309). Keep `SetParent` and `ResumeAllActions`. |
| Property manager to each property | `Register` and `GetProperty` for constructor-built objects. Nobody for the objects the converter swaps in. | `SetProperties` (`ValidatePropertyManager.cs:199-205`) moves the subscription from the outgoing object to the incoming one, skipping when they are the same object. `OnDeserialized` (:237-245) only recalculates validity. |

Tests, written first and seen red (MSTest, real classes, synchronous rules, in `src/Neatoo.UnitTest/Integration/Concepts/Serialization/`):

| Test | Red today |
|---|---|
| Deserialized entity, one write: rule body 1, `PropertyChanged(Name)` 1 | 2 / 2 |
| Deserialized parent, list, child; one child write: child rule 1, list `HandleNeatooPropertyChanged` 1 (for that property name only), parent rule on `t => t.Items![0].X` 1 | 2 / 4 / 8 |
| The same after a second round trip, and through the `ClientServerTestBase` pair | 2 / 4 / 8 |
| The same on a created graph | green (control) |
| Deserialized parent with a loaded lazy child (`LazyLoadEntityObject`): one child write reaches the root once | predicted 8 |

Delivery: branch off main; one commit for the fix and tests, one for 0.36.1 and `docs/release-notes/v0.36.1.md`; PR closing #140; Keith merges; tag on his go-ahead; confirm both packages on NuGet; tell zLaserHub the version.

Verification: `dotnet build` and `dotnet test` on `src/Neatoo.sln` and `src/Design/Design.sln`, output to log files. Any existing expectation that changes is reported with its reason. None is predicted: no test asserts the doubled behavior.
