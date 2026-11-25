## Code Review — WAIT-005 — [2026-11-24]

**Verdict: APPROVED**
**Logs:** build PASSED, 0 errors, 97 warnings, the same count as `004-build.log` (`reviews/005-build.log`); design build PASSED, 0 warnings (`reviews/005-design-build.log`). Tests: 2,090 passed, 2 skipped, 0 failed (`reviews/005-test.log`: Neatoo.UnitTest 1,928 + 2 skipped, BaseGenerator 51, MudNeatoo 33, Analyzers 23, Person 55). Design.Tests: 235 passed (`reviews/005-design-test.log`). Neatoo.UnitTest moves from 1,921 to 1,928, which matches the seven new tests, and no test was removed. The two skipped tests were already skipped in `004-test.log`. `reviews/005-red.log` shows 7 run and 1 failed (`RunRulesWithAFlag_NotAwaited_IsInTheObjectsRunningTasks`), as the plan states.

### Direction & shape

The deliverable landed where D33 puts it.
- **The object.** `ValidateBase.IsBusy` is `RunningTasks.IsRunning` (`src/Neatoo/ValidateBase.cs:172`). Both waits are the tracker alone (`:651-654`, `:670-673`). With no cancellable token, `AsyncTasks.WaitForCompletion` awaits the same `AllDone`, so AC-5 holds by construction.
- **Rising and falling.** The object goes busy in `AddChildTask` (`:546-563`). It goes idle in the tracker's `OnFullSequenceComplete` (`:219-223`).
- **Lists and the property manager.** List busy and the manager's busy are now computed (`ValidateListBase.cs:77`, `ValidatePropertyManager.cs:54`). Every write to `_cachedIsBusy` is gone, and so are both polling loops. `WaitForItems` (`ValidateListBase.cs:440-455`) has the same shape as `ValidatePropertyManager.WaitForTasks`: it asks only busy items for their waits, so it does not take an idle item's held fault.
- **`RunRules(RunRulesFlag)`.** It registers the rules task only, not its own wait on `AllDone`, so it does not wait for itself (`:970-978`).

I traced the Constraints:
- **Exceptions and cancellation.** A rule's exception still reaches the awaiter, from `await rules`, before `AllDone` is touched. A cancelled run is a Canceled task with no `Exception`, so `AsyncTasks.SequenceCompleted` (`AsyncTasks.cs:208`) records nothing and the next wait is clean. `MarkInvalid("Validation cancelled")` is unchanged.
- **One rising and one falling notification.** For a write through the property, the property's own `IsBusy` notification reaches `CheckIfMetaPropertiesChanged` while the object's tracker is still empty, so nothing is raised there. The object then rises once, in `RunRules(string)` → `AddChildTask`. Later registrations find `wasRunning` true and raise nothing.
- **Ordering.** A child registers before its parent, so the child accounts first. Its falling notification reaches the parent while the parent is still running, so the parent raises nothing until its own callback runs.
- **What the waits cover.** With the property scan removed, I looked for work in flight that the object's wait would now miss. Every write is reported from `SetPrivateValue`. Rule runs are registered by `RunRules(string)` and now by `RunRules(RunRulesFlag)`. Lazy loads are reported since WAIT-004 round 1. A child's work passes up through `Parent`.
- **What is left outside.** The paths that are not covered are listed below. Only one is reachable by a user action, and it was not covered before this plan either.
- **Consumers.** EntityBase (`IsSavable`, `Save`, `Save(token)`, `MarkUnmodified`) reads `IsBusy` and the token wait. Both are now the tracker, so these agree with each other. MudNeatoo reads `EntityProperty.IsBusy` and `EntityProperty.WaitForTasks()` only, and neither changed, so nothing there now reads a different truth.

No reflection was added. Nothing in a base class is built with `new`.

Two pieces of stale prose fall to WAIT-008 (AC-9):
- `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs:24-25` still says the object's wait reaches lazy loads through `PropertyManager.WaitForTasks()`.
- `src/Design/CLAUDE-DESIGN.md:376-381` still shows `AddMarkedBusy` / `RemoveMarkedBusy`.

### Veto-tier
None.

### Callouts

- **`src/Neatoo/ValidateListBase.cs:135-156` (`InsertItem`), with `:77` and `ValidateBase.cs:651` · High (traced, not run) · `Affects: AC-4` · `Reachable by:` a user action that adds a `ValidateBase` item to a `ValidateListBase` held by an object while the item's async rule is in flight. For example, a page creates an item, sets a field that has an async rule, and calls `list.Add(item)` straight away.**
  - **The gap.** `ValidateListBase` has no busy guard, so it accepts an item that is already busy. `EntityListBase` refuses one (`EntityListBase.cs:231`), and only when the list is not paused.
  - **What happens.** The item's work was registered while `item.Parent` was null, so only the item's own tracker holds it. `InsertItem` then sets `Parent` to the owner, but that does not move work already in flight.
  - **The result.** The owner's `WaitForTasks()` completes and `owner.IsBusy` is false, while `list.IsBusy` and `item.IsBusy` are true. So AC-4 ("IsBusy is false on the object and its descendants") fails.
  - **Not a regression.** Before this plan the owner did not wait either: `ValidateProperty.ValueAsBase` is `Value as IValidateBase`, a list is not one, so the old property scan never reached list items.
  - **Why it matters now.** The WAIT-004 Discovery Log entry ("a list refuses a busy item, so a parent is never handed work already in flight") holds for `EntityListBase` only. D33's promise now depends on that entry.
  - **Remedy.** One option is the guard `EntityListBase` already has.
- **`src/Neatoo/ValidateListBase.cs:77`, read from `CheckIfMetaPropertiesChanged` (`:399`) and `ResetMetaState` (`:410`) · Medium (complexity traced; cost not measured) · `Affects: AC-4` · `Reachable by:` any list with many items: filling it by a fetch or by deserialization, or editing its items.**
  - **The cost.** List `IsBusy` now walks every item on every read. It is read twice on every item `PropertyChanged` (`HandlePropertyChanged` → `CheckIfMetaPropertiesChanged`) and on every insert, remove, replace and clear, paused or not. Filling a list of n items is therefore O(n²) reads of `IsBusy`, and each item edit costs O(n).
  - **History.** This undoes the caching that shipped in v0.7.1 (`docs/release-notes/v0.7.1.md:11-28`, `docs/todos/completed/list-isvalid-ismodified-caching.md` Phase 2). That change was made for exactly this read path.
  - **The ruling.** D31 explicitly supersedes "the cached busy flags ... on lists", so removing the cache is ruled.
  - **What is missing.** The plan's Notes do not record that it reverses v0.7.1 or what that costs. `docs/guides/collections.md:296` and `:435-437` still describe the O(1) algorithm for `IsBusy`.

### Could not verify
- The O(n) cost in the second callout has not been measured on a large list. No test or benchmark covers it.
- `005-test.log` is at minimal verbosity, so the seven new tests show up only in the count change and by name in `005-red.log`.

### Theoretical (not triaged)
- **`RuleManager.RunRule` or `RunRules` called directly from entity code, not awaited.** The run is tracked on its trigger properties but never registered with the object. Before this plan the property scan covered it; now `IsBusy` is false and the wait does not wait. The only direct caller, `ValidateListBaseRuleTests.cs:39`, runs a sync rule, awaited inside a parent rule that is registered.
- **`ValidateProperty.LoadValue` handed a child that is already busy.** There is no guard, unlike `HandleNonNullValue`. The parameterless wait used to reach that child through the property scan; now it does not. No caller in the repo does this.
- **A paused object now announces busy.** Under a factory operation's pause, `AddChildTask` and the falling callback now raise `NeatooPropertyChanged("IsBusy")` (and `IsValid`/`IsSelfValid` if they changed). This happens for `await RunRules(RunRulesFlag.All)` inside `[Insert]`/`[Update]`, D6's recommended form, because `RaiseIfChanged` (`ValidateBase.cs:321`) is not gated on `IsPaused`. The one live subscriber found is `NeatooValidationSummary.OnNeatooPropertyChanged`, during a local, non-remote save. Its only effect is an extra `StateHasChanged`. This extends the pause item in WAIT-004's Theoretical list.
- **Order of the rising notifications.** The property raises `IsBusy` before the object does. A listener on the property's `IsBusy` that reads `entity.IsBusy` sees false. On the way up, a parent raises before its child. The state is consistent at each point; only the order changed.
- **An un-awaited `RunRules(RunRulesFlag)` can take another task's fault.** It observes `AllDone` after its rules finish. If another task in the same sequence faults and no other wait holds the sequence, that un-awaited task takes the fault, and the next `WaitForTasks()` is clean. The object stays invalid (D34). This is not new.
- **WAIT-008's Scope line is out of date.** It says WAIT-008 moves the legacy cancellation todo to completed, but WAIT-005 has already done it.

### Read report
- Read beyond the brief:
  - `AsyncTasks.cs` in full (observers, held faults, Canceled tasks).
  - `ValidateProperty.cs` in full (`TrackTask`, `ReportToOwner`, `LoadValue` and `HandleNonNullValue` guards).
  - `LazyLoadValidateProperty.cs:94-340`: the report path and the `new` `IsBusy` / `WaitForTasks`.
  - `RuleManager.RunRule` / `RunRules`, `RuleManager.cs:602-745`.
  - `EntityBase.cs:260-533`.
  - The `EntityListBase` insert and replace guards.
  - The busy reads in MudNeatoo, and `NeatooValidationSummary.razor.cs`.
  - The `NeatooPropertyChanged` subscribers in Design.App and the examples.
  - `ValidateListBaseRuleTests.cs`.
  - `FrameworkRegistersObjects.cs` and `OneThreadAtATime.cs`.
  - `docs/release-notes/v0.7.1.md` and `docs/todos/completed/list-isvalid-ismodified-caching.md`.
  - `reviews/005-test-review.md`, so as not to repeat its findings.
  - The Scope line of plan 008.
- Named but unused: `plans/003-property-tasks.md` and `reviews/003-code-review.md`. The WAIT-004 review and the code were enough for the cases I traced.

---

## Code Review — WAIT-005 — Round 2 — [2026-11-24]

**Verdict: APPROVED**
**Logs:** rerun on fddfdbd's code. Build PASSED, 0 errors, 97 warnings, the same count as round 1 (`reviews/005-build.log`). Design build PASSED, 0 warnings (`reviews/005-design-build.log`). Tests: 2,096 passed, 2 skipped, 0 failed (`reviews/005-test.log`: Neatoo.UnitTest 1,934 + 2 skipped, BaseGenerator 51, MudNeatoo 33, Analyzers 23, Person 55). Design.Tests: 235 passed (`reviews/005-design-test.log`). Neatoo.UnitTest moves from 1,928 to 1,934, which matches the six tests the commit adds. The skipped tests are the same two as before. `reviews/005-red-busy-item.log` shows `AnItemAlreadyBusyWhenItIsAddedToAList_HasItsWorkTakenUpByTheListsOwner` failing at `owner.IsBusy` before the fix.

### Round 1 callouts, verified

1. **A busy item taken by a `ValidateListBase`: addressed.** `ValidateBase.SetParent` (`src/Neatoo/ValidateBase.cs:273-287`) now has the new parent register `RunningTasks.AllDone` when the object is running and the parent changed. Through `AddChildTask`, the registration reaches every ancestor.
   - **Every attach path goes through `SetParent`.** These are `ValidateListBase.InsertItem`/`SetItem` (`ValidateListBase.cs:137`, `:213`), the list's own `ISetParent.SetParent` (`:114-126`), and `_PropertyManager_NeatooPropertyChanged` (`ValidateBase.cs:479-481`). The last one covers `LoadValue`, so my round 1 theoretical line about `LoadValue` handed a busy child is closed too, along with a paused `EntityListBase` add.
   - **The parent accounts after the child.** `AllDone` completes only after the child's `OnFullSequenceComplete` has run (`AsyncTasks.cs:243`, then `:285`/`:289`), so the child's idle notification comes first.
   - **`Flatten()` (`AsyncTasks.cs:215`) is what makes one exception surface once.** The registered wait faults with the child's `AggregateException`. Flattened, its inner exceptions are the same objects as those of any task the parent also holds, so the de-duplication by reference still matches them.
   - **Test.** The new test fails before the fix on `owner.IsBusy`.
2. **List `IsBusy` cost: addressed.** `IsBusy` still reads the items on every read (`ValidateListBase.cs:77`), so its value cannot go stale, which is what D31 asks. Only the notification check (`BusyForChangeDetection`, `:437-440`, used at `:417` and `:448`) falls back to its last reading.
   - **When the items are read again.** `_busyMayHaveChanged` is set by an item's `IsBusy` `PropertyChanged` (`:360-363`), by inserting or replacing with a busy item (`:144-147`, `:222-224`), by any remove, replace or clear (`:171`, `:204`, `:251`), and by resume (`:560`). It starts true.
   - **`EntityListBase` goes through the base correctly.** Its `CheckIfMetaPropertiesChanged`, `ResetMetaState` and `HandlePropertyChanged` all call the base (`EntityListBase.cs:180`, `:197`, `:206`).
   - **Its overrides that send notifications before calling the base.** These are `RemoveItem`, where `MarkDeleted` runs first, and `SetItem`. Those notifications can reset the flag, but the base sets it again on its first line, so nothing is lost.
   - **The numbers and the record.** The plan's second amendment gives the measured cost and says this replaces v0.7.1's cache, which answers the gap in the record. I did not re-measure.

### Direction & shape

The two fixes go where the problems are: adoption at the one place an object gets its parent, and the cost cut only in the notification check. The value read is untouched.

The questions in the brief:
- **The child's own first wait and the `IAsyncTasks` contract.** Reading `AllDone` makes the parent's registration a wait that holds the child's sequence. By `IAsyncTasks.cs:13-16` ("reports its exceptions once, to the waits that held `AllDone` while it ran, or else to the first wait after it"), the parent's tracker is therefore the place the fault is reported. The child's own later wait is then clean. That follows the contract's letter. It differs from a child that goes busy after it is attached, whose fault both the child and the parent hold (see Theoretical).
- **Registering the same wait twice.** This happens if an object running one sequence is handed to the same parent twice with a different parent in between, for example `SetParent(A)`, `SetParent(null)`, `SetParent(A)`. It is harmless: the tracker keys tasks by `Guid`, and the flattened exceptions are de-duplicated by reference.
- **Calls that do not register.** `SetParent` with the same parent is skipped, and that covers `_PropertyManager_NeatooPropertyChanged` calling it on every write to a child-holding property. Deserialization (`OnDeserialized`, the list's `OnDeserialized`) hands parents to objects whose trackers are new and idle, so nothing is registered. `SetParent(null)` registers nothing, and nothing is taken off the old parent (see Theoretical).

### Veto-tier
None.

### Callouts
None.

### Could not verify
- The Release measurements in the plan's second amendment. No benchmark is committed, and I did not run one.
- The working tree has uncommitted test changes on top of fddfdbd, in `WaitsAwaitTasksTests.cs` and `FrameworkRegistersObjects.cs`. They add `AnAwaitedRunRules_CancelledBetweenRules_...`, `AList_AnnouncesBusyOnceAndIdleOnce_ForAWriteOnItsItem` and `ABusyItemAddedToAList_WhoseRuleThenThrows_IsReportedByTheOwnersWaitOnce`, and they tighten the throw test. Some of them do not exist in the committed `WaitsAwaitTasksTests.cs` at all, so the committed test count (1,934) cannot include them: they have not been through a full run. The third one is the only test that pins `Flatten()`. In fddfdbd as committed, nothing drives a fault through the adopted wait.

### Theoretical (not triaged)
- **A list item that changes busy while the item itself is paused.** For example, a child's factory operation awaits an async rule run while the child sits in a list. The item's `RaisePropertyChanged` is gated by `IsPaused` (`ValidateBase.cs:432`), so only its `NeatooPropertyChanged("IsBusy")` reaches the list. `HandleNeatooPropertyChanged` (`ValidateListBase.cs:340`) does not set `_busyMayHaveChanged`, so the list never announces the change.
  - The value of `list.IsBusy` stays right, and the owner's busy is its tracker, so it is unaffected.
  - Round 1's code did announce it, through the same handler. The v0.7.1 cache did not.
  - One option is to set the flag there when `PropertyName` is `IsBusy`.
- **A parent the object leaves while busy keeps waiting.** It keeps the adopted `AllDone` until the object's whole sequence ends, including work the object starts after leaving. Before this, only tasks registered while attached held the old parent. This needs an object that was busy when attached and then detached while still busy, and no caller in the repo does that.
- **The asymmetry in where a child's fault is reported.** A child busy when attached reports its fault through the parent only. A child that goes busy after attaching reports it to both its own first wait and the parent's.
- **A rule that throws an `AggregateException` itself.** With `Flatten()`, the tracker now records that exception's inner exceptions instead. An `await` on the wait then throws the first inner exception, not the rule's aggregate.
- **Unchanged from round 1.** These still stand: `RuleManager.RunRule`/`RunRules` called directly and not awaited, a paused object raising `NeatooPropertyChanged`, which now also covers `SetParent`'s registration under a paused owner, the order of the rising notifications, an un-awaited `RunRules(flag)` that observes `AllDone` taking another task's fault, and WAIT-008's Scope line.

### Read report
- Read beyond the brief:
  - `git diff eb22bd8..fddfdbd` in full, including the plan, the todo and the test diffs.
  - `src/Neatoo/IAsyncTasks.cs`.
  - The `SetParent` call sites across `src/Neatoo`.
  - The `EntityListBase` overrides: `HandlePropertyChanged`, `CheckIfMetaPropertiesChanged`, `ResetMetaState`, `RemoveItem`, `FactoryComplete`.
  - `reviews/005-red-busy-item.log`.
  - The uncommitted working-tree diff, `git diff HEAD -- src`, read only.
- Named but unused: none.
